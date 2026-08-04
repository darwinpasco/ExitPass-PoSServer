# ExitPass POS Server Z Close Counter and GTA Decision Record v1.0

## 1. Purpose and Status

Status: `APPROVED`.

Approval authority/reference: `ExitPass v1.3 Product and Fiscal Design Authority`.

Approved by: `Darwin Pasco`.

Approved at: `2026-08-04 08:26 PHT`.

The approval covers DR-001 through DR-009 and the complete wording in Section 12. Candidate labels below preserve the pre-approval evaluation history; their recommended options are the selected decisions and are authoritative for Z-007B and resumed Z-007. This approval does not claim external BIR approval.

This record separates merged authority from recommendations that require explicit accounting and product approval. It does not claim BIR approval and does not authorize schema or runtime implementation.

Decision labels:

- `GOVERNED`: already fixed by merged repository authority.
- `RECOMMENDED_PENDING_APPROVAL`: complete candidate selected for review.
- `DECISION_REQUIRED`: no merged source authorizes the candidate yet.
- `REJECTED_CANDIDATE`: unsuitable for the stated fiscal boundary.

## 2. Governed Baseline

The following facts are `GOVERNED`:

1. A Z Reading covers `[period_start_at, period_end_at)` and is generated no earlier than `period_end_at`.
2. A period has one immutable Z snapshot, and the close is an atomic privileged operation.
3. Resulting Z counter equals previous Z counter plus one.
4. Resulting GTA equals previous GTA plus a current-period contribution.
5. Reset and Z counters have different meanings; an ordinary Z close does not imply a fiscal reset.
6. Exact operation replay returns the original result; changed semantics conflict without mutation.
7. Amounts use integer minor units and one explicit currency per report scope.
8. The current-period contribution formula, canonical mutable state, initialization, strict predecessor rule, and issuance/close protocol are not governed.

## 3. GTA Contribution Candidate Comparison

All candidates below assume qualifying, durably recorded fiscal documents in the complete period. Cross-period void, refund, return, adjustment, and unsupported service-charge treatment remains fail-closed until separately governed.

| Candidate | Definition and VAT posture | Discounts and tenders | Voids and later adjustments | Relationship to Sales Invoice and X Reading | Decision |
|---|---|---|---|---|---|
| Gross sales | Sum of pre-discount VAT-inclusive gross amount. Includes VAT and ignores reductions in deriving the contribution. | Discounts do not reduce GTA; tender mismatch is separately detected. | Same-period void excluded; later effects unresolved. | Does not equal customer final invoice total or current X `netSalesAmountMinorUnits`. | `REJECTED_CANDIDATE`: overstates the amount fiscalized after discounts. |
| Net sales | Sum of final VAT-inclusive fiscal amount after governed discounts and VAT effects. | Discounts reduce contribution; tenders reconcile but are not authoritative. | Same-period void excluded; cross-period effects fail closed. | Equals the final Sales Invoice total and the governed X net amount for the supported source set. | `RECOMMENDED_PENDING_APPROVAL`. |
| Invoice total | Sum of the authoritative persisted Sales Invoice final total. | Same practical basis as net sales when reconciliation holds. | Same unresolved cross-period posture. | Strongest first-class source; must reconcile to X net sales. | `RECOMMENDED_PENDING_APPROVAL` as the source expression of net sales. |
| VAT-inclusive sales | Sum of customer-payable fiscal consideration including any VAT retained in the final total. | Governed discounts and VAT exemption/removal are reflected. | Same unresolved cross-period posture. | Equivalent to final fiscal amount when the document contract reconciles. | `RECOMMENDED_PENDING_APPROVAL`; descriptive VAT posture of the selected basis. |
| VAT-exclusive sales | Sum before VAT. | Discounts may reduce the basis; tender values do not reconcile directly. | Same unresolved cross-period posture. | Does not equal amount paid or final Sales Invoice total for VATable transactions. | `REJECTED_CANDIDATE` absent explicit accounting direction. |
| Collected tender total | Sum of recorded tenders. | Tender is treated as authority rather than reconciliation. | Refund and reversal semantics become payment-led rather than fiscal-led. | Multi-tender documents reconcile, but payment timing can diverge from fiscal fact timing. | `REJECTED_CANDIDATE`: Payment Orchestrator owns provider finality; POS tender is a fiscal snapshot. |
| Fiscal document total | Sum of authoritative final total for qualifying fiscal documents. | Discounts and VAT effects are already resolved; tenders must reconcile. | Same-period void excluded; cross-period effects fail closed. | Equivalent to invoice total/net sales in the currently supported Sales Invoice model. | `RECOMMENDED_PENDING_APPROVAL`; canonical technical source. |
| Another BIR basis | No different approved formula is present in the repository. | Undefined. | Undefined. | BRD leaves final X/Z aggregation open to BIR/accounting confirmation. | `DECISION_REQUIRED`; no basis may be invented. |

