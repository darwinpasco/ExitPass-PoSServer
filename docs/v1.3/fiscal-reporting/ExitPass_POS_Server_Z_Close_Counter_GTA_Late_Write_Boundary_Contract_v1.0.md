# ExitPass POS Server Z Close Counter, GTA, and Late-Write Boundary Contract v1.0

## 1. Status and Authority

Contract package status: `DECISION_PACKAGE_COMPLETE_APPROVAL_REQUIRED`.

This document is authoritative for facts already governed by merged ExitPass material and for the implementation gate itself. Items marked `RECOMMENDED_PENDING_APPROVAL` are precise contract candidates, not approved BIR or accounting policy. Z Reading runtime and period close remain blocked until the approval text in Section 14 is accepted by the named authority and the Z-007B prerequisites merge.

Classification used throughout:

- `GOVERNED`: directly supported by merged repository authority.
- `RECOMMENDED_PENDING_APPROVAL`: selected engineering recommendation requiring explicit approval.
- `UNRESOLVED`: no safe implementation choice is authorized.
- `PROHIBITED`: behavior that must not be implemented.

No web or external legal research was used. This package does not claim BIR approval.

### 1.1 Evidence Register

| Evidence | Governed fact or gap established |
|---|---|
| `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` | X/Z, reset-counter, Z-counter, and GTA intent; ordinary Z does not imply a fiscal reset; final reporting aggregation requires BIR/accounting confirmation. |
| `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` | POS Server fiscal authority, atomic fiscal persistence, idempotency, immutable reporting, and fail-closed boundaries. |
| `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` | Safe API, idempotency, correlation, and fiscal authority posture; no approved Z-close runtime contract. |
| `contracts/pos-server/fiscal-reporting-internal-contract.v1.json` | `pos-server-fiscal-reporting:v1`, report kinds, lifecycle codes, period scope, half-open window, immutable snapshot, and semantic identity. |
| `docs/v1.3/fiscal-reporting/ExitPass_POS_Server_Internal_Fiscal_Reporting_Contract_v1.0.md` | X/Z separation, reporting-time semantics, aggregation fields, replay/conflict posture, and unresolved compliance categories. |
| `docs/v1.3/fiscal-reporting/ExitPass_POS_Server_X_Reading_Runtime_Implementation_Note_v1.0.md` and merged X runtime | Current supported source classifications and immutable observation implementation; X does not define Z state transition. |
| `db/state/tables/pos.fiscal_reporting_periods.sql` | Period scope, sequence, expected prior period, OPEN/CLOSING/CLOSED posture, and closed-period immutability. |
| `db/state/tables/pos.x_z_reports.sql` | First-class aggregate facts, one-Z identity, GTA arithmetic relationship, immutable snapshots, and kind-aware timestamps. |
| `db/state/tables/pos.fiscal_z_counter_snapshots.sql` | Immutable previous/resulting reset and Z counters plus previous/current/resulting GTA; it does not own canonical mutable state. |
| `db/state/tables/pos.fiscal_counter_states.sql` | Existing state is generic, Site-only, JSON-capable posture with no governed identity/currency/version/predecessor contract. |
| `db/state/tables/pos.fiscal_state_snapshots.sql` | Historical state posture exists but does not define the authoritative Z-close transition protocol. |
| `src/ExitPass.PosServer.Persistence.Postgres/FiscalDocuments/PostgresFiscalDocumentRepository.cs` | Fiscal creation and void transactions do not currently acquire a reporting close lock or persist a reporting-period assignment. |
| Preserved read-only `D:\wt\Z007\docs\v1.3\fiscal-reporting\ExitPass_POS_Server_Z_Reading_Runtime_Period_Close_Blocker_v1.0.md` | Z-007 direct audit result `BLOCKED_BY_Z_COUNTER_GTA_AND_LATE_WRITE_CONTRACT`; no file in the Z-007 worktree was modified by this task. |

## 2. Governed Baseline

The following are `GOVERNED`:

