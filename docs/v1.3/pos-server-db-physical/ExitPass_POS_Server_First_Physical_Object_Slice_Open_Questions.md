# ExitPass POS Server First Physical Object Slice Open Questions

## 1. Purpose

This document carries forward open questions that affect the first future physical object slice only. Questions outside the first slice are intentionally excluded unless they affect foundation, fiscal identity, channel/terminal registry, or Central PMS reference naming posture.

## 2. Physical DB Design

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Which first-slice code families should become physical objects versus deferred reference data? | Foundation / configuration / controlled-code posture | Code families are planned; enum vs controlled-code remains per-domain decision. | Blocks controlled-code artifacts. | Decide per code family before first artifact task. |
| Should first-slice tables use flat `db/state/tables/` placement or domain subfolders? | All first-slice object files | Approved standards allow either flat or domain-subfoldered pattern. | Blocks table artifact file layout. | Choose layout before creating table object files. |
| Are Central PMS references embedded in first-slice boundary/registry records, represented in shared reference records, or both? | Central PMS reference naming posture; fiscal identity; channel registry | Reference-only naming is approved; storage pattern is not final. | Blocks reference object artifacts. | Decide during first-slice physical object design. |
| What are the minimum physical columns for fiscal identity and channel/terminal registry objects? | Fiscal identity / Site POS Server boundary; channel/terminal registry | Logical attributes exist; final columns are not decided. | Blocks object artifacts. | Define candidate fields in future first-slice design, then create artifacts only if approved. |

## 3. BIR / Accounting

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Which MIN/PTU/serial/software/supplier attributes belong at Site POS Server level, channel/terminal level, or both? | Fiscal identity / Site POS Server boundary; channel/terminal registry | Flexible model supports both levels. | Blocks final identity uniqueness and effective-dating rules. | Confirm with BIR/accounting/accreditation before production artifacts. |
| Which BIR/report classification code families are needed in the first slice? | Foundation / configuration / controlled-code posture | BIR/report code families are candidates only. | May block controlled-code artifacts. | Confirm which values are needed before reference/control-code artifacts. |

## 4. Security / Privacy

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Should identity configuration audit references store only references or any local metadata? | Fiscal identity / Site POS Server boundary | Audit references are planned; audit physical objects are out of scope. | May block audit-reference fields. | Confirm with Security/Privacy and audit planning. |
| Are any taxpayer/business identity attributes sensitive enough to require separation or masking in first-slice objects? | Fiscal identity / Site POS Server boundary | Privacy posture requires data minimization. | May block identity field artifacts. | Confirm during Security/Privacy review. |

## 5. Engineering / Operations

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| What PostgreSQL version, extensions, hosting, collation/timezone, and deployment assumptions apply? | All first-slice artifacts | PostgreSQL is default engine; details pending. | Blocks SQL/object artifacts. | Confirm before any SQL/object artifact creation. |
| What channel/terminal health statuses and capability values are operationally required for initial deployment? | Channel/terminal registry; controlled codes | ONLINE/OFFLINE is observability only; capability families are candidates. | Blocks final status/capability artifacts. | Confirm with Engineering/Operations. |
| How should logical WebPay channel identity be represented in physical objects? | Channel/terminal registry | WebPay is logical channel/terminal under Site POS Server. | May block WebPay identity fields. | Confirm label/identifier policy before artifacts. |

## 6. Vendor / Supplier

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Which supplier/accreditation metadata fields are required for first-slice identity objects? | Fiscal identity / Site POS Server boundary | Supplier/accreditation metadata is supported as references/metadata. | May block fiscal identity artifacts. | Confirm with supplier/accreditation package. |
| Which vendor acknowledgement references are needed at first slice versus later fiscal document work? | Central PMS reference naming posture | `vendor_ack_ref` and source-specific references are approved naming patterns. | May block vendor reference object planning. | Confirm vendor synchronization needs. |

## 7. Accreditation

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Does the accreditation package require terminal-level fiscal identity evidence before fiscal document objects exist? | Fiscal identity / Site POS Server boundary; channel/terminal registry | Identity model supports Site POS Server and channel/terminal references. | May block identity artifact finalization. | Confirm with accreditation package workstream. |
| Are any accreditation sample/support structures needed in first slice? | Foundation; fiscal identity; channel/terminal registry | Sample/accreditation data files are out of scope. | Does not block first-slice planning; blocks sample artifacts. | Keep sample/accreditation artifacts separate. |

## 8. Questions Not Reopened

This package does not reopen approved decisions that Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, site resolution, and platform authority. It also does not reopen the default posture that WebPay is a logical channel/terminal under Site POS Server, ONLINE/OFFLINE is observability only, and offline fiscal issuance is disabled unless explicitly approved.
