# ExitPass POS Server BIR Annex E Sample Output and Traceability v1.0

## 1. Important notice

All values are synthetic. These examples are contract-analysis fixtures, not BIR-approved output. `PENDING_EXTERNAL[AE-DR-nnn]` is a deliberate fail-closed marker and must never appear in an artifact.

## 2. Common synthetic header

| Field | Value | Source trace |
| --- | --- | --- |
| H01 | `SYNTHETIC PARKING CORP.` | Synthetic immutable fiscal identity profile |
| H02 | `100 TEST AVENUE, TEST CITY` | Synthetic immutable fiscal identity profile |
| H03 | `000-000-000-000` | Synthetic taxpayer TIN placeholder, not a real TIN |
| H04 | `ExitPass POS Server v1.3` | Synthetic release profile |
| H05 | `R1 / 2026-08-06` | Synthetic release profile using the AE-DR-019A internal recommendation; external display confirmation remains pending |
| H06 | `SYN-SERIAL-001` | Synthetic header profile |
| H07 | `SYN-MIN-001` | Synthetic header profile |
| H08 | `SYN-TERM-001` | Synthetic governed Site POS Server fiscal terminal identity under AE-DR-020; examiner acceptance remains AE-DR-020A |
| H09 | `2026-08-06 09:00:00 PHT` | Synthetic export operation |
| H10 | `svc-annex-export` | Synthetic privacy-safe service reference |

## 3. Sample A: normal VATable period

### 3.1 Authoritative input facts

```text
Z reference: Z-SYN-000007
Business date: 2026-08-05
SI range: SI-000100 through SI-000102
Qualifying documents: 3
Previous GTA: PHP 10,000.00
Current GTA contribution / net sales: PHP 300.00
Resulting GTA: PHP 10,300.00
Gross sales: PHP 300.00
VATable sales: PHP 267.86
VAT: PHP 32.14
VAT-exempt / zero-rated / discounts / voids: PHP 0.00
Reset counter: 0
Previous Z counter: 6
Resulting Z counter: 7
Tender validation: cash PHP 100.00 + QRPH PHP 200.00 = net PHP 300.00
```

### 3.2 Expected physical E-1 detail row

| Col | Field | Expected value | Trace/calculation |
| --- | --- | ---: | --- |
| A | D01 Date | `2026-08-05` | Z business date |
| B | D02 Beginning SI/OR | `SI-000100` | First immutable range number |
| C | D03 Ending SI/OR | `SI-000102` | Last immutable range number |
| D | D04 GTA Ending | `10300.00` | `10000.00 + 300.00` |
| E | D05 GTA Beginning | `10000.00` | Z previous GTA |
| F | D06 Manual SI/OR | `PENDING_EXTERNAL[AE-DR-007]` | No authoritative source |
| G | D07 Gross Sales | `300.00` | Z gross |
| H | D08 VATable Sales | `267.86` | Z VATable |
| I | D09 VAT Amount | `32.14` | Z VAT |
| J | D10 VAT-Exempt Sales | `0.00` | Recorded zero |
| K | D11 Zero-Rated Sales | `0.00` | Recorded zero |
| L | D12 Discount SC | `0.00` | Recorded zero |
| M | D13 Discount PWD | `0.00` | Recorded zero |
| N | D14 Discount NAAC | `0.00` | Separate immutable classification records absence; unknown cannot become zero |
| O | D15 Discount Solo Parent | `0.00` | Separate immutable classification records absence; unknown cannot become zero |
| P | D16 Discount Others | `PENDING_EXTERNAL[AE-DR-006]` | Official composition unresolved |
| Q | D17 Returns | `0.00` | Only after approved controlled-zero posture |
| R | D18 Voids | `0.00` | Z void |
| S | D19 Total Deductions | `PENDING_EXTERNAL[AE-DR-006]` | Formula mapping gate |
| T | D20 VAT Adj SC | `0.00` | SC child recorded zero |
| U | D21 VAT Adj PWD | `0.00` | PWD child recorded zero |
| V | D22 VAT Adj Others | `PENDING_EXTERNAL[AE-DR-012]` | Mapping unresolved |
| W | D23 VAT on Returns | `0.00` | Recorded bounded-source zero; any nonzero source fails closed under AE-DR-015 |
| X | D24 VAT Adj Others | `PENDING_EXTERNAL[AE-DR-012]` | Classification unresolved |
| Y | D25 Total VAT Adjustment | `PENDING_EXTERNAL[AE-DR-012]` | Components are not all approved |
| Z | D26 VAT Payable | `PENDING_EXTERNAL[AE-DR-006]` | Official equation interpretation unresolved |
| AA | D27 Net Sales | `300.00` | Z net / current GTA contribution |
| AB | D28 Sales Overrun/Overflow | `PENDING_EXTERNAL[AE-DR-008]` | No source |
| AC | D29 Total Income | `PENDING_EXTERNAL[AE-DR-009]` | No approved equation |
| AD | D30 Reset Counter | `0` | Z resulting reset |
| AE | D31 Z-Counter | `7` | Z resulting counter |
| AF | D32 Remarks | `NONE` | Approved controlled code; external acceptance remains AE-DR-010 |

