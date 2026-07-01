namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentReadResult(
    bool Succeeded,
    FiscalDocumentReadErrorCode ErrorCode,
    string Message,
    FiscalDocumentReadModel? Document = null)
{
    public static FiscalDocumentReadResult Success(FiscalDocumentReadModel document) =>
        new(true, FiscalDocumentReadErrorCode.None, "Fiscal document found.", document);

    public static FiscalDocumentReadResult NotFound(Guid fiscalDocumentId) =>
        new(false, FiscalDocumentReadErrorCode.NotFound, $"Fiscal document '{fiscalDocumentId}' was not found.");

    public static FiscalDocumentReadResult Failure(FiscalDocumentReadErrorCode errorCode, string message) =>
        new(false, errorCode, message);
}

public enum FiscalDocumentReadErrorCode
{
    None = 0,
    NotFound = 1
}
