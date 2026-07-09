namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentVoidPersistenceResult(
    FiscalDocumentVoidRecord Record,
    FiscalDocumentVoidPersistenceOutcome Outcome)
{
    public static FiscalDocumentVoidPersistenceResult NewlyVoided(FiscalDocumentVoidRecord record) =>
        new(record, FiscalDocumentVoidPersistenceOutcome.NewlyVoided);

    public static FiscalDocumentVoidPersistenceResult Replayed(FiscalDocumentVoidRecord record) =>
        new(record, FiscalDocumentVoidPersistenceOutcome.Replayed);

    public static FiscalDocumentVoidPersistenceResult AlreadyVoided(FiscalDocumentVoidRecord record) =>
        new(record, FiscalDocumentVoidPersistenceOutcome.AlreadyVoided);
}

public enum FiscalDocumentVoidPersistenceOutcome
{
    NewlyVoided,
    Replayed,
    AlreadyVoided
}
