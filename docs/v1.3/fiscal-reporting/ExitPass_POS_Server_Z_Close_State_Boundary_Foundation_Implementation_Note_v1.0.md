# ExitPass POS Server Z Close State and Boundary Foundation Implementation Note v1.0

## 1. Status

Z-007B implements the governed schema and runtime foundation for canonical Z/reset/GTA state and the shared fiscal close boundary. The approved authority is ExitPass v1.3 Product and Fiscal Design Authority, approved by Darwin Pasco at `2026-08-04 08:26 PHT`.

This slice does not generate a Z Reading, aggregate a Z report, mutate counter/GTA through a close, close a reporting period, or expose Z generation/readback APIs. Z-007 remains blocked until this branch merges.

## 2. Approved Decisions Implemented

- GTA state is an integer-minor-unit cumulative value for the approved VAT-inclusive final fiscal amount. Z-007 will calculate the current contribution; Z-007B only provides state and transition controls.
- Canonical state scope is `(site_pos_server_id, fiscal_identity_id, currency_code)`.
- New scope zero initialization and verified legacy import are explicit, privileged, idempotent operations.
- Period sequencing uses positive `period_sequence` and `expected_prior_period_id` in the same scope.
- Fiscal creation, fiscal void, and future Z close share `z-close-boundary:v1` transaction advisory locking followed by reporting-period row locking.
- Ordinary Z transition semantics increment Z once, preserve reset, and add current contribution to GTA. Z-007B models and validates this but does not execute a close.
- Period lifecycle remains atomic `OPEN -> CLOSED`; Z-007B never writes `CLOSING` or `CLOSED` as a close operation.
- Refund, return, adjustment, unsupported service charge, and cross-period void remain fail closed.

## 3. Canonical Schema

### 3.1 State and Evidence

`pos.fiscal_z_close_states` is the single canonical state row per Site POS Server, fiscal identity, and currency. It stores contract version, reset counter, Z counter, GTA, optimistic version, prior period/Z linkage, initialization provenance, approval, actor/service, and last transition identity.

`pos.fiscal_z_close_state_transitions` is append-only operation and predecessor evidence. `pos.fiscal_z_close_state_transition_values` stores exactly one immutable value for each governed identity: `z_counter`, `reset_counter`, and `grand_total_amount`.

Direct state deletion is rejected. Direct update is rejected unless performed through the governed transition context. Deferred evidence checks require one matching transition and exactly three controlled values. `pos.apply_fiscal_z_close_state_transition(uuid)` applies a prepared Z transition only when the stored expected version still matches and advances state exactly once. Z-007B exposes a repository primitive but does not call it from an endpoint.

### 3.2 Controlled Families

The governed families are:

| Family | Values |
|---|---|
| `fiscal_z_state_identity` | `z_counter`, `reset_counter`, `grand_total_amount` |
| `fiscal_z_state_initialization_provenance` | `approved_new_scope_zero`, `verified_legacy_import` |
| `fiscal_z_state_transition_type` | `initialization`, `z_close` |

Stable UUIDv5 identities are generated from repository source JSON. Foreign keys and explicit family checks reject wrong-family values. No display label is used as authority.

### 3.3 Z Snapshot Linkage

`pos.fiscal_z_counter_snapshots` now references canonical state and stores expected/resulting versions. Its constraints require Z increment by one, reset preservation, and resulting GTA equal previous GTA plus current-period amount. Existing non-empty environments with missing canonical linkage or reset-advancing legacy rows fail migration and require verified import; the upgrade does not invent state.

## 4. Initialization API and Authorization

`POST /v1/admin/fiscal-z-close-states/initialize` accepts an opaque operation reference, Site POS Server ID, fiscal identity ID, currency, provenance, supplied values, approval reference, and safe actor/service/correlation references.

Dedicated policy `FiscalZCloseStateInitialization` requires server-derived permission `fiscal_z_close_state.initialize` and server-derived Site POS Server and fiscal identity scopes. X Reading, ordinary fiscal-document, APT, WebPay, cashier, reconciliation, and broad Management Platform authority do not grant initialization. Caller-supplied permission text is not authorization authority.

Approved new scope requires reset `0`, Z `0`, GTA `0`, version `1`. Verified legacy import preserves explicitly supplied verified values. Same operation and semantic facts replay; changed semantics return conflict. Semantic hash source excludes actor, service, correlation, request bodies, credentials, and customer facts.

## 5. Fiscal Reporting-Period Assignment

