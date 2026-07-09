using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class InvalidPersistenceConfigurationFiscalDocumentRepository : IFiscalDocumentRepository
{
    public Task<FiscalDocumentPersistenceResult> CreateAsync(
        FiscalDocumentDraft draft,
        FiscalIssuanceIdempotency idempotency,
        CancellationToken cancellationToken)
    {
        throw new FiscalDocumentInvalidPersistenceConfigurationException();
    }

    public Task<FiscalDocumentVoidPersistenceResult> VoidAsync(
        FiscalDocumentVoidCommand command,
        FiscalDocumentVoidIdempotency idempotency,
        CancellationToken cancellationToken)
    {
        throw new FiscalDocumentInvalidPersistenceConfigurationException();
    }
}
