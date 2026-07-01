using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public static class PostgresFiscalDocumentSql
{
    public const string SelectEligibleFiscalIdentity = """
        select
            history.fiscal_identity_id
        from pos.site_pos_server_fiscal_identity_history history
        inner join pos.site_pos_servers site
            on site.site_pos_server_id = history.site_pos_server_id
        inner join pos.fiscal_identities identity
            on identity.fiscal_identity_id = history.fiscal_identity_id
        left join pos.controlled_codes identity_status
            on identity_status.controlled_code_id = identity.fiscal_identity_status_code_id
        where history.site_pos_server_id = @site_pos_server_id
          and site.is_active = true
          and identity.is_active = true
          and history.effective_start_at <= current_timestamp
          and (history.effective_end_at is null or history.effective_end_at > current_timestamp)
          and (
              identity.fiscal_identity_status_code_id is null
              or (
                  identity_status.is_active = true
                  and (identity_status.effective_start_at is null or identity_status.effective_start_at <= current_timestamp)
                  and (identity_status.effective_end_at is null or identity_status.effective_end_at > current_timestamp)
              )
          )
        order by history.effective_start_at desc, history.fiscal_identity_id
        limit 2;
        """;

    public const string CountFiscalIdentityRelationships = """
        select count(*)
        from pos.site_pos_server_fiscal_identity_history
        where site_pos_server_id = @site_pos_server_id;
        """;

    public const string SelectEligibleFiscalSequencePolicy = """
        select
            policy.fiscal_sequence_policy_id,
            policy.policy_code,
            policy.prefix_text,
            policy.suffix_text,
            policy.padding_length
        from pos.fiscal_sequence_policies policy
        inner join pos.site_pos_servers site
            on site.site_pos_server_id = policy.site_pos_server_id
        inner join pos.controlled_codes policy_status
            on policy_status.controlled_code_id = policy.current_policy_status_code_id
        where policy.site_pos_server_id = @site_pos_server_id
          and site.is_active = true
          and policy.document_type_code_id = @fiscal_document_type_code_id
          and policy.effective_start_at <= current_timestamp
          and (policy.effective_end_at is null or policy.effective_end_at > current_timestamp)
          and policy_status.is_active = true
          and (policy_status.effective_start_at is null or policy_status.effective_start_at <= current_timestamp)
          and (policy_status.effective_end_at is null or policy_status.effective_end_at > current_timestamp)
        order by policy.effective_start_at desc, policy.fiscal_sequence_policy_id
        limit 2;
        """;

    public const string SelectFiscalSequenceStateForUpdate = """
        select
            state.current_sequence_value,
            current_timestamp
        from pos.fiscal_sequence_states state
        inner join pos.controlled_codes sequence_state
            on sequence_state.controlled_code_id = state.sequence_state_code_id
        where state.fiscal_sequence_policy_id = @fiscal_sequence_policy_id
          and sequence_state.is_active = true
          and (sequence_state.effective_start_at is null or sequence_state.effective_start_at <= current_timestamp)
          and (sequence_state.effective_end_at is null or sequence_state.effective_end_at > current_timestamp)
        for update;
        """;

    public const string CountFiscalSequenceStates = """
        select count(*)
        from pos.fiscal_sequence_states
        where fiscal_sequence_policy_id = @fiscal_sequence_policy_id;
        """;

    public const string UpdateFiscalSequenceStateIssued = """
        update pos.fiscal_sequence_states
        set
            current_sequence_value = @fiscal_sequence_value,
            last_reserved_sequence_value = @fiscal_sequence_value,
            last_issued_sequence_value = @fiscal_sequence_value,
            last_transition_at = @fiscal_number_assigned_at,
            updated_at = current_timestamp
        where fiscal_sequence_policy_id = @fiscal_sequence_policy_id;
        """;

    public const string SelectReplayFiscalDocumentNumbering = """
        select
            fiscal_identity_id,
            fiscal_sequence_policy_id,
            fiscal_sequence_value,
            fiscal_document_number,
            fiscal_series,
            fiscal_number_prefix_text,
            fiscal_number_suffix_text,
            fiscal_number_assigned_at,
            fiscal_number_assigned_by_ref
        from pos.fiscal_documents
        where fiscal_document_id = @fiscal_document_id;
        """;

    public const string CountFiscalSequencePolicies = """
        select count(*)
        from pos.fiscal_sequence_policies
        where site_pos_server_id = @site_pos_server_id
          and document_type_code_id = @fiscal_document_type_code_id;
        """;

    public const string InsertIdempotencyRecord = """
        insert into pos.idempotency_records (
            idempotency_record_id,
            idempotency_scope,
            idempotency_key,
            semantic_request_hash,
            operation_type_code_id,
            operation_status_code_id,
            idempotency_context,
            completion_unknown,
            created_at,
            updated_at
        ) values (
            @idempotency_record_id,
            @idempotency_scope,
            @idempotency_key,
            @semantic_request_hash,
            @operation_type_code_id,
            @operation_status_code_id,
            @idempotency_context,
            false,
            current_timestamp,
            current_timestamp
        )
        on conflict (idempotency_scope, idempotency_key) do nothing;
        """;

    public const string SelectIdempotencyRecordForUpdate = """
        select
            semantic_request_hash,
            linked_fiscal_document_id
        from pos.idempotency_records
        where idempotency_scope = @idempotency_scope
          and idempotency_key = @idempotency_key
        for update;
        """;

    public const string UpdateIdempotencyRecordCompleted = """
        update pos.idempotency_records
        set
            linked_fiscal_document_id = @fiscal_document_id,
            replay_result_ref = @replay_result_ref,
            completion_unknown = false,
            updated_at = current_timestamp
        where idempotency_scope = @idempotency_scope
          and idempotency_key = @idempotency_key;
        """;

    public const string UpdateIdempotencyRecordReplay = """
        update pos.idempotency_records
        set
            replay_result_ref = @replay_result_ref,
            updated_at = current_timestamp
        where idempotency_scope = @idempotency_scope
          and idempotency_key = @idempotency_key;
        """;

    public const string InsertFiscalDocument = """
        insert into pos.fiscal_documents (
            fiscal_document_id,
            site_pos_server_id,
            channel_terminal_id,
            fiscal_identity_id,
            fiscal_document_type_code_id,
            fiscal_document_status_code_id,
            fiscal_sequence_policy_id,
            fiscal_sequence_value,
            fiscal_document_number,
            fiscal_series,
            fiscal_number_prefix_text,
            fiscal_number_suffix_text,
            fiscal_number_assigned_at,
            fiscal_number_assigned_by_ref,
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
            @fiscal_identity_id,
            @fiscal_document_type_code_id,
            @fiscal_document_status_code_id,
            @fiscal_sequence_policy_id,
            @fiscal_sequence_value,
            @fiscal_document_number,
            @fiscal_series,
            @fiscal_number_prefix_text,
            @fiscal_number_suffix_text,
            @fiscal_number_assigned_at,
            @fiscal_number_assigned_by_ref,
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

    public const string InsertFiscalDocumentLine = """
        insert into pos.fiscal_document_lines (
            fiscal_document_line_id,
            fiscal_document_id,
            line_sequence,
            line_type_code_id,
            line_status_code_id,
            description,
            quantity,
            unit_amount_minor_units,
            gross_amount_minor_units,
            discount_amount_minor_units,
            tax_amount_minor_units,
            net_amount_minor_units,
            currency_code,
            source_ref,
            line_context,
            is_active,
            created_at,
            updated_at
        ) values (
            @fiscal_document_line_id,
            @fiscal_document_id,
            @line_sequence,
            @line_type_code_id,
            @line_status_code_id,
            @description,
            @quantity,
            @unit_amount_minor_units,
            @gross_amount_minor_units,
            @discount_amount_minor_units,
            @tax_amount_minor_units,
            @net_amount_minor_units,
            @currency_code,
            @source_ref,
            @line_context,
            true,
            current_timestamp,
            current_timestamp
        );
        """;

    public const string InsertFiscalTender = """
        insert into pos.fiscal_tenders (
            fiscal_tender_id,
            fiscal_document_id,
            tender_type_code_id,
            amount_minor_units,
            currency_code,
            central_pms_payment_attempt_ref,
            central_pms_payment_confirmation_ref,
            payment_finality_ref,
            provider_ref,
            tender_context,
            created_at,
            updated_at
        ) values (
            @fiscal_tender_id,
            @fiscal_document_id,
            @tender_type_code_id,
            @amount_minor_units,
            @currency_code,
            @central_pms_payment_attempt_ref,
            @central_pms_payment_confirmation_ref,
            @payment_finality_ref,
            @provider_ref,
            @tender_context,
            current_timestamp,
            current_timestamp
        );
        """;

    public const string InsertFiscalTaxDetail = """
        insert into pos.fiscal_tax_details (
            fiscal_tax_detail_id,
            fiscal_document_id,
            fiscal_document_line_id,
            tax_type_code_id,
            tax_classification_code_id,
            tax_rate,
            taxable_amount_minor_units,
            tax_amount_minor_units,
            currency_code,
            tax_context,
            created_at,
            updated_at
        ) values (
            @fiscal_tax_detail_id,
            @fiscal_document_id,
            @fiscal_document_line_id,
            @tax_type_code_id,
            @tax_classification_code_id,
            @tax_rate,
            @taxable_amount_minor_units,
            @tax_amount_minor_units,
            @currency_code,
            @tax_context,
            current_timestamp,
            current_timestamp
        );
        """;

    public const string InsertFiscalDiscountPrivilegeDetail = """
        insert into pos.fiscal_discount_privilege_details (
            fiscal_discount_privilege_detail_id,
            fiscal_document_id,
            fiscal_document_line_id,
            discount_privilege_type_code_id,
            basis_amount_minor_units,
            discount_amount_minor_units,
            vat_privilege_amount_minor_units,
            currency_code,
            beneficiary_ref,
            evidence_ref,
            approval_ref,
            discount_privilege_context,
            created_at,
            updated_at
        ) values (
            @fiscal_discount_privilege_detail_id,
            @fiscal_document_id,
            @fiscal_document_line_id,
            @discount_privilege_type_code_id,
            @basis_amount_minor_units,
            @discount_amount_minor_units,
            @vat_privilege_amount_minor_units,
            @currency_code,
            @beneficiary_ref,
            @evidence_ref,
            @approval_ref,
            @discount_privilege_context,
            current_timestamp,
            current_timestamp
        );
        """;

    public const string InsertFiscalTotal = """
        insert into pos.fiscal_totals (
            fiscal_total_id,
            fiscal_document_id,
            total_type_code_id,
            amount_minor_units,
            currency_code,
            total_context,
            created_at,
            updated_at
        ) values (
            @fiscal_total_id,
            @fiscal_document_id,
            @total_type_code_id,
            @amount_minor_units,
            @currency_code,
            @total_context,
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
            resolved_fiscal_identity_id = draft.ResolvedFiscalIdentityId,
            resolved_fiscal_sequence_policy_id = draft.ResolvedFiscalSequencePolicyId,
            fiscal_sequence_policy_resolution_posture = "allocated_in_document_transaction",
            fiscal_sequence_value = draft.FiscalSequenceValue,
            fiscal_document_number = draft.FiscalDocumentNumber,
            fiscal_series = draft.FiscalSeries,
            fiscal_number_prefix_text = draft.FiscalNumberPrefixText,
            fiscal_number_suffix_text = draft.FiscalNumberSuffixText,
            fiscal_number_assigned_at = draft.FiscalNumberAssignedAt,
            fiscal_number_assigned_by_ref = draft.FiscalNumberAssignedByRef,
            fiscal_document_links = draft.DocumentLinks.Select(link => new
            {
                target_fiscal_document_id = link.TargetFiscalDocumentId,
                link_type_code_id = link.LinkTypeCodeId,
                link_reason_code_id = link.LinkReasonCodeId,
                link_reason_text = link.LinkReasonText,
                created_by_ref = link.CreatedByRef
            }),
            fiscal_document_lines = draft.DocumentLines.Select(line => new
            {
                line_sequence = line.LineSequence,
                line_type_code_id = line.LineTypeCodeId,
                line_status_code_id = line.LineStatusCodeId,
                source_ref = line.SourceRef
            }),
            fiscal_tenders = draft.Tenders.Select(tender => new
            {
                tender_type_code_id = tender.TenderTypeCodeId,
                amount_minor_units = tender.AmountMinorUnits,
                currency_code = tender.CurrencyCode,
                central_pms_payment_attempt_ref = tender.CentralPmsPaymentAttemptRef,
                central_pms_payment_confirmation_ref = tender.CentralPmsPaymentConfirmationRef,
                payment_finality_ref = tender.PaymentFinalityRef,
                provider_ref = tender.ProviderRef
            }),
            fiscal_tax_details = draft.TaxDetails.Select(taxDetail => new
            {
                line_sequence = taxDetail.LineSequence,
                tax_type_code_id = taxDetail.TaxTypeCodeId,
                tax_classification_code_id = taxDetail.TaxClassificationCodeId,
                tax_rate = taxDetail.TaxRate,
                taxable_amount_minor_units = taxDetail.TaxableAmountMinorUnits,
                tax_amount_minor_units = taxDetail.TaxAmountMinorUnits,
                currency_code = taxDetail.CurrencyCode
            }),
            fiscal_discount_privilege_details = draft.DiscountPrivilegeDetails.Select(discountPrivilegeDetail => new
            {
                line_sequence = discountPrivilegeDetail.LineSequence,
                discount_privilege_type_code_id = discountPrivilegeDetail.DiscountPrivilegeTypeCodeId,
                basis_amount_minor_units = discountPrivilegeDetail.BasisAmountMinorUnits,
                discount_amount_minor_units = discountPrivilegeDetail.DiscountAmountMinorUnits,
                vat_privilege_amount_minor_units = discountPrivilegeDetail.VatPrivilegeAmountMinorUnits,
                currency_code = discountPrivilegeDetail.CurrencyCode,
                beneficiary_ref = discountPrivilegeDetail.BeneficiaryRef,
                evidence_ref = discountPrivilegeDetail.EvidenceRef,
                approval_ref = discountPrivilegeDetail.ApprovalRef
            }),
            fiscal_totals = draft.Totals.Select(total => new
            {
                total_type_code_id = total.TotalTypeCodeId,
                amount_minor_units = total.AmountMinorUnits,
                currency_code = total.CurrencyCode
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

    public static string CreateIdempotencyContextJson(FiscalDocumentDraft draft, FiscalIssuanceIdempotency idempotency)
    {
        var context = new
        {
            source = "pos_server_runtime_fiscal_document_creation",
            posture = "idempotency_with_transactional_fiscal_sequence_allocation",
            idempotency_scope = idempotency.Scope,
            idempotency_key_source = "upstream_finality_ref",
            semantic_request_hash = idempotency.SemanticRequestHash,
            operation_type_code_id_source = "fiscal_document_type_code_id",
            operation_status_code_id_source = "fiscal_document_status_code_id",
            site_pos_server_id = draft.SitePosServerId,
            fiscal_document_type_code_id = draft.FiscalDocumentTypeCodeId,
            payable_basis_ref = draft.PayableBasisRef,
            upstream_finality_ref = draft.UpstreamFinalityRef
        };

        return JsonSerializer.Serialize(context);
    }

    public static string? CreateLineContextJson(FiscalDocumentLineInput line)
    {
        if (line.LineContext is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(line.LineContext);
    }

    public static string? CreateTenderContextJson(FiscalTenderInput tender)
    {
        if (tender.TenderContext is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(tender.TenderContext);
    }

    public static string? CreateTaxContextJson(FiscalTaxDetailInput taxDetail)
    {
        if (taxDetail.TaxContext is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(taxDetail.TaxContext);
    }

    public static string? CreateDiscountPrivilegeContextJson(FiscalDiscountPrivilegeDetailInput discountPrivilegeDetail)
    {
        if (discountPrivilegeDetail.DiscountPrivilegeContext is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(discountPrivilegeDetail.DiscountPrivilegeContext);
    }

    public static string? CreateTotalContextJson(FiscalTotalInput total)
    {
        if (total.TotalContext is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(total.TotalContext);
    }
}
