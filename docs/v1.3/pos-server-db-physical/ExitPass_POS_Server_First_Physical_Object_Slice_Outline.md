# ExitPass POS Server First Physical Object Slice Design v1.0 Outline

## Future Document

Proposed future document: `ExitPass_POS_Server_First_Physical_Object_Slice_Design_v1.0.md`

This outline is for a future design document only. It does not create the future design document and does not create SQL/object artifacts.

## Proposed Structure

1. Document Control
2. Purpose and Scope
3. Approved Baseline References
4. First Slice Rationale
5. Authority Boundary
6. Schema and Naming Baseline
7. Included Object Areas
8. Excluded Object Areas
9. Foundation / Configuration / Controlled-Code Object Design
10. Fiscal Identity / Site POS Server Boundary Object Design
11. Channel / Terminal Registry Object Design
12. Central PMS Reference Naming Object Design
13. Validation Plan
14. Open Questions
15. Risks and Mitigations
16. Non-Decisions
17. Appendices

## Section Intent

| Section | Intended content |
| --- | --- |
| Document Control | Title, version, repository, status, output format, approved baselines, and artifact boundary. |
| Purpose and Scope | State that the future document designs the first physical object slice at design level only. |
| Approved Baseline References | Link approved BRD, System Design, API Contract, Database Design, artifact plan, gate resolution, schema/naming standards, Physical Object Design, repository boundary, and `db/README.md`. |
| First Slice Rationale | Explain why the slice is low-risk and does not create issuance, numbering, payment, or exit authority. |
| Authority Boundary | Preserve Central PMS and POS Server authority separation. |
| Schema and Naming Baseline | Apply `pos`, lowercase `snake_case`, `_id`, `_ref`, `central_pms_*_ref`, and domain-specific status names. |
| Included Object Areas | List foundation/configuration/controlled-code posture, fiscal identity/Site POS Server boundary, channel/terminal registry, and Central PMS reference naming posture. |
| Excluded Object Areas | Exclude fiscal issuance, numbering/counters, idempotency constraints, Digital SI URL security objects, reprints, adjustments, reports, exports, audit/recovery, outbox, scripts, and CI. |
| Foundation / Configuration / Controlled-Code Object Design | Define provisional code/status/type/reason object posture and enum/control-code dependencies. |
| Fiscal Identity / Site POS Server Boundary Object Design | Define provisional fiscal identity and Site POS Server boundary object posture. |
| Channel / Terminal Registry Object Design | Define provisional channel/terminal registry, capabilities, health/status, and history posture. |
| Central PMS Reference Naming Object Design | Define reference-only naming and storage posture for Central PMS/vendor references. |
| Validation Plan | Define future validation requirements without creating scripts. |
| Open Questions | Carry first-slice open questions with affected areas and blocker status. |
| Risks and Mitigations | Prevent premature SQL/object creation and authority leakage. |
| Non-Decisions | Preserve non-decisions for final SQL, object files, constraints, indexes, seed/reference/sample data, scripts, CI, source code, and accreditation outputs. |
| Appendices | Candidate name disclaimer, authority-boundary checklist, and future artifact gate reminder. |

## Required Boundary Statement For Future Document

The future design document must state that it does not authorize SQL files, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, DOCX files, or diagrams.

## Recommended Next Step

Create an approval-readiness review for this planning package. After approval, draft `ExitPass_POS_Server_First_Physical_Object_Slice_Design_v1.0.md` as a design document only. Do not create SQL/object artifacts until a later task explicitly authorizes them.
