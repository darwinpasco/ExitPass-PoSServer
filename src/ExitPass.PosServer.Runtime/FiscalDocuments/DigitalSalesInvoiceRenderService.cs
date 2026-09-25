namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class DigitalSalesInvoiceRenderService
{
    private readonly FiscalDocumentReadService readService;

    public DigitalSalesInvoiceRenderService(FiscalDocumentReadService readService)
    {
        this.readService = readService ?? throw new ArgumentNullException(nameof(readService));
    }

    public async Task<DigitalSalesInvoiceRenderResult> RenderAsync(
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var readResult = await readService.GetByIdAsync(fiscalDocumentId, cancellationToken).ConfigureAwait(false);
        if (!readResult.Succeeded || readResult.Document is null)
        {
            return DigitalSalesInvoiceRenderResult.NotFound(fiscalDocumentId);
        }

        return DigitalSalesInvoiceRenderResult.Success(
            DigitalSalesInvoiceRenderModelFactory.Create(readResult.Document));
    }
}
