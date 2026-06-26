# ExitPass POS Server First Physical Object Slice Source Analysis

## 1. Purpose

This source analysis supports the first physical object slice artifact planning package for the ExitPass POS Server database. It translates the approved first-slice posture into a documentation-only plan for a future physical object artifact task.

This package does not create SQL, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, DOCX files, or diagrams.

## 2. Approval / Baseline Status

This First Physical Object Slice Planning Package is approved as the planning baseline for drafting `ExitPass_POS_Server_First_Physical_Object_Slice_Design_v1.0.md`.

Approval of this planning package does not authorize SQL files, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation scripts, rebuild scripts, drift scripts, CI workflows, source code, DOCX files, or diagrams.

The approved first-slice planning scope remains limited to foundation / configuration / controlled-code posture, fiscal identity / Site POS Server boundary, channel/terminal registry, and Central PMS reference naming posture.

Excluded areas remain out of scope: fiscal document issuance, Sales Invoice issuance objects, SI/adjustment numbering implementation, fiscal counters, idempotency physical constraints/indexes, Digital SI URL token/access objects, reprints, adjustments, reports, exports, audit trail physical objects, recovery/anchoring physical objects, optional events/outbox, validation/rebuild/drift scripts, and CI workflows.

## 3. Sources Inspected

| Source | Use in this planning package |
| --- | --- |
| `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` | Approved fiscal/business scope baseline. |
| `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` | Approved system authority, site, channel, audit, and operational boundary baseline. |
| `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` | Approved API and reference posture baseline. |
| `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0.md` | Approved logical database design baseline. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Plan_v1.0.md` | Approved state-based artifact layout, rebuild, validation, and drift posture. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Gate_Resolution_v1.0.md` | Approved gate classifications and placeholder-policy posture. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Schema_Naming_Standards_v1.0.md` | Approved `pos` schema and naming baseline. |
| `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_Object_Design_v1.0.md` | Approved physical object design baseline and first-slice recommendation. |
| Physical object design technical and approval-readiness reviews | Confirmation that the physical object design baseline had P0/P1/P2/editorial findings of 0. |
| Physical object design planning files | Sequencing, grouping, open questions, and impact-map inputs. |
| Gate readiness/open questions/impact map | Remaining blockers for SQL/object artifacts and validation tooling. |
| Schema/naming decision/open questions | Naming safeguards and remaining physical naming questions. |
| `docs/REPOSITORY_BOUNDARY.md` and `db/README.md` | Repository and `db/` folder boundary. |

## 4. Approved Baselines

The first slice inherits these approved decisions:

- POS Server database artifacts remain in `ExitPass-PoSServer`.
- The future `db/` boundary is a folder boundary, not a separate repository.
- Repository-owned state-based artifacts are the future source of truth.
- Local drift must not be promoted unless represented by reviewed repository artifacts.
- The primary PostgreSQL schema posture is `pos`.
- Future names use lowercase `snake_case`, no quoted identifiers, `_id` for internal identifiers, and `_ref` or explicit source prefixes for external references.
- Central PMS authority records must be named as references only.

## 5. First-Slice Rationale

| Rationale | Planning impact |
| --- | --- |
| Establishes foundation and configuration posture first | Future status/type/reason/classification artifacts can be designed before dependent fiscal objects. |
| Establishes Site POS Server fiscal boundary | Future fiscal documents can reference a known POS Server fiscal authority boundary. |
| Establishes channel/terminal registry posture | Future issuance records can reference child endpoints without treating terminals as independent fiscal authorities. |
| Establishes Central PMS reference naming posture | Future fiscal objects can safely reference Central PMS records without owning payment or exit authority. |
| Avoids fiscal issuance side effects | The first slice does not create Sales Invoice issuance, fiscal document creation, numbering, or counters. |
| Avoids payment or exit authority leakage | The first slice prohibits POS-owned payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, and ExitAuthorization lifecycle names. |

## 6. Included Areas

The first future physical object artifact slice should cover planning for these areas only:

1. Foundation / configuration / controlled-code posture.
2. Fiscal identity / Site POS Server boundary.
3. Channel/terminal registry.
4. Central PMS reference naming posture.

All names remain provisional until a later approved SQL/object artifact task.

## 7. Excluded Areas

The first slice must not include object artifact planning for fiscal document issuance, Sales Invoice issuance objects, SI/adjustment numbering implementation, fiscal counters, idempotency physical constraints/indexes, Digital SI URL token/access objects, reprints, adjustments, reports, exports, audit trail physical objects, recovery/anchoring physical objects, optional events/outbox, validation/rebuild/drift scripts, or CI workflows.

## 8. Readiness Summary

| First-slice area | Planning readiness | SQL/object artifact readiness | Notes |
| --- | --- | --- | --- |
| Foundation / configuration / controlled-code posture | Ready for planning. | Ready only after enum vs controlled-code decisions are confirmed per domain. | This package does not choose physical enum/table implementation. |
| Fiscal identity / Site POS Server boundary | Ready for planning. | Partially blocked pending BIR/accreditation details. | MIN/PTU/serial/software/supplier assignment remains external-confirmation dependent. |
| Channel/terminal registry | Ready for planning. | Ready only after final fields and capabilities are confirmed. | WebPay remains a logical channel/terminal under Site POS Server. |
| Central PMS reference naming posture | Ready for planning. | Ready as naming posture; object artifacts remain future task. | References only; no Central PMS lifecycle ownership. |

## 9. Out of Scope

This planning package does not create or authorize SQL files, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data files, validation/rebuild/drift scripts, CI workflows, source code, DOCX files, diagrams, changes to approved baselines, or changes to `db/README.md`.
