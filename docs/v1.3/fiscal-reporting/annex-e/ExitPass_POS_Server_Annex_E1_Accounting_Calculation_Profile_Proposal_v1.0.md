# ExitPass POS Server Annex E-1 Accounting Calculation Profile Proposal v1.0

## 1. Control record

| Item | Value |
|---|---|
| Document status | `PENDING_EXACT_PROFILE_APPROVAL` |
| Authority status | `ACCOUNTING_AUTHORITY_AND_APPROVAL_IN_PRINCIPLE_CONFIRMED` |
| Executable profile status | `EXECUTABLE_CALCULATION_PROFILE_NOT_YET_APPROVED` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Calculation profile | `pos-server-annex-e1-accounting-calculation:v1` |
| Decisions | AE-DR-006, AE-DR-007, AE-DR-008, AE-DR-009 |
| Prepared | 2026-08-10 PHT |
| Approving authority | Accounting |
| Runtime authorization | Not granted by this proposal |

This document presents one exact calculation profile for Accounting approval. It is an engineering recommendation grounded in the official workbook geometry, governed ExitPass fiscal sources, and current fail-closed runtime boundaries. It does not claim that the recommended meanings are prescribed or accepted by BIR or an examiner.

## 2. Source evidence and official grid

The official source is `D:\Docs\ExitPass\POS\RMO 24-2023 Annex E-1 to E-5.xlsx`, SHA-256 `7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197`. The E-1 worksheet has dimension `A1:AF16`, 32 merged ranges, and no executable formulas.

The row-16 content is printed text and field-number annotation. The proposal treats each expression literally by the displayed field number anchored to its physical column. It does not shift operands because NAAC and Solo Parent occupy unnumbered physical columns.

| Official field | Position | Official label | Printed content |
|---:|---|---|---|
| unnumbered | D06 / F | Sales Issued w/ Manual SI/OR (per RR 16-2018) | no formula |
| 6 | D07 / G | Gross Sales for the Day | `6` |
| 8 | D09 / I | VAT Amount | `8` |
| 11 | D12 / L | Deductions / Discount / SC | `11` |
| 12 | D13 / M | Deductions / Discount / PWD | `12` |
| group 13 | D14 / N | Deductions / Discount / NAAC | no separate field number |
| group 13 | D15 / O | Deductions / Discount / Solo Parent | no separate field number |
| 13 | D16 / P | Deductions / Discount / Others | `13` |
| 14 | D17 / Q | Deductions / Returns | `14` |
| 15 | D18 / R | Deductions / Voids | `15` |
| 16 | D19 / S | Deductions / Total Deductions | `16` |
| 17 | D20 / T | Adjustment on VAT / Discount / SC | `17` |
| 18 | D21 / U | Adjustment on VAT / Discount / PWD | `18` |
| 19 | D22 / V | Adjustment on VAT / Discount / Others | `19` |
| 20 | D23 / W | Adjustment on VAT / VAT on Returns | `20` |
| 21 | D24 / X | Adjustment on VAT / Others | `21` |
| 22 | D25 / Y | Adjustment on VAT / Total VAT Adjustment | `22 = 17+18+19+20+21` |
| 23 | D26 / Z | VAT Payable | `23 = 8-19` |
| 24 | D27 / AA | Net Sales | `24 = 6-16-8` |
| 25 | D28 / AB | Sales Overrun /Overflow | `25` |
| 26 | D29 / AC | Total Income | `26` |

The official workbook does not define D06, D28, or D29 and does not explain why field 23 subtracts field 19 rather than field 22. The recommendation below deliberately preserves the literal expression for approval instead of substituting a conventional formula.

## 3. Common execution rules

The calculation-table cells reference these complete common rules.

