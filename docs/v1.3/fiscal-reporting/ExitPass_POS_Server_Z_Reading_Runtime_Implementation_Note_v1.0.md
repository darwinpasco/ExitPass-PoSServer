# ExitPass POS Server Z Reading Runtime Implementation Note v1.0

## 1. Decision and authority

Z-007 implements the first privileged Z Reading and atomic fiscal reporting-period close. It uses the approved Z-007A decisions, merged Z-007B foundation, `pos-server-fiscal-reporting:v1`, and `pos-server-fiscal-report-request:sha256:v1`.

The historical blocker remains in `ExitPass_POS_Server_Z_Reading_Runtime_Period_Close_Blocker_v1.0.md`. Approved Z-007A decisions and merged Z-007B commit `a20a2dc99d95cba99be25991a30f0f6a70df9648` resolve its counter, GTA, sequencing, and late-write prerequisites.

## 2. API and authorization contract

| Operation | Route | Policy | Server-derived permission |
|---|---|---|---|
| Close and generate | `POST /v1/fiscal-reports/z-readings/` | `FiscalZReadingClose` | `fiscal_z_reading.close` |
| Stored readback | `GET /v1/fiscal-reports/z-readings/{zReadingReference}` | `FiscalZReadingRead` | `fiscal_z_reading.read` |

The close request accepts only operation key, Site POS Server ID, fiscal identity ID, currency, target reporting-period ID, and expected canonical state version. It does not accept aggregates, counters, GTA, ranges, tender totals, statutory totals, or lifecycle results.

The API-key handler derives permission, Site POS Server scope, fiscal-identity scope, and currency scope from server configuration. X Reading, state initialization, fiscal creation/read, cashier, APT, WebPay, and reconciliation authority do not grant Z close. Read authority is separate and scoped. Public failures contain safe classifications and support correlation only.

## 3. Eligibility and sequencing

The repository resolves the exact Site POS Server and active fiscal identity, acquires the shared scope boundary, and locks the requested reporting period and canonical Z state. It then requires:

- exact Site POS Server, fiscal identity, and uppercase currency scope;
- one initialized canonical state at the caller's expected version;
- one `OPEN` period whose end has elapsed;
- active reporting contract, timezone, cutoff, and currency snapshots;
- all in-window fiscal documents assigned to that exact period and currency;
- strict `period_sequence` predecessor posture from the Z-007B guard;
- canonical state's last period and Z linkage to match the predecessor;
- no unsupported source classification, cross-period mutation, mixed currency, reconciliation mismatch, or unexplained sequence gap.

Sequence one is the governed initialized first-period posture. Later periods require the immediate prior period to be `CLOSED` with committed Z and transition evidence.

## 4. Aggregation and reconciliation

Membership is the immutable `fiscal_reporting_period_id`, not `created_at`. The complete period is `[period_start_at, period_end_at)`. Only fully recorded Sales Invoices are active sales. Same-period voided invoices are excluded from active sales and retained in governed void totals. Cross-period void, refund, return, adjustment, unsupported service charge, unsupported tender/tax/discount/statutory classification, and unexplained gaps fail closed.

The implementation reuses the governed X aggregation engine over first-class fiscal document, line, tender, tax, discount, applied statutory, sequence, and gap rows. All arithmetic uses checked integer minor units. It reconciles active line net, tax, discount, tender, statutory, and fiscal total facts without recomputing statutory eligibility or payable-basis decisions.

The approved GTA contribution is the reconciled VAT-inclusive final fiscal-document amount of qualifying recorded Sales Invoices assigned to the period. Under the supported source set it must equal Z net sales.

## 5. Atomic close and state transition

One PostgreSQL `READ COMMITTED` transaction owns this order:

