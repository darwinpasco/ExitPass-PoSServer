using System.Net;
using System.Net.Http.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class FiscalZCloseBoundaryFoundationPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_Z_CLOSE_FOUNDATION_TEST_DB_URL";
    private static readonly Guid SiteId = Guid.Parse("77000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("77000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("77000000-0000-4000-8000-000000000003");

    [Fact]
    public async Task InitializationApiAndSharedBoundaryAreDurableReplaySafeAndScopeIsolated()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await RebuildAsync(connectionString);
        await SeedScopeAsync(connectionString);
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);

        var request = new InitializeFiscalZCloseStateRequest(
            "z-state-proof-operation", SiteId, IdentityId, "PHP", "approved_new_scope_zero",
            0, 0, 0, "approved-design-authority-20260804");

        using var unauthorized = await client.PostAsJsonAsync("/v1/admin/fiscal-z-close-states/initialize", request);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        Authorize(client);
        var concurrent = await Task.WhenAll(
            client.PostAsJsonAsync("/v1/admin/fiscal-z-close-states/initialize", request),
            client.PostAsJsonAsync("/v1/admin/fiscal-z-close-states/initialize", request));
        Assert.All(concurrent, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK }));
        Assert.Equal(1, concurrent.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_z_close_states"));
        Assert.Equal(1, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_z_close_state_transitions"));
        Assert.Equal(3, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.fiscal_z_close_state_transition_values"));

        await using (var restartedApp = await StartApiAsync(connectionString))
        using (var restartedClient = CreateClient(restartedApp))
        {
            Authorize(restartedClient);
            using var restartedReplay = await restartedClient.PostAsJsonAsync(
                "/v1/admin/fiscal-z-close-states/initialize", request);
            Assert.Equal(HttpStatusCode.OK, restartedReplay.StatusCode);
        }

        using var conflict = await client.PostAsJsonAsync(
            "/v1/admin/fiscal-z-close-states/initialize",
            request with { Provenance = "verified_legacy_import", ZCounterValue = 1 });
        var conflictBody = await conflict.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.DoesNotContain("hash", conflictBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constraint", conflictBody, StringComparison.OrdinalIgnoreCase);

        await AssertDirectMutationRejectedAsync(connectionString);
        await AssertForcedInitializationFailureRollsBackAsync(connectionString, client);
        await AssertBoundarySerializationAndClosedRevalidationAsync(connectionString);
        await AssertIndependentCurrencyDoesNotBlockAsync(connectionString);
    }

    private static async Task AssertForcedInitializationFailureRollsBackAsync(
        string connectionString,
        HttpClient client)
    {
        const string installFailure = """
            CREATE OR REPLACE FUNCTION pos.fail_z_state_initialization_proof()
            RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION 'synthetic Z state initialization persistence failure';
            END;
            $$;
            CREATE TRIGGER trg_z_state_initialization_proof_failure
            BEFORE INSERT ON pos.fiscal_z_close_state_transition_values
            FOR EACH ROW EXECUTE FUNCTION pos.fail_z_state_initialization_proof();
            """;
        const string removeFailure = """
            DROP TRIGGER IF EXISTS trg_z_state_initialization_proof_failure
                ON pos.fiscal_z_close_state_transition_values;
            DROP FUNCTION IF EXISTS pos.fail_z_state_initialization_proof();
            """;

        await ExecuteAsync(connectionString, installFailure);
        try
        {
            using var failure = await client.PostAsJsonAsync(
                "/v1/admin/fiscal-z-close-states/initialize",
                new InitializeFiscalZCloseStateRequest(
                    "z-state-proof-rollback", SiteId, IdentityId, "USD",
                    "approved_new_scope_zero", 0, 0, 0,
                    "approved-design-authority-20260804"));
            var body = await failure.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
            Assert.DoesNotContain("synthetic", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("trigger", body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await ScalarAsync<long>(connectionString,
                "SELECT count(*) FROM pos.fiscal_z_close_states WHERE currency_code='USD'"));
            Assert.Equal(0, await ScalarAsync<long>(connectionString,
                "SELECT count(*) FROM pos.fiscal_z_close_state_transitions WHERE operation_ref='z-state-proof-rollback'"));
        }
        finally
        {
            await ExecuteAsync(connectionString, removeFailure);
        }
    }

    private static async Task AssertDirectMutationRejectedAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var update = new NpgsqlCommand(
            "UPDATE pos.fiscal_z_close_states SET z_counter_value=z_counter_value+1, state_version=state_version+1", connection);
        var updateFailure = await Assert.ThrowsAsync<PostgresException>(() => update.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, updateFailure.SqlState);

        await using var delete = new NpgsqlCommand("DELETE FROM pos.fiscal_z_close_states", connection);
        var deleteFailure = await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, deleteFailure.SqlState);
    }

    private static async Task AssertBoundarySerializationAndClosedRevalidationAsync(string connectionString)
    {
        await using var first = new NpgsqlConnection(connectionString);
        await using var second = new NpgsqlConnection(connectionString);
        await first.OpenAsync();
        await second.OpenAsync();
        await using var firstTransaction = await first.BeginTransactionAsync();
        await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(first, firstTransaction, SiteId, IdentityId, "PHP", default);
        var assignment = await PostgresFiscalCloseBoundaryCoordinator.ResolveAndLockOpenPeriodAsync(
            first, firstTransaction, SiteId, IdentityId, "PHP", default);
        Assert.Equal(PeriodId, assignment.FiscalReportingPeriodId);

        var waiter = WaitForClosedBoundaryAsync(second);

        await Task.Delay(150);
        await using (var close = new NpgsqlCommand(
            "UPDATE pos.fiscal_reporting_periods SET period_status_code_id='af7ee931-a023-507e-81a4-17adf047eb94', closing_started_at=clock_timestamp(), closed_at=clock_timestamp(), updated_at=clock_timestamp(), updated_by_ref='Z007B-PROOF' WHERE fiscal_reporting_period_id=@period", first, firstTransaction))
        {
            close.Parameters.AddWithValue("period", PeriodId);
            await close.ExecuteNonQueryAsync();
        }
        await firstTransaction.CommitAsync();
        Assert.Equal(ExitPass.PosServer.Runtime.FiscalReports.FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable, await waiter);
    }

    private static async Task<ExitPass.PosServer.Runtime.FiscalReports.FiscalCloseBoundaryErrorCode> WaitForClosedBoundaryAsync(
        NpgsqlConnection connection)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(connection, transaction, SiteId, IdentityId, "PHP", default);
        var error = await Assert.ThrowsAsync<ExitPass.PosServer.Runtime.FiscalReports.FiscalCloseBoundaryException>(() =>
            PostgresFiscalCloseBoundaryCoordinator.ResolveAndLockOpenPeriodAsync(connection, transaction, SiteId, IdentityId, "PHP", default));
        await transaction.RollbackAsync();
        return error.ErrorCode;
    }

    private static async Task AssertIndependentCurrencyDoesNotBlockAsync(string connectionString)
    {
        await using var first = new NpgsqlConnection(connectionString);
        await using var second = new NpgsqlConnection(connectionString);
        await first.OpenAsync();
        await second.OpenAsync();
        await using var firstTransaction = await first.BeginTransactionAsync();
        await using var secondTransaction = await second.BeginTransactionAsync();
        await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(first, firstTransaction, SiteId, IdentityId, "PHP", default);
        var acquisition = PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(second, secondTransaction, SiteId, IdentityId, "USD", default);
        Assert.Same(acquisition, await Task.WhenAny(acquisition, Task.Delay(1000)));
        await secondTransaction.RollbackAsync();
        await firstTransaction.RollbackAsync();
    }

    private static async Task SeedScopeAsync(string connectionString)
    {
        const string sql = """
        INSERT INTO pos.site_pos_servers(site_pos_server_id,site_pos_server_code,display_name,central_pms_site_ref,reporting_timezone_name,business_day_cutoff_local_time)
        VALUES('77000000-0000-4000-8000-000000000001','Z007B-PROOF-SITE','Z007B PROOF SITE','Z007B-PROOF-CENTRAL','Etc/UTC','00:00:00');
        INSERT INTO pos.fiscal_identities(fiscal_identity_id,fiscal_identity_code,registered_business_name,registered_business_address,tin,fiscal_identity_status,created_by_ref,updated_by_ref)
        VALUES('77000000-0000-4000-8000-000000000002','Z007B-PROOF-IDENTITY','SYNTHETIC Z007B BUSINESS','SYNTHETIC ADDRESS','SYNTHETIC-TIN','APPROVED','Z007B-PROOF','Z007B-PROOF');
        INSERT INTO pos.site_pos_server_fiscal_identity_history(site_pos_server_fiscal_identity_history_id,site_pos_server_id,fiscal_identity_id,effective_start_at,assignment_reason_text)
        VALUES('77000000-0000-4000-8000-000000000004','77000000-0000-4000-8000-000000000001','77000000-0000-4000-8000-000000000002','2026-01-01T00:00:00Z','Z007B synthetic proof assignment');
        INSERT INTO pos.fiscal_reporting_periods(fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,business_day_cutoff_local_time,currency_code,period_sequence,opened_at,created_by_ref,updated_by_ref)
        VALUES('77000000-0000-4000-8000-000000000003','f6766f48-62f0-513f-b9eb-e61c2f3e8c66','77000000-0000-4000-8000-000000000001','77000000-0000-4000-8000-000000000002','1a6f7021-bc84-5c01-afaa-c5d6685633c8',current_date,transaction_timestamp()-interval '1 hour',transaction_timestamp()+interval '1 hour','Etc/UTC','00:00:00','PHP',1,transaction_timestamp()-interval '1 hour','Z007B-PROOF','Z007B-PROOF');
        """;
        await ExecuteAsync(connectionString, sql);
    }

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PosServer"] = connectionString,
            ["PosServer:Admin:ApiKeys:0:Principal"] = "z-state-proof-service",
            ["PosServer:Admin:ApiKeys:0:Key"] = "synthetic-z-state-proof-key",
            ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalZCloseStateInitializationAuthorization.Permission,
            ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = SiteId.ToString("D"),
            ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = IdentityId.ToString("D")
        });
        builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapFiscalZCloseStateInitializationEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static void Authorize(HttpClient client)
    {
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, "synthetic-z-state-proof-key");
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName, "z007b-proof-correlation");
    }

    private static async Task RebuildAsync(string connectionString)
    {
        await ExecuteAsync(connectionString, "DROP SCHEMA IF EXISTS pos CASCADE");
        var root = FindRepositoryRoot();
        foreach (var entry in File.ReadLines(Path.Combine(root, "db", "rebuild", "pos_sql_apply_order.txt"))
                     .Select(value => value.Trim()).Where(value => value.Length > 0 && !value.StartsWith('#')))
            await ExecuteAsync(connectionString, await File.ReadAllTextAsync(Path.Combine(root, entry.Replace('/', Path.DirectorySeparatorChar))));
        foreach (var file in Directory.GetFiles(Path.Combine(root, "db", "reference-data", "controlled-codes", "generated", "sql"), "*.sql").OrderBy(value => value, StringComparer.Ordinal))
            await ExecuteAsync(connectionString, await File.ReadAllTextAsync(file));
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

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
}
