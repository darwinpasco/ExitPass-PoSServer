using System.Globalization;
using System.Text;

namespace ExitPass.PosServer.Runtime.FiscalReports;

internal sealed class A4FiscalReportPdf
{
    public const double PageWidth = 595.28;
    public const double PageHeight = 841.89;
    public const double Margin = 32;
    public const double ContentWidth = PageWidth - (Margin * 2);

    private readonly List<PdfPage> pages = [];

    public int PageCount => pages.Count;

    public int AddPage() => AddPage(PageWidth, PageHeight);

    public int AddPage(double width, double height)
    {
        pages.Add(new PdfPage(new StringBuilder(), width, height));
        return pages.Count - 1;
    }

    public void Text(int page, double x, double y, string? value, double size = 8, bool bold = false)
    {
        if (string.IsNullOrEmpty(value)) return;
        pages[page].Content.Append("BT /").Append(bold ? "F2" : "F1").Append(' ')
            .Append(Number(size)).Append(" Tf 1 0 0 1 ").Append(Number(x)).Append(' ')
            .Append(Number(y)).Append(" Tm (").Append(Escape(value)).Append(") Tj ET\n");
    }

    public void RightText(int page, double right, double y, string? value, double size = 8, bool bold = false) =>
        Text(page, right - TextWidth(value, size, bold), y, value, size, bold);

    public void CenterText(int page, double center, double y, string? value, double size = 8, bool bold = false) =>
        Text(page, center - (TextWidth(value, size, bold) / 2), y, value, size, bold);

    public void Line(int page, double x1, double y1, double x2, double y2, double width = 0.5)
    {
        pages[page].Content.Append(Number(width)).Append(" w ")
            .Append(Number(x1)).Append(' ').Append(Number(y1)).Append(" m ")
            .Append(Number(x2)).Append(' ').Append(Number(y2)).Append(" l S\n");
    }

    public void FillRect(int page, double x, double y, double width, double height, double gray = 0.94)
    {
        pages[page].Content.Append(Number(gray)).Append(" g ")
            .Append(Number(x)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(width)).Append(' ').Append(Number(height)).Append(" re f 0 g\n");
    }

    public void FillRect(int page, double x, double y, double width, double height, double red, double green, double blue)
    {
        pages[page].Content.Append(Number(red)).Append(' ').Append(Number(green)).Append(' ').Append(Number(blue)).Append(" rg ")
            .Append(Number(x)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(width)).Append(' ').Append(Number(height)).Append(" re f 0 g\n");
    }

    public void StrokeRect(int page, double x, double y, double width, double height, double lineWidth = 0.5)
    {
        pages[page].Content.Append(Number(lineWidth)).Append(" w ")
            .Append(Number(x)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(width)).Append(' ').Append(Number(height)).Append(" re S\n");
    }

    public static IReadOnlyList<string> Wrap(string? value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var words = value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length == 0 && word.Length > maxCharacters)
            {
                for (var offset = 0; offset < word.Length; offset += maxCharacters)
                    lines.Add(word.Substring(offset, Math.Min(maxCharacters, word.Length - offset)));
                continue;
            }
            if (current.Length == 0)
            {
                current.Append(word);
                continue;
            }
            if (current.Length + 1 + word.Length <= maxCharacters)
            {
                current.Append(' ').Append(word);
                continue;
            }
            lines.Add(current.ToString());
            current.Clear().Append(word);
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines;
    }

    public void AddFooters(string reportName, string? softwareVersion, bool technicalUat)
    {
        for (var index = 0; index < pages.Count; index++)
        {
            var pageWidth = pages[index].Width;
            Line(index, Margin, 24, pageWidth - Margin, 24, 0.35);
            var version = string.IsNullOrWhiteSpace(softwareVersion) ? string.Empty : $" | {softwareVersion.Trim()}";
            Text(index, Margin, 12, $"ExitPass POS Server | {reportName}{version}", 6.5);
            CenterText(index, pageWidth / 2, 12, technicalUat ? "TECHNICAL UAT - NOT FOR BIR SUBMISSION" : string.Empty, 6.5, true);
            RightText(index, pageWidth - Margin, 12, $"Page {index + 1} of {pages.Count}", 6.5);
        }
    }

    public byte[] Build()
    {
        if (pages.Count == 0) AddPage();
        var objects = new List<byte[]>();
        objects.Add(Bytes("<< /Type /Catalog /Pages 2 0 R >>"));
        var pageIds = Enumerable.Range(0, pages.Count).Select(index => 5 + (index * 2)).ToArray();
        objects.Add(Bytes($"<< /Type /Pages /Kids [{string.Join(' ', pageIds.Select(id => $"{id} 0 R"))}] /Count {pages.Count} >>"));
        objects.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        objects.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));
        for (var index = 0; index < pages.Count; index++)
        {
            var contentId = 6 + (index * 2);
            var page = pages[index];
            objects.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(page.Width)} {Number(page.Height)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>"));
            var content = Bytes(page.Content.ToString());
            using var streamObject = new MemoryStream();
            Write(streamObject, $"<< /Length {content.Length} >>\nstream\n");
            streamObject.Write(content);
            Write(streamObject, "\nendstream");
            objects.Add(streamObject.ToArray());
        }

        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            Write(output, $"{index + 1} 0 obj\n");
            output.Write(objects[index]);
            Write(output, "\nendobj\n");
        }
        var xref = output.Position;
        Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        for (var index = 1; index < offsets.Count; index++)
            Write(output, $"{offsets[index]:0000000000} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private static double TextWidth(string? value, double size, bool bold) =>
        (value?.Length ?? 0) * size * (bold ? 0.56 : 0.52);

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Replace('\r', ' ').Replace('\n', ' '))
        {
            var normalized = character <= 255 ? character : '?';
            if (normalized is '\\' or '(' or ')') builder.Append('\\');
            builder.Append(normalized);
        }
        return builder.ToString();
    }

    private static byte[] Bytes(string value) => Encoding.Latin1.GetBytes(value);
    private static void Write(Stream stream, string value) => stream.Write(Bytes(value));
    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed record PdfPage(StringBuilder Content, double Width, double Height);
}
