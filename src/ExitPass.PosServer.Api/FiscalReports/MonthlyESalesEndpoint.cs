using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;
using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record MonthlyESalesRequest(Guid? SitePosServerId, Guid? FiscalIdentityId, string? CurrencyCode, int? CalendarYear, int? CalendarMonth);
public sealed record MonthlyESalesApiResponse(bool Succeeded, string Code, string CorrelationId, int HttpStatusCode, MonthlyESalesReport? Report = null, string? Message = null);
public sealed class MonthlyESalesAudit;

public static class MonthlyESalesEndpoint
{
    public static async Task<MonthlyESalesApiResponse> ReadAsync(
        [AsParameters] MonthlyESalesRequest request,
        MonthlyESalesService service,
        MonthlyESalesOutputRenderer renderer,
        FiscalReportArtifactService artifactService,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var correlation = Correlation(context);
        if (correlation is null) return Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable");
        if (!Authorized(request, context, environment)) return Error("monthly_esales_not_found", 404, "The requested Monthly eSales report is unavailable.", correlation);
        var result = await service.ReadAsync(Query(request), cancellationToken).ConfigureAwait(false);
        if (result.Report is null) return Map(result, correlation);
        var report = result.Report;
        var persisted = await artifactService.GetOrCreateAsync(new(
            report.SitePosServerId, report.FiscalIdentityId, report.CurrencyCode, "MONTHLY_ESALES",
            $"{report.CalendarYear:D4}-{report.CalendarMonth:D2}", "json", MonthlyESalesContract.JsonExportVersion,
            FiscalReportArtifactContract.SourceFingerprint(report), CalendarYear: report.CalendarYear, CalendarMonth: report.CalendarMonth),
            _ =>
            {
                var output = renderer.Render(report, MonthlyESalesExportFormat.Json);
                return Task.FromResult(new FiscalReportArtifactPayload(output.Bytes, output.ContentType, output.FileName));
            }, cancellationToken).ConfigureAwait(false);
        return persisted.Succeeded
            ? Map(result, correlation)
            : Error("monthly_esales_artifact_unavailable", 503, persisted.SafeMessage, correlation);
    }

