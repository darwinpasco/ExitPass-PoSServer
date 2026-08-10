-- GENERATED FILE - DO NOT EDIT MANUALLY. Z-012B deterministic Annex E-1 codes.
BEGIN;
WITH source(id,key,name,description,owner) AS (VALUES
('424bde6f-43b0-5ced-a704-07222ce03df6'::uuid,'annex_e1_accounting_fact_status','Annex E-1 Accounting Fact Status','Recorded and explicit zero evidence states.','Accounting'),
('81afd4a4-0ab9-51cb-a52d-12d494164054'::uuid,'annex_e1_accounting_fact_type','Annex E-1 Accounting Fact Type','Approved named accounting operands.','Accounting'),
('2da8dee4-27cc-5a66-add7-7178011bfd2b'::uuid,'annex_e1_correction_reason','Annex E-1 Correction Reason','Immutable correction lineage.','Accounting'),
('7b30c3d0-935e-5d32-a335-c4ef834bb7c0'::uuid,'annex_e1_remarks','Annex E-1 Remarks','Governed row remarks.','Engineering'),
('a605d001-a04a-5fee-8a35-9031537314a8'::uuid,'annex_e1_workbook_status','Annex E-1 Workbook Status','Immutable artifact status.','Engineering'))
INSERT INTO pos.controlled_code_sets(controlled_code_set_id,code_set_key,display_name,description,governance_owner,source_ref,is_active,effective_start_at,created_at,updated_at)
SELECT id,key,name,description,owner,'Z-012B',true,'2026-08-10T00:00:00Z'::timestamptz,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP FROM source
ON CONFLICT(controlled_code_set_id) DO UPDATE SET code_set_key=EXCLUDED.code_set_key,display_name=EXCLUDED.display_name,description=EXCLUDED.description,governance_owner=EXCLUDED.governance_owner,source_ref=EXCLUDED.source_ref,is_active=true,effective_start_at=EXCLUDED.effective_start_at,updated_at=CURRENT_TIMESTAMP;
WITH source(id,set_key,code_key,name,description,sort_order) AS (VALUES
('deb73a5f-0233-596d-9e59-31758498d2d1'::uuid,'annex_e1_accounting_fact_type','manual_si_or_net_income','Manual SI/OR Net Income','Qualifying continuity or BCP manual net income.',10),
('63a0b090-33f8-5ce3-97f2-174f6d0663be'::uuid,'annex_e1_accounting_fact_type','sales_overrun_overflow_net_income','Sales Overrun/Overflow Net Income','Approved accumulated-sales-capacity overflow net income.',20),
('b66e7744-09cd-5b94-863b-25e1e147153e'::uuid,'annex_e1_accounting_fact_type','naac_discount','NAAC Discount','Zero-only bounded classification.',30),
('857c5f01-52bb-5fa6-8f97-15f070b21c03'::uuid,'annex_e1_accounting_fact_type','solo_parent_discount','Solo Parent Discount','Zero-only bounded classification.',40),
('9761c15f-3ab8-5b5a-9c93-1aff0b85de17'::uuid,'annex_e1_accounting_fact_type','other_vat_adjustment','Other VAT Adjustment','Zero-only bounded classification.',50),
('5184757b-c19d-5927-939c-234e0cc61f3c'::uuid,'annex_e1_accounting_fact_type','vat_on_returns','VAT on Returns','Zero-only bounded classification.',60),
('4e6c15fa-c38b-53c5-a97b-eaf4ff5be951'::uuid,'annex_e1_accounting_fact_type','residual_vat_adjustment','Residual VAT Adjustment','Zero-only bounded classification.',70),
('713f2db7-1059-53c6-9b2a-a7ca2013dd14'::uuid,'annex_e1_accounting_fact_status','recorded','Recorded','An authoritative value was recorded.',10),
('29a6815a-6cfe-538f-a616-243604c6d227'::uuid,'annex_e1_accounting_fact_status','attested_zero','Attested Zero','Authorized exact-period zero evidence.',20),
('bca6e858-3c1a-5641-afbb-35cfba91caa2'::uuid,'annex_e1_correction_reason','source_correction','Source Correction','Approved immutable source correction.',10),
('3324411d-f392-501c-8b97-924fd19e5f7c'::uuid,'annex_e1_correction_reason','authorized_restatement','Authorized Restatement','Approved immutable workbook restatement.',20),
('0dafc990-56aa-559f-9424-5569488a8126'::uuid,'annex_e1_remarks','none','None','No governed exception.',10),
('d74f22db-9c1f-5429-a376-ee1dd2cd5a28'::uuid,'annex_e1_remarks','no_activity','No Activity','Approved no-activity row.',20),
('f5c2b69b-7d60-5fcb-a532-728d179de646'::uuid,'annex_e1_workbook_status','committed','Committed','Artifact bytes and metadata committed.',10))
INSERT INTO pos.controlled_codes(controlled_code_id,controlled_code_set_id,code_key,display_name,description,source_ref,sort_order,is_active,effective_start_at,created_at,updated_at)
SELECT source.id,sets.controlled_code_set_id,source.code_key,source.name,source.description,'Z-012B',source.sort_order,true,'2026-08-10T00:00:00Z'::timestamptz,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP FROM source JOIN pos.controlled_code_sets sets ON sets.code_set_key=source.set_key
ON CONFLICT(controlled_code_id) DO UPDATE SET controlled_code_set_id=EXCLUDED.controlled_code_set_id,code_key=EXCLUDED.code_key,display_name=EXCLUDED.display_name,description=EXCLUDED.description,source_ref=EXCLUDED.source_ref,sort_order=EXCLUDED.sort_order,is_active=true,effective_start_at=EXCLUDED.effective_start_at,updated_at=CURRENT_TIMESTAMP;
COMMIT;
