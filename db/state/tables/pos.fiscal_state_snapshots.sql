-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal state snapshot posture only.
-- This table stores continuity state snapshots without implementing recovery or anchoring behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_state_snapshots (
    fiscal_state_snapshot_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    snapshot_type_code_id uuid NOT NULL,
    snapshot_status_code_id uuid NOT NULL,
    business_day_date date NULL,
    reset_counter_value bigint NULL,
    z_counter_value bigint NULL,
    grand_total_amount_minor_units bigint NULL,
    currency_code char(3) NULL,
    latest_ej_hash_ref text NULL,
    last_fiscal_event_at timestamptz NULL,
    snapshot_context jsonb NULL,
    captured_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_state_snapshots PRIMARY KEY (fiscal_state_snapshot_id),
    CONSTRAINT fk_fiscal_state_snapshots__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_state_snapshots__snapshot_type FOREIGN KEY (snapshot_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_state_snapshots__snapshot_status FOREIGN KEY (snapshot_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_state_snapshots__reset_counter CHECK (
        reset_counter_value IS NULL OR reset_counter_value >= 0
    ),
    CONSTRAINT ck_fiscal_state_snapshots__z_counter CHECK (
        z_counter_value IS NULL OR z_counter_value >= 0
    ),
    CONSTRAINT ck_fiscal_state_snapshots__gta_nonnegative CHECK (
        grand_total_amount_minor_units IS NULL OR grand_total_amount_minor_units >= 0
    ),
    CONSTRAINT ck_fiscal_state_snapshots__currency_format CHECK (
        currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$'
    ),
    CONSTRAINT ck_fiscal_state_snapshots__ej_hash_ref CHECK (
        latest_ej_hash_ref IS NULL OR char_length(btrim(latest_ej_hash_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_state_snapshots__context_object CHECK (
        snapshot_context IS NULL OR jsonb_typeof(snapshot_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_state_snapshots IS 'Fiscal continuity state snapshot posture. Does not implement recovery workflow, hash chaining, external anchoring, or resume behavior.';
COMMENT ON COLUMN pos.fiscal_state_snapshots.latest_ej_hash_ref IS 'Optional reference to latest EJ hash context; reference only and not an anchoring implementation.';
COMMENT ON COLUMN pos.fiscal_state_snapshots.grand_total_amount_minor_units IS 'Optional GTA posture amount in minor units; no final formula is implemented.';
COMMENT ON COLUMN pos.fiscal_state_snapshots.snapshot_context IS 'Flexible snapshot context. Must remain a JSON object when present.';

