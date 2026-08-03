using System.Net;
using System.Net.Http.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class FiscalXReadingPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_X_READING_TEST_DB_URL";
    private static readonly Guid SiteId = Guid.Parse("73000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("73000000-0000-4000-8000-000000000002");
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse("2026-08-03T12:00:00Z");

    [Fact]
    public async Task XReadingApiPersistsReplaysConflictsReadsAndPreservesFiscalState()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await RebuildAsync(connectionString);
        await ExecuteFileAsync(connectionString, Path.Combine(FindRepositoryRoot(), "tests", "ExitPass.PosServer.Api.IntegrationTests", "Fixtures", "fiscal_x_reading_runtime_fixture.sql"));
        var before = await ManifestAsync(connectionString);

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var request = new GenerateFiscalXReadingRequest("x-proof-operation", SiteId, IdentityId, ObservedAt);

        using var unauthorized = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", request);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        Authorize(client);
        using var createdResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", request);
        var createdText = await createdResponse.Content.ReadAsStringAsync();
        Assert.True(createdResponse.IsSuccessStatusCode, $"status={(int)createdResponse.StatusCode}; body={createdText}");
        var created = System.Text.Json.JsonSerializer.Deserialize<FiscalXReadingApiResponse>(createdText, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.NotNull(created?.XReading);
        Assert.Equal(2, created.XReading.QualifyingDocumentCount);
        Assert.Equal(22_000, created.XReading.Amounts.GrossSalesAmountMinorUnits);
        Assert.Equal(16_571, created.XReading.Amounts.NetSalesAmountMinorUnits);
        Assert.Equal(2_143, created.XReading.Amounts.PwdDiscountAmountMinorUnits);
        Assert.Equal(1_286, created.XReading.Amounts.VatExemptionAmountMinorUnits);
        Assert.Equal(2, created.XReading.Tenders.Count);
        Assert.Single(created.XReading.FiscalNumberRanges);
        Assert.Empty(created.XReading.FiscalNumberRanges[0].Gaps);

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", request);
        var replay = await replayResponse.Content.ReadFromJsonAsync<FiscalXReadingApiResponse>();
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal(created.XReading.FiscalReportId, replay?.XReading?.FiscalReportId);
        Assert.Equal(created.XReading.FiscalReportReference, replay?.XReading?.FiscalReportReference);

        await using (var restartedApp = await StartApiAsync(connectionString))
        using (var restartedClient = CreateClient(restartedApp))
        {
            Authorize(restartedClient);
            using var restartedReplayResponse = await restartedClient.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", request);
            var restartedReplay = await restartedReplayResponse.Content.ReadFromJsonAsync<FiscalXReadingApiResponse>();
            Assert.Equal(HttpStatusCode.OK, restartedReplayResponse.StatusCode);
            Assert.Equal(created.XReading.FiscalReportId, restartedReplay?.XReading?.FiscalReportId);
        }

        using var conflictResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", request with { ObservedAt = ObservedAt.AddMinutes(1) });
        var conflictText = await conflictResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.DoesNotContain("hash", conflictText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constraint", conflictText, StringComparison.OrdinalIgnoreCase);

        using var getResponse = await client.GetAsync($"/v1/fiscal-reports/x-readings/{created.XReading.FiscalReportReference}");
        var read = await getResponse.Content.ReadFromJsonAsync<FiscalXReadingApiResponse>();
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created.XReading.FiscalReportId, read?.XReading?.FiscalReportId);
        Assert.Equal(created.XReading.Amounts, read?.XReading?.Amounts);
        Assert.Equal(created.XReading.Tenders, read?.XReading?.Tenders);
        Assert.Equal(created.XReading.Discounts, read?.XReading?.Discounts);

        var after = await ManifestAsync(connectionString);
        Assert.Equal(before, after with { XRequests = 0, XReports = 0, TenderBreakdowns = 0, DiscountBreakdowns = 0, FiscalRanges = 0, AuditRows = 0 });
        Assert.Equal(1, after.XRequests);
        Assert.Equal(1, after.XReports);
        Assert.Equal(2, after.TenderBreakdowns);
        Assert.Equal(3, after.DiscountBreakdowns);
        Assert.Equal(1, after.FiscalRanges);
        Assert.Equal(1, after.AuditRows);

        await ProveConcurrentUncommittedDocumentIsExcludedAsync(connectionString, client);
        Assert.Equal(2, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports"));
        Assert.Equal(2, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_documents"));

        await ProvePersistenceFailureRollsBackAsync(connectionString, client, request);

        await InsertUnsupportedTenderDocumentAsync(connectionString);
        using var rejected = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", request with { OperationKey = "x-proof-rejected", ObservedAt = ObservedAt.AddHours(1) });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        Assert.Equal(2, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports"));
        Assert.Equal(0, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_report_requests WHERE operation_idempotency_key='x-proof-rejected'"));

        await AssertImmutableAsync(connectionString, created.XReading.FiscalReportId);
    }

    private static async Task ProveConcurrentUncommittedDocumentIsExcludedAsync(string connectionString, HttpClient client)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand("INSERT INTO pos.fiscal_documents(fiscal_document_id,site_pos_server_id,fiscal_identity_id,fiscal_document_type_code_id,fiscal_document_status_code_id,created_at,updated_at) VALUES('73000000-0000-4000-8000-000000000399',@site,@identity,'73000000-0000-4000-8000-000000000201','73000000-0000-4000-8000-000000000202','2026-08-03T03:00:00Z','2026-08-03T03:00:00Z')", connection, transaction))
        { command.Parameters.AddWithValue("site", SiteId); command.Parameters.AddWithValue("identity", IdentityId); await command.ExecuteNonQueryAsync(); }
        using var response = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", new GenerateFiscalXReadingRequest("x-proof-concurrent", SiteId, IdentityId, ObservedAt));
        var body = await response.Content.ReadFromJsonAsync<FiscalXReadingApiResponse>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, body?.XReading?.QualifyingDocumentCount);
        await transaction.RollbackAsync();
    }

    private static async Task InsertUnsupportedTenderDocumentAsync(string connectionString)
    {
        const string sql = """
        INSERT INTO pos.controlled_codes VALUES('73000000-0000-4000-8000-000000000299','73000000-0000-4000-8000-000000000104','unsupported_tender','Unsupported Tender',NULL,NULL,99,true,NULL,NULL,clock_timestamp(),clock_timestamp());
        INSERT INTO pos.fiscal_documents(fiscal_document_id,site_pos_server_id,fiscal_identity_id,fiscal_document_type_code_id,fiscal_document_status_code_id,fiscal_sequence_policy_id,fiscal_sequence_value,fiscal_document_number,fiscal_series,fiscal_number_assigned_at,created_at,updated_at) VALUES('73000000-0000-4000-8000-000000000303','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','73000000-0000-4000-8000-000000000201','73000000-0000-4000-8000-000000000202','73000000-0000-4000-8000-000000000003',3,'SI-00000003','SI','2026-08-03T03:00:00Z','2026-08-03T03:00:00Z','2026-08-03T03:00:00Z');
        INSERT INTO pos.fiscal_document_lines(fiscal_document_line_id,fiscal_document_id,line_sequence,line_type_code_id,description,gross_amount_minor_units,net_amount_minor_units,currency_code) VALUES('73000000-0000-4000-8000-000000000403','73000000-0000-4000-8000-000000000303',1,'73000000-0000-4000-8000-000000000204','Synthetic unsupported tender',5000,5000,'PHP');
        INSERT INTO pos.fiscal_tenders(fiscal_tender_id,fiscal_document_id,tender_type_code_id,amount_minor_units,currency_code) VALUES('73000000-0000-4000-8000-000000000503','73000000-0000-4000-8000-000000000303','73000000-0000-4000-8000-000000000299',5000,'PHP');
        """;
        await ExecuteAsync(connectionString, sql);
    }

    private static async Task ProvePersistenceFailureRollsBackAsync(
        string connectionString,
        HttpClient client,
        GenerateFiscalXReadingRequest request)
    {
        const string installFailure = """
        CREATE OR REPLACE FUNCTION pos.fail_x_reading_proof_insert()
        RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            RAISE EXCEPTION 'synthetic X Reading persistence failure';
        END;
        $$;
        CREATE TRIGGER trg_x_z_reports__x_reading_proof_failure
        BEFORE INSERT ON pos.x_z_reports
        FOR EACH ROW EXECUTE FUNCTION pos.fail_x_reading_proof_insert();
        """;
        const string removeFailure = """
        DROP TRIGGER IF EXISTS trg_x_z_reports__x_reading_proof_failure ON pos.x_z_reports;
        DROP FUNCTION IF EXISTS pos.fail_x_reading_proof_insert();
        """;

        await ExecuteAsync(connectionString, installFailure);
        try
        {
            const string operationKey = "x-proof-persistence-failure";
            using var response = await client.PostAsJsonAsync(
                "/v1/fiscal-reports/x-readings/",
                request with { OperationKey = operationKey, ObservedAt = ObservedAt.AddHours(2) });
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.DoesNotContain("synthetic", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("trigger", body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await ScalarAsync<long>(connectionString,
                $"SELECT count(*) FROM pos.fiscal_report_requests WHERE operation_idempotency_key='{operationKey}'"));
        }
        finally
        {
            await ExecuteAsync(connectionString, removeFailure);
        }
    }

    private static async Task AssertImmutableAsync(string connectionString, Guid reportId)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand("UPDATE pos.x_z_reports SET gross_sales_amount_minor_units=0 WHERE x_z_report_id=@id", connection); command.Parameters.AddWithValue("id", reportId);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", exception.SqlState);
    }

    private static async Task<XManifest> ManifestAsync(string connectionString) => new(
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_documents"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_document_status_history"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_sequence_states"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_counter_states"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_state_snapshots"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_reporting_periods WHERE period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_z_counter_snapshots"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.reprint_requests"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_report_requests"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id='5dc3cc94-b3ab-5582-a598-e779871fc3e2'"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_report_tender_breakdowns"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_report_discount_breakdowns"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_report_fiscal_number_ranges"),
        await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_action_audit WHERE fiscal_report_request_id IS NOT NULL"));

    private static async Task RebuildAsync(string connectionString)
    {
        await ExecuteAsync(connectionString, "DROP SCHEMA IF EXISTS pos CASCADE"); var root = FindRepositoryRoot();
        foreach (var entry in File.ReadLines(Path.Combine(root, "db", "rebuild", "pos_sql_apply_order.txt")).Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith('#'))) await ExecuteFileAsync(connectionString, Path.Combine(root, entry.Replace('/', Path.DirectorySeparatorChar)));
        foreach (var file in Directory.GetFiles(Path.Combine(root, "db", "reference-data", "controlled-codes", "generated", "sql"), "*.sql").OrderBy(x => x, StringComparer.Ordinal)) await ExecuteFileAsync(connectionString, file);
    }

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0"); builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { { "ConnectionStrings:PosServer", connectionString }, { "PosServer:Admin:ApiKeys:0:Principal", "x-proof-service" }, { "PosServer:Admin:ApiKeys:0:Key", "synthetic-x-proof-key" }, { "PosServer:Admin:ApiKeys:0:Permissions:0", FiscalXReadingAuthorization.GeneratePermission }, { "PosServer:Admin:ApiKeys:0:Permissions:1", FiscalXReadingAuthorization.ReadPermission }, { "PosServer:Admin:ApiKeys:0:SitePosServerIds:0", SiteId.ToString("D") }, { "PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0", IdentityId.ToString("D") } }); builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration); var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapFiscalXReadingEndpoints(); await app.StartAsync(); return app;
    }
    private static HttpClient CreateClient(WebApplication app) { var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single(); return new() { BaseAddress = new(address) }; }
    private static void Authorize(HttpClient client) { client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, "synthetic-x-proof-key"); client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName, "x-proof-correlation"); }
    private static async Task ExecuteFileAsync(string cs, string path) => await ExecuteAsync(cs, await File.ReadAllTextAsync(path));
    private static async Task ExecuteAsync(string cs, string sql) { await using var c = new NpgsqlConnection(cs); await c.OpenAsync(); await using var cmd = new NpgsqlCommand(sql, c) { CommandTimeout = 120 }; await cmd.ExecuteNonQueryAsync(); }
    private static async Task<T> ScalarAsync<T>(string cs, string sql) { await using var c = new NpgsqlConnection(cs); await c.OpenAsync(); await using var cmd = new NpgsqlCommand(sql, c); return (T)(await cmd.ExecuteScalarAsync())!; }
    private static string FindRepositoryRoot() { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d is not null) { if (File.Exists(Path.Combine(d.FullName, "ExitPass.PosServer.sln"))) return d.FullName; d = d.Parent; } throw new DirectoryNotFoundException(); }
    private sealed record XManifest(long Documents, long StatusHistory, long SequenceStates, long CounterStates, long StateSnapshots, long OpenPeriods, long ZReports, long ZCounters, long Reprints, long XRequests, long XReports, long TenderBreakdowns, long DiscountBreakdowns, long FiscalRanges, long AuditRows);
}
