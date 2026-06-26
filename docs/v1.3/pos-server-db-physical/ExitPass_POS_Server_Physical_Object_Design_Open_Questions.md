# ExitPass POS Server Physical Object Design Open Questions

## 1. Engineering / Operations

| Question | Current posture | Blocks object design? | Blocks SQL/object artifacts? | Target resolution |
| --- | --- | --- | --- | --- |
| PostgreSQL major version, extensions, hosting, collation/timezone, deployment topology. | PostgreSQL is default engine. | Partially | Yes | Confirm before SQL/object artifact creation. |
| Object file layout under `db/state/tables`. | Approved standards allow flat or domain-subfoldered pattern. | Partially | Yes | Decide before first table artifact. |
| Generated vs hand-authored object files. | Boundary must be explicit if generation is used. | No | Yes | Decide before tooling adoption. |

## 2. Physical DB Design

| Question | Current posture | Blocks object design? | Blocks SQL/object artifacts? | Target resolution |
| --- | --- | --- | --- | --- |
| Final physical object list. | This package plans groupings only. | Yes for final design | Yes | Resolve in future object design document. |
| Final column groups and nullable/required rules. | Logical attributes exist; physical columns pending. | Yes | Yes | Resolve per object group. |
| Final constraints/indexes. | Placeholder uniqueness/counter policies exist. | Yes | Yes | Resolve in physical design and validation plan. |
| Enum vs controlled-code per domain. | Strategy exists; storage per domain pending. | Partially | Yes | Resolve before type/reference-data artifacts. |

## 3. BIR / Accounting

| Question | Current posture | Blocks object design? | Blocks SQL/object artifacts? | Target resolution |
| --- | --- | --- | --- | --- |
| Exact SI numbering format. | Display number separate from internal ID; placeholder policy exists. | No for planning | Yes for production sequence artifacts | Confirm with BIR/accounting. |
| Adjustment numbering by family. | Family-specific sequences are expected. | No for planning | Yes | Confirm families and format. |
| Sequence gap treatment. | Permanent gap/audit posture unless approved otherwise. | No for planning | Yes | Confirm official gap treatment. |
| VAT/tax and Diplomat treatment. | Explicit classifications and evidence references planned. | No for planning | May block tax/report artifacts | Confirm accounting and compliance rules. |

## 4. Security / Privacy

| Question | Current posture | Blocks object design? | Blocks SQL/object artifacts? | Target resolution |
| --- | --- | --- | --- | --- |
| Digital SI URL token/auth/expiry model. | Opaque tokenized read-only posture. | No for grouping | Yes for URL objects | Complete Security/Privacy Review. |
| Evidence storage vs references. | Evidence references preferred. | No for grouping | May block evidence objects | Confirm evidence model. |
| Final RBAC matrix. | Actor/approval references planned only. | No for grouping | May block security artifacts | Complete Security/Privacy/Engineering review. |

## 5. Engineering Pack / CI/CD

| Question | Current posture | Blocks object design? | Blocks SQL/object artifacts? | Target resolution |
| --- | --- | --- | --- | --- |
| Validation/rebuild/drift tooling. | Plans exist; scripts not created. | No | Yes for script artifacts | Confirm tooling. |
| Event/outbox persistence. | Optional and late sequence only. | No for core objects | Yes for outbox artifacts | Decide in event contract work. |
| PR evidence format. | Evidence categories planned. | No | Yes for workflow implementation | Confirm CI/CD approach. |

## 6. BIR / Accreditation / Vendor

| Question | Current posture | Blocks object design? | Blocks SQL/object artifacts? | Target resolution |
| --- | --- | --- | --- | --- |
| Final report/export layouts and sample package. | Minimum targets known; exact package pending. | No for grouping | May block report/export artifacts | Confirm with accreditation package. |
| Final ARTS POSLog profile/mapping. | ARTS 6.x default reference posture. | No for grouping | May block export artifacts | Confirm profile and mapping. |
| Supplier/accreditation metadata. | Flexible fiscal identity model. | No for grouping | May block fiscal identity artifacts | Confirm supplier/accreditation fields. |
