-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal operation retry posture only.
-- This table records retry attempts without implementing retry execution behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_operation_retries (
    fiscal_operation_retry_id uuid NOT NULL,
    idempotency_record_id uuid NULL,
    fiscal_document_id uuid NULL,
    retry_status_code_id uuid NOT NULL,
    retry_reason_code_id uuid NULL,
    retry_reason_text text NULL,
    attempt_number integer NOT NULL,
    attempted_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actor_ref text NULL,
    service_identity_ref text NULL,
    retry_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_operation_retries PRIMARY KEY (fiscal_operation_retry_id),
    CONSTRAINT fk_fiscal_operation_retries__idempotency FOREIGN KEY (idempotency_record_id)
        REFERENCES pos.idempotency_records (idempotency_record_id),
    CONSTRAINT fk_fiscal_operation_retries__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_operation_retries__status_code FOREIGN KEY (retry_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_operation_retries__reason_code FOREIGN KEY (retry_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_operation_retries__attempt_positive CHECK (attempt_number > 0),
    CONSTRAINT ck_fiscal_operation_retries__reason_text CHECK (
        retry_reason_text IS NULL OR char_length(btrim(retry_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_operation_retries__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_operation_retries__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_operation_retries__context_object CHECK (
        retry_context IS NULL OR jsonb_typeof(retry_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_operation_retries IS 'Fiscal operation retry attempt posture. Does not implement retry execution, payment authority, or exit authority.';
COMMENT ON COLUMN pos.fiscal_operation_retries.idempotency_record_id IS 'Optional idempotency record reference for retry correlation.';
COMMENT ON COLUMN pos.fiscal_operation_retries.fiscal_document_id IS 'Optional fiscal document reference for retry correlation.';
COMMENT ON COLUMN pos.fiscal_operation_retries.actor_ref IS 'Reference to actor context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_operation_retries.service_identity_ref IS 'Reference to service identity context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_operation_retries.retry_context IS 'Flexible retry context. Must remain a JSON object when present.';

