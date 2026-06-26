# ExitPass POS Server Physical DB Artifact Plan v1.0

## 1. Document Control

### Document Information

| Field | Value |
| --- | --- |
| Document title | ExitPass POS Server Physical DB Artifact Plan v1.0 |
| Version | v1.0 |
| Repository | `ExitPass-PoSServer` |
| Product scope | POS Server physical database artifact and state-based object layout planning for ExitPass v1.3 |
| Status | Draft for review |
| Output format | Markdown only |
| Approved BRD baseline | `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` |
| Approved System Design baseline | `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` |
| Approved API Contract baseline | `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` |
| Approved Database Design baseline | `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0.md` |
| Physical artifact status | No SQL, Atlas, migrations, physical object files, `db/` folder, seed data, validation scripts, rebuild scripts, drift scripts, or CI workflows are created by this draft. |

### Version History

| Version | Status | Notes |
| --- | --- | --- |
| v1.0 | Draft for review | First POS Server Physical DB Artifact Plan draft based on the approved logical Database Design and physical artifact planning package. |

### Baseline Scope Statement

This document plans the future repository layout, state-based versioning workflow, rebuild strategy, validation strategy, drift-check approach, and PR evidence discipline for POS Server physical database artifacts. It does not create actual physical artifacts.

## 2. Purpose and Scope

### Purpose

This plan defines how future POS Server physical database artifacts should be organized, governed, rebuilt, validated, drift-checked, and reviewed in the `ExitPass-PoSServer` repository.

The POS Server database is the persistence layer of the POS Server system. It remains inside `ExitPass-PoSServer` at this stage. A separate `ExitPass-PoSServer-Db` repository is not created by this plan and may be reconsidered only if database releases become independently owned, independently versioned, or compliance requires separate change control.

### In Scope

- Future repository DB artifact layout.
- State-based versioning workflow.
- Object-level file strategy.
- Naming standards plan.
- Rebuild strategy.
- Drift-check strategy.
- Validation strategy.
- Seed/reference separation.
- CI/PR evidence expectations.
- Physical Design Gate.
- Open questions and downstream dependencies.

### Out of Scope

- Actual SQL or DDL.
- Actual Atlas files.
- Actual migrations.
- Actual `db/` folders.
- Physical database object files.
- Database schema changes.
- Implementation code.
- CI workflow files.
- Sample or accreditation outputs.
- DOCX files or diagrams.

## 3. Approved Baseline References

