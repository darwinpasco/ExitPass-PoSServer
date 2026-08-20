using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace ExitPass.PosServer.Api.Health;

public sealed class PosServerDatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString =
            configuration.GetConnectionString("PosServer") ??
            configuration["POSSERVER_DB_URL"] ??
            configuration["PosServer:Database:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("pos_database_not_configured");
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return result is 1
                ? HealthCheckResult.Healthy("pos_database_ready")
                : HealthCheckResult.Unhealthy("pos_database_readiness_query_failed");
        }
        catch (Exception exception) when (exception is ArgumentException or NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return HealthCheckResult.Unhealthy("pos_database_unavailable");
        }
    }
}
