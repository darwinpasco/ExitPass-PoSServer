# ExitPass POS Server DB Validation / Rebuild Source Analysis

## 1. Purpose

This document records the source analysis for future POS Server database validation, clean rebuild, and drift-check workflow planning. It prepares for future scripts and CI evidence, but does not create scripts, SQL, migrations, Atlas files, CI workflows, source code, seed/reference/sample data, DOCX files, or diagrams.

## 2. Sources Inspected

| Source | Use in this planning package |
| --- | --- |
| `ExitPass_POS_Server_Physical_DB_Artifact_Plan_v1.0.md` | Approved state-based artifact, rebuild, validation, drift-check, and PR evidence posture. |
| `ExitPass_POS_Server_Physical_DB_Gate_Resolution_v1.0.md` | Approved gate posture: repository state as source of truth, no local drift promotion, validation/rebuild/drift scripts not yet created. |
| `ExitPass_POS_Server_Physical_DB_Schema_Naming_Standards_v1.0.md` | Naming rules for schema, tables, columns, constraints, files, and authority-safe references. |
| `ExitPass_POS_Server_Physical_Object_Design_v1.0.md` | Approved physical object sequencing and authority-boundary checks. |
| `ExitPass_POS_Server_First_Physical_Object_Slice_Design_v1.0.md` | Approved first-slice object scope and exclusions. |
| `ExitPass_POS_Server_First_Slice_SQL_Object_Technical_Review.md` | Confirms first-slice SQL artifacts are in scope, authority-safe, and ready with no remaining P0/P1/P2 findings. |
| `db/README.md` | Repository-owned state-based artifact posture, no local drift promotion, and `db/` folder purposes. |
| Current `db/state` SQL files | Current approved SQL inventory to validate and rebuild. |

## 3. Current Approved SQL Scope

The current approved SQL scope is limited to:

- `db/state/schemas/pos.sql`
- `db/state/tables/pos.controlled_code_sets.sql`
- `db/state/tables/pos.controlled_codes.sql`
- `db/state/tables/pos.site_pos_servers.sql`
- `db/state/tables/pos.fiscal_identities.sql`
- `db/state/tables/pos.site_pos_server_fiscal_identity_history.sql`
- `db/state/tables/pos.channel_terminals.sql`
- `db/state/tables/pos.channel_terminal_capabilities.sql`
- `db/state/tables/pos.channel_terminal_status_history.sql`

This planning package does not add, remove, or modify SQL files.

## 4. Approved Boundaries

Future validation, rebuild, and drift-check workflows must preserve these boundaries:

- Repository state is the source of truth.
- Local database drift must not be promoted directly.
- Drift must be reported as evidence, not auto-promoted.
- Accepted drift must become a reviewed repository change.
- Central PMS authority is external to POS Server.
- POS Server database must not own payment finality.
- POS Server database must not own PaymentAttempt lifecycle.
- POS Server database must not own PaymentConfirmation lifecycle.
- POS Server database must not own ExitAuthorization.
- ONLINE/OFFLINE remains observability only.
- No offline fiscal issuance approval is implied.

## 5. Validation Needs

Future validation must prove:

- SQL inventory matches approved scope.
- Schema and first-slice tables exist after rebuild.
- Object names follow approved naming standards.
- Constraint names fit PostgreSQL identifier limits.
- Foreign keys reference approved in-scope objects.
- Controlled-code foundation tables exist without seed/reference values unless approved.
- No prohibited object families exist.
- No authority-leaking object or column names exist.
- No unauthorized functions, triggers, extensions, sequences, seed data, or scripts are present.

## 6. Rebuild Needs

Future rebuild workflow must support:

- disposable clean database creation;
- deterministic SQL application order;
- schema creation before table creation;
- table creation in dependency order;
- optional PostgreSQL syntax/application smoke checks;
- inventory capture after rebuild;
- evidence suitable for local development and CI.

## 7. Drift-Check Needs

Future drift workflow must:

- compare live/test database state to repository state;
- classify missing, unexpected, and changed objects;
- include schema, table, constraint, index, and controlled-code posture checks;
- report drift without changing repository files;
- require an explicit PR for any accepted database state change;
- optionally use Atlas/state comparison only after tooling review.

## 8. Out of Scope

This package does not create or modify:

- SQL files;
- Atlas files;
- migrations;
- seed/reference/sample data;
- validation/rebuild/drift scripts;
- CI workflows;
- source code;
- approved baseline documents;
- `db/README.md`;
- DOCX files;
- diagrams.

