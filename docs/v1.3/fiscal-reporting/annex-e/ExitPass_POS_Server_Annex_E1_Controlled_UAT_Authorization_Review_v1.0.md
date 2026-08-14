# ExitPass POS Server Annex E-1 Controlled UAT Authorization Review v1.0

## 1. Control record

| Item | Value |
|---|---|
| Review date | 2026-08-14 PHT |
| Repository baseline | `fc5abf490ea81a2bd4e061bf32adcf402601ea5c` |
| Z-012D integration | Merge commit `fc5abf490ea81a2bd4e061bf32adcf402601ea5c`, PR #103 |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Z-012D preparation authority | `AUTHORIZED_FOR_Z012D_CONTROLLED_UAT_PREPARATION` |
| Overall UAT decision | `AUTHORIZED_FOR_NEXT_BOUNDED_PREPARATION` |

This is a documentation and governance decision. It assigns no dataset or person, provisions no environment, loads no data, executes no scenario, and creates no workbook.

## 2. Executive decision

| Question | Decision | Basis |
|---|---|---|
| Is the Z-012D preparation package complete? | `YES` | Seven governed documents cover scenarios, evidence, environment readiness, roles, cleanup, and all ten external confirmations. |
| Is synthetic dataset specification ready to proceed? | `AUTHORIZED` | The scenario catalogue and approved contracts provide the boundary for an exact versioned specification. |
| Is synthetic dataset implementation authorized? | `BLOCKED` | No implementation-ready dataset, deterministic dataset identity, exact fixture values, or approval record exists. |
| Is isolated environment specification ready to proceed? | `AUTHORIZED` | Z-012D defines the control model; an exact non-provisioning environment specification may now be prepared. |
| Is environment provisioning authorized? | `BLOCKED` | No environment ID, owner assignment, approved configuration manifest, isolation proof, or provisioning authorization exists. |
| Are named internal UAT roles assigned? | `NO - BLOCKED` | Z-012D defines roles but assigns no approved principals. |
| Is an executable scenario subset approved? | `NO - BLOCKED` | All 25 scenarios remain specifications; none has an approved dataset, environment, roles, or execution record. |
| Is Controlled UAT data loading authorized? | `BLOCKED` | Dataset implementation, environment readiness, role assignment, scenario selection, and loading authority are absent. |
| Is Controlled UAT execution authorized? | `BLOCKED` | Entry criteria are unmet and ten external confirmations remain unresolved. |
| Is internal Annex E-1 workbook generation authorized for UAT evidence? | `BLOCKED` | It is an execution activity and no run, dataset, environment, role, or scenario authority exists. |
| Is external delivery authorized? | `BLOCKED` | No delivery authority exists; AE-DR-002, 004, 010, 016, 019, 020A, and 024 remain unresolved. |
| Is BIR submission authorized? | `BLOCKED` | No submission channel or BIR acceptance evidence exists. |
| Is Production rollout authorized? | `BLOCKED` | UAT, external decisions, retention policy, and Production controls are incomplete. |

## 3. Activity gate matrix

`CONDITIONALLY_AUTHORIZED` is not used because no activity requires a delayed permission in this review: preparation specifications are authorized now, while operational activities remain blocked pending new evidence.

