# ExitPass POS Server Database Design v1.0

## Document Control

### Document Information

| Field | Value |
| --- | --- |
| Document title | ExitPass POS Server Database Design v1.0 |
| Version | v1.0 |
| Product scope | ExitPass POS Server fiscal database design for ExitPass v1.3 |
| Repository | `ExitPass-PoSServer` |
| Status | Approved baseline |
| Output format | Markdown only |
| BRD baseline | `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` |
| System Design baseline | `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` |
| API Contract baseline | `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` |
| Physical database artifact status | Not created by this document |

### Version History

| Version | Status | Notes |
| --- | --- | --- |
| v1.0 | Approved baseline | Approved logical database design baseline for ExitPass POS Server v1.0. |

### Approval and Baseline Status

This document is approved as the POS Server Database Design v1.0 logical database design baseline. It governs POS Server database persistence semantics for downstream physical database design, state-based database object artifacts, Engineering Pack work, BIR/accreditation support, test data planning, validation planning, and implementation planning.

Approval of this logical database design does not create or approve SQL DDL, physical table names, physical column names, physical indexes, physical constraints, physical schemas, enum implementation, Atlas/state files, migrations, endpoint DTOs, event payloads, final RBAC matrix, or the final BIR/accreditation package.

### Baseline Scope Statement

This document translates the approved POS/Invoicing BRD, POS Server System Design, POS Server API Contract, and POS Server Database Design planning package into database design language.

This document governs logical persistence semantics for:

- Site POS Server fiscal authority boundary.
- Fiscal identity and taxpayer/site/branch identity references.
- Channel and terminal registry.
- Fiscal document lifecycle.
- Explicit fiscal lines, tenders, tax, discounts, and totals.
- Fiscal numbering, reset counter, Z-counter, Grand Total Amount, EJ hash, and recovery continuity.
- Idempotency and retry records.
- Digital SI URL references and access audit impact.
- QR presentation data boundary.
- Reprints, fiscal adjustments, reports, EJ, POSLog, JSON, exports, audit, recovery, retention, security, privacy, and state-based database versioning.

It does not govern final SQL syntax, final physical schema objects, final indexes, final constraints, final migrations, final Atlas files, final endpoint DTOs, final event payloads, or final RBAC implementation.

### Controlled Change Rule

A proposed change to this database design must identify:

- affected BRD requirement, if any;
- affected System Design section, if any;
- affected API Contract section, if any;
- affected logical data area;
- implementation impact;
- future physical schema impact;
- migration or state-based database artifact impact;
- BIR/accreditation, accounting, audit, security, privacy, or operational impact;
- test and validation impact.

No downstream work may weaken the approved authority model through undocumented DDL, local database drift, direct table writes, ad hoc scripts, or undocumented service behavior.

### Downstream Artifact Rule

The following downstream artifacts must be derived from or reconciled against this database design after approval:

- POS Server physical database design.
- POS Server state-based database object files.
- POS Server SQL DDL, if later produced.
- POS Server Atlas/state files, if later used.
- POS Server seed/reference data files.
- POS Server validation and drift-check scripts.
- POS Server Engineering Pack.
- POS Server BIR/accreditation package support files.
- POS Server test fixtures and sample fiscal data.
- POS Server operational runbooks for fiscal reset, recovery, export validation, retention, archival, and audit retrieval.

Where a downstream artifact conflicts with this document on persistence semantics, this document governs unless the approved BRD, System Design, or API Contract is formally revised.

## Purpose

The purpose of this document is to define the logical database design for the ExitPass POS Server system.

This document translates the approved POS Server business, system, and API baselines into database ownership boundaries, logical record groups, candidate attributes, relationships, lifecycle rules, integrity expectations, traceability rules, audit posture, recovery posture, retention categories, and state-based versioning direction.

This document exists to do five things:

1. Preserve the approved authority model in database design form.
2. Define POS Server-owned fiscal data areas without moving Central PMS payment finality or ExitAuthorization authority into the POS Server database.
3. Define the canonical fiscal record posture that reconciles Sales Invoice, printed output, digital Sales Invoice, EJ, POSLog, reports, exports, and audit.
4. Identify candidate logical structures needed for future physical database design without prematurely creating SQL, Atlas files, migrations, final schemas, or final table/column definitions.
5. Carry forward BIR/accounting/security/privacy/accreditation open questions so downstream design does not silently decide them.

This document is not a replacement for the BRD, System Design, or API Contract. The BRD defines business scope and acceptance expectations. The System Design defines architecture, component responsibilities, runtime behavior, authority boundaries, and operational controls. The API Contract defines integration behavior, route families, status/error semantics, idempotency, and trust boundaries. This database design defines persistence semantics and logical data structures needed to support those documents.

## Scope

### In Scope

This document covers logical database design for:

- Repository and schema boundary.
- Authority separation.
- Site POS Server fiscal boundary.
- Fiscal identity.
- Channel and terminal registry.
- Fiscal documents.
- Fiscal lines.
- Tender, tax, discount, and totals.
- Numbering and counter state.
- Idempotency and retry.
- Digital SI URL and access audit.
- QR presentation data boundary.
- Reprints.
- Fiscal adjustments.
- X-read and Z-read.
- BIR Sales Summary and Annex E.
- Electronic Journal, POSLog, JSON, and exports.
- Audit trail.
- Recovery and tamper-evident continuity.
- Security/RBAC and privacy data impacts.
- Retention and archival.
- Central PMS integration references.
- ARTS POSLog / BIR extension mapping.
- State-based database versioning plan.
- Open questions, risks, mitigations, and non-decisions.

### Out of Scope

This document does not cover:

- Source code.
- SQL DDL.
- Atlas migration files or state files.
- Actual database migration execution.
- Final physical table names.
- Final physical column names.
- Final constraints, indexes, partitions, or enum storage.
- Final endpoint DTO definitions.
- Final event payload schemas.
- Final RBAC matrix.
- Final BIR accreditation package.
- DOCX output.
- Diagram changes.

## Reference Documents

### Reference Hierarchy

The following hierarchy governs interpretation:

1. `ExitPass_POS_Invoicing_BRD_v1.0.md` governs business scope, compliance intent, functional requirements, and acceptance criteria.
2. `ExitPass_POS_Server_System_Design_v1.0.md` governs POS Server architecture, component boundaries, runtime behavior, recovery posture, and authority model realization.
3. `ExitPass_POS_Server_API_Contract_v1.0.md` governs route families, API ownership, caller responsibilities, status/error semantics, idempotency, and trust boundaries.
4. This database design governs POS Server persistence semantics and logical database design.
5. Future SQL/Atlas/state files, Engineering Pack artifacts, test fixtures, and implementation work must align with the approved BRD, System Design, API Contract, and this database design.

### Source Baselines

| Source | Use in this document |
| --- | --- |
| Approved POS/Invoicing BRD v1.0 | Business and compliance authority baseline. |
| Approved POS Server System Design v1.0 | Architecture and technical authority baseline. |
| Approved POS Server API Contract v1.0 | API behavior, status, idempotency, and trust-boundary baseline. |
| POS Server Database Design planning artifacts | Source analysis, decision log, open questions, impact map, and outline. |
| BIR/ARTS Source Impact Review | BIR examiner material and ARTS POSLog alignment inputs. |
| `docs/REPOSITORY_BOUNDARY.md` | Separate repository and ownership boundary. |
| `docs/references/External_POS_BIR_ARTS_References.md` | External/local source inventory; source files are not copied into this repository. |
| ExitPass Database Design v1.2 | Writing pattern, documentation discipline, and structure style only. |

### Conflict Rule

If this database design conflicts with the approved BRD on business intent, the BRD governs and this document must be updated.

If this database design conflicts with the approved System Design on architecture, component ownership, authority boundaries, recovery behavior, or runtime control, the System Design governs and this document must be updated.

If this database design conflicts with the approved API Contract on API behavior, caller responsibility, idempotency, status/error semantics, or trust boundaries, the API Contract governs for API behavior and this document must be updated if persistence semantics are affected.

If a BIR, vendor, ARTS POSLog, or external reference conflicts with the approved ExitPass authority model, the approved BRD, System Design, API Contract, and this database design govern the canonical ExitPass model unless formally revised.

### Version Alignment Note

This is a POS Server v1.0 database design draft for the separate `ExitPass-PoSServer` repository. It uses ExitPass Database Design v1.2 as a writing-pattern reference only. It does not import unrelated v1.2 Central PMS schema content into POS Server.

## Design Principles

### Authority Separation

The database design shall preserve the approved authority boundaries.

