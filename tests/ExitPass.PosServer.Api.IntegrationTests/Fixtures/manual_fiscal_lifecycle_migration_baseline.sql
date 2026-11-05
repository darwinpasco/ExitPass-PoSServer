CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA pos;

CREATE TABLE pos.site_pos_servers (
    site_pos_server_id uuid PRIMARY KEY,
    is_active boolean NOT NULL DEFAULT true
);
CREATE TABLE pos.fiscal_identities (fiscal_identity_id uuid PRIMARY KEY);

CREATE TABLE pos.fiscal_reporting_periods (
    fiscal_reporting_period_id uuid PRIMARY KEY,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL REFERENCES pos.site_pos_servers(site_pos_server_id),
    fiscal_identity_id uuid NOT NULL REFERENCES pos.fiscal_identities(fiscal_identity_id),
    period_status_code_id uuid NOT NULL,
    business_day_date date NOT NULL,
    period_start_at timestamptz NOT NULL,
    period_end_at timestamptz NOT NULL,
    reporting_timezone_name text NOT NULL,
    business_day_cutoff_local_time time NOT NULL,
    currency_code char(3) NOT NULL,
    period_sequence bigint NOT NULL,
    expected_prior_period_id uuid NULL,
    opened_at timestamptz NOT NULL,
    closing_started_at timestamptz NULL,
    closed_at timestamptz NULL,
    created_by_ref text NOT NULL,
    updated_by_ref text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_fiscal_reporting_periods__snapshot_identity UNIQUE (
        fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,
        fiscal_identity_id,business_day_date,period_start_at,period_end_at,currency_code),
    CONSTRAINT uq_fiscal_reporting_periods__assignment_scope UNIQUE (
        fiscal_reporting_period_id,site_pos_server_id,fiscal_identity_id,currency_code),
    CONSTRAINT ck_fiscal_reporting_periods__period_range CHECK (period_end_at > period_start_at),
    CONSTRAINT ck_fiscal_reporting_periods__closed_posture CHECK (
        (period_status_code_id='af7ee931-a023-507e-81a4-17adf047eb94' AND closed_at IS NOT NULL)
        OR (period_status_code_id<>'af7ee931-a023-507e-81a4-17adf047eb94' AND closed_at IS NULL))
);

CREATE TABLE pos.x_z_reports (
    x_z_report_id uuid PRIMARY KEY,
    fiscal_reporting_period_id uuid NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    business_day_date date NOT NULL,
    period_start_at timestamptz NOT NULL,
    period_end_at timestamptz NOT NULL,
    currency_code char(3) NOT NULL,
    CONSTRAINT fk_x_z_reports__period_identity FOREIGN KEY (
        fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,
        fiscal_identity_id,business_day_date,period_start_at,period_end_at,currency_code)
    REFERENCES pos.fiscal_reporting_periods (
        fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,
        fiscal_identity_id,business_day_date,period_start_at,period_end_at,currency_code)
);

CREATE TABLE pos.fiscal_report_requests (
    fiscal_report_request_id uuid PRIMARY KEY,
    semantic_hash_version text NOT NULL,
    CONSTRAINT ck_fiscal_report_requests__semantic_version CHECK (
        semantic_hash_version = 'pos-server-fiscal-report-request:sha256:v1')
);

INSERT INTO pos.site_pos_servers VALUES ('10000000-0000-4000-8000-000000000001',true);
INSERT INTO pos.fiscal_identities VALUES ('10000000-0000-4000-8000-000000000002');
INSERT INTO pos.fiscal_reporting_periods(
    fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,
    period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,
    business_day_cutoff_local_time,currency_code,period_sequence,opened_at,created_by_ref,updated_by_ref)
VALUES(
    '10000000-0000-4000-8000-000000000003','10000000-0000-4000-8000-000000000004',
    '10000000-0000-4000-8000-000000000001','10000000-0000-4000-8000-000000000002',
    '1a6f7021-bc84-5c01-afaa-c5d6685633c8','2026-11-05','2026-11-04T23:00:00Z',
    '2026-11-05T23:00:00Z','Asia/Manila','07:00:00','PHP',1,'2026-11-04T23:00:00Z','fixture','fixture');
INSERT INTO pos.fiscal_reporting_periods(
    fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,
    period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,
    business_day_cutoff_local_time,currency_code,period_sequence,opened_at,closing_started_at,closed_at,
    created_by_ref,updated_by_ref)
VALUES(
    '10000000-0000-4000-8000-000000000006','10000000-0000-4000-8000-000000000004',
    '10000000-0000-4000-8000-000000000001','10000000-0000-4000-8000-000000000002',
    'af7ee931-a023-507e-81a4-17adf047eb94','2026-11-04','2026-11-03T23:00:00Z',
    '2026-11-04T23:00:00Z','Asia/Manila','07:00:00','PHP',0,'2026-11-03T23:00:00Z',
    '2026-11-04T22:30:00Z','2026-11-04T22:45:00Z','fixture','fixture');
INSERT INTO pos.x_z_reports VALUES(
    '10000000-0000-4000-8000-000000000005','10000000-0000-4000-8000-000000000003',
    '10000000-0000-4000-8000-000000000004','10000000-0000-4000-8000-000000000001',
    '10000000-0000-4000-8000-000000000002','2026-11-05','2026-11-04T23:00:00Z',
    '2026-11-05T23:00:00Z','PHP');
