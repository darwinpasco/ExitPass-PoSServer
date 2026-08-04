-- GENERATED FILE - DO NOT EDIT MANUALLY.
-- ExitPass POS Server governed Z close state controlled codes, Z-007B.
-- UUID v5 namespace: d07a7416-af9c-556d-86cc-7061335f7c11
-- Source: db/reference-data/controlled-codes/source/controlled_code_source_index.json

BEGIN;

WITH source(controlled_code_set_id, code_set_key, display_name, description) AS (VALUES
    ('fb9705bc-286f-5048-ac65-09394a26a7a0'::uuid, 'fiscal_z_state_identity', 'Fiscal Z State Identity', 'Strong identities for immutable transition-value evidence.'),
    ('6d19aed1-3a64-5a85-8e45-13937ecf50dd'::uuid, 'fiscal_z_state_initialization_provenance', 'Fiscal Z State Initialization Provenance', 'Approved provenance for explicit canonical Z state initialization.'),
    ('ee4d2af8-cdd7-5474-92db-a80e6250e954'::uuid, 'fiscal_z_state_transition_type', 'Fiscal Z State Transition Type', 'Initialization foundation and future atomic Z-close transition identity.')
)
INSERT INTO pos.controlled_code_sets (
    controlled_code_set_id, code_set_key, display_name, description,
    governance_owner, source_ref, is_active, effective_start_at, effective_end_at,
    created_at, updated_at
)
SELECT controlled_code_set_id, code_set_key, display_name, description,
       'Engineering', 'ExitPass_POS_Server_Z_Close_Counter_GTA_Late_Write_Boundary_Contract_v1.0; Z-007B',
       true, '2026-08-04T00:00:00Z'::timestamptz, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
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
    ('baf7bb67-9f11-5b2f-a750-e5689a5f0142'::uuid, 'fiscal_z_state_identity', 'z_counter', 'Z Counter', 'Count of committed Z closes in the governed state scope.', 10),
    ('a1dddcd8-aab5-51a8-bb64-f610edbdc1eb'::uuid, 'fiscal_z_state_identity', 'reset_counter', 'Reset Counter', 'Count of separately authorized fiscal resets; ordinary Z close preserves it.', 20),
    ('e01e75a6-6ee9-5bdf-9bf4-0a38476f8805'::uuid, 'fiscal_z_state_identity', 'grand_total_amount', 'Grand Total Amount', 'Cumulative approved VAT-inclusive final fiscal amount in integer minor units.', 30),
    ('8f31c890-2aa2-50ef-a815-0e0c8cf90983'::uuid, 'fiscal_z_state_initialization_provenance', 'approved_new_scope_zero', 'Approved New Scope Zero', 'Explicit approved initialization of reset, Z, and GTA values to zero.', 10),
    ('9ce30367-0ff0-5c5e-8d48-b4a3b3b9df71'::uuid, 'fiscal_z_state_initialization_provenance', 'verified_legacy_import', 'Verified Legacy Import', 'Explicit import of verified pre-existing reset, Z, and GTA values.', 20),
    ('21880ca5-b803-5abc-959f-def2f232ad73'::uuid, 'fiscal_z_state_transition_type', 'initialization', 'Initialization', 'Explicit new-scope zero initialization or verified legacy import.', 10),
    ('52a28fc9-7c24-5810-8f98-30dbc7c134b9'::uuid, 'fiscal_z_state_transition_type', 'z_close', 'Z Close', 'Future atomic Z-close state transition; Z-007B does not execute it.', 20)
)
INSERT INTO pos.controlled_codes (
    controlled_code_id, controlled_code_set_id, code_key, display_name, description,
    source_ref, sort_order, is_active, effective_start_at, effective_end_at,
    created_at, updated_at
)
SELECT source.controlled_code_id, code_set.controlled_code_set_id, source.code_key,
       source.display_name, source.description, 'Z-007B', source.sort_order,
       true, '2026-08-04T00:00:00Z'::timestamptz, NULL,
       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM source
JOIN pos.controlled_code_sets code_set ON code_set.code_set_key = source.set_key
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
