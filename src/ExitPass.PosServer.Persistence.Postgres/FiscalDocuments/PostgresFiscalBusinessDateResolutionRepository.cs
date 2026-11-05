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

            const string periodSql = """
                SELECT period.business_day_date,period.period_start_at,period.period_end_at,
                       period.reporting_timezone_name,period.business_day_cutoff_local_time
                FROM pos.fiscal_reporting_periods period
                WHERE period.site_pos_server_id=@site_pos_server_id
                  AND period.period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'
                ORDER BY period.period_sequence DESC
                LIMIT 2;
                """;
            await using var periodCommand = new NpgsqlCommand(periodSql, connection, transaction);
            periodCommand.Parameters.AddWithValue("site_pos_server_id", query.SitePosServerId);
            await using var reader = await periodCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new FiscalBusinessDateResolutionUnavailableException(
                    "FISCAL_BUSINESS_DATE_NOT_OPEN");
            }
            var businessDate = reader.GetFieldValue<DateOnly>(0);
            var periodStart = reader.GetFieldValue<DateTimeOffset>(1);
            DateTimeOffset? periodEnd = reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2);
            var timezone = reader.GetString(3);
            var cutoff = reader.GetFieldValue<TimeOnly>(4);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new FiscalBusinessDateResolutionUnavailableException("The OPEN fiscal business date is ambiguous.");
            await reader.CloseAsync().ConfigureAwait(false);

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
                businessDate,
                periodStart,
                periodEnd,
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
