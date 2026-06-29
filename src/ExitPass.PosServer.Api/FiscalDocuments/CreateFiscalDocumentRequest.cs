namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record CreateFiscalDocumentRequest(
    string? SitePosServerRef,
    string? FiscalDocumentTypeCodeKey,
    FiscalizationPayableBasisRequest? PayableBasis,
    string? CentralPmsPaymentAttemptRef = null,
    string? CentralPmsPaymentConfirmationRef = null,
    string? PaymentFinalityRef = null,
    string? VendorAckRef = null,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
