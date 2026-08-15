# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.1

## 1. Document control

| Item | Value |
|---|---|
| Specification ID | `annex-e1-synthetic-uat-dataset:v1.1` |
| Version | `v1.1` |
| Creation baseline | `d16f23cef27d87f451ef08ff9f74eb4afe800c8f` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile | `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Specification outcome | `READY_FOR_V1_1_SPECIFICATION_AUTHORIZATION_REVIEW` |
| Canonical implementation gate | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Dataset / validator implementation | `NOT_AUTHORIZED` / `NOT_AUTHORIZED` |
| Loading / execution / workbook generation | `NOT_AUTHORIZED` / `NOT_AUTHORIZED` / `NOT_AUTHORIZED` |
| Delivery / BIR submission / Production | `NOT_AUTHORIZED` / `NOT_AUTHORIZED` / `NOT_AUTHORIZED` |

The merged v1.0 package remains the immutable historical version reviewed as blocked. This v1.1 package supersedes v1.0 only for future authorization and implementation consideration. It does not change the v1.0 review result retroactively. A separate authorization review of v1.1 is mandatory before dataset or validator implementation.

## 2. Scope and scenario partition

The included scenarios are exactly `AE1-UAT-001`, `003`, `004`, `005`, `007`, `009`, `010`, `011`, `012`, `013`, `014`, `016`, `017`, `018`, `019`, `020`, `021`, `023`, and `024`. The excluded scenarios are exactly `AE1-UAT-002`, `006`, `008`, `015`, `022`, and `025`. The sets are disjoint and their union is the 25-scenario catalogue.

Each included scenario owns one case named `DS-AE1-<three-digit scenario number>`. Every case is isolated and owns its Site POS Server, fiscal identity, sequence state, periods, source facts, reports, stream, expected output, and evidence identities. No shared population can cause an excluded scenario to execute. The package specifies exactly 22 expected Annex E-1 control or output rows.

## 3. Canonical deterministic identity contract

### 3.1 Algorithm and grammar

The namespace is UUID `ae1d5e7a-7e2d-5c4d-9a11-202608140001`. IDs use UUIDv5 under RFC 4122 section 4.3: namespace bytes in network order, SHA-1 name digest, RFC variant, and version 5 bits. The canonical name grammar is:

```text
annex-e1-synthetic-uat:v1.1|AE1-UAT-NNN|object-type|OOOO
```

`NNN` and `OOOO` are ASCII decimal fields of width three and four. Scenario IDs are uppercase and must match `AE1-UAT-[0-9]{3}`. Object types are lowercase ASCII tokens from section 3.2. Ordinals start at `0001`, contain leading zeroes, and cannot exceed the registry maximum for their owner case.

The name is Unicode NFC, encoded as UTF-8 without BOM. Case is significant. Leading or trailing whitespace is prohibited. CR, LF, NUL, and vertical bar are prohibited in every segment, so no escaping is permitted or required. The only delimiters are the three literal ASCII vertical bars shown. Missing, null, and empty segments are invalid. The resulting UUID is lowercase hyphenated text when serialized.

The dataset case itself is `object-type=dataset-case`, ordinal `0001`; no `COMMON` owner or case-ID substitution is allowed. A parent ID is the UUID for the parent registry entry under the same scenario. Replay reuses the original operation UUID. A changed-semantic conflict reuses that operation UUID but never creates a new authoritative object. Recovery evidence has its own `recovery-record` identity. Correction facts use the next `accounting-fact` ordinal and point to the prior fact; correction workbooks are prohibited because `AE1-UAT-015` remains excluded.

### 3.2 Closed object-type and ordinal registry

| Object type | Parent | Per-case ordinal rule | Package count | Maximum ordinal in one case |
|---|---|---|---:|---:|
| `dataset-case` | none | `0001` | 19 | 1 |
| `site-pos-server` | case | `0001` | 19 | 1 |
| `fiscal-identity` | case | `0001` | 19 | 1 |
| `header-profile` | fiscal identity | `0001` | 19 | 1 |
| `sequence-policy`, `sequence-state`, `z-close-state` | Site POS Server | `0001` | 19 each | 1 |
| `period` | case | chronological order | 22 | 3 |
| `fiscal-document`, `document-line`, `document-total`, `tax-detail` | period/document | fiscal order | 67 each | 4 |
| `tender` | fiscal document | document then tender order | 69 | 4 |
| `discount-detail` | fiscal document | document then detail order | 45 | 3 |
| `statutory-fact` | fiscal document | statutory document order | 30 | 2 |
| `status-history` | fiscal document | commit then void | 82 | 5 |
| `fiscal-range` | X or Z report | report order then period order | 44 | 6 |
| `x-request`, `x-scope`, `x-report` | period | period order | 22 each | 3 |
| `z-request`, `z-scope`, `z-report` | period | period order | 22 each | 3 |
| `z-transition`, `z-transition-value` | Z request | period order | 22 each | 3 |
| `bir-request`, `bir-scope`, `bir-summary` | Z report | period order | 22 each | 3 |
| `tender-breakdown` | X or Z report | report order then classification | 48 | 8 |
| `discount-breakdown` | X or Z report | report order then classification | 90 | 6 |
| `accounting-fact` | period | fact order in section 8.2 | 155 | 22 |
| `electronic-journal-stream` | case | `0001` | 19 | 1 |
| `electronic-journal-record` | stream | stream sequence | 148 | 12 |
| `annex-generation-request` | case | attempt order | 31 | 8 |
| `annex-workbook` | case | committed revision | 19 | 1 |
| `annex-row` | workbook | row order | 22 | 3 |
| `annex-fact-source` | Annex row | row then fact order | 154 | 21 |
| `operation-request` | case | section 10 request order | 346 | 28 |
| `source-transition` | case | EJ source order | 148 | 12 |
| `audit-record` | case | chronological audit order | 186 | 16 |
| `replay-record` | case | replay attempt order | 2 | 1 |
| `conflict-record` | case | conflict attempt order | 2 | 1 |
| `recovery-record` | case | recovery boundary order | 2 | 1 |
| `evidence-record` | case | scenario evidence order | 57 | 3 |

Every row and artifact named by the v1.1 package uses exactly one registry entry. Aliases, random IDs, reuse across object types, ordinal zero, and an ordinal above the case maximum are prohibited. An implementation must generate all documented names, verify package-wide UUID uniqueness before writing an artifact, and fail on collision with any pre-existing identifier.

Controlled codes are not fixture-owned rows and do not receive case UUIDs. The future implementation must resolve active repository-governed codes by exact set/key pairs and must not insert aliases or duplicate catalog rows. Required fiscal source keys are: document type `sales_invoice`; document statuses `recorded` and `voided`; line type `parking_fee`; tender types `cash` and `digital_wallet`; tax classification `vatable`; discount types `statutory_peer` and `coupon`; total type `final_payable`; fiscal sequence family `sales_invoice`; sequence state `active`; period status `closed`; report status `committed`; Annex fact statuses `recorded` and `attested_zero`; correction reason `source_correction`; Annex fact types are the seven exact keys in section 8.2; Annex remarks are `none`. Statutory policy values are the existing exact uppercase values listed in the source matrix. A missing or inactive code fails fixture validation.

### 3.3 UUID test vectors and v1.0 comparison

| Canonical name | Expected UUIDv5 |
|---|---|
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|period\|0001` | `e1f31225-7457-5573-a8f2-1250e7382ee5` |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|fiscal-document\|0001` | `192219dd-a7c9-5a53-adb8-ee1342aff771` |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-013\|accounting-fact\|0008` | `e0602e8f-8662-5a63-8313-25672d017b8e` |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|dataset-case\|0001` | `8c4c08e5-cc95-5d96-8410-17c8e18d69a7` |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|electronic-journal-stream\|0001` | `3d2facd8-f877-512b-99c4-d8b9f89269cb` |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|electronic-journal-record\|0001` | `df9a01b4-07e5-5e4e-b4bf-b9b7c824b061` |

The v1.0 input `annex-e1-synthetic-uat:v1.0|AE1-UAT-001|period|0001` produced `0fe9c9ae-80bc-56d1-be36-f0abbdc23bbf`; v1.1 produces `e1f31225-7457-5573-a8f2-1250e7382ee5`. Every v1.1 UUID changes because the version segment is part of the semantic identity. No v1.0 executable dataset or database identity exists, so this change mutates no runtime record. A v1.0 UUID cannot be reused for a v1.1 object.

## 4. Semantic-hash contracts

### 4.1 Current Annex fact hash

`pos-server-annex-e1-period-fact:sha256:v1` is SHA-256 over the exact no-BOM UTF-8 JSON emitted by `AnnexE1SemanticHasher.ComputeFact`. Property order is: `version`, `sitePosServerId`, `fiscalIdentityId`, `currencyCode`, `fiscalReportingPeriodId`, `factType`, `factStatus`, `amountMinorUnits`, `sourceDocumentCount`, `firstSourceReference`, `lastSourceReference`, `sourceEventReference`, `approvalReference`, `supersedesFactId`, `correctionReason`. GUIDs are lowercase `D`; integers are unquoted base-10; nullable properties are always present and use JSON `null`; strings are JSON escaped without Unicode normalization by the runtime. Operation key, actor, service identity, correlation, effective time, and recorded time are excluded.

The normal `AE1-UAT-001` Manual fact canonical payload is:

```text
{"version":"pos-server-annex-e1-period-fact:sha256:v1","sitePosServerId":"1f3884b8-a7d5-582f-93d3-89dacbb76bf9","fiscalIdentityId":"f7e7f0db-19bc-50a2-adc4-458036993e38","currencyCode":"PHP","fiscalReportingPeriodId":"e1f31225-7457-5573-a8f2-1250e7382ee5","factType":"manual_si_or_net_income","factStatus":"recorded","amountMinorUnits":500,"sourceDocumentCount":1,"firstSourceReference":"SYN-AE1-MANUAL-001","lastSourceReference":"SYN-AE1-MANUAL-001","sourceEventReference":null,"approvalReference":"SYN-AE1-ACCOUNTING-V11","supersedesFactId":null,"correctionReason":null}
```

Its hash is `4ef18d69421a1e8a289f4bb7327c1b3da86dc260b38bb6c50119ed7e31418c4d`. Exact replay produces the same hash. Changing only `amountMinorUnits` to `501` produces `5f1f028745d3e2205efc1f339befb98c9e06bf1fb3a72d9013037ceb738b5159` and is a conflict under the same operation key. Omitting `sourceEventReference` instead of writing null produces `f32c10c4982811e357230d439a35eabc5281a4739ff869111d23f342635bd0c7`; omission is therefore not replay-equivalent and is prohibited.

### 4.2 Current workbook hash

`pos-server-annex-e1-request:sha256:v1` uses the exact property order in `AnnexE1SemanticHasher.ComputeWorkbook`. The root inventory is `version`, scope IDs, currency, year, month, profile, calculation profile and hash, template hash, renderer, correction fields, header object, and sources array. The header order is taxpayer name/address/TIN, software name/version, release number/date, serial, MIN, and terminal. `GeneratedAt` and `GeneratedByRef` are excluded.

Sources are ordered by `fiscal_reporting_periods.period_sequence`. Each source property order is period ID/sequence, Z ID, BIR ID/hash, business date, `factIds`, and `factSemanticHashes`. Both nested arrays use the required fact order in section 8.2; the stable key is fact-type ordinal, and fact UUID is the tie-breaker. Duplicate facts are invalid. The runtime does not sort these arrays, so an implementation must sort before invoking the hasher. Empty sources serialize as `[]` but generation rejects them before authoritative output. A source object is never null.

The exact normal one-source `AE1-UAT-001` canonical payload is this single UTF-8 line with no BOM or trailing LF:

```text
{"version":"pos-server-annex-e1-request:sha256:v1","sitePosServerId":"1f3884b8-a7d5-582f-93d3-89dacbb76bf9","fiscalIdentityId":"f7e7f0db-19bc-50a2-adc4-458036993e38","currencyCode":"PHP","calendarYear":2026,"calendarMonth":9,"profile":"pos-server-bir-annex-e1-rmo24-2023:v1","calculationProfile":"pos-server-annex-e1-accounting-calculation:v1","calculationProfileSha256":"36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f","templateSha256":"7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197","rendererVersion":"pos-server-annex-e1-openxml-renderer:v1","supersedesWorkbookId":null,"correctionReason":null,"correctionApprovalReference":null,"header":{"taxpayerName":"SYNTHETIC ANNEX E1 PARKING SERVICES INC.","taxpayerAddress":"100 SYNTHETIC AVENUE, TEST CITY 0000","tin":"000-000-000-000","softwareName":"ExitPass POS Server","softwareVersion":"1.3","releaseNumber":"Z-012B","releaseDate":"2026-08-10","posSerialNumber":"SYN-AE1-SERIAL-001","machineIdentificationNumber":"SYN-AE1-MIN-001","posTerminalNumber":"SYN-AE1-SPS-001"},"sources":[{"periodId":"e1f31225-7457-5573-a8f2-1250e7382ee5","periodSequence":1,"governingZReportId":"f488eca0-0973-52ce-b761-c251dcac1cdb","birSalesSummaryReportId":"742033a8-6dca-5804-95d9-be97b7e9061d","birSalesSummarySemanticHash":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","businessDayDate":"2026-09-10","factIds":["33e0a390-457d-5555-b962-f5bad0de3788","fbaea14b-98b8-5d56-9c18-daf030e92703","828fb4e8-e185-5d3b-b4f4-0e6fa2f930eb","6d7bacd1-501b-5d36-a2a6-e4bc266b0d41","971a7739-4a9c-572e-9787-9fd53645654a","455dffb3-0f43-5fae-963f-81da7b32f8de","1618744a-80c2-5aeb-8fac-6af48ffffac4"],"factSemanticHashes":["1111111111111111111111111111111111111111111111111111111111111111","2222222222222222222222222222222222222222222222222222222222222222","3333333333333333333333333333333333333333333333333333333333333333","4444444444444444444444444444444444444444444444444444444444444444","5555555555555555555555555555555555555555555555555555555555555555","6666666666666666666666666666666666666666666666666666666666666666","7777777777777777777777777777777777777777777777777777777777777777"]}]}
```

Its hash is `2e88439b86615f33934026243d57c0f3b2da2aa4ea1aaaaf66458cf37bc10d95`; exact replay has the same bytes and hash. Replacing `sources` with `[]` produces `7616f56c09787d7944a0d80f3026c588243dd42c50222e862120a9cc226991c9` but generation must reject it. Reordering an array is material: UTF-8 `{"values":["a","b"]}` hashes to `207571055a4dc87f9c27edd8f935f23fee19c6c3c1e0b96fe16369e7ac32ee89`, while `{"values":["b","a"]}` hashes to `ff26eaa80429b6e71e09658838120e216c1cb5ad60ecae64501ecfc9133fb17c`.

### 4.3 Dataset package hash

The offline dataset semantic profile is `annex-e1-synthetic-uat-dataset:sha256:v1.1`. Its canonical payload is an LF-terminated sequence of `key=value` records in this fixed order: specification ID, case ID, scenario ID, Site UUID, fiscal identity UUID, ordered period UUIDs, ordered source-row identity/hash pairs, ordered EJ semantic/integrity hash pairs, ordered H01-H10 values, ordered row/D01-D32 values, expected outcomes, and expected evidence IDs. UTF-8 has no BOM. Strings are NFC and escaped as JSON strings; integers are base-10; booleans are `true`/`false`; timestamps are UTC `yyyy-MM-ddTHH:mm:ss.fffffffZ`; null is `null`; empty string is `""`; empty collection is `[]`. Missing keys are invalid. Nested arrays sort by the registry ordinal, then UUID text. Duplicate sort keys are invalid. SHA-256 output is lowercase hexadecimal.

Current runtime hashes remain authoritative for runtime objects. The package hash does not replace or recalculate a runtime hash.

## 5. Scope, chronology, and deterministic clock contract

Each case owns one synthetic Site POS Server and fiscal identity. Display codes are `SYN-AE1-SPS-NNN` and `SYN-AE1-FI-NNN`; serial and MIN are `SYN-AE1-SERIAL-NNN` and `SYN-AE1-MIN-NNN`. Currency is `PHP`, time zone `Asia/Manila`, and cutoff `00:00:00`.

Default period 1 is `[2026-09-09T16:00:00.0000000Z, 2026-09-10T16:00:00.0000000Z)`, business date `2026-09-10`, sequence 1. Later periods are adjacent 24-hour half-open windows. A document request occurs one second before its document; documents occur at period start plus `09:00:00` plus their zero-based ordinal in seconds, except the boundary cases stated in the mapping. The X request, generation and commit instants are period end minus 31, 30 and 29 seconds. The Z request, generation and commit instants are period end plus 9, 10 and 11 seconds. The BIR request, generation and commit instants are period end plus 11.5, 12 and 13 seconds. Accounting facts are effective at period end minus 10 seconds plus fact ordinal milliseconds, and recorded at period end plus 14 seconds plus fact ordinal milliseconds. Annex generation uses `2026-09-30T10:00:00.0000000Z`. Therefore every fact satisfies `period_start_at <= effective_at < period_end_at`; no fact uses the exclusive end.

The future test-clock contract is `pos-server-controlled-test-clock:v1`. It is a documented prerequisite, not an implemented capability. It must be accepted only when host environment is exactly `CONTROLLED_UAT_ISOLATED`, configuration names the case UUID and fixed UTC instant, startup includes an explicit operator acknowledgement, and Production hosting rejects startup. It must govern all application `UtcNow`/`Guid.NewGuid` consumption and PostgreSQL `clock_timestamp()`/`CURRENT_TIMESTAMP` consumption in one serial case. PostgreSQL must use a transaction-local approved clock function; direct database clock reads outside that boundary fail closed. Concurrency is disabled except in the concurrency-specific case, whose schedule provides one ordered instant per worker.

The controlled clock must cover fiscal assignment, void, report request/generation/commit, period close, Accounting fact record, EJ record, workbook generation/commit, audit, and evidence timestamps. It must not change fiscal business dates or source-provided effective times. The case startup record captures environment label, case ID, fixed schedule hash, operator acknowledgement reference, and repository commit. Cleanup removes the override and proves the next process observes the normal clock. Replay reuses stored timestamps and bytes. An unrecognized override, missing acknowledgement, Production host, clock rollback, schedule exhaustion, or concurrent duplicate instant fails startup or the operation without persistence.

Current runtime support is `ABSENT`: `PostgresAnnexE1Repository`, `PostgresElectronicJournalWriter`, X/Z/BIR repositories, and fiscal-document void paths use live clocks and random GUIDs. Implementing this contract is not authorized by v1.1. H09, runtime-generated IDs, recorded/audit timestamps, and EJ integrity hashes require that separately reviewed prerequisite. Fiscal source amounts, business dates, effective times, H01-H08/H10, D01-D32, and runtime semantic hashes that exclude clocks are specified independently.

## 6. Exact supported monetary profiles

All amounts are signed 64-bit PHP minor units and use checked integer arithmetic. Workbook presentation divides by 100 with two digits; no input is rounded in the Annex projection.

| Profile | Exact active source composition | Exact aggregate |
|---|---|---|
| `SP-A11` | SC document: gross 11200, statutory discount 2000, VATable basis 10000, VAT 1200, final/tender 9200, VAT privilege 0. PWD document: identical amounts with PWD entitlement. Coupon document: gross 11200, coupon 1000, VATable 10000, VAT 1200, final/tender 10200. Voided document: original line net 11200 and void magnitude 11200. | active gross 33600; active net/tenders/GTA contribution 28600; VATable 30000; VAT 3600; VAT-exempt and zero-rated 0; SC 2000; PWD 2000; other statutory 0; coupon 1000; promotional 0; void 11200; SC/PWD VAT adjustment 0. |
| `SP-B11` | One recorded ordinary VAT Sales Invoice: gross/net/tender 11200, VATable 10000, VAT 1200. | every discount, exception, Manual, overflow, and unsupported privilege amount 0. |
| `SP-D11` | One recorded ordinary VAT Sales Invoice: gross/net 22400, VATable 20000, VAT 2400; cash 10000 and digital wallet 12400. | every discount, exception, Manual, overflow, and unsupported privilege amount 0. |

`SP-A11` uses one statutory snapshot per statutory document. The snapshots use `SENIOR_CITIZEN` and `PWD`, benefit `STATUTORY_DISCOUNT_ONLY`, VAT treatment `VAT_INCLUSIVE_NO_EXEMPTION`, discount 2000, VAT amount 1200, and final 9200. The matching discount detail has discount 2000 and VAT privilege 0. This is compatible with the one-snapshot-per-document constraint and current aggregation. `otherStatutoryDiscount` is exactly zero because current aggregation has no source that increments it. D16 is supplied only by the supported coupon amount 1000. No unsupported statutory category is introduced.

The exact `SP-A11` Annex results are D07 44800, D16 1000, D19 16200, D25 0, D26 3600, D27 25000, D29 25700, and GTA contribution 28600. Manual D06 is 500 and overflow D28 is 200. All other Accounting fact amounts are attested zero.

## 7. Expected header contract

| Position | Exact value rule |
|---|---|
| H01 | `SYNTHETIC ANNEX E1 PARKING SERVICES INC.` |
| H02 | `100 SYNTHETIC AVENUE, TEST CITY 0000` |
| H03 | `000-000-000-000` reserved synthetic value |
| H04 | `ExitPass POS Server 1.3` |
| H05 | `Z-012B / 2026-08-10` |
| H06 | `SYN-AE1-SERIAL-NNN` |
| H07 | `SYN-AE1-MIN-NNN` |
| H08 | `SYN-AE1-SPS-NNN`, provisional under AE-DR-020A |
| H09 | `2026-09-30 10:00:00 UTC`; requires the unauthorized future test-clock contract |
| H10 | `svc-syn-annex-e1-uat-v11` |

## 8. Accounting facts and C02 schedule

### 8.1 Status and source rules

Every period has seven current immutable facts. Manual and overflow are `recorded` only in `SP-A11`; all other profile values use `attested_zero`. `recorded` Manual uses count 1, first/last `SYN-AE1-MANUAL-NNN`, null event reference. `recorded` overflow uses count 1, first/last `SYN-AE1-OVERFLOW-NNN`, and event `SYN-AE1-CAPACITY-EVENT-NNN`. Attested-zero rows have amount/count 0 and all three source references null. Approval is `SYN-AE1-ACCOUNTING-V11`. Actor is `synthetic-data-steward`, service is `pos-server-annex-e1-uat-preparer`, and correlation is the case ID.

### 8.2 Required order and effective instants

| Fact ordinal within period | Type | Effective offset from period end |
|---:|---|---|
| 1 | `manual_si_or_net_income` | minus 10 seconds plus 1 millisecond |
| 2 | `sales_overrun_overflow_net_income` | minus 10 seconds plus 2 milliseconds |
| 3 | `naac_discount` | minus 10 seconds plus 3 milliseconds |
| 4 | `solo_parent_discount` | minus 10 seconds plus 4 milliseconds |
| 5 | `other_vat_adjustment` | minus 10 seconds plus 5 milliseconds |
| 6 | `vat_on_returns` | minus 10 seconds plus 6 milliseconds |
| 7 | `residual_vat_adjustment` | minus 10 seconds plus 7 milliseconds |

For a multi-period case, global Accounting fact ordinal is `(period ordinal - 1) * 7 + fact ordinal`. `AE1-UAT-013` adds ordinal `0008` as a Manual correction for its only period, amount 600, `source_correction`, superseding ordinal `0001`, effective at period end minus 10 seconds plus 8 milliseconds, with its later recorded instant. Equal effective timestamps, if introduced by a later version, sort by fact-type order and UUID; v1.1 has no equal effective timestamps and all recorded timestamps are unique. The current repository writes the invalid exclusive period end, so an authorized future runtime change must accept or calculate this in-period effective instant and enforce C02 before dataset implementation or loading.

## 9. Reconciliation contract

All monetary tolerances are 0 minor units and count tolerances are 0 rows.

```text
R01 D07 - D17 - D18 = ACTIVE_GROSS
R02 D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
R03 D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
R04 D25 = D20 + D21 + D22 + D23 + D24
R05 D26 + D22 = D09
R06 D27 + D19 + D09 = D07
R07 D29 - D06 - D28 = D27
R08 every copied operand equals the committed BIR Summary, Z, or Accounting fact
R09 Site POS Server, fiscal identity, PHP, and period IDs agree across sources
R10 every Z/BIR transition has exactly one integrity-valid EJ record and EJ supplies no amount
R11 included active tender total = BIR net_sales_amount_minor_units
R12 present GTA = previous GTA + BIR net_sales_amount_minor_units
```

For `SP-A11`, R01 through R07 results are 33600, 1000, 16200, 0, 3600, 44800, and 25000; R11 is 28600; R12 is 128600 = 100000 + 28600. For `SP-B11`, the corresponding results are 11200, 0, 0, 0, 1200, 11200, 10000, 11200, and prior GTA + 11200. For `SP-D11`, they are 22400, 0, 0, 0, 2400, 22400, 20000, 22400, and prior GTA + 22400.

## 10. Source population totals and request controls

The corrected package specifies 19 Sites, 19 fiscal identities, 19 header profiles, 22 periods, 67 fiscal documents/lines/totals/tax rows, 69 tenders, 45 discount details, 30 statutory snapshots, 82 document status-history rows, 22 X reports, 22 Z reports, 22 BIR summaries, 155 Accounting facts, 148 EJ records, 44 fiscal ranges, 48 tender breakdowns, 90 discount breakdowns, 19 workbooks, 22 Annex rows, and 154 Annex fact-source links. Each period contributes one independently identified child range under X and one under Z. Each X and Z report persists its own tender and discount breakdown rows.

The 346 planned operation-request identities are 67 document creates, 15 voids, 22 X, 22 Z, 22 BIR, 155 Accounting facts, 31 Annex generation attempts, and 12 EJ read/integrity/export attempts. Annex attempts are one per case plus a second attempt in 012, 013, 014, 016, and 024 and seven additional attempts in 021. The 12 EJ/access attempts are: 001 deterministic export; 011 integrity verification; 014 post-restart read; 016 missing and tampered downloads; 018 metadata read and download; 019 wrong-scope metadata read and download; 023 privacy-safe metadata inspection; and 024 orphan lookup and post-retry metadata read. Within each case, operation-request ordinals follow document creates, voids, X/Z/BIR requests, Accounting fact requests, Annex attempts, then EJ/access attempts.

The 148 source-transition identities equal 67 document commits + 15 voids + 22 X + 22 Z + 22 BIR commits. The 186 audit identities are 155 created fact audits, 19 created workbook audits, and 12 EJ access audits. Two replay records belong to 012 and 014; two conflict records belong to 013 and 021; two recovery records belong to 014 and 024.

These are specification records, not created data. The source-population matrix defines their exact field contracts and case ownership.

## 11. Replay, conflict, recovery, correction, and privacy

Exact replay reuses operation identity and source semantics and must return the original workbook ID, revision, metadata, filename, content hash, byte length, and stored bytes. Changed semantics under the same operation return conflict and create no source link, row, metadata, or artifact. Restart reads committed metadata and stored bytes. Publication failure before metadata commit leaves no API-resolvable workbook; cleanup may remove only a proven invocation-owned unreferenced file.

The only specified correction is the superseding Manual fact in case 013. It does not authorize a correction workbook. Historical rows and artifacts are never updated or deleted. Destructive retention, archive, and purge remain prohibited.

All identifiers are synthetic. No customer, vehicle, taxpayer, employee, credential, payment account, statutory ID, evidence image, raw request/response, connection string, filesystem path, stack trace, or external endpoint appears in the package. The future dataset and validator must operate offline.

## 12. Change control and next gate

Any change to the namespace, grammar, object registry, scenario partition, source row, value, chronology, hash contract, formula, or external-decision cross-reference requires a new version and new manifest hashes. Runtime preconditions in this document are specifications only and remain unauthorized.

The next bounded activity is **Review Annex E-1 Synthetic UAT Dataset Specification v1.1**. Dataset implementation remains blocked until a later merged review explicitly records `AUTHORIZED_FOR_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION`.
