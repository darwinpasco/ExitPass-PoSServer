-- ExitPass POS Server Slice 5 table artifact.
-- Reprint request lifecycle posture only.
-- Reprints preserve original fiscal facts and do not create new SI numbers or fiscal issuance behavior.

CREATE TABLE IF NOT EXISTS pos.reprint_requests (
    reprint_request_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    reprint_type_code_id uuid NOT NULL,
    reprint_status_code_id uuid NOT NULL,
    reprint_reason_code_id uuid NULL,
    reprint_reason_text text NULL,
    requested_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    requested_by_ref text NULL,
    approval_ref text NULL,
    actor_ref text NULL,
    service_identity_ref text NULL,
    reprint_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_reprint_requests PRIMARY KEY (reprint_request_id),
    CONSTRAINT fk_reprint_requests__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_reprint_requests__type_code FOREIGN KEY (reprint_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_reprint_requests__status_code FOREIGN KEY (reprint_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_reprint_requests__reason_code FOREIGN KEY (reprint_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_reprint_requests__reason_text CHECK (
        reprint_reason_text IS NULL OR char_length(btrim(reprint_reason_text)) > 0
    ),
    CONSTRAINT ck_reprint_requests__requested_by_ref CHECK (
        requested_by_ref IS NULL OR char_length(btrim(requested_by_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__approval_ref CHECK (
        approval_ref IS NULL OR char_length(btrim(approval_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__context_object CHECK (
        reprint_context IS NULL OR jsonb_typeof(reprint_context) = 'object'
    )
);

COMMENT ON TABLE pos.reprint_requests IS 'Controlled reprint request lifecycle posture. Reprints preserve original fiscal facts and do not create new Sales Invoice numbers or fiscal issuance behavior.';
COMMENT ON COLUMN pos.reprint_requests.approval_ref IS 'Optional approval reference only; does not create external approval authority.';
COMMENT ON COLUMN pos.reprint_requests.reprint_context IS 'Flexible reprint context for unresolved reprint attributes. Must remain a JSON object when present.';

