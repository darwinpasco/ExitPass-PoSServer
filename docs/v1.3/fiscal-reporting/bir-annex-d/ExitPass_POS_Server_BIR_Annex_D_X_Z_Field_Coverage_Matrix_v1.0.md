# ExitPass POS Server BIR Annex D X/Z Field Coverage Matrix v1.0

## Document control

| Item | Value |
| --- | --- |
| Baseline | `b182c7e3278f0cb8e4df7ddd401d44ea149c70a5` |
| Scope | POS Server X/Z immutable snapshots, presentation, PDF, and source arithmetic only |
| Inventory | 50 fields: 17 `BASELINE_CORRECT`, 13 `CORRECTED_AND_VALIDATED`, 16 `SOURCE_CONTRACT_REQUIRED`, 0 `NOT_APPLICABLE`, 4 `BIR_EXAMINER_DECISION_REQUIRED` |
| Regulatory posture | Source correction is independently reviewable; BIR compliance is not accepted while applicable source and examiner gaps remain. |

The Annex D PDFs are field and presentation references, not accounting test data. The D-2 example is internally inconsistent, so its example numbers are not used as a computational oracle.

## Direct PDF evidence

| Role | Exact path | Bytes | SHA-256 | Inspection result |
| --- | --- | ---: | --- | --- |
| BIR D-1 reference | `D:\XZ_Compare\RMO 24-2023 Annex D-1_Sample X-Reading.pdf` | 252,997 | `c0003de379938f2e3bd70b2f163ed9b281f625d0333aeb2913d06b837cc50584` | Directly rendered and inspected. Shows cashier accountability, payment, custody, SI/OR range, machine, and report-time fields. |
| BIR D-2 reference | `D:\XZ_Compare\RMO 24-2023 Annex D-2_Sample Z-Reading.pdf` | 418,099 | `43352d92b649d603e3b3d4d2bfb404f310de98a7ad41106a6b1061b4af57f029` | Directly rendered and inspected. Shows counters, accumulated sales, sales/discount/VAT adjustments, tender, and custody fields. |
| ExitPass baseline X | `D:\XZ_Compare\x-reading-x-20261021-96dbacd0fdcd40faaf5366fc7c02fa99-57mm.pdf` | 3,625 | `f03e13ad9ac6728410b6f11a29a71f9deaa9d67e5d4d67b1f48c0a187b466a44` | Directly rendered and inspected. Missing address/custody; scheduled end is ambiguous; prior-Z/count values and operator are hardcoded. |
| ExitPass baseline Z | `D:\XZ_Compare\z-reading-z-20261020-00000033-492353440f344e5997a3db9ce4f45ad6-57mm.pdf` | 3,645 | `e49822a75e320a7c8c5b67e987168e36e30058f639b71de52ada37f3ce6a35ec` | Directly rendered and inspected. Missing address, GTA, detailed discounts/VAT adjustments/custody; unsupported values appear as zeros and operator is hardcoded. |

Both baseline ExitPass PDFs are 161.5748 points wide (57.000 mm). Their financial relationships are arithmetically consistent, but the PDFs alone do not contain invoice-level evidence.

## Classification legend

| Classification | Meaning |
| --- | --- |
| `BASELINE_CORRECT` | Baseline field is present and sourced from an authoritative fiscal fact. |
| `CORRECTED_AND_VALIDATED` | This branch adds or corrects the field using authoritative immutable evidence and focused validation. |
| `SOURCE_CONTRACT_REQUIRED` | Applicable field lacks the governed source evidence needed for truthful output. |
| `NOT_APPLICABLE` | Statutory field is proven outside the supported transaction model; no field met this threshold. |
| `BIR_EXAMINER_DECISION_REQUIRED` | Source facts exist, but required statutory scope or accounting mapping is not proven. |

## Exact 50-field comparison

