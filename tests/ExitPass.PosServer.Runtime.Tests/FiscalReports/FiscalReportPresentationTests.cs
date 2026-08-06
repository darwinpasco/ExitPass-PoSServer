using System.Text;
using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class FiscalReportPresentationTests
{
    [Fact]
    public void ValidXAndZPresentationsRemainDistinctAndUseStoredFacts()
    {
        var service = new FiscalReportPresentationService();

        var x = service.Present(XRecord());
        var z = service.Present(ZRecord());

        Assert.True(x.Succeeded);
        Assert.True(z.Succeeded);
        Assert.Equal("X READING", x.Presentation?.Title);
        Assert.Equal("INTERIM_READ_ONLY", x.Presentation?.Finality);
        Assert.Equal("OPEN_AT_OBSERVATION", x.Presentation?.PeriodStatus);
        Assert.Null(x.Presentation?.Counters);
        Assert.Contains(x.Presentation!.FieldPostures, field => field is { Field: "grandTotalObservation", Posture: "not_recorded" });
        Assert.Equal("Z READING", z.Presentation?.Title);
        Assert.Equal("IMMUTABLE_CLOSED", z.Presentation?.Finality);
        Assert.Equal("CLOSED", z.Presentation?.PeriodStatus);
        Assert.Equal(7, z.Presentation?.PeriodSequence);
        Assert.Equal(12, z.Presentation?.Counters?.ResultingZCounterValue);
        Assert.Equal(3, z.Presentation?.Counters?.ResultingResetCounterValue);
        Assert.Equal(60_000, z.Presentation?.Counters?.ResultingGrandTotalAmountMinorUnits);
        Assert.Equal(Amounts(), x.Presentation?.Amounts);
        Assert.Equal(Amounts(), z.Presentation?.Amounts);
        Assert.Equal("RECONCILED", z.Presentation?.Reconciliation.Status);
        Assert.DoesNotContain("beneficiary", JsonSerializer.Serialize(z.Presentation), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnsupportedVersionWrongKindAndMalformedSnapshotsFailClosed()
    {
        var service = new FiscalReportPresentationService();

        Assert.Equal(FiscalReportPresentationErrorCode.UnsupportedVersion,
            service.Present(XRecord() with { ContractVersion = "future:v2" }).ErrorCode);
        Assert.Equal(FiscalReportPresentationErrorCode.WrongReportKind,
            service.Present(XRecord() with { ReportKind = "Z_READING" }).ErrorCode);
        Assert.Equal(FiscalReportPresentationErrorCode.NotFinalized,
            service.Present(XRecord() with { ReportStatus = "PROCESSING" }).ErrorCode);
        Assert.Equal(FiscalReportPresentationErrorCode.MalformedSnapshot,
            service.Present(XRecord() with { Immutable = false }).ErrorCode);
        Assert.Equal(FiscalReportPresentationErrorCode.MalformedSnapshot,
            service.Present(XRecord() with { Tenders = [new("cash", 2, 9_999, "PHP")] }).ErrorCode);
        Assert.Equal(FiscalReportPresentationErrorCode.MalformedSnapshot,
            service.Present(ZRecord() with
            {
                CounterSnapshot = ZRecord().CounterSnapshot with { ResultingZCounterValue = 99 }
            }).ErrorCode);
    }

    [Fact]
    public void JsonCsvAndTextOutputsAreDeterministicVersionedAndHashBound()
    {
        var presentation = new FiscalReportPresentationService().Present(ZRecord()).Presentation!;
        var renderer = new FiscalReportOutputRenderer();

        var presentationJson = renderer.CreatePresentationJson(presentation);
        var json1 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Json);
        var json2 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Json);
        var csv1 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Csv);
        var csv2 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Csv);
        var text1 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Text, FiscalReportPrintWidthProfile.Standard);
        var text2 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Text, FiscalReportPrintWidthProfile.Standard);

        Assert.Equal(json1.Content, json2.Content);
        Assert.Equal(csv1.Content, csv2.Content);
        Assert.Equal(text1.Content, text2.Content);
        Assert.Equal(json1.OutputIdentity, json2.OutputIdentity);
        Assert.NotEqual(json1.OutputIdentity, csv1.OutputIdentity);
        Assert.NotEqual(csv1.OutputIdentity, text1.OutputIdentity);
        Assert.NotEqual(presentationJson.OutputIdentity, json1.OutputIdentity);
        Assert.StartsWith("fiscal-report-output:sha256:v1:", json1.OutputIdentity, StringComparison.Ordinal);
        Assert.Equal("application/json; charset=utf-8", presentationJson.ContentType);
        Assert.Equal("application/json; charset=utf-8", json1.ContentType);
        Assert.Equal("text/csv; charset=utf-8", csv1.ContentType);
        Assert.Equal("text/plain; charset=utf-8", text1.ContentType);
        Assert.Contains("section,key,classification,count,amount_minor_units,currency,value\n", Encoding.UTF8.GetString(csv1.Content), StringComparison.Ordinal);
        Assert.Contains("Z READING", Encoding.UTF8.GetString(text1.Content), StringComparison.Ordinal);
        Assert.DoesNotContain("REPRINT", Encoding.UTF8.GetString(text1.Content), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(FiscalReportPrintWidthProfile.Narrow, 32)]
    [InlineData(FiscalReportPrintWidthProfile.Standard, 48)]
    [InlineData(FiscalReportPrintWidthProfile.Office, 80)]
    public void PrintProfilesWrapWithoutTruncatingSafeReferences(FiscalReportPrintWidthProfile profile, int expectedWidth)
    {
        var record = XRecord() with { FiscalReportReference = "X-VERY-LONG-AUTHORITATIVE-REFERENCE-1234567890" };
        var presentation = new FiscalReportPresentationService().Present(record).Presentation!;
        var artifact = new FiscalReportOutputRenderer().CreateExport(presentation, FiscalReportOutputFormat.Text, profile);
        var text = Encoding.UTF8.GetString(artifact.Content);

        Assert.Equal(expectedWidth, FiscalReportOutputRenderer.Width(profile));
        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), line => Assert.True(line.Length <= expectedWidth, line));
        Assert.Contains(
            "X-VERY-LONG-AUTHORITATIVE-REFERENCE-1234567890",
            text.Replace("\n", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Contains("THIS X READING DOES NOT CLOSE", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FormattingIsInvariantAndSupportsExplicitNegativeAndZeroValues()
    {
        Assert.Equal("PHP 0.00", FiscalReportOutputRenderer.FormatAmount(0, "PHP"));
        Assert.Equal("PHP -12.34", FiscalReportOutputRenderer.FormatAmount(-1_234, "PHP"));
        Assert.Equal("USD 12.34", FiscalReportOutputRenderer.FormatAmount(1_234, "USD"));
        Assert.True(FiscalReportOutputRenderer.TryParseWidthProfile("58mm", out var narrow));
        Assert.Equal(FiscalReportPrintWidthProfile.Narrow, narrow);
        Assert.False(FiscalReportOutputRenderer.TryParseWidthProfile("unknown", out _));
        Assert.False(FiscalReportOutputRenderer.TryParseFormat("pdf", out _));
    }

    private static FiscalXReadingRecord XRecord() => new(
        Guid.Parse("81000000-0000-4000-8000-000000000001"),
        "X-20260806-0001",
        Guid.Parse("81000000-0000-4000-8000-000000000002"),
        "x-operation-001",
        "X_READING",
        FiscalXReadingContract.ContractVersion,
        FiscalXReadingContract.SemanticHashVersion,
        Guid.Parse("81000000-0000-4000-8000-000000000003"),
        Guid.Parse("81000000-0000-4000-8000-000000000004"),
        Guid.Parse("81000000-0000-4000-8000-000000000005"),
        new DateOnly(2026, 8, 6),
        DateTimeOffset.Parse("2026-08-05T16:00:00Z"),
        DateTimeOffset.Parse("2026-08-06T16:00:00Z"),
        "Asia/Manila",
        new TimeOnly(0, 0),
        "PHP",
        DateTimeOffset.Parse("2026-08-06T08:00:00Z"),
        DateTimeOffset.Parse("2026-08-06T08:00:01Z"),
        2,
        Amounts(),
        [new("cash", 2, 10_000, "PHP")],
        [new("coupon", 1, 500, 0, "PHP"), new("senior_citizen_statutory", 1, 1_000, 0, "PHP"), new("vat_exemption_adjustment", 1, 0, 500, "PHP")],
        [new(Guid.Parse("81000000-0000-4000-8000-000000000006"), "SI", 1, 2, "SI-0001", "SI-0002", 2, [], "PHP")],
        "correlation-001",
        "X-20260806-0001",
        true,
        7,
        "COMMITTED");

    private static FiscalZReadingRecord ZRecord()
    {
        var x = XRecord();
        return new(
            Guid.Parse("82000000-0000-4000-8000-000000000001"),
            "Z-20260806-00000012",
            Guid.Parse("82000000-0000-4000-8000-000000000002"),
            "z-operation-001",
            "Z_READING",
            FiscalZReadingContract.ContractVersion,
            FiscalZReadingContract.SemanticHashVersion,
            x.SitePosServerId,
            x.FiscalIdentityId,
            x.FiscalReportingPeriodId,
            Guid.Parse("82000000-0000-4000-8000-000000000003"),
            x.BusinessDayDate,
            x.PeriodStartAt,
            x.PeriodEndAt,
            x.ReportingTimezoneName,
            x.BusinessDayCutoffLocalTime,
            x.CurrencyCode,
            x.PeriodEndAt,
            x.PeriodEndAt.AddSeconds(1),
            x.PeriodEndAt.AddSeconds(1),
            x.QualifyingDocumentCount,
            x.Amounts,
            x.Tenders,
            x.Discounts,
            x.FiscalNumberRanges,
            new(3, 3, 11, 12, 50_000, 10_000, 60_000, 9, 10),
            "CLOSED",
            "correlation-002",
            "Z-20260806-00000012",
            true,
            7,
            "COMMITTED");
    }

    private static FiscalXReadingAmounts Amounts() => new(
        12_000,
        10_000,
        8_000,
        960,
        2_000,
        0,
        2_000,
        1_000,
        0,
        0,
        500,
        500,
        0,
        0,
        0,
        0,
        0,
        0);
}
