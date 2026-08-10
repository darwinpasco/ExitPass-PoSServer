# ExitPass POS Server BIR Annex E Calculation and Reconciliation Rules v1.0

## 1. Authority boundary

Annex E is a deterministic projection of an immutable committed Z Reading, its merged Z-010 BIR Sales Summary, and approved historical header facts. It does not recalculate fiscal policy or query live mutable transactions after close. Z-011A Electronic Journal evidence may verify transition traceability but is not a financial calculation input.

Rules below distinguish three levels:

- **Recorded-copy rule:** copy an authoritative immutable value.
- **Integrity derivation:** calculate a presentation-only total expressly supported by the workbook.
- **Decision-required equation:** do not implement until approved.

All arithmetic uses checked integer minor units. Output conversion to decimal major units occurs only during serialization.

Z-012A originally found AE-DR-006 through AE-DR-009 unresolved. Accounting subsequently approved the exact Z-012A1 profile under `Z-012B-ACCOUNTING-APPROVAL-001`; those values and equations are now governed implementation requirements. Missing inputs still must not be assumed zero.

## 2. Inclusion and period rules

| Rule | Definition | Classification / source | Failure posture | Current support |
| --- | --- | --- | --- | --- |
| Governing source | One E-1 detail row consumes one committed `Z_READING`, its immutable children, and its committed Z-010 BIR Sales Summary. | `EXISTING_EXITPASS_DECISION`; AE-SRC-016, AE-SRC-017; merged Z-010 | Reject open, missing, mutable, wrong-kind, or unreconciled source | Supported |
| Window | Governing Z covers `[period_start_at, period_end_at)` and uses durable reporting-period assignments. | `EXISTING_EXITPASS_DECISION`; AE-SRC-017 | Reject inconsistent period or assignment | Supported |
| Scope | Site POS Server, fiscal identity, and currency must match the Z and approved profile. | `EXISTING_EXITPASS_DECISION`; AE-SRC-016, AE-SRC-017 | Hidden-scope denial / fail closed | Supported for Z; Annex profile pending |
| Qualifying documents | Recorded Sales Invoices assigned to the period; failed/incomplete operations and reprints do not add sales. | `EXISTING_EXITPASS_DECISION`; Z-006A/Z-007 | Reject unresolved classification | Supported |
| Period date | D01 uses the governing Z business-day date, not local calendar conversion during export. | `EXISTING_EXITPASS_DECISION`; AE-DR-003 | Reject mismatch | Supported |
| Post-close facts | No live data is added after Z close. | `EXISTING_EXITPASS_DECISION` | A correction requires approved lineage, never silent recomputation | Supported boundary |

## 3. Recorded amount rules

| Annex E fact | Authoritative input | Inclusion/exclusion and sign | Required reconciliation | Classification | Support |
| --- | --- | --- | --- | --- | --- |
| Gross sales D07 | `ACTIVE_GROSS`, `RETURN_AMOUNT`, `VOID_AMOUNT` | Approved `ACTIVE_GROSS+RETURN_AMOUNT+VOID_AMOUNT`; nonnegative | `D07-D17-D18=ACTIVE_GROSS` | `Z-012B-ACCOUNTING-APPROVAL-001` | Derivation to implement |
| Net sales D27 | D07, D19, D09 | Approved Annex VAT-exclusive regular electronic value `D07-D19-D09`; not VAT-inclusive Z net | `D27+D19+D09=D07` | `Z-012B-ACCOUNTING-APPROVAL-001` | Derivation to implement |
| VATable sales D08 | Z VATable sales | Recorded tax classification only | Exact equality to Z | `EXISTING_EXITPASS_DECISION` | Supported |
| VAT D09 | Z VAT amount | Recorded VAT; exporter does not recalculate a tax rate | Exact equality to Z | `EXISTING_EXITPASS_DECISION` | Supported |
| VAT-exempt sales D10 | Z VAT-exempt sales | Recorded VAT treatment only | Exact equality to Z | `EXISTING_EXITPASS_DECISION` | Supported |
| Zero-rated sales D11 | Z zero-rated sales | Recorded classification only | Exact equality to Z | `EXISTING_EXITPASS_DECISION` | Supported |
| Total discounts | Approved D12-D18 operands | Statutory and commercial categories must not be double counted | `D19=D12+D13+D14+D15+D16+D17+D18` | `Z-012B-ACCOUNTING-APPROVAL-001` | Derivation to implement |
| SC discount D12 | Z SC discount and SC child | Positive deduction display | Top-level equals child | `EXISTING_EXITPASS_DECISION` | Supported |
| PWD discount D13 | Z PWD discount and PWD child | Positive deduction display | Top-level equals child | `EXISTING_EXITPASS_DECISION` | Supported |
| Other discount D16 | Z/BIR other statutory, coupon, and promotional named operands | Include exactly those three; exclude NAAC, Solo Parent, Diplomat/unresolved privilege, and VAT adjustment | Exact named-operand sum | Approved AE-DR-006; AE-DR-012 nonzero path remains fail closed | Derivation to implement |
| VAT removal D20-D24 | Z discount-child VAT exemption/removal values | Separate from discount amount | Component sum equals D25 | `EXISTING_EXITPASS_DECISION` for separation; detailed mapping unresolved | Partial |
| Coupon/promotional discount | Z separate fields/children | Included exactly once in D16 | D16 named-operand reconciliation | `Z-012B-ACCOUNTING-APPROVAL-001` | Source exists |
| Void D18 | Z same-period void amount | Positive deduction display; voided sale excluded from active sale contribution | Exact equality to Z void facts | `EXISTING_EXITPASS_DECISION` | Supported |
| Return D17 | Z reserved return total | Nonzero generation prohibited until source attribution/sign contract exists | Exact equality once governed | `EXISTING_EXITPASS_DECISION`; AE-DR-015 | Fail closed |
| Refund | No E-1 labeled field | Must not be silently folded into Returns or Others | None until separately governed | `EXISTING_EXITPASS_DECISION`; AE-DR-015 | Fail closed |
| Cancellation | No E-1 labeled field | Must not be silently treated as void | None until separately governed | `EXISTING_EXITPASS_DECISION`; AE-DR-015 | Fail closed |
| Adjustment | Z reserved adjustment total; no direct E-1 deduction label except VAT Others/Remarks possibilities | Must not be silently mapped | None until separately governed | `EXISTING_EXITPASS_DECISION`; AE-DR-015 | Fail closed |
| Service charge | Z reserved service-charge total; no E-1 label | Must not be silently included/excluded | None until separately governed | `EXISTING_EXITPASS_DECISION`; AE-DR-015 | Fail closed |
| Tender totals | Z tender children | E-1 has no tender output field; may be used only as a validation input | Tender sum equals Z net under supported source set | `NOT_APPLICABLE` to output; reconciliation is `EXISTING_EXITPASS_DECISION` | Supported validation |