| # | Report / BIR reference field | Baseline ExitPass | Corrected ExitPass / authoritative source | Classification | Validation / remaining limitation |
| ---: | --- | --- | --- | --- | --- |
| 1 | X Registered business name | Printed | Effective fiscal identity | `BASELINE_CORRECT` | Direct PDF and presentation tests pass. |
| 2 | X Business address | Missing | Printed from `registered_business_address` | `CORRECTED_AND_VALIDATED` | Source-backed; corrected PDF inspected. |
| 3 | X TIN | Printed | Effective fiscal identity | `BASELINE_CORRECT` | Direct PDF and tests pass. |
| 4 | X MIN | Printed when configured | Effective SI header profile | `BASELINE_CORRECT` | No fabricated fallback. |
| 5 | X POS serial number | Printed when configured | Effective SI header profile | `BASELINE_CORRECT` | No internal UUID substituted. |
| 6 | X Report date/time | Printed | Immutable `generated_at` in PHT | `BASELINE_CORRECT` | Observation timestamp remains immutable. |
| 7 | X Period start | Printed | Immutable `period_start_at` | `BASELINE_CORRECT` | PITX 07:00 PHT boundary retained. |
| 8 | X Observation cutoff | Not identified | `generated_at` labeled Observation Cutoff | `CORRECTED_AND_VALIDATED` | Future scheduled end is no longer presented as observed-through time. |
| 9 | X Scheduled period end | Printed ambiguously | Printed separately as Scheduled Period End | `CORRECTED_AND_VALIDATED` | Presentation-only correction. |
| 10 | X Cashier identity | Hardcoded `SYSTEM` operator | `requested_by_ref` labeled Request Actor | `SOURCE_CONTRACT_REQUIRED` | Actor is not called Cashier without human-session/custody proof. |
| 11 | X Beginning SI | Printed when available | Immutable fiscal-number range | `BASELINE_CORRECT` | SI terminology retained. |
| 12 | X Ending SI | Printed when available | Immutable fiscal-number range | `BASELINE_CORRECT` | SI terminology retained. |
| 13 | X Last Z counter | Hardcoded `0000` | Prior committed Z counter captured in X snapshot | `CORRECTED_AND_VALIDATED` | Null means no prior evidence; never fabricated. |
| 14 | X Last Z closed time | Hardcoded `0000` | Prior closed-period time captured in X snapshot | `CORRECTED_AND_VALIDATED` | Pair integrity enforced by constraint. |
| 15 | X Issued transaction count | Mixed qualifying count | Separate issued/recorded count | `CORRECTED_AND_VALIDATED` | Persisted in v2 snapshot. |
| 16 | X Void transaction count | Hardcoded zero | Governed voided-document count | `CORRECTED_AND_VALIDATED` | Void amount remains source-backed. |
| 17 | X Cancellation count | Hardcoded zero | Omitted | `SOURCE_CONTRACT_REQUIRED` | Requires governed cancellation facts. |
| 18 | X Return count | Hardcoded zero | Omitted | `SOURCE_CONTRACT_REQUIRED` | Requires governed return facts. |
| 19 | X Refund count | Unsupported/zero posture | Omitted | `SOURCE_CONTRACT_REQUIRED` | Requires governed refund facts. |
| 20 | X Opening fund | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | Requires approved immutable APT custody boundary. |
| 21 | X Physical cash in drawer | Missing | Not derived from tenders | `SOURCE_CONTRACT_REQUIRED` | Requires actual custody count evidence. |
| 22 | X Cash withdrawals | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | Requires approved Stage 2B movement evidence. |
| 23 | X Short/Over | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | Requires opening, movements, and blind count. |
| 24 | X Cashier accountability scope | Site-period X only | Site-period X preserved | `BIR_EXAMINER_DECISION_REQUIRED` | Examiner must decide whether a supplemental cashier reading is required. |
| 25 | X Tender breakdown | Cash/Card/QRPh/GCash/Maya | Immutable tender breakdown retained | `BASELINE_CORRECT` | No digital tender enters physical drawer cash. |
| 26 | X Gross/discount/tax/net | Printed | Checked source-line/tax/discount/tender chain | `BASELINE_CORRECT` | No presentation recalculation. |
| 27 | Z Registered business identity | Printed | Effective fiscal identity/header profile | `BASELINE_CORRECT` | Direct PDF and tests pass. |
| 28 | Z Business address | Missing | Printed from `registered_business_address` | `CORRECTED_AND_VALIDATED` | Source-backed; corrected PDF inspected. |
| 29 | Z Period/report timestamps | Printed | Immutable closed Z snapshot | `BASELINE_CORRECT` | PITX fiscal date retained. |
| 30 | Z Beginning/ending SI | Printed when available | Immutable fiscal-number range | `BASELINE_CORRECT` | No sequence fabrication. |
| 31 | Z Beginning/ending void numbers | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | No separate governed void-number sequence. |
| 32 | Z Beginning/ending return numbers | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | Applicable Annex D field needs a future return-number contract. |
| 33 | Z Reset counter | Printed | Immutable counter snapshot | `BASELINE_CORRECT` | Existing reset authority unchanged. |
| 34 | Z Z counter | Printed | Immutable exactly-once counter snapshot | `BASELINE_CORRECT` | Sequential close guard unchanged. |
| 35 | Z Previous accumulated sales | Missing | Previous GTA printed under GTA heading | `BIR_EXAMINER_DECISION_REQUIRED` | ExitPass GTA is net-based; BIR basis not proven equivalent. |
| 36 | Z Present accumulated sales | Missing | Present GTA printed under GTA heading | `BIR_EXAMINER_DECISION_REQUIRED` | Not relabeled as statutory accumulated sales. |
| 37 | Z Sales for the day | Gross and net both exist | Period Net GTA printed | `BIR_EXAMINER_DECISION_REQUIRED` | D-2 sample visually aligns movement with gross; examiner decision required. |
| 38 | Z VAT breakdown | Printed | Immutable tax-detail sums | `BASELINE_CORRECT` | Invoice/line rounding retained. |
| 39 | Z Sales summary | Printed | Checked immutable snapshot | `BASELINE_CORRECT` | Voids excluded from active gross/net and not deducted twice. |
| 40 | Z Senior Citizen discount | Not summarized | Printed from statutory breakdown | `CORRECTED_AND_VALIDATED` | No category inference. |
| 41 | Z PWD discount | Not summarized | Printed from statutory breakdown | `CORRECTED_AND_VALIDATED` | No category inference. |
| 42 | Z NAAC discount | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | Requires governed entitlement code/facts. |
| 43 | Z Solo Parent discount | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | Requires governed entitlement code/facts. |
| 44 | Z Commercial discounts | Only aggregate shown | Coupon/promotional classes summarized | `CORRECTED_AND_VALIDATED` | Existing classifications preserved. |
| 45 | Z Void adjustment | Amount present, count fixed zero | Source-backed amount and count | `CORRECTED_AND_VALIDATED` | No second subtraction from active sales. |
| 46 | Z Return adjustment | Unsupported zero posture | Omitted | `SOURCE_CONTRACT_REQUIRED` | Requires governed return contract. |
| 47 | Z Refund adjustment | Unsupported zero posture | Omitted | `SOURCE_CONTRACT_REQUIRED` | Requires governed refund contract. |
| 48 | Z Aggregate VAT privilege adjustment | Not summarized | Printed from immutable aggregate privilege amount | `CORRECTED_AND_VALIDATED` | Not split into SC/PWD without attribution facts. |
| 49 | Z Category-specific VAT adjustments | Missing | Not printed | `SOURCE_CONTRACT_REQUIRED` | Requires immutable category attribution. |
| 50 | Z Cashier physical accountability | Missing | Not derived from POS tenders | `SOURCE_CONTRACT_REQUIRED` | Requires approved APT custody evidence. |

