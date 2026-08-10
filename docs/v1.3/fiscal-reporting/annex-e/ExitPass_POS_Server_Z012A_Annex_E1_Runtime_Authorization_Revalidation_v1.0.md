# ExitPass POS Server Z-012A Annex E-1 Runtime Authorization Revalidation v1.0

## 1. Control record

| Item | Value |
|---|---|
| Task | Z-012A |
| Review date | 2026-08-10 PHT |
| Repository baseline | `227cdc708d1a685cd56986f084d0cc1aad3e81dd` |
| Approved profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Generator decision | `BLOCKED_PENDING_ACCOUNTING_CONFIRMATION` |
| Controlled UAT | Not authorized |
| External delivery | Not authorized |
| Production | Not authorized |

This review revalidates the frozen Annex E-1 contract against merged Z-010 BIR Sales Summary and Z-011A Electronic Journal behavior. It does not authorize or implement an Annex E runtime.

> **Historical-status notice:** the original `BLOCKED_PENDING_ACCOUNTING_CONFIRMATION` conclusion below records Z-012A at the time it was issued. It was superseded on 2026-08-10 by `Z-012B-ACCOUNTING-APPROVAL-001` and the [Z-012A2 revalidation](ExitPass_POS_Server_Z012A2_Annex_E1_Accounting_Approval_and_Runtime_Authorization_Revalidation_v1.0.md). The approved proposal itself remains unchanged.

## 2. Source evidence

The sources were inspected in the approved authority order. The local source package was sufficient for this revalidation; no web research was used.

| Source | Evidence |
|---|---|
| Official workbook | `D:\Docs\ExitPass\POS\RMO 24-2023 Annex E-1 to E-5.xlsx`; SHA-256 `7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197` |
| RMO source | `D:\Docs\ExitPass\POS\RMO No. 24-2023.pdf`; SHA-256 `c9b7f0df72f96f4c2f4c4e16a6d0d1aab0f9ef745c5f31cbff0d5b72fe5ba3eb` |
| Examiner minutes | `D:\Docs\ExitPass\POS\RMO 24-2023 Annex G_Minutes of Meeting_v2_RAF (1).docx`; SHA-256 `6640806dacd11bf63efd4891ca4f3895c14c76cc226b255ac7546340bcd7e6c2` |
| Accreditation requirements | `D:\Docs\ExitPass\POS\BIR POS Accreditation Requirements.docx`; SHA-256 `c52cb74ff0ac7eb96801a8d47145625fe2f408bcea1f5fd6e3520ed66a530094` |
| Project gap analysis | `D:\Docs\ExitPass\POS\FINAL GAP ANALYSIS - Hikvision AutoPay Machine BIR Accreditation.docx`; SHA-256 `21ad8a9f9aa1330f723f18930e70627f40e25be8621c957e1c1eb8b3a1de92ac` |
| Z-010 | `pos.bir_sales_summary_reports`, committed-Z binding, API contract, runtime DTO, deterministic output |
| Z-011A | Canonical fiscal event stream and Electronic Journal traceability for committed Z and BIR Sales Summary events |

The official E-1 worksheet has dimension `A1:AF16`, 32 merged ranges, 10 header positions, and 32 physical detail columns. The displayed numbered detail fields total 29 because Manual SI/OR is unnumbered and NAAC/Solo Parent expand the displayed field 13 group. This distinction does not reduce the physical output count.

The workbook contains text labels for `22 = 17+18+19+20+21`, `23 = 8-19`, and `24 = 6-16-8`. It does not contain executable formulas or definitions that resolve the operands' business meaning.

## 3. Authority boundary after Z-010 and Z-011A

The committed Z Reading and immutable BIR Sales Summary are the Annex E-1 financial authority. Z-010 now persists the period, fiscal range, GTA, sales, tax, discount, exceptional-transaction, reset-counter, Z-counter, and selected registration-header facts needed by many E-1 positions.

The Electronic Journal provides transition traceability and integrity evidence. It must not replace the committed Z Reading or BIR Sales Summary as the financial source and must not be used to invent missing Annex E facts.

Neither runtime resolves:

* the official meaning and authoritative source of Manual SI/OR;
* Sales Overrun/Overflow;
* Total Income; or
* the official operand mapping for the worksheet equations.

## 4. Exact 42-position reconciliation

