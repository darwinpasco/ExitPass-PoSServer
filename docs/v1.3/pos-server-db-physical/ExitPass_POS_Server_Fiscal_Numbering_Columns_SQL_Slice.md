# ExitPass POS Server Fiscal Numbering Columns SQL Slice

## 1. Summary

This SQL slice adds dedicated nullable fiscal numbering columns to `pos.fiscal_documents`.

The change implements the approved Option A schema posture from the fiscal numbering schema design package:

- fiscal numbering fields live directly on `pos.fiscal_documents`;
- `document_context` is not authoritative fiscal number storage;
- fiscal number assignment remains future runtime behavior;
- sequence/counter mutation remains blocked until a separate runtime allocation slice.

This slice does not allocate fiscal numbers, does not mutate `pos.fiscal_sequence_states`, does not mutate `pos.fiscal_counter_states`, and does not add runtime/API behavior.

## 2. Columns Added

The following nullable columns were added to `pos.fiscal_documents`:

- `fiscal_identity_id uuid null`
- `fiscal_sequence_policy_id uuid null`
- `fiscal_sequence_value bigint null`
- `fiscal_document_number text null`
- `fiscal_series text null`
- `fiscal_number_prefix_text text null`
- `fiscal_number_suffix_text text null`
- `fiscal_number_assigned_at timestamptz null`
- `fiscal_number_assigned_by_ref text null`

`fiscal_number_allocation_status_code_id` was not added. Allocation status remains deferred until allocation semantics and controlled-code values are approved.

## 3. Foreign Keys

The following FK constraints were added:

- `fk_fiscal_documents__fiscal_identity`
  - `fiscal_identity_id` references `pos.fiscal_identities (fiscal_identity_id)`
- `fk_fiscal_documents__sequence_policy`
  - `fiscal_sequence_policy_id` references `pos.fiscal_sequence_policies (fiscal_sequence_policy_id)`

These references are nullable for the initial rollout.

## 4. Checks

The slice adds local checks for assigned fiscal numbering posture:

- `fiscal_sequence_value` must be positive when present.
- `fiscal_document_number` must be nonblank when present.
- `fiscal_series` must be nonblank when present.
- `fiscal_number_prefix_text` must be nonblank when present.
- `fiscal_number_suffix_text` must be nonblank when present.
- `fiscal_number_assigned_by_ref` must be nonblank when present.
- Core assignment fields must be paired:
  - either all core assignment fields are null;
  - or `fiscal_sequence_policy_id`, `fiscal_sequence_value`, `fiscal_document_number`, and `fiscal_number_assigned_at` are all present.

The paired assignment check intentionally does not require `fiscal_identity_id` during the initial nullable rollout.

## 5. Indexes

The slice adds partial unique indexes for assigned fiscal numbers:

- `ux_fiscal_documents__seq_policy_value`
  - unique on `(fiscal_sequence_policy_id, fiscal_sequence_value)` where both are not null
- `ux_fiscal_documents__seq_policy_number`
  - unique on `(fiscal_sequence_policy_id, fiscal_document_number)` where both are not null

The slice also adds lookup indexes:

- `ix_fiscal_documents__fiscal_identity`
- `ix_fiscal_documents__seq_policy`
- `ix_fiscal_documents__document_number`

These indexes support future allocation validation and read/audit lookup without requiring any runtime allocation in this slice.

## 6. Rebuild Manifest

`db/rebuild/pos_sql_apply_order.txt` was updated to apply `pos.fiscal_sequence_policies.sql` before `pos.fiscal_documents.sql`.

This manifest change is required because `pos.fiscal_documents` now has a foreign key to `pos.fiscal_sequence_policies`.

No table inventory update was required because `db/validation/pos_expected_inventory.json` tracks schema/table inventory, not columns, constraints, or indexes.

## 7. Rollout Posture

The rollout is intentionally nullable:

- existing rows are not forced to have fiscal numbers;
- no invented historical fiscal numbers are created;
- no backfill is performed;
- future backfill must use an approved authoritative source only;
- later non-null tightening must wait until runtime allocation and validation are proven.

`document_context` remains available for non-authoritative context only. It must not be used as the authoritative fiscal number store.

## 8. Future Runtime Work

Runtime fiscal number allocation remains blocked until a separate implementation slice.

That future slice must:

- validate first;
- perform idempotency checks;
- lock the relevant sequence state inside a PostgreSQL transaction;
- allocate sequence value and formatted fiscal number;
- persist numbering fields and fiscal document rows in one transaction;
- mutate sequence/counter state only inside the approved transaction boundary;
- return the allocated fiscal number only after durable commit.

## 9. Non-Goals

This slice does not add:

- runtime fiscal number allocation;
- fiscal sequence or counter mutation;
- BIR reporting;
- Digital SI;
- Annex E;
- X/Z behavior;
- statutory discount validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution;
- migrations;
- seed/reference data;
- controlled-code values.
