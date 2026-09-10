-- Canonical append-only Electronic Journal events.
-- Legacy posture rows remain identifiable and are never synthesized into the
-- canonical chain; all newly written fiscal events use is_canonical = true.

CREATE TABLE IF NOT EXISTS pos.electronic_journal_records (
    electronic_journal_record_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_document_id uuid NULL,
    fiscal_report_request_id uuid NULL,
    journal_record_type_code_id uuid NOT NULL,
    journal_record_status_code_id uuid NOT NULL,
    business_day_date date NULL,
    journal_sequence_ref text NULL,
    journal_hash_ref text NULL,
    previous_journal_hash_ref text NULL,
    recorded_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    journal_context jsonb NULL,
    printable_sales_invoice_text text NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_electronic_journal_records PRIMARY KEY (electronic_journal_record_id),
    CONSTRAINT fk_electronic_journal_records__site_pos_server
        FOREIGN KEY (site_pos_server_id) REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_electronic_journal_records__fiscal_document
        FOREIGN KEY (fiscal_document_id) REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_electronic_journal_records__report_request
        FOREIGN KEY (fiscal_report_request_id) REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_electronic_journal_records__record_type
        FOREIGN KEY (journal_record_type_code_id) REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_electronic_journal_records__record_status
        FOREIGN KEY (journal_record_status_code_id) REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_electronic_journal_records__sequence_ref
        CHECK (journal_sequence_ref IS NULL OR btrim(journal_sequence_ref) <> ''),
    CONSTRAINT ck_electronic_journal_records__hash_ref
        CHECK (journal_hash_ref IS NULL OR btrim(journal_hash_ref) <> ''),
    CONSTRAINT ck_electronic_journal_records__previous_hash_ref
        CHECK (previous_journal_hash_ref IS NULL OR btrim(previous_journal_hash_ref) <> ''),
    CONSTRAINT ck_electronic_journal_records__context_object
        CHECK (journal_context IS NULL OR jsonb_typeof(journal_context) = 'object')
);

