# ExitPass POS Server Fiscal Numbering Allocation Design

## 1. Decision Summary

Fiscal number allocation should happen during `POST /v1/fiscal-documents`.

Allocation must happen inside the same PostgreSQL transaction as fiscal document creation. The allocator must use row-level locking on the selected `pos.fiscal_sequence_states` row and must return the fiscal document number only after durable commit.

Runtime allocation implementation remains future work. This design does not change runtime code, mutate sequence/counter state, modify `db/state`, create migrations, or add BIR/reporting behavior.

`document_context` is not authoritative fiscal number storage. The authoritative persistence target is the nullable fiscal numbering columns on `pos.fiscal_documents`.

## 2. Required Inputs and Selection Rules

Future allocation must resolve these inputs before assigning a fiscal number:

- `site_pos_server_id`: already required by the current command/API and must identify the Site POS Server fiscal boundary.
- `channel_terminal_id`: optional supporting context. It may help resolve terminal/session posture, but must not create independent terminal fiscal authority.
- `fiscal_identity_id`: must be resolved before numbered issuance is enabled. Preferred source is active Site POS Server fiscal identity posture, likely through `pos.site_pos_server_fiscal_identity_history`. If no safe identity is resolved when required, fail closed.
- `fiscal_document_type_code_id`: already required by the current command/API and should be used for document-type-specific sequence policy matching.
- `fiscal_sequence_policy_id`: must be selected by trusted server-side resolution, not from arbitrary client input in the public API.
- `fiscal_sequence_state` row: must be the single state row for the selected sequence policy and must be locked with row-level lock semantics.
- Fiscal series/prefix/suffix/padding: should be copied from `pos.fiscal_sequence_policies` at assignment time so historical formatting remains reconstructable after policy changes.
- Service identity/assigned-by reference: should be a configured POS Server service identity reference, not a client-provided authority claim.

Current gaps:

- current public command/API does not expose an idempotency key;
- approved controlled-code values for sequence family/status and policy status are not fully established;
- exact fiscal identity selection rule is not yet implemented;
- exact service identity source for `fiscal_number_assigned_by_ref` is not yet approved.

## 3. Sequence Policy Resolution

Recommended policy resolution:

1. Match `pos.fiscal_sequence_policies.site_pos_server_id` to the document `site_pos_server_id`.
2. Prefer a policy whose `document_type_code_id` equals the document `fiscal_document_type_code_id`.
3. If no document-type-specific policy exists, allow a site-level policy with `document_type_code_id is null` only if the sequence family/status rules explicitly allow it.
4. Require active/effective policy posture:
   - `effective_start_at <= current_timestamp`;
   - `effective_end_at is null or effective_end_at > current_timestamp`;
   - policy status must be an approved active/usable controlled-code value.
5. Fail closed if zero policies match.
6. Fail closed if multiple policies match with the same precedence.
7. Do not infer policy from `document_context`.
8. Do not allow public callers to directly select arbitrary `fiscal_sequence_policy_id`.

Trusted/internal selection hints may be considered later only if a separate design constrains authority, caller identity, and allowed scope.

## 4. Fiscal Identity Selection

Fiscal identity selection options:

- derive from active Site POS Server fiscal identity history;
- derive from channel terminal only as supporting context and only if it resolves to the same approved Site POS Server fiscal boundary;
- derive from selected sequence policy only if a future schema/design explicitly carries identity on the policy;
- fail closed when identity is required but cannot be resolved safely.

Terminal identity must not create independent terminal fiscal authority. A channel terminal can help identify the transaction context, but Site POS Server remains the fiscal boundary.

Open question: whether `fiscal_identity_id` should be required for all newly numbered fiscal documents in the first allocation implementation or staged after identity history rules are fully approved.

## 5. Idempotency Design

Fiscal number allocation must be idempotency-protected.

Required behavior:

- `POST /v1/fiscal-documents` must eventually require or derive an idempotency scope/key for fiscal document creation.
- Same idempotency key plus same semantic request hash returns the same fiscal document and fiscal number after commit.
- Same idempotency key plus different semantic request hash returns a deterministic conflict.
- Transient failure before commit must not allocate a second durable number on retry.
- `completion_unknown` must be handled conservatively when a prior attempt may have committed but the caller did not receive a result.
- Idempotency records must link to the committed fiscal document when allocation succeeds.
- Idempotency does not imply payment finality, exit authorization, refund/reversal authority, or gate behavior.

Current gap: the public command/API does not yet expose an explicit idempotency key. Implementation must resolve this before allocation code is added. Candidate sources are an explicit trusted request field/header, or a server-derived scope/key from approved upstream payable-basis/finality references. That choice requires approval before implementation.

## 6. Transaction Order

Future allocation transaction order:

1. Validate request and authority-boundary guardrails.
2. Compute semantic request hash.
3. Open PostgreSQL transaction.
4. Create or lock the idempotency record for the scope/key.
5. If idempotency record is completed with the same semantic hash, replay the linked fiscal document result without allocating a new number.
6. If idempotency record has a different semantic hash, fail with deterministic conflict.
7. Resolve fiscal identity and sequence policy.
8. Check fiscal lock state and fail closed if issuance is blocked.
9. Lock the selected `pos.fiscal_sequence_states` row with `SELECT ... FOR UPDATE` or equivalent.
10. Compute the next sequence value.
11. Format `fiscal_document_number` from copied policy prefix, sequence value, padding, suffix, and any approved series rule.
12. Insert fiscal document header with fiscal numbering fields.
13. Insert status history, document links, lines, tenders, tax details, discount/privilege details, and totals.
14. Update `pos.fiscal_sequence_states` in the same transaction.
15. Update idempotency record with linked fiscal document and replay result reference.
16. Commit.
17. Return success with fiscal document ID and fiscal document number only after commit.

