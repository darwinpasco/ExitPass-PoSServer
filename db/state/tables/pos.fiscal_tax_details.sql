-- ExitPass POS Server Slice 3 table artifact.
-- Fiscal tax detail posture only.
-- This table captures tax classifications and amounts without encoding final BIR/accounting formulas.

CREATE TABLE IF NOT EXISTS pos.fiscal_tax_details (
    fiscal_tax_detail_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    fiscal_document_line_id uuid NULL,
    tax_type_code_id uuid NOT NULL,
    tax_classification_code_id uuid NOT NULL,
    tax_rate numeric(9, 6) NULL,
    taxable_amount_minor_units bigint NOT NULL DEFAULT 0,
    tax_amount_minor_units bigint NOT NULL DEFAULT 0,
    currency_code char(3) NOT NULL DEFAULT 'PHP',
    tax_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_tax_details PRIMARY KEY (fiscal_tax_detail_id),
    CONSTRAINT fk_fiscal_tax_details__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_tax_details__line FOREIGN KEY (fiscal_document_line_id)
        REFERENCES pos.fiscal_document_lines (fiscal_document_line_id),
    CONSTRAINT fk_fiscal_tax_details__tax_type_code FOREIGN KEY (tax_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_tax_details__tax_class_code FOREIGN KEY (tax_classification_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_tax_details__tax_rate_nonnegative CHECK (
        tax_rate IS NULL OR tax_rate >= 0
    ),
    CONSTRAINT ck_fiscal_tax_details__taxable_amt_nonnegative CHECK (taxable_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_tax_details__tax_amt_nonnegative CHECK (tax_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_tax_details__currency_format CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_tax_details__context_object CHECK (
        tax_context IS NULL OR jsonb_typeof(tax_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_tax_details IS 'Fiscal tax classification and amount detail posture. Does not encode final VAT, exemption, or BIR/accounting formulas.';
COMMENT ON COLUMN pos.fiscal_tax_details.fiscal_document_line_id IS 'Optional line-level tax detail reference; document-level tax detail is allowed when null.';
COMMENT ON COLUMN pos.fiscal_tax_details.tax_type_code_id IS 'Controlled-code reference for tax type.';
COMMENT ON COLUMN pos.fiscal_tax_details.tax_classification_code_id IS 'Controlled-code reference for tax classification such as VATable, VAT-exempt, zero-rated, non-VAT, or future approved classifications.';
COMMENT ON COLUMN pos.fiscal_tax_details.tax_context IS 'Flexible tax context for unresolved tax attributes. Must remain a JSON object when present.';

