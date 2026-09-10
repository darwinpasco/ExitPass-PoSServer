namespace ExitPass.PosServer.Runtime.ElectronicJournal;

public sealed record ElectronicJournalInvoiceTextItem(
    Guid FiscalDocumentId,
    string FiscalDocumentNumber,
    DateOnly BusinessDayDate,
    DateTimeOffset IssuedAt,
    string PrintableText);

public sealed record ElectronicJournalInvoiceTextReadResult(
    ElectronicJournalOutcome Outcome,
    IReadOnlyList<ElectronicJournalInvoiceTextItem>? Invoices = null,
    ElectronicJournalInvoiceTextExport? Export = null,
    string? SafeMessage = null);

public sealed class ElectronicJournalInvoiceTextService(
    IElectronicJournalRepository repository,
    ElectronicJournalInvoiceTextRenderer exportRenderer)
{
    public async Task<ElectronicJournalInvoiceTextReadResult> ReadAsync(
        ElectronicJournalQuery query,
        bool createExport,
        CancellationToken cancellationToken = default)
    {
        var eventsResult = await repository.ReadAsync(query with
        {
            EventType = "fiscal_document_committed",
            PageSize = ElectronicJournalContract.MaximumExportEvents,
            Cursor = null
        }, cancellationToken).ConfigureAwait(false);
        if (eventsResult.Page is null)
            return new(eventsResult.Outcome, SafeMessage: eventsResult.SafeMessage);
        if (eventsResult.Page.NextCursor is not null)
            return new(ElectronicJournalOutcome.RangeTooLarge, SafeMessage: "The requested Electronic Journal exceeds the governed invoice limit.");

        var invoices = new List<ElectronicJournalInvoiceTextItem>();
        foreach (var journalEvent in eventsResult.Page.Events
            .Where(value => value.FiscalDocumentId.HasValue)
            .GroupBy(value => value.FiscalDocumentId!.Value)
            .Select(group => group.OrderBy(value => value.StreamSequence).First())
            .OrderBy(value => value.StreamSequence))
        {
            if (string.IsNullOrEmpty(journalEvent.PrintableSalesInvoiceText) || journalEvent.BusinessDayDate is null ||
                !journalEvent.Facts.TryGetValue("fiscal_document_number", out var number) || string.IsNullOrWhiteSpace(number))
                return new(ElectronicJournalOutcome.IntegrityFailure, SafeMessage: "A committed Sales Invoice has no immutable canonical printable text payload.");
            invoices.Add(new(journalEvent.FiscalDocumentId.GetValueOrDefault(), number, journalEvent.BusinessDayDate.GetValueOrDefault(),
                journalEvent.EffectiveAt, journalEvent.PrintableSalesInvoiceText));
        }

        var ordered = invoices.OrderBy(value => value.IssuedAt).ThenBy(value => value.FiscalDocumentNumber, StringComparer.Ordinal).ToArray();
        var export = createExport && ordered.Length > 0 ? exportRenderer.RenderPersisted(ordered) : null;
        return new(ElectronicJournalOutcome.Success, ordered, export);
    }
}
