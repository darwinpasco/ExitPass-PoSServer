namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class FiscalDocumentReprintContract
{
    public const string ContractVersion = "pos-server-fiscal-document-reprint:v1";
    public const string SemanticHashVersion = "pos-server-fiscal-document-reprint:sha256:v1";
    public const string EventType = "fiscal_document_reprint_recorded";
}

public sealed record FiscalDocumentReprintCommand(
    Guid FiscalDocumentId,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    string OperationKey,
    string ReasonCode,
    string ActorReference,
    string ServiceIdentityReference,
    string CorrelationReference);

public sealed record FiscalDocumentReprintRecord(
    Guid ReprintRequestId,
    string ReprintReference,
    Guid FiscalDocumentId,
    string FiscalDocumentNumber,
    long FiscalSequenceValue,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid FiscalReportingPeriodId,
    Guid FiscalSequencePolicyId,
    DateOnly BusinessDayDate,
    long CopySequence,
    string ReprintType,
    string ReprintStatus,
    string ReprintReason,
    string OutputReference,
    string OutputType,
    bool ReprintLabelApplied,
    DateTimeOffset RequestedAt,
    DateTimeOffset CommittedAt,
    string ActorReference,
    string ServiceIdentityReference,
    string CorrelationReference,
    string OperationKey,
    string ElectronicJournalEventReference);

public enum FiscalDocumentReprintOutcome
{
    Created,
    Replayed,
    InvalidRequest,
    Conflict,
    NotFound,
    ScopeMismatch,
    InvalidState,
    IntegrityFailure,
    PersistenceFailure
}

public sealed record FiscalDocumentReprintResult(
    FiscalDocumentReprintOutcome Outcome,
    FiscalDocumentReprintRecord? Record = null,
    string? SafeMessage = null);

public interface IFiscalDocumentReprintRepository
{
    Task<FiscalDocumentReprintResult> RecordAsync(
        FiscalDocumentReprintCommand command,
        string semanticRequestHash,
        CancellationToken cancellationToken = default);
}
