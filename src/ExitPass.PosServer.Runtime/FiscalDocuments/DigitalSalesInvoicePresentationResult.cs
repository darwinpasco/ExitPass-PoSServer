namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record DigitalSalesInvoicePresentationResult(
    bool Succeeded,
    DigitalSalesInvoicePresentationErrorCode ErrorCode,
    string Message,
    DigitalSalesInvoicePresentationModel? Presentation = null)
{
    public static DigitalSalesInvoicePresentationResult Success(DigitalSalesInvoicePresentationModel presentation) =>
        new(true, DigitalSalesInvoicePresentationErrorCode.None, "Digital Sales Invoice presentation model created.", presentation);

    public static DigitalSalesInvoicePresentationResult NotFound(Guid fiscalDocumentId) =>
        new(false, DigitalSalesInvoicePresentationErrorCode.NotFound, $"Fiscal document '{fiscalDocumentId}' was not found.");

    public static DigitalSalesInvoicePresentationResult InvalidSource(string message) =>
        new(false, DigitalSalesInvoicePresentationErrorCode.InvalidSource, message);
}

public enum DigitalSalesInvoicePresentationErrorCode
{
    None = 0,
    NotFound = 1,
    InvalidSource = 2
}
