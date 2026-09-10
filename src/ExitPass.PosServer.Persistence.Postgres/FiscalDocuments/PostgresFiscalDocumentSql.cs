using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public static class PostgresFiscalDocumentSql
{
    public const string SelectEffectiveSalesInvoiceHeaderProfileForUpdate = """
        select
            profile.sales_invoice_header_profile_id,
            profile.fiscal_identity_id,
            profile.site_id,
            profile.site_pos_server_id,
            profile.profile_version,
            profile.template_version,
            profile.presentation_version,
            profile.pos_serial_number,
            profile.machine_identification_number,
            profile.parking_location_display,
            profile.bir_accreditation_number,
            profile.bir_accreditation_issued_date,
            profile.bir_accreditation_valid_until,
            profile.ptu_number,
            profile.ptu_issued_date,
            profile.sales_invoice_legal_statement,
            profile.customer_service_footer,
            profile.effective_from,
            profile.effective_to,
            profile.lifecycle_status,
            profile.approved_at,
            profile.approved_by_ref,
            profile.retired_at,
            profile.created_at,
            profile.updated_at,
            profile.created_by_ref,
            profile.updated_by_ref,
            identity.registered_business_name,
            identity.registered_business_address,
            identity.tin,
            identity.taxpayer_classification,
            identity.fiscal_identity_status,
            identity.created_at,
            identity.updated_at,
            identity.created_by_ref,
            identity.updated_by_ref,
            profile.supplier_developer_registered_name,
            profile.supplier_developer_address,
            profile.supplier_developer_tin
        from pos.sales_invoice_header_profiles profile
        inner join pos.fiscal_identities identity
            on identity.fiscal_identity_id = profile.fiscal_identity_id
        where profile.site_pos_server_id = @site_pos_server_id
          and (@site_id::uuid is null or profile.site_id = @site_id::uuid)
          and profile.lifecycle_status = 'APPROVED'
          and profile.retired_at is null
          and profile.effective_from <= @effective_at
          and (profile.effective_to is null or profile.effective_to > @effective_at)
        order by profile.effective_from, profile.sales_invoice_header_profile_id
        limit 2
        for update of profile;
        """;

    public const string InsertFiscalDocumentHeaderSnapshot = """
        insert into pos.fiscal_document_header_snapshots (
            fiscal_document_header_snapshot_id,
            fiscal_document_id,
            fiscal_identity_id,
            sales_invoice_header_profile_id,
            profile_version,
            registered_business_name,
            registered_business_address,
            tin,
            pos_serial_number,
            machine_identification_number,
            parking_location_display,
            terminal_id,
            bir_accreditation_number,
            bir_accreditation_issued_date,
            bir_accreditation_valid_until,
            ptu_number,
            ptu_issued_date,
            sales_invoice_legal_statement,
            customer_service_footer,
            supplier_developer_registered_name,
            supplier_developer_address,
            supplier_developer_tin,
            template_version,
            presentation_version,
            effective_at,
            snapshot_created_at,
            snapshot_json,
            created_at
        ) values (
            @fiscal_document_header_snapshot_id,
            @fiscal_document_id,
            @fiscal_identity_id,
            @sales_invoice_header_profile_id,
            @profile_version,
            @registered_business_name,
            @registered_business_address,
            @tin,
            @pos_serial_number,
            @machine_identification_number,
            @parking_location_display,
            @terminal_id,
            @bir_accreditation_number,
            @bir_accreditation_issued_date,
            @bir_accreditation_valid_until,
            @ptu_number,
            @ptu_issued_date,
            @sales_invoice_legal_statement,
            @customer_service_footer,
            @supplier_developer_registered_name,
            @supplier_developer_address,
            @supplier_developer_tin,
            @template_version,
            @presentation_version,
            @effective_at,
            @snapshot_created_at,
            @snapshot_json,
            current_timestamp
        );
        """;

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
            fiscal_number_assigned_by_ref,
            journal.event_reference
        from pos.fiscal_documents document
        left join lateral (
            select event_reference
            from pos.electronic_journal_records
            where fiscal_document_id = document.fiscal_document_id
              and is_canonical
            order by stream_sequence_value desc, electronic_journal_record_id
            limit 1
        ) journal on true
        where document.fiscal_document_id = @fiscal_document_id;
        """;

    public const string SelectReplayFiscalDocumentHeaderSnapshot = """
        select
            fiscal_identity_id,
            sales_invoice_header_profile_id,
            profile_version,
            registered_business_name,
            registered_business_address,
            tin,
            pos_serial_number,
            machine_identification_number,
            parking_location_display,
            terminal_id,
            bir_accreditation_number,
            bir_accreditation_issued_date,
            bir_accreditation_valid_until,
            ptu_number,
            ptu_issued_date,
            sales_invoice_legal_statement,
            customer_service_footer,
            template_version,
            presentation_version,
            effective_at,
            snapshot_created_at,
            supplier_developer_registered_name,
            supplier_developer_address,
            supplier_developer_tin
        from pos.fiscal_document_header_snapshots
        where fiscal_document_id = @fiscal_document_id;
        """;

    public const string SelectAppliedStatutoryFiscalFacts = """
        select
            facts.statutory_discount_decision_command_id,
            facts.statutory_request_reference,
            facts.statutory_payable_basis_application_command_id,
            facts.statutory_validation_id,
            facts.parking_session_id,
            facts.site_id,
            facts.site_group_id,
            entitlement.code_key,
            benefit.code_key,
            policy_basis.code_key,
            facts.applied_policy_reference_id,
            facts.policy_code,
            facts.policy_version_id,
            facts.national_law_reference,
            facts.ordinance_reference,
            facts.original_tariff_snapshot_id,
            facts.applied_tariff_snapshot_id,
            facts.original_amount_minor_units,
            facts.vat_exclusive_basis_amount_minor_units,
            facts.vat_amount_minor_units,
            vat_treatment.code_key,
            facts.statutory_discount_amount_minor_units,
            facts.final_payable_amount_minor_units,
            facts.currency_code,
            facts.applied_at,
            source_channel.code_key,
            facts.terminal_cash_tender_id,
            facts.snapshot_created_at
        from pos.fiscal_document_applied_statutory_facts facts
        inner join pos.controlled_codes entitlement
            on entitlement.controlled_code_id = facts.entitlement_type_code_id
        inner join pos.controlled_codes benefit
            on benefit.controlled_code_id = facts.benefit_classification_code_id
        inner join pos.controlled_codes policy_basis
            on policy_basis.controlled_code_id = facts.policy_resolution_basis_code_id
        inner join pos.controlled_codes vat_treatment
            on vat_treatment.controlled_code_id = facts.vat_treatment_code_id
        inner join pos.controlled_codes source_channel
            on source_channel.controlled_code_id = facts.source_payment_channel_code_id
        where facts.fiscal_document_id = @fiscal_document_id;
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

    public const string SelectFiscalDocumentForVoidUpdate = """
        select
            document.fiscal_document_id,
            document.fiscal_document_number,
            document.fiscal_sequence_value,
            document.fiscal_document_type_code_id,
            document.fiscal_document_status_code_id,
            current_status.code_key,
            current_status.controlled_code_set_id,
            document.void_status,
            document.void_reason_code,
            document.void_reason_text,
            document.void_requested_by_ref,
            document.voided_at,
            document.void_idempotency_key,
            document.void_semantic_request_hash,
            document.void_correlation_id,
            document.site_pos_server_id,
            document.fiscal_identity_id,
            document.currency_code,
            document.fiscal_reporting_period_id
        from pos.fiscal_documents document
        inner join pos.controlled_codes current_status
            on current_status.controlled_code_id = document.fiscal_document_status_code_id
        where document.fiscal_document_id = @fiscal_document_id
          and document.is_active = true
        for update of document;
        """;

    public const string SelectFiscalDocumentReportingScope = """
        select
            site_pos_server_id,
            fiscal_identity_id,
            currency_code,
            fiscal_reporting_period_id
        from pos.fiscal_documents
        where fiscal_document_id = @fiscal_document_id
          and is_active = true;
        """;

    public const string SelectVoidFiscalDocumentStatusCode = """
        select controlled_code_id
        from pos.controlled_codes
        where controlled_code_set_id = @controlled_code_set_id
          and code_key = 'voided'
          and is_active = true
          and (effective_start_at is null or effective_start_at <= current_timestamp)
          and (effective_end_at is null or effective_end_at > current_timestamp)
        order by controlled_code_id
        limit 2;
        """;

    public const string SelectActiveControlledCodeIdBySetAndCode = """
        select code.controlled_code_id
        from pos.controlled_codes code
        inner join pos.controlled_code_sets code_set
            on code_set.controlled_code_set_id = code.controlled_code_set_id
        where code_set.code_set_key = @code_set_key
          and code.code_key = @code_key
          and code_set.is_active = true
          and code.is_active = true
          and (code_set.effective_start_at is null or code_set.effective_start_at <= current_timestamp)
          and (code_set.effective_end_at is null or code_set.effective_end_at > current_timestamp)
          and (code.effective_start_at is null or code.effective_start_at <= current_timestamp)
          and (code.effective_end_at is null or code.effective_end_at > current_timestamp)
        order by code.controlled_code_id
        limit 2;
        """;

    public const string InsertAppliedStatutoryFiscalFacts = """
        insert into pos.fiscal_document_applied_statutory_facts (
            fiscal_document_applied_statutory_fact_id,
            fiscal_document_id,
            statutory_discount_decision_command_id,
            statutory_request_reference,
            statutory_payable_basis_application_command_id,
            statutory_validation_id,
            parking_session_id,
            site_id,
            site_group_id,
            entitlement_type_code_id,
            benefit_classification_code_id,
            policy_resolution_basis_code_id,
            applied_policy_reference_id,
            policy_code,
            policy_version_id,
            national_law_reference,
            ordinance_reference,
            original_tariff_snapshot_id,
            applied_tariff_snapshot_id,
            original_amount_minor_units,
            vat_exclusive_basis_amount_minor_units,
            vat_amount_minor_units,
            vat_treatment_code_id,
            statutory_discount_amount_minor_units,
            final_payable_amount_minor_units,
            currency_code,
            applied_at,
            source_payment_channel_code_id,
            terminal_cash_tender_id,
            snapshot_created_at,
            created_at,
            updated_at
        ) values (
            @fiscal_document_applied_statutory_fact_id,
            @fiscal_document_id,
            @statutory_discount_decision_command_id,
            @statutory_request_reference,
            @statutory_payable_basis_application_command_id,
            @statutory_validation_id,
            @parking_session_id,
            @site_id,
            @site_group_id,
            @entitlement_type_code_id,
            @benefit_classification_code_id,
            @policy_resolution_basis_code_id,
            @applied_policy_reference_id,
            @policy_code,
            @policy_version_id,
            @national_law_reference,
            @ordinance_reference,
            @original_tariff_snapshot_id,
            @applied_tariff_snapshot_id,
            @original_amount_minor_units,
            @vat_exclusive_basis_amount_minor_units,
            @vat_amount_minor_units,
            @vat_treatment_code_id,
            @statutory_discount_amount_minor_units,
            @final_payable_amount_minor_units,
            @currency_code,
            @applied_at,
            @source_payment_channel_code_id,
            @terminal_cash_tender_id,
            @snapshot_created_at,
            @snapshot_created_at,
            @snapshot_created_at
        );
        """;

    public const string UpdateFiscalDocumentVoided = """
        update pos.fiscal_documents
        set
            fiscal_document_status_code_id = @voided_fiscal_document_status_code_id,
            void_status = 'recorded',
            void_reason_code = @void_reason_code,
            void_reason_text = @void_reason_text,
            void_requested_by_ref = @void_requested_by_ref,
            void_requested_at = @void_requested_at,
            voided_at = @voided_at,
            void_idempotency_key = @void_idempotency_key,
            void_semantic_request_hash = @void_semantic_request_hash,
            void_correlation_id = @void_correlation_id,
            void_source_system_ref = @void_source_system_ref,
            void_business_day_date = @void_business_day_date,
            updated_at = current_timestamp
        where fiscal_document_id = @fiscal_document_id;
        """;

    public const string InsertFiscalDocumentVoidStatusHistory = """
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
            @prior_fiscal_document_status_code_id,
            @voided_fiscal_document_status_code_id,
            null,
            @void_reason_text,
            @voided_at,
            @void_requested_by_ref,
            'pos-server:fiscal-document-void-runtime',
            current_timestamp
        );
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
            completion_basis,
            completion_authority_ref,
            vendor_ack_ref,
            business_day_date,
            currency_code,
            fiscal_reporting_period_id,
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
            @completion_basis,
            @completion_authority_ref,
            @vendor_ack_ref,
            @business_day_date,
            @currency_code,
            @fiscal_reporting_period_id,
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
            site_id = draft.SiteId,
            fiscal_document_type_code_key = draft.FiscalDocumentTypeCodeKey,
            runtime_terminal_ref = draft.RuntimeTerminalRef,
            payable_basis_ref = draft.PayableBasisRef,
            upstream_finality_ref = draft.UpstreamFinalityRef,
            completion_basis = draft.CompletionBasis,
            completion_authority_ref = draft.CompletionAuthorityRef,
            reference_context = draft.ReferenceContext,
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
            sales_invoice_header_snapshot = draft.SalesInvoiceHeaderSnapshot,
            invoice_customer_information = draft.InvoiceCustomerInformation is null ? null : new
            {
                customer_name = draft.InvoiceCustomerInformation.CustomerName,
                address = draft.InvoiceCustomerInformation.Address,
                tin = draft.InvoiceCustomerInformation.Tin,
                business_style = draft.InvoiceCustomerInformation.BusinessStyle,
                statutory_id_number = draft.InvoiceCustomerInformation.StatutoryIdNumber
            },
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
            semantic_request_hash_version = FiscalDocumentSemanticRequestHasher.GetVersion(draft),
            semantic_request_hash_status = FiscalDocumentSemanticRequestHasher.Status,
            operation_type_code_id_source = "fiscal_document_type_code_id",
            operation_status_code_id_source = "fiscal_document_status_code_id",
            site_pos_server_id = draft.SitePosServerId,
            fiscal_document_type_code_id = draft.FiscalDocumentTypeCodeId,
            payable_basis_ref = draft.PayableBasisRef,
            upstream_finality_ref = draft.UpstreamFinalityRef
        };

        return JsonSerializer.Serialize(context);
    }

    public static string CreateSalesInvoiceHeaderSnapshotJson(SalesInvoiceHeaderSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot);
    }

    public static string CreateVoidIdempotencyContextJson(FiscalDocumentVoidCommand command, FiscalDocumentVoidIdempotency idempotency)
    {
        var context = new
        {
            source = "pos_server_runtime_fiscal_document_void",
            posture = "idempotency_with_fiscal_document_status_transition_only",
            idempotency_scope = idempotency.Scope,
            idempotency_key_source = "void_request_idempotency_key",
            semantic_request_hash = idempotency.SemanticRequestHash,
            semantic_request_hash_version = FiscalDocumentVoidSemanticRequestHasher.Version,
            semantic_request_hash_status = FiscalDocumentVoidSemanticRequestHasher.Status,
            fiscal_document_id = command.FiscalDocumentId,
            reason_code = command.ReasonCode,
            requested_by_ref = command.RequestedByRef,
            correlation_id = command.CorrelationId,
            source_system_ref = command.SourceSystemRef,
            business_day_date = command.BusinessDayDate
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
