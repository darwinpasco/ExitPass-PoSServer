# ExitPass POS Server API PostgreSQL Smoke Test Technical Review

## Review Summary

Added a gated disposable PostgreSQL API integration smoke test project for fiscal document creation. The harness can rebuild the POS schema from the repository manifest, load generated controlled-code SQL, start the API in-process with a disposable database connection, post one complete fiscal document request, verify rows in the eight persistence tables, and verify rollback for a forced late persistence failure.

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

## Test Project Review

Created:

```text
tests/ExitPass.PosServer.Api.IntegrationTests/
```

Updated:

```text
ExitPass.PoSServer.sln
```

The integration test project is gated by `POSSERVER_API_SMOKE_DB_URL`. Without that variable, it returns safely without connecting to PostgreSQL. With that variable set to an Npgsql key-value connection string for a disposable local database, it performs the full smoke flow.

## Database Safety Review

The test requires a database name that looks local, smoke-oriented, and validation-oriented. It rejects names containing production/shared/live posture. It also rejects URL-style PostgreSQL connection strings because the API DI path currently treats URL-style strings as invalid persistence configuration.

The test drops only the `pos` schema in the connected disposable database. It does not create/drop production or shared databases.

## Rebuild / Baseline Review

The smoke test:

1. applies `db/rebuild/pos_sql_apply_order.txt`;
2. applies generated controlled-code SQL files from `db/reference-data/controlled-codes/generated/sql/` in deterministic filename order;
3. inserts smoke-only disposable controlled-code fixture rows required for fiscal document FK coverage because approved BIR/accounting-sensitive code families are not yet part of source JSON/generated SQL.

These smoke-only fixture rows are not committed source data and are not reference-data approval.

## API Smoke Review

The successful smoke request includes local fiscal schema context, payable-basis/finality references, one line, one tender, one tax detail, one discount/privilege detail, and one total.

The test verifies rows in:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links` with zero rows for the no-link request
- `pos.fiscal_document_lines`
- `pos.fiscal_tenders`
- `pos.fiscal_tax_details`
- `pos.fiscal_discount_privilege_details`
- `pos.fiscal_totals`

It also checks that upstream references are persisted as traceability only and that raw evidence/payment payload markers are not stored.

## Rollback Review

The rollback smoke case uses an invalid total type foreign key to force a late failure after earlier runtime validation passes. The expected response is `persistence_write_failed`, and the test verifies no partial document, line, tender, or discount/privilege rows remain for that failed request.

## Boundary Review

The smoke harness does not add:

- BIR report finality;
- Digital SI issuance;
- Annex E;
- X/Z finality;
- statutory discount entitlement validation;
- entitlement approval;
- local ordinance decisions;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution.

Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.

## Validation Results

- `dotnet build`: passed.
- `dotnet test`: passed; Runtime 52 tests, Persistence.Postgres 17 tests, API 42 tests, API.IntegrationTests 2 tests.
- Codex environment did not have `POSSERVER_API_SMOKE_DB_URL` set, so the integration tests took the safe no-database path during this run.
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed.
- `git diff --check`: passed.
- Scoped status confirmed no `db/state`, reference-data, or CI workflow changes.

## Local Disposable PostgreSQL Result

Darwin ran the smoke test against a local disposable PostgreSQL database:

- database: `posserver_api_smoke_validation_local`
- connection posture: `Host=localhost;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<redacted>`
- database reset: `docker exec exitpass-postgres psql -U exitpass -d template1` was used to drop and recreate the disposable database
- command: `dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj`
- result: passed
- API.IntegrationTests: 2 passed
- successful fiscal document POST returned HTTP 202
- forced late FK failure returned HTTP 503
- build succeeded

The password and connection string secret remain redacted in this document. Generated/disposable database state was not committed.

## Manual API Smoke Requirement

Manual API smoke testing is now satisfied for this slice. The command shape remains:

```powershell
$env:POSSERVER_API_SMOKE_DB_URL = "Host=localhost;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<redacted>"
dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj
```

The target database must be disposable.

## Recommended Next Step

Proceed only to the next approved runtime slice after review. Do not proceed to BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, or production runtime behavior based solely on this smoke test.
