using System.Data;
using System.Security.Cryptography;
using System.Text;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresAnnexE1Repository(
    NpgsqlDataSource dataSource,
    AnnexE1CalculationEngine calculationEngine,
    AnnexE1DeterministicXlsxRenderer renderer,
    IAnnexE1ArtifactStore artifactStore) : IAnnexE1Repository
{
    private static readonly Guid ZKindId = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");
    private static readonly string[] RequiredFactTypes =
    [
        AnnexE1FactTypes.ManualSiOrNetIncome, AnnexE1FactTypes.SalesOverrunOverflowNetIncome,
        AnnexE1FactTypes.NaacDiscount, AnnexE1FactTypes.SoloParentDiscount,
        AnnexE1FactTypes.OtherVatAdjustment, AnnexE1FactTypes.VatOnReturns,
        AnnexE1FactTypes.ResidualVatAdjustment
    ];

    public async Task<AnnexE1FactResult> RecordFactAsync(AnnexE1PeriodFactCommand command, CancellationToken cancellationToken = default)
    {
        var semanticHash = AnnexE1SemanticHasher.ComputeFact(command);
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            await AcquireLockAsync(connection, transaction, $"annex-e1-fact:v1:{command.SitePosServerId:D}:{command.FiscalIdentityId:D}:{command.CurrencyCode}:{command.FiscalReportingPeriodId:D}:{command.FactType}", cancellationToken).ConfigureAwait(false);

            var replay = await ReadFactByOperationAsync(connection, transaction, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (replay is not null)
                return string.Equals(replay.SemanticHash, semanticHash, StringComparison.Ordinal)
                    ? new(AnnexE1Outcome.Replayed, replay)
                    : FailureFact(AnnexE1Outcome.Conflict, "The accounting-fact operation is already bound to different governed semantics.");

            var period = await ReadPeriodAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            if (period is null) return FailureFact(AnnexE1Outcome.ScopeMismatch, "The governed Annex E-1 period scope is unavailable.");
            var typeId = await ResolveCodeAsync(connection, transaction, "annex_e1_accounting_fact_type", command.FactType, cancellationToken).ConfigureAwait(false);
            var statusId = await ResolveCodeAsync(connection, transaction, "annex_e1_accounting_fact_status", command.FactStatus, cancellationToken).ConfigureAwait(false);
            Guid? reasonId = command.CorrectionReason is null ? null : await ResolveCodeAsync(connection, transaction, "annex_e1_correction_reason", command.CorrectionReason, cancellationToken).ConfigureAwait(false);
            if (command.FactStatus == AnnexE1FactStatuses.Recorded && command.FactType is not (AnnexE1FactTypes.ManualSiOrNetIncome or AnnexE1FactTypes.SalesOverrunOverflowNetIncome))
                return FailureFact(AnnexE1Outcome.UnsupportedPrivilege, "The bounded Annex E-1 profile permits only explicit zero evidence for this unresolved classification.");

            var id = Guid.NewGuid();
            var recordedAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            const string sql = """
            INSERT INTO pos.annex_e1_period_accounting_facts(
              annex_e1_period_accounting_fact_id,operation_key,site_pos_server_id,fiscal_identity_id,currency_code,
              fiscal_reporting_period_id,business_day_date,fact_type_code_id,fact_status_code_id,amount_minor_units,
              source_document_count,first_source_reference,last_source_reference,source_event_reference,approval_reference,
              semantic_hash_version,semantic_hash,supersedes_fact_id,correction_reason_code_id,effective_at,recorded_at,
              recorded_by_ref,service_identity_ref,correlation_id,created_at)
            VALUES(@id,@operation,@site,@identity,@currency,@period,@business,@type,@status,@amount,@count,@first,@last,@event,
              @approval,@hash_version,@hash,@supersedes,@reason,@effective,@recorded,@actor,@service,@correlation,@recorded)
            """;
            await using (var insert = Command(connection, transaction, sql))
            {
                insert.Parameters.AddWithValue("id", id); insert.Parameters.AddWithValue("operation", command.OperationKey);
                insert.Parameters.AddWithValue("site", command.SitePosServerId); insert.Parameters.AddWithValue("identity", command.FiscalIdentityId);
                insert.Parameters.AddWithValue("currency", command.CurrencyCode); insert.Parameters.AddWithValue("period", command.FiscalReportingPeriodId);
                insert.Parameters.AddWithValue("business", period.Value.BusinessDayDate); insert.Parameters.AddWithValue("type", typeId);
                insert.Parameters.AddWithValue("status", statusId); insert.Parameters.AddWithValue("amount", command.AmountMinorUnits);
                insert.Parameters.AddWithValue("count", command.SourceDocumentCount); AddNullable(insert, "first", command.FirstSourceReference, NpgsqlDbType.Text);
                AddNullable(insert, "last", command.LastSourceReference, NpgsqlDbType.Text); AddNullable(insert, "event", command.SourceEventReference, NpgsqlDbType.Text);
                insert.Parameters.AddWithValue("approval", command.ApprovalReference); insert.Parameters.AddWithValue("hash_version", AnnexE1Contract.FactSemanticHashVersion);
                insert.Parameters.AddWithValue("hash", semanticHash); AddNullable(insert, "supersedes", command.SupersedesFactId, NpgsqlDbType.Uuid);
                AddNullable(insert, "reason", reasonId, NpgsqlDbType.Uuid); insert.Parameters.AddWithValue("effective", period.Value.PeriodEndAt);
                insert.Parameters.AddWithValue("recorded", recordedAt); insert.Parameters.AddWithValue("actor", command.RequestedByRef);
                insert.Parameters.AddWithValue("service", command.ServiceIdentityRef); insert.Parameters.AddWithValue("correlation", command.CorrelationId);
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            var record = await ReadFactAsync(connection, transaction, id, cancellationToken).ConfigureAwait(false)
                ?? throw new AnnexE1SafeException(AnnexE1Outcome.PersistenceFailure, "The committed accounting fact was not readable in its transaction.");
            await InsertAuditAsync(connection,transaction,command.SitePosServerId,command.RequestedByRef,command.ServiceIdentityRef,command.CorrelationId,recordedAt,cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(AnnexE1Outcome.Created, record);
        }
        catch (AnnexE1SafeException exception) { return FailureFact(exception.Outcome, exception.Message); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                var replay = await ReadFactByOperationAsync(connection, null, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
                return replay is not null && string.Equals(replay.SemanticHash, semanticHash, StringComparison.Ordinal)
                    ? new(AnnexE1Outcome.Replayed, replay)
                    : FailureFact(AnnexE1Outcome.Conflict, "The accounting fact conflicts with existing immutable evidence.");
            }
            catch (NpgsqlException) { return FailureFact(AnnexE1Outcome.UnknownCommitOutcome, "The accounting-fact outcome requires durable-state reconciliation."); }
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure) { return FailureFact(AnnexE1Outcome.RetryableConcurrencyFailure, "The accounting fact encountered a retryable concurrency conflict."); }
        catch (NpgsqlException) { return FailureFact(AnnexE1Outcome.PersistenceFailure, "The accounting fact failed safely."); }
    }

    public async Task<AnnexE1WorkbookResult> GenerateAsync(AnnexE1GenerationCommand command, CancellationToken cancellationToken = default)
    {
        string? semanticHash = null;
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            await AcquireLockAsync(connection, transaction, $"annex-e1-workbook:v1:{command.SitePosServerId:D}:{command.FiscalIdentityId:D}:{command.CurrencyCode}:{command.CalendarYear:D4}{command.CalendarMonth:D2}:{command.Profile}", cancellationToken).ConfigureAwait(false);
            var sources = await ReadSourcesAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            if (sources.Count == 0) return FailureWorkbook(AnnexE1Outcome.MissingAuthoritativeSource, "No committed Z-bound BIR Sales Summary exists for the governed month.");

            var generatedAt = await ClockAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var header = BuildHeader(sources, generatedAt, command.RequestedByRef);
            var inputs = new List<AnnexE1RowInputs>(sources.Count);
            foreach (var source in sources)
            {
                var facts = await ReadCurrentFactsAsync(connection, transaction, source.PeriodId, cancellationToken).ConfigureAwait(false);
                inputs.Add(ToInputs(source, facts));
            }
            semanticHash = AnnexE1SemanticHasher.ComputeWorkbook(command, inputs, header);

            var operation = await ReadWorkbookByOperationAsync(connection, transaction, command.SitePosServerId, command.OperationKey, cancellationToken).ConfigureAwait(false);
            if (operation is not null)
                return string.Equals(operation.SemanticHash, semanticHash, StringComparison.Ordinal)
                    ? new(AnnexE1Outcome.Replayed, operation)
                    : FailureWorkbook(AnnexE1Outcome.Conflict, "The Annex E-1 operation is already bound to different source semantics.");

            var current = await ReadCurrentWorkbookAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            if (command.SupersedesWorkbookId is null)
            {
                if (current is not null)
                    return string.Equals(current.SemanticHash, semanticHash, StringComparison.Ordinal)
                        ? new(AnnexE1Outcome.Replayed, current)
                        : FailureWorkbook(AnnexE1Outcome.Conflict, "Changed source semantics require an explicit immutable correction.");
            }
            else if (current is null || current.WorkbookId != command.SupersedesWorkbookId)
                return FailureWorkbook(AnnexE1Outcome.Conflict, "The correction must supersede the current workbook in the exact governed scope.");

            var rows = inputs.Select(calculationEngine.Calculate).ToArray();
            var bytes = renderer.Render(header, rows);
            var artifactSha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var artifactKey = await artifactStore.PublishAsync(bytes, artifactSha, cancellationToken).ConfigureAwait(false);
            var revision = current is null ? 1 : checked(current.Revision + 1);
            var workbookId = Guid.NewGuid();
            var reference = $"AE1-{command.CalendarYear:D4}{command.CalendarMonth:D2}-{semanticHash[..16].ToUpperInvariant()}-R{revision:D3}";
            var fileName = BuildFileName(sources[0].FiscalIdentityCode, sources[0].MachineIdentificationNumber, command.CalendarYear, command.CalendarMonth);
            var statusId = await ResolveCodeAsync(connection, transaction, "annex_e1_workbook_status", "committed", cancellationToken).ConfigureAwait(false);
            Guid? correctionReasonId = command.CorrectionReason is null ? null : await ResolveCodeAsync(connection, transaction, "annex_e1_correction_reason", command.CorrectionReason, cancellationToken).ConfigureAwait(false);
            await InsertWorkbookAsync(connection, transaction, command, header, workbookId, reference, revision, semanticHash, artifactSha, bytes.LongLength, artifactKey, fileName, statusId, correctionReasonId, generatedAt, cancellationToken).ConfigureAwait(false);
            await InsertRowsAsync(connection, transaction, workbookId, rows, cancellationToken).ConfigureAwait(false);
            var record = await ReadWorkbookAsync(connection, transaction, workbookId, cancellationToken).ConfigureAwait(false)
                ?? throw new AnnexE1SafeException(AnnexE1Outcome.PersistenceFailure, "The Annex E-1 workbook was not readable in its transaction.");
            await InsertAuditAsync(connection,transaction,command.SitePosServerId,command.RequestedByRef,command.ServiceIdentityRef,command.CorrelationId,generatedAt,cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(AnnexE1Outcome.Created, record);
        }
        catch (AnnexE1SafeException exception) { return FailureWorkbook(exception.Outcome, exception.Message); }
        catch (OverflowException) { return FailureWorkbook(AnnexE1Outcome.ArithmeticOverflow, "Annex E-1 checked minor-unit arithmetic exceeded supported bounds."); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return await ReconcileGenerationOutcomeAsync(command, semanticHash, cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure) { return FailureWorkbook(AnnexE1Outcome.RetryableConcurrencyFailure, "Annex E-1 generation encountered a retryable concurrency conflict."); }
        catch (NpgsqlException)
        {
            return semanticHash is null
                ? FailureWorkbook(AnnexE1Outcome.PersistenceFailure, "Annex E-1 generation failed safely before an authoritative result was prepared.")
                : await ReconcileGenerationOutcomeAsync(command, semanticHash, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) { return FailureWorkbook(AnnexE1Outcome.ArtifactUnavailable, "The Annex E-1 artifact could not be published safely."); }
    }

    public async Task<AnnexE1WorkbookResult> GetAsync(Guid workbookId, CancellationToken cancellationToken = default)
    {
        try { await using var c = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false); var record = await ReadWorkbookAsync(c, null, workbookId, cancellationToken).ConfigureAwait(false); return record is null ? new(AnnexE1Outcome.NotFound) : new(AnnexE1Outcome.Replayed, record); }
        catch (NpgsqlException) { return FailureWorkbook(AnnexE1Outcome.PersistenceFailure, "Annex E-1 metadata readback failed safely."); }
    }

    public async Task<AnnexE1ArtifactResult> DownloadAsync(Guid workbookId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var c = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var record = await ReadWorkbookAsync(c, null, workbookId, cancellationToken).ConfigureAwait(false);
            if (record is null) return new(AnnexE1Outcome.NotFound);
            var key = await ReadArtifactKeyAsync(c, workbookId, cancellationToken).ConfigureAwait(false);
            var bytes = key is null ? null : await artifactStore.ReadAsync(key, cancellationToken).ConfigureAwait(false);
            if (bytes is null) return new(AnnexE1Outcome.ArtifactUnavailable, record, SafeMessage: "The immutable Annex E-1 artifact is unavailable.");
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (bytes.LongLength != record.ArtifactByteLength || !string.Equals(hash, record.ArtifactSha256, StringComparison.Ordinal))
                return new(AnnexE1Outcome.ArtifactIntegrityFailure, record, SafeMessage: "The immutable Annex E-1 artifact failed integrity validation.");
            return new(AnnexE1Outcome.Replayed, record, bytes);
        }
        catch (NpgsqlException) { return new(AnnexE1Outcome.PersistenceFailure, SafeMessage: "Annex E-1 artifact readback failed safely."); }
        catch (IOException) { return new(AnnexE1Outcome.ArtifactUnavailable, SafeMessage: "The immutable Annex E-1 artifact is unavailable."); }
    }

    private static AnnexE1RowInputs ToInputs(Source s, IReadOnlyDictionary<string, AnnexE1PeriodFactRecord> facts)
    {
        foreach (var type in RequiredFactTypes) if (!facts.ContainsKey(type)) throw new AnnexE1SafeException(AnnexE1Outcome.MissingAccountingFact, $"The required governed Annex E-1 fact '{type}' is unavailable.");
        var zeroOnly = new[] { AnnexE1FactTypes.NaacDiscount, AnnexE1FactTypes.SoloParentDiscount, AnnexE1FactTypes.OtherVatAdjustment, AnnexE1FactTypes.VatOnReturns, AnnexE1FactTypes.ResidualVatAdjustment };
        if (zeroOnly.Any(type => facts[type].FactStatus != AnnexE1FactStatuses.AttestedZero || facts[type].AmountMinorUnits != 0))
            throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedPrivilege, "A nonzero or unattested unresolved Annex E-1 classification is not authorized.");
        var ordered = RequiredFactTypes.Select(type => facts[type]).ToArray();
        return new(s.PeriodId,s.PeriodSequence,s.ZId,s.ZReference,s.BirId,s.BirSemanticHash,s.BusinessDate,s.BeginningNumber,s.EndingNumber,
            s.PreviousGta,s.ResultingGta,facts[AnnexE1FactTypes.ManualSiOrNetIncome].AmountMinorUnits,s.GrossSales,s.ReturnAmount,s.VoidAmount,
            s.VatableSales,s.VatAmount,s.VatExemptSales,s.ZeroRatedSales,s.ScDiscount,s.PwdDiscount,facts[AnnexE1FactTypes.NaacDiscount].AmountMinorUnits,
            facts[AnnexE1FactTypes.SoloParentDiscount].AmountMinorUnits,s.OtherStatutoryDiscount,s.CouponDiscount,s.PromotionalDiscount,s.ScVatAdjustment,
            s.PwdVatAdjustment,facts[AnnexE1FactTypes.OtherVatAdjustment].AmountMinorUnits,facts[AnnexE1FactTypes.VatOnReturns].AmountMinorUnits,
            facts[AnnexE1FactTypes.ResidualVatAdjustment].AmountMinorUnits,facts[AnnexE1FactTypes.SalesOverrunOverflowNetIncome].AmountMinorUnits,
            s.ResetCounter,s.ZCounter,s.TransactionCount,s.RefundAmount,s.AdjustmentAmount,s.ServiceChargeAmount,s.RangeCount,s.GapCount,
            ordered.Select(f => f.FactId).ToArray(),ordered.Select(f => f.SemanticHash).ToArray());
    }

    private static AnnexE1Header BuildHeader(IReadOnlyList<Source> sources, DateTimeOffset at, string actor)
    {
        var first = sources[0];
        if (sources.Any(s => s.TaxpayerName != first.TaxpayerName || s.TaxpayerAddress != first.TaxpayerAddress || s.Tin != first.Tin || s.PosSerialNumber != first.PosSerialNumber || s.MachineIdentificationNumber != first.MachineIdentificationNumber || s.SiteCode != first.SiteCode))
            throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedClassification, "Historical Annex E-1 header facts are not stable across the governed month.");
        if (new[] { first.TaxpayerName, first.TaxpayerAddress, first.Tin, first.PosSerialNumber, first.MachineIdentificationNumber, first.SiteCode }.Any(string.IsNullOrWhiteSpace))
            throw new AnnexE1SafeException(AnnexE1Outcome.MissingAuthoritativeSource, "A mandatory immutable Annex E-1 header fact is unavailable.");
        return new(first.TaxpayerName,first.TaxpayerAddress,first.Tin,AnnexE1Contract.SoftwareName,AnnexE1Contract.SoftwareVersion,AnnexE1Contract.ReleaseNumber,AnnexE1Contract.ReleaseDate,first.PosSerialNumber,first.MachineIdentificationNumber,first.SiteCode,at,actor);
    }

    private static string BuildFileName(string fiscalIdentityCode,string min,int year,int month)
    {
        static string Safe(string value) { var result = new string(value.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_').ToArray()).Trim('_','.'); return string.IsNullOrEmpty(result) ? "UNAVAILABLE" : result[..Math.Min(result.Length,64)]; }
        return $"ANNEX-E1_{Safe(fiscalIdentityCode)}_{Safe(min)}_{year:D4}{month:D2}_v1.xlsx";
    }

    private static async Task<List<Source>> ReadSourcesAsync(NpgsqlConnection c,NpgsqlTransaction t,AnnexE1GenerationCommand command,CancellationToken ct)
    {
        const string sql="""
        SELECT p.fiscal_reporting_period_id,p.period_sequence,p.business_day_date,ps.code_key,p.closed_at,
          z.x_z_report_id,z.report_number,zreqs.code_key,b.bir_sales_summary_report_id,breq.semantic_request_hash,
          b.beginning_si_ref,b.ending_si_ref,b.previous_grand_total_amount_minor_units,b.present_grand_total_amount_minor_units,
          b.gross_sales_amount_minor_units,b.vatable_sales_amount_minor_units,b.vat_amount_minor_units,b.vat_exempt_sales_amount_minor_units,b.zero_rated_sales_amount_minor_units,
          b.senior_citizen_discount_amount_minor_units,b.pwd_discount_amount_minor_units,b.other_statutory_discount_amount_minor_units,
          z.coupon_discount_amount_minor_units,z.promotional_discount_amount_minor_units,b.void_amount_minor_units,b.return_amount_minor_units,b.refund_amount_minor_units,b.adjustment_amount_minor_units,z.service_charge_amount_minor_units,
          b.reset_counter_value,b.z_counter_value,b.transaction_count,
          COALESCE((SELECT count(*) FROM pos.fiscal_report_fiscal_number_ranges fr WHERE fr.x_z_report_id=z.x_z_report_id),0),
          COALESCE((SELECT sum(fr.gap_count) FROM pos.fiscal_report_fiscal_number_ranges fr WHERE fr.x_z_report_id=z.x_z_report_id),0),
          COALESCE((SELECT d.vat_exemption_amount_minor_units FROM pos.fiscal_report_discount_breakdowns d JOIN pos.controlled_codes dc ON dc.controlled_code_id=d.discount_classification_code_id WHERE d.x_z_report_id=z.x_z_report_id AND dc.code_key='senior_citizen_statutory'),0),
          COALESCE((SELECT d.vat_exemption_amount_minor_units FROM pos.fiscal_report_discount_breakdowns d JOIN pos.controlled_codes dc ON dc.controlled_code_id=d.discount_classification_code_id WHERE d.x_z_report_id=z.x_z_report_id AND dc.code_key='pwd_statutory'),0),
          fi.fiscal_identity_code,COALESCE(fi.registered_business_name,fi.taxpayer_display_name),fi.registered_business_address,fi.tin,
          b.pos_serial_number,b.machine_identification_number,s.site_pos_server_code
        FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes ps ON ps.controlled_code_id=p.period_status_code_id
        LEFT JOIN pos.x_z_reports z ON z.fiscal_reporting_period_id=p.fiscal_reporting_period_id AND z.report_kind_code_id=@zkind
        LEFT JOIN pos.fiscal_report_requests zreq ON zreq.fiscal_report_request_id=z.fiscal_report_request_id LEFT JOIN pos.controlled_codes zreqs ON zreqs.controlled_code_id=zreq.report_status_code_id
        LEFT JOIN pos.bir_sales_summary_reports b ON b.governing_z_report_id=z.x_z_report_id
        LEFT JOIN pos.fiscal_report_requests breq ON breq.fiscal_report_request_id=b.fiscal_report_request_id
        JOIN pos.fiscal_identities fi ON fi.fiscal_identity_id=p.fiscal_identity_id JOIN pos.site_pos_servers s ON s.site_pos_server_id=p.site_pos_server_id
        WHERE p.site_pos_server_id=@site AND p.fiscal_identity_id=@identity AND p.currency_code=@currency
          AND EXTRACT(YEAR FROM p.business_day_date)=@year AND EXTRACT(MONTH FROM p.business_day_date)=@month
        ORDER BY p.period_sequence FOR SHARE OF p,fi,s
        """;
        await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("zkind",ZKindId);cmd.Parameters.AddWithValue("site",command.SitePosServerId);cmd.Parameters.AddWithValue("identity",command.FiscalIdentityId);cmd.Parameters.AddWithValue("currency",command.CurrencyCode);cmd.Parameters.AddWithValue("year",command.CalendarYear);cmd.Parameters.AddWithValue("month",command.CalendarMonth);
        var result=new List<Source>();await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false))
        {
            if(r.IsDBNull(4)||r.IsDBNull(5)||r.IsDBNull(8)||!string.Equals(r.GetString(3),"closed",StringComparison.Ordinal)||!string.Equals(r.GetString(7),"committed",StringComparison.Ordinal))throw new AnnexE1SafeException(AnnexE1Outcome.GoverningZNotCommitted,"Every configured period in the month requires a committed Z Reading and committed BIR Sales Summary.");
            result.Add(new(r.GetGuid(0),r.GetInt64(1),r.GetFieldValue<DateOnly>(2),r.GetGuid(5),r.GetString(6),r.GetGuid(8),r.GetString(9),r.IsDBNull(10)?null:r.GetString(10),r.IsDBNull(11)?null:r.GetString(11),r.GetInt64(12),r.GetInt64(13),r.GetInt64(14),r.GetInt64(15),r.GetInt64(16),r.GetInt64(17),r.GetInt64(18),r.GetInt64(19),r.GetInt64(20),r.GetInt64(21),r.GetInt64(22),r.GetInt64(23),r.GetInt64(24),r.GetInt64(25),r.GetInt64(26),r.GetInt64(27),r.GetInt64(28),r.GetInt64(29),r.GetInt64(30),r.GetInt64(31),r.GetInt64(32),r.GetInt64(33),r.GetInt64(34),r.GetInt64(35),r.GetString(36),r.IsDBNull(37)?"":r.GetString(37),r.IsDBNull(38)?"":r.GetString(38),r.IsDBNull(39)?"":r.GetString(39),r.GetString(40),r.GetString(41),r.GetString(42)));
        } return result;
    }

    private static async Task<IReadOnlyDictionary<string,AnnexE1PeriodFactRecord>> ReadCurrentFactsAsync(NpgsqlConnection c,NpgsqlTransaction t,Guid periodId,CancellationToken ct)
    {
        const string sql="""SELECT f.annex_e1_period_accounting_fact_id,f.operation_key,f.site_pos_server_id,f.fiscal_identity_id,f.currency_code,f.fiscal_reporting_period_id,f.business_day_date,ft.code_key,fs.code_key,f.amount_minor_units,f.source_document_count,f.first_source_reference,f.last_source_reference,f.source_event_reference,f.approval_reference,f.semantic_hash,f.supersedes_fact_id,cr.code_key,f.effective_at,f.recorded_at,f.recorded_by_ref,f.service_identity_ref,f.correlation_id FROM pos.annex_e1_period_accounting_facts f JOIN pos.controlled_codes ft ON ft.controlled_code_id=f.fact_type_code_id JOIN pos.controlled_codes fs ON fs.controlled_code_id=f.fact_status_code_id LEFT JOIN pos.controlled_codes cr ON cr.controlled_code_id=f.correction_reason_code_id WHERE f.fiscal_reporting_period_id=@period AND NOT EXISTS(SELECT 1 FROM pos.annex_e1_period_accounting_facts child WHERE child.supersedes_fact_id=f.annex_e1_period_accounting_fact_id) ORDER BY ft.code_key FOR SHARE OF f""";
        await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("period",periodId);var result=new Dictionary<string,AnnexE1PeriodFactRecord>(StringComparer.Ordinal);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false)){var fact=MapFact(r);if(!result.TryAdd(fact.FactType,fact))throw new AnnexE1SafeException(AnnexE1Outcome.MissingAccountingFact,"Duplicate current Annex E-1 accounting facts are not authoritative.");}return result;
    }

    private static async Task InsertWorkbookAsync(NpgsqlConnection c,NpgsqlTransaction t,AnnexE1GenerationCommand q,AnnexE1Header h,Guid id,string reference,int revision,string semantic,string artifact,long length,string key,string file,Guid status,Guid? reason,DateTimeOffset at,CancellationToken ct)
    {
        const string sql="""INSERT INTO pos.annex_e1_workbooks(annex_e1_workbook_id,annex_e_reference,operation_key,site_pos_server_id,fiscal_identity_id,currency_code,calendar_year,calendar_month,profile_ref,calculation_profile_ref,calculation_profile_sha256,template_sha256,renderer_version,revision,semantic_hash_version,semantic_hash,artifact_sha256,artifact_byte_length,artifact_storage_key,mime_type,file_name,taxpayer_name,taxpayer_address,tin,pos_serial_number,machine_identification_number,pos_terminal_number,software_name,software_version,release_number,release_date,workbook_status_code_id,supersedes_workbook_id,correction_reason_code_id,correction_approval_reference,generated_at,committed_at,generated_by_ref,service_identity_ref,correlation_id,created_at) VALUES(@id,@ref,@operation,@site,@identity,@currency,@year,@month,@profile,@calculation,@calculation_hash,@template,@renderer,@revision,@hash_version,@semantic,@artifact,@length,@key,@mime,@file,@name,@address,@tin,@serial,@min,@terminal,@software,@software_version,@release,@release_date,@status,@supersedes,@reason,@approval,@at,@at,@actor,@service,@correlation,@at)""";
        await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("id",id);cmd.Parameters.AddWithValue("ref",reference);cmd.Parameters.AddWithValue("operation",q.OperationKey);cmd.Parameters.AddWithValue("site",q.SitePosServerId);cmd.Parameters.AddWithValue("identity",q.FiscalIdentityId);cmd.Parameters.AddWithValue("currency",q.CurrencyCode);cmd.Parameters.AddWithValue("year",q.CalendarYear);cmd.Parameters.AddWithValue("month",q.CalendarMonth);cmd.Parameters.AddWithValue("profile",q.Profile);cmd.Parameters.AddWithValue("calculation",AnnexE1Contract.CalculationProfile);cmd.Parameters.AddWithValue("calculation_hash",AnnexE1Contract.CalculationProfileSha256);cmd.Parameters.AddWithValue("template",AnnexE1Contract.OfficialTemplateSha256);cmd.Parameters.AddWithValue("renderer",AnnexE1Contract.RendererVersion);cmd.Parameters.AddWithValue("revision",revision);cmd.Parameters.AddWithValue("hash_version",AnnexE1Contract.SemanticHashVersion);cmd.Parameters.AddWithValue("semantic",semantic);cmd.Parameters.AddWithValue("artifact",artifact);cmd.Parameters.AddWithValue("length",length);cmd.Parameters.AddWithValue("key",key);cmd.Parameters.AddWithValue("mime",AnnexE1Contract.MimeType);cmd.Parameters.AddWithValue("file",file);cmd.Parameters.AddWithValue("name",h.TaxpayerName);cmd.Parameters.AddWithValue("address",h.TaxpayerAddress);cmd.Parameters.AddWithValue("tin",h.Tin);cmd.Parameters.AddWithValue("serial",h.PosSerialNumber);cmd.Parameters.AddWithValue("min",h.MachineIdentificationNumber);cmd.Parameters.AddWithValue("terminal",h.PosTerminalNumber);cmd.Parameters.AddWithValue("software",h.SoftwareName);cmd.Parameters.AddWithValue("software_version",h.SoftwareVersion);cmd.Parameters.AddWithValue("release",h.ReleaseNumber);cmd.Parameters.AddWithValue("release_date",h.ReleaseDate);cmd.Parameters.AddWithValue("status",status);AddNullable(cmd,"supersedes",q.SupersedesWorkbookId,NpgsqlDbType.Uuid);AddNullable(cmd,"reason",reason,NpgsqlDbType.Uuid);AddNullable(cmd,"approval",q.CorrectionApprovalReference,NpgsqlDbType.Text);cmd.Parameters.AddWithValue("at",at);cmd.Parameters.AddWithValue("actor",q.RequestedByRef);cmd.Parameters.AddWithValue("service",q.ServiceIdentityRef);cmd.Parameters.AddWithValue("correlation",q.CorrelationId);await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task InsertRowsAsync(NpgsqlConnection c,NpgsqlTransaction t,Guid workbookId,IReadOnlyList<AnnexE1Row> rows,CancellationToken ct)
    {
        const string sql="""INSERT INTO pos.annex_e_reports(annex_e_report_id,annex_e1_workbook_id,row_sequence,fiscal_reporting_period_id,governing_z_report_id,governing_report_kind_code_id,related_bir_sales_summary_report_id,site_pos_server_id,fiscal_identity_id,currency_code,business_day_date,beginning_fiscal_number,ending_fiscal_number,d04_gta_ending,d05_gta_beginning,d06_manual_net_income,d07_annex_gross_sales,d08_vatable_sales,d09_vat_amount,d10_vat_exempt_sales,d11_zero_rated_sales,d12_sc_discount,d13_pwd_discount,d14_naac_discount,d15_solo_parent_discount,d16_other_discount,d17_returns,d18_voids,d19_total_deductions,d20_sc_vat_adjustment,d21_pwd_vat_adjustment,d22_other_vat_adjustment,d23_vat_on_returns,d24_residual_vat_adjustment,d25_total_vat_adjustment,d26_vat_payable,d27_net_sales_ex_vat,d28_overflow_net_income,d29_total_income,d30_reset_counter,d31_z_counter,remarks_code_id,source_semantic_hash,created_at) VALUES(@id,@workbook,@sequence,@period,@z,@zkind,@bir,@site,@identity,@currency,@business,@beginning,@ending,@d04,@d05,@d06,@d07,@d08,@d09,@d10,@d11,@d12,@d13,@d14,@d15,@d16,@d17,@d18,@d19,@d20,@d21,@d22,@d23,@d24,@d25,@d26,@d27,@d28,@d29,@d30,@d31,@remarks,@hash,CURRENT_TIMESTAMP)""";
        for(var i=0;i<rows.Count;i++)
        {
            var row=rows[i];var map=row.Positions.ToDictionary(p=>p.Position,StringComparer.Ordinal);var reportId=Guid.NewGuid();var remarks=await ResolveCodeAsync(c,t,"annex_e1_remarks",map["D32"].TextValue!.ToLowerInvariant(),ct).ConfigureAwait(false);var sourceHash=AnnexE1SemanticHasher.Hash(Encoding.UTF8.GetBytes(string.Join("|",row.Source.GoverningZReportId,row.Source.BirSalesSummaryReportId,string.Join(',',row.Source.FactSemanticHashes))));await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("id",reportId);cmd.Parameters.AddWithValue("workbook",workbookId);cmd.Parameters.AddWithValue("sequence",i+1);cmd.Parameters.AddWithValue("period",row.Source.FiscalReportingPeriodId);cmd.Parameters.AddWithValue("z",row.Source.GoverningZReportId);cmd.Parameters.AddWithValue("zkind",ZKindId);cmd.Parameters.AddWithValue("bir",row.Source.BirSalesSummaryReportId);cmd.Parameters.AddWithValue("site",rows[0].Source.FiscalReportingPeriodId==Guid.Empty?Guid.Empty:await ReadScopeIdAsync(c,t,row.Source.FiscalReportingPeriodId,"site_pos_server_id",ct).ConfigureAwait(false));cmd.Parameters.AddWithValue("identity",await ReadScopeIdAsync(c,t,row.Source.FiscalReportingPeriodId,"fiscal_identity_id",ct).ConfigureAwait(false));cmd.Parameters.AddWithValue("currency","PHP");cmd.Parameters.AddWithValue("business",row.Source.BusinessDayDate);AddNullable(cmd,"beginning",row.Source.BeginningFiscalNumber,NpgsqlDbType.Text);AddNullable(cmd,"ending",row.Source.EndingFiscalNumber,NpgsqlDbType.Text);for(var n=4;n<=29;n++)cmd.Parameters.AddWithValue($"d{n:D2}",map[$"D{n:D2}"].MinorUnitsValue!.Value);cmd.Parameters.AddWithValue("d30",map["D30"].IntegerValue!.Value);cmd.Parameters.AddWithValue("d31",map["D31"].IntegerValue!.Value);cmd.Parameters.AddWithValue("remarks",remarks);cmd.Parameters.AddWithValue("hash",sourceHash);await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);for(var f=0;f<row.Source.FactIds.Count;f++){await using var link=Command(c,t,"INSERT INTO pos.annex_e1_report_fact_sources(annex_e_report_id,annex_e1_period_accounting_fact_id,source_order) VALUES(@report,@fact,@order)");link.Parameters.AddWithValue("report",reportId);link.Parameters.AddWithValue("fact",row.Source.FactIds[f]);link.Parameters.AddWithValue("order",f+1);await link.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
        }
    }

    private static async Task<Guid> ReadScopeIdAsync(NpgsqlConnection c,NpgsqlTransaction t,Guid period,string column,CancellationToken ct){await using var cmd=Command(c,t,$"SELECT {column} FROM pos.fiscal_reporting_periods WHERE fiscal_reporting_period_id=@id");cmd.Parameters.AddWithValue("id",period);return (Guid)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false))!;}

    private static async Task<AnnexE1WorkbookRecord?> ReadWorkbookByOperationAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid site,string operation,CancellationToken ct){await using var cmd=Command(c,t,"SELECT annex_e1_workbook_id FROM pos.annex_e1_workbooks WHERE site_pos_server_id=@site AND operation_key=@operation");cmd.Parameters.AddWithValue("site",site);cmd.Parameters.AddWithValue("operation",operation);var id=await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);return id is Guid guid?await ReadWorkbookAsync(c,t,guid,ct).ConfigureAwait(false):null;}
    private static async Task<AnnexE1WorkbookRecord?> ReadCurrentWorkbookAsync(NpgsqlConnection c,NpgsqlTransaction t,AnnexE1GenerationCommand q,CancellationToken ct){await using var cmd=Command(c,t,"SELECT w.annex_e1_workbook_id FROM pos.annex_e1_workbooks w WHERE w.site_pos_server_id=@site AND w.fiscal_identity_id=@identity AND w.currency_code=@currency AND w.calendar_year=@year AND w.calendar_month=@month AND w.profile_ref=@profile AND NOT EXISTS(SELECT 1 FROM pos.annex_e1_workbooks child WHERE child.supersedes_workbook_id=w.annex_e1_workbook_id) ORDER BY revision DESC LIMIT 1 FOR SHARE OF w");cmd.Parameters.AddWithValue("site",q.SitePosServerId);cmd.Parameters.AddWithValue("identity",q.FiscalIdentityId);cmd.Parameters.AddWithValue("currency",q.CurrencyCode);cmd.Parameters.AddWithValue("year",q.CalendarYear);cmd.Parameters.AddWithValue("month",q.CalendarMonth);cmd.Parameters.AddWithValue("profile",q.Profile);var id=await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);return id is Guid guid?await ReadWorkbookAsync(c,t,guid,ct).ConfigureAwait(false):null;}

    private static async Task<AnnexE1WorkbookRecord?> ReadWorkbookAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct)
    {
        const string sql="""SELECT w.annex_e1_workbook_id,w.annex_e_reference,w.operation_key,w.profile_ref,w.calculation_profile_ref,w.renderer_version,w.site_pos_server_id,w.fiscal_identity_id,w.currency_code,w.calendar_year,w.calendar_month,w.revision,w.semantic_hash,w.artifact_sha256,w.artifact_byte_length,w.mime_type,w.file_name,w.taxpayer_name,w.taxpayer_address,w.tin,w.software_name,w.software_version,w.release_number,w.release_date,w.pos_serial_number,w.machine_identification_number,w.pos_terminal_number,w.generated_at,w.generated_by_ref,w.supersedes_workbook_id,w.correction_reason_code_id,w.correction_approval_reference,w.committed_at,w.correlation_id,(SELECT child.annex_e_reference FROM pos.annex_e1_workbooks child WHERE child.supersedes_workbook_id=w.annex_e1_workbook_id) FROM pos.annex_e1_workbooks w WHERE w.annex_e1_workbook_id=@id""";
        await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("id",id);AnnexE1WorkbookRecord? shell;await using(var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false)){if(!await r.ReadAsync(ct).ConfigureAwait(false))return null;var header=new AnnexE1Header(r.GetString(17),r.GetString(18),r.GetString(19),r.GetString(20),r.GetString(21),r.GetString(22),r.GetString(23),r.GetString(24),r.GetString(25),r.GetString(26),r.GetFieldValue<DateTimeOffset>(27),r.GetString(28));shell=new(r.GetGuid(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetGuid(6),r.GetGuid(7),r.GetString(8),r.GetInt32(9),r.GetInt32(10),r.GetInt32(11),r.GetString(12),r.GetString(13),r.GetInt64(14),r.GetString(15),r.GetString(16),header,Array.Empty<AnnexE1Row>(),r.IsDBNull(29)?null:r.GetGuid(29),r.IsDBNull(34)?null:r.GetString(34),r.IsDBNull(30)?null:"controlled",r.IsDBNull(31)?null:r.GetString(31),r.GetFieldValue<DateTimeOffset>(27),r.GetFieldValue<DateTimeOffset>(32),r.GetString(33));}
        var rows=await ReadRowsAsync(c,t,id,ct).ConfigureAwait(false);return shell with{Rows=rows};
    }

    private static async Task<IReadOnlyList<AnnexE1Row>> ReadRowsAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid workbook,CancellationToken ct)
    {
        const string sql="""SELECT r.fiscal_reporting_period_id,p.period_sequence,r.governing_z_report_id,z.report_number,r.related_bir_sales_summary_report_id,breq.semantic_request_hash,r.business_day_date,r.beginning_fiscal_number,r.ending_fiscal_number,r.d04_gta_ending,r.d05_gta_beginning,r.d06_manual_net_income,r.d07_annex_gross_sales,r.d08_vatable_sales,r.d09_vat_amount,r.d10_vat_exempt_sales,r.d11_zero_rated_sales,r.d12_sc_discount,r.d13_pwd_discount,r.d14_naac_discount,r.d15_solo_parent_discount,r.d16_other_discount,r.d17_returns,r.d18_voids,r.d19_total_deductions,r.d20_sc_vat_adjustment,r.d21_pwd_vat_adjustment,r.d22_other_vat_adjustment,r.d23_vat_on_returns,r.d24_residual_vat_adjustment,r.d25_total_vat_adjustment,r.d26_vat_payable,r.d27_net_sales_ex_vat,r.d28_overflow_net_income,r.d29_total_income,r.d30_reset_counter,r.d31_z_counter,remarks.code_key FROM pos.annex_e_reports r JOIN pos.fiscal_reporting_periods p ON p.fiscal_reporting_period_id=r.fiscal_reporting_period_id JOIN pos.x_z_reports z ON z.x_z_report_id=r.governing_z_report_id JOIN pos.bir_sales_summary_reports b ON b.bir_sales_summary_report_id=r.related_bir_sales_summary_report_id JOIN pos.fiscal_report_requests breq ON breq.fiscal_report_request_id=b.fiscal_report_request_id JOIN pos.controlled_codes remarks ON remarks.controlled_code_id=r.remarks_code_id WHERE r.annex_e1_workbook_id=@workbook ORDER BY r.row_sequence""";
        await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("workbook",workbook);var rows=new List<AnnexE1Row>();await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);while(await r.ReadAsync(ct).ConfigureAwait(false)){var positions=new List<AnnexE1PositionValue>{new("D01","business_date",r.GetFieldValue<DateOnly>(6).ToString("yyyy-MM-dd"),null,null),new("D02","beginning_fiscal_number",r.IsDBNull(7)?null:r.GetString(7),null,null),new("D03","ending_fiscal_number",r.IsDBNull(8)?null:r.GetString(8),null,null)};for(var n=4;n<=29;n++)positions.Add(new($"D{n:D2}","stored",null,r.GetInt64(n+5),null));positions.Add(new("D30","reset_counter",null,null,r.GetInt64(35)));positions.Add(new("D31","z_counter",null,null,r.GetInt64(36)));positions.Add(new("D32","remarks",r.GetString(37).ToUpperInvariant(),null,null));var source=new AnnexE1RowInputs(
            FiscalReportingPeriodId:r.GetGuid(0),PeriodSequence:r.GetInt64(1),GoverningZReportId:r.GetGuid(2),GoverningZReference:r.GetString(3),BirSalesSummaryReportId:r.GetGuid(4),BirSalesSummarySemanticHash:r.GetString(5),BusinessDayDate:r.GetFieldValue<DateOnly>(6),BeginningFiscalNumber:r.IsDBNull(7)?null:r.GetString(7),EndingFiscalNumber:r.IsDBNull(8)?null:r.GetString(8),PreviousGta:r.GetInt64(10),ResultingGta:r.GetInt64(9),ManualNetIncome:r.GetInt64(11),ActiveGross:r.GetInt64(12)-r.GetInt64(22)-r.GetInt64(23),ReturnAmount:r.GetInt64(22),VoidAmount:r.GetInt64(23),VatableSales:r.GetInt64(13),VatAmount:r.GetInt64(14),VatExemptSales:r.GetInt64(15),ZeroRatedSales:r.GetInt64(16),SeniorCitizenDiscount:r.GetInt64(17),PwdDiscount:r.GetInt64(18),NaacDiscount:r.GetInt64(19),SoloParentDiscount:r.GetInt64(20),OtherStatutoryDiscount:r.GetInt64(21),CouponDiscount:0,PromotionalDiscount:0,SeniorCitizenVatAdjustment:r.GetInt64(25),PwdVatAdjustment:r.GetInt64(26),OtherVatAdjustment:r.GetInt64(27),VatOnReturns:r.GetInt64(28),ResidualVatAdjustment:r.GetInt64(29),OverflowNetIncome:r.GetInt64(33),ResetCounter:r.GetInt64(35),ZCounter:r.GetInt64(36),TransactionCount:0,RefundAmount:0,AdjustmentAmount:0,ServiceChargeAmount:0,FiscalRangeCount:0,FiscalGapCount:0,FactIds:Array.Empty<Guid>(),FactSemanticHashes:Array.Empty<string>());rows.Add(new(source,positions,Array.Empty<AnnexE1Reconciliation>()));}return rows;
    }

    private static async Task<string?> ReadArtifactKeyAsync(NpgsqlConnection c,Guid id,CancellationToken ct){await using var cmd=Command(c,null,"SELECT artifact_storage_key FROM pos.annex_e1_workbooks WHERE annex_e1_workbook_id=@id");cmd.Parameters.AddWithValue("id",id);return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;}
    private async Task<AnnexE1WorkbookResult> ReconcileGenerationOutcomeAsync(AnnexE1GenerationCommand command,string? semanticHash,CancellationToken ct)
    {
        try
        {
            await using var connection=await dataSource.OpenConnectionAsync(ct).ConfigureAwait(false);
            var durable=await ReadWorkbookByOperationAsync(connection,null,command.SitePosServerId,command.OperationKey,ct).ConfigureAwait(false);
            if(durable is null)return FailureWorkbook(AnnexE1Outcome.UnknownCommitOutcome,"The Annex E-1 generation outcome requires durable-state reconciliation before retry.");
            return semanticHash is not null&&string.Equals(durable.SemanticHash,semanticHash,StringComparison.Ordinal)
                ? new(AnnexE1Outcome.Replayed,durable)
                : FailureWorkbook(AnnexE1Outcome.Conflict,"The Annex E-1 operation is durably bound to different source semantics.");
        }
        catch(NpgsqlException)
        {
            return FailureWorkbook(AnnexE1Outcome.UnknownCommitOutcome,"The Annex E-1 generation outcome requires durable-state reconciliation before retry.");
        }
    }
    private static async Task<(DateOnly BusinessDayDate,DateTimeOffset PeriodEndAt)?> ReadPeriodAsync(NpgsqlConnection c,NpgsqlTransaction t,AnnexE1PeriodFactCommand q,CancellationToken ct)
    {
        const string sql="""SELECT p.business_day_date,p.period_end_at FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes ps ON ps.controlled_code_id=p.period_status_code_id JOIN pos.x_z_reports z ON z.fiscal_reporting_period_id=p.fiscal_reporting_period_id AND z.report_kind_code_id=@zkind JOIN pos.fiscal_report_requests zr ON zr.fiscal_report_request_id=z.fiscal_report_request_id JOIN pos.controlled_codes zrs ON zrs.controlled_code_id=zr.report_status_code_id JOIN pos.bir_sales_summary_reports b ON b.governing_z_report_id=z.x_z_report_id JOIN pos.fiscal_report_requests br ON br.fiscal_report_request_id=b.fiscal_report_request_id JOIN pos.controlled_codes brs ON brs.controlled_code_id=br.report_status_code_id WHERE p.fiscal_reporting_period_id=@period AND p.site_pos_server_id=@site AND p.fiscal_identity_id=@identity AND p.currency_code=@currency AND ps.code_key='closed' AND p.closed_at IS NOT NULL AND zrs.code_key='committed' AND brs.code_key='committed' FOR SHARE OF p,z,b""";
        await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("zkind",ZKindId);cmd.Parameters.AddWithValue("period",q.FiscalReportingPeriodId);cmd.Parameters.AddWithValue("site",q.SitePosServerId);cmd.Parameters.AddWithValue("identity",q.FiscalIdentityId);cmd.Parameters.AddWithValue("currency",q.CurrencyCode);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);return await r.ReadAsync(ct).ConfigureAwait(false)?(r.GetFieldValue<DateOnly>(0),r.GetFieldValue<DateTimeOffset>(1)):null;
    }
    private static async Task<AnnexE1PeriodFactRecord?> ReadFactByOperationAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid site,string operation,CancellationToken ct){await using var cmd=Command(c,t,"SELECT annex_e1_period_accounting_fact_id FROM pos.annex_e1_period_accounting_facts WHERE site_pos_server_id=@site AND operation_key=@operation");cmd.Parameters.AddWithValue("site",site);cmd.Parameters.AddWithValue("operation",operation);var id=await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);return id is Guid guid?await ReadFactAsync(c,t,guid,ct).ConfigureAwait(false):null;}
    private static async Task<AnnexE1PeriodFactRecord?> ReadFactAsync(NpgsqlConnection c,NpgsqlTransaction? t,Guid id,CancellationToken ct){const string sql="SELECT f.annex_e1_period_accounting_fact_id,f.operation_key,f.site_pos_server_id,f.fiscal_identity_id,f.currency_code,f.fiscal_reporting_period_id,f.business_day_date,ft.code_key,fs.code_key,f.amount_minor_units,f.source_document_count,f.first_source_reference,f.last_source_reference,f.source_event_reference,f.approval_reference,f.semantic_hash,f.supersedes_fact_id,cr.code_key,f.effective_at,f.recorded_at,f.recorded_by_ref,f.service_identity_ref,f.correlation_id FROM pos.annex_e1_period_accounting_facts f JOIN pos.controlled_codes ft ON ft.controlled_code_id=f.fact_type_code_id JOIN pos.controlled_codes fs ON fs.controlled_code_id=f.fact_status_code_id LEFT JOIN pos.controlled_codes cr ON cr.controlled_code_id=f.correction_reason_code_id WHERE f.annex_e1_period_accounting_fact_id=@id";await using var cmd=Command(c,t,sql);cmd.Parameters.AddWithValue("id",id);await using var r=await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);return await r.ReadAsync(ct).ConfigureAwait(false)?MapFact(r):null;}
    private static AnnexE1PeriodFactRecord MapFact(NpgsqlDataReader r)=>new(r.GetGuid(0),r.GetString(1),r.GetGuid(2),r.GetGuid(3),r.GetString(4),r.GetGuid(5),r.GetFieldValue<DateOnly>(6),r.GetString(7),r.GetString(8),r.GetInt64(9),r.GetInt64(10),r.IsDBNull(11)?null:r.GetString(11),r.IsDBNull(12)?null:r.GetString(12),r.IsDBNull(13)?null:r.GetString(13),r.GetString(14),r.GetString(15),r.IsDBNull(16)?null:r.GetGuid(16),r.IsDBNull(17)?null:r.GetString(17),r.GetFieldValue<DateTimeOffset>(18),r.GetFieldValue<DateTimeOffset>(19),r.GetString(20),r.GetString(21),r.GetString(22));
    private static async Task<Guid> ResolveCodeAsync(NpgsqlConnection c,NpgsqlTransaction t,string set,string code,CancellationToken ct){await using var cmd=Command(c,t,"SELECT cc.controlled_code_id FROM pos.controlled_codes cc JOIN pos.controlled_code_sets cs ON cs.controlled_code_set_id=cc.controlled_code_set_id WHERE cs.code_set_key=@set AND cc.code_key=@code AND cs.is_active AND cc.is_active");cmd.Parameters.AddWithValue("set",set);cmd.Parameters.AddWithValue("code",code);return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is Guid id?id:throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedClassification,"A required Annex E-1 controlled classification is unavailable.");}
    private static async Task InsertAuditAsync(NpgsqlConnection c,NpgsqlTransaction t,Guid site,string actor,string service,string correlation,DateTimeOffset at,CancellationToken ct)
    {
        var action=await ResolveCodeAsync(c,t,"fiscal_report_kind","annex_e",ct).ConfigureAwait(false);
        var result=await ResolveCodeAsync(c,t,"fiscal_action_audit_result","success",ct).ConfigureAwait(false);
        await using var cmd=Command(c,t,"INSERT INTO pos.fiscal_action_audit(fiscal_action_audit_id,site_pos_server_id,audit_action_type_code_id,audit_result_code_id,actor_ref,service_identity_ref,correlation_ref,occurred_at,created_at) VALUES(@id,@site,@action,@result,@actor,@service,@correlation,@at,@at)");
        cmd.Parameters.AddWithValue("id",Guid.NewGuid());cmd.Parameters.AddWithValue("site",site);cmd.Parameters.AddWithValue("action",action);cmd.Parameters.AddWithValue("result",result);cmd.Parameters.AddWithValue("actor",actor);cmd.Parameters.AddWithValue("service",service);cmd.Parameters.AddWithValue("correlation",correlation);cmd.Parameters.AddWithValue("at",at);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
    private static async Task AcquireLockAsync(NpgsqlConnection c,NpgsqlTransaction t,string key,CancellationToken ct){await using var cmd=Command(c,t,"SELECT pg_advisory_xact_lock(hashtextextended(@key,0))");cmd.Parameters.AddWithValue("key",key);await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);}
    private static async Task<DateTimeOffset> ClockAsync(NpgsqlConnection c,NpgsqlTransaction t,CancellationToken ct)
    {
        await using var cmd=Command(c,t,"SELECT clock_timestamp()");
        return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) switch
        {
            DateTimeOffset value => value.ToUniversalTime(),
            DateTime value => new DateTimeOffset(DateTime.SpecifyKind(value,DateTimeKind.Utc)),
            _ => throw new AnnexE1SafeException(AnnexE1Outcome.PersistenceFailure,"The governed database clock is unavailable.")
        };
    }
    private static NpgsqlCommand Command(NpgsqlConnection c,NpgsqlTransaction? t,string sql)=>new(sql,c,t);
    private static void AddNullable(NpgsqlCommand cmd,string name,object? value,NpgsqlDbType type){cmd.Parameters.Add(new NpgsqlParameter(name,type){Value=value??DBNull.Value});}
    private static AnnexE1FactResult FailureFact(AnnexE1Outcome outcome,string message)=>new(outcome,SafeMessage:message);
    private static AnnexE1WorkbookResult FailureWorkbook(AnnexE1Outcome outcome,string message)=>new(outcome,SafeMessage:message);

    private sealed record Source(Guid PeriodId,long PeriodSequence,DateOnly BusinessDate,Guid ZId,string ZReference,Guid BirId,string BirSemanticHash,string? BeginningNumber,string? EndingNumber,long PreviousGta,long ResultingGta,long GrossSales,long VatableSales,long VatAmount,long VatExemptSales,long ZeroRatedSales,long ScDiscount,long PwdDiscount,long OtherStatutoryDiscount,long CouponDiscount,long PromotionalDiscount,long VoidAmount,long ReturnAmount,long RefundAmount,long AdjustmentAmount,long ServiceChargeAmount,long ResetCounter,long ZCounter,long TransactionCount,long RangeCount,long GapCount,long ScVatAdjustment,long PwdVatAdjustment,string FiscalIdentityCode,string TaxpayerName,string TaxpayerAddress,string Tin,string PosSerialNumber,string MachineIdentificationNumber,string SiteCode);
}
