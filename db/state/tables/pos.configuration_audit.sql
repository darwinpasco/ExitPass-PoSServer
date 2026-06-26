-- POS Server Slice 8: configuration/status audit posture.
-- This table stores configuration change evidence references only; it does not
-- store raw secrets, sensitive configuration payloads, or event data.

CREATE TABLE IF NOT EXISTS pos.configuration_audit (
    configuration_audit_id uuid NOT NULL,
    site_pos_server_id uuid NULL,
    configuration_area_code_id uuid NOT NULL,
    configuration_action_code_id uuid NOT NULL,
    affected_record_ref text NULL,
    prior_value_ref text NULL,
    new_value_ref text NULL,
    actor_ref text NULL,
    service_identity_ref text NULL,
    reason_code_id uuid NULL,
    reason_text text NULL,
    occurred_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    configuration_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_configuration_audit PRIMARY KEY (configuration_audit_id),
    CONSTRAINT fk_configuration_audit__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_configuration_audit__area_code
        FOREIGN KEY (configuration_area_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_configuration_audit__action_code
        FOREIGN KEY (configuration_action_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_configuration_audit__reason_code
        FOREIGN KEY (reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_configuration_audit__affected_ref
        CHECK (affected_record_ref IS NULL OR btrim(affected_record_ref) <> ''),
    CONSTRAINT ck_configuration_audit__prior_value_ref
        CHECK (prior_value_ref IS NULL OR btrim(prior_value_ref) <> ''),
    CONSTRAINT ck_configuration_audit__new_value_ref
        CHECK (new_value_ref IS NULL OR btrim(new_value_ref) <> ''),
    CONSTRAINT ck_configuration_audit__actor_ref
        CHECK (actor_ref IS NULL OR btrim(actor_ref) <> ''),
    CONSTRAINT ck_configuration_audit__service_ref
        CHECK (service_identity_ref IS NULL OR btrim(service_identity_ref) <> ''),
    CONSTRAINT ck_configuration_audit__reason_text
        CHECK (reason_text IS NULL OR btrim(reason_text) <> ''),
    CONSTRAINT ck_configuration_audit__context_object
        CHECK (configuration_context IS NULL OR jsonb_typeof(configuration_context) = 'object')
);

COMMENT ON TABLE pos.configuration_audit IS
    'Configuration and status audit evidence posture. Stores metadata and references only; raw secrets and sensitive payloads are prohibited.';
COMMENT ON COLUMN pos.configuration_audit.configuration_audit_id IS
    'Internal identifier for the configuration audit row.';
COMMENT ON COLUMN pos.configuration_audit.site_pos_server_id IS
    'Optional Site POS Server boundary for the configuration audit evidence.';
COMMENT ON COLUMN pos.configuration_audit.configuration_area_code_id IS
    'Controlled code identifying the configuration area.';
COMMENT ON COLUMN pos.configuration_audit.configuration_action_code_id IS
    'Controlled code identifying the configuration action.';
COMMENT ON COLUMN pos.configuration_audit.affected_record_ref IS
    'External affected-record reference only.';
COMMENT ON COLUMN pos.configuration_audit.prior_value_ref IS
    'Prior value reference only; do not store raw sensitive configuration values.';
COMMENT ON COLUMN pos.configuration_audit.new_value_ref IS
    'New value reference only; do not store raw sensitive configuration values.';
COMMENT ON COLUMN pos.configuration_audit.configuration_context IS
    'Optional configuration audit metadata as a JSON object; do not store raw secrets or sensitive payloads.';

