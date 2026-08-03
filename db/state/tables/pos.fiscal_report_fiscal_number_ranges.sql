-- Immutable fiscal-number ranges captured by one X/Z snapshot.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_fiscal_number_ranges (
    fiscal_report_fiscal_number_range_id uuid NOT NULL,
    x_z_report_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    fiscal_sequence_policy_id uuid NOT NULL,
    fiscal_series text NOT NULL,
    first_sequence_value bigint NOT NULL,
    last_sequence_value bigint NOT NULL,
    first_fiscal_number text NOT NULL,
    last_fiscal_number text NOT NULL,
    qualifying_document_count bigint NOT NULL,
    gap_count bigint NOT NULL,
    currency_code char(3) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_fiscal_number_ranges PRIMARY KEY (fiscal_report_fiscal_number_range_id),
    CONSTRAINT fk_fiscal_report_fiscal_number_ranges__report_scope FOREIGN KEY (x_z_report_id, fiscal_identity_id, currency_code)
        REFERENCES pos.x_z_reports (x_z_report_id, fiscal_identity_id, currency_code),
    CONSTRAINT fk_fiscal_report_fiscal_number_ranges__sequence_policy FOREIGN KEY (fiscal_sequence_policy_id)
        REFERENCES pos.fiscal_sequence_policies (fiscal_sequence_policy_id),
    CONSTRAINT uq_fiscal_report_fiscal_number_ranges__report_policy_series UNIQUE (x_z_report_id, fiscal_sequence_policy_id, fiscal_series),
    CONSTRAINT uq_fiscal_report_fiscal_number_ranges__id_range UNIQUE (fiscal_report_fiscal_number_range_id, first_sequence_value, last_sequence_value),
    CONSTRAINT ck_fiscal_report_fiscal_number_ranges__series CHECK (char_length(btrim(fiscal_series)) > 0),
    CONSTRAINT ck_fiscal_report_fiscal_number_ranges__sequence CHECK (
        first_sequence_value > 0 AND last_sequence_value >= first_sequence_value
    ),
    CONSTRAINT ck_fiscal_report_fiscal_number_ranges__numbers CHECK (
        char_length(btrim(first_fiscal_number)) > 0 AND char_length(btrim(last_fiscal_number)) > 0
    ),
    CONSTRAINT ck_fiscal_report_fiscal_number_ranges__counts CHECK (
        qualifying_document_count >= 0 AND gap_count >= 0
    ),
    CONSTRAINT ck_fiscal_report_fiscal_number_ranges__currency CHECK (currency_code ~ '^[A-Z]{3}$')
);

CREATE INDEX IF NOT EXISTS ix_fiscal_report_fiscal_number_ranges__identity_series
    ON pos.fiscal_report_fiscal_number_ranges (fiscal_identity_id, fiscal_series, first_sequence_value, last_sequence_value);

CREATE OR REPLACE TRIGGER trg_fiscal_report_fiscal_number_ranges_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_report_fiscal_number_ranges
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_report_fiscal_number_ranges IS 'Immutable fiscal sequence range snapshots by report, fiscal identity, policy, and series. Runtime gap calculation is not implemented.';
