-- ExitPass POS Server Slice 5 table artifact.
-- Fiscal adjustment posture only.
-- This table links original and adjustment fiscal documents without creating refund/reversal finality, money-movement ownership, or adjustment numbering.

CREATE TABLE IF NOT EXISTS pos.fiscal_adjustments (
    fiscal_adjustment_id uuid NOT NULL,
    original_fiscal_document_id uuid NOT NULL,
    adjustment_fiscal_document_id uuid NOT NULL,
    adjustment_type_code_id uuid NOT NULL,
    adjustment_status_code_id uuid NOT NULL,
    adjustment_reason_code_id uuid NULL,
    adjustment_reason_text text NULL,
    approval_ref text NULL,
    payment_reversal_ref text NULL,
    reconciliation_ref text NULL,
    adjustment_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_adjustments PRIMARY KEY (fiscal_adjustment_id),
    CONSTRAINT fk_fiscal_adjustments__original_doc FOREIGN KEY (original_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_adjustments__adjustment_doc FOREIGN KEY (adjustment_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_adjustments__type_code FOREIGN KEY (adjustment_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_adjustments__status_code FOREIGN KEY (adjustment_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_adjustments__reason_code FOREIGN KEY (adjustment_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_adjustments__doc_pair_type UNIQUE (
        original_fiscal_document_id,
        adjustment_fiscal_document_id,
        adjustment_type_code_id
    ),
    CONSTRAINT ck_fiscal_adjustments__different_docs CHECK (
        original_fiscal_document_id <> adjustment_fiscal_document_id
    ),
    CONSTRAINT ck_fiscal_adjustments__reason_text CHECK (
        adjustment_reason_text IS NULL OR char_length(btrim(adjustment_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_adjustments__approval_ref CHECK (
        approval_ref IS NULL OR char_length(btrim(approval_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_adjustments__payment_reversal_ref CHECK (
        payment_reversal_ref IS NULL OR char_length(btrim(payment_reversal_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_adjustments__reconciliation_ref CHECK (
        reconciliation_ref IS NULL OR char_length(btrim(reconciliation_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_adjustments__context_object CHECK (
        adjustment_context IS NULL OR jsonb_typeof(adjustment_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_adjustments IS 'Fiscal adjustment posture linking original and adjustment fiscal documents. Does not create refund/reversal finality, money-movement ownership, or adjustment numbering.';
COMMENT ON COLUMN pos.fiscal_adjustments.payment_reversal_ref IS 'Optional payment reversal reference only; POS Server does not own payment reversal or money-movement finality.';
COMMENT ON COLUMN pos.fiscal_adjustments.reconciliation_ref IS 'Optional reconciliation reference only.';
COMMENT ON COLUMN pos.fiscal_adjustments.adjustment_context IS 'Flexible adjustment context for unresolved adjustment family attributes. Must remain a JSON object when present.';