| Reference | Role |
| --- | --- |
| `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` | Approved business baseline. |
| `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` | Approved POS Server architecture baseline. |
| `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` | Approved API contract baseline. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0.md` | Approved logical database design baseline. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_Open_Questions_Resolution_Addendum.md` | Logical default and open-question classification. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0_Technical_Review.md` | Technical review of logical database design. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0_Approval_Readiness_Review.md` | Approval-readiness review of logical database design. |
| `docs/REPOSITORY_BOUNDARY.md` | Repository and authority boundary. |
| `docs/DOCUMENT_MANIFEST.md` | Documentation baseline manifest. |
| `docs/references/External_POS_BIR_ARTS_References.md` | External/local source inventory. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Source_Analysis.md` | Physical artifact source analysis. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Decision_Log.md` | Physical artifact planning decisions and non-decisions. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Open_Questions.md` | Physical artifact open questions. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Layout_Plan.md` | Proposed layout and state-file strategy. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Validation_Plan.md` | Validation and PR evidence planning. |
| `D:\Docs\ExitPass\v1.2\ExitPass Database Design v1.2.docx` | Writing-pattern and artifact-discipline reference only. |

## 4. Physical Design Gate

Physical artifacts must not be created until the gate items below are confirmed or explicitly approved as placeholder policies.

| Gate item | Current status | Required decision / confirmation | Owner / dependency | Blocks physical artifact creation |
| --- | --- | --- | --- | --- |
| Target database engine confirmed | PostgreSQL recommended. | Confirm PostgreSQL version, edition, extensions, hosting/runtime assumptions. | Engineering / Operations / Security | Yes |
| Schema/domain decomposition approved | Candidate domains identified. | Approve physical schema/domain grouping and boundaries. | Physical DB Design | Yes |
| Naming standards approved | Naming categories planned. | Approve naming rules for all physical object and script families. | Physical DB Design / Engineering | Yes |
| Object-level folder structure approved | Proposed `db/...` layout documented only. | Approve folder layout and file strategy. | Physical DB Design / Engineering | Yes |
| State-based versioning workflow approved | Repository-owned state planned. | Approve state workflow and source-of-truth rules. | Engineering / CI/CD | Yes |
| Rebuild script plan approved | Workflow planned only. | Approve rebuild script approach and execution environments. | Engineering / Operations | Yes |
| Drift-check plan approved | Drift-check categories planned. | Approve drift tool/process and evidence rules. | Engineering / CI/CD | Yes |
| Validation script plan approved | Validation categories planned. | Approve validation tool/process and evidence output. | Engineering / QA | Yes |
| Seed/reference data separation approved | Separation model planned. | Approve seed, reference, sample, accreditation, secret, and external file rules. | Engineering / Operations / Compliance | Yes |
| Enum versus controlled-code strategy approved | Strategy pending. | Decide enum vs controlled-code rules. | Physical DB Design / BIR/accounting | Yes |
| Idempotency uniqueness strategy approved | Logical need approved. | Decide uniqueness and semantic request identity enforcement. | API detail / Physical DB Design | Yes |
| Fiscal numbering/counter strategy confirmed or placeholder policy approved | Logical defaults approved. | Confirm BIR/accounting strategy or approve placeholder policy. | BIR/accounting | Yes |
| Retention/partitioning strategy reviewed | Categories identified. | Confirm retention periods and partitioning posture. | BIR/accounting / Security / Operations | Yes |
| Digital SI URL security model reviewed | Default posture approved logically. | Confirm token/access/expiry/authentication and audit model. | Security/Privacy | Yes |
| BIR/accreditation output/export expectations reviewed | Output families identified. | Confirm expected reports, layouts, samples, and package evidence. | BIR/accreditation / Engineering | Yes |
| ARTS POSLog profile/schema mapping reviewed | Default posture approved. | Confirm accepted ARTS profile, local/BIR mappings, and validation. | BIR/accreditation / Engineering | Yes |
| Tamper-evident anchoring approach reviewed | Default posture approved. | Confirm hash chain, external anchor, and recovery mechanism. | Security / Engineering / Operations | Yes |
| No local drift promotion rule confirmed | Approved posture. | Confirm local drift must become reviewed repository artifact changes. | Engineering / CI/CD | Yes |

## 5. Target Database Engine

The recommended default target database engine is PostgreSQL.

PostgreSQL is recommended because it provides mature transactional behavior, schemas, constraints, indexes, JSON/JSONB support, views, functions/routines, sequences, extension support, and tooling compatibility for rebuild and validation workflows.

Before physical artifact creation, the team must confirm:

- PostgreSQL version.
- Edition or managed-service constraints.
- Required extensions.
- Collation, timezone, and locale assumptions.
- JSON/JSONB usage posture.
- Function/routine and trigger policy.
- Sequence behavior and fiscal numbering support.
- Validation and drift-check tooling compatibility.

This plan does not create database engine configuration.

## 6. Repository DB Artifact Layout

All paths below are proposed only. This draft does not create these folders.

| Proposed path | Purpose | When created | Notes |
| --- | --- | --- | --- |
| `db/README.md` | Entry point for DB artifact rules and commands. | After layout approval. | Should explain authority boundary and state-based workflow. |
| `db/state/` | Repository-owned desired database state. | After physical layout approval. | Future source of truth. |
| `db/state/schemas/` | Schema/domain definitions. | After schema decomposition approval. | No final schema names yet. |
| `db/state/tables/` | Table definitions, one file per table where practical. | After physical object design. | No table files in this task. |
| `db/state/views/` | View definitions. | After view strategy approval. | For report/read models where approved. |
| `db/state/functions/` | Function/routine definitions. | After routine policy approval. | For approved database routines only. |
| `db/state/triggers/` | Trigger definitions. | After trigger policy approval. | Use only where justified. |
| `db/state/types/` | Enums, domains, composite types, or equivalent approved types. | After enum/type strategy approval. | Controlled-code tables may be preferred for evolving values. |
| `db/state/sequences/` | Fiscal and technical sequences where approved. | After numbering strategy approval. | Fiscal numbering needs BIR/accounting confirmation or placeholder policy. |
| `db/state/policies/` | Security policies, if approved. | After security/privacy and DB policy review. | May include row-level security if selected. |
| `db/state/extensions/` | Required PostgreSQL extension declarations. | After target engine and extension approval. | No extensions approved by this draft. |
| `db/seeds/` | Environment-neutral seed data. | After seed policy approval. | Keep separate from object state. |
| `db/reference-data/` | Controlled code sets and reference values. | After controlled-code strategy approval. | Must preserve historical readability. |
| `db/validation/` | Validation scripts and evidence generators. | After validation plan approval. | No scripts in this task. |
| `db/rebuild/` | Clean rebuild scripts and manifests. | After rebuild plan approval. | No scripts in this task. |
| `db/drift/` | Drift-check scripts/configuration. | After drift plan approval. | No tools/config in this task. |
| `db/scripts/` | Utilities outside rebuild/validation/drift. | After script policy approval. | Avoid dumping ad hoc scripts. |
| `db/samples/` | Synthetic or approved sample data/output support. | After sample data policy approval. | Do not copy external source files. |
| `db/accreditation/` | Future accreditation support generated from approved sources. | After accreditation package policy approval. | Do not copy BIR/ARTS/vendor files without policy approval. |

## 7. Schema / Domain Decomposition

Final physical schema names remain provisional. Candidate domains are:

| Candidate domain | Covers | Boundary rule |
| --- | --- | --- |
| Fiscal identity / boundary | Site POS Server, site/branch/business unit, taxpayer, MIN/PTU/serial/software/supplier metadata. | POS Server fiscal identity only. |
| Channel registry | WebPay, APM, Cashier POS, EC/continuity, operator-assisted, future channels, capabilities, ONLINE/OFFLINE health. | Child endpoints, not independent fiscal authorities. |
| Fiscal documents | Sales Invoice and adjustment document headers, statuses, Central PMS references, original document linkage. | Canonical fiscal records owned by POS Server. |
| Fiscal details | Fiscal lines, tenders, tax, discounts, totals, VAT privilege/exemption support. | Explicit fiscal detail for reports and exports. |
| Numbering/counters | SI sequence, adjustment sequence, reset counter, Z-counter, GTA, sequence allocation/gaps. | Requires BIR/accounting confirmation or placeholder policy. |
| Operation safety / idempotency | Idempotency key, semantic request identity, replay result, timeout, completion unknown, retry state. | Prevent duplicate fiscal documents. |
| Digital delivery | Digital SI URL lifecycle, access audit, QR capability metadata. | POS Server stores URL state, not required QR image binaries. |
| Reports | X-read, Z-read, BIR Sales Summary, Annex E, report request/status/output references. | BIR outputs remain required. |
| Exports | EJ, POSLog, JSON, export package metadata, schema/profile versions, validation status/errors. | ARTS is an export reference only. |
| Audit | Fiscal audit records, actor/service identity, approvals, unauthorized actions, configuration changes. | Evidence only, not payment or exit authority. |
| Recovery/continuity | Fiscal state snapshots, hash/reference chain, external anchors, recovery requests, block/resume status. | Must prevent rollback and stale recovery. |
| Security/privacy references | Actor references, role references, evidence references, restricted access/audit references. | Final RBAC/evidence model downstream. |
| Configuration/controlled codes | Reason codes, statuses, classifications, report/export codes. | Controlled-code governance for evolving values. |
| Events/outbox if approved later | POS/fiscal event publication references. | Events are audit/integration/observability only. |
| Integration references | Central PMS and vendor synchronization/context references. | References only; no ownership transfer. |

## 8. Naming Standards

Final physical names are open. Future naming standards shall cover:

| Artifact type | Planned rule |
| --- | --- |
| Schema names | Lowercase, domain-oriented, stable, and aligned with approved decomposition. |
| Table names | One approved singular/plural convention used consistently. |
| Column names | Lowercase, explicit, business-readable; use approved acronyms only where clear. |
| Primary keys | Consistent object-specific key naming. |
| Foreign keys | Identify referencing object and referenced object. |
| Unique constraints | Identify the business uniqueness rule, especially idempotency and fiscal numbering. |
| Check constraints | Identify the validated business rule, status rule, range, or classification. |
| Indexes | Identify target object and access-path purpose. |
| Enum types | Use only if stable lifecycle states are approved for enum use. |
| Controlled code sets | Use for evolving classifications, reason codes, BIR/accreditation codes, and operational reasons. |
| Functions/routines | Use a consistent domain-action pattern after routine policy approval. |
| Triggers | Identify timing, event, and target object. |
| Sequences | Identify fiscal scope and document family. |
| Views | Identify report/read-model purpose. |
| Seed/reference data files | Identify data category and load order where needed. |
| Validation scripts | Identify validation category and sequence. |
| Rebuild scripts | Identify rebuild workflow step and environment neutrality. |
| Drift scripts | Identify comparison target and evidence output. |

## 9. Object-Level State File Strategy

Repository state is the future source of truth. Object files should be per object where practical:

| Object family | Strategy |
| --- | --- |
| Schemas/extensions/types/sequences | One file per object or approved manifest where practical. |
| Tables | One file per table where practical. |
| Views | One file per view. |
| Functions/routines | One file per routine. |
| Triggers | One file per trigger. |
| Policies | One file per policy where applicable. |
| Constraints/indexes | Inline or separate files by approved convention, but always reviewable. |
| Seed/reference data | Separate from object DDL. |

A manifest or deterministic ordering strategy must define rebuild order. The preferred default is an explicit manifest with reviewable order. Generated artifacts are not allowed unless the generator is tracked, deterministic, reviewable, and validated against the approved authority boundary.

## 10. Rebuild Strategy

Future clean rebuild workflow:

1. Create a clean target database.
2. Apply approved extensions, types, and schemas.
3. Apply tables and constraints.
4. Apply views, functions, triggers, and policies.
5. Load approved seed and reference data.
6. Run validation scripts.
7. Run drift check.
8. Capture evidence output.

The workflow must support local development, CI, and clean-environment rebuilds. It must not embed secrets, local external reference paths, or unapproved sample data.

## 11. Drift Check Strategy

Repository state is the source of truth. Future drift checks shall compare live/test database state to repository state and classify:

- Unexpected objects.
- Missing objects.
- Changed objects.
- Constraint drift.
- Index drift.
- Type/enum/controlled-code drift.
- Seed/reference data drift where applicable.

Drift reports are evidence. Drift must not be promoted automatically. Accepted drift must be expressed as a reviewed repository PR. Optional Atlas/state-based comparison may be used only after workflow review.

## 12. Validation Strategy

Future validation categories:

| Category | Purpose |
| --- | --- |
| Rebuild validation | Prove a clean database can be created from repository state. |
| Schema/object inventory validation | Prove required schemas, tables, views, routines, triggers, types, sequences, policies, and extensions exist. |
| Constraint/index validation | Prove approved keys, uniqueness, checks, and indexes exist. |
| Enum/controlled-code validation | Prove approved state/code strategy and values. |
| Authority boundary validation | Prove no POS-owned payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, or gate authority exists. |
| Fiscal identity/channel registry validation | Prove identity and channel models support approved fiscal boundary. |
| Idempotency validation | Prove duplicate fiscal documents are prevented conceptually and physically. |
| Fiscal numbering/counter validation | Prove SI/adjustment sequences, reset/Z counters, GTA, EJ hash, and continuity state. |
| Digital SI URL lifecycle validation | Prove URL lifecycle, access audit, read-only posture, and privacy boundaries. |
| Reprint/adjustment validation | Prove original linkage, metadata, approvals, labels, and audit support. |
| Report/export validation | Prove X/Z, BIR Sales Summary, Annex E, EJ, POSLog, JSON, schema/profile version, and validation status support. |
| Audit/recovery validation | Prove fiscal audit and recovery continuity support. |
| Security/privacy validation | Prove privileged access, evidence references, and data minimization posture. |
| Drift-check validation | Prove live/test state matches repository state or produces reviewable drift report. |

## 13. Seed and Reference Data Strategy

| Data category | Treatment |
| --- | --- |
| Schema/object definitions | Future `db/state/`; source of truth. |
| Seed data | Future `db/seeds/`; minimal and environment-neutral. |
| Reference data | Future `db/reference-data/`; controlled code sets and approved values. |
| Controlled code sets | Govern evolving classifications and reason/status codes. |
| Sample/accreditation data | Future `db/samples/` or `db/accreditation/`; synthetic or approved only. |
| Test fixtures | Must not be mistaken for production/reference data. |
| Environment-specific data | Not committed unless explicitly approved. |
| Secrets | Must not be committed. |
| External BIR/ARTS/vendor references | Must not be committed unless licensing, ownership, size, and repository policy are confirmed. |

## 14. Enum vs Controlled-Code Strategy

Stable lifecycle states may use PostgreSQL enums or controlled state tables, pending PostgreSQL strategy approval. Evolving classifications should use controlled code sets. BIR/report classifications and operational reason codes likely require controlled-code governance. Historical readability must be preserved when codes change.

Final enum versus controlled-code decisions are required before physical schema work.

## 15. Idempotency and Fiscal Numbering Strategy

Future artifacts must support:

- Idempotency keys and scope.
- Semantic request identity.
- Retry, timeout, and completion-unknown handling.
- SI sequence policy.
- Adjustment sequence policy.
- Reserved, issued, failed, and abandoned allocation states where approved.
- Sequence-gap audit.
- Duplicate fiscal document prevention.
- BIR/accounting confirmation or placeholder policy for final numbering and gap treatment.

## 16. Digital SI URL Security Gate

Future artifacts must support token/access reference, active/expired/revoked/blocked lifecycle, issue/expiry timestamps, access audit where required, privacy/data minimization, read-only customer view, and no fiscal mutation through URL access.

Security/Privacy Review must confirm the final token/access/expiry/authentication model before URL-related physical artifacts are finalized.

## 17. Reports, EJ, POSLog, JSON, and Accreditation Outputs

Future artifacts must support report metadata, generated output references, Print/PDF/JSON modes, EJ, POSLog, ARTS POSLog 6.x profile references, local/BIR extension mapping, validation status/errors, sample output data, and accreditation package support.

Philippine BIR outputs remain required. ARTS POSLog is a structured export/schema reference only and does not replace BIR fiscal document/report requirements.

## 18. Tamper-Evident Recovery and Anchoring

Future artifacts must support latest fiscal state, previous fiscal state, counters, GTA, latest EJ hash, last fiscal event timestamp, hash chaining where practical, external anchor reference if used, supervised recovery, and recovery block/resume status.

Security and Engineering must confirm the final anchoring and recovery mechanism before physical artifacts are finalized.

## 19. CI / PR Review Evidence

Future DB artifact PRs should include:

| Evidence | Required |
| --- | --- |
| Rebuild success | Yes |
| Validation success | Yes |
| Drift-check result | Yes |
| Schema inventory | Yes |
| Constraint inventory | Yes |
| Index inventory | Yes |
| Seed/reference data validation | Yes, when changed |
| Authority boundary checklist | Yes |
| No POS-owned payment finality check | Yes |
| No POS-owned ExitAuthorization check | Yes |
| No untracked local DB artifacts | Yes |
| External reference files not copied | Yes |
| BIR/accreditation impact note | Required when fiscal outputs, exports, identity, or counters change |
| Security/Privacy impact note | Required when Digital SI URL, evidence, access audit, or privileged data changes |

## 20. Open Questions

### BIR / Accounting

| Question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- |
| Final MIN/PTU/serial/software/supplier assignment | Flexible Site POS Server plus channel/terminal model. | Yes for final identity rules | Yes | May block accreditation-ready implementation | Yes |
| Exact BIR-approved Sales Invoice numbering format | Site POS Server-scoped configurable sequence policy. | Yes | Yes unless placeholder approved | Yes | Yes |
| Exact adjustment numbering and document families | Configurable per BIR-confirmed family. | Yes | Yes unless placeholder approved | Yes | Yes |
| Sequence gaps, failed issuance, abandoned issuance | Conservative audit-first default. | Yes | Yes unless placeholder approved | Yes | Yes |
| X-read/Z-read aggregation scopes | Site POS Server primary with optional terminal/channel/cashier/session dimensions. | May block report keys | May block report objects | May block fiscal close | Yes |
| Final VAT/tax treatment | Flexible fiscal classifications. | No if flexible | No for base objects | Yes | Yes |
| Diplomat VAT wording/evidence/reporting/retention | Active VAT privilege/exemption with evidence references. | May block evidence/retention | May block artifacts | Yes | Yes |

### Security / Privacy

| Question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- |
| Digital SI URL token/access/expiry/authentication | Opaque tokenized URL, read-only view, minimum exposure. | Yes | Yes | Yes | May block evidence |
| Digital SI URL access audit retention | Store access audit where required. | Yes | May block artifacts | Yes | May block privacy evidence |
| Final RBAC matrix | Baseline roles identified. | No if references flexible | May block permission objects | Yes | May block evidence |
| Tamper-evident anchoring | Hash chain/checkpoint default posture. | Yes | Yes | Yes | May block recovery evidence |

### Physical DB / Engineering / Operations / CI / Vendor

| Question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- |
| PostgreSQL version/features/extensions | PostgreSQL recommended. | Yes | Yes | Yes | No direct block |
| Schema/domain decomposition | Candidate domains identified. | Yes | Yes | Yes | No direct block |
| Naming standards | Categories planned. | Yes | Yes | Yes | No direct block |
| Folder layout and manifest ordering | Proposed `db/...` layout. | Yes | Yes | Yes | No direct block |
| Enum vs controlled-code strategy | Pending. | Yes | Yes | Yes | May affect reports |
| Retention/partitioning | Categories identified. | Yes | Yes | Yes | Yes |
| Rebuild/validation/drift tooling | Planned only. | No | Yes for scripts | Yes for CI | May affect evidence |
| Atlas or other comparison tool | Optional after review. | No | May block drift tooling | May block CI | No direct block |
| Report/export layouts and file names | Output families supported. | May block metadata | May block sample/export objects | Yes | Yes |
| ARTS POSLog profile/mapping | Default reference posture. | May block export profile storage | May block validation | Yes | Yes |
| Supplier/accreditation metadata | Flexible model. | May block identity fields | May block artifacts | Yes | Yes |

## 21. Risks and Mitigations

| Risk | Why it matters | Mitigation |
| --- | --- | --- |
| Authority leakage | Physical object names or relationships could imply POS ownership of payment finality or ExitAuthorization. | Use authority-boundary validation and naming review. |
| Premature physical design | SQL could lock in unresolved BIR/security decisions. | Enforce Physical Design Gate before artifacts. |
| Local drift promotion | Unreviewed live DB state could become baseline. | Require reviewed repository state changes and drift evidence. |
| Incomplete fiscal numbering policy | SI/adjustment sequence behavior could fail BIR audit. | Require BIR/accounting confirmation or approved placeholder. |
| Incomplete idempotency uniqueness | Retries could produce duplicate fiscal documents. | Require idempotency uniqueness strategy and validation. |
| Digital SI URL privacy exposure | Public access could expose unnecessary sensitive data. | Require Security/Privacy Review and data minimization checks. |
| ARTS POSLog replacing BIR outputs | Export alignment could weaken local compliance. | Treat ARTS as reference only and preserve BIR outputs. |
| Offline fiscal issuance implication | ONLINE/OFFLINE fields could be misread as approval for offline issuance. | Validate offline issuance remains disabled by default. |
| Incomplete validation evidence | PRs may merge unverified DB changes. | Require rebuild, validation, drift, inventory, and authority evidence. |
| Accreditation mismatch | Sample package may not satisfy examiner expectations. | Carry BIR/accreditation package confirmation into physical design. |
| Over-fragmented object files | Too many files can obscure review. | Use per-object files where practical with manifest discipline. |
| Generated artifact opacity | Generated SQL may hide design changes. | Require tracked generator, deterministic output, and reviewable diffs. |
| Physical schema names implying wrong authority | Schema names could suggest ownership of Central PMS concepts. | Naming standards must prohibit payment/exit authority leakage. |

## 22. Non-Decisions

This draft does not decide or create:

- Final SQL DDL.
- Final physical table names.
- Final physical column names.
- Final constraints or indexes.
- Final physical schemas.
- Final enum implementation.
- Final Atlas or migration approach.
- Final seed, reference, or sample data.
- Final CI workflow.
- Final BIR/accreditation package.
- Offline fiscal issuance approval.
- A separate DB repository.

## 23. Appendices

### Appendix A: Glossary

| Term | Meaning |
| --- | --- |
| POS Server | ExitPass fiscal system that owns POS Server fiscal issuance and fiscal records for the resolved Site. |
| Central PMS | Main ExitPass platform component that owns parking session state, site resolution, payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. |
| State-based database versioning | Repository-owned desired database state used for rebuilds and drift checks. |
| Drift | Difference between live/test database state and repository-owned desired state. |
| Digital SI URL | POS Server-returned URL for viewing/saving the issued digital Sales Invoice. |
| ARTS POSLog | Structured POSLog export/schema reference; it does not replace BIR fiscal outputs. |

### Appendix B: Proposed Folder Layout

| Proposed path | Status |
| --- | --- |
| `db/README.md` | Proposed only; not created. |
| `db/state/` | Proposed only; not created. |
| `db/state/schemas/` | Proposed only; not created. |
| `db/state/tables/` | Proposed only; not created. |
| `db/state/views/` | Proposed only; not created. |
| `db/state/functions/` | Proposed only; not created. |
| `db/state/triggers/` | Proposed only; not created. |
| `db/state/types/` | Proposed only; not created. |
| `db/state/sequences/` | Proposed only; not created. |
| `db/state/policies/` | Proposed only; not created. |
| `db/state/extensions/` | Proposed only; not created. |
| `db/seeds/` | Proposed only; not created. |
| `db/reference-data/` | Proposed only; not created. |
| `db/validation/` | Proposed only; not created. |
| `db/rebuild/` | Proposed only; not created. |
| `db/drift/` | Proposed only; not created. |
| `db/scripts/` | Proposed only; not created. |
| `db/samples/` | Proposed only; not created. |
| `db/accreditation/` | Proposed only; not created. |

### Appendix C: Physical Design Gate Checklist

| Gate item | Required before physical artifacts |
| --- | --- |
| Target database engine confirmed | Yes |
| Schema/domain decomposition approved | Yes |
| Naming standards approved | Yes |
| Object-level folder structure approved | Yes |
| State-based versioning workflow approved | Yes |
| Rebuild script plan approved | Yes |
| Drift-check plan approved | Yes |
| Validation script plan approved | Yes |
| Seed/reference data separation approved | Yes |
| Enum versus controlled-code strategy approved | Yes |
| Idempotency uniqueness strategy approved | Yes |
| Fiscal numbering/counter strategy confirmed or placeholder policy approved | Yes |
| Retention/partitioning strategy reviewed | Yes |
| Digital SI URL security model reviewed | Yes |
| BIR/accreditation output/export expectations reviewed | Yes |
| ARTS POSLog profile/schema mapping reviewed | Yes |
| Tamper-evident anchoring approach reviewed | Yes |
| No local drift promotion rule confirmed | Yes |

### Appendix D: Authority Boundary Checklist

Future physical artifacts must confirm:

- No POS-owned payment finality lifecycle object.
- No POS-owned PaymentAttempt lifecycle object.
- No POS-owned PaymentConfirmation lifecycle object.
- No POS-owned ExitAuthorization issue/approve/mutate/bypass object.
- No POS-owned gate execution authority.
- Vendor PMS / HikCentral references are synchronization/context only.
- POS/fiscal events and audit records are evidence only.
- Channels/terminals remain child endpoints under Site POS Server.
- ONLINE/OFFLINE remains observability only.
- Offline fiscal issuance remains disabled unless separately approved by BIR/accounting.

### Appendix E: Source Traceability

| Plan area | Source baseline |
| --- | --- |
| Authority boundary | Approved BRD, System Design, API Contract, Database Design, Repository Boundary. |
| Layout plan | Physical DB Artifact Layout Plan and approved Database Design Physical Design Gate. |
| Validation plan | Physical DB Artifact Validation Plan and approved Database Design validation expectations. |
| Open questions | Open Questions Resolution Addendum and Physical DB Artifact Open Questions. |
| BIR/ARTS posture | BIR/ARTS impact review, Database Design, External Reference Inventory. |
| State-based workflow | Approved Database Design, Repository Boundary, Physical DB Artifact Decision Log. |
