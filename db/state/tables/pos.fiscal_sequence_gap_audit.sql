-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal sequence gap audit posture only.
-- This table preserves gap records without implementing final BIR/accounting sequence treatment.

CREATE TABLE IF NOT EXISTS pos.fiscal_sequence_gap_audit (
    fiscal_sequence_gap_audit_id uuid NOT NULL,
    fiscal_sequence_policy_id uuid NOT NULL,
    fiscal_document_id uuid NULL,
    gap_sequence_value bigint NOT NULL,
    gap_reason_code_id uuid NOT NULL,
    gap_reason_text text NULL,
    detected_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actor_ref text NULL,
    service_identity_ref text NULL,
    gap_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_sequence_gap_audit PRIMARY KEY (fiscal_sequence_gap_audit_id),
    CONSTRAINT fk_fiscal_seq_gap_audit__policy FOREIGN KEY (fiscal_sequence_policy_id)
        REFERENCES pos.fiscal_sequence_policies (fiscal_sequence_policy_id),
    CONSTRAINT fk_fiscal_seq_gap_audit__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_seq_gap_audit__reason_code FOREIGN KEY (gap_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_seq_gap_audit__policy_value UNIQUE (fiscal_sequence_policy_id, gap_sequence_value),
    CONSTRAINT ck_fiscal_seq_gap_audit__gap_value_positive CHECK (gap_sequence_value > 0),
    CONSTRAINT ck_fiscal_seq_gap_audit__reason_text CHECK (
        gap_reason_text IS NULL OR char_length(btrim(gap_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_seq_gap_audit__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_seq_gap_audit__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_seq_gap_audit__context_object CHECK (
        gap_context IS NULL OR jsonb_typeof(gap_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_sequence_gap_audit IS 'Permanent fiscal sequence gap audit posture. Preserves gap evidence without implementing allocation, recovery, or final BIR/accounting treatment.';
COMMENT ON COLUMN pos.fiscal_sequence_gap_audit.fiscal_document_id IS 'Optional fiscal document reference when a gap can be associated to a document.';
COMMENT ON COLUMN pos.fiscal_sequence_gap_audit.gap_sequence_value IS 'Sequence value with gap posture. Does not authorize reuse.';
COMMENT ON COLUMN pos.fiscal_sequence_gap_audit.actor_ref IS 'Reference to actor context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_sequence_gap_audit.service_identity_ref IS 'Reference to service identity context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_sequence_gap_audit.gap_context IS 'Flexible gap audit context. Must remain a JSON object when present.';