## 4. Workbook equations

AE-SRC-001 prints the following equations in the E-1 field-number row:

```text
22 = 17 + 18 + 19 + 20 + 21
23 = 8 - 19
24 = 6 - 16 - 8
```

The first equation unambiguously supports:

```text
D25 Total VAT Adjustment = D20 + D21 + D22 + D23 + D24
```

Tolerance is exactly zero minor units. Overflow is terminal. Missing components are not treated as zero unless their classification is approved and the governing source records zero.

Accounting approved the literal named-operand interpretation under `Z-012B-ACCOUNTING-APPROVAL-001`:

```text
D26 = D09 - D22
D27 = D07 - D19 - D09
```

D26 must not substitute D25 or another conventional VAT equation. D27 is not the VAT-inclusive Z/BIR Summary net amount.

## 5. Candidate reconciliation equations

These are recommendations, not approved BIR equations:

| ID | Recommended integrity check | Inputs | Tolerance | Mandatory status | Failure posture |
| --- | --- | --- | ---: | --- | --- |
| AE-RC-001 | Annex Z identity equals governing committed Z identity | Report/profile/Z references | 0 | `EXISTING_EXITPASS_DECISION` | Reject |
| AE-RC-002 | D04 = D05 + Z current GTA contribution | Recorded GTA values | 0 minor units | `EXISTING_EXITPASS_DECISION` | Reject |
| AE-RC-003 | Z current GTA contribution = Z net sales | Recorded Z values | 0 | `EXISTING_EXITPASS_DECISION` under supported source set | Reject |
| AE-RC-004 | D12 = SC discount child; D13 = PWD child | Recorded snapshot/children | 0 | `EXISTING_EXITPASS_DECISION` | Reject |
| AE-RC-005 | D25 = D20 + D21 + D22 + D23 + D24 | Annex E components | 0 | `EXPLICITLY_REQUIRED`; AE-SRC-001 | Reject |
| AE-RC-006 | D19 = D12+D13+D14+D15+D16+D17+D18 | Annex E deduction components | 0 | Approved by `Z-012B-ACCOUNTING-APPROVAL-001` | Reject mismatch |
| AE-RC-007 | D02/D03 and counts equal immutable fiscal range children | Z range children | 0 | `EXISTING_EXITPASS_DECISION` | Reject |
| AE-RC-008 | Unexplained sequence gaps are absent | Z gap children | 0 unexplained gaps | `EXISTING_EXITPASS_DECISION` | Reject |
| AE-RC-009 | Tender child total = Z net sales for supported source set | Z tender children | 0 | `EXISTING_EXITPASS_DECISION` | Reject |
| AE-RC-010 | Header profile scope equals Z Site POS Server/fiscal identity | Historical profile and Z | Exact | `EXISTING_EXITPASS_DECISION`; `Z-009B-USER-APPROVAL-001` | Reject |

## 6. GTA, counters, and ranges

