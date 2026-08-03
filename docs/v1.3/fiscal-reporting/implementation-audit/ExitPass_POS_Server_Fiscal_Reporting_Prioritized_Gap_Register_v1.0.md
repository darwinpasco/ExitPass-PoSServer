# ExitPass POS Server Fiscal Reporting Prioritized Gap Register v1.0

## 1. Summary

| Priority | Count | Meaning |
| --- | ---: | --- |
| P0 | 4 | Fiscal, financial, sequence, or irreversible-close risk |
| P1 | 11 | Correctness, compliance, authority, or security gap |
| P2 | 9 | Operational, reconciliation, recovery, or API gap |
| P3 | 4 | Rendering, usability, observability, or documentation gap |
| Total | 28 | All are unimplemented or incomplete on current `origin/dev`. |

## 2. P0 Gaps

| ID | Evidence/current behavior | Required behavior and impact | Dependency/owner/repository/branch | Classification/order |
| --- | --- | --- | --- | --- |
| FR-P0-01 | No Z-close service, unique close identity, transaction, lock, or replay. | Atomic single close; otherwise duplicate closes, amounts, counters, or fiscal ranges are possible. | Aggregation and period contract; Codex Z; POS Server; `feature/fiscal-z-reading-period-close-runtime` | Runtime/schema/API/test/UAT; 3 |
| FR-P0-02 | No approved source/status/sign/rounding/currency rules; multiple amount sources exist. | Freeze one authoritative aggregation model; otherwise financial and statutory totals can be wrong or double counted. | Compliance/tax decisions; Codex Z plus fiscal/compliance owner; POS Server; `feature/fiscal-reporting-contract-and-schema-hardening` | Contract/schema/test; 1 |
| FR-P0-03 | X/Z snapshots allow duplicate rows and direct monetary updates; no immutable close ledger. | Database-enforced identity, controlled families, immutability, and retention; otherwise closed fiscal facts can change. | FR-P0-02; Codex Z; POS Server; `feature/fiscal-reporting-contract-and-schema-hardening` | Schema/DB test; 1 |
| FR-P0-04 | Counter/GTA/state tables are posture only; no atomic reset/Z transition or recovery. | Expected-state transition committed with Z snapshot and recoverable after lost response; otherwise irreversible state can diverge. | FR-P0-01 and frozen GTA model; Codex Z; POS Server; `feature/fiscal-z-reading-period-close-runtime` | Runtime/schema/test/UAT; 3 |

## 3. P1 Gaps

| ID | Evidence/current behavior | Required behavior and impact | Dependency/owner/repository/branch | Classification/order |
| --- | --- | --- | --- | --- |
| FR-P1-01 | Only report/export status code families are seeded; generic FKs accept wrong-family codes. | Govern report kind/scope/output/Annex/EJ/export and ordinary fiscal classification families with family enforcement. | Contract decision; Codex Z; POS Server; contract/schema hardening branch | Schema/test; 1 |
| FR-P1-02 | Business date and periods are nullable; no timezone/cutoff or late-document policy. | Deterministic Site POS Server business clock and `[start,end)` rule. | Site configuration/compliance; Codex Z; POS Server; contract/schema hardening branch | Contract/config/schema/test; 1 |
| FR-P1-03 | X/Z table lacks required VAT/statutory/tender/count/range/report identity facts. | First-class immutable snapshot sufficient for readback and reconciliation. | FR-P0-02; Codex Z; POS Server; contract/schema hardening branch | Schema/test; 1 |
| FR-P1-04 | BIR summary is mutable, nullable, and not bound to Z. | Immutable versioned summary sourced atomically from Z with exact mappings. | Z contract and compliance layout; Codex Z; POS Server; `feature/fiscal-bir-summary-annex-e-runtime` | Schema/runtime/test/UAT; 4 |
| FR-P1-05 | Annex E form/version, fields, serialization, and privacy posture are not approved. | Freeze exact compliance contract before implementing first-class data/export. | Compliance authority; Codex Z; POS Server; `docs/fiscal-annex-e-contract-freeze` | Contract/compliance/UAT; 4 |
| FR-P1-06 | EJ records are mutable with nullable/non-unique sequence/hash references and no writer. | Immutable chronological event model with integrity verification and retention. | Event/retention contract; Codex Z; POS Server; `feature/fiscal-ej-poslog-export-runtime` | Schema/runtime/security/test; 6 |
| FR-P1-07 | POSLog v6.0 mapping/profile/output is open. | Approved versioned mapping, validation, manifest, and safe export. | Compliance format; Codex Z; POS Server; `feature/fiscal-ej-poslog-export-runtime` | Contract/runtime/export/UAT; 6 |
| FR-P1-08 | No report authorization policies or permissions. | Separate trusted permissions for X, Z close, read/reprint, compliance export, EJ, reconciliation, and admin. | Service-auth/RBAC mapping; Codex Z; POS Server; `feature/fiscal-reporting-api-presentation` | Security/API/test; 5 |
| FR-P1-09 | No report-specific durable audit writer. | Actor, scope, correlation, operation, outcome, and state-transition evidence without payload leakage. | FR-P1-08; Codex Z; POS Server; API/presentation branch | Runtime/security/test; 5 |
| FR-P1-10 | Reprint schema requires fiscal document and cannot target reports. | First-class immutable report output/reprint history without counting sales again. | Presentation contract; Codex Z; POS Server; API/presentation branch | Schema/runtime/API/test; 5 |
| FR-P1-11 | Exact BIR/Annex/POSLog formats, signatures, retention, and submission posture remain open. | Approved compliance decisions and controlled samples before UAT. | External compliance owner; POS Server docs/schema/runtime follow-on | Compliance/UAT; 1-6 |

