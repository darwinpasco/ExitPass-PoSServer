using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record ResolveFiscalBusinessDateRequest(
    Guid SitePosServerId,
    Guid FiscalDocumentTypeCodeId,
    DateTimeOffset FiscalEventAt,
    string? UpstreamFinalityReference);

public sealed record ResolveFiscalBusinessDateResponse(
    bool Succeeded,
    string Code,
    string Message,
    DateOnly? BusinessDayDate = null,
    DateTimeOffset? PeriodStartAt = null,
    DateTimeOffset? PeriodEndAt = null,
    string? ReportingTimezoneName = null,
    TimeOnly? BusinessDayCutoffLocalTime = null,
    bool? IdempotencyRecordExists = null,
    Guid? LinkedFiscalDocumentId = null,
    string? ExistingSemanticRequestHash = null);

public static class FiscalBusinessDateResolutionEndpoint
{
    public static async Task<IResult> ResolveAsync(
        ResolveFiscalBusinessDateRequest request,
        FiscalBusinessDateResolutionService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ResolveAsync(
                new FiscalBusinessDateResolutionQuery(
                    request.SitePosServerId,
                    request.FiscalDocumentTypeCodeId,
                    request.FiscalEventAt,
                    request.UpstreamFinalityReference ?? string.Empty),
                cancellationToken).ConfigureAwait(false);
            return Results.Ok(new ResolveFiscalBusinessDateResponse(
                true,
                "fiscal_business_date_resolved",
                "The POS Server fiscal business date was resolved from the immutable event timestamp.",
                result.BusinessDayDate,
                result.PeriodStartAt,
                result.PeriodEndAt,
                result.ReportingTimezoneName,
                result.BusinessDayCutoffLocalTime,
                result.IdempotencyRecordExists,
                result.LinkedFiscalDocumentId,
                result.ExistingSemanticRequestHash));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ResolveFiscalBusinessDateResponse(
                false,
                "fiscal_business_date_request_invalid",
                ex.Message));
        }
        catch (FiscalBusinessDateResolutionUnavailableException ex)
        {
            return Results.Json(new ResolveFiscalBusinessDateResponse(
                false,
                "fiscal_business_date_unavailable",
                ex.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
