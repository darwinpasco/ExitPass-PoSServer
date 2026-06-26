-- POS Server Slice 6: BIR Sales Summary report posture.
-- This table stores BIR Sales Summary / Annex E-1 snapshot metadata only and
-- does not encode final BIR formulas, layouts, or export package behavior.

CREATE TABLE IF NOT EXISTS pos.bir_sales_summary_reports (
    bir_sales_summary_report_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    business_day_date date NULL,
    reporting_period_start_date date NULL,
    reporting_period_end_date date NULL,
    beginning_si_ref text NULL,
    ending_si_ref text NULL,
    previous_grand_total_amount_minor_units bigint NULL,
    present_grand_total_amount_minor_units bigint NULL,
    gross_sales_amount_minor_units bigint NULL,
    net_sales_amount_minor_units bigint NULL,
    vat_amount_minor_units bigint NULL,
    vat_exempt_sales_amount_minor_units bigint NULL,
    zero_rated_sales_amount_minor_units bigint NULL,
    discount_amount_minor_units bigint NULL,
    void_amount_minor_units bigint NULL,
    return_amount_minor_units bigint NULL,
    reset_counter_value bigint NULL,
    z_counter_value bigint NULL,
    currency_code char(3) NULL,
    summary_context jsonb NULL,
    generated_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_bir_sales_summary_reports PRIMARY KEY (bir_sales_summary_report_id),
    CONSTRAINT fk_bir_sales_summary__request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_bir_sales_summary__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT ck_bir_sales_summary__period_range
        CHECK (
            reporting_period_start_date IS NULL
            OR reporting_period_end_date IS NULL
            OR reporting_period_end_date >= reporting_period_start_date
        ),
    CONSTRAINT ck_bir_sales_summary__beginning_si_ref
        CHECK (beginning_si_ref IS NULL OR btrim(beginning_si_ref) <> ''),
    CONSTRAINT ck_bir_sales_summary__ending_si_ref
        CHECK (ending_si_ref IS NULL OR btrim(ending_si_ref) <> ''),
    CONSTRAINT ck_bir_sales_summary__prev_gta_nonnegative
        CHECK (previous_grand_total_amount_minor_units IS NULL OR previous_grand_total_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__present_gta_nonnegative
        CHECK (present_grand_total_amount_minor_units IS NULL OR present_grand_total_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__gross_sales_nonnegative
        CHECK (gross_sales_amount_minor_units IS NULL OR gross_sales_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__net_sales_nonnegative
        CHECK (net_sales_amount_minor_units IS NULL OR net_sales_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__vat_nonnegative
        CHECK (vat_amount_minor_units IS NULL OR vat_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__vat_exempt_nonnegative
        CHECK (vat_exempt_sales_amount_minor_units IS NULL OR vat_exempt_sales_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__zero_rated_nonnegative
        CHECK (zero_rated_sales_amount_minor_units IS NULL OR zero_rated_sales_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__discount_nonnegative
        CHECK (discount_amount_minor_units IS NULL OR discount_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__void_nonnegative
        CHECK (void_amount_minor_units IS NULL OR void_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__return_nonnegative
        CHECK (return_amount_minor_units IS NULL OR return_amount_minor_units >= 0),
    CONSTRAINT ck_bir_sales_summary__reset_counter_nonnegative
        CHECK (reset_counter_value IS NULL OR reset_counter_value >= 0),
    CONSTRAINT ck_bir_sales_summary__z_counter_nonnegative
        CHECK (z_counter_value IS NULL OR z_counter_value >= 0),
    CONSTRAINT ck_bir_sales_summary__currency_code
        CHECK (currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_bir_sales_summary__context_object
        CHECK (summary_context IS NULL OR jsonb_typeof(summary_context) = 'object')
);

COMMENT ON TABLE pos.bir_sales_summary_reports IS
    'BIR Sales Summary / Annex E-1 report snapshot posture; final formulas, layouts, and exports remain separate approvals.';
COMMENT ON COLUMN pos.bir_sales_summary_reports.bir_sales_summary_report_id IS
    'Internal identifier for the BIR Sales Summary snapshot.';
COMMENT ON COLUMN pos.bir_sales_summary_reports.fiscal_report_request_id IS
    'Report request associated with the summary snapshot.';
COMMENT ON COLUMN pos.bir_sales_summary_reports.site_pos_server_id IS
    'Site POS Server boundary for the summary snapshot.';
COMMENT ON COLUMN pos.bir_sales_summary_reports.beginning_si_ref IS
    'Reference text for beginning SI or fiscal number; not an allocator.';
COMMENT ON COLUMN pos.bir_sales_summary_reports.ending_si_ref IS
    'Reference text for ending SI or fiscal number; not an allocator.';
COMMENT ON COLUMN pos.bir_sales_summary_reports.summary_context IS
    'Optional BIR Sales Summary metadata as a JSON object.';