| Rule | Definition |
|---|---|
| C01 Source state | Governing Z is `COMMITTED`, its reporting period is `CLOSED`, and exactly one committed Z-010 BIR Sales Summary is bound to it. |
| C02 Period | Every source fact belongs to the same `fiscal_reporting_period_id`; its effective instant is in `[period_start_at, period_end_at)`. Later encoding does not move an original manual issuance to another period. |
| C03 Scope | Site POS Server, fiscal identity, and currency must match across Z, BIR Summary, period fact, and output row. |
| C04 Currency | PHP only. Mixed currency and non-PHP facts fail closed. |
| C05 Arithmetic | Checked signed 64-bit integer minor units. No floating point. No aggregation rounding. Two-decimal conversion occurs only in XLSX presentation. |
| C06 Null | A missing mandatory fact, source identity, status, attestation, or semantic hash blocks generation. Null is never zero. |
| C07 Known zero | Zero is authoritative only when a canonical amount is recorded as zero or a first-class status is `ATTESTED_ZERO` under the same scope and period. Absence of a row is not evidence. |
| C08 No activity | Requires Z transaction count zero; no fiscal range; all Z/BIR Summary amount facts zero; prior GTA equals resulting GTA; Z counter advanced; reset unchanged; and Manual SI/OR, overrun/overflow, NAAC, Solo Parent, and unsupported VAT-adjustment facts explicitly attest zero. |
| C09 Corrections | Source facts are append-only. A correction supersedes the prior fact with reason, approval reference, prior identity, new semantic hash, and effective period unchanged. A generated workbook is corrected only through immutable supersession. |
| C10 Unsupported | Negative inputs, arithmetic overflow, duplicate facts, wrong period/scope, nonzero unsupported privilege, nonzero refund/adjustment/service charge, unresolved return tax, or unclassified source blocks generation. |
| C11 Tolerance | Every equation and source reconciliation has tolerance exactly zero minor units. |
| C12 Source hierarchy | Z-010 BIR Sales Summary and its governing committed Z are financial authority. Z-011A Electronic Journal is traceability evidence only and never supplies a missing amount. |

## 4. Named operands

| Operand | Semantic meaning | Source classification | Canonical source |
|---|---|---|---|
| `ACTIVE_GROSS` | Gross amount of recorded active Sales Invoices in the period | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.gross_sales_amount_minor_units` |
| `VAT_AMOUNT` | Recorded VAT amount for active qualifying Sales Invoices | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.vat_amount_minor_units` |
| `FINAL_FISCAL_AMOUNT` | Existing VAT-inclusive final electronic Sales Invoice amount | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.net_sales_amount_minor_units`; not D27 |
| `SC_DISCOUNT` | Senior Citizen discount excluding separately recorded VAT adjustment | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.senior_citizen_discount_amount_minor_units` |
| `PWD_DISCOUNT` | PWD discount excluding separately recorded VAT adjustment | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.pwd_discount_amount_minor_units` |
| `NAAC_DISCOUNT` | Separately classified NAAC discount | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future immutable Z/BIR discount classification; zero requires attestation |
| `SOLO_PARENT_DISCOUNT` | Separately classified Solo Parent discount | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future immutable Z/BIR discount classification; zero requires attestation |
| `OTHER_STATUTORY_DISCOUNT` | Approved statutory discount not represented by D12-D15 and not an unresolved VAT privilege | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.other_statutory_discount_amount_minor_units` |
| `COUPON_DISCOUNT` | Governed coupon discount | `AVAILABLE_CANONICAL_SOURCE` | Governing `pos.x_z_reports.coupon_discount_amount_minor_units` through the BIR Summary source binding |
| `PROMOTIONAL_DISCOUNT` | Governed promotional discount | `AVAILABLE_CANONICAL_SOURCE` | Governing `pos.x_z_reports.promotional_discount_amount_minor_units` through the BIR Summary source binding |
| `RETURN_AMOUNT` | Governed return deduction magnitude | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.return_amount_minor_units`; bounded runtime permits only recorded zero |
| `VOID_AMOUNT` | Governed same-period void deduction magnitude | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports.void_amount_minor_units` |
| `SC_VAT_ADJUSTMENT` | VAT removed for Senior Citizen classification | `AVAILABLE_CANONICAL_DERIVATION` | Governing Z discount child `senior_citizen_statutory.vat_exemption_amount_minor_units` |
| `PWD_VAT_ADJUSTMENT` | VAT removed for PWD classification | `AVAILABLE_CANONICAL_DERIVATION` | Governing Z discount child `pwd_statutory.vat_exemption_amount_minor_units` |
| `OTHER_VAT_ADJUSTMENT` | Approved VAT adjustment for Discount Others | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | Zero only when no nonzero AE-DR-012 classification exists; nonzero blocks |
| `VAT_ON_RETURNS` | VAT adjustment attributable to returns | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | Zero only when `RETURN_AMOUNT = 0` and no return-tax fact exists |
| `VAT_ADJUSTMENT_OTHER` | VAT adjustment not represented by fields 17-20 | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | Zero only when adjustment, refund, service-charge, and unsupported tax facts are all recorded zero |
| `MANUAL_NET_INCOME` | VAT-exclusive net income from qualifying manual SI/OR issued under approved continuity authority | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future immutable period accounting fact `manual_si_or_net_income` |
| `OVERFLOW_NET_INCOME` | VAT-exclusive net income omitted from regular electronic sales solely by a governed accumulated-sales capacity overflow event | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future immutable period accounting fact `sales_overrun_overflow_net_income` |

