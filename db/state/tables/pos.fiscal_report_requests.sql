-- POS Server Slice 6: fiscal report request/status posture.
-- This table records report lifecycle metadata only; it does not generate,
-- calculate, or store report output binaries.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_requests (
    fiscal_report_request_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    requested_by_channel_terminal_id uuid NULL,
    report_type_code_id uuid NOT NULL,
    report_status_code_id uuid NOT NULL,
    business_day_date date NULL,
    requested_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    requested_by_ref text NULL,
    service_identity_ref text NULL,
    request_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_requests PRIMARY KEY (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_report_requests__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_report_requests__channel
        FOREIGN KEY (requested_by_channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT fk_fiscal_report_requests__report_type
        FOREIGN KEY (report_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_requests__report_status
        FOREIGN KEY (report_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_report_requests__requested_by_ref
        CHECK (requested_by_ref IS NULL OR btrim(requested_by_ref) <> ''),
    CONSTRAINT ck_fiscal_report_requests__service_ref
        CHECK (service_identity_ref IS NULL OR btrim(service_identity_ref) <> ''),
    CONSTRAINT ck_fiscal_report_requests__context_object
        CHECK (request_context IS NULL OR jsonb_typeof(request_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_report_requests IS
    'Report request and status posture for X-read, Z-read, BIR Sales Summary, Annex E, and future approved fiscal reports.';
COMMENT ON COLUMN pos.fiscal_report_requests.fiscal_report_request_id IS
    'Internal identifier for the fiscal report request.';
COMMENT ON COLUMN pos.fiscal_report_requests.site_pos_server_id IS
    'Site POS Server boundary for the report request.';
COMMENT ON COLUMN pos.fiscal_report_requests.requested_by_channel_terminal_id IS
    'Optional child channel or terminal that initiated the report request.';
COMMENT ON COLUMN pos.fiscal_report_requests.report_type_code_id IS
    'Controlled code identifying the report type.';
COMMENT ON COLUMN pos.fiscal_report_requests.report_status_code_id IS
    'Controlled code identifying the report request status.';
COMMENT ON COLUMN pos.fiscal_report_requests.business_day_date IS
    'Optional business day associated with the report request.';
COMMENT ON COLUMN pos.fiscal_report_requests.requested_by_ref IS
    'External actor reference only; this does not create an identity authority table.';
COMMENT ON COLUMN pos.fiscal_report_requests.service_identity_ref IS
    'External service identity reference only.';
COMMENT ON COLUMN pos.fiscal_report_requests.request_context IS
    'Optional report request metadata as a JSON object.';

