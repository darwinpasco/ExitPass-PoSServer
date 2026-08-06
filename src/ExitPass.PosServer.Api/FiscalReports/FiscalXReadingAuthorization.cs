using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalXReadingAuthorization
{
    public const string GeneratePolicyName = "FiscalXReadingGeneration";
    public const string ReadPolicyName = "FiscalXReadingRead";
    public const string GeneratePermission = "fiscal_x_reading.generate";
    public const string ReadPermission = "fiscal_x_reading.read";
    public const string SitePosServerScopeClaimType = "pos_server_site_pos_server_scope";
    public const string FiscalIdentityScopeClaimType = "pos_server_fiscal_identity_scope";

    public static bool IsXReadingPermission(string permission) =>
        permission is GeneratePermission or ReadPermission or FiscalReportOutputAuthorization.XExportPermission;

    public static bool IsInScope(ClaimsPrincipal principal, Guid sitePosServerId, Guid fiscalIdentityId) =>
        HasScope(principal, SitePosServerScopeClaimType, sitePosServerId) &&
        HasScope(principal, FiscalIdentityScopeClaimType, fiscalIdentityId);

    private static bool HasScope(ClaimsPrincipal principal, string claimType, Guid value)
    {
        var expected = value.ToString("D");
        return principal.FindAll(claimType)
            .Any(claim => claim.Value == "*" || string.Equals(claim.Value, expected, StringComparison.OrdinalIgnoreCase));
    }
}
