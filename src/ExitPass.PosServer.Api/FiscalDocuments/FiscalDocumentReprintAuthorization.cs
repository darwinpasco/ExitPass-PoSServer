using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentReprintAuthorization
{
    public const string RecordPolicyName = "FiscalDocumentReprintRecord";
    public const string RecordPermission = "fiscal_document.reprint.record";

    public static bool IsReprintPermission(string permission) => permission == RecordPermission;

    public static bool IsInScope(ClaimsPrincipal principal, Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode) =>
        Exact(principal, FiscalXReadingAuthorization.SitePosServerScopeClaimType, sitePosServerId.ToString("D")) &&
        Exact(principal, FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, fiscalIdentityId.ToString("D")) &&
        Exact(principal, FiscalZReadingAuthorization.CurrencyScopeClaimType, currencyCode);

    public static bool IsHostingAuthorityAllowed(ClaimsPrincipal principal, IHostEnvironment environment) =>
        !environment.IsProduction() || string.Equals(
            principal.FindFirstValue(BirSalesSummaryAuthorization.AuthorityClassClaimType),
            BirSalesSummaryAuthorization.ProductionAuthorityClass,
            StringComparison.Ordinal);

    private static bool Exact(ClaimsPrincipal principal, string claimType, string value) =>
        principal.FindAll(claimType).Any(claim =>
            claim.Value != "*" && string.Equals(claim.Value, value, StringComparison.OrdinalIgnoreCase));
}
