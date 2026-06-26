-- POS Server Slice 7: Electronic Journal record posture.
-- This table stores EJ record metadata and hash references only; it does not
-- implement hash chaining, anchoring, or a full audit subsystem.

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
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_electronic_journal_records PRIMARY KEY (electronic_journal_record_id),
    CONSTRAINT fk_electronic_journal_records__site_pos_server
        FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_electronic_journal_records__fiscal_document
        FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_electronic_journal_records__report_request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_electronic_journal_records__record_type
        FOREIGN KEY (journal_record_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_electronic_journal_records__record_status
        FOREIGN KEY (journal_record_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_electronic_journal_records__sequence_ref
        CHECK (journal_sequence_ref IS NULL OR btrim(journal_sequence_ref) <> ''),
    CONSTRAINT ck_electronic_journal_records__hash_ref
        CHECK (journal_hash_ref IS NULL OR btrim(journal_hash_ref) <> ''),
    CONSTRAINT ck_electronic_journal_records__previous_hash_ref
        CHECK (previous_journal_hash_ref IS NULL OR btrim(previous_journal_hash_ref) <> ''),
    CONSTRAINT ck_electronic_journal_records__context_object
        CHECK (journal_context IS NULL OR jsonb_typeof(journal_context) = 'object')
);

COMMENT ON TABLE pos.electronic_journal_records IS
    'Electronic Journal record posture for fiscal traceability. Hash values are references only and do not implement anchoring or a full audit subsystem.';
COMMENT ON COLUMN pos.electronic_journal_records.electronic_journal_record_id IS
    'Internal identifier for the Electronic Journal record.';
COMMENT ON COLUMN pos.electronic_journal_records.site_pos_server_id IS
    'Site POS Server boundary for the Electronic Journal record.';
COMMENT ON COLUMN pos.electronic_journal_records.fiscal_document_id IS
    'Optional fiscal document reference for the journal record.';
COMMENT ON COLUMN pos.electronic_journal_records.fiscal_report_request_id IS
    'Optional fiscal report request reference for the journal record.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_record_type_code_id IS
    'Controlled code identifying the journal record type.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_record_status_code_id IS
    'Controlled code identifying the journal record status.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_sequence_ref IS
    'Journal sequence reference only; no sequence allocator is created here.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_hash_ref IS
    'Journal hash reference only; hash chaining or anchoring is not implemented here.';
COMMENT ON COLUMN pos.electronic_journal_records.previous_journal_hash_ref IS
    'Previous journal hash reference only; continuity implementation remains a later approved task.';
COMMENT ON COLUMN pos.electronic_journal_records.journal_context IS
    'Optional Electronic Journal metadata as a JSON object.';

