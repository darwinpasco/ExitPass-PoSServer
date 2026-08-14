# ExitPass POS Server Annex E-1 Synthetic UAT Expected Value Matrix v1.0

## 1. Normative use

This matrix is a closed lookup: each dataset case selects the exact header profile and ordered row profiles in section 4. References are not formulas or placeholders. A later fixture implementation must compare all 42 physical positions to these literal values.

Money values are PHP minor units followed by the exact workbook display in parentheses. Blank is written `BLANK` and is permitted only for D02/D03 in a governed no-activity row. Dates are `YYYY-MM-DD`. The renderer writes H09 as UTC text and money with `0.00`, no grouping separator.

## 2. Header profile `HP-01`

| Position | Official meaning | Exact semantic value | Exact workbook-visible value / reason |
|---|---|---|---|
| H01 | Name of Taxpayer | `SYNTHETIC ANNEX E1 PARKING SERVICES INC.` | same |
| H02 | Address of Taxpayer | `100 SYNTHETIC AVENUE, TEST CITY 0000` | same |
| H03 | TIN | `000-000-000-000` | same; reserved synthetic value |
| H04 | Software Name and Version No. | `ExitPass POS Server` + `1.3` | row 5 begins `ExitPass POS Server 1.3` |
| H05 | Release No. / Release Date | `Z-012B` + `2026-08-10` | row 5 complete value `ExitPass POS Server 1.3 / Z-012B / 2026-08-10` |
| H06 | Serial No. | `SYN-AE1-SERIAL-01` | same |
| H07 | Machine Identification Number | `SYN-AE1-MIN-01` | same |
| H08 | POS Terminal No. | `SYN-AE1-SPS-01` | same; internal Site POS Server code remains externally unaccepted under AE-DR-020A |
| H09 | Date and Time Generated | `2026-09-30T10:00:00.0000000Z` | `2026-09-30 10:00:00 UTC` |
| H10 | UserID | `svc-syn-annex-e1-uat-v1` | same; pseudonymous service reference |

## 3. Exact detail row profiles

### 3.1 D01-D11

