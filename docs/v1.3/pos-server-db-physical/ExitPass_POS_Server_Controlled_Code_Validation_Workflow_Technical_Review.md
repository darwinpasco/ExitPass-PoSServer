# ExitPass POS Server Controlled-Code Validation Workflow Technical Review

## 1. Review Summary

The POS Server DB validation workflow now includes an opt-in `ControlledCodeLoad` mode in `db/scripts/Invoke-PosDbChecks.ps1`.

The new mode is separate from existing schema-only `Static`, `Rebuild`, `Inventory`, `Drift`, and `All` behavior. Existing `All` mode remains unchanged and does not load controlled-code reference data unless `ControlledCodeLoad` is explicitly selected.

## 2. Implementation Review

`ControlledCodeLoad` validates controlled-code reference data by:

- requiring an explicit `-ConnectionString`
- requiring an explicit `-DatabaseName`
- refusing production/shared/authority-looking database names
- requiring the database name to clearly indicate validation/local/disposable/test use
- resetting only the named disposable validation database through a `template1` maintenance connection
- rebuilding schema from `db/rebuild/pos_sql_apply_order.txt`
- discovering generated controlled-code SQL files from `db/reference-data/controlled-codes/generated/sql/`
- applying generated SQL in deterministic filename order
- applying the latest generated SQL file a second time for idempotency validation
- reading expected code-set and code-key inventory from `db/reference-data/controlled-codes/source/controlled_code_source_index.json` and referenced family JSON files
- comparing loaded code-set and code-key inventory to JSON source
- checking cumulative row counts, orphan controlled-code rows, and UUID inventory stability after repeat load
- writing evidence only when `-EvidenceDir` is provided

Docker-backed psql execution now uses a Windows PowerShell 5.1-compatible native process invocation that captures stdout and stderr separately and checks the process exit code explicitly. Normal psql stderr output such as `NOTICE` or `WARNING` is tolerated when the exit code is `0`; non-zero psql exit codes still fail with stdout/stderr context for diagnosis.

## 3. Scope Review

The workflow update did not create or modify controlled-code JSON source values, generated SQL files, schema SQL, migrations, Atlas configuration, CI workflows, application source code, or sample transaction data.

Generated controlled-code SQL remains separate from `db/state` schema SQL. JSON source remains the source of truth for controlled-code reference data, and disposable database state is validation evidence only.

## 4. Documentation Review

`db/scripts/README.md` now documents:

- `ControlledCodeLoad` mode
- local and Docker-backed examples
- disposable database reset behavior
- evidence output expectations
- controlled-code validation coverage and limitations

The validation implementation notes now document the opt-in controlled-code mode, source-of-truth posture, no-drift-promotion posture, and remaining limitations.

## 5. Docker psql stderr Handling Review

The prior Docker-backed execution path used PowerShell stream redirection:

```powershell
$output = & docker @($dockerArgs.ToArray()) 2>&1
```

That pattern could surface normal psql stderr notices as `NativeCommandError` records even when the Docker/psql process returned exit code `0`. This affected the disposable reset path because `DROP DATABASE IF EXISTS` can emit a harmless notice:

```text
NOTICE: database "posserver_controlled_code_workflow_validation_local" does not exist, skipping
```

The script now invokes Docker through `System.Diagnostics.ProcessStartInfo`, redirects stdout and stderr separately, and treats only the native exit code as authoritative. The implementation uses the older `Arguments` string property with explicit native-argument quoting instead of `ArgumentList`, because `ArgumentList` is not available in Windows PowerShell 5.1 / older .NET runtimes. Stderr text from successful psql calls is retained in `psql_diagnostics` evidence for modes that include it.

## 6. Validation Results

Static validation was run successfully:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static
```

Result:

```text
POS DB checks completed for mode Static.
```

Docker-backed controlled-code load validation completed successfully with the credentialed disposable database connection:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode ControlledCodeLoad `
  -UseDockerPsql `
  -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName posserver_controlled_code_workflow_validation_local `
  -EvidenceDir .\db\validation\evidence\controlled-code-workflow
```

Result:

```text
POS DB checks completed for mode ControlledCodeLoad.
```

Evidence source:

- `db/validation/evidence/controlled-code-workflow/pos-db-checks-controlledcodeload-20260629T103241Z.local.json`
- `db/validation/evidence/controlled-code-workflow/pos-db-checks-controlledcodeload-20260629T103241Z.local.txt`

Controlled-code load evidence:

- mode: `ControlledCodeLoad`
- status: `passed`
- database: `posserver_controlled_code_workflow_validation_local`
- schema rebuild manifest: `db/rebuild/pos_sql_apply_order.txt`
- schema files applied: 52
- generated SQL files applied: 3
- generated SQL files applied in deterministic filename order:
  - `db/reference-data/controlled-codes/generated/sql/001_controlled_codes_slice_1.sql`
  - `db/reference-data/controlled-codes/generated/sql/002_controlled_codes_slice_2.sql`
  - `db/reference-data/controlled-codes/generated/sql/003_controlled_codes_slice_3.sql`
- repeated generated SQL file: `db/reference-data/controlled-codes/generated/sql/003_controlled_codes_slice_3.sql`
- expected code sets: 19
- actual code sets after repeat load: 19
- expected code values: 101
- actual code values after repeat load: 101
- orphan controlled-code rows: 0
- missing code-set keys: 0
- unexpected code-set keys: 0
- missing code values: 0
- unexpected code values: 0
- UUID inventory stable after repeat load: true
- evidence path: `db/validation/evidence/controlled-code-workflow/`

Docker psql diagnostics included the harmless `DROP DATABASE IF EXISTS` notice with exit code `0`:

```text
NOTICE:  database "posserver_controlled_code_workflow_validation_local" does not exist, skipping
```

This confirms harmless psql `NOTICE` / `WARNING` stderr output is tolerated when the psql exit code is `0`. Non-zero psql exit codes still fail.

Additional validation notes:

- Static mode passed.
- `git diff --check` passed.
- evidence remains ignored under `db/validation/evidence/`.
- no `db/state`, JSON source, generated SQL, source code, CI, Atlas, migrations, sample transaction data, or Slice 4 changes were introduced.

## 7. Controlled-Code Validation Coverage

The implemented workflow is designed to validate the current cumulative controlled-code baseline from JSON source:

- 19 controlled-code sets
- 101 controlled-code values
- generated SQL files discovered in deterministic filename order
- loaded key inventory compared to JSON source
- orphan count expected to be zero
- UUID inventory expected to remain stable after repeat load

The credentialed evidence confirms these checks passed against `posserver_controlled_code_workflow_validation_local`.

## 8. Finding Counts

P0: 0

P1: 0

P2: 0

Editorial: 0

## 9. Final Recommendation

The controlled-code validation workflow is ready for commit review. Static mode passed, Docker-backed `ControlledCodeLoad` passed against the disposable validation database, `git diff --check` passed, and no `db/state`, JSON source, generated SQL, source code, CI, Atlas, migrations, sample transaction data, or Slice 4 changes were introduced.
