using System.Globalization;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;

public static class PostgresElectronicJournalWriter
{
    private static readonly Guid CommittedStatusId = Guid.Parse("07022b0a-3ef9-5f82-880a-d27a569f5e7b");
    private static readonly Guid RetentionPolicyId = Guid.Parse("bf711168-63d1-539e-9e8f-6de908978b3e");
    private static readonly string[] ProhibitedFactKeyFragments =
    [
        "authorization", "beneficiary", "connection", "credential", "customer_name", "evidence",
        "password", "private_key", "raw_request", "raw_response", "secret", "stack_trace",
        "statutory_id", "token"
    ];

    public static async Task<string> AppendAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ElectronicJournalAppendRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var eventTypeId = await ResolveCodeAsync(connection, transaction, "electronic_journal_event_type", request.EventType, cancellationToken).ConfigureAwait(false);
        var semanticHash = ElectronicJournalCanonicalizer.ComputeSemanticHash(request);

        var existing = await ReadExistingAsync(connection, transaction, request, eventTypeId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (!string.Equals(existing.Value.SemanticHash, semanticHash, StringComparison.Ordinal))
                throw new ElectronicJournalSemanticConflictException();
            return existing.Value.EventReference;
        }

        var candidateStreamId = Guid.NewGuid();
        await using (var createStream = new NpgsqlCommand("""
            INSERT INTO pos.electronic_journal_streams(
                electronic_journal_stream_id,site_pos_server_id,fiscal_identity_id,currency_code,
                retention_policy_code_id,chronology_version,integrity_hash_version,last_sequence_value,
                last_event_hash,created_at,updated_at)
            VALUES(@id,@site,@identity,@currency,@retention,@chronology,@integrity,0,@genesis,clock_timestamp(),clock_timestamp())
            ON CONFLICT (site_pos_server_id,fiscal_identity_id,currency_code) DO NOTHING
            """, connection, transaction))
        {
            createStream.Parameters.AddWithValue("id", candidateStreamId);
            createStream.Parameters.AddWithValue("site", request.SitePosServerId);
            createStream.Parameters.AddWithValue("identity", request.FiscalIdentityId);
            createStream.Parameters.AddWithValue("currency", request.CurrencyCode.ToUpperInvariant());
            createStream.Parameters.AddWithValue("retention", RetentionPolicyId);
            createStream.Parameters.AddWithValue("chronology", ElectronicJournalContract.ChronologyVersion);
            createStream.Parameters.AddWithValue("integrity", ElectronicJournalContract.IntegrityHashVersion);
            createStream.Parameters.AddWithValue("genesis", ElectronicJournalContract.GenesisHash);
            await createStream.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        Guid streamId;
        long previousSequence;
        string previousHash;
        await using (var lockStream = new NpgsqlCommand("""
            SELECT electronic_journal_stream_id,last_sequence_value,last_event_hash
            FROM pos.electronic_journal_streams
            WHERE site_pos_server_id=@site AND fiscal_identity_id=@identity AND currency_code=@currency
            FOR UPDATE
            """, connection, transaction))
        {
            lockStream.Parameters.AddWithValue("site", request.SitePosServerId);
            lockStream.Parameters.AddWithValue("identity", request.FiscalIdentityId);
            lockStream.Parameters.AddWithValue("currency", request.CurrencyCode.ToUpperInvariant());
            await using var reader = await lockStream.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("Electronic Journal stream head was unavailable.");
            streamId = reader.GetGuid(0);
            previousSequence = reader.GetInt64(1);
            previousHash = reader.GetString(2);
        }

        existing = await ReadExistingAsync(connection, transaction, request, eventTypeId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (!string.Equals(existing.Value.SemanticHash, semanticHash, StringComparison.Ordinal))
                throw new ElectronicJournalSemanticConflictException();
            return existing.Value.EventReference;
        }

        var sequence = checked(previousSequence + 1);
        var eventId = Guid.NewGuid();
        var eventReference = $"EJ-{streamId:N}-{sequence:D20}".ToUpperInvariant();
        var recordedAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        var integrityHash = ElectronicJournalCanonicalizer.ComputeIntegrityHash(
            request, eventReference, sequence, recordedAt, semanticHash, previousHash);
        var factsJson = ElectronicJournalCanonicalizer.CanonicalFactsJson(request.Facts);

        await using (var insert = new NpgsqlCommand("""
            INSERT INTO pos.electronic_journal_records(
                electronic_journal_record_id,site_pos_server_id,fiscal_document_id,fiscal_report_request_id,
                journal_record_type_code_id,journal_record_status_code_id,business_day_date,journal_sequence_ref,
                journal_hash_ref,previous_journal_hash_ref,recorded_at,journal_context,created_at,updated_at,
                electronic_journal_stream_id,fiscal_identity_id,currency_code,fiscal_sequence_policy_id,
                fiscal_reporting_period_id,x_z_report_id,bir_sales_summary_report_id,reprint_request_id,event_reference,
                event_schema_version,stream_sequence_value,effective_at,actor_ref,service_identity_ref,
                correlation_ref,source_transition_ref,source_transition_version,idempotency_ref,
                semantic_hash_version,semantic_hash,integrity_hash_version,integrity_hash,
                previous_integrity_hash,retention_policy_code_id,event_facts,is_canonical)
            VALUES(
                @id,@site,@document,@request,@type,@status,@business_date,@sequence_ref,@hash,@previous_hash,
                @recorded_at,@journal_context,@recorded_at,@recorded_at,@stream,@identity,@currency,@sequence_policy,
                @period,@xz,@bir,@reprint,@event_ref,@event_schema,@sequence,@effective_at,@actor,@service,@correlation,
                @source_ref,@source_version,@idempotency,@semantic_version,@semantic_hash,@integrity_version,
                @hash,@previous_hash,@retention,@facts,true)
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue("id", eventId); insert.Parameters.AddWithValue("site", request.SitePosServerId);
            AddNullable(insert, "document", request.FiscalDocumentId); AddNullable(insert, "request", request.FiscalReportRequestId);
            insert.Parameters.AddWithValue("type", eventTypeId); insert.Parameters.AddWithValue("status", CommittedStatusId);
            AddNullableDate(insert, "business_date", request.BusinessDayDate);
            insert.Parameters.AddWithValue("sequence_ref", sequence.ToString(CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("hash", integrityHash); insert.Parameters.AddWithValue("previous_hash", previousHash);
            insert.Parameters.AddWithValue("recorded_at", recordedAt);
            insert.Parameters.AddWithValue("journal_context", NpgsqlDbType.Jsonb,
                request.PrintableSalesInvoiceText is null
                    ? "{}"
                    : JsonSerializer.Serialize(new { printableSalesInvoiceText = request.PrintableSalesInvoiceText, encoding = "utf-8" }));
            insert.Parameters.AddWithValue("stream", streamId);
            insert.Parameters.AddWithValue("identity", request.FiscalIdentityId); insert.Parameters.AddWithValue("currency", request.CurrencyCode.ToUpperInvariant());
            AddNullable(insert, "sequence_policy", request.FiscalSequencePolicyId); insert.Parameters.AddWithValue("period", request.FiscalReportingPeriodId);
            AddNullable(insert, "xz", request.XZReportId); AddNullable(insert, "bir", request.BirSalesSummaryReportId);
            AddNullable(insert, "reprint", request.ReprintRequestId);
            insert.Parameters.AddWithValue("event_ref", eventReference); insert.Parameters.AddWithValue("event_schema", ElectronicJournalContract.EventSchemaVersion);
            insert.Parameters.AddWithValue("sequence", sequence); insert.Parameters.AddWithValue("effective_at", request.EffectiveAt);
            insert.Parameters.AddWithValue("actor", request.ActorReference); insert.Parameters.AddWithValue("service", request.ServiceIdentityReference);
            insert.Parameters.AddWithValue("correlation", request.CorrelationReference); insert.Parameters.AddWithValue("source_ref", request.SourceTransitionReference);
            insert.Parameters.AddWithValue("source_version", request.SourceTransitionVersion); AddNullableText(insert, "idempotency", request.IdempotencyReference);
            insert.Parameters.AddWithValue("semantic_version", ElectronicJournalContract.SemanticHashVersion); insert.Parameters.AddWithValue("semantic_hash", semanticHash);
            insert.Parameters.AddWithValue("integrity_version", ElectronicJournalContract.IntegrityHashVersion); insert.Parameters.AddWithValue("retention", RetentionPolicyId);
            insert.Parameters.AddWithValue("facts", NpgsqlDbType.Jsonb, factsJson);
            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var update = new NpgsqlCommand("""
            UPDATE pos.electronic_journal_streams
            SET last_sequence_value=@sequence,last_event_hash=@hash,updated_at=@recorded_at
            WHERE electronic_journal_stream_id=@stream AND last_sequence_value=@previous_sequence AND last_event_hash=@previous_hash
            """, connection, transaction);
        update.Parameters.AddWithValue("sequence", sequence); update.Parameters.AddWithValue("hash", integrityHash);
        update.Parameters.AddWithValue("recorded_at", recordedAt); update.Parameters.AddWithValue("stream", streamId);
        update.Parameters.AddWithValue("previous_sequence", previousSequence); update.Parameters.AddWithValue("previous_hash", previousHash);
        if (await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            throw new InvalidOperationException("Electronic Journal stream head changed unexpectedly.");
        return eventReference;
    }

    private static async Task<(string EventReference, string SemanticHash)?> ReadExistingAsync(
        NpgsqlConnection c, NpgsqlTransaction t, ElectronicJournalAppendRequest request, Guid eventTypeId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT event_reference,semantic_hash FROM pos.electronic_journal_records
            WHERE is_canonical AND site_pos_server_id=@site AND fiscal_identity_id=@identity
              AND currency_code=@currency AND source_transition_ref=@source AND journal_record_type_code_id=@type
            """, c, t);
        command.Parameters.AddWithValue("site", request.SitePosServerId); command.Parameters.AddWithValue("identity", request.FiscalIdentityId);
        command.Parameters.AddWithValue("currency", request.CurrencyCode.ToUpperInvariant()); command.Parameters.AddWithValue("source", request.SourceTransitionReference);
        command.Parameters.AddWithValue("type", eventTypeId);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? (reader.GetString(0), reader.GetString(1)) : null;
    }

    private static async Task<Guid> ResolveCodeAsync(NpgsqlConnection c, NpgsqlTransaction t, string set, string code, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT value.controlled_code_id FROM pos.controlled_codes value
            JOIN pos.controlled_code_sets family ON family.controlled_code_set_id=value.controlled_code_set_id
            WHERE family.code_set_key=@set AND value.code_key=@code AND family.is_active AND value.is_active
            """, c, t);
        command.Parameters.AddWithValue("set", set); command.Parameters.AddWithValue("code", code);
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return value is Guid id ? id : throw new InvalidOperationException("Governed Electronic Journal classification is unavailable.");
    }

    private static async Task<DateTimeOffset> ClockAsync(NpgsqlConnection c, NpgsqlTransaction t, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT clock_timestamp()", c, t);
        return await command.ExecuteScalarAsync(ct).ConfigureAwait(false) switch
        {
            DateTimeOffset value => value,
            DateTime value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("Database clock was unavailable.")
        };
    }

    private static void Validate(ElectronicJournalAppendRequest r)
    {
        if (r.SitePosServerId == Guid.Empty || r.FiscalIdentityId == Guid.Empty || r.FiscalReportingPeriodId == Guid.Empty ||
            r.CurrencyCode.Length != 3 || r.CurrencyCode.Any(ch => ch is < 'A' or > 'Z') ||
            string.IsNullOrWhiteSpace(r.EventType) || string.IsNullOrWhiteSpace(r.SourceTransitionReference) ||
            string.IsNullOrWhiteSpace(r.SourceTransitionVersion) || string.IsNullOrWhiteSpace(r.ActorReference) ||
            string.IsNullOrWhiteSpace(r.ServiceIdentityReference) || string.IsNullOrWhiteSpace(r.CorrelationReference))
            throw new InvalidOperationException("Canonical Electronic Journal event facts are incomplete.");
        if (r.Facts.Keys.Any(key => string.IsNullOrWhiteSpace(key)) || r.Facts.Count > 128)
            throw new InvalidOperationException("Canonical Electronic Journal event facts are invalid.");
        if (new[] { r.EventType, r.SourceTransitionReference, r.SourceTransitionVersion, r.ActorReference,
                r.ServiceIdentityReference, r.CorrelationReference, r.IdempotencyReference }
            .Where(value => value is not null)
            .Any(value => value!.Length > 200 || value.Contains('\r') || value.Contains('\n')))
            throw new InvalidOperationException("Canonical Electronic Journal event references are invalid.");
        if (r.Facts.Any(pair => pair.Key.Length > 80 || pair.Key.Any(character => character is '\r' or '\n') ||
                ProhibitedFactKeyFragments.Any(fragment => pair.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase)) ||
                pair.Value is { Length: > 2048 } || pair.Value?.Any(character => character is '\r' or '\n') == true))
            throw new InvalidOperationException("Canonical Electronic Journal event facts violate the privacy-safe contract.");
        if (r.PrintableSalesInvoiceText is { Length: > 131072 } ||
            r.PrintableSalesInvoiceText?.IndexOf('\0') >= 0)
            throw new InvalidOperationException("Canonical Sales Invoice text exceeds the governed journal payload contract.");
    }

    private static void AddNullable(NpgsqlCommand command, string name, Guid? value) =>
        command.Parameters.AddWithValue(name, NpgsqlDbType.Uuid, value is null ? DBNull.Value : value.Value);
    private static void AddNullableText(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.AddWithValue(name, NpgsqlDbType.Text, value is null ? DBNull.Value : value);
    private static void AddNullableDate(NpgsqlCommand command, string name, DateOnly? value) =>
        command.Parameters.AddWithValue(name, NpgsqlDbType.Date, value is null ? DBNull.Value : value.Value);
}

public sealed class ElectronicJournalSemanticConflictException : InvalidOperationException
{
    public ElectronicJournalSemanticConflictException() : base("Electronic Journal event identity is bound to different semantics.") { }
}
