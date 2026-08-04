-- POS Server fiscal-reporting contract/schema proof fixture.
-- Run only against a disposable PostgreSQL database after rebuild and controlled-code load.
-- All identifiers and registration values are deterministic non-production test data.

\pset format unaligned
\pset tuples_only on

BEGIN;

INSERT INTO pos.controlled_code_sets (
    controlled_code_set_id, code_set_key, display_name, description
) VALUES
    ('46000000-0000-4000-8000-000000000101', 'reporting_proof_sequence_family', 'Reporting Proof Sequence Family', 'Disposable sequence family.'),
    ('46000000-0000-4000-8000-000000000102', 'reporting_proof_policy_status', 'Reporting Proof Policy Status', 'Disposable policy status.')
ON CONFLICT (controlled_code_set_id) DO NOTHING;

INSERT INTO pos.controlled_codes (
    controlled_code_id, controlled_code_set_id, code_key, display_name, description
) VALUES
    ('46000000-0000-4000-8000-000000000201', '46000000-0000-4000-8000-000000000101', 'reporting_proof_si', 'Reporting Proof SI', 'Disposable SI sequence family.'),
    ('46000000-0000-4000-8000-000000000202', '46000000-0000-4000-8000-000000000102', 'reporting_proof_active', 'Reporting Proof Active', 'Disposable active status.')
ON CONFLICT (controlled_code_id) DO NOTHING;

INSERT INTO pos.site_pos_servers (
    site_pos_server_id, site_pos_server_code, display_name, central_pms_site_ref,
    reporting_timezone_name, business_day_cutoff_local_time
) VALUES (
    '46000000-0000-4000-8000-000000000301', 'REPORTING-PROOF-POS-01',
    'REPORTING PROOF POS SERVER', 'REPORTING-PROOF-SITE-01', 'Etc/UTC', '00:00:00'
);

INSERT INTO pos.fiscal_identities (
    fiscal_identity_id, fiscal_identity_code, registered_business_name,
    registered_business_address, tin, fiscal_identity_status, created_by_ref, updated_by_ref
) VALUES (
    '46000000-0000-4000-8000-000000000302', 'REPORTING-PROOF-FISCAL-01',
    'GOVERNED REPORTING TEST BUSINESS', 'GOVERNED REPORTING TEST ADDRESS',
    'TEST-TIN-REPORTING-0001', 'APPROVED', 'Z-006A-PROOF', 'Z-006A-PROOF'
);

INSERT INTO pos.sales_invoice_header_profiles (
    sales_invoice_header_profile_id, fiscal_identity_id, site_id, site_pos_server_id,
    profile_version, template_version, presentation_version, pos_serial_number,
    machine_identification_number, parking_location_display, bir_accreditation_number,
    bir_accreditation_issued_date, bir_accreditation_valid_until, ptu_number, ptu_issued_date,
    sales_invoice_legal_statement, customer_service_footer, effective_from, effective_to,
    lifecycle_status, approved_at, approved_by_ref, created_by_ref, updated_by_ref
) VALUES (
    '46000000-0000-4000-8000-000000000303',
    '46000000-0000-4000-8000-000000000302',
    '46000000-0000-4000-8000-000000000304',
    '46000000-0000-4000-8000-000000000301',
    'REPORTING-PROOF-V1', 'digital-sales-invoice-json-v1',
    'digital-sales-invoice-presentation-json-v1', 'TEST-SERIAL-REPORTING-0001',
    'TEST-MIN-REPORTING-0001', 'REPORTING PROOF PARKING LOCATION',
    'TEST-BIR-ACCREDITATION-REPORTING-0001', '2026-01-01', '2027-01-01',
    'TEST-PTU-REPORTING-0001', '2026-01-02', 'TEST LEGAL STATEMENT',
    'TEST CUSTOMER SERVICE FOOTER', '2026-01-01T00:00:00Z', NULL,
    'APPROVED', '2026-01-01T00:00:00Z', 'Z-006A-PROOF', 'Z-006A-PROOF', 'Z-006A-PROOF'
);

INSERT INTO pos.fiscal_sequence_policies (
    fiscal_sequence_policy_id, site_pos_server_id, sequence_family_code_id,
    policy_code, display_name, current_policy_status_code_id, effective_start_at
) VALUES (
    '46000000-0000-4000-8000-000000000401',
    '46000000-0000-4000-8000-000000000301',
    '46000000-0000-4000-8000-000000000201', 'REPORTING-PROOF-SI-POLICY',
    'Reporting Proof SI Policy', '46000000-0000-4000-8000-000000000202',
    '2026-01-01T00:00:00Z'
);

INSERT INTO pos.fiscal_reporting_periods (
    fiscal_reporting_period_id, fiscal_reporting_contract_version_id, site_pos_server_id,
    fiscal_identity_id, period_status_code_id, business_day_date, period_start_at,
    period_end_at, reporting_timezone_name, business_day_cutoff_local_time, currency_code,
    period_sequence, opened_at, closing_started_at, closed_at, created_by_ref, updated_by_ref
) VALUES (
    '46000000-0000-4000-8000-000000000501',
    'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
    '46000000-0000-4000-8000-000000000301',
    '46000000-0000-4000-8000-000000000302',
    'af7ee931-a023-507e-81a4-17adf047eb94', '2026-08-01',
    '2026-08-01T00:00:00Z', '2026-08-02T00:00:00Z', 'Etc/UTC', '00:00:00',
    'PHP', 1, '2026-08-01T00:00:00Z', '2026-08-02T00:00:01Z',
    '2026-08-02T00:00:02Z', 'Z-006A-PROOF', 'Z-006A-PROOF'
);

