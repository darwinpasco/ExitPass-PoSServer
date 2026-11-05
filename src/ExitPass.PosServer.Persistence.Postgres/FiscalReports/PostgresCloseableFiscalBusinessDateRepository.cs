using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresCloseableFiscalBusinessDateRepository(NpgsqlDataSource dataSource)
    : ICloseableFiscalBusinessDateRepository
{
    public async Task<CloseableFiscalBusinessDatesSnapshot> ReadAsync(
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(Sql, connection);
            command.Parameters.AddWithValue("site", sitePosServerId);
            command.Parameters.AddWithValue("identity", fiscalIdentityId);
            command.Parameters.AddWithValue("currency", currencyCode);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var periods = new List<CloseableFiscalBusinessDate>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                periods.Add(new CloseableFiscalBusinessDate(
                    reader.GetGuid(0),
                    reader.GetFieldValue<DateOnly>(1),
                    reader.GetFieldValue<DateTimeOffset>(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    reader.GetString(4).ToUpperInvariant(),
                    reader.GetInt64(5),
                    reader.GetInt64(6)));
            }

            return new CloseableFiscalBusinessDatesSnapshot(
                sitePosServerId,
                fiscalIdentityId,
                currencyCode,
                periods,
                "closeable_fiscal_business_dates_read");
        }
        catch (NpgsqlException)
        {
            return new CloseableFiscalBusinessDatesSnapshot(
                sitePosServerId,
                fiscalIdentityId,
                currencyCode,
                [],
                "closeable_fiscal_business_dates_unavailable",
                "Closeable fiscal business dates are temporarily unavailable.");
        }
    }

    private const string Sql = """
        WITH RECURSIVE scope_state AS (
            SELECT state.state_version,
                   state.last_closed_reporting_period_id,
                   COALESCE(last_period.period_sequence, 0) AS last_closed_period_sequence
            FROM pos.fiscal_z_close_states state
            LEFT JOIN pos.fiscal_reporting_periods last_period
              ON last_period.fiscal_reporting_period_id = state.last_closed_reporting_period_id
             AND last_period.site_pos_server_id = state.site_pos_server_id
             AND last_period.fiscal_identity_id = state.fiscal_identity_id
             AND last_period.currency_code = state.currency_code
            LEFT JOIN pos.controlled_codes last_status
              ON last_status.controlled_code_id = last_period.period_status_code_id
            LEFT JOIN pos.x_z_reports last_z
              ON last_z.x_z_report_id = state.last_committed_z_report_id
             AND last_z.fiscal_reporting_period_id = last_period.fiscal_reporting_period_id
             AND last_z.site_pos_server_id = state.site_pos_server_id
             AND last_z.fiscal_identity_id = state.fiscal_identity_id
             AND last_z.currency_code = state.currency_code
             AND last_z.report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
            WHERE state.site_pos_server_id = @site
              AND state.fiscal_identity_id = @identity
              AND state.currency_code = @currency
              AND (
                    (state.state_version = 1
                     AND state.last_closed_reporting_period_id IS NULL
                     AND state.last_committed_z_report_id IS NULL)
                    OR
                    (last_status.code_key = 'closed'
                     AND last_z.x_z_report_id IS NOT NULL)
                  )
        ), candidate_periods AS (
            SELECT period.fiscal_reporting_period_id,
                   period.business_day_date,
                   period.period_start_at,
                   period.period_end_at,
                   status.code_key AS status,
                   period.period_sequence,
                   period.expected_prior_period_id,
                   (
                       SELECT count(*)
                       FROM pos.fiscal_documents document
                       JOIN pos.controlled_codes document_type
                         ON document_type.controlled_code_id = document.fiscal_document_type_code_id
                       JOIN pos.controlled_codes document_status
                         ON document_status.controlled_code_id = document.fiscal_document_status_code_id
                       WHERE document.fiscal_reporting_period_id = period.fiscal_reporting_period_id
                         AND document.site_pos_server_id = period.site_pos_server_id
                         AND document.fiscal_identity_id = period.fiscal_identity_id
                         AND document.currency_code = period.currency_code
                         AND document_type.code_key = 'sales_invoice'
                         AND document_status.code_key IN ('recorded', 'issued', 'voided')
                   ) AS transaction_count
            FROM pos.fiscal_reporting_periods period
            JOIN pos.controlled_codes status
              ON status.controlled_code_id = period.period_status_code_id
            JOIN pos.site_pos_servers site
              ON site.site_pos_server_id = period.site_pos_server_id
             AND site.is_active
            JOIN pos.fiscal_identities identity
              ON identity.fiscal_identity_id = period.fiscal_identity_id
             AND identity.is_active
            JOIN pos.fiscal_reporting_contract_versions contract
              ON contract.fiscal_reporting_contract_version_id = period.fiscal_reporting_contract_version_id
             AND contract.contract_key = 'pos-server-fiscal-reporting'
             AND contract.contract_version = 'v1'
             AND contract.semantic_hash_version = 'pos-server-fiscal-report-request:sha256:v1'
             AND contract.is_active
            WHERE period.site_pos_server_id = @site
              AND period.fiscal_identity_id = @identity
              AND period.currency_code = @currency
              AND status.code_key = 'open'
              AND period.period_end_at <= transaction_timestamp()
              AND NOT EXISTS (
                  SELECT 1
                  FROM pos.x_z_reports report
                  WHERE report.fiscal_reporting_period_id = period.fiscal_reporting_period_id
                    AND report.site_pos_server_id = period.site_pos_server_id
                    AND report.fiscal_identity_id = period.fiscal_identity_id
                    AND report.currency_code = period.currency_code
                    AND report.report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM pos.fiscal_documents document
                  WHERE document.site_pos_server_id = period.site_pos_server_id
                    AND document.fiscal_identity_id = period.fiscal_identity_id
                    AND (
                         (document.business_day_date = period.business_day_date
                          AND (document.currency_code IS NULL
                               OR document.currency_code <> period.currency_code
                               OR document.fiscal_reporting_period_id IS DISTINCT FROM period.fiscal_reporting_period_id))
                         OR
                         (document.fiscal_reporting_period_id = period.fiscal_reporting_period_id
                          AND (document.business_day_date IS DISTINCT FROM period.business_day_date
                               OR document.currency_code IS DISTINCT FROM period.currency_code))
                    )
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM pos.fiscal_documents document
                  JOIN pos.controlled_codes document_type
                    ON document_type.controlled_code_id = document.fiscal_document_type_code_id
                  JOIN pos.controlled_codes document_status
                    ON document_status.controlled_code_id = document.fiscal_document_status_code_id
                  WHERE document.fiscal_reporting_period_id = period.fiscal_reporting_period_id
                    AND document.site_pos_server_id = period.site_pos_server_id
                    AND document.fiscal_identity_id = period.fiscal_identity_id
                    AND document.currency_code = period.currency_code
                    AND (
                         document_type.code_key <> 'sales_invoice'
                         OR document_status.code_key NOT IN (
                             'recorded', 'issued', 'voided', 'failed', 'requested', 'processing',
                             'in_progress', 'uncertain', 'unknown_commit_outcome', 'rejected')
                    )
              )
        ), closeable_chain AS (
            SELECT period.fiscal_reporting_period_id,
                   period.business_day_date,
                   period.period_start_at,
                   period.period_end_at,
                   period.status,
                   period.period_sequence,
                   period.transaction_count,
                   state.state_version AS expected_state_version
            FROM candidate_periods period
            CROSS JOIN scope_state state
            WHERE period.period_sequence = state.last_closed_period_sequence + 1
              AND period.expected_prior_period_id IS NOT DISTINCT FROM state.last_closed_reporting_period_id

            UNION ALL

            SELECT period.fiscal_reporting_period_id,
                   period.business_day_date,
                   period.period_start_at,
                   period.period_end_at,
                   period.status,
                   period.period_sequence,
                   period.transaction_count,
                   chain.expected_state_version + 1
            FROM candidate_periods period
            JOIN closeable_chain chain
              ON period.period_sequence = chain.period_sequence + 1
             AND period.expected_prior_period_id = chain.fiscal_reporting_period_id
        )
        SELECT fiscal_reporting_period_id,
               business_day_date,
               period_start_at,
               period_end_at,
               status,
               transaction_count,
               expected_state_version
        FROM closeable_chain
        ORDER BY business_day_date, period_sequence, fiscal_reporting_period_id;
        """;
}
