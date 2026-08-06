# ExitPass POS Server BIR Annex E Open Decisions and Assumptions Register v1.0

## 1. Status rules

- `OPEN_BLOCKING`: must be approved before the affected runtime scope.
- `OPEN_NONBLOCKING`: may remain open only when the affected feature is excluded.
- `APPROVED`: requires an explicit approval source and date; none is created by this document.
- `REJECTED`: requires an explicit authority.

Every entry below is a recommendation, not an approval. The required approver roles name the needed authority; they do not assert that BIR has approved the recommendation.

## 2. Decision register

| ID | Question and ambiguity source | Options | Impacts | Recommendation and rationale | Required approver | Status / blocking scope | Acceptance consequence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| AE-DR-001 | Which Annex E profile is the first Z-009 runtime target? AE-SRC-001 has E-1:E-5; BRD has immediate and future scopes. | E-1 only; E-1:E-3; all five | Compliance, privacy, runtime, DB | E-1 only. It is summary-level and Z-backed; E-2:E-5 require distinct personal-data contracts. | Product and Fiscal Design Authority; BIR/accreditation advisor | `OPEN_BLOCKING`, `BLOCKS_Z009` | Freeze `pos-server-bir-annex-e1-rmo24-2023:v1` or stop. |
| AE-DR-002 | What external file format is authoritative? Official source is `.xlsx`; API docs mention Print/PDF/JSON; no submission spec. | XLSX; PDF; JSON; CSV; multiple | Compliance, dependency, determinism | XLSX faithful to E-1 as primary artifact; other formats explicitly noncompliant companions unless approved. | BIR/accreditation advisor; Product | `OPEN_BLOCKING` | No Annex E bytes may be generated before approval. |
| AE-DR-003 | What is output grain and grouping? E-1 has multiple rows but no period instruction. | One file per Z; daily rows grouped monthly; caller-selected closed range | Compliance, API, filename, size | One row per Z; recommend one file per Z for v1 to preserve exact close identity. | BIR/accounting; Product | `OPEN_BLOCKING` | Freeze file/report identity and multi-series rule. |
| AE-DR-004 | What filename is prescribed? No source found. | BIR-prescribed; recommended deterministic; operator-supplied | Compliance, operations | Approve deterministic `ANNEX-E1_<FISCAL>_<MIN>_<DATE>_<VERSION>.xlsx`; never operator text. | BIR/accreditation; Operations | `OPEN_BLOCKING` | Freeze sanitization and collision behavior. |
| AE-DR-005 | Are all 10 header facts mandatory and how are software release/UserID values sourced? Template labels lack nullability. | All required; selected optional; controlled N/A | Compliance, schema, privacy | Require all for E-1; use immutable report profile and privacy-safe service actor. | BIR/accreditation; Product; Security | `OPEN_BLOCKING` | Approve historical header/profile extension. |
| AE-DR-006 | How do 29 official field numbers map to 32 physical columns and equations `23=8-19`, `24=6-16-8`? | Physical-column formulas; official-number formulas; corrected interpretation | Financial correctness | Obtain written BIR/accounting mapping. Do not infer conventional equations. | BIR/accreditation; Accounting | `OPEN_BLOCKING` | D19/D26/D27 integrity equations cannot ship. |
| AE-DR-007 | What is `Sales Issued w/ Manual SI/OR` and its authoritative source? | External ledger import; POS manual issuance mode; always zero when prohibited | Compliance, schema, operations | Define a governed first-class manual-sales source; do not default zero. | BIR/accounting; Product | `OPEN_BLOCKING` | Add source or approve controlled zero/non-applicability. |
| AE-DR-008 | What is Sales Overrun/Overflow? No repository source definition. | Cash/tender variance; sequence overrun; revenue overflow; N/A | Accounting, schema | Require BIR/accounting definition and source. | BIR/accounting | `OPEN_BLOCKING` | D28 blocks E-1. |
| AE-DR-009 | What is Total Income and its equation? Workbook has label only. | Net sales; net plus overrun; other | Financial correctness | Require explicit equation and sign rules. | BIR/accounting | `OPEN_BLOCKING` | D29 blocks E-1. |
| AE-DR-010 | What may Remarks contain? Free text risks privacy and nondeterminism. | Blank; controlled codes; free text | Privacy, audit, schema | Controlled codes only (`NONE`, `NO_ACTIVITY`, approved exception codes). | BIR/accreditation; Security | `OPEN_BLOCKING` | Add controlled code family if nonblank values required. |
| AE-DR-011 | How are NAAC and Solo Parent E-1 columns represented before workflows exist? | Explicit zero; N/A; block; implement classifications | Compliance, schema | Approve controlled zero only for a scope where those privileges are formally unavailable; otherwise extend Z classification. | Product; BIR/accounting | `OPEN_BLOCKING` | D14/D15 cannot be guessed from `other_statutory`. |
| AE-DR-012 | How do Diplomat and other VAT privileges map to E-1 deductions/VAT adjustments? BRD keeps treatment open. | VAT Others; Discount Others; separate future extension | Tax/accounting | Keep Diplomat as VAT treatment and require explicit D22/D24 mapping. | BIR/accounting; Product | `OPEN_BLOCKING` | Nonzero unsupported privilege blocks generation. |
| AE-DR-013 | Are E-2/E-3 applicable to ExitPass and what lawful source retains names/IDs/TIN? | POS stores; external privacy-governed projection; not applicable | Legal/privacy, architecture, DB | Separate task and data-protection decision; do not expand POS statutory snapshot in E-1 task. | Legal/Privacy; BIR; Product | `OPEN_BLOCKING` for E-2/E-3, not E-1 | E-2/E-3 remain unauthorized. |
| AE-DR-014 | Are E-4/E-5 applicable and how are highly sensitive athlete/parent/child fields governed? | Future support; external system; N/A | Privacy, product, DB | Separate future compliance tasks after entitlement support and privacy approval. | Legal/Privacy; BIR; Product | `OPEN_BLOCKING` for E-4/E-5, not E-1 | E-4/E-5 remain unauthorized. |
| AE-DR-015 | How do return, refund, cancellation, adjustment, and cross-period events map? | Separate columns; fold into Returns/Voids/Others; supplemental report | Accounting, close immutability | Preserve Z fail-closed posture until each sign and period rule is approved. | BIR/accounting; Product | `OPEN_BLOCKING` | Any nonzero unsupported category blocks E-1. |
| AE-DR-016 | Are signing, encryption, compression, submission, archive, and retention mandated? No local source defines them. | None; signed; encrypted; portal; audit-only | Security, compliance, operations | Keep generator local/read-only; decide delivery and retention separately from field contract. | BIR/accreditation; Security; Operations | `OPEN_BLOCKING` if external delivery is in Z-009 | Freeze API/download versus submission scope. |
| AE-DR-017 | May prior files be regenerated or corrected, and how is lineage represented? | Deterministic replay; replacement; supplemental correction | Audit, schema, operations | Exact replay for same operation; new immutable lineage for approved correction; never overwrite. | BIR/accreditation; Product; Audit | `OPEN_BLOCKING` | Freeze operation semantics and correction status. |
| AE-DR-018 | Must export history and bytes/hash metadata be persisted? | Derive every time; metadata only; immutable package record | Audit, schema, retention | Persist privacy-safe immutable metadata/hash and lineage, not duplicate fiscal totals; bytes may remain controlled storage. | Audit; Security; Product | `OPEN_BLOCKING` | Determines schema slice before runtime. |
| AE-DR-019 | What decimal places, rounding, date format, separators, and non-PHP behavior apply? Workbook cells use General. | BIR display style; invariant machine style; currency-aware | Financial, layout | Approve two decimals, integer-minor-unit conversion, `.` decimal, no thousands separator, PHP-only v1 unless separately approved. | BIR/accounting | `OPEN_BLOCKING` | Freeze serializer and test vectors. |
| AE-DR-020 | Is E-1 per Site POS Server or terminal, and are APM/WebPay/APT/Cashier combined? Header has one POS Terminal No. | Combined Site server; one per terminal; grouped rows | Architecture, reconciliation | Recommend Site POS Server/fiscal identity/currency/Z scope, with an approved server fiscal terminal identity rather than channel list. | BIR/accreditation; Product | `OPEN_BLOCKING` | Freeze H08 and channel aggregation. |
| AE-DR-021 | How is a configured no-activity Z represented? | Zero row; no file; blank ranges/N/A | Compliance, counters | Recommend zero row with equal GTA, advanced Z, unchanged reset, and controlled no-activity remarks. | BIR/accounting | `OPEN_BLOCKING` | Freeze D02/D03/D32 zero/null behavior. |
| AE-DR-022 | How are multiple fiscal series and sequence gaps shown? E-1 has one range and Remarks only. | Separate rows; separate sheets; controlled remarks; reject | Compliance, layout | One approved fiscal series per E-1 row; unexplained gaps block; classified gaps need approved controlled remarks or companion evidence. | BIR/accreditation; Product | `OPEN_BLOCKING` | Freeze range flattening and gap disclosure. |
| AE-DR-023 | Are reprints and training transactions represented? E-1 has no fields. | Exclude; remarks; separate report | Compliance, totals | Reprints excluded from sales; prohibit training mode in E-1 until a governed source/rule exists. | BIR/accreditation; Product | `OPEN_BLOCKING` for training support | Freeze explicit exclusion. |
| AE-DR-024 | Are E-1 fields required at physical cell widths or only semantic order? Source widths exist but no max lengths. | Exact template geometry; semantic workbook; adaptive widths | Accreditation, rendering | Use approved template geometry and fail on overflow rather than truncate; approve safe wrap behavior. | BIR/accreditation | `OPEN_BLOCKING` | Freeze visual regression baseline. |

