using System.Security.Claims;

namespace ExitPass.PosServer.Api.FiscalReports;

public static class AuditTrailAuthorization
{
    public const string ReadPolicyName = "FiscalAuditTrailRead";
    public const string ExportPolicyName = "FiscalAuditTrailExport";
    public const string ReadPermission = "fiscal_audit_trail.read";
    public const string ExportPermission = "fiscal_audit_trail.export";

    public static bool IsInScope(ClaimsPrincipal principal, Guid sitePosServerId)
    {
        var expected = sitePosServerId.ToString("D");
        return principal.FindAll(FiscalXReadingAuthorization.SitePosServerScopeClaimType)
            .Any(claim => claim.Value == "*" || string.Equals(claim.Value, expected, StringComparison.OrdinalIgnoreCase));
    }
}
