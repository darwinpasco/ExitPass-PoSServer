-- ExitPass POS Server Slice 4 table artifact.
-- Idempotency request identity posture for fiscal side-effect protection only.
-- This table does not create payment authority, exit authority, or retry execution behavior.

CREATE TABLE IF NOT EXISTS pos.idempotency_records (
    idempotency_record_id uuid NOT NULL,
    idempotency_scope text NOT NULL,
    idempotency_key text NOT NULL,
    semantic_request_hash text NULL,
    operation_type_code_id uuid NOT NULL,
    operation_status_code_id uuid NOT NULL,
    linked_fiscal_document_id uuid NULL,
    replay_result_ref text NULL,
    conflict_ref text NULL,
    completion_unknown boolean NOT NULL DEFAULT false,
    expires_at timestamptz NULL,
    idempotency_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_idempotency_records PRIMARY KEY (idempotency_record_id),
    CONSTRAINT fk_idempotency_records__operation_type FOREIGN KEY (operation_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_idempotency_records__operation_status FOREIGN KEY (operation_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_idempotency_records__fiscal_document FOREIGN KEY (linked_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT uq_idempotency_records__scope_key UNIQUE (idempotency_scope, idempotency_key),
    CONSTRAINT ck_idempotency_records__scope_not_blank CHECK (char_length(btrim(idempotency_scope)) > 0),
    CONSTRAINT ck_idempotency_records__key_not_blank CHECK (char_length(btrim(idempotency_key)) > 0),
    CONSTRAINT ck_idempotency_records__semantic_hash CHECK (
        semantic_request_hash IS NULL OR char_length(btrim(semantic_request_hash)) > 0
    ),
    CONSTRAINT ck_idempotency_records__replay_result_ref CHECK (
        replay_result_ref IS NULL OR char_length(btrim(replay_result_ref)) > 0
    ),
    CONSTRAINT ck_idempotency_records__conflict_ref CHECK (
        conflict_ref IS NULL OR char_length(btrim(conflict_ref)) > 0
    ),
    CONSTRAINT ck_idempotency_records__expires_after_created CHECK (
        expires_at IS NULL OR expires_at > created_at
    ),
    CONSTRAINT ck_idempotency_records__context_object CHECK (
        idempotency_context IS NULL OR jsonb_typeof(idempotency_context) = 'object'
    )
);

COMMENT ON TABLE pos.idempotency_records IS 'Idempotency request identity posture for fiscal side-effect protection. Does not create payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, or ExitAuthorization authority.';
COMMENT ON COLUMN pos.idempotency_records.idempotency_scope IS 'Scope used with idempotency_key for duplicate fiscal side-effect protection.';
COMMENT ON COLUMN pos.idempotency_records.semantic_request_hash IS 'Semantic request identity hash used by runtime idempotency conflict detection for fiscal document creation.';
COMMENT ON COLUMN pos.idempotency_records.linked_fiscal_document_id IS 'Linked fiscal document reference used to replay the original fiscal document outcome for duplicate requests.';
COMMENT ON COLUMN pos.idempotency_records.replay_result_ref IS 'Replay result reference for the linked fiscal document outcome.';
COMMENT ON COLUMN pos.idempotency_records.conflict_ref IS 'Optional conflict reference; reference only.';
COMMENT ON COLUMN pos.idempotency_records.idempotency_context IS 'Flexible idempotency context. Must remain a JSON object when present.';
