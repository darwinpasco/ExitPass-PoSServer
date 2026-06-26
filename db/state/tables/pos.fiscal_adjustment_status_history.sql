-- ExitPass POS Server Slice 5 table artifact.
-- Fiscal adjustment status transition posture only.
-- This table is local status history and does not create refund/reversal finality or a full audit subsystem.

CREATE TABLE IF NOT EXISTS pos.fiscal_adjustment_status_history (
    fiscal_adjustment_status_history_id uuid NOT NULL,
    fiscal_adjustment_id uuid NOT NULL,
    prior_adjustment_status_code_id uuid NULL,
    new_adjustment_status_code_id uuid NOT NULL,
    status_reason_code_id uuid NULL,
    status_reason_text text NULL,
    changed_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actor_ref text NULL,
    service_identity_ref text NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_adj_status_history PRIMARY KEY (fiscal_adjustment_status_history_id),
    CONSTRAINT fk_fiscal_adj_status_hist__adjustment FOREIGN KEY (fiscal_adjustment_id)
        REFERENCES pos.fiscal_adjustments (fiscal_adjustment_id),
    CONSTRAINT fk_fiscal_adj_status_hist__prior_status FOREIGN KEY (prior_adjustment_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_adj_status_hist__new_status FOREIGN KEY (new_adjustment_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_adj_status_hist__reason_code FOREIGN KEY (status_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_adj_status_hist__reason_text CHECK (
        status_reason_text IS NULL OR char_length(btrim(status_reason_text)) > 0
    ),
    CONSTRAINT ck_fiscal_adj_status_hist__actor_ref CHECK (
        actor_ref IS NULL OR char_length(btrim(actor_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_adj_status_hist__service_ref CHECK (
        service_identity_ref IS NULL OR char_length(btrim(service_identity_ref)) > 0
    )
);

COMMENT ON TABLE pos.fiscal_adjustment_status_history IS 'Local fiscal adjustment status transition posture. Does not create refund/reversal finality or a full audit subsystem.';
COMMENT ON COLUMN pos.fiscal_adjustment_status_history.actor_ref IS 'Reference to actor context when available; reference only.';
COMMENT ON COLUMN pos.fiscal_adjustment_status_history.service_identity_ref IS 'Reference to service identity context when available; reference only.';

