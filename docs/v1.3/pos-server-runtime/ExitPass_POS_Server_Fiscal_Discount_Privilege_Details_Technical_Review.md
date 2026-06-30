# ExitPass POS Server Fiscal Discount / Privilege Details Technical Review

## Review Summary

Fiscal document creation now supports fiscal discount/privilege detail persistence using the existing `pos.fiscal_discount_privilege_details` table. The implementation keeps the rows as fiscal representation for an upstream approved payable basis and does not introduce entitlement approval, local ordinance decisions, BIR report finality, payment finality ownership, refund/reversal authority, or gate/exit behavior.

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

All writes run in a single transaction. Fiscal line IDs are generated before line inserts and retained in a line-sequence map so line-scoped discount/privilege details can reference the correct `fiscal_document_line_id` in the same transaction.

## SQL Safety Review

SQL uses explicit column lists and Npgsql parameters. No untrusted request values are interpolated into SQL. The fiscal discount/privilege detail insert targets only `pos.fiscal_discount_privilege_details` and maps schema-backed values: document ID, optional line ID, discount/privilege type code, basis amount, discount amount, VAT privilege amount, currency code, beneficiary reference, evidence reference, approval reference, and context JSON.

## Runtime/API Guardrails

Runtime/API validation covers:

- required discount/privilege type code ID;
- nonnegative basis, discount, and VAT privilege amounts;
- discount and VAT privilege amounts bounded by basis amount;
- currency format and payable-basis currency match;
- line-scoped details referencing an existing request line;
- blank optional reference/context rejection;
- raw evidence, credential, payment payload, token, secret, and provider callback marker rejection before persistence.

Pending, rejected, expired, unresolved, or inconsistent statutory discount references remain rejected before persistence.

## Boundary Review

The slice does not add:

- totals;
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

Discount/privilege rows do not establish statutory entitlement, local ordinance eligibility, operator approval, BIR report finality, payment finality, or refund/reversal authority. `beneficiaryRef`, `evidenceRef`, and `approvalRef` remain references only.

## Test Review

Tests cover discount/privilege detail DTO mapping, runtime validation, SQL mapping, reference-only context serialization, parameterized SQL, transaction ordering, and authority-boundary guardrails.

Validation results:

- `dotnet build` passed.
- `dotnet test` passed: Runtime 46 tests, Persistence.Postgres 15 tests, API 38 tests.
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static` passed.
- `git diff --check` passed with line-ending warnings only.

## Manual API Test Status

Manual API testing is still required before commit approval. The manual payload should include `discountPrivilegeDetails` only as fiscalization detail rows derived from the approved upstream payable basis; it must not include raw evidence, entitlement approval fields, local ordinance decision fields, payment finality fields, refund/reversal authority, BIR report fields, or gate/exit fields.

## Recommended Next Step

Run final `git diff --check` and perform manual API testing for valid discount/privilege detail persistence, invalid amounts, currency mismatch, missing line reference, blank reference fields, and sensitive evidence marker rejection. Do not proceed to totals until this slice is reviewed.