1. acquire the shared transaction advisory lock for Site POS Server, fiscal identity, and currency;
2. lock and revalidate the reporting period;
3. lock and version-check canonical Z state;
4. verify predecessor sequencing and source assignment;
5. aggregate and reconcile the complete period;
6. persist request, scope, immutable Z snapshot, tender/discount children, ranges, and governed gaps;
7. persist the Z counter/GTA snapshot and immutable transition evidence;
8. apply the expected-version canonical state transition;
9. transition the period atomically from `OPEN` to `CLOSED` and record its governing Z link;
10. persist safe operation and audit evidence, then commit.

The transition is:

```text
resulting_z_counter = previous_z_counter + 1
resulting_reset_counter = previous_reset_counter
resulting_gta = previous_gta + current_period_gta_contribution
resulting_state_version = expected_state_version + 1
```

No durable `CLOSING` state is written. No report reference or successful result is returned before commit. Any failure rolls back the report, children, state evidence, state update, and period transition together.

## 6. Replay, conflict, competition, and recovery

The semantic request source includes operation key, exact scope, report kind, period identity and historical business-time facts, contract version, prior-period reference, expected state version, and close intent. Correlation and transport diagnostics are excluded.

- same operation key and same semantic request returns the stored report and changes nothing;
- same operation key and changed semantics returns terminal semantic conflict;
- a different operation after durable close returns period-already-closed conflict;
- competing different operations serialize through the shared boundary and exactly one commits;
- a retry after timeout first reconciles durable operation, report, period, canonical state, and transition evidence;
- an unresolved commit outcome returns a safe unknown-outcome classification instead of attempting another state mutation.

Readback loads the immutable report and children. It does not reaggregate fiscal sources or expose semantic hashes, database identifiers, SQL diagnostics, customer data, statutory identity, evidence, reviewer identity, or payment credentials.

## 7. Fiscal ranges, gaps, and children

Ranges use assigned fiscal sequence policy, numeric sequence values, and series; fiscal-number text is presentation evidence only. One immutable range records first/last sequence, first/last fiscal number, qualifying count, and gap count. Governed gap children are persisted by stable classification. Unexplained or unsupported gaps block close.

Tender and discount/statutory rows use controlled classifications and report currency. Senior Citizen, PWD, other statutory benefit, VAT exemption/removal, coupon, and promotional adjustments remain distinct. No beneficiary or evidence data enters reporting storage.

## 8. Validation evidence

Automated unit and API coverage proves command validation, canonical semantic hashing, route and policy metadata, server-derived Site/fiscal-identity/currency scope, denied compatibility permissions, safe error mapping, exact replay, and read privacy.

The disposable PostgreSQL 16 API proof uses synthetic fixtures and proves initialized first-period close, complete ordinary and statutory aggregation, Z/reset/GTA/version transition, immutable snapshot and children, stored readback equality, restart replay, semantic conflict, different-operation closed conflict, competing empty-period close, forced post-report failure rollback, and before/after no-unrelated-mutation manifests. Direct immutable-update proof is included.

The no-unrelated-mutation manifest covers fiscal documents, status history, sequence state, legacy counter state, fiscal state snapshots, X reports, reprints, unrelated scopes, and all expected Z objects. A successful close changes only bounded Z reporting, transition, canonical state, period, operation, and audit facts. Forced failure changes none of them.

## 9. Safe operations and privacy

Safe errors do not expose SQL, constraint or table names, internal hashes, connection strings, credentials, stack traces, or paths. Audit evidence uses opaque operation, report, actor/service, correlation, and outcome references. Full request/report payloads and raw fiscal or statutory data are not logged.

Disposable evidence contains synthetic references only and is not tracked. Database credentials are supplied only through process environment and are excluded from documentation and completion output.

## 10. Readiness and exclusions

- Z Reading runtime and JSON readback: implemented by Z-007.
- Privileged atomic `OPEN -> CLOSED` close: implemented by Z-007.
- Z rendering, printing, reprinting, and export: not implemented.
- BIR, Annex E, EJ, and POSLog generation: not implemented.
- Cross-period void/refund/return/adjustment, unsupported service-charge semantics, fiscal reset, manual counter correction, reopen, and deletion: not implemented and fail closed where applicable.
- Controlled UAT: not authorized.
- Production rollout: not authorized.