| Activity | Decision | Authoritative evidence | Unmet prerequisites / external decisions | Permitted actions | Prohibited actions | Accountable role | Evidence required before next review |
|---|---|---|---|---|---|---|---|
| 1. Synthetic UAT dataset specification | `AUTHORIZED` | Z-012D plan and 25-scenario catalogue; approved Accounting profile | Exact values, identities, source populations, expected rows, reset model, version/hash, and approval record do not exist | Draft one synthetic-only, versioned, deterministic specification | Create fixtures, load data, use real data, or claim dataset approval | Synthetic Data Steward; Controlled UAT Authorizer approves later | Committed specification with exact values, expected outputs, hash/version, scenario coverage, privacy review, and approval status |
| 2. Synthetic UAT dataset implementation | `BLOCKED` | Z-012D requires approved specifications before loading | Approved implementation-ready dataset and implementation authority absent | Read-only implementation assessment | Create fixture files, seed scripts, database rows, or artifacts | Controlled UAT Authorizer | Approved dataset specification plus explicit implementation authorization and changed-path boundary |
| 3. Isolated UAT environment specification | `AUTHORIZED` | Z-012D environment-readiness checklist | Exact topology, IDs, owners, configuration-key manifest, paths, network controls, and cleanup commands absent | Draft a non-secret, non-provisioning environment specification | Start services, create databases/containers/volumes/networks, or store credentials | Environment Owner; Security/Privacy Reviewer | Committed topology and control specification with ownership, isolation, configuration-presence, resource-labeling, rollback, and cleanup design |
| 4. Isolated UAT environment provisioning | `BLOCKED` | Z-012D expressly requires separate authority | Approved environment specification, run ID, owners, resource manifest, security review, and provisioning authorization absent | Read-only feasibility review | Provision or access any shared, standing UAT, staging, or Production resource | Controlled UAT Authorizer; Environment Owner | Explicit provisioning record naming exact baseline, environment ID, owners, approved resources, network boundary, and cleanup verifier |
| 5. Assignment of named internal UAT roles | `BLOCKED` | Z-012D RACI defines roles only | No authoritative principal assignments or acceptance record | Prepare an assignment proposal using role references | Assign people, infer identity, combine prohibited duties, or claim acceptance | Controlled UAT Authorizer | Signed/committed role assignment with accountable, responsible, reviewer, and cleanup-separation acceptance |
| 6. Selection of executable scenario subset | `BLOCKED` | Z-012D catalogue marks every scenario unexecuted | Dataset, environment, roles, external-decision posture, and execution authority absent | Analyze candidate subsets in documentation | Approve or execute a subset | Controlled UAT Authorizer with Accounting and Technical Review | Exact scenario list, exclusions, dependency posture, dataset mapping, owners, stop rules, and approval |
| 7. Controlled UAT data loading | `BLOCKED` | Z-012D plan says no data may be loaded without separate authority | Activities 2, 4, 5, and 6 blocked | None | Create, assign, seed, import, or mutate UAT data | Controlled UAT Authorizer; Environment Owner; Data Steward | Explicit loading authorization binding dataset hash, environment ID, baseline, scope, executor, rollback, and evidence manifest |
| 8. Controlled UAT execution | `BLOCKED` | Z-012C and Z-012D; ten unresolved confirmations | All execution entry criteria unmet; AE-DR-002, 004, 010, 011A, 019, 020A, 024 block affected execution; AE-DR-012 blocks nonzero privilege paths | None | Start POS Server for UAT or invoke any scenario | Controlled UAT Authorizer | Approved dataset/environment/roles/subset, resolved or explicitly bounded external posture, readiness evidence, and execution authorization |
| 9. Evidence review and acceptance | `BLOCKED` | Evidence template is sufficient as a future template | No authorized run or execution evidence exists | Review specification quality only | Populate fabricated run evidence or accept nonexistent results | Evidence Custodian; Technical, Accounting, Security/Privacy Reviewers | Completed immutable manifests, reconciliations, artifact hashes, authorization evidence, failures, cleanup proof, and reviewer decisions |
| 10. Internal workbook generation for evidence | `BLOCKED` | Z-012B supports local deterministic generation; Z-012D does not authorize execution | No approved run, dataset, environment, roles, scenario subset, or generation authority | Inspect merged runtime and expected contracts read-only | Generate, download, or retain a UAT workbook | Controlled UAT Authorizer; Scenario Executor | Explicit scenario/run authorization and readiness evidence; unresolved presentation gates must be bounded or resolved |
| 11. External Annex E-1 delivery | `BLOCKED` | Z-012C/Z-012D external-delivery boundary | AE-DR-002, 004, 010, 016, 019, 020A, 024 and delivery authority unresolved | Prepare questions and non-delivery controls | Email, transfer, publish, or hand off any workbook | External Delivery Authorizer | Exact accepted artifact, filename, remarks, formatting, terminal, geometry, security/channel rules, recipient, and approval record |
| 12. BIR submission | `BLOCKED` | No approved submission contract | AE-DR-002 and 016 plus acceptance/delivery decisions unresolved | Preserve submission questions | Submit or represent an artifact as accepted | BIR/Accreditation authority; External Delivery Authorizer | Written BIR/examiner submission and artifact acceptance evidence plus separate submission authority |
| 13. Production rollout | `BLOCKED` | Package explicitly excludes Production | UAT incomplete; all applicable external gates; AE-DR-016B retention; Production controls and authority absent | Production gap analysis only | Deploy, enable, migrate, use Production data, or purge | Production Authorizer | Accepted UAT evidence, resolved external/retention decisions, deployment/security/rollback controls, and explicit Production authorization |
| 14. Annex E-2 through E-5 work | `BLOCKED` | Existing scope authorizes E-1 only | Separate applicability, privacy, contract, and authorization absent | Retain as future dependency | Design or implement E-2 through E-5 under this review | Product/Fiscal Design Authority plus external/privacy authorities | Separate governed profile and authorization package |
| 15. ARTS POSLog 6.0.0 work | `BLOCKED` | Explicitly deferred from Annex E work | Profile mapping and separate authorization absent | Retain canonical event-stream dependency | Design or implement POSLog under this review | Separate POSLog authority | Separate governed task and approved mapping profile |
| 16. Signing or encryption | `BLOCKED` | AE-DR-016 unresolved | Exact BIR/examiner requirement, algorithms, keys, custody, and delivery process absent | Document external question only | Implement, configure, or apply signing/encryption | BIR/Examiner; Security; External Delivery Authorizer | Written technical requirement, security design, key custody, and implementation authorization |
| 17. Destructive retention, archival, cleanup, or purge | `BLOCKED` | Z-012D permits invocation-owned non-destructive cleanup only | AE-DR-016B and legal-hold/deletion evidence unresolved | Design non-destructive invocation-owned cleanup controls | Delete governed fiscal evidence, archives, legal holds, or shared/Production data | Legal/Compliance/Records Authority | Approved retention schedule, archive/legal-hold rules, deletion authority, evidence, and bounded implementation authorization |

