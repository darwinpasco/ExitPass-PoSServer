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
    {
        const string sql = """
            SELECT period.fiscal_reporting_period_id,
                   period.fiscal_reporting_contract_version_id,
                   period.business_day_date,
                   period.period_start_at,
                   period.period_end_at,
                   period.period_sequence,
                   period.expected_prior_period_id,
                   transaction_timestamp()
            FROM pos.fiscal_reporting_periods period
            WHERE period.site_pos_server_id = @site
              AND period.fiscal_identity_id = @identity
              AND period.currency_code = @currency
              AND period.period_status_code_id = '1a6f7021-bc84-5c01-afaa-c5d6685633c8'
              AND transaction_timestamp() >= period.period_start_at
              AND transaction_timestamp() < period.period_end_at
            ORDER BY period.period_sequence
            LIMIT 2
            FOR UPDATE;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("identity", fiscalIdentityId);
        command.Parameters.AddWithValue("currency", currencyCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var periods = new List<FiscalReportingPeriodAssignment>(2);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            periods.Add(new FiscalReportingPeriodAssignment(
                reader.GetGuid(0), reader.GetGuid(1), sitePosServerId, fiscalIdentityId, currencyCode,
                DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<DateTimeOffset>(4), reader.GetInt64(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return periods.Count switch
        {
            1 => periods[0],
            0 => throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable,
                "A governed OPEN fiscal reporting period is unavailable for this scope."),
            _ => throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodAmbiguous,
                "Multiple governed OPEN fiscal reporting periods match this scope.")
        };
    }

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