- D05 is the previous GTA and D04 is the resulting GTA from the immutable Z counter snapshot.
- The period increment is the Z current GTA contribution, approved as the VAT-inclusive final fiscal-document amount of qualifying recorded Sales Invoices.
- D31 is the resulting Z counter and must equal previous Z counter plus one.
- D30 is the resulting reset counter and must equal the previous reset counter for an ordinary Z close.
- D02 and D03 use first-class fiscal sequence-range facts, never lexical minimum/maximum over SI text.
- A multi-series Z can own multiple ranges, but E-1 has one beginning and one ending column. The AE-DR-022 recommendation fails generation for an ambiguous multi-range or unrepresentable gap; it does not flatten or truncate.

## 7. Classification-specific posture

| Category | Output posture |
| --- | --- |
| Senior Citizen | Separate D12 and D20. No beneficiary identity in E-1. |
| PWD | Separate D13 and D21. No beneficiary identity in E-1. |
| NAAC | D14 exists but runtime source is not governed. Do not use `other_statutory` as a substitute. |
| Solo Parent | D15 exists but runtime source is not governed. Do not use `other_statutory` as a substitute. |
| Diplomat VAT privilege | Preserve as VAT treatment, not ordinary discount, but E-1 column mapping is unresolved under AE-DR-012. |
| Other statutory | Requires approved code-to-column mapping. |
| Coupon/promotion | Remains separate in Z. E-1 mapping unresolved. |

## 8. Exceptional transactions

| Case | Rule | Classification |
| --- | --- | --- |
| Same-period void | Exclude from active sale contribution and copy governed void amount to D18. | `EXISTING_EXITPASS_DECISION` |
| Cross-period void | Fail closed; no approved attribution. | `EXISTING_EXITPASS_DECISION` |
| Refund/return/adjustment | Fail closed for nonzero values until separate source/sign/period decisions. | `EXISTING_EXITPASS_DECISION` from Z-007A/Z-007 |
| Reprint | Does not count as a sale and has no E-1 row. | `EXISTING_EXITPASS_DECISION` |
| Training transaction | No current governed source or E-1 rule; fail closed if encountered. | `EXISTING_EXITPASS_DECISION`; AE-DR-023 |
| Late transaction | Cannot enter a closed period because of the Z close boundary. No Annex E post-close insertion. | `EXISTING_EXITPASS_DECISION` |
| Corrected period/file | No reopen or mutation. Immutable supersession lineage preserves the original. | `EXISTING_EXITPASS_DECISION`; AE-DR-017; `Z-009B-USER-APPROVAL-001` |

## 9. Zero, negative, rounding, and currency

- Zero is explicit for supported recorded categories with an authoritative zero.
- Missing is not zero. A missing mandatory field blocks output.
- Current Z amount fields are nonnegative. The external representation of negative adjustment values remains unresolved and must not be invented.
- No floating-point arithmetic is allowed. Major-unit formatting divides integer minor units using the currency minor-unit contract.
- The workbook does not state decimal places, separator, or rounding mode. PHP-only output using two decimal places, `.` as decimal separator, and no thousands separator is approved under AE-DR-019A and `Z-009B-USER-APPROVAL-001`; regulatory acceptance remains AE-DR-019.
- E-1 has no currency column. One output profile must contain one currency, and non-PHP applicability is AE-DR-019.

## 10. No-activity period

A configured empty period can close and advances the Z counter under the approved Z contract. AE-DR-021 and `Z-009B-USER-APPROVAL-001` require:

- D02/D03 may be blank or use a controlled not-applicable marker;
- amounts and counts may be zero;
- GTA beginning and ending remain equal;
- reset stays unchanged and Z counter advances;
- remarks may carry a controlled `NO_ACTIVITY` code only if AE-DR-010/AE-DR-021 approves it.

Unknown source facts still fail closed rather than being converted into placeholders.

## 11. Replay and regeneration

Exact replay of one approved Annex E operation over one governing Z/profile/version must return identical bytes. A changed profile, grouping, or output contract is a semantic conflict or a new versioned operation, not silent replacement. Prior-period regeneration reads only immutable sources and must not change fiscal state.

## 12. Z-012A1 executable calculation profile approved by Z-012A2

The exact immutable profile is governed by the [Accounting Calculation Profile Proposal](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md) and approved by `Z-012B-ACCOUNTING-APPROVAL-001`. Its principal equations, all in checked PHP minor units with zero tolerance, are:

```text
D07 = ACTIVE_GROSS + RETURN_AMOUNT + VOID_AMOUNT
D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
D25 = D20 + D21 + D22 + D23 + D24
D26 = D09 - D22
D27 = D07 - D19 - D09
D29 = D27 + D06 + D28
```

D26 preserves literal official expression `23=8-19`; D27 preserves `24=6-16-8`. D27 is the approved Annex-calculated VAT-exclusive net value and is not the existing VAT-inclusive Z/BIR Summary net amount. D06 and D28 require new immutable period facts or explicit zero attestations. Any change requires a new profile version, hash, and Accounting approval.
