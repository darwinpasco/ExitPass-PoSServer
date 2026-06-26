-- ExitPass POS Server Slice 3 table artifact.
-- Fiscal discount and privilege detail posture only.
-- This table stores evidence references only and does not encode final BIR/accounting formulas.

CREATE TABLE IF NOT EXISTS pos.fiscal_discount_privilege_details (
    fiscal_discount_privilege_detail_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    fiscal_document_line_id uuid NULL,
    discount_privilege_type_code_id uuid NOT NULL,
    basis_amount_minor_units bigint NOT NULL DEFAULT 0,
    discount_amount_minor_units bigint NOT NULL DEFAULT 0,
    vat_privilege_amount_minor_units bigint NOT NULL DEFAULT 0,
    currency_code char(3) NOT NULL DEFAULT 'PHP',
    beneficiary_ref text NULL,
    evidence_ref text NULL,
    approval_ref text NULL,
    discount_privilege_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_disc_priv_details PRIMARY KEY (fiscal_discount_privilege_detail_id),
    CONSTRAINT fk_fiscal_disc_priv_details__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_disc_priv_details__line FOREIGN KEY (fiscal_document_line_id)
        REFERENCES pos.fiscal_document_lines (fiscal_document_line_id),
    CONSTRAINT fk_fiscal_disc_priv_details__type_code FOREIGN KEY (discount_privilege_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_disc_priv_details__basis_amt CHECK (basis_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_disc_priv_details__discount_amt CHECK (discount_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_disc_priv_details__vat_priv_amt CHECK (vat_privilege_amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_disc_priv_details__currency_format CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_disc_priv_details__beneficiary_ref CHECK (
        beneficiary_ref IS NULL OR char_length(btrim(beneficiary_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_disc_priv_details__evidence_ref CHECK (
        evidence_ref IS NULL OR char_length(btrim(evidence_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_disc_priv_details__approval_ref CHECK (
        approval_ref IS NULL OR char_length(btrim(approval_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_disc_priv_details__context_object CHECK (
        discount_privilege_context IS NULL OR jsonb_typeof(discount_privilege_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_discount_privilege_details IS 'Fiscal discount, statutory privilege, and VAT privilege detail posture. Evidence is stored as references only; no raw evidence files or final formulas are created.';
COMMENT ON COLUMN pos.fiscal_discount_privilege_details.discount_privilege_type_code_id IS 'Controlled-code reference for statutory discount, commercial discount, VAT privilege or exemption, or future approved category.';
COMMENT ON COLUMN pos.fiscal_discount_privilege_details.beneficiary_ref IS 'Optional beneficiary reference only; do not store sensitive evidence content in this field.';
COMMENT ON COLUMN pos.fiscal_discount_privilege_details.evidence_ref IS 'Optional evidence reference only; raw evidence files or sensitive evidence content are not stored by this table.';
COMMENT ON COLUMN pos.fiscal_discount_privilege_details.approval_ref IS 'Optional approval reference only; does not create external approval authority.';
COMMENT ON COLUMN pos.fiscal_discount_privilege_details.discount_privilege_context IS 'Flexible discount or privilege context for unresolved attributes. Must remain a JSON object when present.';

