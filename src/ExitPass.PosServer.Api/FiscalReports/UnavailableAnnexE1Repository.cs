using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableAnnexE1Repository : IAnnexE1Repository
{
    private const string Message = "Annex E-1 persistence is unavailable.";
    public Task<AnnexE1FactResult> RecordFactAsync(AnnexE1PeriodFactCommand command,CancellationToken cancellationToken=default)=>Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.PersistenceFailure,SafeMessage:Message));
    public Task<AnnexE1WorkbookResult> GenerateAsync(AnnexE1GenerationCommand command,CancellationToken cancellationToken=default)=>Task.FromResult(new AnnexE1WorkbookResult(AnnexE1Outcome.PersistenceFailure,SafeMessage:Message));
    public Task<AnnexE1WorkbookResult> GetAsync(Guid workbookId,CancellationToken cancellationToken=default)=>Task.FromResult(new AnnexE1WorkbookResult(AnnexE1Outcome.PersistenceFailure,SafeMessage:Message));
    public Task<AnnexE1ArtifactResult> DownloadAsync(Guid workbookId,CancellationToken cancellationToken=default)=>Task.FromResult(new AnnexE1ArtifactResult(AnnexE1Outcome.PersistenceFailure,SafeMessage:Message));
}
