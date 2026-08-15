# ExitPass POS Server Annex E-1 Synthetic UAT Source Population and Reconciliation Matrix v1.2

## 1. Normative expansion

A row is the unique product of scenario, registered object type/ordinal, canonical row contract, profile binding, and deterministic chronology. The [Canonical Source Row and Hash Contract](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.2.md) fixes fields, types, nullability, controlled-code resolution, relationships, roles, runtime status, transitions, requests, audits, and hash participation.

Controlled code references use exact set/key pairs. Offline UUIDs are evidence-only deterministic references; future runtime compatibility resolves active catalog identities and recomputes runtime hashes. Missing, inactive, duplicate, or differently cased code keys fail validation.

## 2. Population profiles

| Population | Periods | Documents | Lines/totals/tax | Tenders | Discounts | Statutory | X/Z/BIR | Facts | EJ | Ranges |
|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `POP-A12` | 1 | SC 1; PWD 2; coupon 3; void 4 | 4 each | 4 | 3 | 2 | 1 each | 7 | 8 | 2 |
| `POP-B12` | 1 | ordinary 1 | 1 each | 1 | 0 | 0 | 1 each | 7 | 4 | 2 |
| `POP-D12` | 1 | ordinary 1 | 1 each | 2 | 0 | 0 | 1 each | 7 | 4 | 2 |
| `POP-BOUNDARY12` | 2 | one B12 per period | 2 each | 2 | 0 | 0 | 2 each | 14 | 8 | 4 |
| `POP-MONTH12` | 3 | B12, B12, D12 | 3 each | 4 | 0 | 0 | 3 each | 21 | 12 | 6 |

Fifteen scenarios use POP-A12. Case 003 uses B12, 004 boundary, 005 month, and 009 D12.

## 3. Exact SP-A12 documents

| Document | Qty | Unit/gross | Discount | Taxable | VAT | Net/total/tender | Category | Status | Report inclusion |
|---|---:|---:|---:|---:|---:|---:|---|---|---|
| 0001 SC | 1 | 10000 | 2000 | 10000 | 1200 | 9200 | Senior Citizen statutory | recorded | X/Z/BIR/Annex active |
| 0002 PWD | 1 | 10000 | 2000 | 10000 | 1200 | 9200 | PWD statutory | recorded | X/Z/BIR/Annex active |
| 0003 coupon | 1 | 10000 | 1000 | 10000 | 1200 | 10200 | commercial coupon | recorded | X/Z/BIR/Annex active |
| 0004 void | 1 | 10000 | 0 | 10000 | 1200 | 11200 | no discount | voided same period | range and void magnitude only |

Each equation is `gross - discount + tax = net`. Active gross is 30000, active net/tenders 28600, discount 5000, VAT 3600, void 11200, D07 41200, D19 16200, and D27 21400.

## 4. B12 and D12 documents

B12: quantity 1, unit/gross/taxable 10000, discount 0, VAT 1200, net/total/cash tender 11200. D12: quantity 1, unit/gross/taxable 20000, discount 0, VAT 2400, net/total 22400, cash 10000, digital wallet 12400. Both satisfy the current creation invariant.

## 5. Report snapshots

| Profile | Qualifying docs | First/last sequence | Previous/current/resulting GTA | Gross/net | VATable/VAT | Discounts | Void/refund/return/adjustment/service |
|---|---:|---|---|---|---|---|---|
| A12 | 4 | 1/4 | 100000/28600/128600 | 30000/28600 | 30000/3600 | total 5000; SC 2000; PWD 2000; coupon 1000 | 11200/0/0/0/0 |
| B12 | 1 | assigned ordinal | prior/11200/prior+11200 | 10000/11200 | 10000/1200 | all 0 | all 0 |
| D12 | 1 | assigned ordinal | prior/22400/prior+22400 | 20000/22400 | 20000/2400 | all 0 | all 0 |

X and Z each own one range per period. A12 range includes sequences 1-4 and zero gaps because a voided document remains a qualifying numbered document. A12 tender breakdown is cash count 3 amount 28600. Discount breakdowns are coupon 1/1000/0, PWD 1/2000/0, and SC 1/2000/0. D12 has cash 1/10000 and digital_wallet 1/12400.

