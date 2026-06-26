-- ExitPass POS Server first-slice table artifact.
-- Observed channel/terminal status history for child endpoints.
-- ONLINE/OFFLINE and related health values are observability only and do not authorize offline fiscal issuance.

CREATE TABLE IF NOT EXISTS pos.channel_terminal_status_history (
    channel_terminal_status_history_id uuid NOT NULL,
    channel_terminal_id uuid NOT NULL,
    prior_health_status_code_id uuid NULL,
    new_health_status_code_id uuid NOT NULL,
    status_reason_code_id uuid NULL,
    status_reason_text text NULL,
    observed_at timestamptz NOT NULL,
    actor_ref text NULL,
    service_ref text NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_channel_terminal_status_history PRIMARY KEY (channel_terminal_status_history_id),
    CONSTRAINT fk_channel_terminal_status_history__channel_terminal FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT fk_channel_terminal_status_history__prior_health_status_code FOREIGN KEY (prior_health_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_channel_terminal_status_history__new_health_status_code FOREIGN KEY (new_health_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_channel_terminal_status_history__status_reason_code FOREIGN KEY (status_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_channel_terminal_status_hist__reason_text_not_blank CHECK (
        status_reason_text IS NULL OR char_length(btrim(status_reason_text)) > 0
    ),
    CONSTRAINT ck_channel_terminal_status_history__actor_ref_not_blank CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_channel_terminal_status_history__service_ref_not_blank CHECK (
        service_ref IS NULL OR char_length(btrim(service_ref)) > 0
    )
);

COMMENT ON TABLE pos.channel_terminal_status_history IS 'Observed channel/terminal status history. ONLINE/OFFLINE is observability only and does not create offline fiscal issuance authority.';
COMMENT ON COLUMN pos.channel_terminal_status_history.actor_ref IS 'Reference to actor context when available; reference only.';
COMMENT ON COLUMN pos.channel_terminal_status_history.service_ref IS 'Reference to observing service context when available; reference only.';

