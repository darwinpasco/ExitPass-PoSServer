# ExitPass POS Server Physical DB Gate Open Questions

## Engineering / Operations

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| PostgreSQL version, extensions, hosting/runtime, collation/timezone, deployment topology | PostgreSQL is default engine. | Engineering / Operations | Blocks SQL/object artifacts and implementation. | Confirm engine profile before SQL files. |
| Backup/restore/failover assumptions for database layout | Logical recovery model approved. | Operations | May block recovery/anchoring artifacts. | Define operational topology before recovery objects/scripts. |
| Rebuild script tool and execution pattern | Workflow resolved; scripts deferred. | Engineering | Blocks script implementation. | Select scripting approach in Engineering Pack task. |
| Drift-check tooling, including optional Atlas | Drift policy resolved; tool deferred. | Engineering / CI/CD | Blocks drift automation. | Evaluate manual vs Atlas/state comparison. |

## Physical DB Design

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Final schema/domain names | Candidate domains resolved. | Physical DB Design | Blocks SQL/object artifacts. | Approve physical schema names. |
| Final object naming standards | Naming posture resolved. | Physical DB Design | Blocks SQL/object artifacts. | Approve concrete naming standards. |
| Enum vs controlled-code choice per domain | Strategy resolved. | Physical DB Design | Blocks type/reference objects. | Decide per lifecycle/classification family. |
| Final unique constraints/indexes for idempotency | Placeholder strategy resolved. | Physical DB Design / API | Blocks SQL/object artifacts. | Define uniqueness and indexes. |
| Retention/partitioning keys | Categories identified. | Physical DB Design / Operations | Blocks high-volume objects. | Confirm retention periods and partitioning. |

## BIR / Accounting

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Exact SI numbering format | Placeholder policy resolved. | BIR/accounting | Blocks production fiscal issuance. | Confirm BIR-approved format. |
| Adjustment numbering families and format | Placeholder policy resolved. | BIR/accounting | Blocks production adjustment issuance. | Confirm BIR-approved families and formats. |
| Sequence gap treatment | Conservative audit-first placeholder. | BIR/accounting | Blocks production numbering policy. | Confirm official gap treatment. |
| X/Z scope and fiscal close scope | Flexible model resolved. | BIR/accounting | May block report/counter objects. | Confirm required aggregation and close scope. |
| VAT/tax and Diplomat VAT treatment | Logical support only. | BIR/accounting | Blocks production calculations/reporting. | Confirm fiscal treatment and formulas. |

## Security / Privacy

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Digital SI URL token/auth/expiry/access audit | Security posture resolved. | Security / Privacy | Blocks URL physical artifacts. | Complete Security/Privacy Review. |
| Final RBAC matrix and privileged action permissions | Role baseline exists. | Security / Privacy | Blocks permission artifacts and implementation. | Approve permission matrix. |
| Evidence separation and sensitive data handling | Evidence references preferred. | Security / Privacy | May block evidence objects. | Confirm evidence storage/access policy. |
| Tamper-evident anchoring mechanism | Security/recovery posture resolved. | Security / Engineering | Blocks anchoring objects. | Approve hash/anchor mechanism. |

## Engineering Pack

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Validation script implementation language and conventions | Validation categories resolved. | Engineering Pack | Blocks scripts. | Define script stack. |
| Generated vs hand-authored artifact process | Hand-authored/reviewed default. | Engineering Pack | Blocks generator use. | Approve generator if needed. |
| Event/outbox physical artifact need | Optional if approved later. | Engineering Pack / Event contract | May block outbox artifacts. | Decide during event contract work. |

## BIR / Accreditation

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Final examiner-required output package | Minimum target resolved. | BIR/accreditation | Blocks final package. | Confirm sample package with examiner requirements. |
| ARTS POSLog profile/schema mapping | Default mapping posture resolved. | BIR/accreditation / Engineering | Blocks export validation artifacts. | Confirm accepted profile and extensions. |
| Supplier/accreditation metadata | Flexible identity model. | BIR/accreditation / Vendor | Blocks identity artifacts. | Confirm required fields and evidence. |

## CI/CD

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| CI runner and PostgreSQL service image/version | Open. | CI/CD | Blocks CI workflow. | Choose runner and DB image. |
| PR evidence storage/publishing | Checklist resolved. | CI/CD | Blocks automation. | Define artifact publishing conventions. |
| Drift failure rules | Drift policy resolved. | CI/CD | Blocks CI gate. | Define pass/fail thresholds. |

## Vendor / Supplier

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Vendor-specific export/audit fields beyond BIR/ARTS | Context only unless required. | Vendor / Supplier | May block export extensions. | Confirm vendor requirements. |
| Supplier accreditation metadata | Flexible model. | Vendor / Supplier | Blocks accreditation evidence if required. | Confirm supplier/applicant responsibility and metadata. |