# ExitPass POS Server Runtime Fiscal Numbering and Idempotency Implementation Plan

## 1. Purpose

This document plans the safest implementation path for runtime fiscal number allocation and idempotent Sales Invoice issuance in ExitPass-PoSServer.

The plan preserves the approved ExitPass v1.3 authority model:

- POS Server is the Site fiscal issuance authority for fiscal documents it issues.
- Central PMS owns payment finality, payable-basis authority, fiscal reference recording, degraded resolve, and ExitAuthorization.
- POS Server does not declare platform payment finality, issue ExitAuthorization, open gates, approve entitlement, mutate Central PMS payable basis directly, activate continuity, or approve manual release.
- POS Server returns fiscal document identity and status to Central PMS after durable fiscal issuance.

This is an implementation plan only. It does not implement allocation, mutate sequence/counter state, change API contracts, change schema, or add runtime behavior.

## 2. Current Implementation Reality

Repository inspection shows the runtime fiscal document shell is implemented, but fiscal numbering and idempotent issuance are not yet wired.

Current verified posture:

- The API exposes `POST /v1/fiscal-documents/` and `GET /v1/fiscal-documents/{fiscalDocumentId:guid}`.
- `POST` validates local fiscal schema context, upstream payable-basis reference, upstream finality/payment reference, fiscal lines, tenders, tax details, discount/privilege details, and totals.
- `POST` persists a fiscal document shell and child facts in one PostgreSQL transaction.
- `GET` reads persisted fiscal document header and child rows, including nullable fiscal numbering fields.
- Runtime fiscal number allocation is not implemented.
- Runtime idempotency key handling and semantic request hash behavior are not implemented.
- `pos.idempotency_records` exists in schema posture, but runtime code does not yet create, lock, replay, conflict-check, or complete idempotency records.
- X/Z, BIR Sales Summary, Annex E, Electronic Journal, POSLog, reprint, adjustment, Digital SI URL, reset counter, Z-counter, Grand Total Amount, and recovery automation remain SQL/documentation posture rather than runtime behavior.
- No inspected runtime code moves ExitAuthorization, gate opening, payment finality, statutory entitlement approval, continuity activation, or manual release approval into POS Server.

## 3. Existing Relevant Files Inspected

Reference documentation inspected from `D:\SourceCodes\ExitPass`:

- `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md`
- `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0_Alignment_Review.md`
- `docs/v1.3/pos-server/ExitPass_POS_Server_Repo_Inspection_Report.md`
- `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md`
- `docs/v1.3/ExitPass_System_Design_v1.3.md`
- `docs/v1.3/ExitPass_BRD_v1.3.md`
- `docs/v1.3/ExitPass_v1.3_BRD_Approval_Baseline.md`

POS Server repository areas inspected:

- `src/ExitPass.PosServer.Api/FiscalDocuments/`
- `src/ExitPass.PosServer.Runtime/FiscalDocuments/`
- `src/ExitPass.PosServer.Persistence.Postgres/FiscalDocuments/`
- `tests/`
- `db/state/tables/pos.fiscal_documents.sql`
- `db/state/tables/pos.fiscal_sequence_policies.sql`
- `db/state/tables/pos.fiscal_sequence_states.sql`
- `db/state/tables/pos.fiscal_sequence_gap_audit.sql`
- `db/state/tables/pos.fiscal_counter_states.sql`
- `db/state/tables/pos.fiscal_lock_states.sql`
- `db/state/tables/pos.idempotency_records.sql`
- `db/rebuild/`
- `db/validation/`
- `db/scripts/`
- `docs/v1.3/pos-server-runtime/ExitPass_POS_Server_Fiscal_Numbering_Counter_Allocation_Plan.md`
- `docs/v1.3/pos-server-runtime/ExitPass_POS_Server_Fiscal_Numbering_Schema_Gap_Resolution.md`
- `docs/v1.3/pos-server-runtime/ExitPass_POS_Server_Fiscal_Numbering_Schema_Design.md`
- `docs/v1.3/pos-server-runtime/ExitPass_POS_Server_Fiscal_Numbering_Read_Model.md`

## 4. Current Fiscal Document Creation Flow

Current create flow:

1. API route `POST /v1/fiscal-documents/` receives `CreateFiscalDocumentRequest`.
2. API mapping creates `FiscalDocumentCreationCommand`.
3. `FiscalDocumentCreationService` validates:
   - local fiscal schema context
   - payable-basis reference
   - upstream finality/payment reference
   - absence of raw evidence, credentials, secrets, provider callback payloads, and raw payment payloads
   - approved posture for statutory discount references when statutory discount fiscal treatment is requested
   - line, tender, tax, discount/privilege, and total validation
