# ExitPass POS Server Fiscal Totals Technical Review

## Review Summary

Fiscal document creation now supports fiscal total persistence using the existing `pos.fiscal_totals` table. The implementation keeps total rows as fiscal representation/summary rows for an upstream approved payable basis and does not introduce BIR reporting, X/Z behavior, Annex E, Digital SI, statutory discount validation, entitlement approval, payment finality ownership, refund/reversal authority, or gate/exit behavior.

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
7. `pos.fiscal_discount_privilege_details`
8. `pos.fiscal_totals`

All writes run in a single transaction. Fiscal totals are inserted after discount/privilege detail rows and before transaction commit.

## SQL Safety Review

SQL uses explicit column lists and Npgsql parameters. No untrusted request values are interpolated into SQL. The fiscal total insert targets only `pos.fiscal_totals` and maps schema-backed values: document ID, total type code, amount, currency code, and total context JSON.

## Runtime/API Guardrails

Runtime/API validation covers:

- required total type code ID;
- nonnegative amount;
- currency format and payable-basis currency match;
- duplicate total type rejection before database unique-constraint failure;
- blank optional context rejection;
- raw evidence, credential, payment payload, token, secret, and provider callback marker rejection before persistence.

Pending, rejected, expired, unresolved, or inconsistent statutory discount references remain rejected before persistence.

## Boundary Review

The slice does not add:

- BIR reporting;
- Digital SI;
- Annex E;
- X/Z reports;
- statutory discount entitlement validation;
- entitlement approval;
- local ordinance eligibility decisions;
- raw evidence storage;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate behavior.

Fiscal total rows do not establish BIR report finality, tax return finality, payment finality, statutory entitlement, refund/reversal authority, exit authorization, or gate execution.

## Test Review

Tests cover fiscal total DTO mapping, runtime validation, SQL mapping, reference-only context serialization, parameterized SQL, transaction ordering, and authority-boundary guardrails.

Validation results:

- `dotnet build` passed.
- `dotnet test` passed: Runtime 52 tests, Persistence.Postgres 17 tests, API 42 tests.
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static` passed.
- `git diff --check` passed with line-ending warnings only.

## Manual API Test Status

Manual API testing is still required before commit approval. The manual payload should include `totals` only as fiscalization summary rows derived from the approved upstream payable basis; it must not include BIR report fields, X/Z fields, Annex E fields, raw evidence, entitlement approval fields, local ordinance decision fields, payment finality fields, refund/reversal authority, or gate/exit fields.

## Recommended Next Step

Run final `git diff --check` and perform manual API testing for valid fiscal total persistence, invalid amount, currency mismatch, duplicate total type, and sensitive total context rejection. Do not proceed to BIR reporting, Digital SI, Annex E, or X/Z until this slice is reviewed.
