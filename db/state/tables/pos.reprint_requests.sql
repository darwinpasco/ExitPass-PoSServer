-- Canonical fiscal-document reprint records.
-- Legacy posture rows remain non-canonical and are not synthesized into the
-- Electronic Journal. Canonical rows are immutable fiscal presentation facts.

CREATE TABLE IF NOT EXISTS pos.reprint_requests (
    reprint_request_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    reprint_type_code_id uuid NOT NULL,
    reprint_status_code_id uuid NOT NULL,
    reprint_reason_code_id uuid NULL,
    reprint_reason_text text NULL,
    requested_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    requested_by_ref text NULL,
    approval_ref text NULL,
    actor_ref text NULL,
    service_identity_ref text NULL,
    reprint_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_reprint_requests PRIMARY KEY (reprint_request_id),
    CONSTRAINT fk_reprint_requests__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_reprint_requests__type_code FOREIGN KEY (reprint_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_reprint_requests__status_code FOREIGN KEY (reprint_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_reprint_requests__reason_code FOREIGN KEY (reprint_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_reprint_requests__reason_text CHECK (
        reprint_reason_text IS NULL OR char_length(btrim(reprint_reason_text)) > 0
    ),
    CONSTRAINT ck_reprint_requests__requested_by_ref CHECK (
        requested_by_ref IS NULL OR char_length(btrim(requested_by_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__approval_ref CHECK (
        approval_ref IS NULL OR char_length(btrim(approval_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    ),
    CONSTRAINT ck_reprint_requests__context_object CHECK (
        reprint_context IS NULL OR jsonb_typeof(reprint_context) = 'object'
    )
);

ALTER TABLE pos.reprint_requests
    ADD COLUMN IF NOT EXISTS reprint_reference text NULL,
    ADD COLUMN IF NOT EXISTS site_pos_server_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_identity_id uuid NULL,
    ADD COLUMN IF NOT EXISTS currency_code char(3) NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_sequence_policy_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_document_number_snapshot text NULL,
    ADD COLUMN IF NOT EXISTS fiscal_sequence_value_snapshot bigint NULL,
    ADD COLUMN IF NOT EXISTS copy_sequence bigint NULL,
    ADD COLUMN IF NOT EXISTS operation_idempotency_key text NULL,
    ADD COLUMN IF NOT EXISTS semantic_hash_version text NULL,
    ADD COLUMN IF NOT EXISTS semantic_request_hash char(64) NULL,
    ADD COLUMN IF NOT EXISTS source_transition_version text NULL,
    ADD COLUMN IF NOT EXISTS correlation_ref text NULL,
    ADD COLUMN IF NOT EXISTS committed_at timestamptz NULL,
    ADD COLUMN IF NOT EXISTS is_canonical boolean NOT NULL DEFAULT false;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.reprint_requests'::regclass AND conname='fk_reprint_requests__site') THEN
        ALTER TABLE pos.reprint_requests ADD CONSTRAINT fk_reprint_requests__site
            FOREIGN KEY (site_pos_server_id) REFERENCES pos.site_pos_servers (site_pos_server_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.reprint_requests'::regclass AND conname='fk_reprint_requests__identity') THEN
        ALTER TABLE pos.reprint_requests ADD CONSTRAINT fk_reprint_requests__identity
            FOREIGN KEY (fiscal_identity_id) REFERENCES pos.fiscal_identities (fiscal_identity_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.reprint_requests'::regclass AND conname='fk_reprint_requests__period') THEN
        ALTER TABLE pos.reprint_requests ADD CONSTRAINT fk_reprint_requests__period
            FOREIGN KEY (fiscal_reporting_period_id) REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.reprint_requests'::regclass AND conname='fk_reprint_requests__sequence_policy') THEN
        ALTER TABLE pos.reprint_requests ADD CONSTRAINT fk_reprint_requests__sequence_policy
            FOREIGN KEY (fiscal_sequence_policy_id) REFERENCES pos.fiscal_sequence_policies (fiscal_sequence_policy_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.reprint_requests'::regclass AND conname='uq_reprint_requests__identity_document') THEN
        ALTER TABLE pos.reprint_requests ADD CONSTRAINT uq_reprint_requests__identity_document
            UNIQUE (reprint_request_id, fiscal_document_id);
    END IF;
END;
$$;

ALTER TABLE pos.reprint_requests
    DROP CONSTRAINT IF EXISTS ck_reprint_requests__canonical_required,
    DROP CONSTRAINT IF EXISTS ck_reprint_requests__type_family,
    DROP CONSTRAINT IF EXISTS ck_reprint_requests__status_family,
    DROP CONSTRAINT IF EXISTS ck_reprint_requests__reason_family,
    DROP CONSTRAINT IF EXISTS ck_reprint_requests__canonical_privacy;

ALTER TABLE pos.reprint_requests
    ADD CONSTRAINT ck_reprint_requests__canonical_required CHECK (
        NOT is_canonical OR (
            reprint_reference IS NOT NULL AND btrim(reprint_reference) <> ''
            AND site_pos_server_id IS NOT NULL
            AND fiscal_identity_id IS NOT NULL
            AND currency_code ~ '^[A-Z]{3}$'
            AND fiscal_reporting_period_id IS NOT NULL
            AND fiscal_sequence_policy_id IS NOT NULL
            AND fiscal_document_number_snapshot IS NOT NULL AND btrim(fiscal_document_number_snapshot) <> ''
            AND fiscal_sequence_value_snapshot > 0
            AND copy_sequence > 0
            AND operation_idempotency_key IS NOT NULL AND btrim(operation_idempotency_key) <> ''
            AND semantic_hash_version = 'pos-server-fiscal-document-reprint:sha256:v1'
            AND semantic_request_hash ~ '^[0-9a-f]{64}$'
            AND source_transition_version = 'pos-server-fiscal-document-reprint:v1'
            AND correlation_ref IS NOT NULL AND btrim(correlation_ref) <> ''
            AND requested_by_ref IS NOT NULL AND btrim(requested_by_ref) <> ''
            AND actor_ref IS NOT NULL AND btrim(actor_ref) <> ''
            AND service_identity_ref IS NOT NULL AND btrim(service_identity_ref) <> ''
            AND committed_at IS NOT NULL AND committed_at >= requested_at
            AND updated_at = created_at
        )
    ),
    ADD CONSTRAINT ck_reprint_requests__type_family CHECK (
        NOT is_canonical OR reprint_type_code_id = '4322e9c1-2209-5ad4-8587-4bbe4f35a08b'
    ),
    ADD CONSTRAINT ck_reprint_requests__status_family CHECK (
        NOT is_canonical OR reprint_status_code_id = '5dacbba2-6cc7-593f-867b-d7010955d3e1'
    ),
    ADD CONSTRAINT ck_reprint_requests__reason_family CHECK (
        NOT is_canonical OR reprint_reason_code_id IN (
            '5b8a58c8-636f-51fc-8512-85e3d932e8c8',
            '215ad81a-913b-53e2-a009-b5bf1de021a5',
            'c9ab8e23-0f6f-5bbb-a2f5-cc8c5105606e',
            'dbaf50f2-0001-5820-b6f1-ff5803f7d839'
        )
    ),
    ADD CONSTRAINT ck_reprint_requests__canonical_privacy CHECK (
        NOT is_canonical OR (reprint_reason_text IS NULL AND approval_ref IS NULL AND reprint_context IS NULL)
    );

CREATE UNIQUE INDEX IF NOT EXISTS ux_reprint_requests__reference
    ON pos.reprint_requests (reprint_reference) WHERE is_canonical;
CREATE UNIQUE INDEX IF NOT EXISTS ux_reprint_requests__scope_operation
    ON pos.reprint_requests (site_pos_server_id, operation_idempotency_key) WHERE is_canonical;
CREATE UNIQUE INDEX IF NOT EXISTS ux_reprint_requests__document_copy
    ON pos.reprint_requests (fiscal_document_id, copy_sequence) WHERE is_canonical;
CREATE INDEX IF NOT EXISTS ix_reprint_requests__document_committed
    ON pos.reprint_requests (fiscal_document_id, committed_at, copy_sequence) WHERE is_canonical;

CREATE OR REPLACE FUNCTION pos.reject_canonical_reprint_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.is_canonical THEN
        RAISE EXCEPTION 'canonical fiscal reprint records are immutable'
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE TRIGGER trg_reprint_requests_immutable
BEFORE UPDATE OR DELETE ON pos.reprint_requests
FOR EACH ROW EXECUTE FUNCTION pos.reject_canonical_reprint_mutation();

CREATE OR REPLACE FUNCTION pos.require_canonical_reprint_journal_event()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.is_canonical AND NOT EXISTS (
        SELECT 1
        FROM pos.electronic_journal_records event
        WHERE event.is_canonical
          AND event.reprint_request_id = NEW.reprint_request_id
          AND event.fiscal_document_id = NEW.fiscal_document_id
          AND event.source_transition_ref = NEW.reprint_reference
          AND event.journal_record_type_code_id = '77905f62-7d61-521d-bdc3-7c7d57b31efe'
    ) THEN
        RAISE EXCEPTION 'canonical fiscal reprint requires its Electronic Journal event'
            USING ERRCODE = 'foreign_key_violation';
    END IF;
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_reprint_requests_require_journal ON pos.reprint_requests;
CREATE CONSTRAINT TRIGGER trg_reprint_requests_require_journal
AFTER INSERT OR UPDATE ON pos.reprint_requests
DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION pos.require_canonical_reprint_journal_event();

COMMENT ON TABLE pos.reprint_requests IS
    'Canonical immutable fiscal-document reprint records. Legacy posture rows remain non-canonical and are excluded from governed reconstruction.';
COMMENT ON COLUMN pos.reprint_requests.copy_sequence IS
    'One-based copy sequence serialized per authoritative fiscal document.';
COMMENT ON COLUMN pos.reprint_requests.approval_ref IS
    'Optional approval reference only; does not create external approval authority.';
COMMENT ON COLUMN pos.reprint_requests.reprint_context IS
    'Legacy posture metadata only; canonical reprint records prohibit ungoverned context and reason text.';
