-- Canonical Electronic Journal stream head. One row serializes chronology and
-- integrity-chain allocation for one Site POS Server fiscal scope.

CREATE TABLE IF NOT EXISTS pos.electronic_journal_streams (
    electronic_journal_stream_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    currency_code char(3) NOT NULL,
    retention_policy_code_id uuid NOT NULL,
    chronology_version text NOT NULL,
    integrity_hash_version text NOT NULL,
    last_sequence_value bigint NOT NULL DEFAULT 0,
    last_event_hash char(64) NOT NULL DEFAULT repeat('0', 64),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_electronic_journal_streams PRIMARY KEY (electronic_journal_stream_id),
    CONSTRAINT fk_ej_streams__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_ej_streams__fiscal_identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_ej_streams__retention FOREIGN KEY (retention_policy_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_ej_streams__scope UNIQUE (site_pos_server_id, fiscal_identity_id, currency_code),
    CONSTRAINT ck_ej_streams__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_ej_streams__versions CHECK (
        chronology_version = 'pos-server-electronic-journal-chronology:v1'
        AND integrity_hash_version = 'pos-server-electronic-journal-integrity:sha256:v1'
    ),
    CONSTRAINT ck_ej_streams__sequence CHECK (last_sequence_value >= 0),
    CONSTRAINT ck_ej_streams__hash CHECK (last_event_hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT ck_ej_streams__retention_family CHECK (
        retention_policy_code_id = 'bf711168-63d1-539e-9e8f-6de908978b3e'
    )
);

CREATE INDEX IF NOT EXISTS ix_ej_streams__identity_currency
    ON pos.electronic_journal_streams (fiscal_identity_id, currency_code);

COMMENT ON TABLE pos.electronic_journal_streams IS
    'Mutable stream head used only to serialize canonical EJ sequence and integrity hash allocation; fiscal events remain in immutable records.';
COMMENT ON COLUMN pos.electronic_journal_streams.retention_policy_code_id IS
    'Governed retention classification. Z-011A defines no destructive purge duration or worker.';
