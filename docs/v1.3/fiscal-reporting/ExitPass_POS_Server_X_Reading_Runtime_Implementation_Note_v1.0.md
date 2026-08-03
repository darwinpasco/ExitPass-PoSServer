# ExitPass POS Server X Reading Runtime Implementation Note v1.0

## 1. Decision

Z-006B implements the first executable X Reading as a bounded API, application, aggregation, and PostgreSQL persistence slice. It uses `pos-server-fiscal-reporting:v1` and `pos-server-fiscal-report-request:sha256:v1` without introducing a parallel report model.

The earlier timestamp blocker is retained in `ExitPass_POS_Server_X_Reading_Runtime_Blocker_v1.0.md`. Z-006B1 resolved it by enforcing `generated_at >= period_start_at` for X and retaining `generated_at >= period_end_at` for Z.

## 2. API and Authorization

| Operation | Route | Policy | Server-derived permission |
|---|---|---|---|
| Generate | `POST /v1/fiscal-reports/x-readings/` | `FiscalXReadingGeneration` | `fiscal_x_reading.generate` |
| Read | `GET /v1/fiscal-reports/x-readings/{fiscalReportReference}` | `FiscalXReadingRead` | `fiscal_x_reading.read` |

The `PosServerAdminApiKey` handler derives permissions, Site POS Server scopes, and fiscal-identity scopes from server configuration. Request headers cannot grant authority. A valid, bounded `X-Correlation-Id` is required but does not participate in semantic identity.

## 3. Period and Observation Semantics

Generation resolves exactly one `OPEN` period for the requested Site POS Server and fiscal identity. The period must carry the active reporting contract, configured timezone and cutoff snapshots, and one currency. Missing or ambiguous scope fails closed. The source interval is `[period_start_at, min(observed_at, period_end_at))`.

The repository uses a PostgreSQL `REPEATABLE READ` transaction. Aggregation and immutable persistence therefore observe one committed database snapshot. A concurrent fiscal transaction is wholly before or wholly after that snapshot; partial header/child state is not observable.

## 4. Inclusion and Aggregation Matrix

| Source posture | X treatment |
|---|---|
| Recorded Sales Invoice | Included |
| Voided Sales Invoice | Excluded from sales; recorded in void total |
| Failed, rejected, requested, processing, in-progress, uncertain, or unknown-commit operation | Excluded |
| Reprint or Digital SI read | No fiscal document is created; not counted |
| Refund, return, or adjustment document type | Fails closed until governed source types exist |
| Unsupported document, tender, tax, line, discount, statutory, or gap classification | Fails closed |
| Commit at period start | Included when committed before the database snapshot |
| Commit at period end | Excluded by `[start,end)` |

Document totals, lines, taxes, tenders, discounts, applied statutory facts, and assigned fiscal sequence facts each have one purpose. Integer minor-unit arithmetic is checked. Mixed currency and reconciliation failures are rejected. Senior Citizen, PWD, VAT exemption, coupon, promotional, and other statutory categories remain separate.

## 5. Identity, Replay, and Conflict

The semantic source includes the operation key, report kind, Site POS Server, fiscal identity, period identity, business date, exact period bounds, timezone and cutoff snapshots, currency, contract version, and observation time. It excludes correlation and transport diagnostics.

The repository takes a bounded advisory lock for one Site POS Server/operation identity. Same key and same semantics return the stored snapshot and insert nothing. Same key and changed semantics return a safe conflict without exposing hashes or field differences.

## 6. Immutable Persistence and Readback

One transaction persists the report request, scope, X snapshot, tender rows, discount/statutory rows, fiscal-number ranges, governed gap rows, operation result, and privacy-safe audit evidence. Readback loads these committed rows and never reaggregates source documents.

The expected mutation surface is limited to X reporting objects and fiscal action audit evidence. Tests prove no change to fiscal documents, status history, fiscal numbering, sequence state, counter state, fiscal state snapshots, reporting-period lifecycle, Z snapshots, Z counter/GTA rows, or reprints.

## 7. Validation Evidence

Automated coverage includes request/hash behavior, scope authorization, aggregation and classification failures, exact replay, semantic conflict, PostgreSQL rollback, immutable-row enforcement, concurrent-issuance consistency, stored readback equality, and the no-state-mutation manifest. The disposable proof uses PostgreSQL 16 and synthetic non-personal fixtures.

## 8. Security and Privacy

The API returns safe classifications without SQL, constraint names, hashes, connection strings, stack traces, or internal paths. No beneficiary identity, statutory ID, evidence, reviewer identity, payment credential, or generic report JSON is accepted or persisted.

## 9. Readiness and Exclusions

- X Reading runtime and JSON readback: implemented by Z-006B.
- X Reading rendering, printing, reprinting, and export: not implemented.
- Z Reading runtime and fiscal-period close: not implemented or authorized.
- BIR, Annex E, EJ, and POSLog generation: not implemented.
- Controlled UAT: not authorized.
- Production rollout: not authorized.
