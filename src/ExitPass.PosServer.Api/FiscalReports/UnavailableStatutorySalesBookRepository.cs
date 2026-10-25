using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableStatutorySalesBookRepository : IStatutorySalesBookRepository
{
    public Task<StatutorySalesBookResult> ReadAsync(StatutorySalesBookQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new StatutorySalesBookResult(StatutorySalesBookOutcome.PersistenceFailure, SafeMessage: "Statutory sales-book persistence is unavailable."));
}
