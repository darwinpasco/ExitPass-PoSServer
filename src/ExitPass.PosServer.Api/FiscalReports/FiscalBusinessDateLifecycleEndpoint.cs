using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record OpenFiscalBusinessDateRequest(
    string? OperationKey,
    Guid? SitePosServerId,
    Guid? FiscalIdentityId,
    string? CurrencyCode,
    DateOnly? BusinessDateLabel,
    bool ConfirmOpen);

public static class FiscalBusinessDateLifecycleEndpoint
{
    public static async Task<IResult> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string? currencyCode,
        FiscalBusinessDateLifecycleService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var currency = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!FiscalZReadingAuthorization.IsInScope(context.User, sitePosServerId, fiscalIdentityId, currency))
            return Results.NotFound();
        try
        {
            var snapshot = await service.ReadAsync(sitePosServerId, fiscalIdentityId, currency, cancellationToken).ConfigureAwait(false);
            context.Response.Headers.CacheControl = "private, no-store";
            return Results.Ok(new { succeeded = true, code = "fiscal_business_date_lifecycle_read", lifecycle = snapshot });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Npgsql.NpgsqlException)
        {
            return Results.Json(new { succeeded = false, code = "fiscal_business_date_lifecycle_unavailable", message = "Fiscal business-date readiness is unavailable." }, statusCode: 503);
        }
    }

    public static async Task<IResult> OpenAsync(
        OpenFiscalBusinessDateRequest request,
        FiscalBusinessDateLifecycleService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var currency = (request.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (request.SitePosServerId is null || request.FiscalIdentityId is null ||
            !FiscalZReadingAuthorization.IsInScope(context.User, request.SitePosServerId.Value, request.FiscalIdentityId.Value, currency))
            return Results.Json(new { succeeded = false, code = "fiscal_business_date_scope_forbidden" }, statusCode: 403);
        if (!FiscalReportActorContext.TryResolve(context, out var actor))
            return Results.BadRequest(new { succeeded = false, code = "fiscal_report_actor_invalid" });
        var correlation = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName].SingleOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(correlation))
            return Results.BadRequest(new { succeeded = false, code = "missing_correlation_id" });

        var result = await service.OpenAsync(new(
            request.OperationKey ?? string.Empty,
            request.SitePosServerId.Value,
            request.FiscalIdentityId.Value,
            currency,
            request.BusinessDateLabel ?? default,
            request.ConfirmOpen,
            actor!.ActorReference,
            actor.ServiceIdentityReference,
            correlation,
            actor.ActorDisplayName), cancellationToken).ConfigureAwait(false);

        var status = result.Outcome switch
        {
            OpenFiscalBusinessDateOutcome.Opened => 201,
            OpenFiscalBusinessDateOutcome.Replayed => 200,
            OpenFiscalBusinessDateOutcome.InvalidRequest => 400,
            OpenFiscalBusinessDateOutcome.PeriodAlreadyOpen or OpenFiscalBusinessDateOutcome.PriorPeriodUnresolved or OpenFiscalBusinessDateOutcome.Conflict => 409,
            _ => 503
        };
        return Results.Json(new
        {
            succeeded = result.Outcome is OpenFiscalBusinessDateOutcome.Opened or OpenFiscalBusinessDateOutcome.Replayed,
            code = $"fiscal_business_date_{result.Outcome.ToString().ToLowerInvariant()}",
            message = result.SafeMessage,
            lifecycle = result.Snapshot
        }, statusCode: status);
    }
}