ALTER TABLE pos.electronic_journal_records
    ADD COLUMN IF NOT EXISTS electronic_journal_stream_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_identity_id uuid NULL,
    ADD COLUMN IF NOT EXISTS currency_code char(3) NULL,
    ADD COLUMN IF NOT EXISTS fiscal_sequence_policy_id uuid NULL,
    ADD COLUMN IF NOT EXISTS fiscal_reporting_period_id uuid NULL,
    ADD COLUMN IF NOT EXISTS x_z_report_id uuid NULL,
    ADD COLUMN IF NOT EXISTS bir_sales_summary_report_id uuid NULL,
    ADD COLUMN IF NOT EXISTS reprint_request_id uuid NULL,
    ADD COLUMN IF NOT EXISTS event_reference text NULL,
    ADD COLUMN IF NOT EXISTS event_schema_version text NULL,
    ADD COLUMN IF NOT EXISTS stream_sequence_value bigint NULL,
    ADD COLUMN IF NOT EXISTS effective_at timestamptz NULL,
    ADD COLUMN IF NOT EXISTS actor_ref text NULL,
    ADD COLUMN IF NOT EXISTS service_identity_ref text NULL,
    ADD COLUMN IF NOT EXISTS correlation_ref text NULL,
    ADD COLUMN IF NOT EXISTS source_transition_ref text NULL,
    ADD COLUMN IF NOT EXISTS source_transition_version text NULL,
    ADD COLUMN IF NOT EXISTS idempotency_ref text NULL,
    ADD COLUMN IF NOT EXISTS semantic_hash_version text NULL,
    ADD COLUMN IF NOT EXISTS semantic_hash char(64) NULL,
    ADD COLUMN IF NOT EXISTS integrity_hash_version text NULL,
    ADD COLUMN IF NOT EXISTS integrity_hash char(64) NULL,
    ADD COLUMN IF NOT EXISTS previous_integrity_hash char(64) NULL,
    ADD COLUMN IF NOT EXISTS retention_policy_code_id uuid NULL,
    ADD COLUMN IF NOT EXISTS event_facts jsonb NULL,
    ADD COLUMN IF NOT EXISTS printable_sales_invoice_text text NULL,
    ADD COLUMN IF NOT EXISTS is_canonical boolean NOT NULL DEFAULT false;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__stream') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__stream
            FOREIGN KEY (electronic_journal_stream_id) REFERENCES pos.electronic_journal_streams (electronic_journal_stream_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__fiscal_identity') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__fiscal_identity
            FOREIGN KEY (fiscal_identity_id) REFERENCES pos.fiscal_identities (fiscal_identity_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__sequence_policy') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__sequence_policy
            FOREIGN KEY (fiscal_sequence_policy_id) REFERENCES pos.fiscal_sequence_policies (fiscal_sequence_policy_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__reporting_period') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__reporting_period
            FOREIGN KEY (fiscal_reporting_period_id) REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__xz_report') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__xz_report
            FOREIGN KEY (x_z_report_id) REFERENCES pos.x_z_reports (x_z_report_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__bir_summary') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__bir_summary
            FOREIGN KEY (bir_sales_summary_report_id) REFERENCES pos.bir_sales_summary_reports (bir_sales_summary_report_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__reprint') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__reprint
            FOREIGN KEY (reprint_request_id, fiscal_document_id)
            REFERENCES pos.reprint_requests (reprint_request_id, fiscal_document_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.electronic_journal_records'::regclass AND conname='fk_ej_records__retention') THEN
        ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT fk_ej_records__retention
            FOREIGN KEY (retention_policy_code_id) REFERENCES pos.controlled_codes (controlled_code_id);
    END IF;
END;
$$;

ALTER TABLE pos.electronic_journal_records
    DROP CONSTRAINT IF EXISTS ck_ej_records__canonical_required,
    DROP CONSTRAINT IF EXISTS ck_ej_records__event_type_family,
    DROP CONSTRAINT IF EXISTS ck_ej_records__status_family,
    DROP CONSTRAINT IF EXISTS ck_ej_records__retention_family,
    DROP CONSTRAINT IF EXISTS ck_ej_records__hashes,
    DROP CONSTRAINT IF EXISTS ck_ej_records__legacy_context,
    DROP CONSTRAINT IF EXISTS ck_ej_records__printable_sales_invoice,
    DROP CONSTRAINT IF EXISTS ck_ej_records__source_binding;

ALTER TABLE pos.electronic_journal_records
    ADD CONSTRAINT ck_ej_records__canonical_required CHECK (
        NOT is_canonical OR (
            electronic_journal_stream_id IS NOT NULL
            AND fiscal_identity_id IS NOT NULL
            AND currency_code ~ '^[A-Z]{3}$'
            AND fiscal_reporting_period_id IS NOT NULL
            AND event_reference IS NOT NULL AND btrim(event_reference) <> ''
            AND event_schema_version = 'pos-server-electronic-journal-event:v1'
            AND stream_sequence_value > 0
            AND effective_at IS NOT NULL
            AND actor_ref IS NOT NULL AND btrim(actor_ref) <> ''
            AND service_identity_ref IS NOT NULL AND btrim(service_identity_ref) <> ''
            AND correlation_ref IS NOT NULL AND btrim(correlation_ref) <> ''
            AND source_transition_ref IS NOT NULL AND btrim(source_transition_ref) <> ''
            AND source_transition_version IS NOT NULL AND btrim(source_transition_version) <> ''
            AND semantic_hash_version = 'pos-server-electronic-journal-event-semantic:sha256:v1'
            AND integrity_hash_version = 'pos-server-electronic-journal-integrity:sha256:v1'
            AND event_facts IS NOT NULL AND jsonb_typeof(event_facts) = 'object'
            AND retention_policy_code_id IS NOT NULL
            AND updated_at = created_at
        )
    ),
    ADD CONSTRAINT ck_ej_records__event_type_family CHECK (
        NOT is_canonical OR journal_record_type_code_id IN (
            '89dd11fb-87d8-51fe-a042-ef8b2e2e7829', '4e7f833a-449e-56e0-8feb-13dc820fd060',
            '77905f62-7d61-521d-bdc3-7c7d57b31efe', 'ea266ad1-9c7e-5291-b2a7-54eec88a0716',
            '92818433-ecd7-5948-8100-caec955b44e9', '6df85374-d609-58f1-8da5-ae8d6ed2ed35',
            'c428ea97-f423-5445-851e-1402909a6f9c', 'fc645aa9-f76b-5d46-8a67-374feef818cb',
            'ddff6062-b8e4-5f6f-af79-3c2ebc042bc0'
        )
    ),
    ADD CONSTRAINT ck_ej_records__status_family CHECK (
        NOT is_canonical OR journal_record_status_code_id = '07022b0a-3ef9-5f82-880a-d27a569f5e7b'
    ),
    ADD CONSTRAINT ck_ej_records__retention_family CHECK (
        NOT is_canonical OR retention_policy_code_id = 'bf711168-63d1-539e-9e8f-6de908978b3e'
    ),
    ADD CONSTRAINT ck_ej_records__hashes CHECK (
        NOT is_canonical OR (
            semantic_hash ~ '^[0-9a-f]{64}$'
            AND integrity_hash ~ '^[0-9a-f]{64}$'
            AND previous_integrity_hash ~ '^[0-9a-f]{64}$'
        )
    ),
    ADD CONSTRAINT ck_ej_records__legacy_context CHECK (
        NOT is_canonical OR journal_context IS NULL
    ),
    ADD CONSTRAINT ck_ej_records__printable_sales_invoice CHECK (
        printable_sales_invoice_text IS NULL OR (
            is_canonical
            AND journal_record_type_code_id = '89dd11fb-87d8-51fe-a042-ef8b2e2e7829'
            AND char_length(printable_sales_invoice_text) BETWEEN 1 AND 131072
        )
    ),
    ADD CONSTRAINT ck_ej_records__source_binding CHECK (
        NOT is_canonical OR CASE journal_record_type_code_id
            WHEN '89dd11fb-87d8-51fe-a042-ef8b2e2e7829' THEN fiscal_document_id IS NOT NULL
            WHEN '4e7f833a-449e-56e0-8feb-13dc820fd060' THEN fiscal_document_id IS NOT NULL
            WHEN '77905f62-7d61-521d-bdc3-7c7d57b31efe' THEN fiscal_document_id IS NOT NULL AND reprint_request_id IS NOT NULL
            WHEN 'ea266ad1-9c7e-5291-b2a7-54eec88a0716' THEN fiscal_document_id IS NOT NULL
            WHEN '92818433-ecd7-5948-8100-caec955b44e9' THEN fiscal_document_id IS NOT NULL
            WHEN '6df85374-d609-58f1-8da5-ae8d6ed2ed35' THEN fiscal_report_request_id IS NOT NULL AND x_z_report_id IS NOT NULL
            WHEN 'c428ea97-f423-5445-851e-1402909a6f9c' THEN fiscal_report_request_id IS NOT NULL AND x_z_report_id IS NOT NULL
            WHEN 'fc645aa9-f76b-5d46-8a67-374feef818cb' THEN fiscal_report_request_id IS NOT NULL AND bir_sales_summary_report_id IS NOT NULL
            WHEN 'ddff6062-b8e4-5f6f-af79-3c2ebc042bc0' THEN fiscal_report_request_id IS NOT NULL
            ELSE false
        END
    );

CREATE UNIQUE INDEX IF NOT EXISTS ux_ej_records__event_reference
    ON pos.electronic_journal_records (event_reference) WHERE is_canonical;
CREATE UNIQUE INDEX IF NOT EXISTS ux_ej_records__stream_sequence
    ON pos.electronic_journal_records (electronic_journal_stream_id, stream_sequence_value) WHERE is_canonical;
CREATE UNIQUE INDEX IF NOT EXISTS ux_ej_records__source_transition
    ON pos.electronic_journal_records (site_pos_server_id, fiscal_identity_id, currency_code, source_transition_ref, journal_record_type_code_id)
    WHERE is_canonical;
CREATE INDEX IF NOT EXISTS ix_ej_records__stream_recorded
    ON pos.electronic_journal_records (electronic_journal_stream_id, recorded_at, stream_sequence_value) WHERE is_canonical;
CREATE INDEX IF NOT EXISTS ix_ej_records__period_sequence
    ON pos.electronic_journal_records (fiscal_reporting_period_id, stream_sequence_value) WHERE is_canonical;
CREATE INDEX IF NOT EXISTS ix_ej_records__document_sequence
    ON pos.electronic_journal_records (fiscal_document_id, stream_sequence_value) WHERE is_canonical AND fiscal_document_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_ej_records__correlation
    ON pos.electronic_journal_records (correlation_ref, recorded_at) WHERE is_canonical;

CREATE OR REPLACE FUNCTION pos.reject_electronic_journal_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'electronic journal records are append-only'
        USING ERRCODE = 'check_violation';
END;
$$;

CREATE OR REPLACE TRIGGER trg_electronic_journal_records_immutable
BEFORE UPDATE OR DELETE ON pos.electronic_journal_records
FOR EACH ROW EXECUTE FUNCTION pos.reject_electronic_journal_mutation();

COMMENT ON TABLE pos.electronic_journal_records IS
    'Append-only canonical fiscal event records. Legacy pre-cutover posture rows are retained with is_canonical=false and are excluded from canonical reconstruction.';
COMMENT ON COLUMN pos.electronic_journal_records.electronic_journal_record_id IS
    'Internal immutable identifier for the Electronic Journal record.';
COMMENT ON COLUMN pos.electronic_journal_records.site_pos_server_id IS
    'Site POS Server boundary for the Electronic Journal record.';
COMMENT ON COLUMN pos.electronic_journal_records.fiscal_document_id IS
    'Optional authoritative fiscal-document source reference.';
COMMENT ON COLUMN pos.electronic_journal_records.reprint_request_id IS
    'Required governing canonical reprint record for fiscal_document_reprint_recorded events.';
COMMENT ON COLUMN pos.electronic_journal_records.fiscal_report_request_id IS
    'Optional authoritative fiscal-report request source reference.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_record_type_code_id IS
    'Governed Electronic Journal event-type controlled-code identity for canonical records.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_record_status_code_id IS
    'Governed Electronic Journal record-status controlled-code identity for canonical records.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_sequence_ref IS
    'Compatibility text representation of the canonical stream sequence for canonical records.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_hash_ref IS
    'Compatibility reference containing the canonical integrity hash for canonical records.';
COMMENT ON COLUMN pos.electronic_journal_records.previous_journal_hash_ref IS
    'Compatibility reference containing the prior canonical integrity hash for canonical records.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_context IS
    'Legacy posture metadata only; canonical records prohibit this ungoverned context field.';
COMMENT ON COLUMN pos.electronic_journal_records.printable_sales_invoice_text IS
    'Optional exact immutable printer-ready Sales Invoice text for canonical fiscal_document_committed events; historical absence remains null and is never reconstructed.';
COMMENT ON COLUMN pos.electronic_journal_records.event_facts IS
    'Versioned, privacy-minimized fiscal facts. Raw requests, credentials, personal evidence, diagnostics, and unrestricted text are prohibited.';
COMMENT ON COLUMN pos.electronic_journal_records.is_canonical IS
    'True only for events appended from authoritative transitions after the Z-011A cutover; no historical synthesis is performed.';
