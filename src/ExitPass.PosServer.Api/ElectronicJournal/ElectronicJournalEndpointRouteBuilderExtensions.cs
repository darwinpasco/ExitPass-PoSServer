using Microsoft.AspNetCore.Mvc;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.ElectronicJournal;

public static class ElectronicJournalEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapElectronicJournalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/electronic-journal");
        group.MapGet("/invoice-text", ([AsParameters] ElectronicJournalFilterRequest request,
            ElectronicJournalInvoiceTextService service, ElectronicJournalService accessService,
            FiscalReportArtifactService artifactService, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.ReadInvoiceTextAsync(request, service, accessService, artifactService, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.ReadPolicyName);
        group.MapGet("/invoice-text.txt", ([AsParameters] ElectronicJournalFilterRequest request,
            ElectronicJournalInvoiceTextService service, ElectronicJournalService accessService,
            FiscalReportArtifactService artifactService, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.ExportInvoiceTextAsync(request, service, accessService, artifactService, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.ExportPolicyName);
        group.MapGet("/events", ([AsParameters] ElectronicJournalFilterRequest request, ElectronicJournalService service,
            FiscalReportArtifactService artifactService, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.ReadAsync(request, service, artifactService, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.ReadPolicyName);
        group.MapGet("/exports/{format}", (string format, [AsParameters] ElectronicJournalFilterRequest request,
            ElectronicJournalService service, FiscalReportArtifactService artifactService,
            HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.ExportAsync(format, request, service, artifactService, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.ExportPolicyName);
        group.MapPost("/integrity-verifications", (VerifyElectronicJournalIntegrityRequest request,
            ElectronicJournalService service, HttpContext context, IHostEnvironment environment, CancellationToken ct) =>
            ElectronicJournalEndpoint.VerifyAsync(request, service, context, environment, ct))
            .RequireAuthorization(ElectronicJournalAuthorization.IntegrityPolicyName);
        return endpoints;
    }
}
