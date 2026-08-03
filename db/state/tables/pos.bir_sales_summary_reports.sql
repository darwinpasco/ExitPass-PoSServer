-- Immutable BIR Sales Summary snapshot bound to one governing Z Reading.
-- Final external BIR formulas, layouts, and files remain compliance dependencies.

CREATE TABLE IF NOT EXISTS pos.bir_sales_summary_reports (
    bir_sales_summary_report_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    report_kind_code_id uuid NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL,
    governing_z_report_id uuid NOT NULL,
    governing_report_kind_code_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    reporting_contract_profile_ref text NOT NULL,
    sales_invoice_header_profile_id uuid NOT NULL,
    header_profile_version text NOT NULL,
    pos_serial_number text NOT NULL,
    machine_identification_number text NOT NULL,
    bir_accreditation_number text NOT NULL,
    bir_accreditation_issued_date date NOT NULL,
    bir_accreditation_valid_until date NOT NULL,
    ptu_number text NOT NULL,
    ptu_issued_date date NOT NULL,
    business_day_date date NOT NULL,
    reporting_period_start_date date NOT NULL,
    reporting_period_end_date date NOT NULL,
    transaction_count bigint NOT NULL,
    beginning_si_ref text NULL,
    ending_si_ref text NULL,
    previous_grand_total_amount_minor_units bigint NOT NULL,
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
    void_amount_minor_units bigint NOT NULL,
    refund_amount_minor_units bigint NOT NULL,
    return_amount_minor_units bigint NOT NULL,
    adjustment_amount_minor_units bigint NOT NULL,
    reset_counter_value bigint NOT NULL,
    z_counter_value bigint NOT NULL,
    currency_code char(3) NOT NULL,
    generated_at timestamptz NOT NULL,
    committed_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_bir_sales_summary_reports PRIMARY KEY (bir_sales_summary_report_id),
    CONSTRAINT fk_bir_sales_summary__request_kind FOREIGN KEY (fiscal_report_request_id, report_kind_code_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id, report_type_code_id),
    CONSTRAINT fk_bir_sales_summary__governing_z FOREIGN KEY (
        governing_z_report_id,
        governing_report_kind_code_id,
        fiscal_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ) REFERENCES pos.x_z_reports (
        x_z_report_id,
        report_kind_code_id,
        fiscal_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ),
    CONSTRAINT fk_bir_sales_summary__contract FOREIGN KEY (fiscal_reporting_contract_version_id)
        REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    CONSTRAINT fk_bir_sales_summary__header_profile FOREIGN KEY (sales_invoice_header_profile_id)
        REFERENCES pos.sales_invoice_header_profiles (sales_invoice_header_profile_id),
    CONSTRAINT uq_bir_sales_summary__request UNIQUE (fiscal_report_request_id),
    CONSTRAINT uq_bir_sales_summary__z_contract_profile UNIQUE (governing_z_report_id, reporting_contract_profile_ref),
    CONSTRAINT uq_bir_sales_summary__id_scope UNIQUE (bir_sales_summary_report_id, governing_z_report_id, site_pos_server_id, fiscal_identity_id),
    CONSTRAINT ck_bir_sales_summary__request_kind CHECK (report_kind_code_id = '2326447f-74c2-5ed7-83bb-e079fcee7f3d'),
    CONSTRAINT ck_bir_sales_summary__z_kind CHECK (governing_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'),
    CONSTRAINT ck_bir_sales_summary__profile_ref CHECK (char_length(btrim(reporting_contract_profile_ref)) > 0),
    CONSTRAINT ck_bir_sales_summary__header_values CHECK (
        char_length(btrim(header_profile_version)) > 0
        AND char_length(btrim(pos_serial_number)) > 0
        AND char_length(btrim(machine_identification_number)) > 0
        AND char_length(btrim(bir_accreditation_number)) > 0
        AND bir_accreditation_valid_until >= bir_accreditation_issued_date
        AND char_length(btrim(ptu_number)) > 0
    ),
    CONSTRAINT ck_bir_sales_summary__period_range CHECK (reporting_period_end_date >= reporting_period_start_date),
    CONSTRAINT ck_bir_sales_summary__si_refs CHECK (
        (beginning_si_ref IS NULL AND ending_si_ref IS NULL)
        OR (char_length(btrim(beginning_si_ref)) > 0 AND char_length(btrim(ending_si_ref)) > 0)
    ),
    CONSTRAINT ck_bir_sales_summary__values CHECK (
        transaction_count >= 0
        AND previous_grand_total_amount_minor_units >= 0
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
        AND void_amount_minor_units >= 0
        AND refund_amount_minor_units >= 0
        AND return_amount_minor_units >= 0
        AND adjustment_amount_minor_units >= 0
        AND reset_counter_value >= 0
        AND z_counter_value >= 0
    ),
    CONSTRAINT ck_bir_sales_summary__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_bir_sales_summary__timestamps CHECK (
        committed_at >= generated_at AND updated_at = created_at
    )
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'pos' AND table_name = 'bir_sales_summary_reports' AND column_name = 'summary_context'
    ) AND EXISTS (SELECT 1 FROM pos.bir_sales_summary_reports) THEN
        RAISE EXCEPTION 'cannot harden non-empty legacy pos.bir_sales_summary_reports; migrate exact governed summaries first';
    END IF;
END;
$$;

ALTER TABLE pos.bir_sales_summary_reports
    ADD COLUMN IF NOT EXISTS report_kind_code_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_contract_version_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS governing_z_report_id uuid NULL,
    ADD COLUMN IF NOT EXISTS governing_report_kind_code_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_identity_id uuid NULL,
    ADD COLUMN IF NOT EXISTS reporting_contract_profile_ref text NULL,
    ADD COLUMN IF NOT EXISTS sales_invoice_header_profile_id uuid NULL,
    ADD COLUMN IF NOT EXISTS header_profile_version text NULL,
    ADD COLUMN IF NOT EXISTS pos_serial_number text NULL,
    ADD COLUMN IF NOT EXISTS machine_identification_number text NULL,
    ADD COLUMN IF NOT EXISTS bir_accreditation_number text NULL,
    ADD COLUMN IF NOT EXISTS bir_accreditation_issued_date date NULL,
    ADD COLUMN IF NOT EXISTS bir_accreditation_valid_until date NULL,
    ADD COLUMN IF NOT EXISTS ptu_number text NULL,
    ADD COLUMN IF NOT EXISTS ptu_issued_date date NULL,
    ADD COLUMN IF NOT EXISTS transaction_count bigint NULL,
    ADD COLUMN IF NOT EXISTS vatable_sales_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS senior_citizen_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS pwd_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS other_statutory_discount_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS vat_exemption_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS refund_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS adjustment_amount_minor_units bigint NULL,
    ADD COLUMN IF NOT EXISTS committed_at timestamptz NULL;

ALTER TABLE pos.bir_sales_summary_reports DROP COLUMN IF EXISTS summary_context;

ALTER TABLE pos.bir_sales_summary_reports
    ALTER COLUMN report_kind_code_id SET NOT NULL,
    ALTER COLUMN fiscal_reporting_contract_version_id SET NOT NULL,
    ALTER COLUMN fiscal_reporting_period_id SET NOT NULL,
    ALTER COLUMN governing_z_report_id SET NOT NULL,
    ALTER COLUMN governing_report_kind_code_id SET NOT NULL,
    ALTER COLUMN fiscal_identity_id SET NOT NULL,
    ALTER COLUMN reporting_contract_profile_ref SET NOT NULL,
    ALTER COLUMN sales_invoice_header_profile_id SET NOT NULL,
    ALTER COLUMN header_profile_version SET NOT NULL,
    ALTER COLUMN pos_serial_number SET NOT NULL,
    ALTER COLUMN machine_identification_number SET NOT NULL,
    ALTER COLUMN bir_accreditation_number SET NOT NULL,
    ALTER COLUMN bir_accreditation_issued_date SET NOT NULL,
    ALTER COLUMN bir_accreditation_valid_until SET NOT NULL,
    ALTER COLUMN ptu_number SET NOT NULL,
    ALTER COLUMN ptu_issued_date SET NOT NULL,
    ALTER COLUMN business_day_date SET NOT NULL,
    ALTER COLUMN reporting_period_start_date SET NOT NULL,
    ALTER COLUMN reporting_period_end_date SET NOT NULL,
    ALTER COLUMN transaction_count SET NOT NULL,
    ALTER COLUMN previous_grand_total_amount_minor_units SET NOT NULL,
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
    ALTER COLUMN void_amount_minor_units SET NOT NULL,
    ALTER COLUMN refund_amount_minor_units SET NOT NULL,
    ALTER COLUMN return_amount_minor_units SET NOT NULL,
    ALTER COLUMN adjustment_amount_minor_units SET NOT NULL,
    ALTER COLUMN reset_counter_value SET NOT NULL,
    ALTER COLUMN z_counter_value SET NOT NULL,
    ALTER COLUMN currency_code SET NOT NULL,
    ALTER COLUMN generated_at SET NOT NULL,
    ALTER COLUMN committed_at SET NOT NULL;

ALTER TABLE pos.bir_sales_summary_reports
    DROP CONSTRAINT IF EXISTS fk_bir_sales_summary__request,
    DROP CONSTRAINT IF EXISTS fk_bir_sales_summary__site_pos_server,
    DROP CONSTRAINT IF EXISTS fk_bir_sales_summary__request_kind,
    DROP CONSTRAINT IF EXISTS fk_bir_sales_summary__governing_z,
    DROP CONSTRAINT IF EXISTS fk_bir_sales_summary__contract,
    DROP CONSTRAINT IF EXISTS fk_bir_sales_summary__header_profile,
    DROP CONSTRAINT IF EXISTS uq_bir_sales_summary__request,
    DROP CONSTRAINT IF EXISTS uq_bir_sales_summary__z_contract_profile,
    DROP CONSTRAINT IF EXISTS uq_bir_sales_summary__id_scope,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__request_kind,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__z_kind,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__profile_ref,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__header_values,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__period_range,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__si_refs,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__values,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__currency,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__timestamps,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__beginning_si_ref,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__ending_si_ref,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__prev_gta_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__present_gta_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__gross_sales_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__net_sales_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__vat_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__vat_exempt_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__zero_rated_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__discount_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__void_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__return_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__reset_counter_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__z_counter_nonnegative,
    DROP CONSTRAINT IF EXISTS ck_bir_sales_summary__currency_code;

ALTER TABLE pos.bir_sales_summary_reports
    ADD CONSTRAINT fk_bir_sales_summary__request_kind FOREIGN KEY (fiscal_report_request_id, report_kind_code_id) REFERENCES pos.fiscal_report_requests (fiscal_report_request_id, report_type_code_id),
    ADD CONSTRAINT fk_bir_sales_summary__governing_z FOREIGN KEY (governing_z_report_id, governing_report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code) REFERENCES pos.x_z_reports (x_z_report_id, report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code),
    ADD CONSTRAINT fk_bir_sales_summary__contract FOREIGN KEY (fiscal_reporting_contract_version_id) REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    ADD CONSTRAINT fk_bir_sales_summary__header_profile FOREIGN KEY (sales_invoice_header_profile_id) REFERENCES pos.sales_invoice_header_profiles (sales_invoice_header_profile_id),
    ADD CONSTRAINT uq_bir_sales_summary__request UNIQUE (fiscal_report_request_id),
    ADD CONSTRAINT uq_bir_sales_summary__z_contract_profile UNIQUE (governing_z_report_id, reporting_contract_profile_ref),
    ADD CONSTRAINT uq_bir_sales_summary__id_scope UNIQUE (bir_sales_summary_report_id, governing_z_report_id, site_pos_server_id, fiscal_identity_id),
    ADD CONSTRAINT ck_bir_sales_summary__request_kind CHECK (report_kind_code_id = '2326447f-74c2-5ed7-83bb-e079fcee7f3d'),
    ADD CONSTRAINT ck_bir_sales_summary__z_kind CHECK (governing_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'),
    ADD CONSTRAINT ck_bir_sales_summary__profile_ref CHECK (char_length(btrim(reporting_contract_profile_ref)) > 0),
    ADD CONSTRAINT ck_bir_sales_summary__header_values CHECK (char_length(btrim(header_profile_version)) > 0 AND char_length(btrim(pos_serial_number)) > 0 AND char_length(btrim(machine_identification_number)) > 0 AND char_length(btrim(bir_accreditation_number)) > 0 AND bir_accreditation_valid_until >= bir_accreditation_issued_date AND char_length(btrim(ptu_number)) > 0),
    ADD CONSTRAINT ck_bir_sales_summary__period_range CHECK (reporting_period_end_date >= reporting_period_start_date),
    ADD CONSTRAINT ck_bir_sales_summary__si_refs CHECK ((beginning_si_ref IS NULL AND ending_si_ref IS NULL) OR (char_length(btrim(beginning_si_ref)) > 0 AND char_length(btrim(ending_si_ref)) > 0)),
    ADD CONSTRAINT ck_bir_sales_summary__values CHECK (transaction_count >= 0 AND previous_grand_total_amount_minor_units >= 0 AND present_grand_total_amount_minor_units >= 0 AND gross_sales_amount_minor_units >= 0 AND net_sales_amount_minor_units >= 0 AND vatable_sales_amount_minor_units >= 0 AND vat_amount_minor_units >= 0 AND vat_exempt_sales_amount_minor_units >= 0 AND zero_rated_sales_amount_minor_units >= 0 AND discount_amount_minor_units >= 0 AND senior_citizen_discount_amount_minor_units >= 0 AND pwd_discount_amount_minor_units >= 0 AND other_statutory_discount_amount_minor_units >= 0 AND vat_exemption_amount_minor_units >= 0 AND void_amount_minor_units >= 0 AND refund_amount_minor_units >= 0 AND return_amount_minor_units >= 0 AND adjustment_amount_minor_units >= 0 AND reset_counter_value >= 0 AND z_counter_value >= 0),
    ADD CONSTRAINT ck_bir_sales_summary__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    ADD CONSTRAINT ck_bir_sales_summary__timestamps CHECK (committed_at >= generated_at AND updated_at = created_at);

CREATE INDEX IF NOT EXISTS ix_bir_sales_summary__site_business_date
    ON pos.bir_sales_summary_reports (site_pos_server_id, business_day_date DESC);

CREATE INDEX IF NOT EXISTS ix_bir_sales_summary__fiscal_identity_period
    ON pos.bir_sales_summary_reports (fiscal_identity_id, reporting_period_start_date, reporting_period_end_date);

CREATE OR REPLACE TRIGGER trg_bir_sales_summary_reports_immutable
BEFORE UPDATE OR DELETE ON pos.bir_sales_summary_reports
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.bir_sales_summary_reports IS 'Immutable BIR Sales Summary snapshot bound to one governing Z Reading. Final external formula and output remain compliance dependencies.';
COMMENT ON COLUMN pos.bir_sales_summary_reports.reporting_contract_profile_ref IS 'Approved external summary profile/version reference. No layout or payload is stored here.';
