namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class FiscalZReadingService(IFiscalZReadingRepository repository)
{
    public Task<FiscalZReadingResult> CloseAsync(FiscalZReadingCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.OperationKey) || command.OperationKey.Length > 200 ||
            command.SitePosServerId == Guid.Empty || command.FiscalIdentityId == Guid.Empty ||
            command.FiscalReportingPeriodId == Guid.Empty || command.ExpectedStateVersion < 1 ||
            string.IsNullOrWhiteSpace(command.CurrencyCode) || command.CurrencyCode.Length != 3 ||
            command.CurrencyCode.Any(character => character is < 'A' or > 'Z') ||
            !IsSafeReference(command.RequestedByRef) || !IsSafeReference(command.ServiceIdentityRef) ||
            !IsSafeReference(command.CorrelationId))
        {
            return Task.FromResult(new FiscalZReadingResult(
                FiscalZReadingOutcome.InvalidRequest,
                SafeMessage: "The Z Reading close request is incomplete or invalid."));
        }

        return repository.CloseAsync(command with
        {
            OperationKey = command.OperationKey.Trim(),
            CurrencyCode = command.CurrencyCode.Trim(),
            RequestedByRef = command.RequestedByRef.Trim(),
            ServiceIdentityRef = command.ServiceIdentityRef.Trim(),
            CorrelationId = command.CorrelationId.Trim()
        }, cancellationToken);
    }

    public Task<FiscalZReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(fiscalReportReference) || fiscalReportReference.Length > 160
            ? Task.FromResult(new FiscalZReadingResult(FiscalZReadingOutcome.InvalidRequest, SafeMessage: "Z Reading reference is required."))
            : repository.GetByReferenceAsync(fiscalReportReference.Trim(), cancellationToken);

    private static bool IsSafeReference(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 200 && !value.Contains('\r') && !value.Contains('\n');
}
