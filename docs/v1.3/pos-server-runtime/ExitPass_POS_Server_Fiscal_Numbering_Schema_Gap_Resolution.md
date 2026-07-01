# ExitPass POS Server Fiscal Numbering Schema Gap Resolution

## 1. Problem Statement

`pos.fiscal_documents` currently cannot durably store a formal fiscal number. It has no dedicated fiscal identity reference, sequence policy reference, numeric sequence value, formatted fiscal document number, fiscal series, prefix/suffix copy, or fiscal number assignment timestamp.

Runtime fiscal number allocation cannot be implemented safely until a formal persistence target exists. Fiscal number authority must not be hidden in `document_context` as the long-term authoritative field. `document_context` may remain useful for non-authoritative migration notes or temporary diagnostics only if explicitly approved and documented.

This document resolves the gap at design level only. It does not modify `db/state`, create migrations, implement allocation, mutate counters, or add BIR reporting behavior.

## 2. Existing Schema Assessment

### `pos.fiscal_documents`

Current support:

- document identity through `fiscal_document_id`;
- Site POS Server and optional channel terminal references;
- fiscal document type/status controlled-code references;
- Central PMS and payment/finality references;
- business day;
- flexible `document_context`.

Current gaps:

- no `fiscal_identity_id`;
- no `fiscal_sequence_policy_id`;
- no numeric `fiscal_sequence_value`;
- no formatted fiscal document number;
- no fiscal series field;
- no assigned timestamp;
- no allocation status/posture field;
- no uniqueness rule preventing duplicate numbers in the same fiscal scope.

The existing table comment explicitly says the table does not allocate fiscal numbers.

### `pos.fiscal_identities`

Current support:

- registration posture such as MIN, PTU, serial, supplier, accreditation references;
- status controlled-code reference;
- metadata posture.

Current gaps:

- not directly linked from `pos.fiscal_documents`;
- does not define document numbering scope;
- does not define series or sequence policy mapping.

`pos.channel_terminals` can reference `pos.fiscal_identities`, but terminals are not independent fiscal authorities. If fiscal identity is required per document, the fiscal document row needs its own durable identity reference or an approved derivation rule.

### `pos.fiscal_sequence_policies`

Current support:

- policy per Site POS Server;
- sequence family controlled-code reference;
- optional document type controlled-code reference;
- policy code, prefix, suffix, padding, status, effective dates, and context.

Current gaps:

- no formal link from fiscal document rows;
- no fiscal identity column in the policy table;
- exact sequence scope is not finalized;
- no runtime allocation behavior.

### `pos.fiscal_sequence_states`

Current support:

- one state row per sequence policy;
- current, reserved, and issued sequence posture;
- state controlled-code reference;
- likely row-level lock target.

Current gaps:

- cannot by itself prove which fiscal document consumed a value;
- does not persist the formatted number on the fiscal document;
- does not implement allocation.

### `pos.fiscal_sequence_gap_audit`

Current support:

- durable gap record by sequence policy and sequence value;
- optional fiscal document reference;
- reason code/text and actor/service references.

Current gaps:

- does not define what counts as issued, reserved, void, failed, or gap;
- cannot replace the need to store assigned fiscal number on the document;
- final BIR/accounting treatment remains unresolved.

### `pos.fiscal_counter_states`

Current support:

- counter-family state by Site POS Server;
- counter value and optional monetary amount;
- status and context posture.

Current gaps:

- no direct link to a fiscal document;
- no formula or lifecycle rule for document creation;
- no fiscal number persistence.

### `pos.idempotency_records`

Current support:

- idempotency scope/key uniqueness;
- operation type/status controlled-code references;
- optional linked fiscal document;
- replay/conflict references and completion unknown posture.

Current gaps:

- no fiscal number fields;
- no current API input for idempotency key;
- does not authorize payment finality, exit/gate, or fiscal numbering by itself.

## 3. Required Fiscal Document Fields

The fiscal document persistence target should support these fields either directly on `pos.fiscal_documents` or in an approved 1:1 companion table:

- `fiscal_identity_id uuid null/not null`: references `pos.fiscal_identities`;
- `fiscal_sequence_policy_id uuid not null`: references `pos.fiscal_sequence_policies`;
- `fiscal_sequence_value bigint not null`: numeric allocated value;
- `fiscal_document_number text not null`: formatted durable fiscal document number;
- `fiscal_series text null`: series or book/register code if required;
- `fiscal_number_prefix_text text null`: copied prefix used at allocation time, if formatting must be reconstructable after policy changes;
- `fiscal_number_suffix_text text null`: copied suffix used at allocation time, if needed;
- `fiscal_number_assigned_at timestamptz not null`: assignment time;
- `fiscal_number_allocation_status_code_id uuid null`: optional controlled-code posture only if allocation can be assigned/reserved/finalized across more than one state;
- `fiscal_number_assigned_by_ref text null`: optional service/actor reference, if operational traceability requires it.

Minimum uniqueness rule:

- unique fiscal number within the approved fiscal scope.

Candidate scope:

