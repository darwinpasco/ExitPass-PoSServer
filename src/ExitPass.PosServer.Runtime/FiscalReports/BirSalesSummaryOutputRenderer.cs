using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class BirSalesSummaryOutputRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    public BirSalesSummaryExport Render(
        BirSalesSummaryRecord record,
        BirSalesSummaryExportFormat format,
        BirSalesSummaryPdfContext? pdfContext = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        var bytes = format switch
        {
            BirSalesSummaryExportFormat.Json => RenderJson(record),
            BirSalesSummaryExportFormat.Csv => Encoding.UTF8.GetBytes(RenderCsv(record)),
            BirSalesSummaryExportFormat.Pdf when pdfContext is not null => RenderPdf(record, pdfContext),
            BirSalesSummaryExportFormat.Pdf => throw new ArgumentNullException(nameof(pdfContext)),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var extension = format.ToString().ToLowerInvariant();
        var contentType = format switch
        {
            BirSalesSummaryExportFormat.Json => "application/json; charset=utf-8",
            BirSalesSummaryExportFormat.Csv => "text/csv; charset=utf-8",
            _ => "application/pdf"
        };
        var uat = IsTechnicalUat(record);
        var fileName = format == BirSalesSummaryExportFormat.Pdf
            ? $"daily-bir-sales-summary-{(uat ? "technical-uat-" : string.Empty)}{record.BusinessDayDate:yyyyMMdd}.pdf"
            : $"bir-sales-summary-{record.BirSalesSummaryReportId:N}.{extension}";
        return new(
            format,
            contentType,
            fileName,
            bytes,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    private static byte[] RenderJson(BirSalesSummaryRecord record) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = BirSalesSummaryContract.ExportVersion,
            summary = record
        }, JsonOptions);

    private static string RenderCsv(BirSalesSummaryRecord record)
    {
        var rows = new List<string>
        {
            "section,key,classification,count,amount_minor_units,currency,value",
            Row("metadata", "schema_version", value: BirSalesSummaryContract.ExportVersion),
            Row("metadata", "summary_id", value: record.BirSalesSummaryReportId.ToString("D")),
            Row("metadata", "governing_z_reference", value: record.GoverningZReadingReference),
            Row("metadata", "site_pos_server_id", value: record.SitePosServerId.ToString("D")),
            Row("metadata", "fiscal_identity_id", value: record.FiscalIdentityId.ToString("D")),
            Row("metadata", "fiscal_reporting_period_id", value: record.FiscalReportingPeriodId.ToString("D")),
            Row("metadata", "business_day_date", value: record.BusinessDayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Row("metadata", "reporting_period_start_date", value: record.ReportingPeriodStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Row("metadata", "reporting_period_end_date", value: record.ReportingPeriodEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Row("metadata", "beginning_si_ref", value: record.BeginningSalesInvoiceReference),
            Row("metadata", "ending_si_ref", value: record.EndingSalesInvoiceReference),
            Row("metadata", "transaction_count", count: record.TransactionCount),
            Row("counter", "reset_counter", count: record.CounterSnapshot.ResultingResetCounterValue),
            Row("counter", "z_counter", count: record.CounterSnapshot.ResultingZCounterValue),
            Row("amount", "previous_grand_total", amount: record.CounterSnapshot.PreviousGrandTotalAmountMinorUnits, currency: record.CurrencyCode),
            Row("amount", "current_grand_total", amount: record.CounterSnapshot.CurrentPeriodGrandTotalAmountMinorUnits, currency: record.CurrencyCode),
            Row("amount", "present_grand_total", amount: record.CounterSnapshot.ResultingGrandTotalAmountMinorUnits, currency: record.CurrencyCode)
        };

        AddAmounts(rows, record.Amounts, record.CurrencyCode);
        rows.AddRange(record.Tenders.Select(item =>
            Row("tender", "tender_total", item.Classification, item.TransactionCount, item.AmountMinorUnits, item.CurrencyCode)));
        rows.AddRange(record.Discounts.Select(item =>
            Row("discount", "discount_total", item.Classification, item.QualifyingDocumentCount, item.DiscountAmountMinorUnits, item.CurrencyCode,
                $"vat_exemption_minor_units={item.VatExemptionAmountMinorUnits.ToString(CultureInfo.InvariantCulture)}")));
        foreach (var range in record.FiscalNumberRanges)
        {
            rows.Add(Row("fiscal_range", range.FiscalSeries, count: range.QualifyingDocumentCount, currency: range.CurrencyCode,
                value: $"{range.FirstSequenceValue.ToString(CultureInfo.InvariantCulture)}:{range.LastSequenceValue.ToString(CultureInfo.InvariantCulture)}:{range.FirstFiscalNumber}:{range.LastFiscalNumber}"));
            rows.AddRange(range.Gaps.Select(gap =>
                Row("fiscal_gap", range.FiscalSeries, gap.Classification, value: gap.SequenceValue.ToString(CultureInfo.InvariantCulture))));
        }

        return string.Join("\r\n", rows) + "\r\n";
    }

    private static void AddAmounts(ICollection<string> rows, FiscalXReadingAmounts a, string currency)
    {
        rows.Add(Row("amount", "gross_sales", amount: a.GrossSalesAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "net_sales", amount: a.NetSalesAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "vatable_sales", amount: a.VatableSalesAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "vat", amount: a.VatAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "vat_exempt_sales", amount: a.VatExemptSalesAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "zero_rated_sales", amount: a.ZeroRatedSalesAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "discount", amount: a.DiscountAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "senior_citizen_discount", amount: a.SeniorCitizenDiscountAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "pwd_discount", amount: a.PwdDiscountAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "other_statutory_discount", amount: a.OtherStatutoryDiscountAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "vat_exemption", amount: a.VatExemptionAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "coupon_discount", amount: a.CouponDiscountAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "promotional_discount", amount: a.PromotionalDiscountAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "void", amount: a.VoidAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "refund", amount: a.RefundAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "return", amount: a.ReturnAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "adjustment", amount: a.AdjustmentAmountMinorUnits, currency: currency));
        rows.Add(Row("amount", "service_charge", amount: a.ServiceChargeAmountMinorUnits, currency: currency));
    }

    private static string Row(
        string section,
        string key,
        string? classification = null,
        long? count = null,
        long? amount = null,
        string? currency = null,
        string? value = null) =>
        string.Join(',', new[]
        {
            section,
            key,
            classification ?? string.Empty,
            count?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            amount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            currency ?? string.Empty,
            value ?? string.Empty
        }.Select(Escape));

    private static string Escape(string value) =>
        value.ContainsAny(',', '"', '\r', '\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static byte[] RenderPdf(BirSalesSummaryRecord record, BirSalesSummaryPdfContext context)
    {
        var pdf = new A4FiscalReportPdf();
        var page = pdf.AddPage();
        var y = A4FiscalReportPdf.PageHeight - 42;
        var uat = IsTechnicalUat(record);
        pdf.CenterText(page, A4FiscalReportPdf.PageWidth / 2, y, "DAILY BIR SALES SUMMARY", 15, true);
        if (uat)
        {
            y -= 22;
            pdf.FillRect(page, A4FiscalReportPdf.Margin, y - 4, A4FiscalReportPdf.ContentWidth, 17, 0.88);
            pdf.CenterText(page, A4FiscalReportPdf.PageWidth / 2, y, "TECHNICAL UAT - NOT FOR BIR SUBMISSION", 9, true);
        }
        y -= 26;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "TAXPAYER IDENTITY", 9, true);
        y -= 14;
        y = Metadata(pdf, page, y, "Registered Business Name", context.RegisteredBusinessName);
        y = Metadata(pdf, page, y, "Registered Business Address", context.RegisteredBusinessAddress);
        y = Metadata(pdf, page, y, "TIN", context.TaxpayerTin);
        y = Metadata(pdf, page, y, "Site / Parking Location", context.ParkingLocationDisplay ?? context.SiteDisplayName);
        y = MetadataPair(pdf, page, y, "Fiscal Business Date", record.BusinessDayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "Report Status", record.ReportStatus);
        y = MetadataPair(pdf, page, y, "Reporting Period Start", record.ReportingPeriodStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "Reporting Period End", record.ReportingPeriodEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        y = MetadataPair(pdf, page, y, "POS Serial Number", record.HeaderProfile.PosSerialNumber, "MIN", record.HeaderProfile.MachineIdentificationNumber);
        y = MetadataPair(pdf, page, y, "PTU Number", record.HeaderProfile.PtuNumber, "BIR Accreditation Number", record.HeaderProfile.BirAccreditationNumber);
        y = MetadataPair(pdf, page, y, "Summary ID", record.BirSalesSummaryReportId.ToString("D"), "Governing Z Reference", record.GoverningZReadingReference);
        y = Metadata(pdf, page, y, "Report As Of (PHT)", record.CommittedAt.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        y -= 5;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "INVOICE RANGE", 9, true);
        y -= 14;
        y = MetadataPair(pdf, page, y, "Beginning SI", record.BeginningSalesInvoiceReference, "Ending SI", record.EndingSalesInvoiceReference);
        y = MetadataPair(pdf, page, y, "Transaction Count", record.TransactionCount.ToString(CultureInfo.InvariantCulture), "Sequence Gap Count", record.FiscalNumberRanges.Sum(range => range.Gaps.Count).ToString(CultureInfo.InvariantCulture));

        y -= 5;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "SALES, DISCOUNTS, AND ADJUSTMENTS", 9, true);
        y -= 14;
        var amounts = new (string Label, long Value)[]
        {
            ("Gross Sales", record.Amounts.GrossSalesAmountMinorUnits), ("Net Sales", record.Amounts.NetSalesAmountMinorUnits),
            ("VATable Sales", record.Amounts.VatableSalesAmountMinorUnits), ("VAT", record.Amounts.VatAmountMinorUnits),
            ("VAT Exempt Sales", record.Amounts.VatExemptSalesAmountMinorUnits), ("Zero-Rated Sales", record.Amounts.ZeroRatedSalesAmountMinorUnits),
            ("Total Discounts", record.Amounts.DiscountAmountMinorUnits), ("Senior Citizen Discount", record.Amounts.SeniorCitizenDiscountAmountMinorUnits),
            ("PWD Discount", record.Amounts.PwdDiscountAmountMinorUnits), ("Other Statutory Discount", record.Amounts.OtherStatutoryDiscountAmountMinorUnits),
            ("VAT Exemption / Privilege Adjustment", record.Amounts.VatExemptionAmountMinorUnits), ("Coupon Discount", record.Amounts.CouponDiscountAmountMinorUnits),
            ("Promotional Discount", record.Amounts.PromotionalDiscountAmountMinorUnits), ("Void Amount", record.Amounts.VoidAmountMinorUnits),
            ("Refund Amount", record.Amounts.RefundAmountMinorUnits), ("Return Amount", record.Amounts.ReturnAmountMinorUnits),
            ("Adjustment Amount", record.Amounts.AdjustmentAmountMinorUnits), ("Service Charge", record.Amounts.ServiceChargeAmountMinorUnits)
        };
        for (var index = 0; index < amounts.Length; index += 2)
        {
            var right = index + 1 < amounts.Length ? amounts[index + 1] : default;
            y = AmountPair(pdf, page, y, amounts[index], right);
        }

        y -= 4;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "TENDER BREAKDOWN", 9, true);
        y -= 14;
        y = TenderHeader(pdf, page, y);
        foreach (var tender in record.Tenders.OrderBy(item => item.Classification, StringComparer.Ordinal))
            y = TenderRow(pdf, page, y, tender);
        if (record.Tenders.Count == 0)
        {
            pdf.Text(page, A4FiscalReportPdf.Margin + 4, y - 10, "No governed tender breakdown rows.", 7);
            y -= 15;
        }

        y -= 12;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "COUNTERS AND GRAND TOTAL", 9, true);
        y -= 14;
        y = MetadataPair(pdf, page, y, "Reset Counter", record.CounterSnapshot.ResultingResetCounterValue.ToString(CultureInfo.InvariantCulture), "Z Counter", record.CounterSnapshot.ResultingZCounterValue.ToString(CultureInfo.InvariantCulture));
        y = MetadataPair(pdf, page, y, "Previous Grand Total", Money(record.CounterSnapshot.PreviousGrandTotalAmountMinorUnits), "Current Period GTA", Money(record.CounterSnapshot.CurrentPeriodGrandTotalAmountMinorUnits));
        y = Metadata(pdf, page, y, "Present Grand Total", Money(record.CounterSnapshot.ResultingGrandTotalAmountMinorUnits));

        y -= 4;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, "RECONCILIATION", 9, true);
        y -= 14;
        y = MetadataPair(pdf, page, y, "Period Status", "CLOSED", "Source Reconciliation", "COMMITTED / EXACT");
        MetadataPair(pdf, page, y, "Report Status", record.ReportStatus, "Immutable", record.Immutable ? "YES" : "NO");
        pdf.AddFooters("Daily BIR Sales Summary", context.SoftwareVersion, uat);
        return pdf.Build();
    }

    private static double Metadata(A4FiscalReportPdf pdf, int page, double y, string label, string? value)
    {
        pdf.Text(page, A4FiscalReportPdf.Margin, y, label, 7, true);
        var lines = A4FiscalReportPdf.Wrap(value, 80);
        if (lines.Count == 0) return y - 11;
        for (var index = 0; index < lines.Count; index++)
            pdf.Text(page, A4FiscalReportPdf.Margin + 126, y - (index * 9), lines[index], 7);
        return y - Math.Max(11, lines.Count * 9);
    }

    private static double MetadataPair(A4FiscalReportPdf pdf, int page, double y, string leftLabel, string? leftValue, string rightLabel, string? rightValue)
    {
        const double second = 300;
        pdf.Text(page, A4FiscalReportPdf.Margin, y, leftLabel, 7, true);
        pdf.Text(page, second, y, rightLabel, 7, true);
        var leftLines = A4FiscalReportPdf.Wrap(leftValue, 37);
        var rightLines = A4FiscalReportPdf.Wrap(rightValue, 37);
        for (var index = 0; index < leftLines.Count; index++)
            pdf.Text(page, A4FiscalReportPdf.Margin + 104, y - (index * 9), leftLines[index], 7);
        for (var index = 0; index < rightLines.Count; index++)
            pdf.Text(page, second + 104, y - (index * 9), rightLines[index], 7);
        return y - Math.Max(11, Math.Max(leftLines.Count, rightLines.Count) * 9);
    }

    private static double AmountPair(A4FiscalReportPdf pdf, int page, double y, (string Label, long Value) left, (string Label, long Value) right)
    {
        pdf.Text(page, A4FiscalReportPdf.Margin, y, left.Label, 6.8);
        pdf.RightText(page, 285, y, Money(left.Value), 6.8, true);
        if (!string.IsNullOrEmpty(right.Label))
        {
            pdf.Text(page, 310, y, right.Label, 6.8);
            pdf.RightText(page, A4FiscalReportPdf.PageWidth - A4FiscalReportPdf.Margin, y, Money(right.Value), 6.8, true);
        }
        return y - 11;
    }

    private static double TenderHeader(A4FiscalReportPdf pdf, int page, double y)
    {
        pdf.FillRect(page, A4FiscalReportPdf.Margin, y - 15, A4FiscalReportPdf.ContentWidth, 15, 0.88);
        pdf.Text(page, A4FiscalReportPdf.Margin + 4, y - 10, "Tender Type", 6.8, true);
        pdf.RightText(page, 390, y - 10, "Transaction Count", 6.8, true);
        pdf.RightText(page, A4FiscalReportPdf.PageWidth - A4FiscalReportPdf.Margin - 4, y - 10, "Amount", 6.8, true);
        return y - 15;
    }

    private static double TenderRow(A4FiscalReportPdf pdf, int page, double y, FiscalXReadingTenderBreakdown tender)
    {
        pdf.StrokeRect(page, A4FiscalReportPdf.Margin, y - 14, A4FiscalReportPdf.ContentWidth, 14, 0.25);
        pdf.Text(page, A4FiscalReportPdf.Margin + 4, y - 10, tender.Classification.ToUpperInvariant().Replace('_', ' '), 6.8);
        pdf.RightText(page, 390, y - 10, tender.TransactionCount.ToString(CultureInfo.InvariantCulture), 6.8);
        pdf.RightText(page, A4FiscalReportPdf.PageWidth - A4FiscalReportPdf.Margin - 4, y - 10, Money(tender.AmountMinorUnits), 6.8);
        return y - 14;
    }

    private static string Money(long value) => $"PHP {(value / 100m).ToString("N2", CultureInfo.InvariantCulture)}";

    private static bool IsTechnicalUat(BirSalesSummaryRecord record) =>
        record.HeaderProfile.PosSerialNumber.Contains("TEST", StringComparison.OrdinalIgnoreCase) ||
        record.HeaderProfile.MachineIdentificationNumber.Contains("TEST", StringComparison.OrdinalIgnoreCase) ||
        record.HeaderProfile.PtuNumber.Contains("TEST", StringComparison.OrdinalIgnoreCase) ||
        record.HeaderProfile.BirAccreditationNumber.Contains("TEST", StringComparison.OrdinalIgnoreCase) ||
        record.HeaderProfile.MachineIdentificationNumber.Length != 9 ||
        !record.HeaderProfile.MachineIdentificationNumber.All(char.IsAsciiDigit);
}

file static class StringExtensions
{
    public static bool ContainsAny(this string value, params char[] characters) =>
        value.IndexOfAny(characters) >= 0;
}
