using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class MonthlyESalesAuthorization
{
    public const string ReadPolicyName = "MonthlyESalesRead";
    public const string ExportPolicyName = "MonthlyESalesExport";
    public const string ReadPermission = "monthly_esales.read";
    public const string ExportPermission = "monthly_esales.export";

    public static bool IsInScope(ClaimsPrincipal principal, Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode) =>
        FiscalZReadingAuthorization.IsInScope(principal, sitePosServerId, fiscalIdentityId, currencyCode);

    public static bool IsHostingAuthorityAllowed(ClaimsPrincipal principal, IHostEnvironment environment) =>
        BirSalesSummaryAuthorization.IsHostingAuthorityAllowed(principal, environment);
}
