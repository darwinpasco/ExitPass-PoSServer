-- POS Server Slice 8: fiscal action audit posture.
-- This table stores fiscal action evidence references only; it does not publish
-- events, grant authority, or implement workflow behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_action_audit (
    fiscal_action_audit_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_document_id uuid NULL,
    fiscal_report_request_id uuid NULL,
    fiscal_export_package_id uuid NULL,
    audit_action_type_code_id uuid NOT NULL,
    audit_result_code_id uuid NULL,
    actor_ref text NULL,
    service_identity_ref text NULL,
    channel_terminal_id uuid NULL,
    correlation_ref text NULL,
    occurred_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    audit_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_action_audit PRIMARY KEY (fiscal_action_audit_id),
    CONSTRAINT fk_fiscal_action_audit__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_action_audit__fiscal_document
        FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_action_audit__report_request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_action_audit__export_package
        FOREIGN KEY (fiscal_export_package_id)
        REFERENCES pos.fiscal_export_packages (fiscal_export_package_id),
    CONSTRAINT fk_fiscal_action_audit__action_type
        FOREIGN KEY (audit_action_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_action_audit__result
        FOREIGN KEY (audit_result_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_action_audit__channel
        FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT ck_fiscal_action_audit__actor_ref
        CHECK (actor_ref IS NULL OR btrim(actor_ref) <> ''),
    CONSTRAINT ck_fiscal_action_audit__service_ref
        CHECK (service_identity_ref IS NULL OR btrim(service_identity_ref) <> ''),
    CONSTRAINT ck_fiscal_action_audit__correlation_ref
        CHECK (correlation_ref IS NULL OR btrim(correlation_ref) <> ''),
    CONSTRAINT ck_fiscal_action_audit__context_object
        CHECK (audit_context IS NULL OR jsonb_typeof(audit_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_action_audit IS
    'Fiscal action audit evidence posture for fiscal documents, reports, exports, reprints, and adjustments. Does not publish events or grant authority.';
COMMENT ON COLUMN pos.fiscal_action_audit.fiscal_action_audit_id IS
    'Internal identifier for the fiscal action audit row.';
COMMENT ON COLUMN pos.fiscal_action_audit.site_pos_server_id IS
    'Site POS Server boundary for the audit evidence.';
COMMENT ON COLUMN pos.fiscal_action_audit.fiscal_document_id IS
    'Optional fiscal document reference.';
COMMENT ON COLUMN pos.fiscal_action_audit.fiscal_report_request_id IS
    'Optional fiscal report request reference.';
COMMENT ON COLUMN pos.fiscal_action_audit.fiscal_export_package_id IS
    'Optional fiscal export package reference.';
COMMENT ON COLUMN pos.fiscal_action_audit.audit_action_type_code_id IS
    'Controlled code identifying the audited fiscal action.';
COMMENT ON COLUMN pos.fiscal_action_audit.audit_result_code_id IS
    'Optional controlled code identifying the action result.';
COMMENT ON COLUMN pos.fiscal_action_audit.actor_ref IS
    'External actor reference only; no IAM subject ownership is created.';
COMMENT ON COLUMN pos.fiscal_action_audit.service_identity_ref IS
    'External service identity reference only.';
COMMENT ON COLUMN pos.fiscal_action_audit.correlation_ref IS
    'External correlation reference only; not an event or outbox key.';
COMMENT ON COLUMN pos.fiscal_action_audit.audit_context IS
    'Optional audit metadata as a JSON object; do not store raw sensitive evidence.';

