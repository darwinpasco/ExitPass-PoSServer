# ExitPass POS Server Database Design Open Questions Resolution Addendum

## 1. Purpose

This addendum records logical database design defaults and classifications for the major open questions carried into the POS Server Database Design v1.0 draft.

The addendum does not modify the approved BRD, approved POS Server System Design, or approved POS Server API Contract. It resolves only what can be safely resolved for logical database design and classifies remaining work for BIR/accounting, Security/Privacy, API Contract detail, Engineering Pack, physical database design, operations, vendor/supplier, and accreditation workstreams.

## 2. Classification Legend

| Classification | Meaning |
| --- | --- |
| Resolved for logical database design | Sufficiently decided for the logical database design baseline. |
| Resolved as default posture | Safe default can be used unless later confirmation changes implementation detail. |
| Partially resolved; final BIR/accounting confirmation required | Logical database design can proceed, but BIR/accounting confirmation remains needed. |
| Partially resolved; final Security/Privacy confirmation required | Logical database design can proceed, but security/privacy decisions remain needed. |
| Downstream API Contract detail | Belongs to endpoint/DTO/status/event contract work. |
| Downstream Engineering Pack detail | Belongs to implementation, validation, packaging, generation, or operational job work. |
| Downstream physical database design detail | Belongs to future object-level database artifact work. |
| Downstream BIR/accreditation package detail | Belongs to examiner package/sample/output confirmation. |
| Remains open external confirmation | Requires external BIR/accounting/security/privacy/vendor confirmation before final implementation. |

## 3. Resolution Matrix

### DB-OQ-001 MIN/PTU/Serial/Software/Supplier Assignment

| Field | Value |
| --- | --- |
| Question/topic | MIN/PTU/serial/software/supplier assignment. |
| Prior open status | Open for BIR/accounting/accreditation confirmation. |
| Recommended answer / resolution | Keep configurable at Site POS Server plus channel/terminal level. Site POS Server remains fiscal authority, while terminals/channels may carry registered serial, MIN, PTU, terminal number, software version, or supplier/accreditation metadata where required. |
| Final classification | Partially resolved; final BIR/accounting confirmation required. |
| Database design impact | Support flexible, effective-dated fiscal identity assignments at Site POS Server and channel/terminal levels. Do not hard-code one assignment rule. |
| Downstream owner / workstream | BIR/accounting, BIR/accreditation package, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | May block final uniqueness/effective-dating rules. |
| Blocks implementation | May block accreditation-ready implementation. |
| Notes | Logical model must support both levels even if final accreditation assigns most values to one level. |

### DB-OQ-002 WebPay Fiscal Terminal Identity

| Field | Value |
| --- | --- |
| Question/topic | WebPay fiscal terminal identity. |
| Prior open status | Open. |
| Recommended answer / resolution | Treat WebPay as a logical channel/terminal under the Site POS Server. Use a logical identity such as `WEBPAY-{SITE}` or another BIR-approved equivalent. WebPay does not require physical printer serial by default unless BIR requires one. |
| Final classification | Resolved as default posture; exact label may still require BIR/accreditation confirmation. |
| Database design impact | Channel/terminal registry must support logical/non-physical channel identities and no physical printer/hardware serial where allowed. |
| Downstream owner / workstream | BIR/accreditation package, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | No, if model allows logical identities. |
| Blocks implementation | May block final accreditation wording only. |
| Notes | WebPay must not become a separate fiscal authority. |

### DB-OQ-003 Sales Invoice Numbering Pattern

| Field | Value |
| --- | --- |
| Question/topic | Exact Sales Invoice numbering pattern. |
| Prior open status | Open. |
| Recommended answer / resolution | POS Server owns Sales Invoice sequence per Site POS Server. Use configurable sequence policy. Keep display format separate from internal fiscal document ID. Do not append reset counter unless BIR confirms. |
| Final classification | Resolved for architecture/logical design; partially resolved pending BIR/accounting format confirmation. |
| Database design impact | Support sequence policy abstraction, Site POS Server-scoped SI sequence, display number, internal fiscal document ID, and sequence allocation audit. |
| Downstream owner / workstream | BIR/accounting, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | Yes, for final numbering implementation unless placeholder policy is approved. |
| Blocks implementation | Yes, for production fiscal issuance. |
| Notes | Logical design can proceed with configurable policy. |

