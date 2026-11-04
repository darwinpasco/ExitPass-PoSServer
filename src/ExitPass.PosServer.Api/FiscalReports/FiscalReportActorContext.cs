using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalReports;

internal sealed record FiscalReportActorContext(string ActorReference, string? ActorDisplayName, string ServiceIdentityReference)
{
    private const string ActorReferenceHeader = "X-Actor-Reference";
    private const string ActorDisplayNameHeader = "X-Actor-Display-Name";

    public static bool TryResolve(HttpContext context, out FiscalReportActorContext? actor)
    {
        actor = null;
        var service = context.User.FindFirstValue(ClaimTypes.NameIdentifier)?.Trim();
        if (!Safe(service)) return false;

        var actorReferences = context.Request.Headers[ActorReferenceHeader];
        var actorNames = context.Request.Headers[ActorDisplayNameHeader];
        if (actorReferences.Count == 0 && actorNames.Count == 0)
        {
            var serviceName = context.User.FindFirstValue(ClaimTypes.Name)?.Trim();
            actor = new(service!, Safe(serviceName) ? serviceName! : service!, service!);
            return true;
        }

        if (!context.User.HasClaim(
                SalesInvoiceHeaderProfileAdminAuthorization.FiscalReportActorDelegationClaimType,
                "true") ||
            actorReferences.Count != 1 ||
            actorNames.Count > 1)
        {
            return false;
        }

        var actorReference = actorReferences[0]?.Trim();
        if (!Safe(actorReference)) return false;

        if (actorNames.Count == 0)
        {
            actor = new(actorReference!, null, service!);
            return true;
        }

        var actorDisplayName = actorNames[0]?.Trim();
        if (!Safe(actorDisplayName)) return false;
        actor = new(actorReference!, actorDisplayName!, service!);
        return true;
    }

    private static bool Safe(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 200 &&
        !value.Contains('\r') && !value.Contains('\n');
}
