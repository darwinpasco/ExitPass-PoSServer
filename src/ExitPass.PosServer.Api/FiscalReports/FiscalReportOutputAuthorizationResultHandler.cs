using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class FiscalReportOutputAuthorizationResultHandler(
    ILogger<FiscalReportOutputAuthorizationResultHandler> logger) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded && IsGovernedOutputPath(context.Request.Path))
        {
            logger.LogWarning(
                "Governed fiscal output authorization denied. Path={Path} Principal={Principal} Correlation={Correlation}",
                Safe(context.Request.Path.Value),
                Safe(context.User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Safe(context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName].FirstOrDefault()));
        }

        await defaultHandler.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);
    }

    private static bool IsGovernedOutputPath(PathString path) =>
        path.StartsWithSegments("/v1/fiscal-reports/x-readings") ||
        path.StartsWithSegments("/v1/fiscal-reports/z-readings") ||
        path.StartsWithSegments("/v1/electronic-journal");

    private static string Safe(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n')
            ? "unavailable"
            : value;
}
