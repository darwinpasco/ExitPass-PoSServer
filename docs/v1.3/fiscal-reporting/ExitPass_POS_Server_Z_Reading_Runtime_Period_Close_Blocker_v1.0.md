# ExitPass POS Server Z Reading Runtime and Period Close Blocker v1.0

## Resolution record

This document is retained as historical evidence of the original Z-007 stop decision. The blocker was resolved on `2026-08-04` through the approved Z-007A decision package and merged Z-007B foundation commit `a20a2dc99d95cba99be25991a30f0f6a70df9648`.

The merged foundation supplies the approved VAT-inclusive final-fiscal-amount GTA basis, canonical scoped and versioned Z/reset/GTA state, explicit initialization, immutable transition evidence, fiscal-document reporting-period assignment, and the shared issuance/void/close boundary protocol. Z-007 subsequently implemented the privileged close and stored readback described in `ExitPass_POS_Server_Z_Reading_Runtime_Implementation_Note_v1.0.md`.

The original blocker classification below is historical and no longer describes the current branch. Rendering, printing, export, BIR, Annex E, EJ, POSLog, Controlled UAT, and production rollout remain excluded.

## 1. Outcome

Z-007 is blocked before public endpoint activation and before any period, counter, GTA, or fiscal-issuance mutation is introduced.

Blocker classification: `BLOCKED_BY_Z_COUNTER_GTA_AND_LATE_WRITE_CONTRACT`.

The merged reporting schema can persist an immutable Z snapshot and a Z counter/GTA transition snapshot. It does not define enough governed state or runtime coordination to execute the transition safely. A Z close is irreversible, so this branch does not infer the missing fiscal rules.

## 2. GTA Formula Is Not Frozen

`contracts/pos-server/fiscal-reporting-internal-contract.v1.json` and the internal contract document enforce only:

```text
resulting GTA = previous GTA + current period amount
```

They do not define `current period amount`. In particular, the approved material does not select gross sales, net sales, tender-paid amount, VAT-inclusive sales, or another fiscal basis as the GTA contribution.

The product BRD remains explicit that the exact X/Z aggregation model requires BIR/accounting confirmation (`XZ-012`). The schema comment on `pos.fiscal_counter_states.monetary_amount_minor_units` likewise states that no final calculation formula is implemented.

Choosing a source in Z-007 would invent a fiscal accounting rule and could irreversibly advance the wrong accumulated total.

## 3. Governed Counter State Is Absent

`pos.fiscal_counter_states` is generic posture storage. The current merged repository has no controlled-code source family with canonical identities for:

- reset counter;
- Z counter;
- Grand Total Amount.

The table references any `pos.controlled_codes` row and does not enforce a counter family. Runtime therefore cannot resolve the required rows without inventing code values or accepting a wrong-family code.

The state identity is unique only by `(site_pos_server_id, counter_family_code_id)`. It has no fiscal-identity or currency component, although the Z contract and reporting period are scoped by Site POS Server, fiscal identity, and currency. This prevents proof that closing one fiscal identity or currency leaves another identity unaffected.

The table also has no expected version or previous-period reference. `pos.fiscal_z_counter_snapshots` can prove a supplied relationship after the fact, but it does not establish which mutable state rows are authoritative or how their first values are initialized.

## 4. Late Fiscal-Document Boundary Is Not Established

`PostgresFiscalDocumentRepository.CreateAsync` does not read or lock `pos.fiscal_reporting_periods` and does not participate in a close-scoped advisory-lock protocol. Fiscal creation currently allocates a number and inserts the fiscal aggregate without a reporting-period boundary guard.

A Z transaction can lock the reporting period, but that lock alone cannot prevent a concurrent fiscal-document transaction from committing a document in `[period_start_at, period_end_at)` after Z aggregation and before the close commit. This violates the required complete-period snapshot.

The prerequisite must freeze and implement one shared database protocol. A suitable posture to evaluate is:

1. fiscal issuance identifies the applicable configured reporting period using its authoritative commit/assignment instant;
2. issuance and close acquire the same Site/period database lock in one documented order;
3. issuance revalidates the period is `OPEN` under that lock;
4. close waits for earlier issuance, then establishes `CLOSING`/`CLOSED` so later issuance cannot enter the period;
5. missing period configuration preserves the separately governed non-enforced development posture rather than silently bypassing a configured closed period.

The exact behavior for a transaction arriving after cutoff remains `FAIL_CLOSED_PENDING_GOVERNED_SUBSEQUENT_PERIOD_ADJUSTMENT`; Z-007 must not invent reassignment to a later period.

## 5. Additional Contract/Schema Decisions Required

The prerequisite must also freeze and enforce:

- canonical counter family codes and correct-family foreign keys;
- initial reset, Z, and GTA state creation/readiness;
- counter state scope, including fiscal identity and currency;
- exact GTA contribution and reconciliation source;
- expected-version or prior-Z transition concurrency;
- reset counter behavior on Z close (the BRD says it remains unchanged);
- same-scope prior-period ordering and linkage;
- already-closed behavior for a different operation key;
- durable unknown-commit recovery lookup;
- the shared fiscal-issuance/period-close lock order;
- whether `OPEN -> CLOSING -> CLOSED` must be persisted or `OPEN -> CLOSED` is the only committed lifecycle evidence.

## 6. Required Prerequisite Sequence

### Z-007A: Counter, GTA, and Close-Boundary Contract Freeze

- Repository: `D:\SourceCodes\ExitPass-PoSServer`
- Proposed branch: `docs/fiscal-z-close-counter-gta-boundary-contract`
- Owner: Codex Z with BIR/accounting decision authority
- Deliverable: approved GTA formula, counter identities/scope/initialization, prior-period rule, late-write lock protocol, and replay/competing-close decisions

### Z-007B: Counter State and Close-Boundary Schema Hardening

- Repository: `D:\SourceCodes\ExitPass-PoSServer`
- Proposed branch: `feature/fiscal-z-close-counter-gta-boundary-schema`
- Dependency: merged Z-007A
- Deliverable: governed controlled codes, family-enforced/scoped counter state, optimistic or row-locked transition identity, fiscal-issuance close guard support, inventory/drift/proof updates

### Z-007 Resumption

After both prerequisites merge, resume `feature/fiscal-z-reading-runtime-period-close` and implement the API, privileged close, atomic aggregate/counter/GTA transition, immutable readback, competing-close behavior, and disposable PostgreSQL proof.

## 7. Changes Deliberately Not Made

- no Z API route or authorization policy;
- no report request or snapshot write;
- no reporting-period lifecycle transition;
- no fiscal counter or GTA update;
- no fiscal-document creation change;
- no SQL, migration, controlled-code, inventory, or drift change;
- no X Reading behavior change;
- no rendering, printing, export, BIR, Annex E, EJ, or POSLog behavior.

## 8. Readiness

- Historical prerequisite blocker: resolved by approved Z-007A and merged Z-007B.
- Z Reading runtime: implemented by Z-007, subject to the validation evidence in the implementation note.
- Privileged atomic period close: implemented by Z-007, subject to the validation evidence in the implementation note.
- X Reading runtime: unchanged.
- Controlled UAT: not authorized.
- Production rollout: not authorized.
