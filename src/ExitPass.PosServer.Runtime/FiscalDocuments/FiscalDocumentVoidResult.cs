namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentVoidResult(
    bool Succeeded,
    FiscalDocumentVoidErrorCode ErrorCode,
    string Message,
    FiscalDocumentVoidRecord? Record = null,
    string? ResultClassification = null)
{
    public static FiscalDocumentVoidResult Success(
        FiscalDocumentVoidRecord record,
        string resultClassification) =>
        new(true, FiscalDocumentVoidErrorCode.None, "Fiscal document void was recorded.", record, resultClassification);

    public static FiscalDocumentVoidResult Failure(
        FiscalDocumentVoidErrorCode errorCode,
        string message,
        string resultClassification = "rejected") =>
        new(false, errorCode, message, ResultClassification: resultClassification);
}
