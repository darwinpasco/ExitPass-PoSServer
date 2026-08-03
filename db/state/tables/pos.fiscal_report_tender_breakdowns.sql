-- Immutable tender aggregates owned by one X/Z snapshot.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_tender_breakdowns (
    fiscal_report_tender_breakdown_id uuid NOT NULL,
    x_z_report_id uuid NOT NULL,
    tender_classification_code_id uuid NOT NULL,
    tender_transaction_count bigint NOT NULL,
    amount_minor_units bigint NOT NULL,
    currency_code char(3) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_tender_breakdowns PRIMARY KEY (fiscal_report_tender_breakdown_id),
    CONSTRAINT fk_fiscal_report_tender_breakdowns__report_currency FOREIGN KEY (x_z_report_id, currency_code)
        REFERENCES pos.x_z_reports (x_z_report_id, currency_code),
    CONSTRAINT fk_fiscal_report_tender_breakdowns__classification FOREIGN KEY (tender_classification_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_report_tender_breakdowns__report_class UNIQUE (x_z_report_id, tender_classification_code_id),
    CONSTRAINT ck_fiscal_report_tender_breakdowns__class_family CHECK (tender_classification_code_id IN (
        '47a97fd9-987d-5ea2-a8d0-d293343bf7f6',
        '1f429942-5bec-585b-ae4c-a5025ec59827',
        '156acddb-4eee-5653-8b47-55375928c15b',
        'd0eadc45-339d-5376-bad4-ca26167edc25',
        '6f99281a-4847-5699-96e8-5138e82d01cb'
    )),
    CONSTRAINT ck_fiscal_report_tender_breakdowns__values CHECK (
        tender_transaction_count >= 0 AND amount_minor_units >= 0
    ),
    CONSTRAINT ck_fiscal_report_tender_breakdowns__currency CHECK (currency_code ~ '^[A-Z]{3}$')
);

CREATE INDEX IF NOT EXISTS ix_fiscal_report_tender_breakdowns__classification
    ON pos.fiscal_report_tender_breakdowns (tender_classification_code_id, x_z_report_id);

CREATE OR REPLACE TRIGGER trg_fiscal_report_tender_breakdowns_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_report_tender_breakdowns
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_report_tender_breakdowns IS 'Immutable report tender totals by governed reporting classification. Tender references and payment finality remain on fiscal tenders/documents.';
