using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class MonthlyESalesRuntimeTests
{
    [Fact]
    public void CompletePastMonthAggregatesCommittedBirSummariesExactly()
    {
        var result = MonthlyESalesAggregator.Build(Query(), new(2026, 10, 16),
        [
            Source(new(2026,9,1), 1, gross: 100_000, vatable: 80_000, vat: 9_600, exempt: 10_000, zero: 400, ending: "SI-00000010", transactions: 10),
            Source(new(2026,9,2), 2, gross: 95_000, vatable: 75_000, vat: 9_000, exempt: 11_000, zero: 0, ending: "SI-00000018", transactions: 8)
        ]);

        Assert.Equal(MonthlyESalesOutcome.Succeeded, result.Outcome);
        var report = Assert.IsType<MonthlyESalesReport>(result.Report);
        var row = Assert.Single(report.Rows);
        Assert.Equal(195_000, row.MonthlySalesAmountMinorUnits);
        Assert.Equal("1950.00", row.MonthlySalesDisplay);
        Assert.Equal(155_000, row.VatableSalesAmountMinorUnits);
        Assert.Equal(18_600, row.VatAmountMinorUnits);
        Assert.Equal(21_000, row.VatExemptSalesAmountMinorUnits);
        Assert.Equal(400, row.ZeroRatedSalesAmountMinorUnits);
        Assert.Equal(18, row.TransactionCount);
        Assert.Equal("SI-00000018", row.EndingSalesInvoiceReference);
        Assert.Equal(new DateOnly(2026,10,10), report.SubmissionDueDate);
        Assert.Equal("ODD_TIN", report.SubmissionDueDateBasis);
        Assert.Equal(MonthlyESalesSubmissionReadiness.TechnicalUat, report.SubmissionReadiness);
    }

    [Fact]
    public void CurrentMonthIsMonthToDateAndFutureMonthIsRejected()
    {
        var current = MonthlyESalesAggregator.Build(Query() with { CalendarMonth = 10 }, new(2026,10,16),
        [
            Source(new(2026,10,1),1,gross:10_000),
            Source(new(2026,10,16),2,gross:90_000) with { PeriodClosed = false, GoverningZCommitted = false, BirSalesSummaryCommitted = false, BirSalesSummaryCommittedAt = null }
        ]);
        Assert.Equal(MonthlyESalesOutcome.Succeeded, current.Outcome);
        Assert.Equal(MonthlyESalesCoverageStatus.MonthToDate, current.Report!.CoverageStatus);
        Assert.Equal(10_000, Assert.Single(current.Report.Rows).MonthlySalesAmountMinorUnits);
        Assert.Equal(new DateOnly(2026, 10, 1), current.Report.LastCoveredBusinessDate);
        Assert.Null(current.Report.SubmissionDueDate);
        Assert.Contains("month-to-date", current.Report.SubmissionReadinessMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(MonthlyESalesOutcome.MonthNotComplete,
            MonthlyESalesAggregator.Build(Query() with { CalendarMonth = 11 }, new(2026,10,16), [Source(new(2026,11,1),1)]).Outcome);
    }

    [Fact]
    public void MissingCommittedSourcesIdentifyExactSafeDates()
    {
        var result = MonthlyESalesAggregator.Build(Query(), new(2026,10,16),
        [
            Source(new(2026,9,4),1) with { GoverningZCommitted = false, BirSalesSummaryCommitted = false },
            Source(new(2026,9,5),2) with { BirSalesSummaryCommitted = false }
        ]);
        Assert.Equal(MonthlyESalesOutcome.IncompleteAuthoritativeSources, result.Outcome);
        Assert.Contains("committed Z Readings are missing for: 2026-09-04", result.SafeMessage);
        Assert.Contains("committed BIR Sales Summaries are missing for: 2026-09-05", result.SafeMessage);
    }

    [Fact]
    public void DistinctMachinesProduceSeparateDeterministicallyOrderedRows()
    {
        var result = MonthlyESalesAggregator.Build(Query(), new(2026,10,16),
        [
            Source(new(2026,9,2),2,min:"200000002",gross:20_000,ending:"SI-20"),
            Source(new(2026,9,1),1,min:"100000001",gross:10_000,ending:"SI-10")
        ]);
        Assert.Equal(new[] { "100000001", "200000002" }, result.Report!.Rows.Select(row => row.MachineIdentificationNumber));
        Assert.Equal(new long[] { 10_000, 20_000 }, result.Report.Rows.Select(row => row.MonthlySalesAmountMinorUnits));
    }

    [Fact]
    public void CompletedNoSalesMonthStillProducesMachineRow()
    {
        var result = MonthlyESalesAggregator.Build(Query(), new(2026,10,16),
            [Source(new(2026,9,1),1,min:"100000001",gross:0,ending:null,transactions:0)]);
        var row = Assert.Single(result.Report!.Rows);
        Assert.Equal(0, row.MonthlySalesAmountMinorUnits);
        Assert.Equal("0.00", row.MonthlySalesDisplay);
        Assert.Null(row.EndingSalesInvoiceReference);
    }

    [Fact]
    public void NonzeroActivityWithoutEndRangeAndOverflowFailClosed()
    {
        Assert.Equal(MonthlyESalesOutcome.MissingEndRange,
            MonthlyESalesAggregator.Build(Query(), new(2026,10,16), [Source(new(2026,9,1),1,ending:null,transactions:1)]).Outcome);
        Assert.Equal(MonthlyESalesOutcome.ArithmeticOverflow,
            MonthlyESalesAggregator.Build(Query(), new(2026,10,16),
            [Source(new(2026,9,1),1,gross:long.MaxValue),Source(new(2026,9,2),2,gross:1)]).Outcome);
    }

    [Fact]
    public void TinParityAndInvalidTinRemainExplicit()
    {
        var even = MonthlyESalesAggregator.Build(Query(), new(2026,10,16), [Source(new(2026,9,1),1,tin:"123-456-788-000")]).Report!;
        Assert.Equal(new DateOnly(2026,10,8), even.SubmissionDueDate);
        Assert.Equal("EVEN_TIN", even.SubmissionDueDateBasis);
        var invalid = MonthlyESalesAggregator.Build(Query(), new(2026,10,16), [Source(new(2026,9,1),1,tin:"TIN-PENDING")]).Report!;
        Assert.Null(invalid.SubmissionDueDate);
        Assert.Equal(MonthlyESalesSubmissionReadiness.FilingMetadataIncomplete, invalid.SubmissionReadiness);
    }

    [Fact]
    public void JsonAndBirJobAidCsvAreDeterministicAndUseExactAmountFormat()
    {
        var report = MonthlyESalesAggregator.Build(Query(), new(2026,10,16),
            [Source(new(2026,9,1),1,gross:2_500_000,ending:"SI-99")]).Report!;
        var renderer = new MonthlyESalesOutputRenderer();
        var csv1 = renderer.Render(report, MonthlyESalesExportFormat.Csv);
        var csv2 = renderer.Render(report, MonthlyESalesExportFormat.Csv);
        Assert.Equal(csv1.Bytes, csv2.Bytes);
        var text = Encoding.UTF8.GetString(csv1.Bytes);
        Assert.StartsWith("MACHINE IDENTIFICATION NO. of CRM/POS,MONTH,YEAR,MONTHLY SALES,END RANGE OF OFFICIAL RECEIPT\r\n", text);
        Assert.Contains("MIN-TEST-PITX-L3-001,09,2026,25000.00,SI-99\r\n", text);
        Assert.DoesNotContain("25,000.00", text);
        Assert.Contains("technical-uat", csv1.FileName);
        var json1 = renderer.Render(report, MonthlyESalesExportFormat.Json);
        var json2 = renderer.Render(report, MonthlyESalesExportFormat.Json);
        Assert.Equal(json1.Bytes, json2.Bytes);
        using var document = JsonDocument.Parse(json1.Bytes);
        Assert.Equal(2_500_000, document.RootElement.GetProperty("report").GetProperty("rows")[0].GetProperty("monthlySalesAmountMinorUnits").GetInt64());
    }

    [Fact]
    public void PdfIsDeterministicHumanReadableAndUsesOnlyGovernedDailyRows()
    {
        var sources = new[]
        {
            Source(new(2026,9,1),1,gross:100_000,vatable:80_000,vat:9_600,ending:"SI-10",transactions:10),
            Source(new(2026,9,3),2,gross:95_000,vatable:75_000,vat:9_000,ending:"SI-18",transactions:8)
        };
        var report = MonthlyESalesAggregator.Build(Query(), new(2026,10,16), sources).Report!;
        var total = Assert.Single(report.Rows);
        var pdfReport = new MonthlyESalesPdfReport(report, "Professional Parking Management Corporation",
            "PITX, Paranaque City", "PITX Level 3", "ExitPass v1.3", DateTimeOffset.Parse("2026-10-01T01:00:00Z"),
            [new("SN-TEST-PITX-L3-001", "MIN-TEST-PITX-L3-001", "PTU-TEST-PITX-L3-0001", total,
                sources.Select(source => new MonthlyESalesDailyRow(source.BusinessDayDate, source.BeginningSalesInvoiceReference,
                    source.EndingSalesInvoiceReference, source.TransactionCount, source.GrossSalesAmountMinorUnits,
                    source.VatableSalesAmountMinorUnits, source.VatAmountMinorUnits, source.VatExemptSalesAmountMinorUnits,
                    source.ZeroRatedSalesAmountMinorUnits)).ToArray())]);
        var renderer = new MonthlyESalesOutputRenderer();
        var first = renderer.Render(report, MonthlyESalesExportFormat.Pdf, pdfReport);
        var second = renderer.Render(report, MonthlyESalesExportFormat.Pdf, pdfReport);
        Assert.Equal(first.Bytes, second.Bytes);
        Assert.Equal("application/pdf", first.ContentType);
        Assert.Equal("monthly-esales-technical-uat-202609.pdf", first.FileName);
        Assert.StartsWith("%PDF-1.4", Encoding.Latin1.GetString(first.Bytes), StringComparison.Ordinal);
        var text = PdfText(first.Bytes);
        foreach (var expected in new[] { "MONTHLY eSALES", "September 2026",
                     "Registered Business Name", "SN-TEST-PITX-L3-001", "MIN-TEST-PITX-L3-001", "PTU-TEST-PITX-L3-0001",
                     "Beginning SI", "Ending SI", "Transactions", "Gross Sales", "VATable Sales", "VAT Exempt Sales", "Zero-Rated Sales",
                     "09-01", "09-03", "PHP 1,950.00", "Page 1 of 1" })
            Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotContain("TECHNICAL UAT - NOT FOR BIR SUBMISSION", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Submission Due Date", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Submission Readiness", text, StringComparison.Ordinal);
        Assert.DoesNotContain("09-02", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfKeepsMultipleMachinesSeparateAndPaginatesInDeterministicMinOrder()
    {
        var firstSource = Source(new(2026,9,1),1,min:"MIN-AAA",ending:"SI-1");
        var secondSource = Source(new(2026,9,2),2,min:"MIN-ZZZ",ending:"SI-2");
        var report = MonthlyESalesAggregator.Build(Query(), new(2026,10,16), [secondSource, firstSource]).Report!;
        var sections = report.Rows.Select(row =>
        {
            var source = row.MachineIdentificationNumber == firstSource.MachineIdentificationNumber ? firstSource : secondSource;
            return new MonthlyESalesMachineSection(source.PosSerialNumber!, source.MachineIdentificationNumber!, source.PtuNumber!, row,
                [new(source.BusinessDayDate, source.BeginningSalesInvoiceReference, source.EndingSalesInvoiceReference,
                    source.TransactionCount, source.GrossSalesAmountMinorUnits, source.VatableSalesAmountMinorUnits,
                    source.VatAmountMinorUnits, source.VatExemptSalesAmountMinorUnits, source.ZeroRatedSalesAmountMinorUnits)]);
        }).Reverse().ToArray();
        var sourceReport = new MonthlyESalesPdfReport(report, "Professional Parking Management Corporation",
            "PITX, Paranaque City", "PITX Level 3", "ExitPass v1.3", DateTimeOffset.Parse("2026-10-01T01:00:00Z"), sections);

        var bytes = new MonthlyESalesOutputRenderer().Render(report, MonthlyESalesExportFormat.Pdf, sourceReport).Bytes;
        var raw = Encoding.Latin1.GetString(bytes);
        var text = PdfText(bytes);

        Assert.Equal(2, Regex.Matches(raw, @"/Type /Page\b").Count);
        Assert.Contains("Page 1 of 2", text, StringComparison.Ordinal);
        Assert.Contains("Page 2 of 2", text, StringComparison.Ordinal);
        Assert.True(text.IndexOf("MIN-AAA", StringComparison.Ordinal) < text.IndexOf("MIN-ZZZ", StringComparison.Ordinal));
        Assert.Contains("SI-1", text, StringComparison.Ordinal);
        Assert.Contains("SI-2", text, StringComparison.Ordinal);
    }

    private static MonthlyESalesQuery Query() => new(Guid.NewGuid(), Guid.NewGuid(), "PHP", 2026, 9);
    private static MonthlyESalesSourcePeriod Source(DateOnly date,long sequence,string min="MIN-TEST-PITX-L3-001",string tin="999-999-999-000",long gross=10_000,long vatable=8_000,long vat=960,long exempt=1_000,long zero=40,string? ending="SI-1",long transactions=1) =>
        new(Guid.NewGuid(),sequence,date,true,true,true,tin,"Professional Parking Management Corporation","PITX, Paranaque City","PITX Level 3","ExitPass v1.3","SN-TEST-PITX-L3-001",min,"PTU-TEST-PITX-L3-0001",transactions,"SI-0",ending,gross,vatable,vat,exempt,zero,DateTimeOffset.Parse("2026-10-01T01:00:00Z"));

    private static string PdfText(byte[] bytes) => string.Join('\n', Regex.Matches(Encoding.Latin1.GetString(bytes), @"\((?<text>(?:\\.|[^\\)])*)\) Tj")
        .Select(match => Regex.Unescape(match.Groups["text"].Value)));
}
