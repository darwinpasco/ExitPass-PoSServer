-- GENERATED FILE - DO NOT EDIT MANUALLY.
-- Source: db/reference-data/controlled-codes/source/families/discount_privilege_type.json
-- Namespace: d07a7416-af9c-556d-86cc-7061335f7c11
BEGIN;

INSERT INTO pos.controlled_code_sets (
    controlled_code_set_id, code_set_key, display_name, description, governance_owner,
    source_ref, is_active, effective_start_at, created_at, updated_at)
VALUES (
    'cf381b39-3016-5d8a-9833-44ff70ac216d'::uuid, 'discount_privilege_type', 'Discount Privilege Type', 'Classification of a fiscal discount or privilege detail, separate from entitlement type and reporting classification.', 'Accounting',
    'ExitPass_POS_Server_Controlled_Code_Seed_Code_Family_Map.md; ExitPass_POS_Server_Applied_Statutory_Fiscal_Facts_Contract_v1.0', true, '2026-09-21T00:00:00Z'::timestamptz, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (controlled_code_set_id) DO NOTHING;

INSERT INTO pos.controlled_codes (
    controlled_code_id, controlled_code_set_id, code_key, display_name, description,
    source_ref, sort_order, is_active, effective_start_at, created_at, updated_at)
VALUES (
    '3a29922e-e8e6-5adb-a205-f68d09e41762'::uuid, 'cf381b39-3016-5d8a-9833-44ff70ac216d'::uuid, 'statutory_discount_and_vat_privilege',
    'Statutory Discount and VAT Privilege', 'An approved statutory fiscal detail carrying a discount amount and VAT privilege amount calculated by Central PMS; this code does not determine eligibility or recalculate either amount.', 'ExitPass_POS_Server_Applied_Statutory_Fiscal_Facts_Contract_v1.0', 10, true,
    '2026-09-21T00:00:00Z'::timestamptz, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (controlled_code_id) DO NOTHING;

COMMIT;
