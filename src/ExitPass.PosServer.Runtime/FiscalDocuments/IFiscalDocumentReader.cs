namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public interface IFiscalDocumentReader
{
    Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken);
}
