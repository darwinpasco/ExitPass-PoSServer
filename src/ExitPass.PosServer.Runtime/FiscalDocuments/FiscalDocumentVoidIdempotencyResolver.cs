namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class FiscalDocumentVoidIdempotencyResolver
{
    public static FiscalDocumentVoidIdempotency Resolve(FiscalDocumentVoidCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return new FiscalDocumentVoidIdempotency(
            $"fiscal_document_void:{command.FiscalDocumentId:N}",
            command.IdempotencyKey,
            FiscalDocumentVoidSemanticRequestHasher.Hash(command));
    }
}
