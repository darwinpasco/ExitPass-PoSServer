# ExitPass POS Server Fiscal Reporting Readiness Decision Record v1.0

## 1. Decision

At baseline `a70117d7197b778eeb97d3e99ec84b4076c96075`, fiscal reporting is **NOT_READY** for controlled UAT and **NOT_READY** for production.

## 2. Separate Verdicts

| Capability | Verdict |
| --- | --- |
| X Reading schema | PARTIALLY_READY |
| X Reading runtime | NOT_IMPLEMENTED |
| X Reading API | NOT_IMPLEMENTED |
| X Reading rendering | NOT_IMPLEMENTED |
| X Reading UAT readiness | NOT_READY |
| Z Reading schema | PARTIALLY_READY |
| Z Reading runtime | NOT_IMPLEMENTED |
| Z Reading API | NOT_IMPLEMENTED |
| Z Reading rendering | NOT_IMPLEMENTED |
| Z Reading UAT readiness | NOT_READY |
| BIR sales summary | PARTIALLY_READY |
| Annex E | PARTIALLY_READY |
| Electronic Journal | PARTIALLY_READY |
| POSLog/export | PARTIALLY_READY |
| Report RBAC | NOT_IMPLEMENTED |
| Report reconciliation | NOT_IMPLEMENTED |
| Reporting controlled-UAT readiness | NOT_READY |
| Production readiness | NOT_READY |

## 3. Trusted Baseline

- Fiscal-document create/read/void, idempotency, numbering, immutable applied statutory facts, PostgreSQL transaction boundaries, and Digital Sales Invoice presentation are implemented and tested.
- Reporting, BIR, Annex E, EJ, and export database objects rebuild successfully and are inventoried.
- Those objects are design posture. They are not evidence of report generation or close behavior.
- Current automated baseline passes 312 tests, none of which generates or closes a fiscal report.

## 4. Authorization Gates

Controlled UAT remains unauthorized until all P0 and P1 gaps are closed and evidence proves:

- approved fiscal aggregation and business-date rules;
- immutable, unique X/Z and closed-period snapshots;
- read-only X and atomic idempotent Z behavior;
- counter/GTA/range/gap correctness and crash recovery;
- BIR/Annex/POSLog approved contracts and reconciliation;
- trusted RBAC, audit, privacy, and safe errors;
- authoritative presentation, print/reprint, export integrity, and history;
- disposable automated/manual proofs followed by controlled evidence review.

Production additionally requires deployment configuration, monitoring, backup/recovery drills, retention, operator runbooks, compliance approval, and successful controlled UAT.

## 5. Explicit Prohibitions

- Do not execute fiscal report generation against shared or persistent data.
- Do not execute a Z close.
- Do not treat report tables, provisional DTO prose, or passing rebuild checks as runtime readiness.
- Do not infer BIR/Annex/POSLog formats outside approved contracts.
- Do not expose statutory identity, evidence, credentials, hashes, or internal errors in reports or exports.

## 6. Review Trigger

Reassess this decision only after the bounded implementation sequence is completed with current source, database, API, concurrency, security, rendering, export, reconciliation, and controlled evidence.

