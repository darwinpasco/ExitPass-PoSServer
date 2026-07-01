namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentIdempotencyConflictException : Exception
{
    public FiscalDocumentIdempotencyConflictException()
        : base("Fiscal document issuance idempotency key was reused with a different semantic request.")
    {
    }
}
