# ExitPass POS Server BIR Annex E External Confirmation Register v1.0

## Purpose

This register isolates questions that internal architecture cannot answer. Silence, an internal recommendation, or successful software tests do not satisfy these items.

It contains 10 currently active `REQUIRES_EXTERNAL_CONFIRMATION` records plus two deferred future-profile questions (AE-DR-013 and AE-DR-014). The deferred questions do not block bounded E-1.

Z-012A originally revalidated 14 active confirmations on 2026-08-10 PHT. Accounting subsequently resolved AE-DR-006 through AE-DR-009 through `Z-012B-ACCOUNTING-APPROVAL-001`; the remaining 10 are unchanged.

| ID | External question | Why internal resolution is insufficient / source ambiguity | Proposed question and acceptable evidence | Blocking impact | Interim safe posture |
| --- | --- | --- | --- | --- | --- |
| AE-DR-002 | Is deterministic XLSX using RMO 24-2023 E-1 accepted as the official electronic artifact, and are PDF/JSON required companions? | Official workbook is XLSX; API contract says formats remain open. | Written BIR/examiner response identifying accepted artifact and companions; signed accreditation minutes also acceptable. | Controlled UAT | Build only the approved internally labeled E-1 profile; no BIR-accepted claim. |
| AE-DR-004 | Is a filename prescribed? | No filename in local sources. | Written exact pattern, allowed characters, and duplicate/correction naming. | Controlled UAT | Use approved internal safe filename, clearly non-prescriptive. |
| AE-DR-010 | Which Remarks values/blanks are accepted? | Free-form column has no value contract. | Examiner-approved allowed values and blank rule. | Controlled UAT | Use approved internal controlled codes only; retain external acceptance gate. |
| AE-DR-011A | Are explicit zero values acceptable for inactive NAAC/Solo Parent categories? | Physical columns exist but applicability/null rules are absent. | Examiner response for zero versus blank/N/A and required active support. | Controlled UAT | Record explicit classification absence; unknown blocks. |
| AE-DR-012 | Where do Diplomat and other VAT privileges map? | BRD REP-012 is explicitly open. | Accounting/BIR field mapping, tax/discount distinction, and worked example. | Nonzero privilege path | Fail closed for a period containing unsupported treatment. |
| AE-DR-013 | Are E-2/E-3 required for ExitPass, and what lawful system may retain identity/TIN? | Workbook requires identity; POS privacy contract excludes it. | BIR applicability plus Legal/Privacy approved data-flow, lawful basis, access, and retention. | Future E-2/E-3 only | Deferred; collect nothing new. |
| AE-DR-014 | Are E-4/E-5 required, and what lawful source governs athlete/parent/child data? | Future workflows and highly sensitive data. | BIR applicability plus Legal/Privacy approved data-flow and retention. | Future E-4/E-5 only | Deferred; collect nothing new. |
| AE-DR-016 | Are signing, encryption, compression, or a submission channel mandatory? | No local specification. | Written BIR/examiner delivery specification and cryptographic requirements. | Controlled UAT/external delivery | Local authorized download only if user approves AE-DR-016A. |
| AE-DR-016B | What Annex E retention/archive duration and deletion hold apply? | BRD requires confirmed long-term retention but no duration. | Legal/Compliance retention schedule and records-owner approval. | Production | Keep configurable retention metadata; no production purge worker. |
| AE-DR-019 | What official number/date display profile is accepted? | Workbook uses General formatting. | Accounting/examiner-approved decimal places, separators, date format, currency, negatives, and zeros. | Controlled UAT | Use internal invariant profile only, labeled pending confirmation. |
| AE-DR-020A | What should H08 POS Terminal No. contain for one Site POS Server with child channels? | Workbook expects one value; architecture has many channels. | Examiner acceptance of governed Site POS Server fiscal terminal identity, or alternate exact rule. | Controlled UAT | Use server fiscal terminal identity internally; never concatenate channel labels. |
| AE-DR-024 | Must exact template geometry be preserved, and what wrapping is accepted? | Source has widths but no overflow rule. | Examiner-approved golden workbook/printed sample and overflow examples. | Controlled UAT | Preserve source geometry and fail unrepresentable values under the approved internal policy. |

## Z-012A authoritative classifications and staged gates

| ID | Authoritative resolution classification | Gate labels |
|---|---|---|
| AE-DR-002 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` |
| AE-DR-004 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` |
| AE-DR-010 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` |
| AE-DR-011A | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_NONZERO_PRIVILEGE_PATH`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` |
| AE-DR-012 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_NONZERO_PRIVILEGE_PATH`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` |
| AE-DR-016 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` |
| AE-DR-016B | `REQUIRES_LEGAL_COMPLIANCE_APPROVAL` | `BLOCKING_PRODUCTION` |
| AE-DR-019 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` |
| AE-DR-020A | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_SCHEMA_IMPLEMENTATION`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` |
| AE-DR-024 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` |

Exactly 10 rows appear in this table. The questions, acceptable evidence, and fail-closed posture in the primary table remain controlling. AE-DR-013 and AE-DR-014 remain separate deferred E-2 through E-5 matters.

## Evidence handling

Acceptable evidence must identify the authority, date, affected Annex E profile/version, exact decision ID, and selected rule. Do not store confidential correspondence or personal examiner contact details in the repository; store a controlled approval reference and a privacy-safe summary.

## Z-012A1 Accounting approval instrument

The 2026-08-10 Accounting statement establishes `ACCOUNTING_AUTHORITY_AND_APPROVAL_IN_PRINCIPLE_CONFIRMED` for AE-DR-006 through AE-DR-009. It does not approve an unstated formula. The exact proposal and pending instrument are:

* [Accounting Calculation Profile Proposal](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md)
* [Accounting Calculation Profile Approval Form](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Approval_Form_v1.0.md)

This section records the pre-approval Z-012A1 posture. It was superseded on 2026-08-10 by `Z-012B-ACCOUNTING-APPROVAL-001` after exact filename, version, profile, scope, content, and SHA-256 verification.

## Z-012A2 resolved Accounting confirmations

| ID | Resolution | Approval evidence | Current implementation posture |
|---|---|---|---|
| AE-DR-006 | Exact numbered-field mapping, named operands, formulas, source classes, and zero-tolerance reconciliations approved | `Z-012B-ACCOUNTING-APPROVAL-001`; immutable profile SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` | Implement exact approved profile in Z-012B |
| AE-DR-007 | Exact Manual SI/OR definition, period rule, inclusion/exclusion, first-class source contract, and known-zero rule approved | Same approval record and profile | Z-012B implements the first-class fact and attestation; missing remains fail closed |
| AE-DR-008 | Exact accumulated-sales-capacity overrun definition, exclusions, first-class source/event contract, and known-zero rule approved | Same approval record and profile | Z-012B implements the first-class fact/event and attestation; missing remains fail closed |
| AE-DR-009 | Exact Total Income definition `D29=D27+D06+D28` and rejected alternatives approved | Same approval record and profile | Implement exact checked minor-unit derivation in Z-012B |

These four items are no longer active external confirmations. No unrelated Accounting, BIR/examiner, Legal/Compliance, or deferred decision is resolved by this record.
