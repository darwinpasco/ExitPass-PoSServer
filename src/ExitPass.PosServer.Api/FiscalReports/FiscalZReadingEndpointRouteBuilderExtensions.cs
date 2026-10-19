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

        group.MapGet("/closeable-periods", (Guid sitePosServerId, Guid fiscalIdentityId, string? currencyCode,
            CloseableFiscalBusinessDateService service, HttpContext context, CancellationToken ct) =>
            CloseableFiscalBusinessDateEndpoint.ReadAsync(
                sitePosServerId, fiscalIdentityId, currencyCode, service, context, ct))
            .RequireAuthorization(FiscalZReadingAuthorization.ReadPolicyName);

        group.MapPost("/", async (CloseFiscalZReadingRequest request, FiscalZReadingService service,
            FiscalReportPresentationService presentationService, FiscalReportOutputRenderer renderer,
            BirSalesSummaryService summaryService, BirSalesSummaryOutputRenderer summaryRenderer,
            FiscalReportArtifactService artifactService, HttpContext context, CancellationToken cancellationToken) =>
        {
            var response = await FiscalZReadingEndpoint.CloseAsync(request, service, context, cancellationToken).ConfigureAwait(false);
            if (response.Succeeded && response.ZReading is { } reading)
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
                            Message = persisted.SafeMessage ?? "The Z Reading artifact could not be persisted safely.",
                            HttpStatusCode = 503
                        };
                }
            }
            if (response.Succeeded && response.BirSalesSummaryCompletion?.SummaryId is { } summaryId)
            {
                var summaryResult = await summaryService.GetByIdAsync(summaryId, cancellationToken).ConfigureAwait(false);
                if (summaryResult.Record is not { } summary)
                {
                    response = response with
                    {
                        Succeeded = false,
                        Code = "fiscal_report_artifact_unavailable",
                        Message = "The committed Daily BIR Sales Summary could not be read for artifact persistence.",
                        HttpStatusCode = 503
                    };
                }
                else
                {
                    var persisted = await artifactService.GetOrCreateAsync(new(
                            summary.SitePosServerId, summary.FiscalIdentityId, summary.CurrencyCode,
                            "DAILY_BIR_SALES_SUMMARY", summary.BirSalesSummaryReportId.ToString("D"), "json",
                            BirSalesSummaryContract.ExportVersion, FiscalReportArtifactContract.SourceFingerprint(summary),
                            summary.FiscalReportingPeriodId, BusinessDayDate: summary.BusinessDayDate),
                        _ =>
                        {
                            var output = summaryRenderer.Render(summary, BirSalesSummaryExportFormat.Json);
                            return Task.FromResult(new FiscalReportArtifactPayload(output.Bytes, output.ContentType, output.FileName));
                        }, cancellationToken).ConfigureAwait(false);
                    if (!persisted.Succeeded)
                        response = response with
                        {
                            Succeeded = false,
                            Code = "fiscal_report_artifact_unavailable",
                            Message = persisted.SafeMessage ?? "The Daily BIR Sales Summary artifact could not be persisted safely.",
                            HttpStatusCode = 503
                        };
                }
            }
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
