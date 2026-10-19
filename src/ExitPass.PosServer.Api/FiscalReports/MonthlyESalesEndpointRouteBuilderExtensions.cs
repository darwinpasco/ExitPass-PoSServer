using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class MonthlyESalesEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMonthlyESalesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-reports/monthly-esales");
        group.MapGet("/", async ([AsParameters] MonthlyESalesRequest request, MonthlyESalesService service,
            MonthlyESalesOutputRenderer renderer, FiscalReportArtifactService artifactService,
            HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
        {
            var response = await MonthlyESalesEndpoint.ReadAsync(request, service, renderer, artifactService, context, environment, ct).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(MonthlyESalesAuthorization.ReadPolicyName);
        group.MapGet("/download", MonthlyESalesEndpoint.ExportAsync)
            .RequireAuthorization(MonthlyESalesAuthorization.ExportPolicyName);
        return endpoints;
    }
}