1. POS Server owns fiscal reports, fiscal numbering, immutable fiscal facts, replay, and semantic conflict.
2. A Z Reading closes one Site POS Server reporting period and advances the Z counter once.
3. Z counter and reset counter are distinct.
4. Reset counter starts at zero, advances only through a separately governed fiscal reset, and does not advance on ordinary Z close.
5. GTA is an accumulated fiscal amount that must preserve audit and recovery continuity.
6. Reporting scope contains Site POS Server, fiscal identity, business date, exact `[start,end)` window, timezone snapshot, cutoff snapshot, currency, and positive period sequence.
7. A Z snapshot is generated at or after period end and one period owns at most one Z snapshot.
8. The same operation key and semantic request replays the committed snapshot; changed semantics conflict.
9. Report, child, range, gap, and Z counter/GTA snapshots are immutable.
10. One transaction must own period close, report facts, counter/GTA facts, operation result, and audit evidence.
11. Mixed currency, unresolved classifications, and unexplained required gaps fail closed.
12. Beneficiary identity, statutory identifiers, evidence, reviewer identity, credentials, raw requests, and generic customer metadata are prohibited.

## 3. Approval-Gated Contract Candidate

The following complete candidate is `RECOMMENDED_PENDING_APPROVAL`:

- GTA contribution basis: reconciled final fiscal amount of qualifying recorded Sales Invoices.
- Counter/GTA scope: `(sitePosServerId, fiscalIdentityId, currencyCode)`.
- State identities: `z_counter`, `reset_counter`, and `grand_total_amount`; no generic unlabeled row may drive close.
- Initialization: explicit privileged and audited initialization; never implicit close-time creation.
- Prior-period sequencing: strict within the same scope and period sequence.
- Late-write coordination: one transaction-scoped PostgreSQL advisory lock shared by every source-affecting fiscal mutation and Z close, followed by period row locking and revalidation.
- Isolation: PostgreSQL `READ COMMITTED` under the shared scope lock; no relevant source writer can interleave.
- Lifecycle: a single durable `OPEN -> CLOSED` commit; `CLOSING` is not committed in v1 and remains reserved.
- Different operation keys targeting one period: deterministic already-closed/close-identity conflict, never replay under a new identity.

These choices become governed only after explicit approval.

## 4. GTA Contribution Candidate

Recommended exact definition:

```text
currentPeriodGtaContributionMinorUnits =
    sum(finalFiscalAmountMinorUnits of qualifying RECORDED SALES_INVOICE issuance events
        assigned to the reporting period)
```

`finalFiscalAmountMinorUnits` means the VAT-inclusive amount finally due after governed statutory and commercial discounts and VAT treatment, represented by immutable fiscal facts and reconciled to active line net totals, the authoritative fiscal total, and tender totals. Tender is a reconciliation peer, not the amount authority.

For v1:

- qualifying document type is `sales_invoice`;
- qualifying status is durably `recorded`;
- the issuance event is assigned to exactly one reporting period;
- a same-period void contributes no recorded-sale amount and is reported separately as a non-negative void fact;
- a cross-period void, refund, return, adjustment, or unsupported service charge blocks close until its signed contribution and attribution contract is approved;
- all amounts use checked integer minor units;
- one state exists per currency and currencies are never converted or summed;
- `currentGrandTotalAmountMinorUnits` in the Z snapshot and `currentPeriodAmountMinorUnits` in the Z counter snapshot equal this contribution;
- `presentGrandTotalAmountMinorUnits = previousGrandTotalAmountMinorUnits + currentPeriodGtaContributionMinorUnits`;
- the contribution must equal Z `netSalesAmountMinorUnits` under the supported v1 source set, otherwise close fails reconciliation.

This recommendation is not approved because repository authority does not define the BIR GTA basis. Gross sales, VAT-exclusive sales, collected tender, and convenience-based values are not interchangeable substitutes.

## 5. Counter Family Contract

The following stable semantic identities are recommended:

| Code | Value | Purpose | Z transition |
|---|---|---|---|
| `z_counter` | non-negative integer | Counts committed Z closes for one scope | exactly `previous + 1` |
| `reset_counter` | non-negative integer | Counts separately authorized fiscal resets | unchanged by ordinary Z |
| `grand_total_amount` | non-negative integer minor units plus currency | Accumulated approved GTA basis | exactly `previous + current contribution` |

No other report counter is required for Z v1. Report references and period sequences are separate identities and must not be stored as counter-family aliases.

Each state must preserve initialization evidence, state version, last transition operation, last closed period, last Z report, transition timestamp, actor/service reference, and immutable transition history. In-place manual correction is `PROHIBITED`. Recovery uses a separately authorized immutable recovery transition after continuity proof; reset and recovery are outside Z-007 runtime.

## 6. State Scope

Recommended canonical key:

```text
(site_pos_server_id, fiscal_identity_id, currency_code)
```

