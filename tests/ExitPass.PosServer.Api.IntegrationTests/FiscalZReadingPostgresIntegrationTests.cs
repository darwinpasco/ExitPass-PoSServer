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

public sealed class FiscalZReadingPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_Z_READING_TEST_DB_URL";
    private static readonly Guid SiteId = Guid.Parse("73000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("73000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("73000000-0000-4000-8000-000000000010");

    [Fact]
    public async Task ZReadingApiClosesAtomicallyReplaysConflictsReadsAndPreservesUnrelatedState()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await RebuildAsync(connectionString);
        await ExecuteFileAsync(connectionString, Path.Combine(FindRepositoryRoot(), "tests", "ExitPass.PosServer.Api.IntegrationTests", "Fixtures", "fiscal_x_reading_runtime_fixture.sql"));
        await ExecuteAsync(connectionString, "INSERT INTO pos.site_pos_server_fiscal_identity_history(site_pos_server_fiscal_identity_history_id,site_pos_server_id,fiscal_identity_id,effective_start_at,assignment_reason_text) VALUES('73000000-0000-4000-8000-000000000090','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','2026-01-01T00:00:00Z','Synthetic Z proof scope')");
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);

        var close = new CloseFiscalZReadingRequest("z-close-first-period", SiteId, IdentityId, "PHP", PeriodId, 1);
        using var unauthorized = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", close);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Authorize(client);

        using var initializedResponse = await client.PostAsJsonAsync("/v1/admin/fiscal-z-close-states/initialize",
            new InitializeFiscalZCloseStateRequest("z-state-initialize", SiteId, IdentityId, "PHP", "approved_new_scope_zero", 0, 0, 0, "approved-z007-proof"));
        var initializedText = await initializedResponse.Content.ReadAsStringAsync();
        Assert.True(initializedResponse.IsSuccessStatusCode, initializedText);

        var before = await ManifestAsync(connectionString);
        using var createdResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", close);
        var createdText = await createdResponse.Content.ReadAsStringAsync();
        Assert.True(createdResponse.IsSuccessStatusCode, $"status={(int)createdResponse.StatusCode}; body={createdText}");
        var created = await createdResponse.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>();
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.NotNull(created?.ZReading);
        Assert.Equal("CLOSED", created.ZReading.PeriodStatus);
        Assert.Equal(16_571, created.ZReading.Amounts.NetSalesAmountMinorUnits);
        Assert.Equal(created.ZReading.Amounts.NetSalesAmountMinorUnits, created.ZReading.CounterSnapshot.CurrentPeriodGrandTotalAmountMinorUnits);
        Assert.Equal(0, created.ZReading.CounterSnapshot.PreviousZCounterValue);
        Assert.Equal(1, created.ZReading.CounterSnapshot.ResultingZCounterValue);
        Assert.Equal(created.ZReading.CounterSnapshot.PreviousResetCounterValue, created.ZReading.CounterSnapshot.ResultingResetCounterValue);
        Assert.Equal(2, created.ZReading.CounterSnapshot.ResultingStateVersion);

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", close);
        var replay = await replayResponse.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>();
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal(created.ZReading.FiscalReportId, replay?.ZReading?.FiscalReportId);
        Assert.Equal(created.ZReading.CounterSnapshot, replay?.ZReading?.CounterSnapshot);

        using var conflictResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", close with { ExpectedStateVersion = 2 });
        var conflictText = await conflictResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.DoesNotContain("hash", conflictText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constraint", conflictText, StringComparison.OrdinalIgnoreCase);

        using var differentOperation = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", close with { OperationKey = "z-close-second-identity" });
        Assert.Equal(HttpStatusCode.Conflict, differentOperation.StatusCode);

        using var readResponse = await client.GetAsync($"/v1/fiscal-reports/z-readings/{created.ZReading.FiscalReportReference}");
        var read = await readResponse.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>();
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(created.ZReading.FiscalReportId, read?.ZReading?.FiscalReportId);
        Assert.Equal(created.ZReading.Amounts, read?.ZReading?.Amounts);
        Assert.Equal(created.ZReading.CounterSnapshot, read?.ZReading?.CounterSnapshot);
        Assert.Equal(created.ZReading.Tenders, read?.ZReading?.Tenders);
        Assert.Equal(created.ZReading.Discounts, read?.ZReading?.Discounts);
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(created.ZReading.FiscalNumberRanges),
            System.Text.Json.JsonSerializer.Serialize(read?.ZReading?.FiscalNumberRanges));
        var readText = await readResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("semanticRequestHash", readText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("beneficiary", readText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence", readText, StringComparison.OrdinalIgnoreCase);

        await using (var restarted = await StartApiAsync(connectionString))
        using (var restartedClient = CreateClient(restarted))
        {
            Authorize(restartedClient);
            using var response = await restartedClient.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", close);
            var body = await response.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(created.ZReading.FiscalReportId, body?.ZReading?.FiscalReportId);
        }

        var after = await ManifestAsync(connectionString);
        Assert.Equal(before.Documents, after.Documents);
        Assert.Equal(before.Lines, after.Lines);
        Assert.Equal(before.Tenders, after.Tenders);
        Assert.Equal(before.Taxes, after.Taxes);
        Assert.Equal(before.Discounts, after.Discounts);
        Assert.Equal(before.Totals, after.Totals);
        Assert.Equal(before.StatusHistory, after.StatusHistory);
        Assert.Equal(before.SequenceStates, after.SequenceStates);
        Assert.Equal(before.LegacyCounterStates, after.LegacyCounterStates);
        Assert.Equal(before.FiscalStateSnapshots, after.FiscalStateSnapshots);
        Assert.Equal(before.XReports, after.XReports);
        Assert.Equal(before.Reprints, after.Reprints);
        Assert.Equal(before.ZReports + 1, after.ZReports);
        Assert.Equal(before.ZCounterSnapshots + 1, after.ZCounterSnapshots);
        Assert.Equal(before.ZTransitions + 1, after.ZTransitions);
        Assert.Equal(before.ZStateVersion + 1, after.ZStateVersion);
        Assert.Equal(before.ZCounter + 1, after.ZCounter);
        Assert.Equal(before.Gta + 16_571, after.Gta);
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.fiscal_report_requests WHERE fiscal_report_request_id='{created.ZReading.FiscalReportRequestId}'"));
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.fiscal_report_scopes WHERE fiscal_report_request_id='{created.ZReading.FiscalReportRequestId}'"));
        Assert.True(await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.fiscal_report_tender_breakdowns WHERE x_z_report_id='{created.ZReading.FiscalReportId}'") > 0);
        Assert.True(await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.fiscal_report_discount_breakdowns WHERE x_z_report_id='{created.ZReading.FiscalReportId}'") > 0);
        Assert.True(await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.fiscal_report_fiscal_number_ranges WHERE x_z_report_id='{created.ZReading.FiscalReportId}'") > 0);
        Assert.Equal("closed", await ScalarAsync<string>(connectionString, "SELECT code.code_key FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes code ON code.controlled_code_id=p.period_status_code_id WHERE p.fiscal_reporting_period_id='73000000-0000-4000-8000-000000000010'"));

        await AssertImmutableAsync(connectionString, created.ZReading.FiscalReportId);
        await ProveCompetingCloseAsync(connectionString, client);
        await ProvePersistenceFailureRollsBackAsync(connectionString, client);
    }

    private static async Task ProveCompetingCloseAsync(string connectionString, HttpClient client)
    {
        const string sql = """
        INSERT INTO pos.fiscal_reporting_periods(fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,expected_prior_period_id,opened_at,created_by_ref,updated_by_ref)
        VALUES('73000000-0000-4000-8000-000000000011','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','1a6f7021-bc84-5c01-afaa-c5d6685633c8','2026-08-04','2026-08-04T00:00:00Z','2026-08-04T02:00:00Z','Asia/Manila','00:00:00','PHP',2,'73000000-0000-4000-8000-000000000010','2026-08-04T00:00:00Z','z-proof','z-proof');
        """;
        await ExecuteAsync(connectionString, sql);
        var first = new CloseFiscalZReadingRequest("z-competing-a", SiteId, IdentityId, "PHP", Guid.Parse("73000000-0000-4000-8000-000000000011"), 2);
        var second = first with { OperationKey = "z-competing-b" };
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", first),
            client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", second));
        var diagnostics = string.Join(" | ", await Task.WhenAll(responses.Select(async response =>
            $"{(int)response.StatusCode}:{await response.Content.ReadAsStringAsync()}")));
        Assert.True(responses.Count(response => response.StatusCode == HttpStatusCode.Created) == 1, diagnostics);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports WHERE fiscal_reporting_period_id='73000000-0000-4000-8000-000000000011' AND report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'"));
        Assert.Equal(2L, await ScalarAsync<long>(connectionString, "SELECT z_counter_value FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001' AND fiscal_identity_id='73000000-0000-4000-8000-000000000002' AND currency_code='PHP'"));
    }

    private static async Task ProvePersistenceFailureRollsBackAsync(string connectionString, HttpClient client)
    {
        const string period = """
        INSERT INTO pos.fiscal_reporting_periods(fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,expected_prior_period_id,opened_at,created_by_ref,updated_by_ref)
        VALUES('73000000-0000-4000-8000-000000000012','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','1a6f7021-bc84-5c01-afaa-c5d6685633c8','2026-08-04','2026-08-04T02:00:00Z','2026-08-04T03:00:00Z','Asia/Manila','00:00:00','PHP',3,'73000000-0000-4000-8000-000000000011','2026-08-04T02:00:00Z','z-proof','z-proof');
        CREATE FUNCTION pos.fail_z_proof() RETURNS trigger LANGUAGE plpgsql AS $$BEGIN RAISE EXCEPTION 'synthetic Z proof failure';END;$$;
        CREATE TRIGGER trg_z_proof_failure BEFORE INSERT ON pos.fiscal_z_counter_snapshots FOR EACH ROW EXECUTE FUNCTION pos.fail_z_proof();
        """;
        await ExecuteAsync(connectionString, period);
        var before = await ManifestAsync(connectionString);
        try
        {
            using var response = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/",
                new CloseFiscalZReadingRequest("z-forced-failure", SiteId, IdentityId, "PHP", Guid.Parse("73000000-0000-4000-8000-000000000012"), 3));
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.DoesNotContain("synthetic", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("trigger", body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(before, await ManifestAsync(connectionString));
            Assert.Equal("open", await ScalarAsync<string>(connectionString, "SELECT code.code_key FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes code ON code.controlled_code_id=p.period_status_code_id WHERE p.fiscal_reporting_period_id='73000000-0000-4000-8000-000000000012'"));
        }
        finally
        {
            await ExecuteAsync(connectionString, "DROP TRIGGER IF EXISTS trg_z_proof_failure ON pos.fiscal_z_counter_snapshots; DROP FUNCTION IF EXISTS pos.fail_z_proof();");
        }
    }

    private static async Task AssertImmutableAsync(string connectionString, Guid reportId)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand("UPDATE pos.x_z_reports SET net_sales_amount_minor_units=0 WHERE x_z_report_id=@id", connection); command.Parameters.AddWithValue("id", reportId);
        Assert.Equal("23514", (await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync())).SqlState);
    }

    private static async Task<ZManifest> ManifestAsync(string connectionString) => new(
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_documents"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_document_lines"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_tenders"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_tax_details"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_discount_privilege_details"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_totals"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_document_status_history"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_sequence_states"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_counter_states"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_state_snapshots"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id='5dc3cc94-b3ab-5582-a598-e779871fc3e2'"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_z_counter_snapshots"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.fiscal_z_close_state_transitions WHERE resulting_reporting_period_id IS NOT NULL"),
        await ScalarAsync<long>(connectionString,"SELECT state_version FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),
        await ScalarAsync<long>(connectionString,"SELECT z_counter_value FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),
        await ScalarAsync<long>(connectionString,"SELECT grand_total_amount_minor_units FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),
        await ScalarAsync<long>(connectionString,"SELECT count(*) FROM pos.reprint_requests"));

    private static async Task RebuildAsync(string connectionString)
    {
        await ExecuteAsync(connectionString,"DROP SCHEMA IF EXISTS pos CASCADE"); var root=FindRepositoryRoot();
        foreach(var entry in File.ReadLines(Path.Combine(root,"db","rebuild","pos_sql_apply_order.txt")).Select(x=>x.Trim()).Where(x=>x.Length>0&&!x.StartsWith('#'))) await ExecuteFileAsync(connectionString,Path.Combine(root,entry.Replace('/',Path.DirectorySeparatorChar)));
        foreach(var file in Directory.GetFiles(Path.Combine(root,"db","reference-data","controlled-codes","generated","sql"),"*.sql").OrderBy(x=>x,StringComparer.Ordinal)) await ExecuteFileAsync(connectionString,file);
    }

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder=WebApplication.CreateBuilder();builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["ConnectionStrings:PosServer"]=connectionString,["PosServer:Admin:ApiKeys:0:Principal"]="z-proof-service",["PosServer:Admin:ApiKeys:0:Key"]="synthetic-z-proof-key",
            ["PosServer:Admin:ApiKeys:0:Permissions:0"]=FiscalZCloseStateInitializationAuthorization.Permission,["PosServer:Admin:ApiKeys:0:Permissions:1"]=FiscalZReadingAuthorization.ClosePermission,["PosServer:Admin:ApiKeys:0:Permissions:2"]=FiscalZReadingAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"]=SiteId.ToString("D"),["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"]=IdentityId.ToString("D"),["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"]="PHP"
        });
        builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);var app=builder.Build();app.UseAuthentication();app.UseAuthorization();app.MapFiscalZCloseStateInitializationEndpoints();app.MapFiscalZReadingEndpoints();await app.StartAsync();return app;
    }

    private static HttpClient CreateClient(WebApplication app){var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();return new(){BaseAddress=new(address)};}
    private static void Authorize(HttpClient client){client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName,"synthetic-z-proof-key");client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName,"z-proof-correlation");}
    private static async Task ExecuteFileAsync(string cs,string path)=>await ExecuteAsync(cs,await File.ReadAllTextAsync(path));
    private static async Task ExecuteAsync(string cs,string sql){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using var cmd=new NpgsqlCommand(sql,c){CommandTimeout=120};await cmd.ExecuteNonQueryAsync();}
    private static async Task<T> ScalarAsync<T>(string cs,string sql){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using var cmd=new NpgsqlCommand(sql,c);return (T)(await cmd.ExecuteScalarAsync())!;}
    private static string FindRepositoryRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null){if(File.Exists(Path.Combine(d.FullName,"ExitPass.PosServer.sln")))return d.FullName;d=d.Parent;}throw new DirectoryNotFoundException();}
    private sealed record ZManifest(long Documents,long Lines,long Tenders,long Taxes,long Discounts,long Totals,long StatusHistory,long SequenceStates,long LegacyCounterStates,long FiscalStateSnapshots,long XReports,long ZReports,long ZCounterSnapshots,long ZTransitions,long ZStateVersion,long ZCounter,long Gta,long Reprints);
}
