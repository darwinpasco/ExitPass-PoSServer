-- Immutable metadata for report artifacts persisted outside the container writable layer.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_artifacts (
    fiscal_report_artifact_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NULL,
    currency_code char(3) NOT NULL,
    report_type text NOT NULL,
    scope_reference text NOT NULL,
    format text NOT NULL,
    artifact_version text NOT NULL,
    source_fingerprint char(64) NOT NULL,
    fiscal_reporting_period_id uuid NULL,
    calendar_year integer NULL,
    calendar_month integer NULL,
    business_day_date date NULL,
    revision integer NOT NULL,
    supersedes_artifact_id uuid NULL,
    artifact_storage_key text NOT NULL,
    artifact_sha256 char(64) NOT NULL,
    artifact_byte_length bigint NOT NULL,
    mime_type text NOT NULL,
    file_name text NOT NULL,
    generated_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_artifacts PRIMARY KEY (fiscal_report_artifact_id),
    CONSTRAINT fk_fiscal_report_artifacts__site FOREIGN KEY (site_pos_server_id) REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_report_artifacts__identity FOREIGN KEY (fiscal_identity_id) REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_fiscal_report_artifacts__period FOREIGN KEY (fiscal_reporting_period_id) REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT fk_fiscal_report_artifacts__supersedes FOREIGN KEY (supersedes_artifact_id) REFERENCES pos.fiscal_report_artifacts (fiscal_report_artifact_id),
    CONSTRAINT uq_fiscal_report_artifacts__scope_revision UNIQUE NULLS NOT DISTINCT (site_pos_server_id, fiscal_identity_id, currency_code, report_type, scope_reference, format, revision),
    CONSTRAINT uq_fiscal_report_artifacts__supersedes UNIQUE (supersedes_artifact_id),
    CONSTRAINT ck_fiscal_report_artifacts__scope CHECK (
        currency_code ~ '^[A-Z]{3}$' AND report_type ~ '^[A-Z0-9_]{1,80}$' AND
        char_length(scope_reference) BETWEEN 1 AND 500 AND format ~ '^[a-z0-9_-]{1,20}$' AND
        char_length(artifact_version) BETWEEN 1 AND 200 AND revision > 0),
    CONSTRAINT ck_fiscal_report_artifacts__calendar CHECK (
        (calendar_year IS NULL AND calendar_month IS NULL) OR
        (calendar_year BETWEEN 2000 AND 9999 AND calendar_month BETWEEN 1 AND 12)),
    CONSTRAINT ck_fiscal_report_artifacts__integrity CHECK (
        source_fingerprint ~ '^[0-9a-f]{64}$' AND artifact_sha256 ~ '^[0-9a-f]{64}$' AND artifact_byte_length > 0),
    CONSTRAINT ck_fiscal_report_artifacts__storage CHECK (
        char_length(btrim(artifact_storage_key)) > 0 AND artifact_storage_key !~ '(^/|\\\\|(^|/)\.\.(/|$))' AND
        char_length(btrim(mime_type)) > 0 AND char_length(file_name) BETWEEN 1 AND 255 AND file_name !~ '[/\\\\]')
);

CREATE INDEX IF NOT EXISTS ix_fiscal_report_artifacts__scope_current
    ON pos.fiscal_report_artifacts (site_pos_server_id, fiscal_identity_id, currency_code, report_type, scope_reference, format, revision DESC);

CREATE INDEX IF NOT EXISTS ix_fiscal_report_artifacts__period
    ON pos.fiscal_report_artifacts (fiscal_reporting_period_id, report_type, format)
    WHERE fiscal_reporting_period_id IS NOT NULL;

CREATE OR REPLACE TRIGGER trg_fiscal_report_artifacts_immutable
    BEFORE UPDATE OR DELETE ON pos.fiscal_report_artifacts
    FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_report_artifacts IS 'Immutable catalog of content-addressed fiscal report artifacts stored in durable POS Server host storage.';
