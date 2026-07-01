# ExitPass POS Server Fiscal Numbering and Counter Allocation Plan

## 1. Decision Summary

POS Server owns fiscal numbering and counter allocation for fiscal documents it fiscalizes. Central PMS, payment services, WebPay, and payment providers must not allocate POS Server fiscal document numbers.

Numbering must happen inside a controlled PostgreSQL transaction boundary. BIR/accounting-sensitive behavior must remain conservative and fail closed until exact policy, sequence scope, counter meaning, and accreditation expectations are approved.

This document is a plan only. It does not implement fiscal number allocation, counter mutation, schema changes, migrations, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, refund/reversal authority, exit authorization, or gate behavior.

Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.

## 2. Existing Schema Interpretation

### `pos.fiscal_documents`

The current fiscal document header table stores document identity, site/server, terminal, document type/status, upstream PMS/payment references, business day, and `document_context`.

Important gap: the table does not currently expose a dedicated fiscal document number, sequence policy ID, sequence value, fiscal identity ID, series, prefix/suffix, or formatted number column. The table comment explicitly says it does not allocate fiscal numbers. A future implementation must either use approved existing schema posture, such as `document_context` for a temporary reference-only field, or stop for schema/design approval before persisting formal fiscal numbering fields.

### `pos.fiscal_sequence_policies`

This table appears intended to define sequence policy posture by Site POS Server and sequence family. It includes:

- `site_pos_server_id`
- `sequence_family_code_id`
- optional `document_type_code_id`
- `policy_code`
- display/description fields
- optional prefix/suffix/padding posture
- current policy status
- effective date range
- `policy_context`

It does not create PostgreSQL sequences or allocation behavior. It is the likely configuration source for sequence scope and formatted-number posture, but exact document type to policy mapping remains an open decision.

### `pos.fiscal_sequence_states`

This table appears intended to hold the mutable state for a sequence policy. It includes:

- `fiscal_sequence_policy_id`
- `current_sequence_value`
- `last_reserved_sequence_value`
- `last_issued_sequence_value`
- `sequence_state_code_id`
- timestamps and `state_context`

It has a unique row per policy and check constraints that reserved/issued values cannot exceed current value. It is the likely row-level lock target for allocation. It does not implement allocation logic by itself.

### `pos.fiscal_sequence_gap_audit`

This table records permanent gap posture by sequence policy and sequence value. It includes:

- `fiscal_sequence_policy_id`
- optional `fiscal_document_id`
- `gap_sequence_value`
- `gap_reason_code_id`
- optional actor/service/context fields

The unique `(fiscal_sequence_policy_id, gap_sequence_value)` constraint supports one audit record per recognized gap. It does not define final BIR/accounting treatment, recovery, or reuse behavior.

### `pos.fiscal_counter_states`

This table stores counter-family state by Site POS Server. It includes:

- `counter_family_code_id`
- `counter_value`
- optional monetary amount and currency
- status, transition time, and `counter_context`

It appears intended for reset counter, Z-counter, GTA, and related fiscal continuity families. It does not define which counters mutate during document creation or the formulas for monetary counters.

### `pos.fiscal_state_snapshots`

This table stores continuity snapshots, including reset counter, Z-counter, grand total amount, EJ hash reference, and last fiscal event time. It is likely for periodic or lifecycle evidence after state changes, not the primary allocation row. Snapshot timing and mandatory capture points remain open.

### `pos.fiscal_lock_states`

This table stores fiscal lock/block/resume posture. It should be checked before allocation once lock semantics are approved. A future allocator must fail closed if the site/server or sequence scope is locked for fiscal issuance. The table does not implement recovery, automatic unlock, or gate authority.

### `pos.idempotency_records`

This table stores idempotency request identity posture. It includes scope/key, semantic request hash, operation type/status codes, an optional linked fiscal document, replay/conflict references, `completion_unknown`, expiry, and context.

It is the likely guard for retry-safe fiscal document creation. It must not imply payment finality, payment lifecycle ownership, exit authorization, or gate behavior.

