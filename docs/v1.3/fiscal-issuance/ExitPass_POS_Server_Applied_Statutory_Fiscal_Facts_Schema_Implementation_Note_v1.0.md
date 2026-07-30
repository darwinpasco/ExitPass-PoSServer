# ExitPass POS Server Applied Statutory Fiscal Facts Schema Implementation Note v1.0

## Decision

Z-004 resolves the Z-003 `BLOCKED_BY_PERSISTENCE_SCHEMA` blocker at the database layer only.

This slice adds first-class, normalized PostgreSQL storage for the Z-002 `appliedStatutoryFiscalFacts` contract. It does not activate the public create endpoint, change runtime DTOs, implement `pos-server-fiscal-document-create:sha256:v2`, alter readback, or change Sales Invoice presentation.

Runtime remains blocked until Z-003 resumes on top of this schema and integrates validation, hashing, transactional persistence, readback, and presentation.

## Contract-To-Column Mapping

| Z-002 fact | Schema mapping |
| --- | --- |
| Fiscal document identity | `pos.fiscal_document_applied_statutory_facts.fiscal_document_id`, FK to `pos.fiscal_documents` |
| Statutory decision command ID | `statutory_discount_decision_command_id`, required, unique |
| Statutory request reference | `statutory_request_reference`, required, unique |
| Payable-basis application command ID | `statutory_payable_basis_application_command_id`, required, unique |
| Statutory validation ID | `statutory_validation_id`, required, indexed |
| Parking session ID | `parking_session_id`, required, indexed |
| Site ID | `site_id`, required, indexed |
| Site Group ID | `site_group_id`, required, indexed |
| Entitlement type | `entitlement_type_code_id`, FK to controlled codes |
| Benefit classification | `benefit_classification_code_id`, FK to controlled codes |
| Policy reference resolution basis | `policy_resolution_basis_code_id`, FK to controlled codes |
| Applied policy reference ID | `applied_policy_reference_id`, optional, indexed when present |
| Policy code | `policy_code`, optional nonblank safe reference |
| Policy version ID | `policy_version_id`, optional |
| National law reference | `national_law_reference`, optional nonblank safe reference |
| Ordinance reference | `ordinance_reference`, optional nonblank safe reference |
| Original tariff snapshot ID | `original_tariff_snapshot_id`, required |
| Applied tariff snapshot ID | `applied_tariff_snapshot_id`, required, indexed |
| Original amount | `original_amount_minor_units`, required, nonnegative |
| VAT-exclusive basis | `vat_exclusive_basis_amount_minor_units`, required, nonnegative |
| VAT amount | `vat_amount_minor_units`, required, nonnegative |
| VAT treatment | `vat_treatment_code_id`, FK to controlled codes |
| Statutory discount amount | `statutory_discount_amount_minor_units`, required, nonnegative |
| Final payable amount | `final_payable_amount_minor_units`, required, nonnegative |
| Currency | `currency_code`, required, uppercase three-character check |
| Applied timestamp | `applied_at`, required |
| Source payment channel | `source_payment_channel_code_id`, FK to controlled codes |
| Terminal-cash tender ID | `terminal_cash_tender_id`, optional, indexed when present |
| Snapshot timestamp | `snapshot_created_at`, required |

Payment attempt, payment confirmation, and payment finality references remain represented by the parent fiscal document and tender tables. The statutory child table does not create POS-owned payment lifecycles.

## Controlled Codes

Added controlled-code source families and generated slice-4 SQL for:

- `statutory_entitlement_type`: `SENIOR_CITIZEN`, `PWD`
- `statutory_benefit_classification`: `VAT_EXEMPTION_ONLY`, `STATUTORY_DISCOUNT_ONLY`, `VAT_EXEMPTION_AND_STATUTORY_DISCOUNT`, `FREE_PARKING`, `REDUCED_PARKING_RATE`, `CAPPED_PARKING_FEE`
- `statutory_vat_treatment`: `VAT_EXEMPT`, `VAT_EXCLUSIVE`, `VAT_INCLUSIVE_NO_EXEMPTION`, `NON_VAT`, `ZERO_RATED`, `NOT_APPLICABLE`
- `statutory_source_payment_channel`: `WEBPAY`, `ASSISTED_PAYMENT_TERMINAL`, `OPERATOR_CONSOLE`
- `statutory_policy_resolution_basis`: `NATIONAL_LAW`, `LOCAL_ORDINANCE`, `MIXED`, `INTERNAL_POLICY_REFERENCE`
- `statutory_validation_error_code`: safe future runtime validation codes from the Z-002 handoff

