namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public enum FiscalDocumentVoidErrorCode
{
    None,
    MissingIdempotencyKey,
    MissingReasonCode,
    InvalidReasonCode,
    MissingRequestedByRef,
    MissingCorrelationId,
    IdempotencyConflict,
    FiscalDocumentNotFound,
    InvalidStateTransition,
    PersistenceRejected
}
