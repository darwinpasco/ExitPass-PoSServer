# ExitPass POS Server DB Validation / Rebuild Decision Log

## 1. Purpose

This decision log records planning decisions for future POS Server database validation, clean rebuild, and drift-check workflows. It does not create scripts or alter database artifacts.

## 2. Inherited Approved Decisions

| Decision | Source / rationale |
| --- | --- |
| POS Server database artifacts remain inside `ExitPass-PoSServer`. | Approved repository boundary and `db/README.md`. |
| Repository-owned state is the source of truth. | Approved Physical DB Artifact Plan and `db/README.md`. |
| Local drift must not be promoted directly. | Approved Gate Resolution and `db/README.md`. |
| PostgreSQL is the default target engine posture. | Approved Physical DB Artifact Plan and Gate Resolution. |
| Current SQL scope is first-slice only. | Approved First Physical Object Slice Design and SQL technical review. |
| Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. | Approved authority model. |
| POS Server database stores Central PMS references only. | Approved authority and naming baselines. |

## 3. Planning Decisions Resolved Now

| Decision | Resolution |
| --- | --- |
| Validation/rebuild/drift workflow remains documentation-only in this task. | Future scripts are planned but not created. |
| Future rebuild should use a disposable clean database. | Avoids local drift and proves repository state can recreate expected objects. |
| Future SQL application order should be deterministic. | Use schemas first, then tables in dependency order. |
| Future validation should capture inventory evidence. | Evidence should include schema, table, constraint, index, and prohibited-object checks. |
| Future drift checks should report only. | Drift reports are evidence and must not auto-promote live state. |

## 4. Validation Decisions

| Area | Planning decision |
| --- | --- |
| SQL inventory | Validate that only approved SQL files exist for the current scope. |
| Naming | Validate lowercase `snake_case`, `pos` schema, `_id`, `_ref`, `central_pms_*_ref`, and PostgreSQL identifier length. |
| Authority boundary | Validate no POS-owned payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, ExitAuthorization, gate execution, independent terminal fiscal authority, or offline fiscal issuance approval. |
| Object existence | Validate first-slice schema and table existence after rebuild. |
| Constraints/indexes | Capture primary key, foreign key, unique, check, and index inventory; expect no unauthorized indexes yet. |
| Controlled-code posture | Validate structure exists but no code values are introduced unless approved. |

## 5. Rebuild Decisions

| Area | Planning decision |
| --- | --- |
| Database target | Use disposable local or CI database only. Never use production/shared databases for rebuild tests. |
| Application order | Apply `db/state/schemas/pos.sql`, then controlled-code tables, boundary tables, channel registry tables, and history/capability tables. |
| Evidence | Capture command result, applied files, object inventory, and validation summary. |
| Secrets | Do not commit credentials; use local/CI-provided connection settings in future scripts. |

## 6. Drift Decisions

| Area | Planning decision |
| --- | --- |
| Source of truth | Repository SQL files remain source of truth. |
| Drift outcome | Report missing, unexpected, and changed objects. Do not mutate repository state. |
| Accepted drift | Must become a reviewed PR that edits repository artifacts. |
| Atlas option | Defer Atlas/state-comparison decision until tooling review. |

## 7. Decisions Deferred To Future Script Task

- Exact script language and runtime.
- Exact PostgreSQL client tooling.
- Disposable database provisioning method.
- CI runner image and secret handling.
- Machine-readable evidence format.
- Atlas/state-comparison tool selection.
- Exact drift report schema.
- Whether indexes require separate state files or table-inline validation only.

## 8. Non-Decisions

This planning package does not decide or create:

- executable validation/rebuild/drift scripts;
- CI workflows;
- Atlas configuration;
- migrations;
- new SQL object coverage;
- seed/reference/sample data;
- source code changes;
- production database deployment process.

