using System.Globalization;
using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresAuditTrailRepository(NpgsqlDataSource dataSource) : IAuditTrailRepository
{
    public async Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var header = await ReadHeaderAsync(connection, query, cancellationToken).ConfigureAwait(false);
            if (header is null)
                return new(AuditTrailOutcome.ScopeNotFound, SafeMessage: "The requested Audit Trail scope is unavailable.");

            var rows = new List<AuditTrailRow>();
            await ReadElectronicJournalRowsAsync(connection, query, rows, cancellationToken).ConfigureAwait(false);
            await ReadAnnexRowsAsync(connection, query, rows, cancellationToken).ConfigureAwait(false);
            await ReadElectronicJournalAccessRowsAsync(connection, query, rows, cancellationToken).ConfigureAwait(false);
            await ReadFiscalAccessRowsAsync(connection, query, rows, cancellationToken).ConfigureAwait(false);

            var filtered = rows
                .Where(row => Matches(row.ActionCode, query.ActionCode))
                .Where(row => Matches(row.UserId, query.Actor) || Matches(row.ServiceIdentity, query.Actor))
                .Where(row => Matches(row.Result, query.Result))
                .Where(row => Matches(row.SalesInvoiceNumber, query.Reference) || Matches(row.TargetReference, query.Reference) || Matches(row.CorrelationReference, query.Reference))
                .OrderBy(row => row.OccurredAt)
                .ThenBy(row => row.AuditReference, StringComparer.Ordinal)
                .Take(AuditTrailContract.MaximumRows + 1)
                .ToArray();
            if (filtered.Length > AuditTrailContract.MaximumRows)
                return new(AuditTrailOutcome.RangeTooLarge, SafeMessage: "The requested Audit Trail range contains too many rows. Narrow the range or filters.");
            return new(AuditTrailOutcome.Succeeded,
                new(AuditTrailContract.ContractVersion, query.SitePosServerId, query.From, query.To, header, AuditTrailActionCatalog.Definitions, filtered));
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException or JsonException or OverflowException)
        {
            return new(AuditTrailOutcome.PersistenceFailure, SafeMessage: "The Audit Trail is temporarily unavailable.");
        }
    }

    public async Task RecordAccessAsync(
        Guid sitePosServerId,
        string reportKind,
        string subjectReference,
        string actorReference,
        string correlationReference,
        int rowCount,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand("""
            INSERT INTO pos.access_audit(
                access_audit_id,site_pos_server_id,access_subject_type_code_id,access_action_code_id,
                access_result_code_id,subject_record_ref,accessor_ref,request_ref,occurred_at,access_context,created_at)
            SELECT @id,@site,subject.controlled_code_id,action.controlled_code_id,result.controlled_code_id,
                   @subject,@actor,@correlation,clock_timestamp(),@context,clock_timestamp()
            FROM pos.controlled_codes subject
            JOIN pos.controlled_code_sets subject_set ON subject_set.controlled_code_set_id=subject.controlled_code_set_id
            CROSS JOIN pos.controlled_codes action
            JOIN pos.controlled_code_sets action_set ON action_set.controlled_code_set_id=action.controlled_code_set_id
            CROSS JOIN pos.controlled_codes result
            JOIN pos.controlled_code_sets result_set ON result_set.controlled_code_set_id=result.controlled_code_set_id
            WHERE subject_set.code_set_key='fiscal_report_output_type' AND subject.code_key='external_export_reference'
              AND action_set.code_set_key='electronic_journal_access_action' AND action.code_key='read'
              AND result_set.code_set_key='electronic_journal_access_result' AND result.code_key='allowed';
            """, connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("subject", Safe(subjectReference, 200));
        command.Parameters.AddWithValue("actor", Safe(actorReference, 200));
        command.Parameters.AddWithValue("correlation", Safe(correlationReference, 200));
        command.Parameters.Add("context", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(new
        {
            schemaVersion = "pos-server-fiscal-report-access:v1",
            reportKind = Safe(reportKind, 80),
            rowCount
        });
        if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            throw new InvalidOperationException("Fiscal report read audit classification is unavailable.");
    }

    public async Task RecordExportAsync(
        Guid sitePosServerId,
        string reportKind,
        string subjectReference,
        string format,
        string actorReference,
        string correlationReference,
        string contentSha256,
        int rowCount,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand("""
            INSERT INTO pos.access_audit(
                access_audit_id,site_pos_server_id,access_subject_type_code_id,access_action_code_id,
                access_result_code_id,subject_record_ref,accessor_ref,request_ref,occurred_at,access_context,created_at)
            SELECT @id,@site,subject.controlled_code_id,action.controlled_code_id,result.controlled_code_id,
                   @subject,@actor,@correlation,clock_timestamp(),@context,clock_timestamp()
            FROM pos.controlled_codes subject
            JOIN pos.controlled_code_sets subject_set ON subject_set.controlled_code_set_id=subject.controlled_code_set_id
            CROSS JOIN pos.controlled_codes action
            JOIN pos.controlled_code_sets action_set ON action_set.controlled_code_set_id=action.controlled_code_set_id
            CROSS JOIN pos.controlled_codes result
            JOIN pos.controlled_code_sets result_set ON result_set.controlled_code_set_id=result.controlled_code_set_id
            WHERE subject_set.code_set_key='fiscal_report_output_type' AND subject.code_key='external_export_reference'
              AND action_set.code_set_key='electronic_journal_access_action' AND action.code_key='export'
              AND result_set.code_set_key='electronic_journal_access_result' AND result.code_key='allowed';
            """, connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("subject", Safe(subjectReference, 200));
        command.Parameters.AddWithValue("actor", Safe(actorReference, 200));
        command.Parameters.AddWithValue("correlation", Safe(correlationReference, 200));
        var context = JsonSerializer.Serialize(new
        {
            schemaVersion = "pos-server-fiscal-export-access:v1",
            reportKind = Safe(reportKind, 80),
            format = Safe(format, 20).ToUpperInvariant(),
            contentSha256 = Safe(contentSha256, 64),
            rowCount
        });
        command.Parameters.Add("context", NpgsqlDbType.Jsonb).Value = context;
        if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            throw new InvalidOperationException("Fiscal report export audit classification is unavailable.");
    }

    private static async Task<AuditTrailHeader?> ReadHeaderAsync(NpgsqlConnection connection, AuditTrailQuery query, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT h.registered_business_name,h.registered_business_address,h.tin,
                   h.machine_identification_number,h.pos_serial_number,h.parking_location_display
            FROM pos.fiscal_document_header_snapshots h
            JOIN pos.fiscal_documents d ON d.fiscal_document_id=h.fiscal_document_id
            WHERE d.site_pos_server_id=@site AND h.effective_at < @to
            ORDER BY h.effective_at DESC,h.fiscal_document_header_snapshot_id DESC
            LIMIT 1;
            """, connection);
        command.Parameters.AddWithValue("site", query.SitePosServerId);
        command.Parameters.AddWithValue("to", query.To);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5))
            : null;
    }

    private static async Task ReadElectronicJournalRowsAsync(NpgsqlConnection connection, AuditTrailQuery query, List<AuditTrailRow> rows, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT ej.electronic_journal_record_id,ej.event_reference,event_type.code_key,ej.effective_at,
                   ej.actor_ref,ej.service_identity_ref,ej.correlation_ref,ej.fiscal_document_id,ej.fiscal_report_request_id,
                   d.fiscal_document_number,COALESCE(d.business_day_date,xz.business_day_date,bir.business_day_date),d.document_context::text,d.void_reason_text,
                   h.terminal_id,xz.report_number,xz.transaction_count,xz.net_sales_amount_minor_units,
                   bir.bir_sales_summary_report_id,bir.transaction_count,bir.net_sales_amount_minor_units,
                   COALESCE(tender.code_key,''),COALESCE(tender.amount_minor_units,0),
                   COALESCE(discounts.discount_amount_minor_units,0),d.fiscal_number_assigned_at,
                   reprint.copy_sequence,reprint.requested_at
            FROM pos.electronic_journal_records ej
            JOIN pos.controlled_codes event_type ON event_type.controlled_code_id=ej.journal_record_type_code_id
            LEFT JOIN pos.fiscal_documents d ON d.fiscal_document_id=ej.fiscal_document_id
            LEFT JOIN pos.fiscal_document_header_snapshots h ON h.fiscal_document_id=d.fiscal_document_id
            LEFT JOIN LATERAL (
                SELECT min(c.code_key) AS code_key,sum(t.amount_minor_units) AS amount_minor_units
                FROM pos.fiscal_tenders t JOIN pos.controlled_codes c ON c.controlled_code_id=t.tender_type_code_id
                WHERE t.fiscal_document_id=d.fiscal_document_id
            ) tender ON true
            LEFT JOIN LATERAL (
                SELECT sum(p.discount_amount_minor_units) AS discount_amount_minor_units
                FROM pos.fiscal_discount_privilege_details p WHERE p.fiscal_document_id=d.fiscal_document_id
            ) discounts ON true
            LEFT JOIN pos.x_z_reports xz ON xz.x_z_report_id=ej.x_z_report_id
            LEFT JOIN pos.bir_sales_summary_reports bir ON bir.bir_sales_summary_report_id=ej.bir_sales_summary_report_id
            LEFT JOIN pos.reprint_requests reprint ON reprint.reprint_request_id=ej.reprint_request_id
            WHERE ej.is_canonical AND ej.site_pos_server_id=@site
              AND ej.effective_at>=@from AND ej.effective_at<@to
              AND event_type.code_key IN (
                'fiscal_document_committed','fiscal_document_voided','fiscal_document_reprint_recorded',
                'fiscal_adjustment_recorded','x_reading_committed','z_reading_committed','bir_sales_summary_committed')
            ORDER BY ej.effective_at,ej.electronic_journal_record_id;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("site", query.SitePosServerId);
        command.Parameters.AddWithValue("from", query.From);
        command.Parameters.AddWithValue("to", query.To);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(MapElectronicJournal(reader));
    }

    private static AuditTrailRow MapElectronicJournal(NpgsqlDataReader reader)
    {
        var id = reader.GetGuid(0).ToString("D");
        var eventReference = NullableString(reader, 1) ?? id;
        var code = reader.GetString(2);
        var occurredAt = reader.GetFieldValue<DateTimeOffset>(3);
        var actor = NullableString(reader, 4);
        var service = NullableString(reader, 5);
        var correlation = NullableString(reader, 6);
        var documentId = NullableGuid(reader, 7);
        var reportRequestId = NullableGuid(reader, 8);
        var invoice = NullableString(reader, 9);
        var businessDate = NullableDate(reader, 10);
        using var documentContext = Parse(NullableString(reader, 11));
        var voidReason = NullableString(reader, 12);
        var headerTerminal = NullableString(reader, 13);
        var reportReference = NullableString(reader, 14);
        var reportTransactions = NullableLong(reader, 15);
        var reportNet = NullableLong(reader, 16);
        var birId = NullableGuid(reader, 17);
        var birTransactions = NullableLong(reader, 18);
        var birNet = NullableLong(reader, 19);
        var tender = reader.GetString(20);
        var tenderAmount = reader.GetInt64(21);
        var discount = reader.GetInt64(22);
        var assignedAt = NullableInstant(reader, 23);
        var reprintCopy = NullableLong(reader, 24);
        var reprintAt = NullableInstant(reader, 25);

        var cashier = Json(documentContext, "reference_context", "cashier_display_name");
        var human = !string.IsNullOrWhiteSpace(cashier) && !cashier.Equals("System", StringComparison.OrdinalIgnoreCase);
        var userId = human ? cashier : actor;
        var role = human ? "CASHIER" : "SYSTEM";
        var terminal = First(headerTerminal, Json(documentContext, "runtime_terminal_ref"), Json(documentContext, "site_pos_server_ref"), Json(documentContext, "reference_context", "payment_channel"));
        var category = AuditTrailActionCatalog.Category(code);
        var description = code switch
        {
            "fiscal_document_committed" => SalesDescription(documentContext, tender, tenderAmount, discount),
            "fiscal_document_voided" => $"SI No.:\n{invoice}\nResult:VOID",
            "fiscal_document_reprint_recorded" => $"SI No.:\n{invoice}\nCopy:REPRINT{(reprintCopy is null ? string.Empty : $" {reprintCopy}")}\nOriginal:\n{FormatPht(assignedAt ?? reprintAt ?? occurredAt)}",
            "x_reading_committed" => ReportDescription("X READING", businessDate, reportTransactions, reportNet, reportReference),
            "z_reading_committed" => ReportDescription("Z READING", businessDate, reportTransactions, reportNet, reportReference),
            "bir_sales_summary_committed" => ReportDescription("DAILY BIR SALES SUMMARY", businessDate, birTransactions, birNet, birId?.ToString("D")),
            "fiscal_adjustment_recorded" => "Governed fiscal adjustment recorded.",
            _ => category
        };
        var amount = code == "fiscal_document_committed" ? tenderAmount : (long?)null;
        return new(eventReference, occurredAt, terminal, userId, role, code, category, description, invoice,
            amount is null ? null : "P 0.00", amount is null ? null : Money(amount.Value), voidReason ?? "-",
            "SUCCESS", human ? "HUMAN" : "SYSTEM", service, documentId is null ? "FISCAL REPORT" : "FISCAL DOCUMENT",
            invoice ?? reportReference ?? birId?.ToString("D"), documentId, reportRequestId, correlation,
            new Dictionary<string, string?>
            {
                ["eventReference"] = eventReference,
                ["businessDate"] = businessDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["paymentMethod"] = string.IsNullOrEmpty(tender) ? null : tender.ToUpperInvariant()
            });
    }

    private static async Task ReadAnnexRowsAsync(NpgsqlConnection connection, AuditTrailQuery query, List<AuditTrailRow> rows, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT a.fiscal_action_audit_id,a.occurred_at,a.actor_ref,a.service_identity_ref,a.correlation_ref,
                   result.code_key,a.fiscal_report_request_id
            FROM pos.fiscal_action_audit a
            JOIN pos.controlled_codes action ON action.controlled_code_id=a.audit_action_type_code_id
            LEFT JOIN pos.controlled_codes result ON result.controlled_code_id=a.audit_result_code_id
            WHERE a.site_pos_server_id=@site AND a.occurred_at>=@from AND a.occurred_at<@to
              AND action.code_key='annex_e'
            ORDER BY a.occurred_at,a.fiscal_action_audit_id;
            """, connection);
        command.Parameters.AddWithValue("site", query.SitePosServerId); command.Parameters.AddWithValue("from", query.From); command.Parameters.AddWithValue("to", query.To);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var request = NullableGuid(reader, 6);
            rows.Add(new(reader.GetGuid(0).ToString("D"), reader.GetFieldValue<DateTimeOffset>(1), null, NullableString(reader, 2), "SYSTEM",
                "annex_e", "ANNEX E-1", $"ANNEX E-1\nReference:\n{request:D}", null, null, null, "-",
                (NullableString(reader, 5) ?? "success").ToUpperInvariant(), "SYSTEM", NullableString(reader, 3), "FISCAL REPORT", request?.ToString("D"), null, request,
                NullableString(reader, 4), new Dictionary<string, string?>()));
        }
    }

    private static async Task ReadElectronicJournalAccessRowsAsync(NpgsqlConnection connection, AuditTrailQuery query, List<AuditTrailRow> rows, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT a.electronic_journal_access_audit_id,a.occurred_at,action.code_key,result.code_key,
                   a.actor_ref,a.service_identity_ref,a.correlation_ref,a.support_reference,a.event_count
            FROM pos.electronic_journal_access_audit a
            JOIN pos.controlled_codes action ON action.controlled_code_id=a.action_code_id
            JOIN pos.controlled_codes result ON result.controlled_code_id=a.result_code_id
            WHERE a.site_pos_server_id=@site AND a.occurred_at>=@from AND a.occurred_at<@to
            ORDER BY a.occurred_at,a.electronic_journal_access_audit_id;
            """, connection);
        command.Parameters.AddWithValue("site", query.SitePosServerId); command.Parameters.AddWithValue("from", query.From); command.Parameters.AddWithValue("to", query.To);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var action = reader.GetString(2);
            rows.Add(new(reader.GetGuid(0).ToString("D"), reader.GetFieldValue<DateTimeOffset>(1), null, reader.GetString(4), "SYSTEM",
                "electronic_journal_accessed", action == "export" ? "REPORT EXPORT" : "REPORT ACCESS",
                $"Report:ELECTRONIC JOURNAL\nAction:{action.ToUpperInvariant()}\nEvents:{reader.GetInt32(8)}\nReference:\n{reader.GetString(7)}",
                null, null, null, "-", reader.GetString(3).ToUpperInvariant(), "SYSTEM", reader.GetString(5), "ELECTRONIC JOURNAL", reader.GetString(7), null, null,
                reader.GetString(6), new Dictionary<string, string?> { ["accessAction"] = action }));
        }
    }

    private static async Task ReadFiscalAccessRowsAsync(NpgsqlConnection connection, AuditTrailQuery query, List<AuditTrailRow> rows, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT a.access_audit_id,a.occurred_at,action.code_key,result.code_key,a.subject_record_ref,
                   a.accessor_ref,a.request_ref,a.access_context::text
            FROM pos.access_audit a
            JOIN pos.controlled_codes action ON action.controlled_code_id=a.access_action_code_id
            LEFT JOIN pos.controlled_codes result ON result.controlled_code_id=a.access_result_code_id
            WHERE a.site_pos_server_id=@site AND a.occurred_at>=@from AND a.occurred_at<@to
            ORDER BY a.occurred_at,a.access_audit_id;
            """, connection);
        command.Parameters.AddWithValue("site", query.SitePosServerId); command.Parameters.AddWithValue("from", query.From); command.Parameters.AddWithValue("to", query.To);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            using var context = Parse(NullableString(reader, 7));
            var kind = Json(context, "reportKind") ?? "FISCAL REPORT";
            var format = Json(context, "format") ?? string.Empty;
            var subject = NullableString(reader, 4);
            rows.Add(new(reader.GetGuid(0).ToString("D"), reader.GetFieldValue<DateTimeOffset>(1), null, NullableString(reader, 5), "SYSTEM",
                "fiscal_report_exported", "REPORT EXPORT", $"Report:{DisplayKind(kind)}\nFormat:{format}\nReference:\n{subject}",
                null, null, null, "-", (NullableString(reader, 3) ?? "allowed").ToUpperInvariant(), "SYSTEM", NullableString(reader, 5), "FISCAL REPORT",
                subject, null, null, NullableString(reader, 6), new Dictionary<string, string?> { ["reportKind"] = kind, ["format"] = format, ["contentSha256"] = Json(context, "contentSha256") }));
        }
    }

    private static string SalesDescription(JsonDocument? context, string tender, long amount, long discount)
    {
        var entry = Instant(Json(context, "reference_context", "entry_time"));
        return string.Join('\n', new[]
        {
            $"Plate:{Json(context, "reference_context", "plate_number") ?? string.Empty}",
            "Entry:",
            entry?.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            entry?.ToOffset(TimeSpan.FromHours(8)).ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty,
            $"Gross:{Decimal(amount)}",
            $"Net:{Money(amount)}",
            $"Discount:{Money(discount)}",
            $"Payment:{tender.ToUpperInvariant()}"
        });
    }

    private static string ReportDescription(string name, DateOnly? businessDate, long? transactions, long? net, string? reference) =>
        $"{name}\nFiscal Business Date:\n{businessDate:yyyy-MM-dd}\nTransactions:{transactions ?? 0}\nNet:{Money(net ?? 0)}\nReference:\n{reference}";

    private static string DisplayKind(string value) => value.Trim().ToUpperInvariant().Replace('_', ' ');
    private static string Money(long minorUnits) => $"P {Decimal(minorUnits)}";
    private static string Decimal(long minorUnits) => (minorUnits / 100m).ToString("N2", CultureInfo.InvariantCulture);
    private static string FormatPht(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    private static string Safe(string value, int max) => string.IsNullOrWhiteSpace(value) ? "unavailable" : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static bool Matches(string? value, string? filter) => filter is null || value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;
    private static string? First(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    private static string? NullableString(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static Guid? NullableGuid(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    private static long? NullableLong(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    private static DateOnly? NullableDate(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateOnly>(ordinal);
    private static DateTimeOffset? NullableInstant(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    private static JsonDocument? Parse(string? json) => string.IsNullOrWhiteSpace(json) ? null : JsonDocument.Parse(json);
    private static string? Json(JsonDocument? document, params string[] path)
    {
        if (document is null) return null;
        var element = document.RootElement;
        foreach (var part in path) if (!element.TryGetProperty(part, out element)) return null;
        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }
    private static DateTimeOffset? Instant(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : null;
}
