# ExitPass POS Server Z Close Schema and Runtime Handoff v1.0

## 1. Handoff Status

Status: `READY_FOR_Z_007B_AFTER_DECISION_APPROVAL`.

This document defines the smallest additive prerequisite for a truthful Z close. It contains no SQL or runtime implementation. All recommendations depend on approval of the companion Z Close Counter/GTA and Late-Write Boundary Contract v1.0.

## 2. Blocker Being Resolved

The current schema can persist an immutable Z transition snapshot but cannot safely own the transition:

- `pos.fiscal_counter_states` is unique only by Site POS Server and generic family, allows JSON context, and lacks fiscal identity, currency, version, predecessor, and transition identity.
- no governed counter-family source establishes canonical Z, reset, and GTA state identities;
- `pos.fiscal_z_counter_snapshots` proves arithmetic but is not linked to a versioned canonical mutable state;
- fiscal documents have no first-class reporting-period assignment;
- fiscal creation and void transactions do not share a close-boundary lock;
- locking `pos.fiscal_reporting_periods` only in Z close cannot exclude a later fiscal writer.

Z-007B must resolve these defects before Z-007 resumes.

## 3. Additive Canonical State

### 3.1 Dedicated Z Close State

Add one first-class state object, provisionally `pos.fiscal_z_close_states`, with repository-approved naming. It must contain:

- stable state ID;
- Site POS Server ID;
- fiscal identity ID;
- currency code;
- reporting contract version;
- reset counter value;
- Z counter value;
- GTA amount in minor units;
- optimistic state version;
- last closed reporting-period ID, nullable only for initialized state;
- last committed Z report ID, nullable only for initialized state;
- initialization timestamp, actor/service reference, reason/reference, and provenance posture;
- last transition operation reference and transition timestamp;
- created and updated timestamps.

Required database behavior:

- exactly one row per `(site_pos_server_id, fiscal_identity_id, currency_code)`;
- non-negative counters and GTA;
- uppercase three-character currency;
- fiscal identity and Site POS Server references must be valid;
- version increments exactly once per committed Z transition;
- predecessor period/Z and operation references cannot be silently removed;
- direct delete is prohibited;
- updates are restricted to the governed transition function/statement posture and must compare expected version;
- generic JSON, notes, or metadata are prohibited as transition authority.

Do not repurpose existing generic rows as authoritative without a verified migration. `pos.fiscal_counter_states` remains legacy/posture-only until a separate compatibility decision retires it.

### 3.2 Governed State Identities

Add stable controlled-code identities, or equivalent first-class typed semantics, for:

- `z_counter`;
- `reset_counter`;
- `grand_total_amount`.

Any controlled-code FK must enforce the correct family. Do not resolve state using generic code IDs or display labels. The dedicated state may use explicit columns while transition/audit records use these semantic identities.

### 3.3 Initialization Evidence

Add append-only initialization/transition evidence sufficient to prove:

- state scope and contract version;
- initialized values;
- source as approved zero initialization or verified legacy import;
- actor/service and approval reference;
- operation idempotency and semantic hash/version;
- expected and resulting state version;
- durable outcome and safe correlation reference.

Initialization must be idempotent. Same operation/same semantics replays; changed semantics conflict. It must not be executed implicitly by Z close.

## 4. Reporting-Period and Z Snapshot Linkage

Add or strengthen canonical relationships so that:

- the state row identifies its last closed period and committed Z;
- the next period identifies its expected prior period;
- a Z transition snapshot references canonical state, expected state version, resulting state version, prior period, and prior Z;
- one committed Z owns one transition snapshot;
- one period owns at most one committed Z;
- a CLOSED period has exactly one governing committed Z and transition evidence;
- parent deletes cannot remove report, period, or transition evidence.

Resolve circular foreign-key ordering through repository-standard deferred/additive constraints, not nullable evidence after commit.

## 5. First-Class Fiscal Period Assignment

Add `fiscal_reporting_period_id` (or the exact approved name) to the authoritative fiscal document with:

- FK to `pos.fiscal_reporting_periods`;
- an index supporting period aggregation and writer revalidation;
- scope consistency with Site POS Server, fiscal identity, business date/window, and currency;
- immutable assignment after fiscal-document commit;
- no cascading delete;
- an upgrade path for existing documents.

Upgrade posture must be expand-and-validate:

1. add nullable assignment;
2. backfill only by a deterministic, audited mapping whose period/scope/window is unambiguous;
3. report and block ambiguous legacy rows rather than guessing;
4. validate scope and FK constraints;
5. require assignment for new fiscal writes once the close-boundary feature is enabled;
6. permit Z close only when every qualifying source document is assigned and enforcement is active.

