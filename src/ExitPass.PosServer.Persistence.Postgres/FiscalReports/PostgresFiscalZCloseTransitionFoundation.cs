using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed record LockedFiscalZCloseState(
    Guid FiscalZCloseStateId,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    long ResetCounterValue,
    long ZCounterValue,
    long GrandTotalAmountMinorUnits,
    long StateVersion,
    Guid? LastClosedReportingPeriodId,
    Guid? LastCommittedZReportId,
    string LastTransitionOperationReference);

public sealed record FiscalZClosePeriodSequenceGuard(
    Guid ReportingPeriodId,
    long PeriodSequence,
    Guid? ExpectedPriorPeriodId,
    Guid? ImmediatePriorPeriodId,
    Guid? ImmediatePriorZReportId,
    bool IsFirstPeriod,
    bool PriorPeriodIsClosedWithZ);

public static class PostgresFiscalZCloseTransitionFoundation
{
    private static readonly Guid ClosedPeriodStatusId = Guid.Parse("af7ee931-a023-507e-81a4-17adf047eb94");
    private static readonly Guid ZReadingKindId = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");

    public static async Task<LockedFiscalZCloseState> ReadStateForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT fiscal_z_close_state_id, site_pos_server_id, fiscal_identity_id, currency_code,
                   reset_counter_value, z_counter_value, grand_total_amount_minor_units,
                   state_version, last_closed_reporting_period_id, last_committed_z_report_id,
                   last_transition_operation_ref
            FROM pos.fiscal_z_close_states
            WHERE site_pos_server_id = @site
              AND fiscal_identity_id = @identity
              AND currency_code = @currency
            FOR UPDATE;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("identity", fiscalIdentityId);
        command.Parameters.AddWithValue("currency", currencyCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.StateInitializationRequired,
                "Canonical fiscal Z close state must be explicitly initialized for this scope.");
        }

        return new LockedFiscalZCloseState(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3),
            reader.GetInt64(4), reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7),
            reader.IsDBNull(8) ? null : reader.GetGuid(8),
            reader.IsDBNull(9) ? null : reader.GetGuid(9), reader.GetString(10));
    }

    public static async Task<FiscalZClosePeriodSequenceGuard> ReadPeriodSequenceGuardAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid reportingPeriodId,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH target AS (
                SELECT fiscal_reporting_period_id, period_sequence, expected_prior_period_id
                FROM pos.fiscal_reporting_periods
                WHERE fiscal_reporting_period_id = @period
                  AND site_pos_server_id = @site
                  AND fiscal_identity_id = @identity
                  AND currency_code = @currency
            ), prior AS (
                SELECT p.fiscal_reporting_period_id, p.period_status_code_id,
                       z.x_z_report_id AS z_report_id
                FROM target t
                LEFT JOIN pos.fiscal_reporting_periods p
                  ON p.site_pos_server_id = @site
                 AND p.fiscal_identity_id = @identity
                 AND p.currency_code = @currency
                 AND p.period_sequence = t.period_sequence - 1
                LEFT JOIN pos.x_z_reports z
                  ON z.fiscal_reporting_period_id = p.fiscal_reporting_period_id
                 AND z.report_kind_code_id = @z_kind
            )
            SELECT t.fiscal_reporting_period_id, t.period_sequence, t.expected_prior_period_id,
                   p.fiscal_reporting_period_id, p.z_report_id, p.period_status_code_id
            FROM target t CROSS JOIN prior p;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("period", reportingPeriodId);
        command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("identity", fiscalIdentityId);
        command.Parameters.AddWithValue("currency", currencyCode);
        command.Parameters.AddWithValue("z_kind", ZReadingKindId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable,
                "The governed fiscal reporting period is unavailable for this scope.");
        }

        var sequence = reader.GetInt64(1);
        Guid? expectedPrior = reader.IsDBNull(2) ? null : reader.GetGuid(2);
        Guid? immediatePrior = reader.IsDBNull(3) ? null : reader.GetGuid(3);
        Guid? priorZ = reader.IsDBNull(4) ? null : reader.GetGuid(4);
        var isFirst = sequence == 1;
        var priorClosedWithZ = !reader.IsDBNull(5) && reader.GetGuid(5) == ClosedPeriodStatusId && priorZ.HasValue;

        if (isFirst && (expectedPrior.HasValue || immediatePrior.HasValue))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.PriorPeriodSequenceInvalid,
                "First fiscal reporting period has inconsistent predecessor state.");
        }
        if (!isFirst && (!immediatePrior.HasValue || expectedPrior != immediatePrior || !priorClosedWithZ))
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.PriorPeriodSequenceInvalid,
                "Immediate prior fiscal reporting period is not durably closed with a Z Reading.");
        }

        return new FiscalZClosePeriodSequenceGuard(
            reader.GetGuid(0), sequence, expectedPrior, immediatePrior, priorZ, isFirst, priorClosedWithZ);
    }

    public static async Task<long> ApplyPreparedTransitionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid transitionId,
        long expectedStateVersion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT pos.apply_fiscal_z_close_state_transition(@transition)
            WHERE EXISTS (
                SELECT 1
                FROM pos.fiscal_z_close_state_transitions transition
                JOIN pos.fiscal_z_close_states state
                  ON state.fiscal_z_close_state_id = transition.fiscal_z_close_state_id
                WHERE transition.fiscal_z_close_state_transition_id = @transition
                  AND transition.expected_state_version = @expected_version
                  AND state.state_version = @expected_version
            );
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("transition", transitionId);
        command.Parameters.AddWithValue("expected_version", expectedStateVersion);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result is not long resultingVersion)
        {
            throw new FiscalCloseBoundaryException(
                FiscalCloseBoundaryErrorCode.StaleStateVersion,
                "Fiscal Z close state version is stale.");
        }
        return resultingVersion;
    }
}
