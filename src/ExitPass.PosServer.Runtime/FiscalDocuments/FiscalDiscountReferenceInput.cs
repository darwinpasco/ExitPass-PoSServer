namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDiscountReferenceInput(
    string DiscountValidationRef,
    FiscalDiscountReferenceStatus Status,
    bool AppliesStatutoryDiscountTreatment,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
