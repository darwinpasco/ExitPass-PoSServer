# ExitPass POS Server Annex E-1 Synthetic UAT Source Population and Reconciliation Matrix v1.0

## 1. Source authority

This document specifies planned rows only. A later authorized implementation must use current repository APIs and canonical objects; direct SQL shortcuts may be used only by an authorized database fixture task when they preserve all constraints and event atomicity.

Financial authority is `pos.bir_sales_summary_reports` bound to committed Z rows in `pos.x_z_reports` and CLOSED `pos.fiscal_reporting_periods`. Header sources are `pos.fiscal_identities`, `pos.site_pos_servers`, and the immutable BIR header snapshot. `pos.annex_e1_period_accounting_facts` supplies the seven approved first-class facts. `pos.electronic_journal_records` supplies traceability only.

## 2. Exact common rows

For each case, UUID columns use the UUIDv5 rule and canonical object names from the main specification. The literal object name is the source record identifier; the UUIDv5 output is its exact database UUID.

| Object | Exact planned values |
|---|---|
| `pos.site_pos_servers` | code `SYN-AE1-SPS-01`; synthetic Site parent; active; no external endpoint |
| `pos.fiscal_identities` | code `SYN-AE1-FI-01`; registered name/address/TIN from `HP-01`; PHP fiscal scope |
| `pos.sales_invoice_header_profiles` | version `SYN-AE1-HEADER-V1`; serial `SYN-AE1-SERIAL-01`; MIN `SYN-AE1-MIN-01`; synthetic accreditation/PTU references `SYN-AE1-ACCRED-01` / `SYN-AE1-PTU-01`; dates `2026-01-01` through `2026-12-31` |
| `pos.fiscal_z_close_states` | initial reset `0`; initial Z `0`; initial GTA is profile-specific (`100000` for `SP-A`, otherwise `0`) |
| Report scope | `Asia/Manila`; cutoff `00:00:00`; currency `PHP`; contract/profile versions already governed by Z-010/Z-012B |

Synthetic accreditation and PTU references are fixture-only opaque strings and must never be represented as government-issued values.

## 3. Exact period and document populations

### 3.1 `POP-A`

| Record | Identity suffix | Status/time | Exact values and relationships |
|---|---|---|---|
| Fiscal period | `period\|0001` | CLOSED; 2026-09-09T16:00:00Z to 2026-09-10T16:00:00Z; business date 2026-09-10; sequence 1 | owns all following rows |
| Active fiscal document | `fiscal-document\|0001` | committed at 2026-09-10T01:00:01Z; sequence 1; `SYN-AE1-SI-000001` | gross 16000; final fiscal/tender amount 13000; VATable 10000; VAT 2000; VAT-exempt 1000; SC/PWD/other-statutory discounts 1000 each; SC/PWD VAT exemption 300/200 |
| Voided fiscal document | `fiscal-document\|0002` | committed at 01:00:02Z then governed same-period void at 01:00:03Z; sequence 2; `SYN-AE1-SI-000002` | original gross/final amount 1000; void magnitude 1000; excluded from active net/tender reconciliation but retained in range and D18 |
| Lines/totals/tax | matching document ordinals | immutable with parent | one line, one total, and one tax row per document; amounts equal their parent split above |
| Discounts | `discount-detail\|0001..0002` | committed with active document | SC detail 1000 with VAT exemption 300; PWD detail 1000 with VAT exemption 200; other statutory 1000 is the BIR Summary named aggregate and does not create an unapproved privilege identity |
| Tenders | `tender\|0001..0002` | committed with documents | active cash 13000; voided-document cash 1000; included tender sum is exactly 13000 |
| X Reading | `x-report\|0001` | committed 2026-09-10T16:00:05Z | observation before close; same source aggregates; does not close period |
| Z Reading | `z-report\|0001` | generated 16:00:10Z; committed 16:00:11Z; report `SYN-AE1-Z-001`; reset 0; Z 1 | previous GTA 100000; current GTA 13000; present GTA 113000; one range, gap count 0; source aggregates below |
| BIR Summary | `bir-summary\|0001` | generated 16:00:12Z; committed 16:00:13Z; `SYN-AE1-BIR-001` | transaction count 2; range SI-000001..000002; previous/present GTA 100000/113000; gross 16000; net 13000; VATable 10000; VAT 2000; exempt 1000; zero-rated 0; discount total 3000; SC/PWD/other 1000/1000/1000; VAT exemption 500; void 1000; refund/return/adjustment 0 |
| Accounting facts | `accounting-fact\|0001..0007` in lexical fact-type order | effective 16:00:00Z; recorded 16:00:14Z onward | Manual recorded 500 count 1 refs `SYN-AE1-MANUAL-001`; overflow recorded 200 count 1 refs/event `SYN-AE1-OVERFLOW-001`; NAAC, Solo, other VAT, VAT returns, residual VAT each attested zero with count 0 and null source refs |
| EJ | `electronic-journal-record\|0001..0006` | committed sequence 1..6 | document commit 1, document commit 2, document void, X commit, Z commit, BIR commit; each predecessor hash binds sequence; Z/BIR refs match above |

### 3.2 `POP-B`

One CLOSED period and one committed document. Document reference is selected by the row profile, gross/final/tender `11200`, VATable `10000`, VAT `1200`, all other monetary values zero. Z/BIR transaction count 1, one gap-free range, previous GTA `0`, current/present GTA `11200`, reset `0`, and the row-profile Z counter. All seven facts are attested zero. EJ sequence is document, X, Z, BIR.

