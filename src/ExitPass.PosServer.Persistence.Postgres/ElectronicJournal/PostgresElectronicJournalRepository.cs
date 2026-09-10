using System.Globalization;
using System.Text;
using System.Text.Json;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;

public sealed class PostgresElectronicJournalRepository(NpgsqlDataSource dataSource) : IElectronicJournalRepository
{
    public async Task<ElectronicJournalPageResult> ReadAsync(
        ElectronicJournalQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(query);
        if (validation is not null) return validation;
        if (!TryDecodeCursor(query.Cursor, out var afterSequence, out var cursorThrough))
            return new(ElectronicJournalOutcome.MalformedCursor, SafeMessage: "The Electronic Journal cursor is malformed.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var stream = await ReadStreamAsync(connection, query, cancellationToken).ConfigureAwait(false);
            if (stream is null) return new(ElectronicJournalOutcome.NotFound, SafeMessage: "The requested Electronic Journal scope is unavailable.");
            var through = query.ThroughSequence ?? cursorThrough ?? stream.Value.LastSequence;
            if (through < 0 || through > stream.Value.LastSequence || (cursorThrough is not null && query.ThroughSequence is not null && cursorThrough != query.ThroughSequence))
                return new(ElectronicJournalOutcome.MalformedCursor, SafeMessage: "The Electronic Journal cursor is inconsistent with the requested range.");

            var events = await ReadEventsAsync(connection, query, stream.Value.StreamId, afterSequence, through, query.PageSize + 1, cancellationToken).ConfigureAwait(false);
            var hasMore = events.Count > query.PageSize;
            var pageEvents = hasMore ? events.Take(query.PageSize).ToArray() : events;
            var next = hasMore ? EncodeCursor(pageEvents[^1].StreamSequence, through) : null;
            return new(ElectronicJournalOutcome.Success,
                new(pageEvents, next, through, ElectronicJournalContract.ChronologyVersion));
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException or JsonException)
        {
            return new(ElectronicJournalOutcome.PersistenceFailure, SafeMessage: "The Electronic Journal read failed safely.");
        }
    }

