using System.Globalization;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalDocuments;

/// <summary>
/// Builds the one POS Server-owned Sales Invoice fiscal representation used by every output.
/// </summary>
public static class DigitalSalesInvoiceRenderModelFactory
{
    private static readonly string[] FooterPlaceholders =
    [
        "Digital Sales Invoice rendering foundation.",
        "Final statutory footer text and accredited template remain subject to compliance approval."
    ];

    private static readonly string[] ClosingTextLines =
    [
        "THANK YOU FOR CHOOSING OUR SERVICE",
        "===== NOTHING FOLLOWS ====="
    ];

    public static DigitalSalesInvoiceRenderModel Create(FiscalDocumentReadModel document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var context = ReadContext(document.DocumentContextJson);
        var entitlementType = document.AppliedStatutoryFiscalFacts?.EntitlementType;

        var model = new DigitalSalesInvoiceRenderModel(
            document.FiscalDocumentId,
            document.SitePosServerId,
            document.ChannelTerminalId,
            document.FiscalIdentityId,
            document.FiscalDocumentTypeCodeId,
            document.FiscalDocumentTypeCodeKey,
            document.FiscalDocumentStatusCodeId,
            document.FiscalDocumentStatusCodeKey,
            HasCompleteFiscalNumbering(document) ? "assigned" : "not_assigned",
            document.FiscalSequencePolicyId,
            document.FiscalSequenceValue,
            document.FiscalDocumentNumber,
            document.FiscalSeries,
            document.FiscalNumberPrefixText,
            document.FiscalNumberSuffixText,
            document.FiscalNumberAssignedAt,
            document.FiscalNumberAssignedByRef,
            document.BusinessDayDate,
            document.CentralPmsParkingSessionRef,
            document.CentralPmsPaymentAttemptRef,
            document.CentralPmsPaymentConfirmationRef,
            document.PaymentFinalityRef,
            document.VendorAckRef,
            document.SemanticRequestHash,
            document.SemanticRequestHashVersion,
            document.SemanticRequestHashStatus,
            document.VoidStatus,
            document.VoidReasonCode,
            document.VoidedAt,
            document.CreatedAt,
            document.UpdatedAt,
            document.Lines.Select(MapLine).ToArray(),
            document.DiscountPrivilegeDetails.Select(value => MapDiscount(value, entitlementType)).ToArray(),
            document.TaxDetails.Select(MapTaxDetail).ToArray(),
            document.Tenders.Select(MapTender).ToArray(),
            document.Totals.Select(MapTotal).ToArray(),
            CreateFooter(),
            document.SalesInvoiceHeaderSnapshot,
            document.AppliedStatutoryFiscalFacts,
            document.CompletionBasis,
            document.CompletionAuthorityRef,
            document.InvoiceCustomerInformation,
            document.DocumentContextJson);

        return Complete(model, context);
    }

    public static DigitalSalesInvoiceRenderModel Create(
        FiscalDocumentDraft draft,
        IReadOnlyDictionary<Guid, string>? controlledCodeKeys = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        controlledCodeKeys ??= new Dictionary<Guid, string>();
        var assignedAt = draft.FiscalNumberAssignedAt ?? DateTimeOffset.MinValue;
        var entitlementType = draft.AppliedStatutoryFiscalFacts?.EntitlementType;
        var context = NormalizeContext(draft.ReferenceContext);

        var model = new DigitalSalesInvoiceRenderModel(
            draft.FiscalDocumentId,
            draft.SitePosServerId,
            draft.ChannelTerminalId,
            draft.ResolvedFiscalIdentityId,
            draft.FiscalDocumentTypeCodeId,
            draft.FiscalDocumentTypeCodeKey,
            draft.FiscalDocumentStatusCodeId,
            "issued",
            HasCompleteFiscalNumbering(draft) ? "assigned" : "not_assigned",
            draft.ResolvedFiscalSequencePolicyId,
            draft.FiscalSequenceValue,
            draft.FiscalDocumentNumber,
            draft.FiscalSeries,
            draft.FiscalNumberPrefixText,
            draft.FiscalNumberSuffixText,
            draft.FiscalNumberAssignedAt,
            draft.FiscalNumberAssignedByRef,
            draft.BusinessDayDate,
            draft.CentralPmsParkingSessionRef,
            draft.CentralPmsPaymentAttemptRef,
            draft.CentralPmsPaymentConfirmationRef,
            draft.PaymentFinalityRef,
            draft.VendorAckRef,
            null,
            null,
            null,
            "not_voided",
            null,
            null,
            assignedAt,
            assignedAt,
            draft.DocumentLines.Select(MapLine).ToArray(),
            draft.DiscountPrivilegeDetails.Select(value => MapDiscount(value, entitlementType, controlledCodeKeys)).ToArray(),
            draft.TaxDetails.Select(value => MapTaxDetail(value, controlledCodeKeys)).ToArray(),
            draft.Tenders.Select(value => MapTender(value, controlledCodeKeys)).ToArray(),
            draft.Totals.Select(value => MapTotal(value, controlledCodeKeys)).ToArray(),
            CreateFooter(),
            draft.SalesInvoiceHeaderSnapshot,
            draft.AppliedStatutoryFiscalFacts,
            draft.CompletionBasis,
            draft.CompletionAuthorityRef,
            draft.InvoiceCustomerInformation,
            JsonSerializer.Serialize(new { reference_context = draft.ReferenceContext }));

        return Complete(model, context);
    }

