namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalZReadingContract
{
    public const string ContractVersion = "pos-server-fiscal-reporting:v1";
    public const string SemanticHashVersion = "pos-server-fiscal-report-request:sha256:v1";
    public const string StateTransitionSemanticHashVersion = "pos-server-fiscal-z-close-state-transition:sha256:v1";
    public const string ReportKind = "Z_READING";
}

public sealed record FiscalZReadingCommand(
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid FiscalReportingPeriodId,
    long ExpectedStateVersion,
    string RequestedByRef,
    string ServiceIdentityRef,
    string CorrelationId);

public sealed record FiscalZReadingPeriod(
    Guid FiscalReportingPeriodId,
    Guid FiscalReportingContractVersionId,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    DateOnly BusinessDayDate,
    DateTimeOffset PeriodStartAt,
    DateTimeOffset PeriodEndAt,
    string ReportingTimezoneName,
    TimeOnly BusinessDayCutoffLocalTime,
    string CurrencyCode,
    long PeriodSequence,
    Guid? ExpectedPriorPeriodId,
    string Status,
    DateTimeOffset DatabaseNow);

public sealed record FiscalZReadingCounterSnapshot(
    long PreviousResetCounterValue,
    long ResultingResetCounterValue,
    long PreviousZCounterValue,
    long ResultingZCounterValue,
    long PreviousGrandTotalAmountMinorUnits,
    long CurrentPeriodGrandTotalAmountMinorUnits,
    long ResultingGrandTotalAmountMinorUnits,
    long ExpectedStateVersion,
    long ResultingStateVersion);

public sealed record FiscalZReadingRecord(
    Guid FiscalReportId,
    string FiscalReportReference,
    Guid FiscalReportRequestId,
    string OperationKey,
    string ReportKind,
    string ContractVersion,
    string SemanticHashVersion,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    Guid FiscalReportingPeriodId,
    Guid? PriorFiscalReportingPeriodId,
    DateOnly BusinessDayDate,
    DateTimeOffset PeriodStartAt,
    DateTimeOffset PeriodEndAt,
    string ReportingTimezoneName,
    TimeOnly BusinessDayCutoffLocalTime,
    string CurrencyCode,
    DateTimeOffset GeneratedAt,
    DateTimeOffset CommittedAt,
    DateTimeOffset ClosedAt,
    long QualifyingDocumentCount,
    FiscalXReadingAmounts Amounts,
    IReadOnlyList<FiscalXReadingTenderBreakdown> Tenders,
    IReadOnlyList<FiscalXReadingDiscountBreakdown> Discounts,
    IReadOnlyList<FiscalXReadingFiscalNumberRange> FiscalNumberRanges,
    FiscalZReadingCounterSnapshot CounterSnapshot,
    string PeriodStatus,
    string CorrelationId,
    string SupportReference,
    bool Immutable,
    long PeriodSequence = 0,
    string ReportStatus = "COMMITTED");

public enum FiscalZReadingOutcome
{
    Created,
    Replayed,
    Conflict,
    PeriodAlreadyClosed,
    SitePosServerNotFound,
    FiscalIdentityNotFound,
    ReportingConfigurationUnavailable,
    PeriodUnavailable,
    PeriodNotEnded,
    PeriodNotOpen,
    StateInitializationRequired,
    StaleStateVersion,
    PriorPeriodUnresolved,
    UnassignedFiscalDocument,
    UnsupportedSourceClassification,
    UnsupportedCrossPeriodMutation,
    MixedCurrency,
    UnexplainedFiscalGap,
    ReconciliationFailure,
    ArithmeticOverflow,
    LockTimeout,
    RetryableConcurrencyFailure,
    PersistenceFailure,
    UnknownCommitOutcome,
    InvalidRequest,
    NotFound
}

public sealed record FiscalZReadingResult(
    FiscalZReadingOutcome Outcome,
    FiscalZReadingRecord? Record = null,
    string? SafeMessage = null);

public interface IFiscalZReadingRepository
{
    Task<FiscalZReadingResult> CloseAsync(FiscalZReadingCommand command, CancellationToken cancellationToken = default);
    Task<FiscalZReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default);
}

public sealed class FiscalZReadingSafeException : Exception
{
    public FiscalZReadingSafeException(FiscalZReadingOutcome outcome, string message) : base(message) => Outcome = outcome;
    public FiscalZReadingOutcome Outcome { get; }
}
