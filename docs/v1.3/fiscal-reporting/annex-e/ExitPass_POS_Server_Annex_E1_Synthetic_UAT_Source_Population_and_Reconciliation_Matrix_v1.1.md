# ExitPass POS Server Annex E-1 Synthetic UAT Source Population and Reconciliation Matrix v1.1

## 1. Normative expansion rule

This document defines exact planned rows without creating them. A row is the product of: the owning scenario, its object-type/ordinal identity, the closed column contract in section 2, the exact profile values in sections 3-7, and the timestamps in the main specification. Cross-references name exact governed rows. Any column not covered by those four inputs is an error; there is no implicit default.

Controlled codes are repository-governed references, not fixture-owned rows. The accepted aggregation keys are `sales_invoice`, `recorded`, `voided`, `parking_fee`, `cash`, `digital_wallet`, `vat`, `vatable`, `coupon`, `statutory_peer`, `final_payable`, `sales_invoice` sequence family, and `active` sequence state. These keys reproduce current integration-test categories and current `FiscalXReadingAggregationService` branches; they do not add a runtime category. Statutory snapshots resolve governed IDs for `SENIOR_CITIZEN`, `PWD`, `STATUTORY_DISCOUNT_ONLY`, `VAT_INCLUSIVE_NO_EXEMPTION`, `NATIONAL_LAW`, and `OPERATOR_CONSOLE`. Missing, inactive, or differently cased codes fail validation.

## 2. Closed canonical row contracts

### 2.1 Scope and chronology rows

| Table / record | Exact implementation-relevant columns |
|---|---|
| `pos.site_pos_servers` | ID `site-pos-server:0001`; code `SYN-AE1-SPS-NNN`; display `Synthetic Annex E1 Site NNN`; central refs null; time zone `Asia/Manila`; cutoff `00:00:00`; operational status null; active true; created/updated `2026-09-01T00:00:00Z`. |
| `pos.fiscal_identities` | ID `fiscal-identity:0001`; code `SYN-AE1-FI-NNN`; taxpayer and registered name `SYNTHETIC ANNEX E1 PARKING SERVICES INC.`; address `100 SYNTHETIC AVENUE, TEST CITY 0000`; TIN `000-000-000-000`; status `APPROVED`; MIN/serial `SYN-AE1-MIN-NNN` / `SYN-AE1-SERIAL-NNN`; software `ExitPass POS Server` / `1.3`; PTU/accreditation `SYN-AE1-PTU-NNN` / `SYN-AE1-ACCRED-NNN`; metadata `{}`; active true; actor `synthetic-data-steward`. |
| `pos.sales_invoice_header_profiles` | ID `header-profile:0001`; exact scope IDs; profile `SYN-AE1-HEADER-V11-NNN`; template/presentation current `digital-sales-invoice-json-v1` / `digital-sales-invoice-presentation-json-v1`; serial/MIN above; synthetic location/approval refs; effective `[2026-01-01T00:00:00Z,2027-01-01T00:00:00Z)`; `APPROVED`; approval `2026-01-01T00:00:00Z` by `synthetic-data-steward`. |
| `pos.fiscal_sequence_policies` | ID `sequence-policy:0001`; family/document controlled-code IDs; code `SYN-AE1-SI-NNN`; prefix `SYN-AE1-SI-NNN-`; suffix null; padding 4; active status; effective start `2026-01-01T00:00:00Z`; end/context null. |
| `pos.fiscal_sequence_states` | ID `sequence-state:0001`; current/reserved/issued equal the highest document ordinal in the case; state active; last transition equals final document assignment; context null. |
| `pos.fiscal_reporting_periods` | ID `period:p`; contract `f6766f48-62f0-513f-b9eb-e61c2f3e8c66`; exact scope; CLOSED code; business date/start/end/sequence from case; prior null for p=1 and `period:p-1` otherwise; opened at start; closing started end+9s; closed end+11s; actor/service refs `pos-server-annex-e1-uat-preparer`. |
| `pos.fiscal_z_close_states` | ID `z-close-state:0001`; exact scope/contract; final reset 0; final Z equals period count; final GTA from section 7; state version equals period count; last period/Z IDs point to the last rows; initialization synthetic baseline at `2026-09-01T00:00:00Z`; no destructive transition. |

