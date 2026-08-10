namespace ExitPass.PosServer.Api.FiscalReports;

public static class AnnexE1EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAnnexE1Endpoints(this IEndpointRouteBuilder endpoints)
    {
        var group=endpoints.MapGroup("/v1/fiscal-reports/annex-e/e1");
        group.MapPost("/accounting-facts",async(AnnexE1FactRequest request,Runtime.FiscalReports.AnnexE1Service service,HttpContext context,IHostEnvironment environment,CancellationToken ct)=>{var response=await AnnexE1Endpoint.RecordFactAsync(request,service,context,environment,false,ct);return Results.Json(response,statusCode:response.HttpStatusCode);}).RequireAuthorization(AnnexE1Authorization.RecordFactPolicy);
        group.MapPost("/known-zero-attestations",async(AnnexE1FactRequest request,Runtime.FiscalReports.AnnexE1Service service,HttpContext context,IHostEnvironment environment,CancellationToken ct)=>{var response=await AnnexE1Endpoint.RecordFactAsync(request,service,context,environment,true,ct);return Results.Json(response,statusCode:response.HttpStatusCode);}).RequireAuthorization(AnnexE1Authorization.AttestZeroPolicy);
        group.MapPost("/workbooks",async(AnnexE1GenerationRequest request,Runtime.FiscalReports.AnnexE1Service service,HttpContext context,IHostEnvironment environment,CancellationToken ct)=>{var response=await AnnexE1Endpoint.GenerateAsync(request,service,context,environment,ct);return Results.Json(response,statusCode:response.HttpStatusCode);}).RequireAuthorization(AnnexE1Authorization.GeneratePolicy);
        group.MapGet("/workbooks/{id:guid}",async(Guid id,Runtime.FiscalReports.AnnexE1Service service,HttpContext context,IHostEnvironment environment,CancellationToken ct)=>{var response=await AnnexE1Endpoint.GetAsync(id,service,context,environment,ct);return Results.Json(response,statusCode:response.HttpStatusCode);}).RequireAuthorization(AnnexE1Authorization.ReadPolicy);
        group.MapGet("/workbooks/{id:guid}/content",AnnexE1Endpoint.DownloadAsync).RequireAuthorization(AnnexE1Authorization.DownloadPolicy);
        return endpoints;
    }
}
