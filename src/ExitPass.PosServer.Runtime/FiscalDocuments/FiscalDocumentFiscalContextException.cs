namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentFiscalContextException : Exception
{
    public FiscalDocumentFiscalContextException(
        FiscalDocumentCreationErrorCode errorCode,
        string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public FiscalDocumentCreationErrorCode ErrorCode { get; }
}
