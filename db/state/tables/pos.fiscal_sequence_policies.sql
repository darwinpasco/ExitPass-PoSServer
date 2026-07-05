-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal sequence policy state/posture only.
-- Runtime fiscal numbering resolves policies from this table; PostgreSQL sequences are not used.

CREATE TABLE IF NOT EXISTS pos.fiscal_sequence_policies (
    fiscal_sequence_policy_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    sequence_family_code_id uuid NOT NULL,
    document_type_code_id uuid NULL,
    policy_code text NOT NULL,
    display_name text NOT NULL,
    description text NULL,
    prefix_text text NULL,
    suffix_text text NULL,
    padding_length integer NULL,
    current_policy_status_code_id uuid NOT NULL,
    effective_start_at timestamptz NOT NULL,
    effective_end_at timestamptz NULL,
    policy_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_sequence_policies PRIMARY KEY (fiscal_sequence_policy_id),
    CONSTRAINT fk_fiscal_seq_policies__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_seq_policies__sequence_family FOREIGN KEY (sequence_family_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_seq_policies__document_type FOREIGN KEY (document_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_seq_policies__policy_status FOREIGN KEY (current_policy_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_seq_policies__site_policy UNIQUE (site_pos_server_id, policy_code),
    CONSTRAINT ck_fiscal_seq_policies__policy_code CHECK (char_length(btrim(policy_code)) > 0),
    CONSTRAINT ck_fiscal_seq_policies__display_name CHECK (char_length(btrim(display_name)) > 0),
    CONSTRAINT ck_fiscal_seq_policies__prefix_text CHECK (
        prefix_text IS NULL OR char_length(btrim(prefix_text)) > 0
    ),
    CONSTRAINT ck_fiscal_seq_policies__suffix_text CHECK (
        suffix_text IS NULL OR char_length(btrim(suffix_text)) > 0
    ),
    CONSTRAINT ck_fiscal_seq_policies__padding_positive CHECK (
        padding_length IS NULL OR padding_length > 0
    ),
    CONSTRAINT ck_fiscal_seq_policies__effective_range CHECK (
        effective_end_at IS NULL OR effective_end_at > effective_start_at
    ),
    CONSTRAINT ck_fiscal_seq_policies__context_object CHECK (
        policy_context IS NULL OR jsonb_typeof(policy_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_sequence_policies IS 'Configurable fiscal sequence policy by Site POS Server and sequence family. Runtime allocation resolves eligible policies from this table without PostgreSQL sequence objects.';
COMMENT ON COLUMN pos.fiscal_sequence_policies.sequence_family_code_id IS 'Controlled-code reference for sequence family, such as future SI or adjustment family.';
COMMENT ON COLUMN pos.fiscal_sequence_policies.document_type_code_id IS 'Optional controlled-code reference for document type where the sequence policy is document-type-specific.';
COMMENT ON COLUMN pos.fiscal_sequence_policies.policy_context IS 'Flexible policy context for unresolved numbering attributes. Must remain a JSON object when present.';
