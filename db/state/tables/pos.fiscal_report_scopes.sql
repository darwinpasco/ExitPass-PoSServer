-- POS Server fiscal report scope bound to a first-class reporting period.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_scopes (
    fiscal_report_scope_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL,
    scope_type_code_id uuid NOT NULL,
    fiscal_series text NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_scopes PRIMARY KEY (fiscal_report_scope_id),
    CONSTRAINT fk_fiscal_report_scopes__request FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_report_scopes__period FOREIGN KEY (fiscal_reporting_period_id)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT fk_fiscal_report_scopes__scope_type FOREIGN KEY (scope_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_report_scopes__request UNIQUE (fiscal_report_request_id),
    CONSTRAINT ck_fiscal_report_scopes__scope_family CHECK (
        scope_type_code_id = 'ce034742-16fe-578c-85af-a05e62129f21'
    ),
    CONSTRAINT ck_fiscal_report_scopes__series CHECK (
        fiscal_series IS NULL OR char_length(btrim(fiscal_series)) > 0
    )
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'pos' AND table_name = 'fiscal_report_scopes' AND column_name = 'scope_context'
    ) AND EXISTS (SELECT 1 FROM pos.fiscal_report_scopes) THEN
        RAISE EXCEPTION 'cannot harden non-empty legacy pos.fiscal_report_scopes; archive and migrate governed scope facts first';
    END IF;
END;
$$;

ALTER TABLE pos.fiscal_report_scopes
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_series text NULL;

ALTER TABLE pos.fiscal_report_scopes
    DROP COLUMN IF EXISTS site_pos_server_id,
    DROP COLUMN IF EXISTS channel_terminal_id,
    DROP COLUMN IF EXISTS business_day_date,
    DROP COLUMN IF EXISTS period_start_at,
    DROP COLUMN IF EXISTS period_end_at,
    DROP COLUMN IF EXISTS begin_fiscal_document_id,
    DROP COLUMN IF EXISTS end_fiscal_document_id,
    DROP COLUMN IF EXISTS reset_counter_state_id,
    DROP COLUMN IF EXISTS z_counter_state_id,
    DROP COLUMN IF EXISTS scope_context;

ALTER TABLE pos.fiscal_report_scopes ALTER COLUMN fiscal_reporting_period_id SET NOT NULL;

ALTER TABLE pos.fiscal_report_scopes
    DROP CONSTRAINT IF EXISTS fk_fiscal_report_scopes__period,
    DROP CONSTRAINT IF EXISTS uq_fiscal_report_scopes__request,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_scopes__scope_family,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_scopes__series;

ALTER TABLE pos.fiscal_report_scopes
    ADD CONSTRAINT fk_fiscal_report_scopes__period FOREIGN KEY (fiscal_reporting_period_id) REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    ADD CONSTRAINT uq_fiscal_report_scopes__request UNIQUE (fiscal_report_request_id),
    ADD CONSTRAINT ck_fiscal_report_scopes__scope_family CHECK (scope_type_code_id = 'ce034742-16fe-578c-85af-a05e62129f21'),
    ADD CONSTRAINT ck_fiscal_report_scopes__series CHECK (fiscal_series IS NULL OR char_length(btrim(fiscal_series)) > 0);

COMMENT ON TABLE pos.fiscal_report_scopes IS 'One governed period scope per report request. Site, fiscal identity, business time, cutoff, and currency are owned by the referenced period.';