    public async Task<ElectronicJournalIntegrityOutcome> VerifyIntegrityAsync(
        ElectronicJournalQuery query,
        CancellationToken cancellationToken = default)
    {
        if (HasIntegrityUnsupportedFilters(query))
            return new(ElectronicJournalOutcome.InvalidRequest, SafeMessage: "Integrity verification accepts only one exact fiscal stream and an optional through-sequence boundary.");
        var validation = Validate(query with { PageSize = Math.Min(query.PageSize, ElectronicJournalContract.MaximumExportEvents), Cursor = null });
        if (validation is not null) return new(validation.Outcome, SafeMessage: validation.SafeMessage);
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var stream = await ReadStreamAsync(connection, query, cancellationToken).ConfigureAwait(false);
            if (stream is null) return new(ElectronicJournalOutcome.NotFound, SafeMessage: "The requested Electronic Journal scope is unavailable.");
            var through = query.ThroughSequence ?? stream.Value.LastSequence;
            var events = await ReadEventsAsync(connection, query with { PageSize = ElectronicJournalContract.MaximumExportEvents },
                stream.Value.StreamId, 0, through, ElectronicJournalContract.MaximumExportEvents + 1, cancellationToken).ConfigureAwait(false);
            var support = $"EJ-IV-{Guid.NewGuid():N}".ToUpperInvariant();
            if (events.Count > ElectronicJournalContract.MaximumExportEvents)
                return new(ElectronicJournalOutcome.RangeTooLarge, SafeMessage: "The requested integrity range exceeds the governed event limit.");

            var expectedSequence = 1L;
            var previousHash = ElectronicJournalContract.GenesisHash;
            foreach (var item in events)
            {
                if (item.StreamSequence != expectedSequence || !string.Equals(item.PreviousIntegrityHash, previousHash, StringComparison.Ordinal))
                    return IntegrityFailure(events, item.StreamSequence, "chronology_gap", support);
                var append = ToAppendRequest(item);
                var semantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(append);
                var integrity = ElectronicJournalCanonicalizer.ComputeIntegrityHash(
                    append, item.EventReference, item.StreamSequence, item.RecordedAt, semantic, previousHash);
                if (!string.Equals(semantic, item.SemanticHash, StringComparison.Ordinal) ||
                    !string.Equals(integrity, item.IntegrityHash, StringComparison.Ordinal))
                    return IntegrityFailure(events, item.StreamSequence, "integrity_hash_mismatch", support);
                previousHash = item.IntegrityHash;
                expectedSequence++;
            }
            if (through != events.Count || (through == stream.Value.LastSequence && !string.Equals(previousHash, stream.Value.LastHash, StringComparison.Ordinal)))
                return IntegrityFailure(events, expectedSequence, "stream_head_mismatch", support);
            return new(ElectronicJournalOutcome.Success,
                new(true, events.Count, events.Count == 0 ? 0 : 1, events.Count, null, support));
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException or JsonException)
        {
            return new(ElectronicJournalOutcome.PersistenceFailure, SafeMessage: "Electronic Journal integrity verification failed safely.");
        }
    }

    public async Task RecordAccessAsync(
        Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode,
        string action, string result, string actorReference, string serviceIdentityReference,
        string correlationReference, string supportReference, int eventCount,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand("""
            INSERT INTO pos.electronic_journal_access_audit(
                electronic_journal_access_audit_id,site_pos_server_id,fiscal_identity_id,currency_code,
                action_code_id,result_code_id,actor_ref,service_identity_ref,correlation_ref,support_reference,
                event_count,occurred_at,created_at)
            SELECT @id,@site,@identity,@currency,action.controlled_code_id,result.controlled_code_id,
                   @actor,@service,@correlation,@support,@count,clock_timestamp(),clock_timestamp()
            FROM pos.controlled_codes action
            JOIN pos.controlled_code_sets action_set ON action_set.controlled_code_set_id=action.controlled_code_set_id
            CROSS JOIN pos.controlled_codes result
            JOIN pos.controlled_code_sets result_set ON result_set.controlled_code_set_id=result.controlled_code_set_id
            WHERE action_set.code_set_key='electronic_journal_access_action' AND action.code_key=@action
              AND result_set.code_set_key='electronic_journal_access_result' AND result.code_key=@result
            """, connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid()); command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("identity", fiscalIdentityId); command.Parameters.AddWithValue("currency", currencyCode.ToUpperInvariant());
        command.Parameters.AddWithValue("actor", actorReference); command.Parameters.AddWithValue("service", serviceIdentityReference);
        command.Parameters.AddWithValue("correlation", correlationReference); command.Parameters.AddWithValue("support", supportReference);
        command.Parameters.AddWithValue("count", eventCount); command.Parameters.AddWithValue("action", action); command.Parameters.AddWithValue("result", result);
        if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            throw new InvalidOperationException("Electronic Journal access classification is unavailable.");
    }

    public static string EncodeCursor(long sequence, long throughSequence)
    {
        var text = $"v1:{sequence.ToString(CultureInfo.InvariantCulture)}:{throughSequence.ToString(CultureInfo.InvariantCulture)}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryDecodeCursor(string? cursor, out long sequence, out long? through)
    {
        sequence = 0; through = null;
        if (string.IsNullOrWhiteSpace(cursor)) return true;
        try
        {
            var normalized = cursor.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(normalized)).Split(':');
            if (parts.Length != 3 || parts[0] != "v1" ||
                !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out sequence) ||
                !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedThrough) ||
                sequence < 0 || parsedThrough < sequence) return false;
            through = parsedThrough; return true;
        }
        catch (FormatException) { return false; }
    }

    private static ElectronicJournalPageResult? Validate(ElectronicJournalQuery q)
    {
        if (q.SitePosServerId == Guid.Empty || q.FiscalIdentityId == Guid.Empty || q.CurrencyCode.Length != 3 ||
            q.CurrencyCode.Any(ch => ch is < 'A' or > 'Z') || q.PageSize < 1 || q.PageSize > ElectronicJournalContract.MaximumExportEvents)
            return new(ElectronicJournalOutcome.InvalidRequest, SafeMessage: "The Electronic Journal query is invalid.");
        if (!ValidRange(q.EffectiveFrom, q.EffectiveTo) || !ValidRange(q.RecordedFrom, q.RecordedTo))
            return new(ElectronicJournalOutcome.RangeTooLarge, SafeMessage: "The Electronic Journal time range is invalid or exceeds 31 days.");
        return null;
    }

    private static bool ValidRange(DateTimeOffset? from, DateTimeOffset? to) =>
        from is null && to is null || from is not null && to is not null && to > from && to - from <= TimeSpan.FromDays(ElectronicJournalContract.MaximumTimeRangeDays);

    private static bool HasIntegrityUnsupportedFilters(ElectronicJournalQuery q) =>
        q.FiscalReportingPeriodId is not null || q.FiscalDocumentReference is not null || q.FiscalDocumentNumber is not null ||
        q.ZReadingReference is not null || q.EventType is not null || q.EffectiveFrom is not null || q.EffectiveTo is not null ||
        q.RecordedFrom is not null || q.RecordedTo is not null || q.CorrelationReference is not null || q.Cursor is not null;

    private static async Task<(Guid StreamId, long LastSequence, string LastHash)?> ReadStreamAsync(
        NpgsqlConnection connection, ElectronicJournalQuery query, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT electronic_journal_stream_id,last_sequence_value,last_event_hash
            FROM pos.electronic_journal_streams
            WHERE site_pos_server_id=@site AND fiscal_identity_id=@identity AND currency_code=@currency
            """, connection);
        command.Parameters.AddWithValue("site", query.SitePosServerId); command.Parameters.AddWithValue("identity", query.FiscalIdentityId);
        command.Parameters.AddWithValue("currency", query.CurrencyCode.ToUpperInvariant());
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? (reader.GetGuid(0), reader.GetInt64(1), reader.GetString(2)) : null;
    }

    private static async Task<IReadOnlyList<ElectronicJournalEvent>> ReadEventsAsync(
        NpgsqlConnection connection, ElectronicJournalQuery q, Guid streamId, long after, long through, int limit, CancellationToken ct)
    {
        var sql = new StringBuilder("""
            SELECT e.event_reference,type.code_key,e.event_schema_version,e.site_pos_server_id,e.fiscal_identity_id,e.currency_code,
              e.fiscal_reporting_period_id,e.fiscal_document_id,e.fiscal_report_request_id,e.x_z_report_id,e.bir_sales_summary_report_id,
              e.reprint_request_id,e.fiscal_sequence_policy_id,e.business_day_date,e.stream_sequence_value,e.effective_at,e.recorded_at,e.actor_ref,
              e.service_identity_ref,e.correlation_ref,e.source_transition_ref,e.source_transition_version,e.idempotency_ref,
              e.semantic_hash_version,e.semantic_hash,e.integrity_hash_version,e.previous_integrity_hash,e.integrity_hash,
              retention.code_key,e.event_facts,e.journal_context::text
            FROM pos.electronic_journal_records e
            JOIN pos.controlled_codes type ON type.controlled_code_id=e.journal_record_type_code_id
            JOIN pos.controlled_codes retention ON retention.controlled_code_id=e.retention_policy_code_id
            LEFT JOIN pos.fiscal_documents document ON document.fiscal_document_id=e.fiscal_document_id
            LEFT JOIN pos.x_z_reports report ON report.x_z_report_id=e.x_z_report_id
            WHERE e.is_canonical AND e.electronic_journal_stream_id=@stream
              AND e.stream_sequence_value>@after AND e.stream_sequence_value<=@through
            """);
        if (q.FiscalReportingPeriodId is not null) sql.Append(" AND e.fiscal_reporting_period_id=@period");
        if (q.FiscalDocumentReference is not null) sql.Append(" AND e.fiscal_document_id=@document_id");
        if (q.FiscalDocumentNumber is not null) sql.Append(" AND document.fiscal_document_number=@document_number");
        if (q.ZReadingReference is not null) sql.Append(" AND report.report_reference=@z_reference");
        if (q.EventType is not null) sql.Append(" AND type.code_key=@event_type");
        if (q.EffectiveFrom is not null) sql.Append(" AND e.effective_at>=@effective_from AND e.effective_at<@effective_to");
        if (q.RecordedFrom is not null) sql.Append(" AND e.recorded_at>=@recorded_from AND e.recorded_at<@recorded_to");
        if (q.CorrelationReference is not null) sql.Append(" AND e.correlation_ref=@correlation");
        sql.Append(" ORDER BY e.stream_sequence_value LIMIT @limit");
        await using var command = new NpgsqlCommand(sql.ToString(), connection);
        command.Parameters.AddWithValue("stream", streamId); command.Parameters.AddWithValue("after", after);
        command.Parameters.AddWithValue("through", through); command.Parameters.AddWithValue("limit", limit);
        if (q.FiscalReportingPeriodId is not null) command.Parameters.AddWithValue("period", q.FiscalReportingPeriodId.Value);
        if (q.FiscalDocumentReference is not null)
        {
            if (!Guid.TryParse(q.FiscalDocumentReference, out var id)) throw new InvalidOperationException("Fiscal document reference is malformed.");
            command.Parameters.AddWithValue("document_id", id);
        }
        if (q.FiscalDocumentNumber is not null) command.Parameters.AddWithValue("document_number", q.FiscalDocumentNumber);
        if (q.ZReadingReference is not null) command.Parameters.AddWithValue("z_reference", q.ZReadingReference);
        if (q.EventType is not null) command.Parameters.AddWithValue("event_type", q.EventType);
        if (q.EffectiveFrom is not null) { command.Parameters.AddWithValue("effective_from", q.EffectiveFrom.Value); command.Parameters.AddWithValue("effective_to", q.EffectiveTo!.Value); }
        if (q.RecordedFrom is not null) { command.Parameters.AddWithValue("recorded_from", q.RecordedFrom.Value); command.Parameters.AddWithValue("recorded_to", q.RecordedTo!.Value); }
        if (q.CorrelationReference is not null) command.Parameters.AddWithValue("correlation", q.CorrelationReference);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var result = new List<ElectronicJournalEvent>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false)) result.Add(ReadEvent(reader));
        return result;
    }

    private static ElectronicJournalEvent ReadEvent(NpgsqlDataReader r) => new(
        r.GetString(0), r.GetString(1), r.GetString(2), r.GetGuid(3), r.GetGuid(4), r.GetString(5), r.GetGuid(6),
        NullableGuid(r, 7), NullableGuid(r, 8), NullableGuid(r, 9), NullableGuid(r, 10), NullableGuid(r, 12),
        r.IsDBNull(13) ? null : r.GetFieldValue<DateOnly>(13), r.GetInt64(14), r.GetFieldValue<DateTimeOffset>(15),
        r.GetFieldValue<DateTimeOffset>(16), r.GetString(17), r.GetString(18), r.GetString(19), r.GetString(20), r.GetString(21),
        r.IsDBNull(22) ? null : r.GetString(22), r.GetString(23), r.GetString(24), r.GetString(25), r.GetString(26), r.GetString(27),
        r.GetString(28), ParseFacts(r.GetString(29)), NullableGuid(r, 11), ParsePrintableText(r.GetString(30)));

    private static string? ParsePrintableText(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("printableSalesInvoiceText", out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static IReadOnlyDictionary<string, string?> ParseFacts(string json) =>
        JsonSerializer.Deserialize<SortedDictionary<string, string?>>(json)
        ?? throw new JsonException("Electronic Journal facts were malformed.");

    private static Guid? NullableGuid(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);

    private static ElectronicJournalAppendRequest ToAppendRequest(ElectronicJournalEvent e) => new(
        e.SitePosServerId, e.FiscalIdentityId, e.CurrencyCode, e.FiscalReportingPeriodId, e.EventType,
        e.SourceTransitionReference, e.SourceTransitionVersion, e.EffectiveAt, e.ActorReference,
        e.ServiceIdentityReference, e.CorrelationReference, e.Facts, e.FiscalDocumentId,
        e.FiscalReportRequestId, e.XZReportId, e.BirSalesSummaryReportId, e.FiscalSequencePolicyId,
        e.BusinessDayDate, e.IdempotencyReference, e.ReprintRequestId, e.PrintableSalesInvoiceText);

    private static ElectronicJournalIntegrityOutcome IntegrityFailure(
        IReadOnlyList<ElectronicJournalEvent> events, long sequence, string code, string support) =>
        new(ElectronicJournalOutcome.IntegrityFailure,
            new(false, events.Count, events.Count == 0 ? 0 : 1, Math.Max(0, sequence - 1), code, support),
            "Electronic Journal integrity verification failed closed.");
}
