-- ExitPass POS Server Slice 3 table artifact.
-- Fiscal document total posture only.
-- This table stores total rows by type without encoding final reconciliation, report, or export formulas.

CREATE TABLE IF NOT EXISTS pos.fiscal_totals (
    fiscal_total_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    total_type_code_id uuid NOT NULL,
    amount_minor_units bigint NOT NULL DEFAULT 0,
    currency_code char(3) NOT NULL DEFAULT 'PHP',
    total_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_totals PRIMARY KEY (fiscal_total_id),
    CONSTRAINT fk_fiscal_totals__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_totals__total_type_code FOREIGN KEY (total_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_totals__document_total_type UNIQUE (fiscal_document_id, total_type_code_id),
    CONSTRAINT ck_fiscal_totals__amount_nonnegative CHECK (amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_totals__currency_format CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_totals__context_object CHECK (
        total_context IS NULL OR jsonb_typeof(total_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_totals IS 'Fiscal document total rows by controlled total type. Does not encode final BIR/accounting reconciliation, report, or export formulas.';
COMMENT ON COLUMN pos.fiscal_totals.total_type_code_id IS 'Controlled-code reference for total type such as gross, discount, tax, VAT privilege, net, tendered, or future approved total category.';
COMMENT ON COLUMN pos.fiscal_totals.total_context IS 'Flexible total context for unresolved total attributes. Must remain a JSON object when present.';

