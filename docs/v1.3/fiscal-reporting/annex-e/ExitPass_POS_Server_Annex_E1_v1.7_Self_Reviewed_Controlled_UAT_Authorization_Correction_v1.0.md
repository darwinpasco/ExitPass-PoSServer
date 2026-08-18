# ExitPass POS Server Annex E-1 v1.7 Self-Reviewed Controlled UAT Authorization Correction v1.0

## 1. Decision And Authority

**Decision: `AUTHORIZED_FOR_SELF_REVIEWED_ANNEX_E1_V1_7_CONTROLLED_UAT_EXECUTION`.**

| Item | Authorized value |
| --- | --- |
| Applicable specification | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.7` |
| Original authorization | [Controlled UAT Execution Authorization v1.0](ExitPass_POS_Server_Annex_E1_v1.7_Controlled_UAT_Execution_Authorization_v1.0.md) |
| Earlier amendment | [Single-Principal Controlled UAT Amendment v1.0](ExitPass_POS_Server_Annex_E1_v1.7_Single_Principal_Controlled_UAT_Amendment_v1.0.md) |
| Originally authorized ancestor | `e6c85e3f80ba2773104416a649ea164baa81a1aa` |
| Frozen package-root SHA-256 | `5d60cb50d7f43b29f79486801a470e57128e2b571c4e50a37494e4a82131b829` |
| Review mode | `SELF_REVIEWED` |
| Accountable authorization owner | `Darwin Pasco` |
| Technical executor | `Codex I under Darwin Pasco's direction` |
| Independent review | `NOT_PERFORMED` |
| Independent review claim | `NONE` |
| Controlled UAT result taxonomy | Unchanged from the original authorization |
| External-readiness effect | None |
| BIR-submission effect | None |
| Production effect | None |
| Preparation date | `2026-08-18 Asia/Manila` |

The merge of this correction is Darwin Pasco's prospective authorization for the exact self-reviewed execution mode defined here. No separate identity-register lookup, principal assignment, acceptance timestamp, acceptance-source reference, signature record, or role-combination approval is required. This correction does not execute Controlled UAT, create a run ID, or create an evidence directory.

The original authorization and earlier amendment remain historical records. For a future execution explicitly using this correction, this document controls where either record requires separate principals, role records, independent sign-off, or a different result taxonomy. All provisions not expressly superseded below remain effective.

The earlier authorization-gate attempt stopped before execution because the required separate identities, acceptances, timestamps, and approval records were unavailable. This correction does not reclassify that historical stop or fabricate those records.

## 2. Corrected Responsibility Model

### Accountable Authorization Owner

Darwin Pasco:

- authorizes this self-reviewed internal execution model and accepts that it is not independently reviewed;
- authorizes Codex I to perform technical execution, privacy-safe evidence capture, Accounting reconciliation, workbook and visual inspection, and task-owned cleanup verification;
- approves the exact evidence-root base in Section 3;
- preserves every mandatory technical, scope, evidence, incident, and cleanup stop condition;
- cannot waive a validation, deterministic, Accounting, visual, evidence, scope, security, or cleanup failure; and
- will not represent a result as BIR-approved, externally accepted, independently assured, or Production-ready.

### Technical Executor

Codex I under Darwin Pasco's direction:

- performs the original authorization's exact sequence from the post-merge baseline;
- records sanitized commands, exit codes, hashes, inventories, observations, deviations, incidents, and limitations;
- captures privacy-safe evidence and performs deterministic, Accounting, SP-A, R01-R12, workbook, PDF, visual, security/privacy, and cleanup checks;
- labels every executor-performed review and conclusion `SELF_REVIEWED`; and
- never states or implies independent review, independent validation, independent Accounting approval, independent cleanup verification, independent acceptance, or independent certification.

The existing runners, tests, frozen hashes, deterministic comparisons, Open XML checks, workbook safety scans, PDF page counts, resource inventories, and cleanup checks remain objective automated controls. They do not require artificial human role assignments.

## 3. Approved Evidence Root And Retry

The approved evidence-root base is exactly:

`D:\SourceCodes\ExitPass.local\annex-e1-v1.7-controlled-uat\evidence`

A later execution must create one new opaque run-ID subdirectory under that base, outside Git and separate from temporary workbook storage. The directory and retained contents must remain local, task owned, synthetic only, access controlled, not externally shared, and not represented as official BIR evidence. The future run ID is not prescribed here.

If the exact base cannot be created or safely used, execution must return `CONTROLLED_UAT_BLOCKED_ENVIRONMENT`; another path may not be substituted silently.

After this correction is merged, the later execution must:

1. fetch and fast-forward local `dev` to the exact current `origin/dev`;
2. create a new execution branch and worktree from that exact commit;
3. record that commit as the frozen execution baseline for the attempt;
4. verify ancestry from `e6c85e3f80ba2773104416a649ea164baa81a1aa`;
5. verify every frozen package member, hash, blob, count, script, test, and expected commitment before execution; and
6. return `CONTROLLED_UAT_BLOCKED_BASELINE` if any governed commitment differs.

