-- POS Server Slice 6: X-read and Z-read report posture.
-- This table stores report snapshot values only; it does not calculate reports,
-- mutate fiscal documents, or increment Z-counters.

CREATE TABLE IF NOT EXISTS pos.x_z_reports (
    x_z_report_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    report_kind_code_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    business_day_date date NULL,
    period_start_at timestamptz NULL,
    period_end_at timestamptz NULL,
    begin_fiscal_document_id uuid NULL,
    end_fiscal_document_id uuid NULL,
    beginning_si_ref text NULL,
    ending_si_ref text NULL,
    reset_counter_value bigint NULL,
    z_counter_value bigint NULL,
    previous_grand_total_amount_minor_units bigint NULL,
    present_grand_total_amount_minor_units bigint NULL,
    gross_sales_amount_minor_units bigint NULL,
    net_sales_amount_minor_units bigint NULL,
    vat_amount_minor_units bigint NULL,
    discount_amount_minor_units bigint NULL,
    void_amount_minor_units bigint NULL,
    return_amount_minor_units bigint NULL,
    currency_code char(3) NULL,
    report_snapshot_context jsonb NULL,
    generated_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_x_z_reports PRIMARY KEY (x_z_report_id),
    CONSTRAINT fk_x_z_reports__request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_x_z_reports__report_kind
        FOREIGN KEY (report_kind_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_x_z_reports__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_x_z_reports__begin_doc
        FOREIGN KEY (begin_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_x_z_reports__end_doc
        FOREIGN KEY (end_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT ck_x_z_reports__period_range
        CHECK (period_start_at IS NULL OR period_end_at IS NULL OR period_end_at > period_start_at),
    CONSTRAINT ck_x_z_reports__beginning_si_ref
        CHECK (beginning_si_ref IS NULL OR btrim(beginning_si_ref) <> ''),
    CONSTRAINT ck_x_z_reports__ending_si_ref
        CHECK (ending_si_ref IS NULL OR btrim(ending_si_ref) <> ''),
    CONSTRAINT ck_x_z_reports__reset_counter_nonnegative
        CHECK (reset_counter_value IS NULL OR reset_counter_value >= 0),
    CONSTRAINT ck_x_z_reports__z_counter_nonnegative
        CHECK (z_counter_value IS NULL OR z_counter_value >= 0),
    CONSTRAINT ck_x_z_reports__previous_gta_nonnegative
        CHECK (previous_grand_total_amount_minor_units IS NULL OR previous_grand_total_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__present_gta_nonnegative
        CHECK (present_grand_total_amount_minor_units IS NULL OR present_grand_total_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__gross_sales_nonnegative
        CHECK (gross_sales_amount_minor_units IS NULL OR gross_sales_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__net_sales_nonnegative
        CHECK (net_sales_amount_minor_units IS NULL OR net_sales_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__vat_nonnegative
        CHECK (vat_amount_minor_units IS NULL OR vat_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__discount_nonnegative
        CHECK (discount_amount_minor_units IS NULL OR discount_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__void_nonnegative
        CHECK (void_amount_minor_units IS NULL OR void_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__return_nonnegative
        CHECK (return_amount_minor_units IS NULL OR return_amount_minor_units >= 0),
    CONSTRAINT ck_x_z_reports__currency_code
        CHECK (currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_x_z_reports__snapshot_context_object
        CHECK (report_snapshot_context IS NULL OR jsonb_typeof(report_snapshot_context) = 'object')
);

COMMENT ON TABLE pos.x_z_reports IS
    'X-read and Z-read report snapshot posture; values are stored as evidence metadata and are not calculated by this table.';
COMMENT ON COLUMN pos.x_z_reports.x_z_report_id IS
    'Internal identifier for the X-read or Z-read report snapshot.';
COMMENT ON COLUMN pos.x_z_reports.fiscal_report_request_id IS
    'Report request associated with the X/Z report snapshot.';
COMMENT ON COLUMN pos.x_z_reports.report_kind_code_id IS
    'Controlled code identifying whether the snapshot is X-read, Z-read, or another approved X/Z family.';
COMMENT ON COLUMN pos.x_z_reports.site_pos_server_id IS
    'Site POS Server boundary for the report snapshot.';
COMMENT ON COLUMN pos.x_z_reports.beginning_si_ref IS
    'Reference text for the beginning SI or fiscal number; not an allocator.';
COMMENT ON COLUMN pos.x_z_reports.ending_si_ref IS
    'Reference text for the ending SI or fiscal number; not an allocator.';
COMMENT ON COLUMN pos.x_z_reports.reset_counter_value IS
    'Report snapshot value only; this table does not mutate counters.';
COMMENT ON COLUMN pos.x_z_reports.z_counter_value IS
    'Report snapshot value only; this table does not close fiscal days.';
COMMENT ON COLUMN pos.x_z_reports.report_snapshot_context IS
    'Optional report snapshot metadata as a JSON object.';

