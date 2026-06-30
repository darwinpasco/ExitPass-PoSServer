namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalTenderInput(
    Guid TenderTypeCodeId,
    long AmountMinorUnits,
    string CurrencyCode,
    string? CentralPmsPaymentAttemptRef = null,
    string? CentralPmsPaymentConfirmationRef = null,
    string? PaymentFinalityRef = null,
    string? ProviderRef = null,
    IReadOnlyDictionary<string, string>? TenderContext = null);
