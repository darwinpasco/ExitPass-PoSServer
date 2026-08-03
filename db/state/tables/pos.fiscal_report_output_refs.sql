-- Fiscal report output reference lifecycle only; no generated content is stored.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_output_refs (
    fiscal_report_output_ref_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    output_type_code_id uuid NOT NULL,
    output_status_code_id uuid NOT NULL,
    output_ref text NULL,
    generated_at timestamptz NULL,
    generated_by_ref text NULL,
    channel_terminal_id uuid NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_output_refs PRIMARY KEY (fiscal_report_output_ref_id),
    CONSTRAINT fk_fiscal_report_output_refs__request FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_report_output_refs__output_type FOREIGN KEY (output_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_output_refs__output_status FOREIGN KEY (output_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_output_refs__channel FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT uq_fiscal_report_output_refs__request_type UNIQUE (fiscal_report_request_id, output_type_code_id),
    CONSTRAINT ck_fiscal_report_output_refs__type_family CHECK (output_type_code_id IN (
        'd8f2e655-cb41-52ad-a39d-0f5d1005ba1a',
        'cc4ac16f-401f-5b8e-b817-464fc1e4ceb2',
        '9c2b928f-13bf-5226-9a26-4fc4b4966056'
    )),
    CONSTRAINT ck_fiscal_report_output_refs__status_family CHECK (output_status_code_id IN (
        '94bcd4c0-f938-5efc-8b05-51b5f5f32e94',
        'a94d293e-f592-5d9e-b8e1-e67bb6f566c7',
        'f6738162-9db7-5b0b-8f7e-a8736f07d2ff',
        '22758b2e-3a90-59d9-b7a3-a284aed87e12'
    )),
    CONSTRAINT ck_fiscal_report_output_refs__output_ref CHECK (output_ref IS NULL OR btrim(output_ref) <> ''),
    CONSTRAINT ck_fiscal_report_output_refs__generated_by_ref CHECK (generated_by_ref IS NULL OR btrim(generated_by_ref) <> '')
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'pos' AND table_name = 'fiscal_report_output_refs' AND column_name = 'output_context'
    ) AND EXISTS (SELECT 1 FROM pos.fiscal_report_output_refs) THEN
        RAISE EXCEPTION 'cannot remove output_context from non-empty legacy report outputs; migrate governed references first';
    END IF;
END;
$$;

ALTER TABLE pos.fiscal_report_output_refs DROP COLUMN IF EXISTS output_context;

ALTER TABLE pos.fiscal_report_output_refs
    DROP CONSTRAINT IF EXISTS uq_fiscal_report_output_refs__request_type,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_output_refs__type_family,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_output_refs__status_family;

ALTER TABLE pos.fiscal_report_output_refs
    ADD CONSTRAINT uq_fiscal_report_output_refs__request_type UNIQUE (fiscal_report_request_id, output_type_code_id),
    ADD CONSTRAINT ck_fiscal_report_output_refs__type_family CHECK (output_type_code_id IN ('d8f2e655-cb41-52ad-a39d-0f5d1005ba1a','cc4ac16f-401f-5b8e-b817-464fc1e4ceb2','9c2b928f-13bf-5226-9a26-4fc4b4966056')),
    ADD CONSTRAINT ck_fiscal_report_output_refs__status_family CHECK (output_status_code_id IN ('94bcd4c0-f938-5efc-8b05-51b5f5f32e94','a94d293e-f592-5d9e-b8e1-e67bb6f566c7','f6738162-9db7-5b0b-8f7e-a8736f07d2ff','22758b2e-3a90-59d9-b7a3-a284aed87e12'));

CREATE INDEX IF NOT EXISTS ix_fiscal_report_output_refs__status
    ON pos.fiscal_report_output_refs (output_status_code_id, created_at);

COMMENT ON TABLE pos.fiscal_report_output_refs IS 'Governed report output reference lifecycle. Generated files, payloads, credentials, and binaries are not stored.';