No pre-merge working commit is a permanent execution baseline.

## 4. Requirements Prospectively Superseded

For this exact self-reviewed Annex E-1 v1.7 local synthetic Controlled UAT only, the following original or amended administrative prerequisites are superseded:

- separate assignment of the Controlled UAT Executor, Evidence Recorder, Independent Reviewer, Authorization Approver, Environment Owner, Accounting Reviewer, Security/Privacy Reviewer, and Cleanup Verifier roles;
- assignment of one principal to eight role slots;
- authoritative identity-register resolution;
- individual or combined role-acceptance records;
- acceptance and approval timestamps or source references;
- approval of role combinations;
- executor/reviewer, executor/evidence-recorder, and executor/cleanup-verifier separation;
- independent visual-review, Accounting, security/privacy, evidence, or cleanup sign-off;
- multi-principal or single-principal signature completion as an acceptance criterion;
- a separate role record before the first validator command;
- missing role assignments, role acceptances, role signatures, or independence evidence as mandatory stop conditions; and
- the alternate result taxonomy introduced by the earlier single-principal amendment.

This correction does not supersede a technical acceptance criterion or a mandatory stop condition unrelated to role administration. Missing technical evidence, an unexplained deviation or incident, uncertain isolation, prohibited access, governed-package change, real data, or incomplete cleanup remains blocking.

## 5. Technical Controls Preserved

The future execution must retain every original technical requirement, including:

- the frozen v1.7 dataset with `4,074` records, `2,364` deterministic identities, `29` semantic families, `1,690` semantic instances, `19` included cases, `6` excluded cases, `22` Annex rows, `155` Accounting facts, `148` F22 records, `148` F23 transitions, `19` EJ streams, `19` genesis records, `129` predecessors, and `30` statutory-finality comparisons;
- all eight governed manifest members, the `1,897`-byte package-root preimage, package-root verification, and canonical-LF dataset verification;
- offline validation, two clean isolated executions, and the authorized normalized report hash;
- the complete runtime test suite and its authorized count;
- deterministic workbook generation, two-run byte equality, exact aggregate and manifest-file hashes, all `15` negative workbook tests, and Open XML validation;
- macro, formula, external-link, external-connection, hidden-sheet, embedded-content, duplicate-ZIP-entry, and relationship-integrity checks;
- normal read-only Excel opening for all `19` workbooks without repair, recovery, extraction, or compatibility handling;
- one-page `E-1` PDF export and local rasterization for every included case;
- visual inspection of all `19` pages and all `22` rows, including H01-H10, D01-D32, R01-R12, warning visibility, precision, pagination, clipping, overlap, chronology, and readability;
- SP-A and R01-R12 zero-difference reconciliation plus representative `Validation` and `Fact Links` review;
- privacy-safe evidence, prohibited-access checks, exact task-owned resource inventories, and complete ownership-bound cleanup; and
- every technical failure, blocked, scope-violation, and cleanup outcome in the original authorization.

The exact warning remains:

`SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION`

## 6. Review Labels And Result Taxonomy

Every later execution report must state:

- `Review mode: SELF_REVIEWED`
- `Independent review: NOT_PERFORMED`
- `Independent review claim: NONE`

Automated controls may be reported as passed. Executor-performed visual, Accounting, security/privacy, evidence, and cleanup conclusions must be labeled `SELF_REVIEWED`.

The original result taxonomy remains unchanged:

- `CONTROLLED_UAT_PASSED`
- `CONTROLLED_UAT_PASSED_WITH_NON_BLOCKING_OBSERVATIONS`
- `CONTROLLED_UAT_FAILED`
- `CONTROLLED_UAT_BLOCKED_BASELINE`
- `CONTROLLED_UAT_BLOCKED_ENVIRONMENT`
- `CONTROLLED_UAT_BLOCKED_SCOPE_VIOLATION`
- `CONTROLLED_UAT_BLOCKED_CLEANUP`

A passed result must always be qualified as a self-reviewed internal technical result. Independent or stakeholder review may be required later for an external-readiness decision, but it is not a prerequisite for this bounded internal execution.

## 7. External Boundary

This correction does not authorize or decide:

- BIR submission, external delivery, an official filename, signing, certification, external acceptance, or a regulatory retention policy;
- Production use or release approval requiring independent assurance;
- real data or shared development, standing UAT, staging, Production, or `exitpass_v12_dev` access;
- HikCentral, payment-provider, BIR-system, or other external-business-service access;
- Annex E-2 through E-5 or ARTS POSLog; or
- any runtime, API, database, migration, dependency, dataset, validator, generator, workbook-layout, test, expected-result, configuration, or CI change.

All workbooks and derived artifacts remain synthetic internal technical evidence and are not approved for BIR submission. A future external-readiness decision remains separately governed and may require independent review.

## 8. Next Bounded Task

After this correction is independently reviewed and merged: retry the complete Annex E-1 v1.7 Controlled UAT from the exact updated `origin/dev` on a new execution branch and worktree, under a new opaque run ID and the approved evidence-root base, with `Review mode: SELF_REVIEWED`.
