# ExitPass POS Server Fiscal Document Read Endpoint Technical Review

## Review Summary

Added a read-only fiscal document inspection endpoint:

```text
GET /v1/fiscal-documents/{fiscalDocumentId}
```

The endpoint delegates to a runtime read service and PostgreSQL reader. It returns the fiscal document header plus persisted child rows from the current eight-table fiscal document shell.

## Findings

P0 0 / P1 0 / P2 0 / Editorial 0

## Scope Review

- No `db/state` SQL files were modified.
- No migrations were added.
- No controlled-code JSON was modified.
- No generated SQL was modified.
- No Atlas configuration was added.
- No sample production data was added.
- No BIR reporting, Digital SI, Annex E, or X/Z behavior was added.
- No statutory discount validation was added.
- No payment finality ownership was added.
- No refund/reversal authority was added.
- No gate/exit behavior was added.

## Endpoint Review

The API route is:

```text
GET /v1/fiscal-documents/{fiscalDocumentId}
```

Behavior:

- existing document returns `200` with code `found`;
- missing document returns `404` with code `fiscal_document_not_found`;
- missing persistence configuration returns `503` with code `persistence_not_configured`;
- invalid persistence configuration returns `503` with code `invalid_persistence_configuration`;
- read failure returns `503` with code `persistence_read_failed`.

The endpoint is read-only and delegates to `FiscalDocumentReadService`.

## PostgreSQL Reader Review

`PostgresFiscalDocumentReader` queries only:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`
- `pos.fiscal_document_lines`
- `pos.fiscal_tenders`
- `pos.fiscal_tax_details`
- `pos.fiscal_discount_privilege_details`
- `pos.fiscal_totals`

The reader uses parameterized SQL with `@fiscal_document_id`. It does not issue writes or transaction commits. Child rows are ordered deterministically.

## Boundary Review

The read model preserves reference-only posture:

- tender/payment references are traceability only and do not establish POS-owned payment finality;
- discount/privilege references are fiscal representation only and do not establish entitlement approval or local ordinance eligibility;
- fiscal totals are summary rows only and do not establish BIR report, X/Z, Annex E, or tax return finality.

Sensitive marker filtering is present for raw evidence, credentials, tokens, provider callback payloads, raw payment payloads, and secrets.

## Test Review

Added or updated tests for:

- runtime read service success and not-found behavior;
- API response mapping for found, not found, persistence-not-configured, invalid persistence configuration, and read failure;
- DI reader selection for no configuration, invalid configuration, URL-style PostgreSQL strings, and valid Npgsql key-value connection strings;
- PostgreSQL reader source posture: parameterized, read-only, eight-table scope, deterministic ordering, and sensitive marker filtering;
- disposable API smoke path now POSTs a document, GETs it back, verifies child rows in the read model, and verifies missing ID returns 404.

## Validation Results

- `dotnet build`: passed.
- `dotnet test`: passed; Runtime 54 tests, Persistence.Postgres 21 tests, API 51 tests, API.IntegrationTests 2 tests.
- API integration tests remain gated by `POSSERVER_API_SMOKE_DB_URL`; without it, they take the safe no-database path.
- `powershell -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed.
- `git diff --check`: passed.

## Local Disposable PostgreSQL Smoke Result

Darwin ran the gated API integration smoke test against a local disposable PostgreSQL database:

- database: `posserver_api_smoke_validation_local`;
- connection posture: `Host=localhost;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<redacted>`;
- database reset: `docker exec exitpass-postgres psql -U exitpass -d template1` was used to drop and recreate the disposable database;
- command: `dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj`;
- result: passed;
- API.IntegrationTests: 2 passed;
- smoke path posted a fiscal document and read it back through `GET /v1/fiscal-documents/{fiscalDocumentId}`;
- the returned read model included rows from the current eight-table fiscal persistence shell;
- missing fiscal document ID returned `404`.

Command shape:

```powershell
$env:POSSERVER_API_SMOKE_DB_URL = "Host=localhost;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<redacted>"
dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj
```

The password and connection string secret remain redacted. Generated/disposable database state was not committed.

This smoke result did not add or modify `db/state`, migrations, controlled-code JSON, generated SQL, Atlas configuration, sample production data, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, refund/reversal authority, or gate/exit behavior.

## Recommended Next Step

Proceed only to the next approved runtime slice after review. Do not proceed to BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, numbering/counter allocation, or production runtime behavior from this read endpoint alone.
