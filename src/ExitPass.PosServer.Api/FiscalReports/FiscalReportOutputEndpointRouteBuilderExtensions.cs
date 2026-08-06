using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalReportOutputEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFiscalReportOutputEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var x = endpoints.MapGroup("/v1/fiscal-reports/x-readings");
        x.MapGet("/{fiscalReportReference}/presentation", async (
            string fiscalReportReference,
            FiscalXReadingService readService,
            FiscalReportPresentationService presentationService,
            FiscalReportOutputRenderer renderer,
            HttpContext context,
            ILogger<FiscalReportOutputAudit> logger,
            CancellationToken cancellationToken) =>
            await FiscalReportOutputEndpoint.GetXPresentationAsync(
                fiscalReportReference, readService, presentationService, renderer, context, logger, cancellationToken))
            .RequireAuthorization(FiscalXReadingAuthorization.ReadPolicyName);
        x.MapGet("/{fiscalReportReference}/exports/{format}", async (
            string fiscalReportReference,
            string format,
            string? width,
            FiscalXReadingService readService,
            FiscalReportPresentationService presentationService,
            FiscalReportOutputRenderer renderer,
            HttpContext context,
            ILogger<FiscalReportOutputAudit> logger,
            CancellationToken cancellationToken) =>
            await FiscalReportOutputEndpoint.GetXExportAsync(
                fiscalReportReference, format, width, readService, presentationService, renderer, context, logger, cancellationToken))
            .RequireAuthorization(FiscalReportOutputAuthorization.XExportPolicyName);

        var z = endpoints.MapGroup("/v1/fiscal-reports/z-readings");
        z.MapGet("/{zReadingReference}/presentation", async (
            string zReadingReference,
            FiscalZReadingService readService,
            FiscalReportPresentationService presentationService,
            FiscalReportOutputRenderer renderer,
            HttpContext context,
            ILogger<FiscalReportOutputAudit> logger,
            CancellationToken cancellationToken) =>
            await FiscalReportOutputEndpoint.GetZPresentationAsync(
                zReadingReference, readService, presentationService, renderer, context, logger, cancellationToken))
            .RequireAuthorization(FiscalZReadingAuthorization.ReadPolicyName);
        z.MapGet("/{zReadingReference}/exports/{format}", async (
            string zReadingReference,
            string format,
            string? width,
            FiscalZReadingService readService,
            FiscalReportPresentationService presentationService,
            FiscalReportOutputRenderer renderer,
            HttpContext context,
            ILogger<FiscalReportOutputAudit> logger,
            CancellationToken cancellationToken) =>
            await FiscalReportOutputEndpoint.GetZExportAsync(
                zReadingReference, format, width, readService, presentationService, renderer, context, logger, cancellationToken))
            .RequireAuthorization(FiscalReportOutputAuthorization.ZExportPolicyName);

        return endpoints;
    }
}
