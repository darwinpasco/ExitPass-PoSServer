# ExitPass POS Server Physical DB Gate Implementation Impact Map

## 1. Purpose

This impact map connects the gate-resolution decisions to future workstreams. It is planning only and does not create SQL, Atlas files, migrations, physical object files, the `db/` folder, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, or diagrams.

## 2. Workstream Impact Matrix

| Workstream | Gate decisions that affect it | Impact | Next action |
| --- | --- | --- | --- |
| `db/` folder creation | Folder layout resolved; state-based workflow resolved; seed/reference separation resolved. | Folder skeleton can be created in a later bootstrap task if limited to approved layout and README. | Create separate bootstrap task; no SQL/object files. |
| SQL/object artifact creation | PostgreSQL default, schema candidates, naming posture, enum/control-code strategy, idempotency, numbering, retention, Digital SI URL, ARTS mapping, anchoring. | Not ready for full object files until pending confirmations are resolved. | Complete physical object design task after confirmations/placeholders. |
| Physical schema design | Candidate domains and naming posture. | Schema names remain provisional. | Approve final physical schema names and ownership boundaries. |
| Seed/reference data | Seed/reference separation and controlled-code strategy. | Reference data strategy can be designed; actual files deferred. | Define controlled-code ownership and load rules. |
| Validation scripts | Validation plan resolved. | Script categories are clear; actual scripts deferred. | Choose script technology and create scripts later. |
| Rebuild scripts | Rebuild workflow resolved. | Workflow is clear; actual scripts deferred. | Define command style and manifest handling. |
| Drift scripts | Drift policy resolved; no local drift promotion confirmed. | Tooling is open; optional Atlas remains possible. | Select drift-check tooling. |
| CI workflow | PR evidence checklist resolved. | CI workflow not ready until runner/tooling selected. | Define CI runner, DB image, and evidence publishing. |
| Engineering Pack | Workflow, validation, rebuild, drift, generated vs hand-authored boundaries. | Engineering Pack must carry implementation details and command conventions. | Add DB artifact workflow to Engineering Pack later. |
| Security/Privacy Review | Digital SI URL posture, RBAC, evidence references, tamper anchoring. | Security decisions block URL/access, permission, evidence, and anchoring artifacts. | Complete Security/Privacy Review. |
| BIR/accreditation package | Numbering, gaps, reports, ARTS mapping, sample package, supplier metadata. | Accreditation evidence remains pending. | Confirm examiner package and required output formats. |
| Sample data / test fixtures | Sample/accreditation separation and minimum target package. | Actual sample data deferred. | Define synthetic/accreditation data policy. |

## 3. Gate Decisions by Future Artifact Type

| Future artifact type | Required resolved gates | Still pending |
| --- | --- | --- |
| `db/README.md` | Folder layout, state workflow, authority boundary, no local drift rule. | None for README-only bootstrap. |
| `db/state/schemas/` | Schema/domain decomposition, naming standards, PostgreSQL profile. | Final schema names. |
| `db/state/tables/` | Naming, enum/control-code, idempotency, numbering, retention, security model. | Physical object design and unresolved compliance/security items. |
| `db/state/types/` | Enum vs controlled-code strategy. | Per-domain storage choices. |
| `db/state/sequences/` | Fiscal numbering/counter placeholder or final policy. | BIR/accounting final numbering and gap treatment. |
| `db/reference-data/` | Seed/reference separation and controlled-code governance. | Final code ownership and values. |
| `db/validation/` | Validation categories resolved. | Script language/tooling. |
| `db/rebuild/` | Rebuild workflow resolved. | Script technology and manifest strategy. |
| `db/drift/` | Drift policy resolved. | Tooling, optional Atlas decision, CI integration. |
| `db/samples/` | Sample/accreditation separation. | Sample data policy and final package expectations. |
| `db/accreditation/` | Minimum package target. | Examiner-confirmed outputs and evidence set. |

## 4. Authority Boundary Impact

Every future workstream must enforce these constraints:

- No POS-owned payment finality objects.
- No POS-owned PaymentAttempt or PaymentConfirmation lifecycle objects.
- No POS-owned ExitAuthorization issue, approve, mutate, or bypass objects.
- No POS-owned gate execution authority.
- Audit/events remain evidence only.
- Channels/terminals remain child endpoints under Site POS Server.
- ONLINE/OFFLINE remains observability only.
- Offline fiscal issuance remains disabled unless separately approved by BIR/accounting.

## 5. Readiness Recommendation

Recommended sequence:

1. Review and approve this gate-resolution package.
2. Create a folder-skeleton bootstrap task for `db/README.md` and approved empty folder layout only, if desired.
3. Run a physical schema/naming standards task.
4. Resolve PostgreSQL version/features, enum/control-code, idempotency, numbering, retention, Digital SI URL, ARTS mapping, and anchoring details.
5. Only then create SQL/object artifacts, scripts, CI workflows, seed/reference data, samples, or accreditation support files.