INSERT INTO pos.fiscal_report_requests (
    fiscal_report_request_id, fiscal_reporting_contract_version_id, fiscal_reporting_period_id,
    site_pos_server_id, report_type_code_id, report_status_code_id,
    operation_idempotency_key, semantic_request_hash, semantic_hash_version,
    business_day_date, requested_at, requested_by_ref, service_identity_ref
) VALUES
    ('46000000-0000-4000-8000-000000000601', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-X-0001', repeat('a', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000602', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '1c628bc2-49c3-53e8-ae83-2082bcf28467', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-Z-0001', repeat('b', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000603', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '2326447f-74c2-5ed7-83bb-e079fcee7f3d', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-BIR-0001', repeat('c', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000604', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', 'd4c4615e-2cf2-59b7-a6d2-97d22210114c', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-ANNEX-0001', repeat('d', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000605', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '1c628bc2-49c3-53e8-ae83-2082bcf28467', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-Z-0002', repeat('e', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000606', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '2326447f-74c2-5ed7-83bb-e079fcee7f3d', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-BIR-0002', repeat('f', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000608', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-X-0002', repeat('1', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000609', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '2326447f-74c2-5ed7-83bb-e079fcee7f3d', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-BIR-0003', repeat('2', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000610', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'REPORTING-PROOF-X-NEGATIVE', repeat('3', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-01', '2026-08-02T00:00:03Z', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE');

INSERT INTO pos.fiscal_z_close_states (
    fiscal_z_close_state_id, site_pos_server_id, fiscal_identity_id, currency_code,
    fiscal_reporting_contract_version_id, reset_counter_value, z_counter_value,
    grand_total_amount_minor_units, state_version, initialization_provenance_code_id,
    initialized_at, initialized_by_ref, initialization_service_ref, initialization_approval_ref,
    last_transition_operation_ref, last_transition_at
) VALUES (
    '46000000-0000-4000-8000-000000000620',
    '46000000-0000-4000-8000-000000000301',
    '46000000-0000-4000-8000-000000000302', 'PHP',
    'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', 0, 0, 100000, 1,
    '9ce30367-0ff0-5c5e-8d48-b4a3b3b9df71', '2026-08-01T00:00:00Z',
    'Z-007B-PROOF', 'Z-007B-PROOF-SERVICE', 'Z-007B-APPROVAL',
    'REPORTING-PROOF-STATE-INIT', '2026-08-01T00:00:00Z'
);

INSERT INTO pos.fiscal_z_close_state_transitions (
    fiscal_z_close_state_transition_id, fiscal_z_close_state_id,
    fiscal_reporting_contract_version_id, site_pos_server_id, fiscal_identity_id, currency_code,
    transition_type_code_id, initialization_provenance_code_id, operation_ref,
    semantic_request_hash, semantic_hash_version, expected_state_version, resulting_state_version,
    approval_ref, actor_ref, service_identity_ref, correlation_ref, committed_at
) VALUES (
    '46000000-0000-4000-8000-000000000621', '46000000-0000-4000-8000-000000000620',
    'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000301',
    '46000000-0000-4000-8000-000000000302', 'PHP',
    '21880ca5-b803-5abc-959f-def2f232ad73', '9ce30367-0ff0-5c5e-8d48-b4a3b3b9df71',
    'REPORTING-PROOF-STATE-INIT', repeat('4', 64),
    'pos-server-fiscal-z-close-state-initialize:sha256:v1', 0, 1,
    'Z-007B-APPROVAL', 'Z-007B-PROOF', 'Z-007B-PROOF-SERVICE',
    'Z-007B-PROOF-CORRELATION', '2026-08-01T00:00:00Z'
);

INSERT INTO pos.fiscal_z_close_state_transition_values (
    fiscal_z_close_state_transition_value_id, fiscal_z_close_state_transition_id,
    state_identity_code_id, previous_counter_value, resulting_counter_value,
    previous_amount_minor_units, resulting_amount_minor_units, currency_code
) VALUES
    ('46000000-0000-4000-8000-000000000622', '46000000-0000-4000-8000-000000000621',
     'baf7bb67-9f11-5b2f-a750-e5689a5f0142', NULL, 0, NULL, NULL, NULL),
    ('46000000-0000-4000-8000-000000000623', '46000000-0000-4000-8000-000000000621',
     'a1dddcd8-aab5-51a8-bb64-f610edbdc1eb', NULL, 0, NULL, NULL, NULL),
    ('46000000-0000-4000-8000-000000000624', '46000000-0000-4000-8000-000000000621',
     'e01e75a6-6ee9-5bdf-9bf4-0a38476f8805', NULL, NULL, NULL, 100000, 'PHP');

INSERT INTO pos.fiscal_report_scopes (
    fiscal_report_scope_id, fiscal_report_request_id, fiscal_reporting_period_id, scope_type_code_id, fiscal_series
) VALUES (
    '46000000-0000-4000-8000-000000000650',
    '46000000-0000-4000-8000-000000000601',
    '46000000-0000-4000-8000-000000000501',
    'ce034742-16fe-578c-85af-a05e62129f21', 'SI-TEST'
);

INSERT INTO pos.x_z_reports (
    x_z_report_id, fiscal_report_request_id, fiscal_reporting_contract_version_id,
    fiscal_reporting_period_id, report_kind_code_id, site_pos_server_id, fiscal_identity_id,
    report_number, business_day_date, period_start_at, period_end_at, reporting_timezone_name,
    business_day_cutoff_local_time, transaction_count, first_fiscal_sequence_value,
    last_fiscal_sequence_value, fiscal_sequence_gap_count, reset_counter_value, z_counter_value,
    previous_grand_total_amount_minor_units, current_grand_total_amount_minor_units,
    present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
    net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
    vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
    discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
    pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
    vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
    promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
    return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
    currency_code, generated_at, committed_at
) VALUES
    ('46000000-0000-4000-8000-000000000701', '46000000-0000-4000-8000-000000000601', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302', 'X-REPORTING-PROOF-0001', '2026-08-01', '2026-08-01T00:00:00Z', '2026-08-02T00:00:00Z', 'Etc/UTC', '00:00:00', 2, 100, 102, 1, NULL, NULL, 100000, 20000, 120000, 20000, 17857, 10000, 1071, 7143, 0, 2143, 1786, 0, 0, 1071, 357, 0, 0, 0, 0, 0, 0, 'PHP', '2026-08-02T00:00:03Z', '2026-08-02T00:00:04Z'),
    ('46000000-0000-4000-8000-000000000702', '46000000-0000-4000-8000-000000000602', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501', '1c628bc2-49c3-53e8-ae83-2082bcf28467', '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302', 'Z-REPORTING-PROOF-0001', '2026-08-01', '2026-08-01T00:00:00Z', '2026-08-02T00:00:00Z', 'Etc/UTC', '00:00:00', 2, 100, 102, 1, 1, 1, 100000, 20000, 120000, 20000, 17857, 10000, 1071, 7143, 0, 2143, 1786, 0, 0, 1071, 357, 0, 0, 0, 0, 0, 0, 'PHP', '2026-08-02T00:00:03Z', '2026-08-02T00:00:04Z');

INSERT INTO pos.fiscal_report_tender_breakdowns (
    fiscal_report_tender_breakdown_id, x_z_report_id, tender_classification_code_id,
    tender_transaction_count, amount_minor_units, currency_code
) VALUES (
    '46000000-0000-4000-8000-000000000711', '46000000-0000-4000-8000-000000000702',
    '47a97fd9-987d-5ea2-a8d0-d293343bf7f6', 2, 17857, 'PHP'
);

INSERT INTO pos.fiscal_report_discount_breakdowns (
    fiscal_report_discount_breakdown_id, x_z_report_id, discount_classification_code_id,
    qualifying_document_count, discount_amount_minor_units, vat_exemption_amount_minor_units, currency_code
) VALUES (
    '46000000-0000-4000-8000-000000000712', '46000000-0000-4000-8000-000000000702',
    '6fc90db9-12d4-509f-948e-04beaa7de371', 1, 1786, 1071, 'PHP'
);

INSERT INTO pos.fiscal_report_fiscal_number_ranges (
    fiscal_report_fiscal_number_range_id, x_z_report_id, fiscal_identity_id,
    fiscal_sequence_policy_id, fiscal_series, first_sequence_value, last_sequence_value,
    first_fiscal_number, last_fiscal_number, qualifying_document_count, gap_count, currency_code
) VALUES (
    '46000000-0000-4000-8000-000000000713', '46000000-0000-4000-8000-000000000702',
    '46000000-0000-4000-8000-000000000302', '46000000-0000-4000-8000-000000000401',
    'SI-TEST', 100, 102, 'SI-TEST-000100', 'SI-TEST-000102', 2, 1, 'PHP'
);

INSERT INTO pos.fiscal_report_sequence_gaps (
    fiscal_report_sequence_gap_id, fiscal_report_fiscal_number_range_id,
    gap_sequence_value, gap_classification_code_id
) VALUES (
    '46000000-0000-4000-8000-000000000714', '46000000-0000-4000-8000-000000000713',
    101, 'cb7502b1-83bf-5d08-91ef-104cdb4a8f34'
);

DO $$
BEGIN
    BEGIN
        INSERT INTO pos.fiscal_z_counter_snapshots (
            fiscal_z_counter_snapshot_id, fiscal_z_close_state_id, x_z_report_id, report_kind_code_id,
            expected_state_version, resulting_state_version,
            previous_reset_counter_value, resulting_reset_counter_value,
            previous_z_counter_value, resulting_z_counter_value,
            previous_grand_total_amount_minor_units, current_period_amount_minor_units,
            resulting_grand_total_amount_minor_units, currency_code
        ) VALUES (
            '46000000-0000-4000-8000-000000000719', '46000000-0000-4000-8000-000000000620',
            '46000000-0000-4000-8000-000000000702',
            '1c628bc2-49c3-53e8-ae83-2082bcf28467', 1, 2, 0, 0, 0, 2, 100000, 20000, 120000, 'PHP'
        );
        RAISE EXCEPTION 'invalid Z counter relationship was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END;
$$;

INSERT INTO pos.fiscal_z_counter_snapshots (
    fiscal_z_counter_snapshot_id, fiscal_z_close_state_id, x_z_report_id, report_kind_code_id,
    expected_state_version, resulting_state_version,
    previous_reset_counter_value, resulting_reset_counter_value,
    previous_z_counter_value, resulting_z_counter_value,
    previous_grand_total_amount_minor_units, current_period_amount_minor_units,
    resulting_grand_total_amount_minor_units, currency_code
) VALUES (
    '46000000-0000-4000-8000-000000000715', '46000000-0000-4000-8000-000000000620',
    '46000000-0000-4000-8000-000000000702',
    '1c628bc2-49c3-53e8-ae83-2082bcf28467', 1, 2, 0, 0, 0, 1, 100000, 20000, 120000, 'PHP'
);

INSERT INTO pos.bir_sales_summary_reports (
    bir_sales_summary_report_id, fiscal_report_request_id, report_kind_code_id,
    fiscal_reporting_contract_version_id, fiscal_reporting_period_id, governing_z_report_id,
    governing_report_kind_code_id, site_pos_server_id, fiscal_identity_id,
    reporting_contract_profile_ref, sales_invoice_header_profile_id, header_profile_version,
    pos_serial_number, machine_identification_number, bir_accreditation_number,
    bir_accreditation_issued_date, bir_accreditation_valid_until, ptu_number, ptu_issued_date,
    business_day_date, reporting_period_start_date, reporting_period_end_date, transaction_count,
    beginning_si_ref, ending_si_ref, previous_grand_total_amount_minor_units,
    present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
    net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
    vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
    discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
    pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
    vat_exemption_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
    return_amount_minor_units, adjustment_amount_minor_units, reset_counter_value, z_counter_value,
    currency_code, generated_at, committed_at
) VALUES (
    '46000000-0000-4000-8000-000000000721', '46000000-0000-4000-8000-000000000603',
    '2326447f-74c2-5ed7-83bb-e079fcee7f3d', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
    '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000702',
    '1c628bc2-49c3-53e8-ae83-2082bcf28467', '46000000-0000-4000-8000-000000000301',
    '46000000-0000-4000-8000-000000000302', 'BIR-SUMMARY-INTERNAL-PROFILE-V1',
    '46000000-0000-4000-8000-000000000303', 'REPORTING-PROOF-V1',
    'TEST-SERIAL-REPORTING-0001', 'TEST-MIN-REPORTING-0001',
    'TEST-BIR-ACCREDITATION-REPORTING-0001', '2026-01-01', '2027-01-01',
    'TEST-PTU-REPORTING-0001', '2026-01-02', '2026-08-01', '2026-08-01', '2026-08-01',
    2, 'SI-TEST-000100', 'SI-TEST-000102', 100000, 120000, 20000, 17857, 10000,
    1071, 7143, 0, 2143, 1786, 0, 0, 1071, 0, 0, 0, 0, 1, 1, 'PHP',
    '2026-08-02T00:00:05Z', '2026-08-02T00:00:06Z'
);

INSERT INTO pos.annex_e_reports (
    annex_e_report_id, fiscal_report_request_id, report_kind_code_id, report_status_code_id,
    fiscal_reporting_contract_version_id, fiscal_reporting_period_id, governing_z_report_id,
    governing_report_kind_code_id, site_pos_server_id, fiscal_identity_id,
    annex_e_contract_profile_ref, business_day_date, reporting_period_start_date,
    reporting_period_end_date, related_bir_sales_summary_report_id, generated_at, committed_at
) VALUES (
    '46000000-0000-4000-8000-000000000722', '46000000-0000-4000-8000-000000000604',
    'd4c4615e-2cf2-59b7-a6d2-97d22210114c', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04',
    'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000501',
    '46000000-0000-4000-8000-000000000702', '1c628bc2-49c3-53e8-ae83-2082bcf28467',
    '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302',
    'ANNEX-E-INTERNAL-PROFILE-V1', '2026-08-01', '2026-08-01', '2026-08-01',
    '46000000-0000-4000-8000-000000000721', '2026-08-02T00:00:07Z', '2026-08-02T00:00:08Z'
);

DO $$
BEGIN
    BEGIN
        INSERT INTO pos.fiscal_report_requests (
            fiscal_report_request_id, fiscal_reporting_contract_version_id, fiscal_reporting_period_id,
            site_pos_server_id, report_type_code_id, report_status_code_id, operation_idempotency_key,
            semantic_request_hash, semantic_hash_version, business_day_date, requested_by_ref, service_identity_ref
        ) VALUES (
            '46000000-0000-4000-8000-000000000620', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
            '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301',
            '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04',
            'REPORTING-PROOF-X-0001', repeat('9', 64), 'pos-server-fiscal-report-request:sha256:v1',
            '2026-08-01', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'
        );
        RAISE EXCEPTION 'duplicate report operation identity was not rejected';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_report_requests (
            fiscal_report_request_id, fiscal_reporting_contract_version_id, fiscal_reporting_period_id,
            site_pos_server_id, report_type_code_id, report_status_code_id, operation_idempotency_key,
            semantic_request_hash, semantic_hash_version, business_day_date, requested_by_ref, service_identity_ref
        ) VALUES (
            '46000000-0000-4000-8000-000000000621', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
            '46000000-0000-4000-8000-000000000501', '46000000-0000-4000-8000-000000000301',
            '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04',
            'REPORTING-PROOF-WRONG-KIND', repeat('8', 64), 'pos-server-fiscal-report-request:sha256:v1',
            '2026-08-01', 'Z-006A-PROOF', 'Z-006A-PROOF-SERVICE'
        );
        RAISE EXCEPTION 'wrong-family report kind was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_report_scopes (
            fiscal_report_scope_id, fiscal_report_request_id, fiscal_reporting_period_id, scope_type_code_id
        ) VALUES (
            '46000000-0000-4000-8000-000000000651', '46000000-0000-4000-8000-000000000608',
            '46000000-0000-4000-8000-000000000501', '5dc3cc94-b3ab-5582-a598-e779871fc3e2'
        );
        RAISE EXCEPTION 'wrong-family report scope was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_report_output_refs (
            fiscal_report_output_ref_id, fiscal_report_request_id, output_type_code_id, output_status_code_id
        ) VALUES (
            '46000000-0000-4000-8000-000000000731', '46000000-0000-4000-8000-000000000601',
            '47a97fd9-987d-5ea2-a8d0-d293343bf7f6', '94bcd4c0-f938-5efc-8b05-51b5f5f32e94'
        );
        RAISE EXCEPTION 'wrong-family output type was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_report_tender_breakdowns (
            fiscal_report_tender_breakdown_id, x_z_report_id, tender_classification_code_id,
            tender_transaction_count, amount_minor_units, currency_code
        ) VALUES (
            '46000000-0000-4000-8000-000000000732', '46000000-0000-4000-8000-000000000702',
            '6fc90db9-12d4-509f-948e-04beaa7de371', 1, 1, 'PHP'
        );
        RAISE EXCEPTION 'wrong-family tender classification was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_report_tender_breakdowns (
            fiscal_report_tender_breakdown_id, x_z_report_id, tender_classification_code_id,
            tender_transaction_count, amount_minor_units, currency_code
        ) VALUES (
            '46000000-0000-4000-8000-000000000733', '46000000-0000-4000-8000-000000000702',
            '1f429942-5bec-585b-ae4c-a5025ec59827', 1, 1, 'USD'
        );
        RAISE EXCEPTION 'mixed-currency tender row was not rejected';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_report_sequence_gaps (
            fiscal_report_sequence_gap_id, fiscal_report_fiscal_number_range_id,
            gap_sequence_value, gap_classification_code_id
        ) VALUES (
            '46000000-0000-4000-8000-000000000734', '46000000-0000-4000-8000-000000000713',
            102, '47a97fd9-987d-5ea2-a8d0-d293343bf7f6'
        );
        RAISE EXCEPTION 'wrong-family gap classification was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_report_fiscal_number_ranges (
            fiscal_report_fiscal_number_range_id, x_z_report_id, fiscal_identity_id,
            fiscal_sequence_policy_id, fiscal_series, first_sequence_value, last_sequence_value,
            first_fiscal_number, last_fiscal_number, qualifying_document_count, gap_count, currency_code
        ) VALUES (
            '46000000-0000-4000-8000-000000000735', '46000000-0000-4000-8000-000000000701',
            '46000000-0000-4000-8000-000000000302', '46000000-0000-4000-8000-000000000401',
            'SI-TEST-INVALID', 200, 100, 'SI-TEST-000200', 'SI-TEST-000100', 0, 0, 'PHP'
        );
        RAISE EXCEPTION 'invalid fiscal range ordering was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_reporting_periods (
            fiscal_reporting_period_id, fiscal_reporting_contract_version_id, site_pos_server_id,
            fiscal_identity_id, period_status_code_id, business_day_date, period_start_at, period_end_at,
            reporting_timezone_name, business_day_cutoff_local_time, currency_code, period_sequence,
            opened_at, created_by_ref, updated_by_ref
        ) VALUES (
            '46000000-0000-4000-8000-000000000520', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
            '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302',
            '1a6f7021-bc84-5c01-afaa-c5d6685633c8', '2026-08-02',
            '2026-08-03T00:00:00Z', '2026-08-03T00:00:00Z', 'Etc/UTC', '00:00:00', 'PHP', 2,
            '2026-08-03T00:00:00Z', 'Z-006A-PROOF', 'Z-006A-PROOF'
        );
        RAISE EXCEPTION 'invalid half-open period was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END;
$$;

DO $$
BEGIN
    BEGIN
        INSERT INTO pos.x_z_reports (
            x_z_report_id, fiscal_report_request_id, fiscal_reporting_contract_version_id,
            fiscal_reporting_period_id, report_kind_code_id, site_pos_server_id, fiscal_identity_id,
            report_number, business_day_date, period_start_at, period_end_at, reporting_timezone_name,
            business_day_cutoff_local_time, transaction_count, fiscal_sequence_gap_count,
            previous_grand_total_amount_minor_units, current_grand_total_amount_minor_units,
            present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
            net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
            vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
            promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
            currency_code, generated_at, committed_at
        )
        SELECT
            '46000000-0000-4000-8000-000000000740', fiscal_report_request_id,
            fiscal_reporting_contract_version_id, fiscal_reporting_period_id, report_kind_code_id,
            site_pos_server_id, fiscal_identity_id, 'X-REPORTING-PROOF-DUPLICATE', business_day_date,
            period_start_at, period_end_at, reporting_timezone_name, business_day_cutoff_local_time,
            transaction_count, fiscal_sequence_gap_count, previous_grand_total_amount_minor_units,
            current_grand_total_amount_minor_units, present_grand_total_amount_minor_units,
            gross_sales_amount_minor_units, net_sales_amount_minor_units, vatable_sales_amount_minor_units,
            vat_amount_minor_units, vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
            promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
            currency_code, generated_at, committed_at
        FROM pos.x_z_reports WHERE x_z_report_id = '46000000-0000-4000-8000-000000000701';
        RAISE EXCEPTION 'duplicate X request snapshot was not rejected';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.x_z_reports (
            x_z_report_id, fiscal_report_request_id, fiscal_reporting_contract_version_id,
            fiscal_reporting_period_id, report_kind_code_id, site_pos_server_id, fiscal_identity_id,
            report_number, business_day_date, period_start_at, period_end_at, reporting_timezone_name,
            business_day_cutoff_local_time, transaction_count, fiscal_sequence_gap_count,
            previous_grand_total_amount_minor_units, current_grand_total_amount_minor_units,
            present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
            net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
            vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
            promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
            currency_code, generated_at, committed_at
        )
        SELECT
            '46000000-0000-4000-8000-000000000741', '46000000-0000-4000-8000-000000000605',
            fiscal_reporting_contract_version_id, fiscal_reporting_period_id, report_kind_code_id,
            site_pos_server_id, fiscal_identity_id, 'Z-REPORTING-PROOF-0002', business_day_date,
            period_start_at, period_end_at, reporting_timezone_name, business_day_cutoff_local_time,
            transaction_count, fiscal_sequence_gap_count, previous_grand_total_amount_minor_units,
            current_grand_total_amount_minor_units, present_grand_total_amount_minor_units,
            gross_sales_amount_minor_units, net_sales_amount_minor_units, vatable_sales_amount_minor_units,
            vat_amount_minor_units, vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
            promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
            currency_code, generated_at, committed_at
        FROM pos.x_z_reports WHERE x_z_report_id = '46000000-0000-4000-8000-000000000702';
        RAISE EXCEPTION 'duplicate Z close identity was not rejected';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.x_z_reports (
            x_z_report_id, fiscal_report_request_id, fiscal_reporting_contract_version_id,
            fiscal_reporting_period_id, report_kind_code_id, site_pos_server_id, fiscal_identity_id,
            report_number, business_day_date, period_start_at, period_end_at, reporting_timezone_name,
            business_day_cutoff_local_time, transaction_count, fiscal_sequence_gap_count,
            previous_grand_total_amount_minor_units, current_grand_total_amount_minor_units,
            present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
            net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
            vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
            promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
            currency_code, generated_at, committed_at
        )
        SELECT
            '46000000-0000-4000-8000-000000000742', '46000000-0000-4000-8000-000000000610',
            fiscal_reporting_contract_version_id, fiscal_reporting_period_id, report_kind_code_id,
            site_pos_server_id, fiscal_identity_id, 'X-REPORTING-PROOF-NEGATIVE', business_day_date,
            period_start_at, period_end_at, reporting_timezone_name, business_day_cutoff_local_time,
            transaction_count, fiscal_sequence_gap_count, previous_grand_total_amount_minor_units,
            current_grand_total_amount_minor_units, present_grand_total_amount_minor_units,
            -1, net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
            vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
            promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
            currency_code, generated_at, committed_at
        FROM pos.x_z_reports WHERE x_z_report_id = '46000000-0000-4000-8000-000000000701';
        RAISE EXCEPTION 'negative report amount was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END;
$$;

DO $$
BEGIN
    BEGIN
        INSERT INTO pos.bir_sales_summary_reports
        SELECT
            '46000000-0000-4000-8000-000000000750',
            '46000000-0000-4000-8000-000000000609',
            report_kind_code_id, fiscal_reporting_contract_version_id, fiscal_reporting_period_id,
            '46000000-0000-4000-8000-000000000799', governing_report_kind_code_id,
            site_pos_server_id, fiscal_identity_id, 'BIR-SUMMARY-INTERNAL-PROFILE-MISSING-Z',
            sales_invoice_header_profile_id, header_profile_version, pos_serial_number,
            machine_identification_number, bir_accreditation_number, bir_accreditation_issued_date,
            bir_accreditation_valid_until, ptu_number, ptu_issued_date, business_day_date,
            reporting_period_start_date, reporting_period_end_date, transaction_count,
            beginning_si_ref, ending_si_ref, previous_grand_total_amount_minor_units,
            present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
            net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
            vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, reset_counter_value,
            z_counter_value, currency_code, generated_at, committed_at, created_at, updated_at
        FROM pos.bir_sales_summary_reports
        WHERE bir_sales_summary_report_id = '46000000-0000-4000-8000-000000000721';
        RAISE EXCEPTION 'BIR summary without governing Z was not rejected';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.bir_sales_summary_reports
        SELECT
            '46000000-0000-4000-8000-000000000751',
            '46000000-0000-4000-8000-000000000606',
            report_kind_code_id, fiscal_reporting_contract_version_id, fiscal_reporting_period_id,
            governing_z_report_id, governing_report_kind_code_id, site_pos_server_id,
            fiscal_identity_id, reporting_contract_profile_ref, sales_invoice_header_profile_id,
            header_profile_version, pos_serial_number, machine_identification_number,
            bir_accreditation_number, bir_accreditation_issued_date, bir_accreditation_valid_until,
            ptu_number, ptu_issued_date, business_day_date, reporting_period_start_date,
            reporting_period_end_date, transaction_count, beginning_si_ref, ending_si_ref,
            previous_grand_total_amount_minor_units, present_grand_total_amount_minor_units,
            gross_sales_amount_minor_units, net_sales_amount_minor_units,
            vatable_sales_amount_minor_units, vat_amount_minor_units,
            vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
            discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
            pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
            vat_exemption_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
            return_amount_minor_units, adjustment_amount_minor_units, reset_counter_value,
            z_counter_value, currency_code, generated_at, committed_at, created_at, updated_at
        FROM pos.bir_sales_summary_reports
        WHERE bir_sales_summary_report_id = '46000000-0000-4000-8000-000000000721';
        RAISE EXCEPTION 'duplicate BIR summary version for governing Z was not rejected';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;
END;
$$;

DO $$
BEGIN
    BEGIN
        UPDATE pos.x_z_reports SET report_number = report_number
        WHERE x_z_report_id = '46000000-0000-4000-8000-000000000702';
        RAISE EXCEPTION 'committed report update was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        DELETE FROM pos.fiscal_report_tender_breakdowns
        WHERE fiscal_report_tender_breakdown_id = '46000000-0000-4000-8000-000000000711';
        RAISE EXCEPTION 'committed tender child delete was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        UPDATE pos.bir_sales_summary_reports SET generated_at = generated_at
        WHERE bir_sales_summary_report_id = '46000000-0000-4000-8000-000000000721';
        RAISE EXCEPTION 'committed BIR summary update was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        DELETE FROM pos.fiscal_reporting_periods
        WHERE fiscal_reporting_period_id = '46000000-0000-4000-8000-000000000501';
        RAISE EXCEPTION 'closed period delete was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        DELETE FROM pos.x_z_reports
        WHERE x_z_report_id = '46000000-0000-4000-8000-000000000702';
        RAISE EXCEPTION 'committed report delete was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        DELETE FROM pos.bir_sales_summary_reports
        WHERE bir_sales_summary_report_id = '46000000-0000-4000-8000-000000000721';
        RAISE EXCEPTION 'committed BIR summary delete was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        UPDATE pos.annex_e_reports SET generated_at = generated_at
        WHERE annex_e_report_id = '46000000-0000-4000-8000-000000000722';
        RAISE EXCEPTION 'committed Annex E metadata update was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END;
$$;

INSERT INTO pos.fiscal_reporting_periods (
    fiscal_reporting_period_id, fiscal_reporting_contract_version_id, site_pos_server_id,
    fiscal_identity_id, period_status_code_id, business_day_date, period_start_at, period_end_at,
    reporting_timezone_name, business_day_cutoff_local_time, currency_code, period_sequence,
    opened_at, created_by_ref, updated_by_ref
) VALUES (
    '46000000-0000-4000-8000-000000000521', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
    '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302',
    '1a6f7021-bc84-5c01-afaa-c5d6685633c8', '2026-08-02',
    '2026-08-02T00:00:00Z', '2026-08-03T00:00:00Z', 'Etc/UTC', '00:00:00', 'PHP', 2,
    '2026-08-02T00:00:00Z', 'Z-006A-PROOF', 'Z-006A-PROOF'
);

UPDATE pos.fiscal_reporting_periods
SET updated_by_ref = 'Z-006A-PROOF-UPDATED', updated_at = '2026-08-02T00:00:01Z'
WHERE fiscal_reporting_period_id = '46000000-0000-4000-8000-000000000521';

DELETE FROM pos.fiscal_reporting_periods
WHERE fiscal_reporting_period_id = '46000000-0000-4000-8000-000000000521';

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pos.fiscal_reporting_periods
        WHERE fiscal_reporting_period_id = '46000000-0000-4000-8000-000000000521'
    ) THEN
        RAISE EXCEPTION 'open reporting period delete was unexpectedly blocked';
    END IF;
END;
$$;

SAVEPOINT x_z_timestamp_constraint_probe;

INSERT INTO pos.fiscal_reporting_periods (
    fiscal_reporting_period_id, fiscal_reporting_contract_version_id, site_pos_server_id,
    fiscal_identity_id, period_status_code_id, business_day_date, period_start_at, period_end_at,
    reporting_timezone_name, business_day_cutoff_local_time, currency_code, period_sequence,
    opened_at, closing_started_at, closed_at, created_by_ref, updated_by_ref
) VALUES
    (
        '46000000-0000-4000-8000-000000000521', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
        '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302',
        '1a6f7021-bc84-5c01-afaa-c5d6685633c8', '2026-08-02',
        '2026-08-02T00:00:00Z', '2026-08-03T00:00:00Z', 'Etc/UTC', '00:00:00',
        'PHP', 2, '2026-08-02T00:00:00Z', NULL, NULL, 'Z-006B1-PROOF', 'Z-006B1-PROOF'
    ),
    (
        '46000000-0000-4000-8000-000000000522', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
        '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302',
        'af7ee931-a023-507e-81a4-17adf047eb94', '2026-08-03',
        '2026-08-03T00:00:00Z', '2026-08-04T00:00:00Z', 'Etc/UTC', '00:00:00',
        'PHP', 3, '2026-08-03T00:00:00Z', '2026-08-04T00:00:00Z',
        '2026-08-04T00:00:01Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF'
    ),
    (
        '46000000-0000-4000-8000-000000000523', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
        '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302',
        'af7ee931-a023-507e-81a4-17adf047eb94', '2026-08-04',
        '2026-08-04T00:00:00Z', '2026-08-05T00:00:00Z', 'Etc/UTC', '00:00:00',
        'PHP', 4, '2026-08-04T00:00:00Z', '2026-08-05T00:00:00Z',
        '2026-08-05T00:00:01Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF'
    ),
    (
        '46000000-0000-4000-8000-000000000524', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
        '46000000-0000-4000-8000-000000000301', '46000000-0000-4000-8000-000000000302',
        'af7ee931-a023-507e-81a4-17adf047eb94', '2026-08-05',
        '2026-08-05T00:00:00Z', '2026-08-06T00:00:00Z', 'Etc/UTC', '00:00:00',
        'PHP', 5, '2026-08-05T00:00:00Z', '2026-08-06T00:00:00Z',
        '2026-08-06T00:00:01Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF'
    );

INSERT INTO pos.fiscal_report_requests (
    fiscal_report_request_id, fiscal_reporting_contract_version_id, fiscal_reporting_period_id,
    site_pos_server_id, report_type_code_id, report_status_code_id, operation_idempotency_key,
    semantic_request_hash, semantic_hash_version, business_day_date, requested_at,
    requested_by_ref, service_identity_ref
) VALUES
    ('46000000-0000-4000-8000-000000000681', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000521', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-X-AT-START', repeat('4', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-02', '2026-08-02T00:00:00Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000682', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000521', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-X-DURING-OPEN', repeat('5', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-02', '2026-08-02T12:00:00Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000683', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000521', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-X-BEFORE-END', repeat('6', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-02', '2026-08-02T23:59:59Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000684', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000521', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-X-AT-END', repeat('7', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-02', '2026-08-03T00:00:00Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000685', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000521', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-X-AFTER-END', repeat('8', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-02', '2026-08-03T00:00:01Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000686', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000521', '46000000-0000-4000-8000-000000000301', '5dc3cc94-b3ab-5582-a598-e779871fc3e2', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-X-BEFORE-START', repeat('9', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-02', '2026-08-01T23:59:59Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000687', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000522', '46000000-0000-4000-8000-000000000301', '1c628bc2-49c3-53e8-ae83-2082bcf28467', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-Z-BEFORE-END', repeat('a', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-03', '2026-08-03T23:59:59Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000688', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000523', '46000000-0000-4000-8000-000000000301', '1c628bc2-49c3-53e8-ae83-2082bcf28467', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-Z-AT-END', repeat('b', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-04', '2026-08-05T00:00:00Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000689', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000524', '46000000-0000-4000-8000-000000000301', '1c628bc2-49c3-53e8-ae83-2082bcf28467', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-Z-AFTER-END', repeat('c', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-05', '2026-08-06T00:00:01Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE'),
    ('46000000-0000-4000-8000-000000000690', 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', '46000000-0000-4000-8000-000000000523', '46000000-0000-4000-8000-000000000301', '1c628bc2-49c3-53e8-ae83-2082bcf28467', '84ef4d12-b3a1-5385-88b1-3a3eadeb8a04', 'Z-006B1-Z-DUPLICATE', repeat('d', 64), 'pos-server-fiscal-report-request:sha256:v1', '2026-08-04', '2026-08-05T00:00:01Z', 'Z-006B1-PROOF', 'Z-006B1-PROOF-SERVICE');

CREATE TEMP TABLE x_z_timestamp_no_mutation_manifest ON COMMIT DROP AS
SELECT
    (SELECT count(*) FROM pos.fiscal_documents) AS fiscal_document_count,
    (SELECT count(*) FROM pos.fiscal_document_status_history) AS fiscal_status_history_count,
    (SELECT count(*) FROM pos.fiscal_sequence_states) AS fiscal_sequence_state_count,
    (SELECT count(*) FROM pos.fiscal_counter_states) AS fiscal_counter_state_count,
    (SELECT count(*) FROM pos.fiscal_state_snapshots) AS fiscal_state_snapshot_count,
    (SELECT count(*) FROM pos.fiscal_z_counter_snapshots) AS fiscal_z_counter_snapshot_count,
    (SELECT count(*) FROM pos.reprint_requests) AS reprint_request_count,
    (SELECT count(*) FROM pos.reprint_output_refs) AS reprint_output_ref_count;

WITH cases(
    x_z_report_id, fiscal_report_request_id, fiscal_reporting_period_id,
    report_kind_code_id, report_number, generated_at, committed_at
) AS (VALUES
    ('46000000-0000-4000-8000-000000000781'::uuid, '46000000-0000-4000-8000-000000000681'::uuid, '46000000-0000-4000-8000-000000000521'::uuid, '5dc3cc94-b3ab-5582-a598-e779871fc3e2'::uuid, 'Z-006B1-X-AT-START', '2026-08-02T00:00:00Z'::timestamptz, '2026-08-02T00:00:01Z'::timestamptz),
    ('46000000-0000-4000-8000-000000000782'::uuid, '46000000-0000-4000-8000-000000000682'::uuid, '46000000-0000-4000-8000-000000000521'::uuid, '5dc3cc94-b3ab-5582-a598-e779871fc3e2'::uuid, 'Z-006B1-X-DURING-OPEN', '2026-08-02T12:00:00Z'::timestamptz, '2026-08-02T12:00:01Z'::timestamptz),
    ('46000000-0000-4000-8000-000000000783'::uuid, '46000000-0000-4000-8000-000000000683'::uuid, '46000000-0000-4000-8000-000000000521'::uuid, '5dc3cc94-b3ab-5582-a598-e779871fc3e2'::uuid, 'Z-006B1-X-BEFORE-END', '2026-08-02T23:59:59.999999Z'::timestamptz, '2026-08-03T00:00:00Z'::timestamptz),
    ('46000000-0000-4000-8000-000000000784'::uuid, '46000000-0000-4000-8000-000000000684'::uuid, '46000000-0000-4000-8000-000000000521'::uuid, '5dc3cc94-b3ab-5582-a598-e779871fc3e2'::uuid, 'Z-006B1-X-AT-END', '2026-08-03T00:00:00Z'::timestamptz, '2026-08-03T00:00:01Z'::timestamptz),
    ('46000000-0000-4000-8000-000000000785'::uuid, '46000000-0000-4000-8000-000000000685'::uuid, '46000000-0000-4000-8000-000000000521'::uuid, '5dc3cc94-b3ab-5582-a598-e779871fc3e2'::uuid, 'Z-006B1-X-AFTER-END', '2026-08-03T00:00:01Z'::timestamptz, '2026-08-03T00:00:02Z'::timestamptz),
    ('46000000-0000-4000-8000-000000000788'::uuid, '46000000-0000-4000-8000-000000000688'::uuid, '46000000-0000-4000-8000-000000000523'::uuid, '1c628bc2-49c3-53e8-ae83-2082bcf28467'::uuid, 'Z-006B1-Z-AT-END', '2026-08-05T00:00:00Z'::timestamptz, '2026-08-05T00:00:01Z'::timestamptz),
    ('46000000-0000-4000-8000-000000000789'::uuid, '46000000-0000-4000-8000-000000000689'::uuid, '46000000-0000-4000-8000-000000000524'::uuid, '1c628bc2-49c3-53e8-ae83-2082bcf28467'::uuid, 'Z-006B1-Z-AFTER-END', '2026-08-06T00:00:01Z'::timestamptz, '2026-08-06T00:00:02Z'::timestamptz)
)
INSERT INTO pos.x_z_reports (
    x_z_report_id, fiscal_report_request_id, fiscal_reporting_contract_version_id,
    fiscal_reporting_period_id, report_kind_code_id, site_pos_server_id, fiscal_identity_id,
    report_number, business_day_date, period_start_at, period_end_at, reporting_timezone_name,
    business_day_cutoff_local_time, transaction_count, fiscal_sequence_gap_count,
    previous_grand_total_amount_minor_units, current_grand_total_amount_minor_units,
    present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
    net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
    vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
    discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
    pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
    vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
    promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
    return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
    currency_code, generated_at, committed_at
)
SELECT
    cases.x_z_report_id, cases.fiscal_report_request_id,
    period.fiscal_reporting_contract_version_id, period.fiscal_reporting_period_id,
    cases.report_kind_code_id, period.site_pos_server_id, period.fiscal_identity_id,
    cases.report_number, period.business_day_date, period.period_start_at, period.period_end_at,
    period.reporting_timezone_name, period.business_day_cutoff_local_time,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    period.currency_code, cases.generated_at, cases.committed_at
FROM cases
JOIN pos.fiscal_reporting_periods period
  ON period.fiscal_reporting_period_id = cases.fiscal_reporting_period_id;

CREATE OR REPLACE FUNCTION pg_temp.insert_z006b1_timestamp_report(
    report_id uuid,
    request_id uuid,
    period_id uuid,
    report_kind_id uuid,
    report_ref text,
    observation_at timestamptz,
    commit_at timestamptz
)
RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO pos.x_z_reports (
        x_z_report_id, fiscal_report_request_id, fiscal_reporting_contract_version_id,
        fiscal_reporting_period_id, report_kind_code_id, site_pos_server_id, fiscal_identity_id,
        report_number, business_day_date, period_start_at, period_end_at, reporting_timezone_name,
        business_day_cutoff_local_time, transaction_count, fiscal_sequence_gap_count,
        previous_grand_total_amount_minor_units, current_grand_total_amount_minor_units,
        present_grand_total_amount_minor_units, gross_sales_amount_minor_units,
        net_sales_amount_minor_units, vatable_sales_amount_minor_units, vat_amount_minor_units,
        vat_exempt_sales_amount_minor_units, zero_rated_sales_amount_minor_units,
        discount_amount_minor_units, senior_citizen_discount_amount_minor_units,
        pwd_discount_amount_minor_units, other_statutory_discount_amount_minor_units,
        vat_exemption_amount_minor_units, coupon_discount_amount_minor_units,
        promotional_discount_amount_minor_units, void_amount_minor_units, refund_amount_minor_units,
        return_amount_minor_units, adjustment_amount_minor_units, service_charge_amount_minor_units,
        currency_code, generated_at, committed_at
    )
    SELECT
        report_id, request_id, period.fiscal_reporting_contract_version_id,
        period.fiscal_reporting_period_id, report_kind_id, period.site_pos_server_id,
        period.fiscal_identity_id, report_ref, period.business_day_date,
        period.period_start_at, period.period_end_at, period.reporting_timezone_name,
        period.business_day_cutoff_local_time,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        period.currency_code, observation_at, commit_at
    FROM pos.fiscal_reporting_periods period
    WHERE period.fiscal_reporting_period_id = period_id;
$$;

DO $$
BEGIN
    BEGIN
        PERFORM pg_temp.insert_z006b1_timestamp_report(
            '46000000-0000-4000-8000-000000000786',
            '46000000-0000-4000-8000-000000000686',
            '46000000-0000-4000-8000-000000000521',
            '5dc3cc94-b3ab-5582-a598-e779871fc3e2',
            'Z-006B1-X-BEFORE-START',
            '2026-08-01T23:59:59.999999Z',
            '2026-08-02T00:00:00Z'
        );
        RAISE EXCEPTION 'X generated before period start was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        PERFORM pg_temp.insert_z006b1_timestamp_report(
            '46000000-0000-4000-8000-000000000787',
            '46000000-0000-4000-8000-000000000687',
            '46000000-0000-4000-8000-000000000522',
            '1c628bc2-49c3-53e8-ae83-2082bcf28467',
            'Z-006B1-Z-BEFORE-END',
            '2026-08-03T23:59:59.999999Z',
            '2026-08-04T00:00:00Z'
        );
        RAISE EXCEPTION 'Z generated before period end was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        PERFORM pg_temp.insert_z006b1_timestamp_report(
            '46000000-0000-4000-8000-000000000790',
            '46000000-0000-4000-8000-000000000690',
            '46000000-0000-4000-8000-000000000523',
            '1c628bc2-49c3-53e8-ae83-2082bcf28467',
            'Z-006B1-Z-DUPLICATE',
            '2026-08-05T00:00:01Z',
            '2026-08-05T00:00:02Z'
        );
        RAISE EXCEPTION 'one-Z-per-period uniqueness was not enforced';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;

    BEGIN
        PERFORM pg_temp.insert_z006b1_timestamp_report(
            '46000000-0000-4000-8000-000000000791',
            '46000000-0000-4000-8000-000000000603',
            '46000000-0000-4000-8000-000000000501',
            '2326447f-74c2-5ed7-83bb-e079fcee7f3d',
            'Z-006B1-WRONG-FAMILY',
            '2026-08-02T00:00:00Z',
            '2026-08-02T00:00:01Z'
        );
        RAISE EXCEPTION 'wrong-family X/Z report kind was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        UPDATE pos.x_z_reports SET generated_at = generated_at
        WHERE x_z_report_id = '46000000-0000-4000-8000-000000000781';
        RAISE EXCEPTION 'interim X snapshot update was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        DELETE FROM pos.x_z_reports
        WHERE x_z_report_id = '46000000-0000-4000-8000-000000000788';
        RAISE EXCEPTION 'period-final Z snapshot delete was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM pos.fiscal_reporting_periods
        WHERE fiscal_reporting_period_id = '46000000-0000-4000-8000-000000000521'
          AND period_status_code_id = '1a6f7021-bc84-5c01-afaa-c5d6685633c8'
          AND closed_at IS NULL
    ) THEN
        RAISE EXCEPTION 'interim X snapshot changed its OPEN reporting period';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM x_z_timestamp_no_mutation_manifest manifest
        WHERE manifest.fiscal_document_count <> (SELECT count(*) FROM pos.fiscal_documents)
           OR manifest.fiscal_status_history_count <> (SELECT count(*) FROM pos.fiscal_document_status_history)
           OR manifest.fiscal_sequence_state_count <> (SELECT count(*) FROM pos.fiscal_sequence_states)
           OR manifest.fiscal_counter_state_count <> (SELECT count(*) FROM pos.fiscal_counter_states)
           OR manifest.fiscal_state_snapshot_count <> (SELECT count(*) FROM pos.fiscal_state_snapshots)
           OR manifest.fiscal_z_counter_snapshot_count <> (SELECT count(*) FROM pos.fiscal_z_counter_snapshots)
           OR manifest.reprint_request_count <> (SELECT count(*) FROM pos.reprint_requests)
           OR manifest.reprint_output_ref_count <> (SELECT count(*) FROM pos.reprint_output_refs)
    ) THEN
        RAISE EXCEPTION 'X/Z timestamp proof changed protected fiscal or counter state';
    END IF;
END;
$$;

SELECT 'reporting-proof-x-at-period-start' AS proof, 'passed' AS value;
SELECT 'reporting-proof-x-during-open-period' AS proof, 'passed' AS value;
SELECT 'reporting-proof-x-immediately-before-period-end' AS proof, 'passed' AS value;
SELECT 'reporting-proof-x-at-period-end' AS proof, 'passed' AS value;
SELECT 'reporting-proof-x-after-period-end' AS proof, 'passed' AS value;
SELECT 'reporting-proof-x-before-period-start-rejected' AS proof, 'passed' AS value;
SELECT 'reporting-proof-z-before-period-end-rejected' AS proof, 'passed' AS value;
SELECT 'reporting-proof-z-at-period-end' AS proof, 'passed' AS value;
SELECT 'reporting-proof-z-after-period-end' AS proof, 'passed' AS value;
SELECT 'reporting-proof-wrong-family-x-z-kind-rejected' AS proof, 'passed' AS value;
SELECT 'reporting-proof-one-z-per-period-preserved' AS proof, 'passed' AS value;
SELECT 'reporting-proof-x-immutability-preserved' AS proof, 'passed' AS value;
SELECT 'reporting-proof-z-immutability-preserved' AS proof, 'passed' AS value;
SELECT 'reporting-proof-no-protected-state-mutation' AS proof, 'passed' AS value;

ROLLBACK TO SAVEPOINT x_z_timestamp_constraint_probe;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pos.x_z_reports
        WHERE x_z_report_id BETWEEN '46000000-0000-4000-8000-000000000781'
            AND '46000000-0000-4000-8000-000000000791'
    ) OR EXISTS (
        SELECT 1 FROM pos.fiscal_report_requests
        WHERE fiscal_report_request_id BETWEEN '46000000-0000-4000-8000-000000000681'
            AND '46000000-0000-4000-8000-000000000690'
    ) OR EXISTS (
        SELECT 1 FROM pos.fiscal_reporting_periods
        WHERE fiscal_reporting_period_id BETWEEN '46000000-0000-4000-8000-000000000521'
            AND '46000000-0000-4000-8000-000000000524'
    ) THEN
        RAISE EXCEPTION 'timestamp constraint proof rollback left partial evidence';
    END IF;
END;
$$;

SELECT 'reporting-proof-timestamp-transaction-rollback' AS proof, 'passed' AS value;

SAVEPOINT reporting_rollback_probe;

INSERT INTO pos.fiscal_report_output_refs (
    fiscal_report_output_ref_id, fiscal_report_request_id, output_type_code_id,
    output_status_code_id, output_ref, generated_at, generated_by_ref
) VALUES (
    '46000000-0000-4000-8000-000000000760', '46000000-0000-4000-8000-000000000608',
    'd8f2e655-cb41-52ad-a39d-0f5d1005ba1a', 'f6738162-9db7-5b0b-8f7e-a8736f07d2ff',
    'REPORTING-PROOF-OUTPUT-REF', '2026-08-02T00:00:09Z', 'Z-006A-PROOF'
);

ROLLBACK TO SAVEPOINT reporting_rollback_probe;

DO $$
DECLARE
    normalized_trigger_count integer;
    catalog_trigger_count integer;
BEGIN
    IF EXISTS (
        SELECT 1 FROM pos.fiscal_report_output_refs
        WHERE fiscal_report_output_ref_id = '46000000-0000-4000-8000-000000000760'
    ) THEN
        RAISE EXCEPTION 'reporting rollback left a partial output row';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'pos'
          AND table_name IN (
              'fiscal_reporting_periods', 'fiscal_report_requests', 'fiscal_report_scopes',
              'x_z_reports', 'fiscal_report_tender_breakdowns',
              'fiscal_report_discount_breakdowns', 'fiscal_report_fiscal_number_ranges',
              'fiscal_report_sequence_gaps', 'fiscal_z_counter_snapshots',
              'bir_sales_summary_reports', 'annex_e_reports', 'fiscal_report_output_refs'
          )
          AND (
              data_type IN ('json', 'jsonb', 'xml')
              OR column_name ILIKE '%payload%'
              OR column_name ILIKE '%context%'
              OR column_name ILIKE '%metadata%'
              OR column_name ILIKE '%beneficiary%'
              OR column_name ILIKE '%evidence%'
              OR column_name ILIKE '%reviewer%'
              OR column_name ILIKE '%credential%'
              OR column_name ILIKE '%authorization%'
              OR column_name ILIKE '%password%'
              OR column_name ILIKE '%secret%'
          )
    ) THEN
        RAISE EXCEPTION 'prohibited generic or private reporting storage column exists';
    END IF;

    SELECT count(*) INTO normalized_trigger_count
    FROM (
        SELECT DISTINCT event_object_schema, event_object_table, trigger_name
        FROM information_schema.triggers
        WHERE event_object_schema = 'pos'
    ) normalized;

    SELECT count(*) INTO catalog_trigger_count
    FROM pg_trigger trigger_row
    JOIN pg_class table_row ON table_row.oid = trigger_row.tgrelid
    JOIN pg_namespace schema_row ON schema_row.oid = table_row.relnamespace
    WHERE schema_row.nspname = 'pos' AND NOT trigger_row.tgisinternal;

    IF normalized_trigger_count <> catalog_trigger_count THEN
        RAISE EXCEPTION 'normalized trigger inventory does not match PostgreSQL trigger objects';
    END IF;
END;
$$;

SAVEPOINT z_close_boundary_foundation_probe;

INSERT INTO pos.fiscal_reporting_periods (
    fiscal_reporting_period_id, fiscal_reporting_contract_version_id, site_pos_server_id,
    fiscal_identity_id, period_status_code_id, business_day_date, period_start_at,
    period_end_at, reporting_timezone_name, business_day_cutoff_local_time, currency_code,
    period_sequence, expected_prior_period_id, opened_at, created_by_ref, updated_by_ref
) VALUES (
    '46000000-0000-4000-8000-000000000530',
    'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
    '46000000-0000-4000-8000-000000000301',
    '46000000-0000-4000-8000-000000000302',
    '1a6f7021-bc84-5c01-afaa-c5d6685633c8', '2026-08-02',
    '2026-08-02T00:00:00Z', '2026-08-03T00:00:00Z', 'Etc/UTC', '00:00:00',
    'PHP', 2, '46000000-0000-4000-8000-000000000501', '2026-08-02T00:00:00Z',
    'Z-007B-PROOF', 'Z-007B-PROOF'
);

INSERT INTO pos.fiscal_documents (
    fiscal_document_id, site_pos_server_id, fiscal_identity_id, currency_code,
    fiscal_reporting_period_id, fiscal_document_type_code_id, fiscal_document_status_code_id,
    business_day_date, created_at, updated_at
) VALUES (
    '46000000-0000-4000-8000-000000000900',
    '46000000-0000-4000-8000-000000000301',
    '46000000-0000-4000-8000-000000000302', 'PHP',
    '46000000-0000-4000-8000-000000000530',
    '46000000-0000-4000-8000-000000000201',
    '46000000-0000-4000-8000-000000000202',
    '2026-08-02', '2026-08-02T12:00:00Z', '2026-08-02T12:00:00Z'
);

DO $$
BEGIN
    BEGIN
        INSERT INTO pos.fiscal_z_close_states (
            fiscal_z_close_state_id, site_pos_server_id, fiscal_identity_id, currency_code,
            fiscal_reporting_contract_version_id, reset_counter_value, z_counter_value,
            grand_total_amount_minor_units, state_version, initialization_provenance_code_id,
            initialized_at, initialized_by_ref, initialization_service_ref,
            initialization_approval_ref, last_transition_operation_ref, last_transition_at
        ) VALUES (
            '46000000-0000-4000-8000-000000000625',
            '46000000-0000-4000-8000-000000000301',
            '46000000-0000-4000-8000-000000000302', 'PHP',
            'f6766f48-62f0-513f-b9eb-e61c2f3e8c66', 0, 0, 0, 1,
            '8f31c890-2aa2-50ef-a815-0e0c8cf90983', '2026-08-02T00:00:00Z',
            'Z-007B-PROOF', 'Z-007B-PROOF-SERVICE', 'Z-007B-APPROVAL',
            'Z-007B-DUPLICATE-SCOPE', '2026-08-02T00:00:00Z'
        );
        RAISE EXCEPTION 'duplicate canonical Z state scope was not rejected';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_z_close_state_transitions (
            fiscal_z_close_state_transition_id, fiscal_z_close_state_id,
            fiscal_reporting_contract_version_id, site_pos_server_id, fiscal_identity_id,
            currency_code, transition_type_code_id, initialization_provenance_code_id,
            operation_ref, semantic_request_hash, semantic_hash_version,
            expected_state_version, resulting_state_version, approval_ref, actor_ref,
            service_identity_ref, correlation_ref, committed_at
        ) VALUES (
            '46000000-0000-4000-8000-000000000626',
            '46000000-0000-4000-8000-000000000620',
            'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
            '46000000-0000-4000-8000-000000000301',
            '46000000-0000-4000-8000-000000000302', 'PHP',
            '5dc3cc94-b3ab-5582-a598-e779871fc3e2', NULL,
            'Z-007B-WRONG-FAMILY', repeat('5', 64),
            'pos-server-fiscal-z-close-state-transition:sha256:v1', 1, 2,
            'Z-007B-APPROVAL', 'Z-007B-PROOF', 'Z-007B-PROOF-SERVICE',
            'Z-007B-PROOF-CORRELATION', '2026-08-02T00:00:00Z'
        );
        RAISE EXCEPTION 'wrong-family Z state transition type was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        UPDATE pos.fiscal_z_close_states
        SET z_counter_value = z_counter_value + 1
        WHERE fiscal_z_close_state_id = '46000000-0000-4000-8000-000000000620';
        RAISE EXCEPTION 'direct canonical Z state update was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        DELETE FROM pos.fiscal_z_close_states
        WHERE fiscal_z_close_state_id = '46000000-0000-4000-8000-000000000620';
        RAISE EXCEPTION 'direct canonical Z state delete was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        PERFORM pos.apply_fiscal_z_close_state_transition(
            '46000000-0000-4000-8000-000000000621'
        );
        RAISE EXCEPTION 'initialization evidence was incorrectly accepted as Z transition';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        UPDATE pos.fiscal_documents
        SET fiscal_reporting_period_id = NULL
        WHERE fiscal_document_id = '46000000-0000-4000-8000-000000000900';
        RAISE EXCEPTION 'fiscal reporting-period assignment mutation was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_documents (
            fiscal_document_id, site_pos_server_id, fiscal_identity_id, currency_code,
            fiscal_reporting_period_id, fiscal_document_type_code_id, fiscal_document_status_code_id,
            created_at, updated_at
        ) VALUES (
            '46000000-0000-4000-8000-000000000901',
            '46000000-0000-4000-8000-000000000301',
            '46000000-0000-4000-8000-000000000302', 'USD',
            '46000000-0000-4000-8000-000000000530',
            '46000000-0000-4000-8000-000000000201',
            '46000000-0000-4000-8000-000000000202',
            '2026-08-02T12:00:00Z', '2026-08-02T12:00:00Z'
        );
        RAISE EXCEPTION 'mixed-currency reporting-period assignment was not rejected';
    EXCEPTION WHEN foreign_key_violation OR check_violation THEN NULL;
    END;

    BEGIN
        INSERT INTO pos.fiscal_documents (
            fiscal_document_id, site_pos_server_id, fiscal_identity_id, currency_code,
            fiscal_reporting_period_id, fiscal_document_type_code_id, fiscal_document_status_code_id,
            created_at, updated_at
        ) VALUES (
            '46000000-0000-4000-8000-000000000902',
            '46000000-0000-4000-8000-000000000301',
            '46000000-0000-4000-8000-000000000302', 'PHP',
            '46000000-0000-4000-8000-000000000530',
            '46000000-0000-4000-8000-000000000201',
            '46000000-0000-4000-8000-000000000202',
            '2026-08-03T00:00:00Z', '2026-08-03T00:00:00Z'
        );
        RAISE EXCEPTION 'period-end fiscal document was not rejected from prior period';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END;
