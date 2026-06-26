-- ExitPass POS Server first-slice table artifact.
-- Controlled-code set families for statuses, types, reasons, BIR/report classifications, channel capabilities, and fiscal identity states.
-- No code values or seed/reference data are inserted by this artifact.

CREATE TABLE IF NOT EXISTS pos.controlled_code_sets (
    controlled_code_set_id uuid NOT NULL,
    code_set_key text NOT NULL,
    display_name text NOT NULL,
    description text NULL,
    governance_owner text NULL,
    source_ref text NULL,
    is_active boolean NOT NULL DEFAULT true,
    effective_start_at timestamptz NULL,
    effective_end_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_controlled_code_sets PRIMARY KEY (controlled_code_set_id),
    CONSTRAINT uq_controlled_code_sets__code_set_key UNIQUE (code_set_key),
    CONSTRAINT ck_controlled_code_sets__code_set_key_not_blank CHECK (char_length(btrim(code_set_key)) > 0),
    CONSTRAINT ck_controlled_code_sets__display_name_not_blank CHECK (char_length(btrim(display_name)) > 0),
    CONSTRAINT ck_controlled_code_sets__effective_range CHECK (
        effective_start_at IS NULL
        OR effective_end_at IS NULL
        OR effective_end_at > effective_start_at
    )
);

COMMENT ON TABLE pos.controlled_code_sets IS 'Controlled-code set families for POS Server first-slice status, type, reason, and classification posture.';
COMMENT ON COLUMN pos.controlled_code_sets.source_ref IS 'Optional external source reference for governed code-set families; reference only.';