4. The service creates a `FiscalDocumentDraft` with a new runtime-generated `fiscal_document_id`.
5. `PostgresFiscalDocumentRepository.CreateAsync` opens one PostgreSQL transaction.
6. The repository writes:
   - `pos.fiscal_documents`
   - `pos.fiscal_document_status_history`
   - `pos.fiscal_document_links`
   - `pos.fiscal_document_lines`
   - `pos.fiscal_tenders`
   - `pos.fiscal_tax_details`
   - `pos.fiscal_discount_privilege_details`
   - `pos.fiscal_totals`
7. The repository commits only after all requested rows succeed, and rolls back on failure.
8. The API returns the mapped creation result.

Current flow does not create or lock idempotency records, resolve sequence policy, lock sequence state, allocate fiscal sequence value, format a fiscal document number, or update sequence state.

## 5. Current Fiscal Numbering / Read Model Behavior

`pos.fiscal_documents` now has nullable fiscal numbering columns:

- `fiscal_identity_id`
- `fiscal_sequence_policy_id`
- `fiscal_sequence_value`
- `fiscal_document_number`
- `fiscal_series`
- `fiscal_number_prefix_text`
- `fiscal_number_suffix_text`
- `fiscal_number_assigned_at`
- `fiscal_number_assigned_by_ref`

The current read model and `GET /v1/fiscal-documents/{fiscalDocumentId}` can expose these fields safely as nullable or populated values. This is read-only support. `POST` does not populate them.

The schema contains partial unique indexes to protect assigned fiscal sequence values and assigned fiscal document numbers per sequence policy. These indexes are defense-in-depth and must not be treated as the primary allocator.

`document_context` remains non-authoritative for fiscal numbering.

## 6. Current Idempotency Database Posture

`pos.idempotency_records` exists with:

- `idempotency_scope`
- `idempotency_key`
- `semantic_request_hash`
- `operation_type_code_id`
- `operation_status_code_id`
- `linked_fiscal_document_id`
- `replay_result_ref`
- `conflict_ref`
- `completion_unknown`
- `expires_at`
- `idempotency_context`

The table has a uniqueness constraint on `(idempotency_scope, idempotency_key)` and comments limiting it to fiscal side-effect protection.

Current runtime gaps:

- No public or internal idempotency key is accepted by the fiscal document creation command.
- No semantic request hash is computed.
- No idempotency record is inserted, locked, or updated during create.
- No replay result is returned for duplicate requests.
- No deterministic conflict is returned for same key with different semantic payload.
- No completion-unknown recovery path is implemented.

## 7. Proposed Runtime Fiscal Number Allocation Model

Fiscal number allocation should happen during `POST /v1/fiscal-documents/`, inside the same durable PostgreSQL transaction as fiscal document creation.

Target model:

- Validate request before opening the allocation transaction.
- Require or derive a trusted idempotency key and scope before allocating a fiscal number.
- Resolve fiscal identity and sequence policy from server-side fiscal context, not from arbitrary client selection.
- Lock the selected `pos.fiscal_sequence_states` row with row-level database locking.
- Compute the next fiscal sequence value from the locked row.
- Format the fiscal document number from the approved sequence policy fields.
- Insert `pos.fiscal_documents` with fiscal numbering fields populated.
- Insert the child fiscal fact rows.
- Update `pos.fiscal_sequence_states` in the same transaction.
- Update the idempotency record with the committed fiscal document reference.
- Commit once.
- Return fiscal document identity and fiscal number only after durable commit.

The allocation model must fail closed if policy, identity, lock, state, or idempotency posture is ambiguous.

## 8. Proposed Idempotency Source / Key Model

The safest implementation should introduce an explicit idempotency contract for fiscal document creation.

Recommended source:

- A Central PMS-issued fiscal issuance idempotency key, supplied by the trusted upstream fiscal issuance request.
- Scope should include the operation and fiscal authority boundary, such as Site POS Server plus operation type.
- The key must identify the fiscal issuance request, not payment provider retry noise or browser retry noise.

Recommended scope shape:

- operation: fiscal document creation / Sales Invoice issuance
- Site POS Server identity
- upstream payable-basis reference
- upstream payment/finality reference or Central PMS fiscal issuance request reference

Rules:

