# ExitPass POS Server Fiscal Policy and Identity Resolution Slice

## Purpose

This runtime slice adds server-side fiscal identity and fiscal sequence policy resolution for `POST /v1/fiscal-documents/`.

It does not allocate fiscal numbers.

## Implemented Posture

Fiscal identity resolution uses the effective-dated `pos.site_pos_server_fiscal_identity_history` relationship for the request Site POS Server.

Eligible identities must:

- belong to the Site POS Server through active effective history
- reference an active fiscal identity
- have an active/effective status code when a status code is present

Fiscal sequence policy resolution uses a document-type-specific `pos.fiscal_sequence_policies` row for the Site POS Server and fiscal document type.

Eligible policies must:

- belong to the Site POS Server
- match the fiscal document type
- be active/effective by date range
- have an active/effective policy status code

Resolution fails closed if no eligible row exists, multiple eligible rows exist, or only inactive/not-effective/expired rows exist.

## Persistence Posture

`fiscal_identity_id` is persisted on `pos.fiscal_documents` because the current schema supports it without requiring final fiscal number assignment.

`fiscal_sequence_policy_id` is validated but not persisted in this slice. The current `pos.fiscal_documents` number-assignment check requires sequence policy, sequence value, fiscal document number, and assignment timestamp to be present together. Persisting policy alone would incorrectly imply partial number assignment.

The resolved sequence policy is retained as validation context only until the allocation slice.

## Explicit Exclusions

This slice does not:

- query, lock, or update `pos.fiscal_sequence_states`
- allocate fiscal sequence values
- populate `fiscal_sequence_policy_id`
- populate `fiscal_sequence_value`
- populate `fiscal_document_number`
- populate fiscal series, prefix, suffix, assigned-at, or assigned-by fields as final number assignment
- mutate fiscal counter state
- implement BIR reporting, Digital SI, Annex E, X/Z, Electronic Journal, POSLog, reprints, adjustments, reset counters, Z-counters, GTA, or recovery automation
- declare platform payment finality
- issue ExitAuthorization
- open gates
- approve entitlement
- activate continuity
- approve manual release

Central PMS remains payment finality, fiscal reference recording, degraded resolve, and ExitAuthorization authority. POS Server remains fiscal issuance authority only.
