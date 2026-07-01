namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public interface IFiscalDocumentRepository
{
    Task<FiscalDocumentPersistenceResult> CreateAsync(
        FiscalDocumentDraft draft,
        FiscalIssuanceIdempotency idempotency,
        CancellationToken cancellationToken);
}
