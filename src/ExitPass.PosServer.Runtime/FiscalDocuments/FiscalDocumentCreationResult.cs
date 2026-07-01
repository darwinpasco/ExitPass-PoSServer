namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentCreationResult(
    bool Succeeded,
    FiscalDocumentCreationErrorCode ErrorCode,
    string Message,
    FiscalDocumentDraft? Draft,
    bool Replayed = false)
{
    public static FiscalDocumentCreationResult Success(FiscalDocumentDraft draft) =>
        new(true, FiscalDocumentCreationErrorCode.None, "Fiscal document creation accepted for persistence.", draft);

    public static FiscalDocumentCreationResult Replay(FiscalDocumentDraft draft) =>
        new(true, FiscalDocumentCreationErrorCode.None, "Fiscal document creation replayed from idempotency record.", draft, true);

    public static FiscalDocumentCreationResult Failure(FiscalDocumentCreationErrorCode errorCode, string message) =>
        new(false, errorCode, message, null);
}
