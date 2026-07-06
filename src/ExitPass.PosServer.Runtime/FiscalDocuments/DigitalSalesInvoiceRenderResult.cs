namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record DigitalSalesInvoiceRenderResult(
    bool Succeeded,
    DigitalSalesInvoiceRenderErrorCode ErrorCode,
    string Message,
    DigitalSalesInvoiceRenderModel? Render = null)
{
    public static DigitalSalesInvoiceRenderResult Success(DigitalSalesInvoiceRenderModel render) =>
        new(true, DigitalSalesInvoiceRenderErrorCode.None, "Digital Sales Invoice render model created.", render);

    public static DigitalSalesInvoiceRenderResult NotFound(Guid fiscalDocumentId) =>
        new(false, DigitalSalesInvoiceRenderErrorCode.NotFound, $"Fiscal document '{fiscalDocumentId}' was not found.");
}

public enum DigitalSalesInvoiceRenderErrorCode
{
    None = 0,
    NotFound = 1
}
