namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentPersistenceException : InvalidOperationException
{
    public FiscalDocumentPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
