using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public static class PostgresFiscalDocumentSql
{
    public const string InsertFiscalDocument = """
        insert into pos.fiscal_documents (
            fiscal_document_id,
            site_pos_server_id,
            channel_terminal_id,
            fiscal_document_type_code_id,
            fiscal_document_status_code_id,
            central_pms_parking_session_ref,
            central_pms_payment_attempt_ref,
            central_pms_payment_confirmation_ref,
            payment_finality_ref,
            vendor_ack_ref,
            business_day_date,
            document_context,
            is_active,
            created_at,
            updated_at
        ) values (
            @fiscal_document_id,
            @site_pos_server_id,
            @channel_terminal_id,
            @fiscal_document_type_code_id,
            @fiscal_document_status_code_id,
            @central_pms_parking_session_ref,
            @central_pms_payment_attempt_ref,
            @central_pms_payment_confirmation_ref,
            @payment_finality_ref,
            @vendor_ack_ref,
            @business_day_date,
            @document_context,
            true,
            current_timestamp,
            current_timestamp
        );
        """;

    public static string CreateDocumentContextJson(FiscalDocumentDraft draft)
    {
        var context = new
        {
            site_pos_server_ref = draft.SitePosServerRef,
            fiscal_document_type_code_key = draft.FiscalDocumentTypeCodeKey,
            payable_basis_ref = draft.PayableBasisRef,
            upstream_finality_ref = draft.UpstreamFinalityRef,
            currency_code = draft.CurrencyCode,
            payable_amount_minor_units = draft.PayableAmountMinorUnits,
            discount_references = draft.DiscountReferences.Select(discount => new
            {
                discount_validation_ref = discount.DiscountValidationRef,
                status = discount.Status.ToString(),
                applies_statutory_discount_treatment = discount.AppliesStatutoryDiscountTreatment,
                reference_context = discount.ReferenceContext
            })
        };

        return JsonSerializer.Serialize(context);
    }
}