Readiness values use the frozen Z-009A classifications. `BLOCKED` means that an authoritative value cannot be emitted without the cited approval or source extension.

### 4.1 Header positions

| Position | Workbook cell | Official label | Canonical source after Z-010 | Null, zero, and unsupported posture | External dependency | Readiness |
|---|---|---|---|---|---|---|
| H01 | A1 | Name of Taxpayer | Fiscal identity/configuration, but no immutable Annex header snapshot | Missing mandatory value blocks; no current-value replay | None for meaning; snapshot persistence required | `REQUIRES_SCHEMA_CHANGE` |
| H02 | A2 | Registered Address | Fiscal identity/configuration, but no immutable Annex header snapshot | Missing mandatory value blocks | None for meaning; snapshot persistence required | `REQUIRES_SCHEMA_CHANGE` |
| H03 | A3 | TIN | Fiscal identity/configuration, but no immutable Annex header snapshot | Missing mandatory value blocks | None for meaning; snapshot persistence required | `REQUIRES_SCHEMA_CHANGE` |
| H04 | A5 | Software Name and Version | No immutable historical Annex source | Missing mandatory value blocks | None for meaning; snapshot persistence required | `REQUIRES_SCHEMA_CHANGE` |
| H05 | A5 | Release Number and Date | No immutable historical Annex source | Missing mandatory value blocks | None for meaning; snapshot persistence required | `REQUIRES_SCHEMA_CHANGE` |
| H06 | A6 | Serial Number | `pos.bir_sales_summary_reports.pos_serial_number` | Unknown blocks; recorded value emitted verbatim | None | `READY_EXISTING_FIELD` |
| H07 | A7 | Machine Identification Number | `pos.bir_sales_summary_reports.machine_identification_number` | Unknown blocks; recorded value emitted verbatim | None | `READY_EXISTING_FIELD` |
| H08 | A8 | POS Terminal Number | No approved immutable source | Unknown blocks; no invented terminal identifier | AE-DR-020A | `REQUIRES_EXTERNAL_DECISION` |
| H09 | A9 | Date and Time Generated | Future immutable Annex export evidence `generated_at` | Assigned only on durable generation | None | `REQUIRES_NEW_REPORT_PROJECTION` |
| H10 | A10 | User ID | Future server-derived Annex actor/service snapshot | Missing authoritative actor blocks | None | `REQUIRES_NEW_REPORT_PROJECTION` |

### 4.2 Detail positions

