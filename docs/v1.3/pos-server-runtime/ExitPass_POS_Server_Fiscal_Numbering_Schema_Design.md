# ExitPass POS Server Fiscal Numbering Schema Design

## 1. Decision Summary

Option A is selected for schema design: add dedicated nullable fiscal numbering columns directly to `pos.fiscal_documents`.

`document_context` is not authoritative fiscal number storage. It may retain non-authoritative context only; formal fiscal number identity must use typed relational columns with foreign keys, local checks, and uniqueness posture.

Runtime number allocation remains blocked until the future SQL slice, rebuild validation, and runtime persistence updates are complete. This design does not create SQL, modify `db/state`, create migrations, mutate counters, or implement allocation.

## 2. Proposed Columns

### `fiscal_identity_id`

- Data type: `uuid`
- Nullable rollout posture: nullable initially.
- Meaning: fiscal identity/registration context used for the fiscal document number.
- Source/relationship: references `pos.fiscal_identities (fiscal_identity_id)`.
- Validation/check rule: FK only in the initial SQL slice.
- Eventually required: likely yes for new fiscalized documents, after fiscal identity selection rules are approved and existing rows are handled.

### `fiscal_sequence_policy_id`

- Data type: `uuid`
- Nullable rollout posture: nullable initially.
- Meaning: sequence policy used to allocate the fiscal number.
- Source/relationship: references `pos.fiscal_sequence_policies (fiscal_sequence_policy_id)`.
- Validation/check rule: FK plus paired-field checks with `fiscal_sequence_value`, `fiscal_document_number`, and `fiscal_number_assigned_at`.
- Eventually required: yes for new documents that receive POS Server fiscal numbers.

### `fiscal_sequence_value`

- Data type: `bigint`
- Nullable rollout posture: nullable initially.
- Meaning: numeric sequence value allocated from the chosen sequence policy.
- Source/relationship: allocated by future runtime using locked `pos.fiscal_sequence_states`.
- Validation/check rule: `fiscal_sequence_value IS NULL OR fiscal_sequence_value > 0`.
- Eventually required: yes for new numbered fiscal documents.

### `fiscal_document_number`

- Data type: `text`
- Nullable rollout posture: nullable initially.
- Meaning: formatted durable fiscal document number returned/read as the human and audit-facing fiscal number.
- Source/relationship: formatted from sequence policy and allocated sequence value at assignment time.
- Validation/check rule: nonblank when present.
- Eventually required: yes for new numbered fiscal documents.

### `fiscal_series`

- Data type: `text`
- Nullable rollout posture: nullable initially.
- Meaning: optional fiscal series/book/register code if required by policy or accreditation posture.
- Source/relationship: future sequence policy selection or BIR/accreditation configuration.
- Validation/check rule: nonblank when present.
- Eventually required: unknown; defer until sequence scope and accreditation expectations are confirmed.

### `fiscal_number_prefix_text`

- Data type: `text`
- Nullable rollout posture: nullable initially.
- Meaning: prefix copied from the sequence policy at allocation time so historical formatting remains reconstructable after policy changes.
- Source/relationship: `pos.fiscal_sequence_policies.prefix_text`.
- Validation/check rule: nonblank when present.
- Eventually required: no, unless a policy uses a prefix.

### `fiscal_number_suffix_text`

- Data type: `text`
- Nullable rollout posture: nullable initially.
- Meaning: suffix copied from the sequence policy at allocation time so historical formatting remains reconstructable after policy changes.
- Source/relationship: `pos.fiscal_sequence_policies.suffix_text`.
- Validation/check rule: nonblank when present.
- Eventually required: no, unless a policy uses a suffix.

### `fiscal_number_assigned_at`

- Data type: `timestamptz`
- Nullable rollout posture: nullable initially.
- Meaning: timestamp when the fiscal number assignment became part of the durable database transaction.
- Source/relationship: future runtime allocation transaction.
- Validation/check rule: required when any formal number fields are assigned.
- Eventually required: yes for new numbered fiscal documents.

### `fiscal_number_assigned_by_ref`

- Data type: `text`
- Nullable rollout posture: nullable initially.
- Meaning: optional service/actor reference for the allocator, stored as reference only.
- Source/relationship: future runtime service identity configuration or request context.
- Validation/check rule: nonblank when present.
- Eventually required: not initially; may become required if operational audit policy requires service identity on allocation.

### `fiscal_number_allocation_status_code_id`

- Data type: `uuid`
- Nullable rollout posture: deferred for the first SQL slice.
- Meaning: optional controlled-code status for number allocation posture if future design needs assigned/reserved/void/gap-like local states separate from fiscal document status.
- Source/relationship: would reference `pos.controlled_codes (controlled_code_id)`.
- Validation/check rule: FK if implemented.
- Eventually required: not recommended now.

Recommendation: defer `fiscal_number_allocation_status_code_id`. Existing document status, sequence state, and gap audit posture are enough for the first schema design. Adding a new status field now would require controlled-code values before allocation semantics are approved.

## 3. Foreign Key Relationships

Recommended FK posture for the future SQL slice:

- `fiscal_identity_id` references `pos.fiscal_identities (fiscal_identity_id)`.
- `fiscal_sequence_policy_id` references `pos.fiscal_sequence_policies (fiscal_sequence_policy_id)`.
- `fiscal_number_allocation_status_code_id` references `pos.controlled_codes (controlled_code_id)` only if the column is later approved.

No FK should point to Central PMS, payment systems, exit authorization, or gate systems. Upstream values remain references only in existing reference fields.

## 4. Uniqueness and Index Posture

Recommended uniqueness:

