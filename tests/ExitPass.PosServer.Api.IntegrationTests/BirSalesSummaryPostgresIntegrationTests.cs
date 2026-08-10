using System.Net;
using System.Net.Http.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class BirSalesSummaryPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_BIR_SUMMARY_TEST_DB_URL";
    private static readonly Guid SiteId = Guid.Parse("73000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("73000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("73000000-0000-4000-8000-000000000010");

    [Fact]
    public async Task BirSummaryIsCommittedZBoundReconciledImmutableDeterministicAndGoverned()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await RebuildAsync(connectionString);
        await ExecuteFileAsync(connectionString, Path.Combine(FindRepositoryRoot(), "tests", "ExitPass.PosServer.Api.IntegrationTests", "Fixtures", "fiscal_x_reading_runtime_fixture.sql"));
        await SeedReportingConfigurationAsync(connectionString);

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var z = await CloseZAsync(client, PeriodId, "z-summary-source", 1);
        var summaryRequest = new GenerateBirSalesSummaryRequest("summary-concurrent-a", SiteId, IdentityId, "PHP", PeriodId, z.FiscalReportReference);
        var before = await ManifestAsync(connectionString);

        client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName);
        using var unauthorized = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", summaryRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        Authorize(client, "summary-production-key");
        var competing = await Task.WhenAll(
            client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", summaryRequest),
            client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", summaryRequest with { OperationKey = "summary-concurrent-b" }));
        var diagnostics = string.Join(" | ", await Task.WhenAll(competing.Select(async response => $"{(int)response.StatusCode}:{await response.Content.ReadAsStringAsync()}")));
        Assert.True(competing.Count(response => response.StatusCode == HttpStatusCode.Created) == 1, diagnostics);
        Assert.True(competing.Count(response => response.StatusCode == HttpStatusCode.OK) == 1, diagnostics);
        var createdResponse = competing.Single(response => response.StatusCode == HttpStatusCode.Created);
        var created = await createdResponse.Content.ReadFromJsonAsync<BirSalesSummaryApiResponse>();
        Assert.NotNull(created?.Summary);
        var summary = created.Summary;
        Assert.Equal(z.FiscalReportId, summary.GoverningZReportId);
        Assert.Equal(z.FiscalReportReference, summary.GoverningZReadingReference);
        Assert.Equal(z.QualifyingDocumentCount, summary.TransactionCount);
        Assert.Equal(z.Amounts, summary.Amounts);
        Assert.Equal(z.CounterSnapshot, summary.CounterSnapshot);
        Assert.Equal("COMMITTED", summary.ReportStatus);
        Assert.True(summary.Immutable);

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", summaryRequest);
        var replay = await replayResponse.Content.ReadFromJsonAsync<BirSalesSummaryApiResponse>();
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal(summary.BirSalesSummaryReportId, replay?.Summary?.BirSalesSummaryReportId);

        using var readResponse = await client.GetAsync($"/v1/fiscal-reports/bir-sales-summaries/{summary.BirSalesSummaryReportId:D}");
        var read = await readResponse.Content.ReadFromJsonAsync<BirSalesSummaryApiResponse>();
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(summary), System.Text.Json.JsonSerializer.Serialize(read?.Summary));
        var readText = await readResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("semanticRequestHash", readText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("beneficiary", readText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apiKey", readText, StringComparison.OrdinalIgnoreCase);

        var json1 = await ExportAsync(client, summary.BirSalesSummaryReportId, "json");
        var csv1 = await ExportAsync(client, summary.BirSalesSummaryReportId, "csv");
        Assert.Contains(z.FiscalReportReference, System.Text.Encoding.UTF8.GetString(json1.Bytes), StringComparison.Ordinal);
        Assert.Contains("pwd_statutory", System.Text.Encoding.UTF8.GetString(csv1.Bytes), StringComparison.Ordinal);

        await using (var restarted = await StartApiAsync(connectionString))
        using (var restartedClient = CreateClient(restarted))
        {
            Authorize(restartedClient, "summary-production-key");
            var json2 = await ExportAsync(restartedClient, summary.BirSalesSummaryReportId, "json");
            var csv2 = await ExportAsync(restartedClient, summary.BirSalesSummaryReportId, "csv");
            Assert.Equal(json1.Bytes, json2.Bytes); Assert.Equal(json1.Hash, json2.Hash);
            Assert.Equal(csv1.Bytes, csv2.Bytes); Assert.Equal(csv1.Hash, csv2.Hash);
        }

        using var unsupported = await client.GetAsync($"/v1/fiscal-reports/bir-sales-summaries/{summary.BirSalesSummaryReportId:D}/exports/xlsx");
        Assert.Equal(HttpStatusCode.BadRequest, unsupported.StatusCode);
        using var malformed = await client.GetAsync("/v1/fiscal-reports/bir-sales-summaries/not-a-guid/exports/json");
        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);

        var after = await ManifestAsync(connectionString);
        Assert.Equal(before.Documents, after.Documents); Assert.Equal(before.Lines, after.Lines);
        Assert.Equal(before.Tenders, after.Tenders); Assert.Equal(before.Taxes, after.Taxes);
        Assert.Equal(before.Discounts, after.Discounts); Assert.Equal(before.Totals, after.Totals);
        Assert.Equal(before.ZReports, after.ZReports); Assert.Equal(before.ZCounter, after.ZCounter);
        Assert.Equal(before.ResetCounter, after.ResetCounter); Assert.Equal(before.Gta, after.Gta);
        Assert.Equal(before.PeriodStatus, after.PeriodStatus); Assert.Equal(before.OtherReports, after.OtherReports);
        Assert.Equal(before.Summaries + 1, after.Summaries);
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.bir_sales_summary_reports WHERE bir_sales_summary_report_id='{summary.BirSalesSummaryReportId}'"));
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.fiscal_action_audit WHERE fiscal_report_request_id='{summary.FiscalReportRequestId}' AND char_length(correlation_ref)>0"));
        await AssertImmutableAsync(connectionString, summary.BirSalesSummaryReportId);

        await ProveAuthorizationDenialsAsync(client, summary.BirSalesSummaryReportId, summaryRequest);
        await ProveOpenAndMissingZRejectionAsync(connectionString, client);
        await ProveConflictRollbackRetryAndReconciliationAsync(connectionString, client, summary.OperationKey);
    }

    private static async Task ProveAuthorizationDenialsAsync(HttpClient client, Guid summaryId, GenerateBirSalesSummaryRequest request)
    {
        Authorize(client, "summary-readonly-key");
        using var missingPermission = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", request with { OperationKey = "denied-permission" });
        Assert.Equal(HttpStatusCode.Forbidden, missingPermission.StatusCode);
        Authorize(client, "summary-wrong-scope-key");
        using var wrongScope = await client.GetAsync($"/v1/fiscal-reports/bir-sales-summaries/{summaryId:D}");
        Assert.Equal(HttpStatusCode.NotFound, wrongScope.StatusCode);
        Authorize(client, "summary-fixture-key");
        using var fixture = await client.GetAsync($"/v1/fiscal-reports/bir-sales-summaries/{summaryId:D}");
        Assert.Equal(HttpStatusCode.NotFound, fixture.StatusCode);
        Authorize(client, "summary-production-key");
    }

    private static async Task ProveOpenAndMissingZRejectionAsync(string cs, HttpClient client)
    {
        await ExecuteAsync(cs, """
        INSERT INTO pos.fiscal_reporting_periods(fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,expected_prior_period_id,opened_at,closing_started_at,closed_at,created_by_ref,updated_by_ref)
        VALUES('85000000-0000-4000-8000-000000000001','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','1a6f7021-bc84-5c01-afaa-c5d6685633c8','2026-08-05','2026-08-05T00:00:00Z','2026-08-05T01:00:00Z','Asia/Manila','00:00:00','PHP',20,'73000000-0000-4000-8000-000000000010','2026-08-05T00:00:00Z',NULL,NULL,'summary-proof','summary-proof'),
        ('85000000-0000-4000-8000-000000000002','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','af7ee931-a023-507e-81a4-17adf047eb94','2026-08-06','2026-08-06T00:00:00Z','2026-08-06T01:00:00Z','Asia/Manila','00:00:00','PHP',21,'85000000-0000-4000-8000-000000000001','2026-08-06T00:00:00Z','2026-08-06T01:00:00Z','2026-08-06T01:00:00Z','summary-proof','summary-proof');
        """);
        using var open = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", new GenerateBirSalesSummaryRequest("open-period",SiteId,IdentityId,"PHP",Guid.Parse("85000000-0000-4000-8000-000000000001"),"Z-NONE"));
        Assert.Equal(HttpStatusCode.Conflict, open.StatusCode);
        using var absent = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", new GenerateBirSalesSummaryRequest("closed-no-z",SiteId,IdentityId,"PHP",Guid.Parse("85000000-0000-4000-8000-000000000002"),"Z-NONE"));
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
    }

    private static async Task ProveConflictRollbackRetryAndReconciliationAsync(string cs, HttpClient client, string committedOperationKey)
    {
        await ExecuteAsync(cs, """
        INSERT INTO pos.fiscal_reporting_periods(fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,expected_prior_period_id,opened_at,created_by_ref,updated_by_ref)
        VALUES('85000000-0000-4000-8000-000000000010','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','1a6f7021-bc84-5c01-afaa-c5d6685633c8','2026-08-07','2026-08-07T00:00:00Z','2026-08-07T01:00:00Z','Asia/Manila','00:00:00','PHP',2,'73000000-0000-4000-8000-000000000010','2026-08-07T00:00:00Z','summary-proof','summary-proof');
        """);
        var secondZ = await CloseZAsync(client, Guid.Parse("85000000-0000-4000-8000-000000000010"), "z-summary-second", 2);
        using var conflict = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", new GenerateBirSalesSummaryRequest(committedOperationKey,SiteId,IdentityId,"PHP",secondZ.FiscalReportingPeriodId,secondZ.FiscalReportReference));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        await ExecuteAsync(cs, "ALTER TABLE pos.x_z_reports DISABLE TRIGGER trg_x_z_reports_immutable; UPDATE pos.x_z_reports SET net_sales_amount_minor_units=net_sales_amount_minor_units+1 WHERE x_z_report_id='"+secondZ.FiscalReportId+"'; ALTER TABLE pos.x_z_reports ENABLE TRIGGER trg_x_z_reports_immutable;");
        using var mismatch = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", new GenerateBirSalesSummaryRequest("summary-reconciliation",SiteId,IdentityId,"PHP",secondZ.FiscalReportingPeriodId,secondZ.FiscalReportReference));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mismatch.StatusCode);
        Assert.Equal(0L, await ScalarAsync<long>(cs, "SELECT count(*) FROM pos.fiscal_report_requests WHERE operation_idempotency_key='summary-reconciliation'"));
        await ExecuteAsync(cs, "ALTER TABLE pos.x_z_reports DISABLE TRIGGER trg_x_z_reports_immutable; UPDATE pos.x_z_reports SET net_sales_amount_minor_units=net_sales_amount_minor_units-1 WHERE x_z_report_id='"+secondZ.FiscalReportId+"'; ALTER TABLE pos.x_z_reports ENABLE TRIGGER trg_x_z_reports_immutable;");

        await ExecuteAsync(cs, "CREATE FUNCTION pos.fail_summary_proof() RETURNS trigger LANGUAGE plpgsql AS $$BEGIN RAISE EXCEPTION 'synthetic summary failure';END;$$; CREATE TRIGGER trg_summary_proof_failure BEFORE INSERT ON pos.bir_sales_summary_reports FOR EACH ROW EXECUTE FUNCTION pos.fail_summary_proof();");
        var request = new GenerateBirSalesSummaryRequest("summary-rollback",SiteId,IdentityId,"PHP",secondZ.FiscalReportingPeriodId,secondZ.FiscalReportReference);
        try
        {
            var count = await ScalarAsync<long>(cs, "SELECT count(*) FROM pos.bir_sales_summary_reports");
            using var failed = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", request);
            var body = await failed.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            Assert.DoesNotContain("synthetic", body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(count, await ScalarAsync<long>(cs, "SELECT count(*) FROM pos.bir_sales_summary_reports"));
            Assert.Equal(0L, await ScalarAsync<long>(cs, "SELECT count(*) FROM pos.fiscal_report_requests WHERE operation_idempotency_key='summary-rollback'"));
        }
        finally
        {
            await ExecuteAsync(cs, "DROP TRIGGER IF EXISTS trg_summary_proof_failure ON pos.bir_sales_summary_reports; DROP FUNCTION IF EXISTS pos.fail_summary_proof();");
        }
        using var retry = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", request);
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);

    }

    private static async Task<FiscalZReadingRecord> CloseZAsync(HttpClient client, Guid periodId, string operation, long stateVersion)
    {
        Authorize(client, "summary-production-key");
        if (stateVersion == 1)
        {
            using var initialized = await client.PostAsJsonAsync("/v1/admin/fiscal-z-close-states/initialize", new InitializeFiscalZCloseStateRequest("summary-z-init",SiteId,IdentityId,"PHP","approved_new_scope_zero",0,0,0,"summary-proof"));
            Assert.True(initialized.IsSuccessStatusCode, await initialized.Content.ReadAsStringAsync());
        }
        using var response = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", new CloseFiscalZReadingRequest(operation,SiteId,IdentityId,"PHP",periodId,stateVersion));
        var text = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode,text);
        return (await response.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>())!.ZReading!;
    }

    private static async Task<(byte[] Bytes,string Hash)> ExportAsync(HttpClient client, Guid id, string format)
    {
        using var response=await client.GetAsync($"/v1/fiscal-reports/bir-sales-summaries/{id:D}/exports/{format}");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);return(await response.Content.ReadAsByteArrayAsync(),response.Headers.GetValues("X-Content-SHA256").Single());
    }

    private static async Task SeedReportingConfigurationAsync(string cs)=>await ExecuteAsync(cs,"""
    INSERT INTO pos.site_pos_server_fiscal_identity_history(site_pos_server_fiscal_identity_history_id,site_pos_server_id,fiscal_identity_id,effective_start_at,assignment_reason_text) VALUES('86000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','2026-01-01T00:00:00Z','Synthetic summary proof scope');
    INSERT INTO pos.sales_invoice_header_profiles(sales_invoice_header_profile_id,fiscal_identity_id,site_id,site_pos_server_id,profile_version,template_version,presentation_version,pos_serial_number,machine_identification_number,parking_location_display,bir_accreditation_number,bir_accreditation_issued_date,bir_accreditation_valid_until,ptu_number,ptu_issued_date,sales_invoice_legal_statement,customer_service_footer,effective_from,lifecycle_status,approved_at,approved_by_ref,created_by_ref,updated_by_ref)
    VALUES('86000000-0000-4000-8000-000000000002','73000000-0000-4000-8000-000000000002','86000000-0000-4000-8000-000000000003','73000000-0000-4000-8000-000000000001','summary-profile-v1','digital-sales-invoice-json-v1','digital-sales-invoice-presentation-json-v1','SYN-SERIAL-Z010','SYN-MIN-Z010','SYNTHETIC TEST SITE','SYN-ACCREDITATION-Z010','2026-01-01','2027-01-01','SYN-PTU-Z010','2026-01-01','SYNTHETIC SALES INVOICE','SYNTHETIC SUPPORT','2026-01-01T00:00:00Z','APPROVED','2026-01-01T00:00:00Z','summary-proof','summary-proof','summary-proof');
    """);

    private static async Task AssertImmutableAsync(string cs,Guid id){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using(var update=new NpgsqlCommand("UPDATE pos.bir_sales_summary_reports SET net_sales_amount_minor_units=0 WHERE bir_sales_summary_report_id=@id",c)){update.Parameters.AddWithValue("id",id);Assert.Equal("23514",(await Assert.ThrowsAsync<PostgresException>(()=>update.ExecuteNonQueryAsync())).SqlState);}await using(var delete=new NpgsqlCommand("DELETE FROM pos.bir_sales_summary_reports WHERE bir_sales_summary_report_id=@id",c)){delete.Parameters.AddWithValue("id",id);Assert.Equal("23514",(await Assert.ThrowsAsync<PostgresException>(()=>delete.ExecuteNonQueryAsync())).SqlState);}}
    private static async Task<Manifest> ManifestAsync(string cs)=>new(await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_documents"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_document_lines"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_tenders"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_tax_details"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_discount_privilege_details"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_totals"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'"),await ScalarAsync<long>(cs,"SELECT z_counter_value FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),await ScalarAsync<long>(cs,"SELECT reset_counter_value FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),await ScalarAsync<long>(cs,"SELECT grand_total_amount_minor_units FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),await ScalarAsync<string>(cs,"SELECT code.code_key FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes code ON code.controlled_code_id=p.period_status_code_id WHERE p.fiscal_reporting_period_id='73000000-0000-4000-8000-000000000010'"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id<>'1c628bc2-49c3-53e8-ae83-2082bcf28467'"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.bir_sales_summary_reports"));

    private static async Task RebuildAsync(string cs){await ExecuteAsync(cs,"DROP SCHEMA IF EXISTS pos CASCADE");var root=FindRepositoryRoot();foreach(var entry in File.ReadLines(Path.Combine(root,"db","rebuild","pos_sql_apply_order.txt")).Select(x=>x.Trim()).Where(x=>x.Length>0&&!x.StartsWith('#')))await ExecuteFileAsync(cs,Path.Combine(root,entry.Replace('/',Path.DirectorySeparatorChar)));foreach(var file in Directory.GetFiles(Path.Combine(root,"db","reference-data","controlled-codes","generated","sql"),"*.sql").OrderBy(x=>x,StringComparer.Ordinal))await ExecuteFileAsync(cs,file);}
    private static async Task<WebApplication> StartApiAsync(string cs){var builder=WebApplication.CreateBuilder(new WebApplicationOptions{EnvironmentName=Environments.Production});builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");builder.Configuration.AddInMemoryCollection(Configuration(cs));builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);var app=builder.Build();app.UseAuthentication();app.UseAuthorization();app.MapFiscalZCloseStateInitializationEndpoints();app.MapFiscalZReadingEndpoints();app.MapBirSalesSummaryEndpoints();await app.StartAsync();return app;}
    private static Dictionary<string,string?> Configuration(string cs)=>new(){{"ConnectionStrings:PosServer",cs},{"PosServer:Admin:ApiKeys:0:Principal","summary-production"},{"PosServer:Admin:ApiKeys:0:Key","summary-production-key"},{"PosServer:Admin:ApiKeys:0:Permissions:0",FiscalZCloseStateInitializationAuthorization.Permission},{"PosServer:Admin:ApiKeys:0:Permissions:1",FiscalZReadingAuthorization.ClosePermission},{"PosServer:Admin:ApiKeys:0:Permissions:2",FiscalZReadingAuthorization.ReadPermission},{"PosServer:Admin:ApiKeys:0:Permissions:3",BirSalesSummaryAuthorization.GeneratePermission},{"PosServer:Admin:ApiKeys:0:Permissions:4",BirSalesSummaryAuthorization.ReadPermission},{"PosServer:Admin:ApiKeys:0:Permissions:5",BirSalesSummaryAuthorization.ExportPermission},{"PosServer:Admin:ApiKeys:0:SitePosServerIds:0",SiteId.ToString("D")},{"PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0",IdentityId.ToString("D")},{"PosServer:Admin:ApiKeys:0:CurrencyCodes:0","PHP"},{"PosServer:Admin:ApiKeys:1:Principal","summary-readonly"},{"PosServer:Admin:ApiKeys:1:Key","summary-readonly-key"},{"PosServer:Admin:ApiKeys:1:Permissions:0",BirSalesSummaryAuthorization.ReadPermission},{"PosServer:Admin:ApiKeys:1:SitePosServerIds:0",SiteId.ToString("D")},{"PosServer:Admin:ApiKeys:1:FiscalIdentityIds:0",IdentityId.ToString("D")},{"PosServer:Admin:ApiKeys:1:CurrencyCodes:0","PHP"},{"PosServer:Admin:ApiKeys:2:Principal","summary-wrong-scope"},{"PosServer:Admin:ApiKeys:2:Key","summary-wrong-scope-key"},{"PosServer:Admin:ApiKeys:2:Permissions:0",BirSalesSummaryAuthorization.ReadPermission},{"PosServer:Admin:ApiKeys:2:SitePosServerIds:0",Guid.NewGuid().ToString("D")},{"PosServer:Admin:ApiKeys:2:FiscalIdentityIds:0",IdentityId.ToString("D")},{"PosServer:Admin:ApiKeys:2:CurrencyCodes:0","PHP"},{"PosServer:Admin:ApiKeys:3:Principal","summary-fixture"},{"PosServer:Admin:ApiKeys:3:Key","summary-fixture-key"},{"PosServer:Admin:ApiKeys:3:AuthorityClass","FIXTURE"},{"PosServer:Admin:ApiKeys:3:Permissions:0",BirSalesSummaryAuthorization.ReadPermission},{"PosServer:Admin:ApiKeys:3:SitePosServerIds:0",SiteId.ToString("D")},{"PosServer:Admin:ApiKeys:3:FiscalIdentityIds:0",IdentityId.ToString("D")},{"PosServer:Admin:ApiKeys:3:CurrencyCodes:0","PHP"}};
    private static HttpClient CreateClient(WebApplication app){var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();return new(){BaseAddress=new(address)};}
    private static void Authorize(HttpClient client,string key){client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName);client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.PermissionHeaderName);client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName);client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName,key);client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName,"summary-proof-correlation");}
    private static async Task ExecuteFileAsync(string cs,string path)=>await ExecuteAsync(cs,await File.ReadAllTextAsync(path));private static async Task ExecuteAsync(string cs,string sql){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using var cmd=new NpgsqlCommand(sql,c){CommandTimeout=120};await cmd.ExecuteNonQueryAsync();}private static async Task<T> ScalarAsync<T>(string cs,string sql){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using var cmd=new NpgsqlCommand(sql,c);return(T)(await cmd.ExecuteScalarAsync())!;}private static string FindRepositoryRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null){if(File.Exists(Path.Combine(d.FullName,"ExitPass.PosServer.sln")))return d.FullName;d=d.Parent;}throw new DirectoryNotFoundException();}
    private sealed record Manifest(long Documents,long Lines,long Tenders,long Taxes,long Discounts,long Totals,long ZReports,long ZCounter,long ResetCounter,long Gta,string PeriodStatus,long OtherReports,long Summaries);
}
