-- POS Server Slice 8: security/privacy reference posture.
-- This table groups security/privacy references only; it does not create IAM,
-- RBAC, raw evidence storage, credential storage, or token storage.

CREATE TABLE IF NOT EXISTS pos.security_reference_contexts (
    security_reference_context_id uuid NOT NULL,
    site_pos_server_id uuid NULL,
    security_reference_type_code_id uuid NOT NULL,
    security_reference_status_code_id uuid NULL,
    actor_ref text NULL,
    service_identity_ref text NULL,
    approval_ref text NULL,
    evidence_ref text NULL,
    privacy_classification_code_id uuid NULL,
    effective_start_at timestamptz NULL,
    effective_end_at timestamptz NULL,
    security_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_security_reference_contexts PRIMARY KEY (security_reference_context_id),
    CONSTRAINT fk_security_reference_contexts__site
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_security_reference_contexts__type
        FOREIGN KEY (security_reference_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_security_reference_contexts__status
        FOREIGN KEY (security_reference_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_security_reference_contexts__privacy
        FOREIGN KEY (privacy_classification_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_security_reference_contexts__actor_ref
        CHECK (actor_ref IS NULL OR btrim(actor_ref) <> ''),
    CONSTRAINT ck_security_reference_contexts__service_ref
        CHECK (service_identity_ref IS NULL OR btrim(service_identity_ref) <> ''),
    CONSTRAINT ck_security_reference_contexts__approval_ref
        CHECK (approval_ref IS NULL OR btrim(approval_ref) <> ''),
    CONSTRAINT ck_security_reference_contexts__evidence_ref
        CHECK (evidence_ref IS NULL OR btrim(evidence_ref) <> ''),
    CONSTRAINT ck_security_reference_contexts__effective_range
        CHECK (effective_start_at IS NULL OR effective_end_at IS NULL OR effective_end_at > effective_start_at),
    CONSTRAINT ck_security_reference_contexts__context_object
        CHECK (security_context IS NULL OR jsonb_typeof(security_context) = 'object')
);

COMMENT ON TABLE pos.security_reference_contexts IS
    'Security/privacy reference grouping posture. References actor, service, approval, evidence, and privacy classification without creating IAM/RBAC or raw evidence storage.';
COMMENT ON COLUMN pos.security_reference_contexts.security_reference_context_id IS
    'Internal identifier for the security/privacy reference context.';
COMMENT ON COLUMN pos.security_reference_contexts.site_pos_server_id IS
    'Optional Site POS Server boundary for the security/privacy reference context.';
COMMENT ON COLUMN pos.security_reference_contexts.security_reference_type_code_id IS
    'Controlled code identifying the security/privacy reference type.';
COMMENT ON COLUMN pos.security_reference_contexts.security_reference_status_code_id IS
    'Optional controlled code identifying the reference status.';
COMMENT ON COLUMN pos.security_reference_contexts.actor_ref IS
    'External actor reference only; no IAM principal table is created.';
COMMENT ON COLUMN pos.security_reference_contexts.service_identity_ref IS
    'External service identity reference only.';
COMMENT ON COLUMN pos.security_reference_contexts.approval_ref IS
    'External approval reference only.';
COMMENT ON COLUMN pos.security_reference_contexts.evidence_ref IS
    'External evidence reference only; raw evidence files are not stored.';
COMMENT ON COLUMN pos.security_reference_contexts.privacy_classification_code_id IS
    'Optional controlled code identifying privacy classification posture.';
COMMENT ON COLUMN pos.security_reference_contexts.security_context IS
    'Optional security/privacy metadata as a JSON object; do not store secrets, credentials, raw tokens, raw identity documents, raw evidence, or cryptographic material.';

