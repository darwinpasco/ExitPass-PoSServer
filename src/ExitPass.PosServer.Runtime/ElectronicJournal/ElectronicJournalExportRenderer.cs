using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.ElectronicJournal;

public sealed class ElectronicJournalExportRenderer
{
    public ElectronicJournalExport Render(
        ElectronicJournalPage page,
        ElectronicJournalQuery query,
        ElectronicJournalExportFormat format)
    {
        var bytes = format == ElectronicJournalExportFormat.Json ? Json(page) : Csv(page);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var identitySource = string.Join('|', ElectronicJournalContract.ExportVersion, query.SitePosServerId.ToString("D"),
            query.FiscalIdentityId.ToString("D"), query.CurrencyCode.ToUpperInvariant(),
            page.ThroughSequence.ToString(CultureInfo.InvariantCulture), format,
            query.FiscalReportingPeriodId?.ToString("D") ?? "~", query.FiscalDocumentReference ?? "~",
            query.FiscalDocumentNumber ?? "~", query.ZReadingReference ?? "~", query.EventType ?? "~",
            Timestamp(query.EffectiveFrom), Timestamp(query.EffectiveTo), Timestamp(query.RecordedFrom), Timestamp(query.RecordedTo),
            query.CorrelationReference ?? "~");
        var identity = $"ej-{ElectronicJournalCanonicalizer.Sha256(identitySource)}";
        var extension = format == ElectronicJournalExportFormat.Json ? "json" : "csv";
        return new(format,
            format == ElectronicJournalExportFormat.Json ? "application/json; charset=utf-8" : "text/csv; charset=utf-8",
            $"electronic-journal-{query.SitePosServerId:N}-{query.FiscalIdentityId:N}-{page.ThroughSequence:D20}-{identity[^12..]}.{extension}".ToLowerInvariant(),
            bytes, hash, identity, $"\"sha256-{hash}\"");
    }

    private static byte[] Json(ElectronicJournalPage page)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", ElectronicJournalContract.ExportVersion);
            writer.WriteString("chronologyVersion", page.ChronologyVersion);
            writer.WriteNumber("throughSequence", page.ThroughSequence);
            writer.WriteStartArray("events");
            foreach (var item in page.Events) WriteEvent(writer, item);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteEvent(Utf8JsonWriter w, ElectronicJournalEvent e)
    {
        w.WriteStartObject();
        w.WriteString("eventReference", e.EventReference); w.WriteString("eventType", e.EventType);
        w.WriteString("eventSchemaVersion", e.EventSchemaVersion); w.WriteString("sitePosServerId", e.SitePosServerId);
        w.WriteString("fiscalIdentityId", e.FiscalIdentityId); w.WriteString("currencyCode", e.CurrencyCode);
        w.WriteString("fiscalReportingPeriodId", e.FiscalReportingPeriodId);
        NullableGuid(w, "fiscalDocumentId", e.FiscalDocumentId); NullableGuid(w, "fiscalReportRequestId", e.FiscalReportRequestId);
        NullableGuid(w, "xZReportId", e.XZReportId); NullableGuid(w, "birSalesSummaryReportId", e.BirSalesSummaryReportId);
        NullableGuid(w, "reprintRequestId", e.ReprintRequestId);
        NullableGuid(w, "fiscalSequencePolicyId", e.FiscalSequencePolicyId);
        if (e.BusinessDayDate is null) w.WriteNull("businessDayDate");
        else w.WriteString("businessDayDate", e.BusinessDayDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        w.WriteNumber("streamSequence", e.StreamSequence); w.WriteString("effectiveAt", e.EffectiveAt);
        w.WriteString("recordedAt", e.RecordedAt); w.WriteString("actorReference", e.ActorReference);
        w.WriteString("serviceIdentityReference", e.ServiceIdentityReference); w.WriteString("correlationReference", e.CorrelationReference);
        w.WriteString("sourceTransitionReference", e.SourceTransitionReference); w.WriteString("sourceTransitionVersion", e.SourceTransitionVersion);
        if (e.IdempotencyReference is null) w.WriteNull("idempotencyReference"); else w.WriteString("idempotencyReference", e.IdempotencyReference);
        w.WriteString("semanticHashVersion", e.SemanticHashVersion); w.WriteString("semanticHash", e.SemanticHash);
        w.WriteString("integrityHashVersion", e.IntegrityHashVersion); w.WriteString("previousIntegrityHash", e.PreviousIntegrityHash);
        w.WriteString("integrityHash", e.IntegrityHash); w.WriteString("retentionPolicy", e.RetentionPolicy);
        w.WriteStartObject("facts");
        foreach (var pair in e.Facts.OrderBy(item => item.Key, StringComparer.Ordinal))
            if (pair.Value is null) w.WriteNull(pair.Key); else w.WriteString(pair.Key, pair.Value);
        w.WriteEndObject(); w.WriteEndObject();
    }

    private static byte[] Csv(ElectronicJournalPage page)
    {
        var rows = new List<string>
        {
            "sequence,event_reference,event_type,effective_at,recorded_at,business_day_date,fiscal_document_id,fiscal_report_request_id,source_transition_reference,correlation_reference,previous_integrity_hash,integrity_hash,facts_json"
        };
        rows.AddRange(page.Events.Select(e => string.Join(',', new[]
        {
            e.StreamSequence.ToString(CultureInfo.InvariantCulture), e.EventReference, e.EventType,
            e.EffectiveAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            e.RecordedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            e.BusinessDayDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            e.FiscalDocumentId?.ToString("D") ?? string.Empty, e.FiscalReportRequestId?.ToString("D") ?? string.Empty,
            e.SourceTransitionReference, e.CorrelationReference, e.PreviousIntegrityHash, e.IntegrityHash,
            ElectronicJournalCanonicalizer.CanonicalFactsJson(e.Facts)
        }.Select(Escape))));
        return Encoding.UTF8.GetBytes(string.Join("\r\n", rows) + "\r\n");
    }

    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static string Timestamp(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "~";

    private static void NullableGuid(Utf8JsonWriter writer, string name, Guid? value)
    {
        if (value is null) writer.WriteNull(name); else writer.WriteString(name, value.Value);
    }
}
