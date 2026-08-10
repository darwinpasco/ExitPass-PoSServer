using System.Globalization;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

internal static class ElectronicJournalReportFacts
{
    public static IReadOnlyDictionary<string, string?> FromX(FiscalXReadingAggregate aggregate, string reportReference)
    {
        var facts = Amounts(aggregate.Amounts);
        facts["report_reference"] = reportReference;
        facts["qualifying_document_count"] = aggregate.QualifyingDocumentCount.ToString(CultureInfo.InvariantCulture);
        facts["tender_breakdowns"] = string.Join(';', aggregate.Tenders.OrderBy(item => item.Classification, StringComparer.Ordinal)
            .Select(item => $"{item.Classification}:{item.TransactionCount.ToString(CultureInfo.InvariantCulture)}:{item.AmountMinorUnits.ToString(CultureInfo.InvariantCulture)}"));
        facts["discount_breakdowns"] = string.Join(';', aggregate.Discounts.OrderBy(item => item.Classification, StringComparer.Ordinal)
            .Select(item => $"{item.Classification}:{item.QualifyingDocumentCount.ToString(CultureInfo.InvariantCulture)}:{item.DiscountAmountMinorUnits.ToString(CultureInfo.InvariantCulture)}:{item.VatExemptionAmountMinorUnits.ToString(CultureInfo.InvariantCulture)}"));
        facts["fiscal_ranges"] = string.Join(';', aggregate.FiscalNumberRanges.OrderBy(item => item.FiscalSeries, StringComparer.Ordinal)
            .ThenBy(item => item.FirstSequenceValue)
            .Select(item => $"{item.FiscalSeries}:{item.FirstSequenceValue.ToString(CultureInfo.InvariantCulture)}:{item.LastSequenceValue.ToString(CultureInfo.InvariantCulture)}:{item.FirstFiscalNumber}:{item.LastFiscalNumber}:{item.Gaps.Count.ToString(CultureInfo.InvariantCulture)}"));
        return facts;
    }

    public static IReadOnlyDictionary<string, string?> FromZ(
        FiscalXReadingAggregate aggregate,
        string reportReference,
        long previousReset,
        long resultingReset,
        long previousZ,
        long resultingZ,
        long previousGta,
        long currentGta,
        long resultingGta,
        long resultingVersion)
    {
        var facts = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var pair in FromX(aggregate, reportReference)) facts[pair.Key] = pair.Value;
        facts["previous_reset_counter"] = previousReset.ToString(CultureInfo.InvariantCulture);
        facts["resulting_reset_counter"] = resultingReset.ToString(CultureInfo.InvariantCulture);
        facts["previous_z_counter"] = previousZ.ToString(CultureInfo.InvariantCulture);
        facts["resulting_z_counter"] = resultingZ.ToString(CultureInfo.InvariantCulture);
        facts["previous_gta_minor_units"] = previousGta.ToString(CultureInfo.InvariantCulture);
        facts["current_gta_minor_units"] = currentGta.ToString(CultureInfo.InvariantCulture);
        facts["resulting_gta_minor_units"] = resultingGta.ToString(CultureInfo.InvariantCulture);
        facts["resulting_state_version"] = resultingVersion.ToString(CultureInfo.InvariantCulture);
        facts["period_status"] = "closed";
        return facts;
    }

    public static IReadOnlyDictionary<string, string?> FromBir(BirSalesSummaryRecord record)
    {
        var facts = Amounts(record.Amounts);
        facts["bir_sales_summary_id"] = record.BirSalesSummaryReportId.ToString("D");
        facts["governing_z_reference"] = record.GoverningZReadingReference;
        facts["transaction_count"] = record.TransactionCount.ToString(CultureInfo.InvariantCulture);
        facts["beginning_sales_invoice_reference"] = record.BeginningSalesInvoiceReference;
        facts["ending_sales_invoice_reference"] = record.EndingSalesInvoiceReference;
        facts["reset_counter"] = record.CounterSnapshot.ResultingResetCounterValue.ToString(CultureInfo.InvariantCulture);
        facts["z_counter"] = record.CounterSnapshot.ResultingZCounterValue.ToString(CultureInfo.InvariantCulture);
        facts["previous_gta_minor_units"] = record.CounterSnapshot.PreviousGrandTotalAmountMinorUnits.ToString(CultureInfo.InvariantCulture);
        facts["resulting_gta_minor_units"] = record.CounterSnapshot.ResultingGrandTotalAmountMinorUnits.ToString(CultureInfo.InvariantCulture);
        return facts;
    }

    private static SortedDictionary<string, string?> Amounts(FiscalXReadingAmounts a) => new(StringComparer.Ordinal)
    {
        ["gross_sales_minor_units"] = a.GrossSalesAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["net_sales_minor_units"] = a.NetSalesAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["vatable_sales_minor_units"] = a.VatableSalesAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["vat_minor_units"] = a.VatAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["vat_exempt_sales_minor_units"] = a.VatExemptSalesAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["zero_rated_sales_minor_units"] = a.ZeroRatedSalesAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["discount_minor_units"] = a.DiscountAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["senior_discount_minor_units"] = a.SeniorCitizenDiscountAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["pwd_discount_minor_units"] = a.PwdDiscountAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["other_statutory_discount_minor_units"] = a.OtherStatutoryDiscountAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["vat_exemption_minor_units"] = a.VatExemptionAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["coupon_discount_minor_units"] = a.CouponDiscountAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["promotional_discount_minor_units"] = a.PromotionalDiscountAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["void_minor_units"] = a.VoidAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["refund_minor_units"] = a.RefundAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["return_minor_units"] = a.ReturnAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["adjustment_minor_units"] = a.AdjustmentAmountMinorUnits.ToString(CultureInfo.InvariantCulture),
        ["service_charge_minor_units"] = a.ServiceChargeAmountMinorUnits.ToString(CultureInfo.InvariantCulture)
    };
}
