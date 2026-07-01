# ExitPass POS Server Fiscal Document Read Endpoint

## Decision Summary

POS Server now exposes a small read-only endpoint for inspecting a persisted fiscal document shell:

```text
GET /v1/fiscal-documents/{fiscalDocumentId}
```

The endpoint reads existing persistence rows only. It does not mutate database state, allocate numbers, create fiscal records, generate reports, issue Digital SI, perform X/Z or Annex E behavior, validate statutory discount entitlement, own payment finality, or create gate/exit authority.

Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.

## Tables Queried

The PostgreSQL reader maps the existing fiscal document shell from:

1. `pos.fiscal_documents`
2. `pos.fiscal_document_status_history`
3. `pos.fiscal_document_links`
4. `pos.fiscal_document_lines`
5. `pos.fiscal_tenders`
6. `pos.fiscal_tax_details`
7. `pos.fiscal_discount_privilege_details`
8. `pos.fiscal_totals`

No report, Digital SI, Annex E, X/Z, audit, exit, or gate tables are queried by this endpoint.

## Response Shape

A successful response returns:

- fiscal document header;
- status history rows;
- document links;
- fiscal lines;
- fiscal tenders;
- fiscal tax details;
- fiscal discount/privilege details;
- fiscal totals.

The response preserves upstream values as references for traceability. Tender references do not mean POS Server owns payment finality. Discount/privilege references do not mean POS Server approved entitlement or local ordinance eligibility. Total rows do not mean BIR, X/Z, Annex E, or tax return finality.

## Fail-Closed Behavior

The endpoint returns deterministic responses:

- `200` with code `found` when the document exists;
- `404` with code `fiscal_document_not_found` when the ID is not present;
- `503` with code `persistence_not_configured` when no persistence connection is configured;
- `503` with code `invalid_persistence_configuration` when the configured connection string is invalid;
- `503` with code `persistence_read_failed` for read failures.

Connection strings and passwords are not returned in API responses.

## Read-Only Safety

The PostgreSQL implementation uses parameterized `select` commands with `@fiscal_document_id`. It does not issue `insert`, `update`, `delete`, `truncate`, transaction commit, or rollback commands.

The reader also filters sensitive text markers from string/context values before returning the read model. Raw evidence payloads, credentials, tokens, provider callback payloads, raw payment payloads, and secrets must not be exposed through this inspection endpoint.

## Manual API Test

The existing disposable PostgreSQL API smoke test now exercises the read endpoint after a successful fiscal document POST:

```powershell
$env:POSSERVER_API_SMOKE_DB_URL = "Host=localhost;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<redacted>"
dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj
```

The test remains gated by `POSSERVER_API_SMOKE_DB_URL` and must target a disposable local database only.

## Intentionally Unsupported

This slice does not implement:

- BIR reports;
- Digital SI;
- Annex E;
- X/Z reports;
- statutory discount entitlement validation;
- local ordinance eligibility decisions;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution;
- numbering/counter allocation;
- production deployment configuration.
