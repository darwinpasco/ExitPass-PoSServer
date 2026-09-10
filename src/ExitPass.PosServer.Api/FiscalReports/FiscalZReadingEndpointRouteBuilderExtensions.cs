using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalZReadingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFiscalZReadingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-reports/z-readings");
        group.MapGet("/history", (Guid sitePosServerId, Guid fiscalIdentityId, string? currencyCode,
            FiscalReportingHistoryService service, HttpContext context, CancellationToken ct) =>
            FiscalReportingHistoryEndpoint.ReadAsync(sitePosServerId, fiscalIdentityId, currencyCode, "Z", service, context, ct))
            .RequireAuthorization(FiscalZReadingAuthorization.ReadPolicyName);

        group.MapPost("/", async (CloseFiscalZReadingRequest request, FiscalZReadingService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalZReadingEndpoint.CloseAsync(request, service, context, cancellationToken).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalZReadingAuthorization.ClosePolicyName);

        group.MapGet("/{zReadingReference}", async (string zReadingReference, FiscalZReadingService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalZReadingEndpoint.GetAsync(zReadingReference, service, context, cancellationToken).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalZReadingAuthorization.ReadPolicyName);

        return endpoints;
    }
}