Central PMS owns parking session control state, site resolution, payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. POS Server shall not declare platform payment finality and shall not issue, own, or mutate ExitAuthorization.

POS Server owns fiscal issuance and fiscal records only. The POS Server database stores fiscal records and references to Central PMS authority records. It must not become an alternate source of payment finality, parking session control state, or exit authority.

### POS Server Fiscal Source of Truth

The POS Server database is the fiscal source of truth for fiscal documents and reports issued by the Site POS Server for the resolved Site.

It shall store canonical fiscal facts for Sales Invoice issuance, fiscal lines, totals, fiscal reports, EJ, POSLog, exports, reprints, adjustments, audit, retention, and recovery continuity.

### Canonical Fiscal Record Chain

The approved fiscal chain is:

Central PMS verified payment finality reference -> POS Server fiscal issuance -> Sales Invoice fiscal record -> fiscal outputs/reports/exports/audit -> Central PMS fiscal reference recording -> Central PMS ExitAuthorization.

The POS Server database shall support this chain without moving Central PMS authority into POS Server. Fiscal issuance records may reference Central PMS finality context, but they do not create payment finality or ExitAuthorization.

### Logical Before Physical Design

This document defines conceptual and logical persistence design. Candidate logical records and candidate logical attributes are not final tables or columns.

Physical SQL DDL, constraints, indexes, partitions, object files, and Atlas/state files are deferred to later database artifact work.

### Idempotency, Atomicity, and Replay Safety

Side-effecting fiscal operations shall be idempotency-aware. The database design shall support duplicate request detection, idempotent replay, idempotency conflict, timeout, completion-unknown, retry, and exception status without duplicate fiscal document creation.

### Traceability First

A reviewer must be able to reconstruct:

- which Site POS Server issued the fiscal document;
- which Central PMS session/payment/finality context was referenced;
- which channel or terminal initiated or presented the transaction;
- which fiscal identity was effective;
- which Sales Invoice number, fiscal lines, totals, counters, and reports were produced;
- whether digital Sales Invoice, QR presentation, reprint, adjustment, report, export, reset, recovery, or privileged action occurred;
- which actor/service, approval, timestamp, status, reason, and audit reference applies.

Audit records are evidence stores. They must not replace canonical fiscal records or become payment/exit authority.

### Domain Boundary Discipline

Logical data areas exist to represent ownership boundaries, not just storage folders. Fiscal identity, channel registry, fiscal documents, fiscal lines, counters, reports, exports, audit, recovery, and security/privacy references shall remain clearly separated.

Cross-domain references to Central PMS, Vendor PMS / HikCentral, payment providers, or channels/terminals must be context references, not ownership transfers.

### Append-Only and Tamper-Evident Fiscal Posture

Fiscal records shall favor append-only history, controlled correction, explicit adjustments, audit trails, and recovery continuity evidence over silent overwrite.

Fiscal reset, recovery, counter changes, reprints, adjustments, export validation, and privileged configuration changes require auditability.

### Privacy and Evidence Minimization

The database design shall support privacy-aware evidence handling. Sensitive customer details, diplomat VAT evidence, identity evidence, digital Sales Invoice access details, and operator evidence shall be stored only where required and with retention/access controls appropriate to their purpose.

### Offline Fiscal Issuance Restriction

ONLINE/OFFLINE is an operational status concept. It does not approve offline fiscal issuance.

Offline fiscal issuance remains disabled or restricted by default until BIR/accounting approves a compliant sequence, counter, evidence, reconciliation, and recovery model.

### State-Based Database Versioning

Future database artifacts shall use a state-based posture:

- repository-owned database objects are source of truth;
- per-object SQL files should be used where practical;
- repeatable rebuilds and drift checks are required;
- seed/reference data should be separated from object definitions;
- local database drift must not become the baseline without explicit review and repository update;
- Atlas/state-based comparison may be used later;
- PR review must include database validation evidence.
## Database Overview

### Repository and Database Ownership Boundary

| Area | Owner | POS Server database posture |
| --- | --- | --- |
| Parking session control state | Main ExitPass / Central PMS | Reference only. |
| Site resolution authority | Main ExitPass / Central PMS | Reference only. |
| PaymentAttempt | Main ExitPass / Central PMS | Reference only where required for fiscal audit/reconciliation. |
| PaymentConfirmation | Main ExitPass / Central PMS | Reference only where required for fiscal issuance. |
| Payment finality | Main ExitPass / Central PMS | Reference only; POS Server does not declare finality. |
| ExitAuthorization | Main ExitPass / Central PMS | Not owned by POS Server; POS Server does not issue or mutate it. |
| Fiscal issuance | POS Server | POS Server-owned fiscal records. |
| Sales Invoice lifecycle | POS Server | POS Server-owned fiscal records. |
| Fiscal reports, EJ, POSLog, exports | POS Server | POS Server-owned outputs generated from canonical fiscal records. |
| Channels/terminals | POS Server for fiscal registration and capabilities | Child records under Site POS Server; not independent fiscal authorities. |
| Vendor PMS / HikCentral acknowledgment | External/vendor integration | Synchronization/context reference only. |
| POS/fiscal events | POS Server event/audit output | Observability/integration evidence only; not payment or exit authority. |

### Logical Domain Inventory

| Logical domain | Purpose | Physical design status |
| --- | --- | --- |
| Fiscal boundary | Site POS Server and fiscal identity boundary. | Pending physical design. |
| Channel registry | Registered child channels/terminals and presentation capabilities. | Pending physical design. |
| Fiscal document | Sales Invoice and fiscal adjustment document lifecycle. | Pending physical design. |
| Fiscal detail | Fiscal lines, tenders, taxes, discounts, and totals. | Pending physical design. |
| Fiscal state | Numbering, reset counter, Z-counter, GTA, EJ hash, and recovery state. | Pending physical design. |
| Operation safety | Idempotency, retry, exception, timeout, and completion-unknown records. | Pending physical design. |
| Digital delivery | Digital SI URL references, access status, and access audit impact. | Pending physical design. |
| Presentation boundary | QR capability and optional presentation audit, not QR image ownership. | Pending physical design. |
| Fiscal outputs | Reprints, reports, EJ, POSLog, JSON, exports, and validation. | Pending physical design. |
| Audit and evidence | Fiscal audit, approvals, evidence references, and privileged action records. | Pending physical design. |
| Continuity | Recovery, restore/failover checks, tamper-evident continuity. | Pending physical design. |
| Security/privacy | Actor, role/approval, evidence, and access-audit references. | Pending physical design. |

### Physical Design Deferral Rule

The logical domains above may become schemas, tables, views, functions, routines, partitions, or object files in later database artifact work. This document does not decide that physical shape.

Future physical design must not weaken the ownership boundary, authority model, or non-decisions recorded in this document.

## Naming, Identifier, and Status Standards

### Naming Posture

This draft may use descriptive candidate logical names. Those names are not final table names, final column names, or final enum names.

Future physical naming should follow a consistent repository-wide convention and should be reviewed with the physical database artifact task before SQL or state files are created.

### Identifier Strategy

The design shall distinguish:

| Identifier type | Meaning | Database posture |
| --- | --- | --- |
| POS Server-owned identifier | Identifier for POS Server-owned fiscal records. | Stored as canonical POS Server identity. |
| Central PMS reference | Identifier for parking session, PaymentAttempt, PaymentConfirmation, payment finality, or fiscal issuance correlation from Central PMS. | Stored as external authority reference only. |
| Channel/terminal identifier | Identifier for a registered child channel or terminal. | Stored under Site POS Server registry. |
| Fiscal identity identifier | Identifier for effective fiscal identity/configuration. | Stored with history/audit. |
| Human-readable fiscal number | Sales Invoice number or future adjustment document number. | Stored for fiscal document identity, pending final numbering rules. |
| Correlation identifier | Cross-service traceability value. | Stored where required for reconciliation and audit. |
| Idempotency key | Replay/deduplication identifier for side-effecting operations. | Stored with operation scope and replay/conflict state. |
| Evidence reference | Reference to supporting evidence, not necessarily the evidence payload itself. | Stored with privacy and retention controls. |

### Status Strategy

Status fields shall be treated as logical lifecycle states until final physical enum or controlled-code strategy is approved.

Status design shall support:

- issued, failed, blocked, timed out, completion unknown, retry pending, and idempotent replay for fiscal issuance;
- active, inactive, degraded, continuity, ONLINE, and OFFLINE for channel/terminal health where applicable;
- active, expired, revoked, and blocked for Digital SI URL lifecycle;
- reprint requested, completed, and failed;
- report/export requested, generated, failed, validation pending, validation passed, and validation failed;
- reset requested, approved, completed, and rejected;
- recovery check passed, failed, blocked, and supervised recovery completed.

