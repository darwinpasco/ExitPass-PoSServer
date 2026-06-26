-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal lock/block state posture only.
-- This table stores lock state records without implementing recovery, resume, or gate authority behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_lock_states (
    fiscal_lock_state_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    lock_state_code_id uuid NOT NULL,
    lock_reason_code_id uuid NULL,
    lock_reason_text text NULL,
    locked_at timestamptz NULL,
    unlocked_at timestamptz NULL,
    actor_ref text NULL,
    service_identity_ref text NULL,
    lock_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_lock_states PRIMARY KEY (fiscal_lock_state_id),
    CONSTRAINT fk_fiscal_lock_states__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_lock_states__lock_state FOREIGN KEY (lock_state_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_lock_states__lock_reason FOREIGN KEY (lock_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_lock_states__unlock_after_lock CHECK (
        locked_at IS NULL OR unlocked_at IS NULL OR unlocked_at >= locked_at
    ),
    CONSTRAINT ck_fiscal_lock_states__reason_text CHECK (
        lock_reason_text IS NULL OR char_length(btrim(lock_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_lock_states__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_lock_states__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_lock_states__context_object CHECK (
        lock_context IS NULL OR jsonb_typeof(lock_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_lock_states IS 'Fiscal lock/block/resume state posture. Does not implement recovery workflow, gate authority, or offline fiscal issuance approval.';
COMMENT ON COLUMN pos.fiscal_lock_states.lock_state_code_id IS 'Controlled-code reference for fiscal lock/block/resume state.';
COMMENT ON COLUMN pos.fiscal_lock_states.actor_ref IS 'Reference to actor context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_lock_states.service_identity_ref IS 'Reference to service identity context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_lock_states.lock_context IS 'Flexible lock state context. Must remain a JSON object when present.';

