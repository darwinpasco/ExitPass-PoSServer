using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class InvalidPersistenceConfigurationFiscalDocumentReader : IFiscalDocumentReader
{
    public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken)
    {
        throw new FiscalDocumentInvalidPersistenceConfigurationException();
    }
}
