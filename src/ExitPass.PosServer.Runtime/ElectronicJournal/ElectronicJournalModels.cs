namespace ExitPass.PosServer.Runtime.ElectronicJournal;

public static class ElectronicJournalContract
{
    public const string EventSchemaVersion = "pos-server-electronic-journal-event:v1";
    public const string LegacySemanticHashVersion = "pos-server-electronic-journal-event-semantic:sha256:v1";
    public const string CurrentSemanticHashVersion = "pos-server-electronic-journal-event-semantic:sha256:v2";
    public const string IntegrityHashVersion = "pos-server-electronic-journal-integrity:sha256:v1";
    public const string ChronologyVersion = "pos-server-electronic-journal-chronology:v1";
    public const string ExportVersion = "pos-server-electronic-journal-export:v1";
    public const int DefaultPageSize = 100;
    public const int MaximumPageSize = 200;
    public const int MaximumExportEvents = 10_000;
    public const int MaximumTimeRangeDays = 31;
    public const int MaximumPrintableSalesInvoiceTextCharacters = 131_072;
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";
}

public sealed record ElectronicJournalAppendRequest(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid FiscalReportingPeriodId,
    string EventType,
    string SourceTransitionReference,
    string SourceTransitionVersion,
    DateTimeOffset EffectiveAt,
    string ActorReference,
    string ServiceIdentityReference,
    string CorrelationReference,
    IReadOnlyDictionary<string, string?> Facts,
    Guid? FiscalDocumentId = null,
    Guid? FiscalReportRequestId = null,
    Guid? XZReportId = null,
    Guid? BirSalesSummaryReportId = null,
    Guid? FiscalSequencePolicyId = null,
    DateOnly? BusinessDayDate = null,
    string? IdempotencyReference = null,
    Guid? ReprintRequestId = null,
    string? PrintableSalesInvoiceText = null);

public sealed record ElectronicJournalEvent(
    string EventReference,
    string EventType,
    string EventSchemaVersion,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid FiscalReportingPeriodId,
    Guid? FiscalDocumentId,
    Guid? FiscalReportRequestId,
    Guid? XZReportId,
    Guid? BirSalesSummaryReportId,
    Guid? FiscalSequencePolicyId,
    DateOnly? BusinessDayDate,
    long StreamSequence,
    DateTimeOffset EffectiveAt,
    DateTimeOffset RecordedAt,
    string ActorReference,
    string ServiceIdentityReference,
    string CorrelationReference,
    string SourceTransitionReference,
    string SourceTransitionVersion,
    string? IdempotencyReference,
    string SemanticHashVersion,
    string SemanticHash,
    string IntegrityHashVersion,
    string PreviousIntegrityHash,
    string IntegrityHash,
    string RetentionPolicy,
    IReadOnlyDictionary<string, string?> Facts,
    Guid? ReprintRequestId = null,
    string? PrintableSalesInvoiceText = null);

public sealed record ElectronicJournalQuery(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid? FiscalReportingPeriodId = null,
    string? FiscalDocumentReference = null,
    string? FiscalDocumentNumber = null,
    string? ZReadingReference = null,
    string? EventType = null,
    DateTimeOffset? EffectiveFrom = null,
    DateTimeOffset? EffectiveTo = null,
    DateTimeOffset? RecordedFrom = null,
    DateTimeOffset? RecordedTo = null,
    string? CorrelationReference = null,
    int PageSize = ElectronicJournalContract.DefaultPageSize,
    string? Cursor = null,
    long? ThroughSequence = null);

public sealed record ElectronicJournalPage(
    IReadOnlyList<ElectronicJournalEvent> Events,
    string? NextCursor,
    long ThroughSequence,
    string ChronologyVersion);

public sealed record ElectronicJournalIntegrityResult(
    bool IsValid,
    long VerifiedEventCount,
    long FirstSequence,
    long LastSequence,
    string? FailureCode,
    string SupportReference);

public enum ElectronicJournalOutcome
{
    Success,
    InvalidRequest,
    NotFound,
    UnsupportedEventType,
    MalformedCursor,
    RangeTooLarge,
    IntegrityFailure,
    PersistenceFailure
}

public sealed record ElectronicJournalPageResult(
    ElectronicJournalOutcome Outcome,
    ElectronicJournalPage? Page = null,
    string? SafeMessage = null);

public sealed record ElectronicJournalIntegrityOutcome(
    ElectronicJournalOutcome Outcome,
    ElectronicJournalIntegrityResult? Result = null,
    string? SafeMessage = null);

public enum ElectronicJournalExportFormat { Json, Csv }

public sealed record ElectronicJournalExport(
    ElectronicJournalExportFormat Format,
    string ContentType,
    string FileName,
    byte[] Bytes,
    string ContentSha256,
    string OutputIdentity,
    string ETag);

public interface IElectronicJournalRepository
{
    Task<ElectronicJournalPageResult> ReadAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default);
    Task<ElectronicJournalIntegrityOutcome> VerifyIntegrityAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default);
    Task RecordAccessAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        string action,
        string result,
        string actorReference,
        string serviceIdentityReference,
        string correlationReference,
        string supportReference,
        int eventCount,
        CancellationToken cancellationToken = default);
}
