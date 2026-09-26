using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableCloseableFiscalBusinessDateRepository : ICloseableFiscalBusinessDateRepository
{
    public Task<CloseableFiscalBusinessDatesSnapshot> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CloseableFiscalBusinessDatesSnapshot(
            sitePosServerId,
            fiscalIdentityId,
            currencyCode,
            [],
            "closeable_fiscal_business_dates_unavailable",
            "Fiscal reporting persistence is unavailable."));
}