### 2.2 Fiscal-document family

For document ordinal `d`, parent period is explicitly assigned in section 4. `created_at`, `updated_at`, and `fiscal_number_assigned_at` equal the document effective time. All documents use type `sales_invoice`, exact scope, sequence policy 0001, sequence value `d`, number `SI(s,d)`, series `SI-NNN`, prefix `SYN-AE1-SI-NNN-`, null suffix, business date from parent period, `document_context={}`, and active true. Channel terminal and Central PMS references are deterministic opaque UUID/reference strings from the same case; no personal content is present.

| Row | Exact columns and relationships |
|---|---|
| `pos.fiscal_documents` recorded | status `recorded`; all void columns null; number assigned by `pos-server-fiscal-document-runtime`. |
| `pos.fiscal_documents` voided | status `voided`; void status `recorded`; reason `synthetic_same_period_void`; request actor/service/correlation/operation deterministic; request time document effective+1s; voided time +2s; semantic hash from current void hasher. |
| `pos.fiscal_document_lines` | ID `document-line:d`; parent document d; sequence 1; type `parking_fee`; status null; description `Synthetic parking fee`; quantity `1.0000`; unit/gross, discount, tax, net from profile; PHP; source `SYN-AE1-LINE-NNN-dddd`; context `{}`; active true. |
| `pos.fiscal_totals` | ID `document-total:d`; parent d; type `final_payable`; amount equals line net; PHP; context `{}`. |
| `pos.fiscal_tenders` | ID `tender:t`; parent from profile; type and amount from profile; PHP; payment attempt/confirmation/finality `SYN-AE1-PAY-NNN-tttt`; provider null; context `{}`. |
| `pos.fiscal_tax_details` | ID `tax-detail:d`; parent d; line null (document level); type `vat`; classification `vatable`; taxable and tax amounts from profile; PHP; context `{}`. |
| `pos.fiscal_discount_privilege_details` | ID `discount-detail:k`; parent/line from profile; type `statutory_peer` for SC/PWD or `coupon`; basis/discount/VAT privilege from profile; PHP; beneficiary/evidence null; approval `SYN-AE1-DISCOUNT-V11`; context `{}`. |
| `pos.fiscal_document_applied_statutory_facts` | ID `statutory-fact:k`; parent SC/PWD document; decision/request/application/validation/session/Site/Site Group/tariff IDs are UUIDv5 child references; entitlement exact; benefit `STATUTORY_DISCOUNT_ONLY`; policy `NATIONAL_LAW` with `SYN-AE1-LAW-REF`; original 11200; basis 10000; VAT 1200; treatment `VAT_INCLUSIVE_NO_EXEMPTION`; discount 2000; final 9200; PHP; applied document effective-1s; channel `OPERATOR_CONSOLE`; terminal cash null; immutable timestamps equal document effective. |
| `pos.fiscal_document_status_history` | ID `status-history:h`; first row prior null/new recorded at document effective. A voided document has a second row prior recorded/new voided, reason code matching the void, at voided time. Actor/service contain synthetic references only. |

### 2.3 Report and Annex rows

