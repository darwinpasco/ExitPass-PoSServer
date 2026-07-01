namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentReadService
{
    private readonly IFiscalDocumentReader reader;

    public FiscalDocumentReadService(IFiscalDocumentReader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<FiscalDocumentReadResult> GetByIdAsync(
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var document = await reader.GetByIdAsync(fiscalDocumentId, cancellationToken).ConfigureAwait(false);
        return document is null
            ? FiscalDocumentReadResult.NotFound(fiscalDocumentId)
            : FiscalDocumentReadResult.Success(document);
    }
}
