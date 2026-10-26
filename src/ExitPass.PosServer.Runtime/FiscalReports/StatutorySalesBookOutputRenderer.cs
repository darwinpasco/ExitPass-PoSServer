using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class StatutorySalesBookOutputRenderer
{
    private static readonly DateTime FixedZipWallTime = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTimeOffset FixedZipTime = new(FixedZipWallTime, TimeZoneInfo.Local.GetUtcOffset(FixedZipWallTime));

    public StatutorySalesBookArtifact Render(StatutorySalesBookReport report, StatutorySalesBookFormat format)
    {
        var prefix = report.Summary.Type == StatutorySalesBookType.AnnexE2 ? "ANNEX-E2" : "ANNEX-E3";
        var scope = $"{Safe(report.Header.MachineIdentificationNumber)}_{Safe(report.Header.PosServerCode)}_{report.Summary.CalendarYear:0000}{report.Summary.CalendarMonth:00}";
        return format switch
        {
            StatutorySalesBookFormat.Xlsx => new(RenderXlsx(report), StatutorySalesBookContract.XlsxContentType, $"{prefix}_{scope}.xlsx"),
            StatutorySalesBookFormat.Pdf => new(RenderPdf(report), StatutorySalesBookContract.PdfContentType, $"{prefix}_{scope}.pdf"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static byte[] RenderXlsx(StatutorySalesBookReport report)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            Add(archive, "[Content_Types].xml", ContentTypes());
            Add(archive, "_rels/.rels", RootRelationships());
            Add(archive, "docProps/app.xml", AppProperties(report));
            Add(archive, "docProps/core.xml", CoreProperties());
            Add(archive, "xl/workbook.xml", Workbook(report));
            Add(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            Add(archive, "xl/styles.xml", Styles());
            Add(archive, "xl/worksheets/sheet1.xml", Worksheet(report));
        }
        return stream.ToArray();
    }

    private static byte[] RenderPdf(StatutorySalesBookReport report)
    {
        const double width = A4FiscalReportPdf.PageHeight;
        const double height = A4FiscalReportPdf.PageWidth;
        const double left = 20;
        const double right = width - 20;
        const double headerHeight = 38;
        var columns = new[] { 48d, 128d, 80d, 72d, 72d, 80d, 62d, 72d, 48d, 48d, 72d };
        var scale = (right - left) / columns.Sum();
        columns = columns.Select(value => value * scale).ToArray();
        var pdf = new A4FiscalReportPdf();
        var rowsPerPage = 13;
        var pageCount = Math.Max(1, (int)Math.Ceiling(Math.Max(1, report.Rows.Count) / (double)rowsPerPage));
        for (var pageNo = 0; pageNo < pageCount; pageNo++)
        {
            var page = pdf.AddPage(width, height);
            DrawPdfHeader(pdf, page, report, width, pageNo > 0);
            var top = 410d;
            DrawPdfColumns(pdf, page, left, top, columns, headerHeight, report.Summary.Type);
            var y = top - headerHeight;
            foreach (var row in report.Rows.Skip(pageNo * rowsPerPage).Take(rowsPerPage))
            {
                DrawPdfRow(pdf, page, left, y, columns, 22, row);
                y -= 22;
            }
            if (pageNo == pageCount - 1)
                DrawPdfTotals(pdf, page, left, y, columns, 22, report.Summary);
            pdf.Text(page, left, 14, $"Status: {report.Summary.ReportStatus} | Period: {report.Summary.CalendarYear:0000}-{report.Summary.CalendarMonth:00} | {(report.Summary.IsEmpty ? "No qualifying transactions" : $"{report.Summary.QualifyingTransactionCount} qualifying transaction(s)")}", 6.5);
            pdf.RightText(page, right, 14, $"Page {pageNo + 1} of {pageCount}", 6.5);
        }
        return pdf.Build();
    }

    private static void DrawPdfHeader(A4FiscalReportPdf pdf, int page, StatutorySalesBookReport report, double width, bool continued)
    {
        var h = report.Header;
        pdf.RightText(page, width - 20, 565, StatutorySalesBookContract.AnnexLabel(report.Summary.Type), 8, true);
        pdf.CenterText(page, width / 2, 552, h.TaxpayerName, 9, true);
        pdf.CenterText(page, width / 2, 540, h.TaxpayerAddress, 7);
        pdf.CenterText(page, width / 2, 528, h.Tin, 7);
        pdf.Text(page, 20, 506, $"Software Name and Version No. plus Release No./Release Date: {h.SoftwareName} {h.SoftwareVersion} / {h.ReleaseNumber} / {h.ReleaseDate}", 6.5);
        pdf.Text(page, 20, 495, $"Serial No.: {h.SerialNumber}", 6.5);
        pdf.Text(page, 20, 484, $"Machine Identification Number: {h.MachineIdentificationNumber}", 6.5);
        pdf.Text(page, 20, 473, $"POS Server: {h.PosServerCode}", 6.5);
        pdf.Text(page, 20, 462, $"Date and Time Generated: {Pht(h.GeneratedAt):yyyy-MM-dd HH:mm:ss}", 6.5);
        pdf.Text(page, 20, 451, $"UserID: {h.UserId}", 6.5);
        pdf.CenterText(page, width / 2, 430, StatutorySalesBookContract.Title(report.Summary.Type) + (continued ? " (continued)" : string.Empty), 10, true);
    }

    private static void DrawPdfColumns(A4FiscalReportPdf pdf, int page, double x, double y, double[] widths, double height, StatutorySalesBookType type)
    {
        var labels = type == StatutorySalesBookType.AnnexE2
            ? new[] { "Date", "Name of Senior Citizen (SC)", "OSCA ID No./SC ID No.", "SC TIN", "SI/OR Number", "Sales (inclusive of VAT)", "VAT Amount", "VAT Exempt Sales", "Net Sales" }
            : new[] { "Date", "Name of Person with Disability (PWD)", "PWD ID No.", "PWD TIN", "SI / OR Number", "Sales (inclusive of VAT)", "VAT Amount", "VAT Exempt Sales", "Net Sales" };
        var colors = new[]
        {
            (0.65, 0.65, 0.65), (0.00, 0.69, 0.94), (1.00, 1.00, 0.00), (0.60, 0.60, 1.00),
            (1.00, 0.75, 0.00), (0.57, 0.82, 0.31), (0.93, 0.49, 0.19), (0.91, 0.90, 0.90)
        };
        for (var i = 0; i < 8; i++)
        {
            DrawPdfHeaderCell(pdf, page, x, y, widths[i], height, labels[i], colors[i]);
            x += widths[i];
        }

        var half = height / 2;
        var discountWidth = widths[8] + widths[9];
        DrawPdfHeaderCell(pdf, page, x, y, discountWidth, half, "Discount", (1.00, 0.85, 0.40));
        DrawPdfHeaderCell(pdf, page, x, y - half, widths[8], half, "5%", (1.00, 0.85, 0.40));
        DrawPdfHeaderCell(pdf, page, x + widths[8], y - half, widths[9], half, "20%", (1.00, 0.85, 0.40));
        x += discountWidth;
        DrawPdfHeaderCell(pdf, page, x, y, widths[10], height, labels[8], (0.65, 0.65, 0.65));
    }

    private static void DrawPdfHeaderCell(A4FiscalReportPdf pdf, int page, double x, double y, double width, double height, string label, (double Red, double Green, double Blue) color)
    {
        pdf.FillRect(page, x, y - height, width, height, color.Red, color.Green, color.Blue);
        pdf.StrokeRect(page, x, y - height, width, height, 0.35);
        var lines = A4FiscalReportPdf.Wrap(label, Math.Max(5, (int)(width / 4.5))).Take(3).ToArray();
        for (var line = 0; line < lines.Length; line++)
            pdf.CenterText(page, x + width / 2, y - 11 - line * 8, lines[line], 5.8, true);
    }

    private static void DrawPdfRow(A4FiscalReportPdf pdf, int page, double x, double y, double[] widths, double height, StatutorySalesBookRow row)
    {
        var values = new[] { row.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), row.BeneficiaryFullName, row.StatutoryIdNumber, row.BeneficiaryTin ?? string.Empty, row.SalesInvoiceNumber, Money(row.SalesInclusiveVatMinorUnits), Money(row.VatAmountMinorUnits), Money(row.VatExemptSalesMinorUnits), Money(row.Discount5MinorUnits), Money(row.Discount20MinorUnits), Money(row.NetSalesMinorUnits) };
        for (var i = 0; i < widths.Length; i++)
        {
            pdf.StrokeRect(page, x, y - height, widths[i], height, 0.3);
            var shown = values[i].Length > Math.Max(6, (int)(widths[i] / 4.2)) ? values[i][..Math.Max(6, (int)(widths[i] / 4.2))] : values[i];
            if (i >= 5) pdf.RightText(page, x + widths[i] - 2, y - 13, shown, 5.5);
            else pdf.Text(page, x + 2, y - 13, shown, 5.5);
            x += widths[i];
        }
    }

    private static void DrawPdfTotals(A4FiscalReportPdf pdf, int page, double x, double y, double[] widths, double height, StatutorySalesBookSummary summary)
    {
        if (summary.IsEmpty)
        {
            var labelWidth = widths.Take(5).Sum();
            pdf.FillRect(page, x, y - height, labelWidth, height, 0.85, 0.85, 0.85);
            pdf.StrokeRect(page, x, y - height, labelWidth, height, 0.35);
            pdf.Text(page, x + 2, y - 13, "TOTAL - NO QUALIFYING TRANSACTIONS", 5.5, true);
            x += labelWidth;

            var emptyValues = new[]
            {
                summary.SalesInclusiveVatMinorUnits,
                summary.VatAmountMinorUnits,
                summary.VatExemptSalesMinorUnits,
                summary.Discount5MinorUnits,
                summary.Discount20MinorUnits,
                summary.NetSalesMinorUnits
            };
            for (var i = 5; i < widths.Length; i++)
            {
                pdf.FillRect(page, x, y - height, widths[i], height, 0.85, 0.85, 0.85);
                pdf.StrokeRect(page, x, y - height, widths[i], height, 0.35);
                pdf.RightText(page, x + widths[i] - 2, y - 13, Money(emptyValues[i - 5]), 5.5, true);
                x += widths[i];
            }

            return;
        }

        var values = new[]
        {
            "TOTAL", "", "", "", "",
            Money(summary.SalesInclusiveVatMinorUnits), Money(summary.VatAmountMinorUnits), Money(summary.VatExemptSalesMinorUnits),
            Money(summary.Discount5MinorUnits), Money(summary.Discount20MinorUnits), Money(summary.NetSalesMinorUnits)
        };
        for (var i = 0; i < widths.Length; i++)
        {
            pdf.FillRect(page, x, y - height, widths[i], height, 0.85, 0.85, 0.85);
            pdf.StrokeRect(page, x, y - height, widths[i], height, 0.35);
            if (i >= 5) pdf.RightText(page, x + widths[i] - 2, y - 13, values[i], 5.5, true);
            else if (i == 0) pdf.Text(page, x + 2, y - 13, values[i], 5.5, true);
            x += widths[i];
        }
    }

    private static string Worksheet(StatutorySalesBookReport report)
    {
        var h = report.Header;
        var type = report.Summary.Type;
        var totalsRow = 17 + report.Rows.Count;
        var last = Math.Max(25, totalsRow);
        var b = new StringBuilder(24000);
        b.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr><dimension ref=\"A1:K").Append(last).Append("\"/><sheetViews><sheetView workbookViewId=\"0\"/></sheetViews><sheetFormatPr defaultRowHeight=\"15\"/><cols>");
        var widths = type == StatutorySalesBookType.AnnexE2
            ? new[] { 8.43, 22.140625, 14, 14, 11.7109375, 17.28515625, 11.42578125, 11.42578125, 7.140625, 6.7109375, 11.42578125 }
            : new[] { 8.43, 22.140625, 14, 14, 11.7109375, 17.28515625, 11.42578125, 11.42578125, 6.7109375, 6.85546875, 11.42578125 };
        for (var i = 0; i < widths.Length; i++) b.Append("<col min=\"").Append(i + 1).Append("\" max=\"").Append(i + 1).Append("\" width=\"").Append(widths[i].ToString(CultureInfo.InvariantCulture)).Append("\" customWidth=\"1\"/>");
        b.Append("</cols><sheetData>");
        Header(b, 1, h.TaxpayerName); Header(b, 2, h.TaxpayerAddress); Header(b, 3, h.Tin); b.Append("<row r=\"4\"/>");
        HeaderWithAnnex(b, 5, $"Software Name and Version No. plus Release No./Release Date: {h.SoftwareName} {h.SoftwareVersion} / {h.ReleaseNumber} / {h.ReleaseDate}", StatutorySalesBookContract.AnnexLabel(type));
        Header(b, 6, $"Serial No.: {h.SerialNumber}");
        Header(b, 7, $"Machine Identification Number: {h.MachineIdentificationNumber}");
        Header(b, 8, $"POS Server: {h.PosServerCode}");
        Header(b, 9, $"Date and Time Generated: {Pht(h.GeneratedAt):yyyy-MM-dd HH:mm:ss}");
        Header(b, 10, $"UserID: {h.UserId}");
        b.Append("<row r=\"11\" ht=\"15.75\"/>");
        b.Append("<row r=\"12\" ht=\"21.75\"><c r=\"A12\" s=\"2\" t=\"inlineStr\"><is><t>").Append(Xml(StatutorySalesBookContract.Title(type))).Append("</t></is></c></row>");
        Headings(b, type);
        var rowNo = 17;
        foreach (var row in report.Rows) DataRow(b, rowNo++, row);
        TotalRow(b, rowNo++, report.Summary);
        while (rowNo <= last) BlankRow(b, rowNo++);
        b.Append("</sheetData><mergeCells count=\"").Append(report.Summary.IsEmpty ? 17 : 16).Append("\"><mergeCell ref=\"A1:K1\"/><mergeCell ref=\"A2:K2\"/><mergeCell ref=\"A3:K3\"/><mergeCell ref=\"A12:K12\"/><mergeCell ref=\"A13:A16\"/><mergeCell ref=\"B13:B16\"/><mergeCell ref=\"C13:C16\"/><mergeCell ref=\"D13:D16\"/><mergeCell ref=\"E13:E16\"/><mergeCell ref=\"F13:F16\"/><mergeCell ref=\"G13:G16\"/><mergeCell ref=\"H13:H16\"/><mergeCell ref=\"I13:J13\"/><mergeCell ref=\"I14:I16\"/><mergeCell ref=\"J14:J16\"/><mergeCell ref=\"K13:K16\"/>");
        if (report.Summary.IsEmpty) b.Append("<mergeCell ref=\"A").Append(totalsRow).Append(":E").Append(totalsRow).Append("\"/>");
        b.Append("</mergeCells><printOptions horizontalCentered=\"1\"/><pageMargins left=\"0.2362\" right=\"0.2362\" top=\"0.748\" bottom=\"0.748\" header=\"0.315\" footer=\"0.315\"/><pageSetup paperSize=\"9\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"1\"/><headerFooter><oddFooter>&amp;LStatus: ").Append(Xml(report.Summary.ReportStatus)).Append(" | Period: ").Append(report.Summary.CalendarYear.ToString("0000", CultureInfo.InvariantCulture)).Append('-').Append(report.Summary.CalendarMonth.ToString("00", CultureInfo.InvariantCulture)).Append("&amp;RPage &amp;P of &amp;N</oddFooter></headerFooter></worksheet>");
        return b.ToString();
    }

    private static void Headings(StringBuilder b, StatutorySalesBookType type)
    {
        var name = type == StatutorySalesBookType.AnnexE2 ? "Name of Senior Citizen (SC)" : "Name of Person with Disability (PWD)";
        var id = type == StatutorySalesBookType.AnnexE2 ? "OSCA ID No./SC ID No." : "PWD ID No.";
        var tin = type == StatutorySalesBookType.AnnexE2 ? "SC TIN" : "PWD TIN";
        var labels = new[] { "Date", name, id, tin, type == StatutorySalesBookType.AnnexE2 ? "SI/OR Number" : "SI / OR Number", "Sales (inclusive of VAT)", "VAT Amount", "VAT Exempt Sales", "Discount", "", "Net Sales" };
        var row13Height = type == StatutorySalesBookType.AnnexE2 ? "21" : "24.75";
        var row14Height = type == StatutorySalesBookType.AnnexE2 ? "15" : "15.75";
        var row15Height = type == StatutorySalesBookType.AnnexE2 ? "5.25" : "3.75";
        var row16Height = type == StatutorySalesBookType.AnnexE2 ? "2.25" : "9";
        b.Append("<row r=\"13\" ht=\"").Append(row13Height).Append("\" customHeight=\"1\">"); Cells(b, 13, labels, new[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 7 }); b.Append("</row><row r=\"14\" ht=\"").Append(row14Height).Append("\" customHeight=\"1\">");
        Cells(b, 14, new[] { "", "", "", "", "", "", "", "", "5%", "20%", "" }, new[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 7 }); b.Append("</row><row r=\"15\" ht=\"").Append(row15Height).Append("\" customHeight=\"1\"/><row r=\"16\" ht=\"").Append(row16Height).Append("\" customHeight=\"1\"/>");
    }

    private static void DataRow(StringBuilder b, int r, StatutorySalesBookRow row)
    {
        b.Append("<row r=\"").Append(r).Append("\" ht=\"18\">");
        TextCell(b, "A", r, row.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 5); TextCell(b, "B", r, row.BeneficiaryFullName, 5); TextCell(b, "C", r, row.StatutoryIdNumber, 5); TextCell(b, "D", r, row.BeneficiaryTin ?? string.Empty, 5); TextCell(b, "E", r, row.SalesInvoiceNumber, 5);
        MoneyCell(b, "F", r, row.SalesInclusiveVatMinorUnits); MoneyCell(b, "G", r, row.VatAmountMinorUnits); MoneyCell(b, "H", r, row.VatExemptSalesMinorUnits); MoneyCell(b, "I", r, row.Discount5MinorUnits); MoneyCell(b, "J", r, row.Discount20MinorUnits); MoneyCell(b, "K", r, row.NetSalesMinorUnits); b.Append("</row>");
    }
    private static void TotalRow(StringBuilder b, int r, StatutorySalesBookSummary summary)
    {
        b.Append("<row r=\"").Append(r).Append("\" ht=\"18\">");
        TextCell(b, "A", r, summary.IsEmpty ? "TOTAL - NO QUALIFYING TRANSACTIONS" : "TOTAL", 7);
        for (var column = 'B'; column <= 'E'; column++) TextCell(b, column.ToString(), r, string.Empty, 7);
        MoneyCell(b, "F", r, summary.SalesInclusiveVatMinorUnits, 6); MoneyCell(b, "G", r, summary.VatAmountMinorUnits, 6);
        MoneyCell(b, "H", r, summary.VatExemptSalesMinorUnits, 6); MoneyCell(b, "I", r, summary.Discount5MinorUnits, 6);
        MoneyCell(b, "J", r, summary.Discount20MinorUnits, 6); MoneyCell(b, "K", r, summary.NetSalesMinorUnits, 6);
        b.Append("</row>");
    }
    private static void BlankRow(StringBuilder b, int r) { b.Append("<row r=\"").Append(r).Append("\" ht=\"18\">"); for (var i = 0; i < 11; i++) b.Append("<c r=\"").Append((char)('A' + i)).Append(r).Append("\" s=\"5\"/>"); b.Append("</row>"); }
    private static void Header(StringBuilder b, int r, string value) => b.Append("<row r=\"").Append(r).Append("\"><c r=\"A").Append(r).Append("\" s=\"1\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Xml(value)).Append("</t></is></c></row>");
    private static void HeaderWithAnnex(StringBuilder b, int r, string value, string annex) => b.Append("<row r=\"").Append(r).Append("\"><c r=\"A").Append(r).Append("\" s=\"1\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Xml(value)).Append("</t></is></c><c r=\"K").Append(r).Append("\" s=\"3\" t=\"inlineStr\"><is><t>").Append(Xml(annex)).Append("</t></is></c></row>");
    private static void Cells(StringBuilder b,int r,IReadOnlyList<string> values,IReadOnlyList<int> styles){for(var i=0;i<values.Count;i++)TextCell(b,((char)('A'+i)).ToString(),r,values[i],styles[i]);}
    private static void TextCell(StringBuilder b,string c,int r,string value,int style)=>b.Append("<c r=\"").Append(c).Append(r).Append("\" s=\"").Append(style).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Xml(value)).Append("</t></is></c>");
    private static void MoneyCell(StringBuilder b,string c,int r,long value,int style=4)=>b.Append("<c r=\"").Append(c).Append(r).Append("\" s=\"").Append(style).Append("\"><v>").Append(Money(value)).Append("</v></c>");
    private static string Styles() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"3\"><font><sz val=\"9\"/><name val=\"Arial\"/></font><font><b/><sz val=\"10\"/><name val=\"Arial\"/></font><font><b/><sz val=\"9\"/><name val=\"Arial\"/></font></fonts><fills count=\"11\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFA6A6A6\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF00B0F0\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFFF00\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF9999FF\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFC000\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF92D050\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFED7D31\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE7E6E6\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFD966\"/></patternFill></fill></fills><borders count=\"2\"><border/><border><left style=\"thin\"><color auto=\"1\"/></left><right style=\"thin\"><color auto=\"1\"/></right><top style=\"thin\"><color auto=\"1\"/></top><bottom style=\"thin\"><color auto=\"1\"/></bottom></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"16\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"right\"/></xf><xf numFmtId=\"4\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"4\" fontId=\"2\" fillId=\"2\" borderId=\"1\"/><xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"4\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"5\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"6\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"7\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"8\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"9\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"10\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";
    private static string Workbook(StatutorySalesBookReport report) => $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><bookViews><workbookView/></bookViews><sheets><sheet name=\"{(report.Summary.Type == StatutorySalesBookType.AnnexE2 ? "E-2" : "E-3")}\" sheetId=\"1\" r:id=\"rId1\"/></sheets><definedNames><definedName name=\"_xlnm.Print_Area\" localSheetId=\"0\">'{(report.Summary.Type == StatutorySalesBookType.AnnexE2 ? "E-2" : "E-3")}'!$A$1:$K${Math.Max(25, 17 + report.Rows.Count)}</definedName></definedNames><calcPr calcId=\"0\" calcMode=\"manual\"/></workbook>";
    private static string WorkbookRelationships() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
    private static string ContentTypes() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/><Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/></Types>";
    private static string RootRelationships() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/></Relationships>";
    private static string AppProperties(StatutorySalesBookReport report) => $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><Application>ExitPass POS Server</Application><TitlesOfParts><vt:vector size=\"1\" baseType=\"lpstr\"><vt:lpstr>{(report.Summary.Type == StatutorySalesBookType.AnnexE2 ? "E-2" : "E-3")}</vt:lpstr></vt:vector></TitlesOfParts></Properties>";
    private static string CoreProperties() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><dc:creator>ExitPass POS Server</dc:creator><cp:lastModifiedBy>ExitPass POS Server</cp:lastModifiedBy><dcterms:created xsi:type=\"dcterms:W3CDTF\">2000-01-01T00:00:00Z</dcterms:created><dcterms:modified xsi:type=\"dcterms:W3CDTF\">2000-01-01T00:00:00Z</dcterms:modified></cp:coreProperties>";
    private static void Add(ZipArchive archive,string name,string content){var entry=archive.CreateEntry(name,CompressionLevel.NoCompression);entry.LastWriteTime=FixedZipTime;using var writer=new StreamWriter(entry.Open(),new UTF8Encoding(false)){NewLine="\n"};writer.Write(content);}
    private static DateTimeOffset Pht(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, ResolvePht());
    private static TimeZoneInfo ResolvePht(){try{return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");}catch(TimeZoneNotFoundException){return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");}}
    private static string Money(long value)=>(value/100m).ToString("0.00",CultureInfo.InvariantCulture);
    private static string Xml(string value)=>SecurityElement.Escape(value)??string.Empty;
    private static string Safe(string value){var cleaned=new string(value.Select(c=>char.IsLetterOrDigit(c)||c is '-' or '_'?c:'-').ToArray()).Trim('-');return cleaned.Length==0?"unassigned":cleaned.ToLowerInvariant();}
}
