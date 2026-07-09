namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record VoidFiscalDocumentRequest(
    string? IdempotencyKey,
    string? ReasonCode,
    string? ReasonText,
    string? RequestedByRef,
    DateTimeOffset? RequestedAt,
    string? CorrelationId,
    string? SourceSystemRef,
    DateOnly? BusinessDayDate);
