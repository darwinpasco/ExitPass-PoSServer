-- POS Server Slice 8: access audit posture.
-- This table stores access evidence references only; it does not store raw
-- credentials, raw token values, or raw identity evidence.

CREATE TABLE IF NOT EXISTS pos.access_audit (
    access_audit_id uuid NOT NULL,
    site_pos_server_id uuid NULL,
    access_subject_type_code_id uuid NOT NULL,
    access_action_code_id uuid NOT NULL,
    access_result_code_id uuid NULL,
    subject_record_ref text NULL,
    accessor_ref text NULL,
    channel_terminal_id uuid NULL,
    request_ref text NULL,
    occurred_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    access_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_access_audit PRIMARY KEY (access_audit_id),
    CONSTRAINT fk_access_audit__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_access_audit__subject_type
        FOREIGN KEY (access_subject_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_access_audit__access_action
        FOREIGN KEY (access_action_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_access_audit__access_result
        FOREIGN KEY (access_result_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_access_audit__channel
        FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT ck_access_audit__subject_ref
        CHECK (subject_record_ref IS NULL OR btrim(subject_record_ref) <> ''),
    CONSTRAINT ck_access_audit__accessor_ref
        CHECK (accessor_ref IS NULL OR btrim(accessor_ref) <> ''),
    CONSTRAINT ck_access_audit__request_ref
        CHECK (request_ref IS NULL OR btrim(request_ref) <> ''),
    CONSTRAINT ck_access_audit__context_object
        CHECK (access_context IS NULL OR jsonb_typeof(access_context) = 'object')
);

COMMENT ON TABLE pos.access_audit IS
    'Access audit posture for fiscal resources. Stores access metadata and references only; raw credentials, tokens, and identity evidence are prohibited.';
COMMENT ON COLUMN pos.access_audit.access_audit_id IS
    'Internal identifier for the access audit row.';
COMMENT ON COLUMN pos.access_audit.site_pos_server_id IS
    'Optional Site POS Server boundary for the access audit evidence.';
COMMENT ON COLUMN pos.access_audit.access_subject_type_code_id IS
    'Controlled code identifying the accessed subject type.';
COMMENT ON COLUMN pos.access_audit.access_action_code_id IS
    'Controlled code identifying the access action.';
COMMENT ON COLUMN pos.access_audit.access_result_code_id IS
    'Optional controlled code identifying the access result.';
COMMENT ON COLUMN pos.access_audit.subject_record_ref IS
    'External subject record reference only.';
COMMENT ON COLUMN pos.access_audit.accessor_ref IS
    'External accessor reference only.';
COMMENT ON COLUMN pos.access_audit.request_ref IS
    'External request reference only; raw tokens or credentials are not stored.';
COMMENT ON COLUMN pos.access_audit.access_context IS
    'Optional access metadata as a JSON object; do not store raw credentials, tokens, or identity evidence.';