| Row | D01 | D02 | D03 | D04 | D05 | D06 | D07 | D08 | D09 | D10 | D11 |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A` | 2026-09-10 | SYN-AE1-SI-000001 | SYN-AE1-SI-000002 | 113000 (1130.00) | 100000 (1000.00) | 500 (5.00) | 17000 (170.00) | 10000 (100.00) | 2000 (20.00) | 1000 (10.00) | 0 (0.00) |
| `RP-B-START` | 2026-09-10 | SYN-AE1-SI-000101 | SYN-AE1-SI-000101 | 11200 (112.00) | 0 (0.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-C-BOUNDARY` | 2026-09-10 | BLANK | BLANK | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-B-BOUNDARY` | 2026-09-11 | SYN-AE1-SI-000102 | SYN-AE1-SI-000102 | 11200 (112.00) | 0 (0.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-B-MONTH` | 2026-09-10 | SYN-AE1-SI-000201 | SYN-AE1-SI-000201 | 11200 (112.00) | 0 (0.00) | 0 (0.00) | 11200 (112.00) | 10000 (100.00) | 1200 (12.00) | 0 (0.00) | 0 (0.00) |
| `RP-C-MONTH` | 2026-09-11 | BLANK | BLANK | 11200 (112.00) | 11200 (112.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-D-MONTH` | 2026-09-12 | SYN-AE1-SI-000202 | SYN-AE1-SI-000202 | 33600 (336.00) | 11200 (112.00) | 0 (0.00) | 22400 (224.00) | 20000 (200.00) | 2400 (24.00) | 0 (0.00) | 0 (0.00) |
| `RP-D-SINGLE` | 2026-09-10 | SYN-AE1-SI-000301 | SYN-AE1-SI-000301 | 22400 (224.00) | 0 (0.00) | 0 (0.00) | 22400 (224.00) | 20000 (200.00) | 2400 (24.00) | 0 (0.00) | 0 (0.00) |

### 3.2 D12-D22

| Row | D12 | D13 | D14 | D15 | D16 | D17 | D18 | D19 | D20 | D21 | D22 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A` | 1000 (10.00) | 1000 (10.00) | 0 (0.00) | 0 (0.00) | 1000 (10.00) | 0 (0.00) | 1000 (10.00) | 4000 (40.00) | 300 (3.00) | 200 (2.00) | 0 (0.00) |
| `RP-B-START` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-C-BOUNDARY` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-B-BOUNDARY` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-B-MONTH` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-C-MONTH` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-D-MONTH` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |
| `RP-D-SINGLE` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) |

### 3.3 D23-D32

| Row | D23 | D24 | D25 | D26 | D27 | D28 | D29 | D30 | D31 | D32 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `RP-A` | 0 (0.00) | 0 (0.00) | 500 (5.00) | 2000 (20.00) | 11000 (110.00) | 200 (2.00) | 11700 (117.00) | 0 | 1 | `NONE` |
| `RP-B-START` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 1 | `NONE` |
| `RP-C-BOUNDARY` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 | 1 | `NO_ACTIVITY` |
| `RP-B-BOUNDARY` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 2 | `NONE` |
| `RP-B-MONTH` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 1200 (12.00) | 10000 (100.00) | 0 (0.00) | 10000 (100.00) | 0 | 1 | `NONE` |
| `RP-C-MONTH` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 (0.00) | 0 | 2 | `NO_ACTIVITY` |
| `RP-D-MONTH` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 2400 (24.00) | 20000 (200.00) | 0 (0.00) | 20000 (200.00) | 0 | 3 | `NONE` |
| `RP-D-SINGLE` | 0 (0.00) | 0 (0.00) | 0 (0.00) | 2400 (24.00) | 20000 (200.00) | 0 (0.00) | 20000 (200.00) | 0 | 1 | `NONE` |

The only governed blanks are D02 and D03 in `RP-C-BOUNDARY` and `RP-C-MONTH`: transaction count and fiscal range count are zero, beginning/ending references are null, all monetary facts are authoritative zero, GTA is unchanged, and the committed Z advances. No other H/D position is blank.

## 4. Case-to-output lookup

Every case uses `HP-01`.

| Case | Exact ordered detail profiles | Detail count | Workbook action result |
|---|---|---:|---|
| DS-AE1-001 | `RP-A` | 1 | revision 1 committed after future authorization |
| DS-AE1-003 | `RP-B-START` | 1 | revision 1 committed |
| DS-AE1-004 | `RP-C-BOUNDARY`, `RP-B-BOUNDARY` | 2 | revision 1 committed; order fixed |
| DS-AE1-005 | `RP-B-MONTH`, `RP-C-MONTH`, `RP-D-MONTH` | 3 | revision 1 committed; order fixed |
| DS-AE1-007 | `RP-A` | 1 | revision 1 committed |
| DS-AE1-009 | `RP-D-SINGLE` | 1 | revision 1 committed |
| DS-AE1-010 | `RP-A` | 1 | revision 1 committed |
| DS-AE1-011 | `RP-A` | 1 | revision 1 committed |
| DS-AE1-012 | `RP-A` | 1 | first commit then exact replay; still 1 row |
| DS-AE1-013 | `RP-A` | 1 | original remains; changed-semantic retry conflicts |
| DS-AE1-014 | `RP-A` | 1 | same stored bytes after restart |
| DS-AE1-016 | `RP-A` | 1 | control committed; altered/missing bytes never returned |
| DS-AE1-017 | `RP-A` | 1 control expectation | unauthorized request creates 0 workbooks/rows |
| DS-AE1-018 | `RP-A` | 1 pre-created control | permissions do not create additional rows |
| DS-AE1-019 | `RP-A` | 1 pre-created control | wrong scope creates 0 rows |
| DS-AE1-020 | `RP-A` | 1 pre-created control | Production fixture authority creates 0 rows |
| DS-AE1-021 | `RP-A` | 1 control expectation | each invalid mutation creates 0 rows |
| DS-AE1-023 | `RP-A` | 1 | revision 1 committed for privacy inspection only |
| DS-AE1-024 | `RP-A` | 1 after retry | failed publication creates 0 committed rows; clean retry creates 1 |

There are 22 expected control/output rows across the 19 case definitions. Negative attempts do not add to that count.

## 5. Deterministic artifact expectations

For a future authorized implementation, each successful case must use worksheet `E-1`, columns A:AF, data starting row 17, fixed package timestamps `2000-01-01`, fixed ZIP entry order, no compression, invariant formats, and filename `ANNEX-E1_SYN-AE1-FI-01_SYN-AE1-MIN-01_202609_v1.xlsx`. Exact replay returns the initially stored SHA-256 and byte length; those two outputs are recorded in execution evidence and cannot be stated before fixture implementation/workbook generation. This is not a missing field value: all semantic and workbook-visible inputs are fixed above, while generation remains unauthorized.