Expected failures inside the transaction must roll back the entire transaction and return deterministic errors without leaking connection details.

## 7. Locking and Concurrency

Lock target:

- the single `pos.fiscal_sequence_states` row for the selected `fiscal_sequence_policy_id`.

Locking rules:

- use PostgreSQL row-level locking inside the same transaction as document persistence;
- set an explicit lock timeout or command timeout;
- fail closed with deterministic `fiscal_sequence_lock_failed` if the lock cannot be acquired safely;
- do not use application-memory counters;
- do not rely on optimistic duplicate-number errors as the primary allocator control;
- keep partial unique indexes on `pos.fiscal_documents` as defense in depth.

The allocator must avoid duplicate-number races under concurrent POSTs for the same sequence policy.

## 8. Rollback and Gap Behavior

Rollback before commit does not create a durable gap because no assigned sequence value is committed.

Committed numbers must not be reused. Failure after durable commit is not silently renumbered.

Gap audit rules:

- write `pos.fiscal_sequence_gap_audit` only for durably recognized gaps, reservations, or issuance failures as defined by approved BIR/accounting policy;
- do not write gap audit for ordinary transaction rollback before commit;
- do not silently renumber;
- do not reuse committed or issued fiscal numbers;
- treat reservation reuse as not allowed unless explicitly approved.

The first allocation implementation should avoid durable pre-reservation unless a later approved workflow requires it.

## 9. Counter State Interaction

Ordinary fiscal document creation should not mutate `pos.fiscal_counter_states` in the first allocation implementation unless exact counter families and formulas are approved.

Required for first allocation:

- mutate `pos.fiscal_sequence_states` for sequence allocation.

Deferred:

- `pos.fiscal_counter_states` mutation;
- GTA behavior;
- Z/reset counter behavior;
- fiscal state snapshot behavior;
- BIR/accounting-sensitive counter formulas.

Counter state and snapshot behavior should be separate BIR/accounting-sensitive slices.

## 10. API Behavior and Response

Future API behavior:

- successful create returns `fiscalDocumentId` and `fiscalDocumentNumber` only after durable commit;
- idempotent replay returns the same fiscal document and fiscal number;
- idempotency conflict returns deterministic conflict;
- expected allocation failures do not return HTTP 500;
- `GET /v1/fiscal-documents/{fiscalDocumentId}` continues to show persisted nullable/populated fiscal numbering fields;
- no payment finality, refund/reversal, exit, or gate authority is created by allocation success.

Proposed error codes:

- `fiscal_sequence_policy_not_found`
- `fiscal_sequence_policy_ambiguous`
- `fiscal_identity_not_resolved`
- `fiscal_sequence_state_not_available`
- `fiscal_sequence_lock_failed`
- `fiscal_number_allocation_failed`
- `fiscal_number_idempotency_conflict`
- `fiscal_number_completion_unknown`

## 11. Test Strategy

Implementation acceptance should require:

- unit tests for sequence policy resolution and ambiguity handling;
- unit tests for fiscal identity selection and fail-closed gaps;
- unit tests for idempotency replay, conflict, and completion-unknown handling;
- repository tests proving `SELECT ... FOR UPDATE` row-lock SQL posture;
- transaction-order tests proving idempotency, sequence state update, document insert, child inserts, and commit ordering;
- duplicate/retry tests proving same idempotency key does not allocate a second number;
- rollback tests proving no committed document and no sequence state advance on failure before commit;
- disposable PostgreSQL concurrency smoke test if feasible;
- disposable API smoke proving read endpoint shows allocated fiscal number fields after successful create;
- tests confirming no BIR report, Digital SI, Annex E, X/Z, statutory discount validation, payment finality, refund/reversal authority, exit, or gate behavior is introduced.

## 12. Implementation Sequence Recommendation

Recommended future slices:

A. Idempotency input/command design.

B. Sequence policy and fiscal identity resolver design/implementation.

C. Allocation repository SQL with row-level lock and formatting logic.

D. POST transaction integration for idempotency, allocation, document persistence, and sequence state mutation.

E. Read model/API smoke update proving allocated fields are returned after create.

F. Optional counter/GTA/Z/BIR-sensitive work after accounting/accreditation approval.

Do not combine counter/GTA/BIR-sensitive behavior with the first allocation implementation.

## 13. Non-Goals

This design does not add:

- runtime allocation code;
- sequence/counter mutation;
- BIR reporting;
- Digital SI;
- Annex E;
- X/Z;
- statutory discount validation;
- payment finality ownership;
- refund/reversal authority;
- exit/gate behavior;
- schema changes;
- migrations;
- controlled-code values.

## 14. Stop / Go Recommendation

OK to proceed to an idempotency/allocation implementation design slice after this review.

Not OK to implement allocation until idempotency key/source and sequence policy selection are explicitly accepted.

Not OK to proceed to BIR reports from this design alone.
