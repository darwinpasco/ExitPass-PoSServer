# POS Server Database Checks

This folder contains the first executable validation/rebuild/drift package for the POS Server database state.

The script validates repository SQL under `db/state` against:

- `db/rebuild/pos_sql_apply_order.txt`
- `db/validation/pos_expected_inventory.json`
- `db/validation/pos_prohibited_patterns.json`

Repository SQL is the source of truth. Local database drift is reported only and must not be promoted into repository artifacts.

`Invoke-PosPersistentStateReconciliation.ps1` is the guarded upgrade path for an existing
non-production persistent IST database. It extracts the approved fiscal-completion, Electronic
Journal printable-text and canonical-required constraints, and reporting-period assignment function/trigger definitions directly
from `db/state`. It inventories first,
refuses unprovable completion ancestry before mutation, adds completion columns nullable, derives
ordinary paid history only from independently corroborated immutable payment-finality ancestry,
requires zero unresolved rows, and then installs the canonical constraints in one transaction.
Historical EJ printable text is never reconstructed, historical `semantic_hash_version` and hashes
are never rewritten, and historical `journal_context` is never updated. The canonical-required
constraint is definition-reconciled to accept exactly the immutable V1 and current V2 semantic
profiles; Apply stops on any unsupported historical profile. The script is not a migration-history mechanism and does not create a second schema
source of truth. It refuses production-like database names and stops when existing approved
profiles, immutable snapshots, completion ancestry, or pre-existing EJ printable text require a
separately governed decision.
Apply also fails closed when inspection detects material schema drift outside the explicitly
approved scope; that drift must be reviewed and authorized separately. Function reconciliation
uses `CREATE OR REPLACE FUNCTION`, preserves its OID, owner, and security-definer posture, and
replaces the reporting-period trigger only when its normalized canonical contract differs.

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

Inspect or reconcile an approved persistent IST database hosted by its PostgreSQL container:

```powershell
.\db\scripts\Invoke-PosPersistentStateReconciliation.ps1 `
  -Mode Inspect `
  -ContainerName exitpass-pos-ist-persistent-db `
  -DatabaseName exitpass_pos_ist

.\db\scripts\Invoke-PosPersistentStateReconciliation.ps1 `
  -Mode Apply `
  -ContainerName exitpass-pos-ist-persistent-db `
  -DatabaseName exitpass_pos_ist `
  -EvidenceDir .\db\validation\evidence\persistent-ist
```

Back up the target before `-Mode Apply`. Rebuild and ControlledCodeLoad remain disposable-database
operations and must not be used against the persistent IST database.

The governed persistent-IST operator sequence is deliberately separate from ordinary rebuilds.
Set `POS_IST_DB_PASSWORD` from the existing private environment; do not place it in repository
files. These commands are a future runbook and are not authorization to execute Apply:

The required order is: (1) backup/dump, (2) record SHA-256, (3) Inspect, (4) persist the
protected before manifest, (5) separately authorized Apply, (6) Inspect again, (7) Inventory,
(8) Drift, (9) function/trigger contract check, (10) V1/V2 constraint-definition check,
(11) persist the protected after manifest, (12) compare every historical identity, semantic
version/hash, integrity version/hash, prior hash, journal context, and printable-text value,
(13) deploy/restart POS from the reviewed dev baseline, and (14) run readiness. No fiscal
recovery POST is part of this runbook.

```powershell
$evidenceRoot = '.\db\validation\evidence\persistent-ist-schema-reconciliation\real-apply'
$preflightEvidence = Join-Path $evidenceRoot '01-preflight'
$applyEvidence = Join-Path $evidenceRoot '02-apply'
$postEvidence = Join-Path $evidenceRoot '03-post-apply'
New-Item -ItemType Directory -Force -Path $preflightEvidence,$applyEvidence,$postEvidence | Out-Null

docker run --rm --network exitpass-ist-persistent `
  -e "PGPASSWORD=$env:POS_IST_DB_PASSWORD" `
  -v "$((Resolve-Path $evidenceRoot).Path):/evidence" `
  postgres:16-alpine pg_dump `
  -h exitpass-pos-ist-persistent-db -U exitpass_ist -d exitpass_pos_ist `
  --format=custom --no-owner --no-privileges `
  --file=/evidence/exitpass_pos_ist.pre-reconcile.dump
Get-FileHash "$evidenceRoot\exitpass_pos_ist.pre-reconcile.dump" -Algorithm SHA256

.\db\scripts\Invoke-PosPersistentStateReconciliation.ps1 `
  -Mode Inspect -ContainerName exitpass-pos-ist-persistent-db `
  -DatabaseName exitpass_pos_ist -DatabaseUser exitpass_ist -EvidenceDir $preflightEvidence
$preflight = Get-Content (Join-Path $preflightEvidence 'pos-persistent-state-reconciliation.json') -Raw | ConvertFrom-Json
[IO.File]::WriteAllText(
  (Join-Path $preflightEvidence 'protected-before.json'),
  ($preflight.before.protected_manifest | ConvertTo-Json -Depth 20),
  [Text.UTF8Encoding]::new($false))

