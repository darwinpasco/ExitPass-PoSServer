-- POS Server Slice 7: fiscal export package metadata posture.
-- This table stores package references and metadata only; it does not store
-- generated export file contents or POSLog/XML/JSON payloads.

CREATE TABLE IF NOT EXISTS pos.fiscal_export_packages (
    fiscal_export_package_id uuid NOT NULL,
    fiscal_export_request_id uuid NOT NULL,
    export_package_type_code_id uuid NOT NULL,
    export_package_status_code_id uuid NOT NULL,
    schema_profile_ref_id uuid NULL,
    package_ref text NULL,
    package_hash_ref text NULL,
    generated_at timestamptz NULL,
    generated_by_ref text NULL,
    package_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_export_packages PRIMARY KEY (fiscal_export_package_id),
    CONSTRAINT fk_fiscal_export_packages__request
        FOREIGN KEY (fiscal_export_request_id)
        REFERENCES pos.fiscal_export_requests (fiscal_export_request_id),
    CONSTRAINT fk_fiscal_export_packages__package_type
        FOREIGN KEY (export_package_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_export_packages__package_status
        FOREIGN KEY (export_package_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_export_packages__schema_profile
        FOREIGN KEY (schema_profile_ref_id)
        REFERENCES pos.export_schema_profile_refs (export_schema_profile_ref_id),
    CONSTRAINT ck_fiscal_export_packages__package_ref
        CHECK (package_ref IS NULL OR btrim(package_ref) <> ''),
    CONSTRAINT ck_fiscal_export_packages__package_hash_ref
        CHECK (package_hash_ref IS NULL OR btrim(package_hash_ref) <> ''),
    CONSTRAINT ck_fiscal_export_packages__generated_by_ref
        CHECK (generated_by_ref IS NULL OR btrim(generated_by_ref) <> ''),
    CONSTRAINT ck_fiscal_export_packages__context_object
        CHECK (package_context IS NULL OR jsonb_typeof(package_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_export_packages IS
    'Export package metadata posture. Stores references only and does not store generated export files, POSLog payloads, or schema contents.';
COMMENT ON COLUMN pos.fiscal_export_packages.fiscal_export_package_id IS
    'Internal identifier for the fiscal export package.';
COMMENT ON COLUMN pos.fiscal_export_packages.fiscal_export_request_id IS
    'Export request associated with the package.';
COMMENT ON COLUMN pos.fiscal_export_packages.export_package_type_code_id IS
    'Controlled code identifying the export package type.';
COMMENT ON COLUMN pos.fiscal_export_packages.export_package_status_code_id IS
    'Controlled code identifying the export package status.';
COMMENT ON COLUMN pos.fiscal_export_packages.schema_profile_ref_id IS
    'Optional schema/profile reference for validation posture.';
COMMENT ON COLUMN pos.fiscal_export_packages.package_ref IS
    'External package reference only; generated file content is not stored here.';
COMMENT ON COLUMN pos.fiscal_export_packages.package_hash_ref IS
    'Package hash reference only; anchoring or recovery behavior is not implemented here.';
COMMENT ON COLUMN pos.fiscal_export_packages.package_context IS
    'Optional package metadata as a JSON object.';

