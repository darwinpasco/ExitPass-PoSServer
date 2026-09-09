-- ExitPass POS Server Slice 2 table artifact.
-- Fiscal document header/core posture with nullable fiscal numbering persistence fields.
-- Fiscal numbers are allocated by the runtime repository inside the fiscal document transaction.
-- This table does not own payment finality or ExitAuthorization.

CREATE TABLE IF NOT EXISTS pos.fiscal_documents (
    fiscal_document_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    channel_terminal_id uuid NULL,
    fiscal_identity_id uuid NULL,
    currency_code char(3) NULL,
    fiscal_reporting_period_id uuid NULL,
    fiscal_document_type_code_id uuid NOT NULL,
    fiscal_document_status_code_id uuid NOT NULL,
    fiscal_sequence_policy_id uuid NULL,
    fiscal_sequence_value bigint NULL,
    fiscal_document_number text NULL,
    fiscal_series text NULL,
    fiscal_number_prefix_text text NULL,
    fiscal_number_suffix_text text NULL,
    fiscal_number_assigned_at timestamptz NULL,
    fiscal_number_assigned_by_ref text NULL,
    central_pms_parking_session_ref text NULL,
    central_pms_payment_attempt_ref text NULL,
    central_pms_payment_confirmation_ref text NULL,
    payment_finality_ref text NULL,
    completion_basis varchar(64) NOT NULL DEFAULT 'PAYMENT_FINALITY',
    completion_authority_ref text NULL,
    vendor_ack_ref text NULL,
    business_day_date date NULL,
    void_status text NULL,
    void_reason_code text NULL,
    void_reason_text text NULL,
    void_requested_by_ref text NULL,
    void_requested_at timestamptz NULL,
    voided_at timestamptz NULL,
    void_idempotency_key text NULL,
    void_semantic_request_hash text NULL,
    void_correlation_id text NULL,
    void_source_system_ref text NULL,
    void_business_day_date date NULL,
    document_context jsonb NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_documents PRIMARY KEY (fiscal_document_id),
    CONSTRAINT fk_fiscal_documents__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_documents__channel_terminal FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT fk_fiscal_documents__fiscal_identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_fiscal_documents__reporting_period_scope FOREIGN KEY (
        fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code
    ) REFERENCES pos.fiscal_reporting_periods (
        fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code
    ),
    CONSTRAINT fk_fiscal_documents__doc_type_code FOREIGN KEY (fiscal_document_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_documents__doc_status_code FOREIGN KEY (fiscal_document_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_documents__sequence_policy FOREIGN KEY (fiscal_sequence_policy_id)
        REFERENCES pos.fiscal_sequence_policies (fiscal_sequence_policy_id),
    CONSTRAINT ck_fiscal_documents__sequence_value CHECK (
        fiscal_sequence_value IS NULL OR fiscal_sequence_value > 0
    ),
    CONSTRAINT ck_fiscal_documents__currency CHECK (
        currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$'
    ),
    CONSTRAINT ck_fiscal_documents__document_number CHECK (
        fiscal_document_number IS NULL OR char_length(btrim(fiscal_document_number)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__fiscal_series CHECK (
        fiscal_series IS NULL OR char_length(btrim(fiscal_series)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__number_prefix CHECK (
        fiscal_number_prefix_text IS NULL OR char_length(btrim(fiscal_number_prefix_text)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__number_suffix CHECK (
        fiscal_number_suffix_text IS NULL OR char_length(btrim(fiscal_number_suffix_text)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__number_assigned_by CHECK (
        fiscal_number_assigned_by_ref IS NULL OR char_length(btrim(fiscal_number_assigned_by_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__number_assignment CHECK (
        (
            fiscal_sequence_policy_id IS NULL
            AND fiscal_sequence_value IS NULL
            AND fiscal_document_number IS NULL
            AND fiscal_number_assigned_at IS NULL
        )
        OR
        (
            fiscal_sequence_policy_id IS NOT NULL
            AND fiscal_sequence_value IS NOT NULL
            AND fiscal_document_number IS NOT NULL
            AND fiscal_number_assigned_at IS NOT NULL
        )
    ),
    CONSTRAINT ck_fiscal_documents__parking_session_ref CHECK (
        central_pms_parking_session_ref IS NULL OR char_length(btrim(central_pms_parking_session_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__payment_attempt_ref CHECK (
        central_pms_payment_attempt_ref IS NULL OR char_length(btrim(central_pms_payment_attempt_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__payment_confirmation_ref CHECK (
        central_pms_payment_confirmation_ref IS NULL OR char_length(btrim(central_pms_payment_confirmation_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__payment_finality_ref CHECK (
        payment_finality_ref IS NULL OR char_length(btrim(payment_finality_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__vendor_ack_ref CHECK (
        vendor_ack_ref IS NULL OR char_length(btrim(vendor_ack_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_status CHECK (
        void_status IS NULL OR char_length(btrim(void_status)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_reason_code CHECK (
        void_reason_code IS NULL OR char_length(btrim(void_reason_code)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_reason_text CHECK (
        void_reason_text IS NULL OR char_length(btrim(void_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_requested_by_ref CHECK (
        void_requested_by_ref IS NULL OR char_length(btrim(void_requested_by_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_idempotency_key CHECK (
        void_idempotency_key IS NULL OR char_length(btrim(void_idempotency_key)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_semantic_hash CHECK (
        void_semantic_request_hash IS NULL OR char_length(btrim(void_semantic_request_hash)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_correlation_id CHECK (
        void_correlation_id IS NULL OR char_length(btrim(void_correlation_id)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_source_system_ref CHECK (
        void_source_system_ref IS NULL OR char_length(btrim(void_source_system_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_documents__void_record_consistency CHECK (
        (
            void_status IS NULL
            AND void_reason_code IS NULL
            AND void_requested_by_ref IS NULL
            AND void_requested_at IS NULL
            AND voided_at IS NULL
            AND void_idempotency_key IS NULL
            AND void_semantic_request_hash IS NULL
            AND void_correlation_id IS NULL
        )
        OR
        (
            void_status IS NOT NULL
            AND void_reason_code IS NOT NULL
            AND void_requested_by_ref IS NOT NULL
            AND void_requested_at IS NOT NULL
            AND voided_at IS NOT NULL
            AND void_idempotency_key IS NOT NULL
            AND void_semantic_request_hash IS NOT NULL
            AND void_correlation_id IS NOT NULL
        )
    ),
    CONSTRAINT ck_fiscal_documents__document_context_object CHECK (
        document_context IS NULL OR jsonb_typeof(document_context) = 'object'
    )
);

-- Existing state-based environments need additive columns before this file's
-- comments run; referential assignment enforcement is applied later in order.
ALTER TABLE pos.fiscal_documents
    ADD COLUMN IF NOT EXISTS currency_code char(3) NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS completion_basis varchar(64),
    ADD COLUMN IF NOT EXISTS completion_authority_ref text NULL;

UPDATE pos.fiscal_documents
SET completion_basis = COALESCE(completion_basis, 'PAYMENT_FINALITY'),
    completion_authority_ref = COALESCE(
        completion_authority_ref,
        central_pms_payment_confirmation_ref,
        payment_finality_ref,
        document_context ->> 'upstream_finality_ref')
WHERE completion_basis IS NULL OR completion_authority_ref IS NULL;

ALTER TABLE pos.fiscal_documents
    ALTER COLUMN completion_basis SET NOT NULL;

ALTER TABLE pos.fiscal_documents
    DROP CONSTRAINT IF EXISTS ck_fiscal_documents__completion_basis,
    DROP CONSTRAINT IF EXISTS ck_fiscal_documents__completion_ancestry;

ALTER TABLE pos.fiscal_documents
    ADD CONSTRAINT ck_fiscal_documents__completion_basis CHECK (
        completion_basis IN ('PAYMENT_FINALITY', 'ZERO_PAYABLE_STATUTORY_FINALITY')
    ),
    ADD CONSTRAINT ck_fiscal_documents__completion_ancestry CHECK (
        completion_authority_ref IS NOT NULL
        AND btrim(completion_authority_ref) <> ''
        AND (
            (
                completion_basis = 'PAYMENT_FINALITY'
                AND payment_finality_ref IS NOT NULL
                AND btrim(payment_finality_ref) <> ''
            ) OR (
                completion_basis = 'ZERO_PAYABLE_STATUTORY_FINALITY'
                AND central_pms_payment_attempt_ref IS NULL
                AND central_pms_payment_confirmation_ref IS NULL
                AND payment_finality_ref IS NULL
            )
        )
    );

CREATE UNIQUE INDEX IF NOT EXISTS ux_fiscal_documents__seq_policy_value
    ON pos.fiscal_documents (fiscal_sequence_policy_id, fiscal_sequence_value)
    WHERE fiscal_sequence_policy_id IS NOT NULL
      AND fiscal_sequence_value IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_fiscal_documents__seq_policy_number
    ON pos.fiscal_documents (fiscal_sequence_policy_id, fiscal_document_number)
    WHERE fiscal_sequence_policy_id IS NOT NULL
      AND fiscal_document_number IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_fiscal_documents__fiscal_identity
    ON pos.fiscal_documents (fiscal_identity_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_documents__seq_policy
    ON pos.fiscal_documents (fiscal_sequence_policy_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_documents__document_number
    ON pos.fiscal_documents (fiscal_document_number);

COMMENT ON TABLE pos.fiscal_documents IS 'Fiscal document header/core posture with fiscal numbering fields allocated by runtime when prerequisites are satisfied. Does not own payment finality or ExitAuthorization.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_identity_id IS 'Fiscal identity reference selected by runtime fiscal numbering. Nullable only for historical/unassigned shell posture.';
COMMENT ON COLUMN pos.fiscal_documents.currency_code IS 'First-class fiscal document currency used by reporting-period scope enforcement.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_reporting_period_id IS 'Immutable governed reporting-period assignment. Nullable only for explicit legacy upgrade inventory.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_sequence_policy_id IS 'Fiscal sequence policy reference selected by runtime fiscal numbering. Nullable only for historical/unassigned shell posture.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_sequence_value IS 'Allocated fiscal sequence value. Must be positive when present.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_document_number IS 'Optional formatted fiscal document number. Authoritative fiscal number storage must use this column, not document_context.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_series IS 'Optional fiscal series or book/register reference copied at assignment time when approved.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_number_prefix_text IS 'Optional prefix copied from the sequence policy at fiscal number assignment time.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_number_suffix_text IS 'Optional suffix copied from the sequence policy at fiscal number assignment time.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_number_assigned_at IS 'Timestamp for durable fiscal number assignment when allocated by runtime.';
COMMENT ON COLUMN pos.fiscal_documents.fiscal_number_assigned_by_ref IS 'Service or actor reference for fiscal number assignment; reference only.';
COMMENT ON COLUMN pos.fiscal_documents.central_pms_parking_session_ref IS 'Reference to Central PMS parking session context; POS Server does not own parking session lifecycle.';
COMMENT ON COLUMN pos.fiscal_documents.central_pms_payment_attempt_ref IS 'Reference to Central PMS PaymentAttempt context; POS Server does not own PaymentAttempt lifecycle.';
COMMENT ON COLUMN pos.fiscal_documents.central_pms_payment_confirmation_ref IS 'Reference to Central PMS PaymentConfirmation context; POS Server does not own PaymentConfirmation lifecycle.';
COMMENT ON COLUMN pos.fiscal_documents.payment_finality_ref IS 'Reference to Central PMS payment finality context only; not POS-owned payment finality.';
COMMENT ON COLUMN pos.fiscal_documents.completion_basis IS 'Canonical completion basis: PAYMENT_FINALITY or ZERO_PAYABLE_STATUTORY_FINALITY.';
COMMENT ON COLUMN pos.fiscal_documents.completion_authority_ref IS 'Canonical Central PMS durable completion source reference; never a POS-owned finality assertion.';
COMMENT ON COLUMN pos.fiscal_documents.vendor_ack_ref IS 'Reference to vendor acknowledgement context only; not vendor authority.';
COMMENT ON COLUMN pos.fiscal_documents.void_status IS 'Void/cancellation runtime posture. Does not delete, reuse, unburn, reset, or decrement the original fiscal number.';
COMMENT ON COLUMN pos.fiscal_documents.void_reason_code IS 'Reference-safe runtime void reason code captured by the POS Server void API.';
COMMENT ON COLUMN pos.fiscal_documents.void_reason_text IS 'Optional reference-safe void reason text. Must not contain raw evidence, credentials, or sensitive payloads.';
COMMENT ON COLUMN pos.fiscal_documents.void_requested_by_ref IS 'Reference to actor or upstream system requesting the void; reference only.';
COMMENT ON COLUMN pos.fiscal_documents.void_requested_at IS 'Request timestamp supplied by caller or defaulted by POS Server runtime.';
COMMENT ON COLUMN pos.fiscal_documents.voided_at IS 'Timestamp when POS Server durably recorded fiscal document void/cancellation.';
COMMENT ON COLUMN pos.fiscal_documents.void_idempotency_key IS 'Idempotency key that recorded the fiscal document void/cancellation.';
COMMENT ON COLUMN pos.fiscal_documents.void_semantic_request_hash IS 'Semantic hash of meaningful void/cancellation request facts.';
COMMENT ON COLUMN pos.fiscal_documents.void_correlation_id IS 'Correlation reference for tracing the void/cancellation request; reference only.';
COMMENT ON COLUMN pos.fiscal_documents.void_source_system_ref IS 'Optional source-system reference for the void/cancellation request.';
COMMENT ON COLUMN pos.fiscal_documents.void_business_day_date IS 'Optional business day supplied for the void/cancellation request.';
COMMENT ON COLUMN pos.fiscal_documents.document_context IS 'Flexible header context for unresolved fiscal document attributes. Must remain a JSON object and is not authoritative fiscal number storage.';
