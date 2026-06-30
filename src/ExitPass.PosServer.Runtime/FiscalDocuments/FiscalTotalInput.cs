namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalTotalInput(
    Guid TotalTypeCodeId,
    long AmountMinorUnits,
    string CurrencyCode,
    IReadOnlyDictionary<string, string>? TotalContext = null);
