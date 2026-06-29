namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public interface IFiscalDocumentRepository
{
    Task<FiscalDocumentDraft> CreateAsync(FiscalDocumentDraft draft, CancellationToken cancellationToken);
}