| Record | Exact field contract |
|---|---|
| X/Z/BIR `pos.fiscal_report_requests` | IDs `x-request:p`, `z-request:p`, `bir-request:p`; exact period/Site; report kind X/Z/BIR; status committed; operation `SYN-AE1-NNN-X/Z/BIR-pppp`; semantic hash from current report hasher; business date; request timestamps are respectively period end minus 31 seconds, plus 9 seconds, and plus 11.5 seconds; actor/service synthetic. |
| `pos.fiscal_report_scopes` | matching `x-scope`, `z-scope`, `bir-scope`; request/period; exact-period scope controlled code; fiscal series null. |
| `pos.x_z_reports` | IDs `x-report:p`, `z-report:p`; exact request/contract/period/scope; report number `SYN-AE1-X/Z-NNN-pppp`; period identity; source counts/range; counters null for X and exact for Z; monetary columns from section 7; PHP; exact generation/commit schedule. |
| Z transition/state value | IDs `z-transition:p`, `z-transition-value:p`; prepared then applied transition under same operation; prior/result reset 0; prior/result Z p-1/p; prior/current/result GTA exact; state version p-1/p; period closes only with committed Z. |
| report children | Tender breakdown rows sort `cash`, then `digital_wallet`; discount breakdown rows sort `senior_citizen_statutory`, `pwd_statutory`, `coupon`; range row identifies sequence policy, series, first/last sequence and number, qualifying count, gap 0; all PHP. |
| `pos.bir_sales_summary_reports` | ID `bir-summary:p`; request/Z/header IDs; reference `SYN-AE1-BIR-NNN-pppp`; exact scope/date/range/header/counters and monetary values from section 7; report status committed; generated/committed schedule; semantic request hash from request. |
| `pos.annex_e1_period_accounting_facts` | ID/order/status/value/source/effective time from main specification; exact scope/business date; operation `SYN-AE1-NNN-FACT-ffff`; current hash version/hash; correction fields null except case 013 fact 0008; recorded/audit timestamps controlled; actor/service/correlation exact. |
| Annex generation/workbook/rows | Request command uses exact scope/month/profile and correction fields null. Workbook ID `annex-workbook:0001`; reference from current semantic hash; revision 1; all governed profile/template/renderer/hash fields; content metadata captured only after authorized generation. Rows `annex-row:r` contain the expected matrix fields, exact Z/BIR links, remarks `none`, and source hash; seven fact links per row in fact order. |

Current runtime creates report, workbook, Annex-row, audit, stream, and EJ record IDs with random GUIDs. The future deterministic-ID contract must permit the documented IDs only in `CONTROLLED_UAT_ISOLATED`; Production must reject it. This is an unauthorized prerequisite, not present behavior.

## 3. Exact fiscal source profiles

| Profile row | Status | Gross / discount / tax / net | Tax detail | Discount/statutory | Tender |
|---|---|---|---|---|---|
| `A-SC` document 1 | recorded | 11200 / 2000 / 1200 / 9200 | vatable 10000, VAT 1200 | SC snapshot; detail basis 10000, discount 2000, privilege 0 | cash 9200 |
| `A-PWD` document 2 | recorded | 11200 / 2000 / 1200 / 9200 | vatable 10000, VAT 1200 | PWD snapshot; detail basis 10000, discount 2000, privilege 0 | cash 9200 |
| `A-COUPON` document 3 | recorded | 11200 / 1000 / 1200 / 10200 | vatable 10000, VAT 1200 | coupon basis 11200, discount 1000, privilege 0; no statutory snapshot | cash 10200 |
| `A-VOID` document 4 | voided | 11200 / 0 / 1200 / 11200 | vatable 10000, VAT 1200 | no discount/statutory row | cash 11200, excluded from active tender aggregate |
| `B-ORDINARY` | recorded | 11200 / 0 / 1200 / 11200 | vatable 10000, VAT 1200 | no discount/statutory row | cash 11200 |
| `D-MIXED` | recorded | 22400 / 0 / 2400 / 22400 | vatable 20000, VAT 2400 | no discount/statutory row | cash 10000; digital wallet 12400 |

Every line tax amount equals its tax-detail tax amount. Every line net equals gross minus discount. Every active tender total equals line net and final total. Each statutory final equals original minus statutory discount; VAT remains in the VAT-inclusive amount because VAT privilege is zero.

## 4. Exact case population schedule

