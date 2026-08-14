# ExitPass POS Server Z-012D Annex E-1 Controlled UAT Preparation Plan v1.0

## 1. Control record

| Item | Value |
|---|---|
| Preparation authority | `AUTHORIZED_FOR_Z012D_CONTROLLED_UAT_PREPARATION` |
| Repository baseline | `529aa57c549bf76ee8ad1e3ae925f0b855eae3eb` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Approved calculation profile | `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md` |
| Approved calculation-profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Accounting approval | `Z-012B-ACCOUNTING-APPROVAL-001` |
| Controlled UAT data assignment | `NOT_AUTHORIZED` |
| Controlled UAT execution | `NOT_AUTHORIZED` |
| External delivery or BIR submission | `NOT_AUTHORIZED` |
| Production | `NOT_AUTHORIZED` |

## 2. Purpose

This plan prepares specifications and reusable templates for a separately authorized future Annex E-1 Controlled UAT. It does not assign data, create an environment, invoke the POS Server, generate a UAT workbook, or execute any scenario.

The package is grounded in the immutable Accounting profile, merged Z-012B runtime, Z-012C acceptance review, and Z-012C1 authorization-token correction. It does not replace those authorities or resolve any external confirmation.

## 3. Scope and non-goals

Preparation covers:

1. [Scenario definitions and expected governed outcomes](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Synthetic_Scenario_Catalogue_v1.0.md).
2. Synthetic-only data requirements.
3. [Evidence metadata and review controls](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Evidence_Manifest_Template_v1.0.md).
4. [Isolated-environment readiness requirements](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Environment_Readiness_Checklist_v1.0.md).
5. [Role-based authority and responsibility](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Authority_and_Responsibility_Matrix_v1.0.md).
6. [Failure classification, escalation, rollback, and cleanup](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Rollback_and_Cleanup_Checklist_v1.0.md).
7. [External-confirmation status tracking](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_External_Confirmation_Tracker_v1.0.md).

This task does not authorize actual data assignment, scenario execution, shared/UAT/Production database access, external artifact delivery, BIR submission, Production use, signing, encryption, purge, Annex E-2 through E-5, or ARTS POSLog.

## 4. Authoritative baseline

The future authorization review must use, in order:

1. The official local RMO 24-2023 Annex E-1 workbook and its governed hash.
2. The approved Accounting Calculation Profile v1.0 and `Z-012B-ACCOUNTING-APPROVAL-001`.
3. The Annex E field dictionary, source mapping, calculation/reconciliation rules, and file-layout specification.
4. Z-012B deterministic runtime implementation evidence.
5. Z-012C gate revalidation and the external-confirmation closure package.
6. This preparation package.

The committed Z Reading and CLOSED fiscal period establish period finality. Z-010 BIR Sales Summary is the financial authority. Z-011A Electronic Journal is traceability evidence only and must not replace financial source facts.

## 5. Entry criteria for a later authorization review

Every item must be evidenced before Controlled UAT data assignment or execution is considered:

- [ ] This Z-012D package is reviewed and merged.
- [ ] The proposed execution commit is immutable and identified exactly.
- [ ] The approved calculation-profile hash matches exactly.
- [ ] A separate authorization record explicitly permits data assignment and execution scope.
- [ ] Every execution-blocking external decision is resolved, or the authorization defines a governed fail-closed subset accepted by the responsible authority.
- [ ] The isolated environment passes the environment-readiness checklist.
- [ ] Synthetic dataset specifications are approved; no actual customer, statutory, payment, or Production data is present.
- [ ] Scenario inclusion/exclusion and blocked scenarios are approved.
- [ ] Executor, reviewer, Accounting reviewer, technical reviewer, and cleanup verifier roles are assigned.
- [ ] Evidence storage is invocation-owned, access controlled, and contains no secrets.
- [ ] Rollback and cleanup ownership is accepted before any resource is created.
- [ ] External network isolation and no-delivery controls are proven.
- [ ] Open blockers have owners and stop conditions.

## 6. Environment model

The planned environment is an isolated, invocation-owned, non-Production environment using PostgreSQL 16 and synthetic fixtures only. Every container, database, volume, network, artifact directory, and evidence directory must carry the authorized UAT run ID. No shared development, standing UAT, staging, or Production database is permitted.

The environment must default-deny outbound external delivery. The POS Server must reject fixture/development authority when configured as Production. Configuration presence may be evidenced by key names and redacted classifications, never values.

## 7. Synthetic-data rules

1. Use reserved synthetic UUIDs and references clearly labeled `SYNTHETIC`.
2. Do not use real names, TINs, statutory IDs, ticket numbers, plates, payment credentials, or examiner correspondence.
3. Use only PHP integer minor units.
4. Model committed Z, CLOSED period, BIR Sales Summary, and governed first-class facts exactly as required by each scenario.
5. Missing, unknown, unsupported, not-applicable, and known-zero states remain distinct.
6. Known zero requires immutable, scoped, attributable evidence; absence is not zero.
7. Nonzero unresolved privilege facts remain blocked by AE-DR-012.
8. No synthetic data may be loaded until a separate data-assignment authorization identifies the dataset and environment.

