-- POS Server immutable X Reading and Z Reading fiscal aggregate snapshots.
-- Values are supplied by future governed runtime; this table performs no aggregation or close.

CREATE TABLE IF NOT EXISTS pos.x_z_reports (
    x_z_report_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL,
    report_kind_code_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    report_number text NOT NULL,
    business_day_date date NOT NULL,
    period_start_at timestamptz NOT NULL,
    period_end_at timestamptz NOT NULL,
    reporting_timezone_name text NOT NULL,
    business_day_cutoff_local_time time without time zone NOT NULL,
    transaction_count bigint NOT NULL,
    begin_fiscal_document_id uuid NULL,
    end_fiscal_document_id uuid NULL,
    beginning_si_ref text NULL,
    ending_si_ref text NULL,
    first_fiscal_sequence_value bigint NULL,
    last_fiscal_sequence_value bigint NULL,
    fiscal_sequence_gap_count bigint NOT NULL,
    reset_counter_value bigint NULL,
    z_counter_value bigint NULL,
    previous_grand_total_amount_minor_units bigint NOT NULL,
    current_grand_total_amount_minor_units bigint NOT NULL,
    present_grand_total_amount_minor_units bigint NOT NULL,
    gross_sales_amount_minor_units bigint NOT NULL,
    net_sales_amount_minor_units bigint NOT NULL,
    vatable_sales_amount_minor_units bigint NOT NULL,
    vat_amount_minor_units bigint NOT NULL,
    vat_exempt_sales_amount_minor_units bigint NOT NULL,
    zero_rated_sales_amount_minor_units bigint NOT NULL,
    discount_amount_minor_units bigint NOT NULL,
    senior_citizen_discount_amount_minor_units bigint NOT NULL,
    pwd_discount_amount_minor_units bigint NOT NULL,
    other_statutory_discount_amount_minor_units bigint NOT NULL,
    vat_exemption_amount_minor_units bigint NOT NULL,
    coupon_discount_amount_minor_units bigint NOT NULL,
    promotional_discount_amount_minor_units bigint NOT NULL,
    void_amount_minor_units bigint NOT NULL,
    refund_amount_minor_units bigint NOT NULL,
    return_amount_minor_units bigint NOT NULL,
    adjustment_amount_minor_units bigint NOT NULL,
    service_charge_amount_minor_units bigint NOT NULL,
    currency_code char(3) NOT NULL,
    generated_at timestamptz NOT NULL,
    committed_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_x_z_reports PRIMARY KEY (x_z_report_id),
    CONSTRAINT fk_x_z_reports__request_kind FOREIGN KEY (fiscal_report_request_id, report_kind_code_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id, report_type_code_id),
    CONSTRAINT fk_x_z_reports__period_identity FOREIGN KEY (
        fiscal_reporting_period_id,
        fiscal_reporting_contract_version_id,
        site_pos_server_id,
        fiscal_identity_id,
        business_day_date,
        period_start_at,
        period_end_at,
        currency_code
    ) REFERENCES pos.fiscal_reporting_periods (
        fiscal_reporting_period_id,
        fiscal_reporting_contract_version_id,
        site_pos_server_id,
        fiscal_identity_id,
        business_day_date,
        period_start_at,
        period_end_at,
        currency_code
    ),
    CONSTRAINT fk_x_z_reports__begin_doc FOREIGN KEY (begin_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_x_z_reports__end_doc FOREIGN KEY (end_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT uq_x_z_reports__request UNIQUE (fiscal_report_request_id),
    CONSTRAINT uq_x_z_reports__identity_kind UNIQUE (x_z_report_id, report_kind_code_id),
    CONSTRAINT uq_x_z_reports__identity_currency UNIQUE (x_z_report_id, currency_code),
    CONSTRAINT uq_x_z_reports__identity_scope_currency UNIQUE (x_z_report_id, fiscal_identity_id, currency_code),
    CONSTRAINT uq_x_z_reports__governing_scope UNIQUE (
        x_z_report_id,
        report_kind_code_id,
        fiscal_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ),
    CONSTRAINT uq_x_z_reports__governing_scope_no_currency UNIQUE (
        x_z_report_id,
        report_kind_code_id,
        fiscal_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id
    ),
    CONSTRAINT uq_x_z_reports__fiscal_report_number UNIQUE (fiscal_identity_id, report_number),
    CONSTRAINT ck_x_z_reports__kind_family CHECK (report_kind_code_id IN (
        '5dc3cc94-b3ab-5582-a598-e779871fc3e2',
        '1c628bc2-49c3-53e8-ae83-2082bcf28467'
    )),
    CONSTRAINT ck_x_z_reports__report_number CHECK (char_length(btrim(report_number)) > 0),
    CONSTRAINT ck_x_z_reports__period_range CHECK (period_end_at > period_start_at),
    CONSTRAINT ck_x_z_reports__timezone CHECK (char_length(btrim(reporting_timezone_name)) > 0),
    CONSTRAINT ck_x_z_reports__counts CHECK (
        transaction_count >= 0 AND fiscal_sequence_gap_count >= 0
    ),
    CONSTRAINT ck_x_z_reports__document_range CHECK (
        (begin_fiscal_document_id IS NULL AND end_fiscal_document_id IS NULL)
        OR (begin_fiscal_document_id IS NOT NULL AND end_fiscal_document_id IS NOT NULL)
    ),
    CONSTRAINT ck_x_z_reports__sequence_range CHECK (
        (first_fiscal_sequence_value IS NULL AND last_fiscal_sequence_value IS NULL)
        OR (
            first_fiscal_sequence_value > 0
            AND last_fiscal_sequence_value >= first_fiscal_sequence_value
        )
    ),
    CONSTRAINT ck_x_z_reports__si_refs CHECK (
        (beginning_si_ref IS NULL AND ending_si_ref IS NULL)
        OR (
            char_length(btrim(beginning_si_ref)) > 0
            AND char_length(btrim(ending_si_ref)) > 0
        )
    ),
    CONSTRAINT ck_x_z_reports__counters CHECK (
        (reset_counter_value IS NULL OR reset_counter_value >= 0)
        AND (z_counter_value IS NULL OR z_counter_value >= 0)
    ),
    CONSTRAINT ck_x_z_reports__amounts_nonnegative CHECK (
        previous_grand_total_amount_minor_units >= 0
        AND current_grand_total_amount_minor_units >= 0
        AND present_grand_total_amount_minor_units >= 0
        AND gross_sales_amount_minor_units >= 0
        AND net_sales_amount_minor_units >= 0
        AND vatable_sales_amount_minor_units >= 0
        AND vat_amount_minor_units >= 0
        AND vat_exempt_sales_amount_minor_units >= 0
        AND zero_rated_sales_amount_minor_units >= 0
        AND discount_amount_minor_units >= 0
        AND senior_citizen_discount_amount_minor_units >= 0
        AND pwd_discount_amount_minor_units >= 0
        AND other_statutory_discount_amount_minor_units >= 0
        AND vat_exemption_amount_minor_units >= 0
        AND coupon_discount_amount_minor_units >= 0
        AND promotional_discount_amount_minor_units >= 0
        AND void_amount_minor_units >= 0
        AND refund_amount_minor_units >= 0
        AND return_amount_minor_units >= 0
        AND adjustment_amount_minor_units >= 0
        AND service_charge_amount_minor_units >= 0
    ),
    CONSTRAINT ck_x_z_reports__discount_breakdown CHECK (
        senior_citizen_discount_amount_minor_units
        + pwd_discount_amount_minor_units
        + other_statutory_discount_amount_minor_units
        + coupon_discount_amount_minor_units
        + promotional_discount_amount_minor_units
        <= discount_amount_minor_units
    ),
    CONSTRAINT ck_x_z_reports__gta_relationship CHECK (
        present_grand_total_amount_minor_units
        = previous_grand_total_amount_minor_units + current_grand_total_amount_minor_units
    ),
    CONSTRAINT ck_x_z_reports__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_x_z_reports__timestamps CHECK (
        (
            (
                report_kind_code_id = '5dc3cc94-b3ab-5582-a598-e779871fc3e2'
                AND generated_at >= period_start_at
            )
            OR (
                report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
                AND generated_at >= period_end_at
            )
        )
        AND committed_at >= generated_at
        AND updated_at = created_at
    )
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'pos' AND table_name = 'x_z_reports' AND column_name = 'report_snapshot_context'
    ) AND EXISTS (SELECT 1 FROM pos.x_z_reports) THEN
        RAISE EXCEPTION 'cannot harden non-empty legacy pos.x_z_reports; migrate exact governed snapshots before upgrade';
    END IF;
END;
$$;

ALTER TABLE pos.x_z_reports
    ADD COLUMN IF NOT EXISTS fiscal_reporting_contract_version_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_identity_id uuid NULL,
    ADD COLUMN IF NOT EXISTS report_number text NULL,
    ADD COLUMN IF NOT EXISTS reporting_timezone_name text NULL,
    ADD COLUMN IF NOT EXISTS business_day_cutoff_local_time time without time zone NULL,
    ADD COLUMN IF NOT EXISTS transaction_count bigint NULL,
    ADD COLUMN IF NOT EXISTS first_fiscal_sequence_value bigint NULL,
    ADD COLUMN IF NOT EXISTS last_fiscal_sequence_value bigint NULL,
    ADD COLUMN IF NOT EXISTS fiscal_sequence_gap_count bigint NULL,
    ADD COLUMN IF NOT EXISTS current_grand_total_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS vatable_sales_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS vat_exempt_sales_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS zero_rated_sales_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS senior_citizen_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS pwd_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS other_statutory_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS vat_exemption_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS coupon_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS promotional_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS refund_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS adjustment_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS service_charge_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS committed_at timestamptz NULL;

ALTER TABLE pos.x_z_reports DROP COLUMN IF EXISTS report_snapshot_context;

ALTER TABLE pos.x_z_reports
    ALTER COLUMN fiscal_reporting_contract_version_id SET NOT NULL,
    ALTER COLUMN fiscal_reporting_period_id SET NOT NULL,
    ALTER COLUMN fiscal_identity_id SET NOT NULL,
    ALTER COLUMN report_number SET NOT NULL,
    ALTER COLUMN business_day_date SET NOT NULL,
    ALTER COLUMN period_start_at SET NOT NULL,
    ALTER COLUMN period_end_at SET NOT NULL,
    ALTER COLUMN reporting_timezone_name SET NOT NULL,
    ALTER COLUMN business_day_cutoff_local_time SET NOT NULL,
    ALTER COLUMN transaction_count SET NOT NULL,
    ALTER COLUMN fiscal_sequence_gap_count SET NOT NULL,
    ALTER COLUMN previous_grand_total_amount_minor_units SET NOT NULL,
    ALTER COLUMN current_grand_total_amount_minor_units SET NOT NULL,
    ALTER COLUMN present_grand_total_amount_minor_units SET NOT NULL,
    ALTER COLUMN gross_sales_amount_minor_units SET NOT NULL,
    ALTER COLUMN net_sales_amount_minor_units SET NOT NULL,
    ALTER COLUMN vatable_sales_amount_minor_units SET NOT NULL,
    ALTER COLUMN vat_amount_minor_units SET NOT NULL,
    ALTER COLUMN vat_exempt_sales_amount_minor_units SET NOT NULL,
    ALTER COLUMN zero_rated_sales_amount_minor_units SET NOT NULL,
    ALTER COLUMN discount_amount_minor_units SET NOT NULL,
    ALTER COLUMN senior_citizen_discount_amount_minor_units SET NOT NULL,
    ALTER COLUMN pwd_discount_amount_minor_units SET NOT NULL,
    ALTER COLUMN other_statutory_discount_amount_minor_units SET NOT NULL,
    ALTER COLUMN vat_exemption_amount_minor_units SET NOT NULL,
    ALTER COLUMN coupon_discount_amount_minor_units SET NOT NULL,
    ALTER COLUMN promotional_discount_amount_minor_units SET NOT NULL,
    ALTER COLUMN void_amount_minor_units SET NOT NULL,
    ALTER COLUMN refund_amount_minor_units SET NOT NULL,
    ALTER COLUMN return_amount_minor_units SET NOT NULL,
    ALTER COLUMN adjustment_amount_minor_units SET NOT NULL,
    ALTER COLUMN service_charge_amount_minor_units SET NOT NULL,
    ALTER COLUMN currency_code SET NOT NULL,
    ALTER COLUMN generated_at SET NOT NULL,
    ALTER COLUMN committed_at SET NOT NULL;

ALTER TABLE pos.x_z_reports
    DROP CONSTRAINT IF EXISTS fk_x_z_reports__request,
    DROP CONSTRAINT IF EXISTS fk_x_z_reports__report_kind,
    DROP CONSTRAINT IF EXISTS fk_x_z_reports__site_pos_server,
    DROP CONSTRAINT IF EXISTS fk_x_z_reports__request_kind,
    DROP CONSTRAINT IF EXISTS fk_x_z_reports__period_identity,
    DROP CONSTRAINT IF EXISTS uq_x_z_reports__request,
    DROP CONSTRAINT IF EXISTS uq_x_z_reports__fiscal_report_number,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__period_range,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__timezone,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__kind_family,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__report_number,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__counts,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__document_range,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__sequence_range,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__si_refs,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__counters,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__amounts_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__discount_breakdown,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__gta_relationship,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__currency,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__timestamps,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__beginning_si_ref,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__ending_si_ref,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__reset_counter_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__z_counter_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__previous_gta_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__present_gta_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__gross_sales_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__net_sales_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__vat_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__discount_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__void_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__return_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__currency_code;

ALTER TABLE pos.x_z_reports
    ADD CONSTRAINT fk_x_z_reports__request_kind FOREIGN KEY (fiscal_report_request_id, report_kind_code_id) REFERENCES pos.fiscal_report_requests (fiscal_report_request_id, report_type_code_id),
    ADD CONSTRAINT fk_x_z_reports__period_identity FOREIGN KEY (fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,business_day_date,period_start_at,period_end_at,currency_code) REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,business_day_date,period_start_at,period_end_at,currency_code),
    ADD CONSTRAINT uq_x_z_reports__request UNIQUE (fiscal_report_request_id),
    ADD CONSTRAINT uq_x_z_reports__fiscal_report_number UNIQUE (fiscal_identity_id, report_number),
    ADD CONSTRAINT ck_x_z_reports__kind_family CHECK (report_kind_code_id IN ('5dc3cc94-b3ab-5582-a598-e779871fc3e2','1c628bc2-49c3-53e8-ae83-2082bcf28467')),
    ADD CONSTRAINT ck_x_z_reports__report_number CHECK (char_length(btrim(report_number)) > 0),
    ADD CONSTRAINT ck_x_z_reports__period_range CHECK (period_end_at > period_start_at),
    ADD CONSTRAINT ck_x_z_reports__timezone CHECK (char_length(btrim(reporting_timezone_name)) > 0),
    ADD CONSTRAINT ck_x_z_reports__counts CHECK (transaction_count >= 0 AND fiscal_sequence_gap_count >= 0),
    ADD CONSTRAINT ck_x_z_reports__document_range CHECK ((begin_fiscal_document_id IS NULL AND end_fiscal_document_id IS NULL) OR (begin_fiscal_document_id IS NOT NULL AND end_fiscal_document_id IS NOT NULL)),
    ADD CONSTRAINT ck_x_z_reports__sequence_range CHECK ((first_fiscal_sequence_value IS NULL AND last_fiscal_sequence_value IS NULL) OR (first_fiscal_sequence_value > 0 AND last_fiscal_sequence_value >= first_fiscal_sequence_value)),
    ADD CONSTRAINT ck_x_z_reports__si_refs CHECK ((beginning_si_ref IS NULL AND ending_si_ref IS NULL) OR (char_length(btrim(beginning_si_ref)) > 0 AND char_length(btrim(ending_si_ref)) > 0)),
    ADD CONSTRAINT ck_x_z_reports__counters CHECK ((reset_counter_value IS NULL OR reset_counter_value >= 0) AND (z_counter_value IS NULL OR z_counter_value >= 0)),
    ADD CONSTRAINT ck_x_z_reports__amounts_nonnegative CHECK (previous_grand_total_amount_minor_units >= 0 AND current_grand_total_amount_minor_units >= 0 AND present_grand_total_amount_minor_units >= 0 AND gross_sales_amount_minor_units >= 0 AND net_sales_amount_minor_units >= 0 AND vatable_sales_amount_minor_units >= 0 AND vat_amount_minor_units >= 0 AND vat_exempt_sales_amount_minor_units >= 0 AND zero_rated_sales_amount_minor_units >= 0 AND discount_amount_minor_units >= 0 AND senior_citizen_discount_amount_minor_units >= 0 AND pwd_discount_amount_minor_units >= 0 AND other_statutory_discount_amount_minor_units >= 0 AND vat_exemption_amount_minor_units >= 0 AND coupon_discount_amount_minor_units >= 0 AND promotional_discount_amount_minor_units >= 0 AND void_amount_minor_units >= 0 AND refund_amount_minor_units >= 0 AND return_amount_minor_units >= 0 AND adjustment_amount_minor_units >= 0 AND service_charge_amount_minor_units >= 0),
    ADD CONSTRAINT ck_x_z_reports__discount_breakdown CHECK (senior_citizen_discount_amount_minor_units + pwd_discount_amount_minor_units + other_statutory_discount_amount_minor_units + coupon_discount_amount_minor_units + promotional_discount_amount_minor_units <= discount_amount_minor_units),
    ADD CONSTRAINT ck_x_z_reports__gta_relationship CHECK (present_grand_total_amount_minor_units = previous_grand_total_amount_minor_units + current_grand_total_amount_minor_units),
    ADD CONSTRAINT ck_x_z_reports__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    ADD CONSTRAINT ck_x_z_reports__timestamps CHECK ((((report_kind_code_id = '5dc3cc94-b3ab-5582-a598-e779871fc3e2') AND generated_at >= period_start_at) OR ((report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467') AND generated_at >= period_end_at)) AND committed_at >= generated_at AND updated_at = created_at);

DO $$
DECLARE
    definition text;
BEGIN
    FOREACH definition IN ARRAY ARRAY[
        'ALTER TABLE pos.x_z_reports ADD CONSTRAINT uq_x_z_reports__identity_kind UNIQUE (x_z_report_id, report_kind_code_id)',
        'ALTER TABLE pos.x_z_reports ADD CONSTRAINT uq_x_z_reports__identity_currency UNIQUE (x_z_report_id, currency_code)',
        'ALTER TABLE pos.x_z_reports ADD CONSTRAINT uq_x_z_reports__identity_scope_currency UNIQUE (x_z_report_id, fiscal_identity_id, currency_code)',
        'ALTER TABLE pos.x_z_reports ADD CONSTRAINT uq_x_z_reports__governing_scope UNIQUE (x_z_report_id, report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code)',
        'ALTER TABLE pos.x_z_reports ADD CONSTRAINT uq_x_z_reports__governing_scope_no_currency UNIQUE (x_z_report_id, report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id)'
    ] LOOP
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conrelid = 'pos.x_z_reports'::regclass
              AND conname = substring(definition from 'CONSTRAINT ([^ ]+)')
        ) THEN
            EXECUTE definition;
        END IF;
    END LOOP;
END;
$$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_x_z_reports__z_period
    ON pos.x_z_reports (fiscal_reporting_period_id)
    WHERE report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467';

CREATE INDEX IF NOT EXISTS ix_x_z_reports__site_business_date
    ON pos.x_z_reports (site_pos_server_id, business_day_date DESC, committed_at DESC);

CREATE INDEX IF NOT EXISTS ix_x_z_reports__period_kind
    ON pos.x_z_reports (fiscal_reporting_period_id, report_kind_code_id);

CREATE OR REPLACE TRIGGER trg_x_z_reports_immutable
BEFORE UPDATE OR DELETE ON pos.x_z_reports
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.x_z_reports IS 'Immutable first-class X Reading and Z Reading aggregate snapshots. The table neither aggregates fiscal documents nor closes periods.';
COMMENT ON COLUMN pos.x_z_reports.report_number IS 'Stable internal report reference unique within fiscal identity. External BIR numbering remains a compliance dependency.';
COMMENT ON COLUMN pos.x_z_reports.generated_at IS 'Immutable observation time. X Reading requires period start or later; Z Reading requires period end or later.';
COMMENT ON COLUMN pos.x_z_reports.current_grand_total_amount_minor_units IS 'Current period contribution to GTA; future Z runtime must persist the supplied aggregate without policy recalculation.';
COMMENT ON COLUMN pos.x_z_reports.present_grand_total_amount_minor_units IS 'Resulting GTA, constrained as previous plus current. Preserved column name for additive compatibility.';
COMMENT ON COLUMN pos.x_z_reports.refund_amount_minor_units IS 'Reserved first-class non-negative category total. Future runtime fails closed until refund period/sign rules are governed.';
COMMENT ON COLUMN pos.x_z_reports.service_charge_amount_minor_units IS 'Reserved first-class category total. Future runtime fails closed until a governed source classification exists.';
