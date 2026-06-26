-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal operation exception posture only.
-- This table records exception posture without creating a full audit or recovery subsystem.

CREATE TABLE IF NOT EXISTS pos.fiscal_operation_exceptions (
    fiscal_operation_exception_id uuid NOT NULL,
    idempotency_record_id uuid NULL,
    fiscal_document_id uuid NULL,
    exception_type_code_id uuid NOT NULL,
    exception_status_code_id uuid NOT NULL,
    exception_message text NULL,
    detected_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at timestamptz NULL,
    actor_ref text NULL,
    service_identity_ref text NULL,
    exception_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_operation_exceptions PRIMARY KEY (fiscal_operation_exception_id),
    CONSTRAINT fk_fiscal_op_exceptions__idempotency FOREIGN KEY (idempotency_record_id)
        REFERENCES pos.idempotency_records (idempotency_record_id),
    CONSTRAINT fk_fiscal_op_exceptions__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_op_exceptions__type_code FOREIGN KEY (exception_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_op_exceptions__status_code FOREIGN KEY (exception_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_op_exceptions__resolved_after_detected CHECK (
        resolved_at IS NULL OR resolved_at >= detected_at
    ),
    CONSTRAINT ck_fiscal_op_exceptions__message CHECK (
        exception_message IS NULL OR char_length(btrim(exception_message)) > 0
    ),
    CONSTRAINT ck_fiscal_op_exceptions__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_op_exceptions__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_op_exceptions__context_object CHECK (
        exception_context IS NULL OR jsonb_typeof(exception_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_operation_exceptions IS 'Fiscal operation exception posture. Does not create a full audit subsystem, recovery workflow, payment authority, or exit authority.';
COMMENT ON COLUMN pos.fiscal_operation_exceptions.idempotency_record_id IS 'Optional idempotency record reference for exception correlation.';
COMMENT ON COLUMN pos.fiscal_operation_exceptions.fiscal_document_id IS 'Optional fiscal document reference for exception correlation.';
COMMENT ON COLUMN pos.fiscal_operation_exceptions.actor_ref IS 'Reference to actor context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_operation_exceptions.service_identity_ref IS 'Reference to service identity context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_operation_exceptions.exception_context IS 'Flexible exception context. Must remain a JSON object when present.';