Final status storage remains a future database design and API alignment decision.

## Audit and Traceability Model

### Traceability Objective

The database shall preserve end-to-end fiscal traceability across:

- Central PMS payment/session/finality references.
- Site POS Server fiscal issuance.
- Sales Invoice number and fiscal document identity.
- Fiscal lines, tenders, taxes, discounts, totals, and GTA contribution.
- Digital SI URL creation/access where required.
- Reprints and fiscal adjustments.
- X-read, Z-read, BIR Sales Summary, Annex E, EJ, POSLog, JSON, and exports.
- Export validation.
- Fiscal reset and recovery continuity.
- Fiscal identity and channel/terminal configuration changes.
- Approval and privileged action records.

### Audit Record Rules

Audit records shall capture actor/service identity, timestamp, action, affected logical record, status/result, reason/error code where applicable, approval reference where applicable, and correlation/reference identifiers.

Audit records are append-only evidence in posture. They shall not be modeled as alternate payment finality records or ExitAuthorization records.

### Evidence Reference Rules

Evidence references shall support privacy-aware storage. The design should permit evidence to be stored separately or referenced through controlled storage when required by security/privacy decisions.

## Logical Data Model

### Purpose of the Logical Data Model

The logical data model identifies the candidate record groups required for POS Server fiscal persistence. It is not a physical ERD and does not create schema objects.

### Conceptual Relationships

- Site POS Server has fiscal identity configuration history.
- Site POS Server has many registered child channels and terminals.
- Channel/terminal may have fiscal identity reference and presentation capabilities.
- Fiscal document belongs to one Site POS Server and references Central PMS context.
- Fiscal document has ordered fiscal lines and fiscal totals.
- Sales Invoice may have Digital SI URL reference.
- Fiscal document may have reprints and adjustment documents.
- Fiscal reports, EJ, POSLog, JSON, and exports are generated from canonical fiscal records.
- Audit records link fiscal actions, configuration changes, privileged actions, and recovery events.
- Recovery state links fiscal state snapshots, counters, GTA, EJ hash, and last fiscal event timestamp.

### Logical Record Group Inventory

| Logical record group | Responsibility | Key relationships | Physical design status |
| --- | --- | --- | --- |
| Site POS Server boundary | Fiscal authority boundary for a Site or parking operation boundary. | Site, branch/business unit, fiscal identity, channels/terminals. | Provisional. |
| Fiscal identity | Taxpayer, branch, POS Server, terminal/channel, MIN/PTU/serial/software/supplier data as confirmed. | Site POS Server and optional channel/terminal references. | Provisional. |
| Channel/terminal registry | Child channel/terminal association, type, capability, and status. | Site POS Server, fiscal identity, audit records. | Provisional. |
| Fiscal document | Sales Invoice and adjustment document identity/lifecycle. | Site POS Server, Central PMS references, fiscal lines, audit. | Provisional. |
| Fiscal line | Ordered fiscal classifications and line amounts. | Fiscal document, entitlement/tax evidence references. | Provisional. |
| Tender/tax/discount/totals | Tender context and fiscal totals for reconciliation/reporting. | Fiscal document and BIR Sales Summary. | Provisional. |
| Numbering/counter state | SI sequence, adjustment sequence, reset counter, Z-counter, GTA, EJ hash. | Site POS Server and recovery continuity records. | Provisional. |
| Idempotency/retry | Deduplication, replay, conflict, timeout, and retry state. | Fiscal operations and replay results. | Provisional. |
| Digital SI URL/access | Digital SI URL reference, lifecycle, access audit impact. | Fiscal document and security/privacy references. | Provisional. |
| QR presentation capability | Channel/terminal ability to present QR generated from URL. | Channel/terminal and Digital SI URL reference. | Provisional. |
| Reprint | Controlled reprint request/history and output linkage. | Original document/report/EJ and audit. | Provisional. |
| Fiscal adjustment | Void/refund/cancel/return fiscal adjustment records. | Original fiscal document and Central PMS/payment references. | Provisional. |
| Fiscal report | X-read, Z-read, BIR Sales Summary, Annex E. | Canonical fiscal records and audit. | Provisional. |
| EJ/POSLog/export | EJ records, structured exports, validation status, schema/profile references. | Canonical fiscal records and retention. | Provisional. |
| Audit trail | Fiscal audit and privileged action evidence. | All fiscal and configuration actions. | Provisional. |
| Recovery/continuity | Restore/failover/recovery continuity evidence. | Counter state, fiscal state snapshots, audit. | Provisional. |
| Security/privacy references | Actor, approval, evidence, access audit, sensitive-data separation. | Privileged operations and evidence records. | Provisional. |
## Logical Data Area Specifications

### Fiscal Identity and Site POS Server Boundary

#### Purpose

This area defines the fiscal identity and authority boundary for one Site POS Server.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Site POS Server boundary | Identifies the Site-level fiscal authority boundary. |
| Taxpayer identity | Holds taxpayer/registered business identity needed for fiscal output. |
| Site/branch/business unit identity | Connects Site POS Server to reporting and fiscal location identity. |
| POS Server fiscal identity | Holds POS Server-level fiscal identity fields where assigned. |
| Channel/terminal fiscal identity reference | Links a child channel/terminal to fiscal identity fields where applicable. |
| Fiscal identity history | Preserves effective-dated identity/configuration history. |
| Fiscal identity change audit | Audits changes and approvals. |

#### Candidate Logical Attributes

- Site POS Server identity.
- Site, branch, and business unit mapping.
- Taxpayer / registered business name, registered address, TIN, and VAT/non-VAT classification.
- POS Server fiscal identity.
- Channel or terminal fiscal identity reference where applicable.
- MIN, PTU / ATG if applicable, serial number, terminal number, software name/version, and supplier accreditation metadata.
- Effective date/time range, configuration status, approval reference, and audit reference.

#### Design Rules

- Fiscal identity records shall be effective-dated and auditable.
- Fiscal identity changes shall not rewrite already-issued fiscal documents.
- Fiscal identity misconfiguration shall be able to block fiscal issuance.
- Final assignment between Site POS Server and channel/terminal identities remains open.

#### Future Physical Design Notes

Future physical design shall decide object grouping, effective-dating uniqueness, fiscal identity versioning, controlled-code handling, and audit linkage. It shall not hard-code a single MIN/PTU/serial/software/supplier assignment rule until BIR/accreditation confirmation is available.

### Channel and Terminal Registry

#### Purpose

This area stores registered child channels and terminals under the Site POS Server. Channels and terminals are payment or presentation endpoints, not independent fiscal authorities.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Channel/terminal registration | Registers WebPay, APM, Cashier POS, EC Device / Continuity Terminal, operator-assisted, or future channel endpoint. |
| Capability record | Stores print, display, Digital SI URL, and QR presentation capabilities. |
| Status/health record | Stores ONLINE/OFFLINE or equivalent reachability state. |
| Fiscal identity reference | Links channel/terminal to fiscal identity where required. |
| Configuration audit | Audits registration/configuration changes. |

#### Candidate Logical Attributes

- Channel/terminal ID.
- Site POS Server association.
- Channel/terminal type.
- Presentation capabilities: print, display, Digital SI URL, QR presentation.
- Fiscal identity reference.
- Active, inactive, degraded, or continuity state.
- ONLINE/OFFLINE or equivalent health state.
- Last status timestamp and audit reference.

#### Design Rules

- Channel/terminal records shall be scoped to the Site POS Server.
- QR presentation capability means the channel/terminal can convert the returned Digital SI URL into a QR code where supported.
- QR capability does not make the channel/terminal the fiscal issuer.
- ONLINE/OFFLINE status is observability only and does not approve offline fiscal issuance.

#### Future Physical Design Notes

Future physical design shall decide object grouping, uniqueness for logical and physical channel identities, status history retention, capability storage, and health/status indexing. It shall support logical/non-physical channels such as WebPay without creating independent fiscal authority.

### Fiscal Documents

#### Purpose

This area stores canonical fiscal document records issued or controlled by the Site POS Server.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Fiscal document | Canonical fiscal document identity and lifecycle record. |
| Sales Invoice | Primary parking fiscal output. |
| Adjustment fiscal document | Fiscal document for void/refund/cancel/return or other confirmed adjustment workflows. |
| Fiscal document status history | Lifecycle/status history for audit and replay handling. |
| Original document linkage | Links adjustment or reprint context to original fiscal document. |
| Fiscal output reference | References printed/digital/exported output records. |

#### Candidate Logical Attributes