| Population | Periods | Documents by period | Lines/totals/tax | Tenders | Discounts | Statutory | X/Z/BIR | Facts | EJ | Ranges |
|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `POP-A11` | 1 | p1: A-SC 1, A-PWD 2, A-COUPON 3, A-VOID 4 | 4 each | 4 | 3 | 2 | 1 each | 7 | 8 | 2 |
| `POP-B11` | 1 | p1: B-ORDINARY 1 | 1 each | 1 | 0 | 0 | 1 each | 7 | 4 | 2 |
| `POP-BOUNDARY11` | 2 | p1: B-ORDINARY 1; p2: B-ORDINARY 2 exactly at p1 end | 2 each | 2 | 0 | 0 | 2 each | 14 | 8 | 4 |
| `POP-MONTH11` | 3 | p1: B-ORDINARY 1; p2: B-ORDINARY 2; p3: D-MIXED 3 | 3 each | 4 | 0 | 0 | 3 each | 21 | 12 | 6 |
| `POP-D11` | 1 | p1: D-MIXED 1 | 1 each | 2 | 0 | 0 | 1 each | 7 | 4 | 2 |

`POP-A11` applies to scenarios 001, 007, 010, 011, 012, 013, 014, 016, 017, 018, 019, 020, 021, 023, and 024. Case 013 adds fact 0008. Other assignments match the scenario mapping.

## 5. Exact report aggregates

| Profile | Count/range | GTA previous/current/result | Gross/net | VATable/VAT/exempt/zero | Discount breakdown | Void/refund/return/adjustment/service |
|---|---|---|---|---|---|---|
| `SP-A11` | 4; document 1..4; one gap-free range | 100000 / 28600 / 128600 | 33600 / 28600 | 30000 / 3600 / 0 / 0 | total 5000; SC 2000; PWD 2000; other 0; coupon 1000; promotion 0; VAT exemption 0 | 11200 / 0 / 0 / 0 / 0 |
| `SP-B11` | 1; one number/range | prior / 11200 / prior+11200 | 11200 / 11200 | 10000 / 1200 / 0 / 0 | all 0 | all 0 |
| `SP-D11` | 1; one number/range | prior / 22400 / prior+22400 | 22400 / 22400 | 20000 / 2400 / 0 / 0 | all 0 | all 0 |

X copies the aggregate at observed time; Z adds counters/GTA and closes the period; BIR Summary copies the committed governing Z and immutable header. `other_statutory_discount_amount_minor_units` is 0 in every row.

## 6. Electronic Journal complete contract

### 6.1 Event schedule for every row

The schedule below expands once per population instance. Thus all 148 rows are identified exactly by case, period, sequence, source ordinal, and event template.

| Population | Sequence | Period | Event type | Source and transition |
|---|---:|---:|---|---|
| `POP-A11` | 1 | 1 | `fiscal_document_committed` | document 1 / `fiscal-document:<document-uuid>` |
|  | 2 | 1 | `fiscal_document_committed` | document 2 / `fiscal-document:<document-2-uuid>` |
|  | 3 | 1 | `fiscal_document_committed` | document 3 / `fiscal-document:<document-3-uuid>` |
|  | 4 | 1 | `fiscal_document_committed` | document 4 / `fiscal-document:<document-4-uuid>` |
|  | 5 | 1 | `fiscal_document_voided` | document 4 / `fiscal-document-void:<uuid>:<void-operation>` |
|  | 6 | 1 | `x_reading_committed` | X request 1 / `fiscal-report-request:<x-request-uuid>` |
|  | 7 | 1 | `z_reading_committed` | Z request 1 / `fiscal-report-request:<z-request-uuid>` |
|  | 8 | 1 | `bir_sales_summary_committed` | BIR request 1 / `fiscal-report-request:<bir-request-uuid>` |
| `POP-B11` / `POP-D11` | 1 | 1 | `fiscal_document_committed` | document 1 |
|  | 2 | 1 | `x_reading_committed` | X request 1 |
|  | 3 | 1 | `z_reading_committed` | Z request 1 |
|  | 4 | 1 | `bir_sales_summary_committed` | BIR request 1 |
| `POP-BOUNDARY11` | 1-4 | 1 | document, X, Z, BIR | period-1 rows |
|  | 5-8 | 2 | document, X, Z, BIR | period-2 rows |
| `POP-MONTH11` | 1-4 | 1 | document, X, Z, BIR | period-1 rows |
|  | 5-8 | 2 | document, X, Z, BIR | period-2 rows |
|  | 9-12 | 3 | document, X, Z, BIR | period-3 rows |

