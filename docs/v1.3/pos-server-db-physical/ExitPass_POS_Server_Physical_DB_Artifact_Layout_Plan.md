# ExitPass POS Server Physical DB Artifact Layout Plan

## Purpose

This layout plan proposes how future POS Server physical database artifacts should be organized in the `ExitPass-PoSServer` repository.

All paths are proposed only. This task does not create `db/` folders, SQL files, Atlas files, migrations, physical object files, seed data, validation scripts, rebuild scripts, drift scripts, sample data, or CI workflows.

## Target Database Engine

Recommended default target database engine: PostgreSQL. Final PostgreSQL version, edition, enabled extensions, collation/timezone assumptions, deployment topology, backup/restore topology, and managed-hosting constraints must be confirmed before physical artifact creation.

## Proposed Repository Folder Layout

| Proposed path | Purpose | Status |
| --- | --- | --- |
| `db/README.md` | Entry point for future DB artifact rules and commands. | Proposed only. |
| `db/state/` | Repository-owned desired database state. | Proposed only. |
| `db/state/schemas/` | Schema/domain definitions. | Proposed only. |
| `db/state/tables/` | One file per table where practical. | Proposed only. |
| `db/state/views/` | One file per view. | Proposed only. |
| `db/state/functions/` | One file per function/routine. | Proposed only. |
| `db/state/triggers/` | One file per trigger. | Proposed only. |
| `db/state/types/` | Enums, domains, composite types, or equivalent approved types. | Proposed only. |
| `db/state/sequences/` | Fiscal and technical sequences where approved. | Proposed only. |
| `db/state/policies/` | Security policies if approved. | Proposed only. |
| `db/state/extensions/` | Required PostgreSQL extensions where approved. | Proposed only. |
| `db/seeds/` | Environment-neutral minimal seed data. | Proposed only. |
| `db/reference-data/` | Controlled code sets and reference values. | Proposed only. |
| `db/validation/` | Validation scripts and evidence generators. | Proposed only. |
| `db/rebuild/` | Clean rebuild scripts and manifests. | Proposed only. |
| `db/drift/` | Drift-check scripts/configuration. | Proposed only. |
| `db/scripts/` | Utility scripts outside rebuild/validation/drift. | Proposed only. |
| `db/samples/` | Synthetic or approved sample data/output support. | Proposed only. |
| `db/accreditation/` | Future accreditation package support generated from approved sources. | Proposed only. |

## Proposed Schema / Domain Decomposition

Physical schema names remain provisional. Candidate domains are fiscal identity/boundary, channel registry, fiscal documents, fiscal details, numbering/counters, operation safety/idempotency, digital delivery, reports, exports, audit, recovery/continuity, security/privacy references, configuration/controlled codes, optional events/outbox, and integration references.

| Domain | Covers | Boundary rule |
| --- | --- | --- |
| Fiscal identity / boundary | Site POS Server, site/branch, taxpayer, MIN/PTU/serial/software/supplier metadata. | POS Server fiscal identity only. |
| Channel registry | WebPay, APM, cashier, EC/continuity, operator-assisted, future channels, capabilities, health. | Child endpoints, not fiscal authorities. |
| Fiscal documents/details | SI, adjustments, fiscal lines, tenders, taxes, discounts, totals. | Canonical fiscal records. |
| Numbering/counters | SI/adjustment sequences, reset counter, Z-counter, GTA, gaps. | Requires BIR/accounting confirmation or placeholder policy. |
| Operation safety | Idempotency, retry, timeout, completion unknown. | Prevent duplicate fiscal documents. |
| Digital delivery | Digital SI URL lifecycle, access audit, QR capability metadata. | No required QR image fiscal storage. |
| Reports/exports | X/Z, BIR Sales Summary, Annex E, EJ, POSLog, JSON, validation. | ARTS as export reference only. |
| Audit/recovery | Fiscal audit chain, state snapshots, hash/anchor references, recovery approvals. | Evidence only, not payment/exit authority. |
| Security/privacy | Actors, approvals, evidence refs, privileged access audit. | Final RBAC/evidence model downstream. |
| Integration refs | Central PMS and vendor synchronization/context references. | References only; no ownership transfer. |

