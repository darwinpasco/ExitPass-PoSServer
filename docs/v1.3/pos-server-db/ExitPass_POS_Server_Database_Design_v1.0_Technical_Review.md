# ExitPass POS Server Database Design v1.0 Technical Review

## 1. Review Summary

Review target: `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0.md`

Style/pattern reference: `D:\Docs\ExitPass\v1.2\ExitPass Database Design v1.2.docx`

Review scope:

- Repository and database ownership boundary.
- Authority model preservation.
- Alignment to ExitPass Database Design v1.2 writing pattern.
- Logical design scope discipline and over-specification check.
- Coverage of POS Server fiscal database design areas.
- Open question hygiene.
- Readiness for targeted revision or approval-readiness review.

Summary result: The draft is structurally aligned with the v1.2 database design writing pattern and preserves the approved POS Server authority model. No P0 or P1 findings were identified. The draft is ready for approval-readiness review. Optional P2 cleanups can be handled before approval-readiness review or folded into the future physical database artifact task.

## 2. Overall Recommendation

Recommendation: Proceed to approval-readiness review.

Rationale:

- The document now has v1.2-style document control, reference hierarchy, conflict rules, design principles, database overview, identifier/status discipline, audit/traceability model, logical data model, bounded-area specifications, source traceability, risks, non-decisions, and physical design gate.
- The draft remains logical/design-level and does not create or imply final SQL DDL, physical tables, columns, constraints, indexes, Atlas files, endpoint DTOs, event payloads, or final RBAC implementation.
- The POS Server-specific content is preserved, including fiscal identity, fiscal documents, fiscal lines, counters, Digital SI URL, QR boundary, reprints, adjustments, reports, EJ/POSLog/exports, audit, recovery, retention, ARTS/BIR mapping, and state-based versioning.

## 3. Blocking Findings

No P0 findings identified.

## 4. Should-Fix Findings

No P1 findings identified.

## 5. Non-Blocking Findings

| ID | Severity | Section | Finding | Why it matters | Recommended correction |
| --- | --- | --- | --- | --- | --- |
| DBR-P2-001 | P2 | Open Questions | The Open Questions table carries the correct unresolved topics, but it does not show source, blocking scope, or downstream owner detail at the same level as the planning artifact. | The draft is still usable, but physical database design and implementation planning will benefit from knowing which questions block BIR/accounting, API, security/privacy, database, engineering, or accreditation work. | Add optional columns such as source, blocks physical design, blocks implementation, and target owner, or keep the current table and link explicitly to the planning open-question artifact for full classification. |
| DBR-P2-002 | P2 | Logical Data Area Specifications | The area-by-area sections are well structured, but most areas do not include a dedicated downstream physical design note. | The v1.2 pattern is explicit about how logical design flows into downstream artifacts. A short physical-design note per area would make handoff to object-level database work cleaner. | Add a brief `Future physical design notes` row or subsection per major area, limited to physical-design considerations and without defining table/column names. |
| DBR-P2-003 | P2 | State-Based Versioning Plan / Physical Design Gate | The state-based versioning posture is correct, but the future physical artifact gate could be more operational as a checklist. | This is not blocking for the logical design, but the next database task will need a concrete gate for artifact folder layout, rebuild validation, drift checks, seed separation, and CI evidence. | Expand Appendix C or the State-Based Versioning section with a short checklist for the future physical artifact task. |

## 6. Editorial Findings

| ID | Severity | Section | Finding | Why it matters | Recommended correction |
| --- | --- | --- | --- | --- | --- |
| DBR-ED-001 | Editorial | Throughout | The draft uses both `Digital SI URL` and `digital SI` forms. This is understandable, but capitalization could be standardized. | Consistent terminology improves review readability and future searchability. | Standardize on `Digital SI URL` for the named database concept and `digital Sales Invoice` for the customer-facing output. |
| DBR-ED-002 | Editorial | Source Traceability | The traceability table is useful but compact. | It is adequate for review, but future reviewers may benefit from section references. | Add section references in the traceability table when preparing for approval-readiness review. |

## 7. Repository Boundary Review

Result: Pass.

The draft correctly treats `ExitPass-PoSServer` as the repository that owns POS Server database design and future POS Server database artifacts. It also clearly states that the main ExitPass repository remains responsible for Central PMS, WebPay/platform channels, payment orchestration, parking session state, site resolution, PaymentAttempt, PaymentConfirmation, payment finality, and ExitAuthorization.

The document repeatedly states that POS Server database stores Central PMS references only and must not own Central PMS state. The ownership matrix in `Database Overview` is especially clear and appropriate for downstream implementation readers.

