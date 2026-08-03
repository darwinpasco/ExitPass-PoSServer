# ExitPass POS Server X Reading Capability Matrix v1.0

## 1. Verdict

| Layer | Verdict |
| --- | --- |
| Schema | PARTIALLY_READY |
| Write runtime | NOT_IMPLEMENTED |
| Read runtime | NOT_IMPLEMENTED |
| API | NOT_IMPLEMENTED |
| Rendering | NOT_IMPLEMENTED |
| Print/reprint | NOT_IMPLEMENTED |
| Export | NOT_IMPLEMENTED |
| Automated validation | NOT_IMPLEMENTED |
| Manual/UAT evidence | NOT_IMPLEMENTED |
| Production readiness | NOT_READY |

`pos.x_z_reports` can hold a small common snapshot, but no code produces or reads an X Reading. Its SQL comment states that the object does not calculate reports or mutate fiscal state.

## 2. Required Capability Matrix

| Capability | Current evidence | Status | Required follow-up |
| --- | --- | --- | --- |
| On-demand interim reading | No service, endpoint, or query | NOT_IMPLEMENTED | Add a read-only generation service after aggregation rules are frozen. |
| No fiscal-period closure | No X behavior exists | UNKNOWN | Specify and test that X generation never closes a period or advances counters/GTA. |
| Current period scope | Nullable scope and period columns exist | PARTIALLY_READY | Govern open-period identity, business date, and timezone boundaries. |
| Site POS Server identity | Request/scope tables can reference Site POS Server | PARTIALLY_READY | Require scope and enforce tenant/site ownership in API and query. |
| Report number/reference | No governed X report number | NOT_IMPLEMENTED | Define deterministic identity and repeated-generation posture. |
| Generation timestamp | Snapshot timestamps are available | PARTIALLY_READY | Define server clock and whether timestamp is semantic or presentational. |
| Business date | Nullable `business_day_date` | PARTIALLY_READY | Make source and timezone/cutoff rules explicit. |
| First/last fiscal number | Generic range columns exist | PARTIALLY_READY | Define status inclusion, series boundaries, and missing-number treatment. |
| Transaction count | No first-class column | NOT_IMPLEMENTED | Add governed count and source status rules. |
| Gross/net/VAT totals | Only broad aggregate columns exist | PARTIALLY_READY | Freeze formulas and add missing VATable/exempt/zero-rated facts. |
| Statutory breakdown | Applied facts exist per document; no X projection | NOT_IMPLEMENTED | Aggregate entitlement and benefit classifications without double counting. |
| Voids/refunds/returns/adjustments | No X calculation; refund/return runtime absent | NOT_IMPLEMENTED | Freeze sign and period rules before runtime. |
| Tender breakdown | Fiscal tenders exist; X snapshot has no breakdown | NOT_IMPLEMENTED | Govern tender codes and persist/render a breakdown. |
| Cumulative/grand totals | Counter/state tables exist only as posture | NOT_IMPLEMENTED | Define whether X displays but never mutates GTA/counters. |
| Deterministic repeated generation | No identity/idempotency | NOT_IMPLEMENTED | Choose fresh snapshot versus stable same-as-of identity and test both semantics. |
| Readback/history | No repository or endpoint | NOT_IMPLEMENTED | Add scoped read/list APIs and immutable snapshots. |
| Render/print/reprint/export | No implementation | NOT_IMPLEMENTED | Add only after JSON presentation is authoritative. |
| Audit/authorization | No report policy or audit events | NOT_IMPLEMENTED | Add distinct X permission and durable actor/correlation evidence. |
| Concurrency/restart | No implementation | NOT_IMPLEMENTED | Prove concurrent reads are consistent and restart cannot mutate state. |

## 3. Mutation Decision

An X Reading must be a read-only observation. It may persist an immutable report snapshot and output references, but generation must not:

- close a fiscal period;
- alter a fiscal document, fiscal number, or status;
- advance Z/reset counters or grand totals;
- change sequence state;
- mark any Sales Invoice printed;
- block ordinary issuance longer than the consistent-read mechanism requires.

The current repository performs none of these mutations because it has no X runtime. This is absence of implementation, not proof of the future invariant.

## 4. Required Tests and Proof

- Exact aggregation fixtures across ordinary, Senior Citizen, PWD, VAT-exempt, void, and multi-tender documents.
- Two reads at the same governed cutoff produce identical fiscal facts.
- Concurrent issuance has a defined before-or-after inclusion result, never a partial document.
- X generation leaves fiscal period, counters, sequence state, and document rows unchanged.
- Restart and lost-response replay do not create ambiguous history.
- Scoped authorization, correlation, safe errors, rendering, print/reprint, and export are proven.