- Fiscal document ID and fiscal document type.
- Sales Invoice number.
- Adjustment document number where applicable.
- Document status and issuance timestamp.
- Site POS Server reference.
- Channel/terminal reference.
- Central PMS parking session, PaymentAttempt where applicable, PaymentConfirmation, payment finality, and payment reference.
- Digital SI URL reference.
- Original fiscal document reference where applicable.
- Audit reference.

#### Design Rules

- Fiscal document records shall belong to a Site POS Server.
- Fiscal document records may reference Central PMS authority records but shall not own or mutate them.
- Fiscal document records shall not store or create ExitAuthorization authority.
- Original fiscal facts shall not be mutated by reprints, digital access, reports, or exports.
- Corrections shall be represented through controlled fiscal adjustment records where applicable.

#### Future Physical Design Notes

Future physical design shall decide fiscal document object grouping, fiscal number uniqueness, status storage, reference integrity strategy, and retention/partitioning. It shall keep display fiscal numbers separate from internal fiscal document identity.

### Fiscal Lines

#### Purpose

This area stores ordered fiscal line classifications and amounts that support Sales Invoice layout, BIR Sales Summary, Annex E reports, EJ, POSLog, JSON exports, and audit.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Fiscal line | Ordered fiscal document line. |
| Fiscal line classification | Business/fiscal classification of the line. |
| Entitlement/tax evidence reference | Line-level supporting evidence reference where required. |
| Fiscal line adjustment reference | Links fiscal adjustment effects to affected lines where needed. |

#### Candidate Logical Attributes

- Fiscal document reference.
- Line sequence number.
- Line classification and item/charge description.
- VATable sales, VAT-exempt sales, zero-rated sales, non-VAT sales, and VAT amount.
- Statutory discount, VAT privilege/exemption, coupon, parking fee, lost ticket fee, overstay fee, penalty, service charge, and other adjustment amounts.
- Line totals.
- Line-level entitlement/tax evidence reference where applicable.

#### Design Rules

- Fiscal lines shall be ordered and auditably tied to the fiscal document.
- Line sequence numbers should support ARTS POSLog-style structured export alignment without replacing BIR terminology.
- Exact VAT/tax treatment remains open for BIR/accounting confirmation.
- Diplomat VAT Privilege / VAT Exemption shall be representable as VAT privilege/exemption treatment, not as an ordinary commercial discount.

#### Future Physical Design Notes

Future physical design shall decide line classification storage, line ordering enforcement, controlled-code or enum strategy, and report/export query patterns. It shall preserve explicit fiscal classifications and avoid hiding tax treatment only in tariff snapshots.

### Tender, Tax, Discount, and Totals

#### Purpose

This area stores tender context and fiscal totals required for reconciliation, fiscal reports, and structured exports.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Tender/payment reference | Fiscal reference to payment/tender context. |
| Fiscal totals | Document-level fiscal totals. |
| Tax summary | VATable, VAT-exempt, zero-rated, and VAT amount summaries. |
| Discount summary | Discount, privilege, coupon, void, and return summaries. |
| GTA contribution | Grand Total Amount contribution for fiscal continuity. |

#### Candidate Logical Attributes

- Tender type and payment method.
- Provider/payment reference where applicable.
- Gross sales, net sales, VATable sales, VAT amount, VAT-exempt sales, zero-rated sales, discounts, voids, returns, total amount due, amount paid, and change amount where applicable.
- Grand Total Amount contribution.
- BIR Sales Summary reconciliation reference.

#### Design Rules

- Tender/payment references are fiscal context only. Payment finality remains with Central PMS.
- Fiscal totals shall reconcile to fiscal lines and generated reports.
- Grand Total Amount contribution shall be preserved for reset, recovery, and audit continuity.

#### Future Physical Design Notes

Future physical design shall decide totals snapshot structure, reconciliation query patterns, controlled-code strategy for tender/tax/discount types, and report aggregation support. It shall keep Central PMS payment finality as an external reference.

### Numbering and Counter State

#### Purpose

This area stores fiscal numbering and counter state required for fiscal issuance, Z-read, reset, Grand Total Amount, EJ continuity, and recovery.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Sales Invoice sequence state | Tracks Sales Invoice sequencing for a Site POS Server. |
| Adjustment document sequence state | Tracks adjustment document sequencing where applicable. |
| Reset counter state | Tracks fiscal reset counter. |
| Z-counter state | Tracks Z-reading/fiscal day close counter. |
| Grand Total Amount state | Tracks GTA accumulator and snapshots. |
| Fiscal state snapshot | Captures continuity state for reset/recovery. |
| EJ hash reference | Tracks latest EJ hash reference. |
| Fiscal lock/block status | Blocks issuance during recovery or unsafe state. |

#### Candidate Logical Attributes

- Site POS Server reference.
- Sales Invoice sequence state and adjustment sequence state.
- Reset counter and Z-counter.
- Grand Total Amount accumulator.
- Previous Grand Total Amount snapshot and previous reset counter snapshot.
- Latest EJ hash.
- Last fiscal event timestamp.
- Last known fiscal state.
- Recovery continuity reference.
- Fiscal lock/block status.
- Approval and audit reference for reset/recovery events.

#### Design Rules

- Reset counter starts from zero and increments only on fiscal reset.
- Z-counter advances per Z-reading / fiscal day close.
- Reset counter must not advance per Z-read.
- POS Server must preserve previous Grand Total Amount, previous reset counter, reset timestamp, reset reason, approving user, and recovery/reference notes when reset occurs.
- POS Server must not resume fiscal issuance from lower counters, lower Grand Total Amount, earlier Sales Invoice sequence, broken EJ hash continuity, or earlier last fiscal event timestamp.

#### Future Physical Design Notes

Future physical design shall decide sequence allocation storage, uniqueness rules, fiscal lock handling, sequence-gap audit records, and counter snapshot strategy. Final SI and adjustment numbering formats require BIR/accounting confirmation or an approved placeholder policy.

### Idempotency and Retry

#### Purpose

This area prevents duplicate fiscal documents and supports safe retry after timeout, completion-unknown, service interruption, or caller retry.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Idempotency record | Stores idempotency key, scope, and linked operation. |
| Request identity | Stores request hash or semantic identity. |
| Operation linkage | Links request to fiscal document/report/export/reprint/adjustment. |
| Replay result | Stores deterministic replay result. |
| Conflict record | Stores idempotency conflicts. |
| Retry status | Stores retry and exception status. |

#### Candidate Logical Attributes

- Idempotency key and idempotency scope.
- Request semantic hash or request identity.
- Fiscal operation type.
- Linked fiscal document, report, export, reprint, or adjustment.
- Replay result, conflict status, timeout status, completion-unknown status, retry status, exception reference, and retention policy reference.

#### Design Rules

- Side-effecting fiscal operations shall be idempotency-aware.
- Duplicate request handling shall not create duplicate Sales Invoices or duplicate fiscal adjustment documents.
- Timeout and completion-unknown states must trigger status lookup before retry or downstream authorization decisions.
- Idempotency retention shall be long enough to support fiscal audit and duplicate prevention requirements.

#### Future Physical Design Notes

Future physical design shall decide idempotency uniqueness strategy, request identity storage, retention period, replay-result storage, and status lookup indexing. It shall support timeout and completion-unknown recovery without duplicate fiscal documents.

### Digital SI URL and Access Audit

#### Purpose

This area stores Digital SI URL references and access audit data where required by security, privacy, and fiscal audit policy.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Digital SI URL reference | Links issued Sales Invoice to digital access URL. |
| digital Sales Invoice access token/reference | Represents access mechanism at logical level. |
| URL status history | Tracks active, expired, revoked, or blocked status. |
| Access audit | Records access where required. |
| Revocation/block record | Records revocation or block reason. |

#### Candidate Logical Attributes

- Fiscal document reference.
- Digital SI URL reference and token/access reference.
- URL status: active, expired, revoked, or blocked.
- Issue timestamp and expiry timestamp where applicable.
- Access audit and repeated access audit where required.
- Revocation or block reason.
- Privacy/data minimization marker and audit reference.

#### Design Rules

- Digital SI URL points to the same issued Sales Invoice as the printed SI.
- Printed and digital Sales Invoice records must not represent different fiscal facts.
- Digital SI URL records shall not allow unauthorized modification of the issued Sales Invoice.
- Digital SI URL records shall avoid unnecessary sensitive data exposure.
- Repeated digital access shall be controlled and auditable where required.

#### Future Physical Design Notes

Future physical design shall decide token/access reference storage, URL lifecycle uniqueness, expiry handling, access audit retention, and security/privacy separation. It shall store the Digital SI URL lifecycle without allowing fiscal mutation through public access.

