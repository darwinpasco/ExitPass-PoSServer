-- POS Server Slice 8: recovery request posture.
-- This table stores supervised recovery request references only and does not
-- implement recovery automation, unlocks, resume behavior, or anchoring.

CREATE TABLE IF NOT EXISTS pos.recovery_requests (
    recovery_request_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_state_snapshot_id uuid NULL,
    fiscal_lock_state_id uuid NULL,
    recovery_type_code_id uuid NOT NULL,
    recovery_status_code_id uuid NOT NULL,
    recovery_reason_code_id uuid NULL,
    recovery_reason_text text NULL,
    requested_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    requested_by_ref text NULL,
    approval_ref text NULL,
    service_identity_ref text NULL,
    recovery_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_recovery_requests PRIMARY KEY (recovery_request_id),
    CONSTRAINT fk_recovery_requests__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_recovery_requests__snapshot
        FOREIGN KEY (fiscal_state_snapshot_id)
        REFERENCES pos.fiscal_state_snapshots (fiscal_state_snapshot_id),
    CONSTRAINT fk_recovery_requests__lock_state
        FOREIGN KEY (fiscal_lock_state_id)
        REFERENCES pos.fiscal_lock_states (fiscal_lock_state_id),
    CONSTRAINT fk_recovery_requests__recovery_type
        FOREIGN KEY (recovery_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_recovery_requests__recovery_status
        FOREIGN KEY (recovery_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_recovery_requests__reason_code
        FOREIGN KEY (recovery_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_recovery_requests__reason_text
        CHECK (recovery_reason_text IS NULL OR btrim(recovery_reason_text) <> ''),
    CONSTRAINT ck_recovery_requests__requested_by_ref
        CHECK (requested_by_ref IS NULL OR btrim(requested_by_ref) <> ''),
    CONSTRAINT ck_recovery_requests__approval_ref
        CHECK (approval_ref IS NULL OR btrim(approval_ref) <> ''),
    CONSTRAINT ck_recovery_requests__service_ref
        CHECK (service_identity_ref IS NULL OR btrim(service_identity_ref) <> ''),
    CONSTRAINT ck_recovery_requests__context_object
        CHECK (recovery_context IS NULL OR jsonb_typeof(recovery_context) = 'object')
);

COMMENT ON TABLE pos.recovery_requests IS
    'Recovery request and supervised approval posture. Does not implement recovery workflow, automatic unlock, unsafe resume, or anchoring behavior.';
COMMENT ON COLUMN pos.recovery_requests.recovery_request_id IS
    'Internal identifier for the recovery request.';
COMMENT ON COLUMN pos.recovery_requests.site_pos_server_id IS
    'Site POS Server boundary for the recovery request.';
COMMENT ON COLUMN pos.recovery_requests.fiscal_state_snapshot_id IS
    'Optional fiscal state snapshot reference for recovery evidence.';
COMMENT ON COLUMN pos.recovery_requests.fiscal_lock_state_id IS
    'Optional fiscal lock state reference for recovery evidence.';
COMMENT ON COLUMN pos.recovery_requests.recovery_type_code_id IS
    'Controlled code identifying the recovery request type.';
COMMENT ON COLUMN pos.recovery_requests.recovery_status_code_id IS
    'Controlled code identifying recovery request status.';
COMMENT ON COLUMN pos.recovery_requests.requested_by_ref IS
    'External requester reference only.';
COMMENT ON COLUMN pos.recovery_requests.approval_ref IS
    'External approval reference only; no approval workflow is implemented.';
COMMENT ON COLUMN pos.recovery_requests.recovery_context IS
    'Optional recovery metadata as a JSON object; do not store raw sensitive evidence.';

