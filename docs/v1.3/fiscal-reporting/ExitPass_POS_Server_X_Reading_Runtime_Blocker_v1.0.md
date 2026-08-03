# ExitPass POS Server X Reading Runtime Blocker v1.0

## 1. Outcome

Z-006B originally stopped before public endpoint activation because the Z-006A timestamp constraint rejected an interim X observation. Z-006B1 corrected that schema rule and merged it to `dev`; the blocker is now resolved.

- Original classification: `BLOCKED_BY_X_READING_AS_OF_SCHEMA`
- Current disposition: `RESOLVED_BY_Z_006B1`

## 2. Original Evidence

The frozen reporting contract permits an immutable X observation while a reporting period remains `OPEN`. The original `pos.x_z_reports.ck_x_z_reports__timestamps` constraint required `generated_at >= period_end_at` for both X and Z. PostgreSQL therefore rejected an X generated after `period_start_at` but before `period_end_at`.

Runtime workarounds were rejected. Z-006B did not falsify `generated_at`, shorten a governed period, create another report table, or hide an observation timestamp in JSON.

## 3. Z-006B1 Resolution

The merged `fix/fiscal-reporting-x-reading-as-of-constraint` change uses stable, family-enforced report-kind identities:

- X Reading `5dc3cc94-b3ab-5582-a598-e779871fc3e2`: `generated_at >= period_start_at`
- Z Reading `1c628bc2-49c3-53e8-ae83-2082bcf28467`: `generated_at >= period_end_at`

The correction preserved the shared snapshot table, one-Z-per-period uniqueness, committed-row immutability, parent retention, currency rules, reporting children, counter/GTA posture, BIR relationship, and Annex E relationship. Its direct PostgreSQL proof covered X at and within the period, X before-period rejection, Z before-end rejection, Z at/after-end acceptance, rebuild, upgrade, replay, drift, and no-state mutation.

## 4. Z-006B Resumption

On the corrected schema, Z-006B implements:

- `POST /v1/fiscal-reports/x-readings/`;
- `GET /v1/fiscal-reports/x-readings/{fiscalReportReference}`;
- separate server-derived generate and read permissions;
- configured Site POS Server and fiscal-identity scopes;
- deterministic `OPEN` period resolution without period mutation;
- PostgreSQL `REPEATABLE READ` aggregation;
- `pos-server-fiscal-report-request:sha256:v1` replay and conflict behavior;
- immutable request, scope, X snapshot, tender, discount, range, gap, operation-result, and audit persistence;
- stored-snapshot readback and no-fiscal-state-mutation proof.

The complete implementation record is `ExitPass_POS_Server_X_Reading_Runtime_Implementation_Note_v1.0.md`.

## 5. Readiness

- X Reading runtime and JSON readback: implemented by Z-006B.
- X Reading rendering, printing, reprinting, and export: not implemented.
- Z Reading runtime and fiscal-period close: not implemented and not authorized.
- Controlled UAT: not authorized.
- Production rollout: not authorized.
