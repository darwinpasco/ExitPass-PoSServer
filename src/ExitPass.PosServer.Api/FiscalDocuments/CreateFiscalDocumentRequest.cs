namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record CreateFiscalDocumentRequest(
    string? SitePosServerRef,
    string? FiscalDocumentTypeCodeKey,
    FiscalizationPayableBasisRequest? PayableBasis,
    Guid? SitePosServerId = null,
    Guid? ChannelTerminalId = null,
    Guid? FiscalDocumentTypeCodeId = null,
    Guid? FiscalDocumentStatusCodeId = null,
    DateOnly? BusinessDayDate = null,
    string? CentralPmsParkingSessionRef = null,
    string? CentralPmsPaymentAttemptRef = null,
    string? CentralPmsPaymentConfirmationRef = null,
    string? PaymentFinalityRef = null,
    string? VendorAckRef = null,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
