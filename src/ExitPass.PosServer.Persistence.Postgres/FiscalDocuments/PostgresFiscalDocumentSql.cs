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

    public const string InsertFiscalDocumentStatusHistory = """
        insert into pos.fiscal_document_status_history (
            fiscal_document_status_history_id,
            fiscal_document_id,
            prior_fiscal_document_status_code_id,
            new_fiscal_document_status_code_id,
            status_reason_code_id,
            status_reason_text,
            changed_at,
            actor_ref,
            service_identity_ref,
            created_at
        ) values (
            @fiscal_document_status_history_id,
            @fiscal_document_id,
            null,
            @fiscal_document_status_code_id,
            null,
            null,
            current_timestamp,
            null,
            null,
            current_timestamp
        );
        """;

    public const string InsertFiscalDocumentLink = """
        insert into pos.fiscal_document_links (
            fiscal_document_link_id,
            source_fiscal_document_id,
            target_fiscal_document_id,
            fiscal_document_link_type_code_id,
            link_reason_code_id,
            link_reason_text,
            created_at,
            created_by_ref
        ) values (
            @fiscal_document_link_id,
            @source_fiscal_document_id,
            @target_fiscal_document_id,
            @fiscal_document_link_type_code_id,
            @link_reason_code_id,
            @link_reason_text,
            current_timestamp,
            @created_by_ref
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
            fiscal_document_links = draft.DocumentLinks.Select(link => new
            {
                target_fiscal_document_id = link.TargetFiscalDocumentId,
                link_type_code_id = link.LinkTypeCodeId,
                link_reason_code_id = link.LinkReasonCodeId,
                link_reason_text = link.LinkReasonText,
                created_by_ref = link.CreatedByRef
            }),
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
