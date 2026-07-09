using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapFiscalDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-documents");

        group.MapPost("/", async (
            CreateFiscalDocumentRequest request,
            FiscalDocumentCreationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalDocumentCreationEndpoint.CreateAsync(request, service, cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        group.MapGet("/{fiscalDocumentId:guid}", async (
            Guid fiscalDocumentId,
            FiscalDocumentReadService service,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalDocumentReadEndpoint.GetByIdAsync(fiscalDocumentId, service, cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        group.MapPost("/{fiscalDocumentId:guid}/void", async (
            Guid fiscalDocumentId,
            VoidFiscalDocumentRequest request,
            FiscalDocumentVoidService service,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalDocumentVoidEndpoint
                .VoidAsync(fiscalDocumentId, request, service, cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        group.MapGet("/{fiscalDocumentId:guid}/digital-sales-invoice", async (
            Guid fiscalDocumentId,
            DigitalSalesInvoiceRenderService service,
            CancellationToken cancellationToken) =>
        {
            var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(
                    fiscalDocumentId,
                    service,
                    cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        group.MapGet("/{fiscalDocumentId:guid}/digital-sales-invoice/presentation", async (
            Guid fiscalDocumentId,
            DigitalSalesInvoiceRenderService renderService,
            DigitalSalesInvoicePresentationAdapter presentationAdapter,
            CancellationToken cancellationToken) =>
        {
            var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
                    fiscalDocumentId,
                    renderService,
                    presentationAdapter,
                    cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        return group;
    }
}
