using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalZReadingSemanticRequestHasher
{
    public static string Compute(FiscalZReadingCommand command, FiscalZReadingPeriod period) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalSource(command, period)))).ToLowerInvariant();

    public static string CanonicalSource(FiscalZReadingCommand command, FiscalZReadingPeriod period)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("semanticHashVersion", FiscalZReadingContract.SemanticHashVersion);
            writer.WriteString("operationKey", command.OperationKey);
            writer.WriteString("sitePosServerId", command.SitePosServerId);
            writer.WriteString("fiscalIdentityId", command.FiscalIdentityId);
            writer.WriteString("currencyCode", command.CurrencyCode);
            writer.WriteString("reportKind", FiscalZReadingContract.ReportKind);
            writer.WriteString("fiscalReportingPeriodId", period.FiscalReportingPeriodId);
            writer.WriteString("businessDayDate", period.BusinessDayDate.ToString("yyyy-MM-dd"));
            writer.WriteString("periodStartAt", period.PeriodStartAt.ToUniversalTime().ToString("O"));
            writer.WriteString("periodEndAt", period.PeriodEndAt.ToUniversalTime().ToString("O"));
            writer.WriteString("reportingTimezoneName", period.ReportingTimezoneName);
            writer.WriteString("businessDayCutoffLocalTime", period.BusinessDayCutoffLocalTime.ToString("HH:mm:ss.fffffff"));
            writer.WriteNumber("periodSequence", period.PeriodSequence);
            if (period.ExpectedPriorPeriodId.HasValue) writer.WriteString("expectedPriorPeriodId", period.ExpectedPriorPeriodId.Value);
            else writer.WriteNull("expectedPriorPeriodId");
            writer.WriteNumber("expectedStateVersion", command.ExpectedStateVersion);
            writer.WriteString("contractVersion", FiscalZReadingContract.ContractVersion);
            writer.WriteString("closeIntent", "atomic_open_to_closed");
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string ComputeTransition(
        string reportSemanticHash,
        long previousResetCounter,
        long resultingResetCounter,
        long previousZCounter,
        long resultingZCounter,
        long previousGta,
        long currentGta,
        long resultingGta)
    {
        var canonical = string.Join('|',
            FiscalZReadingContract.StateTransitionSemanticHashVersion,
            reportSemanticHash,
            previousResetCounter,
            resultingResetCounter,
            previousZCounter,
            resultingZCounter,
            previousGta,
            currentGta,
            resultingGta);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
