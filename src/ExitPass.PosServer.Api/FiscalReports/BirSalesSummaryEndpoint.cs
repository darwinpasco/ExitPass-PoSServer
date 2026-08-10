using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record GenerateBirSalesSummaryRequest(
    string? OperationKey,
    Guid? SitePosServerId,
    Guid? FiscalIdentityId,
    string? CurrencyCode,
    Guid? FiscalReportingPeriodId,
    string? GoverningZReadingReference);

public sealed record BirSalesSummaryApiResponse(
    bool Succeeded,
    string Code,
    string CorrelationId,
    int HttpStatusCode,
    BirSalesSummaryRecord? Summary = null,
    string? Message = null);

public sealed class BirSalesSummaryAudit;

public static class BirSalesSummaryEndpoint
{
    public static async Task<BirSalesSummaryApiResponse> GenerateAsync(
        GenerateBirSalesSummaryRequest request,
        BirSalesSummaryService service,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null) return Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable");
        if (!BirSalesSummaryAuthorization.IsHostingAuthorityAllowed(context.User, environment))
            return Error("bir_sales_summary_authority_forbidden", 403, "The authenticated reporting authority is unavailable in this hosting environment.", correlation);
        if (request.SitePosServerId is null || request.FiscalIdentityId is null || string.IsNullOrWhiteSpace(request.CurrencyCode) ||
            !BirSalesSummaryAuthorization.IsInScope(context.User, request.SitePosServerId.Value, request.FiscalIdentityId.Value, request.CurrencyCode))
            return Error("bir_sales_summary_scope_forbidden", 403, "The authenticated service identity is not authorized for the requested fiscal scope.", correlation);

