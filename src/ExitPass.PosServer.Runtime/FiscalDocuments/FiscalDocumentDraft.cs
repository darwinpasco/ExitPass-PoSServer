namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentDraft(
    Guid FiscalDocumentId,
    string SitePosServerRef,
    string FiscalDocumentTypeCodeKey,
    string PayableBasisRef,
    string UpstreamFinalityRef,
    string CurrencyCode,
    long PayableAmountMinorUnits,
    string? CentralPmsPaymentAttemptRef,
    string? CentralPmsPaymentConfirmationRef,
    string? PaymentFinalityRef,
    string? VendorAckRef,
    IReadOnlyList<FiscalDiscountReferenceInput> DiscountReferences);
