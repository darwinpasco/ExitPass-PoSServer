using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class FiscalDocumentReprintSemanticRequestHasher
{
    public static string Hash(FiscalDocumentReprintCommand command) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalize(command)))).ToLowerInvariant();

    public static string Canonicalize(FiscalDocumentReprintCommand command) =>
        string.Join('\n', new[]
        {
            $"profile={FiscalDocumentReprintContract.SemanticHashVersion}",
            $"fiscal_document_id={command.FiscalDocumentId:D}",
            $"site_pos_server_id={command.SitePosServerId:D}",
            $"fiscal_identity_id={command.FiscalIdentityId:D}",
            $"currency={command.CurrencyCode.ToUpperInvariant()}",
            $"reason={command.ReasonCode.ToLowerInvariant()}",
            $"actor={command.ActorReference}",
            $"service={command.ServiceIdentityReference}",
            string.Empty
        });
}
