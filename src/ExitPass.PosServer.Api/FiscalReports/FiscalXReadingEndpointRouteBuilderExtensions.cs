using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalXReadingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFiscalXReadingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-reports/x-readings");
        group.MapGet("/history", (Guid sitePosServerId, Guid fiscalIdentityId, string? currencyCode,
            FiscalReportingHistoryService service, HttpContext context, CancellationToken ct) =>
            FiscalReportingHistoryEndpoint.ReadAsync(sitePosServerId, fiscalIdentityId, currencyCode, "X", service, context, ct))
            .RequireAuthorization(FiscalXReadingAuthorization.ReadPolicyName);

        group.MapPost("/", async (GenerateFiscalXReadingRequest request, FiscalXReadingService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalXReadingEndpoint.GenerateAsync(request, service, context, cancellationToken).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalXReadingAuthorization.GeneratePolicyName);

        group.MapGet("/{fiscalReportReference}", async (string fiscalReportReference, FiscalXReadingService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalXReadingEndpoint.GetAsync(fiscalReportReference, service, context, cancellationToken).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalXReadingAuthorization.ReadPolicyName);

        return endpoints;
    }
}
