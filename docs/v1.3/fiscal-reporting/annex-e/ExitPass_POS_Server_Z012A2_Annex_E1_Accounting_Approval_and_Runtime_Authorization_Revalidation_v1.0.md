# ExitPass POS Server Z-012A2 Annex E-1 Accounting Approval and Runtime Authorization Revalidation v1.0

## 1. Purpose and decision

This record verifies the exact Accounting-approved calculation profile, records `Z-012B-ACCOUNTING-APPROVAL-001`, resolves only AE-DR-006 through AE-DR-009, and revalidates the local bounded Z-012B implementation gate.

```text
Final decision: AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME
Controlled UAT: NOT AUTHORIZED
External delivery: NOT AUTHORIZED
Production: NOT AUTHORIZED
```

This record authorizes implementation work only. It does not implement or approve runtime code, schema, APIs, controlled codes, XLSX generation, artifact storage, Controlled UAT, submission, or Production.

## 2. Baseline and immutable artifact verification

| Item | Verified value |
|---|---|
| Repository baseline and HEAD | `fba99c4b2e94cc27dcba40ebd657fcccdf744a75` |
| origin/dev integration | HEAD equals origin/dev; merge base equals HEAD; 0 ahead / 0 behind |
| Approved filename | `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md` |
| Document version | `v1.0` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Calculation profile | `pos-server-annex-e1-accounting-calculation:v1` |
| Decision scope | AE-DR-006, AE-DR-007, AE-DR-008, AE-DR-009 |
| Calculated SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Expected SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Content verification | 21 affected position rows, 30 governed columns per row, 19 named operands, complete common rules C01-C12, reconciliations R01-R10 |
| Proposal mutation | None; the approved file is unchanged |

The proposal's embedded pre-approval status is part of the approved immutable bytes. The mutable approval form and this record carry the subsequent Accounting decision; the proposal is not edited to rewrite history.

## 3. Accounting approval record

| Item | Recorded value |
|---|---|
| Approval record ID | `Z-012B-ACCOUNTING-APPROVAL-001` |
| Approval authority | Accounting |
| Confirmation date | 2026-08-10 |
| Status | `APPROVED_EXACTLY_AS_SPECIFIED` |
| Source posture | Accounting approval supplied through the Product Owner |
| Approved artifact | Exact filename, version, profile, scope, and SHA-256 in section 2 |
| Approval scope | Exact executable calculation profile only |
| Approval effect | Engineering may implement the approved calculations subject to all other runtime gates |
| Change rule | Any change requires a new profile version, new SHA-256, and renewed Accounting approval |

Exact approval statement:

