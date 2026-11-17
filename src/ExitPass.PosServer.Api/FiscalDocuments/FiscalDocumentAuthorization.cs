using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentAuthorization
{
    public const string CreatePolicyName = "FiscalDocumentCreate";
    public const string ReadPolicyName = "FiscalDocumentRead";
    public const string VoidPolicyName = "FiscalDocumentVoid";

    public const string CreatePermission = "fiscal_document.create";
    public const string ReadPermission = "fiscal_document.read";
    public const string VoidPermission = "fiscal_document.void";

    public static bool IsReadAllowed(
        ClaimsPrincipal principal,
        IHostEnvironment environment,
        Guid sitePosServerId,
        Guid? fiscalIdentityId) =>
        sitePosServerId != Guid.Empty &&
        fiscalIdentityId is { } identity &&
        identity != Guid.Empty &&
        IsHostingAuthorityAllowed(principal, environment) &&
        HasExactScope(
            principal,
            FiscalXReadingAuthorization.SitePosServerScopeClaimType,
            sitePosServerId.ToString("D")) &&
        HasExactScope(
            principal,
            FiscalXReadingAuthorization.FiscalIdentityScopeClaimType,
            identity.ToString("D"));

    private static bool IsHostingAuthorityAllowed(ClaimsPrincipal principal, IHostEnvironment environment) =>
        !environment.IsProduction() || string.Equals(
            principal.FindFirstValue(BirSalesSummaryAuthorization.AuthorityClassClaimType),
            BirSalesSummaryAuthorization.ProductionAuthorityClass,
            StringComparison.Ordinal);

    private static bool HasExactScope(ClaimsPrincipal principal, string claimType, string value) =>
        principal.FindAll(claimType).Any(claim =>
            claim.Value != "*" && string.Equals(claim.Value, value, StringComparison.OrdinalIgnoreCase));
}
