namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentCreationCommand(
    string SitePosServerRef,
    string FiscalDocumentTypeCodeKey,
    FiscalizationPayableBasisInput? PayableBasis,
    string? CentralPmsPaymentAttemptRef = null,
    string? CentralPmsPaymentConfirmationRef = null,
    string? PaymentFinalityRef = null,
    string? VendorAckRef = null,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
