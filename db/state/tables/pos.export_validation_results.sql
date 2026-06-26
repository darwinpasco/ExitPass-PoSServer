-- POS Server Slice 7: export validation result posture.
-- This table stores validation status, rule, message, and profile references
-- only; it does not create validation scripts or embed schema/payload files.

CREATE TABLE IF NOT EXISTS pos.export_validation_results (
    export_validation_result_id uuid NOT NULL,
    fiscal_export_package_id uuid NOT NULL,
    schema_profile_ref_id uuid NULL,
    validation_status_code_id uuid NOT NULL,
    validation_severity_code_id uuid NULL,
    validation_rule_ref text NULL,
    validation_message text NULL,
    validated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    validated_by_ref text NULL,
    validation_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_export_validation_results PRIMARY KEY (export_validation_result_id),
    CONSTRAINT fk_export_validation_results__package
        FOREIGN KEY (fiscal_export_package_id)
        REFERENCES pos.fiscal_export_packages (fiscal_export_package_id),
    CONSTRAINT fk_export_validation_results__schema_profile
        FOREIGN KEY (schema_profile_ref_id)
        REFERENCES pos.export_schema_profile_refs (export_schema_profile_ref_id),
    CONSTRAINT fk_export_validation_results__status
        FOREIGN KEY (validation_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_export_validation_results__severity
        FOREIGN KEY (validation_severity_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_export_validation_results__rule_ref
        CHECK (validation_rule_ref IS NULL OR btrim(validation_rule_ref) <> ''),
    CONSTRAINT ck_export_validation_results__message
        CHECK (validation_message IS NULL OR btrim(validation_message) <> ''),
    CONSTRAINT ck_export_validation_results__validated_by_ref
        CHECK (validated_by_ref IS NULL OR btrim(validated_by_ref) <> ''),
    CONSTRAINT ck_export_validation_results__context_object
        CHECK (validation_context IS NULL OR jsonb_typeof(validation_context) = 'object')
);

COMMENT ON TABLE pos.export_validation_results IS
    'Export validation result posture for package/schema-profile validation status and messages. No validation scripts, schemas, or payloads are stored here.';
COMMENT ON COLUMN pos.export_validation_results.export_validation_result_id IS
    'Internal identifier for the export validation result.';
COMMENT ON COLUMN pos.export_validation_results.fiscal_export_package_id IS
    'Export package associated with the validation result.';
COMMENT ON COLUMN pos.export_validation_results.schema_profile_ref_id IS
    'Optional schema/profile reference associated with the validation result.';
COMMENT ON COLUMN pos.export_validation_results.validation_status_code_id IS
    'Controlled code identifying validation status.';
COMMENT ON COLUMN pos.export_validation_results.validation_severity_code_id IS
    'Optional controlled code identifying validation severity.';
COMMENT ON COLUMN pos.export_validation_results.validation_rule_ref IS
    'External validation rule reference only.';
COMMENT ON COLUMN pos.export_validation_results.validation_message IS
    'Validation message text only; no generated payload content is stored.';
COMMENT ON COLUMN pos.export_validation_results.validated_by_ref IS
    'External actor or service reference only.';
COMMENT ON COLUMN pos.export_validation_results.validation_context IS
    'Optional validation metadata as a JSON object.';