### 6.2 Exact EJ columns

For sequence `q`, record ID is `electronic-journal-record:q`; stream ID is `electronic-journal-stream:0001`; stream sequence is integer `q` persisted as `stream_sequence_value`; there is no cross-case global sequence. Event reference is uppercase `EJ-` + stream UUID without hyphens + `-` + q as 20 digits. Status is `committed`; schema `pos-server-electronic-journal-event:v1`; chronology/integrity/semantic versions are current v1 constants; currency PHP; canonical true; sequence reference is base-10 q; journal/integrity hash references are equal; prior hash is 64 zeroes for q=1 and the prior integrity hash otherwise. Retention is `fiscal_reconstruction_hold`; retention starts at recorded time; no end exists because no destructive policy is authorized. Privacy classification is `SYNTHETIC_FISCAL_FACTS_NO_PERSONAL_DATA`.

The governed occurred timestamp is the source transition time and maps exactly to runtime column `effective_at`; the current model has no separate occurred column. Recorded time is effective time plus 100 milliseconds under the future clock contract. Actor/service/correlation are the source command's synthetic values. The causation identity is `source-transition:q` and its canonical text is stored in `source_transition_ref`; the current model has no separate causation column. Idempotency reference is the source operation. Document events bind document ID and sequence policy; report events bind report request and X/Z or BIR ID. Unused nullable IDs are null. Business date, fiscal identity, and period IDs are exact. Created/updated equal recorded time; `journal_context` is null.

For each row, the deterministic record identity is `electronic-journal-record:q`, stream identity is `electronic-journal-stream:0001`, source identity is the document/report UUID named by the event schedule, correlation identity is the owning command's `operation-request` UUID, and causation identity is `source-transition:q`. Retention policy reference is the existing active `fiscal_reconstruction_hold` controlled code. Retention start is the recorded timestamp used by eligibility evidence; it is not a separate persisted column. No retention end is specified or persisted because destructive retention remains unauthorized.

### 6.3 Exact canonical facts

| Event | Exact fact keys and values |
|---|---|
| document commit | `fiscal_document_type=sales_invoice`; exact fiscal number/series/sequence; payable amount from profile; line/tender/tax/discount/total counts; `tender_facts`, `tax_facts`, `discount_facts`, `total_facts` sorted by UUID then amount exactly as `CreateFiscalDocumentJournalFacts`; statutory rows add exact entitlement, benefit, discount 2000, VAT 1200. |
| document void | exact fiscal number/sequence; `previous_status=recorded`; `resulting_status=voided`; `void_reason_code=synthetic_same_period_void`; `void_status=recorded`. |
| X | all 19 amount keys from `ElectronicJournalReportFacts.Amounts`; exact report reference/count; tender breakdown string sorted by classification; discount breakdown string sorted by classification; one fiscal-range string with gap count 0. |
| Z | exact X facts plus previous/result reset, previous/result Z, previous/current/result GTA, resulting state version, and `period_status=closed`. |
| BIR | all 19 amount keys plus BIR ID, governing Z reference, transaction count, beginning/ending number, reset/Z, previous/result GTA. |

There are no raw requests, personal facts, or Annex-derived amounts in EJ. R10 requires exactly one Z event and one BIR event per row and zero EJ contribution to Annex money.

### 6.4 Semantic and integrity hash values

For every scheduled row, expected semantic hash `EJS(s,q)` is lowercase SHA-256 of `ElectronicJournalCanonicalizer.CanonicalSemanticText` using sections 6.1-6.3. Expected content/integrity hash `EJI(s,q)` is lowercase SHA-256 of current `ComputeIntegrityHash`, using the deterministic event reference, q, exact recorded time, `EJS(s,q)`, and q-1 hash. `journal_hash_ref`, `integrity_hash`, and compatibility content hash equal `EJI(s,q)`. The schema stores no event byte length, so byte length is explicitly `NOT_GOVERNED_BY_EJ_SCHEMA` and cannot be populated.

