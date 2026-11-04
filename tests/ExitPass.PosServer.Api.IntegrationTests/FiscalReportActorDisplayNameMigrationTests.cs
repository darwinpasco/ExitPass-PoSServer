using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class FiscalReportActorDisplayNameMigrationTests
{
    private const string ConnectionVariable = "POSSERVER_ACTOR_DISPLAY_NAME_MIGRATION_TEST_DB_URL";

    [Fact]
    public async Task MigrationIsIdempotentAndPreservesExistingRows()
    {
        if (!TryGetConnectionString(out var connectionString)) return;

        await ResetFixtureAsync(connectionString, compatibleColumns: false);
        var migration = await File.ReadAllTextAsync(MigrationPath());
        var before = await ReadFingerprintAsync(connectionString);

        await ExecuteAsync(connectionString, migration);
        var afterFirst = await ReadFingerprintAsync(connectionString);
        await ExecuteAsync(connectionString, migration);
        var afterSecond = await ReadFingerprintAsync(connectionString);

        Assert.Equal(before, afterFirst);
        Assert.Equal(afterFirst, afterSecond);
        Assert.Equal(2, await ScalarAsync<long>(connectionString, """
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema='pos'
              AND ((table_name='fiscal_report_requests' AND column_name='requested_by_display_name' AND data_type='text' AND is_nullable='YES')
                OR (table_name='annex_e1_workbooks' AND column_name='generated_by_display_name' AND data_type='text' AND is_nullable='YES'))
            """));
        Assert.Equal(2, await ScalarAsync<long>(connectionString, """
            SELECT count(*) FROM pg_constraint
            WHERE (conrelid='pos.fiscal_report_requests'::regclass AND conname='ck_fiscal_report_requests__actor_display_name' AND contype='c' AND convalidated)
               OR (conrelid='pos.annex_e1_workbooks'::regclass AND conname='ck_annex_e1_workbooks__generated_by_display_name' AND contype='c' AND convalidated)
            """));
        Assert.Equal(0, await ScalarAsync<long>(connectionString,
            "SELECT count(*) FROM pos.fiscal_report_requests WHERE requested_by_display_name IS NOT NULL"));
        Assert.Equal(0, await ScalarAsync<long>(connectionString,
            "SELECT count(*) FROM pos.annex_e1_workbooks WHERE generated_by_display_name IS NOT NULL"));

        await AssertBlankRejectedAsync(connectionString,
            "UPDATE pos.fiscal_report_requests SET requested_by_display_name=' ' WHERE fiscal_report_request_id='10000000-0000-0000-0000-000000000001'");
        await AssertBlankRejectedAsync(connectionString,
            "UPDATE pos.annex_e1_workbooks SET generated_by_display_name='' WHERE annex_e1_workbook_id='20000000-0000-0000-0000-000000000001'");
    }

    [Fact]
    public async Task IncompatibleExistingColumnRollsBackEveryChange()
    {
        if (!TryGetConnectionString(out var connectionString)) return;

        await ResetFixtureAsync(connectionString, compatibleColumns: true);
        var migration = await File.ReadAllTextAsync(MigrationPath());

        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connectionString, migration));
        Assert.Contains("must be nullable text", exception.MessageText, StringComparison.Ordinal);
        Assert.Equal("integer", await ScalarAsync<string>(connectionString, """
            SELECT data_type FROM information_schema.columns
            WHERE table_schema='pos' AND table_name='fiscal_report_requests' AND column_name='requested_by_display_name'
            """));
        Assert.Equal(0, await ScalarAsync<long>(connectionString, """
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema='pos' AND table_name='annex_e1_workbooks' AND column_name='generated_by_display_name'
            """));
        Assert.Equal(0, await ScalarAsync<long>(connectionString, """
            SELECT count(*) FROM pg_constraint
            WHERE conname IN ('ck_fiscal_report_requests__actor_display_name','ck_annex_e1_workbooks__generated_by_display_name')
            """));
    }

    private static async Task ResetFixtureAsync(string connectionString, bool compatibleColumns)
    {
        var incompatibleColumn = compatibleColumns ? ", requested_by_display_name integer NULL" : string.Empty;
        await ExecuteAsync(connectionString, $"""
            DROP SCHEMA IF EXISTS pos CASCADE;
            CREATE SCHEMA pos;
            CREATE TABLE pos.fiscal_report_requests (
                fiscal_report_request_id uuid PRIMARY KEY,
                marker text NOT NULL{incompatibleColumn}
            );
            CREATE TABLE pos.annex_e1_workbooks (
                annex_e1_workbook_id uuid PRIMARY KEY,
                marker text NOT NULL
            );
            INSERT INTO pos.fiscal_report_requests(fiscal_report_request_id,marker)
            VALUES ('10000000-0000-0000-0000-000000000001','request');
            INSERT INTO pos.annex_e1_workbooks(annex_e1_workbook_id,marker)
            VALUES ('20000000-0000-0000-0000-000000000001','workbook');
            """);
    }

    private static async Task<string> ReadFingerprintAsync(string connectionString) =>
        await ScalarAsync<string>(connectionString, """
            SELECT md5(
                (SELECT string_agg((to_jsonb(r)-'requested_by_display_name')::text,'' ORDER BY fiscal_report_request_id::text) FROM pos.fiscal_report_requests r)
                || '|'
                || (SELECT string_agg((to_jsonb(w)-'generated_by_display_name')::text,'' ORDER BY annex_e1_workbook_id::text) FROM pos.annex_e1_workbooks w))
            """);

    private static async Task AssertBlankRejectedAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", exception.SqlState);
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

    private static bool TryGetConnectionString(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database ?? string.Empty;
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase)
            && !database.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{ConnectionVariable} must target an isolated test or validation database.");
        }

        return true;
    }

    private static string MigrationPath() => Path.Combine(
        FindRepositoryRoot(), "db", "scripts", "pos-fiscal-report-actor-display-name-migration.sql");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ExitPass.PosServer.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the POS Server repository root.");
    }
}