## 8. Planned future execution sequence

The following sequence is descriptive, not executable authority:

1. Verify the separate UAT authorization and exact commit.
2. Assign a run ID, roles, approved scenario set, and approved synthetic dataset.
3. Prove environment readiness and capture pre-run resource/state manifest.
4. Start only invocation-owned resources.
5. Load only the approved synthetic baseline.
6. Execute scenarios in approved order, stopping on a mandatory failure.
7. Record evidence metadata without raw payloads or secrets.
8. Review reconciliations, hashes, workbook geometry, authorization denials, and mutation boundaries.
9. Record pass/fail/blocked results and unresolved external dependencies.
10. Stop execution, perform non-destructive cleanup, and capture the post-cleanup manifest.
11. Conduct technical and Accounting evidence review.
12. Issue a separate gate decision; never infer delivery or Production approval from UAT results.

## 9. Evidence requirements

Use the [Evidence Manifest Template](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Evidence_Manifest_Template_v1.0.md). Each executed scenario must eventually have one immutable manifest entry binding the run, scenario, commit, environment, synthetic dataset, result, safe correlation, artifact filename/hash/length when applicable, reconciliation evidence, failure classification, and cleanup evidence.

Evidence must not contain credentials, tokens, raw request/response bodies, personal information, statutory evidence, stack traces, connection strings, internal filesystem paths, or unrestricted SQL diagnostics.

## 10. Failure classification and escalation

| Classification | Meaning | Required future action |
|---|---|---|
| `UAT_AUTHORITY_MISSING` | Data assignment or execution lacks explicit authority | Stop before creating resources |
| `UAT_BASELINE_MISMATCH` | Commit, profile, template, or approved hash differs | Stop; require revalidation |
| `UAT_ENVIRONMENT_UNSAFE` | Isolation, configuration, or Production rejection is not proven | Stop; environment owner remediates |
| `UAT_SYNTHETIC_DATA_INVALID` | Dataset is unapproved, ambiguous, or contains prohibited data | Stop; quarantine specification, do not load |
| `UAT_EXTERNAL_DECISION_BLOCKED` | Expected outcome depends on an unresolved confirmation | Do not execute affected scenario |
| `UAT_RECONCILIATION_FAILURE` | Governed equation differs by any minor unit | Stop affected run; preserve safe evidence |
| `UAT_DETERMINISM_FAILURE` | Replay bytes/hash/order differ | Stop; technical investigation required |
| `UAT_AUTHORIZATION_FAILURE` | Denial/scope/fixture behavior differs | Stop; security owner review |
| `UAT_ARTIFACT_INTEGRITY_FAILURE` | Stored bytes, length, hash, or lineage differs | Stop; preserve artifact metadata only |
| `UAT_PRIVACY_FAILURE` | Prohibited sensitive content is detected | Stop, isolate evidence, notify privacy/security roles |
| `UAT_CLEANUP_FAILURE` | Invocation-owned resources remain or shared resources may be affected | Keep run blocked until independently verified |

Unknown failures are `UAT_UNKNOWN_FAIL_CLOSED`; do not expose raw diagnostics in approval evidence.

## 11. Exit criteria for a future Controlled UAT run

- [ ] Every authorized scenario has `PASS`, `FAIL`, or governed `BLOCKED` evidence.
- [ ] All mandatory scenarios pass; no reconciliation or privacy failure is waived.
- [ ] Every blocked scenario cites its exact external decision.
- [ ] Repeated and restart outputs match expected identity, SHA-256, length, and bytes.
- [ ] Authorization, scope, and Production fixture rejection pass.
- [ ] Fiscal documents, Z state, BIR Summary, and unrelated scope remain unchanged by read/download/replay.
- [ ] Original artifacts remain immutable after correction tests.
- [ ] No external delivery or submission occurred.
- [ ] Cleanup is independently verified and shared resources are proven untouched.
- [ ] Evidence manifests are complete and reviewed by assigned roles.
- [ ] The run result does not claim BIR, delivery, or Production approval.

## 12. Rollback and cleanup

Use the [Rollback and Cleanup Checklist](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Rollback_and_Cleanup_Checklist_v1.0.md). Cleanup is limited to invocation-owned disposable resources and temporary output. Governed fiscal evidence must not be deleted, purged, or rewritten.

## 13. Authorization boundary and next review

Z-012D ends with preparation artifacts only. The next bounded step is an unnamed, separately authorized review of this package, external-confirmation status, proposed synthetic dataset, environment plan, roles, and scenario scope. This package deliberately does not assign the next tracker identifier.

Controlled UAT data assignment, execution, external delivery, BIR submission, Production, Annex E-2 through E-5, ARTS POSLog, signing, encryption, and destructive retention remain unauthorized.