`created_at` filtering alone must not remain the close authority.

## 6. Shared Close-Boundary Coordinator

Implement one shared coordinator used by fiscal-document creation, void, every future period-affecting adjustment writer, and Z close.

Canonical lock identity:

```text
z-close-boundary:v1:<site-pos-server-id>:<fiscal-identity-id>:<currency>
```

The actual PostgreSQL advisory key must be derived deterministically with an existing approved hash primitive. It must not use process-local locking.

Required writer order:

1. begin transaction at `READ COMMITTED`;
2. resolve Site POS Server, fiscal identity, and currency;
3. acquire transaction-scoped advisory lock;
4. resolve exactly one governed reporting period;
5. lock/revalidate the period is OPEN and assignment is in `[start,end)`;
6. persist fiscal document and period assignment, or apply the governed period-affecting action;
7. commit.

Required close order:

1. begin transaction at `READ COMMITTED`;
2. resolve scope and target period;
3. acquire the same transaction-scoped advisory lock;
4. lock period and canonical state rows in a fixed order;
5. revalidate period, predecessor, assignment completeness, and expected state version;
6. aggregate assigned committed facts and validate ranges/gaps/reconciliation;
7. persist Z request, scope, immutable report and children;
8. persist transition snapshot and versioned state update;
9. transition period `OPEN -> CLOSED` and link governing Z;
10. persist operation outcome and audit evidence;
11. commit before returning success.

Lock order is always advisory scope lock, reporting-period row, canonical Z state row, then report-owned rows. Timeouts map to safe retryable errors. A transaction waiting on the lock must re-resolve/revalidate after acquisition.

## 7. Runtime Components Required in Z-007B

Z-007B must deliver only the prerequisite foundation:

1. typed Z state repository with expected-version compare-and-transition;
2. explicit initialization command/service and authorization boundary;
3. controlled family resolver;
4. shared transaction-scoped lock coordinator;
5. reporting-period assignment resolver/guard;
6. integration into fiscal-document creation before the first fiscal source write;
7. integration into fiscal-document void before period-affecting mutation;
8. fail-closed hooks for unsupported future refund/return/adjustment writers;
9. safe errors for missing initialization, unavailable period, CLOSED period, lock timeout, version conflict, and ambiguous legacy assignment;
10. privacy-safe transition and denial audit evidence.

Z-007B must not implement Z API, Z aggregation, Z snapshot generation, rendering, printing, export, BIR, Annex E, EJ, or POSLog.

## 8. Schema Validation Requirements

Use state-based SQL and disposable PostgreSQL 16 proof. Validate:

- clean rebuild and expected inventory;
- origin/dev-to-feature upgrade and second replay;
- controlled-code family loading and wrong-family rejection;
- state scope uniqueness and cross-currency/identity isolation;
- explicit initialization replay and semantic conflict;
- expected-version success and stale-version rejection;
- predecessor and state/report linkage;
- fiscal-document period-assignment scope and immutability;
- legacy backfill ambiguity rejection;
- retention and parent-delete protection;
- no generic JSON/notes/metadata authority;
- rollback leaves state, assignment, and transition evidence unchanged.

## 9. Runtime Concurrency Proof

Required database-backed scenarios:

1. issuance acquires the scope lock first; close waits and later observes the committed document;
2. close acquires first; issuance waits, observes CLOSED, and fails without fiscal rows or number allocation;
3. two state initializations: one commit, exact replay or conflict as appropriate;
4. stale expected state version is rejected;
5. one identity/currency lock does not block or mutate another scope;
6. lock timeout is safe and retryable;
7. forced failure rolls back fiscal assignment or state transition in full;
8. restart/retry reconciles durable operation identity before mutation.

The proof must compare fiscal documents, numbering, status history, periods, state, Z snapshots, and transition evidence before and after each case.

## 10. Security and Privacy

State and audit evidence may contain only opaque operation, period, report, actor/service, and correlation references. It must not contain beneficiary identity, statutory IDs, evidence, reviewer data, payment credentials, connection secrets, request bodies, semantic source strings, or raw hashes in public errors.

## 11. Exact Handoff to Resumed Z-007

Z-007 may resume only after:

- the candidate decisions are explicitly approved;
- Z-007B schema and runtime foundation merges;
- new fiscal creation and void operations participate in the shared boundary;
- legacy assignment and initialized state gates are enforceable;
- disposable concurrency and rollback proof passes.

Resumed Z-007 then owns privileged close authorization, complete-period aggregation, counter/GTA transition, Z snapshot/children, atomic OPEN-to-CLOSED transition, readback, replay/conflict, and its own runtime proof.

Controlled UAT and production rollout remain unauthorized.