### DB-OQ-004 Adjustment Document Numbering Pattern

| Field | Value |
| --- | --- |
| Question/topic | Exact adjustment document numbering pattern. |
| Prior open status | Open. |
| Recommended answer / resolution | Use separate configurable sequences per adjustment document family where required, such as void, refund, cancel, return, credit/debit memo equivalent, or other BIR-confirmed fiscal adjustment document type. Every adjustment document links to original Sales Invoice or fiscal document. |
| Final classification | Resolved for architecture/logical design; partially resolved pending BIR/accounting confirmation. |
| Database design impact | Support adjustment sequence policy by document family, original fiscal document linkage, adjustment status, and audit history. |
| Downstream owner / workstream | BIR/accounting, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | Yes, for final numbering implementation unless placeholder policy is approved. |
| Blocks implementation | Yes, for production adjustment document issuance. |
| Notes | Do not invent final document family names beyond policy placeholders. |

### DB-OQ-005 Sequence Gaps, Reserved Numbers, Failed Issuance, Abandoned Issuance

| Field | Value |
| --- | --- |
| Question/topic | Sequence gaps, reserved numbers, failed issuance, abandoned issuance. |
| Prior open status | Open. |
| Recommended answer / resolution | Do not consume a Sales Invoice number until fiscal issuance commit. If a number is reserved and issuance fails after reservation, retain a permanent gap/audit record. Never reuse consumed numbers. Never reuse reserved numbers unless BIR/accounting explicitly approves a compliant reuse rule. Timeout/completion-unknown cases must use status lookup and idempotency before retry. |
| Final classification | Resolved as default posture; final BIR/accounting confirmation still required for official gap treatment. |
| Database design impact | Support sequence allocation state, reserved/issued/failed/abandoned status, gap/audit records, idempotency, and completion-unknown handling. |
| Downstream owner / workstream | BIR/accounting, physical database design, Engineering Pack. |
| Blocks logical database design | No. |
| Blocks physical database design | Yes, for final sequence implementation unless placeholder policy is approved. |
| Blocks implementation | Yes, for production fiscal numbering. |
| Notes | Safe default is conservative and audit-first. |

### DB-OQ-006 X-read and Z-read Aggregation Scope

| Field | Value |
| --- | --- |
| Question/topic | X-read and Z-read aggregation scope. |
| Prior open status | Open. |
| Recommended answer / resolution | Support Site POS Server scope as primary fiscal scope. Also support optional terminal/channel and cashier/session dimensions where required. Z-read closes approved fiscal scope. X-read may be generated for approved operational scopes. |
| Final classification | Resolved as flexible logical design; final BIR/accounting confirmation required for required scope. |
| Database design impact | Report records must support scope type and scope reference. Counter boundaries remain configurable until confirmed. |
| Downstream owner / workstream | BIR/accounting, physical database design, Engineering Pack. |
| Blocks logical database design | No. |
| Blocks physical database design | May block final report snapshot keys and counter boundary implementation. |
| Blocks implementation | May block fiscal day-close implementation. |
| Notes | Model must not assume only one fixed aggregation level. |

### DB-OQ-007 Exact VAT/Tax Treatment

| Field | Value |
| --- | --- |
| Question/topic | Exact VAT/tax treatment. |
| Prior open status | Open external confirmation. |
| Recommended answer / resolution | Do not decide exact accounting treatment in database design. Support required fiscal classifications: VATable, VAT-exempt, zero-rated, non-VAT, statutory discounts, VAT privileges/exemptions, coupons, parking fees, lost ticket fees, penalties, overstay charges, service charges, voids, returns, and adjustments. |
| Final classification | Remains open external confirmation. |
| Database design impact | Preserve explicit fiscal line classifications, tax summaries, and totals. Do not bury tax treatment only in tariff snapshots. |
| Downstream owner / workstream | Finance/accounting, BIR/accounting, Engineering Pack tests. |
| Blocks logical database design | No. |
| Blocks physical database design | No, if classification model stays flexible. |
| Blocks implementation | Yes, for final tax rules, report formulas, and expected values. |
| Notes | Logical model supports classifications without choosing accounting outcomes. |

