# ExitPass POS Server Physical Object Design Object Grouping Plan

## 1. Purpose

This plan proposes future physical object groups. Candidate names are provisional examples only and are not final table names, columns, constraints, indexes, SQL files, or object artifacts.

## 2. Object Grouping Matrix

| Logical area | Purpose | Candidate physical objects, provisional examples | Depends on | Unresolved gates | Authority-boundary considerations | Validation implications | SQL/object creation blocked? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Foundation and controlled codes | Common status/type/reason values. | `*_codes`, `*_code_sets`, status/type families. | Schema/naming standards. | Enum vs controlled-code per domain. | Codes must not imply payment/exit authority. | Validate code inventory and historical readability. | Yes. |
| Fiscal identity / boundary | Site POS Server fiscal authority, taxpayer and registration metadata. | Fiscal identity records, Site POS Server boundary records, effective-dated identity assignments. | Foundation. | MIN/PTU/serial/supplier assignment. | Site POS Server is fiscal authority; channels are children. | Validate effective dating and audit references. | Yes. |
| Channel registry | Channels/terminals, capabilities, health, ONLINE/OFFLINE. | Channel terminal registry, capability records, status history. | Fiscal identity/boundary. | Logical vs physical terminal identity fields. | ONLINE/OFFLINE observability only; no independent terminal fiscal authority. | Validate active/degraded/offline status and capabilities. | Yes. |
| Fiscal documents | Canonical SI and adjustment headers/statuses. | Fiscal document records, status history, original document linkage. | Fiscal identity, channel registry. | Final object names; reference integrity strategy. | Central PMS records are references only; no ExitAuthorization ownership. | Validate no duplicate fiscal documents and external refs. | Yes. |
| Fiscal details | Lines, tender, tax, discount, totals. | Fiscal lines, fiscal totals, tender context, tax/discount details. | Fiscal documents. | VAT/tax treatment and controlled codes. | Money movement finality remains outside POS Server. | Validate line ordering, totals reconciliation, VAT classification. | Yes. |
| Numbering/counters | SI sequence, adjustment sequence, reset/Z counters, GTA, fiscal state. | Sequence state, counter state, fiscal state snapshots, gap audit. | Fiscal documents and identity. | Final BIR numbering/counter confirmation. | Numbering does not grant payment or exit authority. | Validate no reuse, gap audit, monotonic counters. | Yes. |
| Idempotency/retry | Duplicate prevention and completion-unknown handling. | Idempotency records, request identity, replay/conflict state, retry status. | Fiscal operation targets. | Exact uniqueness/index strategy. | Idempotency protects fiscal side effects only. | Validate key/scope uniqueness and no duplicate SI. | Yes. |
| Digital delivery | Digital SI URL lifecycle and access audit. | Digital SI URL references, lifecycle records, access audit. | Fiscal documents and Security/Privacy posture. | Token/auth/expiry/access policy. | URL access cannot mutate fiscal records. | Validate active/expired/revoked/blocked lifecycle. | Yes. |
| Reprints and adjustments | Controlled reprints and fiscal corrections. | Reprint requests/history, adjustment records/status, approval refs. | Fiscal documents, reports, audit refs. | Adjustment families and approvals. | Refund/reversal finality remains outside POS Server. | Validate original linkage and no mutation of original facts. | Yes. |
| Reports | X-read, Z-read, BIR Sales Summary, Annex E. | Report request/status/output metadata, report snapshots. | Documents, details, counters. | X/Z scope and exact layouts. | Reports are fiscal outputs, not payment/exit authority. | Validate SI range, GTA, reset/Z counters, Annex content. | Yes. |
| Exports | EJ, POSLog, JSON, export packages and validation. | EJ records, export requests, package metadata, validation results. | Documents, lines, reports. | ARTS profile/schema mapping. | ARTS is reference only; BIR outputs remain required. | Validate schema/profile version and validation status. | Yes. |
| Audit | Fiscal action and privileged operation evidence. | Audit records, status change audit, config audit. | All core groups. | Retention/partitioning and append-only posture. | Audit is evidence only, not payment finality or ExitAuthorization. | Validate actor/action/status/reason/reference completeness. | Yes. |
| Recovery/continuity | Fiscal state continuity and supervised recovery. | Continuity snapshots, recovery requests, anchors, fiscal lock/block state. | Counters, EJ/export, audit. | Anchoring mechanism. | Recovery cannot rewrite fiscal history. | Validate no resume from lower counters/GTA/SI/EJ hash. | Yes. |
| Security/privacy references | Actor, approval, evidence, privileged access references. | Actor refs, approval refs, evidence refs, access audit refs. | Object scope known. | RBAC/evidence/privacy model. | References only; no final RBAC matrix here. | Validate privileged action references and data minimization. | Yes. |
| Optional events/outbox | Publication support if approved. | Outbox/event publication records. | Event contract approval and core objects. | Final event payloads and persistence decision. | Events do not grant payment finality or ExitAuthorization. | Validate event authority boundary if used. | Yes. |
| Integration references | Central PMS/vendor references. | Central PMS refs, vendor ack refs, reconciliation refs. | Core issuance and integration needs. | Reference formats and reconciliation rules. | References only; no Central PMS ownership. | Validate `_ref` naming and no authority lifecycle. | Yes. |

## 3. Planning Notes

- Candidate names are examples only.
- Physical object definitions remain downstream.
- SQL/object artifact creation remains blocked until physical gates are resolved or explicitly handled by approved placeholder policy.
