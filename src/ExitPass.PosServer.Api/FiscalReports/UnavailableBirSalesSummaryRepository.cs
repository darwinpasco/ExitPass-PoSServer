using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableBirSalesSummaryRepository : IBirSalesSummaryRepository
{
    public Task<BirSalesSummaryResult> GenerateAsync(BirSalesSummaryCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BirSalesSummaryResult(BirSalesSummaryOutcome.PersistenceFailure,
            SafeMessage: "BIR sales-summary persistence is unavailable."));

    public Task<BirSalesSummaryResult> GetByIdAsync(Guid birSalesSummaryReportId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BirSalesSummaryResult(BirSalesSummaryOutcome.PersistenceFailure,
            SafeMessage: "BIR sales-summary persistence is unavailable."));
}
