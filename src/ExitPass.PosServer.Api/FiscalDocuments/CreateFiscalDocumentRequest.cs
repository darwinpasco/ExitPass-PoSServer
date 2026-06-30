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
    string? UpstreamFinalityRef = null,
    string? PaymentFinalityRef = null,
    string? VendorAckRef = null,
    IReadOnlyList<FiscalDocumentLinkRequest>? DocumentLinks = null,
    IReadOnlyList<CreateFiscalDocumentLineRequest>? DocumentLines = null,
    IReadOnlyList<CreateFiscalDocumentLineRequest>? Lines = null,
    IReadOnlyList<CreateFiscalTenderRequest>? Tenders = null,
    IReadOnlyList<CreateFiscalTaxDetailRequest>? TaxDetails = null,
    IReadOnlyList<CreateFiscalDiscountPrivilegeDetailRequest>? DiscountPrivilegeDetails = null,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);

public sealed record FiscalDocumentLinkRequest(
    Guid? TargetFiscalDocumentId,
    Guid? LinkTypeCodeId,
    Guid? LinkReasonCodeId = null,
    string? LinkReasonText = null,
    string? CreatedByRef = null);

public sealed record CreateFiscalDocumentLineRequest(
    int LineSequence,
    Guid? LineTypeCodeId,
    string? Description,
    decimal Quantity,
    long UnitAmountMinorUnits,
    long GrossAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long TaxAmountMinorUnits,
    long NetAmountMinorUnits,
    string? CurrencyCode,
    Guid? LineStatusCodeId = null,
    string? SourceRef = null,
    IReadOnlyDictionary<string, string>? LineContext = null);

public sealed record CreateFiscalTenderRequest(
    Guid? TenderTypeCodeId,
    long AmountMinorUnits,
    string? CurrencyCode,
    string? CentralPmsPaymentAttemptRef = null,
    string? CentralPmsPaymentConfirmationRef = null,
    string? PaymentFinalityRef = null,
    string? ProviderRef = null,
    IReadOnlyDictionary<string, string>? TenderContext = null);

public sealed record CreateFiscalTaxDetailRequest(
    Guid? TaxTypeCodeId,
    Guid? TaxClassificationCodeId,
    long TaxableAmountMinorUnits,
    long TaxAmountMinorUnits,
    string? CurrencyCode,
    int? LineSequence = null,
    decimal? TaxRate = null,
    IReadOnlyDictionary<string, string>? TaxContext = null);

public sealed record CreateFiscalDiscountPrivilegeDetailRequest(
    Guid? DiscountPrivilegeTypeCodeId,
    long BasisAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long VatPrivilegeAmountMinorUnits,
    string? CurrencyCode,
    int? LineSequence = null,
    string? BeneficiaryRef = null,
    string? EvidenceRef = null,
    string? ApprovalRef = null,
    IReadOnlyDictionary<string, string>? DiscountPrivilegeContext = null);
