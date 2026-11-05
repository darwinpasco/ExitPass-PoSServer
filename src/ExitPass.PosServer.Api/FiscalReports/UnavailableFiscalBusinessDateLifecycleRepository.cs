using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableFiscalBusinessDateLifecycleRepository : IFiscalBusinessDateLifecycleRepository
{
    public Task<FiscalBusinessDateLifecycleSnapshot> ReadAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromException<FiscalBusinessDateLifecycleSnapshot>(new InvalidOperationException("Fiscal business-date lifecycle persistence is unavailable."));

    public Task<OpenFiscalBusinessDateResult> OpenAsync(OpenFiscalBusinessDateCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(new OpenFiscalBusinessDateResult(OpenFiscalBusinessDateOutcome.PersistenceFailure, SafeMessage: "Fiscal business-date lifecycle persistence is unavailable."));
}
