using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class InvalidPersistenceConfigurationFiscalDocumentRepository : IFiscalDocumentRepository
{
    public Task<FiscalDocumentDraft> CreateAsync(FiscalDocumentDraft draft, CancellationToken cancellationToken)
    {
        throw new FiscalDocumentInvalidPersistenceConfigurationException();
    }
}
