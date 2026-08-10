namespace ExitPass.PosServer.Runtime.ElectronicJournal;

public sealed class ElectronicJournalService(
    IElectronicJournalRepository repository,
    ElectronicJournalExportRenderer renderer)
{
    public Task<ElectronicJournalPageResult> ReadAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default) =>
        repository.ReadAsync(query, cancellationToken);

    public Task<ElectronicJournalIntegrityOutcome> VerifyIntegrityAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default) =>
        repository.VerifyIntegrityAsync(query, cancellationToken);

    public async Task<(ElectronicJournalPageResult Result, ElectronicJournalExport? Export)> ExportAsync(
        ElectronicJournalQuery query,
        ElectronicJournalExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.ReadAsync(
            query with { PageSize = ElectronicJournalContract.MaximumExportEvents }, cancellationToken).ConfigureAwait(false);
        if (result.Page is null) return (result, null);
        if (result.Page.NextCursor is not null)
            return (new(ElectronicJournalOutcome.RangeTooLarge, SafeMessage: "The requested Electronic Journal export exceeds the governed event limit."), null);
        return (result, renderer.Render(result.Page, query, format));
    }

    public Task RecordAccessAsync(
        Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode,
        string action, string result, string actorReference, string serviceIdentityReference,
        string correlationReference, string supportReference, int eventCount,
        CancellationToken cancellationToken = default) =>
        repository.RecordAccessAsync(sitePosServerId, fiscalIdentityId, currencyCode, action, result,
            actorReference, serviceIdentityReference, correlationReference, supportReference, eventCount, cancellationToken);
}
