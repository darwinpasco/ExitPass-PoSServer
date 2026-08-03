-- POS Server fiscal report operation identity and lifecycle.
-- Requests do not calculate reports or close fiscal periods.

CREATE TABLE IF NOT EXISTS pos.fiscal_report_requests (
    fiscal_report_request_id uuid NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    requested_by_channel_terminal_id uuid NULL,
    report_type_code_id uuid NOT NULL,
    report_status_code_id uuid NOT NULL,
    operation_idempotency_key text NOT NULL,
    semantic_request_hash char(64) NOT NULL,
    semantic_hash_version text NOT NULL,
    business_day_date date NOT NULL,
    requested_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    requested_by_ref text NOT NULL,
    service_identity_ref text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_report_requests PRIMARY KEY (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_report_requests__contract FOREIGN KEY (fiscal_reporting_contract_version_id)
        REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    CONSTRAINT fk_fiscal_report_requests__period FOREIGN KEY (fiscal_reporting_period_id)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT fk_fiscal_report_requests__site_pos_server FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_report_requests__channel FOREIGN KEY (requested_by_channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT fk_fiscal_report_requests__report_type FOREIGN KEY (report_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_report_requests__report_status FOREIGN KEY (report_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_report_requests__site_operation UNIQUE (site_pos_server_id, operation_idempotency_key),
    CONSTRAINT uq_fiscal_report_requests__id_kind UNIQUE (fiscal_report_request_id, report_type_code_id),
    CONSTRAINT ck_fiscal_report_requests__kind_family CHECK (report_type_code_id IN (
        '5dc3cc94-b3ab-5582-a598-e779871fc3e2',
        '1c628bc2-49c3-53e8-ae83-2082bcf28467',
        '2326447f-74c2-5ed7-83bb-e079fcee7f3d',
        'd4c4615e-2cf2-59b7-a6d2-97d22210114c'
    )),
    CONSTRAINT ck_fiscal_report_requests__status_family CHECK (report_status_code_id IN (
        '3ffc8385-4f56-5ae8-a2f6-b0c1bddafa76',
        '7440d7ed-a192-5613-bb65-019058a80256',
        'd5273f98-440f-5a89-aab0-cb34f74d6b80',
        '27c4af8e-00e8-50ec-a3f0-53efb2def09a',
        '48f0ed5e-0fe1-5fa5-bfec-586bcf5b1b55',
        '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04',
        'acce1c93-a76e-5142-9b42-4314616373c4',
        '523aa5d7-85bc-526d-b36c-22a0075d50db',
        'f89b81d9-14eb-5509-94fe-84caf60eb21f'
    )),
    CONSTRAINT ck_fiscal_report_requests__operation_key CHECK (char_length(btrim(operation_idempotency_key)) > 0),
    CONSTRAINT ck_fiscal_report_requests__semantic_hash CHECK (semantic_request_hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT ck_fiscal_report_requests__semantic_version CHECK (
        semantic_hash_version = 'pos-server-fiscal-report-request:sha256:v1'
    ),
    CONSTRAINT ck_fiscal_report_requests__actor_refs CHECK (
        char_length(btrim(requested_by_ref)) > 0 AND char_length(btrim(service_identity_ref)) > 0
    )
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'pos' AND table_name = 'fiscal_report_requests' AND column_name = 'request_context'
    ) AND EXISTS (SELECT 1 FROM pos.fiscal_report_requests) THEN
        RAISE EXCEPTION 'cannot harden non-empty legacy pos.fiscal_report_requests; archive and migrate governed request facts first';
    END IF;
END;
$$;

ALTER TABLE pos.fiscal_report_requests
    ADD COLUMN IF NOT EXISTS fiscal_reporting_contract_version_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS operation_idempotency_key text NULL,
    ADD COLUMN IF NOT EXISTS semantic_request_hash char(64) NULL,
    ADD COLUMN IF NOT EXISTS semantic_hash_version text NULL;

ALTER TABLE pos.fiscal_report_requests DROP COLUMN IF EXISTS request_context;

ALTER TABLE pos.fiscal_report_requests
    ALTER COLUMN fiscal_reporting_contract_version_id SET NOT NULL,
    ALTER COLUMN fiscal_reporting_period_id SET NOT NULL,
    ALTER COLUMN operation_idempotency_key SET NOT NULL,
    ALTER COLUMN semantic_request_hash SET NOT NULL,
    ALTER COLUMN semantic_hash_version SET NOT NULL,
    ALTER COLUMN business_day_date SET NOT NULL,
    ALTER COLUMN requested_by_ref SET NOT NULL,
    ALTER COLUMN service_identity_ref SET NOT NULL;

ALTER TABLE pos.fiscal_report_requests
    DROP CONSTRAINT IF EXISTS fk_fiscal_report_requests__contract,
    DROP CONSTRAINT IF EXISTS fk_fiscal_report_requests__period,
    DROP CONSTRAINT IF EXISTS uq_fiscal_report_requests__site_operation,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__kind_family,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__status_family,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__operation_key,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__semantic_hash,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__semantic_version,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__actor_refs,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__requested_by_ref,
    DROP CONSTRAINT IF EXISTS ck_fiscal_report_requests__service_ref;

ALTER TABLE pos.fiscal_report_requests
    ADD CONSTRAINT fk_fiscal_report_requests__contract FOREIGN KEY (fiscal_reporting_contract_version_id) REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    ADD CONSTRAINT fk_fiscal_report_requests__period FOREIGN KEY (fiscal_reporting_period_id) REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    ADD CONSTRAINT uq_fiscal_report_requests__site_operation UNIQUE (site_pos_server_id, operation_idempotency_key),
    ADD CONSTRAINT ck_fiscal_report_requests__kind_family CHECK (report_type_code_id IN ('5dc3cc94-b3ab-5582-a598-e779871fc3e2','1c628bc2-49c3-53e8-ae83-2082bcf28467','2326447f-74c2-5ed7-83bb-e079fcee7f3d','d4c4615e-2cf2-59b7-a6d2-97d22210114c')),
    ADD CONSTRAINT ck_fiscal_report_requests__status_family CHECK (report_status_code_id IN ('3ffc8385-4f56-5ae8-a2f6-b0c1bddafa76','7440d7ed-a192-5613-bb65-019058a80256','d5273f98-440f-5a89-aab0-cb34f74d6b80','27c4af8e-00e8-50ec-a3f0-53efb2def09a','48f0ed5e-0fe1-5fa5-bfec-586bcf5b1b55','84ef4d12-b3a1-5385-88b1-3a3eadeb8a04','acce1c93-a76e-5142-9b42-4314616373c4','523aa5d7-85bc-526d-b36c-22a0075d50db','f89b81d9-14eb-5509-94fe-84caf60eb21f')),
    ADD CONSTRAINT ck_fiscal_report_requests__operation_key CHECK (char_length(btrim(operation_idempotency_key)) > 0),
    ADD CONSTRAINT ck_fiscal_report_requests__semantic_hash CHECK (semantic_request_hash ~ '^[0-9a-f]{64}$'),
    ADD CONSTRAINT ck_fiscal_report_requests__semantic_version CHECK (semantic_hash_version = 'pos-server-fiscal-report-request:sha256:v1'),
    ADD CONSTRAINT ck_fiscal_report_requests__actor_refs CHECK (char_length(btrim(requested_by_ref)) > 0 AND char_length(btrim(service_identity_ref)) > 0);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'pos.fiscal_report_requests'::regclass
          AND conname = 'uq_fiscal_report_requests__id_kind'
    ) THEN
        ALTER TABLE pos.fiscal_report_requests
            ADD CONSTRAINT uq_fiscal_report_requests__id_kind UNIQUE (fiscal_report_request_id, report_type_code_id);
    END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS ix_fiscal_report_requests__period_kind_status
    ON pos.fiscal_report_requests (fiscal_reporting_period_id, report_type_code_id, report_status_code_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_report_requests__site_business_date
    ON pos.fiscal_report_requests (site_pos_server_id, business_day_date DESC, requested_at DESC);

COMMENT ON TABLE pos.fiscal_report_requests IS 'Report operation identity and mutable lifecycle posture. It does not generate a report or close a period.';
COMMENT ON COLUMN pos.fiscal_report_requests.operation_idempotency_key IS 'Caller operation identity scoped to Site POS Server. Exact semantic replay returns the original future runtime outcome.';
COMMENT ON COLUMN pos.fiscal_report_requests.semantic_request_hash IS 'Lowercase SHA-256 digest of governed report request semantics. The canonical source is never stored.';
COMMENT ON COLUMN pos.fiscal_report_requests.semantic_hash_version IS 'Independent report request semantic version; it does not alter fiscal-document sha256:v1 or statutory sha256:v2.';
