-- POS Server Slice 8: privileged action audit posture.
-- This table stores privileged operation evidence references only and does not
-- create IAM, RBAC, approval workflow, or authority behavior.

CREATE TABLE IF NOT EXISTS pos.privileged_action_audit (
    privileged_action_audit_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    privileged_action_type_code_id uuid NOT NULL,
    privileged_action_result_code_id uuid NULL,
    actor_ref text NULL,
    approval_ref text NULL,
    service_identity_ref text NULL,
    affected_record_ref text NULL,
    reason_code_id uuid NULL,
    reason_text text NULL,
    occurred_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    privileged_action_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_privileged_action_audit PRIMARY KEY (privileged_action_audit_id),
    CONSTRAINT fk_privileged_action_audit__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_privileged_action_audit__action_type
        FOREIGN KEY (privileged_action_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_privileged_action_audit__action_result
        FOREIGN KEY (privileged_action_result_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_privileged_action_audit__reason_code
        FOREIGN KEY (reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_privileged_action_audit__actor_ref
        CHECK (actor_ref IS NULL OR btrim(actor_ref) <> ''),
    CONSTRAINT ck_privileged_action_audit__approval_ref
        CHECK (approval_ref IS NULL OR btrim(approval_ref) <> ''),
    CONSTRAINT ck_privileged_action_audit__service_ref
        CHECK (service_identity_ref IS NULL OR btrim(service_identity_ref) <> ''),
    CONSTRAINT ck_privileged_action_audit__affected_ref
        CHECK (affected_record_ref IS NULL OR btrim(affected_record_ref) <> ''),
    CONSTRAINT ck_privileged_action_audit__reason_text
        CHECK (reason_text IS NULL OR btrim(reason_text) <> ''),
    CONSTRAINT ck_privileged_action_audit__context_object
        CHECK (privileged_action_context IS NULL OR jsonb_typeof(privileged_action_context) = 'object')
);

COMMENT ON TABLE pos.privileged_action_audit IS
    'Privileged operation audit evidence posture. Actor, approval, and service values are references only and do not create IAM/RBAC tables.';
COMMENT ON COLUMN pos.privileged_action_audit.privileged_action_audit_id IS
    'Internal identifier for the privileged action audit row.';
COMMENT ON COLUMN pos.privileged_action_audit.site_pos_server_id IS
    'Site POS Server boundary for the privileged action evidence.';
COMMENT ON COLUMN pos.privileged_action_audit.privileged_action_type_code_id IS
    'Controlled code identifying the privileged action type.';
COMMENT ON COLUMN pos.privileged_action_audit.privileged_action_result_code_id IS
    'Optional controlled code identifying the privileged action result.';
COMMENT ON COLUMN pos.privileged_action_audit.actor_ref IS
    'External actor reference only.';
COMMENT ON COLUMN pos.privileged_action_audit.approval_ref IS
    'External approval reference only; no approval workflow is implemented.';
COMMENT ON COLUMN pos.privileged_action_audit.affected_record_ref IS
    'External affected-record reference only.';
COMMENT ON COLUMN pos.privileged_action_audit.privileged_action_context IS
    'Optional privileged action metadata as a JSON object; do not store raw sensitive evidence.';

