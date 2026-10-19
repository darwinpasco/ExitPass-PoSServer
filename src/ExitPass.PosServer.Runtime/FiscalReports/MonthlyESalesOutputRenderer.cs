using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class MonthlyESalesOutputRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    public MonthlyESalesExport Render(
        MonthlyESalesReport report,
        MonthlyESalesExportFormat format,
        MonthlyESalesPdfReport? pdfReport = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        var bytes = format switch
        {
            MonthlyESalesExportFormat.Json => JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = MonthlyESalesContract.JsonExportVersion, report }, JsonOptions),
            MonthlyESalesExportFormat.Csv => Encoding.UTF8.GetBytes(RenderCsv(report)),
            MonthlyESalesExportFormat.Pdf when pdfReport is not null => RenderPdf(pdfReport),
            MonthlyESalesExportFormat.Pdf => throw new ArgumentNullException(nameof(pdfReport)),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var extension = format.ToString().ToLowerInvariant();
        var contentType = format switch
        {
            MonthlyESalesExportFormat.Json => "application/json; charset=utf-8",
            MonthlyESalesExportFormat.Csv => "text/csv; charset=utf-8",
            _ => "application/pdf"
        };
        var posture = report.SubmissionReadiness == MonthlyESalesSubmissionReadiness.TechnicalUat ? "technical-uat-" : string.Empty;
        var coverage = report.CoverageStatus == MonthlyESalesCoverageStatus.MonthToDate
            ? $"mtd-through-{report.LastCoveredBusinessDate:yyyyMMdd}-"
            : string.Empty;
        return new(format, contentType, $"monthly-esales-{posture}{coverage}{report.CalendarYear:D4}{report.CalendarMonth:D2}.{extension}",
            bytes, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    private static string RenderCsv(MonthlyESalesReport report)
    {
        var rows = new List<string> { "MACHINE IDENTIFICATION NO. of CRM/POS,MONTH,YEAR,MONTHLY SALES,END RANGE OF OFFICIAL RECEIPT" };
        rows.AddRange(report.Rows.OrderBy(row => row.MachineIdentificationNumber, StringComparer.Ordinal).Select(row => string.Join(',',
            Escape(row.MachineIdentificationNumber),
            row.CalendarMonth.ToString("D2", CultureInfo.InvariantCulture),
            row.CalendarYear.ToString("D4", CultureInfo.InvariantCulture),
            row.MonthlySalesDisplay,
            Escape(row.EndingSalesInvoiceReference ?? string.Empty))));
        return string.Join("\r\n", rows) + "\r\n";
    }

    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;

    private static byte[] RenderPdf(MonthlyESalesPdfReport source)
    {
        var pdf = new A4FiscalReportPdf();
        foreach (var machine in source.Machines.OrderBy(item => item.MachineIdentificationNumber, StringComparer.Ordinal))
        {
            var page = pdf.AddPage();
            var y = DrawHeader(pdf, page, source);
            y = DrawMachineBand(pdf, page, machine, y);
            y = DrawTableHeader(pdf, page, y);
            foreach (var row in machine.DailyRows)
            {
                if (y < 66)
                {
                    page = pdf.AddPage();
                    y = DrawContinuationHeader(pdf, page, source, machine);
                    y = DrawTableHeader(pdf, page, y);
                }
                y = DrawDailyRow(pdf, page, row, y);
            }
            DrawTotalRow(pdf, page, machine.Total, y);
        }
        pdf.AddFooters("Monthly eSales", source.SoftwareVersion, technicalUat: false);
        return pdf.Build();
    }

    private static double DrawHeader(A4FiscalReportPdf pdf, int page, MonthlyESalesPdfReport source)
    {
        var y = A4FiscalReportPdf.PageHeight - 42;
        pdf.CenterText(page, A4FiscalReportPdf.PageWidth / 2, y, "MONTHLY eSALES", 16, true);
        y -= 18;
        pdf.CenterText(page, A4FiscalReportPdf.PageWidth / 2, y,
            new DateOnly(source.Report.CalendarYear, source.Report.CalendarMonth, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture), 10, true);
        y -= 25;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "TAXPAYER INFORMATION", 9, true);
        y -= 14;
        y = Metadata(pdf, page, y, "Registered Business Name", source.RegisteredBusinessName);
        y = Metadata(pdf, page, y, "Registered Business Address", source.RegisteredBusinessAddress);
        y = Metadata(pdf, page, y, "TIN", source.Report.TaxpayerTin);
        y = Metadata(pdf, page, y, "Site / Parking Location", source.SiteDisplayName);
        y = Metadata(pdf, page, y, "Reporting Month", $"{source.Report.CalendarYear:D4}-{source.Report.CalendarMonth:D2}");
        y = Metadata(pdf, page, y, "Coverage", source.Report.CoverageMessage);
        y = Metadata(pdf, page, y, "Report As Of (PHT)", source.ReportAsOf.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        return y - 5;
    }

    private static double DrawContinuationHeader(A4FiscalReportPdf pdf, int page, MonthlyESalesPdfReport source, MonthlyESalesMachineSection machine)
    {
        var y = A4FiscalReportPdf.PageHeight - 42;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "MONTHLY eSALES - CONTINUED", 11, true);
        pdf.RightText(page, A4FiscalReportPdf.PageWidth - A4FiscalReportPdf.Margin, y,
            $"{source.Report.CalendarYear:D4}-{source.Report.CalendarMonth:D2} | MIN {machine.MachineIdentificationNumber}", 8);
        return y - 18;
    }

    private static double DrawMachineBand(A4FiscalReportPdf pdf, int page, MonthlyESalesMachineSection machine, double y)
    {
        pdf.FillRect(page, A4FiscalReportPdf.Margin, y - 7, A4FiscalReportPdf.ContentWidth, 29, 0.92);
        pdf.Text(page, A4FiscalReportPdf.Margin + 7, y + 8, "MACHINE", 8, true);
        pdf.Text(page, A4FiscalReportPdf.Margin + 70, y + 8, $"POS Serial Number: {machine.PosSerialNumber}", 7.5);
        pdf.Text(page, A4FiscalReportPdf.Margin + 250, y + 8, $"MIN: {machine.MachineIdentificationNumber}", 7.5);
        pdf.Text(page, A4FiscalReportPdf.Margin + 70, y - 3, $"PTU Number: {machine.PtuNumber}", 7.5);
        return y - 24;
    }

    private static readonly double[] Columns = [44, 57, 57, 39, 62, 62, 51, 64, 64];
    private static readonly string[] Headings = ["Date", "Beginning SI", "Ending SI", "Transactions", "Gross Sales", "VATable Sales", "VAT", "VAT Exempt Sales", "Zero-Rated Sales"];

    private static double DrawTableHeader(A4FiscalReportPdf pdf, int page, double y)
    {
        const double height = 26;
        pdf.FillRect(page, A4FiscalReportPdf.Margin, y - height, Columns.Sum(), height, 0.86);
        var x = A4FiscalReportPdf.Margin;
        for (var index = 0; index < Columns.Length; index++)
        {
            pdf.StrokeRect(page, x, y - height, Columns[index], height, 0.35);
            var lines = A4FiscalReportPdf.Wrap(Headings[index], Math.Max(5, (int)(Columns[index] / 3.2)));
            for (var line = 0; line < lines.Count; line++)
                pdf.CenterText(page, x + (Columns[index] / 2), y - 10 - (line * 8), lines[line], 5.7, true);
            x += Columns[index];
        }
        return y - height;
    }

    private static double DrawDailyRow(A4FiscalReportPdf pdf, int page, MonthlyESalesDailyRow row, double y)
    {
        const double height = 15;
        var values = new[]
        {
            row.BusinessDayDate.ToString("MM-dd", CultureInfo.InvariantCulture), row.BeginningSalesInvoiceReference ?? string.Empty,
            row.EndingSalesInvoiceReference ?? string.Empty, row.TransactionCount.ToString(CultureInfo.InvariantCulture),
            Money(row.GrossSalesAmountMinorUnits), Money(row.VatableSalesAmountMinorUnits), Money(row.VatAmountMinorUnits),
            Money(row.VatExemptSalesAmountMinorUnits), Money(row.ZeroRatedSalesAmountMinorUnits)
        };
        var x = A4FiscalReportPdf.Margin;
        for (var index = 0; index < Columns.Length; index++)
        {
            pdf.StrokeRect(page, x, y - height, Columns[index], height, 0.25);
            if (index is 3 or >= 4) pdf.RightText(page, x + Columns[index] - 3, y - 10, values[index], 5.8);
            else pdf.Text(page, x + 3, y - 10, values[index], 5.8);
            x += Columns[index];
        }
        return y - height;
    }

    private static void DrawTotalRow(A4FiscalReportPdf pdf, int page, MonthlyESalesRow row, double y)
    {
        const double height = 18;
        pdf.FillRect(page, A4FiscalReportPdf.Margin, y - height, Columns.Sum(), height, 0.88);
        var values = new[] { "TOTAL", string.Empty, row.EndingSalesInvoiceReference ?? string.Empty, row.TransactionCount.ToString(CultureInfo.InvariantCulture), Money(row.MonthlySalesAmountMinorUnits), Money(row.VatableSalesAmountMinorUnits), Money(row.VatAmountMinorUnits), Money(row.VatExemptSalesAmountMinorUnits), Money(row.ZeroRatedSalesAmountMinorUnits) };
        var x = A4FiscalReportPdf.Margin;
        for (var index = 0; index < Columns.Length; index++)
        {
            pdf.StrokeRect(page, x, y - height, Columns[index], height, 0.4);
            if (index is 3 or >= 4) pdf.RightText(page, x + Columns[index] - 3, y - 12, values[index], 6, true);
            else pdf.Text(page, x + 3, y - 12, values[index], 6, true);
            x += Columns[index];
        }
    }

    private static double Metadata(A4FiscalReportPdf pdf, int page, double y, string label, string? value)
    {
        pdf.Text(page, A4FiscalReportPdf.Margin, y, label, 7, true);
        var lines = A4FiscalReportPdf.Wrap(value, 82);
        if (lines.Count == 0) return y - 11;
        for (var index = 0; index < lines.Count; index++)
            pdf.Text(page, A4FiscalReportPdf.Margin + 120, y - (index * 10), lines[index], 7);
        return y - Math.Max(11, lines.Count * 10);
    }

    private static string Money(long minorUnits) => $"PHP {(minorUnits / 100m).ToString("N2", CultureInfo.InvariantCulture)}";
}