- Do not use a payment provider transaction reference alone as the idempotency key.
- Do not allow an untrusted public client to select arbitrary fiscal idempotency scope.
- Do not let idempotency imply payment finality, ExitAuthorization, gate authority, refund/reversal authority, or manual release authority.
- If no explicit Central PMS fiscal issuance key exists yet, implementation must either add one or define a server-derived key from stable Central PMS references. This must be approved before allocation code is written.

## 9. Proposed Semantic Request Hash Model

The semantic request hash should detect same idempotency key with materially different fiscal issuance payload.

Recommended behavior:

- Compute a deterministic hash over canonical fiscal issuance facts.
- Use canonical ordering for arrays and object properties where order is not business-significant.
- Normalize values before hashing: trim safe string references, normalize currency code casing, normalize date/time representation, and use stable numeric formats.
- Exclude generated values: `fiscal_document_id`, fiscal sequence value, fiscal document number, timestamps, database-generated IDs, retry counters, and transport metadata.
- Include authority-relevant fiscal input facts:
  - Site POS Server context
  - channel terminal context where provided
  - fiscal document type/status code IDs
  - business day
  - payable-basis reference
  - upstream finality/payment reference
  - upstream traceability references
  - fiscal lines
  - fiscal tenders
  - tax details
  - discount/privilege details
  - totals
  - approved statutory discount references when they affect fiscal treatment

The hash algorithm should be selected during implementation, with SHA-256 over canonical UTF-8 JSON as the preferred default unless repo standards require otherwise.

## 10. Duplicate Request Behavior

Same idempotency scope and key with the same semantic request hash should be treated as a duplicate/retry.

Expected behavior:

- If the original request committed successfully, return the original fiscal document identity, fiscal number, and status.
- Do not allocate another fiscal number.
- Do not insert a second fiscal document.
- Do not mutate Central PMS payable basis.
- Do not declare payment finality or ExitAuthorization.
- Return deterministic duplicate/replay posture.

If the original request is still in progress, the service should return a deterministic in-progress or retry-later response rather than racing the allocator.

## 11. Conflicting Request Behavior

Same idempotency scope and key with a different semantic request hash must fail closed.

Expected behavior:

- Return a deterministic idempotency conflict response.
- Do not allocate a fiscal number.
- Do not insert a fiscal document.
- Do not mutate sequence state.
- Store or reference conflict context only if it does not expose sensitive payloads.

Suggested error code:

- `fiscal_number_idempotency_conflict`

## 12. Timeout / Unknown Outcome Behavior

The most dangerous retry case is a timeout after the database transaction may have committed but before the caller received the response.

Required posture:

- Idempotency lookup must be the source of recovery.
- If the transaction committed and idempotency was updated with the fiscal document reference, retry returns the committed result.
- If the transaction did not commit, retry may allocate only after confirming no committed idempotency/document outcome exists.
- If the record is marked `completion_unknown`, the service must recover conservatively by checking linked fiscal document and persisted numbering state before deciding whether replay is safe.
- Completion-unknown handling must not allocate a second fiscal number for the same semantic issuance request.

## 13. Failed Issuance / Rollback Behavior

If validation fails before the transaction, no idempotency completion, fiscal document, or fiscal number should be created.

If persistence or allocation fails inside the transaction:

- The transaction rolls back.
- No fiscal document header remains.
- No child rows remain.
- No sequence state advance remains.
- No durable fiscal number exists.
- Retry with the same semantic request may proceed through idempotency recovery rules.

Rollback before commit does not create a durable sequence gap.

## 14. Sequence Gap and Abandoned Issuance Posture

Sequence gap behavior must remain explicit and conservative.

Recommended posture:

- A rolled-back transaction before durable commit does not create a fiscal gap.
- A committed fiscal number must never be silently renumbered or reused.
- A gap should be recorded only for durably recognized reservation, issuance, void, abandonment, or reconciliation cases approved by BIR/accounting policy.
- `pos.fiscal_sequence_gap_audit` should be used only for recognized durable gaps, not for failed pre-commit transactions.
- No silent renumbering.
- No reuse of committed or issued fiscal numbers.

Open BIR/accounting questions remain around reserved numbers, abandoned issuance, failed issuance after response, manual recovery, and reports required for gap reconciliation.

## 15. Transaction and Locking Design

Preferred future transaction order:

