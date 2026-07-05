# ExitPass POS Server Fiscal Numbering / Idempotency Runtime Note v1.0

## Purpose

This note records the POS Server runtime foundation required before Central PMS can safely execute Fiscal Exception Queue retries against `POST /v1/fiscal-documents`.

## Current Readiness Inventory

`POST /v1/fiscal-documents` now uses a stable idempotency identity resolved inside POS Server runtime:

- Idempotency scope: `fiscal_document_creation:{site_pos_server_id:N}:{fiscal_document_type_code_id:N}`.
- Idempotency key source: `payableBasis.upstreamFinalityRef`.
- Idempotency key: the trimmed upstream finality reference.
- Semantic request hash: POS Server-calculated SHA-256 hash over normalized fiscal document semantics.
- Semantic request hash version: `sha256:v1`.

Persisted/read fiscal numbering fields currently include:

- `fiscal_identity_id`
- `fiscal_sequence_policy_id`
- `fiscal_sequence_value`
- `fiscal_document_number`
- `fiscal_series`
- `fiscal_number_prefix_text`
- `fiscal_number_suffix_text`
- `fiscal_number_assigned_at`
- `fiscal_number_assigned_by_ref`

Fiscal identity, sequence policy, and sequence state objects exist in runtime schema and are used by the PostgreSQL repository. Fiscal identity is resolved from `pos.site_pos_server_fiscal_identity_history` and `pos.fiscal_identities`. Sequence policy is resolved from `pos.fiscal_sequence_policies`. Sequence state is locked and advanced in `pos.fiscal_sequence_states`.

## Implemented Runtime Behavior

Same idempotency scope/key with the same semantic request hash resolves to the original fiscal document outcome. Same scope/key with a different semantic request hash fails closed as `fiscal_document_idempotency_conflict`.

Fiscal number allocation is implemented when all prerequisites exist. Allocation occurs in the same database transaction as idempotency completion and fiscal document persistence. The repository locks the idempotency row first, resolves fiscal context, locks the sequence state row with `FOR UPDATE`, inserts the fiscal document shell and child rows, advances sequence state, and links the idempotency record to the persisted fiscal document before commit.

Replay does not insert another fiscal document and does not allocate another fiscal number.

## Readback Fields Available

`GET /v1/fiscal-documents/{id}` returns the fiscal document id, fiscal identity id, sequence policy id, sequence value, formatted fiscal document number, series, prefix, suffix, assigned timestamp, assigned-by reference, document status, safe created/updated timestamps, and safe idempotency/hash fields:

- `idempotencyScope`
- `idempotencyKey`
- `idempotencyKeySource`
- `semanticRequestHash`
- `semanticRequestHashVersion`
- `semanticRequestHashStatus`

## Tests Added Or Updated

Coverage now includes:

- deterministic semantic hash calculation;
- same idempotency key/hash replay;
- same idempotency key with different semantic hash conflict;
- replay preserves original fiscal document facts;
- replay does not allocate a second fiscal number;
- readback exposes idempotency, semantic hash, and fiscal numbering fields;
- missing fiscal identity blocks allocation;
- missing sequence policy blocks allocation;
- missing sequence state blocks allocation;
- concurrent allocation receives distinct sequence values under row-level locking;
- boundary checks confirming no payment finality, refund/reversal, statutory discount validation authority, or gate/exit behavior was introduced.

## Remaining Blockers Before Central PMS FEQ Retry Execution

Central PMS FEQ retry execution should remain disabled until the integration contract is reviewed against this runtime behavior and a controlled UAT run verifies:

- Central PMS semantic request hash compatibility with POS Server `sha256:v1` semantics;
- Central PMS retry command idempotency source/key mapping to `upstreamFinalityRef`;
- readback classification consumes the new idempotency/hash/readback fields correctly;
- operational seed/configuration readiness for each target Site POS Server fiscal identity, sequence policy, and sequence state;
- approval that retry execution will only replay or create through the controlled POS Server idempotency contract.

## Intentionally Not Implemented

This slice did not implement BIR X/Z reports, Annex E reports, Digital Sales Invoice rendering, statutory discount validation inside POS Server, payment finality ownership, refund/reversal authority, gate/exit behavior, manual fiscal number creation, fiscal number editing, uncontrolled retry behavior, Operator Console, Management Dashboard, or Central PMS changes.
