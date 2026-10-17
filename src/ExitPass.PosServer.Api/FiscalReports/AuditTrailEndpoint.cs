using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record AuditTrailRequest(
    Guid? SitePosServerId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? ActionCode,
    string? Actor,
    string? Result,
    string? Reference);

public sealed record AuditTrailApiResponse(
    bool Succeeded,
    string Code,
    string CorrelationId,
    int HttpStatusCode,
    AuditTrailReport? Report = null,
    string? Message = null);

public static class AuditTrailEndpoint
{
    public static async Task<IResult> ReadAsync(
        [AsParameters] AuditTrailRequest request,
        AuditTrailService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var correlation = Correlation(context);
        if (correlation is null) return Json(Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"));
        if (!Authorized(request, context)) return Json(Error("audit_trail_not_found", 404, "The requested Audit Trail is unavailable.", correlation));
        var result = await service.ReadAsync(Query(request), cancellationToken).ConfigureAwait(false);
        return Json(Map(result, correlation));
    }

    public static async Task<IResult> ExportAsync(
        [AsParameters] AuditTrailRequest request,
        string format,
        AuditTrailService service,
        AuditTrailOutputRenderer renderer,
        IAuditTrailRepository repository,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var correlation = Correlation(context);
        if (correlation is null) return Json(Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"));
        if (!TryFormat(format, out var outputFormat)) return Json(Error("audit_trail_export_format_unsupported", 400, "The requested Audit Trail export format is unsupported.", correlation));
        if (!Authorized(request, context)) return Json(Error("audit_trail_not_found", 404, "The requested Audit Trail is unavailable.", correlation));
        var result = await service.ReadAsync(Query(request), cancellationToken).ConfigureAwait(false);
        if (result.Report is null) return Json(Map(result, correlation));

        var output = renderer.Render(result.Report, outputFormat);
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        var subject = $"{result.Report.From:O}/{result.Report.To:O}";
        await repository.RecordExportAsync(result.Report.SitePosServerId, "AUDIT_TRAIL", subject, format, actor,
            correlation, output.Sha256, result.Report.Rows.Count, cancellationToken).ConfigureAwait(false);
        context.Response.Headers["X-Content-SHA256"] = output.Sha256;
        return Results.File(output.Bytes, output.ContentType, output.FileName, enableRangeProcessing: false);
    }

    private static AuditTrailQuery Query(AuditTrailRequest request) => new(
        request.SitePosServerId ?? Guid.Empty,
        request.From ?? default,
        request.To ?? default,
        request.ActionCode,
        request.Actor,
        request.Result,
        request.Reference);

    private static bool Authorized(AuditTrailRequest request, HttpContext context) =>
        request.SitePosServerId is { } site && AuditTrailAuthorization.IsInScope(context.User, site);

    private static AuditTrailApiResponse Map(AuditTrailResult result, string correlation) => result.Outcome switch
    {
        AuditTrailOutcome.Succeeded => new(true, "audit_trail_read", correlation, 200, result.Report),
        AuditTrailOutcome.InvalidRequest => Error("audit_trail_invalid_request", 400, result.SafeMessage, correlation),
        AuditTrailOutcome.RangeTooLarge => Error("audit_trail_range_too_large", 400, result.SafeMessage, correlation),
        AuditTrailOutcome.ScopeNotFound => Error("audit_trail_not_found", 404, "The requested Audit Trail is unavailable.", correlation),
        _ => Error("audit_trail_unavailable", 503, result.SafeMessage, correlation)
    };

    private static AuditTrailApiResponse Error(string code, int status, string? message, string correlation) =>
        new(false, code, correlation, status, Message: message ?? "The Audit Trail operation failed safely.");

    private static IResult Json(AuditTrailApiResponse response) => Results.Json(response, statusCode: response.HttpStatusCode);

    private static bool TryFormat(string value, out AuditTrailExportFormat format)
    {
        if (string.Equals(value, "json", StringComparison.OrdinalIgnoreCase)) { format = AuditTrailExportFormat.Json; return true; }
        if (string.Equals(value, "csv", StringComparison.OrdinalIgnoreCase)) { format = AuditTrailExportFormat.Csv; return true; }
        if (string.Equals(value, "pdf", StringComparison.OrdinalIgnoreCase)) { format = AuditTrailExportFormat.Pdf; return true; }
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