1. Validate request and authority boundaries.
2. Compute semantic request hash.
3. Open PostgreSQL transaction.
4. Create or lock the idempotency record for `(idempotency_scope, idempotency_key)`.
5. If duplicate committed request with same hash exists, return replay result without allocation.
6. If same key has a different hash, fail closed as conflict.
7. Resolve fiscal identity and sequence policy.
8. Check fiscal lock state where applicable.
9. Lock the selected `pos.fiscal_sequence_states` row using `SELECT ... FOR UPDATE` or equivalent.
10. Compute next fiscal sequence value.
11. Format fiscal document number using policy series/prefix/suffix/padding rules.
12. Insert `pos.fiscal_documents` with fiscal numbering fields.
13. Insert status history, links, lines, tenders, tax details, discount/privilege details, and totals.
14. Update `pos.fiscal_sequence_states`.
15. Update `pos.idempotency_records` with linked fiscal document and completed status.
16. Commit.
17. Return fiscal document ID, fiscal number, and fiscal status.

Locking requirements:

- Lock the database sequence state row, not an in-memory application counter.
- Use lock timeout behavior and deterministic failure codes.
- Fail closed if the sequence state cannot be safely locked or selected.
- Treat partial unique indexes as defense-in-depth, not allocator control.

Suggested error codes:

- `fiscal_sequence_policy_not_found`
- `fiscal_sequence_policy_ambiguous`
- `fiscal_identity_not_resolved`
- `fiscal_sequence_state_not_available`
- `fiscal_sequence_lock_failed`
- `fiscal_number_allocation_failed`
- `fiscal_number_idempotency_conflict`

## 16. API Response Semantics Impact

Future `POST /v1/fiscal-documents/` should return fiscal number data only after durable commit.

Expected response posture:

- Success includes fiscal document ID, fiscal status, and assigned fiscal document number.
- Replay returns the original fiscal document ID, fiscal status, and assigned fiscal document number where safe.
- Conflict returns deterministic idempotency conflict.
- Allocation failures return deterministic fiscal allocation errors, not generic HTTP 500 for expected failure modes.
- `GET /v1/fiscal-documents/{fiscalDocumentId}` continues to show persisted fiscal numbering fields.

Response semantics must remain fiscal-only. They do not authorize payment finality, exit, gate opening, refund/reversal, continuity, or manual release.

## 17. Repository / Service Changes Needed

Future implementation will likely need:

- Command/request input for a trusted fiscal issuance idempotency key and scope or a clearly defined server-derived equivalent.
- Semantic request hash service.
- Idempotency repository or methods inside the fiscal document persistence transaction.
- Fiscal sequence policy resolver.
- Fiscal identity resolver.
- Fiscal lock state checker.
- Sequence state allocator using row-level lock.
- Fiscal document repository changes to insert numbering fields into `pos.fiscal_documents`.
- Sequence state update SQL inside the same transaction.
- Idempotency record completion/replay/conflict SQL inside the same transaction.
- Creation result/response extensions for fiscal number and replay/conflict posture.
- Tests proving no duplicate fiscal numbers and no unsafe replay.

`PostgresFiscalDocumentReader` already has read-model support for nullable/populated fiscal numbering fields, so it should require little or no change for populated allocation fields.

## 18. Database Changes Needed, if any

No additional schema change is approved by this planning document.

Existing schema appears to provide the core persistence targets for first allocation implementation:

- fiscal numbering columns on `pos.fiscal_documents`
- sequence policy rows in `pos.fiscal_sequence_policies`
- sequence state rows in `pos.fiscal_sequence_states`
- gap audit posture in `pos.fiscal_sequence_gap_audit`
- idempotency rows in `pos.idempotency_records`

Potential non-schema prerequisites:

- Approved controlled-code values for idempotency operation/status posture.
- Approved controlled-code values for fiscal sequence families/states/gap reasons/lock posture if the allocator needs them.
- Seeded sequence policy and sequence state data for disposable tests.

If implementation discovers missing indexes, constraints, or columns, that must be handled in a separate schema design/SQL slice. This plan does not authorize schema edits.

## 19. Test Plan

Required tests before allocation is accepted:

- Unit tests for semantic request hash normalization and determinism.
- Unit tests for duplicate same-key/same-hash behavior.
- Unit tests for conflict same-key/different-hash behavior.
- Unit tests proving idempotency does not imply payment finality, ExitAuthorization, gate behavior, refund/reversal authority, or manual release.
- Repository tests for idempotency record create/lock/update SQL.
- Repository tests for sequence policy/state selection SQL.
- Repository tests proving `SELECT ... FOR UPDATE` or equivalent row-level lock posture.
- Transaction ordering tests proving idempotency, sequence allocation, fiscal document insert, child inserts, sequence update, and idempotency completion commit atomically.
- Rollback tests proving no partial fiscal document shell and no durable sequence advance on late failure.
- Duplicate/retry tests proving a second identical request returns the original fiscal document and fiscal number.
- Conflict tests proving same key with changed fiscal payload fails closed.
- Disposable PostgreSQL concurrency smoke test proving concurrent requests cannot allocate the same fiscal number.
- Disposable PostgreSQL retry/unknown outcome smoke test if feasible.
- Read endpoint test proving allocated fiscal numbering fields are visible after successful create.
- Regression tests proving X/Z, BIR reporting, Annex E, Digital SI, EJ, POSLog, reprint, adjustment, recovery automation, ExitAuthorization, and gate behavior remain out of scope.

