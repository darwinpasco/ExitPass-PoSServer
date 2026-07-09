using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class PersistenceNotConfiguredFiscalDocumentRepository : IFiscalDocumentRepository
{
    public Task<FiscalDocumentPersistenceResult> CreateAsync(
        FiscalDocumentDraft draft,
        FiscalIssuanceIdempotency idempotency,
        CancellationToken cancellationToken)
    {
        throw new FiscalDocumentPersistenceNotConfiguredException();
    }

    public Task<FiscalDocumentVoidPersistenceResult> VoidAsync(
        FiscalDocumentVoidCommand command,
        FiscalDocumentVoidIdempotency idempotency,
        CancellationToken cancellationToken)
    {
        throw new FiscalDocumentPersistenceNotConfiguredException();
    }
}
