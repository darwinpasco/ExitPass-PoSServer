namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class FiscalXReadingService(IFiscalXReadingRepository repository)
{
    public Task<FiscalXReadingResult> GenerateAsync(FiscalXReadingCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.OperationKey) || command.OperationKey.Length > 200 ||
            command.SitePosServerId == Guid.Empty || command.FiscalIdentityId == Guid.Empty || command.ObservedAt == default ||
            !IsSafeReference(command.RequestedByRef) || !IsSafeReference(command.ServiceIdentityRef) || !IsSafeReference(command.CorrelationId))
        {
            return Task.FromResult(new FiscalXReadingResult(FiscalXReadingOutcome.InvalidRequest, SafeMessage: "The X Reading request is incomplete or invalid."));
        }

        return repository.GenerateAsync(command with
        {
            OperationKey = command.OperationKey.Trim(),
            RequestedByRef = command.RequestedByRef.Trim(),
            ServiceIdentityRef = command.ServiceIdentityRef.Trim(),
            CorrelationId = command.CorrelationId.Trim()
        }, cancellationToken);
    }

    public Task<FiscalXReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(fiscalReportReference) || fiscalReportReference.Length > 160
            ? Task.FromResult(new FiscalXReadingResult(FiscalXReadingOutcome.InvalidRequest, SafeMessage: "Fiscal report reference is required."))
            : repository.GetByReferenceAsync(fiscalReportReference.Trim(), cancellationToken);

    private static bool IsSafeReference(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 200 && !value.Contains('\r') && !value.Contains('\n');
}
