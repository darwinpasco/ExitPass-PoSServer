-- ExitPass POS Server immutable Sales Invoice header snapshot.
-- Captures effective header profile facts at fiscal-document issuance so historical invoices do not change.

CREATE TABLE IF NOT EXISTS pos.fiscal_document_header_snapshots (
    fiscal_document_header_snapshot_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    sales_invoice_header_profile_id uuid NOT NULL,
    profile_version text NOT NULL,
    registered_business_name text NOT NULL,
    registered_business_address text NOT NULL,
    tin text NOT NULL,
    pos_serial_number text NOT NULL,
    machine_identification_number text NOT NULL,
    parking_location_display text NOT NULL,
    terminal_id text NULL,
    bir_accreditation_number text NOT NULL,
    bir_accreditation_issued_date date NOT NULL,
    bir_accreditation_valid_until date NOT NULL,
    ptu_number text NOT NULL,
    ptu_issued_date date NOT NULL,
    sales_invoice_legal_statement text NOT NULL,
    customer_service_footer text NOT NULL,
    template_version text NOT NULL,
    presentation_version text NOT NULL,
    effective_at timestamptz NOT NULL,
    snapshot_created_at timestamptz NOT NULL,
    snapshot_json jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_document_header_snapshots PRIMARY KEY (fiscal_document_header_snapshot_id),
    CONSTRAINT uq_fiscal_document_header_snapshots__fiscal_document UNIQUE (fiscal_document_id),
    CONSTRAINT fk_fiscal_document_header_snapshots__fiscal_document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_document_header_snapshots__fiscal_identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_fiscal_document_header_snapshots__header_profile FOREIGN KEY (sales_invoice_header_profile_id)
        REFERENCES pos.sales_invoice_header_profiles (sales_invoice_header_profile_id),
    CONSTRAINT ck_fiscal_document_header_snapshots__profile_version_not_blank CHECK (char_length(btrim(profile_version)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__registered_business_name_not_blank CHECK (char_length(btrim(registered_business_name)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__registered_business_address_not_blank CHECK (char_length(btrim(registered_business_address)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__tin_not_blank CHECK (char_length(btrim(tin)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__pos_serial_number_not_blank CHECK (char_length(btrim(pos_serial_number)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__min_not_blank CHECK (char_length(btrim(machine_identification_number)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__parking_location_not_blank CHECK (char_length(btrim(parking_location_display)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__terminal_id_not_blank CHECK (terminal_id IS NULL OR char_length(btrim(terminal_id)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__bir_accreditation_number_not_blank CHECK (char_length(btrim(bir_accreditation_number)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__bir_dates_order CHECK (bir_accreditation_valid_until >= bir_accreditation_issued_date),
    CONSTRAINT ck_fiscal_document_header_snapshots__ptu_number_not_blank CHECK (char_length(btrim(ptu_number)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__legal_statement_not_blank CHECK (char_length(btrim(sales_invoice_legal_statement)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__footer_not_blank CHECK (char_length(btrim(customer_service_footer)) > 0),
    CONSTRAINT ck_fiscal_document_header_snapshots__supported_template_version CHECK (template_version = 'digital-sales-invoice-json-v1'),
    CONSTRAINT ck_fiscal_document_header_snapshots__supported_presentation_version CHECK (presentation_version = 'digital-sales-invoice-presentation-json-v1'),
    CONSTRAINT ck_fiscal_document_header_snapshots__snapshot_json_object CHECK (jsonb_typeof(snapshot_json) = 'object')
);

COMMENT ON TABLE pos.fiscal_document_header_snapshots IS 'Immutable Sales Invoice header snapshot captured at fiscal-document issuance. Historical presentations must read this snapshot, not the current profile.';
COMMENT ON COLUMN pos.fiscal_document_header_snapshots.terminal_id IS 'Runtime terminal identifier captured from issuance context when supplied. It is not stored on the reusable taxpayer/header profile.';
COMMENT ON COLUMN pos.fiscal_document_header_snapshots.bir_accreditation_issued_date IS 'BIR accreditation issued date snapshotted at issuance. Distinct from PTU issued date.';
COMMENT ON COLUMN pos.fiscal_document_header_snapshots.bir_accreditation_valid_until IS 'BIR accreditation valid-until date snapshotted at issuance.';
COMMENT ON COLUMN pos.fiscal_document_header_snapshots.ptu_issued_date IS 'PTU issued date snapshotted at issuance. Distinct from BIR accreditation dates.';
