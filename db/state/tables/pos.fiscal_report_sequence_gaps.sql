-- Immutable classified sequence gaps captured under a report fiscal-number range.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_sequence_gaps (
    fiscal_report_sequence_gap_id uuid NOT NULL,
    fiscal_report_fiscal_number_range_id uuid NOT NULL,
    gap_sequence_value bigint NOT NULL,
    gap_classification_code_id uuid NOT NULL,
    source_sequence_gap_audit_id uuid NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_sequence_gaps PRIMARY KEY (fiscal_report_sequence_gap_id),
    CONSTRAINT fk_fiscal_report_sequence_gaps__range FOREIGN KEY (fiscal_report_fiscal_number_range_id)
        REFERENCES pos.fiscal_report_fiscal_number_ranges (fiscal_report_fiscal_number_range_id),
    CONSTRAINT fk_fiscal_report_sequence_gaps__classification FOREIGN KEY (gap_classification_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_sequence_gaps__source_audit FOREIGN KEY (source_sequence_gap_audit_id)
        REFERENCES pos.fiscal_sequence_gap_audit (fiscal_sequence_gap_audit_id),
    CONSTRAINT uq_fiscal_report_sequence_gaps__range_value UNIQUE (fiscal_report_fiscal_number_range_id, gap_sequence_value),
    CONSTRAINT ck_fiscal_report_sequence_gaps__class_family CHECK (gap_classification_code_id IN (
        'd7151b60-b08f-587f-9063-dd81e6ea5663',
        'b1d73736-c963-575a-a497-fb9d16ca8914',
        '32d4908a-62bd-58b0-9c76-5e284bf8f5af',
        'cb7502b1-83bf-5d08-91ef-104cdb4a8f34'
    )),
    CONSTRAINT ck_fiscal_report_sequence_gaps__value CHECK (gap_sequence_value > 0)
);

CREATE INDEX IF NOT EXISTS ix_fiscal_report_sequence_gaps__classification
    ON pos.fiscal_report_sequence_gaps (gap_classification_code_id, fiscal_report_fiscal_number_range_id);

CREATE OR REPLACE TRIGGER trg_fiscal_report_sequence_gaps_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_report_sequence_gaps
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_report_sequence_gaps IS 'Immutable report-local classified fiscal sequence gap references. Unexplained gaps remain explicit and fail future close readiness.';
