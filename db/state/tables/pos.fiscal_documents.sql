-- ExitPass POS Server Slice 2 table artifact.
-- Fiscal document header/core posture only.
-- This table does not issue Sales Invoices, allocate fiscal numbers, create counters, own payment finality, or own ExitAuthorization.

CREATE TABLE IF NOT EXISTS pos.fiscal_documents (
    fiscal_document_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    channel_terminal_id uuid NULL,
    fiscal_document_type_code_id uuid NOT NULL,
    fiscal_document_status_code_id uuid NOT NULL,
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
    CONSTRAINT fk_fiscal_documents__doc_type_code FOREIGN KEY (fiscal_document_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_documents__doc_status_code FOREIGN KEY (fiscal_document_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
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

COMMENT ON TABLE pos.fiscal_documents IS 'Fiscal document header/core posture. Does not issue Sales Invoices, allocate fiscal numbers, create fiscal sequencing, own payment finality, or own ExitAuthorization.';
COMMENT ON COLUMN pos.fiscal_documents.central_pms_parking_session_ref IS 'Reference to Central PMS parking session context; POS Server does not own parking session lifecycle.';
COMMENT ON COLUMN pos.fiscal_documents.central_pms_payment_attempt_ref IS 'Reference to Central PMS PaymentAttempt context; POS Server does not own PaymentAttempt lifecycle.';
COMMENT ON COLUMN pos.fiscal_documents.central_pms_payment_confirmation_ref IS 'Reference to Central PMS PaymentConfirmation context; POS Server does not own PaymentConfirmation lifecycle.';
COMMENT ON COLUMN pos.fiscal_documents.payment_finality_ref IS 'Reference to Central PMS payment finality context only; not POS-owned payment finality.';
COMMENT ON COLUMN pos.fiscal_documents.vendor_ack_ref IS 'Reference to vendor acknowledgement context only; not vendor authority.';
COMMENT ON COLUMN pos.fiscal_documents.document_context IS 'Flexible header context for unresolved fiscal document attributes. Must remain a JSON object when present.';