| Position | Column | Official label | Canonical source or rule after Z-010 | Null, zero, no-activity, and unsupported posture | External dependency | Readiness |
|---|---|---|---|---|---|---|
| D01 | A | Date | BIR Summary `business_date` | Mandatory | AE-DR-019 display profile | `READY_EXISTING_FIELD` |
| D02 | B | Beginning SI/OR No. | BIR Summary `beginning_si_number` | Blank only for a committed no-activity Z | AE-DR-019 display profile | `READY_EXISTING_FIELD` |
| D03 | C | Ending SI/OR No. | BIR Summary `ending_si_number` | Blank only for a committed no-activity Z | AE-DR-019 display profile | `READY_EXISTING_FIELD` |
| D04 | D | Grand Total Sales Ending Balance | BIR Summary `present_gta_minor` | Known zero is `0.00`; unknown blocks | None | `READY_EXISTING_FIELD` |
| D05 | E | Grand Total Sales Beginning Balance | BIR Summary `previous_gta_minor` | Known zero is `0.00`; unknown blocks | None | `READY_EXISTING_FIELD` |
| D06 | F | Manual SI/OR | No authoritative source or approved definition | Unknown cannot become zero, including no-activity rows | AE-DR-007 | `REQUIRES_EXTERNAL_DECISION` |
| D07 | G | Gross Sales | BIR Summary `gross_sales_minor` | Recorded zero emitted | None | `READY_EXISTING_FIELD` |
| D08 | H | VATable Sales | BIR Summary `vatable_sales_minor` | Recorded zero emitted | None | `READY_EXISTING_FIELD` |
| D09 | I | VAT Amount | BIR Summary `vat_amount_minor` | Recorded zero emitted | None | `READY_EXISTING_FIELD` |
| D10 | J | VAT-Exempt Sales | BIR Summary `vat_exempt_sales_minor` | Recorded zero emitted | None | `READY_EXISTING_FIELD` |
| D11 | K | Zero-Rated Sales | BIR Summary `zero_rated_sales_minor` | Recorded zero emitted | None | `READY_EXISTING_FIELD` |
| D12 | L | Senior Citizen Discount | BIR Summary `senior_citizen_discount_minor` | Recorded zero emitted | None | `READY_EXISTING_FIELD` |
| D13 | M | PWD Discount | BIR Summary `pwd_discount_minor` | Recorded zero emitted | None | `READY_EXISTING_FIELD` |
| D14 | N | NAAC Discount | No separate immutable classification | Unknown blocks; explicit zero only after accepted mapping/blank rule | AE-DR-011, AE-DR-011A | `REQUIRES_SCHEMA_CHANGE` |
| D15 | O | Solo Parent Discount | No separate immutable classification | Unknown blocks; explicit zero only after accepted mapping/blank rule | AE-DR-011, AE-DR-011A | `REQUIRES_SCHEMA_CHANGE` |
| D16 | P | Other Discount | BIR Summary has other statutory, coupon, and promotional components, but official composition is unapproved | No silent aggregation | AE-DR-006 and AE-DR-012 | `REQUIRES_EXTERNAL_DECISION` |
| D17 | Q | Returns | BIR Summary `return_amount_minor` | Current unsupported nonzero return posture blocks Z close; recorded zero is authoritative | None for current supported zero path | `READY_EXISTING_FIELD` |
| D18 | R | Voids | BIR Summary `void_amount_minor` | Recorded zero or governed same-period void emitted | None | `READY_EXISTING_FIELD` |
| D19 | S | Total Deductions | Text rule `22 = 17+18+19+20+21`; official operands are not mapped unambiguously | Unknown blocks; do not derive by convenience | AE-DR-006 | `REQUIRES_EXTERNAL_DECISION` |
| D20 | T | Senior Citizen VAT Adjustment | Immutable Z discount child `senior_citizen_vat_adjustment` | Recorded zero emitted | None | `READY_EXISTING_DERIVATION` |
| D21 | U | PWD VAT Adjustment | Immutable Z discount child `pwd_vat_adjustment` | Recorded zero emitted | None | `READY_EXISTING_DERIVATION` |
| D22 | V | Other VAT Adjustment | No approved mapping for Diplomat or other VAT privileges | Nonzero unsupported privilege blocks; no remapping | AE-DR-012 | `REQUIRES_EXTERNAL_DECISION` |
| D23 | W | VAT on Returns | No separately governed immutable source | Nonzero blocks; unknown is not zero | AE-DR-012 and source-model approval | `NOT_AVAILABLE` |
| D24 | X | Other VAT Adjustment Component | No approved immutable classification | Nonzero or unknown blocks | AE-DR-012 | `NOT_AVAILABLE` |
| D25 | Y | Total VAT Adjustment | Text rule `22 = 17+18+19+20+21`; component semantics remain unresolved | Unknown blocks | AE-DR-012 | `REQUIRES_EXTERNAL_DECISION` |
| D26 | Z | VAT Payable | Text rule `23 = 8-19`; official operands are not mapped unambiguously | Unknown blocks; no convenience formula | AE-DR-006 | `REQUIRES_EXTERNAL_DECISION` |
| D27 | AA | Net Sales | BIR Summary `net_sales_minor`; workbook text also states `24 = 6-16-8` with unresolved operands | Stored value is authoritative, but workbook-rule validation cannot be frozen | AE-DR-006 | `REQUIRES_EXTERNAL_DECISION` |
| D28 | AB | Sales Overrun/Overflow | No authoritative source, definition, or approved equation | Unknown cannot become zero, including no-activity rows | AE-DR-008 | `REQUIRES_EXTERNAL_DECISION` |
| D29 | AC | Total Income | No authoritative definition or approved equation | Unknown cannot be replaced by gross, net, GTA, or tender total | AE-DR-009 | `REQUIRES_EXTERNAL_DECISION` |
| D30 | AD | Reset Counter | BIR Summary `reset_counter_value` | Recorded integer zero is explicit | None | `READY_EXISTING_FIELD` |
| D31 | AE | Z Counter | BIR Summary `z_counter_value` | Recorded integer; no-activity Z still advances | None | `READY_EXISTING_FIELD` |
| D32 | AF | Remarks | Approved governed codes, but accepted official values and blank behavior are unconfirmed | Emit only accepted governed code; otherwise block | AE-DR-010 | `REQUIRES_CONTROLLED_CODE` |