Rationale:

- Site POS Server is the runtime fiscal authority boundary.
- Fiscal identity prevents one registered taxpayer's state from affecting another.
- Currency prevents mathematically invalid accumulated totals.
- Report family is excluded because the state is specifically Z-close state.
- Fiscal series and sequence policy are excluded because one Z period may contain multiple governed ranges; those dimensions remain immutable report children.
- Site Group, terminal, cashier, channel, and business unit are excluded because the merged reporting scope is Site POS Server plus fiscal identity, not channel accountability.

The state must reference the active reporting contract version but contract version is not part of the uniqueness key. A contract upgrade requires an explicit compatible transition or migration, not a parallel accidental counter.

## 7. Initialization

Recommended initialization posture:

1. No state is created implicitly by Z close.
2. A dedicated privileged initialization operation establishes reset counter `0`, Z counter `0`, GTA `0`, state version `1`, currency, fiscal identity, Site POS Server, actor, correlation, reason, and timestamp.
3. Zero initialization is allowed only for a demonstrably new fiscal scope and requires explicit fiscal-administrator approval recorded durably.
4. Migrated scopes import verified existing reset, Z, and GTA values; they never fall back to zero.
5. Missing, duplicate, disabled, mismatched-currency, or continuity-uncertain state blocks issuance enforcement and Z close.
6. A new currency or fiscal identity is a new scope and requires separate initialization.
7. A disabled fiscal identity cannot initialize or close.

The first Z close reads previous values `(reset=0, z=0, gta=0)` only when that exact initialization is present. It produces `z=1`, leaves reset at `0`, and adds the approved period contribution to GTA.

## 8. Prior-Period Sequencing

Recommended rule:

- Z closes are strictly sequential within `(site, fiscal identity, currency)` by positive `period_sequence`.
- The immediate prior period is the greatest lower sequence in the same scope.
- `expected_prior_period_id` must equal that period.
- Every existing earlier period must be `CLOSED` and own one committed Z and one valid counter/GTA snapshot.
- A closed period without a valid Z is a continuity failure.
- An open or closing prior period blocks close.
- Calendar-date gaps are permitted only when no reporting-period record exists for the omitted dates; configured empty periods still require a Z and advance the Z counter.
- The first-ever period is allowed only when initialized state has no prior period/report reference and the period is explicitly marked as the first sequence for that scope.
- Missing or conflicting prior linkage blocks close and requires supervised recovery.

## 9. Shared Late-Write Protocol

Recommended lock identity:

```text
z-close-boundary:v1:<sitePosServerId>:<fiscalIdentityId>:<currencyCode>
```

The implementation derives a 64-bit PostgreSQL advisory key from the complete canonical string. Hash algorithm and seed are versioned and tested.

Required lock order for every fiscal creation, void, future refund/return/adjustment, and Z close:

1. begin database transaction;
2. resolve and validate Site POS Server, fiscal identity, and currency without writing fiscal facts;
3. acquire `pg_advisory_xact_lock` for the canonical scope;
4. resolve and lock the applicable reporting-period row;
5. revalidate configuration, scope, period status, interval, and prior linkage;
6. perform the source-affecting mutation or complete Z aggregate/close;
7. commit; transaction-scoped locks release automatically.

Z close locks the target period `FOR UPDATE` after the advisory lock, verifies `OPEN` and `now >= period_end_at`, aggregates the full interval, transitions state, and closes the period. Fiscal issuance or void arriving first completes before close can aggregate. A mutation arriving after close waits, then observes `CLOSED` and fails safely. No mutation is automatically assigned to a next period.

`READ COMMITTED` is recommended because the advisory lock excludes all relevant source writers before aggregation; each subsequent statement sees earlier committed writers. `REPEATABLE READ` acquired before a wait could retain a snapshot that predates a writer's commit. `SERIALIZABLE` alone is rejected because it does not define the fiscal ownership protocol and creates avoidable retry complexity.

Lock timeout or deadlock is retryable and returns no fiscal success. Unknown commit outcome is resolved by durable operation lookup before retry. Lock acquisition must be bounded and instrumented without logging fiscal payloads.

## 10. Period Lifecycle

Recommended v1 lifecycle:

- Durable states remain `OPEN` and `CLOSED` for the atomic close path.
- The close transaction sets `closing_started_at`, `closed_at`, and `CLOSED` together with the Z commit.
- `CLOSING` remains a reserved controlled value for a future externally coordinated or resumable workflow and is not durably committed by Z-007.
- Database/advisory locks represent in-progress close, avoiding a stranded durable `CLOSING` state.
- A committed `CLOSED` period is immutable and cannot reopen, reverse, delete, or amend.

