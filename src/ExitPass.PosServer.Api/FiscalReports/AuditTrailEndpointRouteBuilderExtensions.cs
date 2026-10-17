namespace ExitPass.PosServer.Api.FiscalReports;

public static class AuditTrailEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuditTrailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-reports/audit-trail");
        group.MapGet("/", AuditTrailEndpoint.ReadAsync).RequireAuthorization(AuditTrailAuthorization.ReadPolicyName);
        group.MapGet("/download", AuditTrailEndpoint.ExportAsync).RequireAuthorization(AuditTrailAuthorization.ExportPolicyName);
        return endpoints;
    }
}
