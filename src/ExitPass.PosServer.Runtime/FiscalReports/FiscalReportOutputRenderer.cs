using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class FiscalReportOutputRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public FiscalReportOutputArtifact CreatePresentationJson(FiscalReportPresentationModel presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        var formatKey = "presentation-json";
        var identity = ComputeOutputIdentity(presentation, formatKey);
        var content = Serialize(new FiscalReportPresentationEnvelope(
            presentation.PresentationContractVersion,
            identity,
            presentation));
        return Artifact(
            FiscalReportOutputFormat.PresentationJson,
            presentation.PresentationContractVersion,
            identity,
            "application/json; charset=utf-8",
            FileName(presentation, "presentation.json"),
            "inline",
            content);
    }

    public FiscalReportOutputArtifact CreateExport(
        FiscalReportPresentationModel presentation,
        FiscalReportOutputFormat format,
        FiscalReportPrintWidthProfile widthProfile = FiscalReportPrintWidthProfile.Standard)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        return format switch
        {
            FiscalReportOutputFormat.Json => CreateJsonExport(presentation),
            FiscalReportOutputFormat.Csv => CreateCsvExport(presentation),
            FiscalReportOutputFormat.Text => CreateTextExport(presentation, widthProfile),
            _ => throw new ArgumentOutOfRangeException(nameof(format), "Presentation JSON uses the dedicated presentation route.")
        };
    }

    public static bool TryParseFormat(string? value, out FiscalReportOutputFormat format)
    {
        format = value?.Trim().ToLowerInvariant() switch
        {
            "json" => FiscalReportOutputFormat.Json,
            "csv" => FiscalReportOutputFormat.Csv,
            "text" or "txt" => FiscalReportOutputFormat.Text,
            _ => FiscalReportOutputFormat.PresentationJson
        };
        return value?.Trim().ToLowerInvariant() is "json" or "csv" or "text" or "txt";
    }

    public static bool TryParseWidthProfile(string? value, out FiscalReportPrintWidthProfile profile)
    {
        profile = value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "standard" or "80mm" => FiscalReportPrintWidthProfile.Standard,
            "narrow" or "58mm" or "57mm" => FiscalReportPrintWidthProfile.Narrow,
            "office" or "plain" => FiscalReportPrintWidthProfile.Office,
            _ => FiscalReportPrintWidthProfile.Standard
        };
        return string.IsNullOrWhiteSpace(value) || value.Trim().ToLowerInvariant() is
            "standard" or "80mm" or "narrow" or "58mm" or "57mm" or "office" or "plain";
    }

    public static string ComputeSnapshotIdentity(FiscalReportPresentationModel presentation)
    {
        var canonical = Serialize(presentation with { SnapshotIdentity = string.Empty });
        return Hash(FiscalReportPresentationContract.OutputIdentityVersion, canonical);
    }

    public static string FormatAmount(long amountMinorUnits, string currencyCode)
    {
        var amount = amountMinorUnits / 100m;
        return string.Format(CultureInfo.InvariantCulture, "{0} {1:0.00}", currencyCode, amount);
    }

    public static int Width(FiscalReportPrintWidthProfile profile) => profile switch
    {
        FiscalReportPrintWidthProfile.Narrow => 32,
        FiscalReportPrintWidthProfile.Standard => 48,
        FiscalReportPrintWidthProfile.Office => 80,
        _ => throw new ArgumentOutOfRangeException(nameof(profile))
    };

    private static FiscalReportOutputArtifact CreateJsonExport(FiscalReportPresentationModel presentation)
    {
        var identity = ComputeOutputIdentity(presentation, "json");
        var content = Serialize(new FiscalReportJsonExportEnvelope(
            FiscalReportPresentationContract.JsonExportVersion,
            identity,
            presentation));
        return Artifact(
            FiscalReportOutputFormat.Json,
            FiscalReportPresentationContract.JsonExportVersion,
            identity,
            "application/json; charset=utf-8",
            FileName(presentation, "json"),
            "attachment",
            content);
    }

    private static FiscalReportOutputArtifact CreateCsvExport(FiscalReportPresentationModel presentation)
    {
        var identity = ComputeOutputIdentity(presentation, "csv");
        var content = Encoding.UTF8.GetBytes(BuildCsv(presentation, identity));
        return Artifact(
            FiscalReportOutputFormat.Csv,
            FiscalReportPresentationContract.CsvExportVersion,
            identity,
            "text/csv; charset=utf-8",
            FileName(presentation, "csv"),
            "attachment",
            content);
    }

    private static FiscalReportOutputArtifact CreateTextExport(
        FiscalReportPresentationModel presentation,
        FiscalReportPrintWidthProfile widthProfile)
    {
        var profile = widthProfile.ToString().ToLowerInvariant();
        var identity = ComputeOutputIdentity(presentation, $"text:{profile}");
        var content = Encoding.UTF8.GetBytes(BuildText(presentation, widthProfile, identity));
        return Artifact(
            FiscalReportOutputFormat.Text,
            FiscalReportPresentationContract.PrintVersion,
            identity,
            "text/plain; charset=utf-8",
            FileName(presentation, $"{profile}.txt"),
            "inline",
            content);
    }

    private static FiscalReportOutputArtifact Artifact(
        FiscalReportOutputFormat format,
        string contractVersion,
        string identity,
        string contentType,
        string fileName,
        string disposition,
        byte[] content) =>
        new(
            format,
            contractVersion,
            identity,
            $"\"{identity.Replace(':', '-')}\"",
            contentType,
            fileName,
            $"{disposition}; filename=\"{fileName}\"",
            content);

    private static string ComputeOutputIdentity(FiscalReportPresentationModel presentation, string formatKey)
    {
        var material = string.Join(
            '\n',
            FiscalReportPresentationContract.OutputIdentityVersion,
            presentation.ReportReference,
            presentation.ReportKind,
            presentation.SourceReportContractVersion,
            presentation.PresentationContractVersion,
            presentation.SnapshotIdentity,
            formatKey);
        return Hash(FiscalReportPresentationContract.OutputIdentityVersion, Encoding.UTF8.GetBytes(material));
    }

    private static string Hash(string version, byte[] bytes) =>
        $"{version}:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    private static string BuildCsv(FiscalReportPresentationModel presentation, string outputIdentity)
    {
        var rows = new List<string[]>();
        void Add(string section, string key, string? classification = null, long? count = null, long? amount = null, string? currency = null, string? value = null) =>
            rows.Add([section, key, classification ?? string.Empty, count?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, amount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, currency ?? string.Empty, value ?? string.Empty]);

        Add("metadata", "export_contract_version", value: FiscalReportPresentationContract.CsvExportVersion);
        Add("metadata", "output_identity", value: outputIdentity);
        Add("metadata", "report_reference", value: presentation.ReportReference);
        Add("metadata", "report_kind", value: presentation.ReportKind);
        Add("metadata", "report_status", value: presentation.ReportStatus);
        Add("metadata", "period_status", value: presentation.PeriodStatus);
        Add("metadata", "site_pos_server_id", value: presentation.SitePosServerId.ToString("D"));
        Add("metadata", "fiscal_identity_id", value: presentation.FiscalIdentityId.ToString("D"));
        Add("metadata", "fiscal_reporting_period_id", value: presentation.FiscalReportingPeriodId.ToString("D"));
        Add("metadata", "period_sequence", count: presentation.PeriodSequence);
        Add("metadata", "period_start_at", value: Timestamp(presentation.PeriodStartAt));
        Add("metadata", "period_end_at", value: Timestamp(presentation.PeriodEndAt));
        Add("metadata", "generated_at", value: Timestamp(presentation.GeneratedAt));
        Add("metadata", "committed_at", value: Timestamp(presentation.CommittedAt));
        Add("metadata", "closed_at", value: presentation.ClosedAt is null ? "not_applicable" : Timestamp(presentation.ClosedAt.Value));
        Add("count", "qualifying_documents", count: presentation.QualifyingDocumentCount);
        AddAmountRows(Add, presentation.Amounts, presentation.CurrencyCode);

        foreach (var tender in presentation.Tenders)
            Add("tender", "tender_total", tender.Classification, tender.TransactionCount, tender.AmountMinorUnits, tender.CurrencyCode);
        foreach (var discount in presentation.Discounts)
        {
            Add("discount", "discount_amount", discount.Classification, discount.QualifyingDocumentCount, discount.DiscountAmountMinorUnits, discount.CurrencyCode);
            Add("discount", "vat_exemption_amount", discount.Classification, discount.QualifyingDocumentCount, discount.VatExemptionAmountMinorUnits, discount.CurrencyCode);
        }
        foreach (var range in presentation.FiscalNumberRanges)
        {
            Add("range", "first_sequence", range.FiscalSeries, range.FirstSequenceValue, value: range.FirstFiscalNumber);
            Add("range", "last_sequence", range.FiscalSeries, range.LastSequenceValue, value: range.LastFiscalNumber);
            Add("range", "qualifying_documents", range.FiscalSeries, range.QualifyingDocumentCount, currency: range.CurrencyCode);
            foreach (var gap in range.Gaps)
                Add("gap", "sequence", gap.Classification, gap.SequenceValue, value: range.FiscalSeries);
        }
        if (presentation.Counters is { } counters)
        {
            Add("counter", "previous_reset", count: counters.PreviousResetCounterValue);
            Add("counter", "resulting_reset", count: counters.ResultingResetCounterValue);
            Add("counter", "previous_z", count: counters.PreviousZCounterValue);
            Add("counter", "resulting_z", count: counters.ResultingZCounterValue);
            Add("counter", "previous_gta", amount: counters.PreviousGrandTotalAmountMinorUnits, currency: presentation.CurrencyCode);
            Add("counter", "current_gta", amount: counters.CurrentPeriodGrandTotalAmountMinorUnits, currency: presentation.CurrencyCode);
            Add("counter", "resulting_gta", amount: counters.ResultingGrandTotalAmountMinorUnits, currency: presentation.CurrencyCode);
        }
        Add("reconciliation", "status", value: presentation.Reconciliation.Status);
        Add("reconciliation", "sequence_gap_count", count: presentation.Reconciliation.SequenceGapCount);

        var builder = new StringBuilder("section,key,classification,count,amount_minor_units,currency,value\n");
        foreach (var row in rows)
            builder.AppendJoin(',', row.Select(Csv)).Append('\n');
        return builder.ToString();
    }

    private static string BuildText(
        FiscalReportPresentationModel presentation,
        FiscalReportPrintWidthProfile profile,
        string outputIdentity)
    {
        var width = Width(profile);
        var lines = new List<string>();
        AddCentered(lines, presentation.Title, width);
        AddCentered(lines, presentation.Finality, width);
        lines.Add(new string('=', width));
        AddSection(lines, "REPORT IDENTITY", width);
        AddValue(lines, "Reference", presentation.ReportReference, width);
        AddValue(lines, "Kind", presentation.ReportKind, width);
        AddValue(lines, "Status", presentation.ReportStatus, width);
        AddValue(lines, "Site POS", presentation.SitePosServerId.ToString("D"), width);
        AddValue(lines, "Site", presentation.SiteIdentityPosture.ToUpperInvariant(), width);
        AddValue(lines, "Fiscal identity", presentation.FiscalIdentityId.ToString("D"), width);
        AddValue(lines, "Period", presentation.FiscalReportingPeriodId.ToString("D"), width);
        AddValue(lines, "Period sequence", presentation.PeriodSequence.ToString(CultureInfo.InvariantCulture), width);
        AddValue(lines, "Business date", presentation.BusinessDayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), width);
        AddValue(lines, "Period start", Timestamp(presentation.PeriodStartAt), width);
        AddValue(lines, "Period end", Timestamp(presentation.PeriodEndAt), width);
        AddValue(lines, "Generated", Timestamp(presentation.GeneratedAt), width);
        AddValue(lines, "Committed", Timestamp(presentation.CommittedAt), width);
        AddValue(lines, "Closed", presentation.ClosedAt is null ? "NOT APPLICABLE" : Timestamp(presentation.ClosedAt.Value), width);

        if (presentation.Counters is { } counters)
        {
            AddSection(lines, "Z COUNTER AND GTA", width);
            AddValue(lines, "Z number", counters.ResultingZCounterValue.ToString(CultureInfo.InvariantCulture), width);
            AddValue(lines, "Previous Z", counters.PreviousZCounterValue.ToString(CultureInfo.InvariantCulture), width);
            AddValue(lines, "Reset counter", counters.ResultingResetCounterValue.ToString(CultureInfo.InvariantCulture), width);
            AddValue(lines, "Previous GTA", FormatAmount(counters.PreviousGrandTotalAmountMinorUnits, presentation.CurrencyCode), width);
            AddValue(lines, "Period GTA", FormatAmount(counters.CurrentPeriodGrandTotalAmountMinorUnits, presentation.CurrencyCode), width);
            AddValue(lines, "Resulting GTA", FormatAmount(counters.ResultingGrandTotalAmountMinorUnits, presentation.CurrencyCode), width);
        }

        AddSection(lines, "SALES AND TAX TOTALS", width);
        AddValue(lines, "Transactions", presentation.QualifyingDocumentCount.ToString(CultureInfo.InvariantCulture), width);
        AddPrintAmountRows(lines, presentation.Amounts, presentation.CurrencyCode, width);

        AddSection(lines, "TENDER BREAKDOWN", width);
        if (presentation.Tenders.Count == 0) lines.Add("NONE RECORDED");
        foreach (var item in presentation.Tenders)
            AddValue(lines, Label(item.Classification), $"{item.TransactionCount} / {FormatAmount(item.AmountMinorUnits, item.CurrencyCode)}", width);

        AddSection(lines, "DISCOUNT BREAKDOWN", width);
        if (presentation.Discounts.Count == 0) lines.Add("NONE RECORDED");
        foreach (var item in presentation.Discounts)
            AddValue(lines, Label(item.Classification), FormatAmount(item.DiscountAmountMinorUnits, item.CurrencyCode), width);

        AddSection(lines, "FISCAL NUMBER RANGES", width);
        if (presentation.FiscalNumberRanges.Count == 0) lines.Add("NONE RECORDED");
        foreach (var range in presentation.FiscalNumberRanges)
        {
            AddValue(lines, "Series", range.FiscalSeries, width);
            AddValue(lines, "First", $"{range.FirstSequenceValue}: {range.FirstFiscalNumber}", width);
            AddValue(lines, "Last", $"{range.LastSequenceValue}: {range.LastFiscalNumber}", width);
            AddValue(lines, "Gap count", range.Gaps.Count.ToString(CultureInfo.InvariantCulture), width);
            foreach (var gap in range.Gaps)
                AddValue(lines, $"Gap {gap.SequenceValue}", Label(gap.Classification), width);
        }

        AddSection(lines, "RECONCILIATION", width);
        AddValue(lines, "Status", presentation.Reconciliation.Status, width);
        AddValue(lines, "Tender", presentation.Reconciliation.TenderReconciled ? "RECONCILED" : "FAILED", width);
        AddValue(lines, "Discount", presentation.Reconciliation.DiscountReconciled ? "RECONCILED" : "FAILED", width);
        AddValue(lines, "Ranges", presentation.Reconciliation.RangeCountReconciled ? "RECONCILED" : "FAILED", width);
        AddValue(lines, "Sequence gaps", presentation.Reconciliation.SequenceGapCount.ToString(CultureInfo.InvariantCulture), width);

        AddSection(lines, "FISCAL POSTURE", width);
        foreach (var statement in presentation.Statements)
            AddWrapped(lines, statement, width);
        lines.Add(new string('=', width));
        AddValue(lines, "Output identity", outputIdentity, width);
        AddValue(lines, "Print contract", FiscalReportPresentationContract.PrintVersion, width);
        return string.Join('\n', lines) + "\n";
    }

    private static void AddAmountRows(
        Action<string, string, string?, long?, long?, string?, string?> add,
        FiscalXReadingAmounts amounts,
        string currency)
    {
        add("amount", "gross_sales", null, null, amounts.GrossSalesAmountMinorUnits, currency, null);
        add("amount", "net_sales", null, null, amounts.NetSalesAmountMinorUnits, currency, null);
        add("amount", "vatable_sales", null, null, amounts.VatableSalesAmountMinorUnits, currency, null);
        add("amount", "vat", null, null, amounts.VatAmountMinorUnits, currency, null);
        add("amount", "vat_exempt_sales", null, null, amounts.VatExemptSalesAmountMinorUnits, currency, null);
        add("amount", "zero_rated_sales", null, null, amounts.ZeroRatedSalesAmountMinorUnits, currency, null);
        add("amount", "total_discounts", null, null, amounts.DiscountAmountMinorUnits, currency, null);
        add("amount", "senior_citizen_discount", null, null, amounts.SeniorCitizenDiscountAmountMinorUnits, currency, null);
        add("amount", "pwd_discount", null, null, amounts.PwdDiscountAmountMinorUnits, currency, null);
        add("amount", "other_statutory_discount", null, null, amounts.OtherStatutoryDiscountAmountMinorUnits, currency, null);
        add("amount", "vat_exemption", null, null, amounts.VatExemptionAmountMinorUnits, currency, null);
        add("amount", "coupon_discount", null, null, amounts.CouponDiscountAmountMinorUnits, currency, null);
        add("amount", "promotional_discount", null, null, amounts.PromotionalDiscountAmountMinorUnits, currency, null);
        add("amount", "void", null, null, amounts.VoidAmountMinorUnits, currency, null);
        add("amount", "refund", null, null, amounts.RefundAmountMinorUnits, currency, null);
        add("amount", "return", null, null, amounts.ReturnAmountMinorUnits, currency, null);
        add("amount", "adjustment", null, null, amounts.AdjustmentAmountMinorUnits, currency, null);
        add("amount", "service_charge", null, null, amounts.ServiceChargeAmountMinorUnits, currency, null);
    }

    private static void AddPrintAmountRows(List<string> lines, FiscalXReadingAmounts amounts, string currency, int width)
    {
        AddValue(lines, "Gross sales", FormatAmount(amounts.GrossSalesAmountMinorUnits, currency), width);
        AddValue(lines, "Net sales", FormatAmount(amounts.NetSalesAmountMinorUnits, currency), width);
        AddValue(lines, "VATable sales", FormatAmount(amounts.VatableSalesAmountMinorUnits, currency), width);
        AddValue(lines, "VAT", FormatAmount(amounts.VatAmountMinorUnits, currency), width);
        AddValue(lines, "VAT-exempt", FormatAmount(amounts.VatExemptSalesAmountMinorUnits, currency), width);
        AddValue(lines, "Zero-rated", FormatAmount(amounts.ZeroRatedSalesAmountMinorUnits, currency), width);
        AddValue(lines, "Discounts", FormatAmount(amounts.DiscountAmountMinorUnits, currency), width);
        AddValue(lines, "Senior citizen", FormatAmount(amounts.SeniorCitizenDiscountAmountMinorUnits, currency), width);
        AddValue(lines, "PWD", FormatAmount(amounts.PwdDiscountAmountMinorUnits, currency), width);
        AddValue(lines, "Other statutory", FormatAmount(amounts.OtherStatutoryDiscountAmountMinorUnits, currency), width);
        AddValue(lines, "VAT exemption", FormatAmount(amounts.VatExemptionAmountMinorUnits, currency), width);
        AddValue(lines, "Coupon", FormatAmount(amounts.CouponDiscountAmountMinorUnits, currency), width);
        AddValue(lines, "Promotional", FormatAmount(amounts.PromotionalDiscountAmountMinorUnits, currency), width);
        AddValue(lines, "Voids", FormatAmount(amounts.VoidAmountMinorUnits, currency), width);
        AddValue(lines, "Refunds", FormatAmount(amounts.RefundAmountMinorUnits, currency), width);
        AddValue(lines, "Returns", FormatAmount(amounts.ReturnAmountMinorUnits, currency), width);
        AddValue(lines, "Adjustments", FormatAmount(amounts.AdjustmentAmountMinorUnits, currency), width);
        AddValue(lines, "Service charges", FormatAmount(amounts.ServiceChargeAmountMinorUnits, currency), width);
    }

    private static string FileName(FiscalReportPresentationModel presentation, string suffix)
    {
        var kind = presentation.ReportKind == FiscalXReadingContract.ReportKind ? "x-reading" : "z-reading";
        var safeReference = new string(presentation.ReportReference
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? char.ToLowerInvariant(character) : '-')
            .ToArray()).Trim('-');
        return $"{kind}-{safeReference}-{suffix}";
    }

    private static string Csv(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static string Timestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string Label(string value) => value.Replace('_', ' ').ToUpperInvariant();

    private static void AddSection(List<string> lines, string title, int width)
    {
        lines.Add(new string('-', width));
        AddCentered(lines, $"[{title}]", width);
    }

    private static void AddCentered(List<string> lines, string value, int width)
    {
        foreach (var part in Wrap(value, width))
            lines.Add(part.Length >= width ? part : new string(' ', (width - part.Length) / 2) + part);
    }

    private static void AddValue(List<string> lines, string label, string value, int width)
    {
        var prefix = $"{label}: ";
        if (prefix.Length + value.Length <= width)
        {
            lines.Add(prefix + new string(' ', width - prefix.Length - value.Length) + value);
            return;
        }

        AddWrapped(lines, prefix + value, width);
    }

    private static void AddWrapped(List<string> lines, string value, int width)
    {
        foreach (var part in Wrap(value, width)) lines.Add(part);
    }

    private static IEnumerable<string> Wrap(string value, int width)
    {
        var remaining = value.Trim();
        if (remaining.Length == 0)
        {
            yield return string.Empty;
            yield break;
        }

        while (remaining.Length > width)
        {
            var split = remaining.LastIndexOf(' ', width - 1, width);
            if (split <= 0) split = width;
            yield return remaining[..split].TrimEnd();
            remaining = remaining[split..].TrimStart();
        }
        yield return remaining;
    }
}
