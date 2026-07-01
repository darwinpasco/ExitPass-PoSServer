# ExitPass POS Server Fiscal Sequence Allocation Slice

## Purpose

This slice implements runtime fiscal sequence-state allocation during `POST /v1/fiscal-documents/`.

POS Server now allocates the next fiscal sequence value for a resolved fiscal sequence policy inside the same PostgreSQL transaction that persists the fiscal document shell and child fiscal facts.

## Implemented Behavior

- Fiscal issuance idempotency and semantic request hash behavior remain in force.
- Fiscal identity and fiscal sequence policy are resolved before allocation.
- The selected `pos.fiscal_sequence_states` row is locked with row-level locking using `SELECT ... FOR UPDATE`.
- The next sequence value is computed from the locked state row.
- The fiscal document number is formatted from the resolved sequence policy prefix, suffix, and padding fields.
- The fiscal document header is inserted with:
  - `fiscal_sequence_policy_id`
  - `fiscal_sequence_value`
  - `fiscal_document_number`
  - `fiscal_series`
  - `fiscal_number_prefix_text`
  - `fiscal_number_suffix_text`
  - `fiscal_number_assigned_at`
  - `fiscal_number_assigned_by_ref`
- The sequence state is advanced in the same transaction after document shell rows are inserted and before idempotency completion.
- The API success response includes the assigned fiscal numbering fields after durable commit.
- The read endpoint returns persisted numbering fields through the existing read model.

## Idempotency and Replay

Duplicate requests with the same idempotency key and same semantic request hash replay the original fiscal document result. Replay reads the existing fiscal document numbering fields and does not allocate or advance another sequence value.

Requests with the same idempotency key but a different semantic request hash still fail closed as an idempotency conflict before sequence allocation.

## Rollback Posture

If any write fails before commit, the PostgreSQL transaction rolls back the fiscal document shell, child fiscal facts, idempotency completion, and sequence-state advancement. A pre-commit rollback does not create a durable fiscal sequence gap.

## Exclusions

This slice does not add BIR reports, Digital SI, Annex E, X/Z, electronic journal generation, POSLog generation, reprints, adjustments, reset counter mechanics, Z-counter mechanics, Grand Total Amount mechanics, recovery automation, runbooks, statutory discount validation, payment finality ownership, refund/reversal authority, ExitAuthorization, or gate behavior.

`pos.fiscal_counter_states` and `pos.fiscal_sequence_gap_audit` are not mutated by ordinary fiscal document creation in this slice.

## Authority Boundary

POS Server remains fiscal issuance authority only. Central PMS continues to own payment finality, fiscal reference recording, degraded resolve, and ExitAuthorization. POS Server does not approve entitlement, mutate Central PMS payable basis directly, activate continuity, approve manual release, or operate gates.

## Open Items

- Durable gap handling after committed issuance remains a later BIR/accounting-sensitive design slice.
- Counter/GTA/Z/reset mechanics remain separate future work.
- Recovery automation and operational runbooks remain future work.