# Execute only after a separate authorization confirms that Inspect contains exactly
# the reviewed scoped drift and no unexpected object-inventory findings.
.\db\scripts\Invoke-PosPersistentStateReconciliation.ps1 `
  -Mode Apply -ContainerName exitpass-pos-ist-persistent-db `
  -DatabaseName exitpass_pos_ist -DatabaseUser exitpass_ist -EvidenceDir $applyEvidence -Confirm:$false

.\db\scripts\Invoke-PosPersistentStateReconciliation.ps1 `
  -Mode Inspect -ContainerName exitpass-pos-ist-persistent-db `
  -DatabaseName exitpass_pos_ist -DatabaseUser exitpass_ist -EvidenceDir $postEvidence

$env:POSSERVER_DB_URL = "postgresql://exitpass_ist:$($env:POS_IST_DB_PASSWORD)@exitpass-pos-ist-persistent-db:5432/exitpass_pos_ist"
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Inventory -UseDockerPsql `
  -DockerNetwork exitpass-ist-persistent -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName exitpass_pos_ist -EvidenceDir (Join-Path $evidenceRoot '04-inventory')
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Drift -UseDockerPsql `
  -DockerNetwork exitpass-ist-persistent -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName exitpass_pos_ist -EvidenceDir (Join-Path $evidenceRoot '05-drift')

$postApply = Get-Content (Join-Path $postEvidence 'pos-persistent-state-reconciliation.json') -Raw | ConvertFrom-Json
if (-not $postApply.before.reporting_period_assignment.function_definition_aligned -or
    -not $postApply.before.reporting_period_assignment.trigger_definition_aligned) {
  throw 'Post-Apply reporting-period function or trigger is not canonical.'
}
if (-not $postApply.before.canonical_required_constraint.definition_aligned -or
    $postApply.before.canonical_required_constraint.unsupported_rows -ne 0) {
  throw 'Post-Apply EJ canonical-required constraint is not the exact V1/V2 contract.'
}
[IO.File]::WriteAllText(
  (Join-Path $postEvidence 'protected-after.json'),
  ($postApply.before.protected_manifest | ConvertTo-Json -Depth 20),
  [Text.UTF8Encoding]::new($false))
if (($preflight.before.table_row_counts | ConvertTo-Json -Compress) -cne
    ($postApply.before.table_row_counts | ConvertTo-Json -Compress) -or
    ($preflight.before.protected_manifest | ConvertTo-Json -Depth 20 -Compress) -cne
    ($postApply.before.protected_manifest | ConvertTo-Json -Depth 20 -Compress)) {
  throw 'Post-Apply protected manifest differs from the preflight manifest, including historical EJ semantic/integrity versions or hashes.'
}
```

After every database and manifest check passes, stop only the POS application container and
relaunch the current reviewed dev source with the repository launcher; do not stop, recreate, or
reset the persistent database:

```powershell
docker stop --time 10 exitpass-pos-server-pitx-local
.\scripts\Start-PosServerPitxLocal.ps1 -SmokeTest
.\db\scripts\Test-PosPersistentIstFiscalIssuanceReadiness.ps1 -RequireReady
```

These commands stop after readiness verification. Terminal-cash recovery and all Central PMS
state changes are separately governed and are not part of schema Apply.

The Electronic Journal verifier dispatches by each persisted semantic profile. Historical rows
remain immutable under `pos-server-electronic-journal-event-semantic:sha256:v1`; new writes use
`pos-server-electronic-journal-event-semantic:sha256:v2`, whose semantic text includes
`printable_sales_invoice_text_sha256=`. Before and after Apply, compare the protected manifest,
including every historical `semantic_hash_version`, semantic hash, integrity version/hash,
previous hash, journal context, and printable text. Never rewrite historical semantic or integrity
hashes merely to make verification pass.

Rollback posture is backup restore into a separately named database/container followed by an
explicit cutover decision. The reconciliation does not provide reverse DDL because dropping
completion/EJ columns after new writes could destroy canonical facts. Never reset sequences,
renumber documents, recreate reporting periods, or reconstruct EJ history as rollback.

Persistent PITX fiscal issuance configuration remains separate from schema reconciliation:

```powershell
.\db\scripts\Set-PosPersistentIstFiscalOperationalConfiguration.ps1 -Mode Inspect
.\db\scripts\Set-PosPersistentIstFiscalOperationalConfiguration.ps1 -Mode Apply
.\db\scripts\Ensure-PosPersistentIstFiscalReportingPeriod.ps1 -Mode Apply
.\db\scripts\Test-PosPersistentIstFiscalIssuanceReadiness.ps1 -RequireReady
```

The static configuration command additively loads the canonical fiscal-issuance controlled-code
slice and materializes one stable PITX WebPay terminal, Sales Invoice sequence policy, and sequence
state. It never updates an existing sequence counter. The period command is intentionally separate:
it derives the current half-open Asia/Manila business day, reuses an existing current OPEN period,
and rejects overlap or ambiguity. All three commands refuse production-like database names.

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
- Inventory checks allow only functions and triggers explicitly listed in `db/validation/pos_expected_inventory.json`; all other functions and triggers remain prohibited drift.
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
