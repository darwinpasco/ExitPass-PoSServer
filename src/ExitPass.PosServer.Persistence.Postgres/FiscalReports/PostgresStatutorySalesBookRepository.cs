using System.Data;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresStatutorySalesBookRepository(
    NpgsqlDataSource dataSource,
    IStatutoryBeneficiaryIdentityProtector protector) : IStatutorySalesBookRepository
{
    private static readonly Guid ZReadingKindId = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");

    public async Task<StatutorySalesBookResult> ReadAsync(StatutorySalesBookQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);
            await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
                await readOnly.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            var header = await ReadHeaderAsync(connection, transaction, query, cancellationToken).ConfigureAwait(false);
            if (header is null) return new(StatutorySalesBookOutcome.ScopeNotFound, SafeMessage: "The statutory sales-book scope is unavailable.");
            var rows = await ReadRowsAsync(connection, transaction, query, cancellationToken).ConfigureAwait(false);
            if (rows.MissingSnapshotReference is not null)
                return new(StatutorySalesBookOutcome.MissingBeneficiarySnapshot, SafeMessage: $"E2_E3_REQUIRED_BENEFICIARY_SNAPSHOT_MISSING:{rows.MissingSnapshotReference}");

            var ordered = rows.Rows.OrderBy(x => x.TransactionTimestamp).ThenBy(x => x.FiscalSequence).ThenBy(x => x.FiscalDocumentId).ToArray();
            if (ordered.Select(x => x.FiscalDocumentId).Distinct().Count() != ordered.Length ||
                ordered.Any(row => !StatutorySalesBookReconciliation.IsValid(row)))
                return new(StatutorySalesBookOutcome.ReconciliationFailure, SafeMessage: "The statutory sales-book rows did not reconcile with committed fiscal evidence.");
            var period = await ReadPeriodStatusAsync(connection, transaction, query, cancellationToken).ConfigureAwait(false);
            var asOf = ordered.Select(x => x.TransactionTimestamp).DefaultIfEmpty(header.GeneratedAt).Max();
            var summary = new StatutorySalesBookSummary(
                query.Type, query.SitePosServerId, query.FiscalIdentityId, query.ChannelTerminalId, query.CurrencyCode,
                query.CalendarYear, query.CalendarMonth, header.PosTerminalNumber, ordered.Length,
                Sum(ordered, x => x.SalesInclusiveVatMinorUnits), Sum(ordered, x => x.VatAmountMinorUnits),
                Sum(ordered, x => x.VatExemptSalesMinorUnits), Sum(ordered, x => x.Discount5MinorUnits),
                Sum(ordered, x => x.Discount20MinorUnits), Sum(ordered, x => x.NetSalesMinorUnits), asOf,
                rows.Warnings.Order(StringComparer.Ordinal).ToArray(), period.Status, ordered.Length == 0,
                period.TotalCount, period.ClosedCount, period.CommittedZCount);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(StatutorySalesBookOutcome.Succeeded, new(header, summary, ordered));
        }
        catch (NpgsqlException)
        {
            return new(StatutorySalesBookOutcome.PersistenceFailure, SafeMessage: "The statutory sales-book source read failed safely.");
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return new(StatutorySalesBookOutcome.PersistenceFailure, SafeMessage: "The statutory beneficiary snapshot could not be opened safely.");
        }
    }

    private static async Task<PeriodStatus> ReadPeriodStatusAsync(NpgsqlConnection c, NpgsqlTransaction t, StatutorySalesBookQuery q, CancellationToken ct)
    {
        const string sql = """
        SELECT COUNT(*)::integer,
               COUNT(*) FILTER (WHERE lower(status.code_key)='closed')::integer,
               COUNT(*) FILTER (
                   WHERE lower(status.code_key)='closed'
                     AND EXISTS (
                         SELECT 1
                         FROM pos.x_z_reports z
                         JOIN pos.fiscal_report_requests request
                           ON request.fiscal_report_request_id=z.fiscal_report_request_id
                         JOIN pos.controlled_codes request_status
                           ON request_status.controlled_code_id=request.report_status_code_id
                         WHERE z.fiscal_reporting_period_id=period.fiscal_reporting_period_id
                           AND z.report_kind_code_id=@z_kind
                           AND lower(request_status.code_key)='committed'
                     )
               )::integer
        FROM pos.fiscal_reporting_periods period
        JOIN pos.controlled_codes status ON status.controlled_code_id=period.period_status_code_id
        WHERE period.site_pos_server_id=@site AND period.fiscal_identity_id=@identity
          AND period.currency_code=@currency AND period.business_day_date>=@month_start
          AND period.business_day_date<@month_end
        """;
        await using var cmd = new NpgsqlCommand(sql, c, t);
        BindScope(cmd, q);
        cmd.Parameters.AddWithValue("z_kind", ZReadingKindId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        await reader.ReadAsync(ct).ConfigureAwait(false);
        var total = reader.GetInt32(0);
        var closed = reader.GetInt32(1);
        var committedZ = reader.GetInt32(2);
        return new(StatutorySalesBookStatuses.Resolve(total, closed, committedZ), total, closed, committedZ);
    }

    private static async Task<StatutorySalesBookHeader?> ReadHeaderAsync(NpgsqlConnection c, NpgsqlTransaction t, StatutorySalesBookQuery q, CancellationToken ct)
    {
        const string sql = """
        SELECT fi.registered_business_name, fi.registered_business_address, fi.tin,
               COALESCE(fi.software_name, @software), COALESCE(fi.software_version, @version),
               hp.pos_serial_number, hp.machine_identification_number, terminal.channel_terminal_code,
               hp.effective_from, transaction_timestamp()
        FROM pos.site_pos_servers site
        JOIN pos.fiscal_identities fi ON fi.fiscal_identity_id=@identity AND fi.is_active
        JOIN pos.channel_terminals terminal ON terminal.channel_terminal_id=@terminal
             AND terminal.site_pos_server_id=site.site_pos_server_id AND terminal.is_active
        JOIN LATERAL (
            SELECT p.* FROM pos.sales_invoice_header_profiles p
            WHERE p.site_pos_server_id=site.site_pos_server_id AND p.fiscal_identity_id=fi.fiscal_identity_id
              AND p.lifecycle_status='APPROVED' AND p.effective_from < @month_end
              AND (p.effective_to IS NULL OR p.effective_to >= @month_start)
            ORDER BY p.effective_from DESC, p.sales_invoice_header_profile_id DESC LIMIT 1
        ) hp ON true
        WHERE site.site_pos_server_id=@site AND site.is_active
        """;
        await using var cmd = new NpgsqlCommand(sql, c, t);
        BindScope(cmd, q);
        cmd.Parameters.AddWithValue("software", StatutorySalesBookContract.SoftwareName);
        cmd.Parameters.AddWithValue("version", StatutorySalesBookContract.SoftwareVersion);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await r.ReadAsync(ct).ConfigureAwait(false)) return null;
        return new(
            r.IsDBNull(0) ? string.Empty : r.GetString(0), r.IsDBNull(1) ? string.Empty : r.GetString(1), r.IsDBNull(2) ? string.Empty : r.GetString(2),
            r.GetString(3), r.GetString(4), StatutorySalesBookContract.ReleaseNumber, StatutorySalesBookContract.ReleaseDate,
            r.IsDBNull(5) ? string.Empty : r.GetString(5), r.IsDBNull(6) ? string.Empty : r.GetString(6), r.GetString(7),
            r.GetFieldValue<DateTimeOffset>(9), q.RequestedByRef);
    }

    private async Task<RowReadResult> ReadRowsAsync(NpgsqlConnection c, NpgsqlTransaction t, StatutorySalesBookQuery q, CancellationToken ct)
    {
        const string sql = """
        SELECT d.fiscal_document_id, d.business_day_date, COALESCE(d.fiscal_number_assigned_at,d.created_at),
               d.fiscal_sequence_value, d.fiscal_document_number,
               f.statutory_discount_decision_command_id, ent.code_key, basis.code_key,
               f.ordinance_reference, f.statutory_discount_rate_basis_points,
               f.percentage_discount_amount_minor_units, f.percentage_component_separately_proven,
               f.original_amount_minor_units, f.vat_amount_minor_units, f.final_payable_amount_minor_units,
               COALESCE((SELECT SUM(td.taxable_amount_minor_units)
                         FROM pos.fiscal_tax_details td
                         JOIN pos.controlled_codes tc ON tc.controlled_code_id=td.tax_classification_code_id
                         WHERE td.fiscal_document_id=d.fiscal_document_id AND upper(tc.code_key) LIKE '%EXEMPT%'),0),
               s.protected_identity_envelope, s.protection_key_reference, s.protection_key_version,
               s.envelope_format_version
        FROM pos.fiscal_documents d
        JOIN pos.controlled_codes dt ON dt.controlled_code_id=d.fiscal_document_type_code_id AND dt.code_key='sales_invoice'
        JOIN pos.controlled_codes document_status
          ON document_status.controlled_code_id=d.fiscal_document_status_code_id
         AND document_status.code_key='issued'
        JOIN pos.fiscal_document_applied_statutory_facts f ON f.fiscal_document_id=d.fiscal_document_id
        JOIN pos.controlled_codes ent ON ent.controlled_code_id=f.entitlement_type_code_id
        JOIN pos.controlled_codes basis ON basis.controlled_code_id=f.policy_resolution_basis_code_id
        LEFT JOIN pos.fiscal_document_statutory_beneficiary_snapshots s ON s.fiscal_document_id=d.fiscal_document_id
        WHERE d.site_pos_server_id=@site AND d.fiscal_identity_id=@identity AND d.channel_terminal_id=@terminal
          AND d.currency_code=@currency AND d.business_day_date>=@month_start AND d.business_day_date<@month_end
          AND d.fiscal_document_number IS NOT NULL AND d.fiscal_sequence_value IS NOT NULL AND d.is_active
          AND d.voided_at IS NULL AND upper(ent.code_key)=@entitlement
        ORDER BY COALESCE(d.fiscal_number_assigned_at,d.created_at),d.fiscal_sequence_value,d.fiscal_document_id
        """;
        await using var cmd = new NpgsqlCommand(sql, c, t);
        BindScope(cmd, q);
        cmd.Parameters.AddWithValue("entitlement", StatutorySalesBookContract.Entitlement(q.Type));
        var result = new List<StatutorySalesBookRow>();
        var warnings = new HashSet<string>(StringComparer.Ordinal);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            var documentId = r.GetGuid(0);
            var documentNumber = r.GetString(4);
            var rate = r.IsDBNull(9) ? (int?)null : r.GetInt32(9);
            var discount = r.GetInt64(10);
            var warning = StatutorySalesBookQualification.SafeExclusionClassification(
                r.GetString(6), q.Type, r.GetString(7), rate, r.GetBoolean(11));
            if (warning is not null) warnings.Add(warning);
            if (!StatutorySalesBookQualification.IsQualifyingPercentageRow(
                    r.GetString(6), q.Type, r.GetString(7), rate, discount, r.GetBoolean(11)))
                continue;
            if (r.IsDBNull(16)) return new([], documentNumber, warnings.ToArray());
            var decisionId = r.GetGuid(5);
            var identity = protector.Unprotect(documentId, decisionId, new(
                (byte[])r.GetValue(16), r.GetString(17), r.GetString(18), r.GetInt16(19)));
            result.Add(new(documentId, r.GetFieldValue<DateOnly>(1), r.GetFieldValue<DateTimeOffset>(2), r.GetInt64(3),
                identity.BeneficiaryFullName, identity.StatutoryIdType, identity.StatutoryIdNumber, identity.BeneficiaryTin,
                documentNumber, r.GetInt64(12), r.GetInt64(13), r.GetInt64(15), rate == 500 ? discount : 0,
                rate == 2000 ? discount : 0, r.GetInt64(14), r.IsDBNull(8) ? string.Empty : r.GetString(8), rate!.Value));
        }
        return new(result, null, warnings.ToArray());
    }

    private static void BindScope(NpgsqlCommand command, StatutorySalesBookQuery query)
    {
        var start = new DateOnly(query.CalendarYear, query.CalendarMonth, 1);
        command.Parameters.AddWithValue("site", query.SitePosServerId);
        command.Parameters.AddWithValue("identity", query.FiscalIdentityId);
        command.Parameters.AddWithValue("terminal", query.ChannelTerminalId);
        command.Parameters.AddWithValue("currency", query.CurrencyCode);
        command.Parameters.AddWithValue("month_start", start);
        command.Parameters.AddWithValue("month_end", start.AddMonths(1));
    }

    private static long Sum(IEnumerable<StatutorySalesBookRow> rows, Func<StatutorySalesBookRow, long> selector)
    {
        long value = 0;
        foreach (var row in rows) value = checked(value + selector(row));
        return value;
    }

    private sealed record RowReadResult(
        IReadOnlyList<StatutorySalesBookRow> Rows,
        string? MissingSnapshotReference,
        IReadOnlyList<string> Warnings);

    private sealed record PeriodStatus(string Status, int TotalCount, int ClosedCount, int CommittedZCount);
}
