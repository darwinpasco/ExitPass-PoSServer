# ExitPass POS Server Controlled-Code Seed Decision Log

## 1. Purpose

This decision log records planning decisions for a future POS Server controlled-code seed/reference data package.

This task does not create seed data, SQL files, JSON/YAML source files, or generated artifacts.

## 2. Inherited Decisions

| Decision | Source / basis | Implication |
| --- | --- | --- |
| Controlled-code foundation exists in `pos.controlled_code_sets` and `pos.controlled_codes`. | Implemented Slice 1 SQL objects. | Future seed/reference data should load into these tables. |
| Repository SQL state remains source of truth. | DB validation/rebuild/drift package. | Seed/reference source must be repository-reviewed and not promoted from local drift. |
| Reference data must stay separate from schema SQL. | `db/README.md` and DB roadmap check. | Future values should live outside `db/state`. |
| Sample data is not approved. | Current physical DB and validation baselines. | Do not include fiscal documents, payments, reports, exports, audit, or recovery samples. |
| Central PMS authority remains external. | Approved physical object and gate baselines. | No controlled-code value may imply POS-owned payment or exit authority. |

## 3. Planning Decisions Resolved Now

| Decision | Rationale | Future implication |
| --- | --- | --- |
| Treat controlled-code seed data as baseline reference data, not sample data. | Code values support foreign-key interpretation; sample transactions would create operational facts. | Future seed tasks must reject transaction-like rows. |
| Plan all code families implied by slices 1 through 8. | The current schema has controlled-code references across all implemented object areas. | Future seed scope can be reviewed family-by-family. |
| Prefer JSON/YAML source with generated SQL later. | Structured source is easier to review, validate, diff, and generate repeatable SQL from. | Future implementation should define source schema and generator/load strategy. |
| Keep future generated SQL separate from `db/state`. | `db/state` contains schema objects only. | Seed SQL, if generated, should be placed in a future approved seed/reference-data location. |
| Require validation before actual seed data creation. | Controlled codes affect runtime behavior and authority boundaries. | Future seed task must include static and DB load validation. |

## 4. Deferred Decisions

| Deferred decision | Reason deferred | Target resolution |
| --- | --- | --- |
| Final source file format: JSON vs YAML. | Both are viable; team tooling preference is not finalized. | Decide before seed source files are created. |
| Exact code-set key naming convention. | Current schema supports flexible keys; final convention needs governance. | Define a canonical naming rule in the seed implementation task. |
| Final code values per family. | Many values require business, BIR/accounting, operations, Security/Privacy, or vendor review. | Approve values in family-specific review before insertion. |
| Whether generated SQL is committed. | The team may prefer source-only plus generated CI artifacts, or committed generated SQL. | Decide with validation/rebuild workflow owners. |
| Whether seed loading is idempotent SQL, loader script, or psql manifest entry. | Depends on selected source format and generation approach. | Decide in future implementation task. |
| Retention of inactive historical codes. | Historical readability is needed, but policy is not final. | Define controlled-code lifecycle governance. |

## 5. Non-Decisions

This package does not decide or create:

- seed SQL
- JSON/YAML source files
- generated SQL
- concrete code values
- sample data
- schema changes
- migrations
- Atlas files
- CI workflows
- source code
- BIR/accreditation package values
- ARTS POSLog profile mapping values
- security/privacy classification values

## 6. Recommended Next Decision

Before implementation, decide the controlled-code source format and governance model:

- source format: JSON or YAML
- canonical `code_set_key` naming
- required metadata per code set and code value
- approval owners per family
- load/generation strategy
- validation evidence requirements
