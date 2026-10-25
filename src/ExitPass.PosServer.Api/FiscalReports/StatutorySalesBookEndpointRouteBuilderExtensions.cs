namespace ExitPass.PosServer.Api.FiscalReports;

public static class StatutorySalesBookEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapStatutorySalesBookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-reports/annex-e/{annex}");
        group.MapGet("/", StatutorySalesBookEndpoint.ReadAsync).RequireAuthorization(StatutorySalesBookAuthorization.ReadPolicyName);
        group.MapGet("/download", StatutorySalesBookEndpoint.DownloadAsync).RequireAuthorization(StatutorySalesBookAuthorization.ExportPolicyName);
        return endpoints;
    }
}
