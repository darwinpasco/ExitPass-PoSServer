CREATE TABLE IF NOT EXISTS pos.annex_e1_report_fact_sources (
    annex_e_report_id uuid NOT NULL,
    annex_e1_period_accounting_fact_id uuid NOT NULL,
    source_order integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_annex_e1_report_fact_sources PRIMARY KEY (annex_e_report_id, annex_e1_period_accounting_fact_id),
    CONSTRAINT fk_annex_e1_report_fact_sources__report FOREIGN KEY (annex_e_report_id) REFERENCES pos.annex_e_reports (annex_e_report_id),
    CONSTRAINT fk_annex_e1_report_fact_sources__fact FOREIGN KEY (annex_e1_period_accounting_fact_id) REFERENCES pos.annex_e1_period_accounting_facts (annex_e1_period_accounting_fact_id),
    CONSTRAINT uq_annex_e1_report_fact_sources__order UNIQUE (annex_e_report_id, source_order),
    CONSTRAINT ck_annex_e1_report_fact_sources__order CHECK (source_order BETWEEN 1 AND 7)
);
CREATE OR REPLACE TRIGGER trg_annex_e1_report_fact_sources_immutable BEFORE UPDATE OR DELETE ON pos.annex_e1_report_fact_sources FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();
