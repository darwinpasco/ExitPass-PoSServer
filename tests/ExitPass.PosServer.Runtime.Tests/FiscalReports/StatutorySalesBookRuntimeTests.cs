using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class StatutorySalesBookRuntimeTests
{
    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2, "SI-00000000000176", "SI-00000000000179")]
    [InlineData(StatutorySalesBookType.AnnexE3, "SI-00000000000177", "SI-00000000000180")]
    public void NovemberSerialTransitionProducesAttributedReconciledXlsxAndPdfSections(
        StatutorySalesBookType type,
        string historicalInvoice,
        string currentInvoice)
    {
        var report = NovemberTransitionReport(type, historicalInvoice, currentInvoice);
        var sections = report.EffectiveSections;

        Assert.Equal(2, sections.Count);
        Assert.Equal("SN-TEST-PITX-L3-001", sections[0].Header.SerialNumber);
        Assert.Equal("0026_B728_3B2F_8505", sections[1].Header.SerialNumber);
        Assert.Equal([historicalInvoice], sections[0].Rows.Select(row => row.SalesInvoiceNumber));
        Assert.Equal([currentInvoice], sections[1].Rows.Select(row => row.SalesInvoiceNumber));
        Assert.All(sections, section =>
        {
            Assert.Equal(1, section.Summary.QualifyingTransactionCount);
            Assert.Equal(5_000, section.Summary.SalesInclusiveVatMinorUnits);
            Assert.Equal(5_000, section.Summary.LguWaiverVatInclusiveMinorUnits);
            Assert.Equal(0, section.Summary.NetSalesMinorUnits);
        });
        Assert.Equal(10_000, sections.Sum(section => section.Summary.SalesInclusiveVatMinorUnits));
        Assert.Equal(report.Summary.LguWaiverVatInclusiveMinorUnits, sections.Sum(section => section.Summary.LguWaiverVatInclusiveMinorUnits));

        var renderer = new StatutorySalesBookOutputRenderer();
        var xlsx = renderer.Render(report, StatutorySalesBookFormat.Xlsx);
        using (var stream = new MemoryStream(xlsx.Bytes))
        using (var document = SpreadsheetDocument.Open(stream, false))
        {
            var workbook = Assert.IsType<Workbook>(Assert.IsType<WorkbookPart>(document.WorkbookPart).Workbook);
            Assert.Equal(
                type == StatutorySalesBookType.AnnexE2 ? ["E-2 1", "E-2 2"] : ["E-3 1", "E-3 2"],
                Assert.IsType<Sheets>(workbook.Sheets).Elements<Sheet>().Select(sheet => sheet.Name!.Value).ToArray());
            Assert.Equal(2, Assert.IsType<WorkbookPart>(document.WorkbookPart).WorksheetParts.Count());
        }
        using (var archive = new System.IO.Compression.ZipArchive(new MemoryStream(xlsx.Bytes)))
        {
            using var historicalReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
            using var currentReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet2.xml")!.Open());
            var historicalXml = historicalReader.ReadToEnd();
            var currentXml = currentReader.ReadToEnd();
            Assert.Contains("Serial No.: SN-TEST-PITX-L3-001", historicalXml);
            Assert.Contains(historicalInvoice, historicalXml);
            Assert.DoesNotContain(currentInvoice, historicalXml);
            Assert.Contains("Serial No.: 0026_B728_3B2F_8505", currentXml);
            Assert.Contains(currentInvoice, currentXml);
            Assert.DoesNotContain(historicalInvoice, currentXml);
            Assert.Contains("MONTHLY TOTAL", currentXml);
            Assert.Contains("<c r=\"F19\" s=\"6\"><v>100.00</v></c>", currentXml);
            Assert.Contains("<c r=\"O19\" s=\"6\"><v>100.00</v></c>", currentXml);
        }

        var pdf = renderer.Render(report, StatutorySalesBookFormat.Pdf);
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf.Bytes);
        Assert.Contains("Serial No.: SN-TEST-PITX-L3-001", pdfText);
        Assert.Contains("Serial No.: 0026_B728_3B2F_8505", pdfText);
        Assert.Contains(historicalInvoice, pdfText);
        Assert.Contains(currentInvoice, pdfText);
        Assert.Contains("MONTHLY TOTAL", pdfText);
        Assert.Contains("Page 1 of 2", pdfText);
        Assert.Contains("Page 2 of 2", pdfText);
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2)]
    [InlineData(StatutorySalesBookType.AnnexE3)]
    public void StatutorySalesBookFailsClosedForIncompatibleTaxpayerScopes(StatutorySalesBookType type)
    {
        var report = NovemberTransitionReport(type, "SI-HISTORICAL", "SI-CURRENT");
        var incompatible = report.Rows[1] with
        {
            FiscalHeader = report.Rows[1].FiscalHeader! with { TaxpayerName = "Different Taxpayer" }
        };

        Assert.Throws<InvalidOperationException>(() =>
            StatutorySalesBookSectionPlanner.Resolve(report with { Rows = [report.Rows[0], incompatible] }));
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2, "E-2", "Senior Citizen Sales Book/Report", "ANNEX \"E-2\"")]
    [InlineData(StatutorySalesBookType.AnnexE3, "E-3", "Persons with Disability Sales Book/Report", "ANNEX \"E-3\"")]
    public void XlsxPreservesPrescribedColumnsAndAppendsAgreedLguPrivilegeDetail(StatutorySalesBookType type, string sheetName, string title, string annex)
    {
        var output = new StatutorySalesBookOutputRenderer().Render(Report(type, [Row(type, 2000)]), StatutorySalesBookFormat.Xlsx);
        using var stream = new MemoryStream(output.Bytes);
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = Assert.IsType<WorkbookPart>(document.WorkbookPart);
        var workbook = Assert.IsType<Workbook>(workbookPart.Workbook);
        var sheets = Assert.IsType<Sheets>(workbook.Sheets);
        Assert.Equal(sheetName, sheets.Elements<Sheet>().Single().Name!.Value);
        var sheet = Assert.IsType<Worksheet>(workbookPart.WorksheetParts.Single().Worksheet);
        Assert.Equal(16, sheet.Elements<Columns>().Single().Elements<Column>().Count());
        var merges = sheet.Elements<MergeCells>().Single().Elements<MergeCell>().Select(x => x.Reference!.Value).ToArray();
        Assert.Contains("I13:J13", merges);
        Assert.Contains("I14:I16", merges);
        Assert.Contains("J14:J16", merges);
        Assert.Contains("K13:K16", merges);
        Assert.Contains("L13:P13", merges);
        Assert.Contains("L14:L16", merges);
        Assert.Contains("P14:P16", merges);
        var names = Assert.IsType<DefinedNames>(workbook.DefinedNames);
        Assert.Equal("$A$1:$P$25", names.Elements<DefinedName>().Single().Text!.Split('!')[1]);
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
        Assert.Contains("Software Name and Version No. plus Release No./Release Date: ExitPass POS Server V1.0.0 / v1.3 / 2026-10-17", worksheetXml);
        Assert.Contains("Serial No.: SN-TEST", worksheetXml);
        Assert.Contains("Machine Identification Number: MIN-TEST", worksheetXml);
        Assert.Contains("POS Server: PITX-L3-POS-01", worksheetXml);
        Assert.Contains("Date and Time Generated: 2026-10-17 16:00:00", worksheetXml);
        Assert.Contains("UserID: Juan Dela Cruz", worksheetXml);
        Assert.Contains(">TOTAL<", worksheetXml);
        Assert.Contains("ADDITIONAL LGU PARKING-PRIVILEGE DETAIL", worksheetXml);
        Assert.Contains("LGU Privilege Type", worksheetXml);
        Assert.DoesNotContain("LGU Policy Reference", worksheetXml);
        Assert.Contains("<c r=\"K13\" s=\"7\"", worksheetXml);
        Assert.Contains("<c r=\"L13\" s=\"16\"", worksheetXml);
        Assert.Contains("<c r=\"P13\" s=\"16\"", worksheetXml);
        Assert.DoesNotContain("<c r=\"Q13\"", worksheetXml);
        Assert.Contains("<c r=\"N17\" s=\"4\"><v>0.00</v></c>", worksheetXml);
        Assert.Contains("<c r=\"P18\" s=\"6\"><v>0.00</v></c>", worksheetXml);
        using var stylesReader = new StreamReader(archive.GetEntry("xl/styles.xml")!.Open());
        var stylesXml = stylesReader.ReadToEnd();
        Assert.Contains("<color rgb=\"FFFFFFFF\"/>", stylesXml);
        Assert.Contains("<fgColor rgb=\"FF375623\"/>", stylesXml);
        Assert.Contains("fontId=\"3\" fillId=\"11\"", stylesXml);
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

    [Fact]
    public void FullFeeLguDetailFailsClosedWhenFinalPayableExceedsOriginalFee()
    {
        var row = FullFeeRow(StatutorySalesBookType.AnnexE2) with
        {
            LguFinalPayableVatInclusiveMinorUnits = 5_001,
            LguWaiverVatInclusiveMinorUnits = 0
        };

        Assert.False(StatutorySalesBookReconciliation.IsValid(row));
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
        Assert.Contains("<c r=\"P17\" s=\"6\"><v>0.00</v></c>", xml);
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2)]
    [InlineData(StatutorySalesBookType.AnnexE3)]
    public void PdfIsLandscapeAndIncludesAllPrescribedHeadings(StatutorySalesBookType type)
    {
        var output = new StatutorySalesBookOutputRenderer().Render(Report(type, [Row(type, 2000)]), StatutorySalesBookFormat.Pdf);
        var text = System.Text.Encoding.Latin1.GetString(output.Bytes);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("/MediaBox [0 0 841.89 595.28]", text);
        Assert.Contains(type == StatutorySalesBookType.AnnexE2
            ? "Senior Citizen Sales Book/Report"
            : "Persons with Disability Sales Book/Report", text);
        Assert.Contains("Software Name and Version No. plus Release No./Release Date", text);
        Assert.Contains("Discount", text);
        Assert.Contains("5%", text);
        Assert.Contains("20%", text);
        Assert.Contains("ADDITIONAL LGU PARKING-PRIVILEGE DETAIL", text);
        Assert.Contains("LGU Privilege", text);
        Assert.DoesNotContain("LGU Policy", text);
        Assert.Contains("0.22 0.34 0.14 rg", text);
        Assert.Contains("1 1 1 rg BT /F2", text);
        Assert.Contains("0 0.69 0.94 rg", text);
        Assert.Contains("1 0.85 0.4 rg", text);
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2)]
    [InlineData(StatutorySalesBookType.AnnexE3)]
    public void PdfRendersCompleteCanonicalSalesInvoiceNumberInsideExistingCell(StatutorySalesBookType type)
    {
        const string salesInvoiceNumber = "SI-00000000000171";
        var row = FullFeeRow(type) with { SalesInvoiceNumber = salesInvoiceNumber };
        var output = new StatutorySalesBookOutputRenderer().Render(
            Report(type, [row]),
            StatutorySalesBookFormat.Pdf);
        var text = System.Text.Encoding.Latin1.GetString(output.Bytes);

        Assert.Contains($"({salesInvoiceNumber}) Tj", text);
        Assert.DoesNotContain("(SI-000000000001) Tj", text);

        var configuredWidths = new[] { 42d, 96d, 62d, 54d, 62d, 52d, 44d, 50d, 34d, 34d, 48d, 66d, 48d, 52d, 52d, 48d };
        var scale = (841.89d - 40d) / configuredWidths.Sum();
        var invoiceCellWidth = configuredWidths[4] * scale;
        var renderedInvoiceWidth = salesInvoiceNumber.Length * 4.2d * 0.52d;
        Assert.True(renderedInvoiceWidth <= invoiceCellWidth - 4d);

        var invoicePosition = text.IndexOf($"({salesInvoiceNumber}) Tj", StringComparison.Ordinal);
        var followingAmountPosition = text.IndexOf("(50.00) Tj", invoicePosition, StringComparison.Ordinal);
        Assert.True(followingAmountPosition > invoicePosition);
        Assert.Contains("(50.00) Tj", text);
        Assert.DoesNotContain("SUPPLEMENTARY LGU PARKING BENEFIT DETAILS", text);
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2)]
    [InlineData(StatutorySalesBookType.AnnexE3)]
    public void FullFeeExemptionPreservesPrescribedSheetWithoutSeparateLguSupplement(
        StatutorySalesBookType type)
    {
        var row = FullFeeRow(type);
        Assert.True(StatutorySalesBookReconciliation.IsValid(row));

        var output = new StatutorySalesBookOutputRenderer().Render(Report(type, [row]), StatutorySalesBookFormat.Xlsx);
        using var stream = new MemoryStream(output.Bytes);
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = Assert.IsType<WorkbookPart>(document.WorkbookPart);
        var sheets = Assert.IsType<Sheets>(Assert.IsType<Workbook>(workbookPart.Workbook).Sheets).Elements<Sheet>().ToArray();
        Assert.Single(sheets);
        Assert.Equal(type == StatutorySalesBookType.AnnexE2 ? "E-2" : "E-3", sheets[0].Name!.Value);

        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(output.Bytes));
        using var primaryReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var primary = primaryReader.ReadToEnd();
        Assert.Contains("<c r=\"I17\" s=\"4\"><v>0.00</v></c>", primary);
        Assert.Contains("<c r=\"J17\" s=\"4\"><v>0.00</v></c>", primary);
        Assert.Contains("<c r=\"K17\" s=\"4\"><v>0.00</v></c>", primary);
        Assert.Contains("<c r=\"L17\" s=\"5\" t=\"inlineStr\"><is><t xml:space=\"preserve\">FULL_FEE_EXEMPTION</t></is></c>", primary);
        Assert.Contains("<c r=\"M17\" s=\"5\" t=\"inlineStr\"><is><t xml:space=\"preserve\"></t></is></c>", primary);
        Assert.Contains("<c r=\"N17\" s=\"4\"><v>50.00</v></c>", primary);
        Assert.Contains("<c r=\"O17\" s=\"4\"><v>50.00</v></c>", primary);
        Assert.Contains("<c r=\"P17\" s=\"4\"><v>0.00</v></c>", primary);
        Assert.DoesNotContain("<c r=\"Q17\"", primary);
        Assert.DoesNotContain("LGU Policy Reference", primary);
        Assert.Contains("<c r=\"N18\" s=\"6\"><v>50.00</v></c>", primary);
        Assert.Contains("<c r=\"O18\" s=\"6\"><v>50.00</v></c>", primary);
        Assert.Contains("<c r=\"P18\" s=\"6\"><v>0.00</v></c>", primary);

        Assert.Null(archive.GetEntry("xl/worksheets/sheet2.xml"));
    }

    [Theory]
    [InlineData(StatutorySalesBookType.AnnexE2)]
    [InlineData(StatutorySalesBookType.AnnexE3)]
    public void FullFeeExemptionPdfAddsAgreedLguDetailWithoutSupplement(StatutorySalesBookType type)
    {
        var output = new StatutorySalesBookOutputRenderer().Render(
            Report(type, [FullFeeRow(type)]),
            StatutorySalesBookFormat.Pdf);
        var text = System.Text.Encoding.Latin1.GetString(output.Bytes);

        Assert.Contains(type == StatutorySalesBookType.AnnexE2
            ? "Senior Citizen Sales Book/Report"
            : "Persons with Disability Sales Book/Report", text);
        Assert.Contains("ADDITIONAL LGU PARKING-PRIVILEGE DETAIL", text);
        Assert.Contains("FULL_FEE_EXEMPTION", text);
        Assert.Contains("50.00", text);
        Assert.DoesNotContain("SUPPLEMENTARY LGU PARKING BENEFIT DETAILS", text);
        Assert.DoesNotContain("prescribed Annex E-2/E-3 columns remain unchanged", text);
        Assert.DoesNotContain("LGU Policy Reference", text);
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
    [InlineData(false, 2, 2, 2, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(true, 0, 0, 0, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(true, 1, 0, 0, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(true, 1, 1, 0, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(true, 2, 2, 1, StatutorySalesBookStatuses.Preliminary)]
    [InlineData(true, 2, 2, 2, StatutorySalesBookStatuses.Final)]
    public void FinalStatusRequiresCompletedMonthlyCoverageAndCommittedZEvidence(
        bool reportingMonthComplete,
        int scoped,
        int closed,
        int committedZ,
        string expected)
    {
        Assert.Equal(expected, StatutorySalesBookStatuses.Resolve(reportingMonthComplete, scoped, closed, committedZ));
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
            Rows = [report.Rows[0] with { SalesInvoiceNumber = report.Rows[0].SalesInvoiceNumber + "-CHANGED" }]
        };
        var changedLguEvidence = report with
        {
            Rows = [report.Rows[0] with { LguPolicyReference = "DIFFERENT-AUTHORITATIVE-POLICY" }]
        };

        Assert.Equal(StatutorySalesBookContract.SourceFingerprint(report), StatutorySalesBookContract.SourceFingerprint(regenerated));
        Assert.NotEqual(StatutorySalesBookContract.SourceFingerprint(report), StatutorySalesBookContract.SourceFingerprint(changed));
        Assert.NotEqual(StatutorySalesBookContract.SourceFingerprint(report), StatutorySalesBookContract.SourceFingerprint(changedLguEvidence));
        Assert.NotEqual(
            StatutorySalesBookContract.SourceFingerprint(report),
            StatutorySalesBookContract.SourceFingerprint(report with
            {
                Summary = report.Summary with { ReportStatus = StatutorySalesBookStatuses.Preliminary }
            }));
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
            rows.Sum(x => x.SalesInclusiveVatMinorUnits), rows.Sum(x => x.VatAmountMinorUnits), rows.Sum(x => x.VatExemptSalesMinorUnits), rows.Sum(x => x.Discount5MinorUnits), rows.Sum(x => x.Discount20MinorUnits), rows.Sum(x => x.NetSalesMinorUnits), DateTimeOffset.Parse("2026-10-17T08:00:00Z"), [], StatutorySalesBookStatuses.Final, rows.Count == 0, 31, 31, 31,
            rows.Sum(x => x.OriginalParkingFeeVatInclusiveMinorUnits), rows.Sum(x => x.LguWaiverVatInclusiveMinorUnits), rows.Sum(x => x.LguFinalPayableVatInclusiveMinorUnits));
        return new(new("Professional Parking Management Corporation", "PITX, Paranaque City", "999-999-999-000", "ExitPass POS Server", "V1.0.0", "v1.3", "2026-10-17", "SN-TEST", "MIN-TEST", "PITX-L3-POS-01", summary.ReportAsOf, "operator-1", "Juan Dela Cruz"), summary, rows);
    }

    private static StatutorySalesBookReport NovemberTransitionReport(
        StatutorySalesBookType type,
        string historicalInvoice,
        string currentInvoice)
    {
        var historicalHeader = new StatutorySalesBookFiscalHeaderFact(
            "Professional Parking Management Corporation", "PITX, Paranaque City", "999-999-999-000",
            "SN-TEST-PITX-L3-001", "MIN-TEST-PITX-L3-001", "PITX-L3-POS-01",
            "ExitPass POS Server", "V1.0.0");
        var currentHeader = historicalHeader with { SerialNumber = "0026_B728_3B2F_8505" };
        var historical = FullFeeRow(type) with
        {
            TransactionDate = new DateOnly(2026, 11, 3),
            TransactionTimestamp = DateTimeOffset.Parse("2026-11-03T08:00:00Z"),
            FiscalSequence = type == StatutorySalesBookType.AnnexE2 ? 176 : 177,
            SalesInvoiceNumber = historicalInvoice,
            FiscalHeader = historicalHeader
        };
        var current = FullFeeRow(type) with
        {
            TransactionDate = new DateOnly(2026, 11, 5),
            TransactionTimestamp = DateTimeOffset.Parse("2026-11-05T08:00:00Z"),
            FiscalSequence = type == StatutorySalesBookType.AnnexE2 ? 179 : 180,
            SalesInvoiceNumber = currentInvoice,
            FiscalHeader = currentHeader
        };
        var report = Report(type, [historical, current]);
        return report with
        {
            Header = report.Header with
            {
                SerialNumber = historicalHeader.SerialNumber,
                MachineIdentificationNumber = historicalHeader.MachineIdentificationNumber,
                GeneratedAt = DateTimeOffset.Parse("2026-11-30T08:00:00Z")
            },
            Summary = report.Summary with
            {
                CalendarMonth = 11,
                ReportAsOf = DateTimeOffset.Parse("2026-11-30T08:00:00Z")
            }
        };
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
        4_464, 4_464, 0, 0, 0,
        "FULL_FEE_EXEMPTION", null, 5_000, 5_000, 0,
        type == StatutorySalesBookType.AnnexE2
            ? "PH_PARANAQUE_SENIOR_FREE_PARKING"
            : "PH_PARANAQUE_PWD_FREE_PARKING");
}
