using System.Text;
using System.Text.RegularExpressions;
using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class BirSalesSummaryRuntimeTests
{
    [Fact]
    public async Task ServiceRejectsInvalidInputBeforePersistenceAndNormalizesValidInput()
    {
        var repository = new StubRepository(new(BirSalesSummaryOutcome.Created, Record()));
        var service = new BirSalesSummaryService(repository);
        var invalid = await service.GenerateAsync(new("", Guid.Empty, Guid.Empty, "php", Guid.Empty, "", "", "", ""));
        Assert.Equal(BirSalesSummaryOutcome.InvalidRequest, invalid.Outcome);
        Assert.Equal(0, repository.GenerateCalls);

        var valid = await service.GenerateAsync(Command() with { OperationKey = " operation ", GoverningZReadingReference = " Z-TEST " });
        Assert.Equal(BirSalesSummaryOutcome.Created, valid.Outcome);
        Assert.Equal("operation", repository.LastCommand!.OperationKey);
        Assert.Equal("Z-TEST", repository.LastCommand.GoverningZReadingReference);
    }

    [Fact]
    public void SemanticHashBindsGoverningZScopeAndOperationButNotCorrelation()
    {
        var command = Command();
        var z = Guid.Parse("83000000-0000-4000-8000-000000000010");
        var first = BirSalesSummarySemanticRequestHasher.Compute(command, z);
        Assert.Equal(first, BirSalesSummarySemanticRequestHasher.Compute(command with { CorrelationId = "different" }, z));
        Assert.NotEqual(first, BirSalesSummarySemanticRequestHasher.Compute(command with { OperationKey = "different" }, z));
        Assert.NotEqual(first, BirSalesSummarySemanticRequestHasher.Compute(command, Guid.NewGuid()));
        Assert.Matches("^[0-9a-f]{64}$", first);
    }

    [Fact]
    public void JsonAndCsvAreDeterministicCompleteAndRestartIndependent()
    {
        var record = Record();
        var firstRenderer = new BirSalesSummaryOutputRenderer();
        var secondRenderer = new BirSalesSummaryOutputRenderer();
        foreach (var format in new[] { BirSalesSummaryExportFormat.Json, BirSalesSummaryExportFormat.Csv })
        {
            var first = firstRenderer.Render(record, format);
            var second = secondRenderer.Render(record, format);
            Assert.Equal(first.Bytes, second.Bytes);
            Assert.Equal(first.Sha256, second.Sha256);
            Assert.Equal($"bir-sales-summary-{record.BirSalesSummaryReportId:N}.{format.ToString().ToLowerInvariant()}", first.FileName);
            var text = Encoding.UTF8.GetString(first.Bytes);
            Assert.Contains(record.GoverningZReadingReference, text, StringComparison.Ordinal);
            Assert.Contains("senior_citizen_statutory", text, StringComparison.Ordinal);
            Assert.Contains("cash", text, StringComparison.Ordinal);
            Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("apiKey", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("beneficiary", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RendererRejectsFormatsOutsideTheAuthorizedJsonCsvSet() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BirSalesSummaryOutputRenderer().Render(Record(), (BirSalesSummaryExportFormat)99));

    [Fact]
    public void PdfIsDeterministicAndPresentsCommittedSummaryWithoutRecalculation()
    {
        var record = Record() with
        {
            HeaderProfile = Record().HeaderProfile with
            {
                PosSerialNumber = "SN-TEST-PITX-L3-001",
                MachineIdentificationNumber = "MIN-TEST-PITX-L3-001",
                PtuNumber = "PTU-TEST-PITX-L3-0001",
                BirAccreditationNumber = "ACC-TEST-2026-0001"
            }
        };
        var context = new BirSalesSummaryPdfContext("Professional Parking Management Corporation",
            "PITX, Paranaque City", "999-999-999-000", "PITX Level 3", "PITX Level 3", "ExitPass v1.3");
        var renderer = new BirSalesSummaryOutputRenderer();
        var first = renderer.Render(record, BirSalesSummaryExportFormat.Pdf, context);
        var second = renderer.Render(record, BirSalesSummaryExportFormat.Pdf, context);
        Assert.Equal(first.Bytes, second.Bytes);
        Assert.Equal("application/pdf", first.ContentType);
        Assert.Equal("daily-bir-sales-summary-technical-uat-20260803.pdf", first.FileName);
        Assert.StartsWith("%PDF-1.4", Encoding.Latin1.GetString(first.Bytes), StringComparison.Ordinal);
        var text = PdfText(first.Bytes);
        foreach (var expected in new[] { "DAILY BIR SALES SUMMARY", "TECHNICAL UAT - NOT FOR BIR SUBMISSION",
                     "TAXPAYER IDENTITY", "INVOICE RANGE",
                     "Professional Parking Management Corporation", "999-999-999-000", "SN-TEST-PITX-L3-001",
                     "MIN-TEST-PITX-L3-001", "PTU-TEST-PITX-L3-0001", "ACC-TEST-2026-0001", "2026-08-03",
                     "SI-0001", "Gross Sales", "PHP 120.00", "Net Sales", "PHP 100.00", "CASH", "Page 1 of 1" })
            Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotContain("TAXPAYER AND FISCAL IDENTITY", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FISCAL RANGE", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BIR SALES SUMMARY REPORT", text, StringComparison.Ordinal);
    }

    private static BirSalesSummaryCommand Command() => new(
        "operation", SiteId, IdentityId, "PHP", PeriodId, "Z-TEST", "actor", "service", "correlation");

    private static BirSalesSummaryRecord Record()
    {
        var amounts = new FiscalXReadingAmounts(12000, 10000, 8929, 1071, 0, 0, 2000, 2000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        return new(
            Guid.Parse("83000000-0000-4000-8000-000000000020"), Guid.Parse("83000000-0000-4000-8000-000000000021"),
            "operation", BirSalesSummaryContract.ReportKind, BirSalesSummaryContract.ContractVersion,
            BirSalesSummaryContract.SemanticHashVersion, BirSalesSummaryContract.ReportingProfile,
            Guid.Parse("83000000-0000-4000-8000-000000000010"), "Z-TEST", SiteId, IdentityId, PeriodId,
            new(2026, 8, 3), new(2026, 8, 3), new(2026, 8, 4), "PHP", 1, "SI-0001", "SI-0001", amounts,
            [new("cash", 1, 10000, "PHP")], [new("senior_citizen_statutory", 1, 2000, 0, "PHP")],
            [new(Guid.Parse("83000000-0000-4000-8000-000000000030"), "SI", 1, 1, "SI-0001", "SI-0001", 1, [], "PHP")],
            new(0, 0, 0, 1, 0, 10000, 10000, 1, 2),
            new(Guid.Parse("83000000-0000-4000-8000-000000000040"), "profile-v1", "SERIAL", "MIN", "ACCREDITATION", new(2026, 1, 1), new(2027, 1, 1), "PTU", new(2026, 1, 1)),
            DateTimeOffset.Parse("2026-08-04T01:00:00Z"), DateTimeOffset.Parse("2026-08-04T01:00:00Z"), "correlation", "BIRSS-830000000000", true);
    }

    private static readonly Guid SiteId = Guid.Parse("83000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("83000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("83000000-0000-4000-8000-000000000003");

    private sealed class StubRepository(BirSalesSummaryResult result) : IBirSalesSummaryRepository
    {
        public int GenerateCalls { get; private set; }
        public BirSalesSummaryCommand? LastCommand { get; private set; }
        public Task<BirSalesSummaryResult> GenerateAsync(BirSalesSummaryCommand command, CancellationToken cancellationToken = default)
        { GenerateCalls++; LastCommand = command; return Task.FromResult(result); }
        public Task<BirSalesSummaryResult> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private static string PdfText(byte[] bytes) => string.Join('\n', Regex.Matches(Encoding.Latin1.GetString(bytes), @"\((?<text>(?:\\.|[^\\)])*)\) Tj")
        .Select(match => Regex.Unescape(match.Groups["text"].Value)));
}
