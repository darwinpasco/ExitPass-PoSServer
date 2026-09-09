using System.Data;
using ExitPass.PosServer.Runtime.FiscalReports;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresFiscalZReadingRepository(
    NpgsqlDataSource dataSource,
    FiscalXReadingAggregationService aggregationService) : IFiscalZReadingRepository
{
    private static readonly Guid OpenStatusId = Guid.Parse("1a6f7021-bc84-5c01-afaa-c5d6685633c8");
    private static readonly Guid ClosedStatusId = Guid.Parse("af7ee931-a023-507e-81a4-17adf047eb94");
    private static readonly Guid ZReadingKindId = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");
    private static readonly Guid ZCloseTransitionTypeId = Guid.Parse("52a28fc9-7c24-5810-8f98-30dbc7c134b9");
    private static readonly Guid ZCounterIdentityId = Guid.Parse("baf7bb67-9f11-5b2f-a750-e5689a5f0142");
    private static readonly Guid ResetCounterIdentityId = Guid.Parse("a1dddcd8-aab5-51a8-bb64-f610edbdc1eb");
    private static readonly Guid GtaIdentityId = Guid.Parse("e01e75a6-6ee9-5bdf-9bf4-0a38476f8805");

    public async Task<FiscalZReadingResult> CloseAsync(
        FiscalZReadingCommand command,
        CancellationToken cancellationToken = default)
    {
        var semanticHash = string.Empty;
        var commitAttempted = false;
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

            await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(
                connection, transaction, command.SitePosServerId, command.FiscalIdentityId,
                command.CurrencyCode, cancellationToken).ConfigureAwait(false);

            var period = await ReadAndLockPeriodAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            semanticHash = FiscalZReadingSemanticRequestHasher.Compute(command, period);

            var existing = await ReadOperationAsync(
                connection, transaction, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.Value.ReportKindId != ZReadingKindId ||
                    !string.Equals(existing.Value.SemanticHash, semanticHash, StringComparison.Ordinal))
                {
                    return Failure(FiscalZReadingOutcome.Conflict,
                        "The Z Reading operation key is already bound to different governed request semantics.");
                }

                var replay = string.IsNullOrWhiteSpace(existing.Value.ReportReference)
                    ? null
                    : await ReadRecordAsync(connection, transaction, existing.Value.ReportReference, cancellationToken).ConfigureAwait(false);
                return replay is null
                    ? Failure(FiscalZReadingOutcome.UnknownCommitOutcome, "The existing Z Reading operation requires durable-state reconciliation.")
                    : new(FiscalZReadingOutcome.Replayed, replay);
            }

            if (!string.Equals(period.Status, "open", StringComparison.Ordinal))
            {
                return Failure(
                    string.Equals(period.Status, "closed", StringComparison.Ordinal)
                        ? FiscalZReadingOutcome.PeriodAlreadyClosed
                        : FiscalZReadingOutcome.PeriodNotOpen,
                    "The fiscal reporting period is not eligible for Z close.");
            }
            if (period.DatabaseNow < period.PeriodEndAt)
            {
                return Failure(FiscalZReadingOutcome.PeriodNotEnded,
                    "The fiscal reporting period has not reached its governed end instant.");
            }

            var state = await PostgresFiscalZCloseTransitionFoundation.ReadStateForUpdateAsync(
                connection, transaction, command.SitePosServerId, command.FiscalIdentityId,
                command.CurrencyCode, cancellationToken).ConfigureAwait(false);
            if (state.StateVersion != command.ExpectedStateVersion)
            {
                return Failure(FiscalZReadingOutcome.StaleStateVersion,
                    "The expected fiscal Z close state version is stale.");
            }

            var sequence = await PostgresFiscalZCloseTransitionFoundation.ReadPeriodSequenceGuardAsync(
                connection, transaction, period.FiscalReportingPeriodId, command.SitePosServerId,
                command.FiscalIdentityId, command.CurrencyCode, cancellationToken).ConfigureAwait(false);
            ValidateStateContinuity(state, sequence);

            await EnsureAllPeriodDocumentsAssignedAsync(connection, transaction, period, cancellationToken).ConfigureAwait(false);
            var sourceDocuments = await ReadSourceDocumentsAsync(connection, transaction, period, cancellationToken).ConfigureAwait(false);
            var sourceGaps = await ReadSourceGapsAsync(connection, transaction, sourceDocuments, cancellationToken).ConfigureAwait(false);
            var aggregate = aggregationService.Aggregate(sourceDocuments, sourceGaps, period.CurrencyCode);

            var currentGta = aggregate.Amounts.NetSalesAmountMinorUnits;
            var resultingZ = checked(state.ZCounterValue + 1);
            var resultingGta = checked(state.GrandTotalAmountMinorUnits + currentGta);
            var resultingVersion = checked(state.StateVersion + 1);
            if (currentGta != aggregate.Amounts.NetSalesAmountMinorUnits)
            {
                return Failure(FiscalZReadingOutcome.ReconciliationFailure,
                    "The governed GTA contribution does not reconcile to Z Reading net sales.");
            }

            var identifiers = await ResolveReportingCodeIdsAsync(connection, transaction, aggregate, cancellationToken).ConfigureAwait(false);
            var committedAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var requestId = Guid.NewGuid();
            var reportId = Guid.NewGuid();
            var transitionId = Guid.NewGuid();
            var reportReference = $"Z-{period.BusinessDayDate:yyyyMMdd}-{resultingZ:D8}-{reportId:N}".ToUpperInvariant();

            await InsertRequestAsync(connection, transaction, command, period, identifiers, requestId, semanticHash, committedAt, cancellationToken).ConfigureAwait(false);
            await InsertScopeAsync(connection, transaction, period, identifiers, requestId, cancellationToken).ConfigureAwait(false);
            await InsertSnapshotAsync(connection, transaction, period, aggregate, state, resultingZ, resultingGta,
                identifiers, requestId, reportId, reportReference, committedAt, cancellationToken).ConfigureAwait(false);
            await InsertChildrenAsync(connection, transaction, period.FiscalIdentityId, aggregate, identifiers, reportId, cancellationToken).ConfigureAwait(false);
            await InsertCounterSnapshotAsync(connection, transaction, state, sequence, reportId, currentGta,
                resultingZ, resultingGta, resultingVersion, cancellationToken).ConfigureAwait(false);
            await InsertTransitionAsync(connection, transaction, command, period, state, sequence, reportId,
                transitionId, semanticHash, currentGta, resultingZ, resultingGta, resultingVersion,
                committedAt, cancellationToken).ConfigureAwait(false);

            var appliedVersion = await PostgresFiscalZCloseTransitionFoundation.ApplyPreparedTransitionAsync(
                connection, transaction, transitionId, state.StateVersion, cancellationToken).ConfigureAwait(false);
            if (appliedVersion != resultingVersion)
            {
                throw new FiscalZReadingSafeException(FiscalZReadingOutcome.StaleStateVersion,
                    "The fiscal Z close state transition did not advance to the expected version.");
            }

            await ClosePeriodAsync(connection, transaction, period.FiscalReportingPeriodId, committedAt,
                command.ServiceIdentityRef, cancellationToken).ConfigureAwait(false);
            await PostgresElectronicJournalWriter.AppendAsync(
                connection,
                transaction,
                new ElectronicJournalAppendRequest(
                    period.SitePosServerId,
                    period.FiscalIdentityId,
                    period.CurrencyCode,
                    period.FiscalReportingPeriodId,
                    "z_reading_committed",
                    $"fiscal-report-request:{requestId:D}",
                    FiscalZReadingContract.ContractVersion,
                    committedAt,
                    command.RequestedByRef,
                    command.ServiceIdentityRef,
                    command.CorrelationId,
                    ElectronicJournalReportFacts.FromZ(
                        aggregate,
                        reportReference,
                        state.ResetCounterValue,
                        state.ResetCounterValue,
                        state.ZCounterValue,
                        resultingZ,
                        state.GrandTotalAmountMinorUnits,
                        currentGta,
                        resultingGta,
                        resultingVersion),
                    FiscalReportRequestId: requestId,
                    XZReportId: reportId,
                    BusinessDayDate: period.BusinessDayDate,
                    IdempotencyReference: command.OperationKey),
                cancellationToken).ConfigureAwait(false);
            await InsertAuditAsync(connection, transaction, command, identifiers, requestId, cancellationToken).ConfigureAwait(false);

            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            var record = await ReadRecordAsync(connection, null, reportReference, cancellationToken).ConfigureAwait(false);
            return record is null
                ? Failure(FiscalZReadingOutcome.UnknownCommitOutcome, "The committed Z Reading requires durable-state reconciliation.")
                : new(FiscalZReadingOutcome.Created, record);
        }
        catch (FiscalZReadingSafeException exception)
        {
            return Failure(exception.Outcome, exception.Message);
        }
        catch (FiscalCloseBoundaryException exception)
        {
            return Failure(MapBoundaryError(exception.ErrorCode), exception.Message);
        }
        catch (OverflowException)
        {
            return Failure(FiscalZReadingOutcome.ArithmeticOverflow,
                "Z Reading arithmetic exceeded supported minor-unit bounds.");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return await ReconcileAfterFailureAsync(command, semanticHash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure)
        {
            return Failure(FiscalZReadingOutcome.RetryableConcurrencyFailure,
                "The Z Reading close encountered a retryable concurrency failure.");
        }
        catch (NpgsqlException)
        {
            return await ReconcileAfterFailureAsync(command, semanticHash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return await ReconcileAfterFailureAsync(command, semanticHash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<FiscalZReadingResult> GetByReferenceAsync(
        string fiscalReportReference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var record = await ReadRecordAsync(connection, null, fiscalReportReference, cancellationToken).ConfigureAwait(false);
            return record is null ? new(FiscalZReadingOutcome.NotFound) : new(FiscalZReadingOutcome.Replayed, record);
        }
        catch (NpgsqlException)
        {
            return Failure(FiscalZReadingOutcome.PersistenceFailure, "The Z Reading read operation failed safely.");
        }
    }

    private async Task<FiscalZReadingResult> ReconcileAfterFailureAsync(
        FiscalZReadingCommand command,
        string semanticHash,
        bool commitAttempted,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(semanticHash))
        {
            return Failure(commitAttempted ? FiscalZReadingOutcome.UnknownCommitOutcome : FiscalZReadingOutcome.PersistenceFailure,
                commitAttempted ? "The Z Reading commit outcome requires durable-state reconciliation." : "The Z Reading close failed safely.");
        }

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var existing = await ReadOperationAsync(connection, null, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return Failure(commitAttempted ? FiscalZReadingOutcome.UnknownCommitOutcome : FiscalZReadingOutcome.PersistenceFailure,
                    commitAttempted ? "The Z Reading commit outcome requires durable-state reconciliation." : "The Z Reading close failed safely.");
            }
            if (existing.Value.ReportKindId != ZReadingKindId || !string.Equals(existing.Value.SemanticHash, semanticHash, StringComparison.Ordinal))
            {
                return Failure(FiscalZReadingOutcome.Conflict,
                    "The Z Reading operation key is already bound to different governed request semantics.");
            }
            var record = string.IsNullOrWhiteSpace(existing.Value.ReportReference)
                ? null
                : await ReadRecordAsync(connection, null, existing.Value.ReportReference, cancellationToken).ConfigureAwait(false);
            return record is null
                ? Failure(FiscalZReadingOutcome.UnknownCommitOutcome, "The Z Reading operation requires durable-state reconciliation.")
                : new(FiscalZReadingOutcome.Replayed, record);
        }
        catch (NpgsqlException)
        {
            return Failure(FiscalZReadingOutcome.UnknownCommitOutcome,
                "The Z Reading operation requires durable-state reconciliation.");
        }
    }

    private static async Task<FiscalZReadingPeriod> ReadAndLockPeriodAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalZReadingCommand command,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.fiscal_reporting_period_id,p.fiscal_reporting_contract_version_id,p.site_pos_server_id,
                   p.fiscal_identity_id,p.business_day_date,p.period_start_at,p.period_end_at,
                   p.reporting_timezone_name,p.business_day_cutoff_local_time,p.currency_code,p.period_sequence,
                   p.expected_prior_period_id,status.code_key,transaction_timestamp(),site.is_active,identity.is_active,
                   contract.contract_key,contract.contract_version,contract.semantic_hash_version,contract.is_active
            FROM pos.fiscal_reporting_periods p
            JOIN pos.controlled_codes status ON status.controlled_code_id=p.period_status_code_id
            JOIN pos.site_pos_servers site ON site.site_pos_server_id=p.site_pos_server_id
            JOIN pos.fiscal_identities identity ON identity.fiscal_identity_id=p.fiscal_identity_id
            JOIN pos.fiscal_reporting_contract_versions contract ON contract.fiscal_reporting_contract_version_id=p.fiscal_reporting_contract_version_id
            WHERE p.fiscal_reporting_period_id=@period AND p.site_pos_server_id=@site
              AND p.fiscal_identity_id=@identity AND p.currency_code=@currency
            FOR UPDATE OF p;
            """;
        await using var dbCommand = CreateCommand(connection, transaction, sql);
        dbCommand.Parameters.AddWithValue("period", command.FiscalReportingPeriodId);
        dbCommand.Parameters.AddWithValue("site", command.SitePosServerId);
        dbCommand.Parameters.AddWithValue("identity", command.FiscalIdentityId);
        dbCommand.Parameters.AddWithValue("currency", command.CurrencyCode);
        await using var reader = await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new FiscalZReadingSafeException(FiscalZReadingOutcome.PeriodUnavailable,
                "The governed fiscal reporting period is unavailable for this scope.");
        if (!reader.GetBoolean(14) || !reader.GetBoolean(15))
            throw new FiscalZReadingSafeException(FiscalZReadingOutcome.ReportingConfigurationUnavailable,
                "The fiscal reporting scope is inactive.");
        if (reader.IsDBNull(7) || reader.IsDBNull(8) || string.IsNullOrWhiteSpace(reader.GetString(7)) ||
            reader.GetString(16) != "pos-server-fiscal-reporting" || reader.GetString(17) != "v1" ||
            reader.GetString(18) != FiscalZReadingContract.SemanticHashVersion || !reader.GetBoolean(19))
            throw new FiscalZReadingSafeException(FiscalZReadingOutcome.ReportingConfigurationUnavailable,
                "The governed fiscal reporting configuration is unavailable.");
        return new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3),
            reader.GetFieldValue<DateOnly>(4), reader.GetFieldValue<DateTimeOffset>(5), reader.GetFieldValue<DateTimeOffset>(6),
            reader.GetString(7), reader.GetFieldValue<TimeOnly>(8), reader.GetString(9), reader.GetInt64(10),
            reader.IsDBNull(11) ? null : reader.GetGuid(11), reader.GetString(12), reader.GetFieldValue<DateTimeOffset>(13));
    }

    private static void ValidateStateContinuity(LockedFiscalZCloseState state, FiscalZClosePeriodSequenceGuard sequence)
    {
        if (sequence.IsFirstPeriod)
        {
            if (state.StateVersion != 1 || state.LastClosedReportingPeriodId.HasValue || state.LastCommittedZReportId.HasValue)
                throw new FiscalZReadingSafeException(FiscalZReadingOutcome.PriorPeriodUnresolved,
                    "Initialized first-period Z state has inconsistent prior-close continuity.");
            return;
        }
        if (!sequence.PriorPeriodIsClosedWithZ || state.LastClosedReportingPeriodId != sequence.ImmediatePriorPeriodId ||
            state.LastCommittedZReportId != sequence.ImmediatePriorZReportId)
            throw new FiscalZReadingSafeException(FiscalZReadingOutcome.PriorPeriodUnresolved,
                "The immediate prior fiscal reporting period is not the canonical committed Z predecessor.");
    }

    private static async Task EnsureAllPeriodDocumentsAssignedAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, FiscalZReadingPeriod period, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT count(*)
            FROM pos.fiscal_documents d
            WHERE d.site_pos_server_id=@site AND d.fiscal_identity_id=@identity
              AND d.created_at>=@start AND d.created_at<@end
              AND (d.currency_code IS NULL OR d.currency_code<>@currency OR d.fiscal_reporting_period_id IS DISTINCT FROM @period);
            """;
        await using var command = CreateCommand(connection, transaction, sql);
        AddPeriodParameters(command, period);
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 0)
            throw new FiscalZReadingSafeException(FiscalZReadingOutcome.UnassignedFiscalDocument,
                "A fiscal document in the governed period lacks an unambiguous matching period assignment.");
    }

    private static async Task<(string SemanticHash, Guid ReportKindId, string ReportReference)?> ReadOperationAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid sitePosServerId,
        string operationKey, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT request.semantic_request_hash,request.report_type_code_id,report.report_number
            FROM pos.fiscal_report_requests request
            LEFT JOIN pos.x_z_reports report ON report.fiscal_report_request_id=request.fiscal_report_request_id
            WHERE request.site_pos_server_id=@site AND request.operation_idempotency_key=@operation;
            """;
        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.AddWithValue("site", sitePosServerId);
        command.Parameters.AddWithValue("operation", operationKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return (reader.GetString(0), reader.GetGuid(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
    }

    private static async Task<IReadOnlyList<FiscalXReadingSourceDocument>> ReadSourceDocumentsAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, FiscalZReadingPeriod period, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT d.fiscal_document_id,type_code.code_key,status_code.code_key,d.created_at,
                   d.fiscal_sequence_policy_id,d.fiscal_sequence_value,d.fiscal_document_number,d.fiscal_series,
                   d.completion_basis
            FROM pos.fiscal_documents d
            JOIN pos.controlled_codes type_code ON type_code.controlled_code_id=d.fiscal_document_type_code_id
            JOIN pos.controlled_codes status_code ON status_code.controlled_code_id=d.fiscal_document_status_code_id
            WHERE d.fiscal_reporting_period_id=@period
              AND d.site_pos_server_id=@site AND d.fiscal_identity_id=@identity AND d.currency_code=@currency
            ORDER BY d.created_at,d.fiscal_document_id;
            """;
        var documents = new Dictionary<Guid, SourceBuilder>();
        await using (var command = CreateCommand(connection, transaction, sql))
        {
            AddPeriodParameters(command, period);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = reader.GetGuid(0);
                documents[id] = new(id, reader.GetString(1), reader.GetString(2), reader.GetFieldValue<DateTimeOffset>(3),
                    reader.IsDBNull(4) ? null : reader.GetGuid(4), reader.IsDBNull(5) ? null : reader.GetInt64(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.GetString(8));
            }
        }
        if (documents.Count == 0) return [];
        await ReadLinesAsync(connection, transaction, documents, cancellationToken).ConfigureAwait(false);
        await ReadTendersAsync(connection, transaction, documents, cancellationToken).ConfigureAwait(false);
        await ReadTaxesAsync(connection, transaction, documents, cancellationToken).ConfigureAwait(false);
        await ReadDiscountsAsync(connection, transaction, documents, cancellationToken).ConfigureAwait(false);
        await ReadStatutoryAsync(connection, transaction, documents, cancellationToken).ConfigureAwait(false);
        return documents.Values.Select(builder => builder.Build()).ToArray();
    }

    private static async Task ReadLinesAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT l.fiscal_document_id,code.code_key,l.gross_amount_minor_units,l.discount_amount_minor_units,l.tax_amount_minor_units,l.net_amount_minor_units,l.currency_code FROM pos.fiscal_document_lines l JOIN pos.controlled_codes code ON code.controlled_code_id=l.line_type_code_id WHERE l.fiscal_document_id=ANY(@ids) AND l.is_active ORDER BY l.fiscal_document_id,l.line_sequence";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray());
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Lines.Add(new(r.GetString(1), r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetInt64(5), r.GetString(6)));
    }

    private static async Task ReadTendersAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id,code.code_key,x.amount_minor_units,x.currency_code FROM pos.fiscal_tenders x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tender_type_code_id WHERE x.fiscal_document_id=ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_tender_id";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Tenders.Add(new(r.GetString(1), r.GetInt64(2), r.GetString(3)));
    }

    private static async Task ReadTaxesAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id,code.code_key,x.fiscal_document_line_id IS NULL,x.taxable_amount_minor_units,x.tax_amount_minor_units,x.currency_code FROM pos.fiscal_tax_details x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tax_classification_code_id WHERE x.fiscal_document_id=ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_tax_detail_id";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Taxes.Add(new(r.GetString(1), r.GetBoolean(2), r.GetInt64(3), r.GetInt64(4), r.GetString(5)));
    }

    private static async Task ReadDiscountsAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id,code.code_key,x.discount_amount_minor_units,x.vat_privilege_amount_minor_units,x.currency_code FROM pos.fiscal_discount_privilege_details x JOIN pos.controlled_codes code ON code.controlled_code_id=x.discount_privilege_type_code_id WHERE x.fiscal_document_id=ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_discount_privilege_detail_id";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Discounts.Add(new(r.GetString(1), r.GetInt64(2), r.GetInt64(3), r.GetString(4)));
    }

    private static async Task ReadStatutoryAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id,entitlement.code_key,x.statutory_discount_amount_minor_units,x.vat_amount_minor_units,x.final_payable_amount_minor_units,x.currency_code FROM pos.fiscal_document_applied_statutory_facts x JOIN pos.controlled_codes entitlement ON entitlement.controlled_code_id=x.entitlement_type_code_id WHERE x.fiscal_document_id=ANY(@ids)";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Statutory = new(r.GetString(1), r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetString(5));
    }

    private static async Task<IReadOnlyList<FiscalXReadingSourceGap>> ReadSourceGapsAsync(
        NpgsqlConnection c, NpgsqlTransaction t, IReadOnlyList<FiscalXReadingSourceDocument> documents, CancellationToken ct)
    {
        var policies = documents.Where(d => d.SequencePolicyId.HasValue).Select(d => d.SequencePolicyId!.Value).Distinct().ToArray();
        if (policies.Length == 0) return [];
        const string sql = "SELECT g.fiscal_sequence_gap_audit_id,g.fiscal_sequence_policy_id,g.gap_sequence_value,code.code_key FROM pos.fiscal_sequence_gap_audit g JOIN pos.controlled_codes code ON code.controlled_code_id=g.gap_reason_code_id WHERE g.fiscal_sequence_policy_id=ANY(@ids) ORDER BY g.fiscal_sequence_policy_id,g.gap_sequence_value";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", policies); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); var result = new List<FiscalXReadingSourceGap>(); while (await r.ReadAsync(ct).ConfigureAwait(false)) result.Add(new(r.GetGuid(0), r.GetGuid(1), r.GetInt64(2), r.GetString(3))); return result;
    }

    private static async Task<ReportingCodeIds> ResolveReportingCodeIdsAsync(
        NpgsqlConnection c, NpgsqlTransaction t, FiscalXReadingAggregate aggregate, CancellationToken ct)
    {
        var required = new List<(string Set, string Key)>
        {
            ("fiscal_report_kind", "z_reading"), ("fiscal_report_status", "committed"),
            ("fiscal_report_scope_type", "site_pos_server_period"), ("fiscal_action_audit_result", "success")
        };
        required.AddRange(aggregate.Tenders.Select(x => ("fiscal_reporting_tender_classification", x.Classification)));
        required.AddRange(aggregate.Discounts.Select(x => ("fiscal_reporting_discount_classification", x.Classification)));
        required.AddRange(aggregate.FiscalNumberRanges.SelectMany(x => x.Gaps).Select(x => ("fiscal_sequence_gap_classification", x.Classification)));
        var result = new Dictionary<(string, string), Guid>();
        const string sql = "SELECT code.controlled_code_id FROM pos.controlled_codes code JOIN pos.controlled_code_sets set_ ON set_.controlled_code_set_id=code.controlled_code_set_id WHERE set_.code_set_key=@set AND code.code_key=@key AND set_.is_active AND code.is_active";
        foreach (var pair in required.Distinct())
        {
            await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("set", pair.Set); cmd.Parameters.AddWithValue("key", pair.Key);
            if (await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is not Guid id)
                throw new FiscalZReadingSafeException(FiscalZReadingOutcome.UnsupportedSourceClassification,
                    "A required governed reporting classification is unavailable.");
            result[pair] = id;
        }
        return new(result);
    }

    private static Task InsertRequestAsync(
        NpgsqlConnection c, NpgsqlTransaction t, FiscalZReadingCommand command, FiscalZReadingPeriod period,
        ReportingCodeIds ids, Guid requestId, string hash, DateTimeOffset committedAt, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_requests(fiscal_report_request_id,fiscal_reporting_contract_version_id,fiscal_reporting_period_id,site_pos_server_id,report_type_code_id,report_status_code_id,operation_idempotency_key,semantic_request_hash,semantic_hash_version,business_day_date,requested_at,requested_by_ref,service_identity_ref,created_at,updated_at) VALUES(@id,@contract,@period,@site,@kind,@status,@operation,@hash,@version,@business,@at,@actor,@service,@at,@at)", ct, p =>
        {
            p.AddWithValue("id", requestId); p.AddWithValue("contract", period.FiscalReportingContractVersionId); p.AddWithValue("period", period.FiscalReportingPeriodId);
            p.AddWithValue("site", period.SitePosServerId); p.AddWithValue("kind", ids.Get("fiscal_report_kind", "z_reading")); p.AddWithValue("status", ids.Get("fiscal_report_status", "committed"));
            p.AddWithValue("operation", command.OperationKey); p.AddWithValue("hash", hash); p.AddWithValue("version", FiscalZReadingContract.SemanticHashVersion);
            p.AddWithValue("business", period.BusinessDayDate); p.AddWithValue("at", committedAt); p.AddWithValue("actor", command.RequestedByRef); p.AddWithValue("service", command.ServiceIdentityRef);
        });

    private static Task InsertScopeAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalZReadingPeriod period, ReportingCodeIds ids, Guid requestId, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_scopes(fiscal_report_scope_id,fiscal_report_request_id,fiscal_reporting_period_id,scope_type_code_id,created_at) VALUES(@id,@request,@period,@scope,clock_timestamp())", ct, p =>
        { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("request", requestId); p.AddWithValue("period", period.FiscalReportingPeriodId); p.AddWithValue("scope", ids.Get("fiscal_report_scope_type", "site_pos_server_period")); });

    private static Task InsertSnapshotAsync(
        NpgsqlConnection c, NpgsqlTransaction t, FiscalZReadingPeriod period, FiscalXReadingAggregate aggregate,
        LockedFiscalZCloseState state, long resultingZ, long resultingGta, ReportingCodeIds ids,
        Guid requestId, Guid reportId, string reference, DateTimeOffset committedAt, CancellationToken ct)
    {
        const string sql = """
        INSERT INTO pos.x_z_reports(x_z_report_id,fiscal_report_request_id,fiscal_reporting_contract_version_id,fiscal_reporting_period_id,report_kind_code_id,site_pos_server_id,fiscal_identity_id,report_number,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,transaction_count,begin_fiscal_document_id,end_fiscal_document_id,beginning_si_ref,ending_si_ref,first_fiscal_sequence_value,last_fiscal_sequence_value,fiscal_sequence_gap_count,reset_counter_value,z_counter_value,previous_grand_total_amount_minor_units,current_grand_total_amount_minor_units,present_grand_total_amount_minor_units,gross_sales_amount_minor_units,net_sales_amount_minor_units,vatable_sales_amount_minor_units,vat_amount_minor_units,vat_exempt_sales_amount_minor_units,zero_rated_sales_amount_minor_units,discount_amount_minor_units,senior_citizen_discount_amount_minor_units,pwd_discount_amount_minor_units,other_statutory_discount_amount_minor_units,vat_exemption_amount_minor_units,coupon_discount_amount_minor_units,promotional_discount_amount_minor_units,void_amount_minor_units,refund_amount_minor_units,return_amount_minor_units,adjustment_amount_minor_units,service_charge_amount_minor_units,currency_code,generated_at,committed_at,created_at,updated_at)
        VALUES(@id,@request,@contract,@period,@kind,@site,@identity,@reference,@business,@start,@end,@timezone,@cutoff,@count,@begin_doc,@end_doc,@begin_ref,@end_ref,@first_seq,@last_seq,@gap_count,@reset,@z,@previous_gta,@current_gta,@resulting_gta,@gross,@net,@vatable,@vat,@vat_exempt,@zero_rated,@discount,@senior,@pwd,@other_stat,@vat_exemption,@coupon,@promo,@void,0,0,0,0,@currency,@committed,@committed,@committed,@committed)
        """;
        return ExecuteAsync(c, t, sql, ct, p =>
        {
            p.AddWithValue("id", reportId); p.AddWithValue("request", requestId); p.AddWithValue("contract", period.FiscalReportingContractVersionId);
            p.AddWithValue("period", period.FiscalReportingPeriodId); p.AddWithValue("kind", ids.Get("fiscal_report_kind", "z_reading")); p.AddWithValue("site", period.SitePosServerId);
            p.AddWithValue("identity", period.FiscalIdentityId); p.AddWithValue("reference", reference); p.AddWithValue("business", period.BusinessDayDate); p.AddWithValue("start", period.PeriodStartAt);
            p.AddWithValue("end", period.PeriodEndAt); p.AddWithValue("timezone", period.ReportingTimezoneName); p.AddWithValue("cutoff", period.BusinessDayCutoffLocalTime); p.AddWithValue("count", aggregate.QualifyingDocumentCount);
            AddNullable(p, "begin_doc", aggregate.BeginFiscalDocumentId, NpgsqlDbType.Uuid); AddNullable(p, "end_doc", aggregate.EndFiscalDocumentId, NpgsqlDbType.Uuid);
            AddNullable(p, "begin_ref", aggregate.BeginningFiscalNumber, NpgsqlDbType.Text); AddNullable(p, "end_ref", aggregate.EndingFiscalNumber, NpgsqlDbType.Text);
            AddNullable(p, "first_seq", aggregate.FirstSequenceValue, NpgsqlDbType.Bigint); AddNullable(p, "last_seq", aggregate.LastSequenceValue, NpgsqlDbType.Bigint);
            p.AddWithValue("gap_count", aggregate.FiscalNumberRanges.Sum(range => (long)range.Gaps.Count)); p.AddWithValue("reset", state.ResetCounterValue); p.AddWithValue("z", resultingZ);
            p.AddWithValue("previous_gta", state.GrandTotalAmountMinorUnits); p.AddWithValue("current_gta", aggregate.Amounts.NetSalesAmountMinorUnits); p.AddWithValue("resulting_gta", resultingGta);
            AddAmountParameters(p, aggregate.Amounts); p.AddWithValue("currency", period.CurrencyCode); p.AddWithValue("committed", committedAt);
        });
    }

    private static async Task InsertChildrenAsync(NpgsqlConnection c, NpgsqlTransaction t, Guid identityId, FiscalXReadingAggregate aggregate, ReportingCodeIds ids, Guid reportId, CancellationToken ct)
    {
        foreach (var item in aggregate.Tenders)
            await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_tender_breakdowns VALUES(@id,@report,@class,@count,@amount,@currency,clock_timestamp())", ct, p => { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("report", reportId); p.AddWithValue("class", ids.Get("fiscal_reporting_tender_classification", item.Classification)); p.AddWithValue("count", item.TransactionCount); p.AddWithValue("amount", item.AmountMinorUnits); p.AddWithValue("currency", item.CurrencyCode); }).ConfigureAwait(false);
        foreach (var item in aggregate.Discounts)
            await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_discount_breakdowns VALUES(@id,@report,@class,@count,@discount,@vat,@currency,clock_timestamp())", ct, p => { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("report", reportId); p.AddWithValue("class", ids.Get("fiscal_reporting_discount_classification", item.Classification)); p.AddWithValue("count", item.QualifyingDocumentCount); p.AddWithValue("discount", item.DiscountAmountMinorUnits); p.AddWithValue("vat", item.VatExemptionAmountMinorUnits); p.AddWithValue("currency", item.CurrencyCode); }).ConfigureAwait(false);
        foreach (var range in aggregate.FiscalNumberRanges)
        {
            var rangeId = Guid.NewGuid();
            await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_fiscal_number_ranges VALUES(@id,@report,@identity,@policy,@series,@first,@last,@first_no,@last_no,@count,@gaps,@currency,clock_timestamp())", ct, p => { p.AddWithValue("id", rangeId); p.AddWithValue("report", reportId); p.AddWithValue("identity", identityId); p.AddWithValue("policy", range.FiscalSequencePolicyId); p.AddWithValue("series", range.FiscalSeries); p.AddWithValue("first", range.FirstSequenceValue); p.AddWithValue("last", range.LastSequenceValue); p.AddWithValue("first_no", range.FirstFiscalNumber); p.AddWithValue("last_no", range.LastFiscalNumber); p.AddWithValue("count", range.QualifyingDocumentCount); p.AddWithValue("gaps", (long)range.Gaps.Count); p.AddWithValue("currency", range.CurrencyCode); }).ConfigureAwait(false);
            foreach (var gap in range.Gaps)
                await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_sequence_gaps(fiscal_report_sequence_gap_id,fiscal_report_fiscal_number_range_id,gap_sequence_value,gap_classification_code_id,created_at) VALUES(@id,@range,@sequence,@classification,clock_timestamp())", ct, p => { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("range", rangeId); p.AddWithValue("sequence", gap.SequenceValue); p.AddWithValue("classification", ids.Get("fiscal_sequence_gap_classification", gap.Classification)); }).ConfigureAwait(false);
        }
    }

    private static Task InsertCounterSnapshotAsync(
        NpgsqlConnection c, NpgsqlTransaction t, LockedFiscalZCloseState state, FiscalZClosePeriodSequenceGuard sequence,
        Guid reportId, long currentGta, long resultingZ, long resultingGta, long resultingVersion, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO pos.fiscal_z_counter_snapshots(fiscal_z_counter_snapshot_id,fiscal_z_close_state_id,x_z_report_id,report_kind_code_id,expected_prior_period_id,expected_state_version,resulting_state_version,previous_reset_counter_value,resulting_reset_counter_value,previous_z_counter_value,resulting_z_counter_value,previous_grand_total_amount_minor_units,current_period_amount_minor_units,resulting_grand_total_amount_minor_units,currency_code,created_at) VALUES(@id,@state,@report,@kind,@prior,@expected_version,@resulting_version,@previous_reset,@resulting_reset,@previous_z,@resulting_z,@previous_gta,@current_gta,@resulting_gta,@currency,clock_timestamp())", ct, p =>
        {
            p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("state", state.FiscalZCloseStateId); p.AddWithValue("report", reportId); p.AddWithValue("kind", ZReadingKindId);
            AddNullable(p, "prior", sequence.ExpectedPriorPeriodId, NpgsqlDbType.Uuid); p.AddWithValue("expected_version", state.StateVersion); p.AddWithValue("resulting_version", resultingVersion);
            p.AddWithValue("previous_reset", state.ResetCounterValue); p.AddWithValue("resulting_reset", state.ResetCounterValue); p.AddWithValue("previous_z", state.ZCounterValue); p.AddWithValue("resulting_z", resultingZ);
            p.AddWithValue("previous_gta", state.GrandTotalAmountMinorUnits); p.AddWithValue("current_gta", currentGta); p.AddWithValue("resulting_gta", resultingGta); p.AddWithValue("currency", state.CurrencyCode);
        });

    private static async Task InsertTransitionAsync(
        NpgsqlConnection c, NpgsqlTransaction t, FiscalZReadingCommand command, FiscalZReadingPeriod period,
        LockedFiscalZCloseState state, FiscalZClosePeriodSequenceGuard sequence, Guid reportId, Guid transitionId,
        string reportHash, long currentGta, long resultingZ, long resultingGta, long resultingVersion,
        DateTimeOffset committedAt, CancellationToken ct)
    {
        var transitionHash = FiscalZReadingSemanticRequestHasher.ComputeTransition(reportHash, state.ResetCounterValue,
            state.ResetCounterValue, state.ZCounterValue, resultingZ, state.GrandTotalAmountMinorUnits, currentGta, resultingGta);
        const string sql = """
        INSERT INTO pos.fiscal_z_close_state_transitions(fiscal_z_close_state_transition_id,fiscal_z_close_state_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,currency_code,transition_type_code_id,operation_ref,semantic_request_hash,semantic_hash_version,expected_state_version,resulting_state_version,previous_reporting_period_id,resulting_reporting_period_id,previous_z_report_id,previous_z_report_kind_code_id,resulting_z_report_id,resulting_z_report_kind_code_id,approval_ref,actor_ref,service_identity_ref,correlation_ref,committed_at,created_at)
        VALUES(@id,@state,@contract,@site,@identity,@currency,@type,@operation,@hash,@hash_version,@expected_version,@resulting_version,@previous_period,@period,@previous_z,@previous_kind,@report,@kind,@approval,@actor,@service,@correlation,@committed,@committed)
        """;
        await ExecuteAsync(c, t, sql, ct, p =>
        {
            p.AddWithValue("id", transitionId); p.AddWithValue("state", state.FiscalZCloseStateId); p.AddWithValue("contract", period.FiscalReportingContractVersionId);
            p.AddWithValue("site", period.SitePosServerId); p.AddWithValue("identity", period.FiscalIdentityId); p.AddWithValue("currency", period.CurrencyCode); p.AddWithValue("type", ZCloseTransitionTypeId);
            p.AddWithValue("operation", command.OperationKey); p.AddWithValue("hash", transitionHash); p.AddWithValue("hash_version", FiscalZReadingContract.StateTransitionSemanticHashVersion);
            p.AddWithValue("expected_version", state.StateVersion); p.AddWithValue("resulting_version", resultingVersion); AddNullable(p, "previous_period", sequence.ImmediatePriorPeriodId, NpgsqlDbType.Uuid);
            p.AddWithValue("period", period.FiscalReportingPeriodId); AddNullable(p, "previous_z", sequence.ImmediatePriorZReportId, NpgsqlDbType.Uuid);
            AddNullable(p, "previous_kind", sequence.ImmediatePriorZReportId.HasValue ? ZReadingKindId : null, NpgsqlDbType.Uuid); p.AddWithValue("report", reportId); p.AddWithValue("kind", ZReadingKindId);
            p.AddWithValue("approval", "approved-z-close-contract-v1"); p.AddWithValue("actor", command.RequestedByRef); p.AddWithValue("service", command.ServiceIdentityRef);
            p.AddWithValue("correlation", command.CorrelationId); p.AddWithValue("committed", committedAt);
        }).ConfigureAwait(false);

        await InsertTransitionValueAsync(c, t, transitionId, ZCounterIdentityId, state.ZCounterValue, resultingZ, null, null, null, ct).ConfigureAwait(false);
        await InsertTransitionValueAsync(c, t, transitionId, ResetCounterIdentityId, state.ResetCounterValue, state.ResetCounterValue, null, null, null, ct).ConfigureAwait(false);
        await InsertTransitionValueAsync(c, t, transitionId, GtaIdentityId, null, null, state.GrandTotalAmountMinorUnits, resultingGta, state.CurrencyCode, ct).ConfigureAwait(false);
    }

    private static Task InsertTransitionValueAsync(
        NpgsqlConnection c, NpgsqlTransaction t, Guid transitionId, Guid identityId,
        long? previousCounter, long? resultingCounter, long? previousAmount, long? resultingAmount,
        string? currency, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO pos.fiscal_z_close_state_transition_values(fiscal_z_close_state_transition_value_id,fiscal_z_close_state_transition_id,state_identity_code_id,previous_counter_value,resulting_counter_value,previous_amount_minor_units,resulting_amount_minor_units,currency_code,created_at) VALUES(@id,@transition,@identity,@previous_counter,@resulting_counter,@previous_amount,@resulting_amount,@currency,clock_timestamp())", ct, p =>
        {
            p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("transition", transitionId); p.AddWithValue("identity", identityId);
            AddNullable(p, "previous_counter", previousCounter, NpgsqlDbType.Bigint); AddNullable(p, "resulting_counter", resultingCounter, NpgsqlDbType.Bigint);
            AddNullable(p, "previous_amount", previousAmount, NpgsqlDbType.Bigint); AddNullable(p, "resulting_amount", resultingAmount, NpgsqlDbType.Bigint);
            AddNullable(p, "currency", currency, NpgsqlDbType.Char);
        });

    private static async Task ClosePeriodAsync(
        NpgsqlConnection c, NpgsqlTransaction t, Guid periodId, DateTimeOffset committedAt,
        string serviceIdentityRef, CancellationToken ct)
    {
        const string sql = "UPDATE pos.fiscal_reporting_periods SET period_status_code_id=@closed,closing_started_at=@at,closed_at=@at,updated_by_ref=@actor,updated_at=@at WHERE fiscal_reporting_period_id=@period AND period_status_code_id=@open";
        await using var command = CreateCommand(c, t, sql); command.Parameters.AddWithValue("closed", ClosedStatusId); command.Parameters.AddWithValue("at", committedAt);
        command.Parameters.AddWithValue("actor", serviceIdentityRef); command.Parameters.AddWithValue("period", periodId); command.Parameters.AddWithValue("open", OpenStatusId);
        if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
            throw new FiscalZReadingSafeException(FiscalZReadingOutcome.PeriodNotOpen,
                "The fiscal reporting period was no longer OPEN at durable close.");
    }

    private static Task InsertAuditAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalZReadingCommand command, ReportingCodeIds ids, Guid requestId, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO pos.fiscal_action_audit(fiscal_action_audit_id,site_pos_server_id,fiscal_report_request_id,audit_action_type_code_id,audit_result_code_id,actor_ref,service_identity_ref,correlation_ref,occurred_at,created_at) VALUES(@id,@site,@request,@action,@result,@actor,@service,@correlation,clock_timestamp(),clock_timestamp())", ct, p =>
        { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("site", command.SitePosServerId); p.AddWithValue("request", requestId); p.AddWithValue("action", ids.Get("fiscal_report_kind", "z_reading")); p.AddWithValue("result", ids.Get("fiscal_action_audit_result", "success")); p.AddWithValue("actor", command.RequestedByRef); p.AddWithValue("service", command.ServiceIdentityRef); p.AddWithValue("correlation", command.CorrelationId); });

    private static async Task<FiscalZReadingRecord?> ReadRecordAsync(NpgsqlConnection c, NpgsqlTransaction? t, string reference, CancellationToken ct)
    {
        const string sql = """
        SELECT r.x_z_report_id,r.report_number,r.fiscal_report_request_id,req.operation_idempotency_key,kind.code_key,
               contract.contract_key||':'||contract.contract_version,req.semantic_hash_version,r.site_pos_server_id,
               r.fiscal_identity_id,r.fiscal_reporting_period_id,period.expected_prior_period_id,r.business_day_date,
               r.period_start_at,r.period_end_at,r.reporting_timezone_name,r.business_day_cutoff_local_time,r.currency_code,
               r.generated_at,r.committed_at,period.closed_at,r.transaction_count,r.gross_sales_amount_minor_units,
               r.net_sales_amount_minor_units,r.vatable_sales_amount_minor_units,r.vat_amount_minor_units,
               r.vat_exempt_sales_amount_minor_units,r.zero_rated_sales_amount_minor_units,r.discount_amount_minor_units,
               r.senior_citizen_discount_amount_minor_units,r.pwd_discount_amount_minor_units,r.other_statutory_discount_amount_minor_units,
               r.vat_exemption_amount_minor_units,r.coupon_discount_amount_minor_units,r.promotional_discount_amount_minor_units,
               r.void_amount_minor_units,r.refund_amount_minor_units,r.return_amount_minor_units,r.adjustment_amount_minor_units,
               r.service_charge_amount_minor_units,s.previous_reset_counter_value,s.resulting_reset_counter_value,
               s.previous_z_counter_value,s.resulting_z_counter_value,s.previous_grand_total_amount_minor_units,
               s.current_period_amount_minor_units,s.resulting_grand_total_amount_minor_units,s.expected_state_version,
               s.resulting_state_version,status.code_key,period.period_sequence,request_status.code_key,
               COALESCE(a.correlation_ref,'unavailable')
        FROM pos.x_z_reports r
        JOIN pos.fiscal_report_requests req ON req.fiscal_report_request_id=r.fiscal_report_request_id
        JOIN pos.controlled_codes kind ON kind.controlled_code_id=r.report_kind_code_id
        JOIN pos.fiscal_reporting_contract_versions contract ON contract.fiscal_reporting_contract_version_id=r.fiscal_reporting_contract_version_id
        JOIN pos.fiscal_reporting_periods period ON period.fiscal_reporting_period_id=r.fiscal_reporting_period_id
        JOIN pos.controlled_codes status ON status.controlled_code_id=period.period_status_code_id
        JOIN pos.controlled_codes request_status ON request_status.controlled_code_id=req.report_status_code_id
        JOIN pos.fiscal_z_counter_snapshots s ON s.x_z_report_id=r.x_z_report_id
        LEFT JOIN LATERAL(SELECT correlation_ref FROM pos.fiscal_action_audit WHERE fiscal_report_request_id=req.fiscal_report_request_id ORDER BY occurred_at LIMIT 1)a ON true
        WHERE r.report_number=@reference AND kind.code_key='z_reading';
        """;
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("reference", reference);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        var id = reader.GetGuid(0);
        var amounts = new FiscalXReadingAmounts(reader.GetInt64(21), reader.GetInt64(22), reader.GetInt64(23), reader.GetInt64(24), reader.GetInt64(25), reader.GetInt64(26), reader.GetInt64(27), reader.GetInt64(28), reader.GetInt64(29), reader.GetInt64(30), reader.GetInt64(31), reader.GetInt64(32), reader.GetInt64(33), reader.GetInt64(34), reader.GetInt64(35), reader.GetInt64(36), reader.GetInt64(37), reader.GetInt64(38));
        var counter = new FiscalZReadingCounterSnapshot(reader.GetInt64(39), reader.GetInt64(40), reader.GetInt64(41), reader.GetInt64(42), reader.GetInt64(43), reader.GetInt64(44), reader.GetInt64(45), reader.GetInt64(46), reader.GetInt64(47));
        var head = new { Reference=reader.GetString(1),Request=reader.GetGuid(2),Operation=reader.GetString(3),Kind=reader.GetString(4).ToUpperInvariant(),Contract=reader.GetString(5),HashVersion=reader.GetString(6),Site=reader.GetGuid(7),Identity=reader.GetGuid(8),Period=reader.GetGuid(9),Prior=reader.IsDBNull(10)?(Guid?)null:reader.GetGuid(10),Business=reader.GetFieldValue<DateOnly>(11),Start=reader.GetFieldValue<DateTimeOffset>(12),End=reader.GetFieldValue<DateTimeOffset>(13),Timezone=reader.GetString(14),Cutoff=reader.GetFieldValue<TimeOnly>(15),Currency=reader.GetString(16),Generated=reader.GetFieldValue<DateTimeOffset>(17),Committed=reader.GetFieldValue<DateTimeOffset>(18),Closed=reader.GetFieldValue<DateTimeOffset>(19),Count=reader.GetInt64(20),Status=reader.GetString(48).ToUpperInvariant(),PeriodSequence=reader.GetInt64(49),ReportStatus=reader.GetString(50).ToUpperInvariant(),Correlation=reader.GetString(51) };
        await reader.DisposeAsync().ConfigureAwait(false);
        var tenders=await ReadTenderChildrenAsync(c,t,id,ct).ConfigureAwait(false); var discounts=await ReadDiscountChildrenAsync(c,t,id,ct).ConfigureAwait(false); var ranges=await ReadRangeChildrenAsync(c,t,id,ct).ConfigureAwait(false);
        return new(id,head.Reference,head.Request,head.Operation,head.Kind,head.Contract,head.HashVersion,head.Site,head.Identity,head.Period,head.Prior,head.Business,head.Start,head.End,head.Timezone,head.Cutoff,head.Currency,head.Generated,head.Committed,head.Closed,head.Count,amounts,tenders,discounts,ranges,counter,head.Status,head.Correlation,head.Reference,true,head.PeriodSequence,head.ReportStatus);
    }

    private static async Task<IReadOnlyList<FiscalXReadingTenderBreakdown>> ReadTenderChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct) { const string sql="SELECT code.code_key,x.tender_transaction_count,x.amount_minor_units,x.currency_code FROM pos.fiscal_report_tender_breakdowns x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tender_classification_code_id WHERE x.x_z_report_id=@id ORDER BY code.sort_order,code.code_key"; await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var result=new List<FiscalXReadingTenderBreakdown>();while(await r.ReadAsync(ct).ConfigureAwait(false))result.Add(new(r.GetString(0),r.GetInt64(1),r.GetInt64(2),r.GetString(3)));return result; }
    private static async Task<IReadOnlyList<FiscalXReadingDiscountBreakdown>> ReadDiscountChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct) { const string sql="SELECT code.code_key,x.qualifying_document_count,x.discount_amount_minor_units,x.vat_exemption_amount_minor_units,x.currency_code FROM pos.fiscal_report_discount_breakdowns x JOIN pos.controlled_codes code ON code.controlled_code_id=x.discount_classification_code_id WHERE x.x_z_report_id=@id ORDER BY code.sort_order,code.code_key"; await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var result=new List<FiscalXReadingDiscountBreakdown>();while(await r.ReadAsync(ct).ConfigureAwait(false))result.Add(new(r.GetString(0),r.GetInt64(1),r.GetInt64(2),r.GetInt64(3),r.GetString(4)));return result; }
    private static async Task<IReadOnlyList<FiscalXReadingFiscalNumberRange>> ReadRangeChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct) { const string sql="SELECT fiscal_report_fiscal_number_range_id,fiscal_sequence_policy_id,fiscal_series,first_sequence_value,last_sequence_value,first_fiscal_number,last_fiscal_number,qualifying_document_count,currency_code FROM pos.fiscal_report_fiscal_number_ranges WHERE x_z_report_id=@id ORDER BY fiscal_series,first_sequence_value";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var raw=new List<(Guid,Guid,string,long,long,string,string,long,string)>();while(await r.ReadAsync(ct).ConfigureAwait(false))raw.Add((r.GetGuid(0),r.GetGuid(1),r.GetString(2),r.GetInt64(3),r.GetInt64(4),r.GetString(5),r.GetString(6),r.GetInt64(7),r.GetString(8)));await r.DisposeAsync().ConfigureAwait(false);var result=new List<FiscalXReadingFiscalNumberRange>();foreach(var item in raw)result.Add(new(item.Item2,item.Item3,item.Item4,item.Item5,item.Item6,item.Item7,item.Item8,await ReadGapChildrenAsync(c,t,item.Item1,ct).ConfigureAwait(false),item.Item9));return result; }
    private static async Task<IReadOnlyList<FiscalXReadingSequenceGap>> ReadGapChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct) { const string sql="SELECT x.gap_sequence_value,code.code_key FROM pos.fiscal_report_sequence_gaps x JOIN pos.controlled_codes code ON code.controlled_code_id=x.gap_classification_code_id WHERE x.fiscal_report_fiscal_number_range_id=@id ORDER BY x.gap_sequence_value";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var result=new List<FiscalXReadingSequenceGap>();while(await r.ReadAsync(ct).ConfigureAwait(false))result.Add(new(r.GetInt64(0),r.GetString(1)));return result; }

    private static FiscalZReadingOutcome MapBoundaryError(FiscalCloseBoundaryErrorCode code) => code switch
    {
        FiscalCloseBoundaryErrorCode.StateInitializationRequired => FiscalZReadingOutcome.StateInitializationRequired,
        FiscalCloseBoundaryErrorCode.StaleStateVersion => FiscalZReadingOutcome.StaleStateVersion,
        FiscalCloseBoundaryErrorCode.PriorPeriodSequenceInvalid => FiscalZReadingOutcome.PriorPeriodUnresolved,
        FiscalCloseBoundaryErrorCode.LockTimeout => FiscalZReadingOutcome.LockTimeout,
        FiscalCloseBoundaryErrorCode.RetryableConcurrencyFailure => FiscalZReadingOutcome.RetryableConcurrencyFailure,
        FiscalCloseBoundaryErrorCode.UnsupportedCrossPeriodMutation => FiscalZReadingOutcome.UnsupportedCrossPeriodMutation,
        _ => FiscalZReadingOutcome.PeriodUnavailable
    };

    private static FiscalZReadingResult Failure(FiscalZReadingOutcome outcome, string message) => new(outcome, SafeMessage: message);
    private static void AddPeriodParameters(NpgsqlCommand c,FiscalZReadingPeriod p) { c.Parameters.AddWithValue("period",p.FiscalReportingPeriodId);c.Parameters.AddWithValue("site",p.SitePosServerId);c.Parameters.AddWithValue("identity",p.FiscalIdentityId);c.Parameters.AddWithValue("currency",p.CurrencyCode);c.Parameters.AddWithValue("start",p.PeriodStartAt);c.Parameters.AddWithValue("end",p.PeriodEndAt); }
    private static void AddAmountParameters(NpgsqlParameterCollection p,FiscalXReadingAmounts a) { p.AddWithValue("gross",a.GrossSalesAmountMinorUnits);p.AddWithValue("net",a.NetSalesAmountMinorUnits);p.AddWithValue("vatable",a.VatableSalesAmountMinorUnits);p.AddWithValue("vat",a.VatAmountMinorUnits);p.AddWithValue("vat_exempt",a.VatExemptSalesAmountMinorUnits);p.AddWithValue("zero_rated",a.ZeroRatedSalesAmountMinorUnits);p.AddWithValue("discount",a.DiscountAmountMinorUnits);p.AddWithValue("senior",a.SeniorCitizenDiscountAmountMinorUnits);p.AddWithValue("pwd",a.PwdDiscountAmountMinorUnits);p.AddWithValue("other_stat",a.OtherStatutoryDiscountAmountMinorUnits);p.AddWithValue("vat_exemption",a.VatExemptionAmountMinorUnits);p.AddWithValue("coupon",a.CouponDiscountAmountMinorUnits);p.AddWithValue("promo",a.PromotionalDiscountAmountMinorUnits);p.AddWithValue("void",a.VoidAmountMinorUnits); }
    private static void AddNullable(NpgsqlParameterCollection p,string name,object? value,NpgsqlDbType type) { p.Add(new NpgsqlParameter(name,type){Value=value??DBNull.Value}); }
    private static NpgsqlCommand CreateCommand(NpgsqlConnection c,NpgsqlTransaction? t,string sql) { var command=c.CreateCommand();command.CommandText=sql;command.Transaction=t;return command; }
    private static async Task ExecuteAsync(NpgsqlConnection c,NpgsqlTransaction? t,string sql,CancellationToken ct,Action<NpgsqlParameterCollection>? add=null) { await using var command=CreateCommand(c,t,sql);add?.Invoke(command.Parameters);await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async Task<DateTimeOffset> ClockAsync(NpgsqlConnection c,NpgsqlTransaction t,CancellationToken ct) { await using var command=CreateCommand(c,t,"SELECT clock_timestamp()");var value=await command.ExecuteScalarAsync(ct).ConfigureAwait(false);return value switch { DateTimeOffset timestamp=>timestamp,DateTime timestamp=>new DateTimeOffset(DateTime.SpecifyKind(timestamp,DateTimeKind.Utc)),_=>throw new InvalidOperationException("PostgreSQL did not return a supported timestamp value.")}; }

    private sealed class SourceBuilder(Guid id,string type,string status,DateTimeOffset created,Guid? policy,long? sequence,string? number,string? series,string completionBasis)
    { public List<FiscalXReadingSourceLine> Lines{get;}=[];public List<FiscalXReadingSourceTender>Tenders{get;}=[];public List<FiscalXReadingSourceTax>Taxes{get;}=[];public List<FiscalXReadingSourceDiscount>Discounts{get;}=[];public FiscalXReadingSourceStatutory? Statutory{get;set;}public FiscalXReadingSourceDocument Build()=>new(id,type,status,created,policy,sequence,number,series,Lines,Tenders,Taxes,Discounts,Statutory,completionBasis); }
    private sealed record ReportingCodeIds(Dictionary<(string,string),Guid> Values) { public Guid Get(string set,string key)=>Values[(set,key)]; }
}