### QR Presentation Data Boundary

#### Purpose

This area preserves the approved boundary that QR generation is a channel/terminal presentation responsibility, while POS Server stores and returns the Digital SI URL.

#### Approved Boundary

| Responsibility | Owner |
| --- | --- |
| Digital SI URL creation/return | POS Server. |
| QR code conversion/display/printing | Channel or terminal where supported. |
| QR fiscal authority | None; QR presentation does not create fiscal authority. |
| Fiscal issuer | Site POS Server. |

#### Database Impact

- Store channel/terminal QR presentation capability where applicable.
- Store Digital SI URL reference.
- Store optional presentation audit reference where required.
- Do not require storage of QR image binaries as fiscal records.

#### Future Physical Design Notes

Future physical design shall store only capability and optional presentation audit data unless a later implementation explicitly requires support metadata. It shall not require QR image binaries as fiscal records.

### Reprints

#### Purpose

This area stores controlled reprint request, status, approval, output linkage, labels, timestamps, and audit records for fiscal outputs where applicable.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Reprint request | Requests controlled reprint of an output. |
| Reprint history/status | Tracks lifecycle of the reprint. |
| Reprint approval reference | Records authorization where required. |
| Reprint output reference | References generated reprint output. |
| Reprint audit | Captures actor, reason, timestamp, and status. |

#### Candidate Logical Attributes

- Reprint request ID.
- Original document/report/output reference.
- Reprint type: Sales Invoice, X-read, Z-read, or Electronic Journal.
- Reprint reason, requesting actor/service, authorization/approval reference, reprint timestamp, status/history, `REPRINT` metadata support, `DATE / TIME REPRINTED` metadata support, and audit reference.

#### Design Rules

- Reprints shall not mutate original fiscal facts.
- Reprinted fiscal outputs shall show `REPRINT` and `DATE / TIME REPRINTED` where BIR requires them.
- All reprint activity shall be logged and auditable.
- Reprint authorization shall be captured where required by policy.

#### Future Physical Design Notes

Future physical design shall decide reprint history storage, approval linkage, output reference retention, and indexing for audit retrieval. It shall preserve original fiscal facts and support required `REPRINT` and `DATE / TIME REPRINTED` metadata.

### Fiscal Adjustments

#### Purpose

This area stores fiscal adjustment records for void, refund, cancel, return, and other confirmed fiscal adjustment workflows.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Adjustment document | POS Server fiscal adjustment document. |
| Original document linkage | Links adjustment to original fiscal document. |
| Adjustment reason | Records reason code/text. |
| Approval reference | Records required approval. |
| Refund/reversal context reference | References Central PMS/provider money movement context. |
| Reconciliation reference | Links adjustment to reconciliation. |
| Adjustment audit | Captures action evidence. |

#### Candidate Logical Attributes

- Adjustment document ID.
- Original fiscal document reference.
- Adjustment type: void, refund, cancel, return, or other confirmed adjustment.
- Adjustment document identity/status, reason, actor/service identity, approval reference, payment refund/reversal context reference, reconciliation reference, audit reference, and adjustment status.

#### Design Rules

- Adjustment documents shall reference the original fiscal document.
- POS Server owns fiscal adjustment document records.
- Central PMS and/or payment provider remain authority for money movement finality.
- Adjustment records must not silently reverse payment or mark refund final.

#### Future Physical Design Notes

Future physical design shall decide adjustment family policy storage, original-document linkage enforcement, approval references, reconciliation references, and status history. It shall not make POS Server the authority for money movement finality.

### X-read and Z-read

#### Purpose

This area stores report request/status/output data for X-read and Z-read and preserves counter behavior.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| X-read report | X-read request/status/output. |
| Z-read report | Z-read request/status/output. |
| Report scope | Site, terminal/channel, cashier/session, or combined scope. |
| Report audit | Audit record for report generation/reprint/export. |

#### Candidate Logical Attributes

- Report request reference.
- Report type: X-read or Z-read.
- Report scope.
- Period start/end.
- Beginning SI number and ending SI number.
- Z-counter and reset counter.
- Grand Total Amount before/after where applicable.
- Report status, output references, and audit reference.

#### Design Rules

- X-read shall be producible for BIR/accounting-approved operational scopes.
- Z-read shall close the applicable fiscal day for the approved fiscal scope.
- Z-counter advances per Z-reading / fiscal day close.
- Reset counter does not advance per Z-read.
- Reports must reconcile to canonical fiscal records.

#### Future Physical Design Notes

Future physical design shall decide report snapshot keys, scope references, counter boundary storage, output references, and retention/partitioning. It shall support Site POS Server scope plus optional terminal/channel and cashier/session dimensions.

### BIR Sales Summary and Annex E

#### Purpose

This area stores BIR Sales Summary / Annex E report request, output, and reconciliation data.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| BIR Sales Summary / Annex E-1 | Required fiscal sales summary. |
| Annex E-2 Senior Citizen | Immediate entitlement reporting support. |
| Annex E-3 PWD | Immediate entitlement reporting support. |
| Annex E-4 NAAC | Future-supported report structure. |
| Annex E-5 Solo Parent | Future-supported report structure. |
| Diplomat VAT reporting support | Active VAT privilege/exemption reporting support, exact treatment open. |
| Report output reference | Print/PDF/JSON output reference. |
| Report audit | Audit record for generation/export/reprint. |

#### Minimum BIR Sales Summary Content

| Content item | Required design support |
| --- | --- |
| Report Date | Yes. |
| Beginning SI Number | Yes. |
| Ending SI Number | Yes. |
| Previous Grand Total | Yes. |
| Present Grand Total | Yes. |
| Sales for the Day | Yes. |
| Gross Sales | Yes. |
| Net Sales | Yes. |
| VATable Sales | Yes. |
| VAT Amount | Yes. |
| VAT Exempt Sales | Yes. |
| Zero-Rated Sales | Yes. |
| Discounts | Yes. |
| Voids | Yes. |
| Returns | Yes. |
| Reset Counter | Yes. |
| Z Counter | Yes. |

#### Design Rules

- BIR Sales Summary is a required fiscal report, not analytics.
- BIR Sales Summary and Annex E reports must reconcile to Sales Invoice sequence, Z-counter, reset counter, Grand Total Amount, and canonical fiscal records.
- Senior Citizen and PWD are immediate entitlement workflows.
- NAAC and Solo Parent are future-supported categories whose report structures must be accommodated.
- Diplomat VAT Privilege / VAT Exemption is active as VAT privilege/exemption treatment, but exact reporting remains open.

#### Future Physical Design Notes

Future physical design shall decide report snapshot structure, output package references, entitlement report structures, and reconciliation query patterns. Exact report layouts and formulas remain subject to BIR/accounting confirmation.

### Electronic Journal, POSLog, JSON, and Exports

#### Purpose

This area stores EJ records, POSLog export records, fiscal export requests, schema/profile references, validation status, output package metadata, and retention references.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Electronic Journal record | Canonical EJ evidence generated from fiscal records. |
| POSLog export record | Structured POSLog export record/package. |
| Fiscal export request/status | Export lifecycle state. |
| Export output reference | Reference to generated export artifact. |
| Export package metadata | Package metadata for audit/support. |
| Export validation status | Pending/passed/failed validation state. |
| Export validation error | Validation failure details. |
| JSON schema version reference | Schema version reference for JSON outputs. |
| POSLog profile version reference | ARTS POSLog-aligned profile reference. |
| Local/BIR extension mapping reference | Mapping reference for Philippine BIR fields. |

#### Candidate Logical Attributes

- Fiscal document/report/audit source reference.
- Export type and output reference.
- Export package metadata.
- Export status.
- Export validation status: pending, passed, failed.
- Export validation errors.
- JSON schema version.
- POSLog schema/profile version.
- ARTS POSLog 6.x-aligned profile reference.
- Local/BIR extension mapping reference.
- Retention reference.

#### Design Rules

- JSON fiscal/audit records should be complete even when printed outputs are simplified.
- JSON/POSLog exports should be schema-versioned.
- JSON/POSLog exports should support validation against approved BIR/ARTS-aligned schemas where applicable.
- Validation failures should be auditable and visible to operational/support workflows.
- ARTS POSLog is a structured export/schema interoperability reference and does not replace Philippine BIR fiscal outputs.
- BIR/local extension mapping must preserve Sales Invoice / SI / Sales Invoice Number terminology.

#### Future Physical Design Notes

Future physical design shall decide export package storage, schema/profile version references, validation-result storage, retention, and indexing for retrieval. It shall keep ARTS POSLog as an interoperability reference, not a replacement for BIR outputs.