Count proof: 10 header positions plus 32 detail positions equals 42 physical output positions. Every position has an official workbook location, canonical-source assessment, unsupported behavior, reconciliation dependency, and external-decision disposition.

## 5. Four initial-generator blockers

### AE-DR-006: numbered-field mapping and equations

* Exact confirmation question: What official detail-field numbering and operand mapping governs all 32 physical columns, specifically `22 = 17+18+19+20+21`, `23 = 8-19`, and `24 = 6-16-8`?
* Why repository evidence is insufficient: the workbook supplies labels and equation text but no executable formulas or operand dictionary; Z-010 persists totals but does not define these regulatory equations.
* Proposed ExitPass interpretation: map physical columns in workbook order, use committed BIR Summary facts as inputs, and require application-calculated reconciliation values after Accounting approves each operand.
* Alternatives: displayed-number mapping independent of physical columns; formulas referring to another source schedule; manual accounting completion.
* Affected positions: D16, D19, D26, D27 and downstream reconciliation.
* Wrong-interpretation consequence: materially incorrect deductions, VAT payable, or net sales in an official artifact.
* Approving authority: ExitPass Accounting with documentary BIR/examiner confirmation where needed.
* Fail-closed subset: no complete physical E-1 row can be certified while mandatory equations remain undefined.

### AE-DR-007: Manual SI/OR

* Exact confirmation question: What qualifies as Manual SI/OR, what period basis applies, and what immutable source value must populate D06?
* Why repository evidence is insufficient: no current runtime records a governed Manual SI/OR Annex classification or amount/range; the workbook supplies only the label.
* Proposed ExitPass interpretation: introduce a separately controlled immutable manual-document fact only after its document and period semantics are approved.
* Alternatives: zero for fully electronic operation; manual document count; manual sales amount; manual number range.
* Affected positions: D06 and potentially D02/D03, D07/D27, counts, and remarks.
* Wrong-interpretation consequence: omission or misstatement of manually issued fiscal activity.
* Approving authority: ExitPass Accounting, with BIR/examiner confirmation of the field meaning.
* Fail-closed subset: generation cannot treat absence of a source as authoritative zero, even for a no-activity Z.

### AE-DR-008: Sales Overrun/Overflow

* Exact confirmation question: What event or amount constitutes Sales Overrun/Overflow and how is D28 calculated for a Z period?
* Why repository evidence is insufficient: examiner minutes mention the label but define neither trigger nor equation; no canonical runtime fact carries this classification.
* Proposed ExitPass interpretation: add a governed exception classification and immutable amount only after Accounting approves the fiscal meaning; emit zero only when the approved classifier records absence.
* Alternatives: sequence overflow amount; sales beyond a configured threshold; late-posting amount; zero for systems without an overrun facility.
* Affected positions: D28, D29, D32, reconciliation, and exception validation.
* Wrong-interpretation consequence: hidden fiscal exceptions or a false zero in an official artifact.
* Approving authority: ExitPass Accounting, with examiner confirmation where the terminology is regulatory.
* Fail-closed subset: current runtime has no authoritative known-zero signal, so a complete row cannot be emitted safely.

### AE-DR-009: Total Income

* Exact confirmation question: What is the official definition and calculation of Total Income in D29?
* Why repository evidence is insufficient: the workbook provides no formula; Z-010 records gross sales, net sales, tenders, and GTA contribution but none is identified as Total Income.
* Proposed ExitPass interpretation: use only an explicitly approved equation over named immutable BIR Summary inputs and persist the resulting projection fact.
* Alternatives: gross sales; net sales; net plus VAT; total tenders; period GTA contribution; another BIR-defined basis.
* Affected positions: D29 and workbook total/reconciliation checks.
* Wrong-interpretation consequence: a material income figure could be misstated despite internally consistent Z totals.
* Approving authority: ExitPass Accounting, with BIR/examiner confirmation if the source meaning remains ambiguous.
* Fail-closed subset: no safe substitute exists; the generator remains blocked.

## 6. Nonzero privilege boundary: AE-DR-012

The ordinary E-1 path can be designed after AE-DR-006 through AE-DR-009 are approved while failing closed whenever a committed source contains nonzero Diplomat or another unsupported VAT-privilege amount. Such values must not be merged into Other Discount, Other VAT Adjustment, VAT on Returns, or another available column.