## 4. Dataset readiness review

The package provides scenario objectives, synthetic preconditions, expected behavior, reconciliations, evidence categories, privacy rules, and cleanup expectations. It does **not** provide an implementation-ready dataset.

Missing dataset evidence:

1. A stable dataset identifier, version, canonical serialization, and content hash.
2. Exact synthetic Site POS Server, fiscal identity, currency, month, period, document, Z, BIR Summary, Electronic Journal, fact, actor, and authorization references.
3. Exact fiscal-document populations and source status transitions for every included scenario.
4. Exact PHP minor-unit tax, discount, tender, GTA, counter, Manual SI/OR, overflow, and known-zero values.
5. Exact expected H01-H10 and D01-D32 values per generated row.
6. Exact replay, semantic-conflict, correction, supersession, tamper, and rollback fixture lineage.
7. Deterministic setup/reset ordering and a proof that reset touches only invocation-owned resources.
8. A scenario-to-dataset coverage matrix and approval record.

Decision: dataset specification is `AUTHORIZED`; dataset implementation and loading are `BLOCKED` until the exact specification is committed, reviewed, and separately approved.

## 5. Environment readiness review

The Z-012D checklist sufficiently defines the future control categories: PostgreSQL 16, synthetic-only data, exact baseline, `CONTROLLED_UAT_ISOLATED_SYNTHETIC`, Production fixture rejection, exact permissions/scope, isolated artifact/evidence roots, run-owned resources, rollback, and verified cleanup.

No actual environment identifier, owner assignment, application/package hash, configuration-presence manifest, network-isolation proof, resource topology, artifact/evidence path registration, run ID, rollback command set, or cleanup verification exists. Environment specification is `AUTHORIZED`; provisioning and execution are `BLOCKED`.

## 6. Role and authority review

The RACI is complete as a reusable role model but contains no authoritative principal assignments. UAT authorization, environment ownership, dataset approval, execution, evidence custody/review, Accounting review, technical review, blocker disposition, cleanup verification, external confirmation, delivery authorization, and Production authorization remain unassigned. Role definitions are not assignments. Named assignment is `BLOCKED` until the Controlled UAT Authorizer issues and the assignees accept a governed separation-of-duties record.

