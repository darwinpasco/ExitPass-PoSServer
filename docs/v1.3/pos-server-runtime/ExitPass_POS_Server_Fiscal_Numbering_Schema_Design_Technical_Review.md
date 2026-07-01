# ExitPass POS Server Fiscal Numbering Schema Design Technical Review

## Review Summary

Created a documentation-only schema design package for Option A: dedicated nullable fiscal numbering columns on `pos.fiscal_documents`.

The design prepares a future SQL slice but does not create SQL, modify schema, implement runtime allocation, or mutate counters.

## Findings

P0 0 / P1 0 / P2 0 / Editorial 0

## Scope Review

- Documentation/design only.
- No runtime code changed.
- No API code changed.
- No persistence code changed.
- No `db/state` SQL changed.
- No migrations added.
- No controlled-code JSON changed.
- No generated SQL changed.
- No CI changed.
- No Atlas configuration changed.
- No sample data added.
- No number allocation implemented.
- No counter mutation implemented.
- No BIR reporting, Digital SI, Annex E, or X/Z behavior added.
- No statutory discount validation added.
- No payment finality ownership added.
- No refund/reversal authority added.
- No gate/exit behavior added.

## Design Review

The design selects Option A for the future SQL posture:

- add nullable fiscal numbering columns directly to `pos.fiscal_documents`;
- keep `document_context` non-authoritative;
- defer runtime allocation until SQL and validation are complete.

The proposed columns cover:

- `fiscal_identity_id`
- `fiscal_sequence_policy_id`
- `fiscal_sequence_value`
- `fiscal_document_number`
- `fiscal_series`
- `fiscal_number_prefix_text`
- `fiscal_number_suffix_text`
- `fiscal_number_assigned_at`
- `fiscal_number_assigned_by_ref`

The optional `fiscal_number_allocation_status_code_id` is explicitly deferred.

## Constraint and Index Review

The design recommends:

- FKs to `pos.fiscal_identities` and `pos.fiscal_sequence_policies`;
- partial unique index on `(fiscal_sequence_policy_id, fiscal_sequence_value)` for assigned rows;
- partial unique index on `(fiscal_sequence_policy_id, fiscal_document_number)` for assigned rows;
- lookup indexes for fiscal identity, sequence policy, and document number;
- checks for positive sequence value and nonblank optional text;
- paired assignment check for core number fields.

## Rollout Review

The design uses nullable rollout to avoid inventing historical fiscal numbers. Backfill is allowed only from an authoritative source. Later constraint tightening is deferred until runtime allocation and validation are proven.

## Runtime Impact Review

The design identifies future changes needed in:

- `POST /v1/fiscal-documents`;
- `GET /v1/fiscal-documents/{fiscalDocumentId}`;
- `PostgresFiscalDocumentRepository`;
- `PostgresFiscalDocumentReader`;
- disposable PostgreSQL API smoke tests;
- idempotency behavior.

No runtime code was changed in this task.

## Validation Results

- `dotnet build`: passed.
- `dotnet test`: passed; Runtime 54 tests, Persistence.Postgres 21 tests, API 51 tests, API.IntegrationTests 2 tests.
- `powershell -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed.
- `git status --short --untracked-files=all`: only the two fiscal numbering schema design Markdown files are untracked.
- `git diff --check`: passed.

## Recommended Next Step

Proceed to a narrowly scoped schema SQL slice for `pos.fiscal_documents` after review. Do not implement runtime allocation, counter mutation, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, refund/reversal authority, or gate/exit behavior from this design alone.
