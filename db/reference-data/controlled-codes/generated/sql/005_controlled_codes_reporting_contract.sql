-- GENERATED FILE - DO NOT EDIT MANUALLY.
-- ExitPass POS Server fiscal-reporting contract controlled codes, Z-006A.
-- UUID v5 namespace: d07a7416-af9c-556d-86cc-7061335f7c11
-- Source: db/reference-data/controlled-codes/source/controlled_code_source_index.json

BEGIN;

WITH source(controlled_code_set_id, code_set_key, display_name, description) AS (VALUES
    ('b3c752cf-a44f-5b4d-af70-cd78fcc440d9'::uuid, 'fiscal_report_kind', 'Fiscal Report Kind', 'Internal X, Z, BIR Sales Summary, and Annex E report identities.'),
    ('4c354b72-70c9-56d1-a853-550a64dab543'::uuid, 'fiscal_report_output_status', 'Fiscal Report Output Status', 'Report output-reference lifecycle distinct from report and export state.'),
    ('2ac0a3fc-1a82-570c-aa64-121169f7ee53'::uuid, 'fiscal_report_output_type', 'Fiscal Report Output Type', 'Internal presentation and separately governed export reference types.'),
    ('d5253b56-74c9-54a2-8504-767affdc4a7a'::uuid, 'fiscal_report_scope_type', 'Fiscal Report Scope Type', 'Approved first-class report scope types.'),
    ('1f687135-dfbd-5e61-b10c-02e1725c9ea3'::uuid, 'fiscal_report_status', 'Fiscal Report Status', 'Report request lifecycle including durable replay and conflict posture.'),
    ('ad06f8e5-6744-5117-a59b-3aa660b3c9c9'::uuid, 'fiscal_reporting_discount_classification', 'Fiscal Reporting Discount Classification', 'Separate statutory, VAT, coupon, and promotional report categories.'),
    ('46884908-97e4-5e91-ac58-ac04feb54178'::uuid, 'fiscal_reporting_period_status', 'Fiscal Reporting Period Status', 'OPEN, future CLOSING, and immutable CLOSED posture.'),
    ('57acd128-41de-5ea4-903a-9922f15cf7ad'::uuid, 'fiscal_reporting_tender_classification', 'Fiscal Reporting Tender Classification', 'Governed reporting projection categories for fiscal tenders.'),
    ('8c5d1523-8876-5fc5-b3af-7b2f7ee594a8'::uuid, 'fiscal_sequence_gap_classification', 'Fiscal Sequence Gap Classification', 'Immutable report-local fiscal sequence gap classifications.')
)
INSERT INTO pos.controlled_code_sets (
    controlled_code_set_id, code_set_key, display_name, description,
    governance_owner, source_ref, is_active, effective_start_at, effective_end_at,
    created_at, updated_at
)
SELECT controlled_code_set_id, code_set_key, display_name, description,
       'Engineering', 'ExitPass_POS_Server_Internal_Fiscal_Reporting_Contract_v1.0; Z-006A',
       true, '2026-08-03T00:00:00Z'::timestamptz, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM source
ON CONFLICT (controlled_code_set_id) DO UPDATE SET
    code_set_key = EXCLUDED.code_set_key,
    display_name = EXCLUDED.display_name,
    description = EXCLUDED.description,
    governance_owner = EXCLUDED.governance_owner,
    source_ref = EXCLUDED.source_ref,
    is_active = EXCLUDED.is_active,
    effective_start_at = EXCLUDED.effective_start_at,
    effective_end_at = EXCLUDED.effective_end_at,
    updated_at = CURRENT_TIMESTAMP;

