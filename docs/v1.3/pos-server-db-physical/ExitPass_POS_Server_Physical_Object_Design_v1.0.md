# ExitPass POS Server Physical Object Design v1.0

## 1. Document Control

| Field | Value |
| --- | --- |
| Title | ExitPass POS Server Physical Object Design |
| Version | v1.0 |
| Repository | `ExitPass-PoSServer` |
| Status | Draft for review |
| Output format | Markdown only |
| Approved BRD baseline | `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` |
| Approved System Design baseline | `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` |
| Approved API Contract baseline | `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` |
| Approved Database Design baseline | `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0.md` |
| Approved Physical DB Artifact Plan baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Plan_v1.0.md` |
| Approved Gate Resolution baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Gate_Resolution_v1.0.md` |
| Approved Schema/Naming Standards baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Schema_Naming_Standards_v1.0.md` |
| Physical artifact status | No SQL, Atlas, migrations, physical object files, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, or diagrams are created by this draft. |

## 2. Approval / Baseline Status

This document is a draft for review. It proposes a physical object design at documentation level only. Approval of this draft, if granted later, must not authorize SQL/object artifact creation unless a separate task explicitly approves those artifacts.

All proposed object names remain provisional until a future SQL/object artifact task creates actual files under `db/state`.

## 3. Purpose and Scope

This document defines the proposed POS Server physical object design at document/design level only. It translates the approved logical database design, approved physical artifact plan, approved gate resolution, approved schema/naming standards, and approved object design planning package into a structured future object design baseline.

In scope:

- proposed object groups;
- proposed candidate physical object names;
- object dependencies;
- key/reference strategy;
- authority-boundary safeguards;
- validation implications;
- drift-check implications;
- future SQL/object artifact sequencing;
- open questions and blockers.

Out of scope:

- SQL DDL;
- Atlas files;
- migrations;
- physical object files;
- actual table creation;
- actual constraints/indexes;
- seed/reference/sample data;
- validation/rebuild/drift scripts;
- CI workflows;
- source code;
- DOCX files;
- diagrams.

## 4. Approved Baseline References

| Source | Role in this draft |
| --- | --- |
| POS/Invoicing BRD v1.0 | Business and fiscal scope baseline. |
| POS Server System Design v1.0 | Architecture, authority, operational, audit, and recovery baseline. |
| POS Server API Contract v1.0 | API family, status, idempotency, Digital SI URL, reprint, report, and export behavior baseline. |
| POS Server Database Design v1.0 | Approved logical persistence baseline. |
| Physical DB Artifact Plan v1.0 | State-based object layout, validation, rebuild, and drift posture. |
| Physical DB Gate Resolution v1.0 | Gate readiness, placeholder policies, and pending confirmations. |
| Schema/Naming Standards v1.0 | Approved `pos` schema posture and naming safeguards. |
| Physical Object Design Source Analysis | Approved planning baseline and carry-forward P2 items. |
| Physical Object Design planning files | Sequencing, grouping, open questions, impact map, and outline inputs. |
| Repository Boundary and `db/README.md` | Repository ownership and artifact boundary. |

## 5. Repository and Artifact Boundary

POS Server database artifacts belong inside `ExitPass-PoSServer`. The `db/` folder is a folder boundary, not a separate repository. There is no separate `ExitPass-PoSServer-Db` repository at this stage.

Future state-based database artifacts remain the repository-owned source of truth. Local database drift must not become baseline unless expressed as reviewed repository artifacts and merged through pull request review.

This draft does not modify `db/README.md` and does not create files under `db/state`, `db/reference-data`, `db/validation`, `db/rebuild`, `db/drift`, `db/samples`, or `db/accreditation`.

## 6. Authority Boundary

The physical object design must preserve these authority rules:

- Central PMS owns payment finality.
- Central PMS owns PaymentAttempt and PaymentConfirmation.
- Central PMS owns ExitAuthorization.
- POS Server owns fiscal issuance and fiscal records only.
- POS Server database stores references to Central PMS authority records only.
- POS Server database does not own payment finality.
- POS Server database does not own, issue, or mutate ExitAuthorization.
- Audit and events are evidence only.
- Channels/terminals are child endpoints under Site POS Server and are not independent fiscal authorities.
- ONLINE/OFFLINE is observability only.
- Offline fiscal issuance is disabled by default unless BIR/accounting approves a compliant model.
- ARTS POSLog is a structured export reference only and does not replace BIR outputs.

## 7. Physical Design Gate Status

| Gate item | Current posture for this draft | SQL/object artifact readiness |
| --- | --- | --- |
| PostgreSQL version/features/extensions/hosting | PostgreSQL is default engine; final details pending. | Blocked. |
| Schema/domain decomposition | Primary `pos` schema posture approved; object areas proposed. | Blocked until artifact task. |
| Naming standards | Approved baseline applied. | Names still provisional. |
| Object-level folder structure | Bootstrap exists; no object files created. | Blocked until artifact task. |
| Enum vs controlled-code strategy | Strategy exists; per-domain decision pending. | Blocked per domain. |
| Idempotency uniqueness | Placeholder strategy exists. | Blocked until final constraints/indexes. |
| Fiscal numbering/counter strategy | Placeholder strategy exists; BIR/accounting confirmation pending. | Blocked for production artifacts. |
| Retention/partitioning | Categories known; periods and partition keys pending. | Blocked. |
| Digital SI URL security | Security posture exists; token/auth/expiry details pending. | Blocked for URL objects. |
| BIR/accreditation outputs | Minimum target known; final package pending. | Blocked for sample/export artifacts. |
| ARTS POSLog mapping | Posture exists; final profile/mapping pending. | Blocked for final export artifacts. |
| Tamper-evident anchoring | Posture exists; mechanism pending. | Blocked for recovery/anchor artifacts. |
| CI/rebuild/validation/drift tooling | Planned only. | Blocked for scripts/workflows. |

## 8. Schema and Naming Baseline

Approved naming baseline:

- primary PostgreSQL schema posture: `pos`;
- lowercase `snake_case`;
- no quoted identifiers;
- `_id` for internal identifiers;
- `_ref` or explicit source prefixes for external authority references;
- domain-specific status names;
- fiscal numbers distinct from internal IDs;
- Central PMS records named as references only;
- no names implying POS Server owns payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, ExitAuthorization, gate execution, vendor authority, or independent terminal fiscal authority.

Candidate names in this draft are examples only.

## 9. Physical Object Design Principles

| Principle | Design rule |
| --- | --- |
| Authority separation | POS Server objects may reference Central PMS authority records but must not own their lifecycle. |
| Fiscal source of truth | POS Server objects represent fiscal issuance, fiscal records, fiscal outputs, audit, and recovery. |
| State-based readiness | Future physical objects must support clean rebuild, validation, drift checks, and PR evidence. |
| Dependency-first design | Foundation, identity, and channel registry precede fiscal issuance and output groups. |
| Reference clarity | External values use `_ref` or source-specific reference names. |
| Provisional names | Candidate names do not become physical artifacts until a future approved artifact task. |
| BIR terminology | Sales Invoice, SI, EJ, BIR, VAT, MIN, PTU, GTA, X-read, Z-read, and Annex terms are preserved. |
| ARTS posture | ARTS POSLog remains an export/schema reference, not a replacement for BIR outputs. |

## 10. Physical Object Design Sequence

| Step | Group | Dependency-safe reason |
| --- | --- | --- |
| 1 | Foundation and controlled codes | Establishes common status/reason/type posture before dependent objects. |
| 2 | Fiscal identity and Site POS Server boundary | Defines fiscal authority before channels and fiscal documents. |
| 3 | Channel/terminal registry | Defines child endpoints and capabilities before issuance records reference them. |
| 4 | Fiscal document core | Establishes canonical Sales Invoice and adjustment headers. |
| 5 | Fiscal lines, tenders, tax, discount, and totals | Depends on fiscal document core and supports reports/exports. |
| 6 | Numbering and counter state | Depends on fiscal authority/document scope; needed before production issuance. |
| 7 | Idempotency and retry | Wraps side-effecting fiscal operations and duplicate prevention. |
| 8 | Digital SI URL and access audit | Depends on issued SI records and Security/Privacy posture. |
| 9 | Reprints and fiscal adjustments | Depend on original document/report references and audit/approval references. |
| 10 | Reports: X-read, Z-read, BIR Sales Summary, Annex E | Depend on canonical documents, lines, totals, and counters. |
| 11 | EJ, POSLog, JSON, and exports | Depend on canonical fiscal records and report/export profile posture. |
| 12 | Audit trail | Cross-cuts prior groups and validates authority-sensitive actions. |
| 13 | Recovery and tamper-evident continuity | Depends on counters, fiscal state, EJ hash, and audit chain planning. |
| 14 | Security/privacy references | Finalizes actor, approval, evidence, and access references after object scope is known. |
| 15 | Optional events/outbox | Last because events must not become authority and depend on event contract decisions. |

## 11. Explicit First Physical Object Design Slice

Recommended first future physical object design slice:

- foundation / configuration / controlled-code posture;
- fiscal identity / Site POS Server boundary;
- channel/terminal registry;
- Central PMS reference naming posture.

The first slice should avoid:

- fiscal document issuance;
- SI/adjustment numbering;
- counters;
- reports;
- exports;
- recovery/anchoring;
- sensitive Digital SI URL security objects;
- optional outbox/events.

Those areas should wait until their relevant gates are resolved or placeholder policies are explicitly accepted. This keeps the first slice focused on low-risk boundary and reference foundations without creating fiscal issuance side effects.

## 12. Object Area Readiness Matrix

| Object area | Proposed design readiness | SQL/object artifact readiness |
| --- | --- | --- |
| Foundation and controlled codes | Ready for design draft. | Blocked pending enum/control-code decisions and artifact task. |
| Fiscal identity / Site POS Server boundary | Ready for design draft. | Blocked pending final identity fields and artifact task. |
| Channel/terminal registry | Ready for design draft. | Blocked pending final registry fields and artifact task. |
| Fiscal document core | Ready for design draft with provisional names. | Blocked pending object design, numbering, and reference strategy. |
| Fiscal lines/tender/tax/discount/totals | Ready for design draft with tax open questions. | Blocked pending VAT/tax and object design. |
| Numbering/counters | Ready for conceptual physical design. | Blocked pending BIR/accounting confirmation or placeholder approval. |
| Idempotency/retry | Ready for conceptual physical design. | Blocked pending final uniqueness/index strategy. |
| Digital SI URL/access audit | Ready for conceptual physical design. | Blocked pending Security/Privacy Review. |
| Reprints/adjustments | Ready for conceptual physical design. | Blocked pending adjustment families and approvals. |
| Reports | Ready for conceptual physical design. | Blocked pending X/Z scope and layout details. |
| EJ/POSLog/JSON/exports | Ready for conceptual physical design. | Blocked pending ARTS/BIR mapping and validation details. |
| Audit trail | Ready for conceptual physical design. | Blocked pending retention/partitioning and append-only posture. |
| Recovery/continuity | Ready for conceptual physical design. | Blocked pending anchoring/recovery mechanism. |
| Security/privacy references | Ready for conceptual physical design. | Blocked pending RBAC/evidence/privacy decisions. |
| Integration references | Ready for conceptual physical design. | Blocked pending reference formats. |
| Optional events/outbox | Not ready until approved. | Blocked pending event contract decision. |

## 13. Foundation and Controlled-Code Objects

| Item | Design posture |
| --- | --- |
| Purpose | Define future status/type/reason/classification value foundations. |
| Candidate groups | Status families, type families, reason-code families, BIR/report classification code families. |
| Candidate physical object names | Provisional examples: `*_codes`, `*_code_sets`, `fiscal_document_status_codes`, `adjustment_reason_codes`. |
| Key/reference posture | Internal code identifiers use `_id` only if represented as records; external/BIR values retain source references where needed. |
| Dependencies | Schema/naming baseline. |
| Unresolved gates | Enum vs controlled-code per domain; reference-data format. |
| Authority boundary | Codes must not imply payment/exit authority or offline fiscal issuance approval. |
| Validation implications | Validate controlled-code inventory, historical readability, and absence of authority-leaking values. |
| SQL/object readiness | Blocked. |

## 14. Fiscal Identity and Site POS Server Boundary Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent Site POS Server fiscal authority, taxpayer identity, registration metadata, and effective-dated fiscal identity assignments. |
| Candidate groups | Fiscal identity records, Site POS Server boundary records, taxpayer/business unit references, registration metadata, effective-dated identity history. |
| Candidate physical object names | Provisional examples: `fiscal_identities`, `site_pos_servers`, `site_pos_server_fiscal_identity_history`. |
| Key/reference posture | POS-owned identities use `_id`; Central PMS site/business references use `central_pms_*_ref`; supplier/vendor values use `_ref`. |
| Dependencies | Foundation/controlled-code posture. |
| Unresolved gates | MIN/PTU/serial/software/supplier assignment; supplier/accreditation metadata. |
| Authority boundary | Site POS Server is fiscal authority for resolved Site; channel/terminal records are children. |
| Validation implications | Validate effective dating, active fiscal identity readiness, and reference-only Central PMS values. |
| SQL/object readiness | Blocked. |

## 15. Channel and Terminal Registry Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent channels/terminals, association to Site POS Server, presentation capabilities, health state, and registry history. |
| Candidate groups | Channel/terminal registry, capability records, health/status history, configuration audit linkage. |
| Candidate physical object names | Provisional examples: `channel_terminals`, `channel_terminal_capabilities`, `channel_terminal_status_history`. |
| Key/reference posture | Channel/terminal records use POS-owned `_id`; logical/physical external identifiers use `_ref` or explicit source fields. |
| Dependencies | Fiscal identity and Site POS Server boundary. |
| Unresolved gates | Logical vs physical terminal identity, WebPay identity, final capability fields. |
| Authority boundary | Channels/terminals are child endpoints, not independent fiscal authorities; ONLINE/OFFLINE is observability only. |
| Validation implications | Validate active/inactive/degraded/continuity states, QR presentation capability, and no offline issuance enablement. |
| SQL/object readiness | Blocked. |

## 16. Fiscal Document Core Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent canonical Sales Invoice and fiscal adjustment document headers, status history, original document links, and external authority references. |
| Candidate groups | Fiscal document records, fiscal document status history, original document linkage, Central PMS reference context. |
| Candidate physical object names | Provisional examples: `fiscal_documents`, `fiscal_document_status_history`, `fiscal_document_links`. |
| Key/reference posture | Internal document identifiers use `_id`; Sales Invoice number is separate from ID; Central PMS values use `central_pms_*_ref` or `payment_finality_ref`. |
| Dependencies | Fiscal identity, channel registry, foundation codes. |
| Unresolved gates | Final document families, reference integrity strategy, final table/column definitions. |
| Authority boundary | Fiscal documents may reference Central PMS finality context but do not create payment finality or ExitAuthorization. |
| Validation implications | Validate no duplicate fiscal documents, correct issuer boundary, and reference-only Central PMS data. |
| SQL/object readiness | Blocked. |

## 17. Fiscal Lines, Tender, Tax, Discount, and Totals Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent ordered fiscal lines and fiscal financial detail needed for SI, BIR reports, EJ, POSLog, JSON exports, and audit. |
| Candidate groups | Fiscal lines, tender context, tax details, discount/privilege details, fiscal totals. |
| Candidate physical object names | Provisional examples: `fiscal_document_lines`, `fiscal_tenders`, `fiscal_tax_details`, `fiscal_totals`. |
| Key/reference posture | Lines use internal `_id` plus line sequence posture; payment/provider values remain `_ref`; fiscal classification codes use controlled-code posture. |
| Dependencies | Fiscal document core. |
| Unresolved gates | VAT/tax treatment, Diplomat VAT treatment, controlled-code strategy. |
| Authority boundary | Tender/payment references are context only; money movement finality remains outside POS Server. |
| Validation implications | Validate line ordering, totals reconciliation, VAT classification, discounts/privileges, and report/export support. |
| SQL/object readiness | Blocked. |

## 18. Numbering and Counter State Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent SI sequence state, adjustment sequence state, reset counter, Z-counter, GTA, fiscal state snapshots, and gap audit posture. |
| Candidate groups | Sequence state, counter state, fiscal state snapshot, gap audit, fiscal lock/block state. |
| Candidate physical object names | Provisional examples: `fiscal_sequence_states`, `fiscal_counter_states`, `fiscal_state_snapshots`, `sequence_gap_audit`. |
| Key/reference posture | Sequence/counter records are scoped to Site POS Server or approved adjustment family; display numbers remain separate from internal IDs. |
| Dependencies | Fiscal identity and fiscal document core. |
| Unresolved gates | BIR/accounting numbering format, sequence gap treatment, production counter policy. |
| Authority boundary | Numbering/counter state is fiscal evidence only and does not authorize payment or exit. |
| Validation implications | Validate monotonic counters, no reuse, permanent gap audit where applicable, GTA continuity, and Z/reset behavior. |
| SQL/object readiness | Blocked. |

## 19. Idempotency and Retry Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent idempotency keys, scope, semantic request identity, replay/conflict state, timeout, completion unknown, and retry status. |
| Candidate groups | Idempotency records, operation correlation, retry status, exception references. |
| Candidate physical object names | Provisional examples: `idempotency_records`, `fiscal_operation_retries`, `fiscal_operation_exceptions`. |
| Key/reference posture | Idempotency scope and key uniqueness are future constraint/index decisions; linked operations use POS-owned `_id` or domain refs. |
| Dependencies | Fiscal operation targets and foundation codes. |
| Unresolved gates | Exact uniqueness/index strategy and retention. |
| Authority boundary | Idempotency protects fiscal side effects only; it does not create payment finality or ExitAuthorization. |
| Validation implications | Validate duplicate prevention, replay results, conflict handling, and status lookup before retry. |
| SQL/object readiness | Blocked. |

## 20. Digital SI URL and Access Audit Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent Digital SI URL lifecycle, token/access references, read-only customer view posture, and access audit where required. |
| Candidate groups | Digital SI URL records, lifecycle/status history, access audit records. |
| Candidate physical object names | Provisional examples: `digital_si_urls`, `digital_si_url_status_history`, `digital_si_url_access_audit`. |
| Key/reference posture | URL records link to issued fiscal documents; token/access values remain security-sensitive references. |
| Dependencies | Fiscal document core and Security/Privacy posture. |
| Unresolved gates | Token/auth/expiry policy, access audit retention, data minimization. |
| Authority boundary | URL access cannot mutate fiscal records; QR generation is channel/terminal presentation behavior. |
| Validation implications | Validate active/expired/revoked/blocked lifecycle, same issued SI as print, and no fiscal mutation path. |
| SQL/object readiness | Blocked. |

## 21. Reprint Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent controlled reprint requests, history, metadata, labels, timestamps, actor/approval references, and audit linkage. |
| Candidate groups | Reprint request records, reprint status history, reprint output references. |
| Candidate physical object names | Provisional examples: `reprint_requests`, `reprint_status_history`, `reprint_output_refs`. |
| Key/reference posture | Reprint records reference original document/report/output; actor and approval use references. |
| Dependencies | Fiscal document core, reports, audit/security references. |
| Unresolved gates | Approval workflow, output reference model. |
| Authority boundary | Reprints do not mutate original fiscal facts. |
| Validation implications | Validate original linkage, reprint reason, `REPRINT`, `DATE / TIME REPRINTED`, and audit references. |
| SQL/object readiness | Blocked. |

## 22. Fiscal Adjustment Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent void/refund/cancel/return and other BIR-confirmed fiscal adjustment documents and links to originals. |
| Candidate groups | Adjustment records, adjustment status history, approval/reconciliation references. |
| Candidate physical object names | Provisional examples: `fiscal_adjustments`, `fiscal_adjustment_status_history`, `fiscal_adjustment_links`. |
| Key/reference posture | Adjustment identifiers are internal; adjustment document numbers are separate; payment refund/reversal context uses `_ref`. |
| Dependencies | Fiscal document core, fiscal details, foundation codes. |
| Unresolved gates | Adjustment families, numbering, refund/reversal relationship, approvals. |
| Authority boundary | POS Server does not own money movement finality. |
| Validation implications | Validate original fiscal document linkage, reason, approval, reconciliation, and audit. |
| SQL/object readiness | Blocked. |

## 23. X-read and Z-read Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent X-read/Z-read requests, scope, status, output references, and fiscal close/counter context. |
| Candidate groups | X/Z report records, report scope records, output references. |
| Candidate physical object names | Provisional examples: `x_z_reports`, `x_z_report_scopes`, `x_z_report_outputs`. |
| Key/reference posture | Report records use POS-owned IDs; scope references may point to Site POS Server, channel/terminal, cashier/session dimensions. |
| Dependencies | Fiscal documents, lines/totals, numbering/counters. |
| Unresolved gates | Exact X/Z aggregation scope and final layouts. |
| Authority boundary | Reports are fiscal outputs, not payment or exit authority. |
| Validation implications | Validate report period, scope, SI ranges, Z/reset counters, GTA before/after, and output audit. |
| SQL/object readiness | Blocked. |

## 24. BIR Sales Summary and Annex E Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent BIR Sales Summary / Annex E report requests, output metadata, scopes, totals, and supporting Annex E variants. |
| Candidate groups | BIR report records, Annex E report records, report output references. |
| Candidate physical object names | Provisional examples: `bir_sales_summary_reports`, `annex_e_reports`, `bir_report_outputs`. |
| Key/reference posture | Report outputs reference canonical fiscal records and approved periods; sample/accreditation files are not created here. |
| Dependencies | Fiscal documents, details, counters, reports. |
| Unresolved gates | Exact layouts, output package, Annex applicability. |
| Authority boundary | Reports remain fiscal reporting evidence. |
| Validation implications | Validate minimum contents: report date, beginning/ending SI, previous/present GTA, sales, VAT, discounts, voids, returns, reset counter, Z counter. |
| SQL/object readiness | Blocked. |

## 25. EJ, POSLog, JSON, and Export Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent EJ records, POSLog/JSON export requests, package metadata, schema/profile versions, validation status/errors, and output references. |
| Candidate groups | EJ records, export requests, export packages, validation results, schema/profile references. |
| Candidate physical object names | Provisional examples: `electronic_journal_records`, `fiscal_export_requests`, `fiscal_export_packages`, `export_validation_results`. |
| Key/reference posture | Export records reference canonical fiscal records and schema/profile version references. |
| Dependencies | Fiscal documents, details, reports, BIR/ARTS mapping posture. |
| Unresolved gates | Final ARTS POSLog profile, schema mapping, output formats, validation package. |
| Authority boundary | ARTS POSLog is an interoperability reference only; BIR outputs remain required. |
| Validation implications | Validate schema/profile version, validation status pending/passed/failed, and BIR/local extension mapping. |
| SQL/object readiness | Blocked. |

## 26. Audit Trail Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent fiscal action audit, privileged operation audit, configuration/status audit, access audit, export validation audit, reset/recovery audit, and unauthorized action audit. |
| Candidate groups | Audit records, status change audit, configuration audit, privileged action audit. |
| Candidate physical object names | Provisional examples: `fiscal_action_audit`, `configuration_audit`, `privileged_action_audit`. |
| Key/reference posture | Audit records reference affected POS-owned records or external refs; actor/service/approval refs remain references. |
| Dependencies | All core object groups and security/privacy references. |
| Unresolved gates | Retention/partitioning, append-only enforcement posture. |
| Authority boundary | Audit is evidence only and does not grant payment finality or ExitAuthorization. |
| Validation implications | Validate actor, action, timestamp, affected record/ref, status/result, reason, approval, and correlation completeness. |
| SQL/object readiness | Blocked. |

## 27. Recovery and Tamper-Evident Continuity Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent continuity snapshots, recovery requests, supervised recovery approvals, hash/anchor references, fiscal lock/block state, and resume/block status. |
| Candidate groups | Fiscal state snapshots, recovery requests, continuity checks, anchor references. |
| Candidate physical object names | Provisional examples: `fiscal_state_snapshots`, `recovery_requests`, `continuity_check_results`, `fiscal_anchor_refs`. |
| Key/reference posture | Recovery records reference latest known fiscal state, counters, GTA, EJ hash, and audit. |
| Dependencies | Numbering/counters, EJ/export, audit. |
| Unresolved gates | Tamper-evident anchoring mechanism and recovery approval workflow. |
| Authority boundary | Recovery cannot rewrite fiscal history or resume from unsafe lower state. |
| Validation implications | Validate no resume from lower counters, lower GTA, earlier SI sequence, broken EJ hash, or earlier last fiscal event timestamp. |
| SQL/object readiness | Blocked. |

## 28. Security/Privacy Reference Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent actor, service identity, approval, role/permission references, evidence references, Digital SI URL access audit references, and privileged export access references. |
| Candidate groups | Actor refs, service identity refs, approval refs, evidence refs, privileged access refs. |
| Candidate physical object names | Provisional examples: `actor_refs`, `approval_refs`, `evidence_refs`, `privileged_access_refs`. |
| Key/reference posture | Use references unless Security/Privacy approves ownership of specific security records. |
| Dependencies | Object scope, Security/Privacy Review. |
| Unresolved gates | Final RBAC matrix, evidence model, privacy retention. |
| Authority boundary | Security references do not create fiscal, payment, or exit authority. |
| Validation implications | Validate data minimization, sensitive evidence separation, actor/approval linkage, and privileged access audit. |
| SQL/object readiness | Blocked. |

## 29. Central PMS and Vendor Integration Reference Objects

| Item | Design posture |
| --- | --- |
| Purpose | Represent reference-only Central PMS and vendor synchronization context needed for fiscal issuance, reconciliation, audit, and troubleshooting. |
| Candidate groups | Central PMS reference records, vendor acknowledgement references, reconciliation references. |
| Candidate physical object names | Provisional examples: `central_pms_refs`, `vendor_ack_refs`, `reconciliation_refs`. |
| Key/reference posture | External references use `_ref` or explicit source prefixes such as `central_pms_*_ref` and `vendor_ack_ref`. |
| Dependencies | Fiscal document core and integration requirements. |
| Unresolved gates | Reference formats, reconciliation rules, vendor payload details. |
| Authority boundary | References only; no Central PMS or vendor authority transfer. |
| Validation implications | Validate `_ref` naming, no payment/exit lifecycle ownership, and correlation integrity. |
| SQL/object readiness | Blocked. |

## 30. Optional Events/Outbox Objects, If Approved

| Item | Design posture |
| --- | --- |
| Purpose | Represent future event publication persistence only if approved by Engineering Pack/event contract work. |
| Candidate groups | Outbox records, publication status records, event correlation references. |
| Candidate physical object names | Provisional examples: `outbox_events`, `event_publication_status_history`. |
| Key/reference posture | Events reference fiscal/audit records; event payload shape remains downstream. |
| Dependencies | Event contract approval and core fiscal/audit object design. |
| Unresolved gates | Whether outbox persistence is required, final event payloads, delivery semantics. |
| Authority boundary | Events are audit/integration/observability only and do not grant payment finality or ExitAuthorization. |
| Validation implications | Validate no event-as-authority naming or lifecycle. |
| SQL/object readiness | Blocked and optional. |

## 31. Cross-Object Dependency Plan

| Dependency | Affected object areas | Design rule |
| --- | --- | --- |
| Schema/naming baseline | All areas | Use `pos`, lowercase `snake_case`, `_id`, `_ref`, and authority-safe naming. |
| Foundation/controlled codes | Most status/type/reason areas | Decide enum vs controlled-code per domain before artifacts. |
| Fiscal identity | Channel registry, fiscal documents, numbering, reports | Define Site POS Server boundary before dependent objects. |
| Channel registry | Fiscal documents, Digital SI URL presentation, audit | Define child channels/terminals before issuance references. |
| Fiscal document core | Lines/totals, numbering, idempotency, Digital SI URL, reprints, adjustments, reports, exports, audit | Canonical fiscal document records are central dependencies. |
| Fiscal details | Reports, EJ/POSLog/JSON/export, audit | Lines and totals support fiscal outputs and reconciliation. |
| Numbering/counters | Reports, recovery, fiscal document issuance | Counter and sequence design must precede production issuance artifacts. |
| Security/privacy | Digital SI URL, evidence refs, privileged access, audit | Sensitive object artifacts require Security/Privacy confirmation. |
| BIR/accreditation | Reports, exports, samples, fiscal identity | Final layouts and profile mapping affect object details. |
| Engineering/CI tooling | Validation, rebuild, drift | Scripts/workflows remain separate future artifacts. |

## 32. Validation and Drift-Check Implications

Future validation needs:

- naming validation;
- authority-boundary validation;
- object existence validation;
- reference validation;
- idempotency validation;
- numbering/counter validation;
- report/export validation;
- audit/recovery validation;
- security/privacy validation;
- drift-check validation.

Future drift checks must compare live/test database state to reviewed repository artifacts only. Drift must be reported and reviewed; it must not be promoted automatically.

## 33. Open Questions With Affected Object Areas

### Engineering / Operations

| Question | Current posture | Affected object area(s) | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| PostgreSQL major version, extensions, hosting, collation/timezone, deployment topology. | PostgreSQL is default engine. | All SQL/object artifacts; extensions; functions; policies. | Blocks SQL/object artifacts. | Confirm before SQL/object artifact creation. |
| Object file layout under `db/state/tables`. | Approved standards allow flat or domain-subfoldered pattern. | All table artifacts. | Blocks table artifact creation. | Decide before first table artifact. |
| Generated vs hand-authored object files. | Boundary must be explicit if generation is used. | All object files; validation/rebuild/drift. | Blocks generated workflow. | Decide before tooling adoption. |

### Physical DB Design

| Question | Current posture | Affected object area(s) | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Final physical object list. | This draft proposes object groups only. | All object areas. | Blocks final artifacts. | Resolve in future object-specific artifact tasks. |
| Final column groups and nullable/required rules. | Logical attributes exist; physical columns pending. | All table-like objects. | Blocks final artifacts. | Resolve per object group. |
| Final constraints/indexes. | Placeholder uniqueness/counter policies exist. | Idempotency, fiscal documents, numbering, references, reports. | Blocks final artifacts. | Resolve in physical artifact design. |
| Enum vs controlled-code per domain. | Strategy exists; storage per domain pending. | Foundation, statuses, reasons, BIR classifications, adjustment types. | Blocks type/reference-data artifacts. | Resolve before type/reference-data artifacts. |

### BIR / Accounting

| Question | Current posture | Affected object area(s) | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Exact SI numbering format. | Display number separate from internal ID; placeholder policy exists. | Fiscal documents, numbering/counters, reports, audit. | Blocks production sequence artifacts. | Confirm with BIR/accounting. |
| Adjustment numbering by family. | Family-specific sequence posture. | Fiscal adjustments, numbering/counters. | Blocks production adjustment sequence artifacts. | Confirm families and format. |
| Sequence gap treatment. | Permanent gap/audit posture unless approved otherwise. | Numbering/counters, audit, recovery. | Blocks final sequence implementation. | Confirm official gap treatment. |
| VAT/tax and Diplomat treatment. | Explicit classifications and evidence references planned. | Fiscal details, reports, exports, evidence refs. | May block tax/report artifacts. | Confirm accounting/compliance rules. |

### Security / Privacy

| Question | Current posture | Affected object area(s) | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Digital SI URL token/auth/expiry model. | Opaque tokenized read-only posture. | Digital SI URL, access audit, security/privacy refs. | Blocks Digital SI URL artifacts. | Complete Security/Privacy Review. |
| Evidence storage vs references. | Evidence references preferred. | Security/privacy refs, audit, VAT privilege evidence, accreditation. | May block evidence artifacts. | Confirm evidence model. |
| Final RBAC matrix. | Actor/approval references planned only. | Security/privacy refs, audit, reprints, adjustments, recovery. | May block privileged-action artifacts. | Complete Security/Privacy/Engineering review. |

### Engineering Pack / CI/CD

| Question | Current posture | Affected object area(s) | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Validation/rebuild/drift tooling. | Plans exist; scripts not created. | Validation, rebuild, drift, PR evidence. | Blocks scripts/workflows. | Confirm tooling. |
| Event/outbox persistence. | Optional and late sequence only. | Optional events/outbox, audit, integration. | Blocks outbox artifacts. | Decide in event contract work. |
| PR evidence format. | Evidence categories planned. | All future artifact PRs. | Blocks CI workflow implementation. | Confirm CI/CD approach. |

### BIR / Accreditation / Vendor

| Question | Current posture | Affected object area(s) | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Final report/export layouts and sample package. | Minimum targets known; exact package pending. | Reports, exports, samples, accreditation. | May block report/export artifacts. | Confirm with accreditation package. |
| Final ARTS POSLog profile/mapping. | ARTS 6.x default reference posture. | Exports, POSLog, JSON validation, local/BIR mappings. | May block export artifacts. | Confirm profile and mapping. |
| Supplier/accreditation metadata. | Flexible fiscal identity model. | Fiscal identity, Site POS Server boundary, accreditation. | May block fiscal identity artifacts. | Confirm supplier/accreditation fields. |

## 34. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Premature SQL/object creation | Keep this draft documentation-only and require separate artifact task. |
| POS-owned payment finality naming | Use `payment_finality_ref` or `central_pms_payment_finality_ref`; validate no POS-owned lifecycle objects. |
| POS-owned PaymentAttempt or PaymentConfirmation lifecycle naming | Use `central_pms_payment_attempt_ref` and `central_pms_payment_confirmation_ref`. |
| POS-owned ExitAuthorization naming | Do not create POS-owned ExitAuthorization objects; use reference-only naming only if approved. |
| Independent terminal fiscal authority naming | Keep channels/terminals as child endpoints under Site POS Server. |
| DTO/table coupling | Use persistence semantics and approved logical design, not API DTO mirroring. |
| Fiscal numbering before BIR confirmation | Keep numbering/counter objects blocked until confirmation or approved placeholder. |
| Digital SI URL security objects before Security/Privacy confirmation | Keep URL artifacts blocked until token/auth/expiry/access model is approved. |
| Report/export objects before accreditation mapping | Keep final layouts/profile mapping open. |
| Recovery/anchoring before Security/Engineering confirmation | Keep anchoring mechanism open. |
| Optional outbox becoming authority | Keep outbox optional and evidence/integration/observability only. |
| Local drift promotion | Require repository artifacts, validation, drift-check, and PR evidence. |

## 35. Non-Decisions

This draft does not decide or create:

- SQL DDL;
- Atlas files;
- migrations;
- physical object files;
- final table definitions;
- final column lists;
- final constraints/indexes;
- final enum/type implementation;
- final seed/reference/sample data;
- validation/rebuild/drift scripts;
- CI workflows;
- source code;
- final BIR/accreditation package;
- offline fiscal issuance approval;
- separate database repository.

## 36. Appendices

### Appendix A: Candidate Name Disclaimer

All candidate physical object names in this document are provisional examples. They do not create final table names, column names, constraints, indexes, SQL files, Atlas files, migration files, or physical database object files.

### Appendix B: Authority Boundary Checklist

- No POS-owned payment finality lifecycle object.
- No POS-owned PaymentAttempt lifecycle object.
- No POS-owned PaymentConfirmation lifecycle object.
- No POS-owned ExitAuthorization issue/approve/mutate/bypass object.
- No POS-owned gate execution authority.
- No independent terminal fiscal issuer object.
- Central PMS and vendor values are references only.
- Audit and events are evidence only.
- ONLINE/OFFLINE is observability only.
- Offline fiscal issuance remains disabled unless explicitly approved.

### Appendix C: Future Artifact Gate Reminder

Before SQL/object artifacts are created, the future task must confirm or explicitly approve placeholder policy for PostgreSQL details, final schema/object names, enum vs controlled-code strategy, idempotency uniqueness, fiscal numbering/counters, retention/partitioning, Digital SI URL security, BIR/accreditation expectations, ARTS POSLog mapping, tamper-evident anchoring, and CI/rebuild/validation/drift tooling.