- `(fiscal_sequence_policy_id, fiscal_sequence_value)`;
- optionally `(fiscal_identity_id, fiscal_document_number)` if fiscal identity is mandatory and formatted number uniqueness must be enforced by identity.

The exact uniqueness scope requires BIR/accreditation confirmation.

## 4. Schema Design Options

### Option A: Add Dedicated Numbering Columns to `pos.fiscal_documents`

Add fiscal numbering columns directly to the document header.

Pros:

- simplest read model and API response;
- strongest one-row representation of the fiscal document;
- easy to enforce document-level not-null posture once rollout completes;
- avoids accidental one-to-many numbering records;
- simplest idempotency link because `idempotency_records.linked_fiscal_document_id` reaches the assigned number through the header.

Cons:

- modifies an already-used core table;
- nullable rollout is required if existing documents exist;
- future numbering variants may add columns to the header;
- table comment and semantics must be revised from posture-only to allocated-number capable after implementation.

BIR/accreditation posture:

- strong, because the fiscal number is on the fiscal document record;
- supports direct audit and reporting queries.

Read endpoint impact:

- `GET /v1/fiscal-documents/{fiscalDocumentId}` can expose fiscal number fields from the header.

Idempotency impact:

- replay reads the same document and number through the linked fiscal document.

Gap/audit impact:

- `pos.fiscal_sequence_gap_audit.fiscal_document_id` can reference the document when relevant;
- assigned sequence value is directly comparable to sequence state/gap rows.

Migration/schema impact:

- requires an approved schema SQL slice or migration;
- likely starts nullable, backfills only where authoritative numbers exist, then tightens constraints later if needed.

Implementation complexity:

- lowest among durable options.

### Option B: Add `pos.fiscal_document_numbers` 1:1 Companion Table

Create a dedicated companion table linked 1:1 to `pos.fiscal_documents`.

Recommended table shape if selected:

- `fiscal_document_number_id uuid not null`
- `fiscal_document_id uuid not null`
- `fiscal_identity_id uuid null/not null`
- `fiscal_sequence_policy_id uuid not null`
- `fiscal_sequence_value bigint not null`
- `fiscal_document_number text not null`
- `fiscal_series text null`
- `fiscal_number_prefix_text text null`
- `fiscal_number_suffix_text text null`
- `fiscal_number_assigned_at timestamptz not null`
- `fiscal_number_allocation_status_code_id uuid null`
- `fiscal_number_assigned_by_ref text null`
- `number_context jsonb null`
- `created_at timestamptz not null default current_timestamp`
- `updated_at timestamptz not null default current_timestamp`

Core constraints:

- primary key on `fiscal_document_number_id`;
- unique `fiscal_document_id`;
- unique `(fiscal_sequence_policy_id, fiscal_sequence_value)`;
- optional unique `(fiscal_identity_id, fiscal_document_number)` if fiscal identity is mandatory;
- foreign keys to `pos.fiscal_documents`, `pos.fiscal_identities`, `pos.fiscal_sequence_policies`, and controlled codes if allocation status is used;
- local checks for positive sequence value and nonblank formatted number/series/prefix/suffix/actor refs.

Pros:

- isolates numbering concerns from current fiscal document header;
- supports nullable rollout without altering existing document rows heavily;
- can evolve numbering posture independently;
- keeps a clear audit boundary for assignment metadata.

Cons:

- read endpoint and reports require joins;
- allocation transaction must insert both document and number rows;
- implementation must enforce exactly one number row per document;
- slightly higher complexity.

BIR/accreditation posture:

- strong if 1:1 and unique constraints are strict;
- clear, queryable assignment record.

Read endpoint impact:

- reader must join or separately query `pos.fiscal_document_numbers`.

Idempotency impact:

- replay linked to document must join to number row to return fiscal number.

Gap/audit impact:

- gap audit can still reference the fiscal document and sequence policy/value;
- number table provides durable issued values.

Migration/schema impact:

- additive table with lower risk to existing `pos.fiscal_documents`;
- requires manifest/order update and validation inventory updates.

Implementation complexity:

- moderate.

### Option C: Use `document_context`

Store fiscal number metadata in `pos.fiscal_documents.document_context`.

Pros:

- no immediate schema change;
- fastest for prototypes.

Cons:

- weak authority posture;
- hard to enforce uniqueness, not-null, positive sequence, and FK rules;
- poor BIR/accreditation posture;
- harder to audit and report consistently;
- invites hidden fiscal authority in a flexible context field;
- conflicts with the prior planning rule that fiscal numbers should not be hidden in unrelated fields.

BIR/accreditation posture:

- not acceptable as authoritative design without explicit temporary exception.

Read endpoint impact:

- response would parse or expose JSON rather than typed fields.

Idempotency impact:

- replay would be harder to validate against unique fiscal number constraints.

Gap/audit impact:

- no reliable relational join to sequence state or gap audit by assigned value.

Migration/schema impact:

- avoids migration initially but creates future migration and data-cleanup risk.

Implementation complexity:

