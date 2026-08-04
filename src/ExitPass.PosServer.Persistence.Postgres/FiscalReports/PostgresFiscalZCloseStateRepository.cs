using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresFiscalZCloseStateRepository : IFiscalZCloseStateRepository
{
    private static readonly Guid ContractVersionId = Guid.Parse("f6766f48-62f0-513f-b9eb-e61c2f3e8c66");
    private static readonly Guid InitializationTypeId = Guid.Parse("21880ca5-b803-5abc-959f-def2f232ad73");
    private static readonly Guid ApprovedZeroProvenanceId = Guid.Parse("8f31c890-2aa2-50ef-a815-0e0c8cf90983");
    private static readonly Guid LegacyImportProvenanceId = Guid.Parse("9ce30367-0ff0-5c5e-8d48-b4a3b3b9df71");
    private static readonly Guid ZCounterIdentityId = Guid.Parse("baf7bb67-9f11-5b2f-a750-e5689a5f0142");
    private static readonly Guid ResetCounterIdentityId = Guid.Parse("a1dddcd8-aab5-51a8-bb64-f610edbdc1eb");
    private static readonly Guid GtaIdentityId = Guid.Parse("e01e75a6-6ee9-5bdf-9bf4-0a38476f8805");

    private readonly NpgsqlDataSource dataSource;

    public PostgresFiscalZCloseStateRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<FiscalZCloseStateInitializationResult> InitializeAsync(
        InitializeFiscalZCloseStateCommand command,
        string semanticRequestHash,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
        try
        {
            await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(
                connection, transaction, command.SitePosServerId, command.FiscalIdentityId, command.CurrencyCode, cancellationToken).ConfigureAwait(false);

            var replay = await ReadByOperationAsync(connection, transaction, command.OperationReference, cancellationToken).ConfigureAwait(false);
            if (replay is not null)
            {
                if (!string.Equals(replay.Value.SemanticHash, semanticRequestHash, StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return FiscalZCloseStateInitializationResult.Failure(
                        FiscalZCloseStateInitializationErrorCode.SemanticConflict,
                        "Fiscal Z close state operation identity has different semantics.");
                }
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return FiscalZCloseStateInitializationResult.Replayed(replay.Value.State);
            }

            var validationFailure = await ValidateScopeAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            if (validationFailure is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return validationFailure;
            }

            if (await StateExistsAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return FiscalZCloseStateInitializationResult.Failure(
                    FiscalZCloseStateInitializationErrorCode.StateAlreadyInitialized,
                    "Fiscal Z close state is already initialized for this scope.");
            }

            var stateId = Guid.NewGuid();
            var transitionId = Guid.NewGuid();
            var committedAt = DateTimeOffset.UtcNow;
            var provenanceId = command.Provenance == FiscalZCloseStateContract.ApprovedNewScopeZero
                ? ApprovedZeroProvenanceId
                : LegacyImportProvenanceId;

            await InsertStateAsync(connection, transaction, command, stateId, provenanceId, committedAt, cancellationToken).ConfigureAwait(false);
            await InsertTransitionAsync(connection, transaction, command, semanticRequestHash, stateId, transitionId, provenanceId, committedAt, cancellationToken).ConfigureAwait(false);
            await InsertTransitionValuesAsync(connection, transaction, command, transitionId, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return FiscalZCloseStateInitializationResult.Initialized(ToRecord(command, stateId, committedAt));
        }
        catch (FiscalCloseBoundaryException ex) when (ex.ErrorCode == FiscalCloseBoundaryErrorCode.LockTimeout)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            return FiscalZCloseStateInitializationResult.Failure(
                FiscalZCloseStateInitializationErrorCode.LockTimeout,
                "Fiscal Z close state scope is temporarily busy.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            return FiscalZCloseStateInitializationResult.Failure(
                FiscalZCloseStateInitializationErrorCode.StateAlreadyInitialized,
                "Fiscal Z close state initialization conflicted with a concurrent operation.");
        }
        catch (NpgsqlException)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            return FiscalZCloseStateInitializationResult.Failure(
                FiscalZCloseStateInitializationErrorCode.TransientPersistenceFailure,
                "Fiscal Z close state initialization could not be committed.");
        }
    }

    private static async Task<(string SemanticHash, FiscalZCloseStateRecord State)?> ReadByOperationAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string operationReference, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT transition.semantic_request_hash,
                   state.fiscal_z_close_state_id, state.site_pos_server_id, state.fiscal_identity_id,
                   state.currency_code, state.reset_counter_value, state.z_counter_value,
                   state.grand_total_amount_minor_units, state.state_version,
                   state.last_closed_reporting_period_id, state.last_committed_z_report_id,
                   provenance.code_key, state.initialized_at, state.initialization_approval_ref,
                   state.last_transition_operation_ref, state.last_transition_at
            FROM pos.fiscal_z_close_state_transitions transition
            JOIN pos.fiscal_z_close_states state ON state.fiscal_z_close_state_id = transition.fiscal_z_close_state_id
            JOIN pos.controlled_codes provenance ON provenance.controlled_code_id = state.initialization_provenance_code_id
            WHERE transition.operation_ref = @operation;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("operation", operationReference);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        var state = new FiscalZCloseStateRecord(
            reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetString(4),
            FiscalZCloseStateContract.ContractVersion, reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7),
            reader.GetInt64(8), reader.IsDBNull(9) ? null : reader.GetGuid(9), reader.IsDBNull(10) ? null : reader.GetGuid(10),
            reader.GetString(11), reader.GetFieldValue<DateTimeOffset>(12), reader.GetString(13), reader.GetString(14),
            reader.GetFieldValue<DateTimeOffset>(15));
        return (reader.GetString(0), state);
    }

    private static async Task<FiscalZCloseStateInitializationResult?> ValidateScopeAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, InitializeFiscalZCloseStateCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT site.is_active,
                   identity.is_active,
                   identity.fiscal_identity_status,
                   EXISTS (
                       SELECT 1 FROM pos.site_pos_server_fiscal_identity_history history
                       WHERE history.site_pos_server_id = @site
                         AND history.fiscal_identity_id = @identity
                         AND transaction_timestamp() >= history.effective_start_at
                         AND (history.effective_end_at IS NULL OR transaction_timestamp() < history.effective_end_at)
                   ),
                   EXISTS (
                       SELECT 1 FROM pos.fiscal_reporting_contract_versions contract
                       WHERE contract.fiscal_reporting_contract_version_id = @contract AND contract.is_active
                   )
            FROM pos.site_pos_servers site
            CROSS JOIN pos.fiscal_identities identity
            WHERE site.site_pos_server_id = @site AND identity.fiscal_identity_id = @identity;
            """;
        await using var dbCommand = new NpgsqlCommand(sql, connection, transaction);
        dbCommand.Parameters.AddWithValue("site", command.SitePosServerId);
        dbCommand.Parameters.AddWithValue("identity", command.FiscalIdentityId);
        dbCommand.Parameters.AddWithValue("contract", ContractVersionId);
        await using var reader = await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return FiscalZCloseStateInitializationResult.Failure(FiscalZCloseStateInitializationErrorCode.SitePosServerUnavailable, "Fiscal reporting scope is unavailable.");
        if (!reader.GetBoolean(0))
            return FiscalZCloseStateInitializationResult.Failure(FiscalZCloseStateInitializationErrorCode.SitePosServerUnavailable, "Fiscal reporting scope is unavailable.");
        if (!reader.GetBoolean(1) || !string.Equals(reader.GetString(2), "APPROVED", StringComparison.Ordinal))
            return FiscalZCloseStateInitializationResult.Failure(FiscalZCloseStateInitializationErrorCode.FiscalIdentityDisabled, "Fiscal identity is not enabled for initialization.");
        if (!reader.GetBoolean(3))
            return FiscalZCloseStateInitializationResult.Failure(FiscalZCloseStateInitializationErrorCode.FiscalIdentityUnavailable, "Fiscal identity is not assigned to the Site POS Server.");
        if (!reader.GetBoolean(4))
            return FiscalZCloseStateInitializationResult.Failure(FiscalZCloseStateInitializationErrorCode.ContractUnavailable, "Fiscal reporting contract is unavailable.");
        return null;
    }

    private static async Task<bool> StateExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, InitializeFiscalZCloseStateCommand command, CancellationToken cancellationToken)
    {
        await using var dbCommand = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pos.fiscal_z_close_states WHERE site_pos_server_id=@site AND fiscal_identity_id=@identity AND currency_code=@currency)", connection, transaction);
        dbCommand.Parameters.AddWithValue("site", command.SitePosServerId);
        dbCommand.Parameters.AddWithValue("identity", command.FiscalIdentityId);
        dbCommand.Parameters.AddWithValue("currency", command.CurrencyCode);
        return (bool)(await dbCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? false);
    }

    private static async Task InsertStateAsync(NpgsqlConnection c, NpgsqlTransaction t, InitializeFiscalZCloseStateCommand command, Guid stateId, Guid provenanceId, DateTimeOffset at, CancellationToken ct)
    {
        const string sql = """INSERT INTO pos.fiscal_z_close_states(fiscal_z_close_state_id,site_pos_server_id,fiscal_identity_id,currency_code,fiscal_reporting_contract_version_id,reset_counter_value,z_counter_value,grand_total_amount_minor_units,state_version,initialization_provenance_code_id,initialized_at,initialized_by_ref,initialization_service_ref,initialization_approval_ref,last_transition_operation_ref,last_transition_at,created_at,updated_at) VALUES(@id,@site,@identity,@currency,@contract,@reset,@z,@gta,1,@provenance,@at,@actor,@service,@approval,@operation,@at,@at,@at)""";
        await using var dbCommand = new NpgsqlCommand(sql, c, t); AddCommon(dbCommand, command); dbCommand.Parameters.AddWithValue("id", stateId); dbCommand.Parameters.AddWithValue("contract", ContractVersionId); dbCommand.Parameters.AddWithValue("provenance", provenanceId); dbCommand.Parameters.AddWithValue("at", at); await dbCommand.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task InsertTransitionAsync(NpgsqlConnection c, NpgsqlTransaction t, InitializeFiscalZCloseStateCommand command, string hash, Guid stateId, Guid transitionId, Guid provenanceId, DateTimeOffset at, CancellationToken ct)
    {
        const string sql = """INSERT INTO pos.fiscal_z_close_state_transitions(fiscal_z_close_state_transition_id,fiscal_z_close_state_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,currency_code,transition_type_code_id,initialization_provenance_code_id,operation_ref,semantic_request_hash,semantic_hash_version,expected_state_version,resulting_state_version,approval_ref,actor_ref,service_identity_ref,correlation_ref,committed_at,created_at) VALUES(@transition,@state,@contract,@site,@identity,@currency,@type,@provenance,@operation,@hash,@hash_version,0,1,@approval,@actor,@service,@correlation,@at,@at)""";
        await using var dbCommand = new NpgsqlCommand(sql, c, t); AddCommon(dbCommand, command); dbCommand.Parameters.AddWithValue("transition", transitionId); dbCommand.Parameters.AddWithValue("state", stateId); dbCommand.Parameters.AddWithValue("contract", ContractVersionId); dbCommand.Parameters.AddWithValue("type", InitializationTypeId); dbCommand.Parameters.AddWithValue("provenance", provenanceId); dbCommand.Parameters.AddWithValue("hash", hash); dbCommand.Parameters.AddWithValue("hash_version", FiscalZCloseStateContract.InitializationSemanticHashVersion); dbCommand.Parameters.AddWithValue("at", at); await dbCommand.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task InsertTransitionValuesAsync(NpgsqlConnection c, NpgsqlTransaction t, InitializeFiscalZCloseStateCommand command, Guid transitionId, CancellationToken ct)
    {
        const string sql = """INSERT INTO pos.fiscal_z_close_state_transition_values(fiscal_z_close_state_transition_value_id,fiscal_z_close_state_transition_id,state_identity_code_id,resulting_counter_value,resulting_amount_minor_units,currency_code) VALUES(@id,@transition,@identity,@counter,@amount,@currency)""";
        foreach (var value in new[] { (ZCounterIdentityId, (long?)command.ZCounterValue, (long?)null, (string?)null), (ResetCounterIdentityId, (long?)command.ResetCounterValue, (long?)null, (string?)null), (GtaIdentityId, (long?)null, (long?)command.GrandTotalAmountMinorUnits, command.CurrencyCode) })
        {
            await using var dbCommand = new NpgsqlCommand(sql, c, t); dbCommand.Parameters.AddWithValue("id", Guid.NewGuid()); dbCommand.Parameters.AddWithValue("transition", transitionId); dbCommand.Parameters.AddWithValue("identity", value.Item1); dbCommand.Parameters.AddWithValue("counter", (object?)value.Item2 ?? DBNull.Value); dbCommand.Parameters.AddWithValue("amount", (object?)value.Item3 ?? DBNull.Value); dbCommand.Parameters.AddWithValue("currency", (object?)value.Item4 ?? DBNull.Value); await dbCommand.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    private static void AddCommon(NpgsqlCommand dbCommand, InitializeFiscalZCloseStateCommand command)
    {
        dbCommand.Parameters.AddWithValue("site", command.SitePosServerId); dbCommand.Parameters.AddWithValue("identity", command.FiscalIdentityId); dbCommand.Parameters.AddWithValue("currency", command.CurrencyCode); dbCommand.Parameters.AddWithValue("reset", command.ResetCounterValue); dbCommand.Parameters.AddWithValue("z", command.ZCounterValue); dbCommand.Parameters.AddWithValue("gta", command.GrandTotalAmountMinorUnits); dbCommand.Parameters.AddWithValue("operation", command.OperationReference); dbCommand.Parameters.AddWithValue("approval", command.ApprovalReference); dbCommand.Parameters.AddWithValue("actor", command.ActorReference); dbCommand.Parameters.AddWithValue("service", command.ServiceIdentityReference); dbCommand.Parameters.AddWithValue("correlation", command.CorrelationReference);
    }

    private static FiscalZCloseStateRecord ToRecord(InitializeFiscalZCloseStateCommand command, Guid stateId, DateTimeOffset at) =>
        new(stateId, command.SitePosServerId, command.FiscalIdentityId, command.CurrencyCode, FiscalZCloseStateContract.ContractVersion, command.ResetCounterValue, command.ZCounterValue, command.GrandTotalAmountMinorUnits, 1, null, null, command.Provenance, at, command.ApprovalReference, command.OperationReference, at);
}
