using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public sealed class PostgresFiscalBusinessDateResolutionRepository(NpgsqlDataSource dataSource)
    : IFiscalBusinessDateResolutionRepository
{
    public async Task<FiscalBusinessDateResolution> ResolveAsync(
        FiscalBusinessDateResolutionQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            {
                await readOnly.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            const string configurationSql = """
                SELECT reporting_timezone_name, business_day_cutoff_local_time
                FROM pos.site_pos_servers
                WHERE site_pos_server_id = @site_pos_server_id
                  AND is_active;
                """;
            await using var configuration = new NpgsqlCommand(configurationSql, connection, transaction);
            configuration.Parameters.AddWithValue("site_pos_server_id", query.SitePosServerId);
            await using var reader = await configuration.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ||
                reader.IsDBNull(0) ||
                reader.IsDBNull(1))
            {
                throw new FiscalBusinessDateResolutionUnavailableException(
                    "The active Site POS Server fiscal timezone and cutoff are unavailable.");
            }

            var timezone = reader.GetString(0);
            var cutoff = reader.GetFieldValue<TimeOnly>(1);
            await reader.CloseAsync().ConfigureAwait(false);

            (DateOnly BusinessDayDate, DateTimeOffset PeriodStartAt, DateTimeOffset PeriodEndAt) window;
            try
            {
                window = PostgresFiscalCloseBoundaryCoordinator.ResolveBusinessDayWindow(
                    query.FiscalEventAt,
                    timezone,
                    cutoff);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                throw new FiscalBusinessDateResolutionUnavailableException(
                    "The Site POS Server fiscal timezone is invalid.",
                    ex);
            }

            var scope = string.Join(
                ":",
                "fiscal_document_creation",
                query.SitePosServerId.ToString("N"),
                query.FiscalDocumentTypeCodeId.ToString("N"));
            const string idempotencySql = """
                SELECT semantic_request_hash, linked_fiscal_document_id
                FROM pos.idempotency_records
                WHERE idempotency_scope = @idempotency_scope
                  AND idempotency_key = @idempotency_key;
                """;
            await using var idempotency = new NpgsqlCommand(idempotencySql, connection, transaction);
            idempotency.Parameters.AddWithValue("idempotency_scope", scope);
            idempotency.Parameters.AddWithValue("idempotency_key", query.UpstreamFinalityReference);
            await using var idempotencyReader = await idempotency.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var exists = await idempotencyReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var semanticHash = exists ? idempotencyReader.GetString(0) : null;
            var linkedDocument = exists && !idempotencyReader.IsDBNull(1)
                ? idempotencyReader.GetGuid(1)
                : (Guid?)null;
            await idempotencyReader.CloseAsync().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new FiscalBusinessDateResolution(
                window.BusinessDayDate,
                window.PeriodStartAt,
                window.PeriodEndAt,
                timezone,
                cutoff,
                exists,
                linkedDocument,
                semanticHash);
        }
        catch (FiscalBusinessDateResolutionUnavailableException)
        {
            throw;
        }
        catch (NpgsqlException ex)
        {
            throw new FiscalBusinessDateResolutionUnavailableException(
                "Fiscal business-date resolution is temporarily unavailable.",
                ex);
        }
    }
}
