-- GENERATED FILE - DO NOT EDIT MANUALLY.
-- ExitPass POS Server canonical fiscal reprint controlled codes, Z-011A.
-- UUID v5 namespace: d07a7416-af9c-556d-86cc-7061335f7c11
-- Source: db/reference-data/controlled-codes/source/controlled_code_source_index.json

BEGIN;

WITH source(controlled_code_set_id, code_set_key, display_name, description) AS (VALUES
    ('d9000432-ad5e-5472-bc73-8c63fef69b4b'::uuid, 'fiscal_reprint_reason', 'Fiscal Reprint Reason', 'Bounded privacy-safe reasons for recording a fiscal-document copy.'),
    ('d4503980-6149-5643-8e2f-e332a0f29d78'::uuid, 'fiscal_reprint_status', 'Fiscal Reprint Status', 'Stable committed posture for immutable reprint evidence.'),
    ('434b2ce3-00f4-5074-832a-d20ea153cdbd'::uuid, 'fiscal_reprint_type', 'Fiscal Reprint Type', 'Stable classification of a recorded fiscal presentation copy.')
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
    ('5b8a58c8-636f-51fc-8512-85e3d932e8c8'::uuid,'fiscal_reprint_reason','operator_request','Operator Request','An authorized operator requested a labeled fiscal-document copy.',10),
    ('215ad81a-913b-53e2-a009-b5bf1de021a5'::uuid,'fiscal_reprint_reason','customer_request','Customer Request','A customer requested a labeled fiscal-document copy; no customer identity is retained.',20),
    ('c9ab8e23-0f6f-5bbb-a2f5-cc8c5105606e'::uuid,'fiscal_reprint_reason','audit_request','Audit Request','An authorized audit function requested a labeled fiscal-document copy.',30),
    ('dbaf50f2-0001-5820-b6f1-ff5803f7d839'::uuid,'fiscal_reprint_reason','damaged_original','Damaged Original','A replacement copy was requested because the prior presentation was damaged or unreadable.',40),
    ('5dacbba2-6cc7-593f-867b-d7010955d3e1'::uuid,'fiscal_reprint_status','committed','Committed','The reprint evidence and required Electronic Journal event committed atomically.',10),
    ('4322e9c1-2209-5ad4-8587-4bbe4f35a08b'::uuid,'fiscal_reprint_type','fiscal_document_copy','Fiscal Document Copy','A labeled copy of an already committed fiscal document; no new fiscal issuance or number is created.',10)
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
