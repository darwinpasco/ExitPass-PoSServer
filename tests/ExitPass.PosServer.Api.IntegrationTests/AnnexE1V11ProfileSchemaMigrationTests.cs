using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class AnnexE1V11ProfileSchemaMigrationTests
{
    private const string ConnectionVariable = "POSSERVER_ANNEX_E1_V11_PROFILE_MIGRATION_TEST_DB_URL";
    private const string ReportProfile = "pos-server-bir-annex-e1-rmo24-2023:v1";
    private const string V1Profile = "pos-server-annex-e1-accounting-calculation:v1";
    private const string V1Hash = "36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f";
    private const string V11Profile = "pos-server-annex-e1-accounting-calculation:v1.1";
    private const string V11Hash = "02ba18516878ee46364e7c35639fcb89d2976fab18e3a57babd705a3d01bfebb";

    [Fact]
    public async Task MigrationIsIdempotentPreservesHistoryAndAcceptsOnlyApprovedPairs()
    {
        if (!TryGetConnectionString(out var connectionString)) return;

        await ResetFixtureAsync(connectionString, incompatibleConstraint: false);
        var migration = await File.ReadAllTextAsync(MigrationPath());
        var before = await ReadFingerprintAsync(connectionString);

        await ExecuteMigrationAsync(connectionString, migration);
        var afterFirst = await ReadFingerprintAsync(connectionString);
        await ExecuteMigrationAsync(connectionString, migration);
        var afterSecond = await ReadFingerprintAsync(connectionString);

        Assert.Equal(before, afterFirst);
        Assert.Equal(afterFirst, afterSecond);
        Assert.Equal(2, await ScalarAsync<long>(connectionString, "SELECT count(*) FROM pos.annex_e1_workbooks"));

        await AssertAcceptedAsync(connectionString, ReportProfile, V1Profile, V1Hash, 10);
        await AssertAcceptedAsync(connectionString, ReportProfile, V11Profile, V11Hash, 11);
        await AssertRejectedAsync(connectionString, ReportProfile, V1Profile, V11Hash, 12);
        await AssertRejectedAsync(connectionString, ReportProfile, V11Profile, V1Hash, 13);
        await AssertRejectedAsync(connectionString, "unknown-report-profile", V11Profile, V11Hash, 14);
        await AssertRejectedAsync(connectionString, ReportProfile, "unknown-calculation-profile", new string('f', 64), 15);

        Assert.Equal(2, await ScalarAsync<long>(connectionString, """
            SELECT count(*) FROM pos.annex_e1_workbooks
            WHERE annex_e1_workbook_id IN (
                '10000000-0000-0000-0000-000000000001',
                '10000000-0000-0000-0000-000000000002')
              AND calculation_profile_ref='pos-server-annex-e1-accounting-calculation:v1'
              AND calculation_profile_sha256='36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f'
            """));
    }

    [Fact]
    public async Task MigrationPreservesRevisionAndSupersessionConstraints()
    {
        if (!TryGetConnectionString(out var connectionString)) return;

        await ResetFixtureAsync(connectionString, incompatibleConstraint: false);
        await ExecuteMigrationAsync(connectionString, await File.ReadAllTextAsync(MigrationPath()));

        var duplicateRevision = await Assert.ThrowsAsync<PostgresException>(() => InsertAsync(
            connectionString, ReportProfile, V11Profile, V11Hash, 2,
            Guid.Parse("20000000-0000-0000-0000-000000000001"), null,
            Guid.Parse("30000000-0000-0000-0000-000000000001")));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateRevision.SqlState);

        var duplicateSupersession = await Assert.ThrowsAsync<PostgresException>(() => InsertAsync(
            connectionString, ReportProfile, V11Profile, V11Hash, 3,
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Guid.Parse("30000000-0000-0000-0000-000000000002")));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateSupersession.SqlState);
    }

    [Fact]
    public async Task IncompatibleConstraintFailsClosedWithoutChangingRowsOrConstraint()
    {
        if (!TryGetConnectionString(out var connectionString)) return;

        await ResetFixtureAsync(connectionString, incompatibleConstraint: true);
        var beforeRows = await ReadFingerprintAsync(connectionString);
        var beforeConstraint = await ReadConstraintAsync(connectionString);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteMigrationAsync(connectionString, File.ReadAllText(MigrationPath())));

        Assert.Contains("existing ck_annex_e1_workbooks__profile is incompatible", exception.MessageText, StringComparison.Ordinal);
        Assert.Equal(beforeRows, await ReadFingerprintAsync(connectionString));
        Assert.Equal(beforeConstraint, await ReadConstraintAsync(connectionString));
    }

    [Fact]
    public async Task EnvironmentAndDatabaseGuardsFailClosed()
    {
        if (!TryGetConnectionString(out var connectionString)) return;

        await ResetFixtureAsync(connectionString, incompatibleConstraint: false);
        var migration = await File.ReadAllTextAsync(MigrationPath());

        var missingEnvironment = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteMigrationAsync(connectionString, migration, environment: null));
        Assert.Contains("exitpass.migration_environment must be set explicitly", missingEnvironment.MessageText, StringComparison.Ordinal);

        var wrongDatabase = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteMigrationAsync(connectionString, migration, expectedDatabase: "wrong_validation_database"));
        Assert.Contains("database guard mismatch", wrongDatabase.MessageText, StringComparison.Ordinal);
    }

    private static async Task ResetFixtureAsync(string connectionString, bool incompatibleConstraint)
    {
        var profileConstraint = incompatibleConstraint
            ? $"CHECK (profile_ref='{ReportProfile}' AND calculation_profile_ref='{V1Profile}')"
            : $"CHECK (profile_ref='{ReportProfile}' AND calculation_profile_ref='{V1Profile}' AND calculation_profile_sha256='{V1Hash}')";

        await ExecuteAsync(connectionString, $"""
            DROP SCHEMA IF EXISTS pos CASCADE;
            CREATE SCHEMA pos;
            CREATE TABLE pos.annex_e1_workbooks (
                annex_e1_workbook_id uuid NOT NULL,
                operation_key text NOT NULL,
                site_pos_server_id uuid NOT NULL,
                fiscal_identity_id uuid NOT NULL,
                currency_code char(3) NOT NULL,
                calendar_year integer NOT NULL,
                calendar_month integer NOT NULL,
                profile_ref text NOT NULL,
                calculation_profile_ref text NOT NULL,
                calculation_profile_sha256 char(64) NOT NULL,
                revision integer NOT NULL,
                semantic_hash char(64) NOT NULL,
                artifact_sha256 char(64) NOT NULL,
                supersedes_workbook_id uuid NULL,
                metadata jsonb NOT NULL,
                CONSTRAINT pk_annex_e1_workbooks PRIMARY KEY (annex_e1_workbook_id),
                CONSTRAINT fk_annex_e1_workbooks__supersedes FOREIGN KEY (supersedes_workbook_id)
                    REFERENCES pos.annex_e1_workbooks (annex_e1_workbook_id),
                CONSTRAINT uq_annex_e1_workbooks__scope_revision UNIQUE
                    (site_pos_server_id,fiscal_identity_id,currency_code,calendar_year,calendar_month,profile_ref,revision),
                CONSTRAINT uq_annex_e1_workbooks__supersedes UNIQUE (supersedes_workbook_id),
                CONSTRAINT ck_annex_e1_workbooks__profile {profileConstraint}
            );
            INSERT INTO pos.annex_e1_workbooks VALUES
              ('10000000-0000-0000-0000-000000000001','operation-1','20000000-0000-0000-0000-000000000001','30000000-0000-0000-0000-000000000001','PHP',2026,11,'{ReportProfile}','{V1Profile}','{V1Hash}',1,repeat('a',64),repeat('b',64),NULL,jsonb_build_object('reference','R001')),
              ('10000000-0000-0000-0000-000000000002','operation-2','20000000-0000-0000-0000-000000000001','30000000-0000-0000-0000-000000000001','PHP',2026,11,'{ReportProfile}','{V1Profile}','{V1Hash}',2,repeat('c',64),repeat('d',64),'10000000-0000-0000-0000-000000000001',jsonb_build_object('reference','R002'));
            """);
    }

    private static async Task AssertAcceptedAsync(string connectionString, string reportProfile, string calculationProfile, string hash, int discriminator) =>
        await InsertAsync(connectionString, reportProfile, calculationProfile, hash, 1,
            Guid.Parse($"{discriminator:D8}-0000-0000-0000-000000000001"), null,
            Guid.Parse($"{discriminator:D8}-0000-0000-0000-000000000002"));

    private static async Task AssertRejectedAsync(string connectionString, string reportProfile, string calculationProfile, string hash, int discriminator)
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(() => InsertAsync(
            connectionString, reportProfile, calculationProfile, hash, 1,
            Guid.Parse($"{discriminator:D8}-0000-0000-0000-000000000001"), null,
            Guid.Parse($"{discriminator:D8}-0000-0000-0000-000000000002")));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    private static async Task InsertAsync(
        string connectionString, string reportProfile, string calculationProfile, string hash,
        int revision, Guid siteId, Guid? supersedesId, Guid workbookId)
    {
        const string sql = """
            INSERT INTO pos.annex_e1_workbooks(
                annex_e1_workbook_id,operation_key,site_pos_server_id,fiscal_identity_id,currency_code,
                calendar_year,calendar_month,profile_ref,calculation_profile_ref,calculation_profile_sha256,
                revision,semantic_hash,artifact_sha256,supersedes_workbook_id,metadata)
            VALUES(@id,@operation,@site,'30000000-0000-0000-0000-000000000001','PHP',2026,11,
                @report,@calculation,@hash,@revision,repeat('e',64),repeat('f',64),@supersedes,'{"fixture":true}')
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", workbookId);
        command.Parameters.AddWithValue("operation", $"operation-{workbookId:D}");
        command.Parameters.AddWithValue("site", siteId);
        command.Parameters.AddWithValue("report", reportProfile);
        command.Parameters.AddWithValue("calculation", calculationProfile);
        command.Parameters.AddWithValue("hash", hash);
        command.Parameters.AddWithValue("revision", revision);
        command.Parameters.AddWithValue("supersedes", supersedesId is null ? DBNull.Value : supersedesId.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteMigrationAsync(
        string connectionString, string migration, string? environment = "VALIDATION", string? expectedDatabase = null)
    {
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database!;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        if (environment is not null)
        {
            await using var environmentCommand = new NpgsqlCommand(
                "SELECT set_config('exitpass.migration_environment',@environment,false)", connection);
            environmentCommand.Parameters.AddWithValue("environment", environment);
            await environmentCommand.ExecuteNonQueryAsync();
        }
        await using (var databaseCommand = new NpgsqlCommand(
            "SELECT set_config('exitpass.migration_database',@database,false)", connection))
        {
            databaseCommand.Parameters.AddWithValue("database", expectedDatabase ?? database);
            await databaseCommand.ExecuteNonQueryAsync();
        }
        await using var command = new NpgsqlCommand(migration, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadFingerprintAsync(string connectionString) =>
        await ScalarAsync<string>(connectionString, """
            SELECT md5(COALESCE(string_agg(to_jsonb(row_value)::text,''
                ORDER BY annex_e1_workbook_id::text),''))
            FROM pos.annex_e1_workbooks row_value
            """);

    private static async Task<string> ReadConstraintAsync(string connectionString) =>
        await ScalarAsync<string>(connectionString, """
            SELECT pg_get_constraintdef(oid)
            FROM pg_constraint
            WHERE conrelid='pos.annex_e1_workbooks'::regclass
              AND conname='ck_annex_e1_workbooks__profile'
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
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase)
            && !database.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{ConnectionVariable} must target an isolated test or validation database.");
        }

        return true;
    }

    private static string MigrationPath() => Path.Combine(
        FindRepositoryRoot(), "db", "scripts", "pos-annex-e1-v11-profile-schema-migration.sql");

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
