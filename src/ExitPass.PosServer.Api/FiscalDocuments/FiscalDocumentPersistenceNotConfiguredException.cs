namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class FiscalDocumentPersistenceNotConfiguredException : InvalidOperationException
{
    public FiscalDocumentPersistenceNotConfiguredException()
        : base("Fiscal document persistence is not configured for this POS Server runtime.")
    {
    }
}
