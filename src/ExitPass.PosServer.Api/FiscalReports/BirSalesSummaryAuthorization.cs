using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class BirSalesSummaryAuthorization
{
    public const string GeneratePolicyName = "BirSalesSummaryGeneration";
    public const string ReadPolicyName = "BirSalesSummaryRead";
    public const string ExportPolicyName = "BirSalesSummaryExport";
    public const string GeneratePermission = "bir_sales_summary.generate";
    public const string ReadPermission = "bir_sales_summary.read";
    public const string ExportPermission = "bir_sales_summary.export";
    public const string AuthorityClassClaimType = "pos_server_authority_class";
    public const string ProductionAuthorityClass = "PRODUCTION";
    public const string DevelopmentAuthorityClass = "DEVELOPMENT";
    public const string FixtureAuthorityClass = "FIXTURE";

    public static bool IsSummaryPermission(string permission) =>
        permission is GeneratePermission or ReadPermission or ExportPermission;

    public static bool IsInScope(ClaimsPrincipal principal, Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode) =>
        FiscalZReadingAuthorization.IsInScope(principal, sitePosServerId, fiscalIdentityId, currencyCode);

    public static bool IsHostingAuthorityAllowed(ClaimsPrincipal principal, IHostEnvironment environment) =>
        !environment.IsProduction() || string.Equals(
            principal.FindFirstValue(AuthorityClassClaimType),
            ProductionAuthorityClass,
            StringComparison.Ordinal);
}
