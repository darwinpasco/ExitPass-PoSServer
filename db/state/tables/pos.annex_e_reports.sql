-- POS Server Slice 6: Annex E report posture.
-- This table stores Annex E scope and summary references only; it does not
-- create final report layouts, output files, or export packages.

CREATE TABLE IF NOT EXISTS pos.annex_e_reports (
    annex_e_report_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    annex_e_type_code_id uuid NOT NULL,
    business_day_date date NULL,
    reporting_period_start_date date NULL,
    reporting_period_end_date date NULL,
    related_bir_sales_summary_report_id uuid NULL,
    annex_e_context jsonb NULL,
    generated_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_annex_e_reports PRIMARY KEY (annex_e_report_id),
    CONSTRAINT fk_annex_e_reports__request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_annex_e_reports__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_annex_e_reports__annex_e_type
        FOREIGN KEY (annex_e_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_annex_e_reports__sales_summary
        FOREIGN KEY (related_bir_sales_summary_report_id)
        REFERENCES pos.bir_sales_summary_reports (bir_sales_summary_report_id),
    CONSTRAINT ck_annex_e_reports__period_range
        CHECK (
            reporting_period_start_date IS NULL
            OR reporting_period_end_date IS NULL
            OR reporting_period_end_date >= reporting_period_start_date
        ),
    CONSTRAINT ck_annex_e_reports__context_object
        CHECK (annex_e_context IS NULL OR jsonb_typeof(annex_e_context) = 'object')
);

COMMENT ON TABLE pos.annex_e_reports IS
    'Annex E report posture for approved Annex E report families; final layouts and export packages are not created here.';
COMMENT ON COLUMN pos.annex_e_reports.annex_e_report_id IS
    'Internal identifier for the Annex E report snapshot.';
COMMENT ON COLUMN pos.annex_e_reports.fiscal_report_request_id IS
    'Report request associated with the Annex E snapshot.';
COMMENT ON COLUMN pos.annex_e_reports.site_pos_server_id IS
    'Site POS Server boundary for the Annex E snapshot.';
COMMENT ON COLUMN pos.annex_e_reports.annex_e_type_code_id IS
    'Controlled code identifying the Annex E type.';
COMMENT ON COLUMN pos.annex_e_reports.related_bir_sales_summary_report_id IS
    'Optional reference to a related BIR Sales Summary snapshot.';
COMMENT ON COLUMN pos.annex_e_reports.annex_e_context IS
    'Optional Annex E metadata as a JSON object.';

