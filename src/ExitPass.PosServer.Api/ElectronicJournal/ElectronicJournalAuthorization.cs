using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalReports;

namespace ExitPass.PosServer.Api.ElectronicJournal;

public static class ElectronicJournalAuthorization
{
    public const string ReadPolicyName = "ElectronicJournalRead";
    public const string ExportPolicyName = "ElectronicJournalExport";
    public const string IntegrityPolicyName = "ElectronicJournalIntegrityVerify";
    public const string ReadPermission = "electronic_journal.read";
    public const string ExportPermission = "electronic_journal.export";
    public const string IntegrityPermission = "electronic_journal.integrity.verify";

    public static bool IsElectronicJournalPermission(string permission) =>
        permission is ReadPermission or ExportPermission or IntegrityPermission;

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
