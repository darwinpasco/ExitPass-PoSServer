using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalZCounterResetEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFiscalZCounterResetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/admin/fiscal-z-counter");
        group.MapGet("/", async (Guid sitePosServerId, Guid fiscalIdentityId, string? currencyCode,
            FiscalZCounterResetService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalZCounterResetEndpoint.ReadAsync(
                sitePosServerId, fiscalIdentityId, currencyCode, service, context, cancellationToken).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        })
            .RequireAuthorization(FiscalZCounterResetAuthorization.ReadPolicyName);
        group.MapPost("/reset", async (ResetFiscalZCounterRequest request,
            FiscalZCounterResetService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalZCounterResetEndpoint.ResetAsync(
                request, service, context, cancellationToken).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        })
            .RequireAuthorization(FiscalZCounterResetAuthorization.ResetPolicyName);
        return endpoints;
    }
}