### DB-OQ-008 Diplomat VAT Treatment, Evidence, Wording, Reporting, Retention

| Field | Value |
| --- | --- |
| Question/topic | Diplomat VAT treatment, evidence, wording, reporting, retention. |
| Prior open status | Open external confirmation. |
| Recommended answer / resolution | Model Diplomat VAT Privilege / VAT Exemption as active VAT privilege/exemption, not ordinary discount. Store evidence references by default, not raw evidence unless approved. Exact invoice wording, report treatment, evidence requirements, validation workflow, and retention remain compliance/accounting/security/privacy decisions. |
| Final classification | Partially resolved; final BIR/accounting/security/privacy confirmation required. |
| Database design impact | Support VAT privilege/exemption fiscal line treatment, evidence references, restricted access, retention controls, and reporting extension without treating it as ordinary commercial discount. |
| Downstream owner / workstream | BIR/accounting, Security/Privacy, physical database design, Engineering Pack. |
| Blocks logical database design | No. |
| Blocks physical database design | May block evidence storage and retention details. |
| Blocks implementation | Yes, for final validation/reporting behavior. |
| Notes | Active entitlement/treatment category remains in model. |

### DB-OQ-009 Digital SI URL Token/Access/Expiry/Authentication Model

| Field | Value |
| --- | --- |
| Question/topic | Digital SI URL token/access/expiry/authentication model. |
| Prior open status | Open for Security/Privacy Review. |
| Recommended answer / resolution | Use opaque, non-guessable tokenized URL as default posture. Customer-facing view is read-only and does not allow fiscal mutation. Public URL exposes minimum required data. URL lifecycle supports active, expired, revoked, and blocked. Expiry is policy-configurable. Canonical fiscal record remains retained even if public URL access expires. |
| Final classification | Partially resolved; final Security/Privacy confirmation required. |
| Database design impact | Support token/access reference, URL lifecycle status, issue/expiry timestamps, access audit where required, and data minimization. |
| Downstream owner / workstream | Security/Privacy, API Contract detail, physical database design, Engineering Pack. |
| Blocks logical database design | No. |
| Blocks physical database design | May block token/auth/expiry implementation. |
| Blocks implementation | Yes, for customer-facing access. |
| Notes | Database stores references/lifecycle, not fiscal mutation authority. |

### DB-OQ-010 Exact Report/Export Formats and Layouts

| Field | Value |
| --- | --- |
| Question/topic | Exact report/export formats and layouts. |
| Prior open status | Open. |
| Recommended answer / resolution | Support Print, PDF, JSON, EJ, POSLog, BIR Sales Summary, Annex E, audit trail report, and structured export packages. Exact layout, packaging, file naming, and mandatory submission set belong to BIR/accreditation package and Engineering Pack. |
| Final classification | Partially resolved; final BIR/accreditation and Engineering Pack confirmation required. |
| Database design impact | Support output references, output mode, export package metadata, validation status/errors, and no frozen layout in database design. |
| Downstream owner / workstream | BIR/accreditation package, Engineering Pack, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | No, if output metadata remains flexible. |
| Blocks implementation | May block final export generation and validation jobs. |
| Notes | Logical design supports output families and validation lifecycle. |

### DB-OQ-011 ARTS POSLog Profile and Schema Mapping

