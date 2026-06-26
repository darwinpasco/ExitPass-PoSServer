-- ExitPass POS Server Slice 2 table artifact.
-- Fiscal document local status transition history only.
-- This table is not payment finality, does not authorize exit, and is not a full audit subsystem.

CREATE TABLE IF NOT EXISTS pos.fiscal_document_status_history (
    fiscal_document_status_history_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    prior_fiscal_document_status_code_id uuid NULL,
    new_fiscal_document_status_code_id uuid NOT NULL,
    status_reason_code_id uuid NULL,
    status_reason_text text NULL,
    changed_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actor_ref text NULL,
    service_identity_ref text NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_document_status_history PRIMARY KEY (fiscal_document_status_history_id),
    CONSTRAINT fk_fiscal_doc_status_hist__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_doc_status_hist__prior_status FOREIGN KEY (prior_fiscal_document_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_doc_status_hist__new_status FOREIGN KEY (new_fiscal_document_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_doc_status_hist__reason_code FOREIGN KEY (status_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_doc_status_hist__reason_text CHECK (
        status_reason_text IS NULL OR char_length(btrim(status_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_doc_status_hist__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_doc_status_hist__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    )
);

COMMENT ON TABLE pos.fiscal_document_status_history IS 'Local fiscal document status transition history. It is evidence of status change only and does not own payment finality or ExitAuthorization.';
COMMENT ON COLUMN pos.fiscal_document_status_history.actor_ref IS 'Reference to actor context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_document_status_history.service_identity_ref IS 'Reference to service identity context when available; reference only.';