## 6. Accounting facts

Each period has seven facts ordered by the main specification. A12 Manual 500 and overflow 200 are `recorded`; every other fact is `attested_zero`. B12/D12 all seven are `attested_zero`. Case 013 adds Manual correction 600 superseding fact 0001. Every fact has exact in-period effective time, exact source references, exact approval `SYN-AE1-ACCOUNTING-V12`, exact semantic payload, and hash under the canonical contract.

## 7. Electronic Journal schedule

| Population | Sequence | Event |
|---|---:|---|
| A12 | 1-4 | fiscal_document_committed for documents 1-4 |
| A12 | 5 | fiscal_document_voided for document 4 |
| A12 | 6 | x_reading_committed |
| A12 | 7 | z_reading_committed |
| A12 | 8 | bir_sales_summary_committed |
| B12/D12 | 1 | fiscal_document_committed |
| B12/D12 | 2 | x_reading_committed |
| B12/D12 | 3 | z_reading_committed |
| B12/D12 | 4 | bir_sales_summary_committed |

Multi-period cases continue one Site/fiscal-identity/currency stream, yielding sequence 1-8 for case 004 and 1-12 for case 005. The canonical contract enumerates all 148 rows and hashes.

## 8. R01-R12 proof

| Rule | A12 equality | B12 equality | D12 equality |
|---|---|---|---|
| R01 | 41200-0-11200=30000 | 10000=10000 | 20000=20000 |
| R02 | 1000=0+1000+0 | 0=0 | 0=0 |
| R03 | 16200=2000+2000+0+0+1000+0+11200 | 0=0 | 0=0 |
| R04 | 0=0+0+0+0+0 | 0=0 | 0=0 |
| R05 | 3600+0=3600 | 1200=1200 | 2400=2400 |
| R06 | 21400+16200+3600=41200 | 8800+0+1200=10000 | 17600+0+2400=20000 |
| R07 | 22100-500-200=21400 | 8800=8800 | 17600=17600 |
| R08 | all copied operands exact | all copied operands exact | all copied operands exact |
| R09 | scope/currency/period IDs identical | identical | identical |
| R10 | one Z and one BIR event per period | one each | one each |
| R11 | 9200+9200+10200=28600 | 11200=11200 | 10000+12400=22400 |
| R12 | 100000+28600=128600 | prior+11200=result | prior+22400=result |

## 9. Counts and deltas

| Object | v1.0 | v1.1 | v1.2 | v1.0-to-v1.2 reason |
|---|---:|---:|---:|---|
| Sites / identities / headers | 19 each | 19 each | 19 each | unchanged |
| Workbooks | not governed | 19 | 19 | explicit v1.1 inventory retained |
| Periods / X / Z / BIR / Annex rows | 22 each | 22 each | 22 each | unchanged |
| Documents / lines / totals / tax | 35 each | 67 each | 67 each | +32 from supportable separate source documents |
| Tenders | 37 | 69 | 69 | +32 matching corrected documents |
| Discounts | 30 | 45 | 45 | +15 separate coupon rows |
| Statutory facts | not governed | 30 | 30 | explicit SC/PWD snapshots |
| Status histories | not governed | 82 | 82 | commit plus 15 void transitions |
| Accounting facts | 154 | 155 | 155 | case 013 correction fact |
| EJ records / transitions | 116 / not governed | 148 / 148 | 148 / 148 | +32 records; transitions made explicit |
| Fiscal ranges | 20 | 44 | 44 | X and Z child per period |
| Tender / discount breakdowns | not governed | 48 / 90 | 48 / 90 | explicit report children |
| Annex fact links | not governed | 154 | 154 | seven per row |
| Requests / audits | not governed | 346 / 186 | 346 / 186 | explicit operation/evidence rows |
| Replay / conflict / recovery | not governed | 2 each | 2 each | explicit evidence rows |

Every v1.1-to-v1.2 count delta is zero. Version 1.2 changes source amounts, expected values, role/status labels, transition bindings, and hashes only.