> Accounting approves the exact Annex E-1 Accounting Calculation Profile identified as `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md`, version `v1.0`, SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f`, for Annex E profile `pos-server-bir-annex-e1-rmo24-2023:v1`, covering AE-DR-006 through AE-DR-009, with the status `APPROVED_EXACTLY_AS_SPECIFIED`.

No individual Accounting approver, title, email, signature, or other identity is inferred.

## 4. Resolved decisions

The immutable approved profile is the complete authority for every source, status, period, inclusion, exclusion, sign, currency, arithmetic, null, known-zero, no-activity, correction, reconciliation, tolerance, unsupported, and bounded-runtime rule. The summaries below do not replace it.

### AE-DR-006

Resolved by `Z-012B-ACCOUNTING-APPROVAL-001` with these exact named equations:

```text
D07 = ACTIVE_GROSS + RETURN_AMOUNT + VOID_AMOUNT
D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
D25 = D20 + D21 + D22 + D23 + D24
D26 = D09 - D22
D27 = D07 - D19 - D09
```

D26 preserves literal official fields 8 and 19 and may not substitute D25. D27 is Annex VAT-exclusive regular electronic net income, not the VAT-inclusive Z/BIR Summary net amount. All operations use checked PHP minor units with zero tolerance.

### AE-DR-007

D06 is the VAT-exclusive net income of qualifying continuity Manual SI/OR originally issued in the governed period. Later encoding does not move period attribution. Electronic/fiscalized duplicates, drafts, cancellations, and duplicate encoding are excluded. The authoritative source is an immutable scoped period fact in `RECORDED` or `ATTESTED_ZERO` state. Missing is not zero.

### AE-DR-008

D28 is VAT-exclusive net income omitted from regular electronic sales solely because an approved accumulated-sales capacity boundary was reached. Rounding differences, cash overage, fiscal-number exhaustion, counter rollover, transaction volume, manual sales, and late posting are excluded. The authoritative source is an immutable scoped period fact/event in `RECORDED` or `ATTESTED_ZERO` state. Missing is not zero.

### AE-DR-009

```text
D29 = D27 + D06 + D28
```

D29 is VAT-exclusive income across mutually exclusive regular electronic, qualifying manual, and approved overflow paths. Gross, net alone, net plus VAT, tenders/payment confirmations, and GTA contribution are rejected alternatives. Reconciliation is `D29-D06-D28=D27` with zero-minor-unit tolerance.

## 5. First-class facts and Z-012B implementation gates

| Capability | Authorization classification | Required Z-012B posture |
|---|---|---|
| Manual SI/OR period fact | Approved requirement to implement | Immutable exact-scope/period PHP fact, `RECORDED`/`ATTESTED_ZERO`, authority reference, semantic hash, correction lineage, and committed source binding |
| Sales Overrun/Overflow fact/event | Approved requirement to implement | Same controls plus approved accumulated-sales-capacity event identity |
| NAAC and Solo Parent | Approved internal bounded rule | Implement separate immutable classification or exact zero attestation; unknown/nonzero unsupported input blocks |
| Other VAT, return VAT, residual VAT | Approved bounded zero-only path | Emit zero only from enforced same-scope/period evidence; nonzero or unknown blocks |
| Known-zero evidence | Approved requirement to implement | Absence of a source row is never zero; attestation is immutable, authorized, scoped, hashed, and correctable only by supersession |
| No-activity row | Approved requirement to implement | Requires empty committed Z, CLOSED period, zero Z/BIR facts, unchanged GTA/reset, advanced Z, blank range, and every required explicit zero attestation |
| Period attribution | Approved requirement to implement | Bind every source to the exact governed reporting period; later recording never moves effective attribution |
| Committed Z and CLOSED period | Existing canonical enforcement | Reject open periods, missing/noncommitted Z, wrong scope/currency, and ambiguous source membership |
| BIR Sales Summary | Existing financial authority | All available financial facts come from the committed Z-010 summary and bound immutable Z children |
| Electronic Journal | Existing traceability authority | Verify transition identity/chronology/integrity only; never supply a missing amount |
| Correction/supersession | Approved requirement to implement | Immutable new artifact and source lineage; never overwrite prior facts or output |
| Nonzero unresolved privilege | External decision still required | Fail closed under AE-DR-012; never remap into another column |

These are Z-012B deliverables. Their absence from the current runtime is not a new external business decision and does not block starting the authorized task.

## 6. Remaining external decisions and gates

| Decision | Remaining authority | Current gate | Local bounded posture |
|---|---|---|---|
| AE-DR-002 | BIR/examiner | Controlled UAT, external delivery, Production | Generate deterministic internally labeled XLSX; make no acceptance claim |
| AE-DR-004 | BIR/examiner | Controlled UAT, external delivery, Production | Use approved internal deterministic filename |
| AE-DR-010 | BIR/examiner | Controlled UAT, external delivery, Production | Use governed internal `NONE`/`NO_ACTIVITY`; no free text |
| AE-DR-011A | BIR/examiner | Controlled UAT and Production acceptance | Use explicit immutable zero evidence internally; unknown blocks |
| AE-DR-012 | Accounting/BIR | Nonzero privilege path, Controlled UAT, Production | Reject every nonzero unresolved privilege fact |
| AE-DR-016 | BIR/examiner | External delivery, Controlled UAT, Production | Local authorized generation/download only; no signing, encryption, compression, or submission |
| AE-DR-016B | Legal/Compliance | Production | Persist configurable retention metadata; no destructive purge |
| AE-DR-019 | Accounting/examiner | Controlled UAT and Production | Use approved invariant internal PHP/date profile |
| AE-DR-020A | BIR/examiner | Controlled UAT and Production acceptance | Snapshot approved Site POS Server fiscal terminal identity in H08; never concatenate channel labels |
| AE-DR-024 | BIR/examiner | Controlled UAT, external delivery, Production | Preserve official geometry and fail unrepresentable values |

AE-DR-013 and AE-DR-014 remain deferred nonblocking E-2 through E-5 matters. ARTS POSLog 6.0.0 remains a separate deferred task. None of these items blocks local bounded Z-012B implementation because its internal rule or fail-closed boundary is already frozen.

## 7. Authorized Z-012B boundary

Z-012B may implement only the local Annex E-1 runtime for `pos-server-bir-annex-e1-rmo24-2023:v1`, including:

* first-class facts and attestations in section 5;
* generation, metadata readback, and immutable XLSX download APIs;
* exact Site POS Server, fiscal identity, currency, and calendar-month scope;
* one row per committed governing Z and fiscal identity;
* committed-Z, CLOSED-period, and committed BIR Summary enforcement;
* exact approved calculations and all 42 physical positions;
* deterministic Open XML and byte-identical replay;
* immutable artifact metadata, atomic publication, content hash, recovery, and tamper detection;
* semantic identity, replay/conflict, concurrency, correction/supersession, authorization, privacy-safe audit, and PostgreSQL proof;
* fail-closed unsupported classifications, gaps, missing facts, and nonzero unresolved privileges.

This authorization excludes Controlled UAT, external delivery/submission, Production, signing, encryption, destructive purge, E-2 through E-5, and ARTS POSLog.

## 8. Authorization conclusion

The only prior local implementation blocker was the missing exact Accounting calculation profile. Exact approval verification passes, the approval record is complete, AE-DR-006 through AE-DR-009 are consistently resolved, and all remaining decisions have bounded later-stage or fail-closed effects.

```text
AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME
```