These functions are closed because every argument is fixed in this package; the offline validator must expand and materialize all 148 literal values and fail if any differ. Current runtime cannot produce the documented event reference/recorded time deterministically because it uses random stream IDs and `clock_timestamp()`. The future deterministic-ID and clock preconditions must be implemented and separately authorized before dataset loading. This documentation does not claim current support.

## 7. Independent reconciliation table

| Rule | `SP-A11` exact equality | `SP-B11` exact equality | `SP-D11` exact equality |
|---|---|---|---|
| R01 | 44800-0-11200=33600 | 11200=11200 | 22400=22400 |
| R02 | 1000=0+1000+0 | 0=0 | 0=0 |
| R03 | 16200=2000+2000+0+0+1000+0+11200 | 0=0 | 0=0 |
| R04 | 0=0+0+0+0+0 | 0=0 | 0=0 |
| R05 | 3600+0=3600 | 1200+0=1200 | 2400+0=2400 |
| R06 | 25000+16200+3600=44800 | 10000+0+1200=11200 | 20000+0+2400=22400 |
| R07 | 25700-500-200=25000 | 10000=10000 | 20000=20000 |
| R08 | every expected field equals Z/BIR/fact row | exact equality | exact equality |
| R09 | all source scope IDs equal case scope | exact equality | exact equality |
| R10 | 1 Z + 1 BIR event per output row; EJ amount contribution 0 | exact equality | exact equality |
| R11 | 9200+9200+10200=28600 | 11200=11200 | 10000+12400=22400 |
| R12 | 100000+28600=128600 | prior+11200=result | prior+22400=result |

## 8. Count deltas from v1.0

| Object | v1.0 | v1.1 | Delta | Exact reason |
|---|---:|---:|---:|---|
| Sites / identities / headers | 19 each | 19 each | 0 | scenario ownership unchanged |
| Periods | 22 | 22 | 0 | row count retained; empty periods replaced with active periods |
| Documents / lines / totals / tax | 35 each | 67 each | +32 each | each of 15 POP-A cases changes 2 to 4 documents (+30); cases 004/005 add one active document each (+2) |
| Tenders | 37 | 69 | +32 | one tender per added document; D mixed profile retains its second tender |
| Discount details | 30 | 45 | +15 | POP-A uses separate SC, PWD, and coupon details instead of two incompatible combined details |
| Applied statutory facts | not counted | 30 | +30 | separate SC and PWD snapshots in 15 POP-A cases |
| Status-history rows | not counted | 82 | +82 | one commit per document plus one void per POP-A case |
| X / Z / BIR | 22 each | 22 each | 0 | one per output period |
| Accounting facts | 154 | 155 | +1 | case 013 correction fact retained explicitly |
| EJ records | 116 | 148 | +32 | added document commits and replacement active-period documents |
| Fiscal ranges | 20 | 44 | +24 | cases 004/005 no longer contain empty periods and each period has one X child plus one Z child |
| Tender breakdowns | not counted | 48 | +48 | each X and Z report owns its exact tender-classification children |
| Discount breakdowns | not counted | 90 | +90 | each SP-A X and Z report owns SC, PWD, and coupon children |
| Requests / transitions / audit | not counted | 346 / 148 / 186 | new | finding 4 requires explicit operation, transition, and audit inventories; 31 Annex attempts are counted exactly |
| Replay / conflict / recovery | not counted | 2 / 2 / 2 | new | exact control records for 012/014, 013/021, and 014/024 |

## 9. Failure and cleanup boundary

Open period, uncommitted Z, missing/duplicate fact, wrong scope/currency, unsupported privilege, changed semantics, missing/tampered artifact, and clock/ID prerequisite failure must expose no new authoritative workbook. Cleanup is limited to invocation-owned unreferenced temporary material. Committed fiscal evidence, EJ, Accounting facts, reports, legal-hold material, shared data, and Production data are never deleted.
