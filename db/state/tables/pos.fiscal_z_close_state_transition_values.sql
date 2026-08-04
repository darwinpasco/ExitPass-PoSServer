-- Immutable governed reset counter, Z counter, and GTA values for each state transition.

CREATE TABLE IF NOT EXISTS pos.fiscal_z_close_state_transition_values (
    fiscal_z_close_state_transition_value_id uuid NOT NULL,
    fiscal_z_close_state_transition_id uuid NOT NULL,
    state_identity_code_id uuid NOT NULL,
    previous_counter_value bigint NULL,
    resulting_counter_value bigint NULL,
    previous_amount_minor_units bigint NULL,
    resulting_amount_minor_units bigint NULL,
    currency_code char(3) NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_z_close_state_transition_values PRIMARY KEY (fiscal_z_close_state_transition_value_id),
    CONSTRAINT fk_fiscal_z_close_state_transition_values__transition FOREIGN KEY (fiscal_z_close_state_transition_id)
        REFERENCES pos.fiscal_z_close_state_transitions (fiscal_z_close_state_transition_id),
    CONSTRAINT fk_fiscal_z_close_state_transition_values__identity FOREIGN KEY (state_identity_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_z_close_state_transition_values__identity UNIQUE (
        fiscal_z_close_state_transition_id, state_identity_code_id
    ),
    CONSTRAINT ck_fiscal_z_close_state_transition_values__identity_family CHECK (
        state_identity_code_id IN (
            'baf7bb67-9f11-5b2f-a750-e5689a5f0142',
            'a1dddcd8-aab5-51a8-bb64-f610edbdc1eb',
            'e01e75a6-6ee9-5bdf-9bf4-0a38476f8805'
        )
    ),
    CONSTRAINT ck_fiscal_z_close_state_transition_values__shape CHECK (
        (
            state_identity_code_id IN (
                'baf7bb67-9f11-5b2f-a750-e5689a5f0142',
                'a1dddcd8-aab5-51a8-bb64-f610edbdc1eb'
            )
            AND resulting_counter_value IS NOT NULL
            AND resulting_counter_value >= 0
            AND (previous_counter_value IS NULL OR previous_counter_value >= 0)
            AND previous_amount_minor_units IS NULL
            AND resulting_amount_minor_units IS NULL
            AND currency_code IS NULL
        )
        OR
        (
            state_identity_code_id = 'e01e75a6-6ee9-5bdf-9bf4-0a38476f8805'
            AND resulting_amount_minor_units IS NOT NULL
            AND resulting_amount_minor_units >= 0
            AND (previous_amount_minor_units IS NULL OR previous_amount_minor_units >= 0)
            AND previous_counter_value IS NULL
            AND resulting_counter_value IS NULL
            AND currency_code ~ '^[A-Z]{3}$'
        )
    )
);

CREATE OR REPLACE TRIGGER trg_fiscal_z_close_state_transition_values_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_z_close_state_transition_values
FOR EACH ROW EXECUTE FUNCTION pos.reject_fiscal_reporting_snapshot_mutation();

CREATE OR REPLACE FUNCTION pos.protect_fiscal_z_close_state_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'canonical fiscal Z close state cannot be deleted'
            USING ERRCODE = 'check_violation';
    END IF;
    IF current_setting('pos.fiscal_z_close_state_transition', true) IS DISTINCT FROM 'on' THEN
        RAISE EXCEPTION 'canonical fiscal Z close state requires governed transition'
            USING ERRCODE = 'check_violation';
    END IF;
    IF NEW.state_version <> OLD.state_version + 1 THEN
        RAISE EXCEPTION 'canonical fiscal Z close state version must advance exactly once'
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE TRIGGER trg_fiscal_z_close_states_governed_mutation
BEFORE UPDATE OR DELETE ON pos.fiscal_z_close_states
FOR EACH ROW EXECUTE FUNCTION pos.protect_fiscal_z_close_state_mutation();

CREATE OR REPLACE FUNCTION pos.validate_fiscal_z_close_state_evidence()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    evidence_count integer;
BEGIN
    SELECT count(*) INTO evidence_count
    FROM pos.fiscal_z_close_state_transitions transition
    WHERE transition.fiscal_z_close_state_id = NEW.fiscal_z_close_state_id
      AND transition.resulting_state_version = NEW.state_version
      AND transition.operation_ref = NEW.last_transition_operation_ref;

    IF evidence_count <> 1 THEN
        RAISE EXCEPTION 'canonical fiscal Z close state requires matching transition evidence'
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_fiscal_z_close_states_evidence ON pos.fiscal_z_close_states;
CREATE CONSTRAINT TRIGGER trg_fiscal_z_close_states_evidence
AFTER INSERT OR UPDATE ON pos.fiscal_z_close_states
DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION pos.validate_fiscal_z_close_state_evidence();

CREATE OR REPLACE FUNCTION pos.validate_fiscal_z_close_transition_values()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    evidence_count integer;
BEGIN
    SELECT count(DISTINCT state_identity_code_id) INTO evidence_count
    FROM pos.fiscal_z_close_state_transition_values
    WHERE fiscal_z_close_state_transition_id = NEW.fiscal_z_close_state_transition_id;

    IF evidence_count <> 3 THEN
        RAISE EXCEPTION 'fiscal Z close state transition requires exactly three governed values'
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_fiscal_z_close_state_transitions_values ON pos.fiscal_z_close_state_transitions;
CREATE CONSTRAINT TRIGGER trg_fiscal_z_close_state_transitions_values
AFTER INSERT ON pos.fiscal_z_close_state_transitions
DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION pos.validate_fiscal_z_close_transition_values();

CREATE OR REPLACE FUNCTION pos.apply_fiscal_z_close_state_transition(
    p_transition_id uuid
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    transition_row pos.fiscal_z_close_state_transitions%ROWTYPE;
    z_value bigint;
    reset_value bigint;
    gta_value bigint;
    affected integer;
BEGIN
    SELECT * INTO STRICT transition_row
    FROM pos.fiscal_z_close_state_transitions
    WHERE fiscal_z_close_state_transition_id = p_transition_id;

    IF transition_row.transition_type_code_id <> '52a28fc9-7c24-5810-8f98-30dbc7c134b9' THEN
        RAISE EXCEPTION 'only governed Z close transitions may advance initialized state'
            USING ERRCODE = 'check_violation';
    END IF;

    SELECT resulting_counter_value INTO STRICT z_value
    FROM pos.fiscal_z_close_state_transition_values
    WHERE fiscal_z_close_state_transition_id = p_transition_id
      AND state_identity_code_id = 'baf7bb67-9f11-5b2f-a750-e5689a5f0142';
    SELECT resulting_counter_value INTO STRICT reset_value
    FROM pos.fiscal_z_close_state_transition_values
    WHERE fiscal_z_close_state_transition_id = p_transition_id
      AND state_identity_code_id = 'a1dddcd8-aab5-51a8-bb64-f610edbdc1eb';
    SELECT resulting_amount_minor_units INTO STRICT gta_value
    FROM pos.fiscal_z_close_state_transition_values
    WHERE fiscal_z_close_state_transition_id = p_transition_id
      AND state_identity_code_id = 'e01e75a6-6ee9-5bdf-9bf4-0a38476f8805';

    PERFORM set_config('pos.fiscal_z_close_state_transition', 'on', true);
    UPDATE pos.fiscal_z_close_states
    SET reset_counter_value = reset_value,
        z_counter_value = z_value,
        grand_total_amount_minor_units = gta_value,
        state_version = transition_row.resulting_state_version,
        last_closed_reporting_period_id = transition_row.resulting_reporting_period_id,
        last_committed_z_report_id = transition_row.resulting_z_report_id,
        last_committed_z_report_kind_code_id = transition_row.resulting_z_report_kind_code_id,
        last_transition_operation_ref = transition_row.operation_ref,
        last_transition_at = transition_row.committed_at,
        updated_at = transition_row.committed_at
    WHERE fiscal_z_close_state_id = transition_row.fiscal_z_close_state_id
      AND state_version = transition_row.expected_state_version;
    GET DIAGNOSTICS affected = ROW_COUNT;
    PERFORM set_config('pos.fiscal_z_close_state_transition', 'off', true);

    IF affected <> 1 THEN
        RAISE EXCEPTION 'canonical fiscal Z close state version conflict'
            USING ERRCODE = 'serialization_failure';
    END IF;
    RETURN transition_row.resulting_state_version;
END;
$$;

COMMENT ON TABLE pos.fiscal_z_close_state_transition_values IS 'Immutable per-identity before/result evidence. Contains no generic JSON, customer data, or credentials.';
COMMENT ON FUNCTION pos.apply_fiscal_z_close_state_transition(uuid) IS 'Expected-version state transition primitive for future Z close. Z-007B does not call it from an API or execute a period close.';
