-- ExitPass POS Server Slice 2 table artifact.
-- Generic fiscal document-to-document linkage posture only.
-- This table does not create adjustment, reprint, refund, reversal, report, or issuance behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_document_links (
    fiscal_document_link_id uuid NOT NULL,
    source_fiscal_document_id uuid NOT NULL,
    target_fiscal_document_id uuid NOT NULL,
    fiscal_document_link_type_code_id uuid NOT NULL,
    link_reason_code_id uuid NULL,
    link_reason_text text NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_ref text NULL,
    CONSTRAINT pk_fiscal_document_links PRIMARY KEY (fiscal_document_link_id),
    CONSTRAINT fk_fiscal_document_links__source_doc FOREIGN KEY (source_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_document_links__target_doc FOREIGN KEY (target_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_document_links__link_type_code FOREIGN KEY (fiscal_document_link_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_document_links__reason_code FOREIGN KEY (link_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_document_links__not_self_link CHECK (source_fiscal_document_id <> target_fiscal_document_id),
    CONSTRAINT ck_fiscal_document_links__reason_text CHECK (
        link_reason_text IS NULL OR char_length(btrim(link_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_document_links__created_by_ref CHECK (
        created_by_ref IS NULL OR char_length(btrim(created_by_ref)) > 0
    )
);

COMMENT ON TABLE pos.fiscal_document_links IS 'Generic relationship between fiscal documents. Supports future linkage posture without creating adjustment, reprint, report, refund, reversal, or issuance behavior.';
COMMENT ON COLUMN pos.fiscal_document_links.created_by_ref IS 'Reference to actor/service context that created the link; reference only.';

