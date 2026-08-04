using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalZCloseStateInitializationAuthorization
{
    public const string PolicyName = "FiscalZCloseStateInitialization";
    public const string Permission = "fiscal_z_close_state.initialize";

    public static bool IsScopedPermission(string permission) => permission == Permission;

    public static bool IsInScope(ClaimsPrincipal principal, Guid sitePosServerId, Guid fiscalIdentityId) =>
        HasScope(principal, FiscalXReadingAuthorization.SitePosServerScopeClaimType, sitePosServerId) &&
        HasScope(principal, FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, fiscalIdentityId);

    private static bool HasScope(ClaimsPrincipal principal, string claimType, Guid value)
    {
        var expected = value.ToString("D");
        return principal.FindAll(claimType)
            .Any(claim => claim.Value == "*" || string.Equals(claim.Value, expected, StringComparison.OrdinalIgnoreCase));
    }
}
