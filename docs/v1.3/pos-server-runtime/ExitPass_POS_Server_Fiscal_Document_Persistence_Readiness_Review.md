# ExitPass POS Server Fiscal Document Persistence Readiness Review

## 1. Current Runtime Capability

POS Server now has a thin fiscal document creation runtime path:

- API route: `POST /v1/fiscal-documents`
- API DTO mapping into `FiscalDocumentCreationCommand`
- runtime validation through `FiscalDocumentCreationService`
- fail-closed repository behavior when persistence is not configured
- deterministic invalid persistence configuration behavior when configured connection strings are malformed or URL-style values are supplied where Npgsql key-value connection strings are required
- PostgreSQL persistence adapter behind `IFiscalDocumentRepository`
- parameterized SQL insert mapping for the fiscal document persistence shell

The runtime path accepts fiscalization inputs from an upstream approved payable-basis/finality context. It preserves upstream references for audit reconstruction and rejects raw evidence/payment payload markers before persistence.

Manual API testing remains part of the readiness posture before commit approval for each runtime persistence slice. Recent manual testing also confirmed the API can fail closed for missing persistence configuration and invalid URL-style `POSSERVER_DB_URL` values without returning HTTP 500.

## 2. Tables Currently Written

Fiscal document creation now writes these tables:

1. `pos.fiscal_documents`
2. `pos.fiscal_document_status_history`
3. `pos.fiscal_document_links`
4. `pos.fiscal_document_lines`
5. `pos.fiscal_tenders`
6. `pos.fiscal_tax_details`
7. `pos.fiscal_discount_privilege_details`
8. `pos.fiscal_totals`

No report, Digital SI, Annex E, X/Z, audit, exit, or gate tables are written by this runtime path.

## 3. Transaction Boundary

All persistence writes are intended to happen in one PostgreSQL transaction.

The repository opens one transaction, inserts the header, status history, links, lines, tenders, tax details, discount/privilege details, and totals, then commits only after all requested rows succeed.

If any insert fails, the repository rolls back the full transaction and returns a deterministic persistence failure through the API error mapping. There should be no committed partial fiscal document shell when a later requested row fails.

## 4. Authority Boundary

The locked boundary remains:

Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.

POS Server still does not own:

- statutory discount validation
- Senior/PWD entitlement approval
- local ordinance eligibility decision
- raw evidence/image storage
- payment finality
- refund/reversal authority
- exit authorization
- gate execution
- BIR report finality
- X/Z finality
- Annex E finality
- Digital SI issuance

Discount, privilege, tax, tender, and total rows are fiscal representation/evidence rows for the approved upstream payable basis. They do not create upstream authority or finality.

## 5. Manual API Test Coverage Already Performed

Recent runtime slices exposed and addressed API/manual-test issues around DTO mapping and persistence configuration:

- complete local fiscal schema context can reach downstream validation
- top-level `upstreamFinalityRef` can map into the payable-basis upstream finality reference
- missing fiscal lines fail deterministically
- invalid fiscal line quantity/amount/type inputs fail before persistence
- missing fiscal tenders fail deterministically
- invalid tender amount and currency mismatch fail before persistence
- tender raw credential/token/payment payload markers fail before persistence
- invalid tax detail amount, currency mismatch, and missing line reference fail before persistence
- invalid discount/privilege detail amount, currency mismatch, missing line reference, blank refs, and sensitive evidence markers fail before persistence
- invalid fiscal total amount, currency mismatch, duplicate total type, and sensitive total context fail before persistence
- no persistence configuration returns `persistence_not_configured`
- invalid persistence configuration returns `invalid_persistence_configuration`
- URL-style `POSSERVER_DB_URL` does not crash the API with HTTP 500

Manual testing has been aimed at validating request-shape reachability, deterministic error behavior, and authority-boundary guardrails. It is not a replacement for disposable PostgreSQL API integration testing.

## 6. Automated Validation Posture

Current automated checks include:

- `dotnet build`
- `dotnet test`
- POS DB Static validation through `db/scripts/Invoke-PosDbChecks.ps1 -Mode Static`
- CI Static validation
- CI ControlledCodeLoad validation against a disposable PostgreSQL database
- runtime/API unit tests for validation and DTO mapping
- persistence adapter tests that inspect SQL targets, parameterized SQL, context serialization, and transaction ordering

The adapter/unit tests verify intended SQL mapping and transaction behavior by inspection. They do not yet prove an end-to-end API request writes rows into a real disposable PostgreSQL database.

## 7. Gaps Before Real Runtime Confidence

Remaining gaps before runtime confidence:

- no disposable PostgreSQL API integration test yet for actual end-to-end `POST /v1/fiscal-documents` writing rows
- no real configured DB smoke test through the API yet
- no row-count verification after an API write yet
- no read/query endpoint yet to inspect a created fiscal document
- no actual fiscal document number/counter allocation behavior in this runtime path
- no BIR report behavior
- no Digital SI behavior
- no Annex E or X/Z behavior
- no production deployment configuration
- no real controlled-code semantic approval for BIR/accounting-sensitive classifications beyond the current safe baseline

These gaps are expected for the current slice boundary. They should be addressed before any BIR-sensitive/reporting work is treated as runtime-ready.

## 8. Recommended Next Implementation Options

Recommended next slice:

1. Disposable PostgreSQL API integration smoke test or harness.

The next slice should:

- start from the existing schema rebuild workflow
- load the controlled-code baseline
- configure API persistence against a disposable database
- POST one valid fiscal document request
- verify rows exist in the eight fiscal document persistence tables
- verify transaction rollback for a forced failure if feasible
- avoid BIR reporting, Digital SI, Annex E, X/Z, new schema, migrations, sample production data, or new authority behavior

Later implementation options:

2. Read/query endpoint for created fiscal document inspection
3. Fiscal document numbering/counter allocation
4. BIR-sensitive controlled-code approval
5. Digital SI behavior
6. Report slices such as BIR, Annex E, X-read, and Z-read

## 9. Stop / Go Assessment

Go:

- OK to proceed to disposable PostgreSQL API integration smoke testing.

Stop:

- Not OK to proceed directly to BIR reports yet.
- Not OK to add statutory discount validation to POS Server.
- Not OK to treat fiscal totals as BIR, tax return, X/Z, Annex E, payment, refund/reversal, exit, or gate finality.

## 10. Scope Discipline Confirmation

This readiness review is documentation-only and changes no:

- `db/state`
- migrations
- controlled-code JSON
- generated SQL
- CI
- Atlas config
- sample transaction data
- runtime feature code
- BIR reporting
- Digital SI
- Annex E
- X/Z
- statutory discount validation
- payment finality ownership
- refund/reversal authority
- gate/exit behavior

## 11. Validation Result

Validation for this docs-only checkpoint:

- `dotnet build`: passed
- `dotnet test`: passed; Runtime 52 tests, Persistence.Postgres 17 tests, API 42 tests
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed
- `git status --short --untracked-files=all`: only this readiness review document is untracked
- `git diff --check`: passed
