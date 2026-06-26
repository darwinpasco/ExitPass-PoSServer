# ExitPass POS Server Controlled-Code Seed Open Questions

## 1. Purpose

This document lists open questions that block actual controlled-code seed/reference data creation.

This is a planning artifact only.

## 2. Governance / Ownership

| Question | Current posture | Blocker status | Target resolution |
| --- | --- | --- | --- |
| Who owns approval for each code family? | Families span Engineering, Operations, BIR/accounting, Security/Privacy, and vendor/accreditation concerns. | Blocks actual values. | Assign owner per family before seed implementation. |
| Should code-set keys use singular family names or plural names? | Planning recommends singular lowercase `snake_case`; not yet approved. | Blocks source schema. | Approve canonical naming convention. |
| What metadata is mandatory for each code and code set? | Minimum metadata is proposed, not final. | Blocks source format. | Approve required fields and optional fields. |
| How are inactive/deprecated codes governed? | Historical readability is required, but lifecycle policy is not final. | Blocks lifecycle values. | Define active/inactive/deprecation policy. |

## 3. BIR / Accounting

| Question | Current posture | Blocker status | Target resolution |
| --- | --- | --- | --- |
| What fiscal document type values are approved? | Document type posture exists; final values need BIR/accounting review. | Blocks fiscal document type seeds. | Approve initial fiscal document type values. |
| What tax type and tax classification values are approved? | VATable, VAT-exempt, zero-rated, non-VAT are candidate categories. | Blocks tax seeds. | Confirm tax classification list and display labels. |
| What discount/privilege values are approved? | Statutory discount, commercial discount, VAT privilege/exemption are planned. | Blocks discount/privilege seeds. | Confirm values, especially Diplomat VAT Privilege / VAT Exemption handling. |
| What total type values are required for reports and fiscal totals? | Gross, discount, tax, VAT privilege, net, and tendered are candidate areas. | Blocks total seeds. | Approve initial fiscal total type list. |
| Which Annex E types are required initially? | Annex E type posture exists. | Blocks Annex E seeds. | Confirm BIR/accreditation reporting scope. |

## 4. Operations

| Question | Current posture | Blocker status | Target resolution |
| --- | --- | --- | --- |
| What channel/terminal types are required for initial deployment? | WebPay, APM, cashier POS, EC device, and operator-assisted posture exist. | Blocks channel type seeds. | Confirm initial deployment endpoint set. |
| What ONLINE/OFFLINE health values are required? | ONLINE/OFFLINE is observability-only. | Blocks health status seeds. | Confirm values and labels without implying offline fiscal issuance approval. |
| What operational statuses are needed for terminals and Site POS Server? | Active/inactive/degraded/continuity posture exists. | Blocks operational status seeds. | Confirm operational status vocabulary. |
| What reprint reasons are required? | Reprint reason posture exists. | Blocks reprint reason seeds. | Confirm operations/BIR acceptable reasons. |
| What recovery and continuity check values are required? | Recovery/check posture exists without automation. | Blocks recovery/check seeds. | Confirm supervised recovery vocabulary. |

## 5. Security / Privacy

| Question | Current posture | Blocker status | Target resolution |
| --- | --- | --- | --- |
| What privacy classification values are approved? | `privacy_classification_code_id` exists as reference posture. | Blocks privacy classification seeds. | Security/Privacy must approve values. |
| What access subject/action/result values are safe? | Access audit is evidence posture only. | Blocks access audit seeds. | Approve non-sensitive access vocabulary. |
| What Digital SI URL status and access values are safe? | Digital SI URL tokens/auth remain Security/Privacy-dependent. | Blocks Digital SI URL seeds. | Approve lifecycle and access evidence values. |
| What privileged action values are required? | Privileged action audit has references only, no IAM/RBAC. | Blocks privileged action seeds. | Approve action/result/reason vocabulary. |

## 6. Engineering / Validation

| Question | Current posture | Blocker status | Target resolution |
| --- | --- | --- | --- |
| Should source be JSON or YAML? | JSON/YAML is recommended over hand-authored SQL. | Blocks seed source files. | Choose source format and schema. |
| Should SQL seed files be generated and committed? | Generated SQL may be useful; policy is not final. | Blocks implementation approach. | Decide source-only vs source plus generated SQL. |
| Where should future seed/reference artifacts live? | `db/reference-data/` is reserved; `db/seeds/` is reserved for environment-neutral seed data. | Blocks file layout. | Approve final folder and naming convention. |
| Should validation scripts compare loaded code values to source? | Existing validation focuses on SQL object inventory. | Blocks seed validation scope. | Define future seed validation script changes. |
| How should UUIDs for codes be assigned? | Controlled-code tables require UUIDs; generation policy is not approved. | Blocks repeatable loading. | Decide deterministic UUIDs vs assigned constants. |

## 7. Vendor / Accreditation

| Question | Current posture | Blocker status | Target resolution |
| --- | --- | --- | --- |
| What export schema/profile type values are approved? | ARTS POSLog and BIR/local JSON profiles are references only. | Blocks profile seeds. | Confirm vendor/accreditation profile vocabulary. |
| What report/export status values are required by accreditation evidence? | Report/export posture exists without final layouts. | Blocks report/export seeds. | Confirm status vocabulary with accreditation needs. |
| Are supplier-specific acknowledgement or profile values needed? | Vendor acknowledgements are references only. | Partially blocks vendor-specific codes. | Confirm whether generic values are enough for initial seed. |

## 8. Authority Boundary Questions

| Question | Current posture | Blocker status | Target resolution |
| --- | --- | --- | --- |
| How will seed review prevent authority-leaking values? | Naming rules prohibit POS-owned payment/exit/gate authority. | Blocks final approval checklist. | Add validation for prohibited code-set/code keys and display names. |
| Are any payment-related display labels needed? | Tender context and payment references are reference-only. | Blocks tender/payment-adjacent labels. | Confirm labels do not imply payment finality ownership. |
| Are any offline values needed? | Offline fiscal issuance remains disabled unless separately approved. | Blocks offline-related values. | Avoid offline issuance approval values; allow observability labels only. |
