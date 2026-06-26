-- POS Server Slice 8: continuity check result posture.
-- This table stores continuity check evidence references only; it does not
-- generate hashes, perform recovery, or automate resume behavior.

CREATE TABLE IF NOT EXISTS pos.continuity_check_results (
    continuity_check_result_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    recovery_request_id uuid NULL,
    continuity_check_type_code_id uuid NOT NULL,
    continuity_check_result_code_id uuid NOT NULL,
    checked_snapshot_id uuid NULL,
    expected_counter_ref text NULL,
    observed_counter_ref text NULL,
    expected_hash_ref text NULL,
    observed_hash_ref text NULL,
    checked_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    checked_by_ref text NULL,
    continuity_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_continuity_check_results PRIMARY KEY (continuity_check_result_id),
    CONSTRAINT fk_continuity_check_results__site
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_continuity_check_results__recovery
        FOREIGN KEY (recovery_request_id)
        REFERENCES pos.recovery_requests (recovery_request_id),
    CONSTRAINT fk_continuity_check_results__check_type
        FOREIGN KEY (continuity_check_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_continuity_check_results__check_result
        FOREIGN KEY (continuity_check_result_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_continuity_check_results__snapshot
        FOREIGN KEY (checked_snapshot_id)
        REFERENCES pos.fiscal_state_snapshots (fiscal_state_snapshot_id),
    CONSTRAINT ck_continuity_check_results__expected_counter
        CHECK (expected_counter_ref IS NULL OR btrim(expected_counter_ref) <> ''),
    CONSTRAINT ck_continuity_check_results__observed_counter
        CHECK (observed_counter_ref IS NULL OR btrim(observed_counter_ref) <> ''),
    CONSTRAINT ck_continuity_check_results__expected_hash
        CHECK (expected_hash_ref IS NULL OR btrim(expected_hash_ref) <> ''),
    CONSTRAINT ck_continuity_check_results__observed_hash
        CHECK (observed_hash_ref IS NULL OR btrim(observed_hash_ref) <> ''),
    CONSTRAINT ck_continuity_check_results__checked_by_ref
        CHECK (checked_by_ref IS NULL OR btrim(checked_by_ref) <> ''),
    CONSTRAINT ck_continuity_check_results__context_object
        CHECK (continuity_context IS NULL OR jsonb_typeof(continuity_context) = 'object')
);

COMMENT ON TABLE pos.continuity_check_results IS
    'Continuity check result posture for counters, GTA, SI ranges, EJ hash references, and last-event evidence. Does not perform recovery or hash generation.';
COMMENT ON COLUMN pos.continuity_check_results.continuity_check_result_id IS
    'Internal identifier for the continuity check result.';
COMMENT ON COLUMN pos.continuity_check_results.site_pos_server_id IS
    'Site POS Server boundary for the continuity check.';
COMMENT ON COLUMN pos.continuity_check_results.recovery_request_id IS
    'Optional recovery request reference associated with the check.';
COMMENT ON COLUMN pos.continuity_check_results.checked_snapshot_id IS
    'Optional fiscal state snapshot checked for continuity.';
COMMENT ON COLUMN pos.continuity_check_results.expected_counter_ref IS
    'Expected counter reference only; no counter comparison automation is implemented.';
COMMENT ON COLUMN pos.continuity_check_results.observed_counter_ref IS
    'Observed counter reference only.';
COMMENT ON COLUMN pos.continuity_check_results.expected_hash_ref IS
    'Expected hash reference only; no hash generation is implemented.';
COMMENT ON COLUMN pos.continuity_check_results.observed_hash_ref IS
    'Observed hash reference only.';
COMMENT ON COLUMN pos.continuity_check_results.continuity_context IS
    'Optional continuity metadata as a JSON object; do not store raw sensitive evidence.';

