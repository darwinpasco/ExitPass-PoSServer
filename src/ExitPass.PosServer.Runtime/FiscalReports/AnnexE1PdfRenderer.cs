using System.Globalization;
using System.Security.Cryptography;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed record AnnexE1PdfExport(
    string ContentType,
    string FileName,
    byte[] Bytes,
    string Sha256);

public sealed class AnnexE1PdfRenderer
{
    private const double LandscapeWidth = A4FiscalReportPdf.PageHeight;
    private const double LandscapeHeight = A4FiscalReportPdf.PageWidth;
    private const double Margin = 18;
    private const double TableWidth = LandscapeWidth - (Margin * 2);
    private const double HeaderRowHeight = 18;
    private const double DataRowHeight = 13;
    private const double HeaderFontSize = 3.4;
    private const double DataFontSize = 3.8;

    private static readonly double[] ExcelColumnWidths =
    [
        8.22, 10.8518518518519, 10.8518518518519,
        13.4259259259259, 13.4259259259259, 13.4259259259259, 13.4259259259259,
        14, 14, 14, 14,
        10, 10, 10, 10, 10, 10, 10,
        11.712962962963,
        8.22, 8.22, 8.22, 8.22, 8.22,
        17.287037037037,
        11.4259259259259, 11.4259259259259,
        13.287037037037, 13.287037037037,
        11.4259259259259, 11.4259259259259, 11.4259259259259
    ];

    private static readonly double[] Columns = ScaleColumns(ExcelColumnWidths);

    private static readonly HeaderCell[] HeaderCells =
    [
        new(0, 0, 1, 3, "Date"),
        new(1, 0, 1, 3, "Beginning SI/OR No."),
        new(2, 0, 1, 3, "Ending SI/OR No."),
        new(3, 0, 1, 3, "Grand Accum. Sales Ending Balance"),
        new(4, 0, 1, 3, "Grand Accum. Beg.Balance"),
        new(5, 0, 1, 3, "Sales Issued w/ Manual SI/OR (per RR 16-2018)"),
        new(6, 0, 1, 3, "Gross Sales for the Day"),
        new(7, 0, 1, 3, "VATable Sales"),
        new(8, 0, 1, 3, "VAT Amount"),
        new(9, 0, 1, 3, "VAT-Exempt Sales"),
        new(10, 0, 1, 3, "Zero-Rated Sales"),
        new(11, 0, 8, 1, "Deductions"),
        new(19, 0, 6, 1, "Adjustment on VAT"),
        new(25, 0, 1, 3, "VAT Payable"),
        new(26, 0, 1, 3, "Net Sales"),
        new(27, 0, 1, 3, "Sales Overrun /Overflow"),
        new(28, 0, 1, 3, "Total Income"),
        new(29, 0, 1, 3, "Reset Counter"),
        new(30, 0, 1, 3, "Z -Counter"),
        new(31, 0, 1, 3, "Remarks"),
        new(11, 1, 5, 1, "Discount"),
        new(16, 1, 1, 2, "Returns"),
        new(17, 1, 1, 2, "Voids"),
        new(18, 1, 1, 2, "Total Deductions"),
        new(19, 1, 3, 1, "Discount"),
        new(22, 1, 1, 2, "VAT on Returns"),
        new(23, 1, 1, 2, "Others"),
        new(24, 1, 1, 2, "Total VAT Adjustment"),
        new(11, 2, 1, 1, "SC"),
        new(12, 2, 1, 1, "PWD"),
        new(13, 2, 1, 1, "NAAC"),
        new(14, 2, 1, 1, "Solo Parent"),
        new(15, 2, 1, 1, "Others"),
        new(19, 2, 1, 1, "SC"),
        new(20, 2, 1, 1, "PWD"),
        new(21, 2, 1, 1, "Others")
    ];

