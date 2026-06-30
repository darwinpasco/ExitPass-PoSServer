using ExitPass.PosServer.Runtime.FiscalDocuments;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public sealed class PostgresFiscalDocumentRepository : IFiscalDocumentRepository
{
    private readonly NpgsqlDataSource dataSource;

    public PostgresFiscalDocumentRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<FiscalDocumentDraft> CreateAsync(FiscalDocumentDraft draft, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await using var documentCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.InsertFiscalDocument,
                    connection,
                    transaction);
                AddFiscalDocumentParameters(documentCommand, draft);
                await documentCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var statusHistoryCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory,
                    connection,
                    transaction);
                AddStatusHistoryParameters(statusHistoryCommand, draft);
                await statusHistoryCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }

            return draft;
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            throw new FiscalDocumentPersistenceException(
                "Fiscal document persistence write failed.",
                ex);
        }
    }

    private static void AddFiscalDocumentParameters(NpgsqlCommand command, FiscalDocumentDraft draft)
    {
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("site_pos_server_id", draft.SitePosServerId);
        command.Parameters.AddWithValue("channel_terminal_id", (object?)draft.ChannelTerminalId ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_document_type_code_id", draft.FiscalDocumentTypeCodeId);
        command.Parameters.AddWithValue("fiscal_document_status_code_id", draft.FiscalDocumentStatusCodeId);
        command.Parameters.AddWithValue("central_pms_parking_session_ref", (object?)draft.CentralPmsParkingSessionRef ?? DBNull.Value);
        command.Parameters.AddWithValue("central_pms_payment_attempt_ref", (object?)draft.CentralPmsPaymentAttemptRef ?? DBNull.Value);
        command.Parameters.AddWithValue("central_pms_payment_confirmation_ref", (object?)draft.CentralPmsPaymentConfirmationRef ?? DBNull.Value);
        command.Parameters.AddWithValue("payment_finality_ref", (object?)draft.PaymentFinalityRef ?? DBNull.Value);
        command.Parameters.AddWithValue("vendor_ack_ref", (object?)draft.VendorAckRef ?? DBNull.Value);
        command.Parameters.AddWithValue("business_day_date", (object?)draft.BusinessDayDate ?? DBNull.Value);

        var contextParameter = command.Parameters.Add("document_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = PostgresFiscalDocumentSql.CreateDocumentContextJson(draft);
    }

    private static void AddStatusHistoryParameters(NpgsqlCommand command, FiscalDocumentDraft draft)
    {
        command.Parameters.AddWithValue("fiscal_document_status_history_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_status_code_id", draft.FiscalDocumentStatusCodeId);
    }
}
