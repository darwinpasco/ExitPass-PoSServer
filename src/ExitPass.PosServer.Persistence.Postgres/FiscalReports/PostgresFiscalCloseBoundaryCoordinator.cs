using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public static class PostgresFiscalCloseBoundaryCoordinator
{
    public const string LockIdentityVersion = "z-close-boundary:v1";
    private const int LockTimeoutMilliseconds = 5000;
    private const string PeriodRolloverActor = "pos-server:fiscal-period-rollover";

    public static long DeriveAdvisoryKey(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode)
    {
        var canonical = $"{LockIdentityVersion}:{sitePosServerId:D}:{fiscalIdentityId:D}:{currencyCode.ToUpperInvariant()}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return BinaryPrimitives.ReadInt64BigEndian(digest.AsSpan(0, sizeof(long)));
    }

    public static async Task AcquireAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new NpgsqlCommand(
                "SELECT set_config('lock_timeout', @lock_timeout, true); SELECT pg_advisory_xact_lock(@lock_key);",
                connection,
                transaction);
            command.Parameters.AddWithValue("lock_timeout", $"{LockTimeoutMilliseconds}ms");
            command.Parameters.AddWithValue("lock_key", DeriveAdvisoryKey(sitePosServerId, fiscalIdentityId, currencyCode));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.LockTimeout,
                "Fiscal reporting close boundary is temporarily busy.",
                ex);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DeadlockDetected)
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.RetryableConcurrencyFailure,
                "Fiscal reporting close boundary encountered a retryable concurrency failure.",
                ex);
        }
    }

    public static async Task<FiscalReportingPeriodAssignment> ResolveAndLockOpenPeriodAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken)
        => await ResolveAndLockOpenPeriodAsync(
            connection,
            transaction,
            sitePosServerId,
            fiscalIdentityId,
            currencyCode,
            businessDayDate: null,
            cancellationToken).ConfigureAwait(false);

    public static async Task<FiscalReportingPeriodAssignment> ResolveAndLockOpenPeriodAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        DateOnly? businessDayDate,
        CancellationToken cancellationToken)
    {
        var periods = await ReadMatchingPeriodsAsync(
            connection,
            transaction,
            sitePosServerId,
            fiscalIdentityId,
            currencyCode,
            businessDayDate,
            cancellationToken).ConfigureAwait(false);

        if (periods.Count == 0 && businessDayDate is null)
        {
            await EnsureCurrentOpenPeriodAsync(
                connection,
                transaction,
                sitePosServerId,
                fiscalIdentityId,
                currencyCode,
                cancellationToken).ConfigureAwait(false);
            periods = await ReadMatchingPeriodsAsync(
                connection,
                transaction,
                sitePosServerId,
                fiscalIdentityId,
                currencyCode,
                businessDayDate,
                cancellationToken).ConfigureAwait(false);
        }

        if (periods.Count == 0)
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable,
                businessDayDate is null
                    ? "A governed OPEN fiscal reporting period is unavailable for this scope."
                    : "A governed fiscal reporting period is unavailable for the immutable business date.");
        }

        if (periods.Count > 1)
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodAmbiguous,
                businessDayDate is null
                    ? "Multiple governed OPEN fiscal reporting periods match this scope."
                    : "Multiple governed fiscal reporting periods match the immutable business date.");
        }

        if (periods[0].Status != Guid.Parse("1a6f7021-bc84-5c01-afaa-c5d6685633c8"))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodClosed,
                "The canonical fiscal reporting period for the immutable business date is not OPEN.");
        }

        return periods[0].Assignment;
    }

    public static (DateOnly BusinessDayDate, DateTimeOffset PeriodStartAt, DateTimeOffset PeriodEndAt) ResolveBusinessDayWindow(
        DateTimeOffset fiscalTimestamp,
        string reportingTimezoneName,
        TimeOnly businessDayCutoffLocalTime)
    {
        if (string.IsNullOrWhiteSpace(reportingTimezoneName))
        {
            throw new ArgumentException("Reporting timezone is required.", nameof(reportingTimezoneName));
        }

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(reportingTimezoneName);
        var localTimestamp = TimeZoneInfo.ConvertTime(fiscalTimestamp, timezone);
        var localDate = DateOnly.FromDateTime(localTimestamp.DateTime);
        var businessDayDate = TimeOnly.FromDateTime(localTimestamp.DateTime) < businessDayCutoffLocalTime
            ? localDate.AddDays(-1)
            : localDate;
        var startLocal = businessDayDate.ToDateTime(businessDayCutoffLocalTime, DateTimeKind.Unspecified);
        var endLocal = businessDayDate.AddDays(1).ToDateTime(businessDayCutoffLocalTime, DateTimeKind.Unspecified);
        var startAt = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, timezone), TimeSpan.Zero);
        var endAt = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocal, timezone), TimeSpan.Zero);
        return (businessDayDate, startAt, endAt);
    }

    private static async Task<List<(FiscalReportingPeriodAssignment Assignment, Guid Status)>> ReadMatchingPeriodsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        DateOnly? businessDayDate,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT period.fiscal_reporting_period_id,
                   period.fiscal_reporting_contract_version_id,
                   period.business_day_date,
                   period.period_start_at,
                   period.period_end_at,
                   period.period_sequence,
                   period.expected_prior_period_id,
                   transaction_timestamp(),
                   period.period_status_code_id
            FROM pos.fiscal_reporting_periods period
            WHERE period.site_pos_server_id = @site
              AND period.fiscal_identity_id = @identity
              AND period.currency_code = @currency
              AND (
                    (@business_day_date IS NULL
                     AND period.period_status_code_id = '1a6f7021-bc84-5c01-afaa-c5d6685633c8'
                     AND transaction_timestamp() >= period.period_start_at
                     AND transaction_timestamp() < period.period_end_at)
                    OR
                    (@business_day_date IS NOT NULL
                     AND period.business_day_date = @business_day_date)
                  )
            ORDER BY period.period_sequence
            LIMIT 2
            FOR UPDATE;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("identity", fiscalIdentityId);
        command.Parameters.AddWithValue("currency", currencyCode);
        command.Parameters.Add(new NpgsqlParameter("business_day_date", NpgsqlTypes.NpgsqlDbType.Date)
        {
            Value = businessDayDate is null ? DBNull.Value : businessDayDate.Value
        });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var periods = new List<(FiscalReportingPeriodAssignment Assignment, Guid Status)>(2);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            periods.Add((new FiscalReportingPeriodAssignment(
                reader.GetGuid(0), reader.GetGuid(1), sitePosServerId, fiscalIdentityId, currencyCode,
                DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<DateTimeOffset>(4), reader.GetInt64(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.GetFieldValue<DateTimeOffset>(7)),
                reader.GetGuid(8)));
        }
        return periods;
    }

    private static async Task EnsureCurrentOpenPeriodAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        const string configurationSql = """
            SELECT site.reporting_timezone_name,
                   site.business_day_cutoff_local_time,
                   transaction_timestamp(),
                   (
                       SELECT contract.fiscal_reporting_contract_version_id
                       FROM pos.fiscal_reporting_contract_versions contract
                       WHERE contract.contract_key = 'pos-server-fiscal-reporting'
                         AND contract.contract_version = 'v1'
                         AND contract.is_active
                         AND contract.effective_from <= transaction_timestamp()
                         AND (contract.effective_to IS NULL OR contract.effective_to > transaction_timestamp())
                   )
            FROM pos.site_pos_servers site
            WHERE site.site_pos_server_id = @site
              AND site.is_active
            FOR UPDATE OF site;
            """;
        await using var configurationCommand = new NpgsqlCommand(configurationSql, connection, transaction);
        configurationCommand.Parameters.AddWithValue("site", sitePosServerId);
        await using var reader = await configurationCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ||
            reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(3))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable,
                "Site POS Server reporting timezone, cutoff, and reporting contract are required for period rollover.");
        }

        var reportingTimezoneName = reader.GetString(0);
        var businessDayCutoffLocalTime = reader.GetFieldValue<TimeOnly>(1);
        var fiscalTimestamp = reader.GetFieldValue<DateTimeOffset>(2);
        var contractVersionId = reader.GetGuid(3);
        await reader.CloseAsync().ConfigureAwait(false);

        (DateOnly BusinessDayDate, DateTimeOffset PeriodStartAt, DateTimeOffset PeriodEndAt) window;
        try
        {
            window = ResolveBusinessDayWindow(fiscalTimestamp, reportingTimezoneName, businessDayCutoffLocalTime);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw InvalidReportingTimezone(exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw InvalidReportingTimezone(exception);
        }

        const string insertSql = """
            INSERT INTO pos.fiscal_reporting_periods(
                fiscal_reporting_period_id, fiscal_reporting_contract_version_id,
                site_pos_server_id, fiscal_identity_id, period_status_code_id, business_day_date,
                period_start_at, period_end_at, reporting_timezone_name,
                business_day_cutoff_local_time, currency_code, period_sequence,
                expected_prior_period_id, opened_at, created_by_ref, updated_by_ref)
            SELECT @period, @contract, @site, @identity,
                   '1a6f7021-bc84-5c01-afaa-c5d6685633c8', @business_day_date,
                   @period_start_at, @period_end_at, @timezone, @cutoff, @currency,
                   COALESCE((SELECT max(period_sequence) + 1
                             FROM pos.fiscal_reporting_periods
                             WHERE site_pos_server_id = @site), 1),
                   (SELECT fiscal_reporting_period_id
                    FROM pos.fiscal_reporting_periods
                    WHERE site_pos_server_id = @site
                      AND fiscal_identity_id = @identity
                      AND currency_code = @currency
                      AND period_end_at <= @period_start_at
                    ORDER BY period_end_at DESC, period_sequence DESC
                    LIMIT 1),
                   @opened_at, @actor, @actor
            WHERE NOT EXISTS (
                SELECT 1
                FROM pos.fiscal_reporting_periods
                WHERE site_pos_server_id = @site
                  AND fiscal_identity_id = @identity
                  AND currency_code = @currency
                  AND tstzrange(period_start_at, period_end_at, '[)') &&
                      tstzrange(@period_start_at, @period_end_at, '[)'))
            ON CONFLICT (site_pos_server_id, fiscal_identity_id, period_start_at, period_end_at, currency_code)
            DO NOTHING;
            """;
        await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.AddWithValue("period", Guid.NewGuid());
        insertCommand.Parameters.AddWithValue("contract", contractVersionId);
        insertCommand.Parameters.AddWithValue("site", sitePosServerId);
        insertCommand.Parameters.AddWithValue("identity", fiscalIdentityId);
        insertCommand.Parameters.AddWithValue("business_day_date", window.BusinessDayDate);
        insertCommand.Parameters.AddWithValue("period_start_at", window.PeriodStartAt);
        insertCommand.Parameters.AddWithValue("period_end_at", window.PeriodEndAt);
        insertCommand.Parameters.AddWithValue("timezone", reportingTimezoneName);
        insertCommand.Parameters.AddWithValue("cutoff", businessDayCutoffLocalTime);
        insertCommand.Parameters.AddWithValue("currency", currencyCode);
        insertCommand.Parameters.AddWithValue("opened_at", fiscalTimestamp);
        insertCommand.Parameters.AddWithValue("actor", PeriodRolloverActor);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static FiscalCloseBoundaryException InvalidReportingTimezone(Exception innerException) =>
        new(
            FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable,
            "Site POS Server reporting timezone is invalid for period rollover.",
            innerException);

    public static async Task LockAndValidateAssignedPeriodAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid reportingPeriodId,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT period_status_code_id, site_pos_server_id, fiscal_identity_id, currency_code
            FROM pos.fiscal_reporting_periods
            WHERE fiscal_reporting_period_id = @period
            FOR UPDATE;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("period", reportingPeriodId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable,
                "The assigned fiscal reporting period is unavailable.");
        }

        var status = reader.GetGuid(0);
        var scopeMatches = reader.GetGuid(1) == sitePosServerId &&
            reader.GetGuid(2) == fiscalIdentityId &&
            string.Equals(reader.GetString(3), currencyCode, StringComparison.Ordinal);
        if (!scopeMatches)
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodAssignmentMismatch,
                "Fiscal document reporting-period scope is inconsistent.");
        }
        if (status != Guid.Parse("1a6f7021-bc84-5c01-afaa-c5d6685633c8"))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.UnsupportedCrossPeriodMutation,
                "Fiscal mutation against a non-OPEN reporting period is unsupported.");
        }
    }
}
