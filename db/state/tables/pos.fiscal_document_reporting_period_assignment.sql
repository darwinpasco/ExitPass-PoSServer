-- Additive legacy upgrade and enforcement for first-class fiscal-document reporting-period assignment.

ALTER TABLE pos.fiscal_documents
    ADD COLUMN IF NOT EXISTS currency_code char(3) NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL;

WITH monetary_currency AS (
    SELECT fiscal_document_id, currency_code FROM pos.fiscal_document_lines
    UNION ALL SELECT fiscal_document_id, currency_code FROM pos.fiscal_tenders
    UNION ALL SELECT fiscal_document_id, currency_code FROM pos.fiscal_tax_details
    UNION ALL SELECT fiscal_document_id, currency_code FROM pos.fiscal_discount_privilege_details
    UNION ALL SELECT fiscal_document_id, currency_code FROM pos.fiscal_totals
), unambiguous_currency AS (
    SELECT fiscal_document_id, min(currency_code)::char(3) AS currency_code
    FROM monetary_currency
    GROUP BY fiscal_document_id
    HAVING count(DISTINCT currency_code) = 1
)
UPDATE pos.fiscal_documents document
SET currency_code = source.currency_code
FROM unambiguous_currency source
WHERE document.fiscal_document_id = source.fiscal_document_id
  AND document.currency_code IS NULL;

WITH candidates AS (
    SELECT document.fiscal_document_id,
           period.fiscal_reporting_period_id,
           count(*) OVER (PARTITION BY document.fiscal_document_id) AS candidate_count
    FROM pos.fiscal_documents document
    JOIN pos.fiscal_reporting_periods period
      ON period.site_pos_server_id = document.site_pos_server_id
     AND period.fiscal_identity_id = document.fiscal_identity_id
     AND period.currency_code = document.currency_code
     AND document.created_at >= period.period_start_at
     AND document.created_at < period.period_end_at
    WHERE document.fiscal_reporting_period_id IS NULL
      AND document.fiscal_identity_id IS NOT NULL
      AND document.currency_code IS NOT NULL
), unambiguous_period AS (
    SELECT fiscal_document_id, fiscal_reporting_period_id
    FROM candidates
    WHERE candidate_count = 1
)
UPDATE pos.fiscal_documents document
SET fiscal_reporting_period_id = source.fiscal_reporting_period_id
FROM unambiguous_period source
WHERE document.fiscal_document_id = source.fiscal_document_id
  AND document.fiscal_reporting_period_id IS NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'pos.fiscal_documents'::regclass
          AND conname = 'fk_fiscal_documents__reporting_period_scope'
    ) THEN
        ALTER TABLE pos.fiscal_documents
            ADD CONSTRAINT fk_fiscal_documents__reporting_period_scope
            FOREIGN KEY (fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code)
            REFERENCES pos.fiscal_reporting_periods (
                fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code
            );
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'pos.fiscal_documents'::regclass
          AND conname = 'ck_fiscal_documents__currency'
    ) THEN
        ALTER TABLE pos.fiscal_documents
            ADD CONSTRAINT ck_fiscal_documents__currency
            CHECK (currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$');
    END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS ix_fiscal_documents__reporting_period
    ON pos.fiscal_documents (fiscal_reporting_period_id, created_at, fiscal_document_id)
    WHERE fiscal_reporting_period_id IS NOT NULL;

CREATE OR REPLACE FUNCTION pos.validate_fiscal_document_reporting_period_assignment()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    period_row pos.fiscal_reporting_periods%ROWTYPE;
BEGIN
    IF TG_OP = 'UPDATE' THEN
        IF NEW.fiscal_reporting_period_id IS DISTINCT FROM OLD.fiscal_reporting_period_id
           OR NEW.site_pos_server_id IS DISTINCT FROM OLD.site_pos_server_id
           OR NEW.fiscal_identity_id IS DISTINCT FROM OLD.fiscal_identity_id
           OR NEW.currency_code IS DISTINCT FROM OLD.currency_code
           OR NEW.created_at IS DISTINCT FROM OLD.created_at THEN
            RAISE EXCEPTION 'fiscal document reporting-period assignment is immutable'
                USING ERRCODE = 'check_violation';
        END IF;
        RETURN NEW;
    END IF;

    IF NEW.fiscal_reporting_period_id IS NULL
       OR NEW.fiscal_identity_id IS NULL
       OR NEW.currency_code IS NULL THEN
        RAISE EXCEPTION 'new fiscal document requires reporting-period assignment'
            USING ERRCODE = 'check_violation';
    END IF;

    SELECT * INTO STRICT period_row
    FROM pos.fiscal_reporting_periods period
    WHERE period.fiscal_reporting_period_id = NEW.fiscal_reporting_period_id
    FOR SHARE;

    IF period_row.period_status_code_id <> '1a6f7021-bc84-5c01-afaa-c5d6685633c8'
       OR period_row.site_pos_server_id <> NEW.site_pos_server_id
       OR period_row.fiscal_identity_id <> NEW.fiscal_identity_id
       OR period_row.currency_code <> NEW.currency_code
       OR NEW.created_at < period_row.period_start_at
       OR NEW.created_at >= period_row.period_end_at THEN
        RAISE EXCEPTION 'fiscal document reporting-period assignment is invalid'
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE TRIGGER trg_fiscal_documents_reporting_period_assignment
BEFORE INSERT OR UPDATE ON pos.fiscal_documents
FOR EACH ROW EXECUTE FUNCTION pos.validate_fiscal_document_reporting_period_assignment();

COMMENT ON FUNCTION pos.validate_fiscal_document_reporting_period_assignment() IS 'Requires new fiscal documents to be assigned to one matching OPEN half-open reporting period and prevents assignment mutation.';
