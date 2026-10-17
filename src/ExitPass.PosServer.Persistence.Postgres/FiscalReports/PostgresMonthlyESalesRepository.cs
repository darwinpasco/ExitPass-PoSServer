using System.Data;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresMonthlyESalesRepository(NpgsqlDataSource dataSource) : IMonthlyESalesRepository
{
    private static readonly Guid ZReadingKindId = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");

    public async Task<MonthlyESalesResult> ReadAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);
            await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
                await readOnly.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            var currentSiteDate = await ReadCurrentSiteDateAsync(connection, transaction, query.SitePosServerId, cancellationToken).ConfigureAwait(false);
            if (currentSiteDate is null)
                return new(MonthlyESalesOutcome.ScopeMismatch, SafeMessage: "The governed Site POS Server scope is unavailable.");
            var sources = await ReadSourcesAsync(connection, transaction, query, cancellationToken).ConfigureAwait(false);
            var result = MonthlyESalesAggregator.Build(query, currentSiteDate.Value, sources);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (NpgsqlException)
        {
            return new(MonthlyESalesOutcome.PersistenceFailure,
                SafeMessage: "Monthly eSales authoritative source readback failed safely.");
        }
    }

    public async Task<MonthlyESalesPdfResult> ReadPdfAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);
            await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
                await readOnly.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            var currentSiteDate = await ReadCurrentSiteDateAsync(connection, transaction, query.SitePosServerId, cancellationToken).ConfigureAwait(false);
            if (currentSiteDate is null)
                return new(MonthlyESalesOutcome.ScopeMismatch, SafeMessage: "The governed Site POS Server scope is unavailable.");
            var sources = await ReadSourcesAsync(connection, transaction, query, cancellationToken).ConfigureAwait(false);
            var aggregate = MonthlyESalesAggregator.Build(query, currentSiteDate.Value, sources);
            if (aggregate.Report is null)
                return new(aggregate.Outcome, SafeMessage: aggregate.SafeMessage);
            var pdf = BuildPdfReport(aggregate.Report, sources);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return pdf;
        }
        catch (NpgsqlException)
        {
            return new(MonthlyESalesOutcome.PersistenceFailure,
                SafeMessage: "Monthly eSales authoritative source readback failed safely.");
        }
    }

    private static async Task<DateOnly?> ReadCurrentSiteDateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sitePosServerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT (clock_timestamp() AT TIME ZONE reporting_timezone_name)::date
        FROM pos.site_pos_servers
        WHERE site_pos_server_id = @site AND is_active
        """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("site", sitePosServerId);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is DateOnly date ? date : null;
    }

    private static async Task<IReadOnlyList<MonthlyESalesSourcePeriod>> ReadSourcesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MonthlyESalesQuery query,
        CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT p.fiscal_reporting_period_id,
               p.period_sequence,
               p.business_day_date,
               ps.code_key,
               p.closed_at,
               z.x_z_report_id,
               zrs.code_key,
               b.bir_sales_summary_report_id,
               brs.code_key,
               fi.tin,
               fi.registered_business_name,
               fi.registered_business_address,
               site.display_name,
               fi.software_version,
               b.pos_serial_number,
               b.machine_identification_number,
               b.ptu_number,
               b.transaction_count,
               b.beginning_si_ref,
               b.ending_si_ref,
               b.gross_sales_amount_minor_units,
               b.vatable_sales_amount_minor_units,
               b.vat_amount_minor_units,
               b.vat_exempt_sales_amount_minor_units,
               b.zero_rated_sales_amount_minor_units,
               b.committed_at
        FROM pos.fiscal_reporting_periods p
        JOIN pos.controlled_codes ps ON ps.controlled_code_id = p.period_status_code_id
        JOIN pos.fiscal_identities fi ON fi.fiscal_identity_id = p.fiscal_identity_id
        JOIN pos.site_pos_servers site ON site.site_pos_server_id = p.site_pos_server_id
        LEFT JOIN pos.x_z_reports z
          ON z.fiscal_reporting_period_id = p.fiscal_reporting_period_id
         AND z.report_kind_code_id = @z_kind
        LEFT JOIN pos.fiscal_report_requests zr ON zr.fiscal_report_request_id = z.fiscal_report_request_id
        LEFT JOIN pos.controlled_codes zrs ON zrs.controlled_code_id = zr.report_status_code_id
        LEFT JOIN pos.bir_sales_summary_reports b ON b.governing_z_report_id = z.x_z_report_id
        LEFT JOIN pos.fiscal_report_requests br ON br.fiscal_report_request_id = b.fiscal_report_request_id
        LEFT JOIN pos.controlled_codes brs ON brs.controlled_code_id = br.report_status_code_id
        WHERE p.site_pos_server_id = @site
          AND p.fiscal_identity_id = @identity
          AND p.currency_code = @currency
          AND EXTRACT(YEAR FROM p.business_day_date) = @year
          AND EXTRACT(MONTH FROM p.business_day_date) = @month
        ORDER BY p.period_sequence
        """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("z_kind", ZReadingKindId);
        command.Parameters.AddWithValue("site", query.SitePosServerId);
        command.Parameters.AddWithValue("identity", query.FiscalIdentityId);
        command.Parameters.AddWithValue("currency", query.CurrencyCode);
        command.Parameters.AddWithValue("year", query.CalendarYear);
        command.Parameters.AddWithValue("month", query.CalendarMonth);
        var result = new List<MonthlyESalesSourcePeriod>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var periodClosed = !reader.IsDBNull(4) && string.Equals(reader.GetString(3), "closed", StringComparison.Ordinal);
            var zCommitted = !reader.IsDBNull(5) && !reader.IsDBNull(6) && string.Equals(reader.GetString(6), "committed", StringComparison.Ordinal);
            var birCommitted = !reader.IsDBNull(7) && !reader.IsDBNull(8) && string.Equals(reader.GetString(8), "committed", StringComparison.Ordinal);
            result.Add(new(
                reader.GetGuid(0), reader.GetInt64(1), reader.GetFieldValue<DateOnly>(2), periodClosed, zCommitted, birCommitted,
                reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.IsDBNull(17) ? 0 : reader.GetInt64(17),
                reader.IsDBNull(18) ? null : reader.GetString(18),
                reader.IsDBNull(19) ? null : reader.GetString(19),
                reader.IsDBNull(20) ? 0 : reader.GetInt64(20),
                reader.IsDBNull(21) ? 0 : reader.GetInt64(21),
                reader.IsDBNull(22) ? 0 : reader.GetInt64(22),
                reader.IsDBNull(23) ? 0 : reader.GetInt64(23),
                reader.IsDBNull(24) ? 0 : reader.GetInt64(24),
                reader.IsDBNull(25) ? null : reader.GetFieldValue<DateTimeOffset>(25)));
        }
        return result;
    }

    private static MonthlyESalesPdfResult BuildPdfReport(
        MonthlyESalesReport report,
        IReadOnlyList<MonthlyESalesSourcePeriod> sources)
    {
        static (bool Valid, string? Value) Stable(IEnumerable<string?> values)
        {
            var distinct = values.Select(value => value?.Trim()).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).ToArray();
            return (distinct.Length <= 1, distinct.SingleOrDefault());
        }

        var name = Stable(sources.Select(source => source.RegisteredBusinessName));
        var address = Stable(sources.Select(source => source.RegisteredBusinessAddress));
        var site = Stable(sources.Select(source => source.SiteDisplayName));
        var version = Stable(sources.Select(source => source.SoftwareVersion));
        if (!name.Valid || !address.Valid || !site.Valid || !version.Valid)
            return new(MonthlyESalesOutcome.UnsupportedSourceClassification,
                SafeMessage: "The governed Monthly eSales taxpayer or site header is not stable across the selected month.");

        var machines = new List<MonthlyESalesMachineSection>();
        foreach (var group in sources.GroupBy(source => source.MachineIdentificationNumber!.Trim(), StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var ordered = group.OrderBy(source => source.PeriodSequence).ToArray();
            var serial = Stable(ordered.Select(source => source.PosSerialNumber));
            var ptu = Stable(ordered.Select(source => source.PtuNumber));
            if (!serial.Valid || !ptu.Valid)
                return new(MonthlyESalesOutcome.UnsupportedSourceClassification,
                    SafeMessage: $"The governed machine header is not stable for MIN {group.Key}.");
            var total = report.Rows.Single(row => string.Equals(row.MachineIdentificationNumber, group.Key, StringComparison.Ordinal));
            machines.Add(new(
                serial.Value ?? string.Empty,
                group.Key,
                ptu.Value ?? string.Empty,
                total,
                ordered.Select(source => new MonthlyESalesDailyRow(
                    source.BusinessDayDate,
                    source.BeginningSalesInvoiceReference,
                    source.EndingSalesInvoiceReference,
                    source.TransactionCount,
                    source.GrossSalesAmountMinorUnits,
                    source.VatableSalesAmountMinorUnits,
                    source.VatAmountMinorUnits,
                    source.VatExemptSalesAmountMinorUnits,
                    source.ZeroRatedSalesAmountMinorUnits)).ToArray()));
        }
        var reportAsOf = sources.Select(source => source.BirSalesSummaryCommittedAt).Where(value => value.HasValue)
            .Select(value => value!.Value).DefaultIfEmpty(DateTimeOffset.UnixEpoch).Max();
        return new(MonthlyESalesOutcome.Succeeded,
            new(report, name.Value, address.Value, site.Value, version.Value, reportAsOf, machines));
    }
}
