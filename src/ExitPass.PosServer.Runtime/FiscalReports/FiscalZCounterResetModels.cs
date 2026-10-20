namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalZCounterResetContract
{
    public const string SemanticHashVersion = "pos-server-fiscal-z-counter-reset:sha256:v1";
    public const string TransitionVersion = "pos-server-fiscal-z-counter-reset:v1";
}

public sealed record FiscalZCounterResetCommand(
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    long ExpectedStateVersion,
    string Reason,
    string RequestedByRef,
    string ServiceIdentityRef,
    string CorrelationId);

public sealed record FiscalZCounterStateQuery(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode);

public sealed record FiscalZCounterStateRecord(
    Guid FiscalZCloseStateId,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    long ZCounterValue,
    long ResetCounterValue,
    long GrandTotalAmountMinorUnits,
    long StateVersion,
    string? LastZReadingReference,
    DateOnly? LastZReadingBusinessDate,
    DateTimeOffset? LastZReadingCommittedAt,
    FiscalZCounterResetRecord? LastReset);

public sealed record FiscalZCounterResetRecord(
    Guid ResetEventId,
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    long PreviousZCounterValue,
    long ResultingZCounterValue,
    long PreviousResetCounterValue,
    long ResultingResetCounterValue,
    long PreviousGrandTotalAmountMinorUnits,
    long ResultingGrandTotalAmountMinorUnits,
    long ExpectedStateVersion,
    long ResultingStateVersion,
    string AdministratorReference,
    string ServiceIdentityReference,
    string Reason,
    DateTimeOffset ResetAt,
    string ElectronicJournalEventReference);

public enum FiscalZCounterResetOutcome
{
    Created,
    Replayed,
    NotFound,
    StateInitializationRequired,
    StaleStateVersion,
    Conflict,
    ConcurrentFiscalClose,
    ArithmeticOverflow,
    InvalidRequest,
    PersistenceFailure,
    UnknownCommitOutcome
}

public sealed record FiscalZCounterResetResult(
    FiscalZCounterResetOutcome Outcome,
    FiscalZCounterResetRecord? Reset = null,
    FiscalZCounterStateRecord? State = null,
    string? SafeMessage = null);

public interface IFiscalZCounterResetRepository
{
    Task<FiscalZCounterResetResult> ReadStateAsync(FiscalZCounterStateQuery query, CancellationToken cancellationToken = default);
    Task<FiscalZCounterResetResult> ResetAsync(FiscalZCounterResetCommand command, CancellationToken cancellationToken = default);
}
