# ExitPass POS Server Fiscal Document Persistence Wiring Technical Review

## 1. Review Summary

This review covers the first PostgreSQL persistence adapter behind `IFiscalDocumentRepository`.

The implementation writes only the fiscal document header to the existing `pos.fiscal_documents` table and preserves upstream references for audit reconstruction. It does not alter schema, create migrations, add reporting behavior, or move statutory discount validation into POS Server.

## 2. Implementation Review

Added:

- `src/ExitPass.PosServer.Persistence.Postgres/`
- `tests/ExitPass.PosServer.Persistence.Postgres.Tests/`
- `FiscalDocumentPersistenceException`
- schema-backed UUID fields on fiscal document command/draft/API request models
- API DI wiring that selects PostgreSQL persistence only when a connection string is configured

The API still fails closed through `PersistenceNotConfiguredFiscalDocumentRepository` when persistence is not configured.

## 3. Persistence Mapping Review

The adapter writes only:

- `pos.fiscal_documents`

The insert uses an explicit column list and Npgsql parameters.

No rows are written to status history, links, lines, tenders, tax details, discount/privilege details, totals, reports, Digital SI, audit, recovery, outbox, exit, or gate tables.

## 4. Reference Preservation Review

Preserved references include:

- payable-basis reference
- upstream finality reference
- Central PMS parking session reference
- Central PMS payment attempt reference
- Central PMS payment confirmation reference
- reference-only payment finality context
- vendor acknowledgement reference
- statutory discount validation reference

References in `document_context` are traceability only and do not create validation, payment finality, entitlement, exit, or gate authority.

## 5. Authority-Boundary Review

The implementation preserves:

- Operator Console validates
- Central PMS / Discount Service authorizes the payable-basis effect
- POS Server fiscalizes

Confirmed:

- no statutory discount validation moved into POS Server
- no entitlement approval behavior added
- no raw evidence/image storage added
- no local ordinance decision added
- no independent statutory entitlement computation added
- no payment finality ownership added
- no gate/exit behavior added

## 6. SQL Safety Review

The persistence SQL:

- uses explicit column lists
- targets `pos.fiscal_documents` only
- uses parameterized Npgsql commands
- does not interpolate untrusted request values
- stores flexible traceability context as JSONB

Database write failures are wrapped as `FiscalDocumentPersistenceException` and mapped by the API to deterministic `persistence_write_failed`.

## 7. Test Review

Tests cover:

- valid fiscal document creation reaches configured PostgreSQL repository through DI
- API/runtime fail closed when persistence is not configured
- missing payable basis is rejected before persistence
- missing upstream finality/payment reference is rejected before persistence
- pending/rejected/expired/unresolved/inconsistent statutory discount references are rejected before persistence
- raw ID/evidence payload markers are rejected before persistence
- generated SQL is parameterized and does not interpolate untrusted request values
- no entitlement approval, Operator Console validation, local ordinance decision, payment finality, or gate/exit behavior is introduced

Disposable PostgreSQL integration testing was not added in this slice because it would require an external database lifecycle beyond unit/adapter tests. Existing repository DB validation remains available through `db/scripts/Invoke-PosDbChecks.ps1`.

## 8. Scope Discipline Review

Confirmed:

- no `db/state` SQL changed
- no migrations added
- no controlled-code JSON changed
- no generated SQL changed
- no CI changed
- no Atlas config changed
- no sample transaction data added
- no BIR reporting, Digital SI, Annex E, or X/Z behavior added
- persistence writes only to existing POS schema tables

## 9. Validation Results

Commands run:

```powershell
dotnet build
dotnet test
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static
git status --short --untracked-files=all
git diff --check
```

Results:

- `dotnet build` passed.
- `dotnet test` passed.
- Runtime tests: 12 passed.
- API tests: 16 passed.
- PostgreSQL persistence tests: 4 passed.
- Static DB validation passed: `POS DB checks completed for mode Static.`

## 10. Finding Counts

P0 0 / P1 0 / P2 0 / Editorial 0

## 11. Final Recommendation

The persistence wiring is ready for review. Do not proceed to status history, fiscal details, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, or additional runtime flows until separately approved.
