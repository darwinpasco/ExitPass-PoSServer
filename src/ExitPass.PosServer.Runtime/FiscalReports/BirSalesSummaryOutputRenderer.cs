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

    public BirSalesSummaryExport Render(BirSalesSummaryRecord record, BirSalesSummaryExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(record);
        var bytes = format switch
        {
            BirSalesSummaryExportFormat.Json => RenderJson(record),
            BirSalesSummaryExportFormat.Csv => Encoding.UTF8.GetBytes(RenderCsv(record)),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var extension = format == BirSalesSummaryExportFormat.Json ? "json" : "csv";
        var contentType = format == BirSalesSummaryExportFormat.Json
            ? "application/json; charset=utf-8"
            : "text/csv; charset=utf-8";
        return new(
            format,
            contentType,
            $"bir-sales-summary-{record.BirSalesSummaryReportId:N}.{extension}",
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
}

file static class StringExtensions
{
    public static bool ContainsAny(this string value, params char[] characters) =>
        value.IndexOfAny(characters) >= 0;
}
