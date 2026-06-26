# ExitPass POS Server Physical DB Artifact Decision Log

## Purpose

This decision log separates inherited decisions, safe planning defaults, provisional recommendations, non-decisions, and decisions required before physical database artifacts are created.

This is planning only and does not create SQL, Atlas files, migrations, physical database object files, or database schema.

## Approved Decisions Inherited From Baselines

| ID | Decision | Source | Physical artifact implication |
| --- | --- | --- | --- |
| PDB-D001 | POS Server is a separate system and repository. | Repository Boundary; approved Database Design | Physical DB artifacts belong in `ExitPass-PoSServer`, not main `ExitPass`. |
| PDB-D002 | Central PMS owns parking session state, site resolution, PaymentAttempt, PaymentConfirmation, payment finality, and ExitAuthorization. | Approved BRD/System Design/API Contract/Database Design | POS Server DB stores references only and must not own these platform authority records. |
| PDB-D003 | POS Server owns fiscal issuance and fiscal records for the resolved Site. | Approved baselines | Physical artifacts center on fiscal records, counters, reports, exports, audit, retention, and recovery continuity. |
| PDB-D004 | POS Server must not issue ExitAuthorization or declare payment finality. | Approved authority model | No POS-owned ExitAuthorization or payment finality lifecycle object is allowed. |
| PDB-D005 | Channels/terminals are children of the Site POS Server. | Approved BRD/System Design/Database Design | Channel registry artifacts model child endpoints, not independent fiscal authorities. |
| PDB-D006 | POS Server returns Digital SI URL; channels/terminals generate QR where supported. | Approved System Design/API Contract/Database Design | Store URL lifecycle and QR capability metadata, not required QR image binaries as fiscal records. |
| PDB-D007 | Offline fiscal issuance is disabled by default. | Approved System Design/API Contract/Database Design | ONLINE/OFFLINE is observability only and must not imply offline issuance approval. |
| PDB-D008 | ARTS POSLog 6.x is a structured export reference only. | BIR/ARTS impact review; approved Database Design | Export artifacts may support profile/version/mapping references while preserving BIR outputs. |
| PDB-D009 | State-based database versioning is the future posture. | Approved Database Design; Repository Boundary | Repository-owned state, rebuild validation, drift checks, and no local drift promotion are required. |

## Safe Planning Defaults

| ID | Default | Rationale | Status |
| --- | --- | --- | --- |
| PDB-SD001 | PostgreSQL is the recommended default target engine. | Strong consistency, mature constraints/indexes, JSON support, schemas, and CI-friendly rebuilds. | Planning default; final version/features pending. |
| PDB-SD002 | Use per-object SQL files where practical. | Improves reviewability and state-based drift comparison. | Planning default. |
| PDB-SD003 | Separate schema state from seed, reference, sample, accreditation, environment data, and secrets. | Prevents data/policy confusion. | Planning default. |
| PDB-SD004 | Use controlled-code governance for evolving classifications. | BIR/report classifications, reasons, adjustment types, and operational codes may evolve. | Planning default. |
| PDB-SD005 | Use enums only for stable lifecycle states if PostgreSQL strategy approves them. | Enums are useful but harder to evolve. | Pending final strategy. |
| PDB-SD006 | Use explicit manifests or deterministic ordering for rebuilds. | Dependencies must be reviewable and repeatable. | Planning default. |
| PDB-SD007 | Use drift checks as evidence, not automatic mutation. | Keeps repository artifacts as source of truth. | Planning default. |
| PDB-SD008 | DTOs must not mirror database tables directly. | Preserves API/database separation. | Approved design principle. |

## Provisional Recommendations

| ID | Recommendation | Why provisional | Required confirmation |
| --- | --- | --- | --- |
| PDB-PR001 | Proposed future root: `db/`. | Folder is not created in this task. | Object-level folder structure approval. |
| PDB-PR002 | Proposed state folders: `db/state/schemas/`, `tables/`, `views/`, `functions/`, `triggers/`, `types/`, `sequences/`, `policies/`, `extensions/`. | Physical artifact structure must be reviewed before creation. | Physical Design Gate approval. |
| PDB-PR003 | Proposed supporting folders: `db/seeds/`, `db/reference-data/`, `db/validation/`, `db/rebuild/`, `db/drift/`, `db/scripts/`, `db/samples/`, `db/accreditation/`. | Scripts/data are not created yet. | Seed/reference and validation/rebuild plan approval. |
| PDB-PR004 | Group physical schemas by logical domains from the approved Database Design. | Schema names remain provisional. | Schema/domain decomposition approval. |
| PDB-PR005 | Prefer hand-authored or explicitly reviewed state files unless generation is approved. | Generated artifacts must remain deterministic and reviewable. | Engineering Pack / DB workflow approval. |
| PDB-PR006 | Consider Atlas/state comparison only after workflow review. | Tooling must fit CI and review practice. | Operations/Engineering approval. |

## Decisions Required Before Physical Artifact Creation

| ID | Required decision | Owner/dependency | Blocks physical artifacts |
| --- | --- | --- | --- |
| PDB-RD001 | Final PostgreSQL version, edition, hosting assumptions, and required extensions. | Engineering/Operations/Security | Yes. |
| PDB-RD002 | Physical schema/domain decomposition and naming standards. | Physical DB Design | Yes. |
| PDB-RD003 | Object-level folder structure and file naming conventions. | Physical DB Design / Engineering Pack | Yes. |
| PDB-RD004 | Enum versus controlled-code strategy. | Physical DB Design / BIR/accounting / Engineering | Yes. |
| PDB-RD005 | Idempotency uniqueness and semantic request identity strategy. | API detail / Physical DB Design | Yes. |
| PDB-RD006 | Fiscal numbering/counter placeholder policy or confirmed BIR/accounting strategy. | BIR/accounting | Yes for production artifacts. |
| PDB-RD007 | Digital SI URL security/token/access/expiry model. | Security/Privacy | Yes for URL artifacts. |
| PDB-RD008 | Retention/partitioning strategy. | BIR/accounting / Security/Privacy / Operations | Yes. |
| PDB-RD009 | ARTS POSLog profile/schema mapping and validation posture. | BIR/accreditation / Engineering Pack | Yes for export validation artifacts. |
| PDB-RD010 | Tamper-evident anchoring and recovery mechanism. | Security/Engineering/Operations | Yes for recovery/anchor artifacts. |
| PDB-RD011 | Rebuild, validation, drift-check, and PR evidence workflow. | Engineering/CI/CD | Yes. |

## Non-Decisions

This planning package does not decide or create SQL DDL, physical table/column/index/constraint/schema names, enum implementation, Atlas/state files, migrations, seed/reference/sample data, validation scripts, rebuild scripts, drift scripts, CI workflows, endpoint DTOs, event payloads, final RBAC matrix, final BIR/accreditation package, or offline fiscal issuance approval.