-- Idempotent audit identity for an authorized manual fiscal business-date open.

CREATE TABLE IF NOT EXISTS pos.fiscal_business_date_open_operations (
    fiscal_business_date_open_operation_id uuid PRIMARY KEY,
    site_pos_server_id uuid NOT NULL REFERENCES pos.site_pos_servers(site_pos_server_id),
    fiscal_identity_id uuid NOT NULL REFERENCES pos.fiscal_identities(fiscal_identity_id),
    currency_code char(3) NOT NULL,
    operation_key text NOT NULL,
    semantic_request_hash text NOT NULL,
    fiscal_reporting_period_id uuid NOT NULL REFERENCES pos.fiscal_reporting_periods(fiscal_reporting_period_id),
    requested_by_ref text NOT NULL,
    requested_by_display_name text NULL,
    service_identity_ref text NOT NULL,
    correlation_ref text NOT NULL,
    opened_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT uq_fiscal_business_date_open_operations__scope_key
        UNIQUE (site_pos_server_id, operation_key),
    CONSTRAINT ck_fiscal_business_date_open_operations__currency
        CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_business_date_open_operations__text
        CHECK (btrim(operation_key) <> '' AND btrim(semantic_request_hash) <> ''
            AND btrim(requested_by_ref) <> '' AND btrim(service_identity_ref) <> ''
            AND btrim(correlation_ref) <> ''
            AND (requested_by_display_name IS NULL OR btrim(requested_by_display_name) <> ''))
);

COMMENT ON TABLE pos.fiscal_business_date_open_operations IS
    'Immutable idempotency and audit evidence for authorized manual fiscal business-date opening.';