    public static DigitalSalesInvoiceRenderModel Complete(DigitalSalesInvoiceRenderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return Complete(model, ReadContext(model.DocumentContextJson));
    }

    private static DigitalSalesInvoiceRenderModel Complete(
        DigitalSalesInvoiceRenderModel model,
        IReadOnlyDictionary<string, string?> context)
    {
        var currency = ResolveCurrency(model);
        var payableTotal = model.Totals
            .Where(value => string.Equals(value.TotalTypeCodeKey, "payable_total", StringComparison.OrdinalIgnoreCase))
            .Select(value => (long?)value.AmountMinorUnits)
            .SingleOrDefault();
        var totalAmount = payableTotal
            ?? model.AppliedStatutoryFiscalFacts?.FinalPayableAmountMinorUnits
            ?? (model.Totals.Count == 1 ? model.Totals[0].AmountMinorUnits : model.Lines.Sum(line => line.NetAmountMinorUnits));

        var fiscalContent = new DigitalSalesInvoiceFiscalContentRenderModel(
            Value(context, "branch_site", "branchSite", "site_name", "siteName"),
            Value(context, "ticket_number", "ticketNumber", "ticket_ref", "ticketRef"),
            Value(context, "plate_number", "plateNumber", "license_plate", "licensePlate"),
            Value(context, "entry_time", "entryTime", "entry_at", "entryAt"),
            Value(context, "payment_time", "paymentTime", "paid_at", "paidAt"),
            Value(context, "duration", "durationText", "parking_duration"),
            Value(context, "payment_method", "paymentMethod")
                ?? model.Tenders.Select(value => value.TenderTypeCodeKey).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            model.Lines.Sum(line => line.GrossAmountMinorUnits),
            model.Discounts.Sum(discount => discount.DiscountAmountMinorUnits),
            SumTaxableAmount(model.TaxDetails, "vatable"),
            model.TaxDetails.Sum(tax => tax.TaxAmountMinorUnits),
            SumTaxableAmount(model.TaxDetails, "vat_exempt"),
            SumTaxableAmount(model.TaxDetails, "zero_rated"),
            totalAmount,
            model.Tenders.Count == 0 ? totalAmount : model.Tenders.Sum(tender => tender.AmountMinorUnits),
            LongValue(context, "tendered_amount_minor_units", "tenderedAmountMinorUnits"),
            LongValue(context, "change_amount_minor_units", "changeAmountMinorUnits"),
            currency,
            true);

        return model with { FiscalContent = fiscalContent };
    }

    private static long SumTaxableAmount(
        IEnumerable<DigitalSalesInvoiceTaxDetailRenderModel> taxDetails,
        string classification) =>
        taxDetails.Where(tax => string.Equals(
                ResolveTaxClassification(tax),
                classification,
                StringComparison.OrdinalIgnoreCase))
            .Sum(tax => tax.TaxableAmountMinorUnits);

    private static string? ResolveTaxClassification(DigitalSalesInvoiceTaxDetailRenderModel tax) =>
        !string.IsNullOrWhiteSpace(tax.TaxClassificationCodeKey)
            ? tax.TaxClassificationCodeKey
            : tax.TaxRate switch
            {
                > 0 => "vatable",
                0 => "zero_rated",
                _ => null
            };

