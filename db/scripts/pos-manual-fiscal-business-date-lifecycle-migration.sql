\set ON_ERROR_STOP on
BEGIN;

LOCK TABLE pos.fiscal_reporting_periods IN ACCESS EXCLUSIVE MODE;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pos.fiscal_reporting_periods
        GROUP BY site_pos_server_id, fiscal_identity_id, currency_code, business_day_date
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'duplicate fiscal business-date labels prevent manual lifecycle migration';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pos.fiscal_reporting_periods
        WHERE period_status_code_id IN (
            '1a6f7021-bc84-5c01-afaa-c5d6685633c8',
            'f0a44431-611b-5809-b3a9-5b8be8614552')
        GROUP BY site_pos_server_id, fiscal_identity_id, currency_code
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'multiple active fiscal periods prevent manual lifecycle migration';
    END IF;
END;
$$;

ALTER TABLE pos.fiscal_reporting_periods
    ADD COLUMN IF NOT EXISTS legacy_scheduled_period_end_at timestamptz NULL,
    ADD COLUMN IF NOT EXISTS is_manual_lifecycle boolean NOT NULL DEFAULT false;

-- The former X/Z composite relationship included period_end_at. Release it before
-- converting an active legacy period from a scheduled end to an open-ended period.
ALTER TABLE pos.x_z_reports
    DROP CONSTRAINT IF EXISTS fk_x_z_reports__period_identity;

ALTER TABLE pos.fiscal_reporting_periods
    ALTER COLUMN period_end_at DROP NOT NULL,
    DROP CONSTRAINT IF EXISTS ck_fiscal_reporting_periods__period_range,
    DROP CONSTRAINT IF EXISTS ck_fiscal_reporting_periods__closed_posture;

UPDATE pos.fiscal_reporting_periods
SET legacy_scheduled_period_end_at = period_end_at,
    period_end_at = NULL,
    is_manual_lifecycle = true,
    updated_at = clock_timestamp(),
    updated_by_ref = 'pos-server:manual-lifecycle-migration'
WHERE period_status_code_id IN (
        '1a6f7021-bc84-5c01-afaa-c5d6685633c8',
        'f0a44431-611b-5809-b3a9-5b8be8614552')
  AND period_end_at IS NOT NULL;

ALTER TABLE pos.fiscal_reporting_periods
    ADD CONSTRAINT ck_fiscal_reporting_periods__period_range
        CHECK (period_end_at IS NULL OR period_end_at > period_start_at),
    ADD CONSTRAINT ck_fiscal_reporting_periods__closed_posture
        CHECK (
            (period_status_code_id = 'af7ee931-a023-507e-81a4-17adf047eb94'
             AND closed_at IS NOT NULL
             AND period_end_at IS NOT NULL
             AND (NOT is_manual_lifecycle OR period_end_at = closed_at))
            OR
            (period_status_code_id <> 'af7ee931-a023-507e-81a4-17adf047eb94'
             AND closed_at IS NULL
             AND period_end_at IS NULL)
        );

CREATE UNIQUE INDEX IF NOT EXISTS ux_fiscal_reporting_periods__active_scope
    ON pos.fiscal_reporting_periods (site_pos_server_id, fiscal_identity_id, currency_code)
    WHERE period_status_code_id IN (
        '1a6f7021-bc84-5c01-afaa-c5d6685633c8',
        'f0a44431-611b-5809-b3a9-5b8be8614552'
    );

CREATE UNIQUE INDEX IF NOT EXISTS ux_fiscal_reporting_periods__business_date_label
    ON pos.fiscal_reporting_periods (
        site_pos_server_id, fiscal_identity_id, currency_code, business_day_date
    );

CREATE TABLE IF NOT EXISTS pos.fiscal_business_date_open_operations (
    fiscal_business_date_open_operation_id uuid PRIMARY KEY,
    site_pos_server_id uuid NOT NULL REFERENCES pos.site_pos_servers(site_pos_server_id),
    fiscal_identity_id uuid NOT NULL REFERENCES pos.fiscal_identities(fiscal_identity_id),
    currency_code char(3) NOT NULL,
    operation_key text NOT NULL,
    semantic_request_hash text NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL REFERENCES pos.fiscal_reporting_periods(fiscal_reporting_period_id),
    requested_by_ref text NOT NULL,
    requested_by_display_name text NULL,
    service_identity_ref text NOT NULL,
    correlation_ref text NOT NULL,
    opened_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT uq_fiscal_business_date_open_operations__scope_key
        UNIQUE (site_pos_server_id, operation_key),
    CONSTRAINT ck_fiscal_business_date_open_operations__currency
        CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_business_date_open_operations__text
        CHECK (btrim(operation_key) <> '' AND btrim(semantic_request_hash) <> ''
            AND btrim(requested_by_ref) <> '' AND btrim(service_identity_ref) <> ''
            AND btrim(correlation_ref) <> ''
            AND (requested_by_display_name IS NULL OR btrim(requested_by_display_name) <> ''))
);

ALTER TABLE pos.fiscal_reporting_periods
    ALTER COLUMN is_manual_lifecycle SET DEFAULT true;

ALTER TABLE pos.fiscal_report_requests
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__semantic_version,
    ADD CONSTRAINT ck_fiscal_report_requests__semantic_version CHECK (
        semantic_hash_version IN (
            'pos-server-fiscal-report-request:sha256:v1',
            'pos-server-fiscal-report-request:sha256:v2-manual-close'));

ALTER TABLE pos.x_z_reports
    ADD CONSTRAINT fk_x_z_reports__period_identity
    FOREIGN KEY (fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code)
    REFERENCES pos.fiscal_reporting_periods (
        fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code
    );

COMMIT;