        var principal = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        var result = await service.GenerateAsync(new(
            request.OperationKey ?? string.Empty,
            request.SitePosServerId.Value,
            request.FiscalIdentityId.Value,
            request.CurrencyCode,
            request.FiscalReportingPeriodId ?? Guid.Empty,
            request.GoverningZReadingReference ?? string.Empty,
            principal,
            principal,
            correlation), cancellationToken).ConfigureAwait(false);
        return Map(result, correlation);
    }

    public static async Task<BirSalesSummaryApiResponse> GetAsync(
        Guid summaryId,
        BirSalesSummaryService service,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null) return Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable");
        if (!BirSalesSummaryAuthorization.IsHostingAuthorityAllowed(context.User, environment))
            return Error("bir_sales_summary_not_found", 404, "The requested BIR sales summary is unavailable.", correlation);
        var result = await service.GetByIdAsync(summaryId, cancellationToken).ConfigureAwait(false);
        if (result.Record is not null && !BirSalesSummaryAuthorization.IsInScope(
                context.User, result.Record.SitePosServerId, result.Record.FiscalIdentityId, result.Record.CurrencyCode))
            return Error("bir_sales_summary_not_found", 404, "The requested BIR sales summary is unavailable.", correlation);
        return Map(result, correlation);
    }

    public static async Task<IResult> ExportAsync(
        Guid summaryId,
        string format,
        BirSalesSummaryService service,
        BirSalesSummaryOutputRenderer renderer,
        HttpContext context,
        IHostEnvironment environment,
        ILogger<BirSalesSummaryAudit> logger,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null) return Results.Json(Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"), statusCode: 400);
        if (!TryParseFormat(format, out var outputFormat))
            return Results.Json(Error("bir_sales_summary_export_format_unsupported", 400, "The requested export format is unsupported.", correlation), statusCode: 400);
        if (!BirSalesSummaryAuthorization.IsHostingAuthorityAllowed(context.User, environment))
            return Results.Json(Error("bir_sales_summary_not_found", 404, "The requested BIR sales summary is unavailable.", correlation), statusCode: 404);
        var read = await service.GetByIdAsync(summaryId, cancellationToken).ConfigureAwait(false);
        if (read.Record is null || !BirSalesSummaryAuthorization.IsInScope(
                context.User, read.Record.SitePosServerId, read.Record.FiscalIdentityId, read.Record.CurrencyCode))
        {
            logger.LogWarning("BIR sales-summary export denied or hidden. Summary={SummaryId} Correlation={Correlation}", Safe(summaryId), correlation);
            return Results.Json(Error("bir_sales_summary_not_found", 404, "The requested BIR sales summary is unavailable.", correlation), statusCode: 404);
        }
        var output = renderer.Render(read.Record, outputFormat);
        logger.LogInformation(
            "BIR sales-summary export completed. Summary={SummaryId} Format={Format} ContentSha256={ContentSha256} Correlation={Correlation}",
            Safe(summaryId), output.Format, output.Sha256, correlation);
        context.Response.Headers["X-Content-SHA256"] = output.Sha256;
        return Results.File(output.Bytes, output.ContentType, output.FileName, enableRangeProcessing: false);
    }

    private static BirSalesSummaryApiResponse Map(BirSalesSummaryResult result, string correlation) => result.Outcome switch
    {
        BirSalesSummaryOutcome.Created => Success("bir_sales_summary_created", 201, result.Record!, correlation),
        BirSalesSummaryOutcome.Replayed => Success("bir_sales_summary_replayed", 200, result.Record!, correlation),
        BirSalesSummaryOutcome.Conflict => Error("bir_sales_summary_semantic_conflict", 409, result.SafeMessage, correlation),
        BirSalesSummaryOutcome.PeriodNotClosed => Error("bir_sales_summary_period_not_closed", 409, result.SafeMessage, correlation),
        BirSalesSummaryOutcome.GoverningZNotCommitted => Error("bir_sales_summary_z_not_committed", 409, result.SafeMessage, correlation),
        BirSalesSummaryOutcome.HeaderProfileUnavailable => Error("bir_sales_summary_header_profile_unavailable", 409, result.SafeMessage, correlation),
        BirSalesSummaryOutcome.ReconciliationFailure => Error("bir_sales_summary_reconciliation_failed", 422, result.SafeMessage, correlation),
        BirSalesSummaryOutcome.UnsupportedSourceClassification => Error("bir_sales_summary_source_unsupported", 422, result.SafeMessage, correlation),
        BirSalesSummaryOutcome.GoverningZNotFound or BirSalesSummaryOutcome.ScopeMismatch or BirSalesSummaryOutcome.NotFound =>
            Error("bir_sales_summary_not_found", 404, "The requested BIR sales-summary resource is unavailable.", correlation),
        BirSalesSummaryOutcome.RetryableConcurrencyFailure => Error("bir_sales_summary_temporarily_busy", 503, result.SafeMessage, correlation),
        BirSalesSummaryOutcome.PersistenceFailure or BirSalesSummaryOutcome.UnknownCommitOutcome => Error("bir_sales_summary_unavailable", 503, result.SafeMessage, correlation),
        _ => Error("bir_sales_summary_invalid_request", 400, result.SafeMessage, correlation)
    };

    private static BirSalesSummaryApiResponse Success(string code, int status, BirSalesSummaryRecord summary, string correlation) =>
        new(true, code, correlation, status, summary);
    private static BirSalesSummaryApiResponse Error(string code, int status, string? message, string correlation) =>
        new(false, code, correlation, status, Message: message ?? "The BIR sales-summary operation failed safely.");
    private static bool TryParseFormat(string format, out BirSalesSummaryExportFormat result)
    {
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase)) { result = BirSalesSummaryExportFormat.Json; return true; }
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)) { result = BirSalesSummaryExportFormat.Csv; return true; }
        result = default; return false;
    }
    private static string? ResolveCorrelation(HttpContext context)
    {
        var values = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n') ? null : value;
    }
    private static string Safe(Guid value) => value.ToString("N")[..12];
}
