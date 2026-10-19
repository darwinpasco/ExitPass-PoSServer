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
        FiscalReportArtifactService artifactService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var correlation = Correlation(context);
        if (correlation is null) return Json(Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"));
        if (!Authorized(request, context)) return Json(Error("audit_trail_not_found", 404, "The requested Audit Trail is unavailable.", correlation));
        var query = Query(request);
        var result = await service.ReadAsync(query, cancellationToken).ConfigureAwait(false);
        var response = Map(result, correlation);
        if (result.Report is null) return Json(response);
        var report = result.Report;
        var artifactScope = FiscalReportArtifactContract.SourceFingerprint(new
            { report.SitePosServerId, report.From, report.To, request.ActionCode, request.Actor, request.Result, request.Reference });
        var persisted = await artifactService.GetOrCreateAsync(new(
            report.SitePosServerId, null, MonthlyESalesContract.CurrencyCode, "AUDIT_TRAIL", $"{artifactScope}:read", "json",
            AuditTrailContract.JsonExportVersion, FiscalReportArtifactContract.SourceFingerprint(report)),
            _ => Task.FromResult(new FiscalReportArtifactPayload(
                System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(response), "application/json; charset=utf-8",
                $"audit-trail-{report.From:yyyyMMddHHmmss}-{report.To:yyyyMMddHHmmss}.json")),
            cancellationToken).ConfigureAwait(false);
        return persisted.Succeeded ? Json(response) : Json(Error("audit_trail_artifact_unavailable", 503,
            persisted.SafeMessage ?? "The Audit Trail artifact could not be persisted safely.", correlation));
    }

    public static async Task<IResult> ExportAsync(
        [AsParameters] AuditTrailRequest request,
        string format,
        AuditTrailService service,
        AuditTrailOutputRenderer renderer,
        FiscalReportArtifactService artifactService,
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

        var report = result.Report;
        var subject = $"{report.From:O}/{report.To:O}/{request.ActionCode}/{request.Actor}/{request.Result}/{request.Reference}";
        var artifactScope = FiscalReportArtifactContract.SourceFingerprint(new
            { report.SitePosServerId, report.From, report.To, request.ActionCode, request.Actor, request.Result, request.Reference });
        var persisted = await artifactService.GetOrCreateAsync(new(
            report.SitePosServerId, null, MonthlyESalesContract.CurrencyCode, "AUDIT_TRAIL", $"{artifactScope}:export",
            outputFormat.ToString().ToLowerInvariant(), AuditTrailContract.JsonExportVersion,
            FiscalReportArtifactContract.SourceFingerprint(new { report, outputFormat })),
            _ =>
            {
                var output = renderer.Render(report, outputFormat);
                return Task.FromResult(new FiscalReportArtifactPayload(output.Bytes, output.ContentType, output.FileName));
            }, cancellationToken).ConfigureAwait(false);
        if (!persisted.Succeeded || persisted.Artifact is null || persisted.Bytes is null)
            return Json(Error("audit_trail_artifact_unavailable", 503,
                persisted.SafeMessage ?? "The Audit Trail artifact could not be persisted safely.", correlation));
        var artifact = persisted.Artifact;
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        await repository.RecordExportAsync(report.SitePosServerId, "AUDIT_TRAIL", subject, format, actor,
            correlation, artifact.Sha256, report.Rows.Count, cancellationToken).ConfigureAwait(false);
        context.Response.Headers["X-Content-SHA256"] = artifact.Sha256;
        context.Response.Headers["X-ExitPass-Artifact-Revision"] = artifact.Revision.ToString();
        return Results.File(persisted.Bytes, artifact.ContentType, artifact.FileName, enableRangeProcessing: false);
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
