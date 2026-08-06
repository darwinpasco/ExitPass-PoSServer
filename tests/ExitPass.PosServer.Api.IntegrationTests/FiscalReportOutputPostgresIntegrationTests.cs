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

public sealed class FiscalReportOutputPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_REPORT_OUTPUT_TEST_DB_URL";
    private static readonly Guid SiteId = Guid.Parse("73000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("73000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("73000000-0000-4000-8000-000000000010");
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse("2026-08-03T12:00:00Z");

    [Fact]
    public async Task ActualXAndZOutputsAreDeterministicScopedPrivateAndReadOnlyAcrossRestart()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await RebuildAsync(connectionString);
        await ExecuteFileAsync(connectionString, Path.Combine(FindRepositoryRoot(), "tests", "ExitPass.PosServer.Api.IntegrationTests", "Fixtures", "fiscal_x_reading_runtime_fixture.sql"));
        await ExecuteAsync(connectionString, "INSERT INTO pos.site_pos_server_fiscal_identity_history(site_pos_server_fiscal_identity_history_id,site_pos_server_id,fiscal_identity_id,effective_start_at,assignment_reason_text) VALUES('73000000-0000-4000-8000-000000000090','73000000-0000-4000-8000-000000000001','73000000-0000-4000-8000-000000000002','2026-01-01T00:00:00Z','Synthetic Z-008 output scope')");

        await using var app = await StartApiAsync(connectionString);
        using var client = Client(app, "synthetic-output-key");

        var xResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/x-readings/", new GenerateFiscalXReadingRequest("z008-x-operation", SiteId, IdentityId, ObservedAt));
        var xCreatedText = await xResponse.Content.ReadAsStringAsync();
        Assert.True(xResponse.IsSuccessStatusCode, xCreatedText);
        var xCreated = await xResponse.Content.ReadFromJsonAsync<FiscalXReadingApiResponse>();
        var xReference = Assert.IsType<string>(xCreated?.XReading?.FiscalReportReference);

        var xBeforeClose = await ReadAllAsync(client, "x-readings", xReference);
        Assert.Contains("\"title\":\"X READING\"", xBeforeClose.Presentation.Text, StringComparison.Ordinal);
        Assert.Contains("INTERIM_READ_ONLY", xBeforeClose.Presentation.Text, StringComparison.Ordinal);
        Assert.Contains("X READING", xBeforeClose.Text.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Z READING", xBeforeClose.Text.Text, StringComparison.Ordinal);
        AssertOutputHeaders(xBeforeClose);

        var initialized = await client.PostAsJsonAsync("/v1/admin/fiscal-z-close-states/initialize",
            new InitializeFiscalZCloseStateRequest("z008-state-initialize", SiteId, IdentityId, "PHP", "approved_new_scope_zero", 0, 0, 0, "approved-z008-proof"));
        Assert.True(initialized.IsSuccessStatusCode, await initialized.Content.ReadAsStringAsync());
        var zResponse = await client.PostAsJsonAsync("/v1/fiscal-reports/z-readings/",
            new CloseFiscalZReadingRequest("z008-z-operation", SiteId, IdentityId, "PHP", PeriodId, 1));
        var zCreatedText = await zResponse.Content.ReadAsStringAsync();
        Assert.True(zResponse.IsSuccessStatusCode, zCreatedText);
        var zCreated = await zResponse.Content.ReadFromJsonAsync<FiscalZReadingApiResponse>();
        var zReference = Assert.IsType<string>(zCreated?.ZReading?.FiscalReportReference);
        Assert.Equal(16_571, zCreated?.ZReading?.Amounts.NetSalesAmountMinorUnits);

        var baseline = await ManifestAsync(connectionString);
        var xAfterClose = await ReadAllAsync(client, "x-readings", xReference);
        var zOutputs = await ReadAllAsync(client, "z-readings", zReference);
        AssertEquivalent(xBeforeClose, xAfterClose);
        Assert.Contains("\"title\":\"Z READING\"", zOutputs.Presentation.Text, StringComparison.Ordinal);
        Assert.Contains("IMMUTABLE_CLOSED", zOutputs.Presentation.Text, StringComparison.Ordinal);
        Assert.Contains("\"resultingZCounterValue\":1", zOutputs.Presentation.Text, StringComparison.Ordinal);
        Assert.Contains("\"resultingResetCounterValue\":0", zOutputs.Presentation.Text, StringComparison.Ordinal);
        Assert.Contains("\"resultingGrandTotalAmountMinorUnits\":16571", zOutputs.Presentation.Text, StringComparison.Ordinal);
        Assert.Contains("Z READING", zOutputs.Text.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("X READING", zOutputs.Text.Text, StringComparison.Ordinal);
        AssertOutputHeaders(zOutputs);

        using (var replay = await client.GetAsync($"/v1/fiscal-reports/z-readings/{zReference}/exports/json"))
            Assert.Equal(zOutputs.Json.Bytes, await replay.Content.ReadAsByteArrayAsync());

        using var wrongScopeClient = Client(app, "synthetic-wrong-scope-key");
        using var hidden = await wrongScopeClient.GetAsync($"/v1/fiscal-reports/z-readings/{zReference}/exports/json");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var readOnlyClient = Client(app, "synthetic-read-only-key");
        using var forbidden = await readOnlyClient.GetAsync($"/v1/fiscal-reports/x-readings/{xReference}/exports/json");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var unsupported = await client.GetAsync($"/v1/fiscal-reports/x-readings/{xReference}/exports/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, unsupported.StatusCode);

        await using (var restarted = await StartApiAsync(connectionString))
        using (var restartedClient = Client(restarted, "synthetic-output-key"))
        {
            AssertEquivalent(xAfterClose, await ReadAllAsync(restartedClient, "x-readings", xReference));
            AssertEquivalent(zOutputs, await ReadAllAsync(restartedClient, "z-readings", zReference));
        }

        Assert.Equal(baseline, await ManifestAsync(connectionString));
        foreach (var text in xAfterClose.AllText.Concat(zOutputs.AllText))
        {
            Assert.DoesNotContain("beneficiary", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("evidence", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("semanticRequestHash", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("connectionString", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<OutputSet> ReadAllAsync(HttpClient client, string family, string reference)
    {
        var root = $"/v1/fiscal-reports/{family}/{reference}";
        return new(
            await CaptureAsync(client, $"{root}/presentation"),
            await CaptureAsync(client, $"{root}/exports/json"),
            await CaptureAsync(client, $"{root}/exports/csv"),
            await CaptureAsync(client, $"{root}/exports/text?width=narrow"),
            await CaptureAsync(client, $"{root}/exports/text?width=standard"),
            await CaptureAsync(client, $"{root}/exports/text?width=office"));
    }

    private static async Task<Artifact> CaptureAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"path={path}; status={(int)response.StatusCode}; body={System.Text.Encoding.UTF8.GetString(bytes)}");
        return new(bytes, response.Content.Headers.ContentType?.ToString(), response.Content.Headers.ContentDisposition?.ToString(),
            response.Headers.ETag?.ToString(), response.Headers.CacheControl?.ToString(), response.Headers.TryGetValues("X-Content-Type-Options", out var values) ? values.Single() : null);
    }

    private static void AssertOutputHeaders(OutputSet outputs)
    {
        Assert.Equal("application/json; charset=utf-8", outputs.Presentation.ContentType);
        Assert.Equal("application/json; charset=utf-8", outputs.Json.ContentType);
        Assert.Equal("text/csv; charset=utf-8", outputs.Csv.ContentType);
        Assert.Equal("text/plain; charset=utf-8", outputs.Text.ContentType);
        Assert.Contains("attachment", outputs.Json.ContentDisposition, StringComparison.Ordinal);
        Assert.Contains("attachment", outputs.Csv.ContentDisposition, StringComparison.Ordinal);
        foreach (var artifact in outputs.All)
        {
            Assert.Contains("private", artifact.CacheControl, StringComparison.Ordinal);
            Assert.Contains("no-store", artifact.CacheControl, StringComparison.Ordinal);
            Assert.Equal("nosniff", artifact.NoSniff);
            Assert.False(string.IsNullOrWhiteSpace(artifact.ETag));
        }
    }

    private static void AssertEquivalent(OutputSet expected, OutputSet actual)
    {
        Assert.Equal(expected.All.Count, actual.All.Count);
        for (var i = 0; i < expected.All.Count; i++)
        {
            Assert.Equal(expected.All[i].Bytes, actual.All[i].Bytes);
            Assert.Equal(expected.All[i].ContentType, actual.All[i].ContentType);
            Assert.Equal(expected.All[i].ContentDisposition, actual.All[i].ContentDisposition);
            Assert.Equal(expected.All[i].ETag, actual.All[i].ETag);
        }
    }

    private static async Task<string> ManifestAsync(string connectionString)
    {
        const string sql = """
        SELECT concat_ws('|',
          (SELECT count(*) FROM pos.fiscal_documents),
          (SELECT count(*) FROM pos.fiscal_document_lines),
          (SELECT count(*) FROM pos.fiscal_tenders),
          (SELECT count(*) FROM pos.fiscal_tax_details),
          (SELECT count(*) FROM pos.fiscal_discount_privilege_details),
          (SELECT count(*) FROM pos.fiscal_totals),
          (SELECT count(*) FROM pos.fiscal_document_status_history),
          (SELECT count(*) FROM pos.fiscal_sequence_states),
          (SELECT count(*) FROM pos.fiscal_counter_states),
          (SELECT count(*) FROM pos.fiscal_state_snapshots),
          (SELECT string_agg(fiscal_reporting_period_id::text || ':' || period_status_code_id::text || ':' || coalesce(closed_at::text,''),',' ORDER BY fiscal_reporting_period_id) FROM pos.fiscal_reporting_periods),
          (SELECT count(*) FROM pos.x_z_reports),
          (SELECT count(*) FROM pos.fiscal_report_tender_breakdowns),
          (SELECT count(*) FROM pos.fiscal_report_discount_breakdowns),
          (SELECT count(*) FROM pos.fiscal_report_fiscal_number_ranges),
          (SELECT count(*) FROM pos.fiscal_report_sequence_gaps),
          (SELECT count(*) FROM pos.fiscal_z_counter_snapshots),
          (SELECT count(*) FROM pos.fiscal_z_close_state_transitions),
          (SELECT string_agg(state_version::text || ':' || z_counter_value::text || ':' || reset_counter_value::text || ':' || grand_total_amount_minor_units::text,',' ORDER BY fiscal_z_close_state_id) FROM pos.fiscal_z_close_states),
          (SELECT count(*) FROM pos.reprint_requests),
          (SELECT count(*) FROM pos.fiscal_action_audit));
        """;
        return await ScalarAsync<string>(connectionString, sql);
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

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(Configuration(connectionString));
        builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapFiscalXReadingEndpoints();
        app.MapFiscalZCloseStateInitializationEndpoints();
        app.MapFiscalZReadingEndpoints();
        app.MapFiscalReportOutputEndpoints();
        await app.StartAsync();
        return app;
    }

    private static Dictionary<string, string?> Configuration(string connectionString)
    {
        var values = new Dictionary<string, string?> { ["ConnectionStrings:PosServer"] = connectionString };
        AddKey(values, 0, "output", "synthetic-output-key", SiteId, IdentityId,
            FiscalXReadingAuthorization.GeneratePermission, FiscalXReadingAuthorization.ReadPermission,
            FiscalReportOutputAuthorization.XExportPermission, FiscalZCloseStateInitializationAuthorization.Permission,
            FiscalZReadingAuthorization.ClosePermission, FiscalZReadingAuthorization.ReadPermission,
            FiscalReportOutputAuthorization.ZExportPermission);
        AddKey(values, 1, "wrong-scope", "synthetic-wrong-scope-key", Guid.NewGuid(), Guid.NewGuid(),
            FiscalXReadingAuthorization.ReadPermission, FiscalReportOutputAuthorization.XExportPermission,
            FiscalZReadingAuthorization.ReadPermission, FiscalReportOutputAuthorization.ZExportPermission);
        AddKey(values, 2, "read-only", "synthetic-read-only-key", SiteId, IdentityId,
            FiscalXReadingAuthorization.ReadPermission, FiscalZReadingAuthorization.ReadPermission);
        return values;
    }

    private static void AddKey(Dictionary<string, string?> values, int index, string principal, string key, Guid site, Guid identity, params string[] permissions)
    {
        var root = $"PosServer:Admin:ApiKeys:{index}";
        values[$"{root}:Principal"] = principal;
        values[$"{root}:Key"] = key;
        values[$"{root}:SitePosServerIds:0"] = site.ToString("D");
        values[$"{root}:FiscalIdentityIds:0"] = identity.ToString("D");
        values[$"{root}:CurrencyCodes:0"] = "PHP";
        for (var i = 0; i < permissions.Length; i++) values[$"{root}:Permissions:{i}"] = permissions[i];
    }

    private static HttpClient Client(WebApplication app, string key)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var client = new HttpClient { BaseAddress = new(address) };
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, key);
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName, "z008-proof-correlation");
        return client;
    }

    private static async Task ExecuteFileAsync(string cs, string path) => await ExecuteAsync(cs, await File.ReadAllTextAsync(path));
    private static async Task ExecuteAsync(string cs, string sql) { await using var c = new NpgsqlConnection(cs); await c.OpenAsync(); await using var cmd = new NpgsqlCommand(sql, c) { CommandTimeout = 120 }; await cmd.ExecuteNonQueryAsync(); }
    private static async Task<T> ScalarAsync<T>(string cs, string sql) { await using var c = new NpgsqlConnection(cs); await c.OpenAsync(); await using var cmd = new NpgsqlCommand(sql, c); return (T)(await cmd.ExecuteScalarAsync())!; }
    private static string FindRepositoryRoot() { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d is not null) { if (File.Exists(Path.Combine(d.FullName, "ExitPass.PosServer.sln"))) return d.FullName; d = d.Parent; } throw new DirectoryNotFoundException(); }

    private sealed record Artifact(byte[] Bytes, string? ContentType, string? ContentDisposition, string? ETag, string? CacheControl, string? NoSniff)
    {
        public string Text => System.Text.Encoding.UTF8.GetString(Bytes);
    }

    private sealed record OutputSet(Artifact Presentation, Artifact Json, Artifact Csv, Artifact Narrow, Artifact Text, Artifact Office)
    {
        public IReadOnlyList<Artifact> All => [Presentation, Json, Csv, Narrow, Text, Office];
        public IEnumerable<string> AllText => All.Select(value => value.Text);
    }
}
