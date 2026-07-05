using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class FiscalDocumentSemanticRequestHasher
{
    public const string Version = "sha256:v1";
    public const string Status = "calculated";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Hash(FiscalDocumentCreationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.PayableBasis);

        var payload = new SortedDictionary<string, object?>
        {
            ["business_day_date"] = command.BusinessDayDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["central_pms_parking_session_ref"] = Normalize(command.CentralPmsParkingSessionRef),
            ["central_pms_payment_attempt_ref"] = Normalize(command.CentralPmsPaymentAttemptRef),
            ["central_pms_payment_confirmation_ref"] = Normalize(command.CentralPmsPaymentConfirmationRef),
            ["channel_terminal_id"] = command.ChannelTerminalId,
            ["discount_privilege_details"] = (command.DiscountPrivilegeDetails ?? Array.Empty<FiscalDiscountPrivilegeDetailInput>())
                .OrderBy(detail => detail.LineSequence ?? int.MaxValue)
                .ThenBy(detail => detail.DiscountPrivilegeTypeCodeId)
                .Select(NormalizeDiscountPrivilegeDetail)
                .ToArray(),
            ["document_lines"] = (command.DocumentLines ?? Array.Empty<FiscalDocumentLineInput>())
                .OrderBy(line => line.LineSequence)
                .Select(NormalizeLine)
                .ToArray(),
            ["document_links"] = (command.DocumentLinks ?? Array.Empty<FiscalDocumentLinkInput>())
                .OrderBy(link => link.TargetFiscalDocumentId)
                .ThenBy(link => link.LinkTypeCodeId)
                .Select(NormalizeLink)
                .ToArray(),
            ["fiscal_document_status_code_id"] = command.FiscalDocumentStatusCodeId,
            ["fiscal_document_type_code_id"] = command.FiscalDocumentTypeCodeId,
            ["fiscal_document_type_code_key"] = Normalize(command.FiscalDocumentTypeCodeKey),
            ["payable_basis"] = NormalizePayableBasis(command.PayableBasis),
            ["payment_finality_ref"] = Normalize(command.PaymentFinalityRef),
            ["reference_context"] = NormalizeDictionary(command.ReferenceContext),
            ["site_pos_server_id"] = command.SitePosServerId,
            ["site_pos_server_ref"] = Normalize(command.SitePosServerRef),
            ["tax_details"] = (command.TaxDetails ?? Array.Empty<FiscalTaxDetailInput>())
                .OrderBy(tax => tax.LineSequence ?? int.MaxValue)
                .ThenBy(tax => tax.TaxTypeCodeId)
                .ThenBy(tax => tax.TaxClassificationCodeId)
                .Select(NormalizeTaxDetail)
                .ToArray(),
            ["tenders"] = (command.Tenders ?? Array.Empty<FiscalTenderInput>())
                .OrderBy(tender => tender.TenderTypeCodeId)
                .ThenBy(tender => Normalize(tender.PaymentFinalityRef))
                .Select(NormalizeTender)
                .ToArray(),
            ["totals"] = (command.Totals ?? Array.Empty<FiscalTotalInput>())
                .OrderBy(total => total.TotalTypeCodeId)
                .Select(NormalizeTotal)
                .ToArray(),
            ["vendor_ack_ref"] = Normalize(command.VendorAckRef)
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static SortedDictionary<string, object?> NormalizePayableBasis(FiscalizationPayableBasisInput payableBasis) =>
        new()
        {
            ["currency_code"] = NormalizeCurrency(payableBasis.CurrencyCode),
            ["discount_references"] = (payableBasis.DiscountReferences ?? Array.Empty<FiscalDiscountReferenceInput>())
                .OrderBy(reference => Normalize(reference.DiscountValidationRef))
                .Select(NormalizeDiscountReference)
                .ToArray(),
            ["payable_amount_minor_units"] = payableBasis.PayableAmountMinorUnits,
            ["payable_basis_ref"] = Normalize(payableBasis.PayableBasisRef),
            ["reference_context"] = NormalizeDictionary(payableBasis.ReferenceContext),
            ["upstream_finality_ref"] = Normalize(payableBasis.UpstreamFinalityRef)
        };

    private static SortedDictionary<string, object?> NormalizeDiscountReference(FiscalDiscountReferenceInput reference) =>
        new()
        {
            ["applies_statutory_discount_treatment"] = reference.AppliesStatutoryDiscountTreatment,
            ["discount_validation_ref"] = Normalize(reference.DiscountValidationRef),
            ["reference_context"] = NormalizeDictionary(reference.ReferenceContext),
            ["status"] = reference.Status.ToString()
        };

    private static SortedDictionary<string, object?> NormalizeLink(FiscalDocumentLinkInput link) =>
        new()
        {
            ["created_by_ref"] = Normalize(link.CreatedByRef),
            ["link_reason_code_id"] = link.LinkReasonCodeId,
            ["link_reason_text"] = Normalize(link.LinkReasonText),
            ["link_type_code_id"] = link.LinkTypeCodeId,
            ["target_fiscal_document_id"] = link.TargetFiscalDocumentId
        };

    private static SortedDictionary<string, object?> NormalizeLine(FiscalDocumentLineInput line) =>
        new()
        {
            ["currency_code"] = NormalizeCurrency(line.CurrencyCode),
            ["description"] = Normalize(line.Description),
            ["discount_amount_minor_units"] = line.DiscountAmountMinorUnits,
            ["gross_amount_minor_units"] = line.GrossAmountMinorUnits,
            ["line_context"] = NormalizeDictionary(line.LineContext),
            ["line_sequence"] = line.LineSequence,
            ["line_status_code_id"] = line.LineStatusCodeId,
            ["line_type_code_id"] = line.LineTypeCodeId,
            ["net_amount_minor_units"] = line.NetAmountMinorUnits,
            ["quantity"] = line.Quantity,
            ["source_ref"] = Normalize(line.SourceRef),
            ["tax_amount_minor_units"] = line.TaxAmountMinorUnits,
            ["unit_amount_minor_units"] = line.UnitAmountMinorUnits
        };

    private static SortedDictionary<string, object?> NormalizeTender(FiscalTenderInput tender) =>
        new()
        {
            ["amount_minor_units"] = tender.AmountMinorUnits,
            ["central_pms_payment_attempt_ref"] = Normalize(tender.CentralPmsPaymentAttemptRef),
            ["central_pms_payment_confirmation_ref"] = Normalize(tender.CentralPmsPaymentConfirmationRef),
            ["currency_code"] = NormalizeCurrency(tender.CurrencyCode),
            ["payment_finality_ref"] = Normalize(tender.PaymentFinalityRef),
            ["provider_ref"] = Normalize(tender.ProviderRef),
            ["tender_context"] = NormalizeDictionary(tender.TenderContext),
            ["tender_type_code_id"] = tender.TenderTypeCodeId
        };

    private static SortedDictionary<string, object?> NormalizeTaxDetail(FiscalTaxDetailInput taxDetail) =>
        new()
        {
            ["currency_code"] = NormalizeCurrency(taxDetail.CurrencyCode),
            ["line_sequence"] = taxDetail.LineSequence,
            ["tax_amount_minor_units"] = taxDetail.TaxAmountMinorUnits,
            ["tax_classification_code_id"] = taxDetail.TaxClassificationCodeId,
            ["tax_context"] = NormalizeDictionary(taxDetail.TaxContext),
            ["tax_rate"] = taxDetail.TaxRate,
            ["tax_type_code_id"] = taxDetail.TaxTypeCodeId,
            ["taxable_amount_minor_units"] = taxDetail.TaxableAmountMinorUnits
        };

    private static SortedDictionary<string, object?> NormalizeDiscountPrivilegeDetail(
        FiscalDiscountPrivilegeDetailInput detail) =>
        new()
        {
            ["approval_ref"] = Normalize(detail.ApprovalRef),
            ["basis_amount_minor_units"] = detail.BasisAmountMinorUnits,
            ["beneficiary_ref"] = Normalize(detail.BeneficiaryRef),
            ["currency_code"] = NormalizeCurrency(detail.CurrencyCode),
            ["discount_amount_minor_units"] = detail.DiscountAmountMinorUnits,
            ["discount_privilege_context"] = NormalizeDictionary(detail.DiscountPrivilegeContext),
            ["discount_privilege_type_code_id"] = detail.DiscountPrivilegeTypeCodeId,
            ["evidence_ref"] = Normalize(detail.EvidenceRef),
            ["line_sequence"] = detail.LineSequence,
            ["vat_privilege_amount_minor_units"] = detail.VatPrivilegeAmountMinorUnits
        };

    private static SortedDictionary<string, object?> NormalizeTotal(FiscalTotalInput total) =>
        new()
        {
            ["amount_minor_units"] = total.AmountMinorUnits,
            ["currency_code"] = NormalizeCurrency(total.CurrencyCode),
            ["total_context"] = NormalizeDictionary(total.TotalContext),
            ["total_type_code_id"] = total.TotalTypeCodeId
        };

    private static SortedDictionary<string, string>? NormalizeDictionary(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        return new SortedDictionary<string, string>(
            values.ToDictionary(
                pair => pair.Key.Trim(),
                pair => pair.Value.Trim(),
                StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCurrency(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
