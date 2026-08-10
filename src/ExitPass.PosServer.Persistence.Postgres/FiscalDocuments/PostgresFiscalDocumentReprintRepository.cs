using System.Globalization;
using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public sealed class PostgresFiscalDocumentReprintRepository(NpgsqlDataSource dataSource) : IFiscalDocumentReprintRepository
{
    private static readonly Guid ReprintTypeId = Guid.Parse("4322e9c1-2209-5ad4-8587-4bbe4f35a08b");
    private static readonly Guid CommittedStatusId = Guid.Parse("5dacbba2-6cc7-593f-867b-d7010955d3e1");
    private static readonly Guid PrintPresentationTypeId = Guid.Parse("cc4ac16f-401f-5b8e-b817-464fc1e4ceb2");
    private static readonly IReadOnlyDictionary<string, Guid> ReasonIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["operator_request"] = Guid.Parse("5b8a58c8-636f-51fc-8512-85e3d932e8c8"),
        ["customer_request"] = Guid.Parse("215ad81a-913b-53e2-a009-b5bf1de021a5"),
        ["audit_request"] = Guid.Parse("c9ab8e23-0f6f-5bbb-a2f5-cc8c5105606e"),
        ["damaged_original"] = Guid.Parse("dbaf50f2-0001-5820-b6f1-ff5803f7d839")
    };

    public async Task<FiscalDocumentReprintResult> RecordAsync(
        FiscalDocumentReprintCommand command,
        string semanticRequestHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var existing = await ReadExistingAsync(connection, transaction, command, semanticRequestHash, cancellationToken).ConfigureAwait(false);
                if (existing is not null)
                {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return existing;
                }

                var document = await LockDocumentAsync(connection, transaction, command.FiscalDocumentId, cancellationToken).ConfigureAwait(false);
                if (document is null)
                    return await RollbackAsync(transaction, new(FiscalDocumentReprintOutcome.NotFound, SafeMessage: "The requested fiscal document is unavailable."), cancellationToken).ConfigureAwait(false);
                if (document.SitePosServerId != command.SitePosServerId || document.FiscalIdentityId != command.FiscalIdentityId ||
                    !string.Equals(document.CurrencyCode, command.CurrencyCode, StringComparison.Ordinal))
                    return await RollbackAsync(transaction, new(FiscalDocumentReprintOutcome.ScopeMismatch, SafeMessage: "The requested fiscal document is unavailable."), cancellationToken).ConfigureAwait(false);
                if (!string.Equals(document.DocumentType, "sales_invoice", StringComparison.OrdinalIgnoreCase) ||
                    document.DocumentStatus is not ("recorded" or "issued") || document.FiscalSequencePolicyId is null ||
                    document.FiscalSequenceValue is null || string.IsNullOrWhiteSpace(document.FiscalDocumentNumber) ||
                    document.FiscalReportingPeriodId is null || document.BusinessDayDate is null)
                    return await RollbackAsync(transaction, new(FiscalDocumentReprintOutcome.InvalidState, SafeMessage: "The fiscal document is not eligible for governed reprint recording."), cancellationToken).ConfigureAwait(false);

                existing = await ReadExistingAsync(connection, transaction, command, semanticRequestHash, cancellationToken).ConfigureAwait(false);
                if (existing is not null)
                {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return existing;
                }

                var copySequence = await NextCopySequenceAsync(connection, transaction, command.FiscalDocumentId, cancellationToken).ConfigureAwait(false);
                var recordedAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
                var requestId = Guid.NewGuid();
                var outputId = Guid.NewGuid();
                var reprintReference = $"RPR-{requestId:N}".ToUpperInvariant();
                var outputReference = $"RPO-{outputId:N}".ToUpperInvariant();

                await InsertReprintAsync(connection, transaction, command, semanticRequestHash, document,
                    requestId, reprintReference, copySequence, recordedAt, cancellationToken).ConfigureAwait(false);

                var eventReference = await PostgresElectronicJournalWriter.AppendAsync(
                    connection,
                    transaction,
                    new ElectronicJournalAppendRequest(
                        command.SitePosServerId,
                        command.FiscalIdentityId,
                        command.CurrencyCode,
                        document.FiscalReportingPeriodId.Value,
                        FiscalDocumentReprintContract.EventType,
                        reprintReference,
                        FiscalDocumentReprintContract.ContractVersion,
                        recordedAt,
                        command.ActorReference,
                        command.ServiceIdentityReference,
                        command.CorrelationReference,
                        new SortedDictionary<string, string?>(StringComparer.Ordinal)
                        {
                            ["copy_sequence"] = copySequence.ToString(CultureInfo.InvariantCulture),
                            ["fiscal_document_number"] = document.FiscalDocumentNumber,
                            ["fiscal_sequence_value"] = document.FiscalSequenceValue.Value.ToString(CultureInfo.InvariantCulture),
                            ["original_fiscal_document_reference"] = document.FiscalDocumentNumber,
                            ["output_reference"] = outputReference,
                            ["output_type"] = "print_presentation",
                            ["reprint_label_applied"] = "true",
                            ["reprint_reason"] = command.ReasonCode,
                            ["reprint_reference"] = reprintReference,
                            ["reprint_status"] = "committed",
                            ["reprint_type"] = "fiscal_document_copy"
                        },
                        FiscalDocumentId: command.FiscalDocumentId,
                        ReprintRequestId: requestId,
                        FiscalSequencePolicyId: document.FiscalSequencePolicyId,
                        BusinessDayDate: document.BusinessDayDate,
                        IdempotencyReference: command.OperationKey),
                    cancellationToken).ConfigureAwait(false);

                await InsertOutputAsync(connection, transaction, requestId, outputId, outputReference,
                    command.ActorReference, recordedAt, cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new(FiscalDocumentReprintOutcome.Created, new(
                    requestId, reprintReference, command.FiscalDocumentId, document.FiscalDocumentNumber,
                    document.FiscalSequenceValue.Value, command.SitePosServerId, command.FiscalIdentityId,
                    command.CurrencyCode, document.FiscalReportingPeriodId.Value, document.FiscalSequencePolicyId.Value,
                    document.BusinessDayDate.Value, copySequence, "fiscal_document_copy", "committed", command.ReasonCode,
                    outputReference, "print_presentation", true, recordedAt, recordedAt, command.ActorReference,
                    command.ServiceIdentityReference, command.CorrelationReference, command.OperationKey, eventReference));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (ElectronicJournalSemanticConflictException)
        {
            return new(FiscalDocumentReprintOutcome.IntegrityFailure, SafeMessage: "The canonical reprint event failed integrity validation.");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return new(FiscalDocumentReprintOutcome.Conflict, SafeMessage: "The reprint operation identity is already bound to another request.");
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException or OverflowException)
        {
            return new(FiscalDocumentReprintOutcome.PersistenceFailure, SafeMessage: "The fiscal reprint operation failed safely.");
        }
    }

    private static async Task<FiscalDocumentReprintResult?> ReadExistingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentReprintCommand command,
        string semanticRequestHash,
        CancellationToken cancellationToken)
    {
        await using var query = new NpgsqlCommand("""
            SELECT request.reprint_request_id,request.reprint_reference,request.fiscal_document_id,
                   request.fiscal_document_number_snapshot,request.fiscal_sequence_value_snapshot,
                   request.site_pos_server_id,request.fiscal_identity_id,request.currency_code,
                   request.fiscal_reporting_period_id,request.fiscal_sequence_policy_id,document.business_day_date,
                   request.copy_sequence,reason.code_key,output.output_reference,request.requested_at,request.committed_at,
                   request.actor_ref,request.service_identity_ref,request.correlation_ref,request.operation_idempotency_key,
                   request.semantic_request_hash,event.event_reference
            FROM pos.reprint_requests request
            JOIN pos.fiscal_documents document ON document.fiscal_document_id=request.fiscal_document_id
            JOIN pos.controlled_codes reason ON reason.controlled_code_id=request.reprint_reason_code_id
            LEFT JOIN pos.reprint_output_refs output ON output.reprint_request_id=request.reprint_request_id AND output.is_canonical
            LEFT JOIN pos.electronic_journal_records event ON event.reprint_request_id=request.reprint_request_id
                AND event.is_canonical AND event.journal_record_type_code_id='77905f62-7d61-521d-bdc3-7c7d57b31efe'
            WHERE request.is_canonical AND request.site_pos_server_id=@site
              AND request.operation_idempotency_key=@operation
            """, connection, transaction);
        query.Parameters.AddWithValue("site", command.SitePosServerId);
        query.Parameters.AddWithValue("operation", command.OperationKey);
        await using var reader = await query.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        if (!string.Equals(reader.GetString(20), semanticRequestHash, StringComparison.Ordinal))
            return new(FiscalDocumentReprintOutcome.Conflict, SafeMessage: "The reprint operation identity is bound to different semantics.");
        if (reader.IsDBNull(13) || reader.IsDBNull(21))
            return new(FiscalDocumentReprintOutcome.IntegrityFailure, SafeMessage: "The canonical reprint evidence is incomplete.");
        return new(FiscalDocumentReprintOutcome.Replayed, new(
            reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetString(3), reader.GetInt64(4),
            reader.GetGuid(5), reader.GetGuid(6), reader.GetString(7), reader.GetGuid(8), reader.GetGuid(9),
            reader.GetFieldValue<DateOnly>(10), reader.GetInt64(11), "fiscal_document_copy", "committed", reader.GetString(12),
            reader.GetString(13), "print_presentation", true, reader.GetFieldValue<DateTimeOffset>(14),
            reader.GetFieldValue<DateTimeOffset>(15), reader.GetString(16), reader.GetString(17), reader.GetString(18),
            reader.GetString(19), reader.GetString(21)));
    }

    private static async Task<LockedDocument?> LockDocumentAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid documentId, CancellationToken cancellationToken)
    {
        await using var query = new NpgsqlCommand("""
            SELECT document.site_pos_server_id,document.fiscal_identity_id,document.currency_code,
                   document.fiscal_reporting_period_id,document.fiscal_sequence_policy_id,
                   document.fiscal_sequence_value,document.fiscal_document_number,document.business_day_date,
                   type.code_key,status.code_key
            FROM pos.fiscal_documents document
            JOIN pos.controlled_codes type ON type.controlled_code_id=document.fiscal_document_type_code_id
            JOIN pos.controlled_codes status ON status.controlled_code_id=document.fiscal_document_status_code_id
            WHERE document.fiscal_document_id=@document
            FOR UPDATE OF document
            """, connection, transaction);
        query.Parameters.AddWithValue("document", documentId);
        await using var reader = await query.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            reader.GetGuid(0), reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3), reader.IsDBNull(4) ? null : reader.GetGuid(4),
            reader.IsDBNull(5) ? null : reader.GetInt64(5), reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7), reader.GetString(8), reader.GetString(9));
    }

    private static async Task<long> NextCopySequenceAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid documentId, CancellationToken cancellationToken)
    {
        await using var query = new NpgsqlCommand("""
            SELECT COALESCE(MAX(copy_sequence),0) + 1
            FROM pos.reprint_requests
            WHERE is_canonical AND fiscal_document_id=@document
            """, connection, transaction);
        query.Parameters.AddWithValue("document", documentId);
        return (long)(await query.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fiscal reprint copy sequence was unavailable."));
    }

    private static async Task InsertReprintAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, FiscalDocumentReprintCommand command,
        string semanticRequestHash, LockedDocument document, Guid requestId, string reprintReference,
        long copySequence, DateTimeOffset recordedAt, CancellationToken cancellationToken)
    {
        await using var insert = new NpgsqlCommand("""
            INSERT INTO pos.reprint_requests(
                reprint_request_id,fiscal_document_id,reprint_type_code_id,reprint_status_code_id,reprint_reason_code_id,
                requested_at,requested_by_ref,actor_ref,service_identity_ref,created_at,updated_at,
                reprint_reference,site_pos_server_id,fiscal_identity_id,currency_code,fiscal_reporting_period_id,
                fiscal_sequence_policy_id,fiscal_document_number_snapshot,fiscal_sequence_value_snapshot,copy_sequence,
                operation_idempotency_key,semantic_hash_version,semantic_request_hash,source_transition_version,
                correlation_ref,committed_at,is_canonical)
            VALUES(@id,@document,@type,@status,@reason,@at,@actor,@actor,@service,@at,@at,
                @reference,@site,@identity,@currency,@period,@sequence_policy,@document_number,@sequence_value,@copy,
                @operation,@semantic_version,@semantic_hash,@source_version,@correlation,@at,true)
            """, connection, transaction);
        insert.Parameters.AddWithValue("id", requestId); insert.Parameters.AddWithValue("document", command.FiscalDocumentId);
        insert.Parameters.AddWithValue("type", ReprintTypeId); insert.Parameters.AddWithValue("status", CommittedStatusId);
        insert.Parameters.AddWithValue("reason", ReasonIds[command.ReasonCode]); insert.Parameters.AddWithValue("at", recordedAt);
        insert.Parameters.AddWithValue("actor", command.ActorReference); insert.Parameters.AddWithValue("service", command.ServiceIdentityReference);
        insert.Parameters.AddWithValue("reference", reprintReference); insert.Parameters.AddWithValue("site", command.SitePosServerId);
        insert.Parameters.AddWithValue("identity", command.FiscalIdentityId); insert.Parameters.AddWithValue("currency", command.CurrencyCode);
        insert.Parameters.AddWithValue("period", document.FiscalReportingPeriodId!.Value);
        insert.Parameters.AddWithValue("sequence_policy", document.FiscalSequencePolicyId!.Value);
        insert.Parameters.AddWithValue("document_number", document.FiscalDocumentNumber!);
        insert.Parameters.AddWithValue("sequence_value", document.FiscalSequenceValue!.Value); insert.Parameters.AddWithValue("copy", copySequence);
        insert.Parameters.AddWithValue("operation", command.OperationKey);
        insert.Parameters.AddWithValue("semantic_version", FiscalDocumentReprintContract.SemanticHashVersion);
        insert.Parameters.AddWithValue("semantic_hash", semanticRequestHash);
        insert.Parameters.AddWithValue("source_version", FiscalDocumentReprintContract.ContractVersion);
        insert.Parameters.AddWithValue("correlation", command.CorrelationReference);
        await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertOutputAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid requestId, Guid outputId,
        string outputReference, string actorReference, DateTimeOffset recordedAt, CancellationToken cancellationToken)
    {
        await using var insert = new NpgsqlCommand("""
            INSERT INTO pos.reprint_output_refs(
                reprint_output_ref_id,reprint_request_id,output_type_code_id,output_ref,reprint_label_applied,
                reprinted_at,printed_by_ref,created_at,output_reference,is_canonical)
            VALUES(@id,@request,@type,@reference,true,@at,@actor,@at,@reference,true)
            """, connection, transaction);
        insert.Parameters.AddWithValue("id", outputId); insert.Parameters.AddWithValue("request", requestId);
        insert.Parameters.AddWithValue("type", PrintPresentationTypeId); insert.Parameters.AddWithValue("reference", outputReference);
        insert.Parameters.AddWithValue("at", recordedAt); insert.Parameters.AddWithValue("actor", actorReference);
        await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<DateTimeOffset> ClockAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var query = new NpgsqlCommand("SELECT clock_timestamp()", connection, transaction);
        return await query.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) switch
        {
            DateTimeOffset value => value,
            DateTime value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("Database clock was unavailable.")
        };
    }

    private static async Task<FiscalDocumentReprintResult> RollbackAsync(
        NpgsqlTransaction transaction, FiscalDocumentReprintResult result, CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private sealed record LockedDocument(
        Guid SitePosServerId,
        Guid FiscalIdentityId,
        string CurrencyCode,
        Guid? FiscalReportingPeriodId,
        Guid? FiscalSequencePolicyId,
        long? FiscalSequenceValue,
        string? FiscalDocumentNumber,
        DateOnly? BusinessDayDate,
        string DocumentType,
        string DocumentStatus);
}
