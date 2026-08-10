using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class AnnexE1Authorization
{
    public const string RecordFactPolicy = "AnnexE1AccountingFactRecord";
    public const string AttestZeroPolicy = "AnnexE1KnownZeroAttest";
    public const string GeneratePolicy = "AnnexE1Generate";
    public const string ReadPolicy = "AnnexE1Read";
    public const string DownloadPolicy = "AnnexE1Download";
    public const string CorrectPolicy = "AnnexE1Correct";
    public const string RecordFactPermission = "fiscal_annex_e.accounting_fact.record";
    public const string AttestZeroPermission = "fiscal_annex_e.known_zero.attest";
    public const string GeneratePermission = "fiscal_annex_e.generate";
    public const string ReadPermission = "fiscal_annex_e.read";
    public const string DownloadPermission = "fiscal_annex_e.export";
    public const string CorrectPermission = "fiscal_annex_e.correct";

    public static bool IsInScope(ClaimsPrincipal principal, Guid site, Guid identity, string currency) =>
        FiscalZReadingAuthorization.IsInScope(principal, site, identity, currency);

    public static bool IsHostingAuthorityAllowed(ClaimsPrincipal principal, IHostEnvironment environment) =>
        BirSalesSummaryAuthorization.IsHostingAuthorityAllowed(principal, environment);

    public static bool HasPermission(ClaimsPrincipal principal, string permission) =>
        principal.FindAll(FiscalDocuments.SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType)
            .Any(claim => string.Equals(claim.Value, permission, StringComparison.Ordinal));
}
