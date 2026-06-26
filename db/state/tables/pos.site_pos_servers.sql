-- ExitPass POS Server first-slice table artifact.
-- Site POS Server fiscal boundary records.
-- Central PMS site and site-resolution values are stored as references only and do not transfer Central PMS authority.

CREATE TABLE IF NOT EXISTS pos.site_pos_servers (
    site_pos_server_id uuid NOT NULL,
    site_pos_server_code text NOT NULL,
    display_name text NOT NULL,
    central_pms_site_ref text NULL,
    central_pms_site_resolution_ref text NULL,
    operational_status_code_id uuid NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_site_pos_servers PRIMARY KEY (site_pos_server_id),
    CONSTRAINT fk_site_pos_servers__operational_status_code FOREIGN KEY (operational_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_site_pos_servers__site_pos_server_code UNIQUE (site_pos_server_code),
    CONSTRAINT ck_site_pos_servers__site_pos_server_code_not_blank CHECK (char_length(btrim(site_pos_server_code)) > 0),
    CONSTRAINT ck_site_pos_servers__display_name_not_blank CHECK (char_length(btrim(display_name)) > 0),
    CONSTRAINT ck_site_pos_servers__central_pms_site_ref_not_blank CHECK (
        central_pms_site_ref IS NULL OR char_length(btrim(central_pms_site_ref)) > 0
    ),
    CONSTRAINT ck_site_pos_servers__central_pms_site_resolution_ref_not_blank CHECK (
        central_pms_site_resolution_ref IS NULL OR char_length(btrim(central_pms_site_resolution_ref)) > 0
    )
);

COMMENT ON TABLE pos.site_pos_servers IS 'Site POS Server fiscal boundary records. Central PMS site values are reference-only and do not model Central PMS lifecycle ownership.';
COMMENT ON COLUMN pos.site_pos_servers.central_pms_site_ref IS 'Reference to the Central PMS site context; POS Server does not own Central PMS site authority.';
COMMENT ON COLUMN pos.site_pos_servers.central_pms_site_resolution_ref IS 'Reference to Central PMS site-resolution context; reference only.';