### 3.1 Recommended GTA Basis

`RECOMMENDED_PENDING_APPROVAL`:

```text
current_period_gta_contribution =
    SUM(authoritative final fiscal-document amount)
```

The source set is qualifying `RECORDED` `SALES_INVOICE` issuance facts assigned to the period. The value must equal the Z snapshot's `net_sales_amount_minor_units`; otherwise close fails. It is VAT-inclusive, net of governed statutory and commercial discounts and supplied VAT treatment. Tender totals are reconciliation evidence, not the GTA source.

This recommendation is not approved until the wording in Section 12 is accepted.

## 4. Decision Register

| ID | Subject | Existing authority | Recommendation | Status |
|---|---|---|---|---|
| DR-001 | GTA contribution | Only `result = previous + current` is governed. | Final fiscal amount / reconciled Z net sales. | `DECISION_REQUIRED` |
| DR-002 | State families | Reset and Z are distinct; GTA exists. | Exactly `z_counter`, `reset_counter`, and `grand_total_amount`. | `DECISION_REQUIRED` |
| DR-003 | State scope | Existing generic table is Site-scoped only. | Site POS Server + fiscal identity + currency. | `DECISION_REQUIRED` |
| DR-004 | Initialization | No governed production initialization exists. | Explicit privileged initialization; zero only when approved and audited. | `DECISION_REQUIRED` |
| DR-005 | Prior sequence | Period can reference a prior period, but close ordering is not fixed. | Strict sequence by scope; valid prior Z required except first initialized period. | `DECISION_REQUIRED` |
| DR-006 | Late-write protocol | Atomic close intent exists; issuance does not participate. | Shared transaction advisory lock plus reporting-period row lock and assignment guard. | `DECISION_REQUIRED` |
| DR-007 | Period lifecycle | OPEN/CLOSING/CLOSED codes exist. | Durable atomic `OPEN -> CLOSED`; no committed CLOSING state in v1.3. | `DECISION_REQUIRED` |
| DR-008 | Competing close | One Z and operation uniqueness exist. | Serialize by scope; one succeeds, exact operation replays, other operation conflicts/already closed. | `DECISION_REQUIRED` |
| DR-009 | Atomic boundary | Merged design requires atomic close. | One transaction for aggregate, transition, snapshot, close, result, and audit. | `GOVERNED`; detail frozen by contract package. |

## 5. Rejected Alternatives

- Generic unlabeled `fiscal_counter_states` rows: insufficient for an irreversible transition.
- Implicit zero initialization during close: hides missing or migrated fiscal state.
- Site-only state: permits one fiscal identity or currency to affect another.
- Fiscal series or sequence-policy GTA state: fragments taxpayer/currency accumulation without authority.
- Reporting-period row lock alone: issuance currently does not acquire it.
- `SERIALIZABLE` alone: does not define period ownership or force writers into one coordination protocol.
- A durable `CLOSING` commit before the Z transaction: introduces stranded state and recovery ambiguity.
- Automatically moving blocked issuance to the next period: changes fiscal business-date assignment without authority.
- Tender total as GTA authority: confuses fiscal final amount with payment execution evidence.

## 6. Counter-Family Decision

`RECOMMENDED_PENDING_APPROVAL`:

| Family | Purpose | Value and transition | Reset/rollover |
|---|---|---|---|
| `z_counter` | Counts committed Z closes for one state scope. | Non-negative integer; ordinary close increments exactly once. | No implicit rollover. Manual correction prohibited. |
| `reset_counter` | Records a separately authorized fiscal reset generation. | Non-negative integer; ordinary close preserves it. | Reset is a separate future privileged operation outside v1.3. |
| `grand_total_amount` | Cumulative approved final fiscal amount for the scope. | Integer minor units; close adds the approved period contribution once. | No implicit rollover. Adjustment requires separately governed recovery, never row editing. |

Every transition must retain operation reference, period, prior Z, expected state version, actor/service reference, and before/current/result values. Replay reads the committed transition; it never transitions again.

## 7. State-Scope Decision

`RECOMMENDED_PENDING_APPROVAL`: key mutable Z state by:

```text
(site_pos_server_id, fiscal_identity_id, currency_code)
```

- Site POS Server is included because periods, operation authority, and machine reporting are Site-server scoped.
- Fiscal identity is included because taxpayer facts must not cross identities.
- Currency is included because amounts cannot be accumulated across currency.
- Report family is represented by explicit state fields/families, not a second scope dimension.
- Fiscal series and sequence policy are excluded from GTA scope; they remain report range partitions.
- Channels, tenders, and business units are excluded because they are breakdowns, not cumulative fiscal authority.

## 8. Initialization Decision

