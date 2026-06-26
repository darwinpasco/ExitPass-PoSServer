# ExitPass POS Server DB Docker Validation Support Technical Review

## 1. Review Summary

This review covers the targeted Docker-backed `psql` invocation fix in `db/scripts/Invoke-PosDbChecks.ps1`.

The previous fixed-size collection issue was already resolved. The next Docker failure was:

```text
Error: Database is uninitialized and superuser password is not specified.
```

That error showed Docker mode was starting the `postgres:16-alpine` image's default PostgreSQL server command instead of invoking the image as a `psql` client.

The script now explicitly sets Docker `--entrypoint psql`, mounts the repository read-only at `/work`, uses `/work` as the container working directory, and passes `psql` arguments directly after the image name.

The user confirmed Docker-backed `All` mode now passes against the disposable database `posserver_validation_local` exposed on host port `5433`.

## 2. Finding Counts

| Severity | Count |
| --- | ---: |
| P0 | 0 |
| P1 | 0 |
| P2 | 0 |
| Editorial | 0 |

## 3. Root Cause

Docker mode previously built arguments that ended with the Docker image name and then appended:

```powershell
sh -c 'psql ...'
```

Because Docker command placement was wrong, Docker used the image's default entrypoint/command path and attempted to start a PostgreSQL server. The server startup path requires initialization settings such as `POSTGRES_PASSWORD`, which caused:

```text
Database is uninitialized and superuser password is not specified.
```

This was not a database schema validation issue. It was a Docker invocation issue.

## 4. Exact Fix Applied

`Get-DockerPsqlBaseArgs` now adds:

```powershell
--entrypoint psql
```

before the Docker image name.

`Invoke-DockerPsqlCommand` now passes `psql` arguments directly:

```powershell
<connection string> -v ON_ERROR_STOP=1 -f /work/<manifest path>
```

or, for catalog SQL:

```powershell
<connection string> -v ON_ERROR_STOP=1 -t -A -c <sql>
```

The repository mount remains read-only:

```text
<repo>:/work:ro
```

The script still translates manifest paths to container-visible `/work/...` paths and still splats Docker arguments safely:

```powershell
& docker @($dockerArgs.ToArray())
```

## 5. Static Mode Result

Static mode was run with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static
```

Result:

```text
POS DB checks completed for mode Static.
```

Static mode passed.

## 6. Docker All Result

The user reran Docker-backed `All` mode using the real disposable database credentials:

```powershell
.\db\scripts\Invoke-PosDbChecks.ps1 `
  -Mode All `
  -UseDockerPsql `
  -ConnectionString $env:POSSERVER_DB_URL `
  -DatabaseName posserver_validation_local `
  -EvidenceDir .\db\validation\evidence\docker-all
```

Confirmed result:

```text
POS DB checks completed for mode All.
```

Interpretation:

- Docker mode invokes `psql`, not the PostgreSQL server default command.
- The previous fixed-size collection error is resolved.
- The previous `Database is uninitialized and superuser password is not specified` error is resolved.
- The previous authentication issue was resolved by running with the real disposable database credentials.
- Rebuild, Inventory, and Drift reached the disposable PostgreSQL database successfully.
- Docker-backed `All` mode passed against `posserver_validation_local` on host port `5433`.

## 7. Evidence Handling

Generated evidence remains under `db/validation/evidence/` and is ignored by `db/validation/.gitignore`.

No generated evidence is part of the tracked/untracked package status.

## 8. Scope Confirmation

No SQL files were added, modified, or deleted.

No files under `db/state` were modified.

No Atlas files, migrations, seed/reference/sample data, CI workflow files, application source files, approved baseline documents, or `db/README.md` files were modified.

## 9. Recommendation

The Docker validation support package is ready to commit.

Static mode and Docker-backed `All` mode both pass. Generated evidence remains ignored, and no SQL, `db/state`, source, baseline, CI, Atlas, migration, seed/reference/sample, or `db/README.md` files were modified.
