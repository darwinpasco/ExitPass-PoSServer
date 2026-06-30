# ExitPass POS Server Fiscal Document Links Technical Review

## Review Summary

The fiscal document persistence adapter was extended to write supported document-to-document link rows to `pos.fiscal_document_links` in the same transaction as the fiscal document header and initial status history row.

Finding counts: P0 0 / P1 0 / P2 0 / Editorial 0.

## Scope Review

The change is limited to runtime/API mapping, PostgreSQL persistence wiring, tests, and runtime documentation.

No `db/state` SQL changed. No migrations, controlled-code JSON changes, generated SQL changes, CI changes, Atlas config, or sample transaction data were added.

## Persistence Review

Writes are limited to existing POS schema tables:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`

The repository inserts header, status history, and link rows inside one PostgreSQL transaction. Commit occurs only after all inserts succeed; failures roll back the full transaction.

## Link Mapping Review

`pos.fiscal_document_links` is document-to-document only. The runtime therefore accepts supported fiscal document link inputs with target fiscal document ID and link type controlled-code ID. External upstream references remain in existing header/context traceability fields instead of being misrepresented as document links.

Optional statutory discount validation references remain traceability only and do not create discount approval, entitlement validation, or payable-basis authority in POS Server.

## SQL Safety Review

The new link insert uses an explicit column list and parameterized Npgsql commands. Untrusted request values are not interpolated into SQL.

## Boundary Review

No fiscal lines, tenders, tax details, discount details, totals, reports, Digital SI, Annex E, X/Z behavior, statutory discount validation, payment finality ownership, exit authorization, or gate behavior were added.

The locked boundary remains: Operator Console validates; Central PMS/Discount Service authorizes payable-basis effect; POS Server fiscalizes.

## Test Review

Tests cover:

- link DTO/model mapping into the fiscal document creation path
- malformed link rejection before persistence
- raw evidence markers rejected before persistence
- link SQL targeting only `pos.fiscal_document_links`
- header, status history, and links in one transaction
- parameterized SQL posture
- authority-boundary guardrails

Real PostgreSQL integration testing was not added in this slice; adapter tests inspect SQL mapping and transaction behavior without requiring developer secrets or a disposable database.

## Validation Results

Local validation completed:

- `dotnet build`: passed with 0 warnings and 0 errors
- `dotnet test`: passed, 38 total tests
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed
- `git diff --check`: passed
- Targeted scope check: no `db/state`, controlled-code JSON, generated SQL, CI, Atlas, or migration changes

## Recommendation

Ready for commit.
