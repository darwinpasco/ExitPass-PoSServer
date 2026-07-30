-- POS Server applied statutory fiscal facts schema proof fixture.
-- Run only against a disposable PostgreSQL database after schema rebuild and controlled-code load.
-- Uses deterministic non-production identifiers and no beneficiary identity, evidence, credentials, or raw policy text.

BEGIN;

INSERT INTO pos.controlled_code_sets (
    controlled_code_set_id,
    code_set_key,
    display_name,
    description
) VALUES
    ('41000000-0000-4000-8000-000000000101', 'schema_proof_fiscal_document_type', 'Schema Proof Fiscal Document Type', 'Disposable schema proof fiscal document type.'),
    ('41000000-0000-4000-8000-000000000102', 'schema_proof_fiscal_document_status', 'Schema Proof Fiscal Document Status', 'Disposable schema proof fiscal document status.')
ON CONFLICT (controlled_code_set_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    updated_at = CURRENT_TIMESTAMP;

INSERT INTO pos.controlled_codes (
    controlled_code_id,
    controlled_code_set_id,
    code_key,
    display_name,
    description
) VALUES
    ('41000000-0000-4000-8000-000000000201', '41000000-0000-4000-8000-000000000101', 'schema_proof_sales_invoice', 'Schema Proof Sales Invoice', 'Disposable schema proof document type.'),
    ('41000000-0000-4000-8000-000000000202', '41000000-0000-4000-8000-000000000102', 'schema_proof_recorded', 'Schema Proof Recorded', 'Disposable schema proof recorded status.')
ON CONFLICT (controlled_code_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    updated_at = CURRENT_TIMESTAMP;

INSERT INTO pos.site_pos_servers (
    site_pos_server_id,
    site_pos_server_code,
    display_name,
    central_pms_site_ref
) VALUES (
    '41000000-0000-4000-8000-000000000301',
    'schema-proof-site-pos-server',
    'Schema Proof Site POS Server',
    '41000000-0000-4000-8000-000000000302'
) ON CONFLICT (site_pos_server_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    updated_at = CURRENT_TIMESTAMP;

INSERT INTO pos.fiscal_documents (
    fiscal_document_id,
    site_pos_server_id,
    fiscal_document_type_code_id,
    fiscal_document_status_code_id,
    central_pms_parking_session_ref,
    central_pms_payment_attempt_ref,
    central_pms_payment_confirmation_ref,
    payment_finality_ref,
    business_day_date
) VALUES (
    '41000000-0000-4000-8000-000000000401',
    '41000000-0000-4000-8000-000000000301',
    '41000000-0000-4000-8000-000000000201',
    '41000000-0000-4000-8000-000000000202',
    '41000000-0000-4000-8000-000000000501',
    '41000000-0000-4000-8000-000000000502',
    '41000000-0000-4000-8000-000000000503',
    'SCHEMA-PROOF-FINALITY-ORDINARY-0001',
    '2026-07-30'
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pos.fiscal_document_applied_statutory_facts
        WHERE fiscal_document_id = '41000000-0000-4000-8000-000000000401'
    ) THEN
        RAISE EXCEPTION 'ordinary fiscal document unexpectedly has statutory facts';
    END IF;
END;
$$;

INSERT INTO pos.fiscal_documents (
    fiscal_document_id,
    site_pos_server_id,
    fiscal_document_type_code_id,
    fiscal_document_status_code_id,
    central_pms_parking_session_ref,
    central_pms_payment_attempt_ref,
    central_pms_payment_confirmation_ref,
    payment_finality_ref,
    business_day_date
) VALUES (
    '41000000-0000-4000-8000-000000000402',
    '41000000-0000-4000-8000-000000000301',
    '41000000-0000-4000-8000-000000000201',
    '41000000-0000-4000-8000-000000000202',
    '41000000-0000-4000-8000-000000000511',
    '41000000-0000-4000-8000-000000000512',
    '41000000-0000-4000-8000-000000000513',
    'SCHEMA-PROOF-FINALITY-STATUTORY-0001',
    '2026-07-30'
);

INSERT INTO pos.fiscal_document_applied_statutory_facts (
    fiscal_document_applied_statutory_fact_id,
    fiscal_document_id,
    statutory_discount_decision_command_id,
    statutory_request_reference,
    statutory_payable_basis_application_command_id,
    statutory_validation_id,
    parking_session_id,
    site_id,
    site_group_id,
    entitlement_type_code_id,
    benefit_classification_code_id,
    policy_resolution_basis_code_id,
    policy_code,
    original_tariff_snapshot_id,
    applied_tariff_snapshot_id,
    original_amount_minor_units,
    vat_exclusive_basis_amount_minor_units,
    vat_amount_minor_units,
    vat_treatment_code_id,
    statutory_discount_amount_minor_units,
    final_payable_amount_minor_units,
    currency_code,
    applied_at,
    source_payment_channel_code_id,
    snapshot_created_at
) VALUES (
    '41000000-0000-4000-8000-000000000601',
    '41000000-0000-4000-8000-000000000402',
    '41000000-0000-4000-8000-000000000701',
    '41000000-0000-4000-8000-000000000702',
    '41000000-0000-4000-8000-000000000703',
    '41000000-0000-4000-8000-000000000704',
    '41000000-0000-4000-8000-000000000511',
    '41000000-0000-4000-8000-000000000801',
    '41000000-0000-4000-8000-000000000802',
    '3df74017-1768-5285-9abe-f980cd1a4b65',
    '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
    '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
    'TEST-SAFE-POLICY-CODE-0001',
    '41000000-0000-4000-8000-000000000901',
    '41000000-0000-4000-8000-000000000902',
    10000,
    8929,
    0,
    '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
    1786,
    7143,
    'PHP',
    '2026-07-30T08:00:00Z',
    '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
    '2026-07-30T08:00:01Z'
);

DO $$
BEGIN
    BEGIN
        INSERT INTO pos.fiscal_document_applied_statutory_facts (
            fiscal_document_applied_statutory_fact_id,
            fiscal_document_id,
            statutory_discount_decision_command_id,
            statutory_request_reference,
            statutory_payable_basis_application_command_id,
            statutory_validation_id,
            parking_session_id,
            site_id,
            site_group_id,
            entitlement_type_code_id,
            benefit_classification_code_id,
            policy_resolution_basis_code_id,
            policy_code,
            original_tariff_snapshot_id,
            applied_tariff_snapshot_id,
            original_amount_minor_units,
            vat_exclusive_basis_amount_minor_units,
            vat_amount_minor_units,
            vat_treatment_code_id,
            statutory_discount_amount_minor_units,
            final_payable_amount_minor_units,
            currency_code,
            applied_at,
            source_payment_channel_code_id,
            snapshot_created_at
        ) VALUES (
            '41000000-0000-4000-8000-000000000602',
            '41000000-0000-4000-8000-000000000402',
            '41000000-0000-4000-8000-000000000711',
            '41000000-0000-4000-8000-000000000712',
            '41000000-0000-4000-8000-000000000713',
            '41000000-0000-4000-8000-000000000714',
            '41000000-0000-4000-8000-000000000511',
            '41000000-0000-4000-8000-000000000801',
            '41000000-0000-4000-8000-000000000802',
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0002',
            '41000000-0000-4000-8000-000000000911',
            '41000000-0000-4000-8000-000000000912',
            10000,
            8929,
            0,
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            1786,
            7143,
            'PHP',
            '2026-07-30T08:00:00Z',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            '2026-07-30T08:00:01Z'
        );
        RAISE EXCEPTION 'duplicate fiscal-document ownership was not rejected';
    EXCEPTION WHEN unique_violation THEN
        NULL;
    END;
END;
$$;

INSERT INTO pos.fiscal_documents (
    fiscal_document_id,
    site_pos_server_id,
    fiscal_document_type_code_id,
    fiscal_document_status_code_id,
    central_pms_parking_session_ref,
    central_pms_payment_attempt_ref,
    central_pms_payment_confirmation_ref,
    payment_finality_ref,
    business_day_date
) VALUES
    ('41000000-0000-4000-8000-000000000403', '41000000-0000-4000-8000-000000000301', '41000000-0000-4000-8000-000000000201', '41000000-0000-4000-8000-000000000202', '41000000-0000-4000-8000-000000000521', '41000000-0000-4000-8000-000000000522', '41000000-0000-4000-8000-000000000523', 'SCHEMA-PROOF-FINALITY-STATUTORY-0002', '2026-07-30'),
    ('41000000-0000-4000-8000-000000000404', '41000000-0000-4000-8000-000000000301', '41000000-0000-4000-8000-000000000201', '41000000-0000-4000-8000-000000000202', '41000000-0000-4000-8000-000000000531', '41000000-0000-4000-8000-000000000532', '41000000-0000-4000-8000-000000000533', 'SCHEMA-PROOF-FINALITY-STATUTORY-0003', '2026-07-30'),
    ('41000000-0000-4000-8000-000000000405', '41000000-0000-4000-8000-000000000301', '41000000-0000-4000-8000-000000000201', '41000000-0000-4000-8000-000000000202', '41000000-0000-4000-8000-000000000541', '41000000-0000-4000-8000-000000000542', '41000000-0000-4000-8000-000000000543', 'SCHEMA-PROOF-FINALITY-STATUTORY-0004', '2026-07-30'),
    ('41000000-0000-4000-8000-000000000406', '41000000-0000-4000-8000-000000000301', '41000000-0000-4000-8000-000000000201', '41000000-0000-4000-8000-000000000202', '41000000-0000-4000-8000-000000000551', '41000000-0000-4000-8000-000000000552', '41000000-0000-4000-8000-000000000553', 'SCHEMA-PROOF-FINALITY-STATUTORY-0005', '2026-07-30'),
    ('41000000-0000-4000-8000-000000000407', '41000000-0000-4000-8000-000000000301', '41000000-0000-4000-8000-000000000201', '41000000-0000-4000-8000-000000000202', '41000000-0000-4000-8000-000000000561', '41000000-0000-4000-8000-000000000562', '41000000-0000-4000-8000-000000000563', 'SCHEMA-PROOF-FINALITY-STATUTORY-0006', '2026-07-30'),
    ('41000000-0000-4000-8000-000000000408', '41000000-0000-4000-8000-000000000301', '41000000-0000-4000-8000-000000000201', '41000000-0000-4000-8000-000000000202', '41000000-0000-4000-8000-000000000571', '41000000-0000-4000-8000-000000000572', '41000000-0000-4000-8000-000000000573', 'SCHEMA-PROOF-FINALITY-STATUTORY-0007', '2026-07-30');

DO $$
DECLARE
    base_sql text := $sql$
        INSERT INTO pos.fiscal_document_applied_statutory_facts (
            fiscal_document_applied_statutory_fact_id,
            fiscal_document_id,
            statutory_discount_decision_command_id,
            statutory_request_reference,
            statutory_payable_basis_application_command_id,
            statutory_validation_id,
            parking_session_id,
            site_id,
            site_group_id,
            entitlement_type_code_id,
            benefit_classification_code_id,
            policy_resolution_basis_code_id,
            policy_code,
            original_tariff_snapshot_id,
            applied_tariff_snapshot_id,
            original_amount_minor_units,
            vat_exclusive_basis_amount_minor_units,
            vat_amount_minor_units,
            vat_treatment_code_id,
            statutory_discount_amount_minor_units,
            final_payable_amount_minor_units,
            currency_code,
            applied_at,
            source_payment_channel_code_id,
            terminal_cash_tender_id,
            snapshot_created_at
        ) VALUES (
            %L,
            %L,
            %L,
            %L,
            %L,
            %L,
            %L,
            '41000000-0000-4000-8000-000000000801',
            '41000000-0000-4000-8000-000000000802',
            %L,
            %L,
            %L,
            %L,
            %L,
            %L,
            %s,
            8929,
            0,
            %L,
            %s,
            %s,
            %L,
            '2026-07-30T08:00:00Z',
            %L,
            %L,
            '2026-07-30T08:00:01Z'
        )
    $sql$;
BEGIN
    BEGIN
        EXECUTE format(
            base_sql,
            '41000000-0000-4000-8000-000000000603',
            '41000000-0000-4000-8000-000000000403',
            '41000000-0000-4000-8000-000000000721',
            '41000000-0000-4000-8000-000000000722',
            '41000000-0000-4000-8000-000000000703',
            '41000000-0000-4000-8000-000000000724',
            '41000000-0000-4000-8000-000000000521',
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0003',
            '41000000-0000-4000-8000-000000000921',
            '41000000-0000-4000-8000-000000000922',
            10000,
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            1786,
            7143,
            'PHP',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            NULL
        );
        RAISE EXCEPTION 'duplicate application reference was not rejected';
    EXCEPTION WHEN unique_violation THEN
        NULL;
    END;

    BEGIN
        EXECUTE format(
            base_sql,
            '41000000-0000-4000-8000-000000000604',
            '41000000-0000-4000-8000-000000000404',
            '41000000-0000-4000-8000-000000000731',
            '41000000-0000-4000-8000-000000000732',
            '41000000-0000-4000-8000-000000000733',
            '41000000-0000-4000-8000-000000000734',
            '41000000-0000-4000-8000-000000000531',
            '99999999-9999-4999-8999-999999999999',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0004',
            '41000000-0000-4000-8000-000000000931',
            '41000000-0000-4000-8000-000000000932',
            10000,
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            1786,
            7143,
            'PHP',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            NULL
        );
        RAISE EXCEPTION 'unknown entitlement code was not rejected';
    EXCEPTION WHEN foreign_key_violation OR check_violation THEN
        NULL;
    END;

    BEGIN
        EXECUTE format(
            base_sql,
            '41000000-0000-4000-8000-000000000605',
            '41000000-0000-4000-8000-000000000405',
            '41000000-0000-4000-8000-000000000741',
            '41000000-0000-4000-8000-000000000742',
            '41000000-0000-4000-8000-000000000743',
            '41000000-0000-4000-8000-000000000744',
            '41000000-0000-4000-8000-000000000541',
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '99999999-9999-4999-8999-999999999998',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0005',
            '41000000-0000-4000-8000-000000000941',
            '41000000-0000-4000-8000-000000000942',
            10000,
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            1786,
            7143,
            'PHP',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            NULL
        );
        RAISE EXCEPTION 'unknown benefit code was not rejected';
    EXCEPTION WHEN foreign_key_violation OR check_violation THEN
        NULL;
    END;

    BEGIN
        EXECUTE format(
            base_sql,
            '41000000-0000-4000-8000-000000000606',
            '41000000-0000-4000-8000-000000000406',
            '41000000-0000-4000-8000-000000000751',
            '41000000-0000-4000-8000-000000000752',
            '41000000-0000-4000-8000-000000000753',
            '41000000-0000-4000-8000-000000000754',
            '41000000-0000-4000-8000-000000000551',
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0006',
            '41000000-0000-4000-8000-000000000951',
            '41000000-0000-4000-8000-000000000952',
            10000,
            '99999999-9999-4999-8999-999999999997',
            1786,
            7143,
            'PHP',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            NULL
        );
        RAISE EXCEPTION 'unknown VAT treatment code was not rejected';
    EXCEPTION WHEN foreign_key_violation OR check_violation THEN
        NULL;
    END;

    BEGIN
        EXECUTE format(
            base_sql,
            '41000000-0000-4000-8000-000000000607',
            '41000000-0000-4000-8000-000000000407',
            '41000000-0000-4000-8000-000000000761',
            '41000000-0000-4000-8000-000000000762',
            '41000000-0000-4000-8000-000000000763',
            '41000000-0000-4000-8000-000000000764',
            '41000000-0000-4000-8000-000000000561',
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0007',
            '41000000-0000-4000-8000-000000000961',
            '41000000-0000-4000-8000-000000000962',
            10000,
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            1786,
            7143,
            'php',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            NULL
        );
        RAISE EXCEPTION 'invalid currency was not rejected';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;

    BEGIN
        EXECUTE format(
            base_sql,
            '41000000-0000-4000-8000-000000000608',
            '41000000-0000-4000-8000-000000000408',
            '41000000-0000-4000-8000-000000000771',
            '41000000-0000-4000-8000-000000000772',
            '41000000-0000-4000-8000-000000000773',
            '41000000-0000-4000-8000-000000000774',
            '41000000-0000-4000-8000-000000000571',
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0008',
            '41000000-0000-4000-8000-000000000971',
            '41000000-0000-4000-8000-000000000972',
            -1,
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            1786,
            7143,
            'PHP',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            NULL
        );
        RAISE EXCEPTION 'negative amount was not rejected';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;
END;
$$;

INSERT INTO pos.fiscal_documents (
    fiscal_document_id,
    site_pos_server_id,
    fiscal_document_type_code_id,
    fiscal_document_status_code_id,
    central_pms_parking_session_ref,
    central_pms_payment_attempt_ref,
    central_pms_payment_confirmation_ref,
    payment_finality_ref,
    business_day_date
) VALUES (
    '41000000-0000-4000-8000-000000000409',
    '41000000-0000-4000-8000-000000000301',
    '41000000-0000-4000-8000-000000000201',
    '41000000-0000-4000-8000-000000000202',
    '41000000-0000-4000-8000-000000000581',
    '41000000-0000-4000-8000-000000000582',
    '41000000-0000-4000-8000-000000000583',
    'SCHEMA-PROOF-FINALITY-STATUTORY-0008',
    '2026-07-30'
);

DO $$
BEGIN
    BEGIN
        INSERT INTO pos.fiscal_document_applied_statutory_facts (
            fiscal_document_applied_statutory_fact_id,
            fiscal_document_id,
            statutory_discount_decision_command_id,
            statutory_request_reference,
            statutory_payable_basis_application_command_id,
            statutory_validation_id,
            parking_session_id,
            site_id,
            site_group_id,
            entitlement_type_code_id,
            benefit_classification_code_id,
            policy_resolution_basis_code_id,
            policy_code,
            original_tariff_snapshot_id,
            applied_tariff_snapshot_id,
            original_amount_minor_units,
            vat_exclusive_basis_amount_minor_units,
            vat_amount_minor_units,
            vat_treatment_code_id,
            statutory_discount_amount_minor_units,
            final_payable_amount_minor_units,
            currency_code,
            applied_at,
            source_payment_channel_code_id,
            terminal_cash_tender_id,
            snapshot_created_at
        ) VALUES (
            '41000000-0000-4000-8000-000000000609',
            '41000000-0000-4000-8000-000000000409',
            '41000000-0000-4000-8000-000000000781',
            '41000000-0000-4000-8000-000000000782',
            '41000000-0000-4000-8000-000000000783',
            '41000000-0000-4000-8000-000000000784',
            '41000000-0000-4000-8000-000000000581',
            '41000000-0000-4000-8000-000000000801',
            '41000000-0000-4000-8000-000000000802',
            '3df74017-1768-5285-9abe-f980cd1a4b65',
            '0e2eaf44-7921-5b38-8cdf-44a2cfed3dc0',
            '3b1b29ff-eeb5-584f-b8f0-71859ae0994a',
            'TEST-SAFE-POLICY-CODE-0009',
            '41000000-0000-4000-8000-000000000981',
            '41000000-0000-4000-8000-000000000982',
            10000,
            8929,
            0,
            '17ff69f6-32ee-5aee-9cfb-5910f7d4512c',
            9000,
            2000,
            'PHP',
            '2026-07-30T08:00:00Z',
            '6fa7cf8e-35c1-55cd-b1e9-e93d886c62ab',
            '41000000-0000-4000-8000-000000000785',
            '2026-07-30T08:00:01Z'
        );
        RAISE EXCEPTION 'contradictory monetary combination was not rejected';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;
END;
$$;

DO $$
BEGIN
    BEGIN
        UPDATE pos.fiscal_document_applied_statutory_facts
        SET final_payable_amount_minor_units = final_payable_amount_minor_units
        WHERE fiscal_document_applied_statutory_fact_id = '41000000-0000-4000-8000-000000000601';
        RAISE EXCEPTION 'immutable update was not rejected';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;

    BEGIN
        DELETE FROM pos.fiscal_document_applied_statutory_facts
        WHERE fiscal_document_applied_statutory_fact_id = '41000000-0000-4000-8000-000000000601';
        RAISE EXCEPTION 'immutable delete was not rejected';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;

    BEGIN
        DELETE FROM pos.fiscal_documents
        WHERE fiscal_document_id = '41000000-0000-4000-8000-000000000402';
        RAISE EXCEPTION 'parent delete with statutory snapshot was not rejected';
    EXCEPTION WHEN foreign_key_violation THEN
        NULL;
    END;
END;
$$;

SAVEPOINT rollback_probe;

INSERT INTO pos.fiscal_documents (
    fiscal_document_id,
    site_pos_server_id,
    fiscal_document_type_code_id,
    fiscal_document_status_code_id,
    central_pms_parking_session_ref,
    central_pms_payment_attempt_ref,
    central_pms_payment_confirmation_ref,
    payment_finality_ref,
    business_day_date
) VALUES (
    '41000000-0000-4000-8000-000000000410',
    '41000000-0000-4000-8000-000000000301',
    '41000000-0000-4000-8000-000000000201',
    '41000000-0000-4000-8000-000000000202',
    '41000000-0000-4000-8000-000000000591',
    '41000000-0000-4000-8000-000000000592',
    '41000000-0000-4000-8000-000000000593',
    'SCHEMA-PROOF-FINALITY-STATUTORY-0009',
    '2026-07-30'
);

INSERT INTO pos.fiscal_document_applied_statutory_facts (
    fiscal_document_applied_statutory_fact_id,
    fiscal_document_id,
    statutory_discount_decision_command_id,
    statutory_request_reference,
    statutory_payable_basis_application_command_id,
    statutory_validation_id,
    parking_session_id,
    site_id,
    site_group_id,
    entitlement_type_code_id,
    benefit_classification_code_id,
    policy_resolution_basis_code_id,
    applied_policy_reference_id,
    policy_code,
    policy_version_id,
    national_law_reference,
    ordinance_reference,
    original_tariff_snapshot_id,
    applied_tariff_snapshot_id,
    original_amount_minor_units,
    vat_exclusive_basis_amount_minor_units,
    vat_amount_minor_units,
    vat_treatment_code_id,
    statutory_discount_amount_minor_units,
    final_payable_amount_minor_units,
    currency_code,
    applied_at,
    source_payment_channel_code_id,
    terminal_cash_tender_id,
    snapshot_created_at
) VALUES (
    '41000000-0000-4000-8000-000000000610',
    '41000000-0000-4000-8000-000000000410',
    '41000000-0000-4000-8000-000000000791',
    '41000000-0000-4000-8000-000000000792',
    '41000000-0000-4000-8000-000000000793',
    '41000000-0000-4000-8000-000000000794',
    '41000000-0000-4000-8000-000000000591',
    '41000000-0000-4000-8000-000000000801',
    '41000000-0000-4000-8000-000000000802',
    '8ed32ee5-a7f3-58df-9da4-57c93568bcfa',
    'f232a486-a5b7-5f66-aa59-87dc65b3ab4c',
    'd4ecda73-025e-5edd-b784-1ec6aba649a8',
    '41000000-0000-4000-8000-000000000795',
    'TEST-SAFE-POLICY-CODE-0010',
    '41000000-0000-4000-8000-000000000796',
    'TEST-NATIONAL-LAW-REFERENCE',
    'TEST-ORDINANCE-REFERENCE',
    '41000000-0000-4000-8000-000000000991',
    '41000000-0000-4000-8000-000000000992',
    7500,
    6696,
    804,
    '51ea6aa5-78ba-5bff-a556-dbf4ca7a7968',
    7500,
    0,
    'PHP',
    '2026-07-30T08:00:00Z',
    '9139096f-715b-59ae-958c-48984d77d343',
    '41000000-0000-4000-8000-000000000797',
    '2026-07-30T08:00:01Z'
);

ROLLBACK TO SAVEPOINT rollback_probe;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pos.fiscal_document_applied_statutory_facts
        WHERE fiscal_document_applied_statutory_fact_id = '41000000-0000-4000-8000-000000000610'
    ) THEN
        RAISE EXCEPTION 'rollback left a partial statutory row';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'pos'
          AND table_name = 'fiscal_document_applied_statutory_facts'
          AND (
              data_type IN ('json', 'jsonb', 'xml')
              OR column_name ILIKE '%metadata%'
              OR column_name ILIKE '%payload%'
              OR column_name ILIKE '%request_body%'
              OR column_name ILIKE '%evidence%'
              OR column_name ILIKE '%beneficiary%'
              OR column_name ILIKE '%reviewer%'
              OR column_name ILIKE '%credential%'
              OR column_name ILIKE '%authorization%'
              OR column_name ILIKE '%password%'
              OR column_name ILIKE '%secret%'
          )
    ) THEN
        RAISE EXCEPTION 'prohibited statutory storage column exists';
    END IF;

    IF EXISTS (
        SELECT expected.constraint_name
        FROM unnest(ARRAY[
            'pk_fiscal_doc_applied_stat_facts',
            'fk_fiscal_doc_applied_stat_facts__document',
            'fk_fiscal_doc_applied_stat_facts__entitlement',
            'fk_fiscal_doc_applied_stat_facts__benefit',
            'fk_fiscal_doc_applied_stat_facts__policy_basis',
            'fk_fiscal_doc_applied_stat_facts__vat_treatment',
            'fk_fiscal_doc_applied_stat_facts__source_channel',
            'uq_fiscal_doc_applied_stat_facts__document',
            'uq_fiscal_doc_applied_stat_facts__decision',
            'uq_fiscal_doc_applied_stat_facts__request_ref',
            'uq_fiscal_doc_applied_stat_facts__application',
            'ck_fiscal_doc_applied_stat_facts__policy_code',
            'ck_fiscal_doc_applied_stat_facts__national_law',
            'ck_fiscal_doc_applied_stat_facts__ordinance_ref',
            'ck_fiscal_doc_applied_stat_facts__policy_ref',
            'ck_fiscal_doc_applied_stat_facts__entitlement_code',
            'ck_fiscal_doc_applied_stat_facts__benefit_code',
            'ck_fiscal_doc_applied_stat_facts__policy_basis_code',
            'ck_fiscal_doc_applied_stat_facts__vat_code',
            'ck_fiscal_doc_applied_stat_facts__source_channel_code',
            'ck_fiscal_doc_applied_stat_facts__tariff_refs',
            'ck_fiscal_doc_applied_stat_facts__amounts',
            'ck_fiscal_doc_applied_stat_facts__final_amount',
            'ck_fiscal_doc_applied_stat_facts__currency',
            'ck_fiscal_doc_applied_stat_facts__snapshot_time',
            'ck_fiscal_doc_applied_stat_facts__audit_time'
        ]) AS expected(constraint_name)
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.table_constraints actual
            WHERE actual.table_schema = 'pos'
              AND actual.table_name = 'fiscal_document_applied_statutory_facts'
              AND actual.constraint_name = expected.constraint_name
        )
    ) THEN
        RAISE EXCEPTION 'expected statutory constraint is missing';
    END IF;

    IF EXISTS (
        SELECT expected.index_name
        FROM unnest(ARRAY[
            'pk_fiscal_doc_applied_stat_facts',
            'uq_fiscal_doc_applied_stat_facts__document',
            'uq_fiscal_doc_applied_stat_facts__decision',
            'uq_fiscal_doc_applied_stat_facts__request_ref',
            'uq_fiscal_doc_applied_stat_facts__application',
            'ix_fiscal_doc_applied_stat_facts__validation',
            'ix_fiscal_doc_applied_stat_facts__parking_session',
            'ix_fiscal_doc_applied_stat_facts__site',
            'ix_fiscal_doc_applied_stat_facts__site_group',
            'ix_fiscal_doc_applied_stat_facts__entitlement',
            'ix_fiscal_doc_applied_stat_facts__benefit',
            'ix_fiscal_doc_applied_stat_facts__policy_ref',
            'ix_fiscal_doc_applied_stat_facts__policy_code',
            'ix_fiscal_doc_applied_stat_facts__applied_tariff',
            'ix_fiscal_doc_applied_stat_facts__vat_treatment',
            'ix_fiscal_doc_applied_stat_facts__source_channel',
            'ix_fiscal_doc_applied_stat_facts__terminal_cash'
        ]) AS expected(index_name)
        WHERE NOT EXISTS (
            SELECT 1
            FROM pg_indexes actual
            WHERE actual.schemaname = 'pos'
              AND actual.tablename = 'fiscal_document_applied_statutory_facts'
              AND actual.indexname = expected.index_name
        )
    ) THEN
        RAISE EXCEPTION 'expected statutory index is missing';
    END IF;
END;
$$;

SELECT 'schema-proof-table-count' AS proof, count(*)::text AS value
FROM information_schema.tables
WHERE table_schema = 'pos'
  AND table_name = 'fiscal_document_applied_statutory_facts';

SELECT 'schema-proof-index-count' AS proof, count(*)::text AS value
FROM pg_indexes
WHERE schemaname = 'pos'
  AND tablename = 'fiscal_document_applied_statutory_facts';

SELECT 'schema-proof-constraint-count' AS proof, count(*)::text AS value
FROM information_schema.table_constraints
WHERE table_schema = 'pos'
  AND table_name = 'fiscal_document_applied_statutory_facts';

COMMIT;