## 5. Complete named-input calculation table

All rows are part of one recommendation and have approval status `PENDING_EXACT_PROFILE_APPROVAL`. Formula assignment uses `:=`; additions and subtractions operate on nonnegative magnitudes in PHP minor units.

| Decision | Field | Position | Official label | Semantic name | Business definition | Named operands | Exact formula | Source class | Canonical source | Source fact | Source status | Period | Inclusion | Exclusion | Sign | Currency | Units/rounding | Null | Known zero | No activity | Correction | Reconciliation | Tolerance | Unsupported | Initial behavior | New capability | Accounting rationale | Examiner dependency | Approval |
|---|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AE-DR-007 | unnumbered | D06 | Sales Issued w/ Manual SI/OR (per RR 16-2018) | `manual_net_income` | VAT-exclusive net income of qualifying manually issued SI/OR during continuity operation | `MANUAL_NET_INCOME` | `D06 := MANUAL_NET_INCOME` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future `pos.annex_e1_period_accounting_facts` | `manual_si_or_net_income_minor_units` | `RECORDED` or `ATTESTED_ZERO` | C02 | Authorized manual SI/OR originally issued in period | Electronic/fiscalized duplicate, draft, cancelled, later duplicate encoding | Nonnegative income magnitude | C04 | C05 | C06 | C07 | zero attestation required by C08 | C09 | D06 equals bound fact | C11 | Missing/duplicate/late unapproved source blocks | Block until first-class fact exists | Manual period fact and attestation service | Separate amount is needed for D29 without double counting electronic Z | BIR/examiner meaning remains UAT gate | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 6 | D07 | Gross Sales for the Day | `annex_gross_sales` | Active electronic gross plus return and void magnitudes so deductions remain visible once | `ACTIVE_GROSS`, `RETURN_AMOUNT`, `VOID_AMOUNT` | `D07 := ACTIVE_GROSS + RETURN_AMOUNT + VOID_AMOUNT` | `AVAILABLE_CANONICAL_DERIVATION` | `pos.bir_sales_summary_reports` | named operands | C01 | C02 | Recorded active sales and governed exception magnitudes | Manual and overflow sales | Nonnegative | C04 | C05 | C06 | source zeros | zero | C09 | `D07 - D17 - D18 = ACTIVE_GROSS` | C11 | Overflow/negative blocks | Supported when inputs governed | None | Prevents subtracting D17/D18 twice because Z gross excludes voided documents | Formula acceptance remains Accounting subject | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 8 | D09 | VAT Amount | `recorded_vat_amount` | VAT amount recorded for active electronic fiscal sales | `VAT_AMOUNT` | `D09 := VAT_AMOUNT` | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports` | `vat_amount_minor_units` | C01 | C02 | Active qualifying sales | Manual, overrun, unsupported returns | Nonnegative tax magnitude | C04 | C05 | C06 | source zero | zero | C09 | D09 equals source | C11 | Mismatch blocks | Supported | None | Literal official field 8 | None for source; formula use awaits approval | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 11 | D12 | Deductions / Discount / SC | `sc_discount` | Recorded Senior Citizen discount excluding VAT adjustment | `SC_DISCOUNT` | `D12 := SC_DISCOUNT` | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports` | `senior_citizen_discount_amount_minor_units` | C01 | C02 | SC discount | VAT adjustment and PWD | Nonnegative deduction | C04 | C05 | C06 | source zero | zero | C09 | D12 equals source and child | C11 | Mismatch blocks | Supported | None | Preserves separate discount and VAT treatment | None | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 12 | D13 | Deductions / Discount / PWD | `pwd_discount` | Recorded PWD discount excluding VAT adjustment | `PWD_DISCOUNT` | `D13 := PWD_DISCOUNT` | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports` | `pwd_discount_amount_minor_units` | C01 | C02 | PWD discount | VAT adjustment and SC | Nonnegative deduction | C04 | C05 | C06 | source zero | zero | C09 | D13 equals source and child | C11 | Mismatch blocks | Supported | None | Preserves separate discount and VAT treatment | None | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | group 13 | D14 | Deductions / Discount / NAAC | `naac_discount` | Separately recorded NAAC discount | `NAAC_DISCOUNT` | `D14 := NAAC_DISCOUNT` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future `pos.fiscal_report_discount_breakdowns` classification | NAAC amount/status | committed classification | C02 | Approved NAAC fact | Other statutory substitution | Nonnegative deduction | C04 | C05 | C06 | C07 | attested zero | C09 | D14 equals child | C11 | Unknown/nonzero unsupported blocks initial runtime | Zero-only initial path | Classification and source fact | Workbook has a separate physical column | AE-DR-011A blocks UAT acceptance | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | group 13 | D15 | Deductions / Discount / Solo Parent | `solo_parent_discount` | Separately recorded Solo Parent discount | `SOLO_PARENT_DISCOUNT` | `D15 := SOLO_PARENT_DISCOUNT` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future `pos.fiscal_report_discount_breakdowns` classification | Solo Parent amount/status | committed classification | C02 | Approved Solo Parent fact | Other statutory substitution | Nonnegative deduction | C04 | C05 | C06 | C07 | attested zero | C09 | D15 equals child | C11 | Unknown/nonzero unsupported blocks initial runtime | Zero-only initial path | Classification and source fact | Workbook has a separate physical column | AE-DR-011A blocks UAT acceptance | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 13 | D16 | Deductions / Discount / Others | `other_discount` | Governed other statutory plus coupon and promotional discounts | `OTHER_STATUTORY_DISCOUNT`, `COUPON_DISCOUNT`, `PROMOTIONAL_DISCOUNT` | `D16 := OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT` | `AVAILABLE_CANONICAL_DERIVATION` | `pos.bir_sales_summary_reports`, `pos.x_z_reports`, and bound children | named operands | C01 | C02 | Approved named categories | SC, PWD, NAAC, Solo Parent, Diplomat/unresolved privilege, VAT adjustment | Nonnegative deduction | C04 | C05 | C06 | all operands zero | zero | C09 | D16 equals named child sum | C11 | Any unclassified/nonzero AE-DR-012 fact blocks | Supported only for named categories | None | One controlled residual discount column without arbitrary category coercion | AE-DR-012 nonzero path unresolved | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 14 | D17 | Deductions / Returns | `return_deduction` | Governed return deduction magnitude | `RETURN_AMOUNT` | `D17 := RETURN_AMOUNT` | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports` | `return_amount_minor_units` | C01 | C02 | Governed same-period return | Refund/cross-period return | Nonnegative deduction | C04 | C05 | C06 | source zero | zero | C09 | D17 equals source | C11 | Nonzero currently blocks | Zero-only initial path | Future return contract for nonzero | Preserves separate official column | Nonzero treatment remains outside bounded contract | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 15 | D18 | Deductions / Voids | `void_deduction` | Governed same-period void magnitude | `VOID_AMOUNT` | `D18 := VOID_AMOUNT` | `AVAILABLE_CANONICAL_SOURCE` | `pos.bir_sales_summary_reports` | `void_amount_minor_units` | C01 | C02 | Governed same-period void | Cross-period void/cancellation | Nonnegative deduction | C04 | C05 | C06 | source zero | zero | C09 | D18 equals source; D07 adds same magnitude | C11 | Cross-period/unclassified blocks | Supported same-period | None | Makes void visible without changing active gross authority | None for bounded same-period fact | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 16 | D19 | Deductions / Total Deductions | `total_deductions` | Sum of every physical deduction column D12-D18 | D12-D18 | `D19 := D12 + D13 + D14 + D15 + D16 + D17 + D18` | `AVAILABLE_CANONICAL_DERIVATION` | Versioned Annex E-1 projection over bound governed inputs | named D positions | inputs pass status | C02 | All listed deductions | VAT adjustments D20-D24 | Nonnegative deduction | C04 | C05 | C06 | all operands zero | zero | C09 | D19 equals exact physical sum | C11 | Negative/overflow/unclassified blocks | Supported after all operands authoritative | None | Group geometry labels S as Total Deductions | Exact composition requires Accounting approval | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 17 | D20 | Adjustment on VAT / Discount / SC | `sc_vat_adjustment` | VAT removed for SC treatment | `SC_VAT_ADJUSTMENT` | `D20 := SC_VAT_ADJUSTMENT` | `AVAILABLE_CANONICAL_DERIVATION` | `pos.fiscal_report_discount_breakdowns` | SC child VAT exemption | C01 | C02 | SC VAT adjustment | SC discount and PWD | Nonnegative adjustment | C04 | C05 | C06 | child zero | zero | C09 | D20 equals child | C11 | Missing child/mismatch blocks | Supported | None | Literal official field 17 | None | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 18 | D21 | Adjustment on VAT / Discount / PWD | `pwd_vat_adjustment` | VAT removed for PWD treatment | `PWD_VAT_ADJUSTMENT` | `D21 := PWD_VAT_ADJUSTMENT` | `AVAILABLE_CANONICAL_DERIVATION` | `pos.fiscal_report_discount_breakdowns` | PWD child VAT exemption | C01 | C02 | PWD VAT adjustment | PWD discount and SC | Nonnegative adjustment | C04 | C05 | C06 | child zero | zero | C09 | D21 equals child | C11 | Missing child/mismatch blocks | Supported | None | Literal official field 18 | None | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 19 | D22 | Adjustment on VAT / Discount / Others | `other_vat_adjustment` | Approved VAT adjustment associated with Discount Others | `OTHER_VAT_ADJUSTMENT` | `D22 := OTHER_VAT_ADJUSTMENT` | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | Future approved privilege mapping | amount/status | committed or attested zero | C02 | Approved mapped other VAT adjustment | SC/PWD and unresolved Diplomat privilege | Nonnegative adjustment | C04 | C05 | C06 | C07 | attested zero | C09 | D22 equals mapped source | C11 | Nonzero AE-DR-012 blocks | Zero-only initial path | Approved mapping/source for nonzero | Literal field 19 is used by D26 | AE-DR-012 blocks nonzero path | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 20 | D23 | Adjustment on VAT / VAT on Returns | `vat_on_returns` | VAT adjustment attributable to governed returns | `VAT_ON_RETURNS` | `D23 := VAT_ON_RETURNS` | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | Future governed return-tax period fact | amount/status | attested zero initially | C02 | Approved return VAT | Other VAT adjustments | Nonnegative adjustment | C04 | C05 | C06 | C07 with D17 zero | zero | C09 | D23 zero iff return source proves zero | C11 | Nonzero blocks | Zero-only initial path | Return-tax contract for nonzero | Literal official field 20 | Nonzero treatment requires separate approval | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 21 | D24 | Adjustment on VAT / Others | `vat_adjustment_other` | Approved VAT adjustment not represented by D20-D23 | `VAT_ADJUSTMENT_OTHER` | `D24 := VAT_ADJUSTMENT_OTHER` | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | Future governed VAT-adjustment period fact | amount/status | attested zero initially | C02 | Approved residual VAT adjustment | Refund, service charge, unclassified adjustment | Nonnegative adjustment | C04 | C05 | C06 | C07 | attested zero | C09 | D24 equals classified source | C11 | Nonzero/unclassified blocks | Zero-only initial path | Controlled source for nonzero | Literal official field 21 | Nonzero treatment requires separate approval | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 22 | D25 | Adjustment on VAT / Total VAT Adjustment | `total_vat_adjustment` | Sum of official fields 17-21 | D20-D24 | `D25 := D20 + D21 + D22 + D23 + D24` | `AVAILABLE_CANONICAL_DERIVATION` | Versioned Annex E-1 projection | named D positions | inputs pass status | C02 | All listed adjustments | None | Nonnegative adjustment | C04 | C05 | C06 | all operands zero | zero | C09 | D25 equals exact physical sum | C11 | Negative/overflow blocks | Supported after inputs authoritative | None | Literal printed equation `22=17+18+19+20+21` | None for equation text | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 23 | D26 | VAT Payable | `annex_vat_payable` | Workbook-defined VAT payable using literal official fields 8 and 19 | D09, D22 | `D26 := D09 - D22` | `AVAILABLE_CANONICAL_DERIVATION` | Versioned Annex E-1 projection | `VAT_AMOUNT`, `OTHER_VAT_ADJUSTMENT` | inputs pass status | C02 | Literal operands | D20, D21, D23, D24 are not substituted | Nonnegative result; negative blocks | C04 | C05 | C06 | zero when both operands zero | zero | C09 | `D26 + D22 = D09` | C11 | Negative result blocks | Supported with zero-only D22 | None | Preserves literal `23=8-19`; does not silently replace 19 with 22 | Accounting approval essential; examiner acceptance remains UAT gate | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-006 | 24 | D27 | Net Sales | `annex_net_sales_ex_vat` | Workbook-defined regular electronic VAT-exclusive net sales | D07, D19, D09 | `D27 := D07 - D19 - D09` | `AVAILABLE_CANONICAL_DERIVATION` | Annex calculation over BIR Summary inputs | named D positions | inputs pass status | C02 | Regular electronic sales and governed deductions | Manual and overrun amounts | Nonnegative result; negative blocks | C04 | C05 | C06 | zero when operands reconcile to zero | zero | C09 | `D27 + D19 + D09 = D07` | C11 | Negative/mismatch blocks | Supported after operand approval | None | Preserves literal `24=6-16-8`; is not an alias for VAT-inclusive `FINAL_FISCAL_AMOUNT` | Accounting approval essential | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-008 | 25 | D28 | Sales Overrun /Overflow | `overflow_net_income` | VAT-exclusive net income omitted from regular electronic sales only because an approved accumulated-sales capacity boundary was reached | `OVERFLOW_NET_INCOME` | `D28 := OVERFLOW_NET_INCOME` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Future `pos.annex_e1_period_accounting_facts` | `sales_overrun_overflow_net_income_minor_units` | `RECORDED` or `ATTESTED_ZERO` | C02 | System-detected approved accumulated-sales capacity overflow | Rounding, cash overage, SI number exhaustion, counter-display rollover, transaction volume, manual sales, late posting | Nonnegative income magnitude | C04 | C05 | C06 | C07 plus enforced no-overflow state | zero attestation required | C09 | D28 equals bound fact and is excluded from D27 | C11 | Missing/unclassified event blocks | Zero-only until source exists | Overflow event/fact and zero attestation | Gives the adjacent D28/D29 fields a single non-overlapping net-income meaning | BIR/examiner terminology remains UAT gate | `PENDING_EXACT_PROFILE_APPROVAL` |
| AE-DR-009 | 26 | D29 | Total Income | `total_income` | Total VAT-exclusive income represented by regular electronic, manual, and overflow paths | D27, D06, D28 | `D29 := D27 + D06 + D28` | `AVAILABLE_CANONICAL_DERIVATION` | Versioned Annex E-1 projection | named D positions | inputs pass status | C02 | Regular electronic, qualifying manual, approved overflow net income | VAT, tenders, GTA balance, refunds/adjustments unsupported by profile | Nonnegative income | C04 | C05 | C06 | all operands zero | zero | C09 | `D29 - D06 - D28 = D27` | C11 | Negative/overflow/missing source blocks | Supported only after D06/D28 facts exist | None beyond D06/D28 facts | Totals three mutually exclusive income paths on one VAT-exclusive basis | Exact meaning requires Accounting approval | `PENDING_EXACT_PROFILE_APPROVAL` |

