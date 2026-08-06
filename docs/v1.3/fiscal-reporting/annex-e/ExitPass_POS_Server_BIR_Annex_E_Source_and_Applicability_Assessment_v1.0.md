# ExitPass POS Server BIR Annex E Source and Applicability Assessment v1.0

## 1. Purpose and verdict

This assessment identifies the available Annex E source set and separates source requirements from recommendations. It is documentation only.

**Applicability verdict: `BLOCKED_PENDING_DECISIONS`.** Annex E-1 is the recommended first runtime profile because it is the BIR Sales Summary required by the approved ExitPass BRD. Annex E-2 through E-5 are separate statutory sales books with transaction-level identity fields and cannot be folded into an E-1 implementation.

## 2. Source identifiers

| ID | Source | Authority and use |
| --- | --- | --- |
| AE-SRC-001 | `D:\Docs\ExitPass\POS\RMO 24-2023 Annex E-1 to E-5.xlsx` | Primary local Annex E template. Exact worksheet labels, order, physical columns, and sample formulas. |
| AE-SRC-002 | `D:\Docs\ExitPass\POS\RMO No. 24-2023.pdf` | Primary local regulatory package. No full text is copied into this repository. |
| AE-SRC-003 | `D:\Docs\ExitPass\POS\RMO 24-2023 ANNEX F_12072022_Functional and Technical Evaluation Checklist_RAF.docx.pdf` | Primary local evaluation checklist. |
| AE-SRC-004 | `D:\Docs\ExitPass\POS\RMO 24-2023 Annex G_Minutes of Meeting_v2_RAF (1).docx` | Primary local meeting/checklist evidence. Paragraphs 56, 74-76, 99-102 identify transaction classifications and applicable report samples. |
| AE-SRC-005 | `D:\Docs\ExitPass\POS\FINAL GAP ANALYSIS - Hikvision AutoPay Machine BIR Accreditation.docx` | Project assessment. Paragraphs 90, 99, 104-126, and 307-318 identify E-1 as required for the APM scope and E-2 through E-5 as not applicable to that narrower assessment. |
| AE-SRC-006 | `D:\Docs\ExitPass\POS\BIR POS Accreditation Requirements.docx` | Project/vendor requirement compilation, not independently treated as regulatory authority. |
| AE-SRC-007 | `D:\Docs\ExitPass\POS\Hikvision Developer Checklist for BIR-Compliant Autopay Parking Station.docx` | Project implementation checklist; no exact Annex E field layout. |
| AE-SRC-008 | `D:\Docs\ExitPass\POS\BIR Recommended Formats.pptx` | Project sample deck. Slides 7 and 10 contain taxpayer-detail and audit-trail headings, not an Annex E data contract. |
| AE-SRC-009 | `D:\Docs\ExitPass\POS\RMO 24-2023 Annex D-1_Sample X-Reading.pdf` | Related X Reading sample; not Annex E layout authority. |
| AE-SRC-010 | `D:\Docs\ExitPass\POS\RMO 24-2023 Annex D-2_Sample Z-Reading.pdf` | Related Z Reading sample; not Annex E layout authority. |
| AE-SRC-011 | `D:\Docs\ExitPass\POS\sampleejournal.txt` | Electronic Journal sample; not Annex E layout authority. |
| AE-SRC-012 | `D:\Docs\ExitPass\POS\ARTS POSLog\` | ARTS POSLog 6.0 PDFs and XSDs. Separate POSLog authority only. |
| AE-SRC-013 | `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md`, section 18 | Approved ExitPass requirement for E-1, applicable E-2/E-3, future E-4/E-5, and open Diplomat treatment. |
| AE-SRC-014 | `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md`, section 19 | Approved service and reconciliation posture for E-1 through E-5. |
| AE-SRC-015 | `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md`, section 18 | Provisional API family and E-1 minimum content; final layouts and mandatory formats remain open. |
| AE-SRC-016 | `docs/v1.3/fiscal-reporting/ExitPass_POS_Server_Internal_Fiscal_Reporting_Contract_v1.0.md`, section 7 | Existing decision: Annex E metadata is governed by a committed Z and an approved profile; exact external contract is unresolved. |
| AE-SRC-017 | `docs/v1.3/fiscal-reporting/ExitPass_POS_Server_Z_Reading_Runtime_Implementation_Note_v1.0.md` | Governing immutable Z source, period close, counters, GTA, range, and gap posture. |
| AE-SRC-018 | `docs/v1.3/fiscal-reporting/ExitPass_POS_Server_X_Z_Reading_Presentation_Print_Export_Implementation_Note_v1.0.md` | Deterministic output conventions, explicitly excluding Annex E. |
| AE-SRC-019 | `db/state/tables/pos.x_z_reports.sql`, `pos.fiscal_z_counter_snapshots.sql`, and report child tables | Current immutable source facts available to an Annex E projection. |
| AE-SRC-020 | `db/state/tables/pos.annex_e_reports.sql` and `pos.bir_sales_summary_reports.sql` | Existing immutable metadata and summary posture; no external Annex E dataset or file. |

## 3. Complete local package inventory

The local folder contains the five RMO/Annex artifacts listed above, four project/accreditation documents, one EJ sample, and the ARTS POSLog 6.0 package. The ARTS package contains the base domain model, narrative, volumes 1 through 24, `POSLogV6.0.0.xsd`, and related RTS XSD files. None is used to infer Annex E fields.

No examiner email, correspondence file, or separately versioned Annex E sample was found beyond AE-SRC-001, AE-SRC-004, and the project assessments. No official filename, submission protocol, CSV schema, XML schema, fixed-width specification, signature specification, or encryption specification was found.

## 4. Annex E variants found

| Profile | Worksheet title | Grain shown by template | Sensitive content | Applicability classification |
| --- | --- | --- | --- | --- |
| E-1 | `BIR SALES SUMMARY REPORT` | One summary row per `Date`; 32 physical detail columns plus 10 header facts | Taxpayer registration metadata only | `EXPLICITLY_REQUIRED` by AE-SRC-013 REP-003; recommended Z-009 profile |
| E-2 | `Senior Citizen Sales Book/Report` | One row per Senior Citizen Sales Invoice | Name, OSCA/SC ID, TIN | `EXPLICITLY_REQUIRED` for applicable transactions by AE-SRC-013 REP-004; blocked by AE-DR-013 |
| E-3 | `Persons with Disability Sales Book/Report` | One row per PWD Sales Invoice | Name, PWD ID, TIN | `EXPLICITLY_REQUIRED` for applicable transactions by AE-SRC-013 REP-005; blocked by AE-DR-013 |
| E-4 | `National Athletes and Coaches Sales Book/Report` | One row per NAAC Sales Invoice | Name and PNSTM ID | `EXISTING_EXITPASS_DECISION` as future-supported structure by AE-SRC-013 REP-006; blocked by AE-DR-014 |
| E-5 | `Solo Parent Sales Book/Report` | One row per Solo Parent Sales Invoice | Parent and child names, SPIC, birth date, age | `EXISTING_EXITPASS_DECISION` as future-supported structure by AE-SRC-013 REP-007; blocked by AE-DR-014 |

The workbook carries no explicit revision number or issuance date inside the sheet content. The profile identity available from the source is therefore `RMO 24-2023 Annex E-1` through `E-5`; a repository contract version must not claim a more specific regulatory revision without authority.

## 5. Format assessment

AE-SRC-001 is an `.xlsx` workbook with worksheets, page print areas, merged headings, and spreadsheet column widths. E-1 uses columns `A:AF`; E-2 uses `A:K`; E-3 uses `A:K`; E-4 uses `A:G`; and E-5 uses `A:K`. This proves a spreadsheet/printed template exists. It does not prove that an `.xlsx` file is the mandatory electronic submission artifact.

`CSV`, fixed-width text, XML, PDF, JSON, digital signing, encryption, and portal submission are `UNRESOLVED`. AE-SRC-015 calls Print, PDF, and JSON supported BIR Sales Summary modes but expressly leaves mandatory formats and final layouts open. Z-008 CSV/JSON contracts are internal deterministic exports and are not Annex E authority.

## 6. Architecture applicability

| Question | Assessment | Classification | Decision reference |
| --- | --- | --- | --- |
| Per Site | One Site owns one Site POS Server in the approved architecture, but the workbook does not label Site. | `EXISTING_EXITPASS_DECISION` for architecture; output treatment `UNRESOLVED` | AE-DR-020 |
| Per Site POS Server | Current report scope and Z source are Site POS Server scoped. | `EXISTING_EXITPASS_DECISION` | AE-SRC-016, AE-SRC-017 |
| Per terminal/channel | Workbook requires `POS Terminal No.`; channels are child terminals, not independent fiscal authorities. Combined versus separate output is unresolved. | `UNRESOLVED` | AE-DR-020 |
| Per fiscal identity | Required to select taxpayer/TIN/MIN/PTU facts and isolate Z state. | `EXISTING_EXITPASS_DECISION` | AE-SRC-016, AE-SRC-017 |
| Per machine identification number | Header explicitly contains MIN. | `EXPLICITLY_REQUIRED` | AE-SRC-001 E-1 header |
| Per permit | PTU is not an E-1 header label in the workbook, although ExitPass retains it. | `NOT_APPLICABLE` to exact E-1 physical columns; retention remains internal | AE-SRC-001, AE-SRC-020 |
| Per fiscal period / Z close | E-1 rows are dated and need Z/reset/GTA facts. ExitPass requires a governing committed Z. | `EXISTING_EXITPASS_DECISION` | AE-SRC-016 |
| Calendar day | Workbook labels the row `Date`; business-day versus calendar-day interpretation is unresolved. | `UNRESOLVED` | AE-DR-003 |
| Monthly or periodic file | Workbook offers multiple detail rows but states no file grouping interval. | `UNRESOLVED` | AE-DR-003 |
| Transaction versus summary | E-1 is summary-level; E-2 through E-5 are transaction-level. | `EXPLICITLY_REQUIRED` | AE-SRC-001 |
| Channels combined/separate | Not stated by workbook. | `UNRESOLVED` | AE-DR-020 |
| Tender columns/rows | E-1 has no tender field. | `NOT_APPLICABLE` | AE-SRC-001 E-1 `A:AF` |
| Statutory discount separation | E-1 separates SC, PWD, NAAC, Solo Parent, and Others. | `EXPLICITLY_REQUIRED` | AE-SRC-001 E-1 `L:P` |
| VAT removal separate | E-1 has a separate `Adjustment on VAT` group. | `EXPLICITLY_REQUIRED` | AE-SRC-001 E-1 `T:Y` |
| Voids and returns separate | E-1 has separate Returns and Voids. No Cancellation or Refund column is labeled. | `EXPLICITLY_REQUIRED` for returns/voids; others `UNRESOLVED` | AE-DR-015 |
| Reprints | No E-1 field. | `NOT_APPLICABLE` to totals; confirm no separate disclosure | AE-DR-023 |
| Training mode | No E-1 field and no governed runtime source. | `UNRESOLVED` | AE-DR-023 |
| Beginning/ending SI | Explicit columns. | `EXPLICITLY_REQUIRED` | AE-SRC-001 E-1 `B:C` |
| Sequence gaps | No dedicated column; `Remarks` may not be assumed to carry them. | `UNRESOLVED` | AE-DR-022 |
| Z and reset counters | Explicit columns. | `EXPLICITLY_REQUIRED` | AE-SRC-001 E-1 `AE:AF` |
| GTA | Beginning and ending balances are explicit. | `EXPLICITLY_REQUIRED` | AE-SRC-001 E-1 `D:E` |
| Generate only after Z | Existing POS contract requires governing Z metadata. | `EXISTING_EXITPASS_DECISION` | AE-SRC-016 |
| Regeneration/correction | Not stated by source. | `UNRESOLVED` | AE-DR-017 |
| Export history persistence | Existing metadata can identify one profile per Z but no file history exists. | `UNRESOLVED` | AE-DR-018 |

## 7. Existing POS Server support and gaps

The immutable Z snapshot already records business date, exact period, Site POS Server, fiscal identity, currency, document count, SI range, GTA values, reset and Z counters, gross/net/VAT categories, discount totals, Senior/PWD/other statutory totals, VAT exemption/removal, void, refund, return, adjustment, and committed time. It also has immutable tender, discount, fiscal-range, gap, and counter/GTA children.

The existing `pos.bir_sales_summary_reports` posture adds a required header-profile reference, POS serial number, MIN, accreditation and PTU facts, but no runtime populates it. `pos.annex_e_reports` is privacy-safe metadata only and intentionally stores no dataset or file.

Current authoritative gaps for E-1 are:

- immutable report-time taxpayer name/address/TIN and software release facts;
- one governed POS terminal number for a potentially multi-channel Site POS Server;
- manual SI/OR sales;
- separate NAAC, Solo Parent, and Other statutory totals;
- exact E-1 VAT-adjustment category mapping;
- sales overrun/overflow;
- total income;
- controlled remarks;
- exact official formula interpretation and output formatting;
- durable external output identity, history, and regeneration lineage if approved.

E-2 through E-5 additionally require personal facts that the applied statutory fiscal facts contract deliberately prohibits POS persistence: beneficiary names, statutory IDs, TINs, and evidence. No runtime may query live external identity data after close merely to fill those sales books.

## 8. Conflicts

1. AE-SRC-013 requires applicable E-2/E-3 support, while AE-SRC-005 says E-2 through E-5 are not applicable to the narrower Hikvision APM scope. ExitPass is platform-wide and supports Senior/PWD fiscal facts, so the APM conclusion cannot be generalized.
2. AE-SRC-001 asks for transaction-level statutory identity in E-2 through E-5, while the approved POS statutory contract excludes those identities from POS persistence. This requires legal/privacy and architecture approval, not a runtime workaround.
3. AE-SRC-015 recommends Print/PDF/JSON semantics, while AE-SRC-001 supplies an Excel template and does not identify a submission format. Neither source authorizes CSV as the Annex E artifact.
4. The E-1 field-number row has 29 numbered positions but 32 physical columns. NAAC and Solo Parent expand the discount group, and formula labels reference field numbers rather than unambiguously naming physical columns. AE-DR-006 must resolve the normative interpretation.

## 9. Explicit exclusions

- No Annex E runtime, API, schema, controlled code, export job, submission, signing, encryption, retention worker, EJ, or POSLog behavior is authorized.
- No raw statutory ID, evidence, customer name, ticket number, plate number, or payment credential is added to E-1.
- Z-008 JSON/CSV is not relabeled as Annex E.
- Live transactional recomputation after Z close is prohibited unless a future approved contract changes the authoritative source boundary.
- Synthetic examples in this package are not BIR-approved samples.

## 10. Recommended profile and gate

`RMO 24-2023 Annex E-1 BIR Sales Summary` is `RECOMMENDED_FOR_APPROVAL` as the bounded Z-009 profile. Its aggregation grain should be one immutable detail row per governing committed Z snapshot, while file grouping remains AE-DR-003.

Z-009 remains blocked until the decisions marked `BLOCKS_Z009` are approved. E-2 through E-5 should be separate future tasks because their grain, source facts, privacy basis, and retention differ materially from E-1.
