-- ExitPass POS Server Slice 4 table artifact.
-- Fiscal sequence state/posture only.
-- This table tracks sequence state records without implementing allocation logic or PostgreSQL sequences.

CREATE TABLE IF NOT EXISTS pos.fiscal_sequence_states (
    fiscal_sequence_state_id uuid NOT NULL,
    fiscal_sequence_policy_id uuid NOT NULL,
    current_sequence_value bigint NOT NULL DEFAULT 0,
    last_reserved_sequence_value bigint NULL,
    last_issued_sequence_value bigint NULL,
    sequence_state_code_id uuid NOT NULL,
    last_transition_at timestamptz NULL,
    state_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_sequence_states PRIMARY KEY (fiscal_sequence_state_id),
    CONSTRAINT fk_fiscal_seq_states__policy FOREIGN KEY (fiscal_sequence_policy_id)
        REFERENCES pos.fiscal_sequence_policies (fiscal_sequence_policy_id),
    CONSTRAINT fk_fiscal_seq_states__state_code FOREIGN KEY (sequence_state_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_seq_states__policy UNIQUE (fiscal_sequence_policy_id),
    CONSTRAINT ck_fiscal_seq_states__current_nonnegative CHECK (current_sequence_value >= 0),
    CONSTRAINT ck_fiscal_seq_states__reserved_nonnegative CHECK (
        last_reserved_sequence_value IS NULL OR last_reserved_sequence_value >= 0
    ),
    CONSTRAINT ck_fiscal_seq_states__issued_nonnegative CHECK (
        last_issued_sequence_value IS NULL OR last_issued_sequence_value >= 0
    ),
    CONSTRAINT ck_fiscal_seq_states__reserved_within_current CHECK (
        last_reserved_sequence_value IS NULL OR last_reserved_sequence_value <= current_sequence_value
    ),
    CONSTRAINT ck_fiscal_seq_states__issued_within_current CHECK (
        last_issued_sequence_value IS NULL OR last_issued_sequence_value <= current_sequence_value
    ),
    CONSTRAINT ck_fiscal_seq_states__context_object CHECK (
        state_context IS NULL OR jsonb_typeof(state_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_sequence_states IS 'Current fiscal sequence state posture. Does not implement fiscal number allocation, PostgreSQL sequences, functions, or triggers.';
COMMENT ON COLUMN pos.fiscal_sequence_states.current_sequence_value IS 'Current sequence state value only; not an allocation function.';
COMMENT ON COLUMN pos.fiscal_sequence_states.last_reserved_sequence_value IS 'Last reserved sequence value posture. Reserved numbers are not reused unless a future compliant rule is approved.';
COMMENT ON COLUMN pos.fiscal_sequence_states.last_issued_sequence_value IS 'Last issued sequence value posture. Consumed numbers are never reused.';
COMMENT ON COLUMN pos.fiscal_sequence_states.state_context IS 'Flexible sequence state context. Must remain a JSON object when present.';

