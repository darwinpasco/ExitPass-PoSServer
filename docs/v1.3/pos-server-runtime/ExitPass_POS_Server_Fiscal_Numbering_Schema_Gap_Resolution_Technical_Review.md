# ExitPass POS Server Fiscal Numbering Schema Gap Resolution Technical Review

## Review Summary

Created a documentation-only schema gap resolution package for fiscal numbering. The document confirms that runtime number allocation remains blocked until a formal fiscal numbering persistence target is approved and implemented.

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

## Schema Gap Review

The gap is clearly identified: `pos.fiscal_documents` has no durable fiscal number, sequence policy, sequence value, fiscal identity, series, formatted number, or assignment timestamp field.

The resolution narrows the valid schema choices to:

- Option A: add dedicated fiscal numbering columns directly to `pos.fiscal_documents`;
- Option B: add a 1:1 `pos.fiscal_document_numbers` companion table.

Option C, using `document_context`, is rejected as the authoritative design. It may only be used as explicitly documented temporary/non-authoritative posture if separately approved.

## Recommendation Review

Preferred recommendation: Option A, add dedicated fiscal numbering columns directly to `pos.fiscal_documents`, initially nullable for safe rollout.

The document also defines Option B as a valid additive alternative if the project prefers isolating numbering assignment into a companion table.

No SQL was generated in this slice.

## Implementation Blocker Review

Implementation remains blocked until schema design is approved. Open decisions include exact column names, fiscal identity requirement, formatted number storage, prefix/suffix/series copy posture, assignment timestamp, allocation status code need, controlled-code values, uniqueness scope, and BIR/accreditation confirmation.

## Validation Results

- `dotnet build`: passed.
- `dotnet test`: passed on rerun; the first run was blocked by a transient Microsoft Defender file lock on the runtime assembly after build.
- Test counts: Runtime 54, Persistence.Postgres 21, API 51, API.IntegrationTests 2.
- `powershell -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed.
- `git diff --check`: passed.
- `git status --short --untracked-files=all`: only the two schema gap resolution documents are newly added for this slice; the pre-existing untracked `src/ExitPass.PosServer.Api/Properties/launchSettings.json` remains untouched.

## Recommended Next Step

Review and approve Option A or Option B before any schema SQL slice. Do not implement runtime number allocation, counter mutation, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, refund/reversal authority, or gate/exit behavior until schema posture is approved.