`RECOMMENDED_PENDING_APPROVAL`:

- Initialization is an explicit privileged, idempotent, audited operation performed before the first Z close.
- A new, verified scope may initialize reset counter, Z counter, and GTA to zero only under an approved initialization authorization.
- Legacy migration imports verified values with a provenance reference; it must not silently assume zero.
- Missing state, duplicate/conflicting state, disabled fiscal identity, or unapproved currency blocks close.
- A new Site POS Server, fiscal identity, or currency is a new scope requiring initialization.
- Manual UPDATE is prohibited. Correction requires a separately governed append-only recovery transition.

## 9. Prior-Period Sequencing Decision

`RECOMMENDED_PENDING_APPROVAL`:

- Close is sequential within the state scope using `period_sequence`.
- The immediate prior period is the greatest lower governed sequence for that scope.
- An existing prior period must be `CLOSED` and have one valid committed Z and matching state transition.
- A prior period that is OPEN, closed without valid Z evidence, or references inconsistent state blocks close.
- A configured period with no transactions still requires a zero-current Z close and advances the Z counter once.
- Calendar-date gaps are allowed only when no governed period row exists for those dates; sequence continuity remains mandatory.
- The first period is valid only against an explicitly initialized state with no prior period/Z reference.

## 10. Late-Write and Lifecycle Decision

`RECOMMENDED_PENDING_APPROVAL`:

Use a transaction-scoped advisory lock shared by fiscal creation, void, future refund/return/adjustment writers, and Z close. The lock identity is a canonical, versioned encoding of Site POS Server, fiscal identity, and currency. Z close additionally locks the reporting-period and canonical state rows.

Use PostgreSQL `READ COMMITTED`. Writers acquire the shared lock before period assignment and source writes; close acquires it before final period revalidation and aggregation. This order ensures close waits for earlier issuance and later issuance waits for close. Acquiring a repeatable-read snapshot before waiting could preserve a stale view and is therefore rejected.

After acquiring the lock, issuance must assign and persist the first-class reporting period, then revalidate it is OPEN. If close commits first, issuance observes CLOSED and fails safely. It must not silently move to the next period.

The v1.3 lifecycle recommendation is one durable atomic `OPEN -> CLOSED` transition. `CLOSING` remains reserved but is not committed as an intermediate state. Lock contention is operationally visible through safe audit/metrics rather than a stranded durable period status.

## 11. Competing-Close Decision

`RECOMMENDED_PENDING_APPROVAL`:

| Situation | Required outcome |
|---|---|
| Same operation key and identical semantic hash | Exact authoritative replay. |
| Same operation key and changed semantic hash | Safe semantic conflict; no mutation. |
| Different operation keys concurrently target one OPEN period | Lock serialization; one commit, the other receives `PERIOD_ALREADY_CLOSED` with authoritative reference or deterministic close-identity conflict. |
| Second request after durable close | No new transition; safe already-closed result, not replay under the new key. |
| Client timeout or restart | Reconcile operation, report, period, and state transition before retry. |
| Unknown commit outcome | Return unknown/retryable posture until durable identity lookup proves commit or absence. Never repeat counter mutation speculatively. |

## 12. Required Approval

The approving authority must accept the complete decision set; partial approval does not unblock Z-007B or Z-007.

```text
I approve the ExitPass POS Server Z Close Counter, GTA, and Late-Write
Boundary Contract v1.0 candidate decisions:

1. GTA contribution is the VAT-inclusive authoritative final fiscal-document
   amount for qualifying recorded Sales Invoices, reconciled to Z net sales.
2. Z close uses distinct Z counter, reset counter, and GTA state identities.
3. State scope is Site POS Server, fiscal identity, and currency.
4. New zero state requires explicit, privileged, audited initialization;
   missing or legacy state never defaults silently.
5. Periods close sequentially by governed period sequence and prior Z/state.
6. Ordinary Z close increments Z counter once, leaves reset counter unchanged,
   and adds the approved current-period contribution to GTA once.
7. Fiscal issuance/void and Z close share the versioned scope advisory lock;
   issuance persists and revalidates reporting-period assignment under it.
8. Period close is one atomic OPEN-to-CLOSED transaction; no durable CLOSING
   state is introduced in v1.3.
9. Cross-period void/refund/return/adjustment and unsupported service-charge
   cases remain fail-closed until separately approved.

Approval authority/reference: ____________________
Approved by: _____________________________________
Approved at: _____________________________________
```

## 13. Downstream Impact

Approval authorizes design implementation planning only. Z-007B must add governed state, scope, versioning, transition, period assignment, and shared writer coordination. Resumed Z-007 must consume those foundations atomically. Neither Controlled UAT nor production rollout is authorized.

Until approval is recorded, DR-001 through DR-008 remain blockers.
