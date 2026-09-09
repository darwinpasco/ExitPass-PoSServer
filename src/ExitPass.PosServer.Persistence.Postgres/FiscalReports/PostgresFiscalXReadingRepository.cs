using System.Data;
using ExitPass.PosServer.Runtime.FiscalReports;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresFiscalXReadingRepository(
    NpgsqlDataSource dataSource,
    FiscalXReadingAggregationService aggregationService) : IFiscalXReadingRepository
{
    public async Task<FiscalXReadingResult> GenerateAsync(FiscalXReadingCommand command, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var lockKey = $"x-reading:{command.SitePosServerId:D}:{command.OperationKey}";
        await ExecuteAsync(connection, null, "SELECT pg_advisory_lock(hashtextextended(@lock_key, 0))", cancellationToken,
            parameter => parameter.AddWithValue("lock_key", lockKey)).ConfigureAwait(false);

        try
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);
            var committed = false;
            try
            {
                var prerequisite = await ValidateScopeAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
                if (prerequisite is not null) return prerequisite;

                var periods = await ResolveOpenPeriodsAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
                if (periods.Count == 0) return new(FiscalXReadingOutcome.OpenPeriodUnavailable, SafeMessage: "No governed OPEN reporting period is available for the requested scope and observation time.");
                if (periods.Count > 1) return new(FiscalXReadingOutcome.AmbiguousOpenPeriod, SafeMessage: "More than one governed OPEN reporting period matches the requested scope.");
                var period = periods[0];
                var semanticHash = FiscalXReadingSemanticRequestHasher.Compute(command, period);

                var existing = await ReadOperationAsync(connection, transaction, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
                if (existing is not null)
                {
                    if (!string.Equals(existing.Value.SemanticHash, semanticHash, StringComparison.Ordinal))
                        return new(FiscalXReadingOutcome.Conflict, SafeMessage: "The X Reading operation key is already bound to different governed request semantics.");
                    var replay = await ReadRecordAsync(connection, transaction, existing.Value.ReportReference, cancellationToken).ConfigureAwait(false);
                    return replay is null
                        ? new(FiscalXReadingOutcome.UnknownCommitOutcome, SafeMessage: "The existing X Reading operation requires durable-state reconciliation.")
                        : new(FiscalXReadingOutcome.Replayed, replay);
                }

                var effectiveEnd = command.ObservedAt < period.PeriodEndAt ? command.ObservedAt : period.PeriodEndAt;
                var sourceDocuments = effectiveEnd <= period.PeriodStartAt
                    ? []
                    : await ReadSourceDocumentsAsync(connection, transaction, period, effectiveEnd, cancellationToken).ConfigureAwait(false);
                var sourceGaps = await ReadSourceGapsAsync(connection, transaction, sourceDocuments, cancellationToken).ConfigureAwait(false);
                var aggregate = aggregationService.Aggregate(sourceDocuments, sourceGaps, period.CurrencyCode);

                var identifiers = await ResolveReportingCodeIdsAsync(connection, transaction, aggregate, cancellationToken).ConfigureAwait(false);
                var requestId = Guid.NewGuid();
                var reportId = Guid.NewGuid();
                var reportReference = $"X-{period.BusinessDayDate:yyyyMMdd}-{reportId:N}".ToUpperInvariant();
                var committedAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);

                await InsertRequestAsync(connection, transaction, command, period, identifiers, requestId, semanticHash, cancellationToken).ConfigureAwait(false);
                await InsertScopeAsync(connection, transaction, period, identifiers, requestId, cancellationToken).ConfigureAwait(false);
                await InsertSnapshotAsync(connection, transaction, command, period, aggregate, identifiers, requestId, reportId, reportReference, committedAt, cancellationToken).ConfigureAwait(false);
                await InsertChildrenAsync(connection, transaction, period.FiscalIdentityId, aggregate, identifiers, reportId, cancellationToken).ConfigureAwait(false);
                await PostgresElectronicJournalWriter.AppendAsync(
                    connection,
                    transaction,
                    new ElectronicJournalAppendRequest(
                        period.SitePosServerId,
                        period.FiscalIdentityId,
                        period.CurrencyCode,
                        period.FiscalReportingPeriodId,
                        "x_reading_committed",
                        $"fiscal-report-request:{requestId:D}",
                        FiscalXReadingContract.ContractVersion,
                        command.ObservedAt,
                        command.RequestedByRef,
                        command.ServiceIdentityRef,
                        command.CorrelationId,
                        ElectronicJournalReportFacts.FromX(aggregate, reportReference),
                        FiscalReportRequestId: requestId,
                        XZReportId: reportId,
                        BusinessDayDate: period.BusinessDayDate,
                        IdempotencyReference: command.OperationKey),
                    cancellationToken).ConfigureAwait(false);
                await InsertAuditAsync(connection, transaction, command, identifiers, requestId, reportReference, cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                committed = true;
                var record = await ReadRecordAsync(connection, null, reportReference, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Committed X Reading could not be read back.");
                return new(FiscalXReadingOutcome.Created, record);
            }
            catch (FiscalXReadingSafeException exception)
            {
                if (!committed) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new(exception.Outcome, SafeMessage: exception.Message);
            }
            catch (OverflowException)
            {
                if (!committed) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new(FiscalXReadingOutcome.ArithmeticOverflow, SafeMessage: "X Reading arithmetic exceeded supported minor-unit bounds.");
            }
            catch (PostgresException)
            {
                if (!committed) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return committed
                    ? new(FiscalXReadingOutcome.UnknownCommitOutcome, SafeMessage: "The X Reading commit outcome requires durable-state reconciliation.")
                    : new(FiscalXReadingOutcome.PersistenceFailure, SafeMessage: "The X Reading could not be committed.");
            }
            catch (NpgsqlException)
            {
                if (!committed) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return committed
                    ? new(FiscalXReadingOutcome.UnknownCommitOutcome, SafeMessage: "The X Reading commit outcome requires durable-state reconciliation.")
                    : new(FiscalXReadingOutcome.PersistenceFailure, SafeMessage: "The X Reading database operation failed safely.");
            }
            catch (InvalidOperationException)
            {
                if (!committed) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return committed
                    ? new(FiscalXReadingOutcome.UnknownCommitOutcome, SafeMessage: "The X Reading commit outcome requires durable-state reconciliation.")
                    : new(FiscalXReadingOutcome.PersistenceFailure, SafeMessage: "The X Reading persistence operation failed safely.");
            }
        }
        finally
        {
            await ExecuteAsync(connection, null, "SELECT pg_advisory_unlock(hashtextextended(@lock_key, 0))", CancellationToken.None,
                parameter => parameter.AddWithValue("lock_key", lockKey)).ConfigureAwait(false);
        }
    }

    public async Task<FiscalXReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var record = await ReadRecordAsync(connection, null, fiscalReportReference, cancellationToken).ConfigureAwait(false);
            return record is null ? new(FiscalXReadingOutcome.NotFound) : new(FiscalXReadingOutcome.Replayed, record);
        }
        catch (NpgsqlException)
        {
            return new(FiscalXReadingOutcome.PersistenceFailure, SafeMessage: "The X Reading read operation failed safely.");
        }
    }

    private static async Task<FiscalXReadingResult?> ValidateScopeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, FiscalXReadingCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.reporting_timezone_name, s.business_day_cutoff_local_time, s.is_active,
                   EXISTS (SELECT 1 FROM pos.fiscal_identities f WHERE f.fiscal_identity_id = @fiscal_identity_id AND f.is_active)
            FROM pos.site_pos_servers s
            WHERE s.site_pos_server_id = @site_pos_server_id;
            """;
        await using var dbCommand = CreateCommand(connection, transaction, sql);
        dbCommand.Parameters.AddWithValue("site_pos_server_id", command.SitePosServerId);
        dbCommand.Parameters.AddWithValue("fiscal_identity_id", command.FiscalIdentityId);
        await using var reader = await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return new(FiscalXReadingOutcome.SitePosServerNotFound, SafeMessage: "Site POS Server was not found.");
        if (!reader.GetBoolean(2)) return new(FiscalXReadingOutcome.ReportingConfigurationUnavailable, SafeMessage: "Site POS Server is not active for fiscal reporting.");
        if (!reader.GetBoolean(3)) return new(FiscalXReadingOutcome.FiscalIdentityNotFound, SafeMessage: "Fiscal Identity was not found or is not active.");
        if (reader.IsDBNull(0) || reader.IsDBNull(1) || string.IsNullOrWhiteSpace(reader.GetString(0)))
            return new(FiscalXReadingOutcome.ReportingConfigurationUnavailable, SafeMessage: "Site POS Server reporting timezone and cutoff configuration are required.");
        return null;
    }

    private static async Task<List<FiscalXReadingPeriod>> ResolveOpenPeriodsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, FiscalXReadingCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.fiscal_reporting_period_id, p.fiscal_reporting_contract_version_id, p.site_pos_server_id,
                   p.fiscal_identity_id, p.business_day_date, p.period_start_at, p.period_end_at,
                   p.reporting_timezone_name, p.business_day_cutoff_local_time, p.currency_code, p.period_sequence
            FROM pos.fiscal_reporting_periods p
            JOIN pos.controlled_codes status ON status.controlled_code_id = p.period_status_code_id
            JOIN pos.controlled_code_sets status_set ON status_set.controlled_code_set_id = status.controlled_code_set_id
            JOIN pos.site_pos_servers site ON site.site_pos_server_id = p.site_pos_server_id
            JOIN pos.fiscal_reporting_contract_versions contract ON contract.fiscal_reporting_contract_version_id = p.fiscal_reporting_contract_version_id
            WHERE p.site_pos_server_id = @site_pos_server_id
              AND p.fiscal_identity_id = @fiscal_identity_id
              AND status_set.code_set_key = 'fiscal_reporting_period_status' AND status.code_key = 'open'
              AND p.period_start_at <= @observed_at
              AND p.reporting_timezone_name = site.reporting_timezone_name
              AND p.business_day_cutoff_local_time = site.business_day_cutoff_local_time
              AND contract.contract_key = 'pos-server-fiscal-reporting' AND contract.contract_version = 'v1'
              AND contract.semantic_hash_version = @semantic_hash_version AND contract.is_active
            ORDER BY p.period_sequence DESC
            LIMIT 2;
            """;
        await using var dbCommand = CreateCommand(connection, transaction, sql);
        dbCommand.Parameters.AddWithValue("site_pos_server_id", command.SitePosServerId);
        dbCommand.Parameters.AddWithValue("fiscal_identity_id", command.FiscalIdentityId);
        dbCommand.Parameters.AddWithValue("observed_at", command.ObservedAt);
        dbCommand.Parameters.AddWithValue("semantic_hash_version", FiscalXReadingContract.SemanticHashVersion);
        await using var reader = await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var periods = new List<FiscalXReadingPeriod>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            periods.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetFieldValue<DateOnly>(4),
                reader.GetFieldValue<DateTimeOffset>(5), reader.GetFieldValue<DateTimeOffset>(6), reader.GetString(7), reader.GetFieldValue<TimeOnly>(8),
                reader.GetString(9), reader.GetInt64(10)));
        }
        return periods;
    }

    private static async Task<(string SemanticHash, string ReportReference)?> ReadOperationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid sitePosServerId, string operationKey, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT request.semantic_request_hash, report.report_number
            FROM pos.fiscal_report_requests request
            LEFT JOIN pos.x_z_reports report ON report.fiscal_report_request_id = request.fiscal_report_request_id
            WHERE request.site_pos_server_id = @site_pos_server_id AND request.operation_idempotency_key = @operation_key;
            """;
        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.AddWithValue("site_pos_server_id", sitePosServerId); command.Parameters.AddWithValue("operation_key", operationKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return (reader.GetString(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
    }

    private static async Task<IReadOnlyList<FiscalXReadingSourceDocument>> ReadSourceDocumentsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, FiscalXReadingPeriod period, DateTimeOffset effectiveEnd, CancellationToken cancellationToken)
    {
        const string documentSql = """
            SELECT d.fiscal_document_id, type_code.code_key, status_code.code_key, d.created_at,
                   d.fiscal_sequence_policy_id, d.fiscal_sequence_value, d.fiscal_document_number, d.fiscal_series,
                   d.completion_basis
            FROM pos.fiscal_documents d
            JOIN pos.controlled_codes type_code ON type_code.controlled_code_id = d.fiscal_document_type_code_id
            JOIN pos.controlled_codes status_code ON status_code.controlled_code_id = d.fiscal_document_status_code_id
            WHERE d.site_pos_server_id = @site_pos_server_id AND d.fiscal_identity_id = @fiscal_identity_id
              AND d.created_at >= @period_start_at AND d.created_at < @effective_end
            ORDER BY d.created_at, d.fiscal_document_id;
            """;
        var documents = new Dictionary<Guid, SourceBuilder>();
        await using (var command = CreateCommand(connection, transaction, documentSql))
        {
            AddPeriodParameters(command, period, effectiveEnd);
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
        const string sql = "SELECT l.fiscal_document_id, code.code_key, l.gross_amount_minor_units, l.discount_amount_minor_units, l.tax_amount_minor_units, l.net_amount_minor_units, l.currency_code FROM pos.fiscal_document_lines l JOIN pos.controlled_codes code ON code.controlled_code_id=l.line_type_code_id WHERE l.fiscal_document_id = ANY(@ids) AND l.is_active ORDER BY l.fiscal_document_id,l.line_sequence";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray());
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Lines.Add(new(r.GetString(1), r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetInt64(5), r.GetString(6)));
    }

    private static async Task ReadTendersAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id, code.code_key, x.amount_minor_units, x.currency_code FROM pos.fiscal_tenders x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tender_type_code_id WHERE x.fiscal_document_id = ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_tender_id";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Tenders.Add(new(r.GetString(1), r.GetInt64(2), r.GetString(3)));
    }

    private static async Task ReadTaxesAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id, code.code_key, x.fiscal_document_line_id IS NULL, x.taxable_amount_minor_units, x.tax_amount_minor_units, x.currency_code FROM pos.fiscal_tax_details x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tax_classification_code_id WHERE x.fiscal_document_id = ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_tax_detail_id";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Taxes.Add(new(r.GetString(1), r.GetBoolean(2), r.GetInt64(3), r.GetInt64(4), r.GetString(5)));
    }

    private static async Task ReadDiscountsAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id, code.code_key, x.discount_amount_minor_units, x.vat_privilege_amount_minor_units, x.currency_code FROM pos.fiscal_discount_privilege_details x JOIN pos.controlled_codes code ON code.controlled_code_id=x.discount_privilege_type_code_id WHERE x.fiscal_document_id = ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_discount_privilege_detail_id";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Discounts.Add(new(r.GetString(1), r.GetInt64(2), r.GetInt64(3), r.GetString(4)));
    }

    private static async Task ReadStatutoryAsync(NpgsqlConnection c, NpgsqlTransaction t, Dictionary<Guid, SourceBuilder> documents, CancellationToken ct)
    {
        const string sql = "SELECT x.fiscal_document_id, entitlement.code_key, x.statutory_discount_amount_minor_units, x.vat_amount_minor_units, x.final_payable_amount_minor_units, x.currency_code FROM pos.fiscal_document_applied_statutory_facts x JOIN pos.controlled_codes entitlement ON entitlement.controlled_code_id=x.entitlement_type_code_id WHERE x.fiscal_document_id = ANY(@ids)";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", documents.Keys.ToArray()); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); while (await r.ReadAsync(ct).ConfigureAwait(false)) documents[r.GetGuid(0)].Statutory = new(r.GetString(1), r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetString(5));
    }

    private static async Task<IReadOnlyList<FiscalXReadingSourceGap>> ReadSourceGapsAsync(NpgsqlConnection c, NpgsqlTransaction t, IReadOnlyList<FiscalXReadingSourceDocument> documents, CancellationToken ct)
    {
        var policies = documents.Where(d => d.SequencePolicyId.HasValue).Select(d => d.SequencePolicyId!.Value).Distinct().ToArray(); if (policies.Length == 0) return [];
        const string sql = "SELECT g.fiscal_sequence_gap_audit_id,g.fiscal_sequence_policy_id,g.gap_sequence_value,code.code_key FROM pos.fiscal_sequence_gap_audit g JOIN pos.controlled_codes code ON code.controlled_code_id=g.gap_reason_code_id WHERE g.fiscal_sequence_policy_id=ANY(@ids) ORDER BY g.fiscal_sequence_policy_id,g.gap_sequence_value";
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("ids", policies); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); var result = new List<FiscalXReadingSourceGap>(); while (await r.ReadAsync(ct).ConfigureAwait(false)) result.Add(new(r.GetGuid(0), r.GetGuid(1), r.GetInt64(2), r.GetString(3))); return result;
    }

    private static async Task<ReportingCodeIds> ResolveReportingCodeIdsAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalXReadingAggregate aggregate, CancellationToken ct)
    {
        var required = new List<(string Set, string Key)> { ("fiscal_report_kind", "x_reading"), ("fiscal_report_status", "committed"), ("fiscal_report_scope_type", "site_pos_server_period"), ("fiscal_action_audit_result", "success") };
        required.AddRange(aggregate.Tenders.Select(x => ("fiscal_reporting_tender_classification", x.Classification)));
        required.AddRange(aggregate.Discounts.Select(x => ("fiscal_reporting_discount_classification", x.Classification)));
        required.AddRange(aggregate.FiscalNumberRanges.SelectMany(x => x.Gaps).Select(x => ("fiscal_sequence_gap_classification", x.Classification)));
        var result = new Dictionary<(string, string), Guid>();
        const string sql = "SELECT code.controlled_code_id FROM pos.controlled_codes code JOIN pos.controlled_code_sets set_ ON set_.controlled_code_set_id=code.controlled_code_set_id WHERE set_.code_set_key=@set AND code.code_key=@key AND set_.is_active AND code.is_active";
        foreach (var pair in required.Distinct()) { await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("set", pair.Set); cmd.Parameters.AddWithValue("key", pair.Key); var value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false); if (value is not Guid id) throw new FiscalXReadingSafeException(FiscalXReadingOutcome.UnsupportedSourceClassification, "A required governed reporting classification is unavailable."); result[(pair.Set, pair.Key)] = id; }
        return new(result);
    }

    private static async Task InsertRequestAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalXReadingCommand command, FiscalXReadingPeriod period, ReportingCodeIds ids, Guid requestId, string hash, CancellationToken ct)
    {
        const string sql = "INSERT INTO pos.fiscal_report_requests(fiscal_report_request_id,fiscal_reporting_contract_version_id,fiscal_reporting_period_id,site_pos_server_id,report_type_code_id,report_status_code_id,operation_idempotency_key,semantic_request_hash,semantic_hash_version,business_day_date,requested_at,requested_by_ref,service_identity_ref,created_at,updated_at) VALUES(@id,@contract,@period,@site,@kind,@status,@operation,@hash,@version,@business_date,@requested_at,@actor,@service,@requested_at,@requested_at)";
        await ExecuteAsync(c, t, sql, ct, p => { p.AddWithValue("id", requestId); p.AddWithValue("contract", period.FiscalReportingContractVersionId); p.AddWithValue("period", period.FiscalReportingPeriodId); p.AddWithValue("site", period.SitePosServerId); p.AddWithValue("kind", ids.Get("fiscal_report_kind", "x_reading")); p.AddWithValue("status", ids.Get("fiscal_report_status", "committed")); p.AddWithValue("operation", command.OperationKey); p.AddWithValue("hash", hash); p.AddWithValue("version", FiscalXReadingContract.SemanticHashVersion); p.AddWithValue("business_date", period.BusinessDayDate); p.AddWithValue("requested_at", command.ObservedAt); p.AddWithValue("actor", command.RequestedByRef); p.AddWithValue("service", command.ServiceIdentityRef); }).ConfigureAwait(false);
    }

    private static Task InsertScopeAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalXReadingPeriod p, ReportingCodeIds ids, Guid requestId, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_scopes(fiscal_report_scope_id,fiscal_report_request_id,fiscal_reporting_period_id,scope_type_code_id,created_at) VALUES(@id,@request,@period,@scope,clock_timestamp())", ct, x => { x.AddWithValue("id", Guid.NewGuid()); x.AddWithValue("request", requestId); x.AddWithValue("period", p.FiscalReportingPeriodId); x.AddWithValue("scope", ids.Get("fiscal_report_scope_type", "site_pos_server_period")); });

    private static async Task InsertSnapshotAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalXReadingCommand command, FiscalXReadingPeriod p, FiscalXReadingAggregate a, ReportingCodeIds ids, Guid requestId, Guid reportId, string reference, DateTimeOffset committedAt, CancellationToken ct)
    {
        const string sql = """
        INSERT INTO pos.x_z_reports(x_z_report_id,fiscal_report_request_id,fiscal_reporting_contract_version_id,fiscal_reporting_period_id,report_kind_code_id,site_pos_server_id,fiscal_identity_id,report_number,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,transaction_count,begin_fiscal_document_id,end_fiscal_document_id,beginning_si_ref,ending_si_ref,first_fiscal_sequence_value,last_fiscal_sequence_value,fiscal_sequence_gap_count,reset_counter_value,z_counter_value,previous_grand_total_amount_minor_units,current_grand_total_amount_minor_units,present_grand_total_amount_minor_units,gross_sales_amount_minor_units,net_sales_amount_minor_units,vatable_sales_amount_minor_units,vat_amount_minor_units,vat_exempt_sales_amount_minor_units,zero_rated_sales_amount_minor_units,discount_amount_minor_units,senior_citizen_discount_amount_minor_units,pwd_discount_amount_minor_units,other_statutory_discount_amount_minor_units,vat_exemption_amount_minor_units,coupon_discount_amount_minor_units,promotional_discount_amount_minor_units,void_amount_minor_units,refund_amount_minor_units,return_amount_minor_units,adjustment_amount_minor_units,service_charge_amount_minor_units,currency_code,generated_at,committed_at,created_at,updated_at)
        VALUES(@id,@request,@contract,@period,@kind,@site,@identity,@reference,@business_date,@start,@end,@timezone,@cutoff,@count,@begin_doc,@end_doc,@begin_ref,@end_ref,@first_seq,@last_seq,@gap_count,NULL,NULL,0,0,0,@gross,@net,@vatable,@vat,@vat_exempt,@zero_rated,@discount,@senior,@pwd,@other_stat,@vat_exemption,@coupon,@promo,@void,0,0,0,0,@currency,@generated,@committed,@committed,@committed)
        """;
        await ExecuteAsync(c, t, sql, ct, x => { x.AddWithValue("id", reportId); x.AddWithValue("request", requestId); x.AddWithValue("contract", p.FiscalReportingContractVersionId); x.AddWithValue("period", p.FiscalReportingPeriodId); x.AddWithValue("kind", ids.Get("fiscal_report_kind", "x_reading")); x.AddWithValue("site", p.SitePosServerId); x.AddWithValue("identity", p.FiscalIdentityId); x.AddWithValue("reference", reference); x.AddWithValue("business_date", p.BusinessDayDate); x.AddWithValue("start", p.PeriodStartAt); x.AddWithValue("end", p.PeriodEndAt); x.AddWithValue("timezone", p.ReportingTimezoneName); x.AddWithValue("cutoff", p.BusinessDayCutoffLocalTime); x.AddWithValue("count", a.QualifyingDocumentCount); AddNullable(x, "begin_doc", a.BeginFiscalDocumentId, NpgsqlDbType.Uuid); AddNullable(x, "end_doc", a.EndFiscalDocumentId, NpgsqlDbType.Uuid); AddNullable(x, "begin_ref", a.BeginningFiscalNumber, NpgsqlDbType.Text); AddNullable(x, "end_ref", a.EndingFiscalNumber, NpgsqlDbType.Text); AddNullable(x, "first_seq", a.FirstSequenceValue, NpgsqlDbType.Bigint); AddNullable(x, "last_seq", a.LastSequenceValue, NpgsqlDbType.Bigint); x.AddWithValue("gap_count", a.FiscalNumberRanges.Sum(r => (long)r.Gaps.Count)); AddAmountParameters(x, a.Amounts); x.AddWithValue("currency", p.CurrencyCode); x.AddWithValue("generated", command.ObservedAt); x.AddWithValue("committed", committedAt); }).ConfigureAwait(false);
    }

    private static async Task InsertChildrenAsync(NpgsqlConnection c, NpgsqlTransaction t, Guid fiscalIdentityId, FiscalXReadingAggregate a, ReportingCodeIds ids, Guid reportId, CancellationToken ct)
    {
        foreach (var item in a.Tenders) await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_tender_breakdowns VALUES(@id,@report,@class,@count,@amount,@currency,clock_timestamp())", ct, p => { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("report", reportId); p.AddWithValue("class", ids.Get("fiscal_reporting_tender_classification", item.Classification)); p.AddWithValue("count", item.TransactionCount); p.AddWithValue("amount", item.AmountMinorUnits); p.AddWithValue("currency", item.CurrencyCode); }).ConfigureAwait(false);
        foreach (var item in a.Discounts) await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_discount_breakdowns VALUES(@id,@report,@class,@count,@discount,@vat,@currency,clock_timestamp())", ct, p => { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("report", reportId); p.AddWithValue("class", ids.Get("fiscal_reporting_discount_classification", item.Classification)); p.AddWithValue("count", item.QualifyingDocumentCount); p.AddWithValue("discount", item.DiscountAmountMinorUnits); p.AddWithValue("vat", item.VatExemptionAmountMinorUnits); p.AddWithValue("currency", item.CurrencyCode); }).ConfigureAwait(false);
        foreach (var range in a.FiscalNumberRanges)
        {
            var rangeId = Guid.NewGuid(); await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_fiscal_number_ranges VALUES(@id,@report,@identity,@policy,@series,@first,@last,@first_no,@last_no,@count,@gaps,@currency,clock_timestamp())", ct, p => { p.AddWithValue("id", rangeId); p.AddWithValue("report", reportId); p.AddWithValue("identity", fiscalIdentityId); p.AddWithValue("policy", range.FiscalSequencePolicyId); p.AddWithValue("series", range.FiscalSeries); p.AddWithValue("first", range.FirstSequenceValue); p.AddWithValue("last", range.LastSequenceValue); p.AddWithValue("first_no", range.FirstFiscalNumber); p.AddWithValue("last_no", range.LastFiscalNumber); p.AddWithValue("count", range.QualifyingDocumentCount); p.AddWithValue("gaps", (long)range.Gaps.Count); p.AddWithValue("currency", range.CurrencyCode); }).ConfigureAwait(false);
            foreach (var gap in range.Gaps)
            {
                var sourceGap = FindSourceGap(a, range, gap);
                await ExecuteAsync(c, t, "INSERT INTO pos.fiscal_report_sequence_gaps(fiscal_report_sequence_gap_id,fiscal_report_fiscal_number_range_id,gap_sequence_value,gap_classification_code_id,source_sequence_gap_audit_id,created_at) VALUES(@id,@range,@sequence,@classification,@source,clock_timestamp())", ct, p => { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("range", rangeId); p.AddWithValue("sequence", gap.SequenceValue); p.AddWithValue("classification", ids.Get("fiscal_sequence_gap_classification", gap.Classification)); AddNullable(p, "source", sourceGap, NpgsqlDbType.Uuid); }).ConfigureAwait(false);
            }
        }
    }

    private static Task InsertAuditAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalXReadingCommand command, ReportingCodeIds ids, Guid requestId, string reportReference, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO pos.fiscal_action_audit(fiscal_action_audit_id,site_pos_server_id,fiscal_report_request_id,audit_action_type_code_id,audit_result_code_id,actor_ref,service_identity_ref,correlation_ref,occurred_at,created_at) VALUES(@id,@site,@request,@action,@result,@actor,@service,@correlation,clock_timestamp(),clock_timestamp())", ct, p => { p.AddWithValue("id", Guid.NewGuid()); p.AddWithValue("site", command.SitePosServerId); p.AddWithValue("request", requestId); p.AddWithValue("action", ids.Get("fiscal_report_kind", "x_reading")); p.AddWithValue("result", ids.Get("fiscal_action_audit_result", "success")); p.AddWithValue("actor", command.RequestedByRef); p.AddWithValue("service", command.ServiceIdentityRef); p.AddWithValue("correlation", command.CorrelationId); });

    private static async Task<FiscalXReadingRecord?> ReadRecordAsync(NpgsqlConnection c, NpgsqlTransaction? t, string reference, CancellationToken ct)
    {
        const string sql = """
        SELECT r.x_z_report_id,r.report_number,r.fiscal_report_request_id,req.operation_idempotency_key,kind.code_key,contract.contract_key||':'||contract.contract_version,req.semantic_hash_version,r.site_pos_server_id,r.fiscal_identity_id,r.fiscal_reporting_period_id,r.business_day_date,r.period_start_at,r.period_end_at,r.reporting_timezone_name,r.business_day_cutoff_local_time,r.currency_code,r.generated_at,r.committed_at,r.transaction_count,r.gross_sales_amount_minor_units,r.net_sales_amount_minor_units,r.vatable_sales_amount_minor_units,r.vat_amount_minor_units,r.vat_exempt_sales_amount_minor_units,r.zero_rated_sales_amount_minor_units,r.discount_amount_minor_units,r.senior_citizen_discount_amount_minor_units,r.pwd_discount_amount_minor_units,r.other_statutory_discount_amount_minor_units,r.vat_exemption_amount_minor_units,r.coupon_discount_amount_minor_units,r.promotional_discount_amount_minor_units,r.void_amount_minor_units,r.refund_amount_minor_units,r.return_amount_minor_units,r.adjustment_amount_minor_units,r.service_charge_amount_minor_units,period.period_sequence,request_status.code_key,COALESCE(a.correlation_ref,'unavailable')
        FROM pos.x_z_reports r JOIN pos.fiscal_report_requests req ON req.fiscal_report_request_id=r.fiscal_report_request_id JOIN pos.controlled_codes kind ON kind.controlled_code_id=r.report_kind_code_id JOIN pos.controlled_codes request_status ON request_status.controlled_code_id=req.report_status_code_id JOIN pos.fiscal_reporting_contract_versions contract ON contract.fiscal_reporting_contract_version_id=r.fiscal_reporting_contract_version_id JOIN pos.fiscal_reporting_periods period ON period.fiscal_reporting_period_id=r.fiscal_reporting_period_id LEFT JOIN LATERAL(SELECT correlation_ref FROM pos.fiscal_action_audit WHERE fiscal_report_request_id=req.fiscal_report_request_id ORDER BY occurred_at LIMIT 1)a ON true WHERE r.report_number=@reference AND kind.code_key='x_reading'
        """;
        await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("reference", reference); await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        var id = reader.GetGuid(0); var amounts = new FiscalXReadingAmounts(reader.GetInt64(19), reader.GetInt64(20), reader.GetInt64(21), reader.GetInt64(22), reader.GetInt64(23), reader.GetInt64(24), reader.GetInt64(25), reader.GetInt64(26), reader.GetInt64(27), reader.GetInt64(28), reader.GetInt64(29), reader.GetInt64(30), reader.GetInt64(31), reader.GetInt64(32), reader.GetInt64(33), reader.GetInt64(34), reader.GetInt64(35), reader.GetInt64(36));
        var head = new { Id = id, Reference = reader.GetString(1), Request = reader.GetGuid(2), Operation = reader.GetString(3), Kind = reader.GetString(4).ToUpperInvariant(), Contract = reader.GetString(5), HashVersion = reader.GetString(6), Site = reader.GetGuid(7), Identity = reader.GetGuid(8), Period = reader.GetGuid(9), Business = reader.GetFieldValue<DateOnly>(10), Start = reader.GetFieldValue<DateTimeOffset>(11), End = reader.GetFieldValue<DateTimeOffset>(12), Timezone = reader.GetString(13), Cutoff = reader.GetFieldValue<TimeOnly>(14), Currency = reader.GetString(15), Generated = reader.GetFieldValue<DateTimeOffset>(16), Committed = reader.GetFieldValue<DateTimeOffset>(17), Count = reader.GetInt64(18), PeriodSequence = reader.GetInt64(37), Status = reader.GetString(38).ToUpperInvariant(), Correlation = reader.GetString(39) };
        await reader.DisposeAsync().ConfigureAwait(false);
        var tenders = await ReadTenderChildrenAsync(c, t, id, ct).ConfigureAwait(false); var discounts = await ReadDiscountChildrenAsync(c, t, id, ct).ConfigureAwait(false); var ranges = await ReadRangeChildrenAsync(c, t, id, ct).ConfigureAwait(false);
        return new(head.Id, head.Reference, head.Request, head.Operation, head.Kind, head.Contract, head.HashVersion, head.Site, head.Identity, head.Period, head.Business, head.Start, head.End, head.Timezone, head.Cutoff, head.Currency, head.Generated, head.Committed, head.Count, amounts, tenders, discounts, ranges, head.Correlation, head.Reference, true, head.PeriodSequence, head.Status);
    }

    private static async Task<IReadOnlyList<FiscalXReadingTenderBreakdown>> ReadTenderChildrenAsync(NpgsqlConnection c, NpgsqlTransaction? t, Guid id, CancellationToken ct) { const string sql = "SELECT code.code_key,x.tender_transaction_count,x.amount_minor_units,x.currency_code FROM pos.fiscal_report_tender_breakdowns x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tender_classification_code_id WHERE x.x_z_report_id=@id ORDER BY code.sort_order,code.code_key"; await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("id", id); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); var x = new List<FiscalXReadingTenderBreakdown>(); while (await r.ReadAsync(ct).ConfigureAwait(false)) x.Add(new(r.GetString(0), r.GetInt64(1), r.GetInt64(2), r.GetString(3))); return x; }
    private static async Task<IReadOnlyList<FiscalXReadingDiscountBreakdown>> ReadDiscountChildrenAsync(NpgsqlConnection c, NpgsqlTransaction? t, Guid id, CancellationToken ct) { const string sql = "SELECT code.code_key,x.qualifying_document_count,x.discount_amount_minor_units,x.vat_exemption_amount_minor_units,x.currency_code FROM pos.fiscal_report_discount_breakdowns x JOIN pos.controlled_codes code ON code.controlled_code_id=x.discount_classification_code_id WHERE x.x_z_report_id=@id ORDER BY code.sort_order,code.code_key"; await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("id", id); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); var x = new List<FiscalXReadingDiscountBreakdown>(); while (await r.ReadAsync(ct).ConfigureAwait(false)) x.Add(new(r.GetString(0), r.GetInt64(1), r.GetInt64(2), r.GetInt64(3), r.GetString(4))); return x; }
    private static async Task<IReadOnlyList<FiscalXReadingFiscalNumberRange>> ReadRangeChildrenAsync(NpgsqlConnection c, NpgsqlTransaction? t, Guid id, CancellationToken ct) { const string sql = "SELECT fiscal_report_fiscal_number_range_id,fiscal_sequence_policy_id,fiscal_series,first_sequence_value,last_sequence_value,first_fiscal_number,last_fiscal_number,qualifying_document_count,currency_code FROM pos.fiscal_report_fiscal_number_ranges WHERE x_z_report_id=@id ORDER BY fiscal_series,first_sequence_value"; await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("id", id); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); var raw = new List<(Guid, Guid, string, long, long, string, string, long, string)>(); while (await r.ReadAsync(ct).ConfigureAwait(false)) raw.Add((r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetInt64(3), r.GetInt64(4), r.GetString(5), r.GetString(6), r.GetInt64(7), r.GetString(8))); await r.DisposeAsync().ConfigureAwait(false); var result = new List<FiscalXReadingFiscalNumberRange>(); foreach (var item in raw) { var gaps = await ReadGapChildrenAsync(c, t, item.Item1, ct).ConfigureAwait(false); result.Add(new(item.Item2, item.Item3, item.Item4, item.Item5, item.Item6, item.Item7, item.Item8, gaps, item.Item9)); } return result; }
    private static async Task<IReadOnlyList<FiscalXReadingSequenceGap>> ReadGapChildrenAsync(NpgsqlConnection c, NpgsqlTransaction? t, Guid rangeId, CancellationToken ct) { const string sql = "SELECT x.gap_sequence_value,code.code_key FROM pos.fiscal_report_sequence_gaps x JOIN pos.controlled_codes code ON code.controlled_code_id=x.gap_classification_code_id WHERE x.fiscal_report_fiscal_number_range_id=@id ORDER BY x.gap_sequence_value"; await using var cmd = CreateCommand(c, t, sql); cmd.Parameters.AddWithValue("id", rangeId); await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false); var x = new List<FiscalXReadingSequenceGap>(); while (await r.ReadAsync(ct).ConfigureAwait(false)) x.Add(new(r.GetInt64(0), r.GetString(1))); return x; }

    private static Guid? FindSourceGap(FiscalXReadingAggregate aggregate, FiscalXReadingFiscalNumberRange range, FiscalXReadingSequenceGap gap) => null;

    private static void AddPeriodParameters(NpgsqlCommand c, FiscalXReadingPeriod p, DateTimeOffset end) { c.Parameters.AddWithValue("site_pos_server_id", p.SitePosServerId); c.Parameters.AddWithValue("fiscal_identity_id", p.FiscalIdentityId); c.Parameters.AddWithValue("period_start_at", p.PeriodStartAt); c.Parameters.AddWithValue("effective_end", end); }
    private static void AddAmountParameters(NpgsqlParameterCollection p, FiscalXReadingAmounts a) { p.AddWithValue("gross", a.GrossSalesAmountMinorUnits); p.AddWithValue("net", a.NetSalesAmountMinorUnits); p.AddWithValue("vatable", a.VatableSalesAmountMinorUnits); p.AddWithValue("vat", a.VatAmountMinorUnits); p.AddWithValue("vat_exempt", a.VatExemptSalesAmountMinorUnits); p.AddWithValue("zero_rated", a.ZeroRatedSalesAmountMinorUnits); p.AddWithValue("discount", a.DiscountAmountMinorUnits); p.AddWithValue("senior", a.SeniorCitizenDiscountAmountMinorUnits); p.AddWithValue("pwd", a.PwdDiscountAmountMinorUnits); p.AddWithValue("other_stat", a.OtherStatutoryDiscountAmountMinorUnits); p.AddWithValue("vat_exemption", a.VatExemptionAmountMinorUnits); p.AddWithValue("coupon", a.CouponDiscountAmountMinorUnits); p.AddWithValue("promo", a.PromotionalDiscountAmountMinorUnits); p.AddWithValue("void", a.VoidAmountMinorUnits); }
    private static void AddNullable(NpgsqlParameterCollection parameters, string name, object? value, NpgsqlDbType type) { var parameter = new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value }; parameters.Add(parameter); }
    private static NpgsqlCommand CreateCommand(NpgsqlConnection c, NpgsqlTransaction? t, string sql) { var x = c.CreateCommand(); x.CommandText = sql; x.Transaction = t; return x; }
    private static async Task ExecuteAsync(NpgsqlConnection c, NpgsqlTransaction? t, string sql, CancellationToken ct, Action<NpgsqlParameterCollection>? add = null) { await using var cmd = CreateCommand(c, t, sql); add?.Invoke(cmd.Parameters); await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async Task<DateTimeOffset> ClockAsync(NpgsqlConnection c, NpgsqlTransaction t, CancellationToken ct)
    {
        await using var cmd = CreateCommand(c, t, "SELECT clock_timestamp()");
        var value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return value switch
        {
            DateTime timestamp => new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)),
            DateTimeOffset timestamp => timestamp,
            _ => throw new InvalidOperationException("PostgreSQL did not return a supported timestamp value.")
        };
    }

    private sealed class SourceBuilder(Guid id, string type, string status, DateTimeOffset created, Guid? policy, long? sequence, string? number, string? series, string completionBasis)
    { public List<FiscalXReadingSourceLine> Lines { get; } = []; public List<FiscalXReadingSourceTender> Tenders { get; } = []; public List<FiscalXReadingSourceTax> Taxes { get; } = []; public List<FiscalXReadingSourceDiscount> Discounts { get; } = []; public FiscalXReadingSourceStatutory? Statutory { get; set; } public FiscalXReadingSourceDocument Build() => new(id, type, status, created, policy, sequence, number, series, Lines, Tenders, Taxes, Discounts, Statutory, completionBasis); }
    private sealed record ReportingCodeIds(Dictionary<(string, string), Guid> Values) { public Guid Get(string set, string key) => Values[(set, key)]; }
}
