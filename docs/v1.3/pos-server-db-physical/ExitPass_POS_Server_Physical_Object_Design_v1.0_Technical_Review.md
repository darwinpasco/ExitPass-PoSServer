# ExitPass POS Server Physical Object Design v1.0 Technical Review

## 1. Review Summary

This technical review assessed `ExitPass_POS_Server_Physical_Object_Design_v1.0.md` against the approved logical Database Design, Physical DB Artifact Plan, Physical DB Gate Resolution, Schema/Naming Standards, approved Physical Object Design planning package, repository boundary, and `db/README.md` bootstrap boundary.

The draft remains documentation-only, preserves the approved authority model, applies the approved schema/naming baseline, carries forward the required planning-review P2 items, and covers the required physical object design areas at a proposed/design level only. Candidate object names are clearly provisional and do not create SQL, DDL, Atlas, migrations, physical object files, seed/reference/sample data, scripts, CI workflows, or source code.

## 2. Overall Recommendation

Recommendation: proceed to approval-readiness review.

P0 findings: 0
P1 findings: 0
P2 findings: 0
Editorial findings: 0

The draft is ready for approval-readiness review. SQL/object artifact creation must remain blocked until a separate approved artifact task is created after the remaining physical gates are resolved or explicitly handled by approved placeholder policy.

## 3. Blocking Findings

No P0 blocking findings.

## 4. Should-Fix Findings

No P1 should-fix findings.

## 5. Non-Blocking Findings

No P2 non-blocking findings.

## 6. Editorial Findings

No editorial findings.

## 7. Scope Discipline Review

The draft remains documentation-only. It states that it does not create or approve SQL DDL, Atlas files, migrations, physical object files, actual table creation, actual constraints/indexes, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, DOCX files, or diagrams.

Candidate object names are consistently presented as provisional examples. The draft also states that all proposed object names remain provisional until a future SQL/object artifact task creates actual files under `db/state`.

No scope-discipline issue was found.

## 8. Baseline Alignment Review

The draft aligns with the approved baseline set:

| Baseline | Review result |
| --- | --- |
| POS/Invoicing BRD v1.0 | Fiscal scope and BIR terminology are preserved. |
| POS Server System Design v1.0 | Authority, audit, recovery, and operational boundaries are preserved. |
| POS Server API Contract v1.0 | Idempotency, Digital SI URL, reprints, reports, exports, and reference posture are reflected without DTO/table coupling. |
| POS Server Database Design v1.0 | Logical areas are translated into proposed physical object areas without final DDL. |
| Physical DB Artifact Plan v1.0 | State-based artifact, rebuild, validation, drift, and PR evidence posture is preserved. |
| Physical DB Gate Resolution v1.0 | Gate-blocked areas remain blocked, and placeholder policies are not overstated as final artifacts. |
| Schema/Naming Standards v1.0 | `pos`, lowercase `snake_case`, `_id`, `_ref`, and authority-safe naming rules are carried forward. |
| Physical Object Design planning package | Sequencing, grouping, open questions, impact map, and outline are reflected in the draft. |

No baseline alignment issue was found.

## 9. Required Carry-Forward Items Review

The draft addresses both required carry-forward items from the planning package approval-readiness review:

| Carry-forward item | Review result |
| --- | --- |
| Explicit first-slice recommendation | Present in Section 11. The first slice is limited to foundation/configuration/controlled-code posture, fiscal identity/Site POS Server boundary, channel/terminal registry, and Central PMS reference naming posture. |
| Affected-object-area mapping for open questions | Present in Section 33. Open questions include affected object area(s), blocker status, current posture, and target resolution step. |

No carry-forward gap was found.

## 10. Authority Boundary Review

The draft preserves the approved authority boundary:

- Central PMS owns payment finality.
- Central PMS owns PaymentAttempt and PaymentConfirmation.
- Central PMS owns ExitAuthorization.
- POS Server owns fiscal issuance and fiscal records only.
- POS Server database stores references to Central PMS authority records only.
- POS Server database does not own payment finality.
- POS Server database does not own, issue, or mutate ExitAuthorization.
- Audit and events are evidence only.
- Channels/terminals are child endpoints under Site POS Server and are not independent fiscal authorities.
- ONLINE/OFFLINE is observability only.
- Offline fiscal issuance is disabled by default unless BIR/accounting approves a compliant model.
- ARTS POSLog is a structured export reference only and does not replace BIR outputs.

The object-area sections also repeat the relevant authority safeguard where the risk is most likely: fiscal documents, fiscal details, numbering/counters, Digital SI URL, audit, recovery, integration references, and optional events/outbox.

No authority leakage was found.

## 11. Schema and Naming Baseline Review

The draft applies the approved naming baseline:

- primary PostgreSQL schema posture: `pos`;
- lowercase `snake_case`;
- no quoted identifiers;
- `_id` for internal identifiers;
- `_ref` or explicit source prefixes for external authority references;
- domain-specific status names;
- fiscal numbers distinct from internal IDs;
- Central PMS records named as references only;
- no names implying POS Server owns payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, ExitAuthorization, gate execution, vendor authority, or independent terminal fiscal authority.

