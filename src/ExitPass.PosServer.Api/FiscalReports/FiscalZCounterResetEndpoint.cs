using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record ResetFiscalZCounterRequest(
    string? OperationKey,
    Guid? SitePosServerId,
    Guid? FiscalIdentityId,
    string? CurrencyCode,
    long? ExpectedStateVersion,
    string? Reason,
    bool ConfirmReset);

public sealed record FiscalZCounterApiResponse(
    bool Succeeded,
    string Code,
    string CorrelationId,
    int HttpStatusCode,
    FiscalZCounterStateRecord? State = null,
    FiscalZCounterResetRecord? Reset = null,
    string? Message = null);

public static class FiscalZCounterResetEndpoint
{
    public static async Task<FiscalZCounterApiResponse> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string? currencyCode,
        FiscalZCounterResetService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var preflight = Preflight(context, sitePosServerId, fiscalIdentityId, currencyCode);
        if (preflight.Error is not null) return preflight.Error;
        var result = await service.ReadStateAsync(new(sitePosServerId, fiscalIdentityId, preflight.Currency!), cancellationToken).ConfigureAwait(false);
        return Map(result, preflight.Correlation!);
    }

    public static async Task<FiscalZCounterApiResponse> ResetAsync(
        ResetFiscalZCounterRequest request,
        FiscalZCounterResetService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var correlation = ResolveSingleHeader(context, SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName);
        if (correlation is null)
            return Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable");
        if (!request.ConfirmReset)
            return Error("fiscal_z_counter_reset_confirmation_required", 400,
                "Explicit human confirmation is required for manual Z Counter reset.", correlation);
        if (request.SitePosServerId is null || request.FiscalIdentityId is null || string.IsNullOrWhiteSpace(request.CurrencyCode) ||
            !FiscalZCounterResetAuthorization.IsInScope(context.User, request.SitePosServerId.Value,
                request.FiscalIdentityId.Value, request.CurrencyCode.Trim().ToUpperInvariant()))
            return Error("fiscal_z_counter_scope_forbidden", 403,
                "The authenticated service identity is not authorized for the requested fiscal scope.", correlation);

        var serviceIdentity = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        var delegatedActor = ResolveSingleHeader(context, "X-Actor-Reference") ?? serviceIdentity;
        var result = await service.ResetAsync(new(
            request.OperationKey ?? string.Empty,
            request.SitePosServerId.Value,
            request.FiscalIdentityId.Value,
            request.CurrencyCode ?? string.Empty,
            request.ExpectedStateVersion ?? 0,
            request.Reason ?? string.Empty,
            delegatedActor,
            serviceIdentity,
            correlation), cancellationToken).ConfigureAwait(false);
        if (result.Outcome is FiscalZCounterResetOutcome.Created or FiscalZCounterResetOutcome.Replayed)
        {
            var state = await service.ReadStateAsync(new(
                request.SitePosServerId.Value,
                request.FiscalIdentityId.Value,
                request.CurrencyCode!.Trim().ToUpperInvariant()), cancellationToken).ConfigureAwait(false);
            result = result with { State = state.State };
        }
        return Map(result, correlation);
    }

    private static (string? Correlation, string? Currency, FiscalZCounterApiResponse? Error) Preflight(
        HttpContext context, Guid site, Guid identity, string? currency)
    {
        var correlation = ResolveSingleHeader(context, SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName);
        if (correlation is null)
            return (null, null, Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable"));
        var normalized = currency?.Trim().ToUpperInvariant();
        if (site == Guid.Empty || identity == Guid.Empty || string.IsNullOrWhiteSpace(normalized) ||
            !FiscalZCounterResetAuthorization.IsInScope(context.User, site, identity, normalized))
            return (correlation, normalized, Error("fiscal_z_counter_scope_forbidden", 403,
                "The authenticated service identity is not authorized for the requested fiscal scope.", correlation));
        return (correlation, normalized, null);
    }

    private static FiscalZCounterApiResponse Map(FiscalZCounterResetResult result, string correlation) => result.Outcome switch
    {
        FiscalZCounterResetOutcome.Created => new(true, "fiscal_z_counter_reset", correlation, 201, result.State, result.Reset),
        FiscalZCounterResetOutcome.Replayed when result.Reset is not null => new(true, "fiscal_z_counter_reset_replayed", correlation, 200, result.State, result.Reset),
        FiscalZCounterResetOutcome.Replayed => new(true, "fiscal_z_counter_state_read", correlation, 200, result.State),
        FiscalZCounterResetOutcome.NotFound => Error("fiscal_z_counter_state_not_found", 404, result.SafeMessage, correlation),
        FiscalZCounterResetOutcome.StateInitializationRequired => Error("fiscal_z_state_initialization_required", 409, result.SafeMessage, correlation),
        FiscalZCounterResetOutcome.StaleStateVersion => Error("fiscal_z_state_version_conflict", 409, result.SafeMessage, correlation),
        FiscalZCounterResetOutcome.Conflict => Error("fiscal_z_counter_reset_semantic_conflict", 409, result.SafeMessage, correlation),
        FiscalZCounterResetOutcome.ConcurrentFiscalClose => Error("fiscal_z_counter_operation_in_progress", 409, result.SafeMessage, correlation),
        FiscalZCounterResetOutcome.ArithmeticOverflow => Error("fiscal_z_counter_reset_overflow", 422, result.SafeMessage, correlation),
        FiscalZCounterResetOutcome.UnknownCommitOutcome => Error("fiscal_z_counter_reset_unknown_commit_outcome", 503, result.SafeMessage, correlation),
        FiscalZCounterResetOutcome.PersistenceFailure => Error("fiscal_z_counter_reset_persistence_failed", 503, result.SafeMessage, correlation),
        _ => Error("fiscal_z_counter_reset_invalid_request", 400, result.SafeMessage, correlation)
    };

    private static string? ResolveSingleHeader(HttpContext context, string name)
    {
        var values = context.Request.Headers[name];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n') ? null : value;
    }

    private static FiscalZCounterApiResponse Error(string code, int status, string? message, string correlation) =>
        new(false, code, correlation, status, Message: message ?? "The fiscal Z Counter operation failed safely.");
}
