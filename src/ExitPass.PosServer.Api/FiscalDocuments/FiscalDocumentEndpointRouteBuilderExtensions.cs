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

        return group;
    }
}
