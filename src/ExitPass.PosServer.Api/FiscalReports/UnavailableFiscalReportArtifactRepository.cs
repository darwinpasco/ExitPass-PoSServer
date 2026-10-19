using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableFiscalReportArtifactRepository : IFiscalReportArtifactRepository
{
    public Task<FiscalReportArtifactResult> GetOrCreateAsync(
        FiscalReportArtifactCommand command,
        Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new FiscalReportArtifactResult(
            FiscalReportArtifactOutcome.PersistenceUnavailable,
            SafeMessage: "Persistent fiscal report artifact storage is unavailable."));
}
