-- Immutable approved Annex E-1 Accounting facts and explicit known-zero attestations.

CREATE TABLE IF NOT EXISTS pos.annex_e1_period_accounting_facts (
    annex_e1_period_accounting_fact_id uuid NOT NULL,
    operation_key text NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    currency_code char(3) NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL,
    business_day_date date NOT NULL,
    fact_type_code_id uuid NOT NULL,
    fact_status_code_id uuid NOT NULL,
    amount_minor_units bigint NOT NULL,
    source_document_count bigint NOT NULL,
    first_source_reference text NULL,
    last_source_reference text NULL,
    source_event_reference text NULL,
    approval_reference text NOT NULL,
    semantic_hash_version text NOT NULL,
    semantic_hash char(64) NOT NULL,
    supersedes_fact_id uuid NULL,
    correction_reason_code_id uuid NULL,
    effective_at timestamptz NOT NULL,
    recorded_at timestamptz NOT NULL,
    recorded_by_ref text NOT NULL,
    service_identity_ref text NOT NULL,
    correlation_id text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_annex_e1_period_accounting_facts PRIMARY KEY (annex_e1_period_accounting_fact_id),
    CONSTRAINT fk_annex_e1_facts__period_scope FOREIGN KEY (fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code),
    CONSTRAINT fk_annex_e1_facts__fact_type FOREIGN KEY (fact_type_code_id) REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_annex_e1_facts__fact_status FOREIGN KEY (fact_status_code_id) REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_annex_e1_facts__supersedes FOREIGN KEY (supersedes_fact_id) REFERENCES pos.annex_e1_period_accounting_facts (annex_e1_period_accounting_fact_id),
    CONSTRAINT fk_annex_e1_facts__correction_reason FOREIGN KEY (correction_reason_code_id) REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_annex_e1_facts__scope_operation UNIQUE (site_pos_server_id, operation_key),
    CONSTRAINT uq_annex_e1_facts__supersedes UNIQUE (supersedes_fact_id),
    CONSTRAINT ck_annex_e1_facts__type_family CHECK (fact_type_code_id IN ('deb73a5f-0233-596d-9e59-31758498d2d1','63a0b090-33f8-5ce3-97f2-174f6d0663be','b66e7744-09cd-5b94-863b-25e1e147153e','857c5f01-52bb-5fa6-8f97-15f070b21c03','9761c15f-3ab8-5b5a-9c93-1aff0b85de17','5184757b-c19d-5927-939c-234e0cc61f3c','4e6c15fa-c38b-53c5-a97b-eaf4ff5be951')),
    CONSTRAINT ck_annex_e1_facts__status_family CHECK (fact_status_code_id IN ('713f2db7-1059-53c6-9b2a-a7ca2013dd14','29a6815a-6cfe-538f-a616-243604c6d227')),
    CONSTRAINT ck_annex_e1_facts__correction_family CHECK (correction_reason_code_id IS NULL OR correction_reason_code_id IN ('bca6e858-3c1a-5641-afbb-35cfba91caa2','3324411d-f392-501c-8b97-924fd19e5f7c')),
    CONSTRAINT ck_annex_e1_facts__values CHECK (amount_minor_units >= 0 AND source_document_count >= 0),
    CONSTRAINT ck_annex_e1_facts__currency CHECK (currency_code = 'PHP'),
    CONSTRAINT ck_annex_e1_facts__semantic CHECK (semantic_hash_version = 'pos-server-annex-e1-period-fact:sha256:v1' AND semantic_hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT ck_annex_e1_facts__refs CHECK (char_length(btrim(operation_key)) > 0 AND char_length(btrim(approval_reference)) > 0 AND char_length(btrim(recorded_by_ref)) > 0 AND char_length(btrim(service_identity_ref)) > 0 AND char_length(btrim(correlation_id)) > 0),
    CONSTRAINT ck_annex_e1_facts__correction CHECK ((supersedes_fact_id IS NULL) = (correction_reason_code_id IS NULL)),
    CONSTRAINT ck_annex_e1_facts__known_zero CHECK (fact_status_code_id <> '29a6815a-6cfe-538f-a616-243604c6d227' OR (amount_minor_units = 0 AND source_document_count = 0 AND first_source_reference IS NULL AND last_source_reference IS NULL AND source_event_reference IS NULL)),
    CONSTRAINT ck_annex_e1_facts__recorded_source CHECK (fact_status_code_id <> '713f2db7-1059-53c6-9b2a-a7ca2013dd14' OR (fact_type_code_id IN ('deb73a5f-0233-596d-9e59-31758498d2d1','63a0b090-33f8-5ce3-97f2-174f6d0663be') AND amount_minor_units > 0 AND source_document_count > 0 AND first_source_reference IS NOT NULL AND last_source_reference IS NOT NULL)),
    CONSTRAINT ck_annex_e1_facts__overflow_event CHECK (fact_status_code_id <> '713f2db7-1059-53c6-9b2a-a7ca2013dd14' OR fact_type_code_id <> '63a0b090-33f8-5ce3-97f2-174f6d0663be' OR source_event_reference IS NOT NULL),
    CONSTRAINT ck_annex_e1_facts__timestamps CHECK (recorded_at >= effective_at)
);

CREATE INDEX IF NOT EXISTS ix_annex_e1_facts__period_type ON pos.annex_e1_period_accounting_facts (fiscal_reporting_period_id, fact_type_code_id, recorded_at DESC);

ALTER TABLE pos.annex_e1_period_accounting_facts
    DROP CONSTRAINT IF EXISTS ck_annex_e1_facts__recorded_source,
    DROP CONSTRAINT IF EXISTS ck_annex_e1_facts__overflow_event,
    ADD CONSTRAINT ck_annex_e1_facts__recorded_source CHECK (fact_status_code_id <> '713f2db7-1059-53c6-9b2a-a7ca2013dd14' OR (fact_type_code_id IN ('deb73a5f-0233-596d-9e59-31758498d2d1','63a0b090-33f8-5ce3-97f2-174f6d0663be') AND amount_minor_units > 0 AND source_document_count > 0 AND first_source_reference IS NOT NULL AND last_source_reference IS NOT NULL)),
    ADD CONSTRAINT ck_annex_e1_facts__overflow_event CHECK (fact_status_code_id <> '713f2db7-1059-53c6-9b2a-a7ca2013dd14' OR fact_type_code_id <> '63a0b090-33f8-5ce3-97f2-174f6d0663be' OR source_event_reference IS NOT NULL);

CREATE OR REPLACE FUNCTION pos.validate_annex_e1_accounting_fact_append()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE prior pos.annex_e1_period_accounting_facts%ROWTYPE;
BEGIN
    IF NEW.supersedes_fact_id IS NULL THEN
        IF EXISTS (SELECT 1 FROM pos.annex_e1_period_accounting_facts f WHERE f.fiscal_reporting_period_id=NEW.fiscal_reporting_period_id AND f.fact_type_code_id=NEW.fact_type_code_id AND NOT EXISTS (SELECT 1 FROM pos.annex_e1_period_accounting_facts child WHERE child.supersedes_fact_id=f.annex_e1_period_accounting_fact_id)) THEN
            RAISE EXCEPTION 'annex e1 accounting fact already has an active immutable value';
        END IF;
    ELSE
        SELECT * INTO prior FROM pos.annex_e1_period_accounting_facts WHERE annex_e1_period_accounting_fact_id=NEW.supersedes_fact_id FOR SHARE;
        IF NOT FOUND OR prior.site_pos_server_id<>NEW.site_pos_server_id OR prior.fiscal_identity_id<>NEW.fiscal_identity_id OR prior.currency_code<>NEW.currency_code OR prior.fiscal_reporting_period_id<>NEW.fiscal_reporting_period_id OR prior.fact_type_code_id<>NEW.fact_type_code_id THEN
            RAISE EXCEPTION 'annex e1 accounting fact correction scope mismatch';
        END IF;
    END IF;
    RETURN NEW;
END; $$;

CREATE OR REPLACE TRIGGER trg_annex_e1_facts_validate BEFORE INSERT ON pos.annex_e1_period_accounting_facts FOR EACH ROW EXECUTE FUNCTION pos.validate_annex_e1_accounting_fact_append();
CREATE OR REPLACE TRIGGER trg_annex_e1_facts_immutable BEFORE UPDATE OR DELETE ON pos.annex_e1_period_accounting_facts FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();
