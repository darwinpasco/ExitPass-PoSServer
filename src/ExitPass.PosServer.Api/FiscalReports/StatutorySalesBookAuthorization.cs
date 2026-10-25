using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class StatutorySalesBookAuthorization
{
    public const string ReadPolicyName = "FiscalStatutorySalesBookRead";
    public const string ExportPolicyName = "FiscalStatutorySalesBookExport";
    public const string ReadPermission = "fiscal_statutory_sales_book.read";
    public const string ExportPermission = "fiscal_statutory_sales_book.export";

    public static bool IsInScope(ClaimsPrincipal principal, Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode) =>
        FiscalZReadingAuthorization.IsInScope(principal, sitePosServerId, fiscalIdentityId, currencyCode);

    public static bool IsHostingAuthorityAllowed(ClaimsPrincipal principal, IHostEnvironment environment) =>
        BirSalesSummaryAuthorization.IsHostingAuthorityAllowed(principal, environment);
}
