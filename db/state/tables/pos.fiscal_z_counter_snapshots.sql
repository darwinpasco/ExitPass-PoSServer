-- Immutable Z-only reset counter, Z counter, and GTA transition facts.
-- This table does not execute a state transition.

CREATE TABLE IF NOT EXISTS pos.fiscal_z_counter_snapshots (
    fiscal_z_counter_snapshot_id uuid NOT NULL,
    fiscal_z_close_state_id uuid NOT NULL,
    x_z_report_id uuid NOT NULL,
    report_kind_code_id uuid NOT NULL,
    expected_prior_period_id uuid NULL,
    expected_state_version bigint NOT NULL,
    resulting_state_version bigint NOT NULL,
    previous_reset_counter_value bigint NOT NULL,
    resulting_reset_counter_value bigint NOT NULL,
    previous_z_counter_value bigint NOT NULL,
    resulting_z_counter_value bigint NOT NULL,
    previous_grand_total_amount_minor_units bigint NOT NULL,
    current_period_amount_minor_units bigint NOT NULL,
    resulting_grand_total_amount_minor_units bigint NOT NULL,
    currency_code char(3) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_z_counter_snapshots PRIMARY KEY (fiscal_z_counter_snapshot_id),
    CONSTRAINT fk_fiscal_z_counter_snapshots__state FOREIGN KEY (fiscal_z_close_state_id)
        REFERENCES pos.fiscal_z_close_states (fiscal_z_close_state_id),
    CONSTRAINT fk_fiscal_z_counter_snapshots__z_report FOREIGN KEY (x_z_report_id, report_kind_code_id)
        REFERENCES pos.x_z_reports (x_z_report_id, report_kind_code_id),
    CONSTRAINT fk_fiscal_z_counter_snapshots__report_currency FOREIGN KEY (x_z_report_id, currency_code)
        REFERENCES pos.x_z_reports (x_z_report_id, currency_code),
    CONSTRAINT fk_fiscal_z_counter_snapshots__prior_period FOREIGN KEY (expected_prior_period_id)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT uq_fiscal_z_counter_snapshots__report UNIQUE (x_z_report_id),
    CONSTRAINT uq_fiscal_z_counter_snapshots__state_version UNIQUE (
        fiscal_z_close_state_id, resulting_state_version
    ),
    CONSTRAINT ck_fiscal_z_counter_snapshots__z_kind CHECK (
        report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
    ),
    CONSTRAINT ck_fiscal_z_counter_snapshots__counters CHECK (
        previous_reset_counter_value >= 0
        AND resulting_reset_counter_value = previous_reset_counter_value
        AND previous_z_counter_value >= 0
        AND resulting_z_counter_value = previous_z_counter_value + 1
    ),
    CONSTRAINT ck_fiscal_z_counter_snapshots__version CHECK (
        expected_state_version >= 1
        AND resulting_state_version = expected_state_version + 1
    ),
    CONSTRAINT ck_fiscal_z_counter_snapshots__gta CHECK (
        previous_grand_total_amount_minor_units >= 0
        AND current_period_amount_minor_units >= 0
        AND resulting_grand_total_amount_minor_units
            = previous_grand_total_amount_minor_units + current_period_amount_minor_units
    ),
    CONSTRAINT ck_fiscal_z_counter_snapshots__currency CHECK (currency_code ~ '^[A-Z]{3}$')
);

CREATE INDEX IF NOT EXISTS ix_fiscal_z_counter_snapshots__prior_period
    ON pos.fiscal_z_counter_snapshots (expected_prior_period_id)
    WHERE expected_prior_period_id IS NOT NULL;

CREATE OR REPLACE TRIGGER trg_fiscal_z_counter_snapshots_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_z_counter_snapshots
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_z_counter_snapshots IS 'Immutable Z-only expected counter and GTA transition snapshot. Runtime close and counter mutation remain unimplemented.';

ALTER TABLE pos.fiscal_z_counter_snapshots
    ADD COLUMN IF NOT EXISTS fiscal_z_close_state_id uuid NULL,
    ADD COLUMN IF NOT EXISTS expected_state_version bigint NULL,
    ADD COLUMN IF NOT EXISTS resulting_state_version bigint NULL;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pos.fiscal_z_counter_snapshots
        WHERE fiscal_z_close_state_id IS NULL
           OR expected_state_version IS NULL
           OR resulting_state_version IS NULL
    ) THEN
        RAISE EXCEPTION 'existing Z counter snapshots require verified canonical state migration';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'pos.fiscal_z_counter_snapshots'::regclass
          AND conname = 'fk_fiscal_z_counter_snapshots__state'
    ) THEN
        ALTER TABLE pos.fiscal_z_counter_snapshots
            ADD CONSTRAINT fk_fiscal_z_counter_snapshots__state
            FOREIGN KEY (fiscal_z_close_state_id)
            REFERENCES pos.fiscal_z_close_states (fiscal_z_close_state_id);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'pos.fiscal_z_counter_snapshots'::regclass
          AND conname = 'uq_fiscal_z_counter_snapshots__state_version'
    ) THEN
        ALTER TABLE pos.fiscal_z_counter_snapshots
            ADD CONSTRAINT uq_fiscal_z_counter_snapshots__state_version
            UNIQUE (fiscal_z_close_state_id, resulting_state_version);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'pos.fiscal_z_counter_snapshots'::regclass
          AND conname = 'ck_fiscal_z_counter_snapshots__version'
    ) THEN
        ALTER TABLE pos.fiscal_z_counter_snapshots
            ADD CONSTRAINT ck_fiscal_z_counter_snapshots__version
            CHECK (expected_state_version >= 1 AND resulting_state_version = expected_state_version + 1);
    END IF;
END;
$$;

ALTER TABLE pos.fiscal_z_counter_snapshots
    ALTER COLUMN fiscal_z_close_state_id SET NOT NULL,
    ALTER COLUMN expected_state_version SET NOT NULL,
    ALTER COLUMN resulting_state_version SET NOT NULL;

-- Replace the pre-approval posture on upgrade as well as clean rebuild. An
-- existing row that advanced reset during ordinary Z close blocks migration.
ALTER TABLE pos.fiscal_z_counter_snapshots
    DROP CONSTRAINT IF EXISTS ck_fiscal_z_counter_snapshots__counters;

ALTER TABLE pos.fiscal_z_counter_snapshots
    ADD CONSTRAINT ck_fiscal_z_counter_snapshots__counters CHECK (
        previous_reset_counter_value >= 0
        AND resulting_reset_counter_value = previous_reset_counter_value
        AND previous_z_counter_value >= 0
        AND resulting_z_counter_value = previous_z_counter_value + 1
    );
