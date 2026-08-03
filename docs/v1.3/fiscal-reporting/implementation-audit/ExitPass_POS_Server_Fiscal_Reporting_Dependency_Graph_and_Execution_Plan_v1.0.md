# ExitPass POS Server Fiscal Reporting Dependency Graph and Recommended Execution Plan v1.0

## 1. Recommended First Task

The single highest-priority next POS Server task is **fiscal-reporting contract and schema hardening**.

| Attribute | Recommendation |
| --- | --- |
| Persona | Codex Z |
| Repository | `D:\SourceCodes\ExitPass-PoSServer` |
| Branch | `feature/fiscal-reporting-contract-and-schema-hardening` |
| Objective | Freeze aggregation/business-day/classification/idempotency contracts and add immutable, uniquely keyed report/period snapshot schema. |
| Exact dependency | Approved tax/status/tender/statutory aggregation rules plus BIR reporting identity and business-date decisions. |
| Why next | X and Z runtime would otherwise encode ambiguous totals and an unsafe close into mutable posture tables. |
| Likely production files | `db/state/tables`, `db/state/indexes`, controlled-code sources/generated SQL, rebuild/inventory/drift tooling, report contract docs/fixtures. |
| Tests | SQL static, direct constraints/immutability, Docker rebuild/inventory/drift/codes, contract fixtures. |
| Significant manual testing | No product walkthrough; database proof required. |
| UAT impact | Remains blocked; this task creates the runtime prerequisite. |

## 2. Dependency Graph

```text
Compliance decisions: tax/status/tender rules, business date, BIR/Annex/POSLog formats
                                   |
                                   v
Z-006A Contract and schema hardening
        |                         |
        v                         v
Z-006B X Reading runtime     Z-005C Annex E contract freeze (if still open)
        |
        v
Z-007 Z Reading close, counters, recovery
        |
        +-------------------+
        v                   v
Z-008 BIR summary/Annex E   Z-009 APIs/presentation/history/reprint
        |                   |
        +---------+---------+
                  v
Z-010 EJ/POSLog/export
                  |
                  v
Z-011 Reconciliation and controlled-UAT readiness
```

## 3. Bounded Task Sequence

### Z-006A: Contract and Schema Hardening

- Persona/repository/branch: Codex Z, POS Server, `feature/fiscal-reporting-contract-and-schema-hardening`.
- Objective: controlled families, aggregation source rules, business time, report/period identity, immutable snapshots, indexes, idempotency posture, BIR/Z relationship.
- Dependency: compliance owner decisions listed above.
- Tests: contract fixtures and complete disposable DB validation.
- Manual/UAT: no product walkthrough; UAT remains blocked.

### Z-006B: X Reading Runtime

- Branch: `feature/fiscal-x-reading-runtime`.
- Objective: read-only deterministic current-period aggregation, snapshot, repository/readback, concurrency behavior, and proof that no fiscal state mutates.
- Dependency: Z-006A.
- Likely files: Runtime/Persistence services and focused tests; no close endpoint yet.
- Tests: unit aggregation, PostgreSQL consistency/concurrency, restart/replay, privacy.
- Significant manual testing: disposable API/database proof required; controlled UAT remains blocked.

### Z-007: Z Reading Runtime and Fiscal-Period Close

- Branch: `feature/fiscal-z-reading-period-close-runtime`.
- Objective: privileged atomic close, exact period, counters/GTA, range/gaps, replay/conflict, uncertain-outcome recovery.
- Dependency: proven X aggregation basis and hardened schema.
- Tests: complete concurrency/failure-injection/restart/numbering/immutability project coverage.
- Significant manual testing: required; controlled close proof in disposable infrastructure.

### Z-008: BIR Sales Summary and Annex E

- Branch: `feature/fiscal-bir-summary-annex-e-runtime`.
- Objective: approved immutable summary/dataset generation and reconciliation from Z.
- Dependency: Z-007 and exact approved BIR/Annex contracts.
- Tests: versioned golden datasets, DB mapping, export validation, privacy.
- Significant manual testing: required with compliance-reviewed synthetic outputs.

### Z-009: Fiscal Report APIs and Presentation

- Branch: `feature/fiscal-reporting-api-presentation`.
- Objective: secured generate/close/read/history/readiness/presentation/reprint APIs and authoritative render models.
- Dependency: X/Z/BIR core runtime; approved auth and layout contracts.
- Tests: auth boundary, API contracts, safe errors, correlation, replay, rendering, printer adapter proof where introduced.
- Significant manual testing: required.

### Z-010: EJ, POSLog, and Exports

- Branch: `feature/fiscal-ej-poslog-export-runtime`.
- Objective: immutable EJ, approved POSLog mapping, package generation, manifest/checksum, retry, controlled retrieval.
- Dependency: approved formats and report APIs/source snapshots.
- Tests: integrity chain, deterministic bytes, retry/recovery, security/retention, export fixtures.
- Significant manual testing: required.

### Z-011: Reconciliation and Controlled-UAT Readiness

- Branch: `feature/fiscal-reporting-reconciliation-uat-readiness`.
- Objective: end-to-end reconciliation, readiness/health, runbooks, recovery drills, and controlled-UAT evidence package.
- Dependency: Z-006 through Z-010 complete.
- Tests: full affected suites, disposable DB/API/printer/export proofs, discrepancy fixtures.
- Significant manual testing: required; only this task may recommend UAT authorization after all gates pass.

## 4. Tasks Not Authorized by This Audit

The sequence is a recommendation, not authorization to begin runtime generation, Z close, controlled UAT, or production rollout. Each task requires its own branch, exact contract, validation, and review.

