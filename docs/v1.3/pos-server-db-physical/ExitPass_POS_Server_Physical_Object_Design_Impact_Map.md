# ExitPass POS Server Physical Object Design Impact Map

## 1. Purpose

This impact map connects physical object design planning areas to future workstreams. It does not create SQL, DDL, Atlas files, migrations, object files, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, or diagrams.

## 2. Workstream Impact Matrix

| Workstream | Impact from object design planning | Required next action |
| --- | --- | --- |
| `db/state/schemas` | Uses approved `pos` schema posture unless additional schemas are approved. | Confirm final schema names before schema artifacts. |
| `db/state/tables` | Future table artifacts should follow object grouping sequence and naming standards. | Complete object-specific physical design. |
| `db/state/types` | Enum/type artifacts depend on enum vs controlled-code decisions per domain. | Decide storage strategy before type artifacts. |
| `db/state/sequences` | Fiscal and technical sequences depend on numbering/counter strategy. | Confirm BIR/accounting placeholder or final strategy. |
| `db/state/views` | Report/export read models depend on canonical fiscal objects. | Design after documents, lines, totals, reports. |
| `db/state/functions` | Routines require Engineering approval and clear authority boundaries. | Decide routine policy before function artifacts. |
| `db/state/triggers` | Trigger use must be justified and validated. | Decide trigger policy per object group. |
| `db/state/policies` | Security policies depend on Security/Privacy review. | Confirm access model before policy artifacts. |
| `db/reference-data` | Controlled-code groups depend on foundation and BIR/accounting decisions. | Define controlled-code package after object groups. |
| `db/validation` | Validation categories derive from object groups and authority safeguards. | Create validation plan after object design approval. |
| `db/rebuild` | Rebuild manifest/order follows object dependencies. | Build after object artifacts are approved. |
| `db/drift` | Drift checks need schema/object inventory. | Build after artifact layout and object files exist. |
| Engineering Pack | Needs physical object design for implementation mapping without DTO coupling. | Review object plan before implementation design. |
| Security/Privacy Review | Needs Digital SI URL, evidence, actor/approval, retention, and access groups. | Resolve before sensitive artifacts. |
| BIR/accreditation package | Needs report/export/reprint/counter/sample-support groups. | Confirm package expectations before samples/exports. |

## 3. Authority Boundary Impact

| Area | Risk | Planning mitigation |
| --- | --- | --- |
| Central PMS references | Names or objects could imply POS-owned payment lifecycle. | Use `_ref` and `central_pms_*_ref`; validate no ownership terms. |
| ExitAuthorization | POS Server objects could imply issue/approve/mutate authority. | Do not plan POS-owned ExitAuthorization lifecycle objects. |
| Audit/events | Audit/outbox objects could be mistaken as authority. | Mark evidence/integration/observability only. |
| Channel/terminal | Objects could imply independent terminal fiscal issuer. | Keep channel/terminal as child endpoint under Site POS Server. |
| ONLINE/OFFLINE | Status could imply offline fiscal issuance approval. | Treat as observability only. |

## 4. Future Validation Impact

Future validation should check:

- object inventory follows approved sequence and naming baseline;
- no POS-owned payment finality objects;
- no POS-owned PaymentAttempt or PaymentConfirmation lifecycle objects;
- no POS-owned ExitAuthorization objects;
- no gate execution authority objects;
- Central PMS and vendor values are references;
- Digital SI URL objects do not allow fiscal mutation;
- numbering/counter objects preserve no-reuse and gap-audit posture;
- report/export objects preserve BIR outputs and ARTS as reference only;
- audit/recovery objects preserve evidence and continuity semantics.

## 5. Recommended Next Step

Create an approval-readiness review for the physical object design planning package. After approval, draft the future Physical Object Design v1.0 document without creating SQL/object artifacts.
