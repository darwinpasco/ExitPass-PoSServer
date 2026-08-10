using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class BirSalesSummaryEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapBirSalesSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-reports/bir-sales-summaries");
        group.MapPost("/", async (GenerateBirSalesSummaryRequest request, BirSalesSummaryService service, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
        {
            var response = await BirSalesSummaryEndpoint.GenerateAsync(request, service, context, environment, ct).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(BirSalesSummaryAuthorization.GeneratePolicyName);
        group.MapGet("/{summaryId:guid}", async (Guid summaryId, BirSalesSummaryService service, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
        {
            var response = await BirSalesSummaryEndpoint.GetAsync(summaryId, service, context, environment, ct).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(BirSalesSummaryAuthorization.ReadPolicyName);
        group.MapGet("/{summaryId:guid}/exports/{format}", BirSalesSummaryEndpoint.ExportAsync)
            .RequireAuthorization(BirSalesSummaryAuthorization.ExportPolicyName);
        return endpoints;
    }
}
