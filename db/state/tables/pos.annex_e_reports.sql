-- Immutable privacy-safe Annex E metadata bound to a governing Z Reading.
-- The external form dataset and generated output are not implemented.

CREATE TABLE IF NOT EXISTS pos.annex_e_reports (
    annex_e_report_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    report_kind_code_id uuid NOT NULL,
    report_status_code_id uuid NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL,
    governing_z_report_id uuid NOT NULL,
    governing_report_kind_code_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    annex_e_contract_profile_ref text NOT NULL,
    business_day_date date NOT NULL,
    reporting_period_start_date date NOT NULL,
    reporting_period_end_date date NOT NULL,
    related_bir_sales_summary_report_id uuid NULL,
    generated_at timestamptz NOT NULL,
    committed_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_annex_e_reports PRIMARY KEY (annex_e_report_id),
    CONSTRAINT fk_annex_e_reports__request_kind FOREIGN KEY (fiscal_report_request_id, report_kind_code_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id, report_type_code_id),
    CONSTRAINT fk_annex_e_reports__status FOREIGN KEY (report_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_annex_e_reports__contract FOREIGN KEY (fiscal_reporting_contract_version_id)
        REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    CONSTRAINT fk_annex_e_reports__governing_z FOREIGN KEY (
        governing_z_report_id,
        governing_report_kind_code_id,
        fiscal_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id
    ) REFERENCES pos.x_z_reports (
        x_z_report_id,
        report_kind_code_id,
        fiscal_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id
    ),
    CONSTRAINT fk_annex_e_reports__sales_summary FOREIGN KEY (related_bir_sales_summary_report_id)
        REFERENCES pos.bir_sales_summary_reports (bir_sales_summary_report_id),
    CONSTRAINT uq_annex_e_reports__request UNIQUE (fiscal_report_request_id),
    CONSTRAINT uq_annex_e_reports__z_profile UNIQUE (governing_z_report_id, annex_e_contract_profile_ref),
    CONSTRAINT ck_annex_e_reports__request_kind CHECK (report_kind_code_id = 'd4c4615e-2cf2-59b7-a6d2-97d22210114c'),
    CONSTRAINT ck_annex_e_reports__committed_status CHECK (report_status_code_id = '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04'),
    CONSTRAINT ck_annex_e_reports__z_kind CHECK (governing_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'),
    CONSTRAINT ck_annex_e_reports__profile_ref CHECK (char_length(btrim(annex_e_contract_profile_ref)) > 0),
    CONSTRAINT ck_annex_e_reports__period_range CHECK (reporting_period_end_date >= reporting_period_start_date),
    CONSTRAINT ck_annex_e_reports__timestamps CHECK (committed_at >= generated_at AND updated_at = created_at)
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'pos' AND table_name = 'annex_e_reports' AND column_name = 'annex_e_context'
    ) AND EXISTS (SELECT 1 FROM pos.annex_e_reports) THEN
        RAISE EXCEPTION 'cannot harden non-empty legacy pos.annex_e_reports; migrate approved privacy-safe metadata first';
    END IF;
END;
$$;

ALTER TABLE pos.annex_e_reports
    ADD COLUMN IF NOT EXISTS report_kind_code_id uuid NULL,
    ADD COLUMN IF NOT EXISTS report_status_code_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_contract_version_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS governing_z_report_id uuid NULL,
    ADD COLUMN IF NOT EXISTS governing_report_kind_code_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_identity_id uuid NULL,
    ADD COLUMN IF NOT EXISTS annex_e_contract_profile_ref text NULL,
    ADD COLUMN IF NOT EXISTS committed_at timestamptz NULL;

ALTER TABLE pos.annex_e_reports
    DROP COLUMN IF EXISTS annex_e_type_code_id,
    DROP COLUMN IF EXISTS annex_e_context;

ALTER TABLE pos.annex_e_reports
    ALTER COLUMN report_kind_code_id SET NOT NULL,
    ALTER COLUMN report_status_code_id SET NOT NULL,
    ALTER COLUMN fiscal_reporting_contract_version_id SET NOT NULL,
    ALTER COLUMN fiscal_reporting_period_id SET NOT NULL,
    ALTER COLUMN governing_z_report_id SET NOT NULL,
    ALTER COLUMN governing_report_kind_code_id SET NOT NULL,
    ALTER COLUMN fiscal_identity_id SET NOT NULL,
    ALTER COLUMN annex_e_contract_profile_ref SET NOT NULL,
    ALTER COLUMN business_day_date SET NOT NULL,
    ALTER COLUMN reporting_period_start_date SET NOT NULL,
    ALTER COLUMN reporting_period_end_date SET NOT NULL,
    ALTER COLUMN generated_at SET NOT NULL,
    ALTER COLUMN committed_at SET NOT NULL;

ALTER TABLE pos.annex_e_reports
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__request,
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__site_pos_server,
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__annex_e_type,
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__request_kind,
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__status,
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__contract,
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__governing_z,
    DROP CONSTRAINT IF EXISTS fk_annex_e_reports__sales_summary,
    DROP CONSTRAINT IF EXISTS uq_annex_e_reports__request,
    DROP CONSTRAINT IF EXISTS uq_annex_e_reports__z_profile,
    DROP CONSTRAINT IF EXISTS ck_annex_e_reports__request_kind,
    DROP CONSTRAINT IF EXISTS ck_annex_e_reports__committed_status,
    DROP CONSTRAINT IF EXISTS ck_annex_e_reports__z_kind,
    DROP CONSTRAINT IF EXISTS ck_annex_e_reports__profile_ref,
    DROP CONSTRAINT IF EXISTS ck_annex_e_reports__period_range,
    DROP CONSTRAINT IF EXISTS ck_annex_e_reports__timestamps,
    DROP CONSTRAINT IF EXISTS ck_annex_e_reports__context_object;

ALTER TABLE pos.annex_e_reports
    ADD CONSTRAINT fk_annex_e_reports__request_kind FOREIGN KEY (fiscal_report_request_id, report_kind_code_id) REFERENCES pos.fiscal_report_requests (fiscal_report_request_id, report_type_code_id),
    ADD CONSTRAINT fk_annex_e_reports__status FOREIGN KEY (report_status_code_id) REFERENCES pos.controlled_codes (controlled_code_id),
    ADD CONSTRAINT fk_annex_e_reports__contract FOREIGN KEY (fiscal_reporting_contract_version_id) REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    ADD CONSTRAINT fk_annex_e_reports__governing_z FOREIGN KEY (governing_z_report_id, governing_report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id) REFERENCES pos.x_z_reports (x_z_report_id, report_kind_code_id, fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id),
    ADD CONSTRAINT fk_annex_e_reports__sales_summary FOREIGN KEY (related_bir_sales_summary_report_id) REFERENCES pos.bir_sales_summary_reports (bir_sales_summary_report_id),
    ADD CONSTRAINT uq_annex_e_reports__request UNIQUE (fiscal_report_request_id),
    ADD CONSTRAINT uq_annex_e_reports__z_profile UNIQUE (governing_z_report_id, annex_e_contract_profile_ref),
    ADD CONSTRAINT ck_annex_e_reports__request_kind CHECK (report_kind_code_id = 'd4c4615e-2cf2-59b7-a6d2-97d22210114c'),
    ADD CONSTRAINT ck_annex_e_reports__committed_status CHECK (report_status_code_id = '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04'),
    ADD CONSTRAINT ck_annex_e_reports__z_kind CHECK (governing_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'),
    ADD CONSTRAINT ck_annex_e_reports__profile_ref CHECK (char_length(btrim(annex_e_contract_profile_ref)) > 0),
    ADD CONSTRAINT ck_annex_e_reports__period_range CHECK (reporting_period_end_date >= reporting_period_start_date),
    ADD CONSTRAINT ck_annex_e_reports__timestamps CHECK (committed_at >= generated_at AND updated_at = created_at);

CREATE INDEX IF NOT EXISTS ix_annex_e_reports__site_business_date
    ON pos.annex_e_reports (site_pos_server_id, business_day_date DESC);

CREATE OR REPLACE TRIGGER trg_annex_e_reports_immutable
BEFORE UPDATE OR DELETE ON pos.annex_e_reports
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.annex_e_reports IS 'Immutable privacy-safe Annex E metadata bound to a governing Z Reading. No external dataset, customer payload, or file is stored.';
COMMENT ON COLUMN pos.annex_e_reports.annex_e_contract_profile_ref IS 'Required approved external contract/profile reference. Its absence blocks future generation.';
