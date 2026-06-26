-- POS Server Slice 8: fiscal anchor reference posture.
-- This table stores tamper-evident anchor references only and does not
-- implement hash generation, external anchoring, or cryptographic services.

CREATE TABLE IF NOT EXISTS pos.fiscal_anchor_refs (
    fiscal_anchor_ref_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    anchor_type_code_id uuid NOT NULL,
    anchor_status_code_id uuid NOT NULL,
    fiscal_state_snapshot_id uuid NULL,
    electronic_journal_record_ref text NULL,
    anchor_ref text NULL,
    anchor_hash_ref text NULL,
    previous_anchor_hash_ref text NULL,
    anchored_at timestamptz NULL,
    anchor_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_anchor_refs PRIMARY KEY (fiscal_anchor_ref_id),
    CONSTRAINT fk_fiscal_anchor_refs__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_anchor_refs__anchor_type
        FOREIGN KEY (anchor_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_anchor_refs__anchor_status
        FOREIGN KEY (anchor_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_anchor_refs__snapshot
        FOREIGN KEY (fiscal_state_snapshot_id)
        REFERENCES pos.fiscal_state_snapshots (fiscal_state_snapshot_id),
    CONSTRAINT ck_fiscal_anchor_refs__ej_record_ref
        CHECK (electronic_journal_record_ref IS NULL OR btrim(electronic_journal_record_ref) <> ''),
    CONSTRAINT ck_fiscal_anchor_refs__anchor_ref
        CHECK (anchor_ref IS NULL OR btrim(anchor_ref) <> ''),
    CONSTRAINT ck_fiscal_anchor_refs__anchor_hash_ref
        CHECK (anchor_hash_ref IS NULL OR btrim(anchor_hash_ref) <> ''),
    CONSTRAINT ck_fiscal_anchor_refs__previous_hash_ref
        CHECK (previous_anchor_hash_ref IS NULL OR btrim(previous_anchor_hash_ref) <> ''),
    CONSTRAINT ck_fiscal_anchor_refs__context_object
        CHECK (anchor_context IS NULL OR jsonb_typeof(anchor_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_anchor_refs IS
    'Tamper-evident fiscal anchor reference posture. Stores references only and does not implement hash generation, external anchoring, or cryptographic services.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.fiscal_anchor_ref_id IS
    'Internal identifier for the fiscal anchor reference.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.site_pos_server_id IS
    'Site POS Server boundary for the anchor reference.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.anchor_type_code_id IS
    'Controlled code identifying the anchor type.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.anchor_status_code_id IS
    'Controlled code identifying anchor status.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.fiscal_state_snapshot_id IS
    'Optional fiscal state snapshot reference associated with the anchor posture.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.electronic_journal_record_ref IS
    'Electronic Journal record reference text only; no anchoring relationship is implemented.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.anchor_ref IS
    'External anchor reference only.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.anchor_hash_ref IS
    'Anchor hash reference only; raw cryptographic material is not stored.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.previous_anchor_hash_ref IS
    'Previous anchor hash reference only; hash chaining is not implemented.';
COMMENT ON COLUMN pos.fiscal_anchor_refs.anchor_context IS
    'Optional anchor metadata as a JSON object; do not store raw cryptographic material.';