## Keys, Constraints, And Indexes

`pos.fiscal_document_applied_statutory_facts` has:

- primary key on `fiscal_document_applied_statutory_fact_id`;
- FK to `pos.fiscal_documents`;
- FKs to `pos.controlled_codes` for entitlement, benefit, policy-resolution basis, VAT treatment, and source channel;
- fixed approved-code check constraints for those Z-002 controlled-code families, so an existing code from the wrong family cannot satisfy the statutory table;
- unique `fiscal_document_id` to enforce one statutory snapshot per fiscal document;
- unique decision, request, and application references to prevent duplicate fiscalization of the same final Central PMS statutory application chain;
- nonblank checks for optional safe text references;
- at-least-one safe policy reference check;
- nonnegative monetary checks;
- final amount consistency check: final payable plus statutory discount cannot exceed original amount;
- uppercase currency check;
- snapshot timestamp ordering check;
- audit timestamp immutability check;
- reconciliation indexes for validation, parking session, site, site group, entitlement, benefit, policy, tariff, VAT treatment, source channel, and terminal-cash tender references.

The statutory validation ID is indexed but not unique. A later runtime slice may reject reuse if Central PMS makes it one-to-one, but this schema avoids imposing accidental uniqueness before that integration is proven.

## Immutability

The table is an immutable fiscal snapshot. Direct `UPDATE` and `DELETE` are blocked by the approved `pos.reject_applied_stat_facts_mutation()` trigger function and `trg_fiscal_doc_applied_stat_facts_immutable` trigger.

The repository database validation script now allows only this named function and trigger. Any other function or trigger in `pos` remains drift/prohibited inventory.

Parent fiscal-document deletion remains restricted by the child FK. The schema does not add cascade delete.

## Privacy Exclusions

The table intentionally has no columns for:

- raw Senior Citizen ID or PWD ID;
- beneficiary name, birth date, address, or image;
- evidence content, Base64, object-storage URL, or evidence token;
- reviewer, approver, representative, caregiver, companion, or driver identity;
- raw ordinance or policy text;
- credentials, authorization headers, passwords, secrets, or service keys;
- request JSON, arbitrary metadata, notes, payloads, or generic blobs.

## Apply Order And Validation

The new table is applied after `pos.fiscal_document_header_snapshots` and before fiscal-document detail/status tables.

Updated validation artifacts:

- `db/rebuild/pos_sql_apply_order.txt`
- `db/validation/pos_expected_inventory.json`
- `db/validation/pos_prohibited_patterns.json`
- `db/scripts/Invoke-PosDbChecks.ps1`
- `db/validation/pos_applied_statutory_facts_expected_inventory.json`
- `db/validation/fixtures/pos_applied_statutory_facts_schema_proof.sql`

The proof fixture validates valid insert, ordinary document absence, duplicate ownership, duplicate application reference, unknown controlled codes, currency format, negative and contradictory monetary values, immutable update/delete, parent delete restriction, rollback, expected inventory, and prohibited-column absence.

## Runtime Handoff

After Z-004 merges, resume Z-003:

- remove the schema-blocked guard for statutory requests only after repository persistence integration exists;
- resolve code IDs from the seeded statutory controlled-code families;
- implement `pos-server-fiscal-document-create:sha256:v2` for statutory requests while preserving ordinary `sha256:v1`;
- persist the statutory row atomically with fiscal document creation;
- expose safe readback and Sales Invoice presentation fields from this immutable snapshot;
- keep prohibited identity and evidence fields rejected and unlogged.

Controlled UAT remains unauthorized.

Production rollout remains unauthorized.