    public AnnexE1PdfExport Render(AnnexE1WorkbookRecord workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        var pdf = new A4FiscalReportPdf();
        var page = pdf.AddPage(LandscapeWidth, LandscapeHeight);
        var y = DrawHeader(pdf, page, workbook);
        y = DrawTableHeader(pdf, page, y);

        foreach (var row in workbook.Rows.OrderBy(item => item.Source.PeriodSequence))
        {
            if (y - DataRowHeight < 18)
            {
                page = pdf.AddPage(LandscapeWidth, LandscapeHeight);
                y = DrawContinuationHeader(pdf, page, workbook);
                y = DrawTableHeader(pdf, page, y);
            }

            y = DrawRow(pdf, page, row, y);
        }

        var bytes = pdf.Build();
        var stem = Path.GetFileNameWithoutExtension(workbook.FileName);
        var fileName = $"{(string.IsNullOrWhiteSpace(stem) ? workbook.AnnexEReference : stem)}.pdf";
        return new(
            "application/pdf",
            fileName,
            bytes,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    private static double DrawHeader(A4FiscalReportPdf pdf, int page, AnnexE1WorkbookRecord workbook)
    {
        var y = LandscapeHeight - Margin - 7;
        var lines = new[]
        {
            workbook.Header.TaxpayerName,
            workbook.Header.TaxpayerAddress,
            workbook.Header.Tin,
            string.Empty,
            $"{workbook.Header.SoftwareName} {workbook.Header.SoftwareVersion} / {workbook.Header.ReleaseNumber} / {workbook.Header.ReleaseDate}",
            workbook.Header.PosSerialNumber,
            workbook.Header.MachineIdentificationNumber,
            workbook.Header.PosTerminalNumber,
            workbook.Header.GeneratedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            workbook.Header.GeneratedByRef
        };
        foreach (var line in lines)
        {
            pdf.Text(page, Margin, y, line, 6.2);
            y -= string.IsNullOrEmpty(line) ? 4 : 8;
        }

        var annexX = ColumnX(29);
        var annexWidth = Columns.Skip(29).Sum();
        pdf.StrokeRect(page, annexX, LandscapeHeight - Margin - 28, annexWidth, 24, 0.5);
        pdf.CenterText(page, annexX + (annexWidth / 2), LandscapeHeight - Margin - 18, "ANNEX E-1", 10, true);

        y -= 5;
        pdf.CenterText(page, LandscapeWidth / 2, y, "BIR SALES SUMMARY REPORT", 12, true);
        return y - 12;
    }

    private static double DrawContinuationHeader(A4FiscalReportPdf pdf, int page, AnnexE1WorkbookRecord workbook)
    {
        var y = LandscapeHeight - Margin - 8;
        pdf.CenterText(page, LandscapeWidth / 2, y, "BIR SALES SUMMARY REPORT", 9, true);
        pdf.RightText(page, LandscapeWidth - Margin, y, "ANNEX E-1", 7, true);
        return y - 10;
    }

    private static double DrawTableHeader(A4FiscalReportPdf pdf, int page, double y)
    {
        var height = HeaderRowHeight * 3;
        pdf.FillRect(page, Margin, y - height, TableWidth, height, 0.92);
        foreach (var cell in HeaderCells)
        {
            var x = ColumnX(cell.Column);
            var width = Columns.Skip(cell.Column).Take(cell.ColumnSpan).Sum();
            var top = y - (cell.HeaderRow * HeaderRowHeight);
            var cellHeight = cell.HeaderRowSpan * HeaderRowHeight;
            pdf.StrokeRect(page, x, top - cellHeight, width, cellHeight, 0.3);
            DrawCellText(pdf, page, x, top, width, cellHeight, cell.Text, HeaderFontSize, true, false);
        }
        return y - height;
    }

    private static double DrawRow(A4FiscalReportPdf pdf, int page, AnnexE1Row row, double y)
    {
        var ordered = row.Positions.OrderBy(item => int.Parse(item.Position.AsSpan(1), CultureInfo.InvariantCulture)).ToArray();
        if (ordered.Length != Columns.Length)
            throw new InvalidOperationException("Annex E-1 PDF requires all 32 canonical workbook positions.");

        var x = Margin;
        for (var index = 0; index < Columns.Length; index++)
        {
            var value = Display(ordered[index]);
            pdf.StrokeRect(page, x, y - DataRowHeight, Columns[index], DataRowHeight, 0.25);
            DrawCellText(pdf, page, x, y, Columns[index], DataRowHeight, value, DataFontSize, false, ordered[index].MinorUnitsValue is not null);
            x += Columns[index];
        }
        return y - DataRowHeight;
    }

    private static void DrawCellText(A4FiscalReportPdf pdf, int page, double x, double top, double width, double height,
        string text, double fontSize, bool bold, bool rightAligned)
    {
        var maxCharacters = Math.Max(2, (int)((width - 3) / (fontSize * 0.52)));
        var lines = A4FiscalReportPdf.Wrap(text, maxCharacters);
        if (lines.Count == 0) return;
        var lineHeight = fontSize + 0.7;
        var firstY = top - ((height - (lines.Count * lineHeight)) / 2) - fontSize;
        for (var index = 0; index < lines.Count; index++)
        {
            var lineY = firstY - (index * lineHeight);
            if (rightAligned) pdf.RightText(page, x + width - 1.5, lineY, lines[index], fontSize, bold);
            else pdf.CenterText(page, x + (width / 2), lineY, lines[index], fontSize, bold);
        }
    }

    private static string Display(AnnexE1PositionValue value)
    {
        if (value.MinorUnitsValue is long minorUnits)
            return (minorUnits / 100m).ToString("0.00", CultureInfo.InvariantCulture);
        if (value.IntegerValue is long integer)
            return integer.ToString(CultureInfo.InvariantCulture);
        return value.TextValue ?? string.Empty;
    }

    private static double ColumnX(int index) => Margin + Columns.Take(index).Sum();

    private static double[] ScaleColumns(IReadOnlyList<double> source)
    {
        var scale = TableWidth / source.Sum();
        return source.Select(width => width * scale).ToArray();
    }

    private sealed record HeaderCell(int Column, int HeaderRow, int ColumnSpan, int HeaderRowSpan, string Text);
}