| Field | Value |
| --- | --- |
| Question/topic | Exact ARTS POSLog profile and schema mapping. |
| Prior open status | Open. |
| Recommended answer / resolution | Use ARTS POSLog 6.x as default structured export reference where practical and accepted. Preserve BIR terminology and outputs. Use local/BIR extensions or mappings for SI Number, MIN/PTU, serial, ticket/plate, Site/branch/business unit, channel/terminal/workstation, Business Day Date, reset counter, Z-counter, GTA, Digital SI URL, parking timestamps, and audit references. |
| Final classification | Resolved as default posture; final BIR/accreditation and Engineering Pack confirmation required for exact profile/mapping. |
| Database design impact | Support schema/profile version reference, local/BIR extension mapping reference, validation status/errors, and clear statement that ARTS does not replace BIR fiscal outputs. |
| Downstream owner / workstream | BIR/accreditation package, Engineering Pack, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | No, if profile/version fields remain flexible. |
| Blocks implementation | May block final export schema validation and accreditation samples. |
| Notes | Default posture is sufficient for logical design. |

### DB-OQ-012 JSON Schema Versioning Strategy

| Field | Value |
| --- | --- |
| Question/topic | Exact JSON schema versioning strategy. |
| Prior open status | Open. |
| Recommended answer / resolution | Use explicit schema/profile version fields on export packages, JSON/POSLog exports, and validation records. Keep old schema versions readable and reconstructible. Do not overwrite historical export meaning after schema changes. |
| Final classification | Resolved as default posture; final Engineering Pack/database artifact details remain downstream. |
| Database design impact | Support schema version, validation profile/version, export package metadata, and backwards-readable historical exports. |
| Downstream owner / workstream | Engineering Pack, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | No. |
| Blocks implementation | May block export implementation details. |
| Notes | This can be implemented later without changing logical posture. |

### DB-OQ-013 Final Accreditation Sample Package

| Field | Value |
| --- | --- |
| Question/topic | Final accreditation sample package. |
| Prior open status | Open. |
| Recommended answer / resolution | Minimum target package includes sample Sales Invoice, X-read, Z-read, EJ, POSLog, BIR Sales Summary / Annex E-1, Annex E-2 to E-5 where applicable, audit trail report, JSON export, PDF/print outputs, reprint outputs with `REPRINT` and `DATE / TIME REPRINTED`, export validation evidence, supplier/taxpayer/fiscal identity evidence, and recovery/counter continuity evidence where required. |
| Final classification | Resolved as minimum target; final examiner/accreditation package confirmation required. |
| Database design impact | Support deterministic sample data, generated output references, validation/audit evidence, and fiscal identity metadata. |
| Downstream owner / workstream | BIR/accreditation package, Engineering Pack. |
| Blocks logical database design | No. |
| Blocks physical database design | No. |
| Blocks implementation | May block package completion. |
| Notes | Minimum target should be refined during accreditation package preparation. |

### DB-OQ-014 Tamper-Evident Anchoring Mechanism

| Field | Value |
| --- | --- |
| Question/topic | Tamper-evident anchoring mechanism. |
| Prior open status | Open. |
| Recommended answer / resolution | Default posture: append-only fiscal audit chain; hash chaining per Site POS Server or fiscal stream where practical; externally anchored checkpoints where practical; latest EJ hash; last fiscal event timestamp; counters; GTA; recovery snapshots; supervised recovery when continuity cannot be proven. |
| Final classification | Resolved as default posture; final Engineering Pack / Security / physical DB design confirmation required. |
| Database design impact | Support fiscal state snapshot, latest/previous fiscal state, logical hash/reference fields, external anchor reference if used, recovery block/resume status, and audit. |
| Downstream owner / workstream | Security/Privacy, Engineering Pack, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | May block final recovery/tamper-evidence implementation. |
| Blocks implementation | Yes, for production recovery continuity. |
| Notes | Logical design should stay mechanism-flexible. |

### DB-OQ-015 Endpoint Names and DTOs

| Field | Value |
| --- | --- |
| Question/topic | Final endpoint names and DTOs. |
| Prior open status | Open downstream API detail. |
| Recommended answer / resolution | Keep API route families provisional but accepted as v1.0 baseline. DTOs must follow API semantic contracts and should not mirror database tables directly. |
| Final classification | Resolved as API/database design principle; final API detail remains downstream. |
| Database design impact | Define persistence semantics, not DTO shape. Central PMS reference strategy follows approved API Contract. |
| Downstream owner / workstream | API Contract detail, Engineering Pack. |
| Blocks logical database design | No. |
| Blocks physical database design | No. |
| Blocks implementation | May block API implementation details. |
| Notes | Database design and DTOs must remain decoupled. |

