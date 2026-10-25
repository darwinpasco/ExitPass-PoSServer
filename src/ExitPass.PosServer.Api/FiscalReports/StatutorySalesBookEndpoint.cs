using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record StatutorySalesBookRequest(Guid? SitePosServerId, Guid? FiscalIdentityId, Guid? ChannelTerminalId, string? CurrencyCode, int? CalendarYear, int? CalendarMonth);
public sealed record StatutorySalesBookApiResponse(bool Succeeded, string Code, string CorrelationId, int HttpStatusCode, StatutorySalesBookReport? Report = null, string? Message = null);

public static class StatutorySalesBookEndpoint
{
    public static async Task<IResult> ReadAsync(string annex, [AsParameters] StatutorySalesBookRequest request, StatutorySalesBookService service, IAuditTrailRepository audit, HttpContext context, IHostEnvironment environment, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        var correlation = Correlation(context);
        if (correlation is null) return Json(Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"));
        if (!TryType(annex, out var type)) return Json(Error("statutory_sales_book_type_unsupported", 400, "The requested statutory sales-book type is unsupported.", correlation));
        if (!Authorized(request, context, environment)) return Json(Error("statutory_sales_book_not_found", 404, "The requested statutory sales book is unavailable.", correlation));
        var result = await service.ReadAsync(Query(request, type, context), ct).ConfigureAwait(false);
        if (result.Report is not null)
        {
            var summary = result.Report.Summary;
            await audit.RecordAccessAsync(summary.SitePosServerId, type == StatutorySalesBookType.AnnexE2 ? "ANNEX_E2" : "ANNEX_E3",
                $"{summary.CalendarYear:0000}-{summary.CalendarMonth:00}/{summary.ChannelTerminalId:D}",
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable", correlation,
                summary.QualifyingTransactionCount, ct).ConfigureAwait(false);
        }
        return Json(Map(result, correlation));
    }

    public static async Task<IResult> DownloadAsync(string annex, [AsParameters] StatutorySalesBookRequest request, string format, StatutorySalesBookService service, StatutorySalesBookOutputRenderer renderer, FiscalReportArtifactService artifactService, IAuditTrailRepository audit, HttpContext context, IHostEnvironment environment, CancellationToken ct)
    {
        var correlation = Correlation(context);
        if (correlation is null) return Json(Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"));
        if (!TryType(annex, out var type) || !TryFormat(format, out var exportFormat)) return Json(Error("statutory_sales_book_export_format_unsupported", 400, "The requested statutory sales-book export is unsupported.", correlation));
        if (!Authorized(request, context, environment)) return Json(Error("statutory_sales_book_not_found", 404, "The requested statutory sales book is unavailable.", correlation));
        var result = await service.ReadAsync(Query(request, type, context), ct).ConfigureAwait(false);
        if (result.Report is null) return Json(Map(result, correlation));
        var report = result.Report;
        var reportKind = type == StatutorySalesBookType.AnnexE2 ? "ANNEX_E2" : "ANNEX_E3";
        var scopeReference = $"{report.Summary.CalendarYear:0000}-{report.Summary.CalendarMonth:00}/{report.Summary.ChannelTerminalId:D}";
        var persisted = await artifactService.GetOrCreateAsync(new(
            report.Summary.SitePosServerId, report.Summary.FiscalIdentityId, report.Summary.CurrencyCode,
            reportKind, scopeReference, format.Trim().ToLowerInvariant(), "pos-server-statutory-sales-book-rmo24-2023:v1",
            StatutorySalesBookContract.SourceFingerprint(report), CalendarYear: report.Summary.CalendarYear,
            CalendarMonth: report.Summary.CalendarMonth), _ =>
        {
            var rendered = renderer.Render(report, exportFormat);
            return Task.FromResult(new FiscalReportArtifactPayload(rendered.Bytes, rendered.ContentType, rendered.FileName));
        }, ct).ConfigureAwait(false);
        if (!persisted.Succeeded || persisted.Artifact is null || persisted.Bytes is null)
            return Json(Error("statutory_sales_book_artifact_unavailable", 503, persisted.SafeMessage, correlation));
        var artifact = persisted.Artifact;
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        await audit.RecordExportAsync(report.Summary.SitePosServerId, reportKind, scopeReference, format, actor, correlation, artifact.Sha256,
            report.Summary.QualifyingTransactionCount, ct).ConfigureAwait(false);
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.ETag = $"\"sha256-{artifact.Sha256}\"";
        context.Response.Headers["X-Content-SHA256"] = artifact.Sha256;
        context.Response.Headers["X-ExitPass-Artifact-Revision"] = artifact.Revision.ToString();
        return Results.File(persisted.Bytes, artifact.ContentType, artifact.FileName, enableRangeProcessing: false);
    }

    private static StatutorySalesBookQuery Query(StatutorySalesBookRequest request, StatutorySalesBookType type, HttpContext context) => new(
        request.SitePosServerId ?? Guid.Empty, request.FiscalIdentityId ?? Guid.Empty, request.ChannelTerminalId ?? Guid.Empty,
        request.CurrencyCode ?? string.Empty, request.CalendarYear ?? 0, request.CalendarMonth ?? 0, type,
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable");

    private static bool Authorized(StatutorySalesBookRequest request, HttpContext context, IHostEnvironment environment) =>
        StatutorySalesBookAuthorization.IsHostingAuthorityAllowed(context.User, environment) && request.SitePosServerId is { } site &&
        request.FiscalIdentityId is { } identity && request.ChannelTerminalId is { } terminal && terminal != Guid.Empty && request.CurrencyCode is { } currency &&
        StatutorySalesBookAuthorization.IsInScope(context.User, site, identity, currency);

    private static StatutorySalesBookApiResponse Map(StatutorySalesBookResult result, string correlation) => result.Outcome switch
    {
        StatutorySalesBookOutcome.Succeeded => new(true, "statutory_sales_book_read", correlation, 200, result.Report),
        StatutorySalesBookOutcome.InvalidRequest => Error("statutory_sales_book_invalid_request", 400, result.SafeMessage, correlation),
        StatutorySalesBookOutcome.ScopeNotFound => Error("statutory_sales_book_not_found", 404, "The requested statutory sales book is unavailable.", correlation),
        StatutorySalesBookOutcome.MissingBeneficiarySnapshot => Error("E2_E3_REQUIRED_BENEFICIARY_SNAPSHOT_MISSING", 422, result.SafeMessage, correlation),
        StatutorySalesBookOutcome.ReconciliationFailure => Error("statutory_sales_book_reconciliation_failed", 422, result.SafeMessage, correlation),
        _ => Error("statutory_sales_book_unavailable", 503, result.SafeMessage, correlation)
    };

    private static bool TryType(string value, out StatutorySalesBookType type)
    {
        if (string.Equals(value, "e2", StringComparison.OrdinalIgnoreCase)) { type = StatutorySalesBookType.AnnexE2; return true; }
        if (string.Equals(value, "e3", StringComparison.OrdinalIgnoreCase)) { type = StatutorySalesBookType.AnnexE3; return true; }
        type = default; return false;
    }
    private static bool TryFormat(string value, out StatutorySalesBookFormat format)
    {
        if (string.Equals(value, "xlsx", StringComparison.OrdinalIgnoreCase)) { format = StatutorySalesBookFormat.Xlsx; return true; }
        if (string.Equals(value, "pdf", StringComparison.OrdinalIgnoreCase)) { format = StatutorySalesBookFormat.Pdf; return true; }
        format = default; return false;
    }
    private static StatutorySalesBookApiResponse Error(string code, int status, string? message, string correlation) => new(false, code, correlation, status, Message: message ?? "The statutory sales-book operation failed safely.");
    private static IResult Json(StatutorySalesBookApiResponse response) => Results.Json(response, statusCode: response.HttpStatusCode);
    private static string? Correlation(HttpContext context) { var values=context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName]; if(values.Count!=1)return null;var value=values[0]?.Trim();return string.IsNullOrWhiteSpace(value)||value.Length>200||value.Contains('\r')||value.Contains('\n')?null:value; }
}
