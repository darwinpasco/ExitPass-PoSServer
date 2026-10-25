CREATE TABLE IF NOT EXISTS pos.fiscal_document_statutory_beneficiary_snapshots (
    fiscal_document_statutory_beneficiary_snapshot_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    statutory_discount_decision_command_id uuid NOT NULL,
    statutory_id_type varchar(64) NOT NULL,
    protected_identity_envelope bytea NOT NULL,
    protection_key_reference varchar(128) NOT NULL,
    protection_key_version varchar(64) NOT NULL,
    envelope_format_version smallint NOT NULL,
    capture_source varchar(32) NOT NULL,
    correlation_reference varchar(200) NOT NULL,
    recorded_at timestamptz NOT NULL,
    CONSTRAINT pk_fiscal_document_statutory_beneficiary_snapshots PRIMARY KEY (fiscal_document_statutory_beneficiary_snapshot_id),
    CONSTRAINT ux_fiscal_document_statutory_beneficiary_snapshots__document UNIQUE (fiscal_document_id),
    CONSTRAINT fk_fiscal_document_statutory_beneficiary_snapshots__document
        FOREIGN KEY (fiscal_document_id) REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT ck_fiscal_doc_stat_beneficiary__capture_source
        CHECK (capture_source IN ('OPERATOR_CONSOLE', 'MANAGEMENT_PLATFORM')),
    CONSTRAINT ck_fiscal_document_statutory_beneficiary_snapshots__envelope
        CHECK (octet_length(protected_identity_envelope) >= 31 AND envelope_format_version > 0)
);

CREATE OR REPLACE FUNCTION pos.reject_statutory_beneficiary_snapshot_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'fiscal_document_statutory_beneficiary_snapshots is append-only';
END;
$$;

DROP TRIGGER IF EXISTS trg_fiscal_doc_stat_beneficiary_immutable
    ON pos.fiscal_document_statutory_beneficiary_snapshots;
CREATE TRIGGER trg_fiscal_doc_stat_beneficiary_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_document_statutory_beneficiary_snapshots
FOR EACH ROW EXECUTE FUNCTION pos.reject_statutory_beneficiary_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_document_statutory_beneficiary_snapshots IS
    'Append-only encrypted fiscal beneficiary identity for local-ordinance 5%/20% Annex E-2/E-3 transactions.';
