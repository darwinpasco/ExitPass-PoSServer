namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalBusinessDateLifecycleStates
{
    public const string NoOpenPeriod = "NO_OPEN_PERIOD";
    public const string Open = "OPEN";
    public const string Closing = "CLOSING";
    public const string Closed = "CLOSED";
}

public sealed record OpenFiscalBusinessDateCommand(
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    DateOnly BusinessDateLabel,
    bool ConfirmOpen,
    string RequestedByRef,
    string ServiceIdentityRef,
    string CorrelationId,
    string? RequestedByDisplayName = null);

public sealed record FiscalBusinessDateLifecyclePeriod(
    Guid FiscalReportingPeriodId,
    DateOnly BusinessDateLabel,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    long PeriodSequence,
    long TransactionCount,
    long ExpectedZStateVersion,
    string? ZReadingReference = null);

public sealed record FiscalBusinessDateLifecycleSnapshot(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    string ReportingTimezoneName,
    string State,
    DateOnly ProposedNextBusinessDateLabel,
    FiscalBusinessDateLifecyclePeriod? ActivePeriod,
    FiscalBusinessDateLifecyclePeriod? LastClosedPeriod,
    DateTimeOffset DatabaseNow);

public enum OpenFiscalBusinessDateOutcome
{
    Opened,
    Replayed,
    Conflict,
    InvalidRequest,
    PeriodAlreadyOpen,
    PriorPeriodUnresolved,
    ConfigurationUnavailable,
    LockTimeout,
    PersistenceFailure
}

public sealed record OpenFiscalBusinessDateResult(
    OpenFiscalBusinessDateOutcome Outcome,
    FiscalBusinessDateLifecycleSnapshot? Snapshot = null,
    string? SafeMessage = null,
    FiscalBusinessDateLifecyclePeriod? OpenedPeriod = null);

public interface IFiscalBusinessDateLifecycleRepository
{
    Task<FiscalBusinessDateLifecycleSnapshot> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken = default);

    Task<OpenFiscalBusinessDateResult> OpenAsync(
        OpenFiscalBusinessDateCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class FiscalBusinessDateLifecycleService(IFiscalBusinessDateLifecycleRepository repository)
{
    public Task<FiscalBusinessDateLifecycleSnapshot> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        repository.ReadAsync(sitePosServerId, fiscalIdentityId, NormalizeCurrency(currencyCode), cancellationToken);

    public Task<OpenFiscalBusinessDateResult> OpenAsync(
        OpenFiscalBusinessDateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var normalized = command with
        {
            OperationKey = command.OperationKey?.Trim() ?? string.Empty,
            CurrencyCode = NormalizeCurrency(command.CurrencyCode),
            RequestedByRef = command.RequestedByRef?.Trim() ?? string.Empty,
            ServiceIdentityRef = command.ServiceIdentityRef?.Trim() ?? string.Empty,
            CorrelationId = command.CorrelationId?.Trim() ?? string.Empty,
            RequestedByDisplayName = string.IsNullOrWhiteSpace(command.RequestedByDisplayName)
                ? null
                : command.RequestedByDisplayName.Trim()
        };
        if (normalized.SitePosServerId == Guid.Empty || normalized.FiscalIdentityId == Guid.Empty ||
            normalized.CurrencyCode.Length != 3 || string.IsNullOrWhiteSpace(normalized.OperationKey) ||
            normalized.BusinessDateLabel == default || !normalized.ConfirmOpen ||
            string.IsNullOrWhiteSpace(normalized.RequestedByRef) ||
            string.IsNullOrWhiteSpace(normalized.ServiceIdentityRef) ||
            string.IsNullOrWhiteSpace(normalized.CorrelationId))
        {
            return Task.FromResult(new OpenFiscalBusinessDateResult(
                OpenFiscalBusinessDateOutcome.InvalidRequest,
                SafeMessage: "A confirmed manual fiscal business-date open request is required."));
        }
        return repository.OpenAsync(normalized, cancellationToken);
    }

    private static string NormalizeCurrency(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
}
