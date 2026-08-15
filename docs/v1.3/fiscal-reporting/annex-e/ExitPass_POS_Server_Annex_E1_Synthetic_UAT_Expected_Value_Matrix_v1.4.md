# ExitPass POS Server Annex E-1 Synthetic UAT Expected Value Matrix v1.4

## 1. Headers

All 22 rows have H01 `SYNTHETIC ANNEX E1 PARKING SERVICES INC.`, H02 `100 SYNTHETIC AVENUE, TEST CITY 0000`, H03 `000-000-000-000`, H04 `ExitPass POS Server 1.3`, H05 `Z-012B / 2026-08-10`, H06 `SYN-AE1-SERIAL-NNN`, H07 `SYN-AE1-MIN-NNN`, H08 `SYN-AE1-SPS-NNN`, H09 `2026-09-30 10:00:00 UTC`, and H10 `svc-syn-annex-e1-uat-v13`. No H field is blank, null, omitted, or not applicable. H09 is a fixed offline expected value and remains excluded from workbook semantic hashing.

## 2. D01-D11

| Profile | D01 | D02 | D03 | D04 | D05 | D06 | D07 | D08 | D09 | D10 | D11 |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A13` | 2026-09-10 | SI(NNN,1) | SI(NNN,4) | 128600 | 100000 | 500 | 41200 | 30000 | 3600 | 0 | 0 |
| `RP-B13-START` | 2026-09-10 | SI(003,1) | SI(003,1) | 11200 | 0 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B13-BOUNDARY-1` | 2026-09-10 | SI(004,1) | SI(004,1) | 11200 | 0 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B13-BOUNDARY-2` | 2026-09-11 | SI(004,2) | SI(004,2) | 22400 | 11200 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B13-MONTH-1` | 2026-09-10 | SI(005,1) | SI(005,1) | 11200 | 0 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-B13-MONTH-2` | 2026-09-11 | SI(005,2) | SI(005,2) | 22400 | 11200 | 0 | 10000 | 10000 | 1200 | 0 | 0 |
| `RP-D13-MONTH-3` | 2026-09-12 | SI(005,3) | SI(005,3) | 44800 | 22400 | 0 | 20000 | 20000 | 2400 | 0 | 0 |
| `RP-D13-SINGLE` | 2026-09-10 | SI(009,1) | SI(009,1) | 22400 | 0 | 0 | 20000 | 20000 | 2400 | 0 | 0 |

`SI(NNN,o)` is `SYN-AE1-SI-NNN-OOOO`.

## 3. D12-D22

| Profile | D12 | D13 | D14 | D15 | D16 | D17 | D18 | D19 | D20 | D21 | D22 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `RP-A13` | 2000 | 2000 | 0 | 0 | 1000 | 0 | 11200 | 16200 | 0 | 0 | 0 |
| every B13/D13 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

D14, D15, and D22 are attested zero. D20 and D21 are source zero. D16 is the supported coupon amount 1000; other-statutory and promotional are zero.

## 4. D23-D32

| Profile | D23 | D24 | D25 | D26 | D27 | D28 | D29 | D30 | D31 | D32 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `RP-A13` | 0 | 0 | 0 | 3600 | 21400 | 200 | 22100 | 0 | 1 | `NONE` |
| every B13 | 0 | 0 | 0 | 1200 | 8800 | 0 | 8800 | 0 | period sequence | `NONE` |
| every D13 | 0 | 0 | 0 | 2400 | 17600 | 0 | 17600 | 0 | period sequence | `NONE` |

No D position is blank, null, unavailable, omitted, or not applicable. Monetary presentation divides minor units by 100 and renders two decimal places without further rounding.

## 5. Ordered rows and reconciliation

Fifteen A13 cases contribute 15 rows; case 003 contributes one; case 004 contributes two; case 005 contributes three; case 009 contributes one. Total: 22.

| Rule | A13 | B13 | D13 | Monetary difference | Row difference |
|---|---:|---:|---:|---:|---:|
| R01 | 41200-0-11200=30000 | 10000 | 20000 | 0 | 0 |
| R02 | 1000=0+1000+0 | 0 | 0 | 0 | 0 |
| R03 | 16200=2000+2000+0+0+1000+0+11200 | 0 | 0 | 0 | 0 |
| R04 | 0=0+0+0+0+0 | 0 | 0 | 0 | 0 |
| R05 | 3600+0=3600 | 1200 | 2400 | 0 | 0 |
| R06 | 21400+16200+3600=41200 | 8800+1200=10000 | 17600+2400=20000 | 0 | 0 |
| R07 | 22100-500-200=21400 | 8800 | 17600 | 0 | 0 |
| R08 | exact v1.4 source identities/hashes | exact | exact | 0 | 0 |
| R09 | exact scope/currency/period | exact | exact | 0 | 0 |
| R10 | one Z and BIR event per period | same | same | 0 | 0 |
| R11 | 9200+9200+10200=28600 | 11200 | 10000+12400=22400 | 0 | 0 |
| R12 | 100000+28600=128600 | prior+11200 | prior+22400 | 0 | 0 |
