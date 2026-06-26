# ExitPass POS Server DB Validation / Rebuild / Drift Scripts Technical Review

## 1. Review Summary

This review covers the first executable POS Server database validation, rebuild, and drift-check package on branch `db/validation-rebuild-drift-scripts`.

Reviewed files:

- `db/rebuild/pos_sql_apply_order.txt`
- `db/validation/pos_expected_inventory.json`
- `db/validation/pos_prohibited_patterns.json`
- `db/validation/.gitignore`
- `db/scripts/Invoke-PosDbChecks.ps1`
- `db/scripts/README.md`
- `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_DB_Validation_Rebuild_Drift_Scripts_Implementation_Notes.md`

The review also inspected the current SQL inventory under `db/state/schemas/` and `db/state/tables/`.

The package is technically ready to commit. It provides deterministic manifest coverage, static repository validation, optional `psql`-gated rebuild/inventory/drift checks, expected object inventory validation, prohibited object checks, source-of-truth discipline, no-local-drift-promotion behavior, and ignored local evidence output.

## 2. Overall Recommendation

Approve for commit.

The package has:

- P0 findings: 0
- P1 findings: 0
- P2 findings: 0
- Editorial findings: 0

DB-dependent checks were not required for this review because no PostgreSQL connection string was provided. Static mode executed successfully.

## 3. Blocking Findings

No P0 findings.

## 4. Should-Fix Findings

No P1 findings.

## 5. Non-Blocking Findings

No P2 findings.

## 6. Editorial Findings

No editorial findings.

## 7. Manifest Review

`db/rebuild/pos_sql_apply_order.txt` exists and starts with:

```text
db/state/schemas/pos.sql
```

The manifest contains 52 SQL entries, matching the current repository SQL inventory:

- 1 schema SQL file
- 51 table SQL files

Independent comparison confirmed:

- all current `db/state/**/*.sql` files are listed
- no extra nonexistent or non-`db/state` SQL files are listed
- no current SQL files are missing from the manifest

The manifest is dependency-safe and does not rely on alphabetical order. Examples:

- controlled-code foundation appears before tables that reference `pos.controlled_codes`
- Site POS Server and fiscal identity tables appear before channel/terminal tables
- fiscal document core appears before detail, numbering, Digital SI, reprint, adjustment, report, export, audit, and recovery objects
- `pos.bir_sales_summary_reports` appears before `pos.annex_e_reports`
- `pos.fiscal_report_output_refs` appears before export package items that may reference report output rows
- `pos.recovery_requests` appears before `pos.continuity_check_results`

## 8. Expected Inventory Review

`db/validation/pos_expected_inventory.json` is valid JSON.

The expected inventory is correct for the current object state:

- expected schema is only `pos`
- expected table count is 51
- expected table list contains 51 entries
- current `db/state/tables/*.sql` inventory resolves to the same 51 `pos.*` table names
- object families map to slices 1 through 8

The inventory correctly does not expect:

- views
- functions
- triggers
- extensions
- PostgreSQL sequences
- Atlas files
- migrations
- seed/reference/sample data
- CI workflows

The file clearly preserves repository SQL as source of truth and states that local drift must be reported, not promoted.

## 9. Prohibited Pattern Review

`db/validation/pos_prohibited_patterns.json` is valid JSON.

The prohibited SQL checks cover:

- `CREATE FUNCTION`
- `CREATE TRIGGER`
- `CREATE EXTENSION`
- `CREATE SEQUENCE`
- `CREATE MATERIALIZED VIEW`
- outbox/event publication table names
- POS-owned payment finality lifecycle objects
- POS-owned `PaymentAttempt` lifecycle objects
- POS-owned `PaymentConfirmation` lifecycle objects
- POS-owned `ExitAuthorization` objects
- gate execution authority objects
- independent terminal fiscal authority objects
- offline fiscal issuance approval objects
- raw credential/token/evidence/key storage tables
- generated payload/binary storage tables

The artifact pattern checks cover:

- Atlas artifacts
- migration artifacts

The allowed reference-only names are explicitly preserved:

- `payment_finality_ref`
- `central_pms_payment_attempt_ref`
- `central_pms_payment_confirmation_ref`
- `vendor_ack_ref`
- `evidence_ref`

The case-insensitive matching posture is represented by configuration and implemented by the script.

## 10. PowerShell Script Review

`db/scripts/Invoke-PosDbChecks.ps1` implements all required modes:

