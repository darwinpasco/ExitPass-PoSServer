# ExitPass POS Server Z Reading and Fiscal Period Close Capability Matrix v1.0

## 1. Verdict

| Layer | Verdict |
| --- | --- |
| Shared X/Z snapshot schema | PARTIALLY_READY |
| Closed-period schema | NOT_IMPLEMENTED |
| Close runtime | NOT_IMPLEMENTED |
| API | NOT_IMPLEMENTED |
| Rendering/print/export | NOT_IMPLEMENTED |
| Recovery/idempotency | NOT_IMPLEMENTED |
| Controlled-UAT readiness | NOT_READY |

A shared table is not evidence of a Z close. The repository has no atomic period-close command, no immutable closed-period identity, no counter transition, and no replay protocol.

## 2. Close Capability Matrix

| Capability | Current evidence | Status | Required follow-up |
| --- | --- | --- | --- |
| Controlled close authority | No report policy or permission | NOT_IMPLEMENTED | Introduce a dedicated server-derived close permission. |
| Concurrency lock | `fiscal_lock_states` posture exists; no runtime | NOT_IMPLEMENTED | Define lock scope and database transaction behavior. |
| Duplicate-close protection | No unique close identity or idempotency key | NOT_IMPLEMENTED | Enforce one close per Site POS Server/period and deterministic replay. |
| Exact reporting window | Nullable scope timestamps | PARTIALLY_READY | Freeze inclusive/exclusive boundaries and source clock. |
| Business date/timezone | Nullable date; no timezone/cutoff configuration | NOT_IMPLEMENTED | Govern local business-day derivation and DST/clock posture. |
| Late transaction handling | No policy or runtime | NOT_IMPLEMENTED | Decide rejection, next-period assignment, or governed adjustment. |
| Open/incomplete fiscal documents | No reporting readiness query | NOT_IMPLEMENTED | Fail closed on incomplete/uncertain documents as governed. |
| Period sequence/reset counter | State tables exist; no transition | PARTIALLY_READY | Add atomic expected-version transition and durable snapshot. |
| Previous/current accumulated totals | State/snapshot posture only | PARTIALLY_READY | Freeze GTA formulas and reconcile transition values. |
| Fiscal-number range/gaps | Document fields and gap audit exist | PARTIALLY_READY | Detect and classify missing, voided, failed, and reserved numbers. |
| Immutable closed snapshot | X/Z row is directly mutable/deletable | NOT_IMPLEMENTED | Add immutability and retention enforcement. |
| Deterministic report number | No field or rule | NOT_IMPLEMENTED | Define stable report identity and collision prevention. |
| Crash/lost-response recovery | Recovery tables exist only generically | NOT_IMPLEMENTED | Add commit-outcome lookup and exact replay behavior. |
| Readback/history | No endpoint or repository | NOT_IMPLEMENTED | Add scoped ID/history reads after close is durable. |
| Render/print/reprint/export | No implementation | NOT_IMPLEMENTED | Preserve one authoritative persisted presentation input. |
| Audit trail | No close-specific audit writer | NOT_IMPLEMENTED | Persist actor, correlation, request, result, and state transition. |
| Rollback posture | No command exists | NOT_IMPLEMENTED | Prove all report/counter/range writes roll back atomically. |
| Reopen/mutation prohibition | No database enforcement | NOT_IMPLEMENTED | Closed periods and snapshots must never reopen or update. |

## 3. Required Atomic Boundary

One database transaction must establish the close identity, lock/expected state, exact period window, qualifying fiscal-document set, fiscal-number range and gaps, aggregate snapshot, previous and resulting counters/GTA, report history, and audit evidence. The response must be derived from the committed snapshot.

An exact retry after timeout must return that snapshot. A different request for the same close identity must conflict safely. A failure before commit must leave no close, report, counter advance, or partial output. A committed close with a lost response must be discoverable without closing again.

## 4. Current High-Risk Contradictions

- Two rows can currently reference the same report request/scope.
- A report-status code can be stored as report kind because the FK does not enforce a family.
- Stored X/Z monetary values can be updated.
- No schema object proves whether a period is open or closed.
- No unique key binds Site POS Server, business date, period sequence, and Z number.
- No runtime synchronizes document creation with the close cutoff.

## 5. Required Tests and Proof

- Simultaneous close attempts, exact replay, changed-request conflict, deadlock retry, restart during close, and lost response.
- Fiscal issuance concurrent with close at both sides of the cutoff.
- Missing-number, void, failed issuance, late document, and adjustment-after-close cases.
- Atomic rollback of report, status, counters, GTA, and audit rows.
- Immutable direct update/delete rejection and parent retention behavior.
- Original, reprint, export, and reconciliation use the same committed snapshot.

