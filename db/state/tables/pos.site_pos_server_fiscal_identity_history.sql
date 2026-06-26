-- ExitPass POS Server first-slice table artifact.
-- Effective-dated relationship between Site POS Server fiscal boundaries and fiscal identities.
-- Does not create fiscal issuance or fiscal sequencing behavior.

CREATE TABLE IF NOT EXISTS pos.site_pos_server_fiscal_identity_history (
    site_pos_server_fiscal_identity_history_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    effective_start_at timestamptz NOT NULL,
    effective_end_at timestamptz NULL,
    assignment_reason_code_id uuid NULL,
    assignment_reason_text text NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_site_pos_server_fiscal_identity_history PRIMARY KEY (site_pos_server_fiscal_identity_history_id),
    CONSTRAINT fk_site_pos_server_fiscal_identity_history__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_site_pos_server_fiscal_identity_history__fiscal_identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_site_pos_fiscal_identity_hist__assignment_reason_code FOREIGN KEY (assignment_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_site_pos_server_fiscal_identity_history__start UNIQUE (site_pos_server_id, fiscal_identity_id, effective_start_at),
    CONSTRAINT ck_site_pos_server_fiscal_identity_history__effective_range CHECK (
        effective_end_at IS NULL OR effective_end_at > effective_start_at
    ),
    CONSTRAINT ck_site_pos_fiscal_identity_hist__reason_text_not_blank CHECK (
        assignment_reason_text IS NULL OR char_length(btrim(assignment_reason_text)) > 0
    )
);

COMMENT ON TABLE pos.site_pos_server_fiscal_identity_history IS 'Effective-dated Site POS Server to fiscal identity relationship history. Supports identity posture only; not fiscal issuance or numbering.';

