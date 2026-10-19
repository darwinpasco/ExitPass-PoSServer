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

        group.MapPost("/", async (GenerateFiscalXReadingRequest request, FiscalXReadingService service,
            FiscalReportPresentationService presentationService, FiscalReportOutputRenderer renderer,
            FiscalReportArtifactService artifactService, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalXReadingEndpoint.GenerateAsync(request, service, context, cancellationToken).ConfigureAwait(false);
            if (response.Succeeded && response.XReading is { } reading)
            {
                var presented = presentationService.Present(reading);
                if (!presented.Succeeded || presented.Presentation is null)
                    response = response with { Succeeded = false, Code = "fiscal_report_artifact_unavailable", Message = presented.SafeMessage, HttpStatusCode = 503 };
                else
                {
                    var model = presented.Presentation;
                    var persisted = await artifactService.GetOrCreateAsync(new(
                            model.SitePosServerId, model.FiscalIdentityId, model.CurrencyCode,
                            model.ReportKind, model.ReportReference, "presentationjson", model.PresentationContractVersion,
                            FiscalReportArtifactContract.SourceFingerprint(model), model.FiscalReportingPeriodId,
                            BusinessDayDate: model.BusinessDayDate),
                        _ =>
                        {
                            var output = renderer.CreatePresentationJson(model);
                            return Task.FromResult(new FiscalReportArtifactPayload(output.Content, output.ContentType, output.FileName));
                        },
                        cancellationToken).ConfigureAwait(false);
                    if (!persisted.Succeeded)
                        response = response with
                        {
                            Succeeded = false,
                            Code = "fiscal_report_artifact_unavailable",
                            Message = persisted.SafeMessage ?? "The X Reading artifact could not be persisted safely.",
                            HttpStatusCode = 503
                        };
                }
            }
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
