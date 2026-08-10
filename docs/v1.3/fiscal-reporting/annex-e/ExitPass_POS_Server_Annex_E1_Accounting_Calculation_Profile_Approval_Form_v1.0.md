# ExitPass POS Server Annex E-1 Accounting Calculation Profile Approval Form v1.0

## 1. Approval subject

| Item | Value |
|---|---|
| Profile document | `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md` |
| Profile version | `v1.0` |
| Profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Calculation profile | `pos-server-annex-e1-accounting-calculation:v1` |
| Decisions | AE-DR-006, AE-DR-007, AE-DR-008, AE-DR-009 |
| Physical positions | D06, D07, D09, D12-D29 as enumerated in the profile |
| Approving authority | Accounting |
| Authority statement date | 2026-08-10 |
| Authority statement status | `ACCOUNTING_AUTHORITY_AND_APPROVAL_IN_PRINCIPLE_CONFIRMED` |
| Approval record | `Z-012B-ACCOUNTING-APPROVAL-001` |
| Source posture | Accounting approval supplied through the Product Owner |
| Exact profile status | `APPROVED_EXACTLY_AS_SPECIFIED` |
| Prior executable status | `EXECUTABLE_CALCULATION_PROFILE_NOT_YET_APPROVED`, superseded on 2026-08-10 |
| Executable status | `APPROVED_EXACTLY_AS_SPECIFIED` |

The 2026-08-10 statement confirms Accounting authority and approval in principle for resolving AE-DR-006 through AE-DR-009. It does not select formulas or sources that were not yet stated. This form is the instrument for approving or rejecting the exact executable proposal identified above.

## 2. Decision choices

Accounting selected exactly one:

- [x] `APPROVED_EXACTLY_AS_SPECIFIED`
- [ ] `APPROVED_WITH_LISTED_CHANGES`
- [ ] `REJECTED`
- [ ] `REQUIRES_CLARIFICATION`

Approval status: `APPROVED_EXACTLY_AS_SPECIFIED`

Confirmation date: `2026-08-10`

Approving authority: `Accounting`

Approval reference: `Z-012B-ACCOUNTING-APPROVAL-001`

## 3. Listed changes or clarification

Complete only when the selected choice requires it. Every change must identify the decision ID, D-position, named operand, formula/source/rule being changed, exact replacement, and rationale.

| Decision / position | Exact requested change | Accounting rationale |
|---|---|---|
| `________________` | `________________` | `________________` |

Changes produce a new profile version and SHA-256 before approval becomes executable. They do not amend the identified profile in place.

## 4. Approval scope

Approval exactly as specified confirms only:

* the literal numbered-field interpretation and named equations for AE-DR-006;
* the Manual SI/OR definition, source contract, period rule, sign, zero rule, and D06 treatment for AE-DR-007;
* the Sales Overrun/Overflow definition, rejected alternatives, source contract, sign, zero rule, and D28 treatment for AE-DR-008;
* the Total Income meaning, exact D29 equation, source relationships, and rejected alternatives for AE-DR-009;
* PHP minor-unit arithmetic, zero tolerance, immutable correction lineage, and fail-closed missing-source behavior in the profile.

## 5. Exclusions

This approval does not constitute:

* BIR or examiner acceptance;
* approval of AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-012, AE-DR-016, AE-DR-016B, AE-DR-019, AE-DR-020A, or AE-DR-024;
* approval of nonzero Diplomat or another unresolved VAT privilege;
* runtime, schema, controlled-code, API, or migration authorization by this documentation task;
* Controlled UAT, external delivery, or Production authorization;
* Annex E-2 through E-5 or ARTS POSLog authorization.

## 6. Consequences

`APPROVED_EXACTLY_AS_SPECIFIED` permits the decisions to be recorded as resolved by Accounting in a subsequent governed change and permits a bounded runtime task to implement the exact hashed profile, subject to the remaining gates.

`APPROVED_WITH_LISTED_CHANGES` does not approve this hash. A revised profile and approval form must be produced and approved.

`REJECTED` leaves AE-DR-006 through AE-DR-009 unresolved and blocks the Annex E-1 runtime.

`REQUIRES_CLARIFICATION` leaves the profile non-executable until the requested clarifications are incorporated into a new version and approved.

## 7. Copy-ready approval statement

```text
Accounting approves the exact Annex E-1 Accounting Calculation Profile identified as ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md, version v1.0, SHA-256 36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f, for Annex E profile pos-server-bir-annex-e1-rmo24-2023:v1, covering AE-DR-006 through AE-DR-009, with the status APPROVED_EXACTLY_AS_SPECIFIED.
```

## 8. Governed approval record

| Item | Recorded value |
|---|---|
| Approval record ID | `Z-012B-ACCOUNTING-APPROVAL-001` |
| Approval authority | Accounting |
| Confirmation date | 2026-08-10 |
| Status | `APPROVED_EXACTLY_AS_SPECIFIED` |
| Source posture | Accounting approval supplied through the Product Owner |
| Approved filename | `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md` |
| Approved version | `v1.0` |
| Approved SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Affected decisions | AE-DR-006, AE-DR-007, AE-DR-008, AE-DR-009 |
| Approval scope | Exact executable calculation profile only |
| Approval effect | Engineering may implement the approved calculations subject to all other runtime gates |
| Exclusions | Controlled UAT, external delivery, Production, E-2 through E-5, ARTS POSLog 6.0.0, BIR submission, signing, encryption, and destructive purge |
| Change rule | Any profile change requires a new version, new hash, and renewed Accounting approval |

No individual Accounting approver identity is recorded or inferred.