$$;

SELECT 'reporting-proof-z-state-unique-scope' AS proof, 'passed' AS value;
SELECT 'reporting-proof-z-state-wrong-family-rejected' AS proof, 'passed' AS value;
SELECT 'reporting-proof-z-state-direct-mutation-rejected' AS proof, 'passed' AS value;
SELECT 'reporting-proof-z-state-non-transition-rejected' AS proof, 'passed' AS value;
SELECT 'reporting-proof-period-assignment-valid' AS proof, 'passed' AS value;
SELECT 'reporting-proof-period-assignment-immutable' AS proof, 'passed' AS value;
SELECT 'reporting-proof-period-assignment-scope-currency' AS proof, 'passed' AS value;
SELECT 'reporting-proof-period-assignment-half-open-window' AS proof, 'passed' AS value;

ROLLBACK TO SAVEPOINT z_close_boundary_foundation_probe;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pos.fiscal_documents
        WHERE fiscal_document_id BETWEEN '46000000-0000-4000-8000-000000000900'
            AND '46000000-0000-4000-8000-000000000902'
    ) OR EXISTS (
        SELECT 1 FROM pos.fiscal_reporting_periods
        WHERE fiscal_reporting_period_id = '46000000-0000-4000-8000-000000000530'
    ) THEN
        RAISE EXCEPTION 'Z close boundary foundation proof rollback left partial evidence';
    END IF;
END;
$$;

SELECT 'reporting-proof-z-boundary-transaction-rollback' AS proof, 'passed' AS value;

SELECT 'reporting-proof-x-count' AS proof, count(*)::text AS value
FROM pos.x_z_reports WHERE report_kind_code_id = '5dc3cc94-b3ab-5582-a598-e779871fc3e2';

SELECT 'reporting-proof-z-count' AS proof, count(*)::text AS value
FROM pos.x_z_reports WHERE report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467';

SELECT 'reporting-proof-bir-bound-to-z' AS proof, count(*)::text AS value
FROM pos.bir_sales_summary_reports summary
JOIN pos.x_z_reports report ON report.x_z_report_id = summary.governing_z_report_id
WHERE report.report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467';

SELECT 'reporting-proof-normalized-triggers' AS proof, count(*)::text AS value
FROM (
    SELECT DISTINCT event_object_schema, event_object_table, trigger_name
    FROM information_schema.triggers WHERE event_object_schema = 'pos'
) normalized;

ROLLBACK;
