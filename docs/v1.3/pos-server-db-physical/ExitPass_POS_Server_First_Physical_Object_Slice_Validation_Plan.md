# ExitPass POS Server First Physical Object Slice Validation Plan

## 1. Purpose

This validation plan identifies future validation categories needed before, during, and after any approved first-slice physical object artifact task. It does not create validation scripts.

## 2. Validation Principles

| Principle | First-slice validation impact |
| --- | --- |
| Repository state is source of truth | Validate future objects against reviewed repository artifacts only. |
| Documentation does not create artifacts | Validate that planning package changes do not add SQL/object files. |
| Authority boundaries are enforceable by naming | Validate no POS-owned payment finality, PaymentAttempt, PaymentConfirmation, or ExitAuthorization names. |
| First slice is low-risk | Validate no fiscal issuance, numbering, counters, Digital SI URL security objects, reports, exports, audit/recovery objects, or outbox objects are introduced. |
| Drift must not become baseline | Validate drift-check posture and evidence in future artifact tasks. |

## 3. Future Validation Categories

| Validation category | Future validation target | First-slice scope |
| --- | --- | --- |
| Naming compliance | Object names, file names, schema names, status/code names. | Verify lowercase `snake_case`, no quoted identifiers, approved acronyms, `_id`, `_ref`, and source prefixes. |
| Object existence | Approved first-slice object artifacts only. | Confirm only approved foundation, fiscal identity, channel/terminal registry, and reference-naming objects exist. |
| Authority-boundary naming | Names that could imply ownership of external authority. | Block names implying POS-owned payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, ExitAuthorization, gate execution, vendor authority, independent terminal fiscal authority, or offline issuance approval. |
| Central PMS reference-only fields | Central PMS-related names and references. | Confirm `central_pms_*_ref` or approved reference-only naming is used and no lifecycle ownership is implied. |
| Channel/terminal child-endpoint posture | Registry and capability object names. | Confirm channels/terminals remain children of Site POS Server and are not independent fiscal authorities. |
| ONLINE/OFFLINE observability-only posture | Health/status values and names. | Confirm status names do not imply offline fiscal issuance approval. |
| Controlled-code/status posture | Code families and status names. | Confirm status/type/reason values are domain-specific and do not imply unauthorized authority. |
| No payment finality ownership | Object, field, file, and status names. | Confirm payment finality appears only as reference/context if used. |
| No ExitAuthorization ownership | Object, field, file, and status names. | Confirm no POS-owned ExitAuthorization or gate authorization artifacts. |
| No SQL/object artifact until approved | Repository status and file inventory. | Confirm planning tasks do not create SQL, DDL, Atlas, migrations, object files, seed/reference/sample data, scripts, or CI workflows. |
| Drift-check readiness | Future drift-check evidence. | Confirm live/test drift is reported and not promoted automatically. |

## 4. First-Slice File Inventory Validation

Future first-slice artifact tasks should prove:

- only approved first-slice object files were added;
- no fiscal document, numbering, counter, Digital SI URL security, report, export, audit/recovery, or outbox files were added;
- no seed/reference/sample data files were added unless separately approved;
- no validation/rebuild/drift scripts or CI workflows were added unless separately approved;
- no source code or approved baseline documents were modified.

## 5. Future PR Evidence

A future first-slice artifact PR should include object inventory, naming compliance results, authority-boundary naming results, Central PMS reference-only validation results, channel/terminal child-endpoint validation results, ONLINE/OFFLINE observability-only validation results, controlled-code/status posture validation results, no POS-owned payment finality validation results, no POS-owned ExitAuthorization validation results, and drift-check readiness or explicit reason if drift tooling is not yet available.

## 6. Out of Scope

This plan does not create validation scripts, rebuild scripts, drift scripts, CI workflows, SQL files, DDL, Atlas files, migrations, physical object files, seed/reference/sample data, source code, DOCX files, or diagrams.
