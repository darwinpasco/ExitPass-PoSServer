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

[CollectionDefinition(nameof(BirSalesSummaryPostgresCollection), DisableParallelization = true)]
public sealed class BirSalesSummaryPostgresCollection;

[Collection(nameof(BirSalesSummaryPostgresCollection))]
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
        await ExecuteAsync(connectionString, "UPDATE pos.fiscal_discount_privilege_details SET vat_privilege_amount_minor_units=0 WHERE fiscal_discount_privilege_detail_id='73000000-0000-4000-8000-000000000702'");
        await SeedReportingConfigurationAsync(connectionString);

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var close = await CloseZAsync(client, PeriodId, "z-summary-source", 1);
        var z = close.ZReading!;
        Assert.Equal("COMMITTED", close.BirSalesSummaryCompletion?.Status);
        Assert.NotNull(close.BirSalesSummaryCompletion?.SummaryId);
        var summaryRequest = new GenerateBirSalesSummaryRequest("summary-concurrent-a", SiteId, IdentityId, "PHP", PeriodId, z.FiscalReportReference);
        client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName);
        using var unauthorized = await client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", summaryRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        Authorize(client, "summary-production-key");
        var competing = await Task.WhenAll(
            client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", summaryRequest),
            client.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", summaryRequest with { OperationKey = "summary-concurrent-b" }));
        var diagnostics = string.Join(" | ", await Task.WhenAll(competing.Select(async response => $"{(int)response.StatusCode}:{await response.Content.ReadAsStringAsync()}")));
        Assert.True(competing.All(response => response.StatusCode == HttpStatusCode.OK), diagnostics);
        var created = await competing[0].Content.ReadFromJsonAsync<BirSalesSummaryApiResponse>();
        Assert.NotNull(created?.Summary);
        var summary = created.Summary;
        Assert.Equal(close.BirSalesSummaryCompletion!.SummaryId, summary.BirSalesSummaryReportId);
        Assert.Equal(z.FiscalReportId, summary.GoverningZReportId);
        Assert.Equal(z.FiscalReportReference, summary.GoverningZReadingReference);
        Assert.Equal(z.QualifyingDocumentCount, summary.TransactionCount);
        Assert.Equal(z.Amounts, summary.Amounts);
        Assert.Contains(summary.Tenders, tender => tender.Classification == "qrph" && tender.AmountMinorUnits == 8_000);
        Assert.Contains(summary.Tenders, tender => tender.Classification == "gcash" && tender.AmountMinorUnits == 8_571);
        Assert.Equal(z.Tenders, summary.Tenders);
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

        using var monthlyResponse = await client.GetAsync($"/v1/fiscal-reports/monthly-esales/?sitePosServerId={SiteId:D}&fiscalIdentityId={IdentityId:D}&currencyCode=PHP&calendarYear=2026&calendarMonth=8");
        var monthly = await monthlyResponse.Content.ReadFromJsonAsync<MonthlyESalesApiResponse>();
        Assert.Equal(HttpStatusCode.OK, monthlyResponse.StatusCode);
        var monthlyRow = Assert.Single(monthly!.Report!.Rows);
        Assert.Equal("SYN-MIN-Z010", monthlyRow.MachineIdentificationNumber);
        Assert.Equal(summary.TransactionCount, monthlyRow.TransactionCount);
        Assert.Equal(summary.Amounts.GrossSalesAmountMinorUnits, monthlyRow.MonthlySalesAmountMinorUnits);
        Assert.Equal(summary.Amounts.VatableSalesAmountMinorUnits, monthlyRow.VatableSalesAmountMinorUnits);
        Assert.Equal(summary.Amounts.VatAmountMinorUnits, monthlyRow.VatAmountMinorUnits);
        Assert.Equal(summary.Amounts.VatExemptSalesAmountMinorUnits, monthlyRow.VatExemptSalesAmountMinorUnits);
        Assert.Equal(summary.Amounts.ZeroRatedSalesAmountMinorUnits, monthlyRow.ZeroRatedSalesAmountMinorUnits);
        Assert.Equal(summary.EndingSalesInvoiceReference, monthlyRow.EndingSalesInvoiceReference);

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

        await ProveAnnexE1FlowAsync(client, connectionString);
        var afterAnnex = await ManifestAsync(connectionString);
        var after = await ManifestAsync(connectionString);
        Assert.Equal(afterAnnex.Documents, after.Documents); Assert.Equal(afterAnnex.Lines, after.Lines);
        Assert.Equal(afterAnnex.Tenders, after.Tenders); Assert.Equal(afterAnnex.Taxes, after.Taxes);
        Assert.Equal(afterAnnex.Discounts, after.Discounts); Assert.Equal(afterAnnex.Totals, after.Totals);
        Assert.Equal(afterAnnex.ZReports, after.ZReports); Assert.Equal(afterAnnex.ZCounter, after.ZCounter);
        Assert.Equal(afterAnnex.ResetCounter, after.ResetCounter); Assert.Equal(afterAnnex.Gta, after.Gta);
        Assert.Equal(afterAnnex.PeriodStatus, after.PeriodStatus); Assert.Equal(afterAnnex.OtherReports, after.OtherReports);
        Assert.Equal(afterAnnex.Summaries, after.Summaries);
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.bir_sales_summary_reports WHERE bir_sales_summary_report_id='{summary.BirSalesSummaryReportId}'"));
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM pos.fiscal_action_audit WHERE fiscal_report_request_id='{summary.FiscalReportRequestId}' AND char_length(correlation_ref)>0"));
        await AssertImmutableAsync(connectionString, summary.BirSalesSummaryReportId);

        await ProveAuthorizationDenialsAsync(client, summary.BirSalesSummaryReportId, summaryRequest);
        await ProveOpenAndMissingZRejectionAsync(connectionString, client);
        await ProveConflictRollbackRetryAndReconciliationAsync(connectionString, client, summary.OperationKey);
    }

    [Fact]
    public async Task AutomaticBirFailureLeavesZCommittedAndReplayRepairsTheDerivativeSummary()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await RebuildAsync(connectionString);
        await ExecuteFileAsync(connectionString, Path.Combine(FindRepositoryRoot(), "tests", "ExitPass.PosServer.Api.IntegrationTests", "Fixtures", "fiscal_x_reading_runtime_fixture.sql"));
        await SeedReportingConfigurationAsync(connectionString);
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);

        await ExecuteAsync(connectionString, "CREATE FUNCTION pos.fail_auto_summary_proof() RETURNS trigger LANGUAGE plpgsql AS $$BEGIN RAISE EXCEPTION 'synthetic auto summary failure';END;$$; CREATE TRIGGER trg_auto_summary_proof_failure BEFORE INSERT ON pos.bir_sales_summary_reports FOR EACH ROW EXECUTE FUNCTION pos.fail_auto_summary_proof();");
        FiscalZReadingApiResponse close;
        try
        {
            close = await CloseZAsync(client, PeriodId, "z-auto-summary-failure", 1);
        }
        finally
        {
            await ExecuteAsync(connectionString, "DROP TRIGGER IF EXISTS trg_auto_summary_proof_failure ON pos.bir_sales_summary_reports; DROP FUNCTION IF EXISTS pos.fail_auto_summary_proof();");
        }

        Assert.Equal("PENDING", close.BirSalesSummaryCompletion?.Status);
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'"));
        Assert.Equal("closed", await ScalarAsync<string>(connectionString, $"SELECT c.code_key FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes c ON c.controlled_code_id=p.period_status_code_id WHERE p.fiscal_reporting_period_id='{PeriodId:D}'"));
        Assert.Equal(0L, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.bir_sales_summary_reports"));

        var replayRequest = new CloseFiscalZReadingRequest("z-auto-summary-failure",SiteId,IdentityId,"PHP",PeriodId,1);
        var replayResponses = await Task.WhenAll(
            client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", replayRequest),
            client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", replayRequest));
        Assert.All(replayResponses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var replays = await Task.WhenAll(replayResponses.Select(response => response.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>()));
        Assert.All(replays, replay => Assert.Equal("COMMITTED", replay?.BirSalesSummaryCompletion?.Status));
        Assert.Single(replays.Select(replay => replay?.BirSalesSummaryCompletion?.SummaryId).Distinct());
        Assert.Equal(1L, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.bir_sales_summary_reports"));
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
        VALUES('85000000-0000-4000-8000-000000000010','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','1a6f7021-bc84-5c01-afaa-c5d6685633c8','2026-08-07','2026-08-07T00:00:00Z','2026-08-07T01:00:00Z','Asia/Manila','00:00:00','PHP',3,'88000000-0000-4000-8000-000000000001','2026-08-07T00:00:00Z','summary-proof','summary-proof');
        """);
        await using var manualApp = await StartApiAsync(cs, automaticBirSummary: false);
        using var manualClient = CreateClient(manualApp);
        Authorize(manualClient, "summary-production-key");
        var secondZ = (await CloseZAsync(manualClient, Guid.Parse("85000000-0000-4000-8000-000000000010"), "z-summary-second", 3)).ZReading!;
        using var conflict = await manualClient.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", new GenerateBirSalesSummaryRequest(committedOperationKey,SiteId,IdentityId,"PHP",secondZ.FiscalReportingPeriodId,secondZ.FiscalReportReference));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        await ExecuteAsync(cs, "ALTER TABLE pos.x_z_reports DISABLE TRIGGER trg_x_z_reports_immutable; UPDATE pos.x_z_reports SET net_sales_amount_minor_units=net_sales_amount_minor_units+1 WHERE x_z_report_id='"+secondZ.FiscalReportId+"'; ALTER TABLE pos.x_z_reports ENABLE TRIGGER trg_x_z_reports_immutable;");
        using var mismatch = await manualClient.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", new GenerateBirSalesSummaryRequest("summary-reconciliation",SiteId,IdentityId,"PHP",secondZ.FiscalReportingPeriodId,secondZ.FiscalReportReference));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mismatch.StatusCode);
        Assert.Equal(0L, await ScalarAsync<long>(cs, "SELECT count(*) FROM pos.fiscal_report_requests WHERE operation_idempotency_key='summary-reconciliation'"));
        await ExecuteAsync(cs, "ALTER TABLE pos.x_z_reports DISABLE TRIGGER trg_x_z_reports_immutable; UPDATE pos.x_z_reports SET net_sales_amount_minor_units=net_sales_amount_minor_units-1 WHERE x_z_report_id='"+secondZ.FiscalReportId+"'; ALTER TABLE pos.x_z_reports ENABLE TRIGGER trg_x_z_reports_immutable;");

        await ExecuteAsync(cs, "CREATE FUNCTION pos.fail_summary_proof() RETURNS trigger LANGUAGE plpgsql AS $$BEGIN RAISE EXCEPTION 'synthetic summary failure';END;$$; CREATE TRIGGER trg_summary_proof_failure BEFORE INSERT ON pos.bir_sales_summary_reports FOR EACH ROW EXECUTE FUNCTION pos.fail_summary_proof();");
        var request = new GenerateBirSalesSummaryRequest("summary-rollback",SiteId,IdentityId,"PHP",secondZ.FiscalReportingPeriodId,secondZ.FiscalReportReference);
        try
        {
            var count = await ScalarAsync<long>(cs, "SELECT count(*) FROM pos.bir_sales_summary_reports");
            using var failed = await manualClient.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", request);
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
        using var retry = await manualClient.PostAsJsonAsync("/v1/fiscal-reports/bir-sales-summaries/", request);
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);

    }

    private static async Task<FiscalZReadingApiResponse> CloseZAsync(HttpClient client, Guid periodId, string operation, long stateVersion)
    {
        Authorize(client, "summary-production-key");
        if (stateVersion == 1)
        {
            using var initialized = await client.PostAsJsonAsync("/v1/admin/fiscal-z-close-states/initialize", new InitializeFiscalZCloseStateRequest("summary-z-init",SiteId,IdentityId,"PHP","approved_new_scope_zero",0,0,0,"summary-proof"));
            Assert.True(initialized.IsSuccessStatusCode, await initialized.Content.ReadAsStringAsync());
        }
        using var response = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/", new CloseFiscalZReadingRequest(operation,SiteId,IdentityId,"PHP",periodId,stateVersion));
        var text = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode,text);
        return (await response.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>())!;
    }

    private static async Task<(byte[] Bytes,string Hash)> ExportAsync(HttpClient client, Guid id, string format)
    {
        using var response=await client.GetAsync($"/v1/fiscal-reports/bir-sales-summaries/{id:D}/exports/{format}");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);return(await response.Content.ReadAsByteArrayAsync(),response.Headers.GetValues("X-Content-SHA256").Single());
    }

    private static async Task SeedReportingConfigurationAsync(string cs)=>await ExecuteAsync(cs,"""
    INSERT INTO pos.site_pos_server_fiscal_identity_history(site_pos_server_fiscal_identity_history_id,site_pos_server_id,fiscal_identity_id,effective_start_at,assignment_reason_text) VALUES('86000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','2026-01-01T00:00:00Z','Synthetic summary proof scope');
    INSERT INTO pos.sales_invoice_header_profiles(sales_invoice_header_profile_id,fiscal_identity_id,site_id,site_pos_server_id,profile_version,template_version,presentation_version,pos_serial_number,machine_identification_number,parking_location_display,supplier_developer_registered_name,supplier_developer_address,supplier_developer_tin,bir_accreditation_number,bir_accreditation_issued_date,bir_accreditation_valid_until,ptu_number,ptu_issued_date,sales_invoice_legal_statement,customer_service_footer,effective_from,lifecycle_status,approved_at,approved_by_ref,created_by_ref,updated_by_ref)
    VALUES('86000000-0000-4000-8000-000000000002','73000000-0000-4000-8000-000000000002','86000000-0000-4000-8000-000000000003','73000000-0000-4000-8000-000000000001','summary-profile-v1','digital-sales-invoice-json-v1','digital-sales-invoice-presentation-json-v1','SYN-SERIAL-Z010','SYN-MIN-Z010','SYNTHETIC TEST SITE','GOVERNED TEST SOFTWARE SUPPLIER','GOVERNED TEST SOFTWARE ADDRESS','TEST-SUPPLIER-TIN-Z010','SYN-ACCREDITATION-Z010','2026-01-01','2027-01-01','SYN-PTU-Z010','2026-01-01','SYNTHETIC SALES INVOICE','SYNTHETIC SUPPORT','2026-01-01T00:00:00Z','APPROVED','2026-01-01T00:00:00Z','summary-proof','summary-proof','summary-proof');
    """);

    private static async Task AssertImmutableAsync(string cs,Guid id){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using(var update=new NpgsqlCommand("UPDATE pos.bir_sales_summary_reports SET net_sales_amount_minor_units=0 WHERE bir_sales_summary_report_id=@id",c)){update.Parameters.AddWithValue("id",id);Assert.Equal("23514",(await Assert.ThrowsAsync<PostgresException>(()=>update.ExecuteNonQueryAsync())).SqlState);}await using(var delete=new NpgsqlCommand("DELETE FROM pos.bir_sales_summary_reports WHERE bir_sales_summary_report_id=@id",c)){delete.Parameters.AddWithValue("id",id);Assert.Equal("23514",(await Assert.ThrowsAsync<PostgresException>(()=>delete.ExecuteNonQueryAsync())).SqlState);}}
    private static async Task<Manifest> ManifestAsync(string cs)=>new(await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_documents"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_document_lines"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_tenders"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_tax_details"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_discount_privilege_details"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_totals"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'"),await ScalarAsync<long>(cs,"SELECT z_counter_value FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),await ScalarAsync<long>(cs,"SELECT reset_counter_value FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),await ScalarAsync<long>(cs,"SELECT grand_total_amount_minor_units FROM pos.fiscal_z_close_states WHERE site_pos_server_id='73000000-0000-4000-8000-000000000001'"),await ScalarAsync<string>(cs,"SELECT code.code_key FROM pos.fiscal_reporting_periods p JOIN pos.controlled_codes code ON code.controlled_code_id=p.period_status_code_id WHERE p.fiscal_reporting_period_id='73000000-0000-4000-8000-000000000010'"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.x_z_reports WHERE report_kind_code_id<>'1c628bc2-49c3-53e8-ae83-2082bcf28467'"),await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.bir_sales_summary_reports"));

    private static async Task ProveAnnexE1FlowAsync(HttpClient client,string cs)
    {
        var generation=new AnnexE1GenerationRequest("annex-generate",SiteId,IdentityId,"PHP",2026,8,AnnexE1Contract.Profile,null,null,null);
        const string missingZPeriodId="87000000-0000-4000-8000-000000000001";
        await ExecuteAsync(cs,$"INSERT INTO pos.fiscal_reporting_periods(fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,expected_prior_period_id,opened_at,closing_started_at,closed_at,created_by_ref,updated_by_ref) SELECT '{missingZPeriodId}',p.fiscal_reporting_contract_version_id,p.site_pos_server_id,p.fiscal_identity_id,(SELECT c.controlled_code_id FROM pos.controlled_codes c JOIN pos.controlled_code_sets s ON s.controlled_code_set_id=c.controlled_code_set_id WHERE s.code_set_key='fiscal_reporting_period_status' AND c.code_key='closed'),'2026-08-04','2026-08-04T00:00:00Z','2026-08-05T00:00:00Z',p.reporting_timezone_name,p.business_day_cutoff_local_time,p.currency_code,p.period_sequence+1,p.fiscal_reporting_period_id,'2026-08-04T00:00:00Z','2026-08-05T00:00:00Z','2026-08-05T00:00:00Z','summary-proof','summary-proof' FROM pos.fiscal_reporting_periods p WHERE p.fiscal_reporting_period_id='{PeriodId:D}'");
        using(var missingZ=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation))
        {
            var body=await missingZ.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Conflict,missingZ.StatusCode);
            Assert.Contains("missing committed Z Reading",body,StringComparison.Ordinal);
            Assert.Contains("2026-08-04",body,StringComparison.Ordinal);
        }
        await ExecuteAsync(cs,$"ALTER TABLE pos.fiscal_reporting_periods DISABLE TRIGGER trg_fiscal_reporting_periods_closed_immutable; DELETE FROM pos.fiscal_reporting_periods WHERE fiscal_reporting_period_id='{missingZPeriodId}'; ALTER TABLE pos.fiscal_reporting_periods ENABLE TRIGGER trg_fiscal_reporting_periods_closed_immutable;");

        var zRequestId=await ScalarAsync<Guid>(cs,$"SELECT fiscal_report_request_id FROM pos.x_z_reports WHERE fiscal_reporting_period_id='{PeriodId:D}' AND report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'");
        await ExecuteAsync(cs,$"UPDATE pos.fiscal_report_requests SET report_status_code_id=(SELECT c.controlled_code_id FROM pos.controlled_codes c JOIN pos.controlled_code_sets s ON s.controlled_code_set_id=c.controlled_code_set_id WHERE s.code_set_key='fiscal_report_status' AND c.code_key='processing') WHERE fiscal_report_request_id='{zRequestId:D}'");
        using(var uncommittedZ=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation))
        {
            var body=await uncommittedZ.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Conflict,uncommittedZ.StatusCode);
            Assert.Contains("missing committed Z Reading",body,StringComparison.Ordinal);
            Assert.Contains("2026-08-03",body,StringComparison.Ordinal);
        }
        await ExecuteAsync(cs,$"UPDATE pos.fiscal_report_requests SET report_status_code_id=(SELECT c.controlled_code_id FROM pos.controlled_codes c JOIN pos.controlled_code_sets s ON s.controlled_code_set_id=c.controlled_code_set_id WHERE s.code_set_key='fiscal_report_status' AND c.code_key='committed') WHERE fiscal_report_request_id='{zRequestId:D}'");

        var birRequestId=await ScalarAsync<Guid>(cs,$"SELECT fiscal_report_request_id FROM pos.bir_sales_summary_reports WHERE fiscal_reporting_period_id='{PeriodId:D}'");
        await ExecuteAsync(cs,$"UPDATE pos.fiscal_report_requests SET report_status_code_id=(SELECT c.controlled_code_id FROM pos.controlled_codes c JOIN pos.controlled_code_sets s ON s.controlled_code_set_id=c.controlled_code_set_id WHERE s.code_set_key='fiscal_report_status' AND c.code_key='processing') WHERE fiscal_report_request_id='{birRequestId:D}'");
        using(var uncommittedBir=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation))
        {
            var body=await uncommittedBir.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Conflict,uncommittedBir.StatusCode);
            Assert.Contains("missing committed BIR Sales Summary",body,StringComparison.Ordinal);
            Assert.Contains("2026-08-03",body,StringComparison.Ordinal);
        }
        await ExecuteAsync(cs,$"UPDATE pos.fiscal_report_requests SET report_status_code_id=(SELECT c.controlled_code_id FROM pos.controlled_codes c JOIN pos.controlled_code_sets s ON s.controlled_code_set_id=c.controlled_code_set_id WHERE s.code_set_key='fiscal_report_status' AND c.code_key='committed') WHERE fiscal_report_request_id='{birRequestId:D}'");
        using(var missingSource=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation with{OperationKey="annex-no-source",CalendarMonth=7}))Assert.Equal(HttpStatusCode.UnprocessableEntity,missingSource.StatusCode);
        var unsupportedPrivilege=new AnnexE1FactRequest("annex-nonzero-privilege",SiteId,IdentityId,"PHP",PeriodId,AnnexE1FactTypes.NaacDiscount,AnnexE1FactStatuses.Recorded,1,1,"SYN-PRIV-001","SYN-PRIV-001",null,"SYNTHETIC-UNSUPPORTED",null,null);using(var unsupported=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/accounting-facts",unsupportedPrivilege))Assert.Equal(HttpStatusCode.UnprocessableEntity,unsupported.StatusCode);
        var competing=await Task.WhenAll(client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation),client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation));
        var competingBodies=await Task.WhenAll(competing.Select(response=>response.Content.ReadAsStringAsync()));var competingDiagnostics=string.Join(" | ",competing.Select((response,index)=>$"{(int)response.StatusCode}:{competingBodies[index]}"));Assert.True(competing.Count(response=>response.StatusCode==HttpStatusCode.Created)==1,competingDiagnostics);Assert.True(competing.Count(response=>response.StatusCode==HttpStatusCode.OK)==1,competingDiagnostics);var createdIndex=Array.FindIndex(competing,response=>response.StatusCode==HttpStatusCode.Created);var created=System.Text.Json.JsonSerializer.Deserialize<AnnexE1ApiResponse>(competingBodies[createdIndex],new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!.Workbook!;Assert.Single(created.Rows);Assert.Equal(32,created.Rows[0].Positions.Count);
        using var metadata=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}");Assert.Equal(HttpStatusCode.OK,metadata.StatusCode);Assert.Equal(created.ArtifactSha256,(await metadata.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Workbook!.ArtifactSha256);
        var currentUrl=$"/v1/fiscal-reports/annex-e/e1/workbooks/current?sitePosServerId={SiteId:D}&fiscalIdentityId={IdentityId:D}&currencyCode=PHP&calendarYear=2026&calendarMonth=8&profile={Uri.EscapeDataString(AnnexE1Contract.Profile)}";
        using(var current=await client.GetAsync(currentUrl)){Assert.Equal(HttpStatusCode.OK,current.StatusCode);Assert.Equal(created.WorkbookId,(await current.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Workbook!.WorkbookId);}
        using(var missingCurrent=await client.GetAsync(currentUrl.Replace("calendarMonth=8","calendarMonth=7",StringComparison.Ordinal)))Assert.Equal(HttpStatusCode.NotFound,missingCurrent.StatusCode);
        using(var wrongProfile=await client.GetAsync(currentUrl.Replace(Uri.EscapeDataString(AnnexE1Contract.Profile),"unknown-profile",StringComparison.Ordinal)))Assert.Equal(HttpStatusCode.NotFound,wrongProfile.StatusCode);
        using var firstDownload=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}/content");Assert.Equal(HttpStatusCode.OK,firstDownload.StatusCode);Assert.Equal(AnnexE1Contract.MimeType,firstDownload.Content.Headers.ContentType!.MediaType);Assert.Contains(created.FileName,firstDownload.Content.Headers.ContentDisposition!.FileNameStar??firstDownload.Content.Headers.ContentDisposition.FileName);Assert.True(firstDownload.Headers.CacheControl!.Private);Assert.True(firstDownload.Headers.CacheControl.NoStore);Assert.Equal("nosniff",firstDownload.Headers.GetValues("X-Content-Type-Options").Single());var firstBytes=await firstDownload.Content.ReadAsByteArrayAsync();Assert.Equal(created.ArtifactByteLength,firstBytes.LongLength);Assert.Equal(created.ArtifactSha256,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(firstBytes)).ToLowerInvariant());
        using var replayResponse=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation);Assert.Equal(HttpStatusCode.OK,replayResponse.StatusCode);var replay=(await replayResponse.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Workbook!;Assert.Equal(created.WorkbookId,replay.WorkbookId);
        using var secondDownload=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}/content");Assert.Equal(firstBytes,await secondDownload.Content.ReadAsByteArrayAsync());

        var correctionFactRequest=new AnnexE1FactRequest("annex-fact-manual-correction",SiteId,IdentityId,"PHP",PeriodId,AnnexE1FactTypes.ManualSiOrNetIncome,AnnexE1FactStatuses.Recorded,100,1,"SYN-MANUAL-001","SYN-MANUAL-001",null,"SYNTHETIC-ACCOUNTING-CORRECTION",null,null);
        using var correctedFactResponse=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/accounting-facts",correctionFactRequest);Assert.Equal(HttpStatusCode.Created,correctedFactResponse.StatusCode);var correctedFact=(await correctedFactResponse.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Fact!;
        using var changedSemantic=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation);Assert.Equal(HttpStatusCode.Conflict,changedSemantic.StatusCode);
        var correctionGeneration=generation with{OperationKey="annex-generate-correction",SupersedesWorkbookId=created.WorkbookId,CorrectionReason="authorized_restatement",CorrectionApprovalReference="SYNTHETIC-ANNEX-CORRECTION"};
        using var correctionResponse=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",correctionGeneration);var correctionBody=await correctionResponse.Content.ReadAsStringAsync();Assert.True(correctionResponse.StatusCode==HttpStatusCode.Created,$"{correctionResponse.StatusCode}: {correctionBody}");var corrected=System.Text.Json.JsonSerializer.Deserialize<AnnexE1ApiResponse>(correctionBody,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!.Workbook!;Assert.Equal(created.WorkbookId,corrected.SupersedesWorkbookId);Assert.Equal(2,corrected.Revision);Assert.Equal(100,corrected.Rows[0].Positions.Single(position=>position.Position=="D06").MinorUnitsValue);Assert.Equal(100,corrected.Rows[0].Positions.Single(position=>position.Position=="D29").MinorUnitsValue-created.Rows[0].Positions.Single(position=>position.Position=="D29").MinorUnitsValue);
        var workbookCountBeforeCurrentRead=await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.annex_e1_workbooks");
        using(var current=await client.GetAsync(currentUrl)){Assert.Equal(HttpStatusCode.OK,current.StatusCode);var workbook=(await current.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Workbook!;Assert.Equal(corrected.WorkbookId,workbook.WorkbookId);Assert.Equal(2,workbook.Revision);}
        Assert.Equal(workbookCountBeforeCurrentRead,await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.annex_e1_workbooks"));
        using var correctedDownload=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{corrected.WorkbookId:D}/content");var correctedBytes=await correctedDownload.Content.ReadAsByteArrayAsync();Assert.NotEqual(firstBytes,correctedBytes);
        using var originalAfterCorrection=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}/content");Assert.Equal(firstBytes,await originalAfterCorrection.Content.ReadAsByteArrayAsync());
        using var correctionReplay=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",correctionGeneration);Assert.Equal(HttpStatusCode.OK,correctionReplay.StatusCode);Assert.Equal(corrected.WorkbookId,(await correctionReplay.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Workbook!.WorkbookId);

        var supersedingFactRequest=correctionFactRequest with{OperationKey="annex-fact-manual-correction-r2",AmountMinorUnits=200,FirstSourceReference="SYN-MANUAL-002",LastSourceReference="SYN-MANUAL-002",ApprovalReference="SYNTHETIC-ACCOUNTING-CORRECTION-R2",SupersedesFactId=correctedFact.FactId,CorrectionReason="source_correction"};
        using var supersedingFactResponse=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/accounting-facts",supersedingFactRequest);Assert.Equal(HttpStatusCode.Created,supersedingFactResponse.StatusCode);
        using var changedAgain=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation with{OperationKey="annex-generate-changed-r3"});Assert.Equal(HttpStatusCode.Conflict,changedAgain.StatusCode);
        var secondCorrectionGeneration=generation with{OperationKey="annex-generate-correction-r3",SupersedesWorkbookId=corrected.WorkbookId,CorrectionReason="source_correction",CorrectionApprovalReference="SYNTHETIC-ANNEX-CORRECTION-R3"};
        using var secondCorrectionResponse=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",secondCorrectionGeneration);Assert.Equal(HttpStatusCode.Created,secondCorrectionResponse.StatusCode);var correctedAgain=(await secondCorrectionResponse.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Workbook!;Assert.Equal(corrected.WorkbookId,correctedAgain.SupersedesWorkbookId);Assert.Equal(3,correctedAgain.Revision);Assert.Equal(200,correctedAgain.Rows[0].Positions.Single(position=>position.Position=="D06").MinorUnitsValue);
        using var originalAfterSecondCorrection=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}/content");Assert.Equal(firstBytes,await originalAfterSecondCorrection.Content.ReadAsByteArrayAsync());
        using var firstCorrectionAfterSecondCorrection=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{corrected.WorkbookId:D}/content");Assert.Equal(correctedBytes,await firstCorrectionAfterSecondCorrection.Content.ReadAsByteArrayAsync());

        const string automaticPeriodId="88000000-0000-4000-8000-000000000001";
        await ExecuteAsync(cs,$"INSERT INTO pos.fiscal_reporting_periods(fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,expected_prior_period_id,opened_at,created_by_ref,updated_by_ref) VALUES('{automaticPeriodId}','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','{SiteId:D}','{IdentityId:D}','1a6f7021-bc84-5c01-afaa-c5d6685633c8','2026-08-04','2026-08-04T00:00:00Z','2026-08-05T00:00:00Z','Asia/Manila','00:00:00','PHP',2,'{PeriodId:D}','2026-08-04T00:00:00Z','summary-proof','summary-proof')");
        var automaticClose=await CloseZAsync(client,Guid.Parse(automaticPeriodId),"z-annex-automatic-successor",2);Assert.Equal("COMMITTED",automaticClose.BirSalesSummaryCompletion?.Status);
        var sourceBeforeAutomaticGeneration=await ManifestAsync(cs);
        var automaticGenerationA=generation with{OperationKey="annex-generate-automatic-a"};var automaticGenerationB=generation with{OperationKey="annex-generate-automatic-b"};
        var automaticResponses=await Task.WhenAll(client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",automaticGenerationA),client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",automaticGenerationB));var automaticBodies=await Task.WhenAll(automaticResponses.Select(response=>response.Content.ReadAsStringAsync()));Assert.Equal(1,automaticResponses.Count(response=>response.StatusCode==HttpStatusCode.Created));Assert.Equal(1,automaticResponses.Count(response=>response.StatusCode==HttpStatusCode.OK));var automaticCreatedIndex=Array.FindIndex(automaticResponses,response=>response.StatusCode==HttpStatusCode.Created);var automatic=(System.Text.Json.JsonSerializer.Deserialize<AnnexE1ApiResponse>(automaticBodies[automaticCreatedIndex],new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)))!.Workbook!;Assert.Equal(4,automatic.Revision);Assert.Equal(correctedAgain.WorkbookId,automatic.SupersedesWorkbookId);Assert.Null(automatic.CorrectionReason);Assert.Null(automatic.CorrectionApprovalReference);Assert.Equal(2,automatic.Rows.Count);Assert.All(automaticResponses.Select((response,index)=>System.Text.Json.JsonSerializer.Deserialize<AnnexE1ApiResponse>(automaticBodies[index],new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!.Workbook!.WorkbookId),workbookId=>Assert.Equal(automatic.WorkbookId,workbookId));
        using var automaticReplay=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",automaticGenerationA);Assert.Equal(HttpStatusCode.OK,automaticReplay.StatusCode);Assert.Equal(automatic.WorkbookId,(await automaticReplay.Content.ReadFromJsonAsync<AnnexE1ApiResponse>())!.Workbook!.WorkbookId);
        Assert.Equal(sourceBeforeAutomaticGeneration,await ManifestAsync(cs));
        Assert.Equal(firstBytes,await (await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}/content")).Content.ReadAsByteArrayAsync());
        Assert.Equal(correctedBytes,await (await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{corrected.WorkbookId:D}/content")).Content.ReadAsByteArrayAsync());

        var storageKey=await ScalarAsync<string>(cs,$"SELECT artifact_storage_key FROM pos.annex_e1_workbooks WHERE annex_e1_workbook_id='{corrected.WorkbookId:D}'");var artifactPath=Path.GetFullPath(Path.Combine(ArtifactRoot,storageKey.Replace('/',Path.DirectorySeparatorChar)));Assert.StartsWith(Path.GetFullPath(ArtifactRoot)+Path.DirectorySeparatorChar,artifactPath,StringComparison.OrdinalIgnoreCase);var durableBytes=await File.ReadAllBytesAsync(artifactPath);await File.WriteAllBytesAsync(artifactPath,[1,2,3]);using(var tampered=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{corrected.WorkbookId:D}/content"))Assert.Equal(HttpStatusCode.NotFound,tampered.StatusCode);await File.WriteAllBytesAsync(artifactPath,durableBytes);File.Move(artifactPath,artifactPath+".missing");try{using var missing=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{corrected.WorkbookId:D}/content");Assert.Equal(HttpStatusCode.NotFound,missing.StatusCode);}finally{File.Move(artifactPath+".missing",artifactPath);}

        Assert.Equal(4L,await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.annex_e1_workbooks"));Assert.Equal(5L,await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.annex_e_reports"));Assert.Equal(3L,await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.annex_e1_report_fact_sources"));Assert.Equal(2L,await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.annex_e1_period_accounting_facts"));Assert.True(await ScalarAsync<long>(cs,"SELECT count(*) FROM pos.fiscal_action_audit a JOIN pos.controlled_codes c ON c.controlled_code_id=a.audit_action_type_code_id WHERE c.code_key='annex_e'")>=5);
        await using var connection=new NpgsqlConnection(cs);await connection.OpenAsync();await using var mutation=new NpgsqlCommand("UPDATE pos.annex_e1_workbooks SET artifact_byte_length=1 WHERE annex_e1_workbook_id=@id",connection);mutation.Parameters.AddWithValue("id",created.WorkbookId);Assert.Equal("23514",(await Assert.ThrowsAsync<PostgresException>(()=>mutation.ExecuteNonQueryAsync())).SqlState);

        Authorize(client,"annex-readonly-key");using(var readAllowed=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}"))Assert.Equal(HttpStatusCode.OK,readAllowed.StatusCode);using(var currentAllowed=await client.GetAsync(currentUrl))Assert.Equal(HttpStatusCode.OK,currentAllowed.StatusCode);using(var downloadDenied=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}/content"))Assert.Equal(HttpStatusCode.Forbidden,downloadDenied.StatusCode);using(var generateDenied=await client.PostAsJsonAsync("/v1/fiscal-reports/annex-e/e1/workbooks",generation))Assert.Equal(HttpStatusCode.Forbidden,generateDenied.StatusCode);
        Authorize(client,"annex-wrong-scope-key");using(var wrongScope=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}"))Assert.Equal(HttpStatusCode.NotFound,wrongScope.StatusCode);using(var wrongCurrentScope=await client.GetAsync(currentUrl))Assert.Equal(HttpStatusCode.NotFound,wrongCurrentScope.StatusCode);
        Authorize(client,"annex-fixture-key");using(var fixture=await client.GetAsync($"/v1/fiscal-reports/annex-e/e1/workbooks/{created.WorkbookId:D}"))Assert.Equal(HttpStatusCode.Forbidden,fixture.StatusCode);
        Authorize(client,"summary-production-key");
    }

    private static async Task RebuildAsync(string cs){await ExecuteAsync(cs,"DROP SCHEMA IF EXISTS pos CASCADE");var root=FindRepositoryRoot();foreach(var entry in File.ReadLines(Path.Combine(root,"db","rebuild","pos_sql_apply_order.txt")).Select(x=>x.Trim()).Where(x=>x.Length>0&&!x.StartsWith('#')))await ExecuteFileAsync(cs,Path.Combine(root,entry.Replace('/',Path.DirectorySeparatorChar)));foreach(var file in Directory.GetFiles(Path.Combine(root,"db","reference-data","controlled-codes","generated","sql"),"*.sql").OrderBy(x=>x,StringComparer.Ordinal))await ExecuteFileAsync(cs,file);}
    private static async Task<WebApplication> StartApiAsync(string cs,bool automaticBirSummary=true){var builder=WebApplication.CreateBuilder(new WebApplicationOptions{EnvironmentName=Environments.Production});builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");builder.Configuration.AddInMemoryCollection(Configuration(cs));builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);if(!automaticBirSummary)builder.Services.AddScoped(provider=>new FiscalZReadingService(provider.GetRequiredService<IFiscalZReadingRepository>()));var app=builder.Build();app.UseAuthentication();app.UseAuthorization();app.MapFiscalZCloseStateInitializationEndpoints();app.MapFiscalZReadingEndpoints();app.MapBirSalesSummaryEndpoints();app.MapAnnexE1Endpoints();app.MapMonthlyESalesEndpoints();await app.StartAsync();return app;}
    private static Dictionary<string,string?> Configuration(string cs)=>new()
    {
        {"ConnectionStrings:PosServer",cs},
        {"PosServer:AnnexE1:ArtifactRoot",ArtifactRoot},
        {"PosServer:Admin:ApiKeys:0:Principal","summary-production"},
        {"PosServer:Admin:ApiKeys:0:Key","summary-production-key"},
        {"PosServer:Admin:ApiKeys:0:AuthorityClass","PRODUCTION"},
        {"PosServer:Admin:ApiKeys:0:Permissions:0",FiscalZCloseStateInitializationAuthorization.Permission},
        {"PosServer:Admin:ApiKeys:0:Permissions:1",FiscalZReadingAuthorization.ClosePermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:2",FiscalZReadingAuthorization.ReadPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:3",BirSalesSummaryAuthorization.GeneratePermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:4",BirSalesSummaryAuthorization.ReadPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:5",BirSalesSummaryAuthorization.ExportPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:6",AnnexE1Authorization.RecordFactPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:7",AnnexE1Authorization.AttestZeroPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:8",AnnexE1Authorization.GeneratePermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:9",AnnexE1Authorization.ReadPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:10",AnnexE1Authorization.DownloadPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:11",AnnexE1Authorization.CorrectPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:12",MonthlyESalesAuthorization.ReadPermission},
        {"PosServer:Admin:ApiKeys:0:Permissions:13",MonthlyESalesAuthorization.ExportPermission},
        {"PosServer:Admin:ApiKeys:0:SitePosServerIds:0",SiteId.ToString("D")},
        {"PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0",IdentityId.ToString("D")},
        {"PosServer:Admin:ApiKeys:0:CurrencyCodes:0","PHP"},
        {"PosServer:Admin:ApiKeys:1:Principal","summary-readonly"},
        {"PosServer:Admin:ApiKeys:1:Key","summary-readonly-key"},
        {"PosServer:Admin:ApiKeys:1:AuthorityClass","PRODUCTION"},
        {"PosServer:Admin:ApiKeys:1:Permissions:0",BirSalesSummaryAuthorization.ReadPermission},
        {"PosServer:Admin:ApiKeys:1:SitePosServerIds:0",SiteId.ToString("D")},
        {"PosServer:Admin:ApiKeys:1:FiscalIdentityIds:0",IdentityId.ToString("D")},
        {"PosServer:Admin:ApiKeys:1:CurrencyCodes:0","PHP"},
        {"PosServer:Admin:ApiKeys:2:Principal","summary-wrong-scope"},
        {"PosServer:Admin:ApiKeys:2:Key","summary-wrong-scope-key"},
        {"PosServer:Admin:ApiKeys:2:AuthorityClass","PRODUCTION"},
        {"PosServer:Admin:ApiKeys:2:Permissions:0",BirSalesSummaryAuthorization.ReadPermission},
        {"PosServer:Admin:ApiKeys:2:SitePosServerIds:0",Guid.NewGuid().ToString("D")},
        {"PosServer:Admin:ApiKeys:2:FiscalIdentityIds:0",IdentityId.ToString("D")},
        {"PosServer:Admin:ApiKeys:2:CurrencyCodes:0","PHP"},
        {"PosServer:Admin:ApiKeys:3:Principal","summary-fixture"},
        {"PosServer:Admin:ApiKeys:3:Key","summary-fixture-key"},
        {"PosServer:Admin:ApiKeys:3:AuthorityClass","FIXTURE"},
        {"PosServer:Admin:ApiKeys:3:Permissions:0",BirSalesSummaryAuthorization.ReadPermission},
        {"PosServer:Admin:ApiKeys:3:SitePosServerIds:0",SiteId.ToString("D")},
        {"PosServer:Admin:ApiKeys:3:FiscalIdentityIds:0",IdentityId.ToString("D")},
        {"PosServer:Admin:ApiKeys:3:CurrencyCodes:0","PHP"},
        {"PosServer:Admin:ApiKeys:4:Principal","annex-readonly"},{"PosServer:Admin:ApiKeys:4:Key","annex-readonly-key"},{"PosServer:Admin:ApiKeys:4:AuthorityClass","PRODUCTION"},{"PosServer:Admin:ApiKeys:4:Permissions:0",AnnexE1Authorization.ReadPermission},{"PosServer:Admin:ApiKeys:4:SitePosServerIds:0",SiteId.ToString("D")},{"PosServer:Admin:ApiKeys:4:FiscalIdentityIds:0",IdentityId.ToString("D")},{"PosServer:Admin:ApiKeys:4:CurrencyCodes:0","PHP"},
        {"PosServer:Admin:ApiKeys:5:Principal","annex-wrong-scope"},{"PosServer:Admin:ApiKeys:5:Key","annex-wrong-scope-key"},{"PosServer:Admin:ApiKeys:5:AuthorityClass","PRODUCTION"},{"PosServer:Admin:ApiKeys:5:Permissions:0",AnnexE1Authorization.ReadPermission},{"PosServer:Admin:ApiKeys:5:SitePosServerIds:0",Guid.NewGuid().ToString("D")},{"PosServer:Admin:ApiKeys:5:FiscalIdentityIds:0",IdentityId.ToString("D")},{"PosServer:Admin:ApiKeys:5:CurrencyCodes:0","PHP"},
        {"PosServer:Admin:ApiKeys:6:Principal","annex-fixture"},{"PosServer:Admin:ApiKeys:6:Key","annex-fixture-key"},{"PosServer:Admin:ApiKeys:6:AuthorityClass","FIXTURE"},{"PosServer:Admin:ApiKeys:6:Permissions:0",AnnexE1Authorization.ReadPermission},{"PosServer:Admin:ApiKeys:6:SitePosServerIds:0",SiteId.ToString("D")},{"PosServer:Admin:ApiKeys:6:FiscalIdentityIds:0",IdentityId.ToString("D")},{"PosServer:Admin:ApiKeys:6:CurrencyCodes:0","PHP"}
    };
    private static HttpClient CreateClient(WebApplication app){var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();return new(){BaseAddress=new(address)};}
    private static void Authorize(HttpClient client,string key){client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName);client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.PermissionHeaderName);client.DefaultRequestHeaders.Remove(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName);client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName,key);client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName,"summary-proof-correlation");}
    private static async Task ExecuteFileAsync(string cs,string path)=>await ExecuteAsync(cs,await File.ReadAllTextAsync(path));private static async Task ExecuteAsync(string cs,string sql){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using var cmd=new NpgsqlCommand(sql,c){CommandTimeout=120};await cmd.ExecuteNonQueryAsync();}private static async Task<T> ScalarAsync<T>(string cs,string sql){await using var c=new NpgsqlConnection(cs);await c.OpenAsync();await using var cmd=new NpgsqlCommand(sql,c);return(T)(await cmd.ExecuteScalarAsync())!;}private static string FindRepositoryRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null){if(File.Exists(Path.Combine(d.FullName,"ExitPass.PosServer.sln")))return d.FullName;d=d.Parent;}throw new DirectoryNotFoundException();}
    private sealed record Manifest(long Documents,long Lines,long Tenders,long Taxes,long Discounts,long Totals,long ZReports,long ZCounter,long ResetCounter,long Gta,string PeriodStatus,long OtherReports,long Summaries);
    private static string ArtifactRoot=>Path.Combine(Path.GetTempPath(),"exitpass-z012b-annex-e1-proof");
}