For `AE1-UAT-003`, the document effective time is exactly `2026-09-09T16:00:00Z`; all other commit times retain their declared order. For the second period of `POP-BOUNDARY`, document effective time is exactly `2026-09-10T16:00:00Z`.

### 3.3 `POP-BOUNDARY`

Period 1 is 2026-09-10, sequence 1, CLOSED, no activity, previous/current/present GTA `0/0/0`, reset/Z `0/1`, no documents/ranges, one X, one Z, one BIR Summary, seven zero attestations, and EJ X/Z/BIR sequences 1..3. Period 2 starts exactly at period 1 end, is business date 2026-09-11, sequence 2, CLOSED, and uses `POP-B` with reset/Z `0/2`; EJ document/X/Z/BIR sequences 4..7. The document exists only in period 2.

### 3.4 `POP-MONTH`

| Period | Window / sequence | Source profile | GTA previous/current/present | Reset/Z | EJ sequence |
|---|---|---|---|---|---|
| 1 | 2026-09-09T16:00Z..10T16:00Z / 1 | `SP-B`; SI-000201 | 0 / 11200 / 11200 | 0 / 1 | document, X, Z, BIR = 1..4 |
| 2 | 2026-09-10T16:00Z..11T16:00Z / 2 | `SP-C`; no range | 11200 / 0 / 11200 | 0 / 2 | X, Z, BIR = 5..7 |
| 3 | 2026-09-11T16:00Z..12T16:00Z / 3 | `SP-D`; SI-000202 | 11200 / 22400 / 33600 | 0 / 3 | document, X, Z, BIR = 8..11 |

### 3.5 `POP-D`

One CLOSED period with one committed document `SYN-AE1-SI-000301`: gross/net `22400`, VATable `20000`, VAT `2400`, cash tender `10000`, `qr_ph` tender `12400`, all other monetary fields zero. Z/BIR previous/current/present GTA `0/22400/22400`, transaction count 1, reset/Z `0/1`, one range and zero gaps. All seven facts are attested zero. EJ is document/X/Z/BIR sequence 1..4.

## 4. Case population assignment

`POP-A`: DS-AE1-001, 007, 010, 011, 012, 013, 014, 016, 017, 018, 019, 020, 021, 023, 024. `POP-B`: DS-AE1-003. `POP-BOUNDARY`: DS-AE1-004. `POP-MONTH`: DS-AE1-005. `POP-D`: DS-AE1-009.

The per-case identity replaces the scenario segment in every canonical UUIDv5 name; all amounts and relationships remain exact. DS-AE1-013 adds `accounting-fact|0008`, Manual 600, status `recorded`, count 1, ref `SYN-AE1-MANUAL-001-C01`, `supersedes_fact_id` pointing to the Manual fact, correction reason `source_correction`, recorded `2026-09-30T09:59:00Z`. It does not authorize a replacement workbook.

## 5. Exact source-to-output relationships

| Annex positions | Authoritative planned source |
|---|---|
| H01-H03 | immutable fiscal identity snapshot |
| H04-H05 | Z-012B contract constants |
| H06-H07 | committed BIR Summary header snapshot |
| H08 | Site POS Server code, provisional under AE-DR-020A |
| H09-H10 | fixed generation clock and service principal |
| D01-D05, D08-D15, D17-D18, D20-D24, D30-D31 | committed Z/BIR/fact values exactly listed here |
| D06 | current Manual SI/OR fact |
| D07 | BIR gross plus returns plus voids |
| D16 | BIR other statutory plus Z coupon plus Z promotional |
| D19, D25-D27, D29 | approved calculation profile equations |
| D28 | current overflow fact |
| D32 | `NO_ACTIVITY` only when transaction count 0; otherwise `NONE` |

## 6. Exact reconciliation results

| Profile | R01 | R02 | R03 | R04 | R05 | R06 | R07 | R11 | R12 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| SP-A | 16000 | 1000 | 4000 | 500 | 2000 | 17000 | 11000 | 13000 | 113000 = 100000 + 13000 |
| SP-B | 11200 | 0 | 0 | 0 | 1200 | 11200 | 10000 | 11200 | 11200 = 0 + 11200 |
| SP-C boundary | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 = 0 + 0 |
| SP-C month | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 11200 = 11200 + 0 |
| SP-D single | 22400 | 0 | 0 | 0 | 2400 | 22400 | 20000 | 22400 | 22400 = 0 + 22400 |
| SP-D month | 22400 | 0 | 0 | 0 | 2400 | 22400 | 20000 | 22400 | 33600 = 11200 + 22400 |

R08 requires equality for every copied operand. R09 requires exact scope identity. R10 requires integrity-valid EJ refs and zero EJ-derived amounts. Every arithmetic difference is exactly zero minor units.

## 7. Failure and cleanup boundaries

Open-period, uncommitted-Z, missing-fact, duplicate-current-fact, wrong currency, unresolved nonzero privilege, altered artifact, and changed-semantic cases fail before a new authoritative workbook is exposed. Cleanup removes only resources labeled with the case/run identity. No cleanup may delete committed fiscal evidence, legal-hold material, shared data, or Production data. AE-DR-016B remains unresolved.
