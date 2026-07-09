namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentVoidIdempotencyConflictException : InvalidOperationException
{
    public FiscalDocumentVoidIdempotencyConflictException()
        : base("Fiscal document void request idempotency key was reused with different semantic facts.")
    {
    }
}

public sealed class FiscalDocumentVoidNotFoundException : InvalidOperationException
{
    public FiscalDocumentVoidNotFoundException()
        : base("Fiscal document was not found.")
    {
    }
}

public sealed class FiscalDocumentVoidInvalidStateException : InvalidOperationException
{
    public FiscalDocumentVoidInvalidStateException(string message)
        : base(message)
    {
    }
}
