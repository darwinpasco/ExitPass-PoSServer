# ExitPass POS Server Fiscal Document Status History Write Technical Review

## 1. Review Summary

This review covers the persistence extension that writes an initial fiscal document status history row when a fiscal document header is created.

The implementation writes only existing POS schema tables and does not change schema SQL or add migrations.

## 2. Schema Review

Reviewed tables:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`

The status history schema supports an initial row without new runtime fields:

- `prior_fiscal_document_status_code_id` may be null
- `new_fiscal_document_status_code_id` is required
- `actor_ref` may be null
- `service_identity_ref` may be null

No schema changes were required.

## 3. Persistence Mapping Review

`PostgresFiscalDocumentRepository` now writes:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`

The status history row uses the same status code as the header:

- header `fiscal_document_status_code_id`
- history `new_fiscal_document_status_code_id`

The history row uses null prior status because it is the initial lifecycle state.

## 4. Transaction Review

Header and status history inserts run in a single PostgreSQL transaction.

The repository:

- opens a transaction
- executes the header insert
- executes the status history insert
- commits only after both inserts succeed
- rolls back if either insert fails

This provides fail-closed behavior: no committed header without initial status history.

## 5. SQL Safety Review

The SQL:

- uses explicit column lists
- uses parameterized Npgsql commands only
- does not interpolate untrusted request values
- targets only the two approved tables

No fiscal lines, tenders, taxes, discounts, totals, reports, Digital SI, audit, recovery, outbox, exit, or gate tables are written.

## 6. Authority-Boundary Review

The locked boundary remains:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

Confirmed:

- no statutory discount validation moved into POS Server
- no entitlement approval behavior added
- no raw evidence/image storage added
- no local ordinance decision added
- no independent statutory entitlement computation added
- no payment finality ownership added
- no gate/exit behavior added

The status history row is fiscal document lifecycle evidence only. It is not payment finality, ExitAuthorization, gate authority, or full audit behavior.

## 7. Test Review

Tests cover:

- repository writes are scoped to `pos.fiscal_documents` and `pos.fiscal_document_status_history`
- status history uses the same status code parameter as the header
- repository source uses one transaction, commit, and rollback paths
- SQL remains parameterized
- unsupported table writes are absent
- authority-leaking behavior is absent
- existing runtime/API guardrail tests still reject invalid inputs before persistence

Disposable PostgreSQL integration testing was not added in this slice. The current tests inspect SQL mapping and transaction usage without requiring developer database secrets or a local database lifecycle.

## 8. Scope Discipline Review

Confirmed:

- no `db/state` SQL changed
- no migrations added
- no controlled-code JSON changed
- no generated SQL changed
- no CI changed
- no Atlas config changed
- no sample transaction data added
- writes only to existing POS schema tables:
  - `pos.fiscal_documents`
  - `pos.fiscal_document_status_history`
- no BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, or gate/exit behavior added

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
- PostgreSQL persistence tests: 6 passed.

## 10. Finding Counts

P0 0 / P1 0 / P2 0 / Editorial 0

## 11. Final Recommendation

The status history write is ready for review. Do not proceed to fiscal lines, tenders, taxes, discounts, totals, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, or additional runtime flows until separately approved.
