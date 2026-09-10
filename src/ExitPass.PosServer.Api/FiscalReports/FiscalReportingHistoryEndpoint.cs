using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalReportingHistoryEndpoint
{
    public static async Task<IResult> ReadAsync(Guid sitePosServerId, Guid fiscalIdentityId, string? currencyCode, string kind,
        FiscalReportingHistoryService service, HttpContext context, CancellationToken cancellationToken)
    {
        var currency=(currencyCode??string.Empty).Trim().ToUpperInvariant();
        if (!FiscalZReadingAuthorization.IsInScope(context.User,sitePosServerId,fiscalIdentityId,currency)) return Results.NotFound();
        var result=await service.ReadAsync(sitePosServerId,fiscalIdentityId,currency,cancellationToken:cancellationToken).ConfigureAwait(false);
        if (result.Code=="fiscal_reporting_history_unavailable") return Results.Json(new { succeeded=false,code=result.Code,message=result.SafeMessage },statusCode:503);
        if (result.Code!="fiscal_reporting_history_read") return Results.Json(new { succeeded=false,code=result.Code,message=result.SafeMessage },statusCode:409);
        var readings=kind=="X"?result.XReadings:result.ZReadings;
        context.Response.Headers.CacheControl="private, no-store";
        return Results.Ok(new { succeeded=true,code="fiscal_reporting_history_read",currentPeriod=result.CurrentPeriod,readings });
    }
}
