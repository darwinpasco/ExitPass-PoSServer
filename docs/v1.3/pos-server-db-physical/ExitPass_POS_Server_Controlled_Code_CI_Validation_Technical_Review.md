# ExitPass POS Server Controlled-Code CI Validation Technical Review

## 1. Review Summary

This change adds CI/pre-PR validation for the POS Server database validation workflow. The integration is limited to:

- `Static` database checks
- `ControlledCodeLoad` against a disposable PostgreSQL database

The workflow does not add Slice 4, does not add new controlled-code values, and does not modify `db/state`.

## 2. CI Workflow Review

Workflow file:

- `.github/workflows/pos-db-validation.yml`

Triggers:

- `pull_request`
- `workflow_dispatch`

Runner:

- `ubuntu-latest`

PostgreSQL service:

- image: `postgres:16`
- user: `postgres`
- password: `postgres`
- database: `posserver_controlled_code_ci_validation`
- connection string: `postgresql://postgres:postgres@localhost:5432/posserver_controlled_code_ci_validation`

The workflow installs `postgresql-client`, runs the existing PowerShell validation script with local `psql`, and uploads `db/validation/evidence/` as a workflow artifact named `pos-db-validation-evidence`.

## 3. Validation Scope Review

CI/pre-PR validation is intentionally limited to:

- `pwsh ./db/scripts/Invoke-PosDbChecks.ps1 -Mode Static`
- `pwsh ./db/scripts/Invoke-PosDbChecks.ps1 -Mode ControlledCodeLoad`

`ControlledCodeLoad` uses only the CI-local disposable PostgreSQL service database. It resets the explicitly named CI database and applies:

- schema SQL through `db/rebuild/pos_sql_apply_order.txt`
- generated controlled-code SQL files from `db/reference-data/controlled-codes/generated/sql/` in deterministic filename order

The workflow does not run against live, shared, authority, or Central PMS databases.

## 4. Secret and Safety Review

The workflow does not require repository secrets. It uses only CI-local PostgreSQL credentials defined inside the workflow.

Generated evidence is not committed. Evidence remains ignored by `db/validation/.gitignore` and is uploaded only as a CI workflow artifact.

The workflow does not promote disposable database state into repository source.

## 5. Documentation Review

`db/scripts/README.md` now includes:

- local pre-PR `Static` command
- local disposable `ControlledCodeLoad` command
- CI behavior summary
- disposable database warning
- evidence behavior
- no production/shared database warning

Existing local validation behavior remains intact.

## 6. Scope Discipline Review

This task did not modify:

- `db/state` SQL files
- controlled-code JSON source files
- generated controlled-code SQL files
- application source code
- migrations
- Atlas configuration
- sample transaction data

This task did not add:

- Slice 4
- new controlled-code families
- new controlled-code values

## 7. Local Validation Review

Local validation performed:

- `Invoke-PosDbChecks.ps1 -Mode Static`
- `git diff --check`
- workflow tab check

Results:

- Static mode passed.
- `git diff --check` passed.
- Workflow tab check passed.
- Python was not available for local YAML parsing.
- Ruby was not available for local YAML parsing.
- `actionlint` was not installed.

GitHub Actions execution itself was not run locally. Expected CI behavior is documented in the workflow and README: start PostgreSQL 16 service, install `postgresql-client`, run `Static`, run `ControlledCodeLoad`, and upload ignored validation evidence as an artifact.

## 8. Finding Counts

P0: 0

P1: 0

P2: 0

Editorial: 0

## 9. Final Recommendation

The CI/pre-PR validation integration is ready for PR review. It makes the current schema and controlled-code baseline harder to regress while preserving the repository source-of-truth and no-local-drift-promotion rules.
