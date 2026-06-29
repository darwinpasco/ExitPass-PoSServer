namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record FiscalizationPayableBasisRequest(
    string? PayableBasisRef,
    string? UpstreamFinalityRef,
    string? CurrencyCode,
    long PayableAmountMinorUnits,
    IReadOnlyList<FiscalDiscountReferenceRequest>? DiscountReferences = null,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
