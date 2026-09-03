-- ExitPass POS Server Sales Invoice header profile foundation.
-- Stores approved statutory and site-specific header configuration for Site POS Server issuance.

CREATE TABLE IF NOT EXISTS pos.sales_invoice_header_profiles (
    sales_invoice_header_profile_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    site_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    profile_version text NOT NULL,
    template_version text NOT NULL,
    presentation_version text NOT NULL,
    pos_serial_number text NULL,
    machine_identification_number text NULL,
    parking_location_display text NULL,
    supplier_developer_registered_name text NULL,
    supplier_developer_address text NULL,
    supplier_developer_tin text NULL,
    bir_accreditation_number text NULL,
    bir_accreditation_issued_date date NULL,
    bir_accreditation_valid_until date NULL,
    ptu_number text NULL,
    ptu_issued_date date NULL,
    sales_invoice_legal_statement text NULL,
    customer_service_footer text NULL,
    effective_from timestamptz NOT NULL,
    effective_to timestamptz NULL,
    lifecycle_status text NOT NULL DEFAULT 'DRAFT',
    approved_at timestamptz NULL,
    approved_by_ref text NULL,
    retired_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_ref text NULL,
    updated_by_ref text NULL,
    CONSTRAINT pk_sales_invoice_header_profiles PRIMARY KEY (sales_invoice_header_profile_id),
    CONSTRAINT fk_sales_invoice_header_profiles__fiscal_identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_sales_invoice_header_profiles__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT uq_sales_invoice_header_profiles__site_pos_profile_version UNIQUE (site_pos_server_id, profile_version),
    CONSTRAINT ck_sales_invoice_header_profiles__profile_version_not_blank CHECK (char_length(btrim(profile_version)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__supported_template_version CHECK (template_version = 'digital-sales-invoice-json-v1'),
    CONSTRAINT ck_sales_invoice_header_profiles__supported_presentation_version CHECK (presentation_version = 'digital-sales-invoice-presentation-json-v1'),
    CONSTRAINT ck_sales_invoice_header_profiles__pos_serial_number_not_blank CHECK (pos_serial_number IS NULL OR char_length(btrim(pos_serial_number)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__min_not_blank CHECK (machine_identification_number IS NULL OR char_length(btrim(machine_identification_number)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__parking_location_not_blank CHECK (parking_location_display IS NULL OR char_length(btrim(parking_location_display)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__supplier_name_not_blank CHECK (supplier_developer_registered_name IS NULL OR char_length(btrim(supplier_developer_registered_name)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__supplier_address_not_blank CHECK (supplier_developer_address IS NULL OR char_length(btrim(supplier_developer_address)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__supplier_tin_not_blank CHECK (supplier_developer_tin IS NULL OR char_length(btrim(supplier_developer_tin)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__bir_accreditation_number_not_blank CHECK (bir_accreditation_number IS NULL OR char_length(btrim(bir_accreditation_number)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__bir_dates_order CHECK (
        bir_accreditation_issued_date IS NULL OR
        bir_accreditation_valid_until IS NULL OR
        bir_accreditation_valid_until >= bir_accreditation_issued_date
    ),
    CONSTRAINT ck_sales_invoice_header_profiles__ptu_number_not_blank CHECK (ptu_number IS NULL OR char_length(btrim(ptu_number)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__legal_statement_not_blank CHECK (sales_invoice_legal_statement IS NULL OR char_length(btrim(sales_invoice_legal_statement)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__footer_not_blank CHECK (customer_service_footer IS NULL OR char_length(btrim(customer_service_footer)) > 0),
    CONSTRAINT ck_sales_invoice_header_profiles__effective_window CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT ck_sales_invoice_header_profiles__lifecycle_status CHECK (lifecycle_status IN ('DRAFT', 'APPROVED', 'RETIRED')),
    CONSTRAINT ck_sales_invoice_header_profiles__approved_fields CHECK (
        (lifecycle_status <> 'APPROVED') OR (approved_at IS NOT NULL AND approved_by_ref IS NOT NULL AND char_length(btrim(approved_by_ref)) > 0)
    ),
    CONSTRAINT ck_sales_invoice_header_profiles__approved_completeness CHECK (
        lifecycle_status <> 'APPROVED' OR (
            pos_serial_number IS NOT NULL AND char_length(btrim(pos_serial_number)) > 0 AND
            machine_identification_number IS NOT NULL AND char_length(btrim(machine_identification_number)) > 0 AND
            parking_location_display IS NOT NULL AND char_length(btrim(parking_location_display)) > 0 AND
            supplier_developer_registered_name IS NOT NULL AND char_length(btrim(supplier_developer_registered_name)) > 0 AND
            supplier_developer_address IS NOT NULL AND char_length(btrim(supplier_developer_address)) > 0 AND
            supplier_developer_tin IS NOT NULL AND char_length(btrim(supplier_developer_tin)) > 0 AND
            bir_accreditation_number IS NOT NULL AND char_length(btrim(bir_accreditation_number)) > 0 AND
            bir_accreditation_issued_date IS NOT NULL AND
            bir_accreditation_valid_until IS NOT NULL AND
            ptu_number IS NOT NULL AND char_length(btrim(ptu_number)) > 0 AND
            ptu_issued_date IS NOT NULL AND
            sales_invoice_legal_statement IS NOT NULL AND char_length(btrim(sales_invoice_legal_statement)) > 0 AND
            customer_service_footer IS NOT NULL AND char_length(btrim(customer_service_footer)) > 0
        )
    ),
    CONSTRAINT ck_sales_invoice_header_profiles__retired_fields CHECK (
        (lifecycle_status <> 'RETIRED') OR retired_at IS NOT NULL
    ),
    CONSTRAINT ck_sales_invoice_header_profiles__created_by_ref_not_blank CHECK (
        created_by_ref IS NULL OR char_length(btrim(created_by_ref)) > 0
    ),
    CONSTRAINT ck_sales_invoice_header_profiles__updated_by_ref_not_blank CHECK (
        updated_by_ref IS NULL OR char_length(btrim(updated_by_ref)) > 0
    )
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_sales_invoice_header_profiles__one_open_approved
    ON pos.sales_invoice_header_profiles (site_pos_server_id)
    WHERE lifecycle_status = 'APPROVED' AND retired_at IS NULL AND effective_to IS NULL;

COMMENT ON TABLE pos.sales_invoice_header_profiles IS 'Approved Sales Invoice statutory/site header profiles resolved by Site POS Server at issuance and snapshotted into fiscal documents.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.site_id IS 'Site identifier used for profile ownership and resolution. POS Server stores the approved site reference only.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.supplier_developer_registered_name IS 'Registered name of the POS software supplier/developer, distinct from the merchant fiscal identity.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.supplier_developer_address IS 'Registered address of the POS software supplier/developer.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.supplier_developer_tin IS 'Taxpayer identification number of the POS software supplier/developer.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.bir_accreditation_issued_date IS 'BIR accreditation issued date. Distinct from PTU issued date.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.bir_accreditation_valid_until IS 'BIR accreditation valid-until date. Must not precede BIR accreditation issued date.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.ptu_issued_date IS 'PTU issued date. Distinct from BIR accreditation dates.';
