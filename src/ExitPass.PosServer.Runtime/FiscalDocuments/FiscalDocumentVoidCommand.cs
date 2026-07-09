namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentVoidCommand(
    Guid FiscalDocumentId,
    string IdempotencyKey,
    string ReasonCode,
    string? ReasonText,
    string RequestedByRef,
    DateTimeOffset RequestedAt,
    string CorrelationId,
    string? SourceSystemRef,
    DateOnly? BusinessDayDate);
