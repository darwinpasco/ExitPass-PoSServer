# ExitPass POS Server Z Reading Runtime Resume Acceptance Criteria v1.0

## 1. Decision

Z-007 resume gate status: `SATISFIED_BY_MERGED_Z_007B`.

The authority gates were approved by the ExitPass v1.3 Product and Fiscal Design Authority, Darwin Pasco, at `2026-08-04 08:26 PHT`. Z-007B merged as commit `a20a2dc99d95cba99be25991a30f0f6a70df9648`; its implementation evidence is tracked in `ExitPass_POS_Server_Z_Close_State_Boundary_Foundation_Implementation_Note_v1.0.md`.

This checklist is the mandatory gate for resuming executable Z Reading work. A checked documentation item without merged implementation and proof is insufficient.

## 2. Authority Gates

- [x] The GTA contribution basis is approved using the exact companion decision wording.
- [x] Distinct Z counter, reset counter, and GTA semantics are approved.
- [x] State scope `(Site POS Server, fiscal identity, currency)` is approved.
- [x] Initialization and verified legacy-import rules are approved.
- [x] Strict prior-period sequencing is approved.
- [x] The shared advisory-lock and period-assignment protocol is approved.
- [x] Atomic `OPEN -> CLOSED` without durable `CLOSING` is approved.
- [x] Competing-close and unknown-outcome behavior is approved.
- [x] Cross-period void/refund/return/adjustment cases are explicitly accepted as fail-closed for this slice.

Approval must identify authority, approver, timestamp, and approval reference. The documents do not claim BIR approval.

## 3. Z-007B Schema Gates

- [x] Governed canonical Z state exists separately from generic unlabeled state.
- [x] State is unique by Site POS Server, fiscal identity, and currency.
- [x] Z counter, reset counter, GTA, state version, initialization provenance, prior period/Z, and last operation are first-class.
- [x] Stable controlled families and correct-family constraints exist.
- [x] Fiscal documents have immutable first-class reporting-period assignment.
- [x] Period assignment is scope/window/currency consistent.
- [x] Z transition snapshot records canonical state and expected/resulting version.
- [x] CLOSED period, committed Z, and transition evidence are durably linked.
- [x] Parent deletion and direct evidence mutation are prohibited.
- [x] Existing data upgrade fails closed on ambiguous assignment or unverified counter state.
- [x] Clean rebuild, upgrade, replay, inventory, drift, direct constraints, and rollback proof pass.

## 4. Z-007B Runtime Gates

- [x] Explicit privileged state initialization exists and is idempotent.
- [x] One shared transaction-scoped close-boundary lock coordinator exists.
- [x] Fiscal creation acquires the lock before period assignment or fiscal source writes.
- [x] Fiscal void acquires the lock before period-affecting mutation.
- [x] Unsupported future adjustment writers fail closed.
- [x] Writers revalidate OPEN period and assignment after lock acquisition.
- [x] Writers never auto-route to another period after close.
- [x] Lock ordering and timeout/retry behavior are deterministic.
- [x] State transitions compare expected version and retain predecessor evidence.
- [x] Safe errors and privacy-safe audit evidence are implemented.

## 5. Required Boundary Proof

- [x] Issuance-first concurrency: close waits and includes the fully committed document.
- [x] Close-first concurrency: issuance waits, sees CLOSED, and commits no fiscal facts or number.
- [x] No partial document is visible to aggregation.
- [x] No document is committed to a period after its final Z aggregation boundary.
- [x] No document is dropped or counted in two periods.
- [x] Another Site POS Server, fiscal identity, and currency are unaffected.
- [x] Forced failure rolls back assignment, state, transition, and audit facts.
- [x] Restart and retry reconcile operation identity before state mutation.

## 6. Resumed Z-007 Acceptance Scope

Only after Sections 2 through 5 pass may Z-007 implement:

- dedicated privileged Z-close and scoped read authorization;
- exact period eligibility and predecessor validation;
- complete `[start,end)` aggregation using the approved source rules;
- fiscal range/gap and tender/statutory reconciliation;
- one expected-version Z counter and GTA transition;
- immutable Z snapshot and children;
- atomic period `OPEN -> CLOSED` transition;
- exact replay, semantic conflict, competing close, and unknown-outcome recovery;
- stored readback without recomputation;
- disposable API/PostgreSQL concurrency and no-unrelated-mutation proof.

## 7. Z-007 Stop Conditions

Z-007 must stop and report a blocker if any of these remains true:

- approval wording is absent or partial;
- GTA source does not reconcile to Z net sales;
- state is missing, unverified, duplicated, wrong currency, or stale version;
- prior period/Z/transition is unresolved;
- a qualifying fiscal document lacks unambiguous period assignment;
- creation or void can bypass the shared lock;
- an unsupported cross-period fiscal event exists;
- an unexplained sequence gap remains;
- close cannot return success only after durable commit.

## 8. Validation Required Before Z-007 Completion

Resumed Z-007 must pass full restore/build/test, focused API/runtime/PostgreSQL tests, DB static and Docker-backed validation, exact replay/conflict, competing close, counter/GTA once-only transition, failure injection, restart recovery, readback equality, and before/after no-unrelated-state manifests.

Rendering, printing, export, BIR, Annex E, EJ, POSLog, Controlled UAT, and production rollout remain outside the resume authorization.

## 9. Readiness Record

| Capability | Current posture |
|---|---|
| Decision package | Complete and explicitly approved |
| Z-007B schema/runtime foundation | Merged at `a20a2dc99d95cba99be25991a30f0f6a70df9648` |
| Z Reading runtime | Implemented by Z-007; see implementation note and validation evidence |
| Atomic period close | Implemented by Z-007; see implementation note and validation evidence |
| Controlled UAT | Not authorized |
| Production rollout | Not authorized |
