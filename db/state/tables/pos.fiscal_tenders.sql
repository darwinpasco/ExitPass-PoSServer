-- ExitPass POS Server Slice 3 table artifact.
-- Fiscal tender/payment context posture only.
-- This table stores payment references as context only and does not own payment finality or payment lifecycles.

CREATE TABLE IF NOT EXISTS pos.fiscal_tenders (
    fiscal_tender_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    tender_type_code_id uuid NOT NULL,
    amount_minor_units bigint NOT NULL,
    currency_code char(3) NOT NULL DEFAULT 'PHP',
    central_pms_payment_attempt_ref text NULL,
    central_pms_payment_confirmation_ref text NULL,
    payment_finality_ref text NULL,
    provider_ref text NULL,
    tender_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_tenders PRIMARY KEY (fiscal_tender_id),
    CONSTRAINT fk_fiscal_tenders__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_tenders__tender_type_code FOREIGN KEY (tender_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_fiscal_tenders__amount_nonnegative CHECK (amount_minor_units >= 0),
    CONSTRAINT ck_fiscal_tenders__currency_format CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_tenders__payment_attempt_ref CHECK (
        central_pms_payment_attempt_ref IS NULL OR char_length(btrim(central_pms_payment_attempt_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_tenders__payment_confirmation_ref CHECK (
        central_pms_payment_confirmation_ref IS NULL OR char_length(btrim(central_pms_payment_confirmation_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_tenders__payment_finality_ref CHECK (
        payment_finality_ref IS NULL OR char_length(btrim(payment_finality_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_tenders__provider_ref CHECK (
        provider_ref IS NULL OR char_length(btrim(provider_ref)) > 0
    ),
    CONSTRAINT ck_fiscal_tenders__context_object CHECK (
        tender_context IS NULL OR jsonb_typeof(tender_context) = 'object'
    )
);

COMMENT ON TABLE pos.fiscal_tenders IS 'Tender/payment context associated with fiscal documents. Central PMS payment values are references only and do not create POS-owned payment finality.';
COMMENT ON COLUMN pos.fiscal_tenders.central_pms_payment_attempt_ref IS 'Reference to Central PMS PaymentAttempt context; POS Server does not own PaymentAttempt lifecycle.';
COMMENT ON COLUMN pos.fiscal_tenders.central_pms_payment_confirmation_ref IS 'Reference to Central PMS PaymentConfirmation context; POS Server does not own PaymentConfirmation lifecycle.';
COMMENT ON COLUMN pos.fiscal_tenders.payment_finality_ref IS 'Reference to Central PMS payment finality context only; not POS-owned payment finality.';
COMMENT ON COLUMN pos.fiscal_tenders.provider_ref IS 'Optional provider reference for payment context; reference only.';
COMMENT ON COLUMN pos.fiscal_tenders.tender_context IS 'Flexible tender context for unresolved tender attributes. Must remain a JSON object when present.';

