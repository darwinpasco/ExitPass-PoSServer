-- POS Server Slice 7: fiscal export request/status posture.
-- This table stores export request metadata only and does not generate export
-- files, calculate reports, or replace BIR outputs with ARTS POSLog.

CREATE TABLE IF NOT EXISTS pos.fiscal_export_requests (
    fiscal_export_request_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    requested_by_channel_terminal_id uuid NULL,
    export_type_code_id uuid NOT NULL,
    export_status_code_id uuid NOT NULL,
    business_day_date date NULL,
    period_start_at timestamptz NULL,
    period_end_at timestamptz NULL,
    requested_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    requested_by_ref text NULL,
    service_identity_ref text NULL,
    request_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_export_requests PRIMARY KEY (fiscal_export_request_id),
    CONSTRAINT fk_fiscal_export_requests__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_export_requests__channel
        FOREIGN KEY (requested_by_channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT fk_fiscal_export_requests__export_type
        FOREIGN KEY (export_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_export_requests__export_status
        FOREIGN KEY (export_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_export_requests__period_range
        CHECK (period_start_at IS NULL OR period_end_at IS NULL OR period_end_at > period_start_at),
    CONSTRAINT ck_fiscal_export_requests__requested_by_ref
        CHECK (requested_by_ref IS NULL OR btrim(requested_by_ref) <> ''),
    CONSTRAINT ck_fiscal_export_requests__service_ref
        CHECK (service_identity_ref IS NULL OR btrim(service_identity_ref) <> ''),
    CONSTRAINT ck_fiscal_export_requests__context_object
        CHECK (request_context IS NULL OR jsonb_typeof(request_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_export_requests IS
    'Export request and status posture for EJ, POSLog, JSON, report exports, and future approved export families. No export generation logic is created.';
COMMENT ON COLUMN pos.fiscal_export_requests.fiscal_export_request_id IS
    'Internal identifier for the fiscal export request.';
COMMENT ON COLUMN pos.fiscal_export_requests.site_pos_server_id IS
    'Site POS Server boundary for the export request.';
COMMENT ON COLUMN pos.fiscal_export_requests.requested_by_channel_terminal_id IS
    'Optional child channel or terminal that requested the export.';
COMMENT ON COLUMN pos.fiscal_export_requests.export_type_code_id IS
    'Controlled code identifying the export type.';
COMMENT ON COLUMN pos.fiscal_export_requests.export_status_code_id IS
    'Controlled code identifying the export request status.';
COMMENT ON COLUMN pos.fiscal_export_requests.requested_by_ref IS
    'External requester reference only.';
COMMENT ON COLUMN pos.fiscal_export_requests.service_identity_ref IS
    'External service identity reference only.';
COMMENT ON COLUMN pos.fiscal_export_requests.request_context IS
    'Optional export request metadata as a JSON object.';