No repository-boundary violations were found.

## 8. Authority Boundary Review

Result: Pass.

The draft preserves the approved authority model:

- Central PMS owns parking session control state.
- Central PMS owns site resolution.
- Central PMS owns payment finality.
- Central PMS owns PaymentAttempt and PaymentConfirmation.
- Central PMS owns ExitAuthorization.
- POS Server does not declare payment finality.
- POS Server does not issue, own, or mutate ExitAuthorization.
- POS Server owns fiscal issuance and fiscal records only.
- Channels/terminals are child records under the Site POS Server and are not independent fiscal authorities.
- Vendor PMS / HikCentral acknowledgment is synchronization/context only.
- POS/fiscal events and audit records are observability/integration evidence only and not payment or exit authority.

No wording was found that implies POS Server database owns payment finality or ExitAuthorization.

## 9. v1.2 Writing Pattern Alignment Review

Result: Pass.

The draft reasonably follows the ExitPass Database Design v1.2 writing pattern without copying unrelated v1.2 Central PMS content.

Alignment observed:

- Document Control includes document information, version history, approval/baseline status, baseline scope, controlled change rule, and downstream artifact rule.
- Purpose, Scope, Reference Documents, Reference Hierarchy, Conflict Rule, and Version Alignment Note mirror the v1.2 discipline.
- Design Principles are explicit and database-control oriented.
- Database Overview includes ownership and domain inventory tables.
- Naming, Identifier, and Status Standards are included at logical level.
- Audit and Traceability Model is included before the logical data model.
- Logical Data Model includes conceptual relationships and a logical record group inventory.
- Logical Data Area Specifications use purpose, candidate records, candidate attributes, and design rules.
- Risks, source traceability, non-decisions, and physical design gate are present.

The draft correctly avoids the v1.2 physical table specification pattern because this POS Server draft is intentionally logical-only at this stage.

## 10. Scope and Over-Specification Review

Result: Pass.

The draft stays within logical database design scope. It defines conceptual data model, logical data areas, candidate logical records, candidate attributes, lifecycle rules, integrity expectations, database impacts, and state-based versioning posture.

No premature definitions were found for:

- final SQL DDL;
- final physical table names;
- final physical column names;
- final indexes;
- final constraints;
- final enum storage;
- final schemas;
- Atlas migration/state files;
- physical object files;
- endpoint DTOs;
- event payload schemas;
- final RBAC matrix.

The document explicitly records these as non-decisions.

## 11. Coverage Review

Result: Pass.

The draft covers all required database design areas:

- fiscal identity and Site POS Server boundary;
- channel and terminal registry;
- fiscal documents;
- fiscal lines;
- tender, tax, discount, and totals;
- numbering and counter state;
- idempotency and retry;
- Digital SI URL and access audit;
- QR presentation data boundary;
- reprints;
- fiscal adjustments;
- X-read and Z-read;
- BIR Sales Summary and Annex E;
- EJ, POSLog, JSON, and exports;
- audit trail;
- recovery and tamper-evident continuity;
- security/RBAC and privacy data impacts;
- retention and archival;
- Central PMS integration references;
- ARTS POSLog / BIR extension mapping;
- state-based versioning plan;
- open questions;
- risks and mitigations;
- non-decisions;
- source traceability;
- physical design gate.

No required coverage gap was found.

## 12. Fiscal Document and Fiscal Line Review

Result: Pass.

The draft keeps Sales Invoice as the primary fiscal output and states that fiscal documents belong to the Site POS Server. It also states that fiscal documents reference Central PMS payment/session/finality context without owning it, and that fiscal document records do not store or create ExitAuthorization authority.

Fiscal line coverage is explicit and ordered. The model supports VATable, VAT-exempt, zero-rated, non-VAT, statutory discount, VAT privilege/exemption, coupon, parking fee, lost ticket fee, overstay fee, penalty, service charge, and adjustment amounts.

Diplomat VAT Privilege / VAT Exemption is correctly represented as VAT privilege/exemption treatment, not as an ordinary commercial discount. Exact treatment remains open.

## 13. Numbering, Counters, and Recovery Review

Result: Pass.

The draft covers:

- Sales Invoice sequence state;
- adjustment sequence state;
- reset counter;
- Z-counter;
- Grand Total Amount accumulator;
- previous Grand Total snapshot;
- previous reset counter snapshot;
- latest EJ hash;
- last fiscal event timestamp;
- fiscal state snapshot;
- recovery continuity reference;
- fiscal lock/block status;
- supervised recovery and recovery audit record;
- no resume from lower counters, lower GTA, earlier SI sequence, broken EJ hash, or earlier event timestamp.

