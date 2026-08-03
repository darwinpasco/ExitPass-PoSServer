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
            fiscal_z_counter_snapshot_id, x_z_report_id, report_kind_code_id,
            previous_reset_counter_value, resulting_reset_counter_value,
            previous_z_counter_value, resulting_z_counter_value,
            previous_grand_total_amount_minor_units, current_period_amount_minor_units,
            resulting_grand_total_amount_minor_units, currency_code
        ) VALUES (
            '46000000-0000-4000-8000-000000000719', '46000000-0000-4000-8000-000000000702',
            '1c628bc2-49c3-53e8-ae83-2082bcf28467', 0, 1, 0, 2, 100000, 20000, 120000, 'PHP'
        );
        RAISE EXCEPTION 'invalid Z counter relationship was not rejected';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END;
$$;

INSERT INTO pos.fiscal_z_counter_snapshots (
    fiscal_z_counter_snapshot_id, x_z_report_id, report_kind_code_id,
    previous_reset_counter_value, resulting_reset_counter_value,
    previous_z_counter_value, resulting_z_counter_value,
    previous_grand_total_amount_minor_units, current_period_amount_minor_units,
    resulting_grand_total_amount_minor_units, currency_code
) VALUES (
    '46000000-0000-4000-8000-000000000715', '46000000-0000-4000-8000-000000000702',
    '1c628bc2-49c3-53e8-ae83-2082bcf28467', 0, 1, 0, 1, 100000, 20000, 120000, 'PHP'
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
