# ExitPass POS Server Annex E-1 Synthetic UAT Expected Value Matrix v1.2

## 1. Header values

All 22 rows have these exact H values; `NNN` is the owning scenario.

| Position | Exact value | Role / comparison |
|---|---|---|
| H01 | `SYNTHETIC ANNEX E1 PARKING SERVICES INC.` | current header input / `EXACT_OFFLINE` |
| H02 | `100 SYNTHETIC AVENUE, TEST CITY 0000` | current header input / `EXACT_OFFLINE` |
| H03 | `000-000-000-000` | reserved synthetic input / `EXACT_OFFLINE` |
| H04 | `ExitPass POS Server 1.3` | current constant / `EXACT_OFFLINE` |
| H05 | `Z-012B / 2026-08-10` | current constant / `EXACT_OFFLINE` |
| H06 | `SYN-AE1-SERIAL-NNN` | immutable synthetic header / `EXACT_OFFLINE` |
| H07 | `SYN-AE1-MIN-NNN` | immutable synthetic header / `EXACT_OFFLINE` |
| H08 | `SYN-AE1-SPS-NNN` | provisional under AE-DR-020A / `EXACT_OFFLINE` |
| H09 | `2026-09-30 10:00:00 UTC` | future clock prerequisite / `EXACT_AFTER_CLOCK_CONTROL`; excluded from semantic hash |
| H10 | `svc-syn-annex-e1-uat-v12` | exact synthetic actor / `EXACT_OFFLINE` |

No H position is blank, null, omitted, or not-applicable.

## 2. D01-D11

| Profile | D01 | D02 | D03 | D04 | D05 | D06 | D07 | D08 | D09 | D10 | D11 |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A12` | 2026-09-10 | SI(NNN,1) | SI(NNN,4) | 128600 | 100000 | 500 | 41200 | 30000 | 3600 | 0 | 0 |
| `RP-B12-START` | 2026-09-10 | SI(003,1) | SI(003,1) | 11200 | 0 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B12-BOUNDARY-1` | 2026-09-10 | SI(004,1) | SI(004,1) | 11200 | 0 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B12-BOUNDARY-2` | 2026-09-11 | SI(004,2) | SI(004,2) | 22400 | 11200 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B12-MONTH-1` | 2026-09-10 | SI(005,1) | SI(005,1) | 11200 | 0 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B12-MONTH-2` | 2026-09-11 | SI(005,2) | SI(005,2) | 22400 | 11200 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-D12-MONTH-3` | 2026-09-12 | SI(005,3) | SI(005,3) | 44800 | 22400 | 0 | 20000 | 20000 | 2400 | 0 | 0 |
| `RP-D12-SINGLE` | 2026-09-10 | SI(009,1) | SI(009,1) | 22400 | 0 | 0 | 20000 | 20000 | 2400 | 0 | 0 |

`SI(NNN,o)` is exactly `SYN-AE1-SI-NNN-OOOO`.

## 3. D12-D22

| Profile | D12 | D13 | D14 | D15 | D16 | D17 | D18 | D19 | D20 | D21 | D22 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A12` | 2000 | 2000 | 0 | 0 | 1000 | 0 | 11200 | 16200 | 0 | 0 | 0 |
| every B12/D12 profile | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

D14, D15, and D22 are governed attested zero. D20/D21 are source zero because statutory VAT privilege is zero. D16 is coupon 1000 plus supported other/promotional zero.

## 4. D23-D32

| Profile | D23 | D24 | D25 | D26 | D27 | D28 | D29 | D30 | D31 | D32 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `RP-A12` | 0 | 0 | 0 | 3600 | 21400 | 200 | 22100 | 0 | 1 | `NONE` |
| every B12 profile | 0 | 0 | 0 | 1200 | 8800 | 0 | 8800 | 0 | period sequence | `NONE` |
| every D12 profile | 0 | 0 | 0 | 2400 | 17600 | 0 | 17600 | 0 | period sequence | `NONE` |

No D position is blank, null, unavailable, omitted, or not-applicable. Zero is an authoritative source zero, a calculated zero, or an immutable attested zero identified above. Monetary workbook presentation is exact minor units divided by 100 with two decimal places and no additional rounding.

## 5. Ordered 22-row inventory

| Case population | Ordered rows | Count |
|---|---|---:|
| 15 SP-A12 cases | `RP-A12` | 15 |
| case 003 | `RP-B12-START` | 1 |
| case 004 | `RP-B12-BOUNDARY-1`, `RP-B12-BOUNDARY-2` | 2 |
| case 005 | `RP-B12-MONTH-1`, `RP-B12-MONTH-2`, `RP-D12-MONTH-3` | 3 |
| case 009 | `RP-D12-SINGLE` | 1 |
| **Total** |  | **22** |

## 6. Exact reconciliations

| Profile | R01 | R02 | R03 | R04 | R05 | R06 | R07 | R11 | R12 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A12 | 30000 | 1000 | 16200 | 0 | 3600 | 41200 | 21400 | 28600 | 100000+28600=128600 |
| B12 | 10000 | 0 | 0 | 0 | 1200 | 10000 | 8800 | 11200 | prior+11200 |
| D12 | 20000 | 0 | 0 | 0 | 2400 | 20000 | 17600 | 22400 | prior+22400 |

Every difference is 0 minor units. R08-R10 use exact source and journal hashes from the canonical contract.

