namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentVoidRecord(
    Guid FiscalDocumentId,
    string? FiscalDocumentNumber,
    long? FiscalSequenceValue,
    string FiscalDocumentStatus,
    string VoidStatus,
    DateTimeOffset VoidedAt,
    string VoidReasonCode,
    string? VoidReasonText,
    string RequestedByRef,
    string IdempotencyKey,
    string CorrelationId);