Fiscal documents now carry first-class `currency_code` and `fiscal_reporting_period_id`. The composite foreign key binds assignment to the same Site POS Server, fiscal identity, and currency. An insert trigger requires one OPEN period and an event timestamp inside `[period_start_at, period_end_at)`. Updates cannot change assignment, scope, currency, or fiscal event timestamp.

Upgrade is expand-and-validate:

1. add nullable columns before dependent comments and constraints;
2. derive currency only when all first-class monetary children agree;
3. assign a period only when exactly one scope/window candidate exists;
4. leave ambiguous legacy rows unassigned;
5. require assignment for all feature-created writes;
6. block future Z close when any qualifying source document remains unassigned.

There is no automatic next-period routing. A document at `period_end_at` is rejected from the prior period.

## 6. Shared Boundary Protocol

The lock identity is:

```text
z-close-boundary:v1:<sitePosServerId>:<fiscalIdentityId>:<uppercaseCurrency>
```

SHA-256 over the canonical identity produces a deterministic signed 64-bit advisory key. The canonical string and key are not returned publicly.

Transaction order is:

1. begin `READ COMMITTED` transaction;
2. resolve Site POS Server, fiscal identity, and currency;
3. acquire bounded transaction advisory lock;
4. resolve and row-lock the reporting period;
5. revalidate scope, currency, half-open window, and `OPEN` state;
6. perform fiscal source writes;
7. commit all or none.

Fiscal creation acquires the boundary before number allocation and the first fiscal-document write. Fiscal void resolves the immutable assignment, acquires the same boundary, locks/revalidates that period, and only then mutates void state. If a synthetic close boundary commits `CLOSED` first, waiting issuance or void fails without a number or partial rows. If issuance holds the lock first, a competing boundary waits and observes the complete committed document.

## 7. Transition and Sequencing Foundation

`PostgresFiscalZCloseTransitionFoundation` provides bounded Z-007 prerequisites:

- read canonical state `FOR UPDATE` by exact scope;
- reject missing initialization;
- resolve immediate prior period by `period_sequence - 1`;
- require `expected_prior_period_id` consistency and a prior CLOSED period with Z, except initialized sequence one;
- apply a prepared transition through the database expected-version function;
- classify stale version safely.

No method creates a Z report or closes a period.

## 8. Unsupported Writers

`FiscalPeriodMutationGuard` permits only current governed creation and void foundations. Refund, return, adjustment, and service-charge mutation kinds throw `UnsupportedCrossPeriodMutation`. No such API writer exists in the repository. Any future writer must use the shared coordinator and receive a separately approved attribution/sign contract before activation.

## 9. Safe Errors, Audit, and Privacy

Public failures distinguish initialization, scope, period, lock, stale-version, replay/conflict, and unsupported-mutation postures without SQL, constraint/table names, lock strings, hashes, stack traces, credentials, or connection details.

Durable initialization evidence contains only opaque operation, approval, actor/service, and correlation references. Logs and responses contain no customer payload, statutory identity, evidence, payment credential, semantic source, or raw semantic hash.

## 10. Validation Evidence

PostgreSQL 16 disposable validation proves:

- clean rebuild, controlled-code load, inventory, drift, and reporting schema proof;
- state-scope uniqueness, correct-family enforcement, direct state update/delete rejection, and governed transition posture;
- valid assignment, scope/currency/window rejection, assignment immutability, and rollback;
- origin/dev-to-feature additive upgrade and a second feature replay;
- clean and upgraded logical schema fingerprints are equal;
- initialization authorization, version-one zero initialization, exact replay, semantic conflict, duplicate race, immutable evidence, and currency-isolated advisory locks;
- actual fiscal creation and void assignment, closed-period denial, issuance-first and boundary-first concurrency, no partial document, and no number allocation on denial;
- merged X Reading PostgreSQL integration remains compatible.

Generated evidence is local and ignored. Synthetic fixtures contain no customer or production payment data.

## 11. Exclusions and Readiness

Z-007B does not implement Z authorization, Z API, aggregation, report/child persistence, counter/GTA close transition, period close, readback, rendering, printing, export, BIR, Annex E, EJ, or POSLog.

After this branch merges, Z-007 may resume against these canonical artifacts and must still prove complete aggregation, privileged close, one-time state transition, competing close, atomic `OPEN -> CLOSED`, replay/conflict, unknown-outcome recovery, and readback equality.

Controlled UAT is not authorized. Production rollout is not authorized.
