using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableFiscalReportingHistoryRepository : IFiscalReportingHistoryRepository
{
    public Task<FiscalReportingHistorySnapshot> ReadAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode,
        int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult(new FiscalReportingHistorySnapshot(null, [], [], "fiscal_reporting_history_unavailable",
            "Fiscal reporting persistence is unavailable."));
}
