# ExitPass POS Server DB Validation / Rebuild Checklist

## 1. Purpose

This checklist defines future evidence expected for POS Server database artifact pull requests. It is a planning artifact only and does not create validation scripts.

## 2. Future DB Artifact PR Checklist

| Check | Required evidence |
| --- | --- |
| SQL inventory matches approved scope | List of SQL files added/modified/deleted and expected object coverage. |
| No prohibited object families | Scan result proving no unauthorized fiscal issuance, SI, numbering/counter, idempotency, Digital SI URL, report/export, audit, recovery, or outbox objects. |
| No unauthorized functions/triggers/extensions | Scan result proving no unapproved routines, triggers, extensions, sequences, or types. |
| No Atlas/migration files unless explicitly approved | Status scan showing no unapproved Atlas or migration files. |
| No seed/reference/sample data unless explicitly approved | Status scan showing no unapproved data artifacts. |
| No source changes unless explicitly approved | Status scan showing no app/source modifications. |
| Naming compliance | Result for `pos`, lowercase `snake_case`, `_id`, `_ref`, Central PMS reference naming, and identifier length. |
| Authority-boundary compliance | Result proving no POS-owned payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, ExitAuthorization, gate authority, independent terminal fiscal authority, or offline issuance approval. |
| Object existence | Schema/table inventory proving expected objects exist after rebuild. |
| Constraint/index inventory | Inventory of PK/FK/UQ/CK constraints and indexes, including unexpected/missing items. |
| Controlled-code posture | Confirmation that code structures exist and values are not loaded unless approved. |
| Clean rebuild result | Pass/fail result from disposable database rebuild. |
| SQL smoke check | Pass/fail or explicit skip reason when PostgreSQL client is unavailable. |
| Drift-check result | Missing/unexpected/changed object report; no auto-promotion. |
| PR evidence attached | Evidence files or pasted summaries included in PR description. |

## 3. Current First-Slice Expected Inventory

Expected schema:

- `pos`

Expected tables:

- `pos.controlled_code_sets`
- `pos.controlled_codes`
- `pos.site_pos_servers`
- `pos.fiscal_identities`
- `pos.site_pos_server_fiscal_identity_history`
- `pos.channel_terminals`
- `pos.channel_terminal_capabilities`
- `pos.channel_terminal_status_history`

## 4. Current First-Slice Prohibited Families

Until separately approved, future validation should fail if a PR creates:

- fiscal document or Sales Invoice objects;
- fiscal lines, tenders, taxes, discounts, totals;
- numbering, counters, GTA;
- idempotency;
- Digital SI URL/token/access;
- reprints or adjustments;
- reports, exports, EJ, POSLog;
- audit;
- recovery or anchoring;
- outbox/events.

## 5. Review Sign-Off Questions

- Does the PR preserve repository state as source of truth?
- Does the PR avoid promoting local drift?
- Does the PR preserve Central PMS authority?
- Does the PR keep POS Server fiscal records separate from payment and exit authority?
- Does the PR include enough evidence for another reviewer to reproduce or inspect the result?

