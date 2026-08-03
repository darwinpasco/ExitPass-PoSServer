-- Immutable Z-only reset counter, Z counter, and GTA transition facts.
-- This table does not execute a state transition.

CREATE TABLE IF NOT EXISTS pos.fiscal_z_counter_snapshots (
    fiscal_z_counter_snapshot_id uuid NOT NULL,
    x_z_report_id uuid NOT NULL,
    report_kind_code_id uuid NOT NULL,
    expected_prior_period_id uuid NULL,
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
    CONSTRAINT fk_fiscal_z_counter_snapshots__z_report FOREIGN KEY (x_z_report_id, report_kind_code_id)
        REFERENCES pos.x_z_reports (x_z_report_id, report_kind_code_id),
    CONSTRAINT fk_fiscal_z_counter_snapshots__report_currency FOREIGN KEY (x_z_report_id, currency_code)
        REFERENCES pos.x_z_reports (x_z_report_id, currency_code),
    CONSTRAINT fk_fiscal_z_counter_snapshots__prior_period FOREIGN KEY (expected_prior_period_id)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT uq_fiscal_z_counter_snapshots__report UNIQUE (x_z_report_id),
    CONSTRAINT ck_fiscal_z_counter_snapshots__z_kind CHECK (
        report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
    ),
    CONSTRAINT ck_fiscal_z_counter_snapshots__counters CHECK (
        previous_reset_counter_value >= 0
        AND resulting_reset_counter_value >= previous_reset_counter_value
        AND previous_z_counter_value >= 0
        AND resulting_z_counter_value = previous_z_counter_value + 1
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
