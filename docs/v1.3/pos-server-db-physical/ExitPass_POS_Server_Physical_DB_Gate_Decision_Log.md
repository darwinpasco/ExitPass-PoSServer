# ExitPass POS Server Physical DB Gate Decision Log

## 1. Approved Inherited Decisions

| ID | Decision | Source |
| --- | --- | --- |
| GDL-001 | POS Server app and database artifacts remain in `ExitPass-PoSServer`. | Approved Physical DB Artifact Plan. |
| GDL-002 | No separate `ExitPass-PoSServer-Db` repository at this stage. | Approved Physical DB Artifact Plan. |
| GDL-003 | Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. | Approved BRD/System Design/API Contract/Database Design. |
| GDL-004 | POS Server owns fiscal issuance and fiscal records only. | Approved baselines. |
| GDL-005 | POS Server database stores references to Central PMS authority records only. | Approved Database Design. |
| GDL-006 | ONLINE/OFFLINE is observability only. | Approved baselines. |
| GDL-007 | ARTS POSLog is a structured export reference only and does not replace BIR outputs. | Approved Database Design and Physical DB Artifact Plan. |

## 2. Gate Decisions Resolved Now

| ID | Decision | Resolution |
| --- | --- | --- |
| GDL-008 | Target engine default | PostgreSQL is the default target engine; final version/features remain pending. |
| GDL-009 | Candidate decomposition | Use approved logical domains as candidate physical decomposition. |
| GDL-010 | Naming posture | Use ExitPass v1.2 naming discipline: lowercase, snake_case by default, explicit key/constraint/index names, `_id` for internal identifiers, `_ref` for external references. |
| GDL-011 | Folder layout | Proposed `db/` layout is approved as future default, but folders are not created by this package. |
| GDL-012 | State workflow | Edit repo state, rebuild, load approved data, validate, drift-check, produce evidence, review authority boundary, merge reviewed changes only. |
| GDL-013 | Drift policy | Drift is reported, not automatically promoted. |
| GDL-014 | Validation plan | Validation categories from the approved plan are accepted as the future validation baseline. |
| GDL-015 | Seed/reference separation | Object definitions, seed data, reference data, samples, accreditation files, environment data, secrets, and external references remain separated. |

## 3. Placeholder Policies

| ID | Placeholder policy | Applies until |
| --- | --- | --- |
| GDL-016 | Fiscal numbering/counter placeholder policy from the gate resolution document. | BIR/accounting confirms final numbering, gap, reset, and Z-counter handling. |
| GDL-017 | Idempotency uniqueness strategy must support idempotency key, scope, semantic request identity/hash, linked fiscal operation, replay result, conflicts, timeout/completion-unknown, and duplicate prevention. | Physical object design defines exact constraints/indexes. |
| GDL-018 | Digital SI URL security posture uses opaque non-guessable token/reference, read-only access, lifecycle states, minimum exposure, and access audit where required. | Security/Privacy confirms final token/auth/expiry model. |

## 4. Pending Decisions

- PostgreSQL major version, extensions, hosting/runtime assumptions, collation/timezone, and deployment topology.
- Final schema names and decomposition.
- Final concrete object names.
- Final enum versus controlled-code storage per domain.
- Exact idempotency unique constraints and indexes.
- Exact retention periods and partition keys.
- Final Digital SI URL token/auth/expiry/access audit model.
- Final BIR/accreditation outputs and sample package.
- Final ARTS POSLog profile/schema mapping.
- Final tamper-evident anchoring mechanism.
- CI tooling and evidence publishing rules.

## 5. Decisions Explicitly Deferred

- SQL/DDL implementation.
- Physical table, column, index, constraint, type, sequence, view, function, trigger, and policy names.
- Atlas or other state-comparison tooling adoption.
- Seed/reference/sample data content.
- Validation/rebuild/drift script implementation.
- CI workflow implementation.
- Accreditation package content.
- Separate DB repository.

## 6. Non-Decisions

This decision log does not approve creation of SQL files, Atlas files, migrations, physical schemas, physical object files, the `db/` folder, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, diagrams, or offline fiscal issuance.