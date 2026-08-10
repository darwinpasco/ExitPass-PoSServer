using ExitPass.PosServer.Runtime.ElectronicJournal;

namespace ExitPass.PosServer.Api.ElectronicJournal;

public sealed class UnavailableElectronicJournalRepository : IElectronicJournalRepository
{
    public Task<ElectronicJournalPageResult> ReadAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ElectronicJournalPageResult(ElectronicJournalOutcome.PersistenceFailure, SafeMessage: "Electronic Journal persistence is unavailable."));

    public Task<ElectronicJournalIntegrityOutcome> VerifyIntegrityAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ElectronicJournalIntegrityOutcome(ElectronicJournalOutcome.PersistenceFailure, SafeMessage: "Electronic Journal persistence is unavailable."));

    public Task RecordAccessAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode, string action, string result,
        string actorReference, string serviceIdentityReference, string correlationReference, string supportReference,
        int eventCount, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
