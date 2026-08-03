# ExitPass POS Server Fiscal Reporting Contract and Schema Hardening Implementation Note v1.0

## 1. Scope

Z-006A resolves the Z-005 database hazards without implementing X or Z runtime. It adds versioned contract and period identity, semantic request identity, immutable first-class snapshots and breakdowns, BIR-to-Z and Annex-to-Z references, governed reporting codes, indexes, upgrade proof, and deterministic trigger inventory.

## 2. Compatibility and Upgrade

The repository remains state-SQL authoritative. New objects are additive. Existing report posture tables are transitioned in place only when they are empty. Upgrade SQL detects legacy rows before dropping generic context columns or applying mandatory snapshot columns and fails with an explicit operator action instead of inventing values. Clean rebuild and replay are idempotent.

No production migration is executed by this task. The upgrade fixture applies the `origin/dev` state into a disposable PostgreSQL 16 database and then applies the feature state.

## 3. Controlled Codes

New governed families cover report kind, scope, period status, output type/status, sequence-gap classification, tender classification, and discount classification. Report lifecycle adds committed, rejected, conflict, and unknown-commit-outcome values while retaining earlier lifecycle values for compatibility. Stable UUIDv5 identifiers follow the repository namespace.

Every hardened reporting FK has a family-specific UUID check in addition to its controlled-code FK. Wrong-family codes therefore fail at the database boundary.

The governed reporting families are:

- `fiscal_report_kind`: `x_reading`, `z_reading`, `bir_sales_summary`, `annex_e`;
- `fiscal_report_scope_type`: `site_pos_server_period`;
- `fiscal_reporting_period_status`: `open`, `closing`, `closed`;
- `fiscal_report_output_type`: `json_presentation`, `print_presentation`, `external_export_reference`;
- `fiscal_report_output_status`: `requested`, `processing`, `committed`, `failed`;
- `fiscal_sequence_gap_classification`: `voided_document`, `failed_issuance`, `reserved_not_issued`, `unexplained`;
- `fiscal_reporting_tender_classification`: `cash`, `card`, `digital_wallet`, `bank_transfer`, `other_non_cash`;
- `fiscal_reporting_discount_classification`: Senior Citizen statutory, PWD statutory, other statutory, VAT exemption adjustment, coupon, and promotional codes.

Together with the extended report status family, the repository-controlled inventory is 33 sets and 166 values. The generated SQL is repeatable and UUID-stable.

## 4. Immutability and Retention

Contract versions, X/Z snapshots, tender/discount/range/gap children, Z counter snapshots, BIR summaries, and Annex E snapshots reject UPDATE and DELETE. Closed periods reject mutation. Foreign keys use restrictive parent behavior; fiscal evidence is not silently cascaded away.

Requests and open/closing periods remain mutable for future lifecycle runtime. This task does not execute transitions.

## 5. Objects, Keys, and Indexes

New objects are the contract-version registry, reporting periods, tender breakdowns, discount breakdowns, fiscal-number ranges, classified sequence gaps, and Z counter/GTA snapshots. Existing request, scope, X/Z, BIR summary, Annex E, output-reference, and Site POS Server objects are hardened in place.

Database uniqueness enforces Site-scoped operation identity, Site period sequence, period scope/window, one snapshot per request, one Z per period, fiscal-identity report number, one classification row per report, one fiscal range per report/policy/series, one gap sequence per range, one Z counter snapshot per Z, one BIR request, one BIR profile per governing Z, and one Annex profile per governing Z.

Seventeen reporting-specific indexes support operation recovery, Site/business-date history, period/kind lookup, Z close identity, BIR-by-Z access, fiscal range reconciliation, child classifications, and prior-period counter lookup. Primary and unique constraint indexes are not duplicated.

## 6. BIR and Annex E Posture

A BIR summary must reference an existing immutable Z snapshot with matching period, Site POS Server, fiscal identity, and currency. It snapshots approved header-profile identifiers and non-production-safe machine/accreditation fields as first-class columns. A duplicate summary profile for the same Z is rejected.

Annex E stores privacy-safe metadata only and must reference an existing Z snapshot plus an approved contract/profile reference. Neither object stores an external file or uncontrolled JSON. Final BIR formulas and Annex E layouts remain compliance dependencies.

## 7. Trigger Inventory Correction

The inventory query now normalizes `information_schema.triggers` by schema, table, and trigger name. PostgreSQL exposes one row per trigger event, so each `UPDATE OR DELETE` trigger produces two information-schema rows but one actual trigger. The corrected inventory reports 11 triggers, rejects genuinely unexpected triggers, and no longer double-counts multi-event triggers.

Validation result helpers now retain ordered-dictionary mutations, and Docker `psql` output streams are drained asynchronously. These corrections make validation errors fail the command and prevent notice-heavy state replay from deadlocking.

Seven pre-existing overlength constraint identifiers on `origin/dev` are inventoried as warnings because PostgreSQL already truncates them. Any new overlength identifier remains a hard failure.

## 8. Validation Evidence

Validation used PostgreSQL 16.14 in disposable container `posserver-z006a-pg-20260803-a1` on loopback port `55321`. No persistent volume was used.

- clean rebuild and state replay: 62 manifest files, 61 tables;
- `origin/dev` upgrade and second feature replay: passed, with clean/upgrade schema fingerprints identical across 893 columns, 689 constraints, 145 indexes, and 11 triggers;
- controlled-code load: 33 sets, 166 values, zero orphans, zero missing/unexpected entries, stable UUID inventory;
- inventory/drift: 61 expected and actual tables, 3 functions, 11 normalized triggers, no sequence, no non-default extension, no missing reporting index or constraint;
- reporting proof: duplicate operation/X/Z/BIR identities, wrong-family codes, mixed currency, invalid period/range/counter/amounts, immutable updates/deletes, parent retention, rollback, trigger normalization, and privacy-column absence passed;
- unexpected-trigger proof: inventory failed with exactly the synthetic unexpected trigger and passed again after removal;
- Release build: zero warnings and zero errors;
- full tests: 312 passed, zero failed, zero skipped.

All SQL proof rows were rolled back. Disposable databases, container, temporary archive, and generated evidence are removed before handoff.

## 9. External Dependencies

The final BIR formula/layout, Annex E form/profile, POSLog mapping, refund/return/adjustment period treatment, service-charge classification, signature requirements, and external filenames remain explicit compliance dependencies. Future runtime must fail closed when one is required but unresolved.

## 10. Exclusions

No C# runtime, endpoint, renderer, printer, reprint, export generator, EJ writer, POSLog writer, report generation, fiscal-period close, controlled UAT, or production rollout is included.
