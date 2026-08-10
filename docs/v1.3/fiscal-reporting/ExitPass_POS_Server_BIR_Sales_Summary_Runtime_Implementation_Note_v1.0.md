# ExitPass POS Server BIR Sales Summary Runtime Implementation Note v1.0

## Purpose

Z-010 implements the authoritative internal BIR sales-summary dataset. It is generated only by POS Server from one committed Z Reading and one CLOSED fiscal period. It is a reusable fiscal-reporting source; it is not an Annex E E-1 file, layout, submission, or external compliance decision.

## Runtime and identity

The runtime profile is `pos-server-bir-sales-summary:v1`. The generation command contains operation identity and governing scope only; callers cannot supply fiscal totals. The server resolves the committed Z, CLOSED period, immutable counter snapshot, Z children, canonical fiscal documents, and historically effective approved Sales Invoice header profile.

One immutable `pos.bir_sales_summary_reports` row is allowed per governing Z and runtime profile. The existing fiscal-report request and scope objects own operation idempotency. Exact replay returns the committed summary. A reused operation key with changed governed semantics conflicts. A competing request for the same Z/profile returns the one committed summary and cannot create a duplicate.

The database schema already supplied the summary table, composite governing-Z foreign key, uniqueness constraints, and UPDATE/DELETE rejection trigger. Z-010 adds runtime only; it does not add or alter canonical database objects.

## Reconciliation

Generation re-runs the merged `FiscalXReadingAggregationService` over the canonical fiscal documents assigned to the governing period. Before persistence it compares the result exactly with the committed Z:

- qualifying fiscal-document count and Sales Invoice boundaries;
- gross and net sales;
- VATable, VAT, VAT-exempt, and zero-rated amounts;
- total, Senior Citizen, PWD, other statutory, VAT-exemption, coupon, and promotional discounts;
- void, refund, return, adjustment, and service-charge categories;
- tender classifications, counts, amounts, and currency;
- fiscal-sequence policy/series ranges, document counts, and classified gaps;
- reset-counter and Z-counter values and transition;
- previous, current-period, and resulting grand totals.

Any mismatch fails before request, scope, summary, or audit persistence. No partially reconciled summary is readable.

## Transaction and recovery

Generation takes a transaction-scoped advisory lock derived from the governing Z and commits request, scope, summary, and audit in one PostgreSQL transaction. Unique-key races reconcile against durable state. A failure before commit rolls back all summary objects. An uncertain failure resolves the operation and governing Z/profile identity read-only before deciding replay versus unavailable.

The summary references Z-owned immutable tender, discount, fiscal-range, and gap facts on readback. Restart does not change summary identity or values. JSON and CSV render from committed facts only and are byte deterministic for one summary.

## API and authorization

- `POST /v1/fiscal-reports/bir-sales-summaries/` requires `bir_sales_summary.generate`.
- `GET /v1/fiscal-reports/bir-sales-summaries/{summaryId}` requires `bir_sales_summary.read`.
- `GET /v1/fiscal-reports/bir-sales-summaries/{summaryId}/exports/{format}` requires `bir_sales_summary.export`.

Every route enforces Site POS Server, fiscal identity, and currency scope. There is no implicit GLOBAL authority. API-key registrations carry an explicit authority class; `FIXTURE` and `DEVELOPMENT` authority are rejected when the host is `Production`. Missing or wrong scope is denied without exposing another scope's report. Export completion is logged with a shortened summary reference, format, content hash, and safe correlation reference; credentials and authorization headers are excluded.

## Export boundary

The supported internal controlled exports are deterministic JSON and CSV. They include the complete internal summary and committed-Z reconciliation dimensions. They must not be labeled or submitted as Annex E. XLSX, PDF, XML, fixed-width, POSLog, and other formats are rejected by this runtime.

## Validation

Focused unit and API tests cover request validation, semantic identity, deterministic bytes, distinct permissions, scope denial, safe errors, and Production fixture rejection. The PostgreSQL 16 proof rebuilds the disposable database from `db/rebuild/pos_sql_apply_order.txt` and generated controlled-code SQL, closes an actual Z, and covers generation, readback, exports, concurrent replay, conflict, OPEN/absent-Z rejection, exact mismatch rejection, forced rollback and retry, restart determinism, immutability, audit evidence, and before/after no-mutation manifests.

## Gap and residual-risk register

| Gap | Classification | Owner |
| --- | --- | --- |
| Annex E E-1 fields, formulas, layout, filename, submission, and external format | Blocked by the 14 recorded external confirmations; not inferred by Z-010 | Annex E compliance/runtime follow-on |
| Cross-period refund, return, adjustment, and late-mutation policy | Outside the merged Z-close contract; current aggregation fails closed | POS Server fiscal-contract follow-on |
| Staff-facing reporting UI | Not required for authoritative API/runtime | Staff reporting consumer task |
| Physical printer/spooler integration | Explicitly out of scope | Printing/UAT task |
| Central PMS reporting aggregation | POS Server remains source authority; no aggregation added | Central PMS reporting task |
| Controlled UAT and production rollout | Not authorized | Release/UAT governance |

No Z-010 runtime blocker remains when the automated and disposable PostgreSQL validation passes. Annex E runtime remains independently unauthorized.