## 7. Scenario-subset review

Every scenario is currently blocked by the absent implementation-ready dataset, unprovisioned/unverified environment, unassigned roles, unapproved subset, and absent execution authorization. Future eligibility below does not authorize execution.

| Scenario | Future eligibility under current E-1 contract | Additional external blocker |
|---|---|---|
| AE1-UAT-001 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None for bounded internal assertions; AE-DR-002/004/010/019/020A/024 block official acceptance |
| AE1-UAT-002 | `BLOCKED_EXTERNAL_CONFIRMATION` | AE-DR-010 and AE-DR-011A |
| AE1-UAT-003 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-004 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-005 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None for chronology; AE-DR-019/020A/024 affect presentation acceptance |
| AE1-UAT-006 | `BLOCKED_EXTERNAL_CONFIRMATION` | AE-DR-020A and AE-DR-024 |
| AE1-UAT-007 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | AE-DR-019 affects display acceptance only |
| AE1-UAT-008 | `BLOCKED_EXTERNAL_CONFIRMATION` | AE-DR-011A, AE-DR-012, AE-DR-019 |
| AE1-UAT-009 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-010 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-011 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-012 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | AE-DR-002/004/019/024 affect official artifact acceptance only |
| AE1-UAT-013 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-014 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-015 | `BLOCKED_EXTERNAL_CONFIRMATION` | AE-DR-004 |
| AE1-UAT-016 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-017 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-018 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-019 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-020 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-021 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-022 | `BLOCKED_EXTERNAL_CONFIRMATION` | AE-DR-012 |
| AE1-UAT-023 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | None |
| AE1-UAT-024 | `ELIGIBLE_AFTER_COMMON_PREREQUISITES` | AE-DR-016B blocks generalized destructive cleanup, not the specified non-destructive proof |
| AE1-UAT-025 | `BLOCKED_EXTERNAL_CONFIRMATION` | AE-DR-002/004/010/011A/019/020A/024 |

Counts:

- Executable now: **0**.
- Eligible for a future separately authorized bounded run after common prerequisites: **19**.
- Additionally blocked by external confirmations: **6**.
- Permanently outside the current E-1 scenario scope: **0**.
- Blocked by missing dataset specification/implementation: **25**.
- Blocked by missing environment readiness evidence: **25**.
- Blocked by missing role assignments and subset/execution authorization: **25**.

## 8. External-confirmation impact

All ten decisions remain `UNRESOLVED`. Dataset and environment **specification** may proceed if every affected value is marked blocked, provisional, or fail closed exactly as governed. Dataset implementation, data loading, execution, and workbook generation remain blocked independently by missing UAT prerequisites.

| Decision | Affected fields/scenarios | Dataset specification | Dataset implementation | Environment specification | Execution / internal workbook | Delivery or submission |
|---|---|---|---|---|---|---|
| AE-DR-002 | Official XLSX/companions; 001, 012, 025 | May specify current internal XLSX and unresolved acceptance | Not independently blocked, but not authorized | May proceed | Official acceptance blocked | Blocked |
| AE-DR-004 | Filename; 001, 012, 015, 025 | May specify current non-prescriptive filename | Not independently blocked, but not authorized | May proceed | Official filename/correction acceptance blocked | Blocked |
| AE-DR-010 | D32 Remarks; 001, 002, 025 | May specify governed `NONE`/`NO_ACTIVITY`; blank remains unresolved | Affected representation not approved for UAT | May proceed | Affected scenarios blocked | Blocked |
| AE-DR-011A | D14/D15 inactive representation; 002, 006, 008, 020, 025 | May specify immutable known-zero evidence and unresolved display | Affected representation not approved for UAT | May proceed | Affected scenarios blocked | Does not independently authorize transport |
| AE-DR-012 | Nonzero Diplomat/other privilege; 006, 008, 022 | May specify zero-only and expected nonzero rejection | Nonzero path blocked; no remapping | May proceed | Nonzero scenarios blocked; zero-only still needs separate authority | Does not independently authorize transport |
| AE-DR-016 | Signing/encryption/compression/channel; 025 | No dataset impact | No dataset impact | May specify default-deny external network | Does not independently block strictly local execution, which is otherwise blocked | Blocked |
| AE-DR-016B | Retention/archive/legal hold; 015, 024 | May specify non-destructive cleanup only | No dataset effect | May specify run-owned cleanup | Does not independently block non-destructive UAT; destructive cleanup blocked | Production retention/purge blocked |
| AE-DR-019 | Number/date display; 001, 005, 007, 008, 012, 025 | May specify internal invariant values as provisional | Display acceptance not approved for UAT | May proceed | Affected presentation scenarios blocked | Blocked |
| AE-DR-020A | H08 terminal identity; 001, 005, 006, 025 | May specify immutable Site POS Server code as provisional | Header acceptance not approved for UAT | May proceed | Affected scenarios blocked | Blocked |
| AE-DR-024 | Geometry/wrapping; 001, 005, 006, 012, 025 | May reference current preserved geometry and fail-closed overflow | Geometry acceptance not approved for UAT | May proceed | Affected scenarios blocked | Blocked |

