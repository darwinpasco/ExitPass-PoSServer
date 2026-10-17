using System.Text;
using System.Text.RegularExpressions;
using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class AuditTrailRuntimeTests
{
    [Fact]
    public void FiscalAdjustmentUsesAdjustmentCategoryAndDoesNotExposeRecovery()
    {
        var definition = Assert.Single(
            AuditTrailActionCatalog.Definitions,
            item => item.Code == "fiscal_adjustment_recorded");

        Assert.Equal("ADJUSTMENT", definition.DisplayName);
        Assert.Equal("ADJUSTMENT", AuditTrailActionCatalog.Category("fiscal_adjustment_recorded"));
        Assert.DoesNotContain(
            AuditTrailActionCatalog.Definitions,
            item => item.DisplayName == "RECOVERY");
    }

    [Fact]
    public async Task ServiceRequiresABoundedSafeRangeAndNormalizesFilters()
    {
        var repository = new StubRepository();
        var service = new AuditTrailService(repository);

        Assert.Equal(AuditTrailOutcome.InvalidRequest,
            (await service.ReadAsync(new(Site, default, default))).Outcome);
        Assert.Equal(AuditTrailOutcome.RangeTooLarge,
            (await service.ReadAsync(new(Site, From, From.AddDays(32)))).Outcome);
        Assert.Equal(AuditTrailOutcome.InvalidRequest,
            (await service.ReadAsync(new(Site, From, To, Actor: "unsafe\nactor"))).Outcome);

        var result = await service.ReadAsync(new(Site, From, To, " sale ", " operator ", " allowed ", " SI-1 "));
        Assert.Equal(AuditTrailOutcome.Succeeded, result.Outcome);
        Assert.Equal("sale", repository.Query!.ActionCode);
        Assert.Equal("operator", repository.Query.Actor);
        Assert.Equal("allowed", repository.Query.Result);
        Assert.Equal("SI-1", repository.Query.Reference);
    }

    [Fact]
    public void JsonCsvAndPdfAreDeterministicAndPreserveTheGovernedTenColumnProjection()
    {
        var renderer = new AuditTrailOutputRenderer();
        var report = Report();

        foreach (var format in Enum.GetValues<AuditTrailExportFormat>())
        {
            var first = renderer.Render(report, format);
            var second = renderer.Render(report, format);
            Assert.Equal(first.Bytes, second.Bytes);
            Assert.Equal(first.Sha256, second.Sha256);
            Assert.Equal($"audit-trail-20261017-20261017.{format.ToString().ToLowerInvariant()}", first.FileName);
        }

        var json = Encoding.UTF8.GetString(renderer.Render(report, AuditTrailExportFormat.Json).Bytes);
        Assert.Contains("\"actionCodes\"", json, StringComparison.Ordinal);
        Assert.Contains("\"fiscal_document_committed\"", json, StringComparison.Ordinal);
        Assert.Contains("\"correlationReference\":\"safe-correlation\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);

        var csv = Encoding.UTF8.GetString(renderer.Render(report, AuditTrailExportFormat.Csv).Bytes);
        Assert.StartsWith("Date,Terminal,UserID,User Role,Action Category,Action Description,SI No.,Before Value,After Value,Reason/Remarks\r\n", csv, StringComparison.Ordinal);
        Assert.Contains("2026-10-17\r\n12:09:10", csv, StringComparison.Ordinal);
        Assert.EndsWith("\r\n", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", csv.Replace("\r\n", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);

        var pdf = renderer.Render(report, AuditTrailExportFormat.Pdf);
        Assert.StartsWith("%PDF-1.4", Encoding.ASCII.GetString(pdf.Bytes), StringComparison.Ordinal);
        var text = PdfText(pdf.Bytes);
        Assert.Contains("Audit Trail Report", text, StringComparison.Ordinal);
        Assert.Contains("Address: Unit 2103A-B", text, StringComparison.Ordinal);
        Assert.Contains("Action", text, StringComparison.Ordinal);
        Assert.Contains("Category", text, StringComparison.Ordinal);
        Assert.Contains("Reason/", text, StringComparison.Ordinal);
        Assert.Contains("Plate:ABC1264", text, StringComparison.Ordinal);
        Assert.Contains("Page 1 of 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfWrapsLongRowsAndRepeatsTheTenColumnHeaderOnContinuationPages()
    {
        var longDescription = string.Join(' ', Enumerable.Repeat("authoritative", 45));
        var rows = Enumerable.Range(1, 22).Select(index => Row(index, longDescription)).ToArray();
        var report = Report() with { Rows = rows };

        var bytes = new AuditTrailOutputRenderer().Render(report, AuditTrailExportFormat.Pdf).Bytes;
        var text = PdfText(bytes);
        Assert.Contains("Page 2 of", text, StringComparison.Ordinal);
        Assert.True(Regex.Matches(text, "Action", RegexOptions.CultureInvariant).Count >= 2);
        Assert.Contains("authoritative", text, StringComparison.Ordinal);
    }

    private static AuditTrailReport Report() => new(
        AuditTrailContract.ContractVersion, Site, From, To,
        new("Professional Parking Management Corporation", "Unit 2103A-B, East Tower, Tektite Towers, Pasig City",
            "999-999-999-000", "MIN-TEST-PITX-L3-001", "SN-TEST-PITX-L3-001", "PITX Level 3"),
        [new("fiscal_document_committed", "SALES TRANSACTION", "A Sales Invoice was committed.")],
        [Row(1, "Plate:ABC1264\nEntry:\n2026-10-17\n09:40:00\nGross:50.00\nNet:P 50.00\nDiscount:P 0.00\nPayment:CARD")]);

    private static AuditTrailRow Row(int index, string description) => new(
        $"audit-{index:000}", DateTimeOffset.Parse("2026-10-17T04:09:10Z").AddSeconds(index - 1), "PITX-L3-WEBPAY-01",
        "System", "SYSTEM", "fiscal_document_committed", "SALES TRANSACTION", description,
        $"SI-{index:00000000}", "P 0.00", "P 50.00", "-", "ALLOWED", "SERVICE", "central-pms",
        "FISCAL_DOCUMENT", $"document-{index}", Guid.NewGuid(), null, "safe-correlation",
        new Dictionary<string, string?> { ["businessDate"] = "2026-10-17" });

    private static string PdfText(byte[] bytes)
    {
        var pdf = Encoding.ASCII.GetString(bytes);
        return string.Join('\n', Regex.Matches(pdf, @"\((?<text>(?:\\.|[^\\)])*)\) Tj")
            .Select(match => Regex.Unescape(match.Groups["text"].Value)));
    }

    private static readonly Guid Site = Guid.Parse("a1000000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset From = DateTimeOffset.Parse("2026-10-16T16:00:00Z");
    private static readonly DateTimeOffset To = DateTimeOffset.Parse("2026-10-17T16:00:00Z");

    private sealed class StubRepository : IAuditTrailRepository
    {
        public AuditTrailQuery? Query { get; private set; }
        public Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult(new AuditTrailResult(AuditTrailOutcome.Succeeded, Report()));
        }

        public Task RecordExportAsync(Guid sitePosServerId, string reportKind, string subjectReference, string format,
            string actorReference, string correlationReference, string contentSha256, int rowCount,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
