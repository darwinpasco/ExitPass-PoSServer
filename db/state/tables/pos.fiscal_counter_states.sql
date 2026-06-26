-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal counter state/posture only.
-- This table stores counter state records without implementing counter increment behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_counter_states (
    fiscal_counter_state_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    counter_family_code_id uuid NOT NULL,
    counter_value bigint NOT NULL DEFAULT 0,
    monetary_amount_minor_units bigint NULL,
    currency_code char(3) NULL,
    counter_status_code_id uuid NOT NULL,
    last_transition_at timestamptz NULL,
    counter_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_counter_states PRIMARY KEY (fiscal_counter_state_id),
    CONSTRAINT fk_fiscal_counter_states__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_counter_states__counter_family FOREIGN KEY (counter_family_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_counter_states__status_code FOREIGN KEY (counter_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_counter_states__site_family UNIQUE (site_pos_server_id, counter_family_code_id),
    CONSTRAINT ck_fiscal_counter_states__counter_nonnegative CHECK (counter_value >= 0),
    CONSTRAINT ck_fiscal_counter_states__amount_nonnegative CHECK (
        monetary_amount_minor_units IS NULL OR monetary_amount_minor_units >= 0
    ),
    CONSTRAINT ck_fiscal_counter_states__currency_format CHECK (
        currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$'
    ),
    CONSTRAINT ck_fiscal_counter_states__context_object CHECK (
        counter_context IS NULL OR jsonb_typeof(counter_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_counter_states IS 'Fiscal counter state posture for reset counter, Z-counter, GTA, and related families. Does not implement counter increment behavior.';
COMMENT ON COLUMN pos.fiscal_counter_states.counter_family_code_id IS 'Controlled-code reference for fiscal counter family.';
COMMENT ON COLUMN pos.fiscal_counter_states.counter_value IS 'Counter state value only. Reset counter and Z-counter behavior remains governed by approved placeholder policy and future BIR/accounting confirmation.';
COMMENT ON COLUMN pos.fiscal_counter_states.monetary_amount_minor_units IS 'Optional monetary amount such as GTA posture; no final calculation formula is implemented.';
COMMENT ON COLUMN pos.fiscal_counter_states.counter_context IS 'Flexible counter context. Must remain a JSON object when present.';

