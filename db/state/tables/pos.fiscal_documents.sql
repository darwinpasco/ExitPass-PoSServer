-- ExitPass POS Server Slice 2 table artifact.
-- Fiscal document header/core posture with nullable fiscal numbering persistence fields.
-- Fiscal numbers are allocated by the runtime repository inside the fiscal document transaction.
-- This table does not own payment finality or ExitAuthorization.

CREATE TABLE IF NOT EXISTS pos.fiscal_documents (
    fiscal_document_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    channel_terminal_id uuid NULL,
    fiscal_identity_id uuid NULL,
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
    vendor_ack_ref text NULL,
    business_day_date date NULL,
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
    CONSTRAINT fk_fiscal_documents__doc_type_code FOREIGN KEY (fiscal_document_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_documents__doc_status_code FOREIGN KEY (fiscal_document_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_documents__sequence_policy FOREIGN KEY (fiscal_sequence_policy_id)
        REFERENCES pos.fiscal_sequence_policies (fiscal_sequence_policy_id),
    CONSTRAINT ck_fiscal_documents__sequence_value CHECK (
        fiscal_sequence_value IS NULL OR fiscal_sequence_value > 0
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
    CONSTRAINT ck_fiscal_documents__document_context_object CHECK (
        document_context IS NULL OR jsonb_typeof(document_context) = 'object'
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
COMMENT ON COLUMN pos.fiscal_documents.vendor_ack_ref IS 'Reference to vendor acknowledgement context only; not vendor authority.';
COMMENT ON COLUMN pos.fiscal_documents.document_context IS 'Flexible header context for unresolved fiscal document attributes. Must remain a JSON object and is not authoritative fiscal number storage.';