    private static string ResolveCurrency(DigitalSalesInvoiceRenderModel model)
    {
        var currencies = model.Lines.Select(line => line.CurrencyCode)
            .Concat(model.Discounts.Select(discount => discount.CurrencyCode))
            .Concat(model.TaxDetails.Select(tax => tax.CurrencyCode))
            .Concat(model.Tenders.Select(tender => tender.CurrencyCode))
            .Concat(model.Totals.Select(total => total.CurrencyCode))
            .Append(model.AppliedStatutoryFiscalFacts?.Currency)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return currencies.Length switch
        {
            0 => "PHP",
            1 => currencies[0],
            _ => throw new InvalidOperationException("Mixed-currency Sales Invoice fiscal content is prohibited.")
        };
    }

    private static DigitalSalesInvoiceLineRenderModel MapLine(FiscalDocumentLineReadModel line) =>
        new(line.LineSequence, line.LineTypeCodeId, line.LineStatusCodeId, line.Description, line.Quantity,
            line.UnitAmountMinorUnits, line.GrossAmountMinorUnits, line.DiscountAmountMinorUnits,
            line.TaxAmountMinorUnits, line.NetAmountMinorUnits, line.CurrencyCode, line.SourceRef);

    private static DigitalSalesInvoiceLineRenderModel MapLine(FiscalDocumentLineInput line) =>
        new(line.LineSequence, line.LineTypeCodeId, line.LineStatusCodeId, line.Description, line.Quantity,
            line.UnitAmountMinorUnits, line.GrossAmountMinorUnits, line.DiscountAmountMinorUnits,
            line.TaxAmountMinorUnits, line.NetAmountMinorUnits, line.CurrencyCode, line.SourceRef);

    private static DigitalSalesInvoiceDiscountRenderModel MapDiscount(
        FiscalDiscountPrivilegeDetailReadModel discount,
        string? entitlementType) =>
        new(discount.FiscalDocumentLineId, discount.DiscountPrivilegeTypeCodeId, discount.BasisAmountMinorUnits,
            discount.DiscountAmountMinorUnits, discount.VatPrivilegeAmountMinorUnits, discount.CurrencyCode,
            discount.BeneficiaryRef, discount.EvidenceRef, discount.ApprovalRef,
            discount.DiscountPrivilegeTypeCodeKey,
            ResolveDiscountReason(ReadContext(discount.DiscountPrivilegeContextJson), entitlementType,
                discount.DiscountPrivilegeTypeCodeKey));

    private static DigitalSalesInvoiceDiscountRenderModel MapDiscount(
        FiscalDiscountPrivilegeDetailInput discount,
        string? entitlementType,
        IReadOnlyDictionary<Guid, string> controlledCodeKeys)
    {
        controlledCodeKeys.TryGetValue(discount.DiscountPrivilegeTypeCodeId, out var typeCodeKey);
        return new(null, discount.DiscountPrivilegeTypeCodeId, discount.BasisAmountMinorUnits,
            discount.DiscountAmountMinorUnits, discount.VatPrivilegeAmountMinorUnits, discount.CurrencyCode,
            discount.BeneficiaryRef, discount.EvidenceRef, discount.ApprovalRef, typeCodeKey,
            ResolveDiscountReason(NormalizeContext(discount.DiscountPrivilegeContext), entitlementType, typeCodeKey));
    }

    private static DigitalSalesInvoiceTaxDetailRenderModel MapTaxDetail(FiscalTaxDetailReadModel tax) =>
        new(tax.FiscalDocumentLineId, tax.TaxTypeCodeId, tax.TaxClassificationCodeId, tax.TaxRate,
            tax.TaxableAmountMinorUnits, tax.TaxAmountMinorUnits, tax.CurrencyCode, tax.TaxClassificationCodeKey);

    private static DigitalSalesInvoiceTaxDetailRenderModel MapTaxDetail(
        FiscalTaxDetailInput tax,
        IReadOnlyDictionary<Guid, string> controlledCodeKeys)
    {
        controlledCodeKeys.TryGetValue(tax.TaxClassificationCodeId, out var classificationCodeKey);
        return new(null, tax.TaxTypeCodeId, tax.TaxClassificationCodeId, tax.TaxRate,
            tax.TaxableAmountMinorUnits, tax.TaxAmountMinorUnits, tax.CurrencyCode, classificationCodeKey);
    }

    private static DigitalSalesInvoiceTenderRenderModel MapTender(FiscalTenderReadModel tender) =>
        new(tender.TenderTypeCodeId, tender.TenderTypeCodeKey, tender.AmountMinorUnits, tender.CurrencyCode,
            tender.CentralPmsPaymentAttemptRef, tender.CentralPmsPaymentConfirmationRef,
            tender.PaymentFinalityRef, tender.ProviderRef);

