namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class BirSalesSummaryService(IBirSalesSummaryRepository repository)
{
    public Task<BirSalesSummaryResult> GenerateAsync(
        BirSalesSummaryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeReference(command.OperationKey, 200) ||
            command.SitePosServerId == Guid.Empty ||
            command.FiscalIdentityId == Guid.Empty ||
            command.FiscalReportingPeriodId == Guid.Empty ||
            !IsCurrency(command.CurrencyCode) ||
            !IsSafeReference(command.GoverningZReadingReference, 160) ||
            !IsSafeReference(command.RequestedByRef, 200) ||
            !IsSafeReference(command.ServiceIdentityRef, 200) ||
            !IsSafeReference(command.CorrelationId, 200))
        {
            return Task.FromResult(new BirSalesSummaryResult(
                BirSalesSummaryOutcome.InvalidRequest,
                SafeMessage: "The BIR sales-summary request is incomplete or invalid."));
        }

        return repository.GenerateAsync(command with
        {
            OperationKey = command.OperationKey.Trim(),
            CurrencyCode = command.CurrencyCode.Trim(),
            GoverningZReadingReference = command.GoverningZReadingReference.Trim(),
            RequestedByRef = command.RequestedByRef.Trim(),
            ServiceIdentityRef = command.ServiceIdentityRef.Trim(),
            CorrelationId = command.CorrelationId.Trim()
        }, cancellationToken);
    }

    public Task<BirSalesSummaryResult> GetByIdAsync(
        Guid birSalesSummaryReportId,
        CancellationToken cancellationToken = default) =>
        birSalesSummaryReportId == Guid.Empty
            ? Task.FromResult(new BirSalesSummaryResult(
                BirSalesSummaryOutcome.InvalidRequest,
                SafeMessage: "A BIR sales-summary reference is required."))
            : repository.GetByIdAsync(birSalesSummaryReportId, cancellationToken);

    private static bool IsCurrency(string? value) =>
        value is { Length: 3 } && value.All(character => character is >= 'A' and <= 'Z');

    private static bool IsSafeReference(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength &&
        !value.Contains('\r') && !value.Contains('\n');
}