- deceptively low initially, higher long-term.

## 5. Recommended Option

Recommended: Option A if the project accepts modifying `pos.fiscal_documents`; otherwise Option B as the safer additive alternative. Option C must not be used as the authoritative design.

Preferred recommendation for the next schema slice: Option A, add dedicated fiscal numbering columns directly to `pos.fiscal_documents`.

Recommended columns for Option A:

- `fiscal_identity_id uuid null`
- `fiscal_sequence_policy_id uuid null`
- `fiscal_sequence_value bigint null`
- `fiscal_document_number text null`
- `fiscal_series text null`
- `fiscal_number_prefix_text text null`
- `fiscal_number_suffix_text text null`
- `fiscal_number_assigned_at timestamptz null`
- `fiscal_number_assigned_by_ref text null`

Optional column, only if allocation has multi-state posture beyond document status:

- `fiscal_number_allocation_status_code_id uuid null`

Recommended constraints for initial rollout:

- FK `fiscal_identity_id` to `pos.fiscal_identities`;
- FK `fiscal_sequence_policy_id` to `pos.fiscal_sequence_policies`;
- FK `fiscal_number_allocation_status_code_id` to `pos.controlled_codes` if used;
- check `fiscal_sequence_value is null or fiscal_sequence_value > 0`;
- checks that text fields are nonblank when present;
- check that assignment timestamp is present when sequence value/document number are present;
- unique `(fiscal_sequence_policy_id, fiscal_sequence_value)` where both are not null;
- unique `(fiscal_sequence_policy_id, fiscal_document_number)` where both are not null.

Reason for nullable initial rollout:

- existing fiscal documents may already exist;
- no invented historical fiscal numbers should be backfilled;
- constraints can be tightened later only after migration evidence and policy approval.

## 6. Relationship to Sequence Policy / State

`pos.fiscal_sequence_policies` should identify the policy used to allocate the number. `pos.fiscal_documents.fiscal_sequence_policy_id` should store the chosen policy at assignment time.

`pos.fiscal_sequence_states` should be row-locked and mutated in the same transaction as fiscal document creation. The allocated `fiscal_sequence_value` on the document should correspond to the state transition.

`pos.fiscal_sequence_gap_audit` should record recognized gaps by policy/value. It should not replace document-level number storage.

`pos.fiscal_counter_states` should be updated in the same transaction only after the exact counter families and formulas are approved. Counter state is related but not a substitute for assigned document number storage.

`pos.fiscal_identities` should be linked directly or through an approved derivation path. For audit clarity, direct `fiscal_identity_id` on the fiscal document is recommended even if the identity is derived from terminal or policy selection.

## 7. Runtime / API Impact

Expected future changes after schema approval:

- `POST /v1/fiscal-documents` should allocate fiscal number during the persistence transaction.
- Request handling may need idempotency key input or an approved derivation rule.
- `FiscalDocumentDraft` should carry fiscal identity, policy, sequence value, formatted number, and assignment timestamp after allocation.
- `CreateFiscalDocumentResponse` should include allocated fiscal document number only after durable commit.
- `GET /v1/fiscal-documents/{fiscalDocumentId}` should expose fiscal number fields when present.
- `PostgresFiscalDocumentRepository` should lock sequence state and persist document number fields in the same transaction as the current eight-table shell.
- Disposable API smoke tests should verify assigned number fields and no duplicate allocation on retry.
- Idempotency behavior should return the same fiscal document and fiscal number for the same committed upstream request.

Fail-closed persistence behavior remains unchanged for missing or invalid connection configuration.

## 8. Data Migration Posture

If existing fiscal documents exist, rollout should be staged:

1. Add fields nullable or add companion table without requiring historical rows.
2. Do not invent historical fiscal numbers.
3. Backfill only from an authoritative historical source, if one exists and is approved.
4. Validate uniqueness and linkage after any backfill.
5. Add stricter not-null constraints only for new allocation paths after implementation is proven.
6. Keep migration/rebuild validation separate from runtime allocation implementation.

No production/shared database should be mutated by local validation.

## 9. Open Decisions Before Schema Implementation

These decisions must be approved before SQL/schema work:

- exact column names;
- whether fiscal identity is required on every fiscal document;
- whether formatted number is stored, derived, or both;
- whether prefix/suffix/series are copied into the document row;
- whether fiscal number assignment timestamp is required;
- whether an allocation status controlled-code is needed;
- controlled-code values needed for sequence family/status, policy status, allocation status, gap reasons, and idempotency operation/status;
- uniqueness scope for fiscal number;
- BIR/accreditation confirmation for reserved, issued, void, failed, and gap semantics;
- whether Option A is acceptable or Option B is preferred for additive isolation.

## 10. Stop / Go Recommendation

OK to proceed to a schema design/SQL slice after this gap resolution is reviewed and Option A or Option B is approved.

Not OK to implement runtime number allocation yet.

Not OK to use `document_context` as the authoritative fiscal number store unless explicitly accepted as temporary, documented as non-authoritative, and paired with a migration plan to formal fields.
