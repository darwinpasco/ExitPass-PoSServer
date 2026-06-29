# ExitPass POS Server DB Validation / Rebuild / Drift Scripts Implementation Notes

## 1. Purpose

This document records the first executable POS Server database validation, rebuild, and drift-check implementation package.

The package is intended to validate the current repository SQL state for slices 1 through 8 without adding new database objects, modifying existing object SQL, creating Atlas or migration artifacts, creating seed/reference/sample data, or adding CI workflows.

This implementation now supports both local `psql` execution and Docker-based `psql` execution for development environments that use Docker instead of a locally installed PostgreSQL client.

The workflow also includes an opt-in controlled-code reference-data load validation mode. That mode validates generated controlled-code SQL against the JSON source baseline on a disposable PostgreSQL database without modifying `db/state` schema SQL or promoting database state back into repository artifacts.

Repository SQL under `db/state` remains the source of truth. Local database state may be checked and reported, but must not be promoted into repository artifacts.

## 2. Implemented Files

| File | Purpose |
| --- | --- |
| `db/rebuild/pos_sql_apply_order.txt` | Deterministic SQL application manifest for current `db/state` SQL files. |
| `db/validation/pos_expected_inventory.json` | Expected schema/table inventory for the current approved SQL object state. |
| `db/validation/pos_prohibited_patterns.json` | Static prohibited SQL/object/artifact patterns for repository validation. |
| `db/validation/.gitignore` | Ignores generated local evidence output. |
| `db/scripts/Invoke-PosDbChecks.ps1` | PowerShell validation, rebuild, inventory, and drift script. |
| `db/scripts/README.md` | Script usage guide, examples, safety warnings, evidence behavior, and limitations. |
| `db/reference-data/controlled-codes/source/controlled_code_source_index.json` | JSON source index used by opt-in controlled-code load validation. |
| `db/reference-data/controlled-codes/generated/sql/*.sql` | Generated controlled-code reference-data SQL applied by opt-in controlled-code load validation. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_DB_Validation_Rebuild_Drift_Scripts_Implementation_Notes.md` | Implementation notes for this package. |

## 3. Supported Modes

| Mode | Requires `psql` | Summary |
| --- | --- | --- |
| `Static` | No | Validates manifest completeness, SQL inventory coverage, prohibited patterns, naming posture, quoted identifiers, schema qualification, and detectable PostgreSQL identifier length. |
| `Rebuild` | Yes, local or Docker | Applies all SQL files from `db/rebuild/pos_sql_apply_order.txt` to an explicitly provided PostgreSQL connection. |
| `Inventory` | Yes, local or Docker | Queries PostgreSQL catalog/information schema for schemas, tables, constraints, indexes, functions, triggers, extensions, and sequences, then compares schemas/tables to expected inventory. |
| `Drift` | Yes, local or Docker | Compares database inventory to repository expected inventory and reports drift only. |
| `ControlledCodeLoad` | Yes, local or Docker | Resets an explicitly named disposable validation database, rebuilds schema, applies generated controlled-code SQL in deterministic order, repeats the latest generated SQL file, and compares loaded inventory to JSON source. |
| `All` | Static without connection string; all modes with connection string | Runs Static first, then DB-dependent checks only when an explicit connection string is provided. |

## 4. Validated Items

Static validation currently checks:

- manifest file exists
- every manifest entry exists
- all `db/state/**/*.sql` files are listed in the manifest
- no extra unmanifested `db/state/**/*.sql` files exist
- prohibited SQL patterns such as functions, triggers, extensions, sequences, materialized views, outbox/event publication tables, payment/exit/gate authority objects, raw credential/token/evidence/key tables, and generated payload/binary tables
- detectable explicit constraint/object identifier length against PostgreSQL's 63-byte limit
- no quoted identifiers
- `CREATE TABLE` statements use schema-qualified `pos.*` names
- detectable object and constraint names use lowercase `snake_case`
- repository source-of-truth and no-local-drift-promotion posture

DB-dependent validation currently checks:

- deterministic SQL application order using the manifest
- expected `pos` schema exists
- expected table inventory matches `pos_expected_inventory.json`
- unexpected `pos` tables are reported
- functions, triggers, and sequences in `pos` schema are reported as prohibited
- non-default database extensions are reported as prohibited
- constraints and indexes are captured in evidence for review

Controlled-code load validation currently checks:

- disposable database reset is explicit through `-DatabaseName`
- normal schema rebuild uses `db/rebuild/pos_sql_apply_order.txt`
- generated controlled-code SQL files under `db/reference-data/controlled-codes/generated/sql/` are applied in deterministic filename order
- the latest generated SQL file is applied a second time to prove idempotent repeat loading
- expected code-set and code-key inventory is computed from `db/reference-data/controlled-codes/source/controlled_code_source_index.json` and referenced family JSON files
- loaded code-set and code-key inventory matches JSON source
- cumulative controlled-code row counts match JSON source
- controlled-code rows have no orphaned code-set references
- UUID inventory remains stable after the repeat load

## 5. Intentionally Not Validated Yet

The first implementation does not yet validate:

- every expected column name and data type
- every foreign-key relationship against a machine-readable model
- every check constraint expression
- every table and column comment
- BIR/accounting formula correctness
- final report layout correctness
- ARTS POSLog mapping correctness
- Digital SI token/auth implementation
- BIR/accounting-sensitive, Security/Privacy-sensitive, vendor/accreditation, tax, fiscal-document, Annex E, X/Z, discount/privilege, Digital SI URL, privacy-classification, ARTS POSLog, or export-profile controlled-code families that remain intentionally blocked
- performance or query plans
- retention and partitioning rules
- application/service behavior
- CI execution

These are future increments after the base validation/rebuild/drift workflow is stable.

## 6. `psql` / Docker Dependency

`Static` mode does not require `psql`.

`Rebuild`, `Inventory`, `Drift`, `ControlledCodeLoad`, and DB-dependent `All` checks require:

- PostgreSQL client `psql` on `PATH`; or
- Docker with `-UseDockerPsql`, using a PostgreSQL client image such as `postgres:16-alpine`
- explicit `-ConnectionString`
- disposable or explicitly approved non-production database target

Docker mode mounts the repository read-only at `/work` inside the client container, uses Docker `--entrypoint psql`, and translates manifest paths to container-visible paths before applying SQL files. It does not run the PostgreSQL image's default server command. The default Docker image is `postgres:16-alpine`, and the default host alias guidance is `host.docker.internal`.

The script does not install PostgreSQL, Docker, Atlas, or external PowerShell modules.

## 7. Controlled-Code Load Validation Posture

`ControlledCodeLoad` is opt-in and separate from schema-only `All` behavior.

The mode requires:

- `-ConnectionString`
- `-DatabaseName`
- a database name that clearly indicates validation/local/disposable/test use
- local `psql` or Docker-backed psql through `-UseDockerPsql`

The mode may reset the named disposable validation database by connecting to `template1`, then applies the normal schema manifest and generated controlled-code SQL. It does not create new controlled-code values, does not read JSON source directly into PostgreSQL, and does not modify generated SQL.

Generated controlled-code SQL remains a repository artifact derived from JSON source. Disposable database state is evidence only.

## 8. No Atlas / CI Posture

This package does not create:

- Atlas files
- migrations
- CI workflow files
- generated drift baselines
- generated schema state files

Atlas and CI remain future workstreams after the local validation package is stable.

Docker support does not add CI workflow files and does not change the no-Atlas posture.

## 9. Source-of-Truth Rule

The repository SQL files under `db/state` are the source of truth.

The validation inventory JSON describes the expected repository-backed schema/table inventory. Database inventory is compared against that expected state. Any difference is reported as drift.

Controlled-code JSON source under `db/reference-data/controlled-codes/source/` remains the source of truth for controlled-code reference data. Generated controlled-code SQL is applied only for repeatable validation/loading posture.

## 10. No Local Drift Promotion Rule

The script never:

- modifies `db/state`
- writes SQL object files
- updates expected inventory from a live database
- creates migration files
- creates Atlas state
- promotes local database state into repository artifacts
- updates controlled-code JSON source or generated SQL from a disposable database

Drift is evidence only and must be resolved through reviewed repository changes.

## 11. Evidence Output

Evidence is optional and created only when `-EvidenceDir` is provided.

Generated local evidence is ignored by `db/validation/.gitignore` using:

- `evidence/`
- `*.local.json`
- `*.local.txt`

Evidence files should not be committed unless a future task explicitly approves committed evidence artifacts.

## 12. Remaining Limitations

The current implementation is intentionally practical and conservative. It proves manifest coverage, static boundary checks, rebuild execution order, and schema/table inventory comparison before deeper validation.

Known limitations:

- static parsing is not a complete SQL parser
- inventory comparison is schema/table-focused
- Rebuild mode assumes the caller prepares an empty disposable database
- ControlledCodeLoad resets the explicitly named disposable validation database and is not intended for shared or production databases
- production/shared target detection is conservative but not exhaustive
- Docker mode depends on Docker availability, image availability, and valid host/network addressing from the psql client container
- Docker mode does not make production credentials safe; operators must use disposable local validation credentials only
- column-level and constraint-expression validation require a future machine-readable model

## 13. Recommended Next Step

Run `Static` mode in each DB artifact branch and run `All` mode against a disposable PostgreSQL database before future SQL object changes. Run `ControlledCodeLoad` after controlled-code JSON or generated SQL changes. Use local `psql` when available or `-UseDockerPsql` when the development environment uses Docker.

After this package is reviewed, the next recommended implementation increment is CI integration for Static and controlled-code load validation after local checks remain stable.
