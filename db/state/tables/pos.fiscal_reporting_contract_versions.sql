-- POS Server internal fiscal-reporting contract versions.
-- Contract metadata is immutable and does not execute report generation.

CREATE TABLE IF NOT EXISTS pos.fiscal_reporting_contract_versions (
    fiscal_reporting_contract_version_id uuid NOT NULL,
    contract_key text NOT NULL,
    contract_version text NOT NULL,
    semantic_hash_version text NOT NULL,
    effective_from timestamptz NOT NULL,
    effective_to timestamptz NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_reporting_contract_versions PRIMARY KEY (fiscal_reporting_contract_version_id),
    CONSTRAINT uq_fiscal_reporting_contract_versions__key_version UNIQUE (contract_key, contract_version),
    CONSTRAINT uq_fiscal_reporting_contract_versions__semantic_hash UNIQUE (semantic_hash_version),
    CONSTRAINT ck_fiscal_reporting_contract_versions__key CHECK (char_length(btrim(contract_key)) > 0),
    CONSTRAINT ck_fiscal_reporting_contract_versions__version CHECK (char_length(btrim(contract_version)) > 0),
    CONSTRAINT ck_fiscal_reporting_contract_versions__semantic_hash CHECK (char_length(btrim(semantic_hash_version)) > 0),
    CONSTRAINT ck_fiscal_reporting_contract_versions__effective_range CHECK (
        effective_to IS NULL OR effective_to > effective_from
    )
);

INSERT INTO pos.fiscal_reporting_contract_versions (
    fiscal_reporting_contract_version_id,
    contract_key,
    contract_version,
    semantic_hash_version,
    effective_from,
    is_active
) VALUES (
    'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
    'pos-server-fiscal-reporting',
    'v1',
    'pos-server-fiscal-report-request:sha256:v1',
    '2026-08-03T00:00:00Z',
    true
) ON CONFLICT (fiscal_reporting_contract_version_id) DO NOTHING;

CREATE OR REPLACE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'committed fiscal reporting snapshots are immutable'
        USING ERRCODE = 'check_violation';
END;
$$;

CREATE OR REPLACE TRIGGER trg_fiscal_reporting_contract_versions_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_reporting_contract_versions
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_reporting_contract_versions IS 'Immutable internal fiscal-reporting contract/version registry. It freezes schema semantics and does not authorize X or Z runtime.';
COMMENT ON COLUMN pos.fiscal_reporting_contract_versions.semantic_hash_version IS 'Version marker for future report-request semantic hashing; unrelated to fiscal-document request sha256:v1 or statutory sha256:v2.';
COMMENT ON FUNCTION pos.reject_fiscal_reporting_snapshot_mutation() IS 'Rejects direct UPDATE and DELETE of committed fiscal-reporting snapshots and immutable children.';
