using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class FiscalDocumentVoidSemanticRequestHasher
{
    public const string Algorithm = "SHA-256";
    public const string Version = "sha256:void:v1";
    public const string Status = "calculated";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Hash(FiscalDocumentVoidCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var json = Canonicalize(command);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Canonicalize(FiscalDocumentVoidCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var payload = new SortedDictionary<string, object?>
        {
            ["business_day_date"] = command.BusinessDayDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["fiscal_document_id"] = command.FiscalDocumentId,
            ["reason_code"] = NormalizeCode(command.ReasonCode),
            ["reason_text"] = Normalize(command.ReasonText),
            ["requested_by_ref"] = Normalize(command.RequestedByRef),
            ["source_system_ref"] = Normalize(command.SourceSystemRef)
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static string? NormalizeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : string.Join(' ', value.Trim().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
