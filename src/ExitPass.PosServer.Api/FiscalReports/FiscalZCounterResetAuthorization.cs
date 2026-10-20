using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalZCounterResetAuthorization
{
    public const string ReadPolicyName = "FiscalZCounterRead";
    public const string ResetPolicyName = "FiscalZCounterReset";
    public const string ReadPermission = "fiscal_z_counter.read";
    public const string ResetPermission = "fiscal_z_counter.reset";

    public static bool IsZCounterPermission(string permission) =>
        permission is ReadPermission or ResetPermission;

    public static bool IsInScope(
        ClaimsPrincipal principal,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode) =>
        FiscalZReadingAuthorization.IsInScope(principal, sitePosServerId, fiscalIdentityId, currencyCode);
}
