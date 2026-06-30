namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentLineInput(
    int LineSequence,
    Guid LineTypeCodeId,
    string Description,
    decimal Quantity,
    long UnitAmountMinorUnits,
    long GrossAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long TaxAmountMinorUnits,
    long NetAmountMinorUnits,
    string CurrencyCode,
    Guid? LineStatusCodeId = null,
    string? SourceRef = null,
    IReadOnlyDictionary<string, string>? LineContext = null);