The candidate examples follow the naming posture and are marked provisional. No naming-baseline issue was found.

## 12. Physical Object Design Sequence Review

The draft uses the approved sequence:

1. Foundation and controlled codes.
2. Fiscal identity and Site POS Server boundary.
3. Channel/terminal registry.
4. Fiscal document core.
5. Fiscal lines, tenders, tax, discount, and totals.
6. Numbering and counter state.
7. Idempotency and retry.
8. Digital SI URL and access audit.
9. Reprints and fiscal adjustments.
10. Reports: X-read, Z-read, BIR Sales Summary, Annex E.
11. EJ, POSLog, JSON, and exports.
12. Audit trail.
13. Recovery and tamper-evident continuity.
14. Security/privacy references.
15. Optional events/outbox if approved later.

The sequence is dependency-safe because foundation, identity, and channel registry precede fiscal issuance records; fiscal documents precede lines, totals, numbering, Digital SI URL, reprints, reports, exports, audit, and recovery; optional outbox remains last because events must not become authority and require event-contract approval.

## 13. First Slice Recommendation Review

The first-slice recommendation is explicit and safe. It includes:

- foundation / configuration / controlled-code posture;
- fiscal identity / Site POS Server boundary;
- channel/terminal registry;
- Central PMS reference naming posture.

The draft correctly says this first slice should avoid fiscal document issuance, SI/adjustment numbering, counters, reports, exports, recovery/anchoring, sensitive Digital SI URL security objects, and optional outbox/events unless the relevant gates are resolved or placeholder policies are explicitly accepted.

No first-slice issue was found.

## 14. Object Area Coverage Review

The draft covers all required object areas and uses a consistent table pattern for each area: purpose, candidate object groups, candidate physical object names marked provisional, key/reference posture, dependency on prior object groups, unresolved gates, authority-boundary considerations, validation implications, and SQL/object readiness.

| Required object area | Review result |
| --- | --- |
| Foundation and controlled codes | Covered. |
| Fiscal identity and Site POS Server boundary | Covered. |
| Channel and terminal registry | Covered. |
| Fiscal document core | Covered. |
| Fiscal lines, tender, tax, discount, and totals | Covered. |
| Numbering and counter state | Covered. |
| Idempotency and retry | Covered. |
| Digital SI URL and access audit | Covered. |
| Reprint objects | Covered. |
| Fiscal adjustment objects | Covered. |
| X-read and Z-read objects | Covered. |
| BIR Sales Summary and Annex E objects | Covered. |
| EJ, POSLog, JSON, and export objects | Covered. |
| Audit trail objects | Covered. |
| Recovery and tamper-evident continuity objects | Covered. |
| Security/privacy reference objects | Covered. |
| Central PMS and vendor integration reference objects | Covered. |
| Optional events/outbox objects, if approved | Covered and correctly optional. |

No coverage issue was found.

## 15. Open Questions Mapping Review

Open questions are grouped by Engineering / Operations, Physical DB Design, BIR / Accounting, Security / Privacy, Engineering Pack / CI/CD, and BIR / Accreditation / Vendor.

Each open-question row includes the question, current posture, affected object area(s), blocker status, and target resolution step. The questions carry forward unresolved gates and do not reopen approved decisions.

No open-question mapping issue was found.

## 16. Validation and Drift Implications Review

The draft covers future validation needs for:

- naming validation;
- authority-boundary validation;
- object existence validation;
- reference validation;
- idempotency validation;
- numbering/counter validation;
- report/export validation;
- audit/recovery validation;
- security/privacy validation;
- drift-check validation.

It also preserves the no-local-drift-promotion rule: live/test database drift must be compared to reviewed repository artifacts, reported, and reviewed rather than automatically promoted.

No validation or drift-check posture issue was found.

## 17. Risks and Non-Decisions Review

The risks and mitigations are sufficient to prevent premature SQL/object creation and authority leakage. They explicitly address POS-owned payment finality naming, PaymentAttempt/PaymentConfirmation lifecycle naming, ExitAuthorization naming, independent terminal fiscal authority naming, DTO/table coupling, fiscal numbering before BIR confirmation, Digital SI URL security timing, report/export accreditation timing, recovery/anchoring timing, optional outbox authority risk, and local drift promotion.

The draft explicitly preserves non-decisions for SQL DDL, Atlas files, migrations, physical object files, final table definitions, final column lists, final constraints/indexes, final enum/type implementation, final seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, final BIR/accreditation package, offline fiscal issuance approval, and separate database repository.

No risk or non-decision issue was found.

## 18. Recommended Targeted Edits

No targeted edits are required before approval-readiness review.

## 19. Recommended Next Step

Proceed to approval-readiness review for `ExitPass_POS_Server_Physical_Object_Design_v1.0.md`.

Do not create SQL, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, DOCX files, or diagrams as part of the approval-readiness review.