    public static async Task<IResult> ExportAsync(
        [AsParameters] MonthlyESalesRequest request,
        string format,
        MonthlyESalesService service,
        MonthlyESalesOutputRenderer renderer,
        FiscalReportArtifactService artifactService,
        IAuditTrailRepository auditTrailRepository,
        HttpContext context,
        IHostEnvironment environment,
        ILogger<MonthlyESalesAudit> logger,
        CancellationToken cancellationToken)
    {
        var correlation = Correlation(context);
        if (correlation is null) return Results.Json(Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"), statusCode: 400);
        if (!TryFormat(format, out var outputFormat)) return Results.Json(Error("monthly_esales_export_format_unsupported", 400, "The requested export format is unsupported.", correlation), statusCode: 400);
        if (!Authorized(request, context, environment)) return Results.Json(Error("monthly_esales_not_found", 404, "The requested Monthly eSales report is unavailable.", correlation), statusCode: 404);
        MonthlyESalesReport report;
        MonthlyESalesPdfReport? pdfReport = null;
        if (outputFormat == MonthlyESalesExportFormat.Pdf)
        {
            var pdfResult = await service.ReadPdfAsync(Query(request), cancellationToken).ConfigureAwait(false);
            if (pdfResult.Report is null)
            {
                var failure = Map(new MonthlyESalesResult(pdfResult.Outcome, SafeMessage: pdfResult.SafeMessage), correlation);
                return Results.Json(failure, statusCode: failure.HttpStatusCode);
            }
            pdfReport = pdfResult.Report;
            report = pdfReport.Report;
        }
        else
        {
            var result = await service.ReadAsync(Query(request), cancellationToken).ConfigureAwait(false);
            if (result.Report is null)
            {
                var failure = Map(result, correlation);
                return Results.Json(failure, statusCode: failure.HttpStatusCode);
            }
            report = result.Report;
        }
        var normalizedFormat = outputFormat.ToString().ToLowerInvariant();
        var sourceFingerprint = outputFormat == MonthlyESalesExportFormat.Json
            ? FiscalReportArtifactContract.SourceFingerprint(report)
            : FiscalReportArtifactContract.SourceFingerprint(new { report, outputFormat, pdfReport });
        var artifactResult = await artifactService.GetOrCreateAsync(new(
            report.SitePosServerId, report.FiscalIdentityId, report.CurrencyCode, "MONTHLY_ESALES",
            $"{report.CalendarYear:D4}-{report.CalendarMonth:D2}", normalizedFormat,
            outputFormat switch
            {
                MonthlyESalesExportFormat.Json => MonthlyESalesContract.JsonExportVersion,
                MonthlyESalesExportFormat.Csv => MonthlyESalesContract.CsvExportVersion,
                _ => "pos-server-monthly-esales-pdf:v1"
            },
            sourceFingerprint, CalendarYear: report.CalendarYear, CalendarMonth: report.CalendarMonth),
            _ =>
            {
                var output = renderer.Render(report, outputFormat, pdfReport);
                return Task.FromResult(new FiscalReportArtifactPayload(output.Bytes, output.ContentType, output.FileName));
            }, cancellationToken).ConfigureAwait(false);
        if (!artifactResult.Succeeded || artifactResult.Artifact is null || artifactResult.Bytes is null)
            return Results.Json(Error("monthly_esales_artifact_unavailable", 503,
                artifactResult.SafeMessage ?? "The Monthly eSales artifact could not be persisted safely.", correlation), statusCode: 503);
        var artifact = artifactResult.Artifact;
        logger.LogInformation("Monthly eSales export completed. Year={Year} Month={Month} Format={Format} ContentSha256={Hash} Correlation={Correlation}",
            report.CalendarYear, report.CalendarMonth, outputFormat, artifact.Sha256, correlation);
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        await auditTrailRepository.RecordExportAsync(report.SitePosServerId, "MONTHLY_ESALES",
            $"{report.CalendarYear:D4}-{report.CalendarMonth:D2}", format, actor, correlation, artifact.Sha256,
            report.Rows.Count, cancellationToken).ConfigureAwait(false);
        context.Response.Headers["X-Content-SHA256"] = artifact.Sha256;
        context.Response.Headers["X-ExitPass-Artifact-Revision"] = artifact.Revision.ToString();
        return Results.File(artifactResult.Bytes, artifact.ContentType, artifact.FileName, enableRangeProcessing: false);
    }

    private static MonthlyESalesQuery Query(MonthlyESalesRequest request) => new(
        request.SitePosServerId ?? Guid.Empty, request.FiscalIdentityId ?? Guid.Empty, request.CurrencyCode ?? string.Empty,
        request.CalendarYear ?? 0, request.CalendarMonth ?? 0);

    private static bool Authorized(MonthlyESalesRequest request, HttpContext context, IHostEnvironment environment) =>
        MonthlyESalesAuthorization.IsHostingAuthorityAllowed(context.User, environment) &&
        request.SitePosServerId is { } site && request.FiscalIdentityId is { } identity && request.CurrencyCode is { } currency &&
        MonthlyESalesAuthorization.IsInScope(context.User, site, identity, currency);

    private static MonthlyESalesApiResponse Map(MonthlyESalesResult result, string correlation) => result.Outcome switch
    {
        MonthlyESalesOutcome.Succeeded => new(true, "monthly_esales_read", correlation, 200, result.Report),
        MonthlyESalesOutcome.MonthNotComplete => Error("monthly_esales_month_not_complete", 409, result.SafeMessage, correlation),
        MonthlyESalesOutcome.MissingAuthoritativeSource => Error("monthly_esales_source_missing", 409, result.SafeMessage, correlation),
        MonthlyESalesOutcome.IncompleteAuthoritativeSources => Error("monthly_esales_sources_incomplete", 409, result.SafeMessage, correlation),
        MonthlyESalesOutcome.UnsupportedSourceClassification => Error("monthly_esales_source_unsupported", 422, result.SafeMessage, correlation),
        MonthlyESalesOutcome.MissingEndRange => Error("monthly_esales_end_range_missing", 422, result.SafeMessage, correlation),
        MonthlyESalesOutcome.ArithmeticOverflow => Error("monthly_esales_reconciliation_failed", 422, result.SafeMessage, correlation),
        MonthlyESalesOutcome.ScopeMismatch => Error("monthly_esales_not_found", 404, "The requested Monthly eSales report is unavailable.", correlation),
        MonthlyESalesOutcome.PersistenceFailure => Error("monthly_esales_unavailable", 503, result.SafeMessage, correlation),
        _ => Error("monthly_esales_invalid_request", 400, result.SafeMessage, correlation)
    };

    private static MonthlyESalesApiResponse Error(string code, int status, string? message, string correlation) =>
        new(false, code, correlation, status, Message: message ?? "The Monthly eSales operation failed safely.");

    private static bool TryFormat(string value, out MonthlyESalesExportFormat format)
    {
        if (string.Equals(value, "json", StringComparison.OrdinalIgnoreCase)) { format = MonthlyESalesExportFormat.Json; return true; }
        if (string.Equals(value, "csv", StringComparison.OrdinalIgnoreCase)) { format = MonthlyESalesExportFormat.Csv; return true; }
        if (string.Equals(value, "pdf", StringComparison.OrdinalIgnoreCase)) { format = MonthlyESalesExportFormat.Pdf; return true; }
        format = default; return false;
    }

    private static string? Correlation(HttpContext context)
    {
        var values = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n') ? null : value;
    }
}