- Partial unique index on `(fiscal_sequence_policy_id, fiscal_sequence_value)` where both values are not null.
- Partial unique index on `(fiscal_sequence_policy_id, fiscal_document_number)` where both values are not null.

Rationale:

- sequence value uniqueness is the strongest allocator safety rule within a policy;
- formatted number uniqueness protects read/report/audit lookups;
- policy scope is clearer than fiscal identity scope because sequence policies already bind site, sequence family, optional document type, prefix/suffix, padding, and status posture.

Optional later uniqueness:

- `(fiscal_identity_id, fiscal_document_number)` where both are not null, if BIR/accreditation confirms formatted number uniqueness must be identity-scoped.

Recommended lookup indexes:

- index on `fiscal_identity_id` for identity-based audits;
- index on `fiscal_sequence_policy_id` for allocation/read queries;
- index on `fiscal_document_number` for direct read endpoint or operational lookup;
- optional composite index on `(fiscal_sequence_policy_id, fiscal_document_number)` if not already covered by the unique index.

Recommended local checks:

- `fiscal_sequence_value IS NULL OR fiscal_sequence_value > 0`;
- text fields are null or nonblank: `fiscal_document_number`, `fiscal_series`, `fiscal_number_prefix_text`, `fiscal_number_suffix_text`, `fiscal_number_assigned_by_ref`;
- paired assignment check: either all core assignment fields are null, or `fiscal_sequence_policy_id`, `fiscal_sequence_value`, `fiscal_document_number`, and `fiscal_number_assigned_at` are all present.

The paired assignment check should not require `fiscal_identity_id` in the initial nullable rollout until identity selection is approved.

## 5. Rollout Posture

Initial rollout should be nullable:

- add nullable columns;
- add FKs and safe local checks;
- add partial unique indexes for assigned rows only;
- do not invent historical fiscal numbers;
- do not backfill from non-authoritative context fields;
- backfill only from an authoritative source if one exists and is approved;
- validate rebuild and disposable PostgreSQL application before runtime allocation work.

Later tightening may include:

- requiring formal fiscal number fields for new fiscalized documents;
- requiring `fiscal_identity_id` for new fiscalized documents;
- adding or tightening uniqueness by fiscal identity if required;
- adding allocation status only if reserved/assigned/finalized semantics are approved.

No migration should mutate production/shared data during local validation.

## 6. Runtime Impact

Future `POST /v1/fiscal-documents` changes:

- resolve fiscal identity and sequence policy after validation and idempotency checks;
- lock the relevant sequence state row;
- allocate sequence value and formatted number inside the same transaction as document persistence;
- return fiscal number only after durable commit.

Future `GET /v1/fiscal-documents/{fiscalDocumentId}` changes:

- include fiscal identity ID, sequence policy ID, sequence value, formatted fiscal number, series, prefix/suffix copies, assignment timestamp, and assigned-by reference when present.

Future `PostgresFiscalDocumentRepository` changes:

- extend header insert/update mapping to persist fiscal numbering fields;
- lock `pos.fiscal_sequence_states` with transaction-safe row-level locking;
- update sequence state in the same transaction as the current fiscal document shell.

Future `PostgresFiscalDocumentReader` changes:

- select the new fiscal numbering fields from `pos.fiscal_documents`;
- expose them in `FiscalDocumentReadModel`.

Future disposable PostgreSQL API smoke test changes:

- seed/prepare sequence policy/state fixture rows;
- POST a valid fiscal document;
- verify assigned number fields on the document row;
- GET the document and verify the read model includes fiscal number fields;
- verify retry/idempotency does not allocate a second number when idempotency behavior is implemented.

Future idempotency behavior:

- same idempotency scope/key and semantic request hash should replay the same fiscal document and fiscal number after commit;
- conflicting request hash should fail deterministically;
- idempotency remains fiscal side-effect protection only and does not imply payment finality, exit authorization, or gate behavior.

## 7. Controlled-Code Impact

`fiscal_number_allocation_status_code_id` should be deferred.

Reasons:

- allocation statuses are not yet approved;
- existing fiscal document status and sequence state can represent the initial posture;
- adding this field now would force controlled-code seed/governance work before assignment semantics are clear;
- gap handling already has `pos.fiscal_sequence_gap_audit.gap_reason_code_id`.

Controlled-code work still likely needed before runtime allocation:

- sequence family values;
- sequence policy status values;
- sequence state values;
- gap reason values;
- idempotency operation type/status values for fiscal document creation.

Those values should be created in separate controlled-code source/generated SQL slices, not in the schema design task.

## 8. SQL Implementation Plan

Future SQL slice likely affects:

- `db/state/tables/pos.fiscal_documents.sql`

Expected SQL changes:

- add nullable fiscal numbering columns;
- add FKs to `pos.fiscal_identities` and `pos.fiscal_sequence_policies`;
- add local checks for positive sequence value and nonblank optional text;
- add paired-assignment check;
- add partial unique indexes for assigned fiscal sequence value and formatted number;
- update table and column comments;
- keep `CREATE TABLE IF NOT EXISTS` style if editing state SQL follows existing repo posture;
- avoid functions, triggers, PostgreSQL sequences, seed data, migrations, and runtime code in that slice.

Validation package impact:

- no expected table count change if Option A modifies only `pos.fiscal_documents`;
- static validation should still pass;
- future review should verify constraint/index names remain within PostgreSQL's 63-byte identifier limit;
- disposable rebuild should verify the modified table applies cleanly from an empty database.

No SQL files are created or modified in this task.

## 9. Stop / Go Recommendation

OK to proceed to a schema SQL slice after review of this design.

Not OK to implement runtime number allocation yet.

Not OK to proceed to BIR reports from this design alone.

Not OK to use `document_context` as authoritative fiscal number storage.
