using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalZCloseStateInitializationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFiscalZCloseStateInitializationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/admin/fiscal-z-close-states/initialize", async (
            InitializeFiscalZCloseStateRequest request,
            FiscalZCloseStateInitializationService service,
            HttpContext context,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalZCloseStateInitializationEndpoint
                .InitializeAsync(
                    request,
                    service,
                    context,
                    cancellationToken,
                    loggerFactory.CreateLogger("FiscalZCloseStateInitialization"))
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalZCloseStateInitializationAuthorization.PolicyName);
        return endpoints;
    }
}