None of these decisions is resolved or altered by this review.

The affected-scenario lists above are the union of the Z-012D scenario catalogue and external-confirmation tracker. Those two merged documents differ in some cross-references for AE-DR-011A, 012, 019, 020A, and 024. The difference does not alter a decision or authorize a scenario, but the next dataset specification must publish one reconciled decision-to-scenario matrix before subset approval.

## 9. Evidence and cleanup review

The evidence manifest is sufficient as a future template: it binds authorization, commit, profile/hash, template/renderer, environment, dataset, scenario, scope, roles, timestamps, outcome, safe correlation, artifacts, hashes, reconciliations, authorization results, mutation boundaries, failures, external dependencies, and cleanup. Its prohibited-content rules exclude credentials, raw payloads, personal/statutory data, stack traces, paths, and Production endpoints.

The cleanup checklist is sufficient for invocation-owned non-destructive cleanup planning. It requires exact run-ID resources, rollback through supported behavior, preservation of governed evidence, no wildcard/shared deletion, independent verification, and a post-cleanup manifest. It does not resolve AE-DR-016B or authorize deletion of governed fiscal records. Both templates remain unevaluated until a separately authorized run creates evidence.

## 10. Residual risks and blockers

1. Ten external confirmations remain unresolved: AE-DR-002, 004, 010, 011A, 012, 016, 016B, 019, 020A, and 024.
2. No deterministic, versioned, implementation-ready synthetic dataset exists or is approved.
3. No isolated environment is provisioned and no concrete environment specification, identifier, owner, or isolation proof exists.
4. No internal principals are assigned to the governed UAT roles.
5. No executable scenario subset is approved.
6. No data-loading or execution authorization exists.
7. No execution evidence exists for review or acceptance.
8. External delivery, BIR submission, signing/encryption, and Production controls remain absent and unauthorized.
9. AE-DR-016B leaves Production retention, archive, legal hold, and destructive deletion unresolved.
10. Z-012D's scenario catalogue and external tracker have non-authoritative cross-reference differences that must be normalized in the dataset specification without changing decision statuses.
11. E-2 through E-5 and ARTS POSLog 6.0.0 remain outside this authority.

## 11. Next bounded activity

The smallest authorized activity is to create **one versioned, implementation-ready synthetic Annex E-1 dataset specification** for the 19 future-eligible scenarios. It must define exact deterministic identities, source populations, PHP minor-unit values, expected H/D positions, reconciliations, replay/correction lineage, setup/reset ordering, privacy controls, scenario mapping, version/hash, and approval fields. The six externally blocked scenarios must remain excluded or explicitly marked non-executable.

An exact isolated-environment specification may also be prepared as documentation, but no resource may be provisioned. Named role assignment, scenario-subset approval, dataset implementation/loading, environment provisioning, execution, internal workbook generation, delivery, submission, Production, E-2 through E-5, ARTS POSLog, signing/encryption, and destructive retention remain blocked.

Overall UAT decision: `AUTHORIZED_FOR_NEXT_BOUNDED_PREPARATION`.
