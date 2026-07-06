using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class DigitalSalesInvoiceEndpoint
{
    public static async Task<GetDigitalSalesInvoiceResponse> GetByFiscalDocumentIdAsync(
        Guid fiscalDocumentId,
        DigitalSalesInvoiceRenderService service,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await service.RenderAsync(fiscalDocumentId, cancellationToken).ConfigureAwait(false);
            return MapResult(result);
        }
        catch (FiscalDocumentPersistenceNotConfiguredException ex)
        {
            return new GetDigitalSalesInvoiceResponse(
                false,
                "persistence_not_configured",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentInvalidPersistenceConfigurationException ex)
        {
            return new GetDigitalSalesInvoiceResponse(
                false,
                "invalid_persistence_configuration",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentPersistenceException ex)
        {
            return new GetDigitalSalesInvoiceResponse(
                false,
                "persistence_read_failed",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static GetDigitalSalesInvoiceResponse MapResult(DigitalSalesInvoiceRenderResult result)
    {
        if (result.Succeeded)
        {
            return new GetDigitalSalesInvoiceResponse(
                true,
                "rendered",
                result.Message,
                DigitalSalesInvoiceTemplateContract.Create(),
                result.Render,
                FiscalNumberAssignmentState: result.Render?.FiscalNumberAssignmentState,
                FiscalDocumentStatusCodeId: result.Render?.FiscalDocumentStatusCodeId,
                HttpStatusCode: StatusCodes.Status200OK);
        }

        return new GetDigitalSalesInvoiceResponse(
            false,
            result.ErrorCode == DigitalSalesInvoiceRenderErrorCode.NotFound
                ? "fiscal_document_not_found"
                : "digital_sales_invoice_render_failed",
            result.Message,
            FiscalNumberAssignmentState: "not_assigned",
            HttpStatusCode: result.ErrorCode == DigitalSalesInvoiceRenderErrorCode.NotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest);
    }
}