- `Static`
- `Rebuild`
- `Inventory`
- `Drift`
- `All`

Static mode works without `psql` and verifies:

- manifest file exists
- every manifest file exists
- all `db/state/**/*.sql` files are listed in the manifest
- no extra unmanifested `db/state/**/*.sql` files exist
- prohibited SQL patterns
- detectable PostgreSQL 63-byte identifier length issues for explicit constraint names
- quoted identifiers
- schema-qualified `pos.*` table names
- lowercase `snake_case` posture for detectable schema/table/constraint identifiers
- source-of-truth and no-local-drift-promotion posture

Rebuild mode:

- requires an explicit connection string
- checks that `psql` is available
- applies manifest files in order
- uses `ON_ERROR_STOP=1`
- does not create or drop databases
- refuses obvious production/shared/authority target names using conservative matching

Inventory mode:

- requires an explicit connection string
- checks that `psql` is available through the shared database inventory path
- queries PostgreSQL catalog/information schema for schemas, tables, constraints, indexes, functions, triggers, extensions, and sequences
- compares schemas/tables against expected inventory
- reports prohibited functions, triggers, non-default extensions, and sequences

Drift mode:

- uses the same repository-expected inventory comparison
- reports differences only
- does not write SQL or update repository inventory

All mode:

- always runs Static
- skips DB-dependent checks when no connection string is provided
- runs Rebuild, Inventory, and Drift when a connection string is provided

Evidence is written only when `-EvidenceDir` is provided. No external PowerShell modules, Atlas, or Docker are required.

## 11. Safety Review

The package preserves the required safety boundaries:

- no credentials are embedded
- connection strings are not written into script summaries or evidence details
- generated evidence is ignored by `db/validation/.gitignore`
- the script never modifies `db/state`
- the script never updates `pos_expected_inventory.json` from a live database
- the script never creates SQL, Atlas, migration, seed/reference/sample, or CI artifacts
- drift is explicitly reported only
- local database state is never promoted into repository artifacts

Production/shared database safeguards are conservative and refuse targets whose connection string or optional database name appears to reference production, shared, authority, live, or Central PMS targets. This is not a substitute for operator discipline, and the README states that limitation.

## 12. README / Implementation Notes Review

`db/scripts/README.md` accurately explains:

- package purpose
- modes
- examples
- required environment
- `psql` dependency
- no-production/shared-database warning
- evidence output behavior
- known limitations
- next steps for CI integration later

`docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_DB_Validation_Rebuild_Drift_Scripts_Implementation_Notes.md` accurately describes:

- implemented files
- supported modes
- what is validated
- intentionally not-yet-validated items
- `psql` dependency
- no Atlas/CI posture
- repository source-of-truth rule
- no local drift promotion rule
- evidence output handling
- remaining limitations
- recommended next step

Both documents match the implemented script behavior.

## 13. Static Execution Review

Static mode was executed with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static
```

Result:

- Static mode passed
- generated JSON/text evidence was written under `db/validation/evidence/local-static`
- generated evidence is ignored by `db/validation/.gitignore`
- DB-dependent checks were skipped because no PostgreSQL connection string was provided

Independent review checks also confirmed:

- manifest count: 52
- current `db/state` SQL file count: 52
- expected table count: 51
- actual current table SQL count: 51
- expected inventory table list count: 51
- missing expected table count: 0
- extra expected table count: 0

## 14. Scope Review

The package remains within the requested scope.

No SQL object files under `db/state` were created, modified, or deleted. No application source, CI workflow, Atlas, migration, seed/reference/sample data, approved baseline, or `db/README.md` files were modified.

The only package files added are the approved manifest, validation configuration, PowerShell script, script README, validation `.gitignore`, implementation notes, and this technical review report.

## 15. Recommended Targeted Edits

No targeted edits are required before commit.

Future improvements should be handled as separate tasks after this package is committed:

- column-level inventory comparison
- foreign-key inventory comparison against a machine-readable model
- check constraint expression inventory
- optional disposable PostgreSQL rebuild evidence in PRs
- later CI integration after local checks are stable

## 16. Recommended Next Step

Commit the validation/rebuild/drift script package after normal review.

Then run `Rebuild`, `Inventory`, and `Drift` modes against a disposable PostgreSQL database and attach the generated evidence to the PR or follow-up validation review. After DB-dependent evidence is stable, proceed to CI integration or controlled-code seed/reference data as separate approved tasks.