WITH source(controlled_code_id, set_key, code_key, display_name, description, sort_order) AS (VALUES
    ('5dc3cc94-b3ab-5582-a598-e779871fc3e2'::uuid, 'fiscal_report_kind', 'x_reading', 'X Reading', 'Read-only interim fiscal reading.', 10),
    ('1c628bc2-49c3-53e8-ae83-2082bcf28467'::uuid, 'fiscal_report_kind', 'z_reading', 'Z Reading', 'Future atomic fiscal-period close snapshot.', 20),
    ('2326447f-74c2-5ed7-83bb-e079fcee7f3d'::uuid, 'fiscal_report_kind', 'bir_sales_summary', 'BIR Sales Summary', 'Immutable summary governed by one Z Reading.', 30),
    ('d4c4615e-2cf2-59b7-a6d2-97d22210114c'::uuid, 'fiscal_report_kind', 'annex_e', 'Annex E', 'Internal Annex E metadata identity only.', 40),

    ('94bcd4c0-f938-5efc-8b05-51b5f5f32e94'::uuid, 'fiscal_report_output_status', 'requested', 'Requested', 'Output reference requested.', 10),
    ('a94d293e-f592-5d9e-b8e1-e67bb6f566c7'::uuid, 'fiscal_report_output_status', 'processing', 'Processing', 'Future output generation is in progress.', 20),
    ('f6738162-9db7-5b0b-8f7e-a8736f07d2ff'::uuid, 'fiscal_report_output_status', 'committed', 'Committed', 'Output reference is durably committed.', 30),
    ('22758b2e-3a90-59d9-b7a3-a284aed87e12'::uuid, 'fiscal_report_output_status', 'failed', 'Failed', 'Output generation did not commit.', 40),

    ('d8f2e655-cb41-52ad-a39d-0f5d1005ba1a'::uuid, 'fiscal_report_output_type', 'json_presentation', 'JSON Presentation', 'Future authoritative JSON presentation reference.', 10),
    ('cc4ac16f-401f-5b8e-b817-464fc1e4ceb2'::uuid, 'fiscal_report_output_type', 'print_presentation', 'Print Presentation', 'Future print presentation reference.', 20),
    ('9c2b928f-13bf-5226-9a26-4fc4b4966056'::uuid, 'fiscal_report_output_type', 'external_export_reference', 'External Export Reference', 'Reference to a separately approved export profile.', 30),

    ('ce034742-16fe-578c-85af-a05e62129f21'::uuid, 'fiscal_report_scope_type', 'site_pos_server_period', 'Site POS Server Period', 'One Site POS Server, fiscal identity, currency, and half-open period.', 10),

    ('84ef4d12-b3a1-5385-88b1-3a3eadeb8a04'::uuid, 'fiscal_report_status', 'committed', 'Committed', 'Governed report snapshot committed durably.', 30),
    ('acce1c93-a76e-5142-9b42-4314616373c4'::uuid, 'fiscal_report_status', 'rejected', 'Rejected', 'Request safely rejected before commitment.', 50),
    ('523aa5d7-85bc-526d-b36c-22a0075d50db'::uuid, 'fiscal_report_status', 'conflict', 'Conflict', 'Operation identity has different governed semantics.', 60),
    ('f89b81d9-14eb-5509-94fe-84caf60eb21f'::uuid, 'fiscal_report_status', 'unknown_commit_outcome', 'Unknown Commit Outcome', 'Durable outcome must be resolved before retry.', 80),

    ('6fc90db9-12d4-509f-948e-04beaa7de371'::uuid, 'fiscal_reporting_discount_classification', 'senior_citizen_statutory', 'Senior Citizen Statutory', 'Final applied Senior Citizen statutory aggregate.', 10),
    ('10b9c4ac-69eb-56b4-937f-edf81411c035'::uuid, 'fiscal_reporting_discount_classification', 'pwd_statutory', 'PWD Statutory', 'Final applied PWD statutory aggregate.', 20),
    ('419b274d-9ca3-522e-9a78-0a352d19c893'::uuid, 'fiscal_reporting_discount_classification', 'other_statutory', 'Other Statutory', 'Future separately governed statutory aggregate.', 30),
    ('349e303c-7fb3-5f4f-a38f-48cc593c09a8'::uuid, 'fiscal_reporting_discount_classification', 'vat_exemption_adjustment', 'VAT Exemption Adjustment', 'VAT privilege aggregate distinct from discount.', 40),
    ('388289a8-283c-5e61-bee6-a352dc3e25c8'::uuid, 'fiscal_reporting_discount_classification', 'coupon', 'Coupon', 'Governed commercial coupon aggregate.', 50),
    ('c7d13ec7-38f4-5229-8022-456fdf6f0ff1'::uuid, 'fiscal_reporting_discount_classification', 'promotional', 'Promotional', 'Governed commercial promotional aggregate.', 60),

    ('1a6f7021-bc84-5c01-afaa-c5d6685633c8'::uuid, 'fiscal_reporting_period_status', 'open', 'Open', 'Reporting period is not closed.', 10),
    ('f0a44431-611b-5809-b3a9-5b8be8614552'::uuid, 'fiscal_reporting_period_status', 'closing', 'Closing', 'Future transient atomic-close posture.', 20),
    ('af7ee931-a023-507e-81a4-17adf047eb94'::uuid, 'fiscal_reporting_period_status', 'closed', 'Closed', 'Committed immutable fiscal reporting period.', 30),

    ('47a97fd9-987d-5ea2-a8d0-d293343bf7f6'::uuid, 'fiscal_reporting_tender_classification', 'cash', 'Cash', 'Cash tender aggregate.', 10),
    ('1f429942-5bec-585b-ae4c-a5025ec59827'::uuid, 'fiscal_reporting_tender_classification', 'card', 'Card', 'Governed card tender aggregate.', 20),
    ('156acddb-4eee-5653-8b47-55375928c15b'::uuid, 'fiscal_reporting_tender_classification', 'digital_wallet', 'Digital Wallet', 'Governed digital-wallet tender aggregate.', 30),
    ('d0eadc45-339d-5376-bad4-ca26167edc25'::uuid, 'fiscal_reporting_tender_classification', 'bank_transfer', 'Bank Transfer', 'Governed bank-transfer tender aggregate.', 40),
    ('6f99281a-4847-5699-96e8-5138e82d01cb'::uuid, 'fiscal_reporting_tender_classification', 'other_non_cash', 'Other Non-Cash', 'Explicitly governed other non-cash aggregate.', 50),

    ('d7151b60-b08f-587f-9063-dd81e6ea5663'::uuid, 'fiscal_sequence_gap_classification', 'voided_document', 'Voided Document', 'Sequence belongs to a voided document.', 10),
    ('b1d73736-c963-575a-a497-fb9d16ca8914'::uuid, 'fiscal_sequence_gap_classification', 'failed_issuance', 'Failed Issuance', 'Sequence consumed by governed failed issuance.', 20),
    ('32d4908a-62bd-58b0-9c76-5e284bf8f5af'::uuid, 'fiscal_sequence_gap_classification', 'reserved_not_issued', 'Reserved Not Issued', 'Sequence reserved without an issued document.', 30),
    ('cb7502b1-83bf-5d08-91ef-104cdb4a8f34'::uuid, 'fiscal_sequence_gap_classification', 'unexplained', 'Unexplained', 'Gap blocks future close readiness.', 40)
)
INSERT INTO pos.controlled_codes (
    controlled_code_id, controlled_code_set_id, code_key, display_name, description,
    source_ref, sort_order, is_active, effective_start_at, effective_end_at,
    created_at, updated_at
)
SELECT source.controlled_code_id, sets.controlled_code_set_id, source.code_key,
       source.display_name, source.description, 'Z-006A', source.sort_order,
       true, '2026-08-03T00:00:00Z'::timestamptz, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM source
JOIN pos.controlled_code_sets sets ON sets.code_set_key = source.set_key
ON CONFLICT (controlled_code_id) DO UPDATE SET
    controlled_code_set_id = EXCLUDED.controlled_code_set_id,
    code_key = EXCLUDED.code_key,
    display_name = EXCLUDED.display_name,
    description = EXCLUDED.description,
    source_ref = EXCLUDED.source_ref,
    sort_order = EXCLUDED.sort_order,
    is_active = EXCLUDED.is_active,
    effective_start_at = EXCLUDED.effective_start_at,
    effective_end_at = EXCLUDED.effective_end_at,
    updated_at = CURRENT_TIMESTAMP;

COMMIT;
