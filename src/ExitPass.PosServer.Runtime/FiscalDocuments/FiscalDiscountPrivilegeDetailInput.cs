namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDiscountPrivilegeDetailInput(
    Guid DiscountPrivilegeTypeCodeId,
    long BasisAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long VatPrivilegeAmountMinorUnits,
    string CurrencyCode,
    int? LineSequence = null,
    string? BeneficiaryRef = null,
    string? EvidenceRef = null,
    string? ApprovalRef = null,
    IReadOnlyDictionary<string, string>? DiscountPrivilegeContext = null);
