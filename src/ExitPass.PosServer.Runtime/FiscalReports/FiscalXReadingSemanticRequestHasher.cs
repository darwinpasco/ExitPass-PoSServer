using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalXReadingSemanticRequestHasher
{
    public static string Compute(FiscalXReadingCommand command, FiscalXReadingPeriod period) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalSource(command, period))))
            .ToLowerInvariant();

    public static string CanonicalSource(FiscalXReadingCommand command, FiscalXReadingPeriod period)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("semanticHashVersion", FiscalXReadingContract.SemanticHashVersion);
            writer.WriteString("operationKey", command.OperationKey.Trim());
            writer.WriteString("sitePosServerId", period.SitePosServerId.ToString("D"));
            writer.WriteString("fiscalIdentityId", period.FiscalIdentityId.ToString("D"));
            writer.WriteString("reportKind", FiscalXReadingContract.ReportKind);
            writer.WriteString("fiscalReportingPeriodId", period.FiscalReportingPeriodId.ToString("D"));
            writer.WriteString("businessDayDate", period.BusinessDayDate.ToString("yyyy-MM-dd"));
            writer.WriteString("periodStartAt", period.PeriodStartAt.ToUniversalTime().ToString("O"));
            writer.WriteString("periodEndAt", period.PeriodEndAt.ToUniversalTime().ToString("O"));
            writer.WriteString("reportingTimezoneName", period.ReportingTimezoneName);
            writer.WriteString("businessDayCutoffLocalTime", period.BusinessDayCutoffLocalTime.ToString("HH:mm:ss.fffffff"));
            writer.WriteString("currency", period.CurrencyCode);
            writer.WriteString("contractVersion", FiscalXReadingContract.ContractVersion);
            writer.WriteString("observedAt", command.ObservedAt.ToUniversalTime().ToString("O"));
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
