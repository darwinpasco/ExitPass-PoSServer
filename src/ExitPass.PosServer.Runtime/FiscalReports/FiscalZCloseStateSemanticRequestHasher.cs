using System.Security.Cryptography;
using System.Text;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalZCloseStateSemanticRequestHasher
{
    public static string Compute(InitializeFiscalZCloseStateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalSource(command)))).ToLowerInvariant();
    }

    public static string CanonicalSource(InitializeFiscalZCloseStateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return string.Join('\n',
        [
            FiscalZCloseStateContract.InitializationSemanticHashVersion,
            $"operationReference={command.OperationReference}",
            $"sitePosServerId={command.SitePosServerId:D}",
            $"fiscalIdentityId={command.FiscalIdentityId:D}",
            $"currencyCode={command.CurrencyCode}",
            $"provenance={command.Provenance}",
            $"resetCounterValue={command.ResetCounterValue}",
            $"zCounterValue={command.ZCounterValue}",
            $"grandTotalAmountMinorUnits={command.GrandTotalAmountMinorUnits}",
            $"approvalReference={command.ApprovalReference}"
        ]);
    }
}
