-- ExitPass POS Server first-slice table artifact.
-- Fiscal identity and registration metadata posture for Site POS Server fiscal boundaries.
-- Final BIR/accreditation uniqueness rules are intentionally not encoded in this first slice.

CREATE TABLE IF NOT EXISTS pos.fiscal_identities (
    fiscal_identity_id uuid NOT NULL,
    fiscal_identity_code text NOT NULL,
    taxpayer_display_name text NULL,
    registered_business_name text NULL,
    registered_business_address text NULL,
    tin text NULL,
    taxpayer_classification text NULL,
    fiscal_identity_status text NOT NULL DEFAULT 'DRAFT',
    registered_business_display_name text NULL,
    registered_business_address_text text NULL,
    min_ref text NULL,
    ptu_ref text NULL,
    serial_ref text NULL,
    software_name text NULL,
    software_version text NULL,
    supplier_ref text NULL,
    accreditation_ref text NULL,
    fiscal_identity_status_code_id uuid NULL,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_ref text NULL,
    updated_by_ref text NULL,
    CONSTRAINT pk_fiscal_identities PRIMARY KEY (fiscal_identity_id),
    CONSTRAINT fk_fiscal_identities__status_code FOREIGN KEY (fiscal_identity_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_identities__fiscal_identity_code UNIQUE (fiscal_identity_code),
    CONSTRAINT ck_fiscal_identities__fiscal_identity_code_not_blank CHECK (char_length(btrim(fiscal_identity_code)) > 0),
    CONSTRAINT ck_fiscal_identities__registered_business_name_not_blank CHECK (
        registered_business_name IS NULL OR char_length(btrim(registered_business_name)) > 0
    ),
    CONSTRAINT ck_fiscal_identities__registered_business_address_not_blank CHECK (
        registered_business_address IS NULL OR char_length(btrim(registered_business_address)) > 0
    ),
    CONSTRAINT ck_fiscal_identities__tin_not_blank CHECK (tin IS NULL OR char_length(btrim(tin)) > 0),
    CONSTRAINT ck_fiscal_identities__taxpayer_classification_not_blank CHECK (
        taxpayer_classification IS NULL OR char_length(btrim(taxpayer_classification)) > 0
    ),
    CONSTRAINT ck_fiscal_identities__fiscal_identity_status CHECK (
        fiscal_identity_status IN ('DRAFT', 'APPROVED', 'RETIRED')
    ),
    CONSTRAINT ck_fiscal_identities__created_by_ref_not_blank CHECK (
        created_by_ref IS NULL OR char_length(btrim(created_by_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_identities__updated_by_ref_not_blank CHECK (
        updated_by_ref IS NULL OR char_length(btrim(updated_by_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_identities__metadata_object CHECK (jsonb_typeof(metadata_json) = 'object'),
    CONSTRAINT ck_fiscal_identities__min_ref_not_blank CHECK (min_ref IS NULL OR char_length(btrim(min_ref)) > 0),
    CONSTRAINT ck_fiscal_identities__ptu_ref_not_blank CHECK (ptu_ref IS NULL OR char_length(btrim(ptu_ref)) > 0),
    CONSTRAINT ck_fiscal_identities__serial_ref_not_blank CHECK (serial_ref IS NULL OR char_length(btrim(serial_ref)) > 0),
    CONSTRAINT ck_fiscal_identities__supplier_ref_not_blank CHECK (supplier_ref IS NULL OR char_length(btrim(supplier_ref)) > 0),
    CONSTRAINT ck_fiscal_identities__accreditation_ref_not_blank CHECK (accreditation_ref IS NULL OR char_length(btrim(accreditation_ref)) > 0)
);

COMMENT ON TABLE pos.fiscal_identities IS 'Fiscal identity and registration metadata posture for POS Server fiscal boundaries. Does not create fiscal issuance, fiscal sequencing, or BIR/accreditation outputs.';
COMMENT ON COLUMN pos.fiscal_identities.registered_business_name IS 'Registered taxpayer or legal-entity business name for Sales Invoice header profile use.';
COMMENT ON COLUMN pos.fiscal_identities.registered_business_address IS 'Registered taxpayer or legal-entity address for Sales Invoice header profile use.';
COMMENT ON COLUMN pos.fiscal_identities.tin IS 'Taxpayer identification number reference for Sales Invoice header profile use; non-production fixtures must not use real taxpayer data.';
COMMENT ON COLUMN pos.fiscal_identities.taxpayer_classification IS 'Taxpayer classification or VAT-registration posture when governed.';
COMMENT ON COLUMN pos.fiscal_identities.fiscal_identity_status IS 'Small lifecycle posture for POS Server fiscal identity foundation: DRAFT, APPROVED, or RETIRED.';
COMMENT ON COLUMN pos.fiscal_identities.min_ref IS 'Reference to BIR MIN or equivalent registration metadata; reference only until accreditation details are finalized.';
COMMENT ON COLUMN pos.fiscal_identities.ptu_ref IS 'Reference to BIR PTU metadata; reference only until accreditation details are finalized.';
COMMENT ON COLUMN pos.fiscal_identities.serial_ref IS 'Reference to registered machine/device serial metadata; reference only.';
COMMENT ON COLUMN pos.fiscal_identities.supplier_ref IS 'Reference to supplier/accreditation context; reference only.';
COMMENT ON COLUMN pos.fiscal_identities.metadata_json IS 'Flexible metadata for unresolved registration attributes. Must remain a JSON object.';
