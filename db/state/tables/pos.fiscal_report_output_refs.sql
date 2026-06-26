-- POS Server Slice 6: fiscal report output reference posture.
-- This table stores output references and metadata only; it does not store
-- generated PDF, print, JSON, EJ, POSLog, or export package binaries.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_output_refs (
    fiscal_report_output_ref_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    output_type_code_id uuid NOT NULL,
    output_status_code_id uuid NOT NULL,
    output_ref text NULL,
    generated_at timestamptz NULL,
    generated_by_ref text NULL,
    channel_terminal_id uuid NULL,
    output_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_output_refs PRIMARY KEY (fiscal_report_output_ref_id),
    CONSTRAINT fk_fiscal_report_output_refs__request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_report_output_refs__output_type
        FOREIGN KEY (output_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_output_refs__output_status
        FOREIGN KEY (output_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_output_refs__channel
        FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT ck_fiscal_report_output_refs__output_ref
        CHECK (output_ref IS NULL OR btrim(output_ref) <> ''),
    CONSTRAINT ck_fiscal_report_output_refs__generated_by_ref
        CHECK (generated_by_ref IS NULL OR btrim(generated_by_ref) <> ''),
    CONSTRAINT ck_fiscal_report_output_refs__context_object
        CHECK (output_context IS NULL OR jsonb_typeof(output_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_report_output_refs IS
    'Report output reference posture for print, PDF, JSON, and future approved output references; generated content is not stored here.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.fiscal_report_output_ref_id IS
    'Internal identifier for the report output reference.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.fiscal_report_request_id IS
    'Report request associated with the output reference.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.output_type_code_id IS
    'Controlled code identifying the output type.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.output_status_code_id IS
    'Controlled code identifying the output status.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.output_ref IS
    'External output reference only; generated files or binaries are not stored in this table.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.generated_by_ref IS
    'External actor or service reference only.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.channel_terminal_id IS
    'Optional child channel or terminal associated with the output reference.';
COMMENT ON COLUMN pos.fiscal_report_output_refs.output_context IS
    'Optional output metadata as a JSON object.';

