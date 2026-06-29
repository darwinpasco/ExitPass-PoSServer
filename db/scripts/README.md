# POS Server Database Checks

This folder contains the first executable validation/rebuild/drift package for the POS Server database state.

The script validates repository SQL under `db/state` against:

- `db/rebuild/pos_sql_apply_order.txt`
- `db/validation/pos_expected_inventory.json`
- `db/validation/pos_prohibited_patterns.json`

Repository SQL is the source of truth. Local database drift is reported only and must not be promoted into repository artifacts.

Controlled-code JSON source under `db/reference-data/controlled-codes/source/` is the source of truth for controlled-code reference data. Generated controlled-code SQL under `db/reference-data/controlled-codes/generated/sql/` is validated only through the opt-in `ControlledCodeLoad` mode.

## Modes

| Mode | Requires `psql` or Docker | Purpose |
| --- | --- | --- |
| `Static` | No | Validates manifest completeness, prohibited SQL patterns, naming posture, and detectable identifier length. |
| `Rebuild` | Yes | Applies SQL files from the manifest to an explicit PostgreSQL target in deterministic order. |
| `Inventory` | Yes | Reads PostgreSQL catalog inventory and compares schemas/tables to the expected repository inventory. |
| `Drift` | Yes | Reports database inventory drift from repository expected inventory without modifying repository files. |
| `ControlledCodeLoad` | Yes | Resets an explicitly named disposable database, rebuilds schema, applies generated controlled-code SQL in deterministic order, repeats the latest generated SQL file, and validates loaded inventory against JSON source. |
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

Controlled-code load validation against a disposable local database:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode ControlledCodeLoad `
  -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName posserver_controlled_code_workflow_validation_local `
  -EvidenceDir .\db\validation\evidence\controlled-code-load
```

## Pre-PR Validation

Run Static before opening a database artifact or reference-data PR:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode Static `
  -EvidenceDir .\db\validation\evidence\local-static
```

Run controlled-code load validation when controlled-code JSON source or generated SQL changes. Use a disposable PostgreSQL database only:

```powershell
$env:POSSERVER_DB_URL = 'postgresql://postgres:postgres@localhost:5432/posserver_controlled_code_local_validation'

.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode ControlledCodeLoad `
  -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName posserver_controlled_code_local_validation `
  -EvidenceDir .\db\validation\evidence\local-controlled-code-load
```

Do not use production, shared, authority, or Central PMS databases. `ControlledCodeLoad` resets the explicitly named disposable database.

## Docker psql Usage

Use Docker mode when the machine has Docker but does not have a local PostgreSQL client installed.

Start a disposable PostgreSQL database:

```powershell
docker run --name posserver-validation-db `
  -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_DB=posserver_validation_local `
  -p 55432:5432 `
  -d postgres:16-alpine
```

Set a connection string that the Docker psql client container can use. On Docker Desktop for Windows, `host.docker.internal` usually resolves from the client container back to the Windows host port mapping:

```powershell
$env:POSSERVER_DB_URL = 'postgresql://postgres:postgres@host.docker.internal:55432/posserver_validation_local'
```

Run all checks through Docker psql:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode All `
  -UseDockerPsql `
  -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName posserver_validation_local `
  -EvidenceDir .\db\validation\evidence\docker-all
```

Run controlled-code load validation through Docker psql:

```powershell
$env:POSSERVER_DB_URL = 'postgresql://exitpass:<password>@host.docker.internal:5433/posserver_controlled_code_workflow_validation_local'

.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode ControlledCodeLoad `
  -UseDockerPsql `
  -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName posserver_controlled_code_workflow_validation_local `
  -EvidenceDir .\db\validation\evidence\controlled-code-load
```

If the PostgreSQL database is on a Docker network, use a network alias in the connection string and pass the network name:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode All `
  -UseDockerPsql `
  -DockerNetwork posserver-validation-net `
  -ConnectionString 'postgresql://postgres:postgres@posserver-validation-db:5432/posserver_validation_local' `
  -DatabaseName posserver_validation_local `
  -EvidenceDir .\db\validation\evidence\docker-network-all
```

Docker mode defaults:

- `-DockerImage postgres:16-alpine`
- `-DockerHostAlias host.docker.internal`
- `-DockerContainerName posserver-db-checks-psql`

The script mounts the repository read-only at `/work` inside the psql client container, sets Docker `--entrypoint psql`, and applies manifest files using container-visible paths. Docker mode does not run the PostgreSQL image's default server command.

## Required Environment

- PowerShell
- No external PowerShell modules
- local `psql` on `PATH` for DB-dependent checks without `-UseDockerPsql`
- Docker for DB-dependent checks with `-UseDockerPsql`
- An explicit PostgreSQL connection string for DB-dependent checks

The script does not require Atlas. Docker is required only when `-UseDockerPsql` is provided.

## CI Validation

The repository includes `.github/workflows/pos-db-validation.yml`.

The workflow runs on `pull_request` and `workflow_dispatch` and performs:

- `Static` validation
- `ControlledCodeLoad` validation against a CI-local PostgreSQL 16 service database named `posserver_controlled_code_ci_validation`
- evidence upload as the `pos-db-validation-evidence` workflow artifact

CI uses only local workflow credentials:

- user: `postgres`
- password: `postgres`
- database: `posserver_controlled_code_ci_validation`
- connection string: `postgresql://postgres:postgres@localhost:5432/posserver_controlled_code_ci_validation`

The workflow does not require repository secrets, does not connect to live/shared databases, and does not commit generated evidence files.

## No Production / Shared DB Warning

Use only disposable or explicitly approved local validation databases.

The script refuses obvious production/shared target names such as names containing `prod`, `production`, `live`, `shared`, `authority`, or `central_pms`. The check is conservative and does not replace operator judgment.

`Static`, `Rebuild`, `Inventory`, `Drift`, and `All` do not create or drop databases. Prepare the disposable database outside those modes, then pass its connection string.

`ControlledCodeLoad` is intentionally different: it resets only the explicitly named disposable validation database passed through `-DatabaseName`. The name must look local/validation/disposable/test-oriented and must not look production/shared/authority-owned.

Do not pass production/shared database connection strings to local or Docker mode.

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
- ControlledCodeLoad mode validates controlled-code reference data by rebuilding schema, applying generated SQL files in deterministic filename order, repeating the latest generated SQL file for idempotency, and comparing loaded code-set/code-key inventory to JSON source.
- Docker mode depends on Docker being installed/running and on the selected Docker image being pullable or already available.
- Docker mode does not hide secrets from Docker itself; do not use production credentials. The script avoids printing the connection string in summaries and evidence.
- CI validation currently covers Static and ControlledCodeLoad only. Rebuild, Inventory, Drift, and deeper column/constraint checks remain local/manual or future CI increments.

## Next Steps

Recommended follow-up work:

1. Run Static mode locally for each DB artifact PR.
2. Run Rebuild, Inventory, and Drift modes against a disposable PostgreSQL database.
3. Extend inventory validation to column-level and foreign-key-level checks after the base workflow is stable.
4. Run ControlledCodeLoad after controlled-code JSON/generated SQL changes.
5. Expand CI later to include deeper inventory, column, constraint, and drift evidence if approved.