### Audit Trail

#### Purpose

This area stores audit evidence for fiscal actions, privileged operations, configuration changes, status changes, access, export validation, reset, recovery, and unauthorized fiscal actions.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Fiscal audit record | General fiscal audit trail. |
| Actor/service reference | Actor or service identity reference. |
| Approval reference | Approval context for privileged actions. |
| Evidence reference | Supporting evidence pointer. |
| Unauthorized action record | Unauthorized or rejected fiscal action evidence. |
| Configuration change audit | Fiscal identity/channel/terminal/configuration change evidence. |
| Status change audit | ONLINE/OFFLINE, fiscal lock, recovery, and lifecycle status evidence. |

#### Audit Coverage

Audit records shall cover SI issuance, Digital SI URL creation/access where required, reprints, void/refund/cancel/return, X-read, Z-read, BIR Sales Summary, Annex E, EJ/POSLog/export generation, export validation, fiscal reset, recovery continuity check, supervised recovery, fiscal identity changes, channel/terminal changes, RBAC/approval decisions, unauthorized fiscal actions, and ONLINE/OFFLINE status changes where required.

#### Design Rules

- Audit records must be append-only in posture and must reconcile to canonical fiscal records.
- Audit records are fiscal/audit evidence only.
- Audit records must not become payment finality or ExitAuthorization authority.
- Audit records should include enough actor, service, timestamp, reason, approval, and correlation data to support investigation and accreditation review.

#### Future Physical Design Notes

Future physical design shall decide audit object grouping, append-only enforcement posture, correlation indexes, retention/partitioning, and privileged-action linkage. Audit records remain evidence only and do not create payment finality or ExitAuthorization.

### Recovery and Tamper-Evident Continuity

#### Purpose

This area stores continuity state and supervised recovery records required to prove fiscal counters and fiscal records were not rolled back, duplicated, skipped, or tampered with after restore, failover, repair, or recovery.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Latest fiscal state | Current known fiscal continuity state. |
| Previous/last known fiscal state | Prior state used for comparison and recovery. |
| Continuity check result | Result of restore/failover/recovery validation. |
| Recovery request | Requested recovery action. |
| Supervised recovery approval | Approval record for recovery. |
| Recovery audit record | Evidence of recovery action. |
| External anchor reference | External continuity anchor if used. |
| Resume/block status | Whether fiscal issuance may resume. |

#### Candidate Logical Attributes

- Site POS Server reference.
- Latest SI sequence reference and latest adjustment sequence reference.
- Reset counter, Z-counter, Grand Total Amount accumulator, latest EJ hash, and last fiscal event timestamp.
- Previous fiscal state snapshot.
- Continuity check result.
- Recovery request reason.
- Approval reference.
- Recovery audit reference.
- Resume/block status.
- Failure reason.

#### Design Rules

- POS Server must not resume fiscal issuance from lower counters, lower Grand Total Amount, earlier SI sequence, broken EJ hash continuity, or earlier last fiscal event timestamp.
- If continuity cannot be proven, fiscal issuance must be blocked pending supervised recovery and a recovery audit record.
- Recovery records must not silently repair or rewrite fiscal history.

#### Future Physical Design Notes

Future physical design shall decide fiscal state snapshot storage, hash/reference storage, external anchor references if used, recovery block/resume state, and supervised recovery audit linkage. It shall prevent resume from lower counters, lower GTA, broken EJ hash continuity, or earlier fiscal timestamp.

### Security/RBAC and Privacy Data Impacts

#### Purpose

This area identifies database impacts for actor identity, role/permission references, approval records, sensitive evidence handling, and access audit without defining the final RBAC matrix.

#### Candidate Logical Records

| Candidate record | Purpose |
| --- | --- |
| Actor identity reference | Human or service actor reference. |
| Role/permission reference | Logical role/permission reference pending final RBAC matrix. |
| Approval record | Approval context for high-risk fiscal actions. |
| Evidence reference | Reference to supporting evidence. |
| Sensitive evidence separation marker | Supports privacy-aware evidence handling. |
| Digital SI URL access audit | Customer-facing access audit where required. |
| Taxpayer/fiscal identity change audit | Privileged configuration audit. |
| Privileged export access audit | Export access traceability. |

#### Design Rules

- High-risk fiscal actions require actor/service identity and audit evidence.
- Approval references are needed for reprint, adjustment, fiscal reset, recovery, fiscal identity configuration, export, and other privileged actions where required.
- Sensitive evidence should be separated or referenced in a way that supports privacy and retention controls.
- Digital SI URL access audit should avoid unnecessary sensitive data capture.
- Final role/permission model remains open for Security/Privacy Review and implementation design.

#### Future Physical Design Notes

Future physical design shall decide actor/service references, role/reference storage, approval linkage, sensitive evidence separation, privileged access audit, and retention. The final permission matrix remains a Security/Privacy and implementation decision.

### Retention and Archival

#### Purpose

This area defines logical retention categories and archival posture for POS Server fiscal records.

#### Retention Categories

| Category | Required support |
| --- | --- |
| Fiscal documents and fiscal lines | Retain for fiscal audit and reporting. |
| Tender/tax/discount/totals | Retain for reconciliation and reports. |
| EJ and POSLog | Retain/export according to BIR/accreditation requirements. |
| Reports and exports | Retain generated output references and validation state. |
| Audit records | Retain append-only evidence. |
| Digital SI URL access | Retain where required by security/privacy/compliance. |
| Reprints and adjustments | Retain original links, reasons, approvals, and audit. |
| Recovery records | Retain recovery continuity evidence. |
| Evidence references | Retain according to privacy and compliance rules. |
| Fiscal identity history | Retain effective-dated configuration history. |
| Channel/terminal history | Retain configuration and status history where required. |

#### Design Rules

- Retention must support BIR, accounting, audit, security, privacy, and operational requirements.
- Fiscal record retention shall preserve canonical fiscal facts and generated output references.
- Archival must not break audit traceability, report reconciliation, or recovery continuity.
- Sensitive records should support access controls and retention minimization where allowed.

#### Future Physical Design Notes

Future physical design shall decide retention categories, archival storage, legal/operational hold handling, partitioning candidates, and purge controls. Archival must not break fiscal reconstruction, report reconciliation, or recovery continuity.

### Central PMS Integration References

#### Purpose

This area stores references to Central PMS authority records required for fiscal issuance, audit, reconciliation, and exception handling while preserving Central PMS ownership.

#### References to Support

| Reference | POS Server database posture |
| --- | --- |
| Parking session reference | External authority reference only. |
| Site resolution context reference | External authority reference only. |
| PaymentAttempt reference | External authority reference only where applicable. |
| PaymentConfirmation reference | External authority reference only. |
| Payment finality reference | External authority reference only. |
| Central PMS fiscal issuance request/reference | Correlation and reconciliation reference. |
| Exception/retry correlation reference | Exception workflow reference. |
| Reconciliation reference | Fiscal reconciliation context. |
| Manual release incident/reconciliation reference | Incident context where policy allows manual release. |

#### Design Rules

- POS Server database stores Central PMS references only.
- POS Server database must not own or mutate Central PMS parking session state.
- POS Server database must not own or mutate PaymentAttempt or PaymentConfirmation authority records.
- POS Server database must not own payment finality.
- POS Server database must not own or issue ExitAuthorization.

#### Future Physical Design Notes

Future physical design shall decide reference identity formats, correlation storage, cross-reference integrity checks, and reconciliation lookup patterns. It shall use API/reference strategy rather than owning Central PMS records.

### ARTS POSLog / BIR Extension Mapping

#### Purpose

This area preserves a conceptual mapping posture for ARTS POSLog 6.x-aligned exports while retaining Philippine BIR terminology and fiscal requirements.

#### Mapping Posture

| Area | Treatment |
| --- | --- |
| ARTS POSLog | Structured export/schema interoperability reference. |
| Philippine BIR outputs | Remain required and are not replaced by ARTS POSLog. |
| Sales Invoice terminology | Preserve SI / Sales Invoice / Sales Invoice Number. |
| Local/BIR fields | Represent as mapped fields or local extensions where needed. |
| Final profile | Open for later API/database/engineering/accreditation confirmation. |

#### Candidate Mapping Concepts

- Business Unit / Site.
- Workstation / terminal.
- Business Day Date.
- Transaction sequence.
- Line item sequence.
- Tender.
- Tax.
- Discounts.
- Totals.
- Transaction status.
- Sales Invoice Number.
- Ticket Number / Plate Number.
- MIN, PTU, serial number, supplier/accreditation metadata.
- Reset counter, Z-counter, Grand Total Amount.
- Digital SI URL.
- Parking session timestamps and duration.
- Fiscal audit references.

