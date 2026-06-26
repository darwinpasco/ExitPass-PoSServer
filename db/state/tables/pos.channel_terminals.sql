-- ExitPass POS Server first-slice table artifact.
-- Channel and terminal registry for child endpoints under Site POS Server.
-- Channels and terminals are not independent fiscal authorities and ONLINE/OFFLINE values are observability only.

CREATE TABLE IF NOT EXISTS pos.channel_terminals (
    channel_terminal_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    channel_terminal_code text NOT NULL,
    display_name text NOT NULL,
    channel_terminal_type_code_id uuid NULL,
    is_logical_channel boolean NOT NULL DEFAULT false,
    is_physical_terminal boolean NOT NULL DEFAULT false,
    external_channel_terminal_ref text NULL,
    vendor_ref text NULL,
    fiscal_identity_id uuid NULL,
    health_status_code_id uuid NULL,
    operational_status_code_id uuid NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_channel_terminals PRIMARY KEY (channel_terminal_id),
    CONSTRAINT fk_channel_terminals__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_channel_terminals__type_code FOREIGN KEY (channel_terminal_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_channel_terminals__fiscal_identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_channel_terminals__health_status_code FOREIGN KEY (health_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_channel_terminals__operational_status_code FOREIGN KEY (operational_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_channel_terminals__site_code UNIQUE (site_pos_server_id, channel_terminal_code),
    CONSTRAINT ck_channel_terminals__channel_terminal_code_not_blank CHECK (char_length(btrim(channel_terminal_code)) > 0),
    CONSTRAINT ck_channel_terminals__display_name_not_blank CHECK (char_length(btrim(display_name)) > 0),
    CONSTRAINT ck_channel_terminals__external_ref_not_blank CHECK (
        external_channel_terminal_ref IS NULL OR char_length(btrim(external_channel_terminal_ref)) > 0
    ),
    CONSTRAINT ck_channel_terminals__vendor_ref_not_blank CHECK (
        vendor_ref IS NULL OR char_length(btrim(vendor_ref)) > 0
    )
);

COMMENT ON TABLE pos.channel_terminals IS 'Child endpoint registry under Site POS Server. Supports logical WebPay, APM, cashier POS, EC/continuity, operator-assisted, and future channel posture without creating independent terminal fiscal authority.';
COMMENT ON COLUMN pos.channel_terminals.is_logical_channel IS 'Indicates logical channel posture, such as WebPay, without creating separate fiscal authority.';
COMMENT ON COLUMN pos.channel_terminals.is_physical_terminal IS 'Indicates physical terminal posture when applicable; not an independent fiscal authority.';
COMMENT ON COLUMN pos.channel_terminals.health_status_code_id IS 'Health/ONLINE/OFFLINE observability code reference only; does not approve offline fiscal issuance.';

