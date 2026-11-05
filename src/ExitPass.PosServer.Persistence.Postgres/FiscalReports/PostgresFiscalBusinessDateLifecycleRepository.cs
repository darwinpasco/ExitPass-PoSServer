using System.Data;
using System.Security.Cryptography;
using System.Text;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresFiscalBusinessDateLifecycleRepository(NpgsqlDataSource dataSource)
    : IFiscalBusinessDateLifecycleRepository
{
    private static readonly Guid OpenStatusId = Guid.Parse("1a6f7021-bc84-5c01-afaa-c5d6685633c8");
    private static readonly Guid ClosingStatusId = Guid.Parse("f0a44431-611b-5809-b3a9-5b8be8614552");
    private static readonly Guid ClosedStatusId = Guid.Parse("af7ee931-a023-507e-81a4-17adf047eb94");

    public async Task<FiscalBusinessDateLifecycleSnapshot> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await ReadSnapshotAsync(connection, null, sitePosServerId, fiscalIdentityId, currencyCode, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OpenFiscalBusinessDateResult> OpenAsync(
        OpenFiscalBusinessDateCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(
                connection, transaction, command.SitePosServerId, command.FiscalIdentityId,
                command.CurrencyCode, cancellationToken).ConfigureAwait(false);

            var semanticHash = ComputeHash(command);
            const string replaySql = """
                SELECT semantic_request_hash, fiscal_reporting_period_id
                FROM pos.fiscal_business_date_open_operations
                WHERE site_pos_server_id=@site AND operation_key=@operation
                FOR UPDATE;
                """;
            await using (var replay = new NpgsqlCommand(replaySql, connection, transaction))
            {
                replay.Parameters.AddWithValue("site", command.SitePosServerId);
                replay.Parameters.AddWithValue("operation", command.OperationKey);
                await using var reader = await replay.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var matches = string.Equals(reader.GetString(0), semanticHash, StringComparison.Ordinal);
                    var replayPeriodId = reader.GetGuid(1);
                    await reader.CloseAsync().ConfigureAwait(false);
                    if (!matches)
                        return new(OpenFiscalBusinessDateOutcome.Conflict, SafeMessage: "The open operation key is bound to different semantics.");
                    var replaySnapshot = await ReadSnapshotAsync(connection, transaction, command.SitePosServerId,
                        command.FiscalIdentityId, command.CurrencyCode, cancellationToken).ConfigureAwait(false);
                    var replayPeriod = await ReadPeriodAsync(connection, transaction, replayPeriodId, cancellationToken).ConfigureAwait(false);
                    if (replayPeriod is null)
                        return new(OpenFiscalBusinessDateOutcome.PersistenceFailure, SafeMessage: "The opened fiscal business date could not be read safely.");
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return new(OpenFiscalBusinessDateOutcome.Replayed, replaySnapshot, OpenedPeriod: replayPeriod);
                }
            }

            var before = await ReadSnapshotAsync(connection, transaction, command.SitePosServerId,
                command.FiscalIdentityId, command.CurrencyCode, cancellationToken).ConfigureAwait(false);
            if (before.ActivePeriod is not null)
                return new(OpenFiscalBusinessDateOutcome.PeriodAlreadyOpen, before, "A fiscal business date is already active for this scope.");
            if (command.BusinessDateLabel != before.ProposedNextBusinessDateLabel)
                return new(OpenFiscalBusinessDateOutcome.InvalidRequest, before, "The fiscal business-date label does not match the authoritative proposed next label.");
            if (before.LastClosedPeriod is { ZReadingReference: null })
                return new(OpenFiscalBusinessDateOutcome.PriorPeriodUnresolved, before, "The prior fiscal business date does not have committed Z evidence.");

            const string insertPeriod = """
                INSERT INTO pos.fiscal_reporting_periods(
                    fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,
                    fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,
                    reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,
                    expected_prior_period_id,opened_at,created_by_ref,updated_by_ref)
                SELECT @period,contract.fiscal_reporting_contract_version_id,@site,@identity,@open,@label,
                       transaction_timestamp(),NULL,site.reporting_timezone_name,site.business_day_cutoff_local_time,
                       @currency,COALESCE((SELECT max(period_sequence)+1 FROM pos.fiscal_reporting_periods WHERE site_pos_server_id=@site),1),
                       @prior,transaction_timestamp(),@actor,@actor
                FROM pos.site_pos_servers site
                CROSS JOIN LATERAL (
                    SELECT fiscal_reporting_contract_version_id
                    FROM pos.fiscal_reporting_contract_versions
                    WHERE contract_key='pos-server-fiscal-reporting' AND contract_version='v1' AND is_active
                      AND effective_from<=transaction_timestamp()
                      AND (effective_to IS NULL OR effective_to>transaction_timestamp())
                    ORDER BY effective_from DESC LIMIT 1
                ) contract
                WHERE site.site_pos_server_id=@site AND site.is_active
                RETURNING fiscal_reporting_period_id,opened_at;
                """;
            var periodId = Guid.NewGuid();
            DateTimeOffset openedAt;
            await using (var insert = new NpgsqlCommand(insertPeriod, connection, transaction))
            {
                insert.Parameters.AddWithValue("period", periodId);
                insert.Parameters.AddWithValue("site", command.SitePosServerId);
                insert.Parameters.AddWithValue("identity", command.FiscalIdentityId);
                insert.Parameters.AddWithValue("open", OpenStatusId);
                insert.Parameters.AddWithValue("label", command.BusinessDateLabel);
                insert.Parameters.AddWithValue("currency", command.CurrencyCode);
                insert.Parameters.AddWithValue("prior", (object?)before.LastClosedPeriod?.FiscalReportingPeriodId ?? DBNull.Value);
                insert.Parameters.AddWithValue("actor", command.RequestedByRef);
                await using var reader = await insert.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return new(OpenFiscalBusinessDateOutcome.ConfigurationUnavailable, before, "The active POS fiscal reporting configuration is unavailable.");
                openedAt = reader.GetFieldValue<DateTimeOffset>(1);
            }

            const string insertOperation = """
                INSERT INTO pos.fiscal_business_date_open_operations(
                    fiscal_business_date_open_operation_id,site_pos_server_id,fiscal_identity_id,currency_code,
                    operation_key,semantic_request_hash,fiscal_reporting_period_id,requested_by_ref,
                    requested_by_display_name,service_identity_ref,correlation_ref,opened_at)
                VALUES(@id,@site,@identity,@currency,@operation,@hash,@period,@actor,@display,@service,@correlation,@opened);
                """;
            await using (var operation = new NpgsqlCommand(insertOperation, connection, transaction))
            {
                operation.Parameters.AddWithValue("id", Guid.NewGuid());
                operation.Parameters.AddWithValue("site", command.SitePosServerId);
                operation.Parameters.AddWithValue("identity", command.FiscalIdentityId);
                operation.Parameters.AddWithValue("currency", command.CurrencyCode);
                operation.Parameters.AddWithValue("operation", command.OperationKey);
                operation.Parameters.AddWithValue("hash", semanticHash);
                operation.Parameters.AddWithValue("period", periodId);
                operation.Parameters.AddWithValue("actor", command.RequestedByRef);
                operation.Parameters.AddWithValue("display", (object?)command.RequestedByDisplayName ?? DBNull.Value);
                operation.Parameters.AddWithValue("service", command.ServiceIdentityRef);
                operation.Parameters.AddWithValue("correlation", command.CorrelationId);
                operation.Parameters.AddWithValue("opened", openedAt);
                await operation.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            var after = await ReadSnapshotAsync(connection, transaction, command.SitePosServerId,
                command.FiscalIdentityId, command.CurrencyCode, cancellationToken).ConfigureAwait(false);
            var openedPeriod = await ReadPeriodAsync(connection, transaction, periodId, cancellationToken).ConfigureAwait(false);
            if (openedPeriod is null)
                return new(OpenFiscalBusinessDateOutcome.PersistenceFailure, SafeMessage: "The opened fiscal business date could not be read safely.");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(OpenFiscalBusinessDateOutcome.Opened, after, OpenedPeriod: openedPeriod);
        }
        catch (FiscalCloseBoundaryException ex) when (ex.ErrorCode == FiscalCloseBoundaryErrorCode.LockTimeout)
        {
            return new(OpenFiscalBusinessDateOutcome.LockTimeout, SafeMessage: ex.Message);
        }
        catch (NpgsqlException)
        {
            return new(OpenFiscalBusinessDateOutcome.PersistenceFailure, SafeMessage: "The fiscal business date could not be opened safely.");
        }
    }

    private static async Task<FiscalBusinessDateLifecycleSnapshot> ReadSnapshotAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid site, Guid identity,
        string currency, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT site.reporting_timezone_name,transaction_timestamp(),
                   p.fiscal_reporting_period_id,p.business_day_date,status.code_key,p.opened_at,p.closed_at,
                   p.period_sequence,
                   (SELECT count(*) FROM pos.fiscal_documents d WHERE d.fiscal_reporting_period_id=p.fiscal_reporting_period_id),
                   COALESCE(state.state_version,0),z.report_number
            FROM pos.site_pos_servers site
            LEFT JOIN LATERAL (
                SELECT * FROM pos.fiscal_reporting_periods value
                WHERE value.site_pos_server_id=site.site_pos_server_id
                  AND value.fiscal_identity_id=@identity AND value.currency_code=@currency
                ORDER BY CASE WHEN value.period_status_code_id IN (@open,@closing) THEN 0 ELSE 1 END,
                         value.period_sequence DESC LIMIT 2
            ) p ON TRUE
            LEFT JOIN pos.controlled_codes status ON status.controlled_code_id=p.period_status_code_id
            LEFT JOIN pos.fiscal_z_close_states state ON state.site_pos_server_id=site.site_pos_server_id
                 AND state.fiscal_identity_id=@identity AND state.currency_code=@currency
            LEFT JOIN pos.x_z_reports z ON z.fiscal_reporting_period_id=p.fiscal_reporting_period_id
                 AND z.report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'
            WHERE site.site_pos_server_id=@site AND site.is_active
            ORDER BY p.period_sequence DESC NULLS LAST;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("site", site);
        command.Parameters.AddWithValue("identity", identity);
        command.Parameters.AddWithValue("currency", currency);
        command.Parameters.AddWithValue("open", OpenStatusId);
        command.Parameters.AddWithValue("closing", ClosingStatusId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        string? timezone = null;
        DateTimeOffset now = default;
        FiscalBusinessDateLifecyclePeriod? active = null;
        FiscalBusinessDateLifecyclePeriod? lastClosed = null;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            timezone ??= reader.GetString(0);
            now = reader.GetFieldValue<DateTimeOffset>(1);
            if (reader.IsDBNull(2)) continue;
            var period = new FiscalBusinessDateLifecyclePeriod(
                reader.GetGuid(2), reader.GetFieldValue<DateOnly>(3), reader.GetString(4).ToUpperInvariant(),
                reader.GetFieldValue<DateTimeOffset>(5), reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetInt64(7), reader.GetInt64(8), reader.GetInt64(9), reader.IsDBNull(10) ? null : reader.GetString(10));
            if (period.Status is "OPEN" or "CLOSING") active ??= period;
            else if (period.Status == "CLOSED") lastClosed ??= period;
        }
        if (timezone is null)
            throw new InvalidOperationException("The active Site POS Server fiscal configuration is unavailable.");
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(timezone)).DateTime);
        var proposed = lastClosed?.BusinessDateLabel.AddDays(1) ?? localDate;
        return new(site, identity, currency, timezone,
            active?.Status ?? FiscalBusinessDateLifecycleStates.NoOpenPeriod,
            proposed, active, lastClosed, now);
    }

    private static async Task<FiscalBusinessDateLifecyclePeriod?> ReadPeriodAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid fiscalReportingPeriodId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.fiscal_reporting_period_id,p.business_day_date,status.code_key,p.opened_at,p.closed_at,
                   p.period_sequence,
                   (SELECT count(*) FROM pos.fiscal_documents d WHERE d.fiscal_reporting_period_id=p.fiscal_reporting_period_id),
                   COALESCE(state.state_version,0),z.report_number
            FROM pos.fiscal_reporting_periods p
            JOIN pos.controlled_codes status ON status.controlled_code_id=p.period_status_code_id
            LEFT JOIN pos.fiscal_z_close_states state ON state.site_pos_server_id=p.site_pos_server_id
                 AND state.fiscal_identity_id=p.fiscal_identity_id AND state.currency_code=p.currency_code
            LEFT JOIN pos.x_z_reports z ON z.fiscal_reporting_period_id=p.fiscal_reporting_period_id
                 AND z.report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'
            WHERE p.fiscal_reporting_period_id=@period;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("period", fiscalReportingPeriodId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            reader.GetGuid(0), reader.GetFieldValue<DateOnly>(1), reader.GetString(2).ToUpperInvariant(),
            reader.GetFieldValue<DateTimeOffset>(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7), reader.IsDBNull(8) ? null : reader.GetString(8));
    }

    private static string ComputeHash(OpenFiscalBusinessDateCommand command)
    {
        var canonical = string.Join("\n", "pos-server-fiscal-business-date-open:sha256:v1",
            command.SitePosServerId.ToString("D"), command.FiscalIdentityId.ToString("D"),
            command.CurrencyCode, command.BusinessDateLabel.ToString("yyyy-MM-dd"), "manual-open");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
