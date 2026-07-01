using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class PersistenceNotConfiguredFiscalDocumentReader : IFiscalDocumentReader
{
    public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken)
    {
        throw new FiscalDocumentPersistenceNotConfiguredException();
    }
}
