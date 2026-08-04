-- Canonical governed reset counter, Z counter, and GTA state.
-- Generic pos.fiscal_counter_states remains legacy/posture-only and is not authoritative for Z close.

CREATE TABLE IF NOT EXISTS pos.fiscal_z_close_states (
    fiscal_z_close_state_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    currency_code char(3) NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    reset_counter_value bigint NOT NULL,
    z_counter_value bigint NOT NULL,
    grand_total_amount_minor_units bigint NOT NULL,
    state_version bigint NOT NULL,
    last_closed_reporting_period_id uuid NULL,
    last_committed_z_report_id uuid NULL,
    last_committed_z_report_kind_code_id uuid NULL,
    initialization_provenance_code_id uuid NOT NULL,
    initialized_at timestamptz NOT NULL,
    initialized_by_ref text NOT NULL,
    initialization_service_ref text NOT NULL,
    initialization_approval_ref text NOT NULL,
    last_transition_operation_ref text NOT NULL,
    last_transition_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_z_close_states PRIMARY KEY (fiscal_z_close_state_id),
    CONSTRAINT fk_fiscal_z_close_states__site FOREIGN KEY (site_pos_server_id)
        REFERENCES pos.site_pos_servers (site_pos_server_id),
    CONSTRAINT fk_fiscal_z_close_states__identity FOREIGN KEY (fiscal_identity_id)
        REFERENCES pos.fiscal_identities (fiscal_identity_id),
    CONSTRAINT fk_fiscal_z_close_states__contract FOREIGN KEY (fiscal_reporting_contract_version_id)
        REFERENCES pos.fiscal_reporting_contract_versions (fiscal_reporting_contract_version_id),
    CONSTRAINT fk_fiscal_z_close_states__provenance FOREIGN KEY (initialization_provenance_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_z_close_states__last_period_scope FOREIGN KEY (
        last_closed_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code
    ) REFERENCES pos.fiscal_reporting_periods (
        fiscal_reporting_period_id, site_pos_server_id, fiscal_identity_id, currency_code
    ),
    CONSTRAINT fk_fiscal_z_close_states__last_z_scope FOREIGN KEY (
        last_committed_z_report_id,
        last_committed_z_report_kind_code_id,
        last_closed_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ) REFERENCES pos.x_z_reports (
        x_z_report_id,
        report_kind_code_id,
        fiscal_reporting_period_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ),
    CONSTRAINT uq_fiscal_z_close_states__scope UNIQUE (
        site_pos_server_id, fiscal_identity_id, currency_code
    ),
    CONSTRAINT uq_fiscal_z_close_states__governing_scope UNIQUE (
        fiscal_z_close_state_id,
        fiscal_reporting_contract_version_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ),
    CONSTRAINT uq_fiscal_z_close_states__last_operation UNIQUE (last_transition_operation_ref),
    CONSTRAINT ck_fiscal_z_close_states__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_z_close_states__values CHECK (
        reset_counter_value >= 0
        AND z_counter_value >= 0
        AND grand_total_amount_minor_units >= 0
        AND state_version >= 1
    ),
    CONSTRAINT ck_fiscal_z_close_states__provenance_family CHECK (
        initialization_provenance_code_id IN (
            '8f31c890-2aa2-50ef-a815-0e0c8cf90983',
            '9ce30367-0ff0-5c5e-8d48-b4a3b3b9df71'
        )
    ),
    CONSTRAINT ck_fiscal_z_close_states__last_z_kind CHECK (
        last_committed_z_report_kind_code_id IS NULL
        OR last_committed_z_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
    ),
    CONSTRAINT ck_fiscal_z_close_states__continuity CHECK (
        (
            state_version = 1
            AND last_closed_reporting_period_id IS NULL
            AND last_committed_z_report_id IS NULL
            AND last_committed_z_report_kind_code_id IS NULL
        )
        OR
        (
            state_version > 1
            AND last_closed_reporting_period_id IS NOT NULL
            AND last_committed_z_report_id IS NOT NULL
            AND last_committed_z_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
        )
    ),
    CONSTRAINT ck_fiscal_z_close_states__refs CHECK (
        char_length(btrim(initialized_by_ref)) > 0
        AND char_length(btrim(initialization_service_ref)) > 0
        AND char_length(btrim(initialization_approval_ref)) > 0
        AND char_length(btrim(last_transition_operation_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_z_close_states__times CHECK (
        last_transition_at >= initialized_at
        AND updated_at >= created_at
    )
);

CREATE INDEX IF NOT EXISTS ix_fiscal_z_close_states__identity_currency
    ON pos.fiscal_z_close_states (fiscal_identity_id, currency_code);

CREATE INDEX IF NOT EXISTS ix_fiscal_z_close_states__last_period
    ON pos.fiscal_z_close_states (last_closed_reporting_period_id)
    WHERE last_closed_reporting_period_id IS NOT NULL;

COMMENT ON TABLE pos.fiscal_z_close_states IS 'Canonical versioned Z/reset/GTA state scoped by Site POS Server, fiscal identity, and currency. Direct manual mutation is prohibited.';
COMMENT ON COLUMN pos.fiscal_z_close_states.grand_total_amount_minor_units IS 'Approved cumulative VAT-inclusive final fiscal amount in integer minor units.';
COMMENT ON COLUMN pos.fiscal_z_close_states.state_version IS 'Optimistic version initialized at 1 and advanced once by each future committed Z transition.';
COMMENT ON COLUMN pos.fiscal_z_close_states.initialization_approval_ref IS 'Opaque approval reference; no credential, evidence, or customer data.';
