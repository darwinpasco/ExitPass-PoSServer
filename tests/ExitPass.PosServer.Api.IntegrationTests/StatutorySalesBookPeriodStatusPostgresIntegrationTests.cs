using System.Reflection;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class StatutorySalesBookPeriodStatusPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_STATUTORY_SALES_BOOK_TEST_DB_URL";
    private static readonly Guid Site = Guid.Parse("99000000-0000-4000-8000-000000000001");
    private static readonly Guid Identity = Guid.Parse("99000000-0000-4000-8000-000000000002");
    private static readonly Guid ZKind = Guid.Parse("1c628bc2-49c3-53e8-ae83-2082bcf28467");

    [Fact]
    public async Task ExactPeriodStatusSqlUsesValidatedSiteTimezoneAndRejectsMissingOrInvalidConfiguration()
    {
        var connectionString = ConnectionString();
        if (connectionString is null) return;
        await PrepareAsync(connectionString);

        var configured = await ReadStatusAsync(connectionString, new DateOnly(2026, 10, 1));
        Assert.Equal((1, 1, 1), (configured.Total, configured.Closed, configured.CommittedZ));
        Assert.Equal("Asia/Manila", configured.Timezone);
        Assert.NotNull(configured.CurrentSiteDate);

        var boundary = await ScalarAsync<DateOnly>(connectionString, """
            SELECT ('2026-10-31T16:30:00Z'::timestamptz AT TIME ZONE reporting_timezone_name)::date
            FROM pos.site_pos_servers WHERE site_pos_server_id=@site
            """);
        Assert.Equal(new DateOnly(2026, 11, 1), boundary);

        await ExecuteAsync(connectionString,
            "UPDATE pos.site_pos_servers SET reporting_timezone_name=NULL WHERE site_pos_server_id=@site");
        var missing = await ReadStatusAsync(connectionString, new DateOnly(2026, 10, 1));
        Assert.Null(missing.Timezone);
        Assert.Null(missing.CurrentSiteDate);

        await ExecuteAsync(connectionString,
            "UPDATE pos.site_pos_servers SET reporting_timezone_name='Invalid/Timezone' WHERE site_pos_server_id=@site");
        var invalid = await ReadStatusAsync(connectionString, new DateOnly(2026, 10, 1));
        Assert.Equal("Invalid/Timezone", invalid.Timezone);
        Assert.Null(invalid.CurrentSiteDate);
    }

    private static async Task<StatusRow> ReadStatusAsync(string connectionString, DateOnly monthStart)
    {
        var field = typeof(PostgresStatutorySalesBookRepository).GetField(
            "PeriodStatusSql", BindingFlags.NonPublic | BindingFlags.Static);
        var sql = Assert.IsType<string>(field?.GetRawConstantValue());
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("site", Site);
        command.Parameters.AddWithValue("identity", Identity);
        command.Parameters.AddWithValue("currency", "PHP");
        command.Parameters.AddWithValue("month_start", monthStart);
        command.Parameters.AddWithValue("month_end", monthStart.AddMonths(1));
        command.Parameters.AddWithValue("z_kind", ZKind);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetFieldValue<DateOnly>(4));
    }

    private static async Task PrepareAsync(string connectionString)
    {
        await ExecuteAsync(connectionString, """
            DROP SCHEMA IF EXISTS pos CASCADE;
            CREATE SCHEMA pos;
            CREATE TABLE pos.site_pos_servers(
                site_pos_server_id uuid PRIMARY KEY,
                reporting_timezone_name text NULL);
            CREATE TABLE pos.controlled_codes(
                controlled_code_id uuid PRIMARY KEY,
                code_key text NOT NULL);
            CREATE TABLE pos.fiscal_reporting_periods(
                fiscal_reporting_period_id uuid PRIMARY KEY,
                site_pos_server_id uuid NOT NULL,
                fiscal_identity_id uuid NOT NULL,
                currency_code char(3) NOT NULL,
                business_day_date date NOT NULL,
                period_status_code_id uuid NOT NULL);
            CREATE TABLE pos.fiscal_report_requests(
                fiscal_report_request_id uuid PRIMARY KEY,
                report_status_code_id uuid NOT NULL);
            CREATE TABLE pos.x_z_reports(
                x_z_report_id uuid PRIMARY KEY,
                fiscal_reporting_period_id uuid NOT NULL,
                report_kind_code_id uuid NOT NULL,
                fiscal_report_request_id uuid NOT NULL);
            INSERT INTO pos.site_pos_servers VALUES
                ('99000000-0000-4000-8000-000000000001','Asia/Manila');
            INSERT INTO pos.controlled_codes VALUES
                ('99000000-0000-4000-8000-000000000010','closed'),
                ('99000000-0000-4000-8000-000000000011','committed');
            INSERT INTO pos.fiscal_reporting_periods VALUES
                ('99000000-0000-4000-8000-000000000020',
                 '99000000-0000-4000-8000-000000000001',
                 '99000000-0000-4000-8000-000000000002','PHP','2026-10-31',
                 '99000000-0000-4000-8000-000000000010');
            INSERT INTO pos.fiscal_report_requests VALUES
                ('99000000-0000-4000-8000-000000000030',
                 '99000000-0000-4000-8000-000000000011');
            INSERT INTO pos.x_z_reports VALUES
                ('99000000-0000-4000-8000-000000000040',
                 '99000000-0000-4000-8000-000000000020',
                 '1c628bc2-49c3-53e8-ae83-2082bcf28467',
                 '99000000-0000-4000-8000-000000000030');
            """);
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("site", Site);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("site", Site);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static string? ConnectionString()
    {
        var value = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(value)) return null;
        var builder = new NpgsqlConnectionStringBuilder(value);
        var database = builder.Database ?? string.Empty;
        if (!database.Contains("statutory", StringComparison.OrdinalIgnoreCase) ||
            !database.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
            database.Contains("prod", StringComparison.OrdinalIgnoreCase) ||
            database.Contains("live", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{ConnectionVariable} must target an isolated statutory validation database.");
        return value;
    }

    private sealed record StatusRow(int Total, int Closed, int CommittedZ, string? Timezone, DateOnly? CurrentSiteDate);
}
