using System.Data;
using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresFiscalZCounterResetRepository(NpgsqlDataSource dataSource) : IFiscalZCounterResetRepository
{
    private static readonly Guid ResetTransitionTypeId = Guid.Parse("5f17db5b-119f-5c18-8d3d-91153aed17b2");
    private static readonly Guid ZReadingKindId = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");
    private static readonly Guid ZCounterIdentityId = Guid.Parse("baf7bb67-9f11-5b2f-a750-e5689a5f0142");
    private static readonly Guid ResetCounterIdentityId = Guid.Parse("a1dddcd8-aab5-51a8-bb64-f610edbdc1eb");
    private static readonly Guid GtaIdentityId = Guid.Parse("e01e75a6-6ee9-5bdf-9bf4-0a38476f8805");

    public async Task<FiscalZCounterResetResult> ReadStateAsync(
        FiscalZCounterStateQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var state = await ReadStateAsync(connection, null, query, cancellationToken).ConfigureAwait(false);
            return state is null
                ? new(FiscalZCounterResetOutcome.NotFound, SafeMessage: "The fiscal Z Counter state is unavailable for this scope.")
                : new(FiscalZCounterResetOutcome.Replayed, State: state);
        }
        catch (NpgsqlException)
        {
            return Failure(FiscalZCounterResetOutcome.PersistenceFailure, "The fiscal Z Counter state could not be read safely.");
        }
    }

    public async Task<FiscalZCounterResetResult> ResetAsync(
        FiscalZCounterResetCommand command,
        CancellationToken cancellationToken = default)
    {
        var hash = FiscalZCounterResetSemanticHasher.Compute(command);
        var commitAttempted = false;
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(
                connection, transaction, command.SitePosServerId, command.FiscalIdentityId,
                command.CurrencyCode, cancellationToken).ConfigureAwait(false);

            var existing = await ReadResetByOperationAsync(
                connection, transaction, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return existing.Value.TransitionTypeId != ResetTransitionTypeId ||
                       !string.Equals(existing.Value.SemanticHash, hash, StringComparison.Ordinal)
                    ? Failure(FiscalZCounterResetOutcome.Conflict, "The operation identifier is already bound to different fiscal semantics.")
                    : new(FiscalZCounterResetOutcome.Replayed, existing.Value.Record);
            }

            var state = await PostgresFiscalZCloseTransitionFoundation.ReadStateForUpdateAsync(
                connection, transaction, command.SitePosServerId, command.FiscalIdentityId,
                command.CurrencyCode, cancellationToken).ConfigureAwait(false);
            if (state.StateVersion != command.ExpectedStateVersion)
            {
                return Failure(FiscalZCounterResetOutcome.StaleStateVersion,
                    "The expected fiscal Z Counter state version is stale.");
            }

            var periodId = await ReadJournalPeriodIdAsync(connection, transaction, state, cancellationToken).ConfigureAwait(false);
            if (!periodId.HasValue)
            {
                return Failure(FiscalZCounterResetOutcome.NotFound,
                    "A fiscal reporting period is required to record the manual Z Counter reset in the Electronic Journal.");
            }

            var resetAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var transitionId = Guid.NewGuid();
            var resultingReset = checked(state.ResetCounterValue + 1);
            var resultingVersion = checked(state.StateVersion + 1);

            await InsertTransitionAsync(connection, transaction, command, state, transitionId, hash,
                resultingReset, resultingVersion, resetAt, cancellationToken).ConfigureAwait(false);
            await InsertValuesAsync(connection, transaction, state, transitionId, resultingReset, cancellationToken).ConfigureAwait(false);
            var applied = await PostgresFiscalZCloseTransitionFoundation.ApplyPreparedTransitionAsync(
                connection, transaction, transitionId, state.StateVersion, cancellationToken).ConfigureAwait(false);
            if (applied != resultingVersion)
            {
                return Failure(FiscalZCounterResetOutcome.StaleStateVersion,
                    "The fiscal Z Counter reset did not advance the expected state version.");
            }

            var sourceReference = $"fiscal-z-counter-reset:{transitionId:D}";
            var eventReference = await PostgresElectronicJournalWriter.AppendAsync(
                connection,
                transaction,
                new ElectronicJournalAppendRequest(
                    command.SitePosServerId,
                    command.FiscalIdentityId,
                    command.CurrencyCode,
                    periodId.Value,
                    "z_counter_reset_committed",
                    sourceReference,
                    FiscalZCounterResetContract.TransitionVersion,
                    resetAt,
                    command.RequestedByRef,
                    command.ServiceIdentityRef,
                    command.CorrelationId,
                    ResetFacts(state, resultingReset, resultingVersion, command.Reason),
                    BusinessDayDate: await ReadBusinessDayDateAsync(connection, transaction, periodId.Value, cancellationToken).ConfigureAwait(false),
                    IdempotencyReference: command.OperationKey),
                cancellationToken).ConfigureAwait(false);
            await InsertAuditAsync(connection, transaction, command, resetAt, cancellationToken).ConfigureAwait(false);

            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            var record = await ReadResetByOperationAsync(
                connection, null, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            return record is null
                ? Failure(FiscalZCounterResetOutcome.UnknownCommitOutcome, "The committed Z Counter reset requires durable-state reconciliation.")
                : new(FiscalZCounterResetOutcome.Created, record.Value.Record with { ElectronicJournalEventReference = eventReference });
        }
        catch (FiscalCloseBoundaryException exception)
        {
            return Failure(MapBoundary(exception.ErrorCode), exception.Message);
        }
        catch (OverflowException)
        {
            return Failure(FiscalZCounterResetOutcome.ArithmeticOverflow, "The Reset Counter exceeded supported bounds.");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return await ReconcileAsync(command, hash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure)
        {
            return Failure(FiscalZCounterResetOutcome.ConcurrentFiscalClose,
                "A Z Reading close or Z Counter reset is already processing for this fiscal scope.");
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            return await ReconcileAsync(command, hash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<FiscalZCounterResetResult> ReconcileAsync(
        FiscalZCounterResetCommand command,
        string hash,
        bool commitAttempted,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var existing = await ReadResetByOperationAsync(connection, null, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return Failure(commitAttempted ? FiscalZCounterResetOutcome.UnknownCommitOutcome : FiscalZCounterResetOutcome.PersistenceFailure,
                    commitAttempted ? "The Z Counter reset commit outcome requires durable-state reconciliation." : "The Z Counter reset failed safely and was rolled back.");
            return existing.Value.TransitionTypeId == ResetTransitionTypeId &&
                   string.Equals(existing.Value.SemanticHash, hash, StringComparison.Ordinal)
                ? new(FiscalZCounterResetOutcome.Replayed, existing.Value.Record)
                : Failure(FiscalZCounterResetOutcome.Conflict, "The operation identifier is already bound to different fiscal semantics.");
        }
        catch (NpgsqlException)
        {
            return Failure(FiscalZCounterResetOutcome.UnknownCommitOutcome,
                "The Z Counter reset outcome requires durable-state reconciliation.");
        }
    }

    private static async Task InsertTransitionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalZCounterResetCommand command,
        LockedFiscalZCloseState state,
        Guid transitionId,
        string semanticHash,
        long resultingReset,
        long resultingVersion,
        DateTimeOffset resetAt,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO pos.fiscal_z_close_state_transitions(
                fiscal_z_close_state_transition_id,fiscal_z_close_state_id,fiscal_reporting_contract_version_id,
                site_pos_server_id,fiscal_identity_id,currency_code,transition_type_code_id,operation_ref,
                semantic_request_hash,semantic_hash_version,expected_state_version,resulting_state_version,
                previous_reporting_period_id,resulting_reporting_period_id,previous_z_report_id,
                previous_z_report_kind_code_id,resulting_z_report_id,resulting_z_report_kind_code_id,
                approval_ref,actor_ref,service_identity_ref,correlation_ref,reason_text,committed_at,created_at)
            VALUES(@id,@state,@contract,@site,@identity,@currency,@type,@operation,@hash,@hash_version,
                @expected_version,@resulting_version,@previous_period,@resulting_period,@previous_z,@previous_kind,
                @resulting_z,@resulting_kind,@approval,@actor,@service,@correlation,@reason,@at,@at);
            """;
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("id", transitionId);
        cmd.Parameters.AddWithValue("state", state.FiscalZCloseStateId);
        cmd.Parameters.AddWithValue("contract", state.FiscalReportingContractVersionId);
        cmd.Parameters.AddWithValue("site", command.SitePosServerId);
        cmd.Parameters.AddWithValue("identity", command.FiscalIdentityId);
        cmd.Parameters.AddWithValue("currency", command.CurrencyCode);
        cmd.Parameters.AddWithValue("type", ResetTransitionTypeId);
        cmd.Parameters.AddWithValue("operation", command.OperationKey);
        cmd.Parameters.AddWithValue("hash", semanticHash);
        cmd.Parameters.AddWithValue("hash_version", FiscalZCounterResetContract.SemanticHashVersion);
        cmd.Parameters.AddWithValue("expected_version", state.StateVersion);
        cmd.Parameters.AddWithValue("resulting_version", resultingVersion);
        AddNullable(cmd, "previous_period", state.LastClosedReportingPeriodId);
        AddNullable(cmd, "resulting_period", state.LastClosedReportingPeriodId);
        AddNullable(cmd, "previous_z", state.LastCommittedZReportId);
        AddNullable(cmd, "previous_kind", state.LastCommittedZReportId.HasValue ? ZReadingKindId : null);
        AddNullable(cmd, "resulting_z", state.LastCommittedZReportId);
        AddNullable(cmd, "resulting_kind", state.LastCommittedZReportId.HasValue ? ZReadingKindId : null);
        cmd.Parameters.AddWithValue("approval", "not-required:explicit-manual-reset");
        cmd.Parameters.AddWithValue("actor", command.RequestedByRef);
        cmd.Parameters.AddWithValue("service", command.ServiceIdentityRef);
        cmd.Parameters.AddWithValue("correlation", command.CorrelationId);
        cmd.Parameters.AddWithValue("reason", command.Reason);
        cmd.Parameters.AddWithValue("at", resetAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertValuesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        LockedFiscalZCloseState state,
        Guid transitionId,
        long resultingReset,
        CancellationToken cancellationToken)
    {
        await InsertValueAsync(connection, transaction, transitionId, ZCounterIdentityId,
            state.ZCounterValue, 0, null, null, null, cancellationToken).ConfigureAwait(false);
        await InsertValueAsync(connection, transaction, transitionId, ResetCounterIdentityId,
            state.ResetCounterValue, resultingReset, null, null, null, cancellationToken).ConfigureAwait(false);
        await InsertValueAsync(connection, transaction, transitionId, GtaIdentityId,
            null, null, state.GrandTotalAmountMinorUnits, state.GrandTotalAmountMinorUnits,
            state.CurrencyCode, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertValueAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid transitionId, Guid identityId,
        long? previousCounter, long? resultingCounter, long? previousAmount, long? resultingAmount,
        string? currency, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO pos.fiscal_z_close_state_transition_values(
                fiscal_z_close_state_transition_value_id,fiscal_z_close_state_transition_id,state_identity_code_id,
                previous_counter_value,resulting_counter_value,previous_amount_minor_units,resulting_amount_minor_units,
                currency_code,created_at)
            VALUES(@id,@transition,@identity,@previous_counter,@resulting_counter,@previous_amount,@resulting_amount,@currency,clock_timestamp());
            """;
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("transition", transitionId);
        cmd.Parameters.AddWithValue("identity", identityId);
        AddNullable(cmd, "previous_counter", previousCounter, NpgsqlDbType.Bigint);
        AddNullable(cmd, "resulting_counter", resultingCounter, NpgsqlDbType.Bigint);
        AddNullable(cmd, "previous_amount", previousAmount, NpgsqlDbType.Bigint);
        AddNullable(cmd, "resulting_amount", resultingAmount, NpgsqlDbType.Bigint);
        AddNullable(cmd, "currency", currency, NpgsqlDbType.Char);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyDictionary<string, string?> ResetFacts(
        LockedFiscalZCloseState state, long resultingReset, long resultingVersion, string reason) =>
        new SortedDictionary<string, string?>(StringComparer.Ordinal)
        {
            ["previous_z_counter"] = state.ZCounterValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["resulting_z_counter"] = "0",
            ["previous_reset_counter"] = state.ResetCounterValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["resulting_reset_counter"] = resultingReset.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["previous_gta_minor_units"] = state.GrandTotalAmountMinorUnits.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["resulting_gta_minor_units"] = state.GrandTotalAmountMinorUnits.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["resulting_state_version"] = resultingVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reason"] = reason
        };

    private static async Task InsertAuditAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalZCounterResetCommand command,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO pos.fiscal_action_audit(
                fiscal_action_audit_id,site_pos_server_id,audit_action_type_code_id,audit_result_code_id,
                actor_ref,service_identity_ref,correlation_ref,occurred_at,created_at)
            SELECT @id,@site,@action,result.controlled_code_id,@actor,@service,@correlation,@at,@at
            FROM pos.controlled_codes result
            JOIN pos.controlled_code_sets family ON family.controlled_code_set_id=result.controlled_code_set_id
            WHERE family.code_set_key='fiscal_action_audit_result' AND result.code_key='success';
            """;
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("site", command.SitePosServerId);
        cmd.Parameters.AddWithValue("action", ResetTransitionTypeId);
        cmd.Parameters.AddWithValue("actor", command.RequestedByRef);
        cmd.Parameters.AddWithValue("service", command.ServiceIdentityRef);
        cmd.Parameters.AddWithValue("correlation", command.CorrelationId);
        cmd.Parameters.AddWithValue("at", at);
        if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            throw new InvalidOperationException("Fiscal reset audit classification is unavailable.");
    }

    private static async Task<Guid?> ReadJournalPeriodIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        LockedFiscalZCloseState state,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT fiscal_reporting_period_id
            FROM pos.fiscal_reporting_periods
            WHERE site_pos_server_id=@site AND fiscal_identity_id=@identity AND currency_code=@currency
            ORDER BY (period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8') DESC,
                     period_sequence DESC
            LIMIT 1;
            """;
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("site", state.SitePosServerId);
        cmd.Parameters.AddWithValue("identity", state.FiscalIdentityId);
        cmd.Parameters.AddWithValue("currency", state.CurrencyCode);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is Guid value ? value : null;
    }

    private static async Task<DateOnly?> ReadBusinessDayDateAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid periodId, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT business_day_date FROM pos.fiscal_reporting_periods WHERE fiscal_reporting_period_id=@period",
            connection, transaction);
        cmd.Parameters.AddWithValue("period", periodId);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is DateTime value
            ? DateOnly.FromDateTime(value)
            : null;
    }

    private static async Task<(Guid TransitionTypeId, string SemanticHash, FiscalZCounterResetRecord Record)?> ReadResetByOperationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid sitePosServerId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT transition.fiscal_z_close_state_transition_id,transition.transition_type_code_id,
                   transition.semantic_request_hash,transition.operation_ref,transition.site_pos_server_id,
                   transition.fiscal_identity_id,transition.currency_code,
                   z.previous_counter_value,z.resulting_counter_value,
                   reset.previous_counter_value,reset.resulting_counter_value,
                   gta.previous_amount_minor_units,gta.resulting_amount_minor_units,
                   transition.expected_state_version,transition.resulting_state_version,
                   transition.actor_ref,transition.service_identity_ref,transition.reason_text,
                   transition.committed_at,COALESCE(journal.event_reference,'unavailable')
            FROM pos.fiscal_z_close_state_transitions transition
            LEFT JOIN pos.fiscal_z_close_state_transition_values z
              ON z.fiscal_z_close_state_transition_id=transition.fiscal_z_close_state_transition_id
             AND z.state_identity_code_id=@z_identity
            LEFT JOIN pos.fiscal_z_close_state_transition_values reset
              ON reset.fiscal_z_close_state_transition_id=transition.fiscal_z_close_state_transition_id
             AND reset.state_identity_code_id=@reset_identity
            LEFT JOIN pos.fiscal_z_close_state_transition_values gta
              ON gta.fiscal_z_close_state_transition_id=transition.fiscal_z_close_state_transition_id
             AND gta.state_identity_code_id=@gta_identity
            LEFT JOIN pos.electronic_journal_records journal
              ON journal.source_transition_ref='fiscal-z-counter-reset:'||transition.fiscal_z_close_state_transition_id::text
             AND journal.is_canonical
            WHERE transition.site_pos_server_id=@site AND transition.operation_ref=@operation;
            """;
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("site", sitePosServerId);
        cmd.Parameters.AddWithValue("operation", operationKey);
        cmd.Parameters.AddWithValue("z_identity", ZCounterIdentityId);
        cmd.Parameters.AddWithValue("reset_identity", ResetCounterIdentityId);
        cmd.Parameters.AddWithValue("gta_identity", GtaIdentityId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        var transitionType = reader.GetGuid(1);
        if (transitionType != ResetTransitionTypeId)
            return (transitionType, reader.GetString(2), null!);
        return (transitionType, reader.GetString(2), new FiscalZCounterResetRecord(
            reader.GetGuid(0), reader.GetString(3), reader.GetGuid(4), reader.GetGuid(5), reader.GetString(6),
            reader.GetInt64(7), reader.GetInt64(8), reader.GetInt64(9), reader.GetInt64(10),
            reader.GetInt64(11), reader.GetInt64(12), reader.GetInt64(13), reader.GetInt64(14),
            reader.GetString(15), reader.GetString(16), reader.GetString(17),
            reader.GetFieldValue<DateTimeOffset>(18), reader.GetString(19)));
    }

    private static async Task<FiscalZCounterStateRecord?> ReadStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        FiscalZCounterStateQuery query,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT state.fiscal_z_close_state_id,state.site_pos_server_id,state.fiscal_identity_id,state.currency_code,
                   state.z_counter_value,state.reset_counter_value,state.grand_total_amount_minor_units,state.state_version,
                   report.report_number,report.business_day_date,report.committed_at
            FROM pos.fiscal_z_close_states state
            LEFT JOIN pos.x_z_reports report ON report.x_z_report_id=state.last_committed_z_report_id
            WHERE state.site_pos_server_id=@site AND state.fiscal_identity_id=@identity AND state.currency_code=@currency;
            """;
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("site", query.SitePosServerId);
        cmd.Parameters.AddWithValue("identity", query.FiscalIdentityId);
        cmd.Parameters.AddWithValue("currency", query.CurrencyCode);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        var state = new FiscalZCounterStateRecord(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3),
            reader.GetInt64(4), reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : DateOnly.FromDateTime(reader.GetDateTime(9)),
            reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10), null);
        await reader.CloseAsync().ConfigureAwait(false);
        var last = await ReadLastResetAsync(connection, transaction, query, cancellationToken).ConfigureAwait(false);
        return state with { LastReset = last };
    }

    private static async Task<FiscalZCounterResetRecord?> ReadLastResetAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        FiscalZCounterStateQuery query,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT operation_ref FROM pos.fiscal_z_close_state_transitions
            WHERE site_pos_server_id=@site AND fiscal_identity_id=@identity AND currency_code=@currency
              AND transition_type_code_id=@type
            ORDER BY committed_at DESC,fiscal_z_close_state_transition_id DESC LIMIT 1;
            """;
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("site", query.SitePosServerId);
        cmd.Parameters.AddWithValue("identity", query.FiscalIdentityId);
        cmd.Parameters.AddWithValue("currency", query.CurrencyCode);
        cmd.Parameters.AddWithValue("type", ResetTransitionTypeId);
        var operation = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        if (operation is null) return null;
        return (await ReadResetByOperationAsync(connection, transaction, query.SitePosServerId, operation, cancellationToken).ConfigureAwait(false))?.Record;
    }

    private static async Task<DateTimeOffset> ClockAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("SELECT clock_timestamp()", connection, transaction);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) switch
        {
            DateTimeOffset value => value,
            DateTime value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("PostgreSQL clock was unavailable.")
        };
    }

    private static void AddNullable(NpgsqlCommand command, string name, Guid? value) =>
        AddNullable(command, name, value, NpgsqlDbType.Uuid);

    private static void AddNullable(NpgsqlCommand command, string name, object? value, NpgsqlDbType type) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });

    private static FiscalZCounterResetOutcome MapBoundary(FiscalCloseBoundaryErrorCode code) => code switch
    {
        FiscalCloseBoundaryErrorCode.StateInitializationRequired => FiscalZCounterResetOutcome.StateInitializationRequired,
        FiscalCloseBoundaryErrorCode.StaleStateVersion => FiscalZCounterResetOutcome.StaleStateVersion,
        FiscalCloseBoundaryErrorCode.LockTimeout or FiscalCloseBoundaryErrorCode.RetryableConcurrencyFailure => FiscalZCounterResetOutcome.ConcurrentFiscalClose,
        _ => FiscalZCounterResetOutcome.PersistenceFailure
    };

    private static FiscalZCounterResetResult Failure(FiscalZCounterResetOutcome outcome, string message) =>
        new(outcome, SafeMessage: message);
}