## 4. P2 Gaps

| ID | Evidence/current behavior | Required behavior and impact | Dependency/owner/repository/branch | Classification/order |
| --- | --- | --- | --- | --- |
| FR-P2-01 | No report route, DTO, OpenAPI, safe error, or correlation contract. | Versioned scoped APIs with deterministic errors. | X/Z runtime contracts; Codex Z; POS Server; API/presentation branch | API/test; 5 |
| FR-P2-02 | No authoritative report render, print, or export adapters. | Snapshot-backed versioned JSON presentation and approved outputs. | Report schemas/runtime; Codex Z; POS Server; API/presentation and export branches | Runtime/API/test/UAT; 5-6 |
| FR-P2-03 | No report unit, integration, DB, recovery, print, or controlled-UAT evidence. | Layered deterministic fixtures and proofs. | Every implementation task; Codex Z; POS Server | Test/UAT; continuous |
| FR-P2-04 | No reconciliation service/read model/API. | Document-to-report-to-export checks and safe discrepancies. | All report producers; Codex Z; POS Server; `feature/fiscal-reporting-reconciliation-uat-readiness` | Runtime/API/test/UAT; 7 |
| FR-P2-05 | Reporting tables have only PK indexes except schema-profile uniqueness. | Workload-justified scope/period/reference indexes after queries are frozen. | Schema/query design; Codex Z; POS Server; contract/schema hardening branch | Schema/DB test; 1 |
| FR-P2-06 | No idempotency for report close/history/export requests. | Durable operation identity, exact replay, semantic conflict, and lost-response lookup. | API/close/export contracts; Codex Z; POS Server | Runtime/schema/API/test; 2-6 |
| FR-P2-07 | Export metadata has no execution, retry, source binding, manifest, or checksum. | Reproducible committed output lifecycle with verification and cleanup. | Approved formats; Codex Z; POS Server; EJ/POSLog/export branch | Runtime/export/test; 6 |
| FR-P2-08 | No report health, readiness, runbook, backup/recovery, or operator diagnostics. | Safe readiness/monitoring and recovery procedures. | Runtime complete; Codex Z; POS Server; reconciliation/UAT branch | Ops/docs/test/UAT; 7 |
| FR-P2-09 | Trigger inventory duplicates a multi-event trigger and reports it unexpected while validation passes. | Normalize catalog comparison and fail when unexpected inventory exists. | DB validation tooling; Codex Z; POS Server; contract/schema hardening branch | Validation/CI test; 1 |

## 5. P3 Gaps

| ID | Evidence/current behavior | Required behavior and impact | Dependency/owner/repository/branch | Classification/order |
| --- | --- | --- | --- | --- |
| FR-P3-01 | Provisional API route families use overlapping `/reports` and `/exports` patterns. | Freeze coherent route naming and versioning. | API contract; Codex Z; POS Server; API/presentation branch | Docs/API; 5 |
| FR-P3-02 | Support reference, correlation, retryability, and unknown-outcome wording are not frozen. | Consistent customer/operator-safe contract. | Existing API conventions; Codex Z; POS Server; API/presentation branch | Docs/API/test; 5 |
| FR-P3-03 | Paper width, pagination, labels, signatures, and reprint marks are undecided. | Approved presentation templates and printer proof. | Compliance/operations; Codex Z; POS Server; API/presentation branch | Rendering/UAT; 5 |
| FR-P3-04 | No reporting implementation/runbook/observability documentation. | Versioned operating and troubleshooting guidance backed by executable proof. | Runtime tasks; Codex Z; POS Server; reconciliation/UAT branch | Docs/ops/UAT; 7 |

