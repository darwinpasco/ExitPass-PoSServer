# POS Server Database Checks

This folder contains the first executable validation/rebuild/drift package for the POS Server database state.

The script validates repository SQL under `db/state` against:

- `db/rebuild/pos_sql_apply_order.txt`
- `db/validation/pos_expected_inventory.json`
- `db/validation/pos_prohibited_patterns.json`

Repository SQL is the source of truth. Local database drift is reported only and must not be promoted into repository artifacts.

## Modes

| Mode | Requires `psql` | Purpose |
| --- | --- | --- |
| `Static` | No | Validates manifest completeness, prohibited SQL patterns, naming posture, and detectable identifier length. |
| `Rebuild` | Yes | Applies SQL files from the manifest to an explicit PostgreSQL target in deterministic order. |
| `Inventory` | Yes | Reads PostgreSQL catalog inventory and compares schemas/tables to the expected repository inventory. |
| `Drift` | Yes | Reports database inventory drift from repository expected inventory without modifying repository files. |
| `All` | Static only without connection string; all checks with connection string | Runs Static, then DB-dependent checks when `-ConnectionString` is provided. |

## Examples

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static

.\db\scripts\Invoke-PosDbChecks.ps1 -Mode All -ConnectionString $env:POSSERVER_DB_URL -EvidenceDir .\db\validation\evidence\local-all
```

Optional database name evidence/safety context:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Inventory -ConnectionString $env:POSSERVER_DB_URL -DatabaseName posserver_validation_local -EvidenceDir .\db\validation\evidence\local-inventory
```

## Required Environment

- PowerShell
- No external PowerShell modules
- `psql` on `PATH` only for `Rebuild`, `Inventory`, `Drift`, or DB-dependent `All` checks
- An explicit PostgreSQL connection string for DB-dependent checks

The script does not require Atlas or Docker.

## No Production / Shared DB Warning

Use only disposable or explicitly approved local validation databases.

The script refuses obvious production/shared target names such as names containing `prod`, `production`, `live`, `shared`, `authority`, or `central_pms`. The check is conservative and does not replace operator judgment.

The script does not create or drop databases. Prepare the disposable database outside this script, then pass its connection string.

## Evidence Output

Evidence is written only when `-EvidenceDir` is provided.

For each mode, the script writes:

- JSON evidence: `pos-db-checks-<mode>-<timestamp>.local.json`
- text summary: `pos-db-checks-<mode>-<timestamp>.local.txt`

`db/validation/.gitignore` ignores generated local evidence under `db/validation/evidence/` and `*.local.json` / `*.local.txt`.

## Known Limitations

- Static checks are conservative text and metadata checks. They do not replace applying SQL to PostgreSQL.
- Identifier-length checks cover detectable explicit identifiers such as constraint and object names.
- Inventory checks compare expected schemas/tables and report functions, triggers, extensions, and sequences; they do not validate every column definition yet.
- Drift checks report drift only. They never update repository SQL or validation configuration.
- Rebuild mode applies SQL to the provided target but does not create/drop the database.
- CI integration is intentionally deferred.

## Next Steps

Recommended follow-up work:

1. Run Static mode locally for each DB artifact PR.
2. Run Rebuild, Inventory, and Drift modes against a disposable PostgreSQL database.
3. Extend inventory validation to column-level and foreign-key-level checks after the base workflow is stable.
4. Add controlled-code seed/reference data in a separate approved task.
5. Add CI workflow integration after local checks are stable and repeatable.
