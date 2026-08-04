namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalZCloseStateContract
{
    public const string ContractVersion = "pos-server-fiscal-reporting:v1";
    public const string InitializationSemanticHashVersion = "pos-server-fiscal-z-close-state-initialize:sha256:v1";
    public const string ApprovedNewScopeZero = "approved_new_scope_zero";
    public const string VerifiedLegacyImport = "verified_legacy_import";
}

public sealed record InitializeFiscalZCloseStateCommand(
    string OperationReference,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    string Provenance,
    long ResetCounterValue,
    long ZCounterValue,
    long GrandTotalAmountMinorUnits,
    string ApprovalReference,
    string ActorReference,
    string ServiceIdentityReference,
    string CorrelationReference);

public sealed record FiscalZCloseStateRecord(
    Guid FiscalZCloseStateId,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    string ContractVersion,
    long ResetCounterValue,
    long ZCounterValue,
    long GrandTotalAmountMinorUnits,
    long StateVersion,
    Guid? LastClosedReportingPeriodId,
    Guid? LastCommittedZReportId,
    string InitializationProvenance,
    DateTimeOffset InitializedAt,
    string InitializationApprovalReference,
    string LastTransitionOperationReference,
    DateTimeOffset LastTransitionAt);

public enum FiscalZCloseStateInitializationOutcome
{
    Initialized,
    Replayed,
    Rejected
}

public enum FiscalZCloseStateInitializationErrorCode
{
    None,
    InvalidRequest,
    InvalidProvenance,
    InvalidLegacyImport,
    StateAlreadyInitialized,
    SemanticConflict,
    SitePosServerUnavailable,
    FiscalIdentityUnavailable,
    FiscalIdentityDisabled,
    ContractUnavailable,
    ControlledCodeUnavailable,
    LockTimeout,
    TransientPersistenceFailure,
    UnknownCommitOutcome
}

public sealed record FiscalZCloseStateInitializationResult(
    FiscalZCloseStateInitializationOutcome Outcome,
    FiscalZCloseStateInitializationErrorCode ErrorCode,
    string Message,
    FiscalZCloseStateRecord? State)
{
    public bool Succeeded => Outcome is FiscalZCloseStateInitializationOutcome.Initialized or FiscalZCloseStateInitializationOutcome.Replayed;

    public static FiscalZCloseStateInitializationResult Initialized(FiscalZCloseStateRecord state) =>
        new(FiscalZCloseStateInitializationOutcome.Initialized, FiscalZCloseStateInitializationErrorCode.None, "Fiscal Z close state initialized.", state);

    public static FiscalZCloseStateInitializationResult Replayed(FiscalZCloseStateRecord state) =>
        new(FiscalZCloseStateInitializationOutcome.Replayed, FiscalZCloseStateInitializationErrorCode.None, "Fiscal Z close state initialization replayed.", state);

    public static FiscalZCloseStateInitializationResult Failure(FiscalZCloseStateInitializationErrorCode code, string message) =>
        new(FiscalZCloseStateInitializationOutcome.Rejected, code, message, null);
}

public interface IFiscalZCloseStateRepository
{
    Task<FiscalZCloseStateInitializationResult> InitializeAsync(
        InitializeFiscalZCloseStateCommand command,
        string semanticRequestHash,
        CancellationToken cancellationToken = default);
}
