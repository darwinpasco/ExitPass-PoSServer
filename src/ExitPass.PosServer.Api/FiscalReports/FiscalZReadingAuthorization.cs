using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class FiscalZReadingAuthorization
{
    public const string ClosePolicyName = "FiscalZReadingClose";
    public const string ReadPolicyName = "FiscalZReadingRead";
    public const string ClosePermission = "fiscal_z_reading.close";
    public const string ReadPermission = "fiscal_z_reading.read";
    public const string CurrencyScopeClaimType = "pos_server_currency_scope";

    public static bool IsZReadingPermission(string permission) => permission is ClosePermission or ReadPermission;

    public static bool IsInScope(
        ClaimsPrincipal principal,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode) =>
        FiscalXReadingAuthorization.IsInScope(principal, sitePosServerId, fiscalIdentityId) &&
        principal.FindAll(CurrencyScopeClaimType).Any(claim =>
            claim.Value == "*" || string.Equals(claim.Value, currencyCode, StringComparison.Ordinal));
}