AE-DR-012 is `REQUIRES_ACCOUNTING_APPROVAL` and `BLOCKING_NONZERO_PRIVILEGE_PATH`. It does not independently block an ordinary zero-privilege implementation, but it blocks those scenarios, Controlled UAT coverage of those scenarios, and Production acceptance until confirmed.

## 7. Active external-confirmation reconciliation

| Decision | Authoritative resolution classification | Gate labels | Revalidation result |
|---|---|---|---|
| AE-DR-002 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` | Official electronic artifact and companion formats remain unconfirmed; deterministic XLSX remains the internal profile recommendation. |
| AE-DR-004 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` | Internal filename is approved but is not represented as prescribed. |
| AE-DR-006 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_INITIAL_GENERATOR`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | Not resolved by Z-010 or Z-011A. |
| AE-DR-007 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_INITIAL_GENERATOR`, `BLOCKING_SCHEMA_IMPLEMENTATION`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | No governed Manual SI/OR source exists. |
| AE-DR-008 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_INITIAL_GENERATOR`, `BLOCKING_SCHEMA_IMPLEMENTATION`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | No governed overrun/overflow source exists. |
| AE-DR-009 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_INITIAL_GENERATOR`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | Total Income remains undefined. |
| AE-DR-010 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` | Internal governed remarks remain bounded; official accepted values and blank posture are unconfirmed. |
| AE-DR-011A | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_NONZERO_PRIVILEGE_PATH`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | Explicit-zero recommendation remains project-approved but externally unconfirmed. |
| AE-DR-012 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_NONZERO_PRIVILEGE_PATH`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | Ordinary path may fail closed on nonzero unsupported privilege facts. |
| AE-DR-016 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | Local download design remains separate; signing, encryption, compression, and submission are not authorized. |
| AE-DR-016B | `REQUIRES_LEGAL_COMPLIANCE_APPROVAL` | `BLOCKING_PRODUCTION` | Retention duration, archive, legal hold, and deletion remain unresolved; destructive retention is prohibited. |
| AE-DR-019 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | Internal deterministic display profile is approved but not externally accepted. |
| AE-DR-020A | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_SCHEMA_IMPLEMENTATION`, `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` | H08 meaning and source remain unconfirmed. |
| AE-DR-024 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_EXTERNAL_DELIVERY`, `BLOCKING_PRODUCTION` | Official package is the geometry authority; wrapping acceptance remains unconfirmed. |

Exactly 14 active external confirmations remain unresolved. AE-DR-013 and AE-DR-014 remain deferred for E-2 through E-5 and are not part of this count.

For completeness, the remaining allowed authoritative classifications retain their distinct use: approved project decisions are `RESOLVED_BY_APPROVED_DECISION`, explicit workbook positions such as AE-DR-005 are `RESOLVED_BY_OFFICIAL_SOURCE`, and source-availability changes proven by Z-010 or Z-011A are `RESOLVED_BY_MERGED_RUNTIME`. AE-DR-013 and AE-DR-014 are `DEFERRED_NONBLOCKING`. None of those classifications is used to close one of the 14 external questions.

## 8. Deterministic XLSX feasibility

Literal byte-identical XLSX output is technically achievable without Microsoft Excel, macros, volatile formulas, current locale, current time, or manual editing. The implementation must not rely on default ZIP or Open XML metadata behavior.

The required deterministic package strategy is:

1. bind a versioned renderer to the approved profile and an exact official-template content hash;
2. use fixed ZIP entry order and fixed entry timestamps;
3. use deterministic stored ZIP entries, or a specifically pinned deterministic compression implementation whose byte behavior is tested across supported hosts;
4. normalize or remove volatile core properties, calculation metadata, generated relationship IDs, and application-specific timestamps;
5. serialize XML with fixed namespace, attribute, cell, row, style, and relationship ordering;
6. write application-calculated authoritative values, not volatile spreadsheet calculations;
7. bind output identity to source membership, profile, renderer, template hash, and output options;
8. persist immutable output evidence and durable artifact bytes outside the relational database so replay returns the original bytes;
9. test clean generation, replay, process restart, and host restart for byte equality.

The repository currently has no selected XLSX package dependency. Library selection and package-byte proof belong to the future implementation task. A generic ZIP writer using runtime-dependent compression defaults is not sufficient. No semantic-equivalence exception is approved or needed at the contract stage because the deterministic stored-entry strategy can preserve literal bytes.

Result: deterministic XLSX feasibility is passed as an implementation design constraint, not as runtime proof.

## 9. Authorization decision and staged gates

### 9.1 Generator implementation

`BLOCKED_PENDING_ACCOUNTING_CONFIRMATION`

AE-DR-006 through AE-DR-009 affect mandatory physical row positions and calculations. Z-010 and Z-011A do not resolve them. A renderer that must reject every ordinary and no-activity row is not a bounded generator implementation.

No Z-012B runtime authorization is issued by Z-012A. After Accounting resolves AE-DR-006 through AE-DR-009, a bounded implementation may proceed with a fail-closed nonzero privilege path for AE-DR-012 and with all external-delivery features disabled.

### 9.2 Controlled UAT

Blocked pending the BIR/examiner, Accounting, and applicable privilege confirmations listed above.

### 9.3 External delivery

Blocked pending AE-DR-002, AE-DR-004, AE-DR-016, and AE-DR-024. Local generation/download design approval does not authorize submission.

### 9.4 Production

Blocked pending all implementation-critical confirmations, Controlled UAT authorization, formal compliance acceptance, and AE-DR-016B retention approval.

## 10. Gated future Z-012B boundary

This is a handoff boundary, not an authorization to implement. Once AE-DR-006 through AE-DR-009 are approved, Z-012B should implement:

* a generate endpoint scoped to one Site POS Server, fiscal identity, currency, and calendar month;
* metadata readback and XLSX download endpoints using anti-enumerating exact scope;
* semantic identity over profile, renderer, template hash, monthly source membership, options, and operation key;
* committed-Z and CLOSED-period enforcement for every physical row;
* one workbook identity per governed monthly scope and immutable correction/supersession lineage;
* authoritative values from committed Z snapshots, immutable BIR Sales Summaries, immutable Z children, and approved historical header snapshots;
* deterministic Open XML package construction under section 8;
* reconciliation of every row to its governing Z and BIR Sales Summary, with the Electronic Journal used only as integrity evidence;
* separate generate, read, download, and correct/supersede permissions with exact Site POS Server, fiscal identity, and currency scope;
* exclusion of customer identity, statutory identifiers/evidence, reviewer data, credentials, and diagnostic payloads;
* fail-closed rejection for nonzero unresolved privileges, unsupported classifications, ambiguous ranges, gaps, missing snapshots, and unrepresentable workbook values;
* one PostgreSQL transaction for operation identity, source membership, immutable projection/evidence, output identity, content hash, and correction lineage; workbook bytes remain external;
* focused field, package, and formula tests plus PostgreSQL atomicity, replay/conflict, concurrency, rollback, restart, deterministic-byte, authorization, privacy, and no-fiscal-mutation proof;
* explicit exclusion of Controlled UAT, external delivery, signing, encryption, compression policy, E-2 through E-5, and Production.

## 11. Residual approval questions

Accounting must answer AE-DR-006 through AE-DR-009 before a generator task starts. Accounting must also answer AE-DR-012 before nonzero Diplomat or other unresolved VAT-privilege scenarios are enabled. The BIR/examiner and Legal/Compliance questions in section 7 retain their narrower stage gates and fail-closed posture.

## 12. Z-012A1 follow-up status

Z-012A1 records the 2026-08-10 Accounting statement as `ACCOUNTING_AUTHORITY_AND_APPROVAL_IN_PRINCIPLE_CONFIRMED` and prepares one exact, hash-bound [Accounting Calculation Profile Proposal](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md) plus [Approval Form](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Approval_Form_v1.0.md).

This paragraph preserves the Z-012A1 pre-approval posture. It was superseded by exact Accounting approval record `Z-012B-ACCOUNTING-APPROVAL-001`; current authorization is governed by Z-012A2.

## 13. Z-012A2 superseding decision

The approved proposal filename, version, Annex E profile, decision scope, executable content, and SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` were verified exactly. AE-DR-006 through AE-DR-009 are resolved without modifying the proposal. The current decision is `AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME`.

The missing Manual SI/OR, Sales Overrun/Overflow, known-zero attestation, header snapshot, correction-lineage, artifact, API, authorization, and test capabilities are authorized Z-012B implementation work. Nonzero unresolved privilege facts remain fail closed. Controlled UAT, external delivery, Production, E-2 through E-5, and ARTS POSLog remain unauthorized.
