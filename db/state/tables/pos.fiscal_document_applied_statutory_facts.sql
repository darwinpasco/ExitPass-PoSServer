-- ExitPass POS Server applied statutory fiscal facts snapshot.
-- Stores final Central PMS payment-time statutory application facts for a fiscal document.
-- This table does not approve eligibility, calculate statutory benefits, retrieve evidence, or own payment finality.

CREATE TABLE IF NOT EXISTS pos.fiscal_document_applied_statutory_facts (
    fiscal_document_applied_statutory_fact_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    statutory_discount_decision_command_id uuid NOT NULL,
    statutory_request_reference uuid NOT NULL,
    statutory_payable_basis_application_command_id uuid NOT NULL,
    statutory_validation_id uuid NOT NULL,
    parking_session_id uuid NOT NULL,
    site_id uuid NOT NULL,
    site_group_id uuid NOT NULL,
    entitlement_type_code_id uuid NOT NULL,
    benefit_classification_code_id uuid NOT NULL,
    policy_resolution_basis_code_id uuid NOT NULL,
    applied_policy_reference_id uuid NULL,
    policy_code text NULL,
    policy_version_id uuid NULL,
    national_law_reference text NULL,
    ordinance_reference text NULL,
    original_tariff_snapshot_id uuid NOT NULL,
    applied_tariff_snapshot_id uuid NOT NULL,
    original_amount_minor_units bigint NOT NULL,
    vat_exclusive_basis_amount_minor_units bigint NOT NULL,
    vat_amount_minor_units bigint NOT NULL,
    vat_treatment_code_id uuid NOT NULL,
    statutory_discount_amount_minor_units bigint NOT NULL,
    final_payable_amount_minor_units bigint NOT NULL,
    currency_code char(3) NOT NULL,
    applied_at timestamptz NOT NULL,
    source_payment_channel_code_id uuid NOT NULL,
    terminal_cash_tender_id uuid NULL,
    snapshot_created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_doc_applied_stat_facts PRIMARY KEY (fiscal_document_applied_statutory_fact_id),
    CONSTRAINT fk_fiscal_doc_applied_stat_facts__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_doc_applied_stat_facts__entitlement FOREIGN KEY (entitlement_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_doc_applied_stat_facts__benefit FOREIGN KEY (benefit_classification_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_doc_applied_stat_facts__policy_basis FOREIGN KEY (policy_resolution_basis_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_doc_applied_stat_facts__vat_treatment FOREIGN KEY (vat_treatment_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_doc_applied_stat_facts__source_channel FOREIGN KEY (source_payment_channel_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT uq_fiscal_doc_applied_stat_facts__document UNIQUE (fiscal_document_id),
    CONSTRAINT uq_fiscal_doc_applied_stat_facts__decision UNIQUE (statutory_discount_decision_command_id),
    CONSTRAINT uq_fiscal_doc_applied_stat_facts__request_ref UNIQUE (statutory_request_reference),
    CONSTRAINT uq_fiscal_doc_applied_stat_facts__application UNIQUE (statutory_payable_basis_application_command_id),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__policy_code CHECK (
        policy_code IS NULL OR char_length(btrim(policy_code)) > 0
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__national_law CHECK (
        national_law_reference IS NULL OR char_length(btrim(national_law_reference)) > 0
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__ordinance_ref CHECK (
        ordinance_reference IS NULL OR char_length(btrim(ordinance_reference)) > 0
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__policy_ref CHECK (
        applied_policy_reference_id IS NOT NULL
        OR policy_code IS NOT NULL
        OR policy_version_id IS NOT NULL
        OR national_law_reference IS NOT NULL
        OR ordinance_reference IS NOT NULL
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__entitlement_code CHECK (
        entitlement_type_code_id IN (
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '8ed32ee5-a7f3-58df-9da4-57c93568bcfa'
        )
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__benefit_code CHECK (
        benefit_classification_code_id IN (
            'c37209f8-9b53-5b9b-8b6d-e8d8f8987c8e',
            '9bb26872-dcac-507a-9ecc-82b05046e65c',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            'f232a486-a5b7-5f66-aa59-87dc65b3ab4c',
            'a59c5618-51f5-57ee-b53b-7c19f1ee707c',
            'ab221cc4-9d9a-5af7-a08a-a3690362138e'
        )
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__policy_basis_code CHECK (
        policy_resolution_basis_code_id IN (
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'd4ecda73-025e-5edd-b784-1ec6aba649a8',
            '1a2e5599-1818-549b-b270-1eb3015ac533',
            '7d18a81c-8b4a-5021-be9a-0e2ec4e4fa84'
        )
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__vat_code CHECK (
        vat_treatment_code_id IN (
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            '6e07fc9c-eb25-52cf-b105-30e27a35bc3a',
            '51ea6aa5-78ba-5bff-a556-dbf4ca7a7968',
            '90ac1053-326d-55a5-8e58-4ea841d3b928',
            'a54175ed-c8af-5fdd-8844-28707486130f',
            '5c6e1dab-ae5a-5b4e-a651-85442f53dc97'
        )
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__source_channel_code CHECK (
        source_payment_channel_code_id IN (
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            '9139096f-715b-59ae-958c-48984d77d343',
            '06ad63a0-b1f3-5920-afd5-39f8226c1b1d'
        )
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__tariff_refs CHECK (
        applied_tariff_snapshot_id <> original_tariff_snapshot_id
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__amounts CHECK (
        original_amount_minor_units >= 0
        AND vat_exclusive_basis_amount_minor_units >= 0
        AND vat_amount_minor_units >= 0
        AND statutory_discount_amount_minor_units >= 0
        AND final_payable_amount_minor_units >= 0
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__final_amount CHECK (
        final_payable_amount_minor_units <= original_amount_minor_units
        AND statutory_discount_amount_minor_units <= original_amount_minor_units
        AND final_payable_amount_minor_units + statutory_discount_amount_minor_units <= original_amount_minor_units
    ),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__currency CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__snapshot_time CHECK (snapshot_created_at >= applied_at),
    CONSTRAINT ck_fiscal_doc_applied_stat_facts__audit_time CHECK (updated_at = created_at)
);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__validation
    ON pos.fiscal_document_applied_statutory_facts (statutory_validation_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__parking_session
    ON pos.fiscal_document_applied_statutory_facts (parking_session_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__site
    ON pos.fiscal_document_applied_statutory_facts (site_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__site_group
    ON pos.fiscal_document_applied_statutory_facts (site_group_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__entitlement
    ON pos.fiscal_document_applied_statutory_facts (entitlement_type_code_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__benefit
    ON pos.fiscal_document_applied_statutory_facts (benefit_classification_code_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__policy_ref
    ON pos.fiscal_document_applied_statutory_facts (applied_policy_reference_id)
    WHERE applied_policy_reference_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__policy_code
    ON pos.fiscal_document_applied_statutory_facts (policy_code)
    WHERE policy_code IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__applied_tariff
    ON pos.fiscal_document_applied_statutory_facts (applied_tariff_snapshot_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__vat_treatment
    ON pos.fiscal_document_applied_statutory_facts (vat_treatment_code_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__source_channel
    ON pos.fiscal_document_applied_statutory_facts (source_payment_channel_code_id);

CREATE INDEX IF NOT EXISTS ix_fiscal_doc_applied_stat_facts__terminal_cash
    ON pos.fiscal_document_applied_statutory_facts (terminal_cash_tender_id)
    WHERE terminal_cash_tender_id IS NOT NULL;

CREATE OR REPLACE FUNCTION pos.reject_applied_stat_facts_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'applied statutory fiscal facts snapshots are immutable'
        USING ERRCODE = 'check_violation';
END;
$$;

CREATE OR REPLACE TRIGGER trg_fiscal_doc_applied_stat_facts_immutable
BEFORE UPDATE OR DELETE ON pos.fiscal_document_applied_statutory_facts
FOR EACH ROW EXECUTE FUNCTION pos.reject_applied_stat_facts_mutation();

COMMENT ON TABLE pos.fiscal_document_applied_statutory_facts IS 'Immutable POS Server fiscal snapshot of final Central PMS payment-time statutory privilege application facts. Optional for ordinary fiscal documents; one row maximum per statutory fiscal document.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.fiscal_document_id IS 'Owning POS Server fiscal document. This table is a child snapshot and does not create a second fiscal document.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.statutory_discount_decision_command_id IS 'Central PMS statutory discount decision command reference. Immutable external reference only; POS Server does not decide eligibility.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.statutory_request_reference IS 'Central PMS statutory request reference for audit and replay correlation. Immutable external reference only.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.statutory_payable_basis_application_command_id IS 'Central PMS final payable-basis application command reference. Unique to prevent duplicate fiscalization of the same final application.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.statutory_validation_id IS 'Central PMS validation reference for the final application chain. No reviewer, beneficiary, or evidence data is stored.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.parking_session_id IS 'Central PMS parking-session reference from the applied statutory facts. POS Server does not own parking-session lifecycle.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.site_id IS 'Central PMS Site reference associated with the final applied payable basis.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.site_group_id IS 'Central PMS Site Group reference associated with the final applied payable basis.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.entitlement_type_code_id IS 'Controlled-code reference for statutory entitlement classification such as SENIOR_CITIZEN or PWD.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.benefit_classification_code_id IS 'Controlled-code reference for the fiscal effect classification, distinct from entitlement type.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.policy_resolution_basis_code_id IS 'Controlled-code reference for the safe policy-resolution basis. Raw ordinance text is prohibited.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.applied_policy_reference_id IS 'Optional Central PMS applied policy reference. Reference only; no raw policy text is stored.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.policy_code IS 'Optional safe policy code. Must not contain raw ordinance text, eligibility answers, credentials, or identity data.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.policy_version_id IS 'Optional safe policy version reference from Central PMS.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.national_law_reference IS 'Optional safe national-law reference label or code. No raw legal text is stored.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.ordinance_reference IS 'Optional safe ordinance reference label or code. No raw ordinance text is stored.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.original_tariff_snapshot_id IS 'Central PMS original tariff snapshot before statutory application. Reference only.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.applied_tariff_snapshot_id IS 'Central PMS applied tariff snapshot after statutory application. Reference only.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.original_amount_minor_units IS 'Authoritative original gross tariff/payable amount in minor currency units supplied by Central PMS.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.vat_exclusive_basis_amount_minor_units IS 'Authoritative VAT-exclusive basis in minor currency units supplied by Central PMS.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.vat_amount_minor_units IS 'Authoritative VAT amount in minor currency units supplied by Central PMS.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.vat_treatment_code_id IS 'Controlled-code reference for Central PMS supplied VAT treatment. POS Server does not infer VAT treatment.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.statutory_discount_amount_minor_units IS 'Authoritative final statutory discount or benefit amount in minor currency units. Not a rate or locally calculated percentage.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.final_payable_amount_minor_units IS 'Authoritative final payable amount in minor currency units after statutory application. Must reconcile with fiscal totals in runtime.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.currency_code IS 'Uppercase ISO 4217 currency code for all monetary statutory facts.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.applied_at IS 'Timestamp when Central PMS finalized the payment-time statutory payable-basis application.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.source_payment_channel_code_id IS 'Controlled-code reference for the source payment channel. Channels are not statutory fact authorities.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.terminal_cash_tender_id IS 'Optional terminal-cash tender reference required later for terminal-cash statutory issuance. Reference only.';
COMMENT ON COLUMN pos.fiscal_document_applied_statutory_facts.snapshot_created_at IS 'Timestamp when POS Server stored this immutable fiscal snapshot.';
COMMENT ON FUNCTION pos.reject_applied_stat_facts_mutation() IS 'Rejects direct UPDATE and DELETE of immutable applied statutory fiscal facts snapshots.';
