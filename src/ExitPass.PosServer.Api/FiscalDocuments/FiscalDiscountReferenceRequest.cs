namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record FiscalDiscountReferenceRequest(
    string? DiscountValidationRef,
    string? Status,
    bool AppliesStatutoryDiscountTreatment,
    IReadOnlyDictionary<string, string>? ReferenceContext = null);
