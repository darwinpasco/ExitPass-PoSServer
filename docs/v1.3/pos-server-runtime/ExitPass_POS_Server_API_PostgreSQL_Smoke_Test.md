# ExitPass POS Server API PostgreSQL Smoke Test

## Purpose

This smoke test proves that `POST /v1/fiscal-documents` can write a complete fiscal document persistence shell into a real disposable PostgreSQL database.

The smoke test is intentionally not BIR reporting readiness. It validates API-to-PostgreSQL persistence for the current shell only.

## Test Project

Project:

```text
tests/ExitPass.PosServer.Api.IntegrationTests/
```

Primary test file:

```text
tests/ExitPass.PosServer.Api.IntegrationTests/FiscalDocumentApiPostgresSmokeTests.cs
```

The test project is gated by:

```text
POSSERVER_API_SMOKE_DB_URL
```

If the environment variable is not set, the tests return without connecting to PostgreSQL. This keeps normal local and CI `dotnet test` runs safe by default.

## Disposable Database Requirement

The connection string must target a disposable local smoke validation database. The test rejects database names that do not look local/smoke/validation-oriented, and it rejects URL-style PostgreSQL connection strings because the API currently accepts Npgsql key-value connection strings.

Recommended database name:

```text
posserver_api_smoke_validation_local
```

Example environment variable:

```powershell
$env:POSSERVER_API_SMOKE_DB_URL = "Host=host.docker.internal;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<PASSWORD>"
```

If using the existing local Docker PostgreSQL container:

```powershell
docker exec exitpass-postgres psql -U exitpass -d template1 -c "drop database if exists posserver_api_smoke_validation_local;"
docker exec exitpass-postgres psql -U exitpass -d template1 -c "create database posserver_api_smoke_validation_local;"
```

## How To Run

Run only the smoke project:

```powershell
dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj
```

Run the full suite:

```powershell
dotnet test
```

## Schema Rebuild

When `POSSERVER_API_SMOKE_DB_URL` is set, the smoke test:

1. connects to the disposable database;
2. drops `pos` schema with `cascade`;
3. applies `db/rebuild/pos_sql_apply_order.txt` in manifest order;
4. applies generated controlled-code SQL files from `db/reference-data/controlled-codes/generated/sql/` in deterministic filename order.

The test does not modify `db/state`, generated SQL, or source JSON.

## Controlled-Code Fixture Posture

The current approved controlled-code baseline intentionally does not include BIR/accounting-sensitive fiscal document, tender, tax, discount/privilege, or total type families.

To satisfy existing foreign keys in a disposable database, the smoke test inserts isolated smoke-only controlled-code rows directly into the disposable database after loading the approved baseline. These fixture rows are not source JSON, are not generated SQL, and are not reference-data approval.

## API Persistence Configuration

The smoke test starts the API in-process with:

```text
ConnectionStrings:PosServer = $env:POSSERVER_API_SMOKE_DB_URL
```

It then posts to:

```text
POST /v1/fiscal-documents/
```

## Valid Request Shape

The smoke request includes:

- local fiscal schema context;
- upstream payable-basis and finality references;
- one fiscal line;
- one fiscal tender;
- one fiscal tax detail;
- one fiscal discount/privilege detail;
- one fiscal total.

The request does not include raw evidence, entitlement approval fields, local ordinance decision fields, payment finality ownership fields, refund/reversal authority fields, gate/exit fields, BIR report fields, Digital SI fields, Annex E fields, or X/Z fields.

## Row Verification

After a successful API call, the smoke test verifies:

- one row in `pos.fiscal_documents`;
- one row in `pos.fiscal_document_status_history`;
- zero rows in `pos.fiscal_document_links` for the current no-link request;
- one row in `pos.fiscal_document_lines`;
- one row in `pos.fiscal_tenders`;
- one row in `pos.fiscal_tax_details`;
- one row in `pos.fiscal_discount_privilege_details`;
- one row in `pos.fiscal_totals`;
- upstream finality and evidence values are persisted as references only;
- raw evidence and payment payload markers are not stored;
- report, Digital SI, Annex E, X/Z, exit, and gate tables are not populated by the smoke test.

## Rollback Posture

The smoke test also posts a request that passes runtime validation but uses an invalid total type foreign key. This forces a late persistence failure at the totals insert. The test verifies the failed request leaves no partial fiscal document, line, tender, or discount/privilege rows behind.

## Unsupported By This Smoke Test

This smoke test does not implement or validate:

- BIR reports;
- Digital SI;
- Annex E;
- X/Z reports;
- statutory discount entitlement validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution;
- production deployment configuration.

Manual API smoke testing is still required before commit approval.
