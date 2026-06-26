# ExitPass POS Server Physical Object Design Source Analysis

## 1. Purpose

This source analysis prepares for future POS Server physical object design. It translates the approved logical Database Design, Physical DB Artifact Plan, Gate Resolution, and Schema/Naming Standards into planning inputs only.

This document does not create SQL, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, or diagrams.

## 2. Sources Reviewed

| Source | Relevance |
| --- | --- |
| POS/Invoicing BRD v1.0 | Business and fiscal scope, BIR fiscal outputs, reprints, reports, authority boundaries. |
| POS Server System Design v1.0 | POS Server fiscal authority, channel/terminal model, recovery, audit, reports, Digital SI URL. |
| POS Server API Contract v1.0 | API families, status concepts, idempotency, Digital SI URL, QR responsibility, reprint/report/export semantics. |
| POS Server Database Design v1.0 | Approved logical data areas and physical design gate. |
| Physical DB Artifact Plan v1.0 | State-based artifact posture, future `db/` layout, validation/rebuild/drift expectations. |
| Physical DB Gate Resolution v1.0 | Gate classification, placeholder policies, readiness posture. |
| Schema/Naming Standards v1.0 | `pos` schema posture, lowercase `snake_case`, `_id`, `_ref`, authority-boundary naming safeguards. |
| Gate readiness/open questions/impact map | Pending confirmations and physical artifact blockers. |
| `db/README.md` | Bootstrap boundary and prohibited content. |

## 3. Approved Baseline Inputs

| Area | Baseline input for object design planning |
| --- | --- |
| Repository boundary | POS Server database artifacts stay inside `ExitPass-PoSServer`; `db/` is a folder boundary. |
| Schema posture | Use primary PostgreSQL schema posture `pos`; additional schemas require approval. |
| Naming posture | Lowercase `snake_case`; no quoted identifiers; `_id` for internal identifiers; `_ref` for external authority references. |
| Authority model | Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. |
| Fiscal ownership | POS Server owns fiscal issuance and fiscal records only. |
| Artifact posture | Future state-based artifacts are repository-owned source of truth; no local drift promotion. |

## 4. Design Areas To Plan

- Foundation and controlled codes.
- Fiscal identity and Site POS Server boundary.
- Channel/terminal registry.
- Fiscal document core.
- Fiscal lines, tenders, tax, discount, and totals.
- Numbering and counter state.
- Idempotency and retry.
- Digital SI URL and access audit.
- Reprints and fiscal adjustments.
- X-read, Z-read, BIR Sales Summary, and Annex E.
- EJ, POSLog, JSON, and exports.
- Audit trail.
- Recovery and tamper-evident continuity.
- Security/privacy references.
- Optional events/outbox if approved later.
- Integration references.

## 5. Gate Inputs

| Gate item | Planning effect |
| --- | --- |
| PostgreSQL details | Physical object design may use PostgreSQL concepts but must not create SQL until version/features/extensions/hosting are confirmed. |
| Schema/domain decomposition | Use `pos` as primary schema posture; object groups remain provisional. |
| Naming standards | All candidate names are examples only and must follow approved naming safeguards. |
| Idempotency uniqueness | Plan candidate uniqueness areas but defer final constraints/indexes. |
| Fiscal numbering/counters | Use placeholder policy; defer final production sequence implementation. |
| Retention/partitioning | Identify likely high-volume areas; defer final periods and partition keys. |
| Digital SI URL security | Plan lifecycle and access-audit areas; defer token/auth/expiry details. |
| BIR/accreditation outputs | Plan report/export support; defer exact layouts/package. |
| ARTS POSLog mapping | Plan schema/profile/version references; defer final mapping. |
| Tamper-evident anchoring | Plan continuity and hash/anchor areas; defer final mechanism. |

## 6. Out of Scope

This planning package does not decide final physical tables, columns, constraints, indexes, enum implementation, SQL DDL, migration approach, Atlas state, scripts, seed/reference/sample data, CI workflows, or implementation code.
