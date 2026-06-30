namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalTaxDetailInput(
    Guid TaxTypeCodeId,
    Guid TaxClassificationCodeId,
    long TaxableAmountMinorUnits,
    long TaxAmountMinorUnits,
    string CurrencyCode,
    int? LineSequence = null,
    decimal? TaxRate = null,
    IReadOnlyDictionary<string, string>? TaxContext = null);
