# ExitPass POS Server Physical DB Artifact Validation Plan

## Purpose

This validation plan defines future validation categories and PR evidence for POS Server physical database artifacts and state-based object layout.

This is planning only. It does not create validation scripts, SQL, Atlas files, migrations, physical object files, CI workflows, or database schema.

## Validation Objectives

Future validation must prove that repository-owned database state rebuilds cleanly, matches the approved logical design, preserves the Central PMS/POS Server authority split, supports fiscal issuance/report/export/audit/recovery requirements, does not create POS-owned payment finality or ExitAuthorization, and produces reviewable PR evidence.

## Rebuild Validation

| Category | Future evidence |
| --- | --- |
| Clean database creation | Rebuild output showing clean target creation. |
| Object state application | Manifest/application log showing repository state applied. |
| Seed/reference load | Evidence that only approved seed/reference data loaded. |
| Repeatability | Rebuild works in clean local/CI environment. |
| No secrets | Output confirms no secrets or local external references embedded. |

## Schema and Object Inventory Validation

Future validation should inventory required schemas/domains, tables, views, functions/routines, triggers, types/enums, sequences, policies, extensions, and unexpected/missing objects. Inventory must reflect approved physical design and must not infer final objects from unreviewed live database state.

## Constraint and Index Validation

| Category | Future evidence |
| --- | --- |
| Primary keys | Approved primary keys exist. |
| Foreign/reference constraints | Approved internal references exist; Central PMS references follow approved strategy. |
| Unique constraints | Idempotency, fiscal numbering, and identity uniqueness rules exist where approved. |
| Check constraints | Status/range/classification rules exist where approved. |
| Indexes | Approved access-path and uniqueness-support indexes exist. |
| Naming | Constraint and index naming standards are followed. |

## Enum and Controlled-Code Validation

Validation should confirm that stable lifecycle states use the approved enum or controlled-code strategy, evolving classifications use controlled-code governance, BIR/report classifications are present where finalized, operational reason codes are governed, and historical values remain readable.

## Authority Boundary Validation

Future validation must explicitly prove physical artifacts do not create POS-owned authority for payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, gate execution, vendor PMS authority, or event/audit authority. POS/fiscal events and audit records remain evidence and observability only.

## Fiscal Identity and Channel Registry Validation

Validation should confirm support for Site POS Server identity, site/branch/business unit mapping, taxpayer information, Site POS Server and channel/terminal fiscal identity references, MIN/PTU/serial/software/supplier metadata, effective dating, configuration status, and change audit.

Channel registry validation should cover WebPay, APM, Cashier POS, EC Device / Continuity Terminal, operator-assisted, and future channel types; Site POS Server association; print/display/Digital SI URL/QR capabilities; ONLINE/OFFLINE health; active/inactive/degraded/continuity state; and channel/terminal status as child endpoint data only.

## Idempotency Validation

Future validation should confirm idempotency key, idempotency scope, semantic request identity or request hash, linked fiscal operation, replay result, conflict status, timeout, completion unknown, retry status, exception reference, retention, and no duplicate fiscal document issuance on retry.

## Fiscal Numbering and Counter Validation

Validation should confirm Sales Invoice sequence policy, adjustment sequence policy, reserved/issued/failed/abandoned states where approved, sequence-gap audit, reset counter, Z-counter, Grand Total Amount accumulator, previous GTA snapshot, previous reset counter snapshot, latest EJ hash, last fiscal event timestamp, and no resume from lower counters, lower GTA, earlier SI sequence, broken EJ hash continuity, or earlier event timestamp.

## Digital SI URL Lifecycle Validation

Validation should confirm Digital SI URL reference, token/access reference where approved, active/expired/revoked/blocked lifecycle states, issue and expiry timestamps, access audit where required, read-only customer access posture, privacy/data minimization, and no fiscal mutation through public URL access.

## Reprint and Adjustment Validation

| Area | Future validation target |
| --- | --- |
| Reprints | Original document/report/output linkage, reprint type, reason, actor/service, approval, timestamp, status/history, labels/timestamp metadata, and audit. |
| Adjustments | Original fiscal document linkage, adjustment type, reason, actor/service, approval, refund/reversal context reference, reconciliation reference, and audit. |
| Authority boundary | Adjustment records do not declare payment refund/reversal finality. |

## Report and Export Validation

Validation should confirm X-read/Z-read metadata, BIR Sales Summary / Annex E-1 minimum contents, Annex E-2 to E-5 support, EJ records, POSLog export metadata, JSON/POSLog schema/profile versioning, ARTS POSLog 6.x profile reference where accepted, local/BIR extension mapping references, export validation pending/passed/failed status, validation errors, and Print/PDF/JSON output mode metadata.

## Audit and Recovery Validation

Validation should cover SI issuance, Digital SI URL creation/access, reprints, void/refund/cancel/return, X-read, Z-read, BIR Sales Summary, Annex E, EJ/POSLog/export generation, export validation, fiscal reset, recovery continuity check, supervised recovery, fiscal identity changes, channel/terminal changes, RBAC/approval decisions, unauthorized fiscal actions, ONLINE/OFFLINE status changes where required, fiscal state snapshots, and tamper-evident references.

## Security and Privacy Validation

Future validation should confirm actor/service references, approval references, privileged operation audit, sensitive evidence separation or references, Digital SI URL access audit data minimization, privileged export access audit, and absence of final RBAC assumptions until Security/Privacy Review approves them.

## Drift-Check Validation

Drift evidence should include repository state hash or manifest version, target database identity/version, unexpected object report, missing object report, changed object report, constraint/index drift report, controlled-code/reference-data drift report, pass/fail result, and confirmation that drift is not promoted automatically.

## PR Evidence Checklist

| Evidence item | Required before merge |
| --- | --- |
| Rebuild success | Yes. |
| Validation success | Yes. |
| Drift-check result | Yes. |
| Generated schema inventory | Yes. |
| Generated constraint inventory | Yes. |
| Generated index inventory | Yes. |
| Seed/reference data validation | Yes, when changed. |
| Authority boundary checklist | Yes. |
| No POS-owned payment finality check | Yes. |
| No POS-owned ExitAuthorization check | Yes. |
| No untracked local database artifacts | Yes. |
| External reference files not copied | Yes. |
| BIR/accreditation impact note | Required when fiscal reports, exports, identity, or counters change. |
| Security/privacy impact note | Required when Digital SI URL, evidence, access audit, or privileged data changes. |

## Out of Scope

This validation plan does not create validation scripts, SQL queries, Atlas configuration, CI workflows, rebuild scripts, drift-check scripts, physical database objects, seed/reference data files, or accreditation outputs.