### DB-OQ-016 Final Event Payloads

| Field | Value |
| --- | --- |
| Question/topic | Final event payloads. |
| Prior open status | Open downstream event detail. |
| Recommended answer / resolution | Use outbox-style POS/fiscal events for audit, integration, and observability only. Events must not grant payment finality or ExitAuthorization. Final payload schemas belong to Engineering Pack / event contract work. |
| Final classification | Resolved as event authority principle; final event payloads remain downstream. |
| Database design impact | Support audit/event publication references where required. Do not model events as fiscal/payment/exit authority. |
| Downstream owner / workstream | Engineering Pack, event contract work, physical database design if outbox is approved. |
| Blocks logical database design | No. |
| Blocks physical database design | No, pending event/outbox physical design. |
| Blocks implementation | May block event publication implementation. |
| Notes | Event records are evidence and integration signals only. |

### DB-OQ-017 Final RBAC Matrix

| Field | Value |
| --- | --- |
| Question/topic | Final RBAC matrix. |
| Prior open status | Open Security/Privacy / implementation detail. |
| Recommended answer / resolution | Use minimum role baseline: Cashier, Supervisor, Fiscal Administrator, Compliance Auditor, Recovery/DR Approver, System Administrator, Service Identity, Channel/Terminal Identity. Exact permission matrix remains Security/Privacy and implementation design. |
| Final classification | Resolved as role baseline; final Security/Privacy confirmation required. |
| Database design impact | Support actor/service identity references, approval references, and privileged action audit. Do not finalize permission table structure here. |
| Downstream owner / workstream | Security/Privacy, Engineering Pack, physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | No, if role/approval references remain flexible. |
| Blocks implementation | May block privileged-action implementation. |
| Notes | Role baseline supports logical audit design. |

### DB-OQ-018 Physical Table/Column/Index/Constraint Design

| Field | Value |
| --- | --- |
| Question/topic | Final physical table/column/index/constraint design. |
| Prior open status | Open physical design detail. |
| Recommended answer / resolution | Do not decide in logical database design. This belongs to future physical database design / state-based object artifact task. |
| Final classification | Intentionally downstream physical database design. |
| Database design impact | Keep candidate logical records and attributes only. Keep physical design gate checklist. Avoid final physical names. |
| Downstream owner / workstream | Physical database design. |
| Blocks logical database design | No. |
| Blocks physical database design | Yes; this is the physical design task. |
| Blocks implementation | Yes, for database implementation. |
| Notes | Current draft should remain logical. |

### DB-OQ-019 Offline Fiscal Issuance Approval

| Field | Value |
| --- | --- |
| Question/topic | Offline fiscal issuance approval, if any. |
| Prior open status | Open only if business seeks BIR/accounting approval. |
| Recommended answer / resolution | Default is not allowed. ONLINE/OFFLINE is observability only. Offline fiscal issuance requires explicit BIR/accounting-approved sequence, counter, evidence, reconciliation, and recovery model before database or implementation support is activated. |
| Final classification | Resolved as disabled by default; remains open only if later approval is sought. |
| Database design impact | Do not model offline issuance as enabled. Support ONLINE/OFFLINE status as observability and continuity/recovery records without implying offline fiscal issuance approval. |
| Downstream owner / workstream | BIR/accounting, Operations, Engineering Pack if later approved. |
| Blocks logical database design | No. |
| Blocks physical database design | No. |
| Blocks implementation | No for default disabled posture; yes if offline issuance is later requested. |
| Notes | This is no longer a blocker for logical design. |

## 4. Summary for Database Design Draft

The logical database design can proceed with the defaults above. Remaining external confirmations should be treated as downstream blocking details for physical database design, implementation, accreditation package preparation, or security/privacy review, not as blockers to the logical database design baseline.