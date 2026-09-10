using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.ElectronicJournal;

public static class ElectronicJournalCanonicalizer
{
    public static string ComputeSemanticHash(ElectronicJournalAppendRequest request) =>
        Sha256(CanonicalSemanticText(request));

    public static string ComputeIntegrityHash(
        ElectronicJournalAppendRequest request,
        string eventReference,
        long streamSequence,
        DateTimeOffset recordedAt,
        string semanticHash,
        string previousIntegrityHash) =>
        Sha256(string.Join('\n', new[]
        {
            $"profile={ElectronicJournalContract.IntegrityHashVersion}",
            $"event_reference={eventReference}",
            $"stream_sequence={streamSequence.ToString(CultureInfo.InvariantCulture)}",
            $"recorded_at={Utc(recordedAt)}",
            $"semantic_hash={semanticHash}",
            $"previous_integrity_hash={previousIntegrityHash}",
            $"actor_ref={request.ActorReference}",
            $"service_identity_ref={request.ServiceIdentityReference}",
            $"correlation_ref={request.CorrelationReference}",
            $"retention_policy=fiscal_reconstruction_hold",
            string.Empty
        }));

    public static string CanonicalSemanticText(ElectronicJournalAppendRequest request) =>
        string.Join('\n', new[]
        {
            $"profile={ElectronicJournalContract.SemanticHashVersion}",
            $"event_schema={ElectronicJournalContract.EventSchemaVersion}",
            $"site_pos_server_id={request.SitePosServerId:D}",
            $"fiscal_identity_id={request.FiscalIdentityId:D}",
            $"currency={request.CurrencyCode.ToUpperInvariant()}",
            $"fiscal_reporting_period_id={request.FiscalReportingPeriodId:D}",
            $"event_type={request.EventType}",
            $"source_transition_ref={request.SourceTransitionReference}",
            $"source_transition_version={request.SourceTransitionVersion}",
            $"effective_at={Utc(request.EffectiveAt)}",
            $"fiscal_document_id={GuidText(request.FiscalDocumentId)}",
            $"fiscal_report_request_id={GuidText(request.FiscalReportRequestId)}",
            $"x_z_report_id={GuidText(request.XZReportId)}",
            $"bir_sales_summary_report_id={GuidText(request.BirSalesSummaryReportId)}",
            $"reprint_request_id={GuidText(request.ReprintRequestId)}",
            $"fiscal_sequence_policy_id={GuidText(request.FiscalSequencePolicyId)}",
            $"business_day_date={request.BusinessDayDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty}",
            $"idempotency_ref={request.IdempotencyReference ?? string.Empty}",
            $"printable_sales_invoice_text_sha256={(request.PrintableSalesInvoiceText is null ? string.Empty : Sha256(request.PrintableSalesInvoiceText))}",
            $"facts={CanonicalFactsJson(request.Facts)}",
            string.Empty
        });

    public static string CanonicalFactsJson(IReadOnlyDictionary<string, string?> facts)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var pair in facts.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                if (pair.Value is null) writer.WriteNull(pair.Key);
                else writer.WriteString(pair.Key, pair.Value);
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string GuidText(Guid? value) => value?.ToString("D") ?? string.Empty;
    private static string Utc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
}
