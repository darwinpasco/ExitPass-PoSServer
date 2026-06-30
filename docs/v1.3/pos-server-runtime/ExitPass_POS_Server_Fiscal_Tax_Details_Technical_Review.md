# ExitPass POS Server Fiscal Tax Details Technical Review

## Review Summary

Fiscal document creation now supports fiscal tax detail persistence using the existing `pos.fiscal_tax_details` table. The implementation keeps tax details as fiscalization detail/evidence for an approved upstream payable basis and does not introduce tax calculation, BIR finality, statutory discount validation, payment finality ownership, refund/reversal authority, or gate/exit behavior.

## Findings

P0 0 / P1 0 / P2 0 / Editorial 0

## Scope Review

- No `db/state` SQL files were modified.
- No migrations were added.
- No controlled-code JSON was modified.
- No generated SQL was modified.
- No CI workflow was modified.
- No Atlas configuration was added.
- No sample transaction data was added.
- No Slice 4 or new controlled-code values were added.

## Persistence Review

The PostgreSQL adapter writes only to the approved existing POS schema tables:

1. `pos.fiscal_documents`
2. `pos.fiscal_document_status_history`
3. `pos.fiscal_document_links`
4. `pos.fiscal_document_lines`
5. `pos.fiscal_tenders`
6. `pos.fiscal_tax_details`

All writes run in a single transaction. Fiscal line IDs are generated before line inserts and retained in a line-sequence map so line-scoped tax details can reference the correct `fiscal_document_line_id` in the same transaction.

## SQL Safety Review

SQL uses explicit column lists and Npgsql parameters. No untrusted request values are interpolated into SQL. The fiscal tax detail insert targets only `pos.fiscal_tax_details` and maps schema-backed values: document ID, optional line ID, tax type code, tax classification code, tax rate, taxable amount, tax amount, currency code, and tax context JSON.

## Runtime/API Guardrails

Runtime/API validation covers:

- required tax type and tax classification code IDs;
- nonnegative taxable and tax amounts;
- nonnegative optional tax rate;
- currency format and payable-basis currency match;
- line-scoped tax details referencing an existing request line;
- blank optional context rejection;
- raw evidence, credential, payment payload, token, secret, and provider callback marker rejection before persistence.

Pending, rejected, expired, unresolved, or inconsistent statutory discount references remain rejected before persistence.

## Boundary Review

The slice does not add:

- discount/privilege details;
- totals;
- BIR reporting;
- Digital SI;
- Annex E;
- X/Z reports;
- statutory discount entitlement validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate behavior.

Tax detail rows do not establish BIR report finality, tax return finality, statutory entitlement, payment finality, or refund/reversal authority.

## Test Review

Tests cover tax detail DTO mapping, runtime validation, SQL mapping, reference-only context serialization, parameterized SQL, transaction ordering, and authority-boundary guardrails.

Validation results:

- `dotnet build` passed.
- `dotnet test` passed: Runtime 35 tests, Persistence.Postgres 13 tests, API 34 tests.
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static` passed.
- `git diff --check` passed with line-ending warnings only.

## Manual API Test Status

Manual API testing is still required before commit approval. The manual payload should include `taxDetails` only as fiscalization tax detail rows derived from the approved upstream payable basis; it must not include raw evidence, payment payloads, entitlement approval fields, BIR report fields, refund/reversal authority, or gate/exit fields.

## Recommended Next Step

Run final static validation and then perform manual API testing for valid tax detail persistence, invalid tax amount, currency mismatch, missing line reference, and sensitive tax context rejection. Do not proceed to discount/privilege details or totals until this slice is reviewed.
