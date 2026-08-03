using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record GenerateFiscalXReadingRequest(
    string? OperationKey,
    Guid? SitePosServerId,
    Guid? FiscalIdentityId,
    DateTimeOffset? ObservedAt);

public sealed record FiscalXReadingApiResponse(
    bool Succeeded,
    string Code,
    string CorrelationId,
    int HttpStatusCode,
    FiscalXReadingRecord? XReading = null,
    string? Message = null);

public static class FiscalXReadingEndpoint
{
    public static async Task<FiscalXReadingApiResponse> GenerateAsync(
        GenerateFiscalXReadingRequest request,
        FiscalXReadingService service,
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null) return Error("missing_correlation_id", StatusCodes.Status400BadRequest, "A single safe X-Correlation-Id header is required.", "unavailable");
        if (request.SitePosServerId is null || request.FiscalIdentityId is null ||
            !FiscalXReadingAuthorization.IsInScope(context.User, request.SitePosServerId.Value, request.FiscalIdentityId.Value))
            return Error("fiscal_x_reading_scope_forbidden", StatusCodes.Status403Forbidden, "The authenticated service identity is not authorized for the requested fiscal scope.", correlation);

        var principal = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        var result = await service.GenerateAsync(new(
            request.OperationKey ?? string.Empty,
            request.SitePosServerId.Value,
            request.FiscalIdentityId.Value,
            request.ObservedAt ?? default,
            principal,
            principal,
            correlation), cancellationToken).ConfigureAwait(false);
        return Map(result, correlation);
    }

    public static async Task<FiscalXReadingApiResponse> GetAsync(
        string fiscalReportReference,
        FiscalXReadingService service,
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null) return Error("missing_correlation_id", StatusCodes.Status400BadRequest, "A single safe X-Correlation-Id header is required.", "unavailable");
        var result = await service.GetByReferenceAsync(fiscalReportReference, cancellationToken).ConfigureAwait(false);
        if (result.Record is not null && !FiscalXReadingAuthorization.IsInScope(context.User, result.Record.SitePosServerId, result.Record.FiscalIdentityId))
            return Error("fiscal_x_reading_scope_forbidden", StatusCodes.Status403Forbidden, "The authenticated service identity is not authorized for this fiscal scope.", correlation);
        return Map(result, correlation);
    }

    private static FiscalXReadingApiResponse Map(FiscalXReadingResult result, string correlationId) => result.Outcome switch
    {
        FiscalXReadingOutcome.Created => Success("fiscal_x_reading_created", StatusCodes.Status201Created, result.Record!, correlationId),
        FiscalXReadingOutcome.Replayed => Success("fiscal_x_reading_replayed", StatusCodes.Status200OK, result.Record!, correlationId),
        FiscalXReadingOutcome.Conflict => Error("fiscal_x_reading_semantic_conflict", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.SitePosServerNotFound => Error("site_pos_server_not_found", StatusCodes.Status404NotFound, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.FiscalIdentityNotFound => Error("fiscal_identity_not_found", StatusCodes.Status404NotFound, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.ReportingConfigurationUnavailable => Error("fiscal_reporting_configuration_unavailable", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.OpenPeriodUnavailable => Error("fiscal_reporting_open_period_unavailable", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.AmbiguousOpenPeriod => Error("fiscal_reporting_open_period_ambiguous", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.UnsupportedSourceClassification => Error("fiscal_x_reading_source_classification_unsupported", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.MixedCurrency => Error("fiscal_x_reading_mixed_currency", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.ReconciliationFailure => Error("fiscal_x_reading_reconciliation_failed", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.ArithmeticOverflow => Error("fiscal_x_reading_arithmetic_overflow", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.NotFound => Error("fiscal_x_reading_not_found", StatusCodes.Status404NotFound, "X Reading was not found.", correlationId),
        FiscalXReadingOutcome.UnknownCommitOutcome => Error("fiscal_x_reading_unknown_commit_outcome", StatusCodes.Status503ServiceUnavailable, result.SafeMessage, correlationId),
        FiscalXReadingOutcome.PersistenceFailure => Error("fiscal_x_reading_persistence_failed", StatusCodes.Status503ServiceUnavailable, result.SafeMessage, correlationId),
        _ => Error("fiscal_x_reading_invalid_request", StatusCodes.Status400BadRequest, result.SafeMessage, correlationId)
    };

    private static FiscalXReadingApiResponse Success(string code, int status, FiscalXReadingRecord record, string correlation) =>
        new(true, code, correlation, status, record);

    private static FiscalXReadingApiResponse Error(string code, int status, string? message, string correlation) =>
        new(false, code, correlation, status, Message: message ?? "The X Reading operation failed safely.");

    private static string? ResolveCorrelation(HttpContext context)
    {
        var values = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n') ? null : value;
    }
}
