# ExitPass POS Server Fiscal Issuance Idempotency Slice

## Purpose

This runtime slice implements fiscal issuance idempotency and deterministic semantic request hashing for `POST /v1/fiscal-documents/`.

It does not implement fiscal number allocation.

## Implemented Posture

POS Server derives the idempotency scope from:

- fiscal document creation operation
- `site_pos_server_id`
- `fiscal_document_type_code_id`

POS Server derives the idempotency key from the upstream finality reference in the approved payable-basis input.

This is intentionally conservative for the current request shape. The key is a Central PMS/upstream finality reference, not a payment provider reference alone and not a generated fiscal document ID.

## Semantic Request Hash

The runtime computes a deterministic SHA-256 hash over canonical fiscal request facts, including:

- Site POS Server context
- fiscal document type/status code IDs
- business day
- channel terminal context where present
- payable-basis and upstream finality references
- document links
- fiscal lines
- tenders
- tax details
- discount/privilege details
- totals
- fiscal reference context

The hash excludes generated fiscal document IDs, fiscal sequence values, fiscal document numbers, assignment timestamps, database-generated IDs, retry counters, and transport metadata.

## Runtime Behavior

Within the PostgreSQL fiscal document creation transaction, POS Server now:

1. Inserts or locks the `pos.idempotency_records` row for `(idempotency_scope, idempotency_key)`.
2. Replays an existing linked fiscal document when the semantic hash matches.
3. Fails closed on same key with a different semantic hash.
4. Persists the existing fiscal document shell and child facts for first-time requests.
5. Links the idempotency row to the persisted fiscal document before commit.

Duplicate same-key/same-hash requests return the original fiscal document ID and do not create a second fiscal document.

Same-key/different-hash requests return deterministic conflict behavior and do not create a fiscal document.

## Explicit Exclusions

This slice does not:

- query, lock, or update `pos.fiscal_sequence_states`
- allocate fiscal sequence values
- populate fiscal sequence policy, sequence value, fiscal document number, series, prefix, suffix, assignment timestamp, or assigned-by fields
- mutate fiscal counter state
- implement BIR reporting, Digital SI, Annex E, X/Z, Electronic Journal, POSLog, reprints, adjustments, reset counters, Z-counters, GTA, or recovery automation
- declare platform payment finality
- issue ExitAuthorization
- open gates
- approve entitlement
- activate continuity
- approve manual release

Central PMS remains payment finality, fiscal reference recording, degraded resolve, and ExitAuthorization authority. POS Server remains fiscal issuance authority only.

## Implementation Limitation

`pos.idempotency_records` requires operation type and operation status controlled-code references. This first slice uses the existing fiscal document type code ID and fiscal document status code ID as the operation type/status references to avoid schema or reference-data changes. A later controlled-code slice may introduce dedicated idempotency operation/status values if required.
