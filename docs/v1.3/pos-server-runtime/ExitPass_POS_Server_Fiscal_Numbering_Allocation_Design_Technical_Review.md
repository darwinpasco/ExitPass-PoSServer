# ExitPass POS Server Fiscal Numbering Allocation Design Technical Review

## 1. Review Summary

This review covers the runtime fiscal number allocation design package.

The design is documentation only. It defines the intended allocation boundary, idempotency requirements, policy/identity resolution gaps, transaction order, locking posture, rollback/gap posture, API behavior, and future implementation sequence.

## 2. Overall Recommendation

Ready for review after validation.

Runtime fiscal number allocation remains blocked until idempotency source and sequence policy resolution are accepted.

## 3. Blocking Findings

P0: 0

## 4. Should-Fix Findings

P1: 0

## 5. Non-Blocking Findings

P2: 0

## 6. Editorial Findings

Editorial: 0

## 7. Scope Review

This task is documentation/design only.

No runtime allocation code was added. No sequence or counter state mutation was implemented.

## 8. Design Review

The design recommends:

- allocation during `POST /v1/fiscal-documents`;
- allocation inside the same PostgreSQL transaction as fiscal document creation;
- row-level locking on the selected `pos.fiscal_sequence_states` row;
- API response with fiscal number only after durable commit;
- fail-closed policy/identity/idempotency behavior;
- no use of `document_context` as authoritative fiscal number storage.

## 9. Idempotency Review

The design correctly blocks allocation implementation until idempotency source/key behavior is approved.

Current gap is explicit: the public command/API does not yet expose an idempotency key, and implementation must resolve that before allocation code is added.

## 10. Sequence Policy and Identity Review

The design recommends server-side sequence policy resolution by Site POS Server, document type, active/effective policy posture, and ambiguity failure.

Fiscal identity resolution remains an explicit gap. Terminal context is treated as supporting context only and does not create independent terminal fiscal authority.

## 11. Transaction and Locking Review

The proposed transaction order keeps idempotency, policy/identity resolution, fiscal sequence state lock, fiscal document persistence, sequence state mutation, and idempotency completion in one transaction.

The design treats partial unique indexes as defense in depth rather than allocator logic.

## 12. Gap and Counter Review

Rollback before commit is not treated as a durable fiscal gap.

Committed numbers must not be reused. Gap audit is reserved for durably recognized gap/reservation/issuance cases.

The design defers `pos.fiscal_counter_states`, GTA, Z/reset, and BIR/accounting-sensitive counter formulas to later approved slices.

## 13. Boundary Review

This task did not add:

- BIR reporting;
- Digital SI;
- Annex E;
- X/Z;
- statutory discount validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution.

## 14. Scope Discipline Review

This task did not modify:

- runtime code;
- API code;
- persistence code;
- `db/state` SQL;
- migrations;
- controlled-code JSON;
- generated SQL;
- CI workflow files;
- Atlas configuration;
- sample data.

## 15. Validation Review

Local validation performed:

- `dotnet build`
- `dotnet test`
- `powershell -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`
- `git status --short --untracked-files=all`
- `git diff --check`

Results:

- `dotnet build` passed.
- `dotnet test` passed: 23 persistence tests, 54 runtime tests, 2 API integration tests, and 53 API tests passed.
- Static DB validation passed.
- `git status --short --untracked-files=all` showed only the two new documentation files.
- `git diff --check` passed.

Manual API testing is not required because this is a documentation-only task.

## 16. Final Recommendation

Proceed to review after validation.

Do not implement runtime fiscal number allocation until idempotency source/key behavior and sequence policy resolution are explicitly accepted. Do not proceed to BIR reporting from this design alone.
