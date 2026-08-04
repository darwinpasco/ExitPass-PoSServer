-- Immutable operation and continuity evidence for canonical Z close state.

CREATE TABLE IF NOT EXISTS pos.fiscal_z_close_state_transitions (
    fiscal_z_close_state_transition_id uuid NOT NULL,
    fiscal_z_close_state_id uuid NOT NULL,
    fiscal_reporting_contract_version_id uuid NOT NULL,
    site_pos_server_id uuid NOT NULL,
    fiscal_identity_id uuid NOT NULL,
    currency_code char(3) NOT NULL,
    transition_type_code_id uuid NOT NULL,
    initialization_provenance_code_id uuid NULL,
    operation_ref text NOT NULL,
    semantic_request_hash char(64) NOT NULL,
    semantic_hash_version text NOT NULL,
    expected_state_version bigint NOT NULL,
    resulting_state_version bigint NOT NULL,
    previous_reporting_period_id uuid NULL,
    resulting_reporting_period_id uuid NULL,
    previous_z_report_id uuid NULL,
    previous_z_report_kind_code_id uuid NULL,
    resulting_z_report_id uuid NULL,
    resulting_z_report_kind_code_id uuid NULL,
    approval_ref text NOT NULL,
    actor_ref text NOT NULL,
    service_identity_ref text NOT NULL,
    correlation_ref text NOT NULL,
    committed_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_z_close_state_transitions PRIMARY KEY (fiscal_z_close_state_transition_id),
    CONSTRAINT fk_fiscal_z_close_state_transitions__state_scope FOREIGN KEY (
        fiscal_z_close_state_id,
        fiscal_reporting_contract_version_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ) REFERENCES pos.fiscal_z_close_states (
        fiscal_z_close_state_id,
        fiscal_reporting_contract_version_id,
        site_pos_server_id,
        fiscal_identity_id,
        currency_code
    ),
    CONSTRAINT fk_fiscal_z_close_state_transitions__type FOREIGN KEY (transition_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_z_close_state_transitions__provenance FOREIGN KEY (initialization_provenance_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_z_close_state_transitions__previous_period FOREIGN KEY (previous_reporting_period_id)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT fk_fiscal_z_close_state_transitions__resulting_period FOREIGN KEY (resulting_reporting_period_id)
        REFERENCES pos.fiscal_reporting_periods (fiscal_reporting_period_id),
    CONSTRAINT fk_fiscal_z_close_state_transitions__previous_z FOREIGN KEY (
        previous_z_report_id, previous_z_report_kind_code_id
    ) REFERENCES pos.x_z_reports (x_z_report_id, report_kind_code_id),
    CONSTRAINT fk_fiscal_z_close_state_transitions__resulting_z FOREIGN KEY (
        resulting_z_report_id, resulting_z_report_kind_code_id
    ) REFERENCES pos.x_z_reports (x_z_report_id, report_kind_code_id),
    CONSTRAINT uq_fiscal_z_close_state_transitions__operation UNIQUE (operation_ref),
    CONSTRAINT uq_fiscal_z_close_state_transitions__state_version UNIQUE (
        fiscal_z_close_state_id, resulting_state_version
    ),
    CONSTRAINT ck_fiscal_z_close_state_transitions__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_z_close_state_transitions__type_family CHECK (
        transition_type_code_id IN (
            '21880ca5-b803-5abc-959f-def2f232ad73',
            '52a28fc9-7c24-5810-8f98-30dbc7c134b9'
        )
    ),
    CONSTRAINT ck_fiscal_z_close_state_transitions__provenance_family CHECK (
        initialization_provenance_code_id IS NULL
        OR initialization_provenance_code_id IN (
            '8f31c890-2aa2-50ef-a815-0e0c8cf90983',
            '9ce30367-0ff0-5c5e-8d48-b4a3b3b9df71'
        )
    ),
    CONSTRAINT ck_fiscal_z_close_state_transitions__version CHECK (
        expected_state_version >= 0
        AND resulting_state_version = expected_state_version + 1
    ),
    CONSTRAINT ck_fiscal_z_close_state_transitions__posture CHECK (
        (
            transition_type_code_id = '21880ca5-b803-5abc-959f-def2f232ad73'
            AND initialization_provenance_code_id IS NOT NULL
            AND expected_state_version = 0
            AND resulting_state_version = 1
            AND previous_reporting_period_id IS NULL
            AND resulting_reporting_period_id IS NULL
            AND previous_z_report_id IS NULL
            AND previous_z_report_kind_code_id IS NULL
            AND resulting_z_report_id IS NULL
            AND resulting_z_report_kind_code_id IS NULL
        )
        OR
        (
            transition_type_code_id = '52a28fc9-7c24-5810-8f98-30dbc7c134b9'
            AND initialization_provenance_code_id IS NULL
            AND expected_state_version >= 1
            AND resulting_reporting_period_id IS NOT NULL
            AND resulting_z_report_id IS NOT NULL
            AND resulting_z_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
        )
    ),
    CONSTRAINT ck_fiscal_z_close_state_transitions__z_kind CHECK (
        (previous_z_report_kind_code_id IS NULL OR previous_z_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467')
        AND (resulting_z_report_kind_code_id IS NULL OR resulting_z_report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467')
    ),
    CONSTRAINT ck_fiscal_z_close_state_transitions__hash CHECK (
        semantic_request_hash ~ '^[0-9a-f]{64}$'
        AND semantic_hash_version IN (
            'pos-server-fiscal-z-close-state-initialize:sha256:v1',
            'pos-server-fiscal-z-close-state-transition:sha256:v1'
        )
    ),
    CONSTRAINT ck_fiscal_z_close_state_transitions__refs CHECK (
        char_length(btrim(operation_ref)) > 0
        AND char_length(btrim(approval_ref)) > 0
        AND char_length(btrim(actor_ref)) > 0
        AND char_length(btrim(service_identity_ref)) > 0
        AND char_length(btrim(correlation_ref)) > 0
    )
);

CREATE INDEX IF NOT EXISTS ix_fiscal_z_close_state_transitions__state_committed
    ON pos.fiscal_z_close_state_transitions (fiscal_z_close_state_id, committed_at DESC);

CREATE INDEX IF NOT EXISTS ix_fiscal_z_close_state_transitions__period
    ON pos.fiscal_z_close_state_transitions (resulting_reporting_period_id)
    WHERE resulting_reporting_period_id IS NOT NULL;

CREATE OR REPLACE TRIGGER trg_fiscal_z_close_state_transitions_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_z_close_state_transitions
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

COMMENT ON TABLE pos.fiscal_z_close_state_transitions IS 'Append-only operation, semantic identity, version, predecessor, actor, and durable-commit evidence for canonical Z state.';
COMMENT ON COLUMN pos.fiscal_z_close_state_transitions.semantic_request_hash IS 'Internal replay material; never returned by public APIs or logs.';
