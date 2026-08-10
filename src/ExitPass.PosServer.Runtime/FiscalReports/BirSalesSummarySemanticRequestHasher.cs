using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class BirSalesSummarySemanticRequestHasher
{
    public static string Compute(BirSalesSummaryCommand command, Guid governingZReportId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("semanticHashVersion", BirSalesSummaryContract.SemanticHashVersion);
            writer.WriteString("operationKey", command.OperationKey);
            writer.WriteString("sitePosServerId", command.SitePosServerId);
            writer.WriteString("fiscalIdentityId", command.FiscalIdentityId);
            writer.WriteString("currencyCode", command.CurrencyCode);
            writer.WriteString("fiscalReportingPeriodId", command.FiscalReportingPeriodId);
            writer.WriteString("governingZReportId", governingZReportId);
            writer.WriteString("reportKind", BirSalesSummaryContract.ReportKind);
            writer.WriteString("contractVersion", BirSalesSummaryContract.ContractVersion);
            writer.WriteString("reportingProfile", BirSalesSummaryContract.ReportingProfile);
            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }
}
