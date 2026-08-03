# ExitPass POS Server Reporting Integrity, Idempotency, Recovery, and Concurrency Review v1.0

## 1. Verdict

**NOT_READY.** Generic request, lock, state, recovery, and audit posture exists, but no report runtime connects those objects into enforceable behavior.

## 2. Failure and Concurrency Matrix

| Scenario | Database enforcement today | Application behavior today | Required behavior |
| --- | --- | --- | --- |
| Duplicate X generation | None | No runtime | Freeze stable-as-of or distinct-snapshot semantics. |
| Duplicate Z close | No unique close key | No runtime | One close per scope/period; exact retry returns original. |
| Simultaneous Z close | No executed lock/version transition | No runtime | Serialize by Site POS Server/period with deterministic loser outcome. |
| Issuance during close | No report/issuance coordination | No runtime | Define transaction cutoff and include each committed document exactly once. |
| Report read during close | No committed report API | No runtime | Return prior committed state or explicit processing status, never partial data. |
| Failure before commit | Generic transaction capability | No runtime | Roll back report, range, counters, state, audit, and outputs. |
| Commit with lost response | Generic recovery tables only | No runtime | Lookup by durable idempotency/close identity and return committed report. |
| Deadlock/transient DB error | Ordinary fiscal retry posture exists | No report behavior | Retry only safe pre-commit operations; expose safe retryability. |
| Restart during close | No report recovery worker | No runtime | Recover committed outcome or abandon uncommitted work without advancing state. |
| Report/reset collision | No governed unique identity | No runtime | Database uniqueness plus expected previous state. |
| Fiscal-number range mismatch | Gap audit posture only | No runtime | Fail close or classify governed gaps before committing. |
| Late fiscal document | No business-day rule | No runtime | Apply a frozen next-period/adjustment policy. |
| Adjustment after close | Adjustment schema only | No runtime | Preserve closed snapshot and post an approved subsequent-period adjustment. |
| Missing statutory row | Ordinary documents legitimately omit it | No report query | Require it only when statutory discount facts claim an applied privilege. |
| Tender/rounding mismatch | Document-level validations exist | No report validation | Reconcile report source rows before close. |

## 3. Existing Enforcement Versus Gaps

Existing fiscal-document issuance provides strong transaction, idempotency, semantic conflict, numbering, and immutable statutory snapshot patterns. Those patterns are reusable design evidence, but report tables do not currently have equivalent idempotency records, semantic hashes, unique close identities, immutable triggers, or repository services.

The rollback-only database proof established that duplicate and mutable X/Z rows are currently accepted. Generic foreign keys also permit a code from the wrong family.

## 4. Required Invariants

- A Z close is a single atomic state transition tied to an expected prior period/counter state.
- Exactly one committed report owns a Site POS Server/period/Z sequence identity.
- Exact replay is side-effect-free; semantic conflict mutates nothing.
- X never advances close state; Z never recomputes from a mutable current profile after commit.
- Every qualifying fiscal document belongs to exactly one governed closed period.
- Report facts, range, gaps, counters, and presentation inputs become immutable together.
- Unknown commit outcome is recoverable by public operation identity.
- Outputs are published only from committed reports, with deterministic retry and checksum evidence.

## 5. Validation Required Before UAT

Use disposable PostgreSQL concurrency tests, process termination during close, forced persistence failures at each write boundary, API lost-response replay, fiscal issuance races, and direct immutability/uniqueness proofs. Controlled UAT must not be the first place these invariants are exercised.

