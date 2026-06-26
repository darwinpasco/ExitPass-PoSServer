# ExitPass POS Server Physical DB Artifact Source Analysis

## Purpose

This source analysis identifies the approved inputs for future POS Server physical database artifacts and state-based object layout planning.

This is planning only. It does not create SQL, Atlas files, migrations, physical object files, seed data, validation scripts, rebuild scripts, drift scripts, or CI workflows.

## Sources Reviewed

| Source | Planning use |
| --- | --- |
| `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` | Approved business baseline for POS/Invoicing scope, Sales Invoice, fiscal reports, digital SI delivery, and authority boundaries. |
| `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` | Approved technical baseline for Site POS Server, fiscal lifecycle, counters, reporting, exports, audit, recovery, and security/privacy posture. |
| `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` | Approved API baseline for idempotency, status/error posture, Digital SI URL return, reports, exports, recovery, and trust boundaries. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0.md` | Approved logical database baseline and Physical Design Gate. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_Open_Questions_Resolution_Addendum.md` | Resolved defaults and remaining downstream/external confirmations. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0_Technical_Review.md` | Technical review confirmation of no P0/P1 findings. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0_Approval_Readiness_Review.md` | Approval-readiness confirmation for the logical baseline. |
| `docs/REPOSITORY_BOUNDARY.md` | Separate repository and Central PMS/POS Server authority boundary. |
| `docs/DOCUMENT_MANIFEST.md` | Documentation import and source path context. |
| `docs/references/External_POS_BIR_ARTS_References.md` | External/local BIR, POS, ARTS, and vendor reference inventory. |
| `D:\Docs\ExitPass\v1.2\ExitPass Database Design v1.2.docx` | Writing-pattern and database artifact discipline reference only. |

## Approved Logical Baseline Inputs

The approved Database Design establishes that POS Server database artifacts belong to `ExitPass-PoSServer`, store POS fiscal records, and only reference Central PMS authority records. Physical artifacts must not create POS-owned payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, or gate execution authority.

The approved logical areas that drive future artifacts are fiscal identity, channel registry, fiscal documents, fiscal lines, tender/tax/discount/totals, numbering/counters, idempotency/retry, Digital SI URL, QR boundary, reprints, adjustments, X/Z reads, BIR Sales Summary, Annex E, EJ, POSLog, JSON/export records, audit, recovery, security/privacy, retention, Central PMS references, and ARTS POSLog/BIR extension mapping.

## Physical Design Gate Inputs

| Gate item | Artifact planning impact |
| --- | --- |
| Target database engine confirmed | PostgreSQL is the recommended default; final version/features remain open. |
| Schema/domain decomposition approved | Logical domains must be approved before physical schemas exist. |
| Naming standards approved | Physical object naming rules must precede SQL files. |
| Object-level folder structure approved | Proposed `db/...` layout must be reviewed before creation. |
| State-based versioning workflow approved | Repository state, rebuilds, validations, and drift checks must be defined. |
| Rebuild, validation, and drift-check plans approved | Scripts are future artifacts, not created here. |
| Seed/reference data separation approved | Schema, seed, reference, sample, accreditation, and environment data must be separated. |
| Enum versus controlled-code strategy approved | Stable states and evolving classifications need deliberate handling. |
| Idempotency uniqueness strategy approved | Duplicate fiscal document prevention must be physically enforceable. |
| Fiscal numbering/counter strategy confirmed or placeholder approved | SI/adjustment sequence behavior must be confirmed before implementation. |
| Retention/partitioning strategy reviewed | Fiscal, audit, export, URL access, and recovery retention affect physical layout. |
| Digital SI URL security model reviewed | Token/access/lifecycle storage depends on Security/Privacy Review. |
| BIR/accreditation output/export expectations reviewed | Report/export metadata and samples must support accreditation evidence. |
| ARTS POSLog profile/schema mapping reviewed | ARTS remains an export reference, not replacement for BIR outputs. |
| Tamper-evident anchoring approach reviewed | Hash chain, external anchor, and recovery state storage need approval. |
| No local drift promotion confirmed | Live database drift cannot become baseline without reviewed repo artifacts. |

## State-Based Versioning Inputs

Future artifacts should use repository-owned database state as source of truth, per-object SQL files where practical, repeatable clean rebuilds, validation scripts, drift checks, reviewable PR diffs, seed/reference separation, CI-ready evidence, and no local drift promotion. Optional Atlas/state-based comparison may be introduced only after workflow review.

## BIR / ARTS / Accreditation Inputs

Physical artifact planning must preserve BIR Sales Invoice terminology, BIR-required printed/digital outputs, BIR Sales Summary and Annex E support, reprint labels/audit, ONLINE/OFFLINE as observability only, and ARTS POSLog 6.x as a structured export/schema reference only. Local/BIR extension mapping must preserve SI Number, ticket/plate, MIN/PTU, serial, site/branch, channel/terminal, Business Day Date, reset counter, Z-counter, GTA, Digital SI URL, parking timestamps, and audit references.

## Repository Boundary Inputs

| POS Server repo owns | Main ExitPass repo owns |
| --- | --- |
| POS Server database artifacts, validations, rebuild/drift plans, sample/accreditation support. | Central PMS, parking session state, site resolution, PaymentAttempt, PaymentConfirmation, payment finality, ExitAuthorization, WebPay/platform channels, payment orchestration, Operator Console. |

## Out of Scope

This source analysis does not create or approve SQL, DDL, physical schemas, final table/column/index/constraint/type/sequence/function/trigger/policy/view names, Atlas files, migrations, seed/reference/sample data, validation scripts, rebuild scripts, drift scripts, CI workflows, application code, or final BIR/accreditation package files.