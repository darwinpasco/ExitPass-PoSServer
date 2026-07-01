namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentReadModel(
    Guid FiscalDocumentId,
    Guid SitePosServerId,
    Guid? ChannelTerminalId,
    Guid FiscalDocumentTypeCodeId,
    Guid FiscalDocumentStatusCodeId,
    string? CentralPmsParkingSessionRef,
    string? CentralPmsPaymentAttemptRef,
    string? CentralPmsPaymentConfirmationRef,
    string? PaymentFinalityRef,
    string? VendorAckRef,
    DateOnly? BusinessDayDate,
    string? DocumentContextJson,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FiscalDocumentStatusHistoryReadModel> StatusHistory,
    IReadOnlyList<FiscalDocumentLinkReadModel> DocumentLinks,
    IReadOnlyList<FiscalDocumentLineReadModel> Lines,
    IReadOnlyList<FiscalTenderReadModel> Tenders,
    IReadOnlyList<FiscalTaxDetailReadModel> TaxDetails,
    IReadOnlyList<FiscalDiscountPrivilegeDetailReadModel> DiscountPrivilegeDetails,
    IReadOnlyList<FiscalTotalReadModel> Totals);

public sealed record FiscalDocumentStatusHistoryReadModel(
    Guid FiscalDocumentStatusHistoryId,
    Guid FiscalDocumentId,
    Guid? PriorFiscalDocumentStatusCodeId,
    Guid NewFiscalDocumentStatusCodeId,
    Guid? StatusReasonCodeId,
    string? StatusReasonText,
    DateTimeOffset ChangedAt,
    string? ActorRef,
    string? ServiceIdentityRef,
    DateTimeOffset CreatedAt);

public sealed record FiscalDocumentLinkReadModel(
    Guid FiscalDocumentLinkId,
    Guid SourceFiscalDocumentId,
    Guid TargetFiscalDocumentId,
    Guid FiscalDocumentLinkTypeCodeId,
    Guid? LinkReasonCodeId,
    string? LinkReasonText,
    DateTimeOffset CreatedAt,
    string? CreatedByRef);

public sealed record FiscalDocumentLineReadModel(
    Guid FiscalDocumentLineId,
    Guid FiscalDocumentId,
    int LineSequence,
    Guid LineTypeCodeId,
    Guid? LineStatusCodeId,
    string Description,
    decimal Quantity,
    long UnitAmountMinorUnits,
    long GrossAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long TaxAmountMinorUnits,
    long NetAmountMinorUnits,
    string CurrencyCode,
    string? SourceRef,
    string? LineContextJson,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record FiscalTenderReadModel(
    Guid FiscalTenderId,
    Guid FiscalDocumentId,
    Guid TenderTypeCodeId,
    long AmountMinorUnits,
    string CurrencyCode,
    string? CentralPmsPaymentAttemptRef,
    string? CentralPmsPaymentConfirmationRef,
    string? PaymentFinalityRef,
    string? ProviderRef,
    string? TenderContextJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record FiscalTaxDetailReadModel(
    Guid FiscalTaxDetailId,
    Guid FiscalDocumentId,
    Guid? FiscalDocumentLineId,
    Guid TaxTypeCodeId,
    Guid TaxClassificationCodeId,
    decimal? TaxRate,
    long TaxableAmountMinorUnits,
    long TaxAmountMinorUnits,
    string CurrencyCode,
    string? TaxContextJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record FiscalDiscountPrivilegeDetailReadModel(
    Guid FiscalDiscountPrivilegeDetailId,
    Guid FiscalDocumentId,
    Guid? FiscalDocumentLineId,
    Guid DiscountPrivilegeTypeCodeId,
    long BasisAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long VatPrivilegeAmountMinorUnits,
    string CurrencyCode,
    string? BeneficiaryRef,
    string? EvidenceRef,
    string? ApprovalRef,
    string? DiscountPrivilegeContextJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record FiscalTotalReadModel(
    Guid FiscalTotalId,
    Guid FiscalDocumentId,
    Guid TotalTypeCodeId,
    long AmountMinorUnits,
    string CurrencyCode,
    string? TotalContextJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
