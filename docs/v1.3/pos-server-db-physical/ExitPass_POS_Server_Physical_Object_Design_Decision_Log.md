# ExitPass POS Server Physical Object Design Decision Log

## 1. Inherited Approved Decisions

| Decision | Source | Object design impact |
| --- | --- | --- |
| POS Server owns fiscal issuance and fiscal records only. | Approved BRD/System Design/API/DB Design. | Object groups must center on fiscal records, not Central PMS authority. |
| Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. | Approved baselines. | POS Server objects may store references only. |
| Primary schema posture is `pos`. | Approved Schema/Naming Standards. | Future object examples should assume `pos` unless later approved otherwise. |
| Object naming uses lowercase `snake_case`. | Approved Schema/Naming Standards. | Candidate names follow this style. |
| External authority references use `_ref` or explicit source prefix. | Approved Schema/Naming Standards. | Central PMS and vendor references remain reference-only. |
| Offline fiscal issuance is disabled by default. | Approved baselines and gate resolution. | Do not plan offline-issuance enabling objects. |
| ARTS POSLog is an interoperability reference only. | Approved baselines. | Export objects must preserve BIR outputs and terminology. |

## 2. Planning Decisions Resolved Now

| Decision | Resolution | Notes |
| --- | --- | --- |
| Physical object design sequence | Use foundation-first sequencing, then fiscal identity, channel registry, fiscal documents, details, counters, safety, digital delivery, outputs, audit, recovery, security, optional outbox. | Reduces dependency churn and authority risk. |
| Object grouping | Plan by approved logical data areas and domain dependencies. | Candidate object names remain provisional examples only. |
| SQL readiness | SQL/object artifact creation remains not ready. | This package is planning only. |
| Events/outbox | Keep optional and late in the sequence. | Events are evidence/integration/observability only. |
| Central PMS references | Plan as integration/reference group, not owned Central PMS state. | Prevents payment/exit authority leakage. |

## 3. Decisions Deferred

| Deferred decision | Owner / dependency |
| --- | --- |
| Final PostgreSQL version/features/extensions/hosting. | Engineering / Operations. |
| Final physical table and column names. | Future physical object design. |
| Final constraints/indexes/foreign keys. | Future physical object design. |
| Final enum vs controlled-code implementation per domain. | Physical DB design / BIR/accounting / Engineering. |
| Final fiscal numbering/counter implementation. | BIR/accounting / physical DB design. |
| Final Digital SI URL token/auth/expiry/access audit model. | Security/Privacy. |
| Final retention periods and partitioning. | BIR/accounting / Security / Operations. |
| Final ARTS POSLog profile and mapping. | Engineering Pack / BIR accreditation. |
| Final tamper-evident anchoring mechanism. | Security / Engineering / Operations. |

## 4. Non-Decisions

This package does not decide or create SQL, DDL, Atlas files, migrations, object files, seed/reference/sample data, scripts, CI workflows, source code, final object definitions, final RBAC matrix, final accreditation package, offline fiscal issuance approval, or a separate DB repository.
