-- GENERATED FILE - DO NOT EDIT MANUALLY. Restart 41 fiscal issuance core codes.
BEGIN;

WITH source(id,key,name,description,owner) AS (VALUES
('2467e7aa-331d-568b-825f-d400e70ab546'::uuid,'channel_terminal_type','Channel Terminal Type','Channel classifications under a Site POS Server fiscal boundary.','Operations'),
('fc9862e1-6729-58a4-aaf6-e1527ad2916a'::uuid,'fiscal_document_status','Fiscal Document Status','Fiscal document lifecycle states owned by POS.','Accounting'),
('c1420b9a-a7aa-5526-8f8e-17aca4766e1c'::uuid,'fiscal_document_type','Fiscal Document Type','Fiscal document classifications governed by POS issuance.','Accounting'),
('4eced76a-4b1e-5e9c-956e-fd692457f1a4'::uuid,'fiscal_line_type','Fiscal Line Type','Fiscal line classifications used by Sales Invoice issuance.','Accounting'),
('a865e5a7-f371-562d-947c-b83c22c9d5ed'::uuid,'fiscal_sequence_family','Fiscal Sequence Family','Fiscal numbering families selected by document type.','Accounting'),
('0edf7abc-e1e5-52b2-8895-618328a0aaf8'::uuid,'fiscal_sequence_policy_status','Fiscal Sequence Policy Status','Lifecycle states for fiscal sequence policies.','Accounting'),
('39186960-009f-5ca5-8b9e-25f3d67d7b0c'::uuid,'fiscal_sequence_state','Fiscal Sequence State','Allocation states for persisted fiscal sequence counters.','Accounting'),
('f422d27e-2ee8-5ba2-8b95-5d32ec7e1b2a'::uuid,'fiscal_total_type','Fiscal Total Type','Fiscal total classifications used by Sales Invoice issuance.','Accounting'),
('7ed2dbfb-0a21-55c3-b2e1-1179a0cbef81'::uuid,'tax_classification','Tax Classification','Tax treatment classifications used by fiscal details.','Accounting'),
('53e69919-cf74-5386-b444-f68fe49716ed'::uuid,'tax_type','Tax Type','Tax types used by fiscal detail rows.','Accounting'),
('deb6af8c-c899-5f96-ad45-30de91efc208'::uuid,'tender_type','Tender Type','Customer tender classifications recorded on fiscal documents.','Accounting'))
INSERT INTO pos.controlled_code_sets(
    controlled_code_set_id,code_set_key,display_name,description,governance_owner,
    source_ref,is_active,effective_start_at,created_at,updated_at)
SELECT id,key,name,description,owner,'Restart 41 persistent PITX POS issuance readiness',
       true,'2026-09-04T00:00:00Z'::timestamptz,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP
FROM source
ON CONFLICT(controlled_code_set_id) DO UPDATE SET
    code_set_key=EXCLUDED.code_set_key,
    display_name=EXCLUDED.display_name,
    description=EXCLUDED.description,
    governance_owner=EXCLUDED.governance_owner,
    source_ref=EXCLUDED.source_ref,
    is_active=true,
    effective_start_at=EXCLUDED.effective_start_at,
    updated_at=CURRENT_TIMESTAMP
WHERE (controlled_code_sets.code_set_key,
       controlled_code_sets.display_name,
       controlled_code_sets.description,
       controlled_code_sets.governance_owner,
       controlled_code_sets.source_ref,
       controlled_code_sets.is_active,
       controlled_code_sets.effective_start_at)
  IS DISTINCT FROM
      (EXCLUDED.code_set_key,
       EXCLUDED.display_name,
       EXCLUDED.description,
       EXCLUDED.governance_owner,
       EXCLUDED.source_ref,
       true,
       EXCLUDED.effective_start_at);

WITH source(id,set_key,code_key,name,description,sort_order) AS (VALUES
('717d00fa-967e-5220-9023-4a03bc84876e'::uuid,'channel_terminal_type','webpay','WebPay','Logical WebPay channel; not an independent fiscal authority.',10),
('9f868073-a648-5b1d-99c0-f96d598c4eb0'::uuid,'fiscal_document_status','issued','Issued','Original fiscal document committed with an assigned fiscal number.',10),
('b9fc7458-57a7-5bbe-809d-8c8b3d780b1d'::uuid,'fiscal_document_type','sales_invoice','Sales Invoice','Original Sales Invoice fiscal document.',10),
('09997efc-e7b4-5cfb-8904-3020e01de4a9'::uuid,'fiscal_line_type','parking_fee','Parking Fee','Parking service fee line.',10),
('0f9c7b11-4732-5faf-8ddb-4d987a7fd92a'::uuid,'fiscal_sequence_family','sales_invoice','Sales Invoice','Sales Invoice numbering family.',10),
('b9e6cd08-090a-5a82-ab27-2f595ae68f6b'::uuid,'fiscal_sequence_policy_status','active','Active','Policy is approved for current fiscal number allocation.',10),
('716901c2-7c21-5cf9-9732-257168b48ff1'::uuid,'fiscal_sequence_state','active','Active','Sequence state is available for locked runtime allocation.',10),
('6abfc6aa-b94e-5a24-b533-8d18a33c468b'::uuid,'fiscal_total_type','payable_total','Payable Total','Authoritative final payable amount.',10),
('ab180f41-e181-5579-b9f1-5ae7a840a946'::uuid,'tax_classification','vatable','VATable','VATable fiscal amount.',10),
('328dcb64-584a-5f59-a304-2e5189a2aa83'::uuid,'tax_type','vat','VAT','Value-added tax.',10),
('a4bd3153-076e-564f-af56-3532be501e95'::uuid,'tender_type','card','Card','Customer selected card payment.',10))
INSERT INTO pos.controlled_codes(
    controlled_code_id,controlled_code_set_id,code_key,display_name,description,
    source_ref,sort_order,is_active,effective_start_at,created_at,updated_at)
SELECT source.id,sets.controlled_code_set_id,source.code_key,source.name,source.description,
       'Restart 41 persistent PITX POS issuance readiness',source.sort_order,true,
       '2026-09-04T00:00:00Z'::timestamptz,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP
FROM source
JOIN pos.controlled_code_sets sets ON sets.code_set_key=source.set_key
ON CONFLICT(controlled_code_id) DO UPDATE SET
    controlled_code_set_id=EXCLUDED.controlled_code_set_id,
    code_key=EXCLUDED.code_key,
    display_name=EXCLUDED.display_name,
    description=EXCLUDED.description,
    source_ref=EXCLUDED.source_ref,
    sort_order=EXCLUDED.sort_order,
    is_active=true,
    effective_start_at=EXCLUDED.effective_start_at,
    updated_at=CURRENT_TIMESTAMP
WHERE (controlled_codes.controlled_code_set_id,
       controlled_codes.code_key,
       controlled_codes.display_name,
       controlled_codes.description,
       controlled_codes.source_ref,
       controlled_codes.sort_order,
       controlled_codes.is_active,
       controlled_codes.effective_start_at)
  IS DISTINCT FROM
      (EXCLUDED.controlled_code_set_id,
       EXCLUDED.code_key,
       EXCLUDED.display_name,
       EXCLUDED.description,
       EXCLUDED.source_ref,
       EXCLUDED.sort_order,
       true,
       EXCLUDED.effective_start_at);

COMMIT;
