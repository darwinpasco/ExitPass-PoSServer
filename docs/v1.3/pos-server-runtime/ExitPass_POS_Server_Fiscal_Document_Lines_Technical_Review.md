# ExitPass POS Server Fiscal Document Lines Technical Review

## Review Summary

Fiscal document creation now persists fiscal line rows to `pos.fiscal_document_lines` in the same PostgreSQL transaction as the fiscal document header, initial status history, and supported fiscal document links.

Finding counts: P0 0 / P1 0 / P2 0 / Editorial 0.

## Scope Review

The change is limited to runtime/API line input mapping, PostgreSQL persistence wiring, tests, and runtime documentation.

No `db/state` SQL changed. No migrations, controlled-code JSON changes, generated SQL changes, CI changes, Atlas config, or sample transaction data were added.

Manual API testing also found that URL-style PostgreSQL values in `POSSERVER_DB_URL` could crash service resolution because Npgsql expects key-value connection strings. The API now fails closed with deterministic `invalid_persistence_configuration` behavior for URL-style or malformed persistence connection strings, without exposing the connection string or password. Clearing `POSSERVER_DB_URL` still uses the deterministic persistence-not-configured path, and invalid fiscal document requests still return HTTP 400 validation failures before persistence is touched.

## Persistence Review

Writes are limited to existing POS schema tables:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`
- `pos.fiscal_document_lines`

The repository inserts header, status history, links, and lines inside one PostgreSQL transaction. Commit occurs only after all inserts succeed; failures roll back the full transaction.

## Line Mapping Review

Line input maps to schema-backed columns only:

- `line_sequence`
- `line_type_code_id`
- `line_status_code_id`
- `description`
- `quantity`
- `unit_amount_minor_units`
- `gross_amount_minor_units`
- `discount_amount_minor_units`
- `tax_amount_minor_units`
- `net_amount_minor_units`
- `currency_code`
- `source_ref`
- `line_context`

The mapping does not add tender, tax-detail, discount-detail, total, report, Digital SI, exit, gate, or payment lifecycle fields.

## Validation Review

Runtime validation requires at least one fiscal line and rejects duplicate line sequences, missing required fields, nonpositive quantity, negative amounts, inconsistent net amount, invalid currency, blank optional references, and raw evidence markers before persistence.

The amount consistency check is local arithmetic posture only. It does not encode final BIR/accounting report formulas.

## SQL Safety Review

The line insert uses an explicit column list and parameterized Npgsql commands. Untrusted request values are not interpolated into SQL.

## Boundary Review

No tenders, tax details, discount/privilege details, totals, BIR reporting, Digital SI, Annex E, X/Z behavior, statutory discount validation, payment finality ownership, exit authorization, or gate behavior were added.

The locked boundary remains: Operator Console validates; Central PMS/Discount Service authorizes payable-basis effect; POS Server fiscalizes.

## Test Review

Tests cover:

- valid fiscal document line input reaching the creation path
- API DTO mapping for fiscal lines
- no persistence connection string mapping to fail-closed persistence-not-configured behavior
- invalid and URL-style persistence connection strings mapping to deterministic `invalid_persistence_configuration` behavior
- valid Npgsql key-value connection strings still wiring `PostgresFiscalDocumentRepository`
- invalid fiscal document requests still returning HTTP 400 before persistence
- line-required behavior
- duplicate line sequence rejection
- invalid quantity, amount, description, and line type rejection
- raw evidence markers rejected before persistence
- line SQL targeting `pos.fiscal_document_lines`
- header, status history, links, and lines in one transaction
- parameterized SQL posture
- authority-boundary guardrails

Real PostgreSQL integration testing was not added in this slice; adapter tests inspect SQL mapping and transaction behavior without requiring developer secrets or a disposable database.

## Validation Results

Local validation completed:

- `dotnet build`: blocked in the normal output path because a running manual-test `ExitPass.PosServer.Api` process locked `src/ExitPass.PosServer.Api/bin/Debug/net8.0/ExitPass.PosServer.Api.exe`
- `dotnet build -p:OutputPath=D:\SourceCodes\ExitPass\.tmp-posserver-build\bin\`: passed with 0 warnings and 0 errors
- `dotnet test -p:OutputPath=D:\SourceCodes\ExitPass\.tmp-posserver-build\bin\`: passed, 53 total tests
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed
- `git diff --check`: passed
- Targeted scope check: no `db/state`, controlled-code JSON, generated SQL, CI, Atlas, or migration changes

## Recommendation

Ready for commit.
