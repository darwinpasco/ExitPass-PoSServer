using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class PersistenceNotConfiguredFiscalDocumentRepository : IFiscalDocumentRepository
{
    public Task<FiscalDocumentDraft> CreateAsync(FiscalDocumentDraft draft, CancellationToken cancellationToken)
    {
        throw new FiscalDocumentPersistenceNotConfiguredException();
    }
}
