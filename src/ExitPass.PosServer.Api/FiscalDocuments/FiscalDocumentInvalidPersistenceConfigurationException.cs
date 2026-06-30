namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class FiscalDocumentInvalidPersistenceConfigurationException : Exception
{
    public FiscalDocumentInvalidPersistenceConfigurationException()
        : base("Fiscal document persistence is configured with an invalid PostgreSQL connection string.")
    {
    }
}
