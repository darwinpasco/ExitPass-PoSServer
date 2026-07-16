using ExitPass.PosServer.Runtime.FiscalDocuments;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public sealed class PostgresFiscalDocumentReader : IFiscalDocumentReader
{
    private static readonly string[] SensitiveMarkers =
    [
        "raw_id",
        "id_image",
        "identity_document",
        "evidence_payload",
        "evidence_image",
        "credential",
        "payment_payload",
        "provider_callback",
        "card_number",
        "cvv",
        "token",
        "secret"
    ];

    private readonly NpgsqlDataSource dataSource;

    public PostgresFiscalDocumentReader(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<FiscalDocumentReadModel?> GetByIdAsync(
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var header = await ReadHeaderAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false);
            if (header is null)
            {
                return null;
            }

            return header with
            {
                StatusHistory = await ReadStatusHistoryAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false),
                DocumentLinks = await ReadLinksAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false),
                Lines = await ReadLinesAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false),
                Tenders = await ReadTendersAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false),
                TaxDetails = await ReadTaxDetailsAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false),
                DiscountPrivilegeDetails = await ReadDiscountPrivilegeDetailsAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false),
                Totals = await ReadTotalsAsync(connection, fiscalDocumentId, cancellationToken).ConfigureAwait(false)
            };
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            throw new FiscalDocumentPersistenceException(
                "Fiscal document read failed.",
                ex);
        }
    }

    private static async Task<FiscalDocumentReadModel?> ReadHeaderAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                document.fiscal_document_id,
                document.site_pos_server_id,
                document.channel_terminal_id,
                document.fiscal_identity_id,
                document.fiscal_document_type_code_id,
                type_code.code_key,
                document.fiscal_document_status_code_id,
                status_code.code_key,
                document.fiscal_sequence_policy_id,
                document.fiscal_sequence_value,
                document.fiscal_document_number,
                document.fiscal_series,
                document.fiscal_number_prefix_text,
                document.fiscal_number_suffix_text,
                document.fiscal_number_assigned_at,
                document.fiscal_number_assigned_by_ref,
                idempotency.idempotency_scope,
                idempotency.idempotency_key,
                idempotency.idempotency_context ->> 'idempotency_key_source',
                idempotency.semantic_request_hash,
                case
                    when idempotency.semantic_request_hash is null then null
                    else idempotency.idempotency_context ->> 'semantic_request_hash_version'
                end,
                case
                    when idempotency.semantic_request_hash is null then null
                    else 'matched'
                end,
                document.central_pms_parking_session_ref,
                document.central_pms_payment_attempt_ref,
                document.central_pms_payment_confirmation_ref,
                document.payment_finality_ref,
                document.vendor_ack_ref,
                document.business_day_date,
                document.void_status,
                document.void_reason_code,
                document.voided_at,
                document.document_context::text,
                document.is_active,
                document.created_at,
                document.updated_at
            from pos.fiscal_documents document
            left join pos.controlled_codes type_code
                on type_code.controlled_code_id = document.fiscal_document_type_code_id
            left join pos.controlled_codes status_code
                on status_code.controlled_code_id = document.fiscal_document_status_code_id
            left join lateral (
                select
                    idempotency_scope,
                    idempotency_key,
                    semantic_request_hash,
                    idempotency_context
                from pos.idempotency_records
                where linked_fiscal_document_id = document.fiscal_document_id
                order by updated_at desc, created_at desc, idempotency_record_id
                limit 1
            ) idempotency on true
            where document.fiscal_document_id = @fiscal_document_id;
            """,
            fiscalDocumentId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new FiscalDocumentReadModel(
            reader.GetGuid(0),
            reader.GetGuid(1),
            GetNullableGuid(reader, 2),
            GetNullableGuid(reader, 3),
            reader.GetGuid(4),
            GetSafeString(reader, 5),
            reader.GetGuid(6),
            GetSafeString(reader, 7),
            GetNullableGuid(reader, 8),
            GetNullableInt64(reader, 9),
            GetSafeString(reader, 10),
            GetSafeString(reader, 11),
            GetSafeString(reader, 12),
            GetSafeString(reader, 13),
            GetNullableDateTimeOffset(reader, 14),
            GetSafeString(reader, 15),
            GetSafeString(reader, 16),
            GetSafeString(reader, 17),
            GetSafeString(reader, 18),
            GetSafeString(reader, 19),
            GetSafeString(reader, 20) ?? (reader.IsDBNull(19) ? null : FiscalDocumentSemanticRequestHasher.Version),
            GetSafeString(reader, 21),
            GetSafeString(reader, 22),
            GetSafeString(reader, 23),
            GetSafeString(reader, 24),
            GetSafeString(reader, 25),
            GetSafeString(reader, 26),
            GetNullableDateOnly(reader, 27),
            GetSafeString(reader, 28),
            GetSafeString(reader, 29),
            GetNullableDateTimeOffset(reader, 30),
            GetSafeString(reader, 31),
            reader.GetBoolean(32),
            reader.GetFieldValue<DateTimeOffset>(33),
            reader.GetFieldValue<DateTimeOffset>(34),
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    private static async Task<IReadOnlyList<FiscalDocumentStatusHistoryReadModel>> ReadStatusHistoryAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                fiscal_document_status_history_id,
                fiscal_document_id,
                prior_fiscal_document_status_code_id,
                new_fiscal_document_status_code_id,
                status_reason_code_id,
                status_reason_text,
                changed_at,
                actor_ref,
                service_identity_ref,
                created_at
            from pos.fiscal_document_status_history
            where fiscal_document_id = @fiscal_document_id
            order by changed_at, fiscal_document_status_history_id;
            """,
            fiscalDocumentId);

        var results = new List<FiscalDocumentStatusHistoryReadModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new FiscalDocumentStatusHistoryReadModel(
                reader.GetGuid(0),
                reader.GetGuid(1),
                GetNullableGuid(reader, 2),
                reader.GetGuid(3),
                GetNullableGuid(reader, 4),
                GetSafeString(reader, 5),
                reader.GetFieldValue<DateTimeOffset>(6),
                GetSafeString(reader, 7),
                GetSafeString(reader, 8),
                reader.GetFieldValue<DateTimeOffset>(9)));
        }

        return results;
    }

    private static async Task<IReadOnlyList<FiscalDocumentLinkReadModel>> ReadLinksAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                fiscal_document_link_id,
                source_fiscal_document_id,
                target_fiscal_document_id,
                fiscal_document_link_type_code_id,
                link_reason_code_id,
                link_reason_text,
                created_at,
                created_by_ref
            from pos.fiscal_document_links
            where source_fiscal_document_id = @fiscal_document_id
            order by created_at, fiscal_document_link_id;
            """,
            fiscalDocumentId);

        var results = new List<FiscalDocumentLinkReadModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new FiscalDocumentLinkReadModel(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                GetNullableGuid(reader, 4),
                GetSafeString(reader, 5),
                reader.GetFieldValue<DateTimeOffset>(6),
                GetSafeString(reader, 7)));
        }

        return results;
    }

    private static async Task<IReadOnlyList<FiscalDocumentLineReadModel>> ReadLinesAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                fiscal_document_line_id,
                fiscal_document_id,
                line_sequence,
                line_type_code_id,
                line_status_code_id,
                description,
                quantity,
                unit_amount_minor_units,
                gross_amount_minor_units,
                discount_amount_minor_units,
                tax_amount_minor_units,
                net_amount_minor_units,
                currency_code,
                source_ref,
                line_context::text,
                is_active,
                created_at,
                updated_at
            from pos.fiscal_document_lines
            where fiscal_document_id = @fiscal_document_id
            order by line_sequence, fiscal_document_line_id;
            """,
            fiscalDocumentId);

        var results = new List<FiscalDocumentLineReadModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new FiscalDocumentLineReadModel(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetGuid(3),
                GetNullableGuid(reader, 4),
                GetSafeString(reader, 5) ?? string.Empty,
                reader.GetDecimal(6),
                reader.GetInt64(7),
                reader.GetInt64(8),
                reader.GetInt64(9),
                reader.GetInt64(10),
                reader.GetInt64(11),
                reader.GetString(12),
                GetSafeString(reader, 13),
                GetSafeString(reader, 14),
                reader.GetBoolean(15),
                reader.GetFieldValue<DateTimeOffset>(16),
                reader.GetFieldValue<DateTimeOffset>(17)));
        }

        return results;
    }

    private static async Task<IReadOnlyList<FiscalTenderReadModel>> ReadTendersAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                fiscal_tender_id,
                fiscal_document_id,
                tender_type_code_id,
                tender_type_code.code_key,
                amount_minor_units,
                currency_code,
                central_pms_payment_attempt_ref,
                central_pms_payment_confirmation_ref,
                payment_finality_ref,
                provider_ref,
                tender_context::text,
                created_at,
                updated_at
            from pos.fiscal_tenders tender
            left join pos.controlled_codes tender_type_code
                on tender_type_code.controlled_code_id = tender.tender_type_code_id
            where tender.fiscal_document_id = @fiscal_document_id
            order by tender.created_at, tender.fiscal_tender_id;
            """,
            fiscalDocumentId);

        var results = new List<FiscalTenderReadModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new FiscalTenderReadModel(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                GetSafeString(reader, 3),
                reader.GetInt64(4),
                reader.GetString(5),
                GetSafeString(reader, 6),
                GetSafeString(reader, 7),
                GetSafeString(reader, 8),
                GetSafeString(reader, 9),
                GetSafeString(reader, 10),
                reader.GetFieldValue<DateTimeOffset>(11),
                reader.GetFieldValue<DateTimeOffset>(12)));
        }

        return results;
    }

    private static async Task<IReadOnlyList<FiscalTaxDetailReadModel>> ReadTaxDetailsAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                fiscal_tax_detail_id,
                fiscal_document_id,
                fiscal_document_line_id,
                tax_type_code_id,
                tax_classification_code_id,
                tax_rate,
                taxable_amount_minor_units,
                tax_amount_minor_units,
                currency_code,
                tax_context::text,
                created_at,
                updated_at
            from pos.fiscal_tax_details
            where fiscal_document_id = @fiscal_document_id
            order by created_at, fiscal_tax_detail_id;
            """,
            fiscalDocumentId);

        var results = new List<FiscalTaxDetailReadModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new FiscalTaxDetailReadModel(
                reader.GetGuid(0),
                reader.GetGuid(1),
                GetNullableGuid(reader, 2),
                reader.GetGuid(3),
                reader.GetGuid(4),
                GetNullableDecimal(reader, 5),
                reader.GetInt64(6),
                reader.GetInt64(7),
                reader.GetString(8),
                GetSafeString(reader, 9),
                reader.GetFieldValue<DateTimeOffset>(10),
                reader.GetFieldValue<DateTimeOffset>(11)));
        }

        return results;
    }

    private static async Task<IReadOnlyList<FiscalDiscountPrivilegeDetailReadModel>> ReadDiscountPrivilegeDetailsAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                fiscal_discount_privilege_detail_id,
                fiscal_document_id,
                fiscal_document_line_id,
                discount_privilege_type_code_id,
                basis_amount_minor_units,
                discount_amount_minor_units,
                vat_privilege_amount_minor_units,
                currency_code,
                beneficiary_ref,
                evidence_ref,
                approval_ref,
                discount_privilege_context::text,
                created_at,
                updated_at
            from pos.fiscal_discount_privilege_details
            where fiscal_document_id = @fiscal_document_id
            order by created_at, fiscal_discount_privilege_detail_id;
            """,
            fiscalDocumentId);

        var results = new List<FiscalDiscountPrivilegeDetailReadModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new FiscalDiscountPrivilegeDetailReadModel(
                reader.GetGuid(0),
                reader.GetGuid(1),
                GetNullableGuid(reader, 2),
                reader.GetGuid(3),
                reader.GetInt64(4),
                reader.GetInt64(5),
                reader.GetInt64(6),
                reader.GetString(7),
                GetSafeString(reader, 8),
                GetSafeString(reader, 9),
                GetSafeString(reader, 10),
                GetSafeString(reader, 11),
                reader.GetFieldValue<DateTimeOffset>(12),
                reader.GetFieldValue<DateTimeOffset>(13)));
        }

        return results;
    }

    private static async Task<IReadOnlyList<FiscalTotalReadModel>> ReadTotalsAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            """
            select
                fiscal_total_id,
                fiscal_document_id,
                total_type_code_id,
                amount_minor_units,
                currency_code,
                total_context::text,
                created_at,
                updated_at
            from pos.fiscal_totals
            where fiscal_document_id = @fiscal_document_id
            order by total_type_code_id, fiscal_total_id;
            """,
            fiscalDocumentId);

        var results = new List<FiscalTotalReadModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new FiscalTotalReadModel(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetInt64(3),
                reader.GetString(4),
                GetSafeString(reader, 5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return results;
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, Guid fiscalDocumentId)
    {
        var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("fiscal_document_id", fiscalDocumentId);
        return command;
    }

    private static Guid? GetNullableGuid(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);

    private static decimal? GetNullableDecimal(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private static long? GetNullableInt64(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);

    private static DateOnly? GetNullableDateOnly(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateOnly>(ordinal);

    private static DateTimeOffset? GetNullableDateTimeOffset(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);

    private static string? GetSafeString(NpgsqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetString(ordinal);
        return ContainsSensitiveMarker(value) ? null : value;
    }

    private static bool ContainsSensitiveMarker(string value) =>
        SensitiveMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
