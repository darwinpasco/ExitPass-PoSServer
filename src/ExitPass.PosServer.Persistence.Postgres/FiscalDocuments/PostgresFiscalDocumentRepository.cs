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

                foreach (var documentLink in draft.DocumentLinks)
                {
                    await using var linkCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalDocumentLink,
                        connection,
                        transaction);
                    AddDocumentLinkParameters(linkCommand, draft, documentLink);
                    await linkCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                var lineIdsBySequence = new Dictionary<int, Guid>();
                foreach (var documentLine in draft.DocumentLines)
                {
                    var fiscalDocumentLineId = Guid.NewGuid();
                    lineIdsBySequence.Add(documentLine.LineSequence, fiscalDocumentLineId);

                    await using var lineCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalDocumentLine,
                        connection,
                        transaction);
                    AddDocumentLineParameters(lineCommand, draft, documentLine, fiscalDocumentLineId);
                    await lineCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var tender in draft.Tenders)
                {
                    await using var tenderCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalTender,
                        connection,
                        transaction);
                    AddTenderParameters(tenderCommand, draft, tender);
                    await tenderCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var taxDetail in draft.TaxDetails)
                {
                    await using var taxCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalTaxDetail,
                        connection,
                        transaction);
                    AddTaxDetailParameters(taxCommand, draft, taxDetail, lineIdsBySequence);
                    await taxCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var discountPrivilegeDetail in draft.DiscountPrivilegeDetails)
                {
                    await using var discountPrivilegeCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalDiscountPrivilegeDetail,
                        connection,
                        transaction);
                    AddDiscountPrivilegeDetailParameters(
                        discountPrivilegeCommand,
                        draft,
                        discountPrivilegeDetail,
                        lineIdsBySequence);
                    await discountPrivilegeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var total in draft.Totals)
                {
                    await using var totalCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalTotal,
                        connection,
                        transaction);
                    AddTotalParameters(totalCommand, draft, total);
                    await totalCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

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

    private static void AddDocumentLinkParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalDocumentLinkInput link)
    {
        command.Parameters.AddWithValue("fiscal_document_link_id", Guid.NewGuid());
        command.Parameters.AddWithValue("source_fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("target_fiscal_document_id", link.TargetFiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_link_type_code_id", link.LinkTypeCodeId);
        command.Parameters.AddWithValue("link_reason_code_id", (object?)link.LinkReasonCodeId ?? DBNull.Value);
        command.Parameters.AddWithValue("link_reason_text", (object?)link.LinkReasonText ?? DBNull.Value);
        command.Parameters.AddWithValue("created_by_ref", (object?)link.CreatedByRef ?? DBNull.Value);
    }

    private static void AddDocumentLineParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalDocumentLineInput line,
        Guid fiscalDocumentLineId)
    {
        command.Parameters.AddWithValue("fiscal_document_line_id", fiscalDocumentLineId);
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("line_sequence", line.LineSequence);
        command.Parameters.AddWithValue("line_type_code_id", line.LineTypeCodeId);
        command.Parameters.AddWithValue("line_status_code_id", (object?)line.LineStatusCodeId ?? DBNull.Value);
        command.Parameters.AddWithValue("description", line.Description);
        command.Parameters.AddWithValue("quantity", line.Quantity);
        command.Parameters.AddWithValue("unit_amount_minor_units", line.UnitAmountMinorUnits);
        command.Parameters.AddWithValue("gross_amount_minor_units", line.GrossAmountMinorUnits);
        command.Parameters.AddWithValue("discount_amount_minor_units", line.DiscountAmountMinorUnits);
        command.Parameters.AddWithValue("tax_amount_minor_units", line.TaxAmountMinorUnits);
        command.Parameters.AddWithValue("net_amount_minor_units", line.NetAmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", line.CurrencyCode);
        command.Parameters.AddWithValue("source_ref", (object?)line.SourceRef ?? DBNull.Value);

        var lineContextParameter = command.Parameters.Add("line_context", NpgsqlDbType.Jsonb);
        lineContextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateLineContextJson(line) ?? DBNull.Value;
    }

    private static void AddTenderParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalTenderInput tender)
    {
        command.Parameters.AddWithValue("fiscal_tender_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("tender_type_code_id", tender.TenderTypeCodeId);
        command.Parameters.AddWithValue("amount_minor_units", tender.AmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", tender.CurrencyCode);
        command.Parameters.AddWithValue("central_pms_payment_attempt_ref", (object?)tender.CentralPmsPaymentAttemptRef ?? DBNull.Value);
        command.Parameters.AddWithValue("central_pms_payment_confirmation_ref", (object?)tender.CentralPmsPaymentConfirmationRef ?? DBNull.Value);
        command.Parameters.AddWithValue("payment_finality_ref", (object?)tender.PaymentFinalityRef ?? DBNull.Value);
        command.Parameters.AddWithValue("provider_ref", (object?)tender.ProviderRef ?? DBNull.Value);

        var tenderContextParameter = command.Parameters.Add("tender_context", NpgsqlDbType.Jsonb);
        tenderContextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateTenderContextJson(tender) ?? DBNull.Value;
    }

    private static void AddTaxDetailParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalTaxDetailInput taxDetail,
        IReadOnlyDictionary<int, Guid> lineIdsBySequence)
    {
        Guid? fiscalDocumentLineId = null;
        if (taxDetail.LineSequence is not null)
        {
            fiscalDocumentLineId = lineIdsBySequence[taxDetail.LineSequence.Value];
        }

        command.Parameters.AddWithValue("fiscal_tax_detail_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_line_id", (object?)fiscalDocumentLineId ?? DBNull.Value);
        command.Parameters.AddWithValue("tax_type_code_id", taxDetail.TaxTypeCodeId);
        command.Parameters.AddWithValue("tax_classification_code_id", taxDetail.TaxClassificationCodeId);
        command.Parameters.AddWithValue("tax_rate", (object?)taxDetail.TaxRate ?? DBNull.Value);
        command.Parameters.AddWithValue("taxable_amount_minor_units", taxDetail.TaxableAmountMinorUnits);
        command.Parameters.AddWithValue("tax_amount_minor_units", taxDetail.TaxAmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", taxDetail.CurrencyCode);

        var taxContextParameter = command.Parameters.Add("tax_context", NpgsqlDbType.Jsonb);
        taxContextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateTaxContextJson(taxDetail) ?? DBNull.Value;
    }

    private static void AddDiscountPrivilegeDetailParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalDiscountPrivilegeDetailInput discountPrivilegeDetail,
        IReadOnlyDictionary<int, Guid> lineIdsBySequence)
    {
        Guid? fiscalDocumentLineId = null;
        if (discountPrivilegeDetail.LineSequence is not null)
        {
            fiscalDocumentLineId = lineIdsBySequence[discountPrivilegeDetail.LineSequence.Value];
        }

        command.Parameters.AddWithValue("fiscal_discount_privilege_detail_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_line_id", (object?)fiscalDocumentLineId ?? DBNull.Value);
        command.Parameters.AddWithValue("discount_privilege_type_code_id", discountPrivilegeDetail.DiscountPrivilegeTypeCodeId);
        command.Parameters.AddWithValue("basis_amount_minor_units", discountPrivilegeDetail.BasisAmountMinorUnits);
        command.Parameters.AddWithValue("discount_amount_minor_units", discountPrivilegeDetail.DiscountAmountMinorUnits);
        command.Parameters.AddWithValue("vat_privilege_amount_minor_units", discountPrivilegeDetail.VatPrivilegeAmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", discountPrivilegeDetail.CurrencyCode);
        command.Parameters.AddWithValue("beneficiary_ref", (object?)discountPrivilegeDetail.BeneficiaryRef ?? DBNull.Value);
        command.Parameters.AddWithValue("evidence_ref", (object?)discountPrivilegeDetail.EvidenceRef ?? DBNull.Value);
        command.Parameters.AddWithValue("approval_ref", (object?)discountPrivilegeDetail.ApprovalRef ?? DBNull.Value);

        var contextParameter = command.Parameters.Add("discount_privilege_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateDiscountPrivilegeContextJson(discountPrivilegeDetail) ?? DBNull.Value;
    }

    private static void AddTotalParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalTotalInput total)
    {
        command.Parameters.AddWithValue("fiscal_total_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("total_type_code_id", total.TotalTypeCodeId);
        command.Parameters.AddWithValue("amount_minor_units", total.AmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", total.CurrencyCode);

        var contextParameter = command.Parameters.Add("total_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateTotalContextJson(total) ?? DBNull.Value;
    }
}
