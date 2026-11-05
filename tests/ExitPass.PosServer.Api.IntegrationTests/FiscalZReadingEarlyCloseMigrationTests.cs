using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class FiscalZReadingEarlyCloseMigrationTests
{
    private const string ConnectionVariable = "POSSERVER_EARLY_CLOSE_MIGRATION_TEST_DB_URL";

    [Fact]
    public async Task MigrationIsIdempotentPreservesHistoryAndEnforcesEarlyCloseSemantics()
    {
        if (!TryGetConnectionString(out var connectionString)) return;

        await ResetFixtureAsync(connectionString);
        var migration = await File.ReadAllTextAsync(MigrationPath());
        var before = await FingerprintAsync(connectionString);

        await ExecuteAsync(connectionString, migration);
        await ExecuteAsync(connectionString, migration);

        Assert.Equal(before, await FingerprintAsync(connectionString));
        Assert.Equal(2L, await ScalarAsync<long>(connectionString,
            "SELECT count(*) FROM pos.x_z_reports WHERE NOT is_early_closure AND early_closure_reason IS NULL"));
        Assert.Equal(2L, await ScalarAsync<long>(connectionString, """
            SELECT count(*) FROM pg_constraint
            WHERE conrelid='pos.x_z_reports'::regclass
              AND conname IN ('ck_x_z_reports__timestamps','ck_x_z_reports__early_closure')
              AND contype='c' AND convalidated
            """));

        await ExecuteAsync(connectionString, InsertSql(
            "30000000-0000-0000-0000-000000000003",
            "1c628bc2-49c3-53e8-ae83-2082bcf28467",
            "2026-11-05T00:00:00Z", "2026-11-06T00:00:00Z", "2026-11-05T12:00:00Z",
            true, "Authorized maintenance"));

        await AssertConstraintRejectedAsync(connectionString, InsertSql(
            "30000000-0000-0000-0000-000000000004",
            "1c628bc2-49c3-53e8-ae83-2082bcf28467",
            "2026-11-05T00:00:00Z", "2026-11-06T00:00:00Z", "2026-11-05T12:00:00Z",
            true, " "));
        await AssertConstraintRejectedAsync(connectionString, InsertSql(
            "30000000-0000-0000-0000-000000000005",
            "1c628bc2-49c3-53e8-ae83-2082bcf28467",
            "2026-11-05T00:00:00Z", "2026-11-06T00:00:00Z", "2026-11-05T12:00:00Z",
            false, null));
    }

    private static async Task ResetFixtureAsync(string connectionString) =>
        await ExecuteAsync(connectionString, """
            DROP SCHEMA IF EXISTS pos CASCADE;
            CREATE SCHEMA pos;
            CREATE TABLE pos.x_z_reports(
                x_z_report_id uuid PRIMARY KEY,
                report_kind_code_id uuid NOT NULL,
                period_start_at timestamptz NOT NULL,
                period_end_at timestamptz NOT NULL,
                generated_at timestamptz NOT NULL,
                committed_at timestamptz NOT NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL,
                CONSTRAINT ck_x_z_reports__timestamps CHECK (
                    ((report_kind_code_id='5dc3cc94-b3ab-5582-a598-e779871fc3e2' AND generated_at>=period_start_at)
                    OR (report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467' AND generated_at>=period_end_at))
                    AND committed_at>=generated_at AND updated_at=created_at)
            );
            INSERT INTO pos.x_z_reports VALUES
            ('30000000-0000-0000-0000-000000000001','5dc3cc94-b3ab-5582-a598-e779871fc3e2','2026-11-01T00:00:00Z','2026-11-02T00:00:00Z','2026-11-01T12:00:00Z','2026-11-01T12:00:00Z','2026-11-01T12:00:00Z','2026-11-01T12:00:00Z'),
            ('30000000-0000-0000-0000-000000000002','1c628bc2-49c3-53e8-ae83-2082bcf28467','2026-11-01T00:00:00Z','2026-11-02T00:00:00Z','2026-11-02T00:00:01Z','2026-11-02T00:00:01Z','2026-11-02T00:00:01Z','2026-11-02T00:00:01Z');
            """);

    private static string InsertSql(
        string id, string kind, string start, string end, string generated, bool early, string? reason) => $"""
            INSERT INTO pos.x_z_reports(
                x_z_report_id,report_kind_code_id,period_start_at,period_end_at,generated_at,committed_at,
                is_early_closure,early_closure_reason,created_at,updated_at)
            VALUES('{id}','{kind}','{start}','{end}','{generated}','{generated}',
                {early.ToString().ToLowerInvariant()},{(reason is null ? "NULL" : "'" + reason.Replace("'", "''") + "'")},'{generated}','{generated}');
            """;

    private static async Task AssertConstraintRejectedAsync(string connectionString, string sql)
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connectionString, sql));
        Assert.Equal("23514", exception.SqlState);
    }

    private static Task<string> FingerprintAsync(string connectionString) => ScalarAsync<string>(connectionString, """
        SELECT md5(string_agg((to_jsonb(row_value)-'is_early_closure'-'early_closure_reason')::text,'' ORDER BY x_z_report_id::text))
        FROM pos.x_z_reports row_value
        """);

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
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database ?? string.Empty;
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{ConnectionVariable} must target an isolated test database.");
        return true;
    }

    private static string MigrationPath() => Path.Combine(
        FindRepositoryRoot(), "db", "scripts", "pos-fiscal-z-reading-early-close-migration.sql");

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
