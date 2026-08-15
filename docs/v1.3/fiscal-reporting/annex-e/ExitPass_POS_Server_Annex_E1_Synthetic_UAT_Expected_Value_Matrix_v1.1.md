# ExitPass POS Server Annex E-1 Synthetic UAT Expected Value Matrix v1.1

## 1. Exact value notation

This is a closed lookup for all 42 physical positions. Money is PHP minor units followed by workbook text. `SI(s,o)` means the exact ASCII value `SYN-AE1-SI-` + the three digits of scenario `s` + `-` + four-digit ordinal `o`; for example `SI(001,1)` is `SYN-AE1-SI-001-0001`. `SPS(s)`, `FI(s)`, `SERIAL(s)`, and `MIN(s)` use the same three-digit scenario suffix. These functions are total deterministic naming rules, not unspecified values.

No H or D position is blank in an included v1.1 case. Null, blank, unavailable, and not applicable are invalid expected values for these 22 rows. Zero means authoritative source zero or a governed attested-zero Accounting fact. D32 is always `NONE`; `NO_ACTIVITY` is absent from the included population.

## 2. Header H01-H10

| Position | Exact semantic and workbook-visible value | Production method |
|---|---|---|
| H01 | `SYNTHETIC ANNEX E1 PARKING SERVICES INC.` | current runtime |
| H02 | `100 SYNTHETIC AVENUE, TEST CITY 0000` | current runtime |
| H03 | `000-000-000-000` | current runtime; reserved synthetic value |
| H04 | `ExitPass POS Server 1.3` | current constants |
| H05 | `Z-012B / 2026-08-10` | current constants |
| H06 | `SERIAL(s)` = `SYN-AE1-SERIAL-NNN` | current immutable BIR header snapshot |
| H07 | `MIN(s)` = `SYN-AE1-MIN-NNN` | current immutable BIR header snapshot |
| H08 | `SPS(s)` = `SYN-AE1-SPS-NNN` | current Site POS Server code; official acceptance unresolved under AE-DR-020A |
| H09 | semantic `2026-09-30T10:00:00.0000000Z`; visible `2026-09-30 10:00:00 UTC` | requires the unauthorized future controlled-test-clock contract |
| H10 | `svc-syn-annex-e1-uat-v11` | current request actor field |

H09 is excluded from the current workbook semantic hash by `AnnexE1SemanticHasher`, but future Controlled UAT must compare it exactly. H01-H08 and H10 are semantic-hash inputs.

## 3. Row profiles D01-D32

### 3.1 D01-D11

