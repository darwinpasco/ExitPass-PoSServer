# ExitPass POS Server Annex E-1 Synthetic UAT Source Population and Reconciliation Matrix v1.3

## 1. Normative expansion

A row is the unique product of scenario, registered object token and ordinal, the v1.3 canonical row contract, its profile, and deterministic chronology. The canonical population document supplies every field value or closed expression. Missing bindings fail validation; database defaults and runtime-generated values are not substituted.

## 2. Population profiles

| Population | Periods | Documents | Lines/totals/tax | Tenders | Discounts | Statutory | X/Z/BIR | Facts | EJ | Ranges |
|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `POP-A13` | 1 | SC, PWD, coupon, void | 4 each | 4 | 3 | 2 | 1 each | 7 | 8 | 2 |
| `POP-B13` | 1 | ordinary | 1 each | 1 | 0 | 0 | 1 each | 7 | 4 | 2 |
| `POP-D13` | 1 | ordinary | 1 each | 2 | 0 | 0 | 1 each | 7 | 4 | 2 |
| `POP-BOUNDARY13` | 2 | one B13/period | 2 each | 2 | 0 | 0 | 2 each | 14 | 8 | 4 |
| `POP-MONTH13` | 3 | B13, B13, D13 | 3 each | 4 | 0 | 0 | 3 each | 21 | 12 | 6 |

Fifteen scenarios use A13; case 003 uses B13; case 004 boundary; case 005 month; case 009 D13.

## 3. SP-A13 documents

| Doc | Line gross/base | VAT | Discount | Final | Statutory original | Snapshot | State/report treatment |
|---|---:|---:|---:|---:|---:|---|---|
| 0001 SC | 10000 | 1200 | 2000 | 9200 | 11200 | one `SENIOR_CITIZEN` | recorded/active |
| 0002 PWD | 10000 | 1200 | 2000 | 9200 | 11200 | one `PWD` | recorded/active |
| 0003 coupon | 10000 | 1200 | 1000 | 10200 | not applicable | none | recorded/active |
| 0004 void | 10000 | 1200 | 0 | 11200 | not applicable | none | recorded then voided |

For statutory rows, `11200 = 10000 + 1200`, `9200 = 11200 - 2000`, and `9200 + 2000 = 11200`. The fiscal line equation remains `10000 - 2000 + 1200 = 9200`. Coupon is commercial. Other statutory is zero.

## 4. B13 and D13

B13: line base/gross 10000, VAT 1200, discount 0, total/tender 11200. D13: line base/gross 20000, VAT 2400, discount 0, total 22400, cash 10000, digital wallet 12400. Their tax-inclusive original values, where represented outside statutory facts, are 11200 and 22400 respectively.

## 5. Report snapshots

| Profile | First/last sequence | Previous/current/resulting GTA | Gross/net | VATable/VAT | Discounts | Void |
|---|---|---|---|---|---|---:|
| A13 | 1/4 | 100000/28600/128600 | 30000/28600 | 30000/3600 | 5000 total | 11200 |
| B13 | assigned ordinal | prior/11200/prior+11200 | 10000/11200 | 10000/1200 | 0 | 0 |
| D13 | assigned ordinal | prior/22400/prior+22400 | 20000/22400 | 20000/2400 | 0 | 0 |

A13 tender breakdown: cash count 3, amount 28600. A13 discounts: coupon 1/1000/0, PWD 1/2000/0, SC 1/2000/0. D13 tenders: cash 1/10000 and digital wallet 1/12400. X and Z each own one range per period; A13 sequences 1-4 have zero gaps because a voided numbered document remains in range.

## 6. Accounting facts and journal

Each period has seven ordered facts: `manual_si_or_net_income`, `sales_overrun_overflow_net_income`, `naac_discount`, `solo_parent_discount`, `other_vat_adjustment`, `vat_on_returns`, `residual_vat_adjustment`. A13 amounts are 500, 200, 0, 0, 0, 0, 0. B13/D13 are all zero. Case 013 adds correction fact 0008, amount 600, superseding fact 0001.

A13 EJ schedule is document commits 1-4, document-4 void, X commit, Z commit, BIR commit. B13/D13 is document, X, Z, BIR. Multi-period cases continue a single Site/identity/PHP stream. The v1.2 148-row ledger remains byte-identical and is incorporated by the exact digest stated in the v1.3 hash contract; the corrected original amount is not an EJ semantic fact.

## 7. Reconciliation proof

| Rule | A13 equality | B13 equality | D13 equality |
|---|---|---|---|
| R01 | `41200-0-11200=30000` | `10000=10000` | `20000=20000` |
| R02 | `1000=0+1000+0` | `0=0` | `0=0` |
| R03 | `16200=2000+2000+0+0+1000+0+11200` | `0=0` | `0=0` |
| R04 | `0=0+0+0+0+0` | `0=0` | `0=0` |
| R05 | `3600+0=3600` | `1200=1200` | `2400=2400` |
| R06 | `21400+16200+3600=41200` | `8800+1200=10000` | `17600+2400=20000` |
| R07 | `22100-500-200=21400` | `8800=8800` | `17600=17600` |
| R08 | all copied IDs and v1.3 hashes exact | exact | exact |
| R09 | Site/identity/PHP/period exact | exact | exact |
| R10 | one integrity-valid Z and BIR event/period | same | same |
| R11 | `9200+9200+10200=28600` | `11200` | `10000+12400=22400` |
| R12 | `100000+28600=128600` | prior+11200 | prior+22400 |

Every monetary difference and row-count difference is zero.

## 8. Counts and v1.2 delta

| Family group | v1.2 | v1.3 | Delta |
|---|---:|---:|---:|
| Sites / identities / headers / workbooks | 19 each | 19 each | 0 |
| periods / X / Z / BIR / Annex | 22 each | 22 each | 0 |
| documents / lines / totals / tax | 67 each | 67 each | 0 |
| tenders / discounts / statutory | 69 / 45 / 30 | 69 / 45 / 30 | 0 |
| histories / Accounting facts | 82 / 155 | 82 / 155 | 0 |
| EJ records / transitions | 148 / 148 | 148 / 148 | 0 |
| ranges / tender breakdowns / discount breakdowns | 44 / 48 / 90 | 44 / 48 / 90 | 0 |
| fact links / requests / audits | 154 / 346 / 186 | 154 / 346 / 186 | 0 |
| replay / conflict / recovery | 2 each | 2 each | 0 |
