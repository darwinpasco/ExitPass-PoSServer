namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class FiscalIssuanceIdempotencyResolver
{
    public static FiscalIssuanceIdempotency Resolve(FiscalDocumentCreationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.PayableBasis);

        var key = command.PayableBasis.UpstreamFinalityRef.Trim();
        var scope = string.Join(
            ":",
            "fiscal_document_creation",
            command.SitePosServerId!.Value.ToString("N"),
            command.FiscalDocumentTypeCodeId!.Value.ToString("N"));
        var hash = FiscalDocumentSemanticRequestHasher.Hash(command);

        return new FiscalIssuanceIdempotency(scope, key, hash);
    }
}
