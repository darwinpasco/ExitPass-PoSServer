using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class FiscalReportOutputRenderer
{
    public const double PdfPageWidthPoints = 161.5748;

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
        FiscalReportPrintWidthProfile widthProfile = FiscalReportPrintWidthProfile.Standard,
        FiscalReportReceiptProfile? receiptProfile = null)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        return format switch
        {
            FiscalReportOutputFormat.Json => CreateJsonExport(presentation),
            FiscalReportOutputFormat.Csv => CreateCsvExport(presentation),
            FiscalReportOutputFormat.Pdf => CreatePdfExport(presentation, receiptProfile),
            _ => throw new ArgumentOutOfRangeException(nameof(format), "Presentation JSON uses the dedicated presentation route.")
        };
    }

    public static bool TryParseFormat(string? value, out FiscalReportOutputFormat format)
    {
        format = value?.Trim().ToLowerInvariant() switch
        {
            "json" => FiscalReportOutputFormat.Json,
            "csv" => FiscalReportOutputFormat.Csv,
            "pdf" => FiscalReportOutputFormat.Pdf,
            _ => FiscalReportOutputFormat.PresentationJson
        };
        return value?.Trim().ToLowerInvariant() is "json" or "csv" or "pdf";
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

    private static FiscalReportOutputArtifact CreatePdfExport(
        FiscalReportPresentationModel presentation,
        FiscalReportReceiptProfile? receiptProfile)
    {
        var identity = ComputeOutputIdentity(presentation, "pdf:57mm");
        var receipt = BuildText(presentation, FiscalReportPrintWidthProfile.Narrow, receiptProfile);
        var content = BuildPdf(receipt);
        return Artifact(
            FiscalReportOutputFormat.Pdf,
            FiscalReportPresentationContract.PdfExportVersion,
            identity,
            "application/pdf",
            FileName(presentation, "57mm.pdf"),
            "attachment",
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
        FiscalReportReceiptProfile? receiptProfile)
    {
        var width = Width(profile);
        var lines = new List<string>();
        AddCentered(lines, presentation.Title, width);
        AddCentered(lines,
            presentation.ReportKind == FiscalXReadingContract.ReportKind
                ? "(INTERIM SALES REPORT)"
                : "(END-OF-DAY SALES REPORT)",
            width);
        lines.Add(new string('=', width));

        if (receiptProfile is not null)
        {
            AddSection(lines, "TAXPAYER INFORMATION", width);
            AddOptionalValue(lines, "Registered Name", receiptProfile.RegisteredName, width);
            AddOptionalValue(lines, "Trade Name", receiptProfile.TradeName, width);
            AddOptionalValue(lines, "TIN", receiptProfile.Tin, width);
            AddOptionalValue(lines, "VAT Type", receiptProfile.VatType, width);
            AddOptionalValue(lines, "Branch Code", receiptProfile.BranchCode, width);

            if (HasValue(receiptProfile.PtuNumber) || receiptProfile.PtuIssueDate is not null || receiptProfile.ValidUntil is not null)
            {
                AddSection(lines, "PTU INFORMATION", width);
                AddOptionalValue(lines, "PTU Number", receiptProfile.PtuNumber, width);
                AddOptionalValue(lines, "PTU Issue Date", Date(receiptProfile.PtuIssueDate), width);
                AddOptionalValue(lines, "Valid Until", Date(receiptProfile.ValidUntil), width);
            }

            if (HasValue(receiptProfile.MachineIdentificationNumber) || HasValue(receiptProfile.TerminalId) ||
                HasValue(receiptProfile.SerialNumber) || HasValue(receiptProfile.Model) ||
                HasValue(receiptProfile.SoftwareVersion) || HasValue(receiptProfile.Location))
            {
                AddSection(lines, "MACHINE INFORMATION", width);
                AddOptionalValue(lines, "MIN", receiptProfile.MachineIdentificationNumber, width);
                AddOptionalValue(lines, "Terminal ID", receiptProfile.TerminalId, width);
                AddOptionalValue(lines, "Serial Number", receiptProfile.SerialNumber, width);
                AddOptionalValue(lines, "Model", receiptProfile.Model, width);
                AddOptionalValue(lines, "Software Version", receiptProfile.SoftwareVersion, width);
                AddOptionalValue(lines, "Location", receiptProfile.Location, width);
            }
        }

        AddSection(lines, "REPORT INFORMATION", width);
        AddValue(lines,
            presentation.ReportKind == FiscalXReadingContract.ReportKind ? "X Reading ID" : "Z Reading ID",
            presentation.ReportReference,
            width);
        AddValue(lines, "Report Date Time", PhtTimestamp(presentation.GeneratedAt), width);
        AddValue(lines, "Fiscal Business Date", presentation.BusinessDayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), width);
        AddValue(lines, "Period Start", PhtTimestamp(presentation.PeriodStartAt), width);
        AddValue(lines, "Period End", PhtTimestamp(presentation.PeriodEndAt), width);

        var beginningInvoice = BeginningInvoice(presentation.FiscalNumberRanges);
        var endingInvoice = EndingInvoice(presentation.FiscalNumberRanges);
        if (presentation.ReportKind == FiscalXReadingContract.ReportKind)
        {
            AddSection(lines, "SINCE LAST Z READ", width);
            AddValue(lines, "Last Z Counter", "0000", width);
            AddValue(lines, "Last Z Closed", "0000", width);
            AddOptionalValue(lines, "Beginning Invoice", beginningInvoice, width);
            AddOptionalValue(lines, "Ending Invoice", endingInvoice, width);
            AddValue(lines, "Transactions", presentation.QualifyingDocumentCount.ToString(CultureInfo.InvariantCulture), width);
            AddValue(lines, "Voids", "0", width);
            AddValue(lines, "Cancelled", "0", width);
            AddValue(lines, "Returns", "0", width);
        }
        else if (presentation.Counters is { } counters)
        {
            AddSection(lines, "COUNTERS", width);
            AddValue(lines, "Z Counter", counters.ResultingZCounterValue.ToString(CultureInfo.InvariantCulture), width);
            AddValue(lines, "Last Z Counter", counters.PreviousZCounterValue.ToString(CultureInfo.InvariantCulture), width);
            AddValue(lines, "Reset Counter", counters.ResultingResetCounterValue.ToString(CultureInfo.InvariantCulture), width);
            AddOptionalValue(lines, "Beginning Invoice", beginningInvoice, width);
            AddOptionalValue(lines, "Ending Invoice", endingInvoice, width);
            AddValue(lines, "Transactions", presentation.QualifyingDocumentCount.ToString(CultureInfo.InvariantCulture), width);
            AddValue(lines, "Voids", "0", width);
            AddValue(lines, "Cancelled", "0", width);
            AddValue(lines, "Returns", "0", width);
        }

        AddSection(lines, "SALES SUMMARY", width);
        AddValue(lines, "Currency", presentation.CurrencyCode, width);
        AddValue(lines, "Gross Sales", FormatAmount(presentation.Amounts.GrossSalesAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "Service Charge", FormatAmount(presentation.Amounts.ServiceChargeAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "Discounts", FormatAmount(presentation.Amounts.DiscountAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "Void Amount", FormatAmount(presentation.Amounts.VoidAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "Returns Amount", FormatAmount(presentation.Amounts.ReturnAmountMinorUnits, presentation.CurrencyCode), width);

        AddSection(lines, "VATABLE SALES", width);
        AddValue(lines, "VATable Sales", FormatAmount(presentation.Amounts.VatableSalesAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "VAT Amount", FormatAmount(presentation.Amounts.VatAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "VAT Exempt Sales", FormatAmount(presentation.Amounts.VatExemptSalesAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "Zero Rated Sales", FormatAmount(presentation.Amounts.ZeroRatedSalesAmountMinorUnits, presentation.CurrencyCode), width);

        AddSection(lines, "NET SALES", width);
        AddValue(lines, "Net Sales", FormatAmount(presentation.Amounts.NetSalesAmountMinorUnits, presentation.CurrencyCode), width);
        AddValue(lines, "Total Collected", FormatAmount(presentation.Reconciliation.TenderTotalAmountMinorUnits, presentation.CurrencyCode), width);

        AddSection(lines, "PAYMENT METHODS", width);
        AddPaymentHeader(lines, width);
        foreach (var paymentMethod in PaymentMethods)
        {
            var tender = presentation.Tenders.SingleOrDefault(item => item.Classification == paymentMethod.Classification);
            AddPaymentRow(lines, paymentMethod.Label, tender?.TransactionCount ?? 0, tender?.AmountMinorUnits ?? 0,
                presentation.CurrencyCode, width);
        }
        AddValue(lines, "TOTAL PAYMENTS", FormatAmount(presentation.Reconciliation.TenderTotalAmountMinorUnits, presentation.CurrencyCode), width);

        lines.Add(string.Empty);
        AddValue(lines, "Printed Date", PhtTimestamp(presentation.GeneratedAt), width);
        AddValue(lines, "Operator", "SYSTEM", width);
        lines.Add(new string('=', width));
        AddCentered(lines, "NOTHING FOLLOWS", width);
        return string.Join('\n', lines) + "\n";
    }

    private static readonly (string Classification, string Label)[] PaymentMethods =
    [
        ("cash", "CASH"),
        ("card", "CARD"),
        ("qrph", "QRPH"),
        ("gcash", "GCASH"),
        ("maya", "MAYA")
    ];

    private static string? BeginningInvoice(IReadOnlyList<FiscalReportRangePresentation> ranges) =>
        ranges.OrderBy(range => range.FirstSequenceValue).ThenBy(range => range.FiscalSeries, StringComparer.Ordinal)
            .Select(range => range.FirstFiscalNumber).FirstOrDefault();

    private static string? EndingInvoice(IReadOnlyList<FiscalReportRangePresentation> ranges) =>
        ranges.OrderByDescending(range => range.LastSequenceValue).ThenBy(range => range.FiscalSeries, StringComparer.Ordinal)
            .Select(range => range.LastFiscalNumber).FirstOrDefault();

    private static void AddPaymentHeader(List<string> lines, int width)
    {
        const int typeWidth = 5;
        const int providerWidth = 8;
        const int countWidth = 5;
        var amountWidth = width - typeWidth - providerWidth - countWidth - 3;
        lines.Add("TYPE".PadRight(typeWidth) + " " + "PROVIDER".PadRight(providerWidth) + " " +
                  "COUNT".PadLeft(countWidth) + " " + "AMOUNT".PadLeft(amountWidth));
    }

    private static void AddPaymentRow(List<string> lines, string label, long count, long amount, string currency, int width)
    {
        const int typeWidth = 5;
        const int providerWidth = 8;
        const int countWidth = 5;
        var amountWidth = width - typeWidth - providerWidth - countWidth - 3;
        var countText = count.ToString(CultureInfo.InvariantCulture);
        var amountText = FormatAmount(amount, currency);
        var row = label.PadRight(typeWidth) + " " + string.Empty.PadRight(providerWidth) + " " +
                  countText.PadLeft(countWidth) + " " + amountText.PadLeft(amountWidth);
        if (row.Length <= width)
            lines.Add(row);
        else
            AddValue(lines, label, $"{countText} / {amountText}", width);
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

    private static byte[] BuildPdf(string receipt)
    {
        const double marginPoints = 5.6693;
        const double fontSizePoints = 6.5;
        const double lineHeightPoints = 8.25;
        var lines = receipt.Split('\n', StringSplitOptions.None);
        if (lines.Length > 0 && lines[^1].Length == 0) lines = lines[..^1];
        var pageHeight = Math.Max(36d, (marginPoints * 2d) + (lines.Length * lineHeightPoints));
        var content = new StringBuilder()
            .Append("BT\n/F1 ").Append(PdfNumber(fontSizePoints)).Append(" Tf\n")
            .Append(PdfNumber(lineHeightPoints)).Append(" TL\n")
            .Append("1 0 0 1 ").Append(PdfNumber(marginPoints)).Append(' ')
            .Append(PdfNumber(pageHeight - marginPoints - fontSizePoints)).Append(" Tm\n");
        foreach (var line in lines)
            content.Append('(').Append(PdfText(line)).Append(") Tj\nT*\n");
        content.Append("ET\n");

        var streamContent = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PdfNumber(PdfPageWidthPoints)} {PdfNumber(pageHeight)}] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(streamContent)} >>\nstream\n{streamContent}endstream"
        };

        using var output = new MemoryStream();
        WritePdf(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            WritePdf(output, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = output.Position;
        WritePdf(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            WritePdf(output, $"{offset:0000000000} 00000 n \n");
        WritePdf(output, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private static string PdfText(string value)
    {
        var output = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is '\\' or '(' or ')') output.Append('\\');
            output.Append(character is >= ' ' and <= '~' ? character : '?');
        }
        return output.ToString();
    }

    private static string PdfNumber(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static void WritePdf(Stream output, string value) =>
        output.Write(Encoding.ASCII.GetBytes(value));

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

    private static string PhtTimestamp(DateTimeOffset value) =>
        value.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss 'PHT'", CultureInfo.InvariantCulture);

    private static string Label(string value) => value.Replace('_', ' ').ToUpperInvariant();

    private static void AddSection(List<string> lines, string title, int width)
    {
        lines.Add(new string('-', width));
        AddCentered(lines, title, width);
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

        if (value.Length <= width)
        {
            lines.Add(label + ":");
            lines.Add(new string(' ', width - value.Length) + value);
            return;
        }

        AddWrapped(lines, prefix + value, width);
    }

    private static void AddOptionalValue(List<string> lines, string label, string? value, int width)
    {
        if (HasValue(value)) AddValue(lines, label, value!.Trim(), width);
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string? Date(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

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