## 6. Manual SI/OR source contract recommendation

The minimum source is an immutable period fact, provisionally `pos.annex_e1_period_accounting_facts` with controlled classification `manual_si_or_net_income`. It must contain:

* stable fact and operation identities;
* Site POS Server, fiscal identity, PHP currency, reporting period, and business date;
* `RECORDED` or `ATTESTED_ZERO` status;
* manual document count, first/last manual reference where applicable, and VAT-exclusive net income minor units;
* source authority and privacy-safe approval/attestation reference;
* semantic-hash version and hash;
* recorded/effective timestamps;
* prior-fact and superseding-fact references;
* immutable correction reason classification;
* binding into the committed BIR Sales Summary/Annex source membership before generation.

Qualifying manual documents are only SI/OR issued under a separately authorized continuity process during the governed period. Later encoding preserves original period attribution and is excluded from electronic Z aggregation if it represents the same issuance. Draft, cancelled, duplicated, later-fiscalized duplicate, wrong-scope, wrong-currency, and unapproved manual records are excluded. A missing source blocks generation. Zero requires an explicit period attestation by an authorized Accounting/fiscal role; an empty table is not zero.

## 7. Sales Overrun/Overflow source contract recommendation

The same immutable period-fact object uses controlled classification `sales_overrun_overflow_net_income`. The selected qualifying event is an approved accumulated-sales capacity boundary that causes otherwise qualifying sales to be omitted from the regular electronic sales represented by D27. The amount is the VAT-exclusive net-income value of those omitted sales.

