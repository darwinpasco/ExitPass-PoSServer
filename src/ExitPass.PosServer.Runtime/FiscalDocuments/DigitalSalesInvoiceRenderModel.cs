namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record DigitalSalesInvoiceRenderModel(
    Guid FiscalDocumentId,
    Guid SitePosServerId,
    Guid? ChannelTerminalId,
    Guid? FiscalIdentityId,
    Guid FiscalDocumentTypeCodeId,
    string? FiscalDocumentTypeCodeKey,
    Guid FiscalDocumentStatusCodeId,
    string? FiscalDocumentStatusCodeKey,
    string FiscalNumberAssignmentState,
    Guid? FiscalSequencePolicyId,
    long? FiscalSequenceValue,
    string? FiscalDocumentNumber,
    string? FiscalSeries,
    string? FiscalNumberPrefixText,
    string? FiscalNumberSuffixText,
    DateTimeOffset? FiscalNumberAssignedAt,
    string? FiscalNumberAssignedByRef,
    DateOnly? BusinessDayDate,
    string? CentralPmsParkingSessionRef,
    string? CentralPmsPaymentAttemptRef,
    string? CentralPmsPaymentConfirmationRef,
    string? PaymentFinalityRef,
    string? VendorAckRef,
    string? SemanticRequestHash,
    string? SemanticRequestHashVersion,
    string? SemanticRequestHashStatus,
    string? VoidStatus,
    string? VoidReasonCode,
    DateTimeOffset? VoidedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DigitalSalesInvoiceLineRenderModel> Lines,
    IReadOnlyList<DigitalSalesInvoiceDiscountRenderModel> Discounts,
    IReadOnlyList<DigitalSalesInvoiceTaxDetailRenderModel> TaxDetails,
    IReadOnlyList<DigitalSalesInvoiceTenderRenderModel> Tenders,
    IReadOnlyList<DigitalSalesInvoiceTotalRenderModel> Totals,
    DigitalSalesInvoiceFooterRenderModel Footer,
    SalesInvoiceHeaderSnapshot? SalesInvoiceHeaderSnapshot = null,
    AppliedStatutoryFiscalFactsSnapshot? AppliedStatutoryFiscalFacts = null,
    string CompletionBasis = FiscalCompletionBasisCodes.PaymentFinality,
    string? CompletionAuthorityRef = null,
    InvoiceCustomerInformationSnapshot? InvoiceCustomerInformation = null,
    string? DocumentContextJson = null);

public sealed record DigitalSalesInvoiceLineRenderModel(
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
    string? SourceRef);

public sealed record DigitalSalesInvoiceDiscountRenderModel(
    Guid? FiscalDocumentLineId,
    Guid DiscountPrivilegeTypeCodeId,
    long BasisAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long VatPrivilegeAmountMinorUnits,
    string CurrencyCode,
    string? BeneficiaryRef,
    string? EvidenceRef,
    string? ApprovalRef);

public sealed record DigitalSalesInvoiceTaxDetailRenderModel(
    Guid? FiscalDocumentLineId,
    Guid TaxTypeCodeId,
    Guid TaxClassificationCodeId,
    decimal? TaxRate,
    long TaxableAmountMinorUnits,
    long TaxAmountMinorUnits,
    string CurrencyCode,
    string? TaxClassificationCodeKey = null);

public sealed record DigitalSalesInvoiceTenderRenderModel(
    Guid TenderTypeCodeId,
    string? TenderTypeCodeKey,
    long AmountMinorUnits,
    string CurrencyCode,
    string? CentralPmsPaymentAttemptRef,
    string? CentralPmsPaymentConfirmationRef,
    string? PaymentFinalityRef,
    string? ProviderRef);

public sealed record DigitalSalesInvoiceTotalRenderModel(
    Guid TotalTypeCodeId,
    long AmountMinorUnits,
    string CurrencyCode);

public sealed record DigitalSalesInvoiceFooterRenderModel(
    string RenderingStatus,
    IReadOnlyList<string> DisclaimerPlaceholders);