## 3. Exact approval wording

Approvers may use the following bounded wording only after reviewing the affected source and consequence:

```text
ExitPass approves AE-DR-<ID>, option <OPTION>, for the
pos-server-bir-annex-e1-rmo24-2023:v1 scope. This approval is an
ExitPass product/fiscal decision and does not represent BIR acceptance unless
the approval reference explicitly includes BIR authority. Approved by <NAME>,
role <ROLE>, at <TIMESTAMP>, reference <REFERENCE>.
```

For accounting formulas, the approval must include the exact equation, input field IDs, sign convention, and zero/null behavior. For format decisions, it must include file extension, grouping, encoding/package rules, and filename.

## 4. Cross-reference requirements

- AE-DR-001 through 006 affect the source assessment, dictionary, mapping, file layout, and handoff.
- AE-DR-007 through 012 affect D06, D14-D16, D19, D22-D29, and D32.
- AE-DR-013/014 govern E-2 through E-5 and must never be satisfied by adding prohibited identity to E-1.
- AE-DR-015 affects all exceptional-transaction scenarios.
- AE-DR-016 through 018 govern lifecycle, retention, and durable export state.
- AE-DR-019 through 024 govern serialization, scope, empty periods, ranges, exclusions, and layout.

## 5. Runtime gate

All entries marked `BLOCKS_Z009` must become approved, or the approved Z-009 scope must explicitly exclude the affected field/profile while remaining faithful to every mandatory E-1 column. Recommendations alone do not authorize implementation.
