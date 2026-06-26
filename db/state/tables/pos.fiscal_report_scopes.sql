-- POS Server Slice 6: fiscal report scope posture.
-- This table stores report scope references only and does not finalize X/Z,
-- BIR Sales Summary, Annex E, or export aggregation behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_scopes (
    fiscal_report_scope_id uuid NOT NULL,
    fiscal_report_request_id uuid NOT NULL,
    scope_type_code_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    channel_terminal_id uuid NULL,
    business_day_date date NULL,
    period_start_at timestamptz NULL,
    period_end_at timestamptz NULL,
    begin_fiscal_document_id uuid NULL,
    end_fiscal_document_id uuid NULL,
    reset_counter_state_id uuid NULL,
    z_counter_state_id uuid NULL,
    scope_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_scopes PRIMARY KEY (fiscal_report_scope_id),
    CONSTRAINT fk_fiscal_report_scopes__request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_report_scopes__scope_type
        FOREIGN KEY (scope_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_scopes__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_report_scopes__channel
        FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT fk_fiscal_report_scopes__begin_doc
        FOREIGN KEY (begin_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_report_scopes__end_doc
        FOREIGN KEY (end_fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_report_scopes__reset_counter
        FOREIGN KEY (reset_counter_state_id)
        REFERENCES pos.fiscal_counter_states (fiscal_counter_state_id),
    CONSTRAINT fk_fiscal_report_scopes__z_counter
        FOREIGN KEY (z_counter_state_id)
        REFERENCES pos.fiscal_counter_states (fiscal_counter_state_id),
    CONSTRAINT ck_fiscal_report_scopes__period_range
        CHECK (period_start_at IS NULL OR period_end_at IS NULL OR period_end_at > period_start_at),
    CONSTRAINT ck_fiscal_report_scopes__context_object
        CHECK (scope_context IS NULL OR jsonb_typeof(scope_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_report_scopes IS
    'Report scope posture for Site POS Server, channel/terminal, business-day, fiscal-document range, and counter references.';
COMMENT ON COLUMN pos.fiscal_report_scopes.fiscal_report_scope_id IS
    'Internal identifier for the report scope row.';
COMMENT ON COLUMN pos.fiscal_report_scopes.fiscal_report_request_id IS
    'Report request that owns the scope metadata.';
COMMENT ON COLUMN pos.fiscal_report_scopes.scope_type_code_id IS
    'Controlled code identifying the scope type.';
COMMENT ON COLUMN pos.fiscal_report_scopes.site_pos_server_id IS
    'Site POS Server boundary for the scope.';
COMMENT ON COLUMN pos.fiscal_report_scopes.channel_terminal_id IS
    'Optional child channel or terminal included in the scope.';
COMMENT ON COLUMN pos.fiscal_report_scopes.begin_fiscal_document_id IS
    'Optional beginning fiscal document reference for scope metadata.';
COMMENT ON COLUMN pos.fiscal_report_scopes.end_fiscal_document_id IS
    'Optional ending fiscal document reference for scope metadata.';
COMMENT ON COLUMN pos.fiscal_report_scopes.reset_counter_state_id IS
    'Optional reset counter state reference; this does not increment counters.';
COMMENT ON COLUMN pos.fiscal_report_scopes.z_counter_state_id IS
    'Optional Z-counter state reference; this does not close a fiscal day.';
COMMENT ON COLUMN pos.fiscal_report_scopes.scope_context IS
    'Optional scope metadata as a JSON object.';

