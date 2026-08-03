-- POS Server fiscal reporting period identity and future close posture.
-- This object does not derive business dates or execute a Z close.

CREATE TABLE IF NOT EXISTS pos.fiscal_reporting_periods (
    fiscal_reporting_period_id uuid NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    period_status_code_id uuid NOT NULL,
    business_day_date date NOT NULL,
    period_start_at timestamptz NOT NULL,
    period_end_at timestamptz NOT NULL,
    reporting_timezone_name text NOT NULL,
    business_day_cutoff_local_time time without time zone NOT NULL,
    currency_code char(3) NOT NULL,
    period_sequence bigint NOT NULL,
    expected_prior_period_id uuid NULL,
    opened_at timestamptz NOT NULL,
    closing_started_at timestamptz NULL,
    closed_at timestamptz NULL,
    created_by_ref text NOT NULL,
    updated_by_ref text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_reporting_periods PRIMARY KEY (fiscal_reporting_period_id),
    CONSTRAINT fk_fiscal_reporting_periods__contract FOREIGN KEY (fiscal_reporting_contract_version_id)
        REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    CONSTRAINT fk_fiscal_reporting_periods__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_reporting_periods__fiscal_identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_fiscal_reporting_periods__status FOREIGN KEY (period_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_reporting_periods__prior_period FOREIGN KEY (expected_prior_period_id)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT uq_fiscal_reporting_periods__site_sequence UNIQUE (site_pos_server_id, period_sequence),
    CONSTRAINT uq_fiscal_reporting_periods__scope_window UNIQUE (
        site_pos_server_id,
        fiscal_identity_id,
        period_start_at,
        period_end_at,
        currency_code
    ),
    CONSTRAINT uq_fiscal_reporting_periods__snapshot_identity UNIQUE (
        fiscal_reporting_period_id,
        fiscal_reporting_contract_version_id,
        site_pos_server_id,
        fiscal_identity_id,
        business_day_date,
        period_start_at,
        period_end_at,
        currency_code
    ),
    CONSTRAINT ck_fiscal_reporting_periods__status_family CHECK (period_status_code_id IN (
        '1a6f7021-bc84-5c01-afaa-c5d6685633c8',
        'f0a44431-611b-5809-b3a9-5b8be8614552',
        'af7ee931-a023-507e-81a4-17adf047eb94'
    )),
    CONSTRAINT ck_fiscal_reporting_periods__period_range CHECK (period_end_at > period_start_at),
    CONSTRAINT ck_fiscal_reporting_periods__timezone CHECK (char_length(btrim(reporting_timezone_name)) > 0),
    CONSTRAINT ck_fiscal_reporting_periods__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_reporting_periods__sequence CHECK (period_sequence > 0),
    CONSTRAINT ck_fiscal_reporting_periods__prior CHECK (
        expected_prior_period_id IS NULL OR expected_prior_period_id <> fiscal_reporting_period_id
    ),
    CONSTRAINT ck_fiscal_reporting_periods__actor_refs CHECK (
        char_length(btrim(created_by_ref)) > 0 AND char_length(btrim(updated_by_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_reporting_periods__lifecycle_times CHECK (
        (closing_started_at IS NULL OR closing_started_at >= opened_at)
        AND (closed_at IS NULL OR (closing_started_at IS NOT NULL AND closed_at >= closing_started_at))
    ),
    CONSTRAINT ck_fiscal_reporting_periods__closed_posture CHECK (
        (period_status_code_id = 'af7ee931-a023-507e-81a4-17adf047eb94' AND closed_at IS NOT NULL)
        OR (period_status_code_id <> 'af7ee931-a023-507e-81a4-17adf047eb94' AND closed_at IS NULL)
    )
);

CREATE INDEX IF NOT EXISTS ix_fiscal_reporting_periods__site_business_date
    ON pos.fiscal_reporting_periods (site_pos_server_id, business_day_date DESC, period_sequence DESC);

CREATE INDEX IF NOT EXISTS ix_fiscal_reporting_periods__identity_business_date
    ON pos.fiscal_reporting_periods (fiscal_identity_id, business_day_date DESC);

CREATE INDEX IF NOT EXISTS ix_fiscal_reporting_periods__status
    ON pos.fiscal_reporting_periods (period_status_code_id, site_pos_server_id);

CREATE OR REPLACE FUNCTION pos.protect_closed_fiscal_reporting_period()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.period_status_code_id = 'af7ee931-a023-507e-81a4-17adf047eb94' THEN
        RAISE EXCEPTION 'closed fiscal reporting periods are immutable'
            USING ERRCODE = 'check_violation';
    END IF;
    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE TRIGGER trg_fiscal_reporting_periods_closed_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_reporting_periods
FOR EACH ROW EXECUTE FUNCTION pos.protect_closed_fiscal_reporting_period();

COMMENT ON TABLE pos.fiscal_reporting_periods IS 'Site POS Server fiscal reporting period identity with snapshotted timezone/cutoff and future close posture. No close is executed by schema.';
COMMENT ON COLUMN pos.fiscal_reporting_periods.period_start_at IS 'Inclusive UTC instant for the governed half-open reporting interval.';
COMMENT ON COLUMN pos.fiscal_reporting_periods.period_end_at IS 'Exclusive UTC instant for the governed half-open reporting interval.';
COMMENT ON COLUMN pos.fiscal_reporting_periods.reporting_timezone_name IS 'Historical timezone-name snapshot used to derive business date; no production timezone is assumed.';
COMMENT ON COLUMN pos.fiscal_reporting_periods.business_day_cutoff_local_time IS 'Historical local business-day cutoff snapshot.';
COMMENT ON FUNCTION pos.protect_closed_fiscal_reporting_period() IS 'Allows future OPEN/CLOSING lifecycle changes but rejects UPDATE or DELETE after a period is CLOSED.';
