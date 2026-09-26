using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class CloseableFiscalBusinessDateEndpoint
{
    public static async Task<IResult> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string? currencyCode,
        CloseableFiscalBusinessDateService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var currency = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!FiscalZReadingAuthorization.IsInScope(context.User, sitePosServerId, fiscalIdentityId, currency))
        {
            return Results.NotFound();
        }

        var result = await service.ReadAsync(
            sitePosServerId,
            fiscalIdentityId,
            currency,
            cancellationToken).ConfigureAwait(false);
        if (result.Code == "closeable_fiscal_business_dates_unavailable")
        {
            return Results.Json(new
            {
                succeeded = false,
                code = result.Code,
                message = result.SafeMessage
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        if (result.Code != "closeable_fiscal_business_dates_read")
        {
            return Results.Json(new
            {
                succeeded = false,
                code = result.Code,
                message = result.SafeMessage
            }, statusCode: StatusCodes.Status409Conflict);
        }

        context.Response.Headers.CacheControl = "private, no-store";
        return Results.Ok(new
        {
            succeeded = true,
            code = result.Code,
            result.SitePosServerId,
            result.FiscalIdentityId,
            result.CurrencyCode,
            fiscalBusinessDates = result.FiscalBusinessDates
        });
    }
}
