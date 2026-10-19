using System.Text;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class FiscalReportArtifactPostgresIntegrationTests : IDisposable
{
    private const string ConnectionVariable = "POSSERVER_FISCAL_ARTIFACT_TEST_DB_URL";
    private static readonly Guid SiteId = Guid.Parse("fa000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("fa000000-0000-4000-8000-000000000002");
    private readonly string root = Path.Combine(Path.GetTempPath(), "exitpass-artifact-db-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task PostgresCatalogReusesIdenticalContentCreatesImmutableSuccessorAndSerializesConcurrency()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await PrepareAsync(connectionString);
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgresFiscalReportArtifactRepository(dataSource, new FileSystemFiscalReportArtifactStore(root));
        var command = Command("source-a");
        var renders = 0;

        var created = await repository.GetOrCreateAsync(command, _ =>
        {
            renders++;
            return Task.FromResult(Payload("revision one"));
        });
        var replayed = await repository.GetOrCreateAsync(command, _ =>
        {
            renders++;
            return Task.FromResult(Payload("must not render"));
        });
        var successor = await repository.GetOrCreateAsync(Command("source-b"), _ =>
            Task.FromResult(Payload("revision two")));

        Assert.Equal(FiscalReportArtifactOutcome.Created, created.Outcome);
        Assert.Equal(FiscalReportArtifactOutcome.Reused, replayed.Outcome);
        Assert.Equal(1, renders);
        Assert.Equal(created.Artifact!.ArtifactId, replayed.Artifact!.ArtifactId);
        Assert.Equal(2, successor.Artifact!.Revision);
        Assert.Equal(created.Artifact.ArtifactId, successor.Artifact.SupersedesArtifactId);
        Assert.Equal(Encoding.UTF8.GetBytes("revision two"), successor.Bytes);

        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connectionString,
            $"UPDATE pos.fiscal_report_artifacts SET file_name='changed.pdf' WHERE fiscal_report_artifact_id='{created.Artifact.ArtifactId:D}'"));

        var concurrentCommand = Command("concurrent") with { ScopeReference = "2026-11" };
        var results = await Task.WhenAll(
            repository.GetOrCreateAsync(concurrentCommand, async _ => { await Task.Delay(50); return Payload("same"); }),
            repository.GetOrCreateAsync(concurrentCommand, _ => Task.FromResult(Payload("same"))));
        Assert.Single(results, result => result.Outcome == FiscalReportArtifactOutcome.Created);
        Assert.Single(results, result => result.Outcome == FiscalReportArtifactOutcome.Reused);
        Assert.Equal(1, await ScalarAsync<long>(connectionString,
            "SELECT count(*) FROM pos.fiscal_report_artifacts WHERE scope_reference='2026-11'"));
    }

    [Fact]
    public async Task PostgresMonthlySourcesExcludeOpenPeriodsAndFailWhenClosedEvidenceIsIncomplete()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await PrepareAsync(connectionString);
        var currentDate = await ScalarAsync<DateOnly>(connectionString,
            "SELECT (clock_timestamp() AT TIME ZONE 'Asia/Manila')::date");
        var first = new DateOnly(currentDate.Year, currentDate.Month, 1);
        await SeedMonthlySourcesAsync(connectionString, first);

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgresMonthlyESalesRepository(dataSource);
        var query = new MonthlyESalesQuery(SiteId, IdentityId, "PHP", first.Year, first.Month);
        var result = await repository.ReadAsync(query);

        Assert.Equal(MonthlyESalesOutcome.Succeeded, result.Outcome);
        Assert.Equal(MonthlyESalesCoverageStatus.MonthToDate, result.Report!.CoverageStatus);
        var row = Assert.Single(result.Report.Rows);
        Assert.Equal(1, row.SourcePeriodCount);
        Assert.Equal(first, row.FirstBusinessDate);
        Assert.Equal(first, row.LastBusinessDate);

        await ExecuteAsync(connectionString, $"""
        UPDATE pos.fiscal_reporting_periods
        SET period_status_code_id='fa000000-0000-4000-8000-000000000103', closed_at=clock_timestamp()
        WHERE business_day_date='{first.AddDays(1):yyyy-MM-dd}';
        """);
        var incomplete = await repository.ReadAsync(query);
        Assert.Equal(MonthlyESalesOutcome.IncompleteAuthoritativeSources, incomplete.Outcome);
    }

    private static FiscalReportArtifactCommand Command(string source) => new(
        SiteId, IdentityId, "PHP", "MONTHLY_ESALES", "2026-10", "pdf", "monthly-esales-pdf:v1",
        FiscalReportArtifactContract.SourceFingerprint(source), CalendarYear: 2026, CalendarMonth: 10);

    private static FiscalReportArtifactPayload Payload(string value) =>
        new(Encoding.UTF8.GetBytes(value), "application/pdf", "monthly-esales-202610.pdf");

    private static async Task PrepareAsync(string connectionString)
    {
        await ExecuteAsync(connectionString, """
        DROP SCHEMA IF EXISTS pos CASCADE;
        CREATE SCHEMA pos;
        CREATE TABLE pos.site_pos_servers(
          site_pos_server_id uuid PRIMARY KEY, is_active boolean NOT NULL DEFAULT true,
          reporting_timezone_name text NOT NULL DEFAULT 'Asia/Manila', display_name text);
        CREATE TABLE pos.fiscal_identities(
          fiscal_identity_id uuid PRIMARY KEY, tin text, registered_business_name text,
          registered_business_address text, software_version text);
        CREATE TABLE pos.controlled_codes(controlled_code_id uuid PRIMARY KEY, code_key text NOT NULL);
        CREATE TABLE pos.fiscal_reporting_periods(
          fiscal_reporting_period_id uuid PRIMARY KEY, period_sequence bigint,
          business_day_date date, period_status_code_id uuid, closed_at timestamptz,
          site_pos_server_id uuid, fiscal_identity_id uuid, currency_code char(3));
        CREATE TABLE pos.fiscal_report_requests(
          fiscal_report_request_id uuid PRIMARY KEY, report_status_code_id uuid);
        CREATE TABLE pos.x_z_reports(
          x_z_report_id uuid PRIMARY KEY, fiscal_reporting_period_id uuid,
          report_kind_code_id uuid, fiscal_report_request_id uuid);
        CREATE TABLE pos.bir_sales_summary_reports(
          bir_sales_summary_report_id uuid PRIMARY KEY, governing_z_report_id uuid,
          fiscal_report_request_id uuid, pos_serial_number text,
          machine_identification_number text, ptu_number text, transaction_count bigint,
          beginning_si_ref text, ending_si_ref text, gross_sales_amount_minor_units bigint,
          vatable_sales_amount_minor_units bigint, vat_amount_minor_units bigint,
          vat_exempt_sales_amount_minor_units bigint, zero_rated_sales_amount_minor_units bigint,
          committed_at timestamptz);
        CREATE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN RAISE EXCEPTION 'immutable fiscal reporting snapshot'; END; $$;
        """);
        var statePath = Path.Combine(FindRoot(), "db", "state", "tables", "pos.fiscal_report_artifacts.sql");
        await ExecuteAsync(connectionString, await File.ReadAllTextAsync(statePath));
        await ExecuteAsync(connectionString,
            $"INSERT INTO pos.site_pos_servers(site_pos_server_id) VALUES('{SiteId:D}'); " +
            $"INSERT INTO pos.fiscal_identities(fiscal_identity_id) VALUES('{IdentityId:D}');");
    }

    private static Task SeedMonthlySourcesAsync(string connectionString, DateOnly first) => ExecuteAsync(connectionString, $"""
        INSERT INTO pos.controlled_codes VALUES
          ('1c628bc2-49c3-53e8-ae83-2082bcf28467','z'),
          ('fa000000-0000-4000-8000-000000000101','committed'),
          ('fa000000-0000-4000-8000-000000000102','open'),
          ('fa000000-0000-4000-8000-000000000103','closed');
        UPDATE pos.site_pos_servers SET display_name='PITX Level 3' WHERE site_pos_server_id='{SiteId:D}';
        UPDATE pos.fiscal_identities SET tin='123-456-789-000', registered_business_name='ExitPass',
          registered_business_address='PITX', software_version='1.3' WHERE fiscal_identity_id='{IdentityId:D}';
        INSERT INTO pos.fiscal_reporting_periods VALUES
          ('fa000000-0000-4000-8000-000000000201',1,'{first:yyyy-MM-dd}','fa000000-0000-4000-8000-000000000103',clock_timestamp(),'{SiteId:D}','{IdentityId:D}','PHP'),
          ('fa000000-0000-4000-8000-000000000202',2,'{first.AddDays(1):yyyy-MM-dd}','fa000000-0000-4000-8000-000000000102',NULL,'{SiteId:D}','{IdentityId:D}','PHP');
        INSERT INTO pos.fiscal_report_requests VALUES
          ('fa000000-0000-4000-8000-000000000301','fa000000-0000-4000-8000-000000000101'),
          ('fa000000-0000-4000-8000-000000000302','fa000000-0000-4000-8000-000000000101');
        INSERT INTO pos.x_z_reports VALUES
          ('fa000000-0000-4000-8000-000000000401','fa000000-0000-4000-8000-000000000201','1c628bc2-49c3-53e8-ae83-2082bcf28467','fa000000-0000-4000-8000-000000000301');
        INSERT INTO pos.bir_sales_summary_reports VALUES
          ('fa000000-0000-4000-8000-000000000501','fa000000-0000-4000-8000-000000000401','fa000000-0000-4000-8000-000000000302',
           'POS-1','MIN-1','PTU-1',2,'SI-0001','SI-0002',10000,8929,1071,0,0,clock_timestamp());
        """);

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "db", "rebuild", "pos_sql_apply_order.txt"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