## 3. Proposed Allocation Point

Recommended sequence:

1. Receive `POST /v1/fiscal-documents`.
2. Run runtime validation, including authority-boundary checks.
3. Resolve or validate idempotency scope/key before side effects.
4. Open one PostgreSQL transaction.
5. Lock the idempotency row or create it in an in-progress state.
6. Resolve the applicable fiscal sequence policy.
7. Acquire a row-level lock on `pos.fiscal_sequence_states` for the selected policy.
8. Check fiscal lock state and sequence state.
9. Allocate the next sequence value only when the request is ready to persist.
10. Insert fiscal document header and child rows.
11. Mutate sequence/counter state in the same transaction.
12. Link the idempotency record to the committed fiscal document.
13. Commit.
14. Return the allocated fiscal document identifier/number only after durable commit.

Allocation should not happen during DTO mapping or pre-validation. Allocation should not happen after the fiscal document shell commits.

## 4. Transaction Boundary

Numbering, document persistence, idempotency state, and relevant counter mutation should share one PostgreSQL transaction.

Rules:

- no committed document without corresponding sequence/counter state mutation, once numbering is implemented;
- no committed counter advance without a committed document unless explicitly recorded as a gap;
- if allocation happens but the transaction rolls back before commit, the number is not durable and should not be treated as issued;
- failure before durable commit should not create a gap unless the implementation has already made a durable reservation or issued-number record;
- failure after durable commit may require gap/audit handling depending on status and external output posture;
- no silent renumbering;
- no reuse of committed or issued fiscal numbers.

If the current schema cannot persist a formal fiscal number on `pos.fiscal_documents`, implementation must stop for schema/design approval rather than hiding fiscal number authority in unrelated fields.

## 5. Idempotency Interaction

Idempotency must protect retry behavior:

- the same upstream idempotency key and same semantic request hash should return the same fiscal document result if already committed;
- retry after transient failure must not allocate a second fiscal number for the same upstream request;
- conflicting semantic request hash for the same scope/key should fail deterministically;
- `completion_unknown` must be used conservatively when the service cannot determine whether a prior transaction committed;
- idempotency records should link to `pos.fiscal_documents` after durable commit;
- idempotency does not authorize payment finality, exit/gate behavior, refund/reversal behavior, or statutory discount approval.

Open implementation question: current API command does not yet model an explicit idempotency key. A future slice must add a boundary-safe idempotency input or define how it is derived.

## 6. Concurrency and Locking

Desired behavior:

- one allocator per fiscal identity/series/site POS Server sequence scope;
- lock the selected `pos.fiscal_sequence_states` row with a PostgreSQL row-level lock or equivalent transaction-safe mechanism;
- do not rely on application-only in-memory counters;
- do not rely on optimistic duplicate-number detection after the fact as the primary safety mechanism;
- fail closed if the sequence/counter lock cannot be acquired safely;
- set clear timeout behavior for lock waits;
- surface deterministic allocation failure without leaking database details.

The likely locking target is the unique `pos.fiscal_sequence_states` row for the selected `fiscal_sequence_policy_id`. If counter state must also mutate, the corresponding `pos.fiscal_counter_states` row should be locked inside the same transaction.

## 7. Gap Handling

`pos.fiscal_sequence_gap_audit` should be written only when a sequence value is durably recognized as skipped, unusable, or failed after durable reservation/issuance according to approved policy.

Distinctions:

- rollback before commit: normally not a gap because no sequence value became durable;
- committed document with failed downstream output: may not be a sequence gap if the document remains valid with failed output status;
- committed durable reservation without document issuance: may be a gap if policy says reserved numbers cannot be reused;
- manual repair or reconciliation: should write explicit gap audit or recovery evidence, never silently renumber.

No implementation should reuse committed/issued numbers. Whether reserved-but-unissued numbers can ever be reused remains a BIR/accreditation question and should default to no reuse until approved otherwise.

## 8. Counter State and Snapshot Posture

Expected posture:

