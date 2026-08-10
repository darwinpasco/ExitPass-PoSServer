using System.Data;
using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresBirSalesSummaryRepository(
    NpgsqlDataSource dataSource,
    FiscalXReadingAggregationService aggregationService) : IBirSalesSummaryRepository
{
    private static readonly Guid BirSummaryKindId = Guid.Parse("2326447f-74c2-5ed7-83bb-e079fcee7f3d");
    private static readonly Guid ZReadingKindId = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");

    public async Task<BirSalesSummaryResult> GenerateAsync(
        BirSalesSummaryCommand command,
        CancellationToken cancellationToken = default)
    {
        var semanticHash = string.Empty;
        var governingZId = Guid.Empty;
        var commitAttempted = false;
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

            var periodStatus = await ReadPeriodStatusAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            if (periodStatus is null)
                return Failure(BirSalesSummaryOutcome.ScopeMismatch, "The governed fiscal reporting scope is unavailable.");
            if (!string.Equals(periodStatus, "closed", StringComparison.Ordinal))
                return Failure(BirSalesSummaryOutcome.PeriodNotClosed, "A final BIR sales summary requires a CLOSED fiscal reporting period.");

            var governingZ = await ReadGoverningZAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            if (governingZ is null)
                return Failure(BirSalesSummaryOutcome.GoverningZNotFound, "The committed governing Z Reading is unavailable.");

            governingZId = governingZ.Id;
            semanticHash = BirSalesSummarySemanticRequestHasher.Compute(command, governingZ.Id);
            await AcquireGenerationLockAsync(connection, transaction, governingZ.Id, cancellationToken).ConfigureAwait(false);

            var existingOperation = await ReadOperationAsync(connection, transaction, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (existingOperation is not null)
            {
                if (existingOperation.Value.KindId != BirSummaryKindId ||
                    !string.Equals(existingOperation.Value.SemanticHash, semanticHash, StringComparison.Ordinal))
                    return Failure(BirSalesSummaryOutcome.Conflict, "The operation key is already bound to different governed summary semantics.");

                var replay = existingOperation.Value.SummaryId is null
                    ? null
                    : await ReadRecordAsync(connection, transaction, existingOperation.Value.SummaryId.Value, cancellationToken).ConfigureAwait(false);
                return replay is null
                    ? Failure(BirSalesSummaryOutcome.UnknownCommitOutcome, "The existing summary operation requires durable-state reconciliation.")
                    : new(BirSalesSummaryOutcome.Replayed, replay);
            }

            var existingSummaryId = await ReadExistingSummaryIdAsync(connection, transaction, governingZ.Id, cancellationToken).ConfigureAwait(false);
            if (existingSummaryId is not null)
            {
                var replay = await ReadRecordAsync(connection, transaction, existingSummaryId.Value, cancellationToken).ConfigureAwait(false);
                return replay is null
                    ? Failure(BirSalesSummaryOutcome.UnknownCommitOutcome, "The existing summary requires durable-state reconciliation.")
                    : new(BirSalesSummaryOutcome.Replayed, replay);
            }

            if (!string.Equals(governingZ.PeriodStatus, "closed", StringComparison.Ordinal) || governingZ.ClosedAt is null)
                return Failure(BirSalesSummaryOutcome.PeriodNotClosed, "A final BIR sales summary requires a CLOSED fiscal reporting period.");
            if (!string.Equals(governingZ.RequestStatus, "committed", StringComparison.Ordinal))
                return Failure(BirSalesSummaryOutcome.GoverningZNotCommitted, "The governing Z Reading is not committed.");

            var sourceDocuments = await ReadSourceDocumentsAsync(connection, transaction, governingZ, cancellationToken).ConfigureAwait(false);
            var sourceGaps = await ReadSourceGapsAsync(connection, transaction, sourceDocuments, cancellationToken).ConfigureAwait(false);
            var aggregate = aggregationService.Aggregate(sourceDocuments, sourceGaps, governingZ.CurrencyCode);
            var zTenders = await ReadTenderChildrenAsync(connection, transaction, governingZ.Id, cancellationToken).ConfigureAwait(false);
            var zDiscounts = await ReadDiscountChildrenAsync(connection, transaction, governingZ.Id, cancellationToken).ConfigureAwait(false);
            var zRanges = await ReadRangeChildrenAsync(connection, transaction, governingZ.Id, cancellationToken).ConfigureAwait(false);
            EnsureExactReconciliation(governingZ, aggregate, zTenders, zDiscounts, zRanges);

            var profile = await ResolveHeaderProfileAsync(connection, transaction, governingZ, cancellationToken).ConfigureAwait(false);
            if (profile is null)
                return Failure(BirSalesSummaryOutcome.HeaderProfileUnavailable, "An approved historical Sales Invoice header profile is unavailable for the governing Z Reading.");
            await EnsureDocumentHeaderProfileConsistencyAsync(connection, transaction, governingZ, profile.SalesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false);

            var codeIds = await ResolveCodeIdsAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var committedAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var requestId = Guid.NewGuid();
            var summaryId = Guid.NewGuid();

            await InsertRequestAsync(connection, transaction, command, governingZ, codeIds, requestId, semanticHash, committedAt, cancellationToken).ConfigureAwait(false);
            await InsertScopeAsync(connection, transaction, governingZ, codeIds, requestId, committedAt, cancellationToken).ConfigureAwait(false);
            await InsertSummaryAsync(connection, transaction, command, governingZ, profile, codeIds, requestId, summaryId, committedAt, cancellationToken).ConfigureAwait(false);
            await InsertAuditAsync(connection, transaction, command, codeIds, requestId, committedAt, cancellationToken).ConfigureAwait(false);

            var record = await ReadRecordAsync(connection, transaction, summaryId, cancellationToken).ConfigureAwait(false)
                ?? throw new BirSalesSummarySafeException(BirSalesSummaryOutcome.PersistenceFailure, "The summary was not readable inside its transaction.");
            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(BirSalesSummaryOutcome.Created, record);
        }
        catch (BirSalesSummarySafeException exception)
        {
            return Failure(exception.Outcome, exception.Message);
        }
        catch (FiscalXReadingSafeException exception)
        {
            var outcome = exception.Outcome == FiscalXReadingOutcome.UnsupportedSourceClassification
                ? BirSalesSummaryOutcome.UnsupportedSourceClassification
                : BirSalesSummaryOutcome.ReconciliationFailure;
            return Failure(outcome, exception.Message);
        }
        catch (OverflowException)
        {
            return Failure(BirSalesSummaryOutcome.ReconciliationFailure, "BIR sales-summary reconciliation exceeded supported minor-unit bounds.");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return await ReconcileAfterFailureAsync(command, governingZId, semanticHash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure)
        {
            return Failure(BirSalesSummaryOutcome.RetryableConcurrencyFailure, "BIR sales-summary generation encountered a retryable concurrency failure.");
        }
        catch (NpgsqlException)
        {
            return await ReconcileAfterFailureAsync(command, governingZId, semanticHash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return await ReconcileAfterFailureAsync(command, governingZId, semanticHash, commitAttempted, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<BirSalesSummaryResult> GetByIdAsync(Guid birSalesSummaryReportId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var record = await ReadRecordAsync(connection, null, birSalesSummaryReportId, cancellationToken).ConfigureAwait(false);
            return record is null ? new(BirSalesSummaryOutcome.NotFound) : new(BirSalesSummaryOutcome.Replayed, record);
        }
        catch (NpgsqlException)
        {
            return Failure(BirSalesSummaryOutcome.PersistenceFailure, "The BIR sales-summary read operation failed safely.");
        }
    }

    private async Task<BirSalesSummaryResult> ReconcileAfterFailureAsync(
        BirSalesSummaryCommand command,
        Guid governingZId,
        string semanticHash,
        bool commitAttempted,
        CancellationToken cancellationToken)
    {
        if (governingZId == Guid.Empty || string.IsNullOrEmpty(semanticHash))
            return Failure(commitAttempted ? BirSalesSummaryOutcome.UnknownCommitOutcome : BirSalesSummaryOutcome.PersistenceFailure,
                commitAttempted ? "The summary commit outcome requires durable-state reconciliation." : "BIR sales-summary generation failed safely.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var operation = await ReadOperationAsync(connection, null, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (operation is not null)
            {
                if (operation.Value.KindId != BirSummaryKindId || !string.Equals(operation.Value.SemanticHash, semanticHash, StringComparison.Ordinal))
                    return Failure(BirSalesSummaryOutcome.Conflict, "The operation key is already bound to different governed summary semantics.");
                if (operation.Value.SummaryId is not null)
                {
                    var replay = await ReadRecordAsync(connection, null, operation.Value.SummaryId.Value, cancellationToken).ConfigureAwait(false);
                    if (replay is not null) return new(BirSalesSummaryOutcome.Replayed, replay);
                }
            }
            var summaryId = await ReadExistingSummaryIdAsync(connection, null, governingZId, cancellationToken).ConfigureAwait(false);
            if (summaryId is not null)
            {
                var replay = await ReadRecordAsync(connection, null, summaryId.Value, cancellationToken).ConfigureAwait(false);
                if (replay is not null) return new(BirSalesSummaryOutcome.Replayed, replay);
            }
            return Failure(commitAttempted ? BirSalesSummaryOutcome.UnknownCommitOutcome : BirSalesSummaryOutcome.PersistenceFailure,
                commitAttempted ? "The summary commit outcome requires durable-state reconciliation." : "BIR sales-summary generation failed safely.");
        }
        catch (NpgsqlException)
        {
            return Failure(BirSalesSummaryOutcome.UnknownCommitOutcome, "The summary operation requires durable-state reconciliation.");
        }
    }

    private static async Task<string?> ReadPeriodStatusAsync(NpgsqlConnection c, NpgsqlTransaction t, BirSalesSummaryCommand command, CancellationToken ct)
    {
        const string sql = "SELECT status.code_key FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes status ON status.controlled_code_id=p.period_status_code_id WHERE p.fiscal_reporting_period_id=@period AND p.site_pos_server_id=@site AND p.fiscal_identity_id=@identity AND p.currency_code=@currency FOR SHARE OF p";
        await using var cmd = CreateCommand(c, t, sql);
        cmd.Parameters.AddWithValue("period", command.FiscalReportingPeriodId); cmd.Parameters.AddWithValue("site", command.SitePosServerId);
        cmd.Parameters.AddWithValue("identity", command.FiscalIdentityId); cmd.Parameters.AddWithValue("currency", command.CurrencyCode);
        return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
    }

    private static async Task<GoverningZ?> ReadGoverningZAsync(NpgsqlConnection c, NpgsqlTransaction t, BirSalesSummaryCommand command, CancellationToken ct)
    {
        const string sql = """
        SELECT z.x_z_report_id,z.report_number,z.fiscal_reporting_contract_version_id,z.fiscal_reporting_period_id,
               z.site_pos_server_id,z.fiscal_identity_id,z.business_day_date,z.period_start_at,z.period_end_at,
               z.reporting_timezone_name,z.currency_code,z.transaction_count,z.beginning_si_ref,z.ending_si_ref,
               z.first_fiscal_sequence_value,z.last_fiscal_sequence_value,z.fiscal_sequence_gap_count,
               z.reset_counter_value,z.z_counter_value,z.previous_grand_total_amount_minor_units,
               z.current_grand_total_amount_minor_units,z.present_grand_total_amount_minor_units,
               z.gross_sales_amount_minor_units,z.net_sales_amount_minor_units,z.vatable_sales_amount_minor_units,
               z.vat_amount_minor_units,z.vat_exempt_sales_amount_minor_units,z.zero_rated_sales_amount_minor_units,
               z.discount_amount_minor_units,z.senior_citizen_discount_amount_minor_units,z.pwd_discount_amount_minor_units,
               z.other_statutory_discount_amount_minor_units,z.vat_exemption_amount_minor_units,
               z.coupon_discount_amount_minor_units,z.promotional_discount_amount_minor_units,z.void_amount_minor_units,
               z.refund_amount_minor_units,z.return_amount_minor_units,z.adjustment_amount_minor_units,
               z.service_charge_amount_minor_units,z.generated_at,z.committed_at,period_status.code_key,period.closed_at,
               request_status.code_key,s.previous_reset_counter_value,s.resulting_reset_counter_value,
               s.previous_z_counter_value,s.resulting_z_counter_value,s.previous_grand_total_amount_minor_units,
               s.current_period_amount_minor_units,s.resulting_grand_total_amount_minor_units,s.expected_state_version,
               s.resulting_state_version
        FROM pos.x_z_reports z
        JOIN pos.fiscal_report_requests request ON request.fiscal_report_request_id=z.fiscal_report_request_id
        JOIN pos.controlled_codes request_status ON request_status.controlled_code_id=request.report_status_code_id
        JOIN pos.fiscal_reporting_periods period ON period.fiscal_reporting_period_id=z.fiscal_reporting_period_id
        JOIN pos.controlled_codes period_status ON period_status.controlled_code_id=period.period_status_code_id
        JOIN pos.fiscal_z_counter_snapshots s ON s.x_z_report_id=z.x_z_report_id
        WHERE z.report_number=@reference AND z.report_kind_code_id=@kind
          AND z.fiscal_reporting_period_id=@period AND z.site_pos_server_id=@site
          AND z.fiscal_identity_id=@identity AND z.currency_code=@currency
        FOR SHARE OF z,request,period,s;
        """;
        await using var cmd = CreateCommand(c, t, sql);
        cmd.Parameters.AddWithValue("reference", command.GoverningZReadingReference); cmd.Parameters.AddWithValue("kind", ZReadingKindId);
        cmd.Parameters.AddWithValue("period", command.FiscalReportingPeriodId); cmd.Parameters.AddWithValue("site", command.SitePosServerId);
        cmd.Parameters.AddWithValue("identity", command.FiscalIdentityId); cmd.Parameters.AddWithValue("currency", command.CurrencyCode);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await r.ReadAsync(ct).ConfigureAwait(false)) return null;
        var amounts = new FiscalXReadingAmounts(r.GetInt64(22),r.GetInt64(23),r.GetInt64(24),r.GetInt64(25),r.GetInt64(26),r.GetInt64(27),r.GetInt64(28),r.GetInt64(29),r.GetInt64(30),r.GetInt64(31),r.GetInt64(32),r.GetInt64(33),r.GetInt64(34),r.GetInt64(35),r.GetInt64(36),r.GetInt64(37),r.GetInt64(38),r.GetInt64(39));
        var counter = new FiscalZReadingCounterSnapshot(r.GetInt64(45),r.GetInt64(46),r.GetInt64(47),r.GetInt64(48),r.GetInt64(49),r.GetInt64(50),r.GetInt64(51),r.GetInt64(52),r.GetInt64(53));
        return new(r.GetGuid(0),r.GetString(1),r.GetGuid(2),r.GetGuid(3),r.GetGuid(4),r.GetGuid(5),r.GetFieldValue<DateOnly>(6),
            r.GetFieldValue<DateTimeOffset>(7),r.GetFieldValue<DateTimeOffset>(8),r.GetString(9),r.GetString(10),r.GetInt64(11),
            r.IsDBNull(12)?null:r.GetString(12),r.IsDBNull(13)?null:r.GetString(13),r.IsDBNull(14)?null:r.GetInt64(14),
            r.IsDBNull(15)?null:r.GetInt64(15),r.GetInt64(16),r.GetInt64(17),r.GetInt64(18),r.GetInt64(19),r.GetInt64(20),
            r.GetInt64(21),amounts,r.GetFieldValue<DateTimeOffset>(40),r.GetFieldValue<DateTimeOffset>(41),r.GetString(42),
            r.IsDBNull(43)?null:r.GetFieldValue<DateTimeOffset>(43),r.GetString(44),counter);
    }

    private static async Task AcquireGenerationLockAsync(NpgsqlConnection c, NpgsqlTransaction t, Guid zId, CancellationToken ct)
    {
        await using var cmd = CreateCommand(c, t, "SELECT pg_advisory_xact_lock(@key)");
        cmd.Parameters.AddWithValue("key", BitConverter.ToInt64(zId.ToByteArray(), 0));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<(string SemanticHash, Guid KindId, Guid? SummaryId)?> ReadOperationAsync(NpgsqlConnection c, NpgsqlTransaction? t, Guid site, string operation, CancellationToken ct)
    {
        const string sql = "SELECT request.semantic_request_hash,request.report_type_code_id,summary.bir_sales_summary_report_id FROM pos.fiscal_report_requests request LEFT JOIN pos.bir_sales_summary_reports summary ON summary.fiscal_report_request_id=request.fiscal_report_request_id WHERE request.site_pos_server_id=@site AND request.operation_idempotency_key=@operation";
        await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("site",site);cmd.Parameters.AddWithValue("operation",operation);
        await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);if(!await r.ReadAsync(ct).ConfigureAwait(false))return null;
        return(r.GetString(0),r.GetGuid(1),r.IsDBNull(2)?null:r.GetGuid(2));
    }

    private static async Task<Guid?> ReadExistingSummaryIdAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid zId,CancellationToken ct)
    {
        await using var cmd=CreateCommand(c,t,"SELECT bir_sales_summary_report_id FROM pos.bir_sales_summary_reports WHERE governing_z_report_id=@z AND reporting_contract_profile_ref=@profile");
        cmd.Parameters.AddWithValue("z",zId);cmd.Parameters.AddWithValue("profile",BirSalesSummaryContract.ReportingProfile);
        return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is Guid id?id:null;
    }

    private static async Task<BirSalesSummaryHeaderProfile?> ResolveHeaderProfileAsync(NpgsqlConnection c,NpgsqlTransaction t,GoverningZ z,CancellationToken ct)
    {
        const string sql="""
        SELECT sales_invoice_header_profile_id,profile_version,pos_serial_number,machine_identification_number,
               bir_accreditation_number,bir_accreditation_issued_date,bir_accreditation_valid_until,ptu_number,ptu_issued_date
        FROM pos.sales_invoice_header_profiles
        WHERE site_pos_server_id=@site AND fiscal_identity_id=@identity
          AND lifecycle_status IN ('APPROVED','RETIRED') AND effective_from<=@at
          AND (effective_to IS NULL OR @at<effective_to)
        ORDER BY effective_from DESC,sales_invoice_header_profile_id
        LIMIT 2 FOR SHARE;
        """;
        await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("site",z.SitePosServerId);cmd.Parameters.AddWithValue("identity",z.FiscalIdentityId);cmd.Parameters.AddWithValue("at",z.CommittedAt);
        await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);if(!await r.ReadAsync(ct).ConfigureAwait(false))return null;
        var result=new BirSalesSummaryHeaderProfile(r.GetGuid(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetFieldValue<DateOnly>(5),r.GetFieldValue<DateOnly>(6),r.GetString(7),r.GetFieldValue<DateOnly>(8));
        if(await r.ReadAsync(ct).ConfigureAwait(false))throw new BirSalesSummarySafeException(BirSalesSummaryOutcome.HeaderProfileUnavailable,"The historical Sales Invoice header profile is ambiguous.");
        return result;
    }

    private static async Task EnsureDocumentHeaderProfileConsistencyAsync(NpgsqlConnection c,NpgsqlTransaction t,GoverningZ z,Guid profileId,CancellationToken ct)
    {
        const string sql="SELECT array_agg(DISTINCT snapshot.sales_invoice_header_profile_id) FROM pos.fiscal_document_header_snapshots snapshot JOIN pos.fiscal_documents document ON document.fiscal_document_id=snapshot.fiscal_document_id WHERE document.fiscal_reporting_period_id=@period";
        await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("period",z.FiscalReportingPeriodId);
        var value=await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if(value is Guid[] profiles && (profiles.Length!=1||profiles[0]!=profileId))
            throw new BirSalesSummarySafeException(BirSalesSummaryOutcome.ReconciliationFailure,"Fiscal document header snapshots do not reconcile to one historical summary profile.");
    }

    private static void EnsureExactReconciliation(GoverningZ z,FiscalXReadingAggregate aggregate,IReadOnlyList<FiscalXReadingTenderBreakdown> tenders,IReadOnlyList<FiscalXReadingDiscountBreakdown> discounts,IReadOnlyList<FiscalXReadingFiscalNumberRange> ranges)
    {
        var rangesJson=JsonSerializer.Serialize(ranges.OrderBy(x=>x.FiscalSeries,StringComparer.Ordinal).ThenBy(x=>x.FirstSequenceValue));var aggregateRangesJson=JsonSerializer.Serialize(aggregate.FiscalNumberRanges.OrderBy(x=>x.FiscalSeries,StringComparer.Ordinal).ThenBy(x=>x.FirstSequenceValue));
        if(z.TransactionCount!=aggregate.QualifyingDocumentCount||z.Amounts!=aggregate.Amounts||
           !tenders.OrderBy(x=>x.Classification,StringComparer.Ordinal).SequenceEqual(aggregate.Tenders.OrderBy(x=>x.Classification,StringComparer.Ordinal))||
           !discounts.OrderBy(x=>x.Classification,StringComparer.Ordinal).SequenceEqual(aggregate.Discounts.OrderBy(x=>x.Classification,StringComparer.Ordinal))||rangesJson!=aggregateRangesJson||
           z.BeginningSiRef!=aggregate.BeginningFiscalNumber||z.EndingSiRef!=aggregate.EndingFiscalNumber||
           z.FirstSequenceValue!=aggregate.FirstSequenceValue||z.LastSequenceValue!=aggregate.LastSequenceValue||
           z.FiscalSequenceGapCount!=aggregate.FiscalNumberRanges.Sum(range=>(long)range.Gaps.Count))
            throw new BirSalesSummarySafeException(BirSalesSummaryOutcome.ReconciliationFailure,"The governing Z Reading does not reconcile exactly to canonical fiscal records.");
        if(z.ResetCounterValue!=z.CounterSnapshot.ResultingResetCounterValue||z.ZCounterValue!=z.CounterSnapshot.ResultingZCounterValue||
           z.PreviousGrandTotal!=z.CounterSnapshot.PreviousGrandTotalAmountMinorUnits||z.CurrentGrandTotal!=z.CounterSnapshot.CurrentPeriodGrandTotalAmountMinorUnits||
           z.PresentGrandTotal!=z.CounterSnapshot.ResultingGrandTotalAmountMinorUnits||
           checked(z.PreviousGrandTotal+z.CurrentGrandTotal)!=z.PresentGrandTotal||
           checked(z.CounterSnapshot.PreviousZCounterValue+1)!=z.CounterSnapshot.ResultingZCounterValue)
            throw new BirSalesSummarySafeException(BirSalesSummaryOutcome.ReconciliationFailure,"The governing Z counter or grand-total state does not reconcile exactly.");
    }

    private static async Task<CodeIds> ResolveCodeIdsAsync(NpgsqlConnection c,NpgsqlTransaction t,CancellationToken ct)
    {
        var required=new[]{("fiscal_report_kind","bir_sales_summary"),("fiscal_report_status","committed"),("fiscal_report_scope_type","site_pos_server_period"),("fiscal_action_audit_result","success")};
        var values=new Dictionary<(string,string),Guid>();
        const string sql="SELECT code.controlled_code_id FROM pos.controlled_codes code JOIN pos.controlled_code_sets set_ ON set_.controlled_code_set_id=code.controlled_code_set_id WHERE set_.code_set_key=@set AND code.code_key=@key AND set_.is_active AND code.is_active";
        foreach(var pair in required){await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("set",pair.Item1);cmd.Parameters.AddWithValue("key",pair.Item2);if(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is not Guid id)throw new BirSalesSummarySafeException(BirSalesSummaryOutcome.UnsupportedSourceClassification,"A required governed reporting classification is unavailable.");values[pair]=id;}
        return new(values);
    }

    private static Task InsertRequestAsync(NpgsqlConnection c,NpgsqlTransaction t,BirSalesSummaryCommand command,GoverningZ z,CodeIds ids,Guid requestId,string hash,DateTimeOffset at,CancellationToken ct)=>
        ExecuteAsync(c,t,"INSERT INTO pos.fiscal_report_requests(fiscal_report_request_id,fiscal_reporting_contract_version_id,fiscal_reporting_period_id,site_pos_server_id,report_type_code_id,report_status_code_id,operation_idempotency_key,semantic_request_hash,semantic_hash_version,business_day_date,requested_at,requested_by_ref,service_identity_ref,created_at,updated_at) VALUES(@id,@contract,@period,@site,@kind,@status,@operation,@hash,@version,@business,@at,@actor,@service,@at,@at)",ct,p=>{p.AddWithValue("id",requestId);p.AddWithValue("contract",z.ContractId);p.AddWithValue("period",z.FiscalReportingPeriodId);p.AddWithValue("site",z.SitePosServerId);p.AddWithValue("kind",ids.Get("fiscal_report_kind","bir_sales_summary"));p.AddWithValue("status",ids.Get("fiscal_report_status","committed"));p.AddWithValue("operation",command.OperationKey);p.AddWithValue("hash",hash);p.AddWithValue("version",BirSalesSummaryContract.SemanticHashVersion);p.AddWithValue("business",z.BusinessDayDate);p.AddWithValue("at",at);p.AddWithValue("actor",command.RequestedByRef);p.AddWithValue("service",command.ServiceIdentityRef);});

    private static Task InsertScopeAsync(NpgsqlConnection c,NpgsqlTransaction t,GoverningZ z,CodeIds ids,Guid requestId,DateTimeOffset at,CancellationToken ct)=>
        ExecuteAsync(c,t,"INSERT INTO pos.fiscal_report_scopes(fiscal_report_scope_id,fiscal_report_request_id,fiscal_reporting_period_id,scope_type_code_id,created_at) VALUES(@id,@request,@period,@scope,@at)",ct,p=>{p.AddWithValue("id",Guid.NewGuid());p.AddWithValue("request",requestId);p.AddWithValue("period",z.FiscalReportingPeriodId);p.AddWithValue("scope",ids.Get("fiscal_report_scope_type","site_pos_server_period"));p.AddWithValue("at",at);});

    private static Task InsertSummaryAsync(NpgsqlConnection c,NpgsqlTransaction t,BirSalesSummaryCommand command,GoverningZ z,BirSalesSummaryHeaderProfile profile,CodeIds ids,Guid requestId,Guid summaryId,DateTimeOffset at,CancellationToken ct)
    {
        const string sql="""
        INSERT INTO pos.bir_sales_summary_reports(
          bir_sales_summary_report_id,fiscal_report_request_id,report_kind_code_id,fiscal_reporting_contract_version_id,
          fiscal_reporting_period_id,governing_z_report_id,governing_report_kind_code_id,site_pos_server_id,fiscal_identity_id,
          reporting_contract_profile_ref,sales_invoice_header_profile_id,header_profile_version,pos_serial_number,
          machine_identification_number,bir_accreditation_number,bir_accreditation_issued_date,bir_accreditation_valid_until,
          ptu_number,ptu_issued_date,business_day_date,reporting_period_start_date,reporting_period_end_date,transaction_count,
          beginning_si_ref,ending_si_ref,previous_grand_total_amount_minor_units,present_grand_total_amount_minor_units,
          gross_sales_amount_minor_units,net_sales_amount_minor_units,vatable_sales_amount_minor_units,vat_amount_minor_units,
          vat_exempt_sales_amount_minor_units,zero_rated_sales_amount_minor_units,discount_amount_minor_units,
          senior_citizen_discount_amount_minor_units,pwd_discount_amount_minor_units,other_statutory_discount_amount_minor_units,
          vat_exemption_amount_minor_units,void_amount_minor_units,refund_amount_minor_units,return_amount_minor_units,
          adjustment_amount_minor_units,reset_counter_value,z_counter_value,currency_code,generated_at,committed_at,created_at,updated_at)
        VALUES(@id,@request,@kind,@contract,@period,@z,@z_kind,@site,@identity,@profile_ref,@header_id,@header_version,
          @serial,@min,@accreditation,@accreditation_issued,@accreditation_until,@ptu,@ptu_issued,@business,
          (@start AT TIME ZONE @timezone)::date,(@end AT TIME ZONE @timezone)::date,@count,@begin_ref,@end_ref,
          @previous_gta,@present_gta,@gross,@net,@vatable,@vat,@vat_exempt,@zero_rated,@discount,@senior,@pwd,@other_stat,
          @vat_exemption,@void,@refund,@return,@adjustment,@reset,@z_counter,@currency,@at,@at,@at,@at)
        """;
        return ExecuteAsync(c,t,sql,ct,p=>{p.AddWithValue("id",summaryId);p.AddWithValue("request",requestId);p.AddWithValue("kind",ids.Get("fiscal_report_kind","bir_sales_summary"));p.AddWithValue("contract",z.ContractId);p.AddWithValue("period",z.FiscalReportingPeriodId);p.AddWithValue("z",z.Id);p.AddWithValue("z_kind",ZReadingKindId);p.AddWithValue("site",z.SitePosServerId);p.AddWithValue("identity",z.FiscalIdentityId);p.AddWithValue("profile_ref",BirSalesSummaryContract.ReportingProfile);p.AddWithValue("header_id",profile.SalesInvoiceHeaderProfileId);p.AddWithValue("header_version",profile.ProfileVersion);p.AddWithValue("serial",profile.PosSerialNumber);p.AddWithValue("min",profile.MachineIdentificationNumber);p.AddWithValue("accreditation",profile.BirAccreditationNumber);p.AddWithValue("accreditation_issued",profile.BirAccreditationIssuedDate);p.AddWithValue("accreditation_until",profile.BirAccreditationValidUntil);p.AddWithValue("ptu",profile.PtuNumber);p.AddWithValue("ptu_issued",profile.PtuIssuedDate);p.AddWithValue("business",z.BusinessDayDate);p.AddWithValue("start",z.PeriodStartAt);p.AddWithValue("end",z.PeriodEndAt);p.AddWithValue("timezone",z.ReportingTimezoneName);p.AddWithValue("count",z.TransactionCount);AddNullable(p,"begin_ref",z.BeginningSiRef,NpgsqlDbType.Text);AddNullable(p,"end_ref",z.EndingSiRef,NpgsqlDbType.Text);p.AddWithValue("previous_gta",z.PreviousGrandTotal);p.AddWithValue("present_gta",z.PresentGrandTotal);AddAmounts(p,z.Amounts);p.AddWithValue("reset",z.ResetCounterValue);p.AddWithValue("z_counter",z.ZCounterValue);p.AddWithValue("currency",z.CurrencyCode);p.AddWithValue("at",at);});
    }

    private static Task InsertAuditAsync(NpgsqlConnection c,NpgsqlTransaction t,BirSalesSummaryCommand command,CodeIds ids,Guid requestId,DateTimeOffset at,CancellationToken ct)=>
        ExecuteAsync(c,t,"INSERT INTO pos.fiscal_action_audit(fiscal_action_audit_id,site_pos_server_id,fiscal_report_request_id,audit_action_type_code_id,audit_result_code_id,actor_ref,service_identity_ref,correlation_ref,occurred_at,created_at) VALUES(@id,@site,@request,@action,@result,@actor,@service,@correlation,@at,@at)",ct,p=>{p.AddWithValue("id",Guid.NewGuid());p.AddWithValue("site",command.SitePosServerId);p.AddWithValue("request",requestId);p.AddWithValue("action",ids.Get("fiscal_report_kind","bir_sales_summary"));p.AddWithValue("result",ids.Get("fiscal_action_audit_result","success"));p.AddWithValue("actor",command.RequestedByRef);p.AddWithValue("service",command.ServiceIdentityRef);p.AddWithValue("correlation",command.CorrelationId);p.AddWithValue("at",at);});

    private static async Task<BirSalesSummaryRecord?> ReadRecordAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct)
    {
        const string sql="""
        SELECT s.bir_sales_summary_report_id,s.fiscal_report_request_id,request.operation_idempotency_key,kind.code_key,
          contract.contract_key||':'||contract.contract_version,request.semantic_hash_version,s.reporting_contract_profile_ref,
          s.governing_z_report_id,z.report_number,s.site_pos_server_id,s.fiscal_identity_id,s.fiscal_reporting_period_id,
          s.business_day_date,s.reporting_period_start_date,s.reporting_period_end_date,s.currency_code,s.transaction_count,
          s.beginning_si_ref,s.ending_si_ref,z.gross_sales_amount_minor_units,z.net_sales_amount_minor_units,
          z.vatable_sales_amount_minor_units,z.vat_amount_minor_units,z.vat_exempt_sales_amount_minor_units,
          z.zero_rated_sales_amount_minor_units,z.discount_amount_minor_units,z.senior_citizen_discount_amount_minor_units,
          z.pwd_discount_amount_minor_units,z.other_statutory_discount_amount_minor_units,z.vat_exemption_amount_minor_units,
          z.coupon_discount_amount_minor_units,z.promotional_discount_amount_minor_units,z.void_amount_minor_units,
          z.refund_amount_minor_units,z.return_amount_minor_units,z.adjustment_amount_minor_units,z.service_charge_amount_minor_units,
          counter.previous_reset_counter_value,counter.resulting_reset_counter_value,counter.previous_z_counter_value,
          counter.resulting_z_counter_value,counter.previous_grand_total_amount_minor_units,counter.current_period_amount_minor_units,
          counter.resulting_grand_total_amount_minor_units,counter.expected_state_version,counter.resulting_state_version,
          s.sales_invoice_header_profile_id,s.header_profile_version,s.pos_serial_number,s.machine_identification_number,
          s.bir_accreditation_number,s.bir_accreditation_issued_date,s.bir_accreditation_valid_until,s.ptu_number,s.ptu_issued_date,
          s.generated_at,s.committed_at,status.code_key,COALESCE(a.correlation_ref,'unavailable')
        FROM pos.bir_sales_summary_reports s
        JOIN pos.fiscal_report_requests request ON request.fiscal_report_request_id=s.fiscal_report_request_id
        JOIN pos.controlled_codes kind ON kind.controlled_code_id=s.report_kind_code_id
        JOIN pos.controlled_codes status ON status.controlled_code_id=request.report_status_code_id
        JOIN pos.fiscal_reporting_contract_versions contract ON contract.fiscal_reporting_contract_version_id=s.fiscal_reporting_contract_version_id
        JOIN pos.x_z_reports z ON z.x_z_report_id=s.governing_z_report_id
        JOIN pos.fiscal_z_counter_snapshots counter ON counter.x_z_report_id=z.x_z_report_id
        LEFT JOIN LATERAL(SELECT correlation_ref FROM pos.fiscal_action_audit WHERE fiscal_report_request_id=s.fiscal_report_request_id ORDER BY occurred_at LIMIT 1)a ON true
        WHERE s.bir_sales_summary_report_id=@id;
        """;
        await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);if(!await r.ReadAsync(ct).ConfigureAwait(false))return null;
        var summaryId=r.GetGuid(0);var zId=r.GetGuid(7);
        var amounts=new FiscalXReadingAmounts(r.GetInt64(19),r.GetInt64(20),r.GetInt64(21),r.GetInt64(22),r.GetInt64(23),r.GetInt64(24),r.GetInt64(25),r.GetInt64(26),r.GetInt64(27),r.GetInt64(28),r.GetInt64(29),r.GetInt64(30),r.GetInt64(31),r.GetInt64(32),r.GetInt64(33),r.GetInt64(34),r.GetInt64(35),r.GetInt64(36));
        var counter=new FiscalZReadingCounterSnapshot(r.GetInt64(37),r.GetInt64(38),r.GetInt64(39),r.GetInt64(40),r.GetInt64(41),r.GetInt64(42),r.GetInt64(43),r.GetInt64(44),r.GetInt64(45));
        var profile=new BirSalesSummaryHeaderProfile(r.GetGuid(46),r.GetString(47),r.GetString(48),r.GetString(49),r.GetString(50),r.GetFieldValue<DateOnly>(51),r.GetFieldValue<DateOnly>(52),r.GetString(53),r.GetFieldValue<DateOnly>(54));
        var head=new{Request=r.GetGuid(1),Operation=r.GetString(2),Kind=r.GetString(3).ToUpperInvariant(),Contract=r.GetString(4),Hash=r.GetString(5),Profile=r.GetString(6),ZReference=r.GetString(8),Site=r.GetGuid(9),Identity=r.GetGuid(10),Period=r.GetGuid(11),Business=r.GetFieldValue<DateOnly>(12),Start=r.GetFieldValue<DateOnly>(13),End=r.GetFieldValue<DateOnly>(14),Currency=r.GetString(15),Count=r.GetInt64(16),Begin=r.IsDBNull(17)?null:r.GetString(17),EndRef=r.IsDBNull(18)?null:r.GetString(18),Generated=r.GetFieldValue<DateTimeOffset>(55),Committed=r.GetFieldValue<DateTimeOffset>(56),Status=r.GetString(57).ToUpperInvariant(),Correlation=r.GetString(58)};
        await r.DisposeAsync().ConfigureAwait(false);
        var tenders=await ReadTenderChildrenAsync(c,t,zId,ct).ConfigureAwait(false);var discounts=await ReadDiscountChildrenAsync(c,t,zId,ct).ConfigureAwait(false);var ranges=await ReadRangeChildrenAsync(c,t,zId,ct).ConfigureAwait(false);
        return new(summaryId,head.Request,head.Operation,head.Kind,head.Contract,head.Hash,head.Profile,zId,head.ZReference,head.Site,head.Identity,head.Period,head.Business,head.Start,head.End,head.Currency,head.Count,head.Begin,head.EndRef,amounts,tenders,discounts,ranges,counter,profile,head.Generated,head.Committed,head.Correlation,$"BIRSS-{summaryId:N}"[..18].ToUpperInvariant(),true,head.Status);
    }

    private static async Task<IReadOnlyList<FiscalXReadingSourceDocument>> ReadSourceDocumentsAsync(NpgsqlConnection c,NpgsqlTransaction t,GoverningZ z,CancellationToken ct)
    {
        const string sql="SELECT d.fiscal_document_id,type_code.code_key,status_code.code_key,d.created_at,d.fiscal_sequence_policy_id,d.fiscal_sequence_value,d.fiscal_document_number,d.fiscal_series FROM pos.fiscal_documents d JOIN pos.controlled_codes type_code ON type_code.controlled_code_id=d.fiscal_document_type_code_id JOIN pos.controlled_codes status_code ON status_code.controlled_code_id=d.fiscal_document_status_code_id WHERE d.fiscal_reporting_period_id=@period AND d.site_pos_server_id=@site AND d.fiscal_identity_id=@identity AND d.currency_code=@currency ORDER BY d.created_at,d.fiscal_document_id";
        var documents=new Dictionary<Guid,SourceBuilder>();await using(var cmd=CreateCommand(c,t,sql)){cmd.Parameters.AddWithValue("period",z.FiscalReportingPeriodId);cmd.Parameters.AddWithValue("site",z.SitePosServerId);cmd.Parameters.AddWithValue("identity",z.FiscalIdentityId);cmd.Parameters.AddWithValue("currency",z.CurrencyCode);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false)){var id=r.GetGuid(0);documents[id]=new(id,r.GetString(1),r.GetString(2),r.GetFieldValue<DateTimeOffset>(3),r.IsDBNull(4)?null:r.GetGuid(4),r.IsDBNull(5)?null:r.GetInt64(5),r.IsDBNull(6)?null:r.GetString(6),r.IsDBNull(7)?null:r.GetString(7));}}
        if(documents.Count==0)return[];await ReadLinesAsync(c,t,documents,ct).ConfigureAwait(false);await ReadTendersAsync(c,t,documents,ct).ConfigureAwait(false);await ReadTaxesAsync(c,t,documents,ct).ConfigureAwait(false);await ReadDiscountsAsync(c,t,documents,ct).ConfigureAwait(false);await ReadStatutoryAsync(c,t,documents,ct).ConfigureAwait(false);return documents.Values.Select(x=>x.Build()).ToArray();
    }
    private static async Task ReadLinesAsync(NpgsqlConnection c,NpgsqlTransaction t,Dictionary<Guid,SourceBuilder>d,CancellationToken ct){const string sql="SELECT l.fiscal_document_id,code.code_key,l.gross_amount_minor_units,l.discount_amount_minor_units,l.tax_amount_minor_units,l.net_amount_minor_units,l.currency_code FROM pos.fiscal_document_lines l JOIN pos.controlled_codes code ON code.controlled_code_id=l.line_type_code_id WHERE l.fiscal_document_id=ANY(@ids) AND l.is_active ORDER BY l.fiscal_document_id,l.line_sequence";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("ids",d.Keys.ToArray());await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false))d[r.GetGuid(0)].Lines.Add(new(r.GetString(1),r.GetInt64(2),r.GetInt64(3),r.GetInt64(4),r.GetInt64(5),r.GetString(6)));}
    private static async Task ReadTendersAsync(NpgsqlConnection c,NpgsqlTransaction t,Dictionary<Guid,SourceBuilder>d,CancellationToken ct){const string sql="SELECT x.fiscal_document_id,code.code_key,x.amount_minor_units,x.currency_code FROM pos.fiscal_tenders x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tender_type_code_id WHERE x.fiscal_document_id=ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_tender_id";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("ids",d.Keys.ToArray());await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false))d[r.GetGuid(0)].Tenders.Add(new(r.GetString(1),r.GetInt64(2),r.GetString(3)));}
    private static async Task ReadTaxesAsync(NpgsqlConnection c,NpgsqlTransaction t,Dictionary<Guid,SourceBuilder>d,CancellationToken ct){const string sql="SELECT x.fiscal_document_id,code.code_key,x.fiscal_document_line_id IS NULL,x.taxable_amount_minor_units,x.tax_amount_minor_units,x.currency_code FROM pos.fiscal_tax_details x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tax_classification_code_id WHERE x.fiscal_document_id=ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_tax_detail_id";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("ids",d.Keys.ToArray());await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false))d[r.GetGuid(0)].Taxes.Add(new(r.GetString(1),r.GetBoolean(2),r.GetInt64(3),r.GetInt64(4),r.GetString(5)));}
    private static async Task ReadDiscountsAsync(NpgsqlConnection c,NpgsqlTransaction t,Dictionary<Guid,SourceBuilder>d,CancellationToken ct){const string sql="SELECT x.fiscal_document_id,code.code_key,x.discount_amount_minor_units,x.vat_privilege_amount_minor_units,x.currency_code FROM pos.fiscal_discount_privilege_details x JOIN pos.controlled_codes code ON code.controlled_code_id=x.discount_privilege_type_code_id WHERE x.fiscal_document_id=ANY(@ids) ORDER BY x.fiscal_document_id,x.fiscal_discount_privilege_detail_id";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("ids",d.Keys.ToArray());await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false))d[r.GetGuid(0)].Discounts.Add(new(r.GetString(1),r.GetInt64(2),r.GetInt64(3),r.GetString(4)));}
    private static async Task ReadStatutoryAsync(NpgsqlConnection c,NpgsqlTransaction t,Dictionary<Guid,SourceBuilder>d,CancellationToken ct){const string sql="SELECT x.fiscal_document_id,entitlement.code_key,x.statutory_discount_amount_minor_units,x.vat_amount_minor_units,x.final_payable_amount_minor_units,x.currency_code FROM pos.fiscal_document_applied_statutory_facts x JOIN pos.controlled_codes entitlement ON entitlement.controlled_code_id=x.entitlement_type_code_id WHERE x.fiscal_document_id=ANY(@ids)";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("ids",d.Keys.ToArray());await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false))d[r.GetGuid(0)].Statutory=new(r.GetString(1),r.GetInt64(2),r.GetInt64(3),r.GetInt64(4),r.GetString(5));}
    private static async Task<IReadOnlyList<FiscalXReadingSourceGap>> ReadSourceGapsAsync(NpgsqlConnection c,NpgsqlTransaction t,IReadOnlyList<FiscalXReadingSourceDocument>d,CancellationToken ct){var ids=d.Where(x=>x.SequencePolicyId.HasValue).Select(x=>x.SequencePolicyId!.Value).Distinct().ToArray();if(ids.Length==0)return[];const string sql="SELECT g.fiscal_sequence_gap_audit_id,g.fiscal_sequence_policy_id,g.gap_sequence_value,code.code_key FROM pos.fiscal_sequence_gap_audit g JOIN pos.controlled_codes code ON code.controlled_code_id=g.gap_reason_code_id WHERE g.fiscal_sequence_policy_id=ANY(@ids) ORDER BY g.fiscal_sequence_policy_id,g.gap_sequence_value";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("ids",ids);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var result=new List<FiscalXReadingSourceGap>();while(await r.ReadAsync(ct).ConfigureAwait(false))result.Add(new(r.GetGuid(0),r.GetGuid(1),r.GetInt64(2),r.GetString(3)));return result;}

    private static async Task<IReadOnlyList<FiscalXReadingTenderBreakdown>> ReadTenderChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct){const string sql="SELECT code.code_key,x.tender_transaction_count,x.amount_minor_units,x.currency_code FROM pos.fiscal_report_tender_breakdowns x JOIN pos.controlled_codes code ON code.controlled_code_id=x.tender_classification_code_id WHERE x.x_z_report_id=@id ORDER BY code.sort_order,code.code_key";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var result=new List<FiscalXReadingTenderBreakdown>();while(await r.ReadAsync(ct).ConfigureAwait(false))result.Add(new(r.GetString(0),r.GetInt64(1),r.GetInt64(2),r.GetString(3)));return result;}
    private static async Task<IReadOnlyList<FiscalXReadingDiscountBreakdown>> ReadDiscountChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct){const string sql="SELECT code.code_key,x.qualifying_document_count,x.discount_amount_minor_units,x.vat_exemption_amount_minor_units,x.currency_code FROM pos.fiscal_report_discount_breakdowns x JOIN pos.controlled_codes code ON code.controlled_code_id=x.discount_classification_code_id WHERE x.x_z_report_id=@id ORDER BY code.sort_order,code.code_key";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var result=new List<FiscalXReadingDiscountBreakdown>();while(await r.ReadAsync(ct).ConfigureAwait(false))result.Add(new(r.GetString(0),r.GetInt64(1),r.GetInt64(2),r.GetInt64(3),r.GetString(4)));return result;}
    private static async Task<IReadOnlyList<FiscalXReadingFiscalNumberRange>> ReadRangeChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct){const string sql="SELECT fiscal_report_fiscal_number_range_id,fiscal_sequence_policy_id,fiscal_series,first_sequence_value,last_sequence_value,first_fiscal_number,last_fiscal_number,qualifying_document_count,currency_code FROM pos.fiscal_report_fiscal_number_ranges WHERE x_z_report_id=@id ORDER BY fiscal_series,first_sequence_value";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var raw=new List<(Guid,Guid,string,long,long,string,string,long,string)>();while(await r.ReadAsync(ct).ConfigureAwait(false))raw.Add((r.GetGuid(0),r.GetGuid(1),r.GetString(2),r.GetInt64(3),r.GetInt64(4),r.GetString(5),r.GetString(6),r.GetInt64(7),r.GetString(8)));await r.DisposeAsync().ConfigureAwait(false);var result=new List<FiscalXReadingFiscalNumberRange>();foreach(var x in raw)result.Add(new(x.Item2,x.Item3,x.Item4,x.Item5,x.Item6,x.Item7,x.Item8,await ReadGapChildrenAsync(c,t,x.Item1,ct).ConfigureAwait(false),x.Item9));return result;}
    private static async Task<IReadOnlyList<FiscalXReadingSequenceGap>> ReadGapChildrenAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct){const string sql="SELECT x.gap_sequence_value,code.code_key FROM pos.fiscal_report_sequence_gaps x JOIN pos.controlled_codes code ON code.controlled_code_id=x.gap_classification_code_id WHERE x.fiscal_report_fiscal_number_range_id=@id ORDER BY x.gap_sequence_value";await using var cmd=CreateCommand(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);var result=new List<FiscalXReadingSequenceGap>();while(await r.ReadAsync(ct).ConfigureAwait(false))result.Add(new(r.GetInt64(0),r.GetString(1)));return result;}

    private static void AddAmounts(NpgsqlParameterCollection p,FiscalXReadingAmounts a){p.AddWithValue("gross",a.GrossSalesAmountMinorUnits);p.AddWithValue("net",a.NetSalesAmountMinorUnits);p.AddWithValue("vatable",a.VatableSalesAmountMinorUnits);p.AddWithValue("vat",a.VatAmountMinorUnits);p.AddWithValue("vat_exempt",a.VatExemptSalesAmountMinorUnits);p.AddWithValue("zero_rated",a.ZeroRatedSalesAmountMinorUnits);p.AddWithValue("discount",a.DiscountAmountMinorUnits);p.AddWithValue("senior",a.SeniorCitizenDiscountAmountMinorUnits);p.AddWithValue("pwd",a.PwdDiscountAmountMinorUnits);p.AddWithValue("other_stat",a.OtherStatutoryDiscountAmountMinorUnits);p.AddWithValue("vat_exemption",a.VatExemptionAmountMinorUnits);p.AddWithValue("void",a.VoidAmountMinorUnits);p.AddWithValue("refund",a.RefundAmountMinorUnits);p.AddWithValue("return",a.ReturnAmountMinorUnits);p.AddWithValue("adjustment",a.AdjustmentAmountMinorUnits);}
    private static void AddNullable(NpgsqlParameterCollection p,string name,object? value,NpgsqlDbType type)=>p.Add(new NpgsqlParameter(name,type){Value=value??DBNull.Value});
    private static NpgsqlCommand CreateCommand(NpgsqlConnection c,NpgsqlTransaction? t,string sql){var command=c.CreateCommand();command.CommandText=sql;command.Transaction=t;return command;}
    private static async Task ExecuteAsync(NpgsqlConnection c,NpgsqlTransaction? t,string sql,CancellationToken ct,Action<NpgsqlParameterCollection>? add=null){await using var command=CreateCommand(c,t,sql);add?.Invoke(command.Parameters);await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<DateTimeOffset> ClockAsync(NpgsqlConnection c,NpgsqlTransaction t,CancellationToken ct){await using var cmd=CreateCommand(c,t,"SELECT clock_timestamp()");var value=await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);return value switch{DateTimeOffset x=>x,DateTime x=>new DateTimeOffset(DateTime.SpecifyKind(x,DateTimeKind.Utc)),_=>throw new InvalidOperationException("PostgreSQL did not return a supported timestamp value.")};}
    private static BirSalesSummaryResult Failure(BirSalesSummaryOutcome outcome,string message)=>new(outcome,SafeMessage:message);

    private sealed class SourceBuilder(Guid id,string type,string status,DateTimeOffset created,Guid? policy,long? sequence,string? number,string? series){public List<FiscalXReadingSourceLine> Lines{get;}=[];public List<FiscalXReadingSourceTender>Tenders{get;}=[];public List<FiscalXReadingSourceTax>Taxes{get;}=[];public List<FiscalXReadingSourceDiscount>Discounts{get;}=[];public FiscalXReadingSourceStatutory? Statutory{get;set;}public FiscalXReadingSourceDocument Build()=>new(id,type,status,created,policy,sequence,number,series,Lines,Tenders,Taxes,Discounts,Statutory);}
    private sealed record CodeIds(Dictionary<(string,string),Guid> Values){public Guid Get(string set,string key)=>Values[(set,key)];}
    private sealed record GoverningZ(Guid Id,string Reference,Guid ContractId,Guid FiscalReportingPeriodId,Guid SitePosServerId,Guid FiscalIdentityId,DateOnly BusinessDayDate,DateTimeOffset PeriodStartAt,DateTimeOffset PeriodEndAt,string ReportingTimezoneName,string CurrencyCode,long TransactionCount,string? BeginningSiRef,string? EndingSiRef,long? FirstSequenceValue,long? LastSequenceValue,long FiscalSequenceGapCount,long ResetCounterValue,long ZCounterValue,long PreviousGrandTotal,long CurrentGrandTotal,long PresentGrandTotal,FiscalXReadingAmounts Amounts,DateTimeOffset GeneratedAt,DateTimeOffset CommittedAt,string PeriodStatus,DateTimeOffset? ClosedAt,string RequestStatus,FiscalZReadingCounterSnapshot CounterSnapshot);
}
