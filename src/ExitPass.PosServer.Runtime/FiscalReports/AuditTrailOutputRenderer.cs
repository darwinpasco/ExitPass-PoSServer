using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class AuditTrailOutputRenderer
{
    private static readonly TimeSpan PhtOffset = TimeSpan.FromHours(8);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    public AuditTrailExport Render(AuditTrailReport report, AuditTrailExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);
        var bytes = format switch
        {
            AuditTrailExportFormat.Json => JsonSerializer.SerializeToUtf8Bytes(
                new { schemaVersion = AuditTrailContract.JsonExportVersion, report }, JsonOptions),
            AuditTrailExportFormat.Csv => Encoding.UTF8.GetBytes(RenderCsv(report)),
            AuditTrailExportFormat.Pdf => RenderPdf(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var extension = format.ToString().ToLowerInvariant();
        var contentType = format switch
        {
            AuditTrailExportFormat.Json => "application/json; charset=utf-8",
            AuditTrailExportFormat.Csv => "text/csv; charset=utf-8",
            _ => "application/pdf"
        };
        var from = report.From.ToOffset(PhtOffset).Date;
        var inclusiveTo = report.To.ToOffset(PhtOffset).AddTicks(-1).Date;
        var fileName = $"audit-trail-{from:yyyyMMdd}-{inclusiveTo:yyyyMMdd}.{extension}";
        return new(format, contentType, fileName, bytes, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    private static string RenderCsv(AuditTrailReport report)
    {
        var rows = new List<string>
        {
            "Date,Terminal,UserID,User Role,Action Category,Action Description,SI No.,Before Value,After Value,Reason/Remarks"
        };
        rows.AddRange(report.Rows.Select(row => string.Join(',', new[]
        {
            row.OccurredAt.ToOffset(PhtOffset).ToString("yyyy-MM-dd\nHH:mm:ss", CultureInfo.InvariantCulture),
            row.Terminal ?? string.Empty,
            row.UserId ?? string.Empty,
            row.UserRole ?? string.Empty,
            row.ActionCategory,
            row.ActionDescription,
            row.SalesInvoiceNumber ?? string.Empty,
            row.BeforeValue ?? string.Empty,
            row.AfterValue ?? string.Empty,
            row.ReasonRemarks ?? string.Empty
        }.Select(Escape))));
        return string.Join("\r\n", rows) + "\r\n";
    }

    private static string Escape(string value)
    {
        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", "\r\n", StringComparison.Ordinal);
        return normalized.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{normalized.Replace("\"", "\"\"")}\""
            : normalized;
    }

    private static byte[] RenderPdf(AuditTrailReport report)
    {
        var pdf = new AuditTrailPdfDocument();
        var page = pdf.AddPage();
        var y = DrawReportHeader(pdf, page, report);
        y = DrawTableHeader(pdf, page, y);
        foreach (var row in report.Rows)
        {
            var cells = Cells(row);
            var lines = cells.Select((value, index) => WrapCell(value, ColumnWidths[index])).ToArray();
            var height = Math.Max(18, lines.Max(value => value.Count) * 6.4 + 6);
            if (y - height < 28)
            {
                page = pdf.AddPage();
                y = DrawContinuationHeader(pdf, page, report);
                y = DrawTableHeader(pdf, page, y);
            }
            DrawRow(pdf, page, y, height, lines);
            y -= height;
        }
        pdf.AddPageNumbers();
        return pdf.Build();
    }

    private static readonly double[] ColumnWidths = [78, 78, 78, 78, 78, 78, 78, 78, 78, 78];
    private static readonly string[] Headings =
    [
        "Date", "Terminal", "UserID", "User Role", "Action Category", "Action Description", "SI No.",
        "Before Value", "After Value", "Reason/Remarks"
    ];

    private static double DrawReportHeader(AuditTrailPdfDocument pdf, int page, AuditTrailReport report)
    {
        var center = AuditTrailPdfDocument.PageWidth / 2;
        var y = AuditTrailPdfDocument.PageHeight - 35;
        foreach (var line in AuditTrailPdfDocument.Wrap($"Address: {report.Header.RegisteredBusinessAddress}", 116))
        {
            pdf.CenterText(page, center, y, line, 8);
            y -= 9;
        }
        pdf.CenterText(page, center, y, $"TIN: {report.Header.Tin}", 8); y -= 9;
        pdf.CenterText(page, center, y, $"Machine ID No.: {report.Header.MachineIdentificationNumber}", 8); y -= 9;
        pdf.CenterText(page, center, y, $"Serial No.: {report.Header.PosSerialNumber}", 8); y -= 9;
        pdf.CenterText(page, center, y, DateLabel(report), 8); y -= 19;
        pdf.CenterText(page, center, y, "Audit Trail Report", 11, true);
        return y - 14;
    }

    private static double DrawContinuationHeader(AuditTrailPdfDocument pdf, int page, AuditTrailReport report)
    {
        var y = AuditTrailPdfDocument.PageHeight - 32;
        pdf.CenterText(page, AuditTrailPdfDocument.PageWidth / 2, y, "Audit Trail Report", 10, true);
        pdf.RightText(page, AuditTrailPdfDocument.PageWidth - AuditTrailPdfDocument.Margin, y, DateLabel(report), 7);
        return y - 12;
    }

    private static string DateLabel(AuditTrailReport report)
    {
        var from = report.From.ToOffset(PhtOffset).Date;
        var to = report.To.ToOffset(PhtOffset).AddTicks(-1).Date;
        return from == to
            ? from.ToString("MMM dd yyyy", CultureInfo.InvariantCulture)
            : $"{from:MMM dd yyyy} - {to:MMM dd yyyy}";
    }

    private static double DrawTableHeader(AuditTrailPdfDocument pdf, int page, double y)
    {
        const double height = 24;
        var x = AuditTrailPdfDocument.Margin;
        for (var index = 0; index < Headings.Length; index++)
        {
            pdf.FillRect(page, x, y - height, ColumnWidths[index], height, 0.88);
            pdf.StrokeRect(page, x, y - height, ColumnWidths[index], height, 0.45);
            var lines = AuditTrailPdfDocument.Wrap(Headings[index], 12);
            for (var line = 0; line < lines.Count; line++)
                pdf.CenterText(page, x + ColumnWidths[index] / 2, y - 9 - line * 7, lines[line], 5.5, true);
            x += ColumnWidths[index];
        }
        return y - height;
    }

    private static void DrawRow(AuditTrailPdfDocument pdf, int page, double y, double height, IReadOnlyList<string>[] cells)
    {
        var x = AuditTrailPdfDocument.Margin;
        for (var index = 0; index < cells.Length; index++)
        {
            pdf.StrokeRect(page, x, y - height, ColumnWidths[index], height, 0.3);
            for (var line = 0; line < cells[index].Count; line++)
                pdf.Text(page, x + 3, y - 8 - line * 6.4, cells[index][line], 5.25);
            x += ColumnWidths[index];
        }
    }

    private static string[] Cells(AuditTrailRow row) =>
    [
        row.OccurredAt.ToOffset(PhtOffset).ToString("yyyy-MM-dd\nHH:mm:ss", CultureInfo.InvariantCulture),
        row.Terminal ?? string.Empty,
        row.UserId ?? string.Empty,
        row.UserRole ?? string.Empty,
        row.ActionCategory,
        row.ActionDescription,
        row.SalesInvoiceNumber ?? string.Empty,
        row.BeforeValue ?? string.Empty,
        row.AfterValue ?? string.Empty,
        row.ReasonRemarks ?? "-"
    ];

    private static IReadOnlyList<string> WrapCell(string value, double width)
    {
        var result = new List<string>();
        var max = Math.Max(4, (int)((width - 6) / 3.15));
        foreach (var logicalLine in value.Replace("\r", string.Empty).Split('\n'))
        {
            var wrapped = AuditTrailPdfDocument.Wrap(logicalLine, max);
            if (wrapped.Count == 0) result.Add(string.Empty); else result.AddRange(wrapped);
        }
        return result;
    }

    private sealed class AuditTrailPdfDocument
    {
        public const double PageWidth = 842;
        public const double PageHeight = 595;
        public const double Margin = 31;
        private readonly List<Page> pages = [];
        public int AddPage() { pages.Add(new(new StringBuilder())); return pages.Count - 1; }
        public void Text(int page, double x, double y, string value, double size, bool bold = false) =>
            pages[page].Content.Append("BT /").Append(bold ? "F2" : "F1").Append(' ').Append(N(size)).Append(" Tf 1 0 0 1 ").Append(N(x)).Append(' ').Append(N(y)).Append(" Tm (").Append(E(value)).Append(") Tj ET\n");
        public void CenterText(int page, double center, double y, string value, double size, bool bold = false) => Text(page, center - Width(value, size) / 2, y, value, size, bold);
        public void RightText(int page, double right, double y, string value, double size) => Text(page, right - Width(value, size), y, value, size);
        public void FillRect(int page, double x, double y, double width, double height, double gray) => pages[page].Content.Append(N(gray)).Append(" g ").Append(N(x)).Append(' ').Append(N(y)).Append(' ').Append(N(width)).Append(' ').Append(N(height)).Append(" re f 0 g\n");
        public void StrokeRect(int page, double x, double y, double width, double height, double lineWidth) => pages[page].Content.Append(N(lineWidth)).Append(" w ").Append(N(x)).Append(' ').Append(N(y)).Append(' ').Append(N(width)).Append(' ').Append(N(height)).Append(" re S\n");
        public void AddPageNumbers()
        {
            for (var i = 0; i < pages.Count; i++)
            {
                Text(i, Margin, 12, "ExitPass POS Server | Audit Trail Report", 6);
                RightText(i, PageWidth - Margin, 12, $"Page {i + 1} of {pages.Count}", 6);
            }
        }
        public byte[] Build()
        {
            if (pages.Count == 0) AddPage();
            var objects = new List<byte[]>
            {
                B("<< /Type /Catalog /Pages 2 0 R >>"),
                B($"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, pages.Count).Select(i => $"{5 + i * 2} 0 R"))}] /Count {pages.Count} >>"),
                B("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>"),
                B("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>")
            };
            for (var i = 0; i < pages.Count; i++)
            {
                var contentId = 6 + i * 2;
                objects.Add(B($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(PageWidth)} {N(PageHeight)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>"));
                var content = B(pages[i].Content.ToString());
                using var stream = new MemoryStream();
                W(stream, $"<< /Length {content.Length} >>\nstream\n"); stream.Write(content); W(stream, "\nendstream");
                objects.Add(stream.ToArray());
            }
            using var output = new MemoryStream(); W(output, "%PDF-1.4\n"); var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Count; i++) { offsets.Add(output.Position); W(output, $"{i + 1} 0 obj\n"); output.Write(objects[i]); W(output, "\nendobj\n"); }
            var xref = output.Position; W(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            for (var i = 1; i < offsets.Count; i++) W(output, $"{offsets[i]:0000000000} 00000 n \n");
            W(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"); return output.ToArray();
        }
        public static IReadOnlyList<string> Wrap(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return [];
            var result = new List<string>(); var current = new StringBuilder();
            foreach (var word in value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length > max && current.Length == 0) { for (var i = 0; i < word.Length; i += max) result.Add(word.Substring(i, Math.Min(max, word.Length - i))); continue; }
                if (current.Length == 0) current.Append(word);
                else if (current.Length + word.Length + 1 <= max) current.Append(' ').Append(word);
                else { result.Add(current.ToString()); current.Clear().Append(word); }
            }
            if (current.Length > 0) result.Add(current.ToString()); return result;
        }
        private static double Width(string value, double size) => value.Length * size * 0.6;
        private static string E(string value) { var b = new StringBuilder(); foreach (var c in value.Replace('\r', ' ').Replace('\n', ' ')) { var n = c <= 255 ? c : '?'; if (n is '\\' or '(' or ')') b.Append('\\'); b.Append(n); } return b.ToString(); }
        private static byte[] B(string value) => Encoding.Latin1.GetBytes(value);
        private static void W(Stream stream, string value) => stream.Write(B(value));
        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        private sealed record Page(StringBuilder Content);
    }
}
