\set ON_ERROR_STOP on

BEGIN;

ALTER TABLE pos.sales_invoice_header_profiles
    ADD COLUMN IF NOT EXISTS supplier_developer_registered_name text NULL,
    ADD COLUMN IF NOT EXISTS supplier_developer_address text NULL,
    ADD COLUMN IF NOT EXISTS supplier_developer_tin text NULL;

ALTER TABLE pos.sales_invoice_header_profiles
    DROP CONSTRAINT IF EXISTS ck_sales_invoice_header_profiles__supplier_name_not_blank,
    DROP CONSTRAINT IF EXISTS ck_sales_invoice_header_profiles__supplier_address_not_blank,
    DROP CONSTRAINT IF EXISTS ck_sales_invoice_header_profiles__supplier_tin_not_blank,
    DROP CONSTRAINT IF EXISTS ck_sales_invoice_header_profiles__approved_completeness;

ALTER TABLE pos.sales_invoice_header_profiles
    ADD CONSTRAINT ck_sales_invoice_header_profiles__supplier_name_not_blank
        CHECK (supplier_developer_registered_name IS NULL OR char_length(btrim(supplier_developer_registered_name)) > 0),
    ADD CONSTRAINT ck_sales_invoice_header_profiles__supplier_address_not_blank
        CHECK (supplier_developer_address IS NULL OR char_length(btrim(supplier_developer_address)) > 0),
    ADD CONSTRAINT ck_sales_invoice_header_profiles__supplier_tin_not_blank
        CHECK (supplier_developer_tin IS NULL OR char_length(btrim(supplier_developer_tin)) > 0),
    ADD CONSTRAINT ck_sales_invoice_header_profiles__approved_completeness CHECK (
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
    );

ALTER TABLE pos.fiscal_document_header_snapshots
    ADD COLUMN IF NOT EXISTS supplier_developer_registered_name text NULL,
    ADD COLUMN IF NOT EXISTS supplier_developer_address text NULL,
    ADD COLUMN IF NOT EXISTS supplier_developer_tin text NULL;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pos.fiscal_document_header_snapshots
        WHERE supplier_developer_registered_name IS NULL
           OR supplier_developer_address IS NULL
           OR supplier_developer_tin IS NULL
    ) THEN
        RAISE EXCEPTION USING
            MESSAGE = 'Existing fiscal snapshots require governed supplier identity backfill before PR #133 migration.',
            ERRCODE = 'check_violation';
    END IF;
END $$;

ALTER TABLE pos.fiscal_document_header_snapshots
    ALTER COLUMN supplier_developer_registered_name SET NOT NULL,
    ALTER COLUMN supplier_developer_address SET NOT NULL,
    ALTER COLUMN supplier_developer_tin SET NOT NULL;

ALTER TABLE pos.fiscal_document_header_snapshots
    DROP CONSTRAINT IF EXISTS ck_fiscal_document_header_snapshots__supplier_name_not_blank,
    DROP CONSTRAINT IF EXISTS ck_fiscal_document_header_snapshots__supplier_address_not_blank,
    DROP CONSTRAINT IF EXISTS ck_fiscal_document_header_snapshots__supplier_tin_not_blank;

ALTER TABLE pos.fiscal_document_header_snapshots
    ADD CONSTRAINT ck_fiscal_document_header_snapshots__supplier_name_not_blank
        CHECK (char_length(btrim(supplier_developer_registered_name)) > 0),
    ADD CONSTRAINT ck_fiscal_document_header_snapshots__supplier_address_not_blank
        CHECK (char_length(btrim(supplier_developer_address)) > 0),
    ADD CONSTRAINT ck_fiscal_document_header_snapshots__supplier_tin_not_blank
        CHECK (char_length(btrim(supplier_developer_tin)) > 0);

COMMENT ON COLUMN pos.sales_invoice_header_profiles.supplier_developer_registered_name IS
    'Registered name of the POS software supplier/developer, distinct from merchant identity.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.supplier_developer_address IS
    'Registered address of the POS software supplier/developer.';
COMMENT ON COLUMN pos.sales_invoice_header_profiles.supplier_developer_tin IS
    'Taxpayer identification number of the POS software supplier/developer.';
COMMENT ON COLUMN pos.fiscal_document_header_snapshots.supplier_developer_registered_name IS
    'Immutable POS software supplier/developer registered name, distinct from merchant identity.';

COMMIT;
