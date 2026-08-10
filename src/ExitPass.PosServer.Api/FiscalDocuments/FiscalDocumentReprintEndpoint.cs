using System.Security.Claims;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record RecordFiscalDocumentReprintRequest(
    string? OperationKey,
    Guid? SitePosServerId,
    Guid? FiscalIdentityId,
    string? CurrencyCode,
    string? ReasonCode);

public sealed record FiscalDocumentReprintApiResponse(
    bool Succeeded,
    string Code,
    string CorrelationId,
    int HttpStatusCode,
    FiscalDocumentReprintRecord? Reprint = null,
    string? Message = null);

public static class FiscalDocumentReprintEndpoint
{
    public static async Task<FiscalDocumentReprintApiResponse> RecordAsync(
        Guid fiscalDocumentId,
        RecordFiscalDocumentReprintRequest request,
        FiscalDocumentReprintService service,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null)
            return Error("missing_correlation_id", 400, "A single safe X-Correlation-Id header is required.", "unavailable");
        if (!FiscalDocumentReprintAuthorization.IsHostingAuthorityAllowed(context.User, environment))
            return Error("fiscal_document_reprint_not_found", 404, "The requested fiscal document is unavailable.", correlation);
        if (request.SitePosServerId is null || request.FiscalIdentityId is null || string.IsNullOrWhiteSpace(request.CurrencyCode) ||
            !FiscalDocumentReprintAuthorization.IsInScope(
                context.User, request.SitePosServerId.Value, request.FiscalIdentityId.Value, request.CurrencyCode.Trim().ToUpperInvariant()))
            return Error("fiscal_document_reprint_not_found", 404, "The requested fiscal document is unavailable.", correlation);

        var principal = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable";
        var result = await service.RecordAsync(new(
            fiscalDocumentId,
            request.SitePosServerId.Value,
            request.FiscalIdentityId.Value,
            request.CurrencyCode,
            request.OperationKey ?? string.Empty,
            request.ReasonCode ?? string.Empty,
            principal,
            principal,
            correlation), cancellationToken).ConfigureAwait(false);
        return Map(result, correlation);
    }

    private static FiscalDocumentReprintApiResponse Map(FiscalDocumentReprintResult result, string correlation) => result.Outcome switch
    {
        FiscalDocumentReprintOutcome.Created => Success("fiscal_document_reprint_recorded", 201, result.Record!, correlation),
        FiscalDocumentReprintOutcome.Replayed => Success("fiscal_document_reprint_replayed", 200, result.Record!, correlation),
        FiscalDocumentReprintOutcome.Conflict => Error("fiscal_document_reprint_semantic_conflict", 409, result.SafeMessage, correlation),
        FiscalDocumentReprintOutcome.InvalidState => Error("fiscal_document_reprint_state_invalid", 409, result.SafeMessage, correlation),
        FiscalDocumentReprintOutcome.NotFound or FiscalDocumentReprintOutcome.ScopeMismatch =>
            Error("fiscal_document_reprint_not_found", 404, "The requested fiscal document is unavailable.", correlation),
        FiscalDocumentReprintOutcome.IntegrityFailure => Error("fiscal_document_reprint_integrity_failure", 409, result.SafeMessage, correlation),
        FiscalDocumentReprintOutcome.PersistenceFailure => Error("fiscal_document_reprint_unavailable", 503, result.SafeMessage, correlation),
        _ => Error("fiscal_document_reprint_invalid_request", 400, result.SafeMessage, correlation)
    };

    private static FiscalDocumentReprintApiResponse Success(
        string code, int status, FiscalDocumentReprintRecord record, string correlation) =>
        new(true, code, correlation, status, record);

    private static FiscalDocumentReprintApiResponse Error(string code, int status, string? message, string correlation) =>
        new(false, code, correlation, status, Message: message ?? "The fiscal reprint operation failed safely.");

    private static string? ResolveCorrelation(HttpContext context)
    {
        var values = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n') ? null : value;
    }
}
