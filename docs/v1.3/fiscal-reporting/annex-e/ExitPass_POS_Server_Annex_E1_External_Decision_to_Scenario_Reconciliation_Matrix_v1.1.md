# ExitPass POS Server Annex E-1 External Decision-to-Scenario Reconciliation Matrix v1.1

## 1. Control rule

All ten decisions remain exactly `UNRESOLVED`. The scenario set is the union of the Z-012D catalogue, Z-012D external-confirmation tracker, Controlled UAT authorization review, and v1.0 reconciliation matrix. v1.1 changes no authority, decision, interpretation, or approval state.

| Decision | Authority / status | Union of prior scenario references | Included v1.1 cases affected | Specification | Implementation / loading / execution / generation | Delivery / submission | Required evidence |
|---|---|---|---|---|---|---|---|
| AE-DR-002 | BIR/examiner; `UNRESOLVED` | 001, 012, 025 | 001, 012 | internal deterministic artifact semantics may be specified | blocked by canonical implementation gate; official artifact acceptance blocked | blocked | written accepted artifact and companion-format rule |
| AE-DR-004 | BIR/examiner; `UNRESOLVED` | 001, 012, 015, 025 | 001, 012; 015 excluded | current filename may be specified without external acceptance | correction scenario remains excluded; all execution blocked | blocked | exact filename, duplicate, version, and correction rules |
| AE-DR-010 | BIR/examiner; `UNRESOLVED` | 001, 002, 025 | 001; 002 excluded | included rows use only `NONE`; no no-activity behavior is exercised | no-activity implementation/execution remains excluded | blocked | accepted Remarks codes and blank rule |
| AE-DR-011A | BIR/examiner; `UNRESOLVED` | 002, 006, 008, 020, 025 | 020; 002/006/008 excluded | v1.1 preserves explicit scoped zero facts without asserting external presentation acceptance | affected execution remains blocked; nonzero path prohibited | blocked | inactive NAAC/Solo representation and active-category rule |
| AE-DR-012 | Accounting and BIR/examiner; `UNRESOLVED` | 006, 008, 022 | none; all excluded | zero-only fail-closed posture retained | nonzero implementation/loading/execution/generation prohibited | blocked | approved source, destination, formula, classification, and privacy treatment |
| AE-DR-016 | BIR/examiner and Security/Operations; `UNRESOLVED` | 025 | none; 025 excluded | external boundary retained | signing/encryption/channel implementation prohibited | blocked | signing, encryption, compression, and submission requirements |
| AE-DR-016B | Legal/Compliance/Records; `UNRESOLVED` | 015, 024 | 024; 015 excluded | invocation-owned unreferenced cleanup may be specified | destructive cleanup remains prohibited | retention/purge blocked | retention, archive, hold, deletion authority, and evidence rules |
| AE-DR-019 | Accounting and BIR/examiner; `UNRESOLVED` | 001, 005, 007, 008, 012, 025 | 001, 005, 007, 012; 008/025 excluded | invariant internal display values are provisional | official display execution/acceptance blocked | blocked | exact decimal, date, currency, negative, and zero display profile |
| AE-DR-020A | BIR/examiner; `UNRESOLVED` | 001, 005, 006, 025 | 001, 005; 006/025 excluded | H08 Site POS Server code remains provisional | affected header acceptance/execution blocked | blocked | accepted H08 identity rule |
| AE-DR-024 | BIR/examiner; `UNRESOLVED` | 001, 005, 006, 012, 025 | 001, 005, 012; 006/025 excluded | current geometry may be referenced internally | official geometry execution/acceptance blocked | blocked | examiner-approved golden workbook, wrapping cells, and overflow rule |

The union reconciliations for AE-DR-011A, AE-DR-012, AE-DR-019, AE-DR-020A, and AE-DR-024 are unchanged. Cases 004 and 005 no longer exercise the no-activity fingerprint, so no new AE-DR-010 or AE-DR-011A cross-reference is introduced.

## 2. Gate statements

The 19 included cases may be specified and reviewed. The six excluded scenarios remain non-executable. Dataset and validator implementation, database loading, environment provisioning, scenario execution, workbook generation, evidence acceptance, external delivery, BIR submission, and Production remain unauthorized.

No decision is resolved, accepted, waived, assumed, superseded, or marked inapplicable. Silence, synthetic zero, current renderer behavior, or internal convenience is not external approval.