## 20. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Duplicate fiscal numbers under concurrency | Lock `pos.fiscal_sequence_states` row in the same transaction and keep partial unique indexes as defense-in-depth. |
| Duplicate fiscal documents on retry | Require idempotency scope/key and semantic request hash before allocation. |
| Same key reused with different payload | Compare semantic hash and fail closed as conflict. |
| Timeout after commit creates uncertainty | Recover through idempotency lookup and linked fiscal document reference. |
| Sequence gap policy incorrectly implemented | Keep durable gap behavior explicit and defer BIR/accounting-sensitive gap decisions until approved. |
| POS Server accidentally assumes payment/exit authority | Keep fiscal response semantics separate from Central PMS payment finality and ExitAuthorization. |
| Client selects arbitrary sequence policy | Resolve policy server-side from approved fiscal context; fail closed on ambiguity. |
| `document_context` becomes shadow numbering authority | Persist authoritative fiscal number only in dedicated fiscal numbering columns. |

## 21. Open Questions

Open questions before implementation:

- What exact idempotency key will Central PMS provide for fiscal issuance?
- What is the approved idempotency scope string format?
- Which controlled-code values represent fiscal issuance idempotency operation/status?
- What is the exact sequence policy selection rule for Sales Invoice issuance?
- Is sequence policy document-type-specific in the first implementation?
- How is fiscal identity resolved for a Site POS Server at runtime?
- Which sequence policy/status rows are active/effective and eligible?
- What is the approved fiscal document number format, padding, prefix, suffix, and series behavior?
- What lock timeout should be used?
- What exact completion-unknown recovery workflow is required?
- Which failures create a durable fiscal gap?
- Which failures are rollback-only and do not create gaps?
- What service identity should populate `fiscal_number_assigned_by_ref`?
- Which BIR/accreditation reports must consume gap and sequence state later?
- What seed/reference data is required before disposable allocation tests can run?

## 22. Recommended Implementation Slices

Recommended safe sequence:

1. **Idempotency contract and semantic hash**
   - Add trusted idempotency input or approved server-derived key.
   - Compute semantic request hash.
   - Add duplicate and conflict behavior.
   - Add tests without fiscal number allocation.

2. **Fiscal policy and identity resolution**
   - Implement fiscal sequence policy lookup.
   - Implement fiscal identity resolution.
   - Fail closed on missing or ambiguous policy/identity.
   - Add resolver tests and disposable seed posture for tests.

3. **Sequence-state allocation inside document transaction**
   - Lock `pos.fiscal_sequence_states`.
   - Allocate sequence value.
   - Format fiscal document number.
   - Insert fiscal document header with numbering fields.
   - Update sequence state and idempotency completion in the same transaction.
   - Add rollback and duplicate tests.

4. **API response and smoke validation**
   - Return fiscal number only after durable commit.
   - Extend disposable PostgreSQL API smoke tests for retry, conflict, concurrency, and read-back.
   - Document remaining BIR/accounting sequence-gap open items.

5. **Later BIR/accounting-sensitive work**
   - Counter/GTA/Z/reset behavior.
   - Gap reconciliation reporting.
   - X/Z, BIR Sales Summary, Annex E, Electronic Journal, POSLog, Digital SI, reprint, adjustment, and recovery automation.

## 23. Suggested Next Codex Z Implementation Task

Suggested next task:

Create the first implementation slice for fiscal issuance idempotency and semantic request hashing, without fiscal number allocation.

That slice should add or define the trusted idempotency source, compute a deterministic semantic request hash, detect duplicate and conflict requests, add unit tests, and keep sequence-state locking/allocation for the following slice.

Stop/go assessment:

- OK to proceed to an idempotency and semantic request hash implementation slice after this plan is reviewed.
- Not OK to implement fiscal number allocation until idempotency source/key and sequence policy selection are explicitly accepted.
- Not OK to proceed to BIR reports from this plan alone.
- Not OK to add statutory discount validation, payment finality ownership, ExitAuthorization, or gate behavior to POS Server.