## Naming Standards Plan

Future naming standards should define rules for schemas, tables, columns, primary keys, foreign keys, unique constraints, check constraints, indexes, enum/types, controlled-code sets, functions/routines, triggers, sequences, views, seed/reference data files, validation scripts, rebuild scripts, and drift scripts.

Planned discipline:

- Use lowercase names and one approved word separator.
- Prefer business-readable names over abbreviations, except approved terms such as SI, EJ, PTU, MIN, VAT, and GTA where appropriate.
- Constraint and index names should state the rule or access path purpose.
- Sequence names should identify fiscal scope and document family.
- Controlled-code names should support evolution and historical readability.
- Script names should identify workflow step and validation category.

## Object-Level State File Strategy

| Object family | Proposed strategy | Dependency note |
| --- | --- | --- |
| Schemas/extensions/types/sequences | One file per object or manifest where practical. | Applied before dependent objects. |
| Tables | One file per table where practical. | Constraints may be inline or separated by approved convention. |
| Indexes/constraints | Separate files or table-local sections; final rule pending. | Must remain reviewable in PRs. |
| Views/functions/triggers/policies | One file per object where practical. | Applied after dependencies. |
| Seed/reference data | Separate from object DDL. | Loaded after object state. |

## Ordering and Manifest Approach

Preferred default: explicit manifest with reviewable object order. Alternatives are ordered filename prefixes, tool-resolved dependency graph, or a hybrid manifest plus dependency validation. The selected strategy must support clean rebuilds, drift checks, and PR review.

## Generated vs Hand-Authored Boundary

Default posture: hand-authored or explicitly reviewed state files. If generation is introduced later, the generator source must be tracked, output must be deterministic, diffs must be reviewable, and validation must prove generated state preserves approved authority boundaries.

## State-Based Workflow

Future workflow should edit repository state, rebuild clean database, load approved seed/reference data, run validations, run drift checks, produce PR evidence, review authority-boundary checklist, and merge only reviewed repository changes. Local database drift must be expressed as reviewed repository artifacts before it can become baseline.

## Seed and Reference Data Separation

| Data category | Proposed location | Rule |
| --- | --- | --- |
| Object definitions | `db/state/` | Source of truth for schema objects. |
| Seed data | `db/seeds/` | Minimal environment-neutral rebuild data. |
| Reference data | `db/reference-data/` | Controlled codes and approved reference values. |
| Sample/accreditation data | `db/samples/` or `db/accreditation/` | Synthetic or approved sample data only. |
| Environment-specific data | Not committed unless explicitly approved. | Use local/secret configuration. |
| Secrets | Not committed. | Use secret management. |
| External BIR/ARTS/vendor files | Not committed unless licensing, ownership, size, and repository policy are confirmed. | Use external reference inventory. |

## Validation, Rebuild, Drift, and Accreditation Folder Plans

Future `db/validation/` should cover object existence, constraints/indexes, controlled codes, authority boundaries, idempotency/fiscal numbering, Digital SI URL, report/export, audit/recovery, security/privacy, and drift evidence.

Future `db/rebuild/` should create a clean database, apply repository state, load approved data, run validations, and produce rebuild evidence for local and CI use.

Future `db/drift/` should compare live/test DB to repository state, report unexpected/missing/changed objects, and prevent automatic drift promotion.

Future `db/samples/` and `db/accreditation/` should support generated sample SI, X-read, Z-read, EJ, POSLog, BIR Sales Summary/Annex E, reprints, JSON/POSLog validation evidence, fiscal identity samples, and recovery/counter continuity evidence.

## Authority Boundary Guardrails

Future physical layout and names must not imply POS Server ownership of payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, gate execution, payment reversal finality, or vendor PMS authority. POS Server stores references and reconciliation context only.