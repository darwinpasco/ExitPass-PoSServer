-- POS Server Slice 7: export schema/profile reference posture.
-- This table stores schema/profile references only and does not store ARTS,
-- POSLog, BIR, local JSON, or EJ schema file contents.

CREATE TABLE IF NOT EXISTS pos.export_schema_profile_refs (
    export_schema_profile_ref_id uuid NOT NULL,
    profile_type_code_id uuid NOT NULL,
    profile_key text NOT NULL,
    profile_version text NULL,
    profile_source_ref text NULL,
    profile_uri_ref text NULL,
    is_active boolean NOT NULL DEFAULT true,
    effective_start_at timestamptz NULL,
    effective_end_at timestamptz NULL,
    profile_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_export_schema_profile_refs PRIMARY KEY (export_schema_profile_ref_id),
    CONSTRAINT fk_export_schema_profile_refs__profile_type
        FOREIGN KEY (profile_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_export_schema_profile_refs__type_key_version
        UNIQUE (profile_type_code_id, profile_key, profile_version),
    CONSTRAINT ck_export_schema_profile_refs__profile_key
        CHECK (btrim(profile_key) <> ''),
    CONSTRAINT ck_export_schema_profile_refs__profile_version
        CHECK (profile_version IS NULL OR btrim(profile_version) <> ''),
    CONSTRAINT ck_export_schema_profile_refs__profile_source_ref
        CHECK (profile_source_ref IS NULL OR btrim(profile_source_ref) <> ''),
    CONSTRAINT ck_export_schema_profile_refs__profile_uri_ref
        CHECK (profile_uri_ref IS NULL OR btrim(profile_uri_ref) <> ''),
    CONSTRAINT ck_export_schema_profile_refs__effective_range
        CHECK (effective_start_at IS NULL OR effective_end_at IS NULL OR effective_end_at > effective_start_at),
    CONSTRAINT ck_export_schema_profile_refs__context_object
        CHECK (profile_context IS NULL OR jsonb_typeof(profile_context) = 'object')
);

COMMENT ON TABLE pos.export_schema_profile_refs IS
    'Schema/profile reference posture for ARTS POSLog, BIR/local JSON, EJ, and future approved export profiles. External schema files are not stored here.';
COMMENT ON COLUMN pos.export_schema_profile_refs.export_schema_profile_ref_id IS
    'Internal identifier for the export schema/profile reference.';
COMMENT ON COLUMN pos.export_schema_profile_refs.profile_type_code_id IS
    'Controlled code identifying the profile family or type.';
COMMENT ON COLUMN pos.export_schema_profile_refs.profile_key IS
    'Stable profile key used for reference and validation posture.';
COMMENT ON COLUMN pos.export_schema_profile_refs.profile_version IS
    'Optional profile version reference.';
COMMENT ON COLUMN pos.export_schema_profile_refs.profile_source_ref IS
    'External source reference only; no external schema content is stored.';
COMMENT ON COLUMN pos.export_schema_profile_refs.profile_uri_ref IS
    'External URI reference only; no generated or copied schema file is stored.';
COMMENT ON COLUMN pos.export_schema_profile_refs.profile_context IS
    'Optional profile metadata as a JSON object.';

