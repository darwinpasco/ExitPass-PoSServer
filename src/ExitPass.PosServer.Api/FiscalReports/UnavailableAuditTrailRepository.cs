using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableAuditTrailRepository : IAuditTrailRepository
{
    public Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuditTrailResult(AuditTrailOutcome.PersistenceFailure, SafeMessage: "Audit Trail persistence is unavailable."));

    public Task RecordExportAsync(Guid sitePosServerId, string reportKind, string subjectReference, string format,
        string actorReference, string correlationReference, string contentSha256, int rowCount,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Audit Trail persistence is unavailable.");
}
