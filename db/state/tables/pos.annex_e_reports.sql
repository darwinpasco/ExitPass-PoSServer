-- Immutable Annex E-1 per-Z row snapshots with exact 32-position evidence.

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='pos' AND table_name='annex_e_reports' AND column_name='fiscal_report_request_id'
    ) THEN
        IF EXISTS (SELECT 1 FROM pos.annex_e_reports) THEN
            RAISE EXCEPTION 'cannot replace non-empty legacy pos.annex_e_reports without an approved historical conversion';
        END IF;
        DROP TABLE pos.annex_e_reports;
    END IF;
END;
$$;

CREATE TABLE IF NOT EXISTS pos.annex_e_reports (
    annex_e_report_id uuid NOT NULL,
    annex_e1_workbook_id uuid NOT NULL,
    row_sequence integer NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL,
    governing_z_report_id uuid NOT NULL,
    governing_report_kind_code_id uuid NOT NULL,
    related_bir_sales_summary_report_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    currency_code char(3) NOT NULL,
    business_day_date date NOT NULL,
    beginning_fiscal_number text NULL,
    ending_fiscal_number text NULL,
    d04_gta_ending bigint NOT NULL,
    d05_gta_beginning bigint NOT NULL,
    d06_manual_net_income bigint NOT NULL,
    d07_annex_gross_sales bigint NOT NULL,
    d08_vatable_sales bigint NOT NULL,
    d09_vat_amount bigint NOT NULL,
    d10_vat_exempt_sales bigint NOT NULL,
    d11_zero_rated_sales bigint NOT NULL,
    d12_sc_discount bigint NOT NULL,
    d13_pwd_discount bigint NOT NULL,
    d14_naac_discount bigint NOT NULL,
    d15_solo_parent_discount bigint NOT NULL,
    d16_other_discount bigint NOT NULL,
    d17_returns bigint NOT NULL,
    d18_voids bigint NOT NULL,
    d19_total_deductions bigint NOT NULL,
    d20_sc_vat_adjustment bigint NOT NULL,
    d21_pwd_vat_adjustment bigint NOT NULL,
    d22_other_vat_adjustment bigint NOT NULL,
    d23_vat_on_returns bigint NOT NULL,
    d24_residual_vat_adjustment bigint NOT NULL,
    d25_total_vat_adjustment bigint NOT NULL,
    d26_vat_payable bigint NOT NULL,
    d27_net_sales_ex_vat bigint NOT NULL,
    d28_overflow_net_income bigint NOT NULL,
    d29_total_income bigint NOT NULL,
    d30_reset_counter bigint NOT NULL,
    d31_z_counter bigint NOT NULL,
    remarks_code_id uuid NOT NULL,
    source_semantic_hash char(64) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_annex_e_reports PRIMARY KEY (annex_e_report_id),
    CONSTRAINT fk_annex_e_reports__workbook FOREIGN KEY (annex_e1_workbook_id) REFERENCES pos.annex_e1_workbooks (annex_e1_workbook_id),
    CONSTRAINT fk_annex_e_reports__period_scope FOREIGN KEY (fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code) REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code),
    CONSTRAINT fk_annex_e_reports__z FOREIGN KEY (governing_z_report_id, governing_report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code) REFERENCES pos.x_z_reports (x_z_report_id, report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code),
    CONSTRAINT fk_annex_e_reports__summary FOREIGN KEY (related_bir_sales_summary_report_id) REFERENCES pos.bir_sales_summary_reports (bir_sales_summary_report_id),
    CONSTRAINT fk_annex_e_reports__remarks FOREIGN KEY (remarks_code_id) REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_annex_e_reports__workbook_row UNIQUE (annex_e1_workbook_id, row_sequence),
    CONSTRAINT uq_annex_e_reports__workbook_z UNIQUE (annex_e1_workbook_id, governing_z_report_id),
    CONSTRAINT ck_annex_e_reports__row CHECK (row_sequence > 0),
    CONSTRAINT ck_annex_e_reports__z_kind CHECK (governing_report_kind_code_id='1c628bc2-49c3-53e8-ae83-2082bcf28467'),
    CONSTRAINT ck_annex_e_reports__remarks CHECK (remarks_code_id IN ('0dafc990-56aa-559f-9424-5569488a8126','d74f22db-9c1f-5429-a376-ee1dd2cd5a28')),
    CONSTRAINT ck_annex_e_reports__amounts CHECK (d04_gta_ending>=0 AND d05_gta_beginning>=0 AND d06_manual_net_income>=0 AND d07_annex_gross_sales>=0 AND d08_vatable_sales>=0 AND d09_vat_amount>=0 AND d10_vat_exempt_sales>=0 AND d11_zero_rated_sales>=0 AND d12_sc_discount>=0 AND d13_pwd_discount>=0 AND d14_naac_discount>=0 AND d15_solo_parent_discount>=0 AND d16_other_discount>=0 AND d17_returns>=0 AND d18_voids>=0 AND d19_total_deductions>=0 AND d20_sc_vat_adjustment>=0 AND d21_pwd_vat_adjustment>=0 AND d22_other_vat_adjustment>=0 AND d23_vat_on_returns>=0 AND d24_residual_vat_adjustment>=0 AND d25_total_vat_adjustment>=0 AND d26_vat_payable>=0 AND d27_net_sales_ex_vat>=0 AND d28_overflow_net_income>=0 AND d29_total_income>=0 AND d30_reset_counter>=0 AND d31_z_counter>=0),
    CONSTRAINT ck_annex_e_reports__reconcile CHECK (d19_total_deductions=d12_sc_discount+d13_pwd_discount+d14_naac_discount+d15_solo_parent_discount+d16_other_discount+d17_returns+d18_voids AND d25_total_vat_adjustment=d20_sc_vat_adjustment+d21_pwd_vat_adjustment+d22_other_vat_adjustment+d23_vat_on_returns+d24_residual_vat_adjustment AND d26_vat_payable+d22_other_vat_adjustment=d09_vat_amount AND d27_net_sales_ex_vat+d19_total_deductions+d09_vat_amount=d07_annex_gross_sales AND d29_total_income-d06_manual_net_income-d28_overflow_net_income=d27_net_sales_ex_vat),
    CONSTRAINT ck_annex_e_reports__hash CHECK (source_semantic_hash ~ '^[0-9a-f]{64}$')
);

CREATE INDEX IF NOT EXISTS ix_annex_e_reports__period ON pos.annex_e_reports (fiscal_reporting_period_id, annex_e1_workbook_id);
CREATE OR REPLACE TRIGGER trg_annex_e_reports_immutable BEFORE UPDATE OR DELETE ON pos.annex_e_reports FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();
