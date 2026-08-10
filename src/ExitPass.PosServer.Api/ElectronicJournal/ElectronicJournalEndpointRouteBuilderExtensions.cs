using Microsoft.AspNetCore.Mvc;
using ExitPass.PosServer.Runtime.ElectronicJournal;

namespace ExitPass.PosServer.Api.ElectronicJournal;

public static class ElectronicJournalEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapElectronicJournalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/electronic-journal");
        group.MapGet("/events", ([AsParameters] ElectronicJournalFilterRequest request, ElectronicJournalService service,
            HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.ReadAsync(request, service, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.ReadPolicyName);
        group.MapGet("/exports/{format}", (string format, [AsParameters] ElectronicJournalFilterRequest request,
            ElectronicJournalService service, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.ExportAsync(format, request, service, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.ExportPolicyName);
        group.MapPost("/integrity-verifications", (VerifyElectronicJournalIntegrityRequest request,
            ElectronicJournalService service, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.VerifyAsync(request, service, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.IntegrityPolicyName);
        return endpoints;
    }
}
