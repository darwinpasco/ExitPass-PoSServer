# ExitPass POS Server Fiscal Numbering Read Model Technical Review

## 1. Review Summary

This change adds read-only runtime/API support for the nullable fiscal numbering fields now present on `pos.fiscal_documents`.

The change updates the runtime read model, PostgreSQL reader SELECT/mapping, API GET response surface, unit tests, and disposable PostgreSQL smoke assertions.

## 2. Overall Recommendation

Ready for review.

Runtime fiscal number allocation remains blocked.

## 3. Blocking Findings

P0: 0

## 4. Should-Fix Findings

P1: 0

## 5. Non-Blocking Findings

P2: 0

## 6. Editorial Findings

Editorial: 0

## 7. Read Model Review

`FiscalDocumentReadModel` now includes nullable fiscal numbering fields:

- `FiscalIdentityId`
- `FiscalSequencePolicyId`
- `FiscalSequenceValue`
- `FiscalDocumentNumber`
- `FiscalSeries`
- `FiscalNumberPrefixText`
- `FiscalNumberSuffixText`
- `FiscalNumberAssignedAt`
- `FiscalNumberAssignedByRef`

The fields remain nullable because allocation is not implemented.

## 8. PostgreSQL Reader Review

`PostgresFiscalDocumentReader` now selects the nullable fiscal numbering columns from `pos.fiscal_documents` and maps them to the read model.

The reader remains read-only:

- parameterized `SELECT` by `@fiscal_document_id`;
- no insert/update/delete SQL;
- no sequence state lock;
- no fiscal sequence or counter mutation.

## 9. API Response Review

`GET /v1/fiscal-documents/{fiscalDocumentId}` returns the existing document read model, so the new nullable fields are included in the existing response shape.

Null values are safe and expected for documents created by the current POST path because runtime allocation is not implemented.

## 10. Test Review

Tests cover:

- PostgreSQL reader SELECT includes fiscal numbering columns.
- Reader source remains read-only and parameterized.
- Null fiscal numbering fields are returned safely.
- Populated fiscal numbering fields are returned correctly through the API endpoint mapping.
- Missing document still maps to deterministic 404.
- Missing persistence configuration still fails closed.
- Invalid persistence configuration still fails closed.
- Header insert SQL does not populate fiscal numbering fields.
- Disposable smoke GET returns null fields after POST.
- Disposable smoke fixture can read populated fiscal numbering fields when the fields are present in `pos.fiscal_documents`.

## 11. Boundary Review

This change does not add:

- runtime number allocation;
- sequence state locking;
- sequence/counter mutation;
- idempotency allocation behavior;
- BIR reporting;
- Digital SI;
- Annex E;
- X/Z;
- statutory discount validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution.

`document_context` remains non-authoritative for fiscal numbering.

## 12. Scope Discipline Review

This task did not modify:

- `db/state` SQL files;
- migrations;
- controlled-code JSON;
- generated controlled-code SQL;
- CI workflow files;
- Atlas configuration;
- sample production data.

## 13. Validation Review

Local validation performed:

- `dotnet build`
- `dotnet test`
- `dotnet test .\tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj`
- `powershell -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`
- `git status --short --untracked-files=all`
- `git diff --check`

Results:

- `dotnet build` passed.
- `dotnet test` passed overall. The aggregate run reported 23 persistence tests, 2 API integration tests, and 53 API tests passed; the runtime assembly reported no discovered tests in the aggregate run.
- Direct runtime test project execution passed with 54 tests.
- Static DB validation passed.
- `git diff --check` passed with LF/CRLF warnings only.
- `git status --short --untracked-files=all` showed only runtime/read model, reader, tests, and documentation changes.

Darwin completed the credentialed disposable PostgreSQL smoke run:

```powershell
$env:POSSERVER_API_SMOKE_DB_URL = "Host=localhost;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<redacted>"
dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj
```

Disposable smoke results:

- Database used: `posserver_api_smoke_validation_local`.
- The database was dropped and recreated through `docker exec exitpass-postgres psql -U exitpass -d template1`.
- `dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj` passed.
- `API.IntegrationTests` passed with 2 tests.
- The smoke path POSTed a fiscal document and GET read it back.
- Fiscal numbering fields were present/read-safe as nullable fields because allocation is not implemented yet.
- Missing fiscal document ID returned 404.
- Forced late failure still returned 503 and verified rollback/no partial commit.
- Generated/disposable DB state was not committed.
- Password and connection string secret remain redacted.

No `db/state`, migrations, controlled-code JSON, generated SQL, CI, Atlas config, sample production data, runtime allocation, sequence/counter mutation, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, refund/reversal authority, or gate/exit behavior was added.

## 14. Final Recommendation

Proceed to review. Do not proceed to runtime fiscal number allocation until a separate approved allocation slice defines sequence locking, idempotency interaction, counter mutation, and transaction behavior.
