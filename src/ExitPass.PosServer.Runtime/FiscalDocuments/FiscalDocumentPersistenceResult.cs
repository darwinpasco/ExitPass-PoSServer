namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentPersistenceResult(
    FiscalDocumentDraft Draft,
    FiscalDocumentPersistenceOutcome Outcome)
{
    public static FiscalDocumentPersistenceResult Created(FiscalDocumentDraft draft) =>
        new(draft, FiscalDocumentPersistenceOutcome.Created);

    public static FiscalDocumentPersistenceResult Replayed(FiscalDocumentDraft draft) =>
        new(draft, FiscalDocumentPersistenceOutcome.Replayed);
}
