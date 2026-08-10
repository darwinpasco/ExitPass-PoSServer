-- GENERATED FILE - DO NOT EDIT MANUALLY.
-- ExitPass POS Server canonical Electronic Journal controlled codes, Z-011A.
-- UUID v5 namespace: d07a7416-af9c-556d-86cc-7061335f7c11
-- Source: db/reference-data/controlled-codes/source/controlled_code_source_index.json

BEGIN;

WITH source(controlled_code_set_id, code_set_key, display_name, description) AS (VALUES
    ('f87b4055-69eb-5798-9fac-8a79494a4edb'::uuid, 'electronic_journal_access_action', 'Electronic Journal Access Action', 'Read-only governed access actions.'),
    ('873bd607-4ede-5a8e-9b00-0ef4baa3e5ae'::uuid, 'electronic_journal_access_result', 'Electronic Journal Access Result', 'Safe outcomes for append-only EJ access evidence.'),
    ('672c0e36-24f1-5a4f-86bc-dd65c2f91905'::uuid, 'electronic_journal_event_type', 'Electronic Journal Event Type', 'Versioned authoritative transition types represented in the canonical fiscal event stream.'),
    ('2d09a4d2-e244-5e7d-801e-12ec29ce8fa5'::uuid, 'electronic_journal_record_status', 'Electronic Journal Record Status', 'Canonical EJ records are visible only after durable source transaction commit.'),
    ('3d3797c6-2254-5632-b8df-407ff43eee45'::uuid, 'electronic_journal_retention_policy', 'Electronic Journal Retention Policy', 'Classifies fiscal reconstruction evidence without inventing a purge duration.')
)
INSERT INTO pos.controlled_code_sets (
    controlled_code_set_id, code_set_key, display_name, description,
    governance_owner, source_ref, is_active, effective_start_at, effective_end_at,
    created_at, updated_at)
SELECT controlled_code_set_id, code_set_key, display_name, description,
       'Engineering', 'Z-011A', true, '2026-08-10T00:00:00Z'::timestamptz, NULL,
       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM source
ON CONFLICT (controlled_code_set_id) DO UPDATE SET
    code_set_key=EXCLUDED.code_set_key, display_name=EXCLUDED.display_name,
    description=EXCLUDED.description, governance_owner=EXCLUDED.governance_owner,
    source_ref=EXCLUDED.source_ref, is_active=EXCLUDED.is_active,
    effective_start_at=EXCLUDED.effective_start_at, effective_end_at=EXCLUDED.effective_end_at,
    updated_at=CURRENT_TIMESTAMP;

WITH source(controlled_code_id, set_key, code_key, display_name, description, sort_order) AS (VALUES
    ('a98278b3-63a8-5120-b5ca-08ba1443aab6'::uuid,'electronic_journal_access_action','read','Read','Bounded Electronic Journal readback.',10),
    ('b42d48ae-148e-5a1d-b0d0-35926c1aa1a9'::uuid,'electronic_journal_access_action','export','Export','Deterministic Electronic Journal export.',20),
    ('78fbabd3-7ede-55d4-970b-1bbb49b7ad69'::uuid,'electronic_journal_access_action','integrity_verify','Integrity Verify','Administrative integrity-chain verification.',30),
    ('a5959163-f9c7-5cf4-bc17-fa8ff3945265'::uuid,'electronic_journal_access_result','allowed','Allowed','The governed access completed successfully.',10),
    ('49a2352f-8110-5117-96d0-314158d25d90'::uuid,'electronic_journal_access_result','denied','Denied','The governed access was denied without disclosing hidden scope facts.',20),
    ('5c5b7d12-09d9-5b92-b850-4333ab104ea0'::uuid,'electronic_journal_access_result','failed','Failed','The governed access failed safely.',30),
    ('89dd11fb-87d8-51fe-a042-ef8b2e2e7829'::uuid,'electronic_journal_event_type','fiscal_document_committed','Fiscal Document Committed','A complete fiscal document, numbering, status, and governed child facts committed atomically.',10),
    ('4e7f833a-449e-56e0-8feb-13dc820fd060'::uuid,'electronic_journal_event_type','fiscal_document_voided','Fiscal Document Voided','A governed same-period fiscal-document void committed atomically.',20),
    ('77905f62-7d61-521d-bdc3-7c7d57b31efe'::uuid,'electronic_journal_event_type','fiscal_document_reprint_recorded','Fiscal Document Reprint Recorded','A governed reprint record, output reference, and copy sequence committed atomically with its canonical event.',30),
    ('ea266ad1-9c7e-5291-b2a7-54eec88a0716'::uuid,'electronic_journal_event_type','fiscal_adjustment_recorded','Fiscal Adjustment Recorded','A governed adjustment committed by a future supported runtime.',40),
    ('92818433-ecd7-5948-8100-caec955b44e9'::uuid,'electronic_journal_event_type','digital_si_published','Digital SI Published','A governed Digital Sales Invoice publication transition committed by a future supported runtime.',50),
    ('6df85374-d609-58f1-8da5-ae8d6ed2ed35'::uuid,'electronic_journal_event_type','x_reading_committed','X Reading Committed','An immutable X Reading observation committed atomically.',60),
    ('c428ea97-f423-5445-851e-1402909a6f9c'::uuid,'electronic_journal_event_type','z_reading_committed','Z Reading Committed','An immutable Z Reading, period close, counter, and GTA transition committed atomically.',70),
    ('fc645aa9-f76b-5d46-8a67-374feef818cb'::uuid,'electronic_journal_event_type','bir_sales_summary_committed','BIR Sales Summary Committed','An immutable BIR sales summary bound to its governing Z Reading committed atomically.',80),
    ('ddff6062-b8e4-5f6f-af79-3c2ebc042bc0'::uuid,'electronic_journal_event_type','fiscal_report_exported','Fiscal Report Exported','A controlled fiscal-report export when a merged profile explicitly classifies export as a fiscal event.',90),
    ('07022b0a-3ef9-5f82-880a-d27a569f5e7b'::uuid,'electronic_journal_record_status','committed','Committed','The source transition and event are durably committed in one transaction.',10),
    ('bf711168-63d1-539e-9e8f-6de908978b3e'::uuid,'electronic_journal_retention_policy','fiscal_reconstruction_hold','Fiscal Reconstruction Hold','Retain and prohibit deletion pending an approved archival, legal-hold, and purge contract.',10)
)
INSERT INTO pos.controlled_codes (
    controlled_code_id, controlled_code_set_id, code_key, display_name, description,
    source_ref, sort_order, is_active, effective_start_at, effective_end_at,
    created_at, updated_at)
SELECT source.controlled_code_id, code_set.controlled_code_set_id, source.code_key,
       source.display_name, source.description, 'Z-011A', source.sort_order,
       true, '2026-08-10T00:00:00Z'::timestamptz, NULL,
       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM source
JOIN pos.controlled_code_sets code_set ON code_set.code_set_key=source.set_key
ON CONFLICT (controlled_code_id) DO UPDATE SET
    controlled_code_set_id=EXCLUDED.controlled_code_set_id, code_key=EXCLUDED.code_key,
    display_name=EXCLUDED.display_name, description=EXCLUDED.description,
    source_ref=EXCLUDED.source_ref, sort_order=EXCLUDED.sort_order,
    is_active=EXCLUDED.is_active, effective_start_at=EXCLUDED.effective_start_at,
    effective_end_at=EXCLUDED.effective_end_at, updated_at=CURRENT_TIMESTAMP;

COMMIT;
