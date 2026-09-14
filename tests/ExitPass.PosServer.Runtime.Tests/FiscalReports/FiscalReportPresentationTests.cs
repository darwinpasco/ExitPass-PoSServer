using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        Assert.Equal("X READING REPORT", x.Presentation?.Title);
        Assert.Equal("INTERIM_READ_ONLY", x.Presentation?.Finality);
        Assert.Equal("OPEN_AT_OBSERVATION", x.Presentation?.PeriodStatus);
        Assert.Null(x.Presentation?.Counters);
        Assert.Contains(x.Presentation!.FieldPostures, field => field is { Field: "grandTotalObservation", Posture: "not_recorded" });
        Assert.Equal("Z READING REPORT", z.Presentation?.Title);
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
    public void JsonCsvAndPdfOutputsAreDeterministicVersionedAndHashBound()
    {
        var presentation = new FiscalReportPresentationService().Present(ZRecord()).Presentation!;
        var renderer = new FiscalReportOutputRenderer();

        var presentationJson = renderer.CreatePresentationJson(presentation);
        var json1 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Json);
        var json2 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Json);
        var csv1 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Csv);
        var csv2 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Csv);
        var pdf1 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Pdf);
        var pdf2 = renderer.CreateExport(presentation, FiscalReportOutputFormat.Pdf);

        Assert.Equal(json1.Content, json2.Content);
        Assert.Equal(csv1.Content, csv2.Content);
        Assert.Equal(pdf1.Content, pdf2.Content);
        Assert.Equal(json1.OutputIdentity, json2.OutputIdentity);
        Assert.NotEqual(json1.OutputIdentity, csv1.OutputIdentity);
        Assert.NotEqual(csv1.OutputIdentity, pdf1.OutputIdentity);
        Assert.NotEqual(presentationJson.OutputIdentity, json1.OutputIdentity);
        Assert.StartsWith("fiscal-report-output:sha256:v1:", json1.OutputIdentity, StringComparison.Ordinal);
        Assert.Equal("application/json; charset=utf-8", presentationJson.ContentType);
        Assert.Equal("application/json; charset=utf-8", json1.ContentType);
        Assert.Equal("text/csv; charset=utf-8", csv1.ContentType);
        Assert.Equal("application/pdf", pdf1.ContentType);
        Assert.EndsWith(".pdf", pdf1.FileName, StringComparison.Ordinal);
        Assert.Contains("attachment", pdf1.ContentDisposition, StringComparison.Ordinal);
        Assert.Contains("section,key,classification,count,amount_minor_units,currency,value\n", Encoding.UTF8.GetString(csv1.Content), StringComparison.Ordinal);
        Assert.Contains("Z READING REPORT", PdfText(pdf1.Content), StringComparison.Ordinal);
        Assert.DoesNotContain("REPRINT", PdfText(pdf1.Content), StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiptTextUsesApprovedTitlesSectionsPaymentsAmountsAndPht()
    {
        var service = new FiscalReportPresentationService();
        var renderer = new FiscalReportOutputRenderer();
        var x = Text(renderer, service.Present(XRecord()).Presentation!);
        var z = Text(renderer, service.Present(ZRecord()).Presentation!);

        Assert.Equal("X READING REPORT", x.Split('\n')[0].Trim());
        Assert.Contains("(INTERIM SALES REPORT)", x, StringComparison.Ordinal);
        Assert.Equal("Z READING REPORT", z.Split('\n')[0].Trim());
        Assert.Contains("(END-OF-DAY SALES REPORT)", z, StringComparison.Ordinal);

        AssertInOrder(x,
            "TAXPAYER INFORMATION", "PTU INFORMATION", "MACHINE INFORMATION", "REPORT INFORMATION",
            "SINCE LAST Z READ", "SALES SUMMARY", "VATABLE SALES",
            "NET SALES", "PAYMENT METHODS", "Printed Date", "Operator", "NOTHING FOLLOWS");
        AssertInOrder(z,
            "TAXPAYER INFORMATION", "PTU INFORMATION", "MACHINE INFORMATION", "REPORT INFORMATION",
            "COUNTERS", "SALES SUMMARY", "VATABLE SALES", "NET SALES",
            "PAYMENT METHODS", "Printed Date", "Operator", "NOTHING FOLLOWS");

        foreach (var report in new[] { x, z })
        foreach (var method in new[] { "CASH", "CARD", "QRPH", "GCASH", "MAYA" })
            Assert.Single(report.Split('\n'), line => line.StartsWith(method, StringComparison.Ordinal));
        AssertPayment(x, "CASH", 2, "PHP 100.00");
        AssertPayment(x, "CARD", 0, "PHP 0.00");
        AssertPayment(x, "QRPH", 0, "PHP 0.00");
        AssertPayment(x, "GCASH", 0, "PHP 0.00");
        AssertPayment(x, "MAYA", 0, "PHP 0.00");
        Assert.DoesNotContain("BANK TRANSFER", x, StringComparison.Ordinal);
        Assert.DoesNotContain("BANK TRANSFER", z, StringComparison.Ordinal);

        Assert.Contains("2026-08-06 16:00:00 PHT", x, StringComparison.Ordinal);
        Assert.Contains("2026-08-07 00:00:00 PHT", z, StringComparison.Ordinal);
        Assert.Contains("Printed Date", x, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-08-06T08:00:00Z", x, StringComparison.Ordinal);
        Assert.Contains("Gross Sales", x, StringComparison.Ordinal);
        Assert.Contains("PHP 120.00", x, StringComparison.Ordinal);
        Assert.Contains("Net Sales", x, StringComparison.Ordinal);
        Assert.Contains("PHP 100.00", x, StringComparison.Ordinal);
        Assert.Contains("Z Counter", z, StringComparison.Ordinal);
        Assert.Contains("12", z, StringComparison.Ordinal);
        var compactX = string.Concat(x.Where(character => !char.IsWhiteSpace(character)));
        Assert.Contains("Registered Name", x, StringComparison.Ordinal);
        Assert.Contains("Trade Name", x, StringComparison.Ordinal);
        Assert.Contains("ProfessionalParkingManagementCorporation", compactX, StringComparison.Ordinal);
        Assert.Contains("TIN", x, StringComparison.Ordinal);
        Assert.Contains("999-999-999-000", x, StringComparison.Ordinal);
        Assert.Contains("VAT Type", x, StringComparison.Ordinal);
        Assert.Contains("Vat-able", x, StringComparison.Ordinal);
        Assert.Contains("Branch Code", x, StringComparison.Ordinal);
        Assert.Contains("PITX Level 3", x, StringComparison.Ordinal);
        Assert.Contains("PTU-TEST-PITX-L3-0001", compactX, StringComparison.Ordinal);
        Assert.Contains("PTU Issue Date:       2026-01-01", x, StringComparison.Ordinal);
        Assert.Contains("Valid Until:          2031-01-01", x, StringComparison.Ordinal);
        Assert.Contains("Last Z Counter:             0000", x, StringComparison.Ordinal);
        Assert.Contains("Last Z Closed:              0000", x, StringComparison.Ordinal);
        Assert.Contains("Voids:                         0", x, StringComparison.Ordinal);
        Assert.Contains("Cancelled:                     0", x, StringComparison.Ordinal);
        Assert.Contains("Returns:                       0", x, StringComparison.Ordinal);
        foreach (var report in new[] { x, z })
        {
            Assert.DoesNotContain("NOT RECORDED", report, StringComparison.Ordinal);
            Assert.DoesNotContain("PARKING METRICS", report, StringComparison.Ordinal);
            Assert.DoesNotContain("Output Identity", report, StringComparison.Ordinal);
            Assert.DoesNotContain("Print Contract", report, StringComparison.Ordinal);
            Assert.DoesNotContain(XRecord().FiscalIdentityId.ToString("D"), report, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(XRecord().SitePosServerId.ToString("D"), report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Operator:                 SYSTEM", report, StringComparison.Ordinal);
        }
        Assert.Equal(Amounts(), service.Present(XRecord()).Presentation?.Amounts);
        Assert.Equal(ZRecord().CounterSnapshot.ResultingZCounterValue, service.Present(ZRecord()).Presentation?.Counters?.ResultingZCounterValue);
    }

    [Fact]
    public void WalletMethodsRemainDistinctAndUndifferentiatedWalletsAreNotInvented()
    {
        var service = new FiscalReportPresentationService();
        var renderer = new FiscalReportOutputRenderer();
        var distinct = XRecord() with
        {
            Tenders =
            [
                new("qrph", 1, 2_000, "PHP"),
                new("gcash", 1, 3_000, "PHP"),
                new("maya", 1, 5_000, "PHP")
            ]
        };
        var distinctText = Text(renderer, service.Present(distinct).Presentation!);

        AssertPayment(distinctText, "QRPH", 1, "PHP 20.00");
        AssertPayment(distinctText, "GCASH", 1, "PHP 30.00");
        AssertPayment(distinctText, "MAYA", 1, "PHP 50.00");

        var aggregateOnly = XRecord() with { Tenders = [new("digital_wallet", 2, 10_000, "PHP")] };
        var aggregateText = Text(renderer, service.Present(aggregateOnly).Presentation!);
        AssertPayment(aggregateText, "QRPH", 0, "PHP 0.00");
        AssertPayment(aggregateText, "GCASH", 0, "PHP 0.00");
        AssertPayment(aggregateText, "MAYA", 0, "PHP 0.00");
        Assert.DoesNotContain("SOURCE CATEGORY", aggregateText, StringComparison.Ordinal);
        Assert.Contains("TOTAL PAYMENTS", aggregateText, StringComparison.Ordinal);
        Assert.Contains("PHP 100.00", aggregateText, StringComparison.Ordinal);
        Assert.DoesNotContain("BANK TRANSFER", aggregateText, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUsesExact57MmWidthAndReceiptLinesDoNotOverflow()
    {
        var record = XRecord() with { FiscalReportReference = "X-VERY-LONG-AUTHORITATIVE-REFERENCE-1234567890" };
        var presentation = new FiscalReportPresentationService().Present(record).Presentation!;
        var artifact = new FiscalReportOutputRenderer().CreateExport(
            presentation,
            FiscalReportOutputFormat.Pdf,
            FiscalReportPrintWidthProfile.Narrow,
            ReceiptProfile());
        var pdf = Encoding.ASCII.GetString(artifact.Content);
        var text = PdfText(artifact.Content);

        Assert.StartsWith("%PDF-1.4", pdf, StringComparison.Ordinal);
        var mediaBox = Regex.Match(pdf, @"/MediaBox \[0 0 (?<width>[0-9.]+) (?<height>[0-9.]+)\]");
        Assert.True(mediaBox.Success);
        Assert.Equal(FiscalReportOutputRenderer.PdfPageWidthPoints,
            double.Parse(mediaBox.Groups["width"].Value, System.Globalization.CultureInfo.InvariantCulture), 4);
        Assert.Equal(57d, FiscalReportOutputRenderer.PdfPageWidthPoints * 25.4d / 72d, 3);
        Assert.True(double.Parse(mediaBox.Groups["height"].Value, System.Globalization.CultureInfo.InvariantCulture) > 36d);
        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), line => Assert.True(line.Length <= 32, line));
        Assert.Contains(
            "X-VERY-LONG-AUTHORITATIVE-REFERENCE-1234567890",
            text.Replace("\n", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Contains("NOTHING FOLLOWS", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionalMachineAndInvoiceFieldsAreOmittedWhenUnavailable()
    {
        var presentation = new FiscalReportPresentationService().Present(XRecord() with
        {
            FiscalNumberRanges = []
        }).Presentation!;
        var profile = ReceiptProfile() with
        {
            MachineIdentificationNumber = null,
            TerminalId = null,
            SerialNumber = null,
            Model = null,
            SoftwareVersion = null,
            Location = null
        };
        var receipt = PdfText(new FiscalReportOutputRenderer().CreateExport(
            presentation,
            FiscalReportOutputFormat.Pdf,
            FiscalReportPrintWidthProfile.Narrow,
            profile).Content);

        Assert.DoesNotContain("MACHINE INFORMATION", receipt, StringComparison.Ordinal);
        Assert.DoesNotContain("Beginning Invoice", receipt, StringComparison.Ordinal);
        Assert.DoesNotContain("Ending Invoice", receipt, StringComparison.Ordinal);
        Assert.DoesNotContain("NOT RECORDED", receipt, StringComparison.Ordinal);
        Assert.DoesNotContain("Surcharges", receipt, StringComparison.Ordinal);
        Assert.DoesNotContain("Cancel Amount", receipt, StringComparison.Ordinal);
        Assert.DoesNotContain("VAT BREAKDOWN", receipt, StringComparison.Ordinal);
    }

    [Fact]
    public void FormattingIsInvariantAndSupportsExplicitNegativeAndZeroValues()
    {
        Assert.Equal("PHP 0.00", FiscalReportOutputRenderer.FormatAmount(0, "PHP"));
        Assert.Equal("PHP -12.34", FiscalReportOutputRenderer.FormatAmount(-1_234, "PHP"));
        Assert.Equal("USD 12.34", FiscalReportOutputRenderer.FormatAmount(1_234, "USD"));
        Assert.True(FiscalReportOutputRenderer.TryParseFormat("pdf", out var pdf));
        Assert.Equal(FiscalReportOutputFormat.Pdf, pdf);
        Assert.False(FiscalReportOutputRenderer.TryParseFormat("text", out _));
        Assert.False(FiscalReportOutputRenderer.TryParseFormat("txt", out _));
    }

    private static string Text(FiscalReportOutputRenderer renderer, FiscalReportPresentationModel presentation) =>
        PdfText(renderer.CreateExport(
            presentation,
            FiscalReportOutputFormat.Pdf,
            FiscalReportPrintWidthProfile.Narrow,
            ReceiptProfile()).Content);

    private static string PdfText(byte[] bytes)
    {
        var pdf = Encoding.ASCII.GetString(bytes);
        return string.Join('\n', Regex.Matches(pdf, @"\((?<text>(?:\\.|[^\\)])*)\) Tj")
            .Select(match => Regex.Unescape(match.Groups["text"].Value)));
    }

    private static void AssertInOrder(string text, params string[] values)
    {
        var previous = -1;
        foreach (var value in values)
        {
            var current = text.IndexOf(value, StringComparison.Ordinal);
            Assert.True(current > previous, $"'{value}' was missing or out of order.");
            previous = current;
        }
    }

    private static void AssertPayment(string text, string method, long count, string amount)
    {
        var line = Assert.Single(text.Split('\n'), candidate => candidate.StartsWith(method, StringComparison.Ordinal));
        Assert.Contains(count.ToString(System.Globalization.CultureInfo.InvariantCulture), line, StringComparison.Ordinal);
        Assert.Contains(amount, line, StringComparison.Ordinal);
    }

    private static FiscalReportReceiptProfile ReceiptProfile() => new(
        "Professional Parking Management Corporation",
        "Professional Parking Management Corporation",
        "999-999-999-000",
        "Vat-able",
        "PITX Level 3",
        "PTU-TEST-PITX-L3-0001",
        new DateOnly(2026, 1, 1),
        new DateOnly(2031, 1, 1),
        "MIN-TEST-PITX-L3-001",
        null,
        "SN-TEST-PITX-L3-001",
        null,
        null,
        "PITX Level 3");

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
        1_500,
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
