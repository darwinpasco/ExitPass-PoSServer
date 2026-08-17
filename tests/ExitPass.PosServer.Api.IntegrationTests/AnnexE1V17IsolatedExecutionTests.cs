using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class AnnexE1V17IsolatedExecutionTests
{
    private const string ConnectionVariable = "ANNEX_E1_V17_HARNESS_DB_URL";
    private const string DatasetVariable = "ANNEX_E1_V17_DATASET_PATH";
    private const string ReportVariable = "ANNEX_E1_V17_REPORT_PATH";

    private static readonly IReadOnlyDictionary<string, FamilyMapping> FamilyMappings =
        new Dictionary<string, FamilyMapping>(StringComparer.Ordinal)
        {
            ["F01"] = new("pos.site_pos_servers", "CANONICAL_TABLE"),
            ["F02"] = new("pos.fiscal_identities", "CANONICAL_TABLE"),
            ["F03"] = new("pos.sales_invoice_header_profiles", "CANONICAL_TABLE"),
            ["F04"] = new("pos.fiscal_reporting_periods", "CANONICAL_TABLE"),
            ["F05"] = new("pos.fiscal_report_fiscal_number_ranges", "CANONICAL_TABLE"),
            ["F06"] = new("pos.fiscal_documents", "APPLICATION_SERVICE"),
            ["F07"] = new("pos.fiscal_document_lines", "APPLICATION_SERVICE"),
            ["F08"] = new("pos.fiscal_totals", "APPLICATION_SERVICE"),
            ["F09"] = new("pos.fiscal_tax_details", "APPLICATION_SERVICE"),
            ["F10"] = new("pos.fiscal_discount_privilege_details", "APPLICATION_SERVICE"),
            ["F11"] = new("pos.fiscal_document_applied_statutory_facts", "APPLICATION_SERVICE"),
            ["F12"] = new("pos.fiscal_tenders", "APPLICATION_SERVICE"),
            ["F13"] = new("pos.fiscal_report_tender_breakdowns", "CANONICAL_TABLE"),
            ["F14"] = new("pos.fiscal_report_discount_breakdowns", "CANONICAL_TABLE"),
            ["F15"] = new("pos.fiscal_document_status_history", "APPLICATION_SERVICE"),
            ["F16"] = new("pos.annex_e1_period_accounting_facts", "CANONICAL_TABLE"),
            ["F17"] = new("pos.x_z_reports", "APPLICATION_SERVICE"),
            ["F18"] = new("pos.x_z_reports", "APPLICATION_SERVICE"),
            ["F19"] = new("pos.bir_sales_summary_reports", "APPLICATION_SERVICE"),
            ["F20"] = new("pos.annex_e_reports", "APPLICATION_SERVICE"),
            ["F21"] = new("pos.annex_e1_report_fact_sources", "APPLICATION_SERVICE"),
            ["F22"] = new("pos.electronic_journal_records", "APPLICATION_SERVICE"),
            ["F23"] = new("ElectronicJournalCanonicalizer", "APPLICATION_CONTRACT"),
            ["F24"] = new("pos.annex_e1_workbooks", "APPLICATION_SERVICE"),
            ["F25"] = new("pos.fiscal_report_requests", "APPLICATION_SERVICE"),
            ["F26"] = new("AnnexE1Service idempotency", "APPLICATION_CONTRACT"),
            ["F27"] = new("AnnexE1Service conflict", "APPLICATION_CONTRACT"),
            ["F28"] = new("pos.recovery_requests", "CANONICAL_TABLE"),
            ["F29"] = new("pos.fiscal_action_audit", "CANONICAL_TABLE")
        };

    [Fact]
    public async Task LoadsAndExecutesAllIncludedV17Cases()
    {
        var connectionString = RequireSafeConnectionString();
        var datasetPath = RequireFile(DatasetVariable);
        var reportPath = RequireValue(ReportVariable);
        var dataset = ReadDataset(datasetPath);

        Assert.Equal(4_074, dataset.All.Count);
        Assert.Equal(2_364, dataset.Identities.Count);
        Assert.Equal(1_690, dataset.Semantic.Count);
        Assert.Equal(19, dataset.Cases.Count);
        Assert.Equal(29, dataset.Semantic.Select(RowFamily).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(1_690, dataset.Semantic.Select(RowKey).Distinct(StringComparer.Ordinal).Count());

        await RebuildDisposableDatabaseAsync(connectionString);
        await CreateHarnessSchemaAndLoadAsync(connectionString, dataset);
        await AssertCompleteReadbackAsync(connectionString, dataset);
        await LoadCanonicalScopeAndFactsAsync(connectionString, dataset);

        var correctionCodeCount = await ScalarLongAsync(
            connectionString,
            """
            select count(*)
            from pos.controlled_codes cc
            join pos.controlled_code_sets cs on cs.controlled_code_set_id = cc.controlled_code_set_id
            where cs.code_set_key = 'annex_e1_correction_reason'
              and cc.code_key = 'source_correction'
              and cs.is_active and cc.is_active
            """);
        Assert.Equal(1, correctionCodeCount);
        Assert.Equal(0, await ScalarLongAsync(
            connectionString,
            """
            select count(*)
            from pos.controlled_codes cc
            join pos.controlled_code_sets cs on cs.controlled_code_set_id = cc.controlled_code_set_id
            where cs.code_set_key = 'annex_e1_correction_reason'
              and cc.code_key = 'SYNTHETIC_CORRECTION'
            """));

        var correction = Single(dataset.Semantic, "F16|013|0008");
        Assert.Equal("source_correction", Text(correction, "correction_reason"));
        Assert.Equal(Guid.Parse("96cc7392-4dc3-550c-b7e0-8521f77d86ef"), GuidValue(correction, "supersedes_fact_id"));
        Assert.Equal(600, Long(correction, "amount_minor_units"));
        Assert.Equal(1, await ScalarLongAsync(
            connectionString,
            """
            select count(*)
            from pos.annex_e1_period_accounting_facts f
            join pos.controlled_codes cc on cc.controlled_code_id = f.correction_reason_code_id
            where f.annex_e1_period_accounting_fact_id = 'e0602e8f-8662-5a63-8313-25672d017b8e'
              and f.supersedes_fact_id = '96cc7392-4dc3-550c-b7e0-8521f77d86ef'
              and f.amount_minor_units = 600
              and cc.code_key = 'source_correction'
            """));

        var fiscalResults = await ExecuteFiscalDocumentsAsync(dataset);
        Assert.Equal(67, fiscalResults.DocumentCount);
        Assert.Equal(30, fiscalResults.StatutoryCount);

        var annexResults = ExecuteAnnexRows(dataset);
        Assert.Equal(22, annexResults.RowCount);
        Assert.Equal(19, annexResults.Cases.Count);

        var journal = ValidateJournal(dataset);
        Assert.Equal(148, journal.Records);
        Assert.Equal(148, journal.Transitions);
        Assert.Equal(19, journal.Genesis);
        Assert.Equal(129, journal.Predecessors);
        Assert.Equal(19, journal.Streams);

        Assert.Equal(155, await ScalarLongAsync(connectionString, "select count(*) from pos.annex_e1_period_accounting_facts"));
        Assert.Equal(0, await ScalarLongAsync(
            connectionString,
            """
            select count(*)
            from pos.annex_e1_period_accounting_facts f
            join pos.fiscal_reporting_periods p on p.fiscal_reporting_period_id = f.fiscal_reporting_period_id
            where f.effective_at < p.period_start_at or f.effective_at >= p.period_end_at
            """));

        var caseIds = dataset.Cases.Select(row => row.GetProperty("caseId").GetString()!).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.Equal(19, caseIds.Length);
        foreach (var caseId in caseIds)
        {
            var scenario = caseId[^3..];
            Assert.Contains(scenario, annexResults.Cases);
        }

        var lines = new List<string>
        {
            "ANNEX_E1_V17_ISOLATED_EXECUTION=PASS",
            "ACCOUNTING_FACTS_CANONICAL=155",
            "ANNEX_ROWS_EXECUTED=22",
            "CANONICAL_SCOPE_RECORDS=215",
            "CASE_PACKAGES=19",
            "DATASET_RECORDS=4074",
            "DETERMINISTIC_IDENTITIES=2364",
            "EJ_GENESIS=19",
            "EJ_PREDECESSORS=129",
            "EJ_RECORDS=148",
            "EJ_STREAMS=19",
            "FISCAL_DOCUMENTS_EXECUTED=67",
            "LOADABLE_FAMILIES=29",
            "SEMANTIC_INSTANCES=1690",
            "STATUTORY_FINALITY_CASES=30",
            "TRANSITIONS=148"
        };
        lines.AddRange(caseIds.Select(caseId => $"{caseId}=PASS"));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(reportPath, string.Join('\n', lines) + "\n", new UTF8Encoding(false));
    }

    private static async Task CreateHarnessSchemaAndLoadAsync(string connectionString, Dataset dataset)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await ExecuteSqlAsync(connection, """
            create schema annex_e1_v17_harness;
            create table annex_e1_v17_harness.loaded_records (
                line_number integer primary key,
                record_type text not null,
                record_key text not null,
                scenario text null,
                family text null,
                payload_text text not null,
                payload_sha256 char(64) not null,
                unique (record_type, record_key)
            );
            create table annex_e1_v17_harness.family_mappings (
                family text primary key,
                target_contract text not null,
                execution_mode text not null
            );
            """);

        await using var transaction = await connection.BeginTransactionAsync();
        await using var recordCommand = new NpgsqlCommand(
            """
            insert into annex_e1_v17_harness.loaded_records
                (line_number, record_type, record_key, scenario, family, payload_text, payload_sha256)
            values (@line, @type, @key, @scenario, @family, @payload, @hash)
            """, connection, transaction);
        recordCommand.Parameters.Add(new("line", NpgsqlTypes.NpgsqlDbType.Integer));
        recordCommand.Parameters.Add(new("type", NpgsqlTypes.NpgsqlDbType.Text));
        recordCommand.Parameters.Add(new("key", NpgsqlTypes.NpgsqlDbType.Text));
        recordCommand.Parameters.Add(new("scenario", NpgsqlTypes.NpgsqlDbType.Text));
        recordCommand.Parameters.Add(new("family", NpgsqlTypes.NpgsqlDbType.Text));
        recordCommand.Parameters.Add(new("payload", NpgsqlTypes.NpgsqlDbType.Text));
        recordCommand.Parameters.Add(new("hash", NpgsqlTypes.NpgsqlDbType.Char));
        for (var index = 0; index < dataset.All.Count; index++)
        {
            var row = dataset.All[index];
            var type = row.GetProperty("recordType").GetString()!;
            recordCommand.Parameters["line"].Value = index + 1;
            recordCommand.Parameters["type"].Value = type;
            recordCommand.Parameters["key"].Value = RecordKey(row, index);
            recordCommand.Parameters["scenario"].Value = row.TryGetProperty("scenario", out var scenario) ? scenario.GetString() ?? (object)DBNull.Value : DBNull.Value;
            recordCommand.Parameters["family"].Value = row.TryGetProperty("family", out var family) ? family.GetString() ?? (object)DBNull.Value : DBNull.Value;
            var payload = row.GetRawText();
            recordCommand.Parameters["payload"].Value = payload;
            recordCommand.Parameters["hash"].Value = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            await recordCommand.ExecuteNonQueryAsync();
        }

        await using var mappingCommand = new NpgsqlCommand(
            "insert into annex_e1_v17_harness.family_mappings(family,target_contract,execution_mode) values (@family,@target,@mode)",
            connection,
            transaction);
        mappingCommand.Parameters.Add(new("family", NpgsqlTypes.NpgsqlDbType.Text));
        mappingCommand.Parameters.Add(new("target", NpgsqlTypes.NpgsqlDbType.Text));
        mappingCommand.Parameters.Add(new("mode", NpgsqlTypes.NpgsqlDbType.Text));
        foreach (var mapping in FamilyMappings.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            mappingCommand.Parameters["family"].Value = mapping.Key;
            mappingCommand.Parameters["target"].Value = mapping.Value.Target;
            mappingCommand.Parameters["mode"].Value = mapping.Value.Mode;
            await mappingCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        foreach (var mapping in FamilyMappings.Values.Where(value => value.Target.StartsWith("pos.", StringComparison.Ordinal)).Distinct())
        {
            await using var command = new NpgsqlCommand("select to_regclass(@target)::text", connection);
            command.Parameters.AddWithValue("target", mapping.Target);
            Assert.Equal(mapping.Target, (string?)await command.ExecuteScalarAsync());
        }
    }

    private static async Task AssertCompleteReadbackAsync(string connectionString, Dataset dataset)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select line_number, payload_text, payload_sha256 from annex_e1_v17_harness.loaded_records order by line_number",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            var index = reader.GetInt32(0) - 1;
            var payload = reader.GetString(1);
            Assert.Equal(dataset.All[index].GetRawText(), payload);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant(), reader.GetString(2));
            count++;
        }
        Assert.Equal(4_074, count);
    }

    private static async Task LoadCanonicalScopeAndFactsAsync(string connectionString, Dataset dataset)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        foreach (var row in dataset.Semantic.Where(row => RowFamily(row) == "F01").OrderBy(RowKey, StringComparer.Ordinal))
        {
            await ExecuteSqlAsync(connection, """
                insert into pos.site_pos_servers (
                    site_pos_server_id, site_pos_server_code, display_name, central_pms_site_ref,
                    central_pms_site_resolution_ref, reporting_timezone_name, business_day_cutoff_local_time,
                    operational_status_code_id, is_active, created_at, updated_at)
                values (@id,@code,@name,@site_ref,@resolution_ref,@timezone,@cutoff,@status,@active,@created,@updated)
                """, transaction,
                ("id", GuidValue(row, "site_pos_server_id")), ("code", Text(row, "site_pos_server_code")),
                ("name", Text(row, "display_name")), ("site_ref", NullableText(row, "central_pms_site_ref")),
                ("resolution_ref", NullableText(row, "central_pms_zone_ref")), ("timezone", Text(row, "time_zone_id")),
                ("cutoff", TimeSpan.Parse(Text(row, "business_day_cutoff_time"), CultureInfo.InvariantCulture)),
                ("status", NullableGuid(row, "operational_status_code_id")), ("active", Bool(row, "is_active")),
                ("created", Timestamp(row, "created_at")), ("updated", Timestamp(row, "updated_at")));
        }

        foreach (var row in dataset.Semantic.Where(row => RowFamily(row) == "F02").OrderBy(RowKey, StringComparer.Ordinal))
        {
            await ExecuteSqlAsync(connection, """
                insert into pos.fiscal_identities (
                    fiscal_identity_id, fiscal_identity_code, taxpayer_display_name,
                    registered_business_name, registered_business_address, tin, fiscal_identity_status,
                    min_ref, ptu_ref, serial_ref, software_name, software_version, accreditation_ref,
                    metadata_json, is_active, created_at, updated_at, created_by_ref, updated_by_ref)
                values (@id,@code,@taxpayer,@business,@address,@tin,@status,@min,@ptu,@serial,@software,@version,@accreditation,'{}'::jsonb,@active,@created,@updated,@created_by,@updated_by)
                """, transaction,
                ("id", GuidValue(row, "id")), ("code", Text(row, "code")), ("taxpayer", Text(row, "taxpayer_name")),
                ("business", Text(row, "taxpayer_name")), ("address", Text(row, "registered_address")), ("tin", Text(row, "tin")),
                ("status", Text(row, "status")), ("min", Text(row, "machine_identification_number")),
                ("ptu", Text(row, "ptu_reference")), ("serial", Text(row, "serial_number")),
                ("software", Text(row, "software_name")), ("version", Text(row, "software_version")),
                ("accreditation", Text(row, "accreditation_reference")), ("active", Bool(row, "is_active")),
                ("created", Timestamp(row, "created_at")), ("updated", Timestamp(row, "updated_at")),
                ("created_by", Text(row, "created_by")), ("updated_by", Text(row, "updated_by")));
        }

        var contractId = await ScalarGuidAsync(connection, transaction,
            "select fiscal_reporting_contract_version_id from pos.fiscal_reporting_contract_versions where is_active order by effective_from desc limit 1");
        var closedStatusId = await ResolveCodeAsync(connection, transaction, "fiscal_reporting_period_status", "closed");
        foreach (var row in dataset.Semantic.Where(row => RowFamily(row) == "F04").OrderBy(RowKey, StringComparer.Ordinal))
        {
            await ExecuteSqlAsync(connection, """
                insert into pos.fiscal_reporting_periods (
                    fiscal_reporting_period_id, fiscal_reporting_contract_version_id, site_pos_server_id,
                    fiscal_identity_id, period_status_code_id, business_day_date, period_start_at, period_end_at,
                    reporting_timezone_name, business_day_cutoff_local_time, currency_code, period_sequence,
                    expected_prior_period_id, opened_at, closing_started_at, closed_at,
                    created_by_ref, updated_by_ref, created_at, updated_at)
                values (@id,@contract,@site,@identity,@status,@business,@start,@end,'Asia/Manila','00:00:00',@currency,@sequence,@prior,@opened,@closing,@closed,@opened_by,@closed_by,@created,@updated)
                """, transaction,
                ("id", GuidValue(row, "id")), ("contract", contractId), ("site", GuidValue(row, "site_pos_server_id")),
                ("identity", GuidValue(row, "fiscal_identity_id")), ("status", closedStatusId),
                ("business", DateOnly.ParseExact(Text(row, "business_date"), "yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("start", Timestamp(row, "period_start_at")), ("end", Timestamp(row, "period_end_at")),
                ("currency", Text(row, "currency")), ("sequence", Long(row, "sequence")),
                ("prior", NullableGuid(row, "prior_period_id")), ("opened", Timestamp(row, "opened_at")),
                ("closing", Timestamp(row, "closing_started_at")), ("closed", Timestamp(row, "closed_at")),
                ("opened_by", Text(row, "opened_by")), ("closed_by", Text(row, "closed_by")),
                ("created", Timestamp(row, "created_at")), ("updated", Timestamp(row, "updated_at")));
        }

        var periods = dataset.Semantic.Where(row => RowFamily(row) == "F04")
            .ToDictionary(row => GuidValue(row, "id"));
        foreach (var row in dataset.Semantic.Where(row => RowFamily(row) == "F16").OrderBy(RowKey, StringComparer.Ordinal))
        {
            var periodId = GuidValue(row, "period_id");
            var reason = NullableText(row, "correction_reason");
            var reasonId = reason is null ? (Guid?)null : await ResolveCodeAsync(connection, transaction, "annex_e1_correction_reason", reason);
            await ExecuteSqlAsync(connection, """
                insert into pos.annex_e1_period_accounting_facts (
                    annex_e1_period_accounting_fact_id, operation_key, site_pos_server_id, fiscal_identity_id,
                    currency_code, fiscal_reporting_period_id, business_day_date, fact_type_code_id,
                    fact_status_code_id, amount_minor_units, source_document_count, first_source_reference,
                    last_source_reference, source_event_reference, approval_reference, semantic_hash_version,
                    semantic_hash, supersedes_fact_id, correction_reason_code_id, effective_at, recorded_at,
                    recorded_by_ref, service_identity_ref, correlation_id, created_at)
                values (@id,@operation,@site,@identity,@currency,@period,@business,@type,@status,@amount,@count,
                    @first,@last,@event,@approval,@semantic_version,@semantic_hash,@supersedes,@reason,
                    @effective,@recorded,@actor,@service,@correlation,@created)
                """, transaction,
                ("id", GuidValue(row, "id")), ("operation", Text(row, "operation_key")),
                ("site", GuidValue(row, "site_pos_server_id")), ("identity", GuidValue(row, "fiscal_identity_id")),
                ("currency", Text(row, "currency")), ("period", periodId),
                ("business", DateOnly.ParseExact(Text(periods[periodId], "business_date"), "yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("type", await ResolveCodeAsync(connection, transaction, "annex_e1_accounting_fact_type", Text(row, "fact_type"))),
                ("status", await ResolveCodeAsync(connection, transaction, "annex_e1_accounting_fact_status", Text(row, "status"))),
                ("amount", Long(row, "amount_minor_units")), ("count", Long(row, "source_document_count")),
                ("first", NullableText(row, "first_source_reference")), ("last", NullableText(row, "last_source_reference")),
                ("event", NullableText(row, "source_event_reference")), ("approval", Text(row, "approval_reference")),
                ("semantic_version", Text(row, "semantic_version")),
                ("semantic_hash", row.GetProperty("expectedSemanticSha256").GetString()!),
                ("supersedes", NullableGuid(row, "supersedes_fact_id")), ("reason", reasonId),
                ("effective", Timestamp(row, "effective_at")), ("recorded", Timestamp(row, "recorded_at")),
                ("actor", Text(row, "actor")), ("service", Text(row, "service")),
                ("correlation", Text(row, "correlation_id")), ("created", Timestamp(row, "created_at")));
        }
        await transaction.CommitAsync();
    }

    private static async Task<(int DocumentCount, int StatutoryCount)> ExecuteFiscalDocumentsAsync(Dataset dataset)
    {
        var service = new FiscalDocumentCreationService(new AcceptingFiscalDocumentRepository());
        var statutoryCount = 0;
        foreach (var row in dataset.Semantic.Where(row => RowFamily(row) == "F06").OrderBy(RowKey, StringComparer.Ordinal))
        {
            var command = BuildFiscalCommand(row);
            var result = await service.CreateAsync(command);
            Assert.True(result.Succeeded, $"[{RowKey(row)}] Runtime finality failed: {result.ErrorCode} {result.Message}");
            if (command.AppliedStatutoryFiscalFacts is not null)
            {
                statutoryCount++;
                Assert.Equal(command.AppliedStatutoryFiscalFacts.ParkingSessionId?.ToString("D"), command.CentralPmsParkingSessionRef, ignoreCase: true);
                Assert.Equal(11_200, command.AppliedStatutoryFiscalFacts.OriginalAmountMinorUnits);
                Assert.Equal(2_000, command.AppliedStatutoryFiscalFacts.StatutoryDiscountAmountMinorUnits);
                Assert.Equal(9_200, command.AppliedStatutoryFiscalFacts.FinalPayableAmountMinorUnits);
            }
        }
        return (67, statutoryCount);
    }

    private static FiscalDocumentCreationCommand BuildFiscalCommand(JsonElement row)
    {
        var payable = Member(row, "payable_basis");
        var discountReferences = ArrayObjects(Nested(payable, "discount_references")).Select(value =>
            new FiscalDiscountReferenceInput(
                NestedText(value, "discount_validation_ref"),
                Enum.Parse<FiscalDiscountReferenceStatus>(NestedText(value, "status"), true),
                NestedBool(value, "applies_statutory_discount_treatment"))).ToArray();
        var basis = new FiscalizationPayableBasisInput(
            NestedText(payable, "payable_basis_ref"), NestedText(payable, "upstream_finality_ref"),
            NestedText(payable, "currency_code"), NestedLong(payable, "payable_amount_minor_units"), discountReferences);
        var lines = ArrayObjects(Member(row, "document_lines")).Select(value =>
        {
            var discount = NestedLong(value, "discount_amount_minor_units");
            var taxAmount = NestedLong(value, "tax_amount_minor_units");
            var net = NestedLong(value, "net_amount_minor_units");
            // The runtime line contract uses the pre-tax base; the dataset also retains the tax-inclusive original.
            var runtimeGross = checked(net + discount - taxAmount);
            return new FiscalDocumentLineInput(
                checked((int)NestedLong(value, "line_sequence")), NestedGuid(value, "line_type_code_id"),
                NestedText(value, "description"), decimal.Parse(NestedText(value, "quantity"), CultureInfo.InvariantCulture),
                NestedLong(value, "unit_amount_minor_units"), runtimeGross, discount, taxAmount, net,
                NestedText(value, "currency_code"), SourceRef: NestedNullableText(value, "source_ref"));
        }).ToArray();
        var tenders = ArrayObjects(Member(row, "tenders")).Select(value => new FiscalTenderInput(
            NestedGuid(value, "tender_type_code_id"), NestedLong(value, "amount_minor_units"), NestedText(value, "currency_code"),
            NestedNullableText(value, "central_pms_payment_attempt_ref"), NestedNullableText(value, "central_pms_payment_confirmation_ref"),
            NestedNullableText(value, "payment_finality_ref"), NestedNullableText(value, "provider_ref"))).ToArray();
        var tax = ArrayObjects(Member(row, "tax_details")).Select(value => new FiscalTaxDetailInput(
            NestedGuid(value, "tax_type_code_id"), NestedGuid(value, "tax_classification_code_id"),
            NestedLong(value, "taxable_amount_minor_units"), NestedLong(value, "tax_amount_minor_units"),
            NestedText(value, "currency_code"), checked((int)NestedLong(value, "line_sequence")),
            decimal.Parse(NestedText(value, "tax_rate"), CultureInfo.InvariantCulture))).ToArray();
        var discounts = ArrayObjects(Member(row, "discount_privilege_details")).Select(value => new FiscalDiscountPrivilegeDetailInput(
            NestedGuid(value, "discount_privilege_type_code_id"), NestedLong(value, "basis_amount_minor_units"),
            NestedLong(value, "discount_amount_minor_units"), NestedLong(value, "vat_privilege_amount_minor_units"),
            NestedText(value, "currency_code"), checked((int)NestedLong(value, "line_sequence")),
            ApprovalRef: NestedNullableText(value, "approval_ref"))).ToArray();
        var totals = ArrayObjects(Member(row, "totals")).Select(value => new FiscalTotalInput(
            NestedGuid(value, "total_type_code_id"), NestedLong(value, "amount_minor_units"), NestedText(value, "currency_code"))).ToArray();
        AppliedStatutoryFiscalFactsInput? applied = null;
        if (TryNested(row.GetProperty("members"), "applied_statutory_fiscal_facts", out var appliedElement) &&
            appliedElement.ValueKind != JsonValueKind.Null)
        {
            var policy = Nested(appliedElement, "policy_reference");
            applied = new AppliedStatutoryFiscalFactsInput(
                NestedNullableGuid(appliedElement, "statutory_discount_decision_command_id"),
                NestedNullableGuid(appliedElement, "statutory_request_reference"),
                NestedNullableGuid(appliedElement, "statutory_payable_basis_application_command_id"),
                NestedNullableGuid(appliedElement, "statutory_validation_id"), NestedNullableGuid(appliedElement, "parking_session_id"),
                NestedNullableGuid(appliedElement, "site_id"), NestedNullableGuid(appliedElement, "site_group_id"),
                NestedNullableText(appliedElement, "entitlement_type"), NestedNullableText(appliedElement, "benefit_classification"),
                new AppliedStatutoryPolicyReferenceInput(
                    NestedNullableText(policy, "resolution_basis"), NestedNullableGuid(policy, "applied_policy_reference_id"),
                    NestedNullableText(policy, "policy_code"), NestedNullableGuid(policy, "policy_version_id"),
                    NestedNullableText(policy, "national_law_reference"), NestedNullableText(policy, "ordinance_reference")),
                NestedNullableGuid(appliedElement, "original_tariff_snapshot_id"), NestedNullableGuid(appliedElement, "applied_tariff_snapshot_id"),
                NestedNullableLong(appliedElement, "original_amount_minor_units"), NestedNullableLong(appliedElement, "vat_exclusive_basis_amount_minor_units"),
                NestedNullableLong(appliedElement, "vat_amount_minor_units"), NestedNullableText(appliedElement, "vat_treatment"),
                NestedNullableLong(appliedElement, "statutory_discount_amount_minor_units"), NestedNullableLong(appliedElement, "final_payable_amount_minor_units"),
                NestedNullableText(appliedElement, "currency"), NestedNullableTimestamp(appliedElement, "applied_at"),
                NestedNullableText(appliedElement, "source_payment_channel"), NestedNullableGuid(appliedElement, "terminal_cash_tender_id"));
        }

        return new FiscalDocumentCreationCommand(
            Text(row, "site_pos_server_ref"), Text(row, "fiscal_document_type_code_key"), basis,
            GuidValue(row, "site_pos_server_id"), FiscalDocumentTypeCodeId: GuidValue(row, "fiscal_document_type_code_id"),
            FiscalDocumentStatusCodeId: GuidValue(row, "fiscal_document_status_code_id"),
            BusinessDayDate: DateOnly.ParseExact(Text(row, "business_day_date"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            CentralPmsParkingSessionRef: NullableText(row, "central_pms_parking_session_ref"),
            CentralPmsPaymentAttemptRef: NullableText(row, "central_pms_payment_attempt_ref"),
            CentralPmsPaymentConfirmationRef: NullableText(row, "central_pms_payment_confirmation_ref"),
            PaymentFinalityRef: NullableText(row, "payment_finality_ref"), VendorAckRef: NullableText(row, "vendor_ack_ref"),
            DocumentLines: lines, Tenders: tenders, TaxDetails: tax, DiscountPrivilegeDetails: discounts, Totals: totals,
            AppliedStatutoryFiscalFacts: applied);
    }

    private static (int RowCount, HashSet<string> Cases) ExecuteAnnexRows(Dataset dataset)
    {
        var engine = new AnnexE1CalculationEngine();
        var rowsById = dataset.Semantic.ToDictionary(row => row.GetProperty("instanceUuid").GetString()!, StringComparer.Ordinal);
        var cases = new HashSet<string>(StringComparer.Ordinal);
        var count = 0;
        foreach (var expected in dataset.Semantic.Where(row => RowFamily(row) == "F20").OrderBy(RowKey, StringComparer.Ordinal))
        {
            var scenario = expected.GetProperty("scenario").GetString()!;
            var ordinal = expected.GetProperty("ordinal").GetInt32();
            var z = dataset.Semantic.Single(row => RowFamily(row) == "F18" && Scenario(row) == scenario && row.GetProperty("ordinal").GetInt32() == ordinal);
            var bir = dataset.Semantic.Single(row => RowFamily(row) == "F19" && Scenario(row) == scenario && row.GetProperty("ordinal").GetInt32() == ordinal);
            var links = dataset.Semantic.Where(row => RowFamily(row) == "F21" &&
                    GuidValue(row, "workbook_id") == GuidValue(expected, "workbook_id") &&
                    GuidValue(row, "period_id") == GuidValue(z, "period_id"))
                .OrderBy(row => Long(row, "source_ordinal")).ToArray();
            Assert.Equal(7, links.Length);
            var facts = links.Select(link => rowsById[GuidValue(link, "accounting_fact_id").ToString("D")]).ToArray();
            long Fact(string type)
            {
                var link = links.Single(candidate => Text(candidate, "fact_type") == type);
                var fact = rowsById[GuidValue(link, "accounting_fact_id").ToString("D")];
                Assert.Equal(type, Text(fact, "fact_type"));
                return Long(fact, "amount_minor_units");
            }
            var fiscalRangeCount = Member(z, "fiscal_ranges").GetArrayLength();
            var input = new AnnexE1RowInputs(
                GuidValue(z, "period_id"), ordinal, GuidValue(z, "report_id"), Text(bir, "governing_z_reference"),
                GuidValue(bir, "summary_id"), bir.GetProperty("expectedSemanticSha256").GetString()!,
                DateOnly.ParseExact(Text(z, "business_date"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                NullableText(z, "first_document_number"), NullableText(z, "last_document_number"),
                Long(z, "previous_grand_total_minor_units"), Long(z, "resulting_grand_total_minor_units"),
                Fact("manual_si_or_net_income"), Long(z, "gross_sales_minor_units"), 0, Long(z, "void_amount_minor_units"),
                Long(z, "vatable_sales_minor_units"), Long(z, "vat_amount_minor_units"), Long(z, "vat_exempt_sales_minor_units"),
                Long(z, "zero_rated_sales_minor_units"), Long(z, "senior_citizen_discount_minor_units"),
                Long(z, "pwd_discount_minor_units"), Long(z, "naac_discount_minor_units"), Long(z, "solo_parent_discount_minor_units"),
                Long(z, "other_statutory_discount_minor_units"), Long(z, "coupon_discount_minor_units"),
                Long(z, "promotional_discount_minor_units"), 0, 0, 0, 0, 0, Fact("sales_overrun_overflow_net_income"),
                Long(z, "resulting_reset_counter"), ordinal, Long(z, "transaction_count"), Long(z, "refund_amount_minor_units"),
                0, 0, fiscalRangeCount, 0, facts.Select(fact => GuidValue(fact, "id")).ToArray(),
                links.Select(link => Text(link, "fact_semantic_hash")).ToArray());
            var actual = engine.Calculate(input);
            var expectedDetails = Member(expected, "details").EnumerateArray().ToDictionary(member => member.GetProperty("n").GetString()!, StringComparer.Ordinal);
            foreach (var position in actual.Positions)
            {
                var expectedValue = expectedDetails[position.Position].GetProperty("v");
                if (position.MinorUnitsValue is not null) Assert.Equal(position.MinorUnitsValue.Value, expectedValue.GetInt64());
                else if (position.IntegerValue is not null) Assert.Equal(position.IntegerValue.Value, expectedValue.GetInt64());
                else Assert.Equal(position.TextValue, expectedValue.GetString());
            }
            foreach (var rule in actual.Reconciliations) Assert.True(rule.Passed, $"[{RowKey(expected)}] {rule.Rule} failed.");
            foreach (var reconciliation in ArrayObjects(Member(expected, "reconciliations")))
            {
                Assert.Equal(0, NestedLong(reconciliation, "difference_minor_units"));
                Assert.Equal(0, NestedLong(reconciliation, "row_count_difference"));
                Assert.Equal("PASS", NestedText(reconciliation, "result"));
            }
            cases.Add(scenario);
            count++;
        }
        return (count, cases);
    }

    private static (int Records, int Transitions, int Genesis, int Predecessors, int Streams) ValidateJournal(Dataset dataset)
    {
        var records = dataset.Semantic.Where(row => RowFamily(row) == "F22").ToArray();
        var transitions = dataset.Semantic.Where(row => RowFamily(row) == "F23").ToArray();
        var genesis = 0;
        var predecessors = 0;
        var streams = 0;
        foreach (var scenario in records.Select(Scenario).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
        {
            var events = records.Where(row => Scenario(row) == scenario).OrderBy(row => Long(row, "sequence")).ToArray();
            var changes = transitions.Where(row => Scenario(row) == scenario).OrderBy(row => Long(row, "destination_sequence")).ToArray();
            Assert.Equal(events.Length, changes.Length);
            Assert.Single(events.Select(row => Text(row, "stream_id")).Distinct(StringComparer.Ordinal));
            streams++;
            for (var index = 0; index < events.Length; index++)
            {
                Assert.Equal(index + 1, Long(events[index], "sequence"));
                Assert.Equal(GuidValue(changes[index], "transition_id"), GuidValue(events[index], "source_transition_id"));
                Assert.Equal(GuidValue(changes[index], "resulting_electronic_journal_record_id"), GuidValue(events[index], "record_id"));
                Assert.Equal(Text(changes[index], "resulting_event_semantic_hash"), Text(events[index], "runtime_semantic_hash"));
                Assert.Equal(index + 1, Long(changes[index], "destination_sequence"));
                if (index == 0)
                {
                    Assert.Null(NullableGuid(events[index], "predecessor_event_id"));
                    Assert.Null(NullableGuid(changes[index], "predecessor_transition_id"));
                    Assert.Equal(0, Long(changes[index], "source_sequence"));
                    genesis++;
                }
                else
                {
                    Assert.Equal(GuidValue(events[index - 1], "record_id"), GuidValue(events[index], "predecessor_event_id"));
                    Assert.Equal(GuidValue(changes[index - 1], "transition_id"), GuidValue(changes[index], "predecessor_transition_id"));
                    Assert.Equal(Text(events[index - 1], "runtime_integrity_hash"), Text(events[index], "previous_integrity_hash"));
                    Assert.Equal(index, Long(changes[index], "source_sequence"));
                    predecessors++;
                }
            }
        }
        return (records.Length, transitions.Length, genesis, predecessors, streams);
    }

    private static Dataset ReadDataset(string path)
    {
        var all = new List<JsonElement>();
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            using var document = JsonDocument.Parse(line);
            all.Add(document.RootElement.Clone());
        }
        var allowed = new HashSet<string>(["dataset-metadata", "identity", "semantic-instance", "case-package"], StringComparer.Ordinal);
        Assert.All(all, row => Assert.Contains(row.GetProperty("recordType").GetString()!, allowed));
        return new(
            all,
            all.Where(row => row.GetProperty("recordType").GetString() == "identity").ToArray(),
            all.Where(row => row.GetProperty("recordType").GetString() == "semantic-instance").ToArray(),
            all.Where(row => row.GetProperty("recordType").GetString() == "case-package").ToArray());
    }

    private static async Task RebuildDisposableDatabaseAsync(string connectionString)
    {
        var root = FindRepositoryRoot();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var entry in File.ReadLines(Path.Combine(root, "db", "rebuild", "pos_sql_apply_order.txt"))
                     .Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')))
            await ExecuteFileAsync(connection, Path.Combine(root, entry.Replace('/', Path.DirectorySeparatorChar)));
        foreach (var file in Directory.GetFiles(Path.Combine(root, "db", "reference-data", "controlled-codes", "generated", "sql"), "*.sql")
                     .OrderBy(path => path, StringComparer.Ordinal))
            await ExecuteFileAsync(connection, file);
    }

    private static string RequireSafeConnectionString()
    {
        var value = RequireValue(ConnectionVariable);
        if (Uri.TryCreate(value, UriKind.Absolute, out _)) throw new InvalidOperationException("Harness connection string must use Npgsql key-value syntax.");
        var builder = new NpgsqlConnectionStringBuilder(value);
        var database = builder.Database ?? string.Empty;
        if (builder.Host is not ("127.0.0.1" or "localhost") || !database.StartsWith("annex_e1_v17_harness_", StringComparison.Ordinal) ||
            database.Contains("exitpass_v12_dev", StringComparison.OrdinalIgnoreCase) || database.Contains("shared", StringComparison.OrdinalIgnoreCase) ||
            database.Contains("uat", StringComparison.OrdinalIgnoreCase) || database.Contains("prod", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Harness connection string does not target an invocation-owned loopback database.");
        return value;
    }

    private static string RequireFile(string variable)
    {
        var value = RequireValue(variable);
        return File.Exists(value) ? Path.GetFullPath(value) : throw new FileNotFoundException($"{variable} does not identify a file.");
    }

    private static string RequireValue(string variable) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable))
            ? Environment.GetEnvironmentVariable(variable)!
            : throw new InvalidOperationException($"{variable} is required for the isolated harness.");

    private static JsonElement Single(IEnumerable<JsonElement> rows, string key) => rows.Single(row => RowKey(row) == key);
    private static string RowKey(JsonElement row) => row.GetProperty("key").GetString()!;
    private static string RowFamily(JsonElement row) => row.GetProperty("family").GetString()!;
    private static string Scenario(JsonElement row) => row.GetProperty("scenario").GetString()!;
    private static JsonElement Member(JsonElement row, string name) => Nested(row.GetProperty("members"), name);
    private static JsonElement Nested(JsonElement members, string name) => members.EnumerateArray().Single(member => member.GetProperty("n").GetString() == name).GetProperty("v");
    private static IEnumerable<JsonElement> ArrayObjects(JsonElement value) => value.EnumerateArray().Select(item => item.GetProperty("v"));
    private static string Text(JsonElement row, string name) => Member(row, name).GetString()!;
    private static string NestedText(JsonElement members, string name) => Nested(members, name).GetString()!;
    private static string? NullableText(JsonElement row, string name) => NullableString(Member(row, name));
    private static string? NestedNullableText(JsonElement members, string name) => TryNested(members, name, out var value) ? NullableString(value) : null;
    private static string? NullableString(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetString();
    private static long Long(JsonElement row, string name) => Member(row, name).GetInt64();
    private static long NestedLong(JsonElement members, string name) => Nested(members, name).GetInt64();
    private static long? NestedNullableLong(JsonElement members, string name) => TryNested(members, name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetInt64() : null;
    private static bool Bool(JsonElement row, string name) => Member(row, name).GetBoolean();
    private static bool NestedBool(JsonElement members, string name) => Nested(members, name).GetBoolean();
    private static Guid GuidValue(JsonElement row, string name) => Guid.Parse(Text(row, name));
    private static Guid NestedGuid(JsonElement members, string name) => Guid.Parse(NestedText(members, name));
    private static Guid? NullableGuid(JsonElement row, string name) => ParseNullableGuid(Member(row, name));
    private static Guid? NestedNullableGuid(JsonElement members, string name) => TryNested(members, name, out var value) ? ParseNullableGuid(value) : null;
    private static Guid? ParseNullableGuid(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : Guid.Parse(value.GetString()!);
    private static DateTimeOffset Timestamp(JsonElement row, string name) => DateTimeOffset.ParseExact(Text(row, name), "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    private static DateTimeOffset? NestedNullableTimestamp(JsonElement members, string name) => TryNested(members, name, out var value) && value.ValueKind != JsonValueKind.Null ? DateTimeOffset.ParseExact(value.GetString()!, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) : null;
    private static bool TryNested(JsonElement members, string name, out JsonElement value)
    {
        foreach (var member in members.EnumerateArray())
            if (member.GetProperty("n").GetString() == name) { value = member.GetProperty("v"); return true; }
        value = default;
        return false;
    }

    private static string RecordKey(JsonElement row, int index) => row.GetProperty("recordType").GetString() switch
    {
        "dataset-metadata" => "metadata",
        "identity" => row.GetProperty("name").GetString()!,
        "semantic-instance" => RowKey(row),
        "case-package" => row.GetProperty("caseId").GetString()!,
        _ => $"unknown-{index}"
    };

    private static async Task<Guid> ResolveCodeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string set, string code)
    {
        await using var command = new NpgsqlCommand(
            """
            select cc.controlled_code_id
            from pos.controlled_codes cc
            join pos.controlled_code_sets cs on cs.controlled_code_set_id=cc.controlled_code_set_id
            where cs.code_set_key=@set and cc.code_key=@code and cs.is_active and cc.is_active
            """, connection, transaction);
        command.Parameters.AddWithValue("set", set);
        command.Parameters.AddWithValue("code", code);
        return await command.ExecuteScalarAsync() is Guid id ? id : throw new InvalidOperationException($"Controlled code {set}/{code} was not resolved.");
    }

    private static async Task<Guid> ScalarGuidAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteScalarAsync() is Guid value ? value : throw new InvalidOperationException("Expected a UUID scalar.");
    }

    private static async Task<long> ScalarLongAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteFileAsync(NpgsqlConnection connection, string path) => await ExecuteSqlAsync(connection, await File.ReadAllTextAsync(path));

    private static async Task ExecuteSqlAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null, params (string Name, object? Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ExitPass.PosServer.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private sealed record FamilyMapping(string Target, string Mode);
    private sealed record Dataset(IReadOnlyList<JsonElement> All, IReadOnlyList<JsonElement> Identities, IReadOnlyList<JsonElement> Semantic, IReadOnlyList<JsonElement> Cases);

    private sealed class AcceptingFiscalDocumentRepository : IFiscalDocumentRepository
    {
        public Task<FiscalDocumentPersistenceResult> CreateAsync(FiscalDocumentDraft draft, FiscalIssuanceIdempotency idempotency, CancellationToken cancellationToken) =>
            Task.FromResult(FiscalDocumentPersistenceResult.Created(draft));

        public Task<FiscalDocumentVoidPersistenceResult> VoidAsync(FiscalDocumentVoidCommand command, FiscalDocumentVoidIdempotency idempotency, CancellationToken cancellationToken) =>
            throw new NotSupportedException("The isolated creation-finality harness does not issue runtime void commands.");
    }
}