#### Future Physical Design Notes

Future physical design shall decide schema/profile version references, local/BIR extension mapping references, validation metadata, and historical readability strategy. Final ARTS POSLog profile and mapping remain downstream.

## State-Based Versioning Plan

### Versioning Objective

Future POS Server database work shall use repository-owned, state-based database artifacts as source of truth. This draft defines the posture only and does not create those artifacts.

### Future Artifact Expectations

| Artifact type | Future expectation |
| --- | --- |
| Object definitions | Repository-owned, per object where practical. |
| Rebuild scripts | Repeatable rebuild scripts for clean environments. |
| Validation scripts | Drift and integrity validation scripts. |
| Seed/reference data | Separate from schema object definitions. |
| Atlas/state comparison | Optional future state-based comparison mechanism. |
| CI validation | Future PR checks for rebuild, validation, and drift. |
| Review evidence | Database changes should include validation output. |

### No Local Drift Promotion Rule

Local database drift must not become the baseline without explicit review and repository update. Any change discovered in a local database must be expressed as reviewed repository artifacts before it can become source of truth.

## Open Questions

This section now distinguishes resolved logical design defaults from items that still require external or downstream confirmation. The detailed resolution record is maintained in `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_Open_Questions_Resolution_Addendum.md`.

| Owner/dependency | Question / topic | Source or basis | Recommended answer / default posture | Classification | Blocks logical DB design | Blocks physical DB design | Blocks implementation | Downstream workstream |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| BIR/accounting | MIN/PTU/serial/software/supplier assignment | BRD/System Design/API Contract open item | Support configurable Site POS Server-level and channel/terminal-level fiscal identity assignments. | Partially resolved; final BIR/accounting confirmation required | No | May block final uniqueness/effective-dating rules | May block accreditation-ready implementation | BIR/accounting; physical DB design; accreditation package |
| BIR/accreditation | WebPay fiscal terminal identity | API Contract and database planning open item | Treat WebPay as logical channel/terminal under Site POS Server, with logical identity such as `WEBPAY-{SITE}` or approved equivalent. | Resolved as default posture | No | No | May block final label/accreditation wording | Accreditation package; physical DB design |
| BIR/accounting | Exact Sales Invoice numbering pattern | BRD/API Contract open item | Site POS Server-scoped configurable SI sequence policy; display number separate from internal fiscal document ID; do not append reset counter unless confirmed. | Resolved for logical design; final format confirmation required | No | Yes, for final numbering implementation | Yes, for production fiscal issuance | BIR/accounting; physical DB design |
| BIR/accounting | Exact adjustment document numbering pattern | BRD/API Contract open item | Separate configurable sequences per BIR-confirmed adjustment document family; all adjustments link to original fiscal document. | Resolved for logical design; final format/family confirmation required | No | Yes, for final numbering implementation | Yes, for production adjustments | BIR/accounting; physical DB design |
| BIR/accounting | Sequence gaps, reserved numbers, failed issuance, abandoned issuance | API Contract retry/idempotency requirements | Do not consume SI number until commit; retain permanent gap/audit record after failed reservation; never reuse consumed numbers; do not reuse reserved numbers unless approved. | Resolved as default posture; official gap treatment confirmation required | No | Yes, for final sequence implementation | Yes, for production fiscal numbering | BIR/accounting; physical DB design; Engineering Pack |
| BIR/accounting | X-read and Z-read aggregation scope | BRD/System Design open item | Support Site POS Server as primary fiscal scope plus optional terminal/channel and cashier/session dimensions. | Resolved as flexible logical design; required scope confirmation needed | No | May block report snapshot keys and counter boundary implementation | May block fiscal close implementation | BIR/accounting; physical DB design |
| BIR/accounting | Exact VAT/tax treatment | BRD open item | Support all fiscal classifications; do not decide accounting treatment in DB design. | Remains open external confirmation | No | No, if flexible classifications remain | Yes, for final formulas and expected values | Finance/accounting; BIR/accounting; Engineering Pack |
| BIR/accounting; Security/Privacy | Diplomat VAT treatment, evidence, wording, reporting, retention | BRD and RMO 10-2019 concern | Model as active VAT privilege/exemption; store evidence references by default; final wording/evidence/reporting/retention remains external. | Partially resolved; final BIR/accounting/security/privacy confirmation required | No | May block evidence/retention details | Yes, for final validation/reporting behavior | BIR/accounting; Security/Privacy; physical DB design |
| Security/Privacy | Digital SI URL token/access/expiry/authentication model | API Contract and privacy open item | Use opaque, non-guessable tokenized URL; read-only customer view; minimum data exposure; support active/expired/revoked/blocked and policy-configurable expiry. | Partially resolved; final Security/Privacy confirmation required | No | May block token/auth/expiry implementation | Yes, for customer-facing access | Security/Privacy; API; physical DB design |
| BIR/accreditation; Engineering Pack | Exact report/export formats and layouts | BIR/ARTS impact review | Support Print, PDF, JSON, EJ, POSLog, BIR Sales Summary, Annex E, audit trail report, and structured export packages. | Partially resolved; exact layouts/package downstream | No | No, if metadata remains flexible | May block final export jobs | Accreditation package; Engineering Pack |
| BIR/accreditation; Engineering Pack | Exact ARTS POSLog profile and schema mapping | BIR/ARTS impact review | Use ARTS POSLog 6.x as default structured export reference where practical and accepted; preserve BIR terms/outputs through mappings/extensions. | Resolved as default posture; final profile/mapping downstream | No | No, if profile/version fields remain flexible | May block validation/accreditation samples | Accreditation package; Engineering Pack |
| Engineering Pack | Exact JSON schema versioning strategy | BIR/ARTS impact review | Store explicit schema/profile version fields and keep old schema versions readable/reconstructible. | Resolved as default posture | No | No | May block export implementation details | Engineering Pack; physical DB design |
| BIR/accreditation | Final accreditation sample package | BIR examiner materials | Minimum target package includes SI, X/Z, EJ, POSLog, BIR Sales Summary/Annex E, audit, JSON/PDF/print, reprints, validation evidence, identity evidence, and recovery/counter continuity evidence. | Resolved as minimum target; final examiner confirmation required | No | No | May block accreditation package completion | Accreditation package; Engineering Pack |
| Security/Privacy; Engineering Pack | Tamper-evident anchoring mechanism | System Design recovery requirements | Default posture: append-only audit chain, hash chaining where practical, external checkpoints where practical, latest EJ hash, counters, GTA, snapshots, supervised recovery. | Resolved as default posture; final mechanism downstream | No | May block final continuity implementation | Yes, for production recovery continuity | Security/Privacy; Engineering Pack; physical DB design |
| API Contract detail | Final endpoint names and DTOs | API Contract non-decision | Route families are baseline; DTOs follow API semantics and must not mirror DB tables directly. | Resolved as API/database design principle | No | No | May block API implementation | API Contract detail; Engineering Pack |
| Engineering Pack; event contract | Final event payloads | API Contract event non-decision | POS/fiscal events are audit/integration/observability only and do not grant payment finality or ExitAuthorization. | Resolved as event authority principle; final payloads downstream | No | No | May block event implementation | Engineering Pack; event contract |
| Security/Privacy | Final RBAC matrix | BRD/System Design security open item | Use role baseline: Cashier, Supervisor, Fiscal Administrator, Compliance Auditor, Recovery/DR Approver, System Administrator, Service Identity, Channel/Terminal Identity. | Resolved as role baseline; final matrix downstream | No | No, if references remain flexible | May block privileged-action implementation | Security/Privacy; Engineering Pack |
| Physical database design | Final table/column/index/constraint design | Database design scope boundary | Do not decide in logical design; handle in future physical DB/state-based object artifact task. | Intentionally downstream physical database design | No | Yes | Yes, for database implementation | Physical database design |
| BIR/accounting; Operations | Offline fiscal issuance approval, if any | BRD/System Design/API Contract rule | Default is not allowed. ONLINE/OFFLINE is observability only. Offline fiscal issuance requires explicit approval and compliant sequence/counter/evidence/reconciliation/recovery model. | Resolved as disabled by default; open only if later approval is sought | No | No | No for default disabled posture | BIR/accounting; Operations; Engineering Pack |


## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Authority leakage into POS Server database | POS Server could appear to own payment finality or ExitAuthorization. | Store Central PMS records as references only and explicitly prohibit POS Server ownership of payment finality and ExitAuthorization. |
| Accidental ExitAuthorization ownership | Gate/exit authority could bypass Central PMS. | Do not model ExitAuthorization lifecycle as POS Server-owned data. |
| Payment finality confusion | Fiscal issuance could be mistaken for payment finality. | Preserve Central PMS payment finality reference as external authority context. |
| Fiscal sequence gaps or duplicate documents | BIR audit and reconciliation failure. | Preserve idempotency, sequence state, status lookup, and sequence-gap open questions before physical design. |
| Rollback or stale recovery | Fiscal records may resume from lower counters or broken continuity. | Preserve counter/GTA/EJ hash/timestamp state and supervised recovery blocks. |
| Tax/VAT misclassification | Incorrect fiscal reports and compliance exposure. | Store explicit fiscal lines and keep exact VAT/tax treatment open for BIR/accounting confirmation. |
| Digital SI URL privacy exposure | Customer or fiscal data may be exposed. | Store minimal URL/access references and defer token/auth/expiry model to Security/Privacy Review. |
| ARTS POSLog replacing BIR requirements | Local compliance outputs could be weakened. | Treat ARTS as export/schema reference only and preserve BIR terminology and outputs. |
| Offline fiscal issuance implied by status fields | Unapproved fiscal issuance could occur during degraded operation. | Treat ONLINE/OFFLINE as observability only; offline fiscal issuance remains disabled/restricted unless approved. |
| Export validation mismatch | Accreditation sample or operational exports may fail. | Plan schema-versioned JSON/POSLog exports with validation status and auditable failures. |
| Local database drift | Unreviewed database state could become de facto baseline. | Use repository-owned state-based database artifacts and drift checks in later implementation. |
| Over-specification before confirmations | Physical schema may lock in wrong compliance decisions. | Keep final DDL, fields, constraints, indexes, profiles, and retention periods open until confirmations complete. |

## Source Traceability

| Database design area | Draft section(s) | Approved BRD | Approved System Design | Approved API Contract | BIR/ARTS impact review | DB planning artifacts |
| --- | --- | --- | --- | --- | --- | --- |
| Authority separation and repository boundary | Document Control; Design Principles; Database Overview; Central PMS Integration References | POS/Invoicing scope and authority model | Authority model and repository boundary | API ownership and trust boundaries | No direct override | Source analysis, decision log, impact map |
| Fiscal identity | Fiscal Identity and Site POS Server Boundary | Sales Invoice identity/header/footer and open identity questions | Fiscal identity model | Fiscal identity API family | Supplier/footer/accreditation fields | Source analysis, open questions |
| Channel/terminal registry | Channel and Terminal Registry | Channel coverage and Site POS Server model | Channel/terminal registration | Channel/terminal registry API family | APM presenter, not fiscal authority | Decision log, impact map |
| Fiscal documents and SI lifecycle | Fiscal Documents; Numbering and Counter State | Sales Invoice primary fiscal output | SI lifecycle | Fiscal issuance/document API families | Sales Invoice terminology | Source analysis, decision log |
| Fiscal lines and totals | Fiscal Lines; Tender, Tax, Discount, and Totals | Fiscal line model and tax treatment open item | Fiscal line model | Fiscal issuance/document/report API families | Structured detail and POSLog mapping | Source analysis, impact map |
| Digital SI URL and QR boundary | Digital SI URL and Access Audit; QR Presentation Data Boundary | Printed/digital Sales Invoice delivery | Digital SI URL and QR model | Digital SI URL/presentation API family | QR responsibility decision | Decision log, impact map |
| Reprints | Reprints | Reprint controls and labels | Reprint controls | Reprint API family | SI/X/Z/EJ reprint labels | BIR/ARTS impact review |
| BIR Sales Summary and Annex E | BIR Sales Summary and Annex E | Required fiscal report | Reporting services | Report API family | Annex E-1 minimum content | Source analysis, impact map |
| EJ/POSLog/JSON/exports | Electronic Journal, POSLog, JSON, and Exports; ARTS POSLog / BIR Extension Mapping | Canonical digital records | Export model | Export API family | ARTS POSLog and schema validation | BIR/ARTS impact review |
| Recovery and counters | Numbering and Counter State; Recovery and Tamper-Evident Continuity | Reset/Z/GTA requirements | Recovery continuity model | Reset/recovery API family | Counter continuity | Source analysis, open questions |
| Security/privacy and audit | Audit Trail; Security/RBAC and Privacy Data Impacts; Retention and Archival | Security, privacy, retention requirements | Security/RBAC, privacy, recovery controls | Auth/trust boundaries and audit impact | Evidence and audit trail report inputs | Source analysis, impact map |
| State-based versioning | State-Based Versioning Plan; Appendix C | Not a BRD concern | Downstream artifact posture | API/DB alignment | No direct override | DB planning package |


## Non-Decisions

This draft does not decide:

- Final SQL DDL.
- Final table names.
- Final column names.
- Final constraints or indexes.
- Final physical schemas.
- Final enum implementation.
- Final Atlas/state files.
- Final migration approach.
- Final endpoint DTOs.
- Final event payloads.
- Final RBAC matrix.
- Exact Sales Invoice numbering pattern.
- Exact adjustment document numbering pattern.
- Sequence-gap, reserved-number, failed-issuance, and abandoned-issuance treatment.
- Exact X/Z scope.
- Exact tax/VAT treatment.
- Exact Diplomat VAT Privilege / VAT Exemption treatment.
- Exact Digital SI URL access model.
- Exact ARTS POSLog profile.
- Exact JSON schema versioning.
- Exact accreditation sample package.
- Offline fiscal issuance approval.

## Appendices

### Appendix A: Glossary

| Term | Meaning |
| --- | --- |
| Central PMS | ExitPass central platform component that owns parking session control, site resolution, payment finality, and ExitAuthorization. |
| Site POS Server | Site-level fiscal authority component that owns POS Server fiscal issuance and fiscal records for the resolved Site. |
| Sales Invoice | Primary parking fiscal output for ExitPass POS/Invoicing. |
| Digital SI URL | POS Server-returned URL that allows a parker/customer to view and save the issued Sales Invoice where digital delivery is enabled. |
| QR presentation | Channel/terminal conversion of the Digital SI URL into a QR code where supported. It does not make the channel/terminal the fiscal issuer. |
| Grand Total Amount | Fiscal accumulator preserved for BIR reporting, reset, and recovery continuity. |
| Reset counter | Counter that starts at zero and increments only on fiscal reset. |
| Z-counter | Counter that advances per Z-reading / fiscal day close. |
| EJ | Electronic Journal. |
| POSLog | Structured POS log/export record set; ARTS POSLog 6.x is a supporting interoperability reference. |

### Appendix B: Acronyms

| Acronym | Meaning |
| --- | --- |
| APM | AutoPay Machine |
| API | Application Programming Interface |
| ARTS | Association for Retail Technology Standards |
| BIR | Bureau of Internal Revenue |
| BRD | Business Requirements Document |
| DTO | Data Transfer Object |
| EC | Emergency/Exception/Continuity, pending final terminology where used |
| EJ | Electronic Journal |
| GTA | Grand Total Amount |
| MIN | Machine Identification Number |
| POS | Point of Sale |
| PTU | Permit to Use |
| QR | Quick Response code |
| RBAC | Role-Based Access Control |
| SI | Sales Invoice |
| TIN | Taxpayer Identification Number |
| VAT | Value-Added Tax |

### Appendix C: Physical Design Gate

Before physical database artifacts are created, the next database task must confirm the following gate items. This checklist is a readiness gate only; it does not create SQL, Atlas files, migrations, or physical object files.

| Gate item | Required before physical artifact work |
| --- | --- |
| Target database engine confirmed | Yes. |
| Schema/domain decomposition approved | Yes. |
| Naming standards approved | Yes. |
| Object-level folder structure approved | Yes. |
| State-based versioning workflow approved | Yes. |
| Rebuild script plan approved | Yes. |
| Drift-check plan approved | Yes. |
| Validation script plan approved | Yes. |
| Seed/reference data separation approved | Yes. |
| Enum versus controlled-code strategy approved | Yes. |
| Idempotency uniqueness strategy approved | Yes. |
| Fiscal numbering/counter strategy confirmed or placeholder policy approved | Yes. |
| Retention/partitioning strategy reviewed | Yes. |
| Digital SI URL security model reviewed | Yes. |
| BIR/accreditation output/export expectations reviewed | Yes. |
| ARTS POSLog profile/schema mapping reviewed | Yes. |
| Tamper-evident anchoring approach reviewed | Yes. |
| No local drift promotion rule confirmed | Yes. |

Physical design must preserve this logical database design, the approved authority model, and all unresolved external-confirmation boundaries.
