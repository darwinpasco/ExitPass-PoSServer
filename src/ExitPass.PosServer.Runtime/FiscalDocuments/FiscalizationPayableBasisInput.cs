namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalizationPayableBasisInput(
    string PayableBasisRef,
    string UpstreamFinalityRef,
    string CurrencyCode,
    long PayableAmountMinorUnits,
    IReadOnlyList<FiscalDiscountReferenceInput>? DiscountReferences = null,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
