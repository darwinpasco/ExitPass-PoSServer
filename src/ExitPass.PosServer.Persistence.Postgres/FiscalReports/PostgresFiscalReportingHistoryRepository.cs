using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using System.Text.Json;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresFiscalReportingHistoryRepository(NpgsqlDataSource dataSource) : IFiscalReportingHistoryRepository
{
    public async Task<FiscalReportingHistorySnapshot> ReadAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode, int limit, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var periods = await ReadOpenPeriodsAsync(connection, sitePosServerId, fiscalIdentityId, currencyCode, cancellationToken).ConfigureAwait(false);
            if (periods.Count > 1) return new(null, [], [], "fiscal_reporting_period_ambiguous", "More than one OPEN fiscal reporting period exists for the requested scope.");
            var readings = await ReadHistoryAsync(connection, sitePosServerId, fiscalIdentityId, currencyCode, limit, cancellationToken).ConfigureAwait(false);
            return new(periods.SingleOrDefault(), readings.Where(value => value.ReportKind == "X_READING").ToArray(), readings.Where(value => value.ReportKind == "Z_READING").ToArray(), "fiscal_reporting_history_read");
        }
        catch (NpgsqlException)
        {
            return new(null, [], [], "fiscal_reporting_history_unavailable", "Fiscal reporting history is temporarily unavailable.");
        }
    }

    private static async Task<List<FiscalReportingPeriodSnapshot>> ReadOpenPeriodsAsync(NpgsqlConnection connection, Guid site, Guid identity, string currency, CancellationToken ct)
    {
        const string sql = """
            SELECT p.fiscal_reporting_period_id,p.site_pos_server_id,p.fiscal_identity_id,p.business_day_date,
                   p.period_start_at,p.period_end_at,p.currency_code,p.period_sequence,status.code_key,COALESCE(state.state_version,0)
            FROM pos.fiscal_reporting_periods p
            JOIN pos.controlled_codes status ON status.controlled_code_id=p.period_status_code_id
            LEFT JOIN pos.fiscal_z_close_states state ON state.site_pos_server_id=p.site_pos_server_id
                 AND state.fiscal_identity_id=p.fiscal_identity_id AND state.currency_code=p.currency_code
            WHERE p.site_pos_server_id=@site AND p.fiscal_identity_id=@identity AND p.currency_code=@currency
              AND status.code_key='open' ORDER BY p.period_sequence DESC LIMIT 2;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("site", site); command.Parameters.AddWithValue("identity", identity); command.Parameters.AddWithValue("currency", currency);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var result = new List<FiscalReportingPeriodSnapshot>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            result.Add(new(reader.GetGuid(0),reader.GetGuid(1),reader.GetGuid(2),reader.GetFieldValue<DateOnly>(3),reader.GetFieldValue<DateTimeOffset>(4),reader.GetFieldValue<DateTimeOffset>(5),reader.GetString(6),reader.GetInt64(7),reader.GetString(8).ToUpperInvariant(),reader.GetInt64(9)));
        return result;
    }

    private static async Task<List<FiscalReadingHistoryItem>> ReadHistoryAsync(NpgsqlConnection connection, Guid site, Guid identity, string currency, int limit, CancellationToken ct)
    {
        const string sql = """
            SELECT r.report_number,kind.code_key,r.fiscal_reporting_period_id,r.business_day_date,r.period_start_at,r.period_end_at,
                   r.generated_at,r.transaction_count,r.gross_sales_amount_minor_units,r.net_sales_amount_minor_units,
                   r.vatable_sales_amount_minor_units,r.vat_amount_minor_units,r.vat_exempt_sales_amount_minor_units,
                   r.zero_rated_sales_amount_minor_units,r.discount_amount_minor_units,r.senior_citizen_discount_amount_minor_units,
                   r.pwd_discount_amount_minor_units,r.other_statutory_discount_amount_minor_units,r.vat_exemption_amount_minor_units,
                   r.coupon_discount_amount_minor_units,r.promotional_discount_amount_minor_units,r.void_amount_minor_units,
                   r.refund_amount_minor_units,r.return_amount_minor_units,r.adjustment_amount_minor_units,r.service_charge_amount_minor_units,
                   r.currency_code,period.period_sequence,request_status.code_key,
                   COALESCE((SELECT jsonb_agg(jsonb_build_object('classification',classification.code_key,'transactionCount',t.tender_transaction_count,'amountMinorUnits',t.amount_minor_units,'currencyCode',t.currency_code) ORDER BY classification.sort_order,classification.code_key)
                     FROM pos.fiscal_report_tender_breakdowns t JOIN pos.controlled_codes classification ON classification.controlled_code_id=t.tender_classification_code_id WHERE t.x_z_report_id=r.x_z_report_id),'[]'::jsonb)::text,
                   COALESCE((SELECT jsonb_agg(jsonb_build_object('classification',classification.code_key,'qualifyingDocumentCount',d.qualifying_document_count,'discountAmountMinorUnits',d.discount_amount_minor_units,'vatExemptionAmountMinorUnits',d.vat_exemption_amount_minor_units,'currencyCode',d.currency_code) ORDER BY classification.sort_order,classification.code_key)
                     FROM pos.fiscal_report_discount_breakdowns d JOIN pos.controlled_codes classification ON classification.controlled_code_id=d.discount_classification_code_id WHERE d.x_z_report_id=r.x_z_report_id),'[]'::jsonb)::text
            FROM pos.x_z_reports r JOIN pos.controlled_codes kind ON kind.controlled_code_id=r.report_kind_code_id
            JOIN pos.fiscal_reporting_periods period ON period.fiscal_reporting_period_id=r.fiscal_reporting_period_id
            JOIN pos.fiscal_report_requests request ON request.fiscal_report_request_id=r.fiscal_report_request_id
            JOIN pos.controlled_codes request_status ON request_status.controlled_code_id=request.report_status_code_id
            WHERE r.site_pos_server_id=@site AND r.fiscal_identity_id=@identity AND r.currency_code=@currency
              AND kind.code_key IN ('x_reading','z_reading') ORDER BY r.generated_at DESC,r.report_number DESC LIMIT @limit;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("site",site); command.Parameters.AddWithValue("identity",identity); command.Parameters.AddWithValue("currency",currency); command.Parameters.AddWithValue("limit",limit);
        await using var reader=await command.ExecuteReaderAsync(ct).ConfigureAwait(false); var result=new List<FiscalReadingHistoryItem>();
        while(await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var amounts=new FiscalXReadingAmounts(reader.GetInt64(8),reader.GetInt64(9),reader.GetInt64(10),reader.GetInt64(11),reader.GetInt64(12),reader.GetInt64(13),reader.GetInt64(14),reader.GetInt64(15),reader.GetInt64(16),reader.GetInt64(17),reader.GetInt64(18),reader.GetInt64(19),reader.GetInt64(20),reader.GetInt64(21),reader.GetInt64(22),reader.GetInt64(23),reader.GetInt64(24),reader.GetInt64(25));
            result.Add(new(reader.GetString(0),reader.GetString(1).ToUpperInvariant(),reader.GetGuid(2),reader.GetFieldValue<DateOnly>(3),reader.GetFieldValue<DateTimeOffset>(4),reader.GetFieldValue<DateTimeOffset>(5),reader.GetFieldValue<DateTimeOffset>(6),reader.GetInt64(7),amounts,reader.GetString(26),reader.GetInt64(27),reader.GetString(28).ToUpperInvariant(),
                JsonSerializer.Deserialize<FiscalXReadingTenderBreakdown[]>(reader.GetString(29),JsonOptions)??[],
                JsonSerializer.Deserialize<FiscalXReadingDiscountBreakdown[]>(reader.GetString(30),JsonOptions)??[]));
        }
        return result;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
