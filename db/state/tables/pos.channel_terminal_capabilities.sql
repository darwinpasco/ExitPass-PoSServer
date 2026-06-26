-- ExitPass POS Server first-slice table artifact.
-- Capability posture for channel/terminal child endpoints.
-- Capabilities describe presentation or operational capability only and do not authorize fiscal issuance behavior.

CREATE TABLE IF NOT EXISTS pos.channel_terminal_capabilities (
    channel_terminal_capability_id uuid NOT NULL,
    channel_terminal_id uuid NOT NULL,
    capability_code_id uuid NOT NULL,
    is_enabled boolean NOT NULL DEFAULT true,
    effective_start_at timestamptz NOT NULL,
    effective_end_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_channel_terminal_capabilities PRIMARY KEY (channel_terminal_capability_id),
    CONSTRAINT fk_channel_terminal_capabilities__channel_terminal FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT fk_channel_terminal_capabilities__capability_code FOREIGN KEY (capability_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_channel_terminal_capabilities__start UNIQUE (channel_terminal_id, capability_code_id, effective_start_at),
    CONSTRAINT ck_channel_terminal_capabilities__effective_range CHECK (
        effective_end_at IS NULL OR effective_end_at > effective_start_at
    )
);

COMMENT ON TABLE pos.channel_terminal_capabilities IS 'Capability posture for channel/terminal child endpoints, such as print, display, Digital SI URL presentation, or QR presentation capability. Does not create Digital SI URL token/access objects.';

