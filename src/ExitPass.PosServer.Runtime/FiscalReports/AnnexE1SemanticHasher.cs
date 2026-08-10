using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class AnnexE1SemanticHasher
{
    public static string ComputeFact(AnnexE1PeriodFactCommand command)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("version", AnnexE1Contract.FactSemanticHashVersion);
            writer.WriteString("sitePosServerId", command.SitePosServerId);
            writer.WriteString("fiscalIdentityId", command.FiscalIdentityId);
            writer.WriteString("currencyCode", command.CurrencyCode);
            writer.WriteString("fiscalReportingPeriodId", command.FiscalReportingPeriodId);
            writer.WriteString("factType", command.FactType);
            writer.WriteString("factStatus", command.FactStatus);
            writer.WriteNumber("amountMinorUnits", command.AmountMinorUnits);
            writer.WriteNumber("sourceDocumentCount", command.SourceDocumentCount);
            WriteNullable(writer, "firstSourceReference", command.FirstSourceReference);
            WriteNullable(writer, "lastSourceReference", command.LastSourceReference);
            WriteNullable(writer, "sourceEventReference", command.SourceEventReference);
            writer.WriteString("approvalReference", command.ApprovalReference);
            WriteNullable(writer, "supersedesFactId", command.SupersedesFactId);
            WriteNullable(writer, "correctionReason", command.CorrectionReason);
            writer.WriteEndObject();
        }
        return Hash(stream.ToArray());
    }

    public static string ComputeWorkbook(
        AnnexE1GenerationCommand command,
        IReadOnlyList<AnnexE1RowInputs> orderedSources,
        AnnexE1Header header)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("version", AnnexE1Contract.SemanticHashVersion);
            writer.WriteString("sitePosServerId", command.SitePosServerId);
            writer.WriteString("fiscalIdentityId", command.FiscalIdentityId);
            writer.WriteString("currencyCode", command.CurrencyCode);
            writer.WriteNumber("calendarYear", command.CalendarYear);
            writer.WriteNumber("calendarMonth", command.CalendarMonth);
            writer.WriteString("profile", command.Profile);
            writer.WriteString("calculationProfile", AnnexE1Contract.CalculationProfile);
            writer.WriteString("calculationProfileSha256", AnnexE1Contract.CalculationProfileSha256);
            writer.WriteString("templateSha256", AnnexE1Contract.OfficialTemplateSha256);
            writer.WriteString("rendererVersion", AnnexE1Contract.RendererVersion);
            WriteNullable(writer, "supersedesWorkbookId", command.SupersedesWorkbookId);
            WriteNullable(writer, "correctionReason", command.CorrectionReason);
            WriteNullable(writer, "correctionApprovalReference", command.CorrectionApprovalReference);
            writer.WriteStartObject("header");
            writer.WriteString("taxpayerName", header.TaxpayerName);
            writer.WriteString("taxpayerAddress", header.TaxpayerAddress);
            writer.WriteString("tin", header.Tin);
            writer.WriteString("softwareName", header.SoftwareName);
            writer.WriteString("softwareVersion", header.SoftwareVersion);
            writer.WriteString("releaseNumber", header.ReleaseNumber);
            writer.WriteString("releaseDate", header.ReleaseDate);
            writer.WriteString("posSerialNumber", header.PosSerialNumber);
            writer.WriteString("machineIdentificationNumber", header.MachineIdentificationNumber);
            writer.WriteString("posTerminalNumber", header.PosTerminalNumber);
            writer.WriteEndObject();
            writer.WriteStartArray("sources");
            foreach (var source in orderedSources)
            {
                writer.WriteStartObject();
                writer.WriteString("periodId", source.FiscalReportingPeriodId);
                writer.WriteNumber("periodSequence", source.PeriodSequence);
                writer.WriteString("governingZReportId", source.GoverningZReportId);
                writer.WriteString("birSalesSummaryReportId", source.BirSalesSummaryReportId);
                writer.WriteString("birSalesSummarySemanticHash", source.BirSalesSummarySemanticHash);
                writer.WriteString("businessDayDate", source.BusinessDayDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteStartArray("factIds");
                foreach (var value in source.FactIds) writer.WriteStringValue(value);
                writer.WriteEndArray();
                writer.WriteStartArray("factSemanticHashes");
                foreach (var value in source.FactSemanticHashes) writer.WriteStringValue(value);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Hash(stream.ToArray());
    }

    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void WriteNullable(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null) writer.WriteNull(name); else writer.WriteString(name, value);
    }

    private static void WriteNullable(Utf8JsonWriter writer, string name, Guid? value)
    {
        if (value is null) writer.WriteNull(name); else writer.WriteString(name, value.Value);
    }
}
