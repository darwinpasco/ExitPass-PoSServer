# ExitPass POS Server Fiscal Numbering and Counter Allocation Plan Technical Review

## Review Summary

Created a documentation-only planning package for fiscal numbering and counter allocation. The plan recommends same-transaction allocation during fiscal document creation, paired with idempotency and row-level sequence locking, but does not approve or implement allocation yet.

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
- No BIR reporting, Digital SI, Annex E, or X/Z behavior added.
- No statutory discount validation added.
- No payment finality ownership added.
- No refund/reversal authority added.
- No gate/exit behavior added.

## Schema Review

The plan inspected and interpreted:

- `pos.fiscal_documents`
- `pos.fiscal_sequence_policies`
- `pos.fiscal_sequence_states`
- `pos.fiscal_sequence_gap_audit`
- `pos.fiscal_counter_states`
- `pos.fiscal_state_snapshots`
- `pos.fiscal_lock_states`
- `pos.idempotency_records`

Important gap identified: `pos.fiscal_documents` currently has no dedicated fiscal number, sequence policy, sequence value, fiscal identity, series, or formatted-number column. Implementation must not invent hidden fiscal numbering persistence in unrelated fields without approval.

## Runtime/API Review

The current runtime/API model was inspected. Current command, draft, response, and PostgreSQL mapping do not include fiscal number, sequence policy, sequence value, or idempotency key fields. The plan marks these as future implementation decisions.

## Transaction Recommendation Review

The plan recommends Option A: allocate fiscal numbers during the same PostgreSQL transaction as fiscal document creation. The proposed posture is:

- validate first;
- check/lock idempotency;
- select sequence policy;
- lock sequence state row;
- allocate number only when ready to persist;
- insert fiscal document shell;
- update sequence/counter state;
- commit only after all rows and state updates succeed.

This is a recommendation only. Implementation is not approved yet until open questions are resolved or explicitly accepted.

## Boundary Review

The plan preserves authority boundaries:

- Central PMS/payment systems do not allocate fiscal document numbers;
- idempotency does not create payment finality;
- counter/numbering behavior does not create exit authorization or gate execution;
- statutory discount validation remains outside POS Server;
- BIR reporting, Digital SI, Annex E, and X/Z remain future work.

## Open Questions Review

The plan lists required implementation blockers, including sequence scope, fiscal identity/series selection, document type to sequence policy mapping, controlled-code values, issued/reserved/gap semantics, BIR/accreditation expectations, recovery flow, actor/service identity tracking, operational reporting, number formatting, and idempotency key source.

## Validation Results

- `dotnet build`: passed.
- `dotnet test`: passed; Runtime 54 tests, Persistence.Postgres 21 tests, API 51 tests, API.IntegrationTests 2 tests.
- `powershell -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed.
- `git diff --check`: passed.
- `git status --short --untracked-files=all`: only the two planning documents are newly added for this slice; the pre-existing untracked `src/ExitPass.PosServer.Api/Properties/launchSettings.json` remains untouched.

## Recommended Next Step

Review and resolve the open questions before implementation. The preferred future implementation remains same-transaction allocation with idempotency and row-level sequence locking. Do not implement numbering/counter allocation, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, refund/reversal authority, or gate/exit behavior until explicitly approved.
