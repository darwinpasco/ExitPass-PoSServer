using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record InitializeFiscalZCloseStateRequest(
    string? OperationReference,
    Guid? SitePosServerId,
    Guid? FiscalIdentityId,
    string? CurrencyCode,
    string? Provenance,
    long? ResetCounterValue,
    long? ZCounterValue,
    long? GrandTotalAmountMinorUnits,
    string? ApprovalReference);

public sealed record InitializeFiscalZCloseStateResponse(
    bool Succeeded,
    string Code,
    string Message,
    string? ResultClassification = null,
    FiscalZCloseStateRecord? State = null,
    string? CorrelationId = null,
    string? ErrorPosture = null,
    int HttpStatusCode = StatusCodes.Status200OK);

public static class FiscalZCloseStateInitializationEndpoint
{
    public static async Task<InitializeFiscalZCloseStateResponse> InitializeAsync(
        InitializeFiscalZCloseStateRequest request,
        FiscalZCloseStateInitializationService service,
        HttpContext context,
        CancellationToken cancellationToken = default,
        ILogger? logger = null)
    {
        var correlationId = ResolveCorrelationId(context);
        if (request.SitePosServerId is null || request.FiscalIdentityId is null)
        {
            logger?.LogWarning("Fiscal Z close state initialization rejected: scope missing; correlation={CorrelationId}", correlationId);
            return Failure("invalid_request", "Fiscal Z close state scope is required.", correlationId, StatusCodes.Status400BadRequest);
        }
        if (!FiscalZCloseStateInitializationAuthorization.IsInScope(
                context.User, request.SitePosServerId.Value, request.FiscalIdentityId.Value))
        {
            logger?.LogWarning(
                "Fiscal Z close state initialization forbidden; site={SitePosServerId}; fiscalIdentity={FiscalIdentityId}; correlation={CorrelationId}",
                request.SitePosServerId, request.FiscalIdentityId, correlationId);
            return Failure("forbidden_scope", "Caller is not authorized for the requested fiscal scope.", correlationId, StatusCodes.Status403Forbidden);
        }

        var principal = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(principal))
        {
            logger?.LogWarning("Fiscal Z close state initialization rejected: service identity missing; correlation={CorrelationId}", correlationId);
            return Failure("unauthenticated_service_identity", "Authenticated service identity is required.", correlationId, StatusCodes.Status401Unauthorized);
        }

        logger?.LogInformation(
            "Fiscal Z close state initialization requested; site={SitePosServerId}; fiscalIdentity={FiscalIdentityId}; currency={Currency}; principal={Principal}; correlation={CorrelationId}",
            request.SitePosServerId, request.FiscalIdentityId, request.CurrencyCode, principal, correlationId);

        var result = await service.InitializeAsync(new InitializeFiscalZCloseStateCommand(
            request.OperationReference ?? string.Empty,
            request.SitePosServerId.Value,
            request.FiscalIdentityId.Value,
            request.CurrencyCode ?? string.Empty,
            request.Provenance ?? string.Empty,
            request.ResetCounterValue ?? -1,
            request.ZCounterValue ?? -1,
            request.GrandTotalAmountMinorUnits ?? -1,
            request.ApprovalReference ?? string.Empty,
            principal,
            principal,
            correlationId), cancellationToken).ConfigureAwait(false);

        logger?.LogInformation(
            "Fiscal Z close state initialization completed; site={SitePosServerId}; fiscalIdentity={FiscalIdentityId}; outcome={Outcome}; errorCode={ErrorCode}; correlation={CorrelationId}",
            request.SitePosServerId, request.FiscalIdentityId, result.Outcome, result.ErrorCode, correlationId);
        return Map(result, correlationId);
    }

    private static InitializeFiscalZCloseStateResponse Map(FiscalZCloseStateInitializationResult result, string correlationId)
    {
        if (result.Succeeded)
        {
            return new InitializeFiscalZCloseStateResponse(
                true, "accepted", result.Message,
                result.Outcome == FiscalZCloseStateInitializationOutcome.Replayed ? "idempotent_replay" : "initialized",
                result.State, correlationId,
                HttpStatusCode: result.Outcome == FiscalZCloseStateInitializationOutcome.Replayed
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status201Created);
        }

        var status = result.ErrorCode switch
        {
            FiscalZCloseStateInitializationErrorCode.SemanticConflict or
            FiscalZCloseStateInitializationErrorCode.StateAlreadyInitialized => StatusCodes.Status409Conflict,
            FiscalZCloseStateInitializationErrorCode.LockTimeout or
            FiscalZCloseStateInitializationErrorCode.TransientPersistenceFailure or
            FiscalZCloseStateInitializationErrorCode.UnknownCommitOutcome => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
        return Failure(ToCode(result.ErrorCode), result.Message, correlationId, status);
    }

    private static InitializeFiscalZCloseStateResponse Failure(string code, string message, string correlationId, int status) =>
        new(false, code, message, ResultClassification: "rejected", CorrelationId: correlationId,
            ErrorPosture: status == StatusCodes.Status503ServiceUnavailable ? "retry_after_service_recovery" : "do_not_retry_without_request_change",
            HttpStatusCode: status);

    private static string ToCode(FiscalZCloseStateInitializationErrorCode code) => code switch
    {
        FiscalZCloseStateInitializationErrorCode.InvalidProvenance => "invalid_initialization_provenance",
        FiscalZCloseStateInitializationErrorCode.InvalidLegacyImport => "invalid_legacy_import",
        FiscalZCloseStateInitializationErrorCode.StateAlreadyInitialized => "state_already_initialized",
        FiscalZCloseStateInitializationErrorCode.SemanticConflict => "initialization_semantic_conflict",
        FiscalZCloseStateInitializationErrorCode.SitePosServerUnavailable => "site_pos_server_unavailable",
        FiscalZCloseStateInitializationErrorCode.FiscalIdentityUnavailable => "fiscal_identity_unavailable",
        FiscalZCloseStateInitializationErrorCode.FiscalIdentityDisabled => "fiscal_identity_disabled",
        FiscalZCloseStateInitializationErrorCode.ContractUnavailable => "reporting_contract_unavailable",
        FiscalZCloseStateInitializationErrorCode.ControlledCodeUnavailable => "controlled_code_unavailable",
        FiscalZCloseStateInitializationErrorCode.LockTimeout => "close_boundary_lock_timeout",
        FiscalZCloseStateInitializationErrorCode.TransientPersistenceFailure => "transient_persistence_failure",
        FiscalZCloseStateInitializationErrorCode.UnknownCommitOutcome => "unknown_commit_outcome",
        _ => "invalid_request"
    };

    private static string ResolveCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName].FirstOrDefault();
        return string.IsNullOrWhiteSpace(supplied) ? context.TraceIdentifier : supplied.Trim();
    }
}
