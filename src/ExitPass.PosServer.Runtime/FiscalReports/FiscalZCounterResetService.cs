namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class FiscalZCounterResetService(IFiscalZCounterResetRepository repository)
{
    public Task<FiscalZCounterResetResult> ReadStateAsync(
        FiscalZCounterStateQuery query,
        CancellationToken cancellationToken = default) =>
        IsValidScope(query.SitePosServerId, query.FiscalIdentityId, query.CurrencyCode)
            ? repository.ReadStateAsync(query with { CurrencyCode = query.CurrencyCode.Trim().ToUpperInvariant() }, cancellationToken)
            : Task.FromResult(Invalid("The fiscal Z Counter scope is incomplete or invalid."));

    public Task<FiscalZCounterResetResult> ResetAsync(
        FiscalZCounterResetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidScope(command.SitePosServerId, command.FiscalIdentityId, command.CurrencyCode) ||
            !IsSafe(command.OperationKey, 200) || command.ExpectedStateVersion < 1 ||
            !IsSafe(command.Reason, 500) || !IsSafe(command.RequestedByRef, 200) ||
            !IsSafe(command.ServiceIdentityRef, 200) || !IsSafe(command.CorrelationId, 200))
        {
            return Task.FromResult(Invalid("The manual Z Counter reset request is incomplete or invalid."));
        }

        return repository.ResetAsync(command with
        {
            OperationKey = command.OperationKey.Trim(),
            CurrencyCode = command.CurrencyCode.Trim().ToUpperInvariant(),
            Reason = command.Reason.Trim(),
            RequestedByRef = command.RequestedByRef.Trim(),
            ServiceIdentityRef = command.ServiceIdentityRef.Trim(),
            CorrelationId = command.CorrelationId.Trim()
        }, cancellationToken);
    }

    private static bool IsValidScope(Guid site, Guid identity, string? currency) =>
        site != Guid.Empty && identity != Guid.Empty && currency?.Trim() is { Length: 3 } value &&
        value.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');

    private static bool IsSafe(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximumLength &&
        !value.Contains('\r') && !value.Contains('\n');

    private static FiscalZCounterResetResult Invalid(string message) =>
        new(FiscalZCounterResetOutcome.InvalidRequest, SafeMessage: message);
}
