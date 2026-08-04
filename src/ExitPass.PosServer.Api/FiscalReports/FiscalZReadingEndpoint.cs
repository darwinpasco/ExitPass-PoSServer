using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record CloseFiscalZReadingRequest(
    string? OperationKey,
    Guid? SitePosServerId,
    Guid? FiscalIdentityId,
    string? CurrencyCode,
    Guid? FiscalReportingPeriodId,
    long? ExpectedStateVersion);

public sealed record FiscalZReadingApiResponse(
    bool Succeeded,
    string Code,
    string CorrelationId,
    int HttpStatusCode,
    FiscalZReadingRecord? ZReading = null,
    string? Message = null);

public static class FiscalZReadingEndpoint
{
    public static async Task<FiscalZReadingApiResponse> CloseAsync(
        CloseFiscalZReadingRequest request,
        FiscalZReadingService service,
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null)
            return Error("missing_correlation_id", StatusCodes.Status400BadRequest,
                "A single safe X-Correlation-Id header is required.", "unavailable");
        if (request.SitePosServerId is null || request.FiscalIdentityId is null ||
            string.IsNullOrWhiteSpace(request.CurrencyCode) ||
            !FiscalZReadingAuthorization.IsInScope(context.User, request.SitePosServerId.Value,
                request.FiscalIdentityId.Value, request.CurrencyCode))
            return Error("fiscal_z_reading_scope_forbidden", StatusCodes.Status403Forbidden,
                "The authenticated service identity is not authorized for the requested fiscal scope.", correlation);

        var principal = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        var result = await service.CloseAsync(new(
            request.OperationKey ?? string.Empty,
            request.SitePosServerId.Value,
            request.FiscalIdentityId.Value,
            request.CurrencyCode,
            request.FiscalReportingPeriodId ?? Guid.Empty,
            request.ExpectedStateVersion ?? 0,
            principal,
            principal,
            correlation), cancellationToken).ConfigureAwait(false);
        return Map(result, correlation);
    }

    public static async Task<FiscalZReadingApiResponse> GetAsync(
        string zReadingReference,
        FiscalZReadingService service,
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null)
            return Error("missing_correlation_id", StatusCodes.Status400BadRequest,
                "A single safe X-Correlation-Id header is required.", "unavailable");
        var result = await service.GetByReferenceAsync(zReadingReference, cancellationToken).ConfigureAwait(false);
        if (result.Record is not null && !FiscalZReadingAuthorization.IsInScope(
            context.User, result.Record.SitePosServerId, result.Record.FiscalIdentityId, result.Record.CurrencyCode))
            return Error("fiscal_z_reading_scope_forbidden", StatusCodes.Status403Forbidden,
                "The authenticated service identity is not authorized for this fiscal scope.", correlation);
        return Map(result, correlation);
    }

    private static FiscalZReadingApiResponse Map(FiscalZReadingResult result, string correlationId) => result.Outcome switch
    {
        FiscalZReadingOutcome.Created => Success("fiscal_z_reading_closed", StatusCodes.Status201Created, result.Record!, correlationId),
        FiscalZReadingOutcome.Replayed => Success("fiscal_z_reading_replayed", StatusCodes.Status200OK, result.Record!, correlationId),
        FiscalZReadingOutcome.Conflict => Error("fiscal_z_reading_semantic_conflict", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.PeriodAlreadyClosed => Error("fiscal_reporting_period_already_closed", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.PeriodNotEnded => Error("fiscal_reporting_period_not_ended", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.PeriodNotOpen => Error("fiscal_reporting_period_not_open", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.PeriodUnavailable or FiscalZReadingOutcome.SitePosServerNotFound or FiscalZReadingOutcome.FiscalIdentityNotFound or FiscalZReadingOutcome.NotFound =>
            Error("fiscal_z_reading_not_found", StatusCodes.Status404NotFound, "The requested Z Reading resource is unavailable.", correlationId),
        FiscalZReadingOutcome.ReportingConfigurationUnavailable => Error("fiscal_reporting_configuration_unavailable", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.StateInitializationRequired => Error("fiscal_z_state_initialization_required", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.StaleStateVersion => Error("fiscal_z_state_version_conflict", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.PriorPeriodUnresolved => Error("fiscal_z_prior_period_unresolved", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.UnassignedFiscalDocument => Error("fiscal_z_unassigned_document", StatusCodes.Status409Conflict, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.UnsupportedCrossPeriodMutation => Error("fiscal_z_cross_period_mutation_unsupported", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.UnsupportedSourceClassification => Error("fiscal_z_source_classification_unsupported", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.MixedCurrency => Error("fiscal_z_mixed_currency", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.UnexplainedFiscalGap => Error("fiscal_z_unexplained_gap", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.ReconciliationFailure => Error("fiscal_z_reconciliation_failed", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.ArithmeticOverflow => Error("fiscal_z_arithmetic_overflow", StatusCodes.Status422UnprocessableEntity, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.LockTimeout or FiscalZReadingOutcome.RetryableConcurrencyFailure => Error("fiscal_z_close_temporarily_busy", StatusCodes.Status503ServiceUnavailable, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.UnknownCommitOutcome => Error("fiscal_z_reading_unknown_commit_outcome", StatusCodes.Status503ServiceUnavailable, result.SafeMessage, correlationId),
        FiscalZReadingOutcome.PersistenceFailure => Error("fiscal_z_reading_persistence_failed", StatusCodes.Status503ServiceUnavailable, result.SafeMessage, correlationId),
        _ => Error("fiscal_z_reading_invalid_request", StatusCodes.Status400BadRequest, result.SafeMessage, correlationId)
    };

    private static FiscalZReadingApiResponse Success(string code, int status, FiscalZReadingRecord record, string correlation) =>
        new(true, code, correlation, status, record);

    private static FiscalZReadingApiResponse Error(string code, int status, string? message, string correlation) =>
        new(false, code, correlation, status, Message: message ?? "The Z Reading operation failed safely.");

    private static string? ResolveCorrelation(HttpContext context)
    {
        var values = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n') ? null : value;
    }
}