This row demonstrates why Z-009 is blocked: available recorded totals reconcile, but the mandatory physical output cannot be completed without assumptions.

## 4. Sample B: Senior Citizen and PWD statutory discounts

### 4.1 Authoritative input

```text
Z reference: Z-SYN-000008
Previous GTA: 10300.00
Gross: 400.00
SC discount: 20.00
PWD discount: 20.00
SC VAT removal: 10.71
PWD VAT removal: 10.71
Net/current GTA contribution: 360.00
Resulting GTA: 10660.00
VATable: 178.57
VAT: 21.43
VAT-exempt: 160.00
```

### 4.2 Output trace

| Output | Value | Trace |
| --- | ---: | --- |
| D04/D05 | `10660.00` / `10300.00` | Immutable Z GTA transition |
| D07 | `400.00` | Z gross |
| D08/D09/D10 | `178.57` / `21.43` / `160.00` | Recorded Z tax facts |
| D12/D13 | `20.00` / `20.00` | Separate SC/PWD discount children |
| D20/D21 | `10.71` / `10.71` | Separate SC/PWD VAT-removal children |
| D25 candidate | `21.42` | Exact sum of approved components if all other VAT adjustments are approved zero |
| D27 | `360.00` | Z net and current GTA contribution |

No beneficiary name, statutory ID, TIN, evidence, or reviewer data appears.

## 5. Sample C: mixed tender period

E-1 has no tender columns. The synthetic Z records cash `150.00`, QRPH `100.00`, and card `50.00`; their sum equals Z net `300.00`. The output detail amounts equal Sample A and omit tender facts. A tender mismatch blocks generation rather than adding an unofficial column.

## 6. Sample D: same-period void

Synthetic period facts:

```text
One active SI net: 100.00
One same-period void fact: 100.00
Z active net/current GTA contribution: 100.00
Z void amount: 100.00
Previous GTA: 10660.00
Resulting GTA: 10760.00
```

Expected mapped values: D18 `100.00`, D27 `100.00`, D05 `10660.00`, D04 `10760.00`. Whether D19 includes D18 is explicitly gated by AE-DR-006. A cross-period void blocks output under AE-DR-015.

## 7. Sample E: zero-activity period

Synthetic committed Z facts:

```text
Qualifying documents: 0
Previous GTA: 10760.00
Current contribution: 0.00
Resulting GTA: 10760.00
Reset: 0 -> 0
Z: 9 -> 10
All supported recorded amount categories: 0.00
```

D02/D03 are blank and D32 is controlled `NO_ACTIVITY` under approved AE-DR-021 and AE-DR-010A. This remains a traceability sample, not currently generatable evidence: AE-DR-007, AE-DR-008, and AE-DR-009 must establish authoritative known-zero behavior for D06, D28, and D29, and AE-DR-006 must settle the row equations. Unknown is not converted to zero. Examiner acceptance of Remarks remains AE-DR-010.

## 8. Sample F: sequence-gap exception

Synthetic Z range: sequences `200` through `202`, two qualifying documents, gap `201` classified `VOIDED_WITHIN_PERIOD`. The Z can close only when the gap classification is governed. E-1 has no gap column. AE-DR-022 recommends failing E-1 generation when a range or gap cannot be represented unambiguously; it does not flatten, truncate, or invent a Remarks value. An unexplained gap always blocks generation.

## 9. Sample G: Z counter/GTA transition

| Fact | Previous | Current contribution | Resulting | Required check |
| --- | ---: | ---: | ---: | --- |
| Reset counter | 0 | 0 | 0 | unchanged |
| Z counter | 10 | 1 | 11 | previous + 1 |
| GTA | 10760.00 | 240.00 | 11000.00 | previous + contribution |

E-1 maps GTA resulting to D04, GTA previous to D05, reset resulting to D30, and Z resulting to D31. The current contribution must equal Z net sales `240.00` under the approved supported source set.

## 10. Traceability checks

1. Every populated value above comes from an immutable Z/header fact or an explicitly shown checked sum.
2. Every unavailable mandatory value carries a user or external decision ID.
3. No sample uses live recomputation, customer data, statutory identity, ticket number, plate number, credential, or production identifier.
4. No pending marker may be serialized into an artifact.
