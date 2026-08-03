-- Immutable statutory and commercial discount aggregates owned by one X/Z snapshot.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_discount_breakdowns (
    fiscal_report_discount_breakdown_id uuid NOT NULL,
    x_z_report_id uuid NOT NULL,
    discount_classification_code_id uuid NOT NULL,
    qualifying_document_count bigint NOT NULL,
    discount_amount_minor_units bigint NOT NULL,
    vat_exemption_amount_minor_units bigint NOT NULL,
    currency_code char(3) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_discount_breakdowns PRIMARY KEY (fiscal_report_discount_breakdown_id),
    CONSTRAINT fk_fiscal_report_discount_breakdowns__report_currency FOREIGN KEY (x_z_report_id, currency_code)
        REFERENCES pos.x_z_reports (x_z_report_id, currency_code),
    CONSTRAINT fk_fiscal_report_discount_breakdowns__classification FOREIGN KEY (discount_classification_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_report_discount_breakdowns__report_class UNIQUE (x_z_report_id, discount_classification_code_id),
    CONSTRAINT ck_fiscal_report_discount_breakdowns__class_family CHECK (discount_classification_code_id IN (
        '6fc90db9-12d4-509f-948e-04beaa7de371',
        '10b9c4ac-69eb-56b4-937f-edf81411c035',
        '419b274d-9ca3-522e-9a78-0a352d19c893',
        '349e303c-7fb3-5f4f-a38f-48cc593c09a8',
        '388289a8-283c-5e61-bee6-a352dc3e25c8',
        'c7d13ec7-38f4-5229-8022-456fdf6f0ff1'
    )),
    CONSTRAINT ck_fiscal_report_discount_breakdowns__values CHECK (
        qualifying_document_count >= 0
        AND discount_amount_minor_units >= 0
        AND vat_exemption_amount_minor_units >= 0
    ),
    CONSTRAINT ck_fiscal_report_discount_breakdowns__currency CHECK (currency_code ~ '^[A-Z]{3}$')
);

CREATE INDEX IF NOT EXISTS ix_fiscal_report_discount_breakdowns__classification
    ON pos.fiscal_report_discount_breakdowns (discount_classification_code_id, x_z_report_id);

CREATE OR REPLACE TRIGGER trg_fiscal_report_discount_breakdowns_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_report_discount_breakdowns
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_report_discount_breakdowns IS 'Immutable statutory and commercial discount totals. Statutory and promotional classifications remain distinct and contain no beneficiary or evidence data.';