- `pos.fiscal_counter_states` should hold current counter-family values such as reset counter, Z-counter, GTA, or related families once approved.
- `pos.fiscal_state_snapshots` should capture continuity evidence after approved lifecycle events, such as business day boundary, X/Z process, recovery checkpoint, or periodic health point.
- `pos.fiscal_lock_states` should block allocation when fiscal state is locked, blocked, or unsafe according to approved controlled-code values.

Open questions:

- which counters must update during ordinary fiscal document creation;
- whether GTA or monetary counters can be computed in the database, application, or only after BIR/accounting approval;
- which status values in lock/counter/snapshot families must exist before implementation;
- when snapshots are mandatory versus optional evidence.

## 9. API / Runtime Impact

Expected future changes:

- fiscal document creation should allocate a fiscal number during creation once sequence policy and schema mapping are approved;
- API input may need a safe idempotency key and sequence-scope hints, if not fully derivable;
- API response should include the allocated fiscal document number/reference only after durable commit;
- `GET /v1/fiscal-documents/{fiscalDocumentId}` should show fiscal number/counter fields when they exist in approved schema/model;
- persistence-not-configured and invalid-persistence-configuration behavior remains fail closed;
- allocation failure should return deterministic fiscal allocation errors, not HTTP 500.

Current gap: the runtime command/draft/response do not include fiscal number, sequence policy, sequence value, or idempotency key fields.

## 10. Non-Goals

This plan does not add:

- BIR reports;
- Digital SI;
- Annex E;
- X/Z;
- statutory discount validation;
- payment finality ownership;
- refund/reversal authority;
- exit/gate behavior;
- schema changes;
- migrations;
- controlled-code changes;
- source code changes;
- number allocation behavior;
- counter mutation behavior.

## 11. Implementation Options

### Option A: Allocate During Fiscal Document Creation Transaction

Allocate the fiscal number after validation and idempotency check, inside the same PostgreSQL transaction that writes the fiscal document shell and mutates sequence/counter state.

Pros:

- best alignment with atomicity;
- avoids committed document without sequence state;
- avoids committed counter advance without document, except explicit gap paths;
- works naturally with row-level locking;
- best retry posture when paired with idempotency.

Cons:

- requires exact sequence scope and schema mapping before implementation;
- may increase transaction duration;
- requires careful lock timeout handling.

Recommendation: preferred.

### Option B: Preallocate / Reserve Before Document Persistence

Reserve a number before document persistence, then later attach it to the fiscal document.

Pros:

- can expose a reservation concept;
- may support workflows where external devices need a number before full persistence.

Cons:

- creates reserved-but-unissued edge cases;
- likely increases gap handling complexity;
- requires firm BIR/accreditation rules for reservation reuse or non-reuse;
- easier to produce durable counter movement without a committed document.

Recommendation: defer unless accreditation requirements demand explicit reservations.

### Option C: Post-Persistence Numbering

Persist document rows first, then allocate a number afterward.

Pros:

- keeps initial document persistence simple.

Cons:

- creates committed fiscal document shells without fiscal numbers;
- complicates read and retry behavior;
- can leave documents in numbering-pending limbo;
- weakens atomicity between fiscal document creation and sequence state.

Recommendation: do not use as the primary fiscal issuance path.

## 12. Open Questions

These must be resolved or explicitly accepted before implementation:

- exact sequence scope: site, fiscal identity, terminal, document type, business day, series, or a combination;
- fiscal identity/series selection source;
- document type to sequence policy mapping;
- whether `pos.fiscal_documents` needs formal fiscal number columns before implementation;
- controlled-code families and values needed for sequence family/status, policy status, counter family/status, gap reasons, lock states, and idempotency operation/status;
- what counts as issued, reserved, void, failed, or gap;
- BIR/accreditation expectations for reserved numbers and failed transactions;
- manual recovery and reconciliation flow;
- service identity and actor tracking requirements;
- operational reports needed for gaps, counters, and allocation failures;
- whether fiscal number formatting is stored as formatted text, numeric value plus prefix/suffix, or both;
- whether idempotency keys are required from upstream callers or derived from payable-basis/finality references.