The draft does not decide exact numbering patterns, sequence gaps, reserved numbers, failed issuance, or abandoned issuance prematurely. These remain open.

## 14. Idempotency and Retry Review

Result: Pass.

The draft supports the expected idempotency and retry concepts:

- idempotency key;
- idempotency scope;
- request semantic hash/request identity;
- linked fiscal operation;
- replay result;
- conflict status;
- timeout;
- completion unknown;
- retry status;
- exception reference;
- retention policy reference.

The design states that duplicate request handling must not create duplicate Sales Invoices or duplicate fiscal adjustment documents.

## 15. Digital SI URL and QR Boundary Review

Result: Pass.

The draft correctly states that POS Server stores and returns the Digital SI URL, while QR code conversion/display/printing is a channel or terminal responsibility where supported.

The QR presentation section clearly states:

- POS Server does not generate or store a required QR image as fiscal authority.
- Channel/terminal converts URL into QR where supported.
- QR presentation does not make the channel/terminal the fiscal issuer.
- Site POS Server remains the fiscal issuer.

Digital SI URL token/access/expiry/authentication model remains open for Security/Privacy Review.

## 16. Reports, EJ, POSLog, JSON, and Export Review

Result: Pass.

The draft covers:

- X-read and Z-read report scope/status/output references;
- BIR Sales Summary / Annex E-1 minimum contents;
- Annex E-2 to E-5 support;
- Diplomat VAT privilege/exemption reporting support;
- EJ records;
- POSLog exports;
- JSON/POSLog schema versioning;
- export validation status and errors;
- ARTS POSLog 6.x-aligned export posture;
- local/BIR extension mapping;
- export retention references.

ARTS POSLog is correctly treated as a structured export/schema interoperability reference only. It does not replace Philippine BIR fiscal outputs.

## 17. Audit, Security, Privacy, and Retention Review

Result: Pass.

The draft covers fiscal audit records, actor/service references, approval references, evidence references, unauthorized action audit, configuration change audit, ONLINE/OFFLINE status audit where required, sensitive evidence separation, Digital SI URL access audit, privileged export access audit, retention categories, and archival posture.

Final RBAC, evidence model, and retention periods remain open as expected.

## 18. State-Based Versioning Review

Result: Pass with P2 improvement opportunity.

The draft reflects the required posture:

- repository-owned database object definitions as future source of truth;
- per-object SQL files where practical;
- repeatable rebuilds;
- validation scripts;
- drift checks;
- Atlas/state-based comparison option;
- no local drift promotion;
- seed/reference data separation;
- PR review and validation for database changes.

No SQL/Atlas files are created or implied as already existing. A future targeted cleanup could expand the physical design gate into a more concrete checklist before object-level artifact work begins.

## 19. Open Questions Review

Result: Pass with P2 improvement opportunity.

The draft carries forward the major unresolved items without reopening approved decisions. It includes:

- MIN/PTU/serial/software/supplier assignment;
- WebPay fiscal terminal identity;
- exact SI numbering pattern;
- exact adjustment numbering pattern;
- sequence gaps, reserved numbers, failed issuance, abandoned issuance;
- X-read and Z-read aggregation scope;
- exact VAT/tax treatment;
- Diplomat VAT treatment, evidence, wording, reporting, retention;
- Digital SI URL token/access/expiry/authentication model;
- exact report/export formats and layouts;
- exact ARTS POSLog profile and schema mapping;
- exact JSON schema versioning strategy;
- final accreditation sample package;
- tamper-evident anchoring mechanism;
- final endpoint names and DTOs;
- final event payloads;
- final RBAC matrix;
- final physical table/column/index/constraint design;
- offline fiscal issuance approval, if any.

No decided item appears to have been reopened. The only improvement is classification detail for downstream blocking/ownership.

## 20. Recommended Targeted Edits

No blocking targeted edits are required.

Optional P2/editorial edits before approval-readiness review:

1. Add source/blocking/owner columns to the Open Questions table, or explicitly reference the planning open-question artifact for full classification.
2. Add short future physical design notes for each major logical data area.
3. Expand the Physical Design Gate into a checklist for the next database artifact task.
4. Standardize `Digital SI URL` capitalization and add section references to the Source Traceability table.

## 21. Recommended Next Step

Proceed to approval-readiness review for the POS Server Database Design v1.0 draft.

If preferred, apply a small P2/editorial cleanup first, focused only on open-question classification, physical design gate checklist, and terminology consistency. No redesign is needed.