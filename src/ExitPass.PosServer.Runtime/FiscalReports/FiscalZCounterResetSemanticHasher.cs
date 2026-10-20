using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalZCounterResetSemanticHasher
{
    public static string Compute(FiscalZCounterResetCommand command)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("semanticHashVersion", FiscalZCounterResetContract.SemanticHashVersion);
            writer.WriteString("operationKey", command.OperationKey);
            writer.WriteString("sitePosServerId", command.SitePosServerId);
            writer.WriteString("fiscalIdentityId", command.FiscalIdentityId);
            writer.WriteString("currencyCode", command.CurrencyCode);
            writer.WriteNumber("expectedStateVersion", command.ExpectedStateVersion);
            writer.WriteString("reason", command.Reason);
            writer.WriteString("requestedByRef", command.RequestedByRef);
            writer.WriteEndObject();
        }
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }
}