    private static DigitalSalesInvoiceTenderRenderModel MapTender(
        FiscalTenderInput tender,
        IReadOnlyDictionary<Guid, string> controlledCodeKeys)
    {
        controlledCodeKeys.TryGetValue(tender.TenderTypeCodeId, out var typeCodeKey);
        return new(tender.TenderTypeCodeId, typeCodeKey, tender.AmountMinorUnits, tender.CurrencyCode,
            tender.CentralPmsPaymentAttemptRef, tender.CentralPmsPaymentConfirmationRef,
            tender.PaymentFinalityRef, tender.ProviderRef);
    }

    private static DigitalSalesInvoiceTotalRenderModel MapTotal(FiscalTotalReadModel total) =>
        new(total.TotalTypeCodeId, total.AmountMinorUnits, total.CurrencyCode, total.TotalTypeCodeKey);

    private static DigitalSalesInvoiceTotalRenderModel MapTotal(
        FiscalTotalInput total,
        IReadOnlyDictionary<Guid, string> controlledCodeKeys)
    {
        controlledCodeKeys.TryGetValue(total.TotalTypeCodeId, out var typeCodeKey);
        return new(total.TotalTypeCodeId, total.AmountMinorUnits, total.CurrencyCode, typeCodeKey);
    }

    private static string ResolveDiscountReason(
        IReadOnlyDictionary<string, string?> context,
        string? entitlementType,
        string? typeCodeKey)
    {
        var explicitReason = Value(context, "discount_reason", "discountReason", "reason");
        if (!string.IsNullOrWhiteSpace(explicitReason)) return explicitReason;

        var entitlement = entitlementType?.Trim().ToUpperInvariant();
        return entitlement switch
        {
            "SENIOR_CITIZEN" or "SENIOR" => "Senior Citizen Discount",
            "PWD" or "PERSON_WITH_DISABILITY" => "PWD Discount",
            { Length: > 0 } => $"{ToDisplayLabel(entitlement)} Discount",
            _ when !string.IsNullOrWhiteSpace(typeCodeKey) => ToDisplayLabel(typeCodeKey),
            _ => "NOT RECORDED"
        };
    }

    private static string ToDisplayLabel(string value) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' ').ToLowerInvariant());

    private static DigitalSalesInvoiceFooterRenderModel CreateFooter() =>
        new("placeholder_only", FooterPlaceholders, ClosingTextLines);

    private static IReadOnlyDictionary<string, string?> NormalizeContext(
        IReadOnlyDictionary<string, string>? context) =>
        context?.ToDictionary(value => value.Key, value => (string?)value.Value, StringComparer.OrdinalIgnoreCase)
        ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string?> ReadContext(string? json)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return values;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return values;
            AddProperties(values, document.RootElement);
            if (document.RootElement.TryGetProperty("reference_context", out var referenceContext) &&
                referenceContext.ValueKind == JsonValueKind.Object)
                AddProperties(values, referenceContext);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("The persisted Sales Invoice context is invalid.");
        }

        return values;
    }

    private static void AddProperties(IDictionary<string, string?> values, JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            values[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                _ => values.TryGetValue(property.Name, out var existing) ? existing : null
            };
        }
    }

    private static string? Value(IReadOnlyDictionary<string, string?> values, params string[] names) =>
        names.Select(name => values.TryGetValue(name, out var value) ? value?.Trim() : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static long? LongValue(IReadOnlyDictionary<string, string?> values, params string[] names) =>
        long.TryParse(Value(values, names), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static bool HasCompleteFiscalNumbering(FiscalDocumentReadModel document) =>
        document.FiscalIdentityId is not null &&
        document.FiscalSequencePolicyId is not null &&
        document.FiscalSequenceValue is not null &&
        !string.IsNullOrWhiteSpace(document.FiscalDocumentNumber) &&
        !string.IsNullOrWhiteSpace(document.FiscalSeries) &&
        document.FiscalNumberAssignedAt is not null &&
        !string.IsNullOrWhiteSpace(document.FiscalNumberAssignedByRef);

    private static bool HasCompleteFiscalNumbering(FiscalDocumentDraft draft) =>
        draft.ResolvedFiscalIdentityId is not null &&
        draft.ResolvedFiscalSequencePolicyId is not null &&
        draft.FiscalSequenceValue is not null &&
        !string.IsNullOrWhiteSpace(draft.FiscalDocumentNumber) &&
        !string.IsNullOrWhiteSpace(draft.FiscalSeries) &&
        draft.FiscalNumberAssignedAt is not null &&
        !string.IsNullOrWhiteSpace(draft.FiscalNumberAssignedByRef);
}
