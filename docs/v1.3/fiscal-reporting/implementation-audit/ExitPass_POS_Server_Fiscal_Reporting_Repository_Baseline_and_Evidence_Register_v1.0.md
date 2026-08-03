# ExitPass POS Server Fiscal Reporting Repository Baseline and Evidence Register v1.0

## 1. Preflight

| Check | Result |
| --- | --- |
| Repository root | `D:/SourceCodes/ExitPass-PoSServer-Z-FiscalReportingAudit` |
| Branch | `docs/pos-server-fiscal-reporting-implementation-audit` |
| Starting status | Clean; no staged, modified, or untracked files |
| Starting HEAD | `a70117d7197b778eeb97d3e99ec84b4076c96075` |
| Refreshed `origin/dev` | `a70117d7197b778eeb97d3e99ec84b4076c96075` |
| Merge base | `a70117d7197b778eeb97d3e99ec84b4076c96075` |
| Divergence | `0 0` from `git rev-list --left-right --count HEAD...origin/dev` |
| Pre-change `git diff --check` | Passed with no output |

Pre-change inventory: Added None; Modified None; Renamed None; Moved None; Deleted None; Generated None; Intentionally untracked None; Unrelated pre-existing None.

## 2. Git Provenance

| Slice | Branch/PR evidence | Commit | Current interpretation |
| --- | --- | --- | --- |
| Fiscal report posture objects | PR #25, `db/slice-6-reports` | Merge `477bb03e9ad136ff6e895a0ac8b92ef538513da8`; implementation `15be914cea4f90963e1edff17ad0d3551737f06a` | Schema posture only; technical review explicitly excludes formulas, generation, close, counter mutation, layouts, and binaries. |
| EJ/export posture objects | PR #26, `db/slice-7-exports-ej-poslog` | Merge `a13604f0441acd317bb74b7778825be9be6580b5` | Metadata/reference posture only; technical review explicitly excludes payload generation, hash chaining, validation execution, and final POSLog mapping. |
| Z-001 baseline audit | PR #81 | Merge `195a5ae`; implementation `6bbe248` | Already identified report/export generation as unimplemented and production blocking. |
| Applied statutory facts schema/runtime | PR #83/#84 | Merge `7339929`; merge `a70117d` | Adds reliable Senior/PWD final applied facts, but no report aggregation or reporting APIs. |

## 3. Reporting Schema Inventory

| Area | Exact objects |
| --- | --- |
| Request and scope | `pos.fiscal_report_requests`, `pos.fiscal_report_scopes` |
| X/Z snapshot | `pos.x_z_reports` |
| BIR summary | `pos.bir_sales_summary_reports` |
| Annex E | `pos.annex_e_reports` |
| Output references | `pos.fiscal_report_output_refs` |
| EJ | `pos.electronic_journal_records` |
| Export | `pos.fiscal_export_requests`, `pos.fiscal_export_packages`, `pos.fiscal_export_package_items` |
| Export schema/validation | `pos.export_schema_profile_refs`, `pos.export_validation_results` |
| Supporting posture | `pos.fiscal_counter_states`, `pos.fiscal_state_snapshots`, `pos.fiscal_lock_states`, `pos.fiscal_sequence_gap_audit`, audit/recovery tables |
| Reprint posture | `pos.reprint_requests`, `pos.reprint_output_refs` |

All objects are present in `db/rebuild/pos_sql_apply_order.txt` and `db/validation/pos_expected_inventory.json`.

### 3.1 Exact Evidence Paths

- Report objects: `db/state/tables/pos.fiscal_report_requests.sql`, `pos.fiscal_report_scopes.sql`, `pos.x_z_reports.sql`, `pos.bir_sales_summary_reports.sql`, `pos.annex_e_reports.sql`, and `pos.fiscal_report_output_refs.sql` in the same directory.
- EJ/export objects: `db/state/tables/pos.electronic_journal_records.sql`, `pos.fiscal_export_requests.sql`, `pos.fiscal_export_packages.sql`, `pos.fiscal_export_package_items.sql`, `pos.export_schema_profile_refs.sql`, and `pos.export_validation_results.sql`.
- State/range support: `db/state/tables/pos.fiscal_counter_states.sql`, `pos.fiscal_state_snapshots.sql`, `pos.fiscal_lock_states.sql`, and `pos.fiscal_sequence_gap_audit.sql`.
- Reprint posture: `db/state/tables/pos.reprint_requests.sql` and `pos.reprint_output_refs.sql`.
- Reporting controlled-code sources: `db/reference-data/controlled-codes/source/families/fiscal_report_status.json` and `fiscal_export_status.json`.
- Design limits: `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Slice_6_SQL_Object_Technical_Review.md` and `ExitPass_POS_Server_Slice_7_SQL_Object_Technical_Review.md`.
- Runtime boundary: `src/ExitPass.PosServer.Api/Program.cs`, fiscal-document modules under `src/ExitPass.PosServer.*`, and the runtime/API/integration tests under `tests/`.

## 4. Production Runtime Inventory

Production C# is limited to these relevant executable areas:

- Fiscal document create, read, void, numbering, idempotency, and PostgreSQL persistence under `src/ExitPass.PosServer.*/*FiscalDocuments*`.
- Digital Sales Invoice JSON rendering and presentation.
- Fiscal Identity and Sales Invoice Header Profile administration.

Definitive negative evidence:

- No source filename under `src/` matches report, reading, Annex, journal, POSLog, export, reconciliation, or print.
- No source type/member contains XRead, ZRead, or Annex; a runtime test actively asserts this.
- No report repository/service is registered in `FiscalDocumentServiceCollectionExtensions.cs`.
- `Program.cs` maps only fiscal-document and header-profile administration endpoints.
- No report-focused test file, proof script, or machine-readable report contract exists.
- Fiscal-document integration tests assert that report tables stay empty during issuance.

## 5. Current API Inventory

| Route | Method | Current purpose |
| --- | --- | --- |
| `/v1/fiscal-documents/` | POST | Fiscal document issuance |
| `/v1/fiscal-documents/{id}` | GET | Fiscal document readback |
| `/v1/fiscal-documents/{id}/void` | POST | Fiscal void |
| `/v1/fiscal-documents/{id}/digital-sales-invoice` | GET | Digital SI render model |
| `/v1/fiscal-documents/{id}/digital-sales-invoice/presentation` | GET | Digital SI presentation |
| `/v1/admin/fiscal-identities/*` | POST/GET/PATCH | Secured Fiscal Identity administration |
| `/v1/admin/sales-invoice-header-profiles/*` | POST/GET/PATCH | Secured profile lifecycle/readiness/usage administration |

The design-only families `/v1/pos/reports/*` and `/v1/pos/exports/*` remain provisional and are not mapped.

## 6. Database Validation Evidence

| Evidence | Result |
| --- | --- |
| Docker client/server | `29.5.3` / `29.5.3` |
| PostgreSQL image/version | `postgres:16-alpine`, PostgreSQL `16.14` |
| Disposable container | `posserver-fiscal-report-audit-pg-20260803-7e4c` |
| Disposable database | `posserver_fiscal_reporting_audit_local_20260803_7e4c` |
| Host port | `55300` |
| Static validation | Passed |
| Mode All | Passed: rebuild, expected inventory, and drift |
| ControlledCodeLoad | Passed |
| Inventory | 54 expected tables, 54 actual; 994 catalog constraint rows; 94 indexes; no sequence; no non-default extension |
| Reporting rows after rebuild | Zero in X/Z, BIR summary, Annex E, EJ, report request, and export request tables |
| Cleanup | Evidence directories deleted; disposable container removed; no volume was created |

Catalog findings:

- Only `fiscal_report_status` and `fiscal_export_status` reporting/export code families are loaded.
- Reporting tables have primary-key indexes only, except the schema profile type/key/version unique index.
- No reporting table has an UPDATE/DELETE immutability trigger.
- Rollback-only proof result `2|300|2`: two X/Z rows for one request were accepted, a stored gross amount was changed to 300, and both rows used a report-status code as report kind. The transaction was rolled back and left no rows.
- Inventory/Drift evidence reported the one multi-event immutability trigger twice and also listed one occurrence as unexpected while status remained passed. Treat reporting-specific catalog validation as needing hardening.

## 7. Application Validation Evidence

| Command | Result |
| --- | --- |
| `dotnet restore` | Passed; seven projects restored |
| `dotnet build -c Release --no-restore` | Passed; 0 warnings, 0 errors |
| `dotnet test -c Release --no-restore --no-build` | Passed; 312 total, 312 passed, 0 failed, 0 skipped |

Test project results:

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Runtime.Tests | 152 | 0 | 0 |
| Api.Tests | 115 | 0 | 0 |
| Persistence.Postgres.Tests | 38 | 0 | 0 |
| Api.IntegrationTests | 7 | 0 | 0 |

No test exercises report generation, X/Z close, BIR summary generation, Annex E export, EJ generation, POSLog generation, report rendering, report printing, or report RBAC.

## 8. Approved Design Evidence

| Document | Relevant posture |
| --- | --- |
| `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` | Requires X/Z, counters/GTA, BIR summary, Annex E, EJ, POSLog, reports, exports, audit, and reconciliation. |
| `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` | Defines intended services and explicitly keeps X/Z scope, exact formats, and compliance details open. |
| `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` | Defines provisional report/export route families and candidate operations; not final DTOs or endpoints. |
| Slice 6 technical review | Confirms SQL is posture only and no generation, formulas, close, counter mutation, layout, or output is implemented. |
| Slice 7 technical review | Confirms SQL is posture only and no EJ chain, POSLog payload, export generator, validation engine, or schema mapping is implemented. |

## 9. Manual and UAT Evidence

- Existing Digital Sales Invoice preview approval is not report UAT evidence and explicitly excludes X/Z and Annex E.
- No controlled-UAT report fixture, signed X/Z output, BIR summary sample, Annex E sample, EJ export, POSLog export, printer evidence, close/recovery proof, or reconciliation signoff exists.
- Manual product walkthrough for this audit: not required.
- Future X/Z, report printing, export, and close tasks require significant backend/manual validation and controlled UAT after compliance decisions are frozen.