The profile explicitly rejects these alternatives:

* rounding difference: it is a reconciliation defect, not sales;
* cash or tender overage: payment custody is not fiscal income authority;
* fiscal-number exhaustion: it is a sequence/continuity event, not an amount;
* Z/reset display rollover: counters are continuity facts, not income;
* high transaction volume without omitted sales: no overrun occurred;
* manual continuity sales: they belong only in D06;
* late posting: period attribution is governed by source issuance, not processing delay.

The source must carry the same identity, scope, status, amount, attestation, semantic hash, immutability, and correction properties as section 6, plus an approved overflow-event reference. Current ExitPass checked arithmetic and fail-closed state transitions do not by themselves prove the regulatory field is zero. The initial runtime needs a first-class `ATTESTED_ZERO` fact; absence remains blocking.

## 8. Total Income alternatives assessment

| Candidate | Decision | Reason |
|---|---|---|
| Gross basis | Rejected | Ignores official deductions and VAT and conflicts with field 24. |
| Net basis | Selected with explicit additions | D27 is the workbook-defined regular net-income component; D06 and D28 are separate income paths and must be added exactly once. |
| Net plus VAT | Rejected | Reintroduces VAT that literal field 24 subtracts. |
| Tender/payment-confirmation basis | Rejected | Tender is reconciliation evidence and Central PMS owns payment finality; it is not Annex financial authority. |
| GTA contribution basis | Rejected | GTA contribution is VAT-inclusive final electronic fiscal amount, excludes separate manual/overflow paths, and is already represented by D04/D05 continuity. |

