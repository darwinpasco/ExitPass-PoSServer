-- ExitPass POS Server Slice 3 table artifact.
-- Fiscal document line detail posture only.
-- This table does not allocate fiscal numbers, create counters, own payment finality, or create report/export behavior.

CREATE TABLE IF NOT EXISTS pos.fiscal_document_lines (
    fiscal_document_line_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    line_sequence integer NOT NULL,
    line_type_code_id uuid NOT NULL,
    line_status_code_id uuid NULL,
    description text NOT NULL,
    quantity numeric(18, 4) NOT NULL DEFAULT 1,
    unit_amount_minor_units bigint NOT NULL DEFAULT 0,
    gross_amount_minor_units bigint NOT NULL DEFAULT 0,
    discount_amount_minor_units bigint NOT NULL DEFAULT 0,
    tax_amount_minor_units bigint NOT NULL DEFAULT 0,
    net_amount_minor_units bigint NOT NULL DEFAULT 0,
    currency_code char(3) NOT NULL DEFAULT 'PHP',
    source_ref text NULL,
    line_context jsonb NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_document_lines PRIMARY KEY (fiscal_document_line_id),
    CONSTRAINT fk_fiscal_document_lines__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_document_lines__line_type_code FOREIGN KEY (line_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_document_lines__line_status_code FOREIGN KEY (line_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_document_lines__document_sequence UNIQUE (fiscal_document_id, line_sequence),
    CONSTRAINT ck_fiscal_document_lines__line_sequence_positive CHECK (line_sequence > 0),
    CONSTRAINT ck_fiscal_document_lines__quantity_positive CHECK (quantity > 0),
    CONSTRAINT ck_fiscal_document_lines__unit_amt_nonnegative CHECK (unit_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_document_lines__gross_amt_nonnegative CHECK (gross_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_document_lines__discount_amt_nonnegative CHECK (discount_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_document_lines__tax_amt_nonnegative CHECK (tax_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_document_lines__net_amt_nonnegative CHECK (net_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_document_lines__currency_format CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_document_lines__description_not_blank CHECK (char_length(btrim(description)) > 0),
    CONSTRAINT ck_fiscal_document_lines__source_ref CHECK (
        source_ref IS NULL OR char_length(btrim(source_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_document_lines__context_object CHECK (
        line_context IS NULL OR jsonb_typeof(line_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_document_lines IS 'Ordered fiscal document line detail posture. Does not allocate fiscal numbers, create counters, or define final fiscal arithmetic formulas.';
COMMENT ON COLUMN pos.fiscal_document_lines.line_sequence IS 'Document-local line order. Unique within a fiscal document.';
COMMENT ON COLUMN pos.fiscal_document_lines.line_type_code_id IS 'Controlled-code reference for the fiscal line type.';
COMMENT ON COLUMN pos.fiscal_document_lines.line_status_code_id IS 'Optional controlled-code reference for line status; local detail status only.';
COMMENT ON COLUMN pos.fiscal_document_lines.source_ref IS 'Optional source reference for upstream line context; reference only.';
COMMENT ON COLUMN pos.fiscal_document_lines.line_context IS 'Flexible line context for unresolved detail attributes. Must remain a JSON object when present.';

