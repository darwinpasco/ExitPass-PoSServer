using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableMonthlyESalesRepository : IMonthlyESalesRepository
{
    public Task<MonthlyESalesResult> ReadAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MonthlyESalesResult(
            MonthlyESalesOutcome.PersistenceFailure,
            SafeMessage: "Monthly eSales persistence is unavailable."));

    public Task<MonthlyESalesPdfResult> ReadPdfAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MonthlyESalesPdfResult(
            MonthlyESalesOutcome.PersistenceFailure,
            SafeMessage: "Monthly eSales persistence is unavailable."));
}