## 11. Competing Close and Replay

| Situation | Required outcome |
|---|---|
| Same operation key, same semantic hash, committed Z exists | exact replay with original Z and counter/GTA facts |
| Same operation key, changed semantic hash | terminal semantic conflict; no mutation |
| Different operation key, same period, first close committed | terminal `PERIOD_ALREADY_CLOSED`/close-identity conflict; no replay under the new key |
| Simultaneous different operation keys | shared lock serializes; exactly one commits, the other conflicts after revalidation |
| Retry after client timeout | durable lookup; replay only for matching original operation/hash |
| Request status has unknown outcome | reconcile request, Z, period, and counter snapshot before any retry |
| No durable request/report after rollback | retryable according to safe failure classification |

Conflict responses never disclose semantic hashes, database identifiers, field differences, SQL, or constraint names. Authorized clients use the Z read route when given an existing public report reference.

## 12. Atomicity and Rollback

One PostgreSQL transaction must include:

1. shared scope lock and period row lock;
2. eligibility and prior-period revalidation;
3. complete period aggregation and reconciliation;
4. ranges and classified-gap validation;
5. Z state row lock and expected-version validation;
6. report request and scope;
7. immutable Z snapshot and all children;
8. immutable Z counter/GTA transition snapshot;
9. mutable canonical state update with version increment;
10. `OPEN -> CLOSED` period update;
11. operation result and privacy-safe audit evidence.

Failure at any stage rolls back every stage. A report reference, new counter, new GTA, or success response is returned only after durable commit. On uncertain commit, the API returns an unknown-outcome posture and performs read-only durable reconciliation before another close attempt.

## 13. Aggregation Compatibility and Fail-Closed Categories

Z reuses the X aggregation source rules for the complete `[period_start_at, period_end_at)` window: recorded Sales Invoice lines, governed taxes, applied statutory facts, commercial discounts, tenders, assigned sequence ranges, and classified gaps. Z readback always uses the stored immutable snapshot.

Additional Z rules:

- source membership is the durable reporting-period assignment, not a later wall-clock reconstruction;
- documents at period start are included and at period end are excluded;
- fiscal number text is never compared lexically;
- tender, line-net, authoritative total, and statutory final amount reconcile before close;
- current X void attribution based only on document creation time is insufficient for cross-period voids;
- cross-period void, refund, return, adjustment, unsupported service charge, or unexplained gap blocks close until its event/sign contract is governed;
- unsupported categories never become silent zeroes.

## 14. Required Approval Wording

The following wording is prepared for the user and BIR/accounting authority. Approval must be explicit; merging this document alone is not approval.

```text
I approve the ExitPass POS Server Z Close Counter/GTA and Late-Write Boundary
Contract v1.0 candidate as follows:

1. GTA current-period contribution is the reconciled VAT-inclusive final fiscal
   amount of qualifying recorded Sales Invoices assigned to the period.
2. Z/reset/GTA state is scoped by Site POS Server, fiscal identity, and currency.
3. New fiscal scopes require an explicit audited zero initialization; legacy
   scopes must import verified existing state and may not default to zero.
4. Z closes are sequential by period sequence; configured empty periods close
   and advance the Z counter.
5. Ordinary Z close advances Z counter once, does not advance reset counter,
   and updates GTA by the approved current-period contribution.
6. Fiscal mutations and Z close use the shared transaction-scoped advisory-lock
   and reporting-period row-lock protocol defined in this contract.
7. Z v1 commits OPEN to CLOSED atomically and does not persist CLOSING.
8. Cross-period void/refund/return/adjustment and unsupported service-charge
   cases remain fail-closed until separately approved.

Approval authority/reference: ____________________
Approved by: _____________________________________
Approved at: _____________________________________
```

Until this approval exists, GTA basis, counter families, state scope, initialization, prior-period sequencing, late-write protocol, and lifecycle candidate remain `DECISION_REQUIRED` and Z-007 cannot resume.

## 15. Explicit Exclusions

This contract does not implement or authorize API routes, RBAC, SQL, migrations, runtime state transitions, fiscal-document changes, rendering, printing, exports, BIR/Annex E output, EJ, POSLog, Controlled UAT, or production rollout.
