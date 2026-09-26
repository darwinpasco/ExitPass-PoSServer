namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed record CloseableFiscalBusinessDate(
    Guid FiscalReportingPeriodId,
    DateOnly FiscalBusinessDate,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    string Status,
    long TransactionCount,
    long ExpectedStateVersion);

public sealed record CloseableFiscalBusinessDatesSnapshot(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    IReadOnlyList<CloseableFiscalBusinessDate> FiscalBusinessDates,
    string Code,
    string? SafeMessage = null);

public interface ICloseableFiscalBusinessDateRepository
{
    Task<CloseableFiscalBusinessDatesSnapshot> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken = default);
}

public sealed class CloseableFiscalBusinessDateService(ICloseableFiscalBusinessDateRepository repository)
{
    public Task<CloseableFiscalBusinessDatesSnapshot> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var currency = currencyCode?.Trim().ToUpperInvariant();
        if (sitePosServerId == Guid.Empty || fiscalIdentityId == Guid.Empty || currency is not { Length: 3 })
        {
            return Task.FromResult(new CloseableFiscalBusinessDatesSnapshot(
                sitePosServerId,
                fiscalIdentityId,
                currency ?? string.Empty,
                [],
                "closeable_fiscal_business_dates_invalid",
                "The closeable fiscal business date request is invalid."));
        }

        return repository.ReadAsync(sitePosServerId, fiscalIdentityId, currency, cancellationToken);
    }
}
