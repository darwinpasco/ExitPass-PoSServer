-- ExitPass POS Server first-slice table artifact.
-- Controlled-code values for approved code-set families.
-- This table defines structure only; no seed/reference code values are inserted by this artifact.

CREATE TABLE IF NOT EXISTS pos.controlled_codes (
    controlled_code_id uuid NOT NULL,
    controlled_code_set_id uuid NOT NULL,
    code_key text NOT NULL,
    display_name text NOT NULL,
    description text NULL,
    source_ref text NULL,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    effective_start_at timestamptz NULL,
    effective_end_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_controlled_codes PRIMARY KEY (controlled_code_id),
    CONSTRAINT fk_controlled_codes__controlled_code_set FOREIGN KEY (controlled_code_set_id)
        REFERENCES pos.controlled_code_sets (controlled_code_set_id),
    CONSTRAINT uq_controlled_codes__set_code_key UNIQUE (controlled_code_set_id, code_key),
    CONSTRAINT ck_controlled_codes__code_key_not_blank CHECK (char_length(btrim(code_key)) > 0),
    CONSTRAINT ck_controlled_codes__display_name_not_blank CHECK (char_length(btrim(display_name)) > 0),
    CONSTRAINT ck_controlled_codes__sort_order_nonnegative CHECK (sort_order >= 0),
    CONSTRAINT ck_controlled_codes__effective_range CHECK (
        effective_start_at IS NULL
        OR effective_end_at IS NULL
        OR effective_end_at > effective_start_at
    )
);

COMMENT ON TABLE pos.controlled_codes IS 'Controlled-code values for POS Server first-slice code-set families. Values are loaded only by future approved seed/reference-data tasks.';
COMMENT ON COLUMN pos.controlled_codes.source_ref IS 'Optional external source reference for a code value; reference only.';

