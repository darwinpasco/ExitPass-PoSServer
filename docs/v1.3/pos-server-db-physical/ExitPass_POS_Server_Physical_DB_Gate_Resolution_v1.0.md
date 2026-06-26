# ExitPass POS Server Physical DB Gate Resolution v1.0

## 1. Document Control

| Field | Value |
| --- | --- |
| Document title | ExitPass POS Server Physical DB Gate Resolution v1.0 |
| Repository | `ExitPass-PoSServer` |
| Branch | `docs/v1.3-pos-server-physical-db-gate-resolution` |
| Status | Approved baseline |
| Output format | Markdown only |
| Approved Physical DB Artifact Plan | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Plan_v1.0.md` |
| Physical artifact status | No SQL, Atlas, migrations, physical object files, `db/` folder, seed/reference/sample data, scripts, or CI workflows are created by this package. |

## 2. Approval / Baseline Status

This document is approved as the POS Server Physical DB Gate Resolution v1.0 baseline. It governs the next narrowly scoped physical database bootstrap task and records the approved gate classifications, placeholder policies, pending confirmations, repository boundary, authority boundary, and physical artifact readiness posture.

This approval authorizes only a future separate bootstrap task that may create the approved empty `db/` folder skeleton and `db/README.md`.

This approval does not authorize creating SQL files, DDL, Atlas files, migrations, physical schemas, physical database object files, seed/reference/sample data, validation scripts, rebuild scripts, drift scripts, CI workflows, source code, DOCX files, or diagrams.

## 3. Purpose and Scope

This document resolves or classifies the Physical Design Gate items required before future POS Server physical database artifacts are created.

In scope:

- Gate decision classification.
- Safe defaults and placeholder policies.
- Remaining blockers by owner and workstream.
- Readiness for future `db/` folder and object artifact tasks.

Out of scope:

- SQL, DDL, Atlas files, migrations, physical object files, actual `db/` folders, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, DOCX files, and diagrams.

## 4. Approved Baseline References

| Reference | Role |
| --- | --- |
| Approved POS/Invoicing BRD v1.0 | Business baseline and authority model. |
| Approved POS Server System Design v1.0 | Architecture and authority split. |
| Approved POS Server API Contract v1.0 | API/idempotency/status baseline. |
| Approved POS Server Database Design v1.0 | Logical database baseline and Physical Design Gate. |
| Approved Physical DB Artifact Plan v1.0 | Physical artifact planning baseline. |
| Physical DB Artifact planning package | Source analysis, decision log, open questions, layout, validation, and outline. |
| Repository Boundary | Confirms POS Server repository and Central PMS authority boundary. |

## 5. Gate Resolution Summary

| Category | Count | Summary |
| --- | ---: | --- |
| Resolved | 6 | Seed/reference separation, drift policy, validation plan, rebuild plan, state workflow, no local drift promotion. |
| Resolved as planning default | 7 | PostgreSQL, schema/domain candidates, naming posture, folder layout, enum/control-code strategy, Digital SI URL posture, ARTS mapping posture. |
| Resolved as placeholder policy | 2 | Fiscal numbering/counter strategy and idempotency uniqueness strategy. |
| Pending confirmation | 3 | Retention/partitioning, BIR/accreditation outputs, tamper-evident anchoring mechanism. |
| Blocks immediate physical object creation | 10 | Items needing final confirmation or placeholder approval before SQL/object files. |

## 6. Resolved Gate Items

The following gate items are resolved enough to guide future planning tasks:

- Object-level folder structure is approved as proposed future layout, but not yet created.
- State-based versioning workflow is approved as posture.
- Rebuild workflow plan is approved as posture.
- Drift-check plan is approved as policy: report drift, do not auto-promote.
- Validation script plan is approved as validation category baseline.
- Seed/reference data separation is approved.
- No local drift promotion rule is confirmed.

## 7. Placeholder Policies

### Fiscal Numbering and Counters

Use this placeholder policy until BIR/accounting confirms final details:

- Sales Invoice sequence is scoped to Site POS Server.
- Adjustment sequence is scoped by BIR-confirmed adjustment family.
- Display fiscal number is separate from internal fiscal document ID.
- Do not consume SI number until fiscal issuance commit.
- If a reserved number fails after reservation, retain permanent gap/audit record.
- Never reuse consumed numbers.
- Never reuse reserved numbers unless BIR/accounting approves a compliant rule.
- Reset counter starts at zero and increments only on fiscal reset.
- Z-counter advances per fiscal day close / Z-reading.
- Reset counter does not advance per Z-read.

### Idempotency Uniqueness

Physical design must support idempotency key, idempotency scope, semantic request identity/hash, linked fiscal operation, replay result, conflict status, timeout/completion-unknown state, and duplicate fiscal document prevention. Exact unique constraints and indexes remain downstream.

## 8. Pending Confirmation Items

| Item | Pending confirmation | Owner/dependency |
| --- | --- | --- |
| PostgreSQL version/features | Major version, extensions, hosting/runtime, collation/timezone, deployment topology. | Engineering / Operations |
| Physical schema names | Final schema names and decomposition. | Physical DB Design |
| Final concrete object names | Table, column, key, constraint, index, type, sequence, view, function, trigger names. | Physical DB Design |
| Retention/partitioning | Exact retention periods and partition keys. | BIR/accounting / Security / Operations |
| Digital SI URL security details | Token/auth/expiry/access audit model. | Security / Privacy |
| BIR/accreditation package | Final examiner-required outputs, layouts, samples, and evidence. | BIR/accreditation |
| ARTS POSLog mapping | Final accepted profile/schema mapping. | Engineering / BIR accreditation |
| Tamper-evident anchoring | Final hash chain/external anchor/recovery mechanism. | Security / Engineering / Operations |

## 9. Physical Artifact Creation Readiness

| Artifact category | Readiness | Rationale |
| --- | --- | --- |
| `db/` folder creation | Conditionally ready after stakeholder acceptance of this gate package. | Folder layout is resolved as future default, but actual creation requires a separate implementation task. |
| SQL/object artifact creation | Not ready. | PostgreSQL version/features, naming, schema decomposition, numbering, retention, security, and anchoring details remain pending. |
| Seed/reference data | Not ready. | Controlled-code strategy and seed/reference policies need implementation task decisions. |
| Validation/rebuild/drift scripts | Not ready. | Workflow is approved as posture; tooling and script implementation remain downstream. |
| CI workflows | Not ready. | CI runner, service images, evidence publishing, and failure rules remain pending. |
| Accreditation/sample artifacts | Not ready. | Examiner package expectations and sample data policy remain pending. |

## 10. Authority Boundary Confirmation

This gate package preserves the approved authority model:

- Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization.
- POS Server owns fiscal issuance and fiscal records only.
- POS Server database stores references to Central PMS authority records only.
- POS Server database does not own payment finality.
- POS Server database does not own, issue, or mutate ExitAuthorization.
- Audit and events are evidence only.
- Channels/terminals are child endpoints under Site POS Server.
- ONLINE/OFFLINE is observability only.
- Offline fiscal issuance is disabled by default unless BIR/accounting approves a compliant model.
- ARTS POSLog is a structured export reference only and does not replace BIR outputs.

## 11. Repository Boundary Confirmation

POS Server app and database artifacts remain inside `ExitPass-PoSServer`. The future `db/` boundary is a folder boundary only. A separate DB repository is deferred and remains a future option only if independent ownership, independent versioning, or compliance change control requires it.

## 12. Out of Scope

This package does not create or approve actual SQL, DDL, Atlas files, migrations, physical schemas, physical database object files, actual `db/` folders, seed/reference/sample data, validation scripts, rebuild scripts, drift scripts, CI workflows, application code, DOCX files, diagrams, final accreditation package, or offline fiscal issuance approval.

## 13. Recommended Next Step

Create a narrowly scoped physical DB artifact bootstrap task that creates only the approved `db/` folder skeleton and `db/README.md`, with no SQL/object files, after stakeholder acceptance of this gate-resolution package.