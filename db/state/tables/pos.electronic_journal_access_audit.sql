-- Privacy-safe append-only evidence for governed EJ reads, exports, and
-- integrity inspections. Payloads, raw filters, and hash source material are excluded.

CREATE TABLE IF NOT EXISTS pos.electronic_journal_access_audit (
    electronic_journal_access_audit_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    currency_code char(3) NOT NULL,
    action_code_id uuid NOT NULL,
    result_code_id uuid NOT NULL,
    actor_ref text NOT NULL,
    service_identity_ref text NOT NULL,
    correlation_ref text NOT NULL,
    support_reference text NOT NULL,
    event_count integer NOT NULL,
    occurred_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_electronic_journal_access_audit PRIMARY KEY (electronic_journal_access_audit_id),
    CONSTRAINT fk_ej_access_audit__site FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_ej_access_audit__identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_ej_access_audit__action FOREIGN KEY (action_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_ej_access_audit__result FOREIGN KEY (result_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_ej_access_audit__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_ej_access_audit__action_family CHECK (action_code_id IN (
        'a98278b3-63a8-5120-b5ca-08ba1443aab6',
        'b42d48ae-148e-5a1d-b0d0-35926c1aa1a9',
        '78fbabd3-7ede-55d4-970b-1bbb49b7ad69'
    )),
    CONSTRAINT ck_ej_access_audit__result_family CHECK (result_code_id IN (
        'a5959163-f9c7-5cf4-bc17-fa8ff3945265',
        '49a2352f-8110-5117-96d0-314158d25d90',
        '5c5b7d12-09d9-5b92-b850-4333ab104ea0'
    )),
    CONSTRAINT ck_ej_access_audit__refs CHECK (
        btrim(actor_ref) <> '' AND btrim(service_identity_ref) <> ''
        AND btrim(correlation_ref) <> '' AND btrim(support_reference) <> ''
    ),
    CONSTRAINT ck_ej_access_audit__count CHECK (event_count >= 0)
);

CREATE INDEX IF NOT EXISTS ix_ej_access_audit__scope_time
    ON pos.electronic_journal_access_audit (site_pos_server_id, fiscal_identity_id, occurred_at DESC);

CREATE OR REPLACE TRIGGER trg_electronic_journal_access_audit_immutable
BEFORE UPDATE OR DELETE ON pos.electronic_journal_access_audit
FOR EACH ROW EXECUTE FUNCTION pos.reject_electronic_journal_mutation();

COMMENT ON TABLE pos.electronic_journal_access_audit IS
    'Append-only privacy-safe access evidence for EJ read, export, and integrity operations; no event payload or raw filter is stored.';
