# ExitPass POS Server Internal Fiscal Reporting Contract v1.0

## 1. Decision

The internal contract key is `pos-server-fiscal-reporting:v1`, with deterministic identifier `f6766f48-62f0-513f-b9eb-e61c2f3e8c66`. This contract governs database identity and future runtime inputs. It does not authorize report generation or a Z Reading close.

Approved internal report kinds are `X_READING`, `Z_READING`, `BIR_SALES_SUMMARY`, and `ANNEX_E`. No external Annex E form variant or POSLog profile is inferred.

## 2. Business Time

- The timezone and business-day cutoff come from approved Site POS Server configuration.
- A reporting period snapshots the timezone name and local cutoff used to derive its business date.
- Every period uses the half-open interval `[period_start_at, period_end_at)`.
- One period has one Site POS Server, fiscal identity, business date, currency, and positive period sequence.
- Mixed timezone or mixed currency interpretation is prohibited.
- A fiscal document committed after an applicable period is closed is not silently inserted into that period. Runtime must fail closed until a governed subsequent-period adjustment rule applies.
- No production timezone or cutoff value is supplied by this contract.

## 3. Lifecycle

Report requests use `REQUESTED`, `PROCESSING`, `COMMITTED`, `FAILED`, `REJECTED`, `CONFLICT`, `CANCELLED`, or `UNKNOWN_COMMIT_OUTCOME`. `CANCELLED` applies only before irreversible processing. `UNKNOWN_COMMIT_OUTCOME` means runtime must resolve durable state before retrying. Output and export status are separate families.

The legacy `COMPLETED` request value remains loadable only for compatibility and is deprecated in favor of `COMMITTED`. New snapshot runtime must not write `COMPLETED`.

Reporting periods use `OPEN`, `CLOSING`, and `CLOSED`. Z-006A creates no closed period outside disposable constraint fixtures.

## 4. Aggregation Sources

| Fact | Authoritative source and rule |
| --- | --- |
| Document count | Count qualifying `pos.fiscal_documents` once. Future runtime requires document type key `sales_invoice` and status key `recorded` or `voided`; missing or ambiguous code resolution fails closed. |
| Gross/net/total discount | Sum active fiscal-document line gross, net, and discount minor-unit columns for recorded documents. |
| VATable/exempt/zero-rated and VAT | Sum `pos.fiscal_tax_details` once by governed classification. Mixed document-level and line-level representation of one tax classification is prohibited. |
| Senior Citizen/PWD | Project immutable `pos.fiscal_document_applied_statutory_facts` entitlement and discount values. Privilege details are reconciliation peers and are not added again. |
| Other statutory | Use only a governed reporting discount classification; unknown classifications fail closed. |
| VAT exemption/removal | Sum the governed `vat_privilege_amount_minor_units` snapshot exactly once. |
| Coupon/promotion | Sum separately governed commercial privilege-detail classifications. They never become statutory amounts. |
| Tender | Sum `pos.fiscal_tenders` once into a governed reporting tender classification. |
| Void | Exclude voided documents from recorded sales and snapshot their original final fiscal amount as a non-negative void total in the period containing the committed void. |
| Refund/return/adjustment | Schema columns are reserved, but generation fails closed until document types, signs, and period attribution are approved. |
| Service charge | Schema column is reserved, but generation fails closed until a governed source classification exists. |
| Fiscal range/gaps | Use assigned fiscal sequence policy/value and immutable classified gap rows, partitioned by fiscal identity, policy, and series. |
| GTA/counters | A Z-only child snapshot proves previous + current = resulting GTA and previous Z + 1 = resulting Z. Runtime transition is not implemented. |

All amounts are signed only where their named category requires a separately governed rule; Z-006A snapshot amount columns are non-negative category totals. Values use integer minor units and one uppercase three-letter currency per report. Reprints never count as fiscal documents or sales.

The first-class snapshot stores document count, gross/net, VATable, VAT, VAT-exempt, zero-rated, total discount, Senior Citizen, PWD, other statutory, VAT exemption/removal, coupon, promotional, void, refund, return, adjustment, service charge, previous/current/resulting GTA, reset/Z counters, and fiscal sequence range posture. Reserved categories do not authorize runtime aggregation while their source and period rules remain unresolved.

## 5. X Identity and Idempotency

An X Reading is read-only. It may create one immutable observation per operation key. Exact operation-key/hash replay returns the same snapshot; changed semantics conflict. A new operation key may create a later observation of the same open period. X never closes a period or advances reset, Z, sequence, or GTA state.

## 6. Z Close Identity and Idempotency

One fiscal reporting period can own at most one Z snapshot. Exact operation-key/hash replay must return it. Changed semantics or a second close identity must conflict without mutation. Future runtime must atomically commit the period close, Z snapshot, ranges/gaps, counter/GTA snapshot, and audit evidence. No Z close runtime exists in this slice.

## 7. BIR and Annex E

A BIR summary is immutable and references one governing Z snapshot, contract version, fiscal identity, and historical header snapshot. One BIR summary contract version is allowed per Z snapshot.

Annex E metadata requires a governing Z snapshot and an approved external contract/profile reference. No generic context JSON or customer payload is allowed. Exact Annex E fields and export format remain a compliance dependency.

## 8. Database Identity and Retention

- `pos.fiscal_reporting_contract_versions` freezes the internal contract and report semantic-hash version.
- `pos.fiscal_reporting_periods` owns Site POS Server, fiscal identity, business date, half-open instants, timezone/cutoff snapshots, currency, and period sequence.
- `pos.fiscal_report_requests` owns the Site-scoped operation key and semantic request digest. The canonical semantic source is never persisted.
- `pos.x_z_reports` contains committed immutable X/Z aggregate snapshots only.
- Tender, discount, fiscal-range, gap, and Z counter facts use first-class immutable child rows.
- One report request can own one snapshot. One period can own one Z snapshot. A new operation key can own another X observation.
- Report child currency is constrained to the parent report currency.
- No reporting parent uses cascading deletion for fiscal evidence.
- Closed periods, committed snapshots, BIR summaries, Annex E metadata, and committed child facts reject direct `UPDATE` and `DELETE`.

## 9. Privacy and Authority

Reporting stores fiscal aggregates and opaque operational references only. Beneficiary identity, statutory IDs, evidence, reviewer identity, credentials, authorization headers, raw request bodies, raw ordinance text, and customer metadata are prohibited. POS Server does not determine eligibility, tariff, payment finality, or gate authority.

## 10. Migration and Runtime Gate

Existing empty posture tables are transitioned in place. If legacy request, scope, X/Z, BIR, Annex E, or output rows exist, upgrade fails closed before removing generic context or making governed columns mandatory. Such environments require a separately approved data migration because Z-006A does not invent historical fiscal facts.

X and Z runtime, report APIs, rendering, printing, EJ, POSLog, exports, controlled UAT, and production rollout remain unimplemented or unauthorized.
