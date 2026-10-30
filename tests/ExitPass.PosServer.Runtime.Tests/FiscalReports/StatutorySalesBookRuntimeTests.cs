using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class StatutorySalesBookRuntimeTests
{
    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2, "E-2", "Senior Citizen Sales Book/Report", "ANNEX \"E-2\"")]
    [InlineData(StatutorySalesBookType.AnnexE3, "E-3", "Persons with Disability Sales Book/Report", "ANNEX \"E-3\"")]
    public void XlsxPreservesOfficialElevenColumnGeometryAndTitles(StatutorySalesBookType type, string sheetName, string title, string annex)
    {
        var output = new StatutorySalesBookOutputRenderer().Render(Report(type, [Row(type, 2000)]), StatutorySalesBookFormat.Xlsx);
        using var stream = new MemoryStream(output.Bytes);
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = Assert.IsType<WorkbookPart>(document.WorkbookPart);
        var workbook = Assert.IsType<Workbook>(workbookPart.Workbook);
        var sheets = Assert.IsType<Sheets>(workbook.Sheets);
        Assert.Equal(sheetName, sheets.Elements<Sheet>().Single().Name!.Value);
        var sheet = Assert.IsType<Worksheet>(workbookPart.WorksheetParts.Single().Worksheet);
        Assert.Equal(11, sheet.Elements<Columns>().Single().Elements<Column>().Count());
        var merges = sheet.Elements<MergeCells>().Single().Elements<MergeCell>().Select(x => x.Reference!.Value).ToArray();
        Assert.Contains("I13:J13", merges);
        Assert.Contains("I14:I16", merges);
        Assert.Contains("J14:J16", merges);
        var names = Assert.IsType<DefinedNames>(workbook.DefinedNames);
        Assert.Equal("$A$1:$K$25", names.Elements<DefinedName>().Single().Text!.Split('!')[1]);
        Assert.True(output.Bytes[0] == (byte)'P' && output.Bytes[1] == (byte)'K');
        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(output.Bytes));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var worksheetXml = reader.ReadToEnd();
        Assert.Contains(title, worksheetXml);
        Assert.Contains(annex.Replace("\"", "&quot;"), worksheetXml);
        Assert.Contains("width=\"22.140625\"", worksheetXml);
        Assert.Contains(type == StatutorySalesBookType.AnnexE2 ? "ht=\"21\"" : "ht=\"24.75\"", worksheetXml);
        Assert.Contains("orientation=\"landscape\"", worksheetXml);
        Assert.Contains("fitToWidth=\"1\"", worksheetXml);
        Assert.Contains("Software Name and Version No. plus Release No./Release Date: ExitPass POS Server 1.3 / v1.3 / 2026-10-17", worksheetXml);
        Assert.Contains("Serial No.: SN-TEST", worksheetXml);
        Assert.Contains("Machine Identification Number: MIN-TEST", worksheetXml);
        Assert.Contains("POS Server: PITX-L3-POS-01", worksheetXml);
        Assert.Contains("Date and Time Generated: 2026-10-17 16:00:00", worksheetXml);
        Assert.Contains("UserID: operator-1", worksheetXml);
        Assert.Contains(">TOTAL<", worksheetXml);
    }

    [Fact]
    public void PercentageAmountsPopulateOnlyTheProvenFiveOrTwentyPercentColumn()
    {
        var report = Report(StatutorySalesBookType.AnnexE2, [Row(StatutorySalesBookType.AnnexE2, 500), Row(StatutorySalesBookType.AnnexE2, 2000)]);
        Assert.Equal(1_000, report.Rows[0].Discount5MinorUnits);
        Assert.Equal(0, report.Rows[0].Discount20MinorUnits);
        Assert.Equal(0, report.Rows[1].Discount5MinorUnits);
        Assert.Equal(1_000, report.Rows[1].Discount20MinorUnits);
        Assert.Equal(1_000, report.Summary.Discount5MinorUnits);
        Assert.Equal(1_000, report.Summary.Discount20MinorUnits);
    }

    [Theory]
    [InlineData(null, 0, false)]
    [InlineData(null, 5_000, true)]
    [InlineData(500, 0, true)]
    [InlineData(500, 1_000, false)]
    [InlineData(2000, 1_000, false)]
    public void NonPercentageOrUnprovenBenefitsAreExcludedWithoutFailingTheBook(
        int? rateBasisPoints,
        long percentageAmountMinorUnits,
        bool separatelyProven)
    {
        Assert.False(StatutorySalesBookQualification.IsQualifyingPercentageRow(
            "SENIOR_CITIZEN", StatutorySalesBookType.AnnexE2, "LOCAL_ORDINANCE",
            rateBasisPoints, percentageAmountMinorUnits, separatelyProven));
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2, "SENIOR_CITIZEN", 500)]
    [InlineData(StatutorySalesBookType.AnnexE2, "SENIOR_CITIZEN", 2000)]
    [InlineData(StatutorySalesBookType.AnnexE3, "PWD", 500)]
    [InlineData(StatutorySalesBookType.AnnexE3, "PWD", 2000)]
    public void OnlyProvenLocalFiveOrTwentyPercentBenefitsQualify(
        StatutorySalesBookType type,
        string entitlement,
        int rateBasisPoints)
    {
        Assert.True(StatutorySalesBookQualification.IsQualifyingPercentageRow(
            entitlement, type, "LOCAL_ORDINANCE",
            rateBasisPoints, 1_000, true));
        Assert.False(StatutorySalesBookQualification.IsQualifyingPercentageRow(
            entitlement == "PWD" ? "SENIOR_CITIZEN" : "PWD", type, "LOCAL_ORDINANCE",
            rateBasisPoints, 1_000, true));
        Assert.False(StatutorySalesBookQualification.IsQualifyingPercentageRow(
            entitlement, type, "NATIONAL_LAW",
            rateBasisPoints, 1_000, true));
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2, "SENIOR_CITIZEN")]
    [InlineData(StatutorySalesBookType.AnnexE3, "PWD")]
    public void ProvenLocalFullFeeExemptionsQualifyWithoutBecomingPercentageRows(
        StatutorySalesBookType type,
        string entitlement)
    {
        Assert.True(StatutorySalesBookQualification.IsQualifyingFullFeeExemptionRow(
            entitlement, type, "LOCAL_ORDINANCE", "FREE_PARKING", null, 4_464, 4_464, 0));
        Assert.False(StatutorySalesBookQualification.IsQualifyingPercentageRow(
            entitlement, type, "LOCAL_ORDINANCE", null, 0, false));
        Assert.False(StatutorySalesBookQualification.IsQualifyingFullFeeExemptionRow(
            entitlement, type, "NATIONAL_LAW", "FREE_PARKING", null, 4_464, 4_464, 0));
        Assert.False(StatutorySalesBookQualification.IsQualifyingFullFeeExemptionRow(
            entitlement, type, "LOCAL_ORDINANCE", "FREE_PARKING", null, 4_464, 4_464, 1));
    }

    [Theory]
    [InlineData(null, 5_000, true)]
    [InlineData(500, 0, true)]
    [InlineData(500, 5_000, false)]
    [InlineData(1000, 1_000, true)]
    [InlineData(10_000, 10_000, true)]
    public void FreeCappedOrUnprovenPrivilegesDoNotBecomePercentageRows(
        int? rateBasisPoints,
        long percentageAmountMinorUnits,
        bool separatelyProven)
    {
        Assert.False(StatutorySalesBookQualification.IsQualifyingPercentageRow(
            "SENIOR_CITIZEN", StatutorySalesBookType.AnnexE2, "LOCAL_ORDINANCE",
            rateBasisPoints, percentageAmountMinorUnits, separatelyProven));
    }

    [Fact]
    public void UnseparatedMixedPercentageComponentReturnsSafeClassificationAndRemainsExcluded()
    {
        Assert.Equal(
            StatutorySalesBookQualification.PercentageComponentNotSeparatelyProven,
            StatutorySalesBookQualification.SafeExclusionClassification(
                "SENIOR_CITIZEN", StatutorySalesBookType.AnnexE2, "LOCAL_ORDINANCE", 2000, false));
        Assert.False(StatutorySalesBookQualification.IsQualifyingPercentageRow(
            "SENIOR_CITIZEN", StatutorySalesBookType.AnnexE2, "LOCAL_ORDINANCE", 2000, 0, false));
        Assert.Null(StatutorySalesBookQualification.SafeExclusionClassification(
            "SENIOR_CITIZEN", StatutorySalesBookType.AnnexE2, "LOCAL_ORDINANCE", null, false));
    }

    [Fact]
    public void EmptyBookHasOfficialHeaderAndNoFabricatedBeneficiaryRow()
    {
        var output = new StatutorySalesBookOutputRenderer().Render(Report(StatutorySalesBookType.AnnexE3, []), StatutorySalesBookFormat.Xlsx);
        using var stream = new MemoryStream(output.Bytes);
        using var document = SpreadsheetDocument.Open(stream, false);
        var sheet = Assert.IsType<Worksheet>(Assert.IsType<WorkbookPart>(document.WorkbookPart).WorksheetParts.Single().Worksheet);
        Assert.Contains("A17:E17", sheet.Elements<MergeCells>().Single().Elements<MergeCell>().Select(cell => cell.Reference!.Value));
        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(output.Bytes));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = reader.ReadToEnd();
        Assert.Contains("Persons with Disability Sales Book/Report", xml);
        Assert.DoesNotContain("Synthetic Beneficiary", xml);
        Assert.Contains("TOTAL - NO QUALIFYING TRANSACTIONS", xml);
        Assert.Contains("r=\"17\"", xml);
    }

    [Fact]
    public void PdfIsLandscapeAndIncludesAllPrescribedHeadings()
    {
        var output = new StatutorySalesBookOutputRenderer().Render(Report(StatutorySalesBookType.AnnexE2, [Row(StatutorySalesBookType.AnnexE2, 2000)]), StatutorySalesBookFormat.Pdf);
        var text = System.Text.Encoding.Latin1.GetString(output.Bytes);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("/MediaBox [0 0 841.89 595.28]", text);
        Assert.Contains("Senior Citizen Sales Book/Report", text);
        Assert.Contains("Software Name and Version No. plus Release No./Release Date", text);
        Assert.Contains("Discount", text);
        Assert.Contains("5%", text);
        Assert.Contains("20%", text);
        Assert.Contains("0 0.69 0.94 rg", text);
        Assert.Contains("1 0.85 0.4 rg", text);
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2)]
    [InlineData(StatutorySalesBookType.AnnexE3)]
    public void FullFeeExemptionPreservesPrescribedSheetAndAddsSeparateLguSupplement(
        StatutorySalesBookType type)
    {
        var row = FullFeeRow(type);
        Assert.True(StatutorySalesBookReconciliation.IsValid(row));

        var output = new StatutorySalesBookOutputRenderer().Render(Report(type, [row]), StatutorySalesBookFormat.Xlsx);
        using var stream = new MemoryStream(output.Bytes);
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = Assert.IsType<WorkbookPart>(document.WorkbookPart);
        var sheets = Assert.IsType<Sheets>(Assert.IsType<Workbook>(workbookPart.Workbook).Sheets).Elements<Sheet>().ToArray();
        Assert.Equal(2, sheets.Length);
        Assert.Equal(type == StatutorySalesBookType.AnnexE2 ? "E-2" : "E-3", sheets[0].Name!.Value);
        Assert.Equal("LGU Supplement", sheets[1].Name!.Value);

        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(output.Bytes));
        using var primaryReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var primary = primaryReader.ReadToEnd();
        Assert.Contains("<c r=\"I17\" s=\"4\"><v>0.00</v></c>", primary);
        Assert.Contains("<c r=\"J17\" s=\"4\"><v>0.00</v></c>", primary);

        using var supplementReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet2.xml")!.Open());
        var supplement = supplementReader.ReadToEnd();
        Assert.Contains("PH_PARANAQUE_", supplement);
        Assert.Contains("Full Fee Exemption", supplement);
        Assert.Contains("44.64", supplement);
        Assert.Contains("5.36", supplement);
    }

    [Fact]
    public void FullFeeExemptionPdfAddsSupplementWithoutChangingPrescribedReport()
    {
        var output = new StatutorySalesBookOutputRenderer().Render(
            Report(StatutorySalesBookType.AnnexE2, [FullFeeRow(StatutorySalesBookType.AnnexE2)]),
            StatutorySalesBookFormat.Pdf);
        var text = System.Text.Encoding.Latin1.GetString(output.Bytes);

        Assert.Contains("Senior Citizen Sales Book/Report", text);
        Assert.Contains("SUPPLEMENTARY LGU PARKING BENEFIT DETAILS", text);
        Assert.Contains("prescribed Annex E-2/E-3 columns remain unchanged", text);
        Assert.Contains("Final payable: PHP 0.00", text);
    }

    [Theory]
    [InlineData(StatutorySalesBookFormat.Xlsx)]
    [InlineData(StatutorySalesBookFormat.Pdf)]
    public void ExportsAreByteDeterministic(StatutorySalesBookFormat format)
    {
        var report = Report(StatutorySalesBookType.AnnexE2, [Row(StatutorySalesBookType.AnnexE2, 2000)]);
        var renderer = new StatutorySalesBookOutputRenderer();

        Assert.Equal(renderer.Render(report, format).Bytes, renderer.Render(report, format).Bytes);
    }

    [Theory]
    [InlineData(0, 0, 0, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(1, 0, 0, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(1, 1, 0, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(2, 2, 1, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(2, 2, 2, StatutorySalesBookStatuses.Final)]
    public void FinalStatusRequiresEveryScopedPeriodToHaveCommittedZEvidence(
        int scoped,
        int closed,
        int committedZ,
        string expected)
    {
        Assert.Equal(expected, StatutorySalesBookStatuses.Resolve(scoped, closed, committedZ));
    }

    [Fact]
    public void ArtifactFingerprintExcludesGenerationAndRequestingUserButIncludesFiscalEvidence()
    {
        var report = Report(StatutorySalesBookType.AnnexE2, [Row(StatutorySalesBookType.AnnexE2, 2000)]);
        var regenerated = report with
        {
            Header = report.Header with { GeneratedAt = report.Header.GeneratedAt.AddMinutes(5), UserId = "operator-2" },
            Summary = report.Summary with { ReportAsOf = report.Summary.ReportAsOf.AddMinutes(5) }
        };
        var changed = report with
        {
            Rows = [report.Rows[0] with { NetSalesMinorUnits = report.Rows[0].NetSalesMinorUnits + 1 }]
        };

        Assert.Equal(StatutorySalesBookContract.SourceFingerprint(report), StatutorySalesBookContract.SourceFingerprint(regenerated));
        Assert.NotEqual(StatutorySalesBookContract.SourceFingerprint(report), StatutorySalesBookContract.SourceFingerprint(changed));
    }

    [Fact]
    public void DuplicateSalesInvoiceNumberAcrossDifferentDocumentsFailsReconciliation()
    {
        var first = Row(StatutorySalesBookType.AnnexE2, 2000);
        var duplicate = first with { FiscalDocumentId = Guid.NewGuid() };

        Assert.False(StatutorySalesBookReconciliation.HasUniqueFiscalEvidence([first, duplicate]));
        Assert.True(StatutorySalesBookReconciliation.HasUniqueFiscalEvidence(
            [first, duplicate with { SalesInvoiceNumber = duplicate.SalesInvoiceNumber + "-2" }]));
    }

    private static StatutorySalesBookReport Report(StatutorySalesBookType type, IReadOnlyList<StatutorySalesBookRow> rows)
    {
        var summary = new StatutorySalesBookSummary(type, Guid.NewGuid(), Guid.NewGuid(), "PHP", 2026, 10, "PITX-L3-POS-01", rows.Count,
            rows.Sum(x => x.SalesInclusiveVatMinorUnits), rows.Sum(x => x.VatAmountMinorUnits), rows.Sum(x => x.VatExemptSalesMinorUnits), rows.Sum(x => x.Discount5MinorUnits), rows.Sum(x => x.Discount20MinorUnits), rows.Sum(x => x.NetSalesMinorUnits), DateTimeOffset.Parse("2026-10-17T08:00:00Z"), [], StatutorySalesBookStatuses.Final, rows.Count == 0, 31, 31, 31);
        return new(new("Professional Parking Management Corporation", "PITX, Paranaque City", "999-999-999-000", "ExitPass POS Server", "1.3", "v1.3", "2026-10-17", "SN-TEST", "MIN-TEST", "PITX-L3-POS-01", summary.ReportAsOf, "operator-1"), summary, rows);
    }

    private static StatutorySalesBookRow Row(StatutorySalesBookType type, int rate) => new(Guid.NewGuid(), new(2026, 10, 17), DateTimeOffset.Parse("2026-10-17T08:00:00Z"), rate, "Synthetic Beneficiary", type == StatutorySalesBookType.AnnexE2 ? "OSCA_ID" : "PWD_ID", "SYN-001", null, $"SI-{rate}", 10_000, 1_071, 0, rate == 500 ? 1_000 : 0, rate == 2000 ? 1_000 : 0, 9_000, "SYNTHETIC-LGU-PERCENTAGE", rate);

    private static StatutorySalesBookRow FullFeeRow(StatutorySalesBookType type) => new(
        Guid.NewGuid(), new(2026, 10, 17), DateTimeOffset.Parse("2026-10-17T08:00:00Z"), 0,
        "Synthetic Beneficiary", type == StatutorySalesBookType.AnnexE2 ? "SENIOR_CITIZEN_ID" : "PWD_ID",
        "SYN-001", null, type == StatutorySalesBookType.AnnexE2 ? "SI-SC-FULL" : "SI-PWD-FULL",
        5_000, 536, 0, 0, 0, 0, "City Ordinance No. 48", 0,
        "FREE_PARKING",
        type == StatutorySalesBookType.AnnexE2
            ? "PH_PARANAQUE_SENIOR_FREE_PARKING"
            : "PH_PARANAQUE_PWD_FREE_PARKING",
        4_464, 4_464, 0, 0, 0);
}