| Profile | D01 | D02 | D03 | D04 | D05 | D06 | D07 | D08 | D09 | D10 | D11 |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A11` | 2026-09-10 | `SI(s,1)` | `SI(s,4)` | 128600 (1286.00) | 100000 (1000.00) | 500 (5.00) | 44800 (448.00) | 30000 (300.00) | 3600 (36.00) | 0 (0.00) | 0 (0.00) |
| `RP-B11-START` | 2026-09-10 | `SI(003,1)` | `SI(003,1)` | 11200 (112.00) | 0 (0.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-B11-BOUNDARY-1` | 2026-09-10 | `SI(004,1)` | `SI(004,1)` | 11200 (112.00) | 0 (0.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-B11-BOUNDARY-2` | 2026-09-11 | `SI(004,2)` | `SI(004,2)` | 22400 (224.00) | 11200 (112.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-B11-MONTH-1` | 2026-09-10 | `SI(005,1)` | `SI(005,1)` | 11200 (112.00) | 0 (0.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-B11-MONTH-2` | 2026-09-11 | `SI(005,2)` | `SI(005,2)` | 22400 (224.00) | 11200 (112.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-D11-MONTH-3` | 2026-09-12 | `SI(005,3)` | `SI(005,3)` | 44800 (448.00) | 22400 (224.00) | 0 (0.00) | 22400 (224.00) | 20000 (200.00) | 2400 (24.00) | 0 (0.00) | 0 (0.00) |
| `RP-D11-SINGLE` | 2026-09-10 | `SI(009,1)` | `SI(009,1)` | 22400 (224.00) | 0 (0.00) | 0 (0.00) | 22400 (224.00) | 20000 (200.00) | 2400 (24.00) | 0 (0.00) | 0 (0.00) |

For `RP-A11`, `s` is the owning scenario: 001, 007, 010, 011, 012, 013, 014, 016, 017, 018, 019, 020, 021, 023, or 024.

### 3.2 D12-D22

| Profile | D12 | D13 | D14 | D15 | D16 | D17 | D18 | D19 | D20 | D21 | D22 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A11` | 2000 (20.00) | 2000 (20.00) | 0 (0.00) | 0 (0.00) | 1000 (10.00) | 0 (0.00) | 11200 (112.00) | 16200 (162.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| every `RP-B11-*` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| every `RP-D11-*` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |

D14, D15, and D22 are zero only through current immutable `attested_zero` facts. D20 and D21 are source zero because the supported `SP-A11` statutory facts use `STATUTORY_DISCOUNT_ONLY` and VAT privilege 0. D16 is the supported coupon amount; `other_statutory_discount_amount_minor_units` is zero.

### 3.3 D23-D32

| Profile | D23 | D24 | D25 | D26 | D27 | D28 | D29 | D30 | D31 | D32 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `RP-A11` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 3600 (36.00) | 25000 (250.00) | 200 (2.00) | 25700 (257.00) | 0 | 1 | `NONE` |
| `RP-B11-START` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 1 | `NONE` |
| `RP-B11-BOUNDARY-1` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 1 | `NONE` |
| `RP-B11-BOUNDARY-2` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 2 | `NONE` |
| `RP-B11-MONTH-1` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 1 | `NONE` |
| `RP-B11-MONTH-2` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 2 | `NONE` |
| `RP-D11-MONTH-3` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 2400 (24.00) | 20000 (200.00) | 0 (0.00) | 20000 (200.00) | 0 | 3 | `NONE` |
| `RP-D11-SINGLE` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 2400 (24.00) | 20000 (200.00) | 0 (0.00) | 20000 (200.00) | 0 | 1 | `NONE` |

## 4. Case-to-output lookup

| Cases | Exact ordered profile list | Count |
|---|---|---:|
| `DS-AE1-001`, 007, 010, 011, 012, 013, 014, 016, 017, 018, 019, 020, 021, 023, 024 | `RP-A11` | 15 |
| `DS-AE1-003` | `RP-B11-START` | 1 |
| `DS-AE1-004` | `RP-B11-BOUNDARY-1`, `RP-B11-BOUNDARY-2` | 2 |
| `DS-AE1-005` | `RP-B11-MONTH-1`, `RP-B11-MONTH-2`, `RP-D11-MONTH-3` | 3 |
| `DS-AE1-009` | `RP-D11-SINGLE` | 1 |
| **Total** |  | **22** |

## 5. Exact arithmetic results

| Profile | R01 | R02 | R03 | R04 | R05 | R06 | R07 | R11 | R12 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A11` | 33600 | 1000 | 16200 | 0 | 3600 | 44800 | 25000 | 28600 | 128600 |
| every `RP-B11-*` | 11200 | 0 | 0 | 0 | 1200 | 11200 | 10000 | 11200 | prior GTA + 11200 |
| every `RP-D11-*` | 22400 | 0 | 0 | 0 | 2400 | 22400 | 20000 | 22400 | prior GTA + 22400 |

R08-R10 use exact identity and hash comparisons in the source matrix. All differences are 0 minor units or 0 rows.

## 6. Artifact boundary

The future renderer uses worksheet `E-1`, A:AF, first detail row 17, normalized package properties, fixed ZIP timestamps, and filename `ANNEX-E1_<FI(s)>_<MIN(s)>_202609_v1.xlsx`. Artifact SHA-256 and byte length cannot be stated without generating the workbook, which is prohibited. They are not semantic input values and must be captured only in a separately authorized run. Exact replay must compare the initially stored hash, length, and bytes without regeneration.