## Accounting definitions

| Amount | Accepted definition |
| --- | --- |
| Gross Sales | Checked sum of active issued/recorded SI line gross amounts before line discount and line tax. |
| Discounts | Checked active SI line discounts reconciled to governed statutory/commercial evidence. |
| VATable / exempt / zero-rated sales | Checked sums of authoritative tax-detail taxable amounts by classification. |
| VAT | Checked sum of authoritative invoice/line tax details; never recomputed as 12% of the report aggregate. |
| Net Sales | Checked active SI line net: `gross - discount + tax`. |
| Total Collected | Checked sum of authoritative tenders; lawful zero-payable statutory SIs need no synthetic tender. |
| Void Amount | Checked voided SI line net. Voided SIs do not enter active Gross, Discount, VAT, Net, or tenders. |
| GTA movement | Z Net Sales under the existing state-transition contract. |
| BIR accumulated sales | Unmapped pending examiner confirmation; the D-2 sample visually aligns movement with gross. |

## Corrected PDF evidence

| Evidence | Dimensions | SHA-256 | Visual result |
| --- | --- | --- | --- |
| Synthetic corrected X | 161.5748 x 654.8386 points; exactly 57.000 mm wide | `0bdd2c64f509dbb8241c466233205777b663cd279b684704ec194df69dbd45a0` | One continuous page; actual observation cutoff, source-backed prior Z/counts, Request Actor, no unsupported zero, clipping, or overlap. |
| Synthetic corrected Z | 161.5748 x 803.3386 points; exactly 57.000 mm wide | `7bbd01826791fef68bb91e8cf39409c8278dd88b75ac52f4b1233b48294d0d8d` | One continuous page; source-backed discount/VAT adjustment summary and explicitly net-based GTA; no clipping or overlap. |

The old artifacts remain retrievable and immutable. Corrected output uses the v2 presentation/PDF contract and a distinct output identity.