The selected equation is only `D29 := D27 + D06 + D28`. D29 has no equality to GTA, tenders, or the existing VAT-inclusive BIR Summary `net_sales_amount_minor_units`.

## 9. Reconciliation set

All tolerances are zero minor units.

```text
R01: D07 - D17 - D18 = ACTIVE_GROSS
R02: D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
R03: D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
R04: D25 = D20 + D21 + D22 + D23 + D24
R05: D26 + D22 = D09
R06: D27 + D19 + D09 = D07
R07: D29 - D06 - D28 = D27
R08: each copied operand equals its committed source fact
R09: source Site POS Server, fiscal identity, period, and PHP currency are identical
R10: Electronic Journal source-transition references corroborate Z and BIR Summary identities but supply no amount
```

The existing Z/BIR `net_sales_amount_minor_units` remains the VAT-inclusive final electronic fiscal amount and GTA contribution. It is not renamed or overwritten by D27. The proposed profile intentionally creates a separate Annex-calculated VAT-exclusive value because that is what the literal field-24 expression states.

## 10. Approval consequences

Approval exactly as specified authorizes subsequent schema and runtime design to implement these definitions, subject to all other Annex E gates. It does not authorize Controlled UAT, external submission, BIR/examiner acceptance, Production, nonzero unresolved VAT privileges, E-2 through E-5, or runtime implementation in this task.

Rejection or requested changes keep AE-DR-006 through AE-DR-009 unresolved. Any change to an operand, formula, source, sign, period rule, or zero rule requires a new version and hash of this profile before approval.
