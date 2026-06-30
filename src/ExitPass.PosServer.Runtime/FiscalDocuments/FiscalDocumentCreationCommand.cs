namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentCreationCommand(
    string SitePosServerRef,
    string FiscalDocumentTypeCodeKey,
    FiscalizationPayableBasisInput? PayableBasis,
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
    IReadOnlyList<FiscalDocumentLinkInput>? DocumentLinks = null,
    IReadOnlyList<FiscalDocumentLineInput>? DocumentLines = null,
    IReadOnlyList<FiscalTenderInput>? Tenders = null,
    IReadOnlyList<FiscalTaxDetailInput>? TaxDetails = null,
    IReadOnlyList<FiscalDiscountPrivilegeDetailInput>? DiscountPrivilegeDetails = null,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
