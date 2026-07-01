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
}
