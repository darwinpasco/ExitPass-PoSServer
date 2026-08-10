using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class AnnexE1DeterministicXlsxRenderer
{
    private static readonly DateTime FixedZipWallTime = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTimeOffset FixedZipTime = new(FixedZipWallTime, TimeZoneInfo.Local.GetUtcOffset(FixedZipWallTime));

    public byte[] Render(AnnexE1Header header, IReadOnlyList<AnnexE1Row> rows)
    {
        if (rows.Count is < 1 or > 366) throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedClassification, "The Annex E-1 workbook row count is outside the bounded monthly profile.");
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
        {
            Add(archive, "[Content_Types].xml", ContentTypes());
            Add(archive, "_rels/.rels", RootRelationships());
            Add(archive, "docProps/app.xml", AppProperties());
            Add(archive, "docProps/core.xml", CoreProperties());
            Add(archive, "xl/workbook.xml", WorkbookXml(rows.Count));
            Add(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            Add(archive, "xl/styles.xml", StylesXml());
            Add(archive, "xl/worksheets/sheet1.xml", WorksheetXml(header, rows));
            Add(archive, "xl/worksheets/_rels/sheet1.xml.rels", SheetRelationships());
            Add(archive, "xl/drawings/drawing1.xml", DrawingXml());
        }
        return stream.ToArray();
    }

    private static void Add(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
        entry.LastWriteTime = FixedZipTime;
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false), 4096, leaveOpen: false) { NewLine = "\n" };
        writer.Write(content);
    }

    private static string WorksheetXml(AnnexE1Header h, IReadOnlyList<AnnexE1Row> rows)
    {
        var lastRow = 16 + rows.Count;
        var b = new StringBuilder(32768);
        b.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr><dimension ref=\"A1:AF").Append(lastRow).Append("\"/><sheetViews><sheetView view=\"pageBreakPreview\" zoomScaleNormal=\"100\" workbookViewId=\"0\"/></sheetViews><sheetFormatPr defaultColWidth=\"9\" defaultRowHeight=\"14.4\"/><cols>");
        foreach (var c in new[] { "1,1,8.22", "2,3,10.8518518518519", "4,7,13.4259259259259", "8,11,14", "12,18,10", "19,19,11.712962962963", "20,24,8.22", "25,25,17.287037037037", "26,27,11.4259259259259", "28,29,13.287037037037", "30,32,11.4259259259259" })
        {
            var p = c.Split(','); b.Append("<col min=\"").Append(p[0]).Append("\" max=\"").Append(p[1]).Append("\" width=\"").Append(p[2]).Append("\" customWidth=\"1\"/>");
        }
        b.Append("</cols><sheetData>");
        HeaderRow(b, 1, h.TaxpayerName); HeaderRow(b, 2, h.TaxpayerAddress); HeaderRow(b, 3, h.Tin);
        b.Append("<row r=\"4\"/>");
        HeaderRow(b, 5, $"{h.SoftwareName} {h.SoftwareVersion} / {h.ReleaseNumber} / {h.ReleaseDate}");
        HeaderRow(b, 6, h.PosSerialNumber); HeaderRow(b, 7, h.MachineIdentificationNumber); HeaderRow(b, 8, h.PosTerminalNumber);
        HeaderRow(b, 9, h.GeneratedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));
        HeaderRow(b, 10, h.GeneratedByRef); b.Append("<row r=\"11\"/>");
        b.Append("<row r=\"12\" ht=\"21.75\"><c r=\"A12\" s=\"1\" t=\"inlineStr\"><is><t>BIR SALES SUMMARY  REPORT</t></is></c></row>");
        HeadingRows(b); AnnotationRow(b);
        for (var index = 0; index < rows.Count; index++) DataRow(b, 17 + index, rows[index]);
        b.Append("</sheetData><mergeCells count=\"32\">");
        foreach (var merge in Merges) b.Append("<mergeCell ref=\"").Append(merge).Append("\"/>");
        b.Append("</mergeCells><pageMargins left=\"0.236220472440945\" right=\"0.236220472440945\" top=\"0.748031496062992\" bottom=\"0.748031496062992\" header=\"0.31496062992126\" footer=\"0.31496062992126\"/><pageSetup paperSize=\"14\" fitToWidth=\"1\" fitToHeight=\"0\" orientation=\"landscape\"/><headerFooter/><drawing r:id=\"rId1\"/></worksheet>");
        return b.ToString();
    }

    private static void HeaderRow(StringBuilder b, int row, string value) => b.Append("<row r=\"").Append(row).Append("\"><c r=\"A").Append(row).Append("\" s=\"2\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Xml(value)).Append("</t></is></c></row>");

    private static void HeadingRows(StringBuilder b)
    {
        b.Append("<row r=\"13\" ht=\"15\">");
        var top = new[] { "Date", "Beginning SI/OR No.", "Ending SI/OR No.", "Grand Accum. Sales Ending Balance", "Grand Accum. Beg.Balance", "Sales Issued w/ Manual SI/OR (per RR 16-2018)", "Gross Sales for the Day", "VATable Sales", "VAT Amount", "VAT-Exempt Sales", "Zero-Rated Sales", "Deductions", "", "", "", "", "", "", "", "Adjustment on VAT", "", "", "", "", "", "VAT Payable", "Net Sales", "Sales Overrun /Overflow", "Total Income", "Reset Counter", "Z -Counter", "Remarks" };
        Cells(b, 13, top, 3); b.Append("</row><row r=\"14\" ht=\"15\">");
        var mid = new[] { "", "", "", "", "", "", "", "", "", "", "", "Discount", "", "", "", "", "Returns", "Voids", "Total Deductions", "Discount", "", "", "VAT on Returns", "Others", "Total VAT Adjustment", "", "", "", "", "", "", "" };
        Cells(b, 14, mid, 3); b.Append("</row><row r=\"15\" ht=\"29.55\">");
        var low = new[] { "", "", "", "", "", "", "", "", "", "", "", "SC", "PWD", "NAAC", "Solo Parent", "Others", "", "", "", "SC", "PWD", "Others", "", "", "", "", "", "", "", "", "", "" };
        Cells(b, 15, low, 3); b.Append("</row>");
    }

    private static void AnnotationRow(StringBuilder b)
    {
        var annotations = new[] { "1", "2", "3", "4", "5", "", "6", "7", "8", "9", "10", "11", "12", "", "", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22 = 17+18+19+20+21", "23 = 8-19", "24 = 6-16-8", "25", "26", "27", "28", "29" };
        b.Append("<row r=\"16\" hidden=\"1\" ht=\"28.8\">"); Cells(b, 16, annotations, 6); b.Append("</row>");
    }

    private static void DataRow(StringBuilder b, int rowNumber, AnnexE1Row row)
    {
        b.Append("<row r=\"").Append(rowNumber).Append("\">");
        for (var index = 0; index < row.Positions.Count; index++)
        {
            var value = row.Positions[index]; var reference = Column(index + 1) + rowNumber;
            if (value.MinorUnitsValue is long money)
                b.Append("<c r=\"").Append(reference).Append("\" s=\"4\"><v>").Append(Money(money)).Append("</v></c>");
            else if (value.IntegerValue is long integer)
                b.Append("<c r=\"").Append(reference).Append("\" s=\"5\"><v>").Append(integer.ToString(CultureInfo.InvariantCulture)).Append("</v></c>");
            else
                b.Append("<c r=\"").Append(reference).Append("\" s=\"5\" t=\"inlineStr\"><is><t>").Append(Xml(value.TextValue ?? string.Empty)).Append("</t></is></c>");
        }
        b.Append("</row>");
    }

    private static void Cells(StringBuilder b, int row, IReadOnlyList<string> values, int style)
    {
        for (var index = 0; index < values.Count; index++)
        {
            b.Append("<c r=\"").Append(Column(index + 1)).Append(row).Append("\" s=\"").Append(style).Append("\"");
            if (values[index].Length == 0) b.Append("/>");
            else b.Append(" t=\"inlineStr\"><is><t>").Append(Xml(values[index])).Append("</t></is></c>");
        }
    }

    private static string Money(long minorUnits) => (minorUnits / 100m).ToString("0.00", CultureInfo.InvariantCulture);
    private static string Xml(string value) => SecurityElement.Escape(value) ?? string.Empty;
    private static string Column(int index) { var result = ""; while (index > 0) { index--; result = (char)('A' + index % 26) + result; index /= 26; } return result; }

    private static readonly string[] Merges = ["A1:AF1", "A2:AF2", "A3:AF3", "A12:AF12", "L13:S13", "T13:Y13", "L14:P14", "T14:V14", "A13:A15", "B13:B15", "C13:C15", "D13:D15", "E13:E15", "F13:F15", "G13:G15", "H13:H15", "I13:I15", "J13:J15", "K13:K15", "Q14:Q15", "R14:R15", "S14:S15", "W14:W15", "X14:X15", "Y14:Y15", "Z13:Z15", "AA13:AA15", "AB13:AB15", "AC13:AC15", "AD13:AD15", "AE13:AE15", "AF13:AF15"];

    private static string ContentTypes() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawing+xml\"/><Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/><Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/></Types>";
    private static string RootRelationships() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/></Relationships>";
    private static string AppProperties() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><Application>ExitPass POS Server</Application><AppVersion>1.3</AppVersion></Properties>";
    private static string CoreProperties() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><dc:creator>ExitPass POS Server</dc:creator><cp:lastModifiedBy>ExitPass POS Server</cp:lastModifiedBy><dcterms:created xsi:type=\"dcterms:W3CDTF\">2000-01-01T00:00:00Z</dcterms:created><dcterms:modified xsi:type=\"dcterms:W3CDTF\">2000-01-01T00:00:00Z</dcterms:modified></cp:coreProperties>";
    private static string WorkbookXml(int rowCount) => $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><fileVersion appName=\"xl\"/><workbookPr/><bookViews><workbookView windowWidth=\"23040\" windowHeight=\"9000\"/></bookViews><sheets><sheet name=\"E-1\" sheetId=\"1\" r:id=\"rId1\"/></sheets><definedNames><definedName name=\"_xlnm.Print_Area\" localSheetId=\"0\">'E-1'!$A$1:$AF${16 + rowCount}</definedName></definedNames><calcPr calcMode=\"manual\" calcId=\"191029\"/></workbook>";
    private static string WorkbookRelationships() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
    private static string SheetRelationships() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing1.xml\"/></Relationships>";
    private static string DrawingXml() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><xdr:twoCellAnchor><xdr:from><xdr:col>29</xdr:col><xdr:colOff>685800</xdr:colOff><xdr:row>0</xdr:row><xdr:rowOff>47625</xdr:rowOff></xdr:from><xdr:to><xdr:col>31</xdr:col><xdr:colOff>714375</xdr:colOff><xdr:row>2</xdr:row><xdr:rowOff>3810</xdr:rowOff></xdr:to><xdr:sp><xdr:nvSpPr><xdr:cNvPr id=\"4\" name=\"Annex E-1\"/><xdr:cNvSpPr txBox=\"1\"/></xdr:nvSpPr><xdr:spPr><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val=\"FFFFFF\"/></a:solidFill><a:ln><a:solidFill><a:srgbClr val=\"000000\"/></a:solidFill></a:ln></xdr:spPr><xdr:txBody><a:bodyPr wrap=\"square\"/><a:lstStyle/><a:p><a:pPr algn=\"ctr\"/><a:r><a:rPr lang=\"en-PH\" sz=\"1600\" b=\"1\"/><a:t>ANNEX E-1</a:t></a:r></a:p></xdr:txBody></xdr:sp><xdr:clientData/></xdr:twoCellAnchor></xdr:wsDr>";
    private static string StylesXml() => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><numFmts count=\"2\"><numFmt numFmtId=\"164\" formatCode=\"0.00\"/><numFmt numFmtId=\"165\" formatCode=\"yyyy-mm-dd\"/></numFmts><fonts count=\"3\"><font><sz val=\"10\"/><name val=\"Arial\"/></font><font><b/><sz val=\"14\"/><name val=\"Arial\"/></font><font><b/><sz val=\"8\"/><name val=\"Arial\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border><border><left style=\"thin\"/><right style=\"thin\"/><top style=\"thin\"/><bottom style=\"thin\"/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"7\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";
}
