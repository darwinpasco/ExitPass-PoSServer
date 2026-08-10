using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExitPass.PosServer.Api.ElectronicJournal;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class ElectronicJournalPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_ELECTRONIC_JOURNAL_TEST_DB_URL";
    private const string ProductionKey = "synthetic-ej-production-proof-key";
    private const string FixtureKey = "synthetic-ej-fixture-proof-key";
    private static readonly Guid SiteId = Guid.Parse("73000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("73000000-0000-4000-8000-000000000002");
    private static readonly Guid FiscalDocumentId = Guid.Parse("73000000-0000-4000-8000-000000000301");
    private static readonly Guid ReportingPeriodId = Guid.Parse("73000000-0000-4000-8000-000000000010");
    private static readonly Guid SequencePolicyId = Guid.Parse("73000000-0000-4000-8000-000000000003");
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse("2026-08-03T12:00:00Z");

    [Fact]
    public async Task CanonicalJournalIsAtomicOrderedReplaySafeDeterministicAndTamperEvident()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await RebuildAsync(connectionString);
        await ExecuteFileAsync(connectionString, Path.Combine(FindRepositoryRoot(), "tests", "ExitPass.PosServer.Api.IntegrationTests", "Fixtures", "fiscal_x_reading_runtime_fixture.sql"));

        var app = await StartApiAsync(connectionString);
        var xClient = CreateClient(app, ProductionKey, FiscalXReadingAuthorization.GeneratePermission);
        var readClient = CreateClient(app, ProductionKey, ElectronicJournalAuthorization.ReadPermission);
        var exportClient = CreateClient(app, ProductionKey, ElectronicJournalAuthorization.ExportPermission);
        var integrityClient = CreateClient(app, ProductionKey, ElectronicJournalAuthorization.IntegrityPermission);
        var reprintClient = CreateClient(app, ProductionKey, FiscalDocumentReprintAuthorization.RecordPermission);
        try
        {
            var firstRequest = new GenerateFiscalXReadingRequest("ej-proof-x-001", SiteId, IdentityId, ObservedAt);
            using var first = await xClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", firstRequest);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            using var replay = await xClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", firstRequest);
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Equal(1, await CanonicalEventCountAsync(connectionString));

            var reprintJson = await ProveReprintRuntimeAsync(connectionString, app, reprintClient, exportClient);
            Assert.Equal(8, await CanonicalEventCountAsync(connectionString));

            await ProveJournalFailureRollsBackSourceAsync(connectionString, xClient);
            await ProveSourceFailureRollsBackJournalAsync(connectionString, xClient);

            using (var retry = await xClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/",
                       new GenerateFiscalXReadingRequest("ej-proof-journal-failure", SiteId, IdentityId, ObservedAt.AddMinutes(1))))
                Assert.Equal(HttpStatusCode.Created, retry.StatusCode);

            var concurrent = Enumerable.Range(1, 4).Select(async number =>
            {
                using var client = CreateClient(app, ProductionKey, FiscalXReadingAuthorization.GeneratePermission);
                var request = new GenerateFiscalXReadingRequest($"ej-proof-concurrent-{number}", SiteId, IdentityId, ObservedAt.AddMinutes(number + 1));
                using var response = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/",
                    request);
                Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.ServiceUnavailable });
                return (request, response.StatusCode);
            });
            var concurrentResults = await Task.WhenAll(concurrent);
            foreach (var (request, status) in concurrentResults.Where(result => result.StatusCode == HttpStatusCode.ServiceUnavailable))
            {
                using var retryClient = CreateClient(app, ProductionKey, FiscalXReadingAuthorization.GeneratePermission);
                using var retryResponse = await retryClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", request);
                Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
            }
            Assert.Equal(13, await CanonicalEventCountAsync(connectionString));
            Assert.Equal(13, await ScalarAsync<long>(connectionString,
                "SELECT count(DISTINCT stream_sequence_value) FROM pos.electronic_journal_records WHERE is_canonical"));
            Assert.Equal(13, await ScalarAsync<long>(connectionString,
                "SELECT last_sequence_value FROM pos.electronic_journal_streams"));

            var query = Query(pageSize: 2);
            using var pageOneResponse = await readClient.GetAsync(query);
            Assert.Equal(HttpStatusCode.OK, pageOneResponse.StatusCode);
            var pageOne = await pageOneResponse.Content.ReadFromJsonAsync<ElectronicJournalReadResponse>();
            Assert.NotNull(pageOne);
            Assert.Equal(2, pageOne.Page.Events.Count);
            Assert.NotNull(pageOne.Page.NextCursor);
            Assert.Equal(13, pageOne.Page.ThroughSequence);

            using (var later = await xClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/",
                       new GenerateFiscalXReadingRequest("ej-proof-after-cursor", SiteId, IdentityId, ObservedAt.AddMinutes(10))))
                Assert.Equal(HttpStatusCode.Created, later.StatusCode);
            using var pageTwoResponse = await readClient.GetAsync(Query(pageSize: 20, cursor: pageOne.Page.NextCursor, through: pageOne.Page.ThroughSequence));
            var pageTwo = await pageTwoResponse.Content.ReadFromJsonAsync<ElectronicJournalReadResponse>();
            Assert.Equal(HttpStatusCode.OK, pageTwoResponse.StatusCode);
            Assert.Equal(11, pageTwo?.Page.Events.Count);
            Assert.All(pageTwo!.Page.Events, item => Assert.True(item.StreamSequence <= 13));

            var beforeReadOnly = await FiscalManifestAsync(connectionString);
            var jsonBytes = await ExportAsync(exportClient, "json");
            var repeatedJsonBytes = await ExportAsync(exportClient, "json");
            var csvBytes = await ExportAsync(exportClient, "csv");
            Assert.Equal(jsonBytes, repeatedJsonBytes);
            Assert.NotEmpty(csvBytes);
            Assert.DoesNotContain("password", System.Text.Encoding.UTF8.GetString(jsonBytes), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("statutory_id", System.Text.Encoding.UTF8.GetString(jsonBytes), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(beforeReadOnly, await FiscalManifestAsync(connectionString));
            Assert.Equal(reprintJson, await ExportReprintsAsync(exportClient, "json"));

            using var validIntegrity = await integrityClient.PostAsJsonAsync("/v1/electronic-journal/integrity-verifications",
                new VerifyElectronicJournalIntegrityRequest(SiteId, IdentityId, "PHP"));
            Assert.Equal(HttpStatusCode.OK, validIntegrity.StatusCode);

            using var readCannotExport = await readClient.GetAsync(QueryExport("json"));
            Assert.Equal(HttpStatusCode.Forbidden, readCannotExport.StatusCode);
            using var wrongScope = await readClient.GetAsync($"/v1/electronic-journal/events?SitePosServerId={Guid.NewGuid():D}&FiscalIdentityId={IdentityId:D}&CurrencyCode=PHP&PageSize=100");
            Assert.Equal(HttpStatusCode.NotFound, wrongScope.StatusCode);
            using var malformedCursor = await readClient.GetAsync(Query(cursor: "not-a-cursor"));
            Assert.Equal(HttpStatusCode.BadRequest, malformedCursor.StatusCode);
            using var malformedReference = await readClient.GetAsync(Query() + "&FiscalDocumentReference=not-a-guid");
            Assert.Equal(HttpStatusCode.BadRequest, malformedReference.StatusCode);
            using var unsupportedFormat = await exportClient.GetAsync(QueryExport("xml"));
            Assert.Equal(HttpStatusCode.BadRequest, unsupportedFormat.StatusCode);

            using var fixtureClient = CreateClient(app, FixtureKey, ElectronicJournalAuthorization.ReadPermission);
            using var fixtureResponse = await fixtureClient.GetAsync(Query());
            Assert.Equal(HttpStatusCode.NotFound, fixtureResponse.StatusCode);

            await app.StopAsync();
            await app.DisposeAsync();
            xClient.Dispose(); readClient.Dispose(); exportClient.Dispose(); integrityClient.Dispose(); reprintClient.Dispose();

            app = await StartApiAsync(connectionString);
            exportClient = CreateClient(app, ProductionKey, ElectronicJournalAuthorization.ExportPermission);
            reprintClient = CreateClient(app, ProductionKey, FiscalDocumentReprintAuthorization.RecordPermission);
            var restartedJsonBytes = await ExportAsync(exportClient, "json");
            Assert.Equal(jsonBytes, restartedJsonBytes);
            Assert.Equal(reprintJson, await ExportReprintsAsync(exportClient, "json"));
            using var restartedReplay = await reprintClient.PostAsJsonAsync(
                $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-001", "operator_request"));
            Assert.Equal(HttpStatusCode.OK, restartedReplay.StatusCode);
            Assert.Equal(7, await CanonicalReprintCountAsync(connectionString));
            Assert.Equal(14, await CanonicalEventCountAsync(connectionString));

            integrityClient = CreateClient(app, ProductionKey, ElectronicJournalAuthorization.IntegrityPermission);
            await ProveDatabaseImmutabilityAndTamperDetectionAsync(connectionString, integrityClient);
        }
        finally
        {
            xClient.Dispose(); readClient.Dispose(); exportClient.Dispose(); integrityClient.Dispose(); reprintClient.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static async Task<byte[]> ProveReprintRuntimeAsync(
        string connectionString, WebApplication app, HttpClient reprintClient, HttpClient exportClient)
    {
        var fiscalStateBefore = await FiscalManifestAsync(connectionString);
        var request = Reprint("ej-reprint-001", "operator_request");
        using var created = await reprintClient.PostAsJsonAsync($"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdRecord = await created.Content.ReadFromJsonAsync<FiscalDocumentReprintApiResponse>();
        Assert.Equal(1, createdRecord?.Reprint?.CopySequence);
        Assert.StartsWith("RPR-", createdRecord?.Reprint?.ReprintReference, StringComparison.Ordinal);

        using var replay = await reprintClient.PostAsJsonAsync($"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var replayedRecord = await replay.Content.ReadFromJsonAsync<FiscalDocumentReprintApiResponse>();
        Assert.Equal(createdRecord?.Reprint, replayedRecord?.Reprint);
        using var conflict = await reprintClient.PostAsJsonAsync(
            $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", request with { ReasonCode = "audit_request" });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(1, await CanonicalReprintCountAsync(connectionString));
        Assert.Equal(1, await ReprintEventCountAsync(connectionString));

        await ProveReprintJournalFailureRollbackAsync(connectionString, reprintClient);
        await ProveReprintSourceFailureRollbackAsync(connectionString, reprintClient);
        await ProveOrphanReprintAndEventRejectionAsync(connectionString);

        var concurrent = Enumerable.Range(1, 4).Select(async number =>
        {
            using var client = CreateClient(app, ProductionKey, FiscalDocumentReprintAuthorization.RecordPermission);
            using var response = await client.PostAsJsonAsync(
                $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints",
                Reprint($"ej-reprint-concurrent-{number}", number % 2 == 0 ? "customer_request" : "operator_request"));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        });
        await Task.WhenAll(concurrent);
        Assert.Equal(7, await CanonicalReprintCountAsync(connectionString));
        Assert.Equal(7, await ReprintEventCountAsync(connectionString));
        Assert.Equal(7, await ScalarAsync<long>(connectionString,
            "SELECT count(DISTINCT copy_sequence) FROM pos.reprint_requests WHERE is_canonical AND fiscal_document_id='73000000-0000-4000-8000-000000000301'"));
        Assert.Equal(7, await ScalarAsync<long>(connectionString,
            "SELECT max(copy_sequence) FROM pos.reprint_requests WHERE is_canonical AND fiscal_document_id='73000000-0000-4000-8000-000000000301'"));
        await AssertPostgresFailureAsync(connectionString,
            "UPDATE pos.reprint_requests SET updated_at=clock_timestamp() WHERE is_canonical AND fiscal_document_id='73000000-0000-4000-8000-000000000301' AND copy_sequence=1", "23514");
        await AssertPostgresFailureAsync(connectionString,
            "DELETE FROM pos.reprint_requests WHERE is_canonical AND fiscal_document_id='73000000-0000-4000-8000-000000000301' AND copy_sequence=1", "23514");
        await AssertPostgresFailureAsync(connectionString,
            "UPDATE pos.reprint_output_refs SET output_reference=output_reference WHERE is_canonical", "23514");

        using var wrongScope = await reprintClient.PostAsJsonAsync(
            $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints",
            Reprint("ej-reprint-wrong-scope", "operator_request") with { SitePosServerId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, wrongScope.StatusCode);
        using var fixture = CreateClient(app, FixtureKey, FiscalDocumentReprintAuthorization.RecordPermission);
        using var fixtureResponse = await fixture.PostAsJsonAsync(
            $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-fixture", "operator_request"));
        Assert.Equal(HttpStatusCode.NotFound, fixtureResponse.StatusCode);
        using var readOnly = CreateClient(app, ProductionKey, ElectronicJournalAuthorization.ReadPermission);
        using var readOnlyResponse = await readOnly.PostAsJsonAsync(
            $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-read-only", "operator_request"));
        Assert.Equal(HttpStatusCode.Forbidden, readOnlyResponse.StatusCode);
        using var unauthenticated = CreateUnauthenticatedClient(app);
        using var unauthenticatedResponse = await unauthenticated.PostAsJsonAsync(
            $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-unauthenticated", "operator_request"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);

        var json = await ExportReprintsAsync(exportClient, "json");
        Assert.Equal(json, await ExportReprintsAsync(exportClient, "json"));
        var csv = await ExportReprintsAsync(exportClient, "csv");
        var jsonText = System.Text.Encoding.UTF8.GetString(json);
        var csvText = System.Text.Encoding.UTF8.GetString(csv);
        Assert.Contains("fiscal_document_reprint_recorded", jsonText, StringComparison.Ordinal);
        Assert.Contains("copy_sequence", jsonText, StringComparison.Ordinal);
        Assert.Contains("fiscal_document_reprint_recorded", csvText, StringComparison.Ordinal);
        Assert.DoesNotContain("reason_text", jsonText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("customer_name", jsonText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(fiscalStateBefore, await FiscalManifestAsync(connectionString));
        return json;
    }

    private static async Task ProveReprintJournalFailureRollbackAsync(string connectionString, HttpClient client)
    {
        var beforeReprints = await CanonicalReprintCountAsync(connectionString);
        var beforeEvents = await CanonicalEventCountAsync(connectionString);
        await ExecuteAsync(connectionString, """
            CREATE OR REPLACE FUNCTION pos.z011a_fail_reprint_journal() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.journal_record_type_code_id='77905f62-7d61-521d-bdc3-7c7d57b31efe' THEN RAISE EXCEPTION 'synthetic reprint journal failure'; END IF; RETURN NEW; END; $$;
            CREATE TRIGGER z011a_fail_reprint_journal BEFORE INSERT ON pos.electronic_journal_records
            FOR EACH ROW EXECUTE FUNCTION pos.z011a_fail_reprint_journal();
            """);
        try
        {
            using var failed = await client.PostAsJsonAsync(
                $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-journal-failure", "audit_request"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            Assert.Equal(beforeReprints, await CanonicalReprintCountAsync(connectionString));
            Assert.Equal(beforeEvents, await CanonicalEventCountAsync(connectionString));
        }
        finally
        {
            await ExecuteAsync(connectionString, "DROP TRIGGER IF EXISTS z011a_fail_reprint_journal ON pos.electronic_journal_records; DROP FUNCTION IF EXISTS pos.z011a_fail_reprint_journal();");
        }
        using var retry = await client.PostAsJsonAsync(
            $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-journal-failure", "audit_request"));
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
    }

    private static async Task ProveReprintSourceFailureRollbackAsync(string connectionString, HttpClient client)
    {
        var beforeReprints = await CanonicalReprintCountAsync(connectionString);
        var beforeEvents = await CanonicalEventCountAsync(connectionString);
        await ExecuteAsync(connectionString, """
            CREATE OR REPLACE FUNCTION pos.z011a_fail_reprint_output() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'synthetic reprint output failure'; END; $$;
            CREATE TRIGGER z011a_fail_reprint_output BEFORE INSERT ON pos.reprint_output_refs
            FOR EACH ROW EXECUTE FUNCTION pos.z011a_fail_reprint_output();
            """);
        try
        {
            using var failed = await client.PostAsJsonAsync(
                $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-source-failure", "damaged_original"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            Assert.Equal(beforeReprints, await CanonicalReprintCountAsync(connectionString));
            Assert.Equal(beforeEvents, await CanonicalEventCountAsync(connectionString));
        }
        finally
        {
            await ExecuteAsync(connectionString, "DROP TRIGGER IF EXISTS z011a_fail_reprint_output ON pos.reprint_output_refs; DROP FUNCTION IF EXISTS pos.z011a_fail_reprint_output();");
        }
        using var retry = await client.PostAsJsonAsync(
            $"/v1/fiscal-documents/{FiscalDocumentId:D}/reprints", Reprint("ej-reprint-source-failure", "damaged_original"));
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
    }

    private static async Task ProveOrphanReprintAndEventRejectionAsync(string connectionString)
    {
        await AssertPostgresFailureAsync(connectionString, """
            INSERT INTO pos.reprint_requests(
              reprint_request_id,fiscal_document_id,reprint_type_code_id,reprint_status_code_id,reprint_reason_code_id,
              requested_at,requested_by_ref,actor_ref,service_identity_ref,created_at,updated_at,reprint_reference,
              site_pos_server_id,fiscal_identity_id,currency_code,fiscal_reporting_period_id,fiscal_sequence_policy_id,
              fiscal_document_number_snapshot,fiscal_sequence_value_snapshot,copy_sequence,operation_idempotency_key,
              semantic_hash_version,semantic_request_hash,source_transition_version,correlation_ref,committed_at,is_canonical)
            VALUES('85000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000301',
              '4322e9c1-2209-5ad4-8587-4bbe4f35a08b','5dacbba2-6cc7-593f-867b-d7010955d3e1','5b8a58c8-636f-51fc-8512-85e3d932e8c8',
              clock_timestamp(),'proof','proof','proof',clock_timestamp(),clock_timestamp(),'RPR-ORPHAN',
              '73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','PHP',
              '73000000-0000-4000-8000-000000000010','73000000-0000-4000-8000-000000000003','SI-00000001',1,100,
              'orphan-source','pos-server-fiscal-document-reprint:sha256:v1',repeat('a',64),
              'pos-server-fiscal-document-reprint:v1','orphan-correlation',clock_timestamp(),true)
            """, "23503");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => PostgresElectronicJournalWriter.AppendAsync(
            connection, transaction, new ElectronicJournalAppendRequest(
                SiteId, IdentityId, "PHP", ReportingPeriodId, "fiscal_document_reprint_recorded",
                "RPR-MISSING", FiscalDocumentReprintContract.ContractVersion, ObservedAt, "proof", "proof", "proof-correlation",
                new SortedDictionary<string, string?> { ["copy_sequence"] = "1" },
                FiscalDocumentId: FiscalDocumentId, FiscalSequencePolicyId: SequencePolicyId,
                BusinessDayDate: DateOnly.Parse("2026-08-03"), IdempotencyReference: "missing-source",
                ReprintRequestId: Guid.Parse("85000000-0000-4000-8000-000000000002")), default));
        Assert.Equal("23503", exception.SqlState);
        await transaction.RollbackAsync();
    }

    private static async Task ProveJournalFailureRollsBackSourceAsync(string connectionString, HttpClient xClient)
    {
        var beforeReports = await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports");
        var beforeEvents = await CanonicalEventCountAsync(connectionString);
        await ExecuteAsync(connectionString, """
            CREATE OR REPLACE FUNCTION pos.z011a_fail_journal_insert() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'synthetic journal persistence failure'; END; $$;
            CREATE TRIGGER z011a_fail_journal_insert BEFORE INSERT ON pos.electronic_journal_records
            FOR EACH ROW EXECUTE FUNCTION pos.z011a_fail_journal_insert();
            """);
        try
        {
            using var response = await xClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/",
                new GenerateFiscalXReadingRequest("ej-proof-journal-failure", SiteId, IdentityId, ObservedAt.AddMinutes(1)));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(beforeReports, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports"));
            Assert.Equal(beforeEvents, await CanonicalEventCountAsync(connectionString));
        }
        finally
        {
            await ExecuteAsync(connectionString, "DROP TRIGGER IF EXISTS z011a_fail_journal_insert ON pos.electronic_journal_records; DROP FUNCTION IF EXISTS pos.z011a_fail_journal_insert();");
        }
    }

    private static async Task ProveSourceFailureRollsBackJournalAsync(string connectionString, HttpClient xClient)
    {
        var beforeReports = await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports");
        var beforeEvents = await CanonicalEventCountAsync(connectionString);
        await ExecuteAsync(connectionString, """
            CREATE OR REPLACE FUNCTION pos.z011a_fail_source_completion() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'synthetic source completion failure'; END; $$;
            CREATE TRIGGER z011a_fail_source_completion BEFORE INSERT ON pos.fiscal_action_audit
            FOR EACH ROW EXECUTE FUNCTION pos.z011a_fail_source_completion();
            """);
        try
        {
            using var response = await xClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/",
                new GenerateFiscalXReadingRequest("ej-proof-source-failure", SiteId, IdentityId, ObservedAt.AddMinutes(2)));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(beforeReports, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports"));
            Assert.Equal(beforeEvents, await CanonicalEventCountAsync(connectionString));
        }
        finally
        {
            await ExecuteAsync(connectionString, "DROP TRIGGER IF EXISTS z011a_fail_source_completion ON pos.fiscal_action_audit; DROP FUNCTION IF EXISTS pos.z011a_fail_source_completion();");
        }
    }

    private static async Task ProveDatabaseImmutabilityAndTamperDetectionAsync(string connectionString, HttpClient integrityClient)
    {
        await AssertPostgresFailureAsync(connectionString,
            "UPDATE pos.electronic_journal_records SET event_facts='{\"tampered\":\"true\"}'::jsonb WHERE is_canonical AND stream_sequence_value=1", "23514");
        await AssertPostgresFailureAsync(connectionString,
            "DELETE FROM pos.electronic_journal_records WHERE is_canonical AND stream_sequence_value=1", "23514");

        var originalFacts = await ScalarAsync<string>(connectionString,
            "SELECT event_facts::text FROM pos.electronic_journal_records WHERE is_canonical AND stream_sequence_value=1");
        await ExecuteAsync(connectionString, "ALTER TABLE pos.electronic_journal_records DISABLE TRIGGER trg_electronic_journal_records_immutable; UPDATE pos.electronic_journal_records SET event_facts='{\"tampered\":\"true\"}'::jsonb WHERE is_canonical AND stream_sequence_value=1; ALTER TABLE pos.electronic_journal_records ENABLE TRIGGER trg_electronic_journal_records_immutable;");
        using (var altered = await integrityClient.PostAsJsonAsync("/v1/electronic-journal/integrity-verifications",
                   new VerifyElectronicJournalIntegrityRequest(SiteId, IdentityId, "PHP")))
            Assert.Equal(HttpStatusCode.Conflict, altered.StatusCode);
        await ExecuteAsync(connectionString,
            "ALTER TABLE pos.electronic_journal_records DISABLE TRIGGER trg_electronic_journal_records_immutable; UPDATE pos.electronic_journal_records SET event_facts=@facts::jsonb WHERE is_canonical AND stream_sequence_value=1; ALTER TABLE pos.electronic_journal_records ENABLE TRIGGER trg_electronic_journal_records_immutable;",
            new NpgsqlParameter("facts", originalFacts));

        await ExecuteAsync(connectionString, """
            ALTER TABLE pos.electronic_journal_records DISABLE TRIGGER trg_electronic_journal_records_immutable;
            UPDATE pos.electronic_journal_records SET stream_sequence_value=1000001 WHERE is_canonical AND stream_sequence_value=1;
            UPDATE pos.electronic_journal_records SET stream_sequence_value=1 WHERE is_canonical AND stream_sequence_value=2;
            UPDATE pos.electronic_journal_records SET stream_sequence_value=2 WHERE is_canonical AND stream_sequence_value=1000001;
            ALTER TABLE pos.electronic_journal_records ENABLE TRIGGER trg_electronic_journal_records_immutable;
            """);
        using (var reordered = await integrityClient.PostAsJsonAsync("/v1/electronic-journal/integrity-verifications",
                   new VerifyElectronicJournalIntegrityRequest(SiteId, IdentityId, "PHP")))
            Assert.Equal(HttpStatusCode.Conflict, reordered.StatusCode);
        await ExecuteAsync(connectionString, """
            ALTER TABLE pos.electronic_journal_records DISABLE TRIGGER trg_electronic_journal_records_immutable;
            UPDATE pos.electronic_journal_records SET stream_sequence_value=1000002 WHERE is_canonical AND stream_sequence_value=1;
            UPDATE pos.electronic_journal_records SET stream_sequence_value=1 WHERE is_canonical AND stream_sequence_value=2;
            UPDATE pos.electronic_journal_records SET stream_sequence_value=2 WHERE is_canonical AND stream_sequence_value=1000002;
            ALTER TABLE pos.electronic_journal_records ENABLE TRIGGER trg_electronic_journal_records_immutable;
            """);

        await ExecuteAsync(connectionString, "ALTER TABLE pos.electronic_journal_records DISABLE TRIGGER trg_electronic_journal_records_immutable; DELETE FROM pos.electronic_journal_records WHERE is_canonical AND stream_sequence_value=2; ALTER TABLE pos.electronic_journal_records ENABLE TRIGGER trg_electronic_journal_records_immutable;");
        using var missing = await integrityClient.PostAsJsonAsync("/v1/electronic-journal/integrity-verifications",
            new VerifyElectronicJournalIntegrityRequest(SiteId, IdentityId, "PHP"));
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
    }

    private static async Task<byte[]> ExportAsync(HttpClient client, string format)
    {
        using var response = await client.GetAsync(QueryExport(format));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.True(response.Headers.Contains("ETag"));
        Assert.True(response.Headers.Contains("X-Content-SHA256"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        return await response.Content.ReadAsByteArrayAsync();
    }

    private static async Task<byte[]> ExportReprintsAsync(HttpClient client, string format)
    {
        using var response = await client.GetAsync(QueryExport(format) + "&ThroughSequence=8&EventType=fiscal_document_reprint_recorded&FiscalDocumentReference=" + FiscalDocumentId.ToString("D"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-SHA256"));
        return await response.Content.ReadAsByteArrayAsync();
    }

    private static RecordFiscalDocumentReprintRequest Reprint(string operationKey, string reasonCode) =>
        new(operationKey, SiteId, IdentityId, "PHP", reasonCode);

    private static string Query(int pageSize = 100, string? cursor = null, long? through = null) =>
        $"/v1/electronic-journal/events?SitePosServerId={SiteId:D}&FiscalIdentityId={IdentityId:D}&CurrencyCode=PHP&PageSize={pageSize}" +
        (cursor is null ? string.Empty : $"&Cursor={Uri.EscapeDataString(cursor)}") +
        (through is null ? string.Empty : $"&ThroughSequence={through}");

    private static string QueryExport(string format) =>
        $"/v1/electronic-journal/exports/{format}?SitePosServerId={SiteId:D}&FiscalIdentityId={IdentityId:D}&CurrencyCode=PHP&PageSize=100";

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PosServer"] = connectionString,
            ["PosServer:Admin:ApiKeys:0:Principal"] = "ej-production-service",
            ["PosServer:Admin:ApiKeys:0:Key"] = ProductionKey,
            ["PosServer:Admin:ApiKeys:0:AuthorityClass"] = "PRODUCTION",
            ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = SiteId.ToString("D"),
            ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = IdentityId.ToString("D"),
            ["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"] = "PHP",
            ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalXReadingAuthorization.GeneratePermission,
            ["PosServer:Admin:ApiKeys:0:Permissions:1"] = ElectronicJournalAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:0:Permissions:2"] = ElectronicJournalAuthorization.ExportPermission,
            ["PosServer:Admin:ApiKeys:0:Permissions:3"] = ElectronicJournalAuthorization.IntegrityPermission,
            ["PosServer:Admin:ApiKeys:0:Permissions:4"] = FiscalDocumentReprintAuthorization.RecordPermission,
            ["PosServer:Admin:ApiKeys:1:Principal"] = "ej-fixture-service",
            ["PosServer:Admin:ApiKeys:1:Key"] = FixtureKey,
            ["PosServer:Admin:ApiKeys:1:AuthorityClass"] = "FIXTURE",
            ["PosServer:Admin:ApiKeys:1:SitePosServerIds:0"] = SiteId.ToString("D"),
            ["PosServer:Admin:ApiKeys:1:FiscalIdentityIds:0"] = IdentityId.ToString("D"),
            ["PosServer:Admin:ApiKeys:1:CurrencyCodes:0"] = "PHP",
            ["PosServer:Admin:ApiKeys:1:Permissions:0"] = ElectronicJournalAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:1:Permissions:1"] = FiscalDocumentReprintAuthorization.RecordPermission
        };
        builder.Configuration.AddInMemoryCollection(values);
        builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);
        var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization();
        app.MapFiscalDocumentEndpoints(); app.MapFiscalXReadingEndpoints(); app.MapElectronicJournalEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app, string key, string permission)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var client = new HttpClient { BaseAddress = new(address) };
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, key);
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.PermissionHeaderName, permission);
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName, "ej-proof-correlation");
        return client;
    }

    private static HttpClient CreateUnauthenticatedClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new(address) };
    }

    private static async Task RebuildAsync(string connectionString)
    {
        await ExecuteAsync(connectionString, "DROP SCHEMA IF EXISTS pos CASCADE");
        var root = FindRepositoryRoot();
        foreach (var entry in File.ReadLines(Path.Combine(root, "db", "rebuild", "pos_sql_apply_order.txt")).Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith('#')))
            await ExecuteFileAsync(connectionString, Path.Combine(root, entry.Replace('/', Path.DirectorySeparatorChar)));
        foreach (var file in Directory.GetFiles(Path.Combine(root, "db", "reference-data", "controlled-codes", "generated", "sql"), "*.sql").OrderBy(x => x, StringComparer.Ordinal))
            await ExecuteFileAsync(connectionString, file);
    }

    private static async Task AssertPostgresFailureAsync(string connectionString, string sql, string state)
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connectionString, sql));
        Assert.Equal(state, exception.SqlState);
    }

    private static async Task ExecuteFileAsync(string connectionString, string path) =>
        await ExecuteAsync(connectionString, await File.ReadAllTextAsync(path));

    private static async Task ExecuteAsync(string connectionString, string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 120 };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static Task<long> CanonicalEventCountAsync(string connectionString) =>
        ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.electronic_journal_records WHERE is_canonical");

    private static Task<long> CanonicalReprintCountAsync(string connectionString) =>
        ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.reprint_requests WHERE is_canonical");

    private static Task<long> ReprintEventCountAsync(string connectionString) =>
        ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.electronic_journal_records WHERE is_canonical AND journal_record_type_code_id='77905f62-7d61-521d-bdc3-7c7d57b31efe'");

    private static async Task<FiscalManifest> FiscalManifestAsync(string connectionString) => new(
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_documents"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_document_lines"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_tenders"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_tax_details"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_discount_privilege_details"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_document_applied_statutory_facts"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_totals"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_reporting_periods WHERE period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_z_close_states"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_z_counter_snapshots"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.bir_sales_summary_reports"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_document_status_history"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_sequence_states"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_counter_states"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_state_snapshots"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports"),
        await ScalarAsync<string>(connectionString,
            "SELECT md5(COALESCE(string_agg(row_to_json(d)::text, '|' ORDER BY d.fiscal_document_id::text), '')) FROM pos.fiscal_documents d"),
        await ScalarAsync<string>(connectionString,
            "SELECT md5(COALESCE(string_agg(row_to_json(p)::text, '|' ORDER BY p.fiscal_reporting_period_id::text), '')) FROM pos.fiscal_reporting_periods p"),
        await ScalarAsync<string>(connectionString,
            "SELECT md5(COALESCE(string_agg(row_to_json(s)::text, '|' ORDER BY s.fiscal_z_close_state_id::text), '')) FROM pos.fiscal_z_close_states s"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ExitPass.PosServer.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException();
    }

    private sealed record FiscalManifest(long Documents, long Lines, long Tenders, long TaxDetails, long DiscountDetails,
        long AppliedStatutoryFacts, long Totals, long OpenPeriods, long ZStates, long ZSnapshots, long BirSummaries,
        long StatusHistory, long SequenceStates, long CounterStates, long StateSnapshots, long Reports,
        string FiscalDocumentHash, string ReportingPeriodHash, string ZStateHash);
}
