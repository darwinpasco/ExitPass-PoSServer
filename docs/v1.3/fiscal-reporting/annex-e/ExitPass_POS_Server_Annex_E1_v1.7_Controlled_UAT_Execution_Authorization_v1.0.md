# ExitPass POS Server Annex E-1 v1.7 Controlled UAT Execution Authorization v1.0

## 1. Document Control

| Item | Value |
| --- | --- |
| Document version | `v1.0` |
| Status | `AUTHORIZED_FOR_ANNEX_E1_V1_7_CONTROLLED_UAT_EXECUTION` |
| Repository | `D:\SourceCodes\ExitPass-PoSServer` |
| Authorization branch | `docs/annex-e1-v1-7-controlled-uat-execution-authorization` |
| Reviewed `origin/dev` baseline | `e6c85e3f80ba2773104416a649ea164baa81a1aa` |
| Predecessor decision | `READY_FOR_CONTROLLED_UAT_AUTHORIZATION` |
| Applicable package | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.7` |
| Preparation date | `2026-08-17 Asia/Manila` |
| Scope owner | Product/Fiscal Design Authority |
| Approval authority | Controlled UAT Authorizer |

This document prospectively supersedes older Annex E-1 execution-blocked preparation records only for the exact, synthetic, 19-case v1.7 Controlled UAT scope defined here. It does not rewrite their historical decisions, resolve an external decision, or supersede any Production, external-delivery, BIR-submission, legal-hold, or regulatory-retention gate.

## 2. Authorization Decision

**Decision: `AUTHORIZED_FOR_ANNEX_E1_V1_7_CONTROLLED_UAT_EXECUTION`.**

This decision authorizes a separately initiated task to execute the complete frozen Annex E-1 v1.7 synthetic Controlled UAT exactly as specified below. Permission is effective for a run only after every precondition has passed, the required principals have accepted their roles, and the run record binds the exact authorized Git commit and artifact commitments. Any mismatch fails closed.

This document does not execute Controlled UAT. No UAT command, application, database, container, workbook generator, Excel renderer, PDF renderer, or external service was run while preparing it.

## 3. Purpose And Boundary

The authorized later task may validate, load, execute, render, visually inspect, and package evidence for the 19 included v1.7 synthetic cases in one invocation-owned environment. It may retain only the governed internal evidence described in this document.

This authorization does not:

- authorize Production, a shared development environment, standing UAT, staging, or `exitpass_v12_dev`;
- approve any workbook or derived artifact for official BIR submission;
- authorize external delivery, an official filename, signing, certification, acceptance, BIR retention, or submission;
- authorize Annex E-2 through E-5 or ARTS POSLog;
- authorize HikCentral, payment-provider, BIR, or other external-service access;
- authorize runtime, API, database-schema, migration, controlled-code, dependency, CI, dataset, validator, generator, workbook-layout, or expected-result changes;
- authorize real customer, vehicle, payment, statutory, fiscal, or Production data.

The ten external decisions `AE-DR-002`, `AE-DR-004`, `AE-DR-010`, `AE-DR-011A`, `AE-DR-012`, `AE-DR-016`, `AE-DR-016B`, `AE-DR-019`, `AE-DR-020A`, and `AE-DR-024` remain `UNRESOLVED`. The merged v1.7 package proves that none supplies a byte needed for this bounded internal 19-case run. They continue to govern external presentation, acceptance, delivery, signing, retention, submission, nonzero unsupported privileges, and Production activity.

## 4. Authorized Baseline

### 4.1 Repository And Package Identity

| Commitment | Authorized value |
| --- | --- |
| Git commit | `e6c85e3f80ba2773104416a649ea164baa81a1aa` |
| Specification ID | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.7` |
| Manifest path | `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Manifest_v1.7.md` |
| Manifest Git blob | `7a2fda73bf84aafc2131e670138a880dd03ecea8` |
| Governed manifest members | `8` |
| Package-root preimage bytes | `1,897` |
| Package-root SHA-256 | `5d60cb50d7f43b29f79486801a470e57128e2b571c4e50a37494e4a82131b829` |
| Dataset path | `docs/v1.3/fiscal-reporting/annex-e/dataset/v1.7/annex-e1-synthetic-uat-dataset-v1.7.jsonl` |
| Dataset Git blob | `08e83c0f98b3d29fb9cc5fe9114f1447a0c9f0b7` |
| Canonical dataset format | UTF-8 JSON Lines with LF terminators |
| Canonical dataset bytes | `8,070,439` |
| Canonical dataset SHA-256 | `ed6971df7a142a54492eea968587a150c0229618650cee56ea2116154692a1ac` |
| Dataset records | `4,074` |
| Deterministic identities | `2,364` |
| Semantic families / instances | `29` / `1,690` |
| Included / excluded cases | `19` / `6` |
| Annex rows | `22` |
| Accounting facts | `155` |
| F22 records / F23 transitions | `148` / `148` |
| EJ streams / genesis / predecessors | `19` / `19` / `129` |
| Statutory finality comparisons | `30` |
| Isolated-execution normalized SHA-256 | `eb49cc8f3ba5a2935463e030f7e2b1707131aff68bcfd1d37c4b6ea1e224a18e` |
| Workbook aggregate-manifest SHA-256 | `d965ec39286ea3d92fc32e64a968ed014f0486a8698819ae6caff2b95d7d72b5` |
| Workbook manifest-file SHA-256 | `b2961b498ec7b6654366eb5ef30f99cde5def62326f52249f05cedf09405217d` |
| Workbook negative tests | `15` |
| Runtime tests | `209 passed, 0 failed, 0 skipped` |
| Printable warning | `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION` |

The dataset commitment applies to canonical LF repository bytes. A Windows checkout may present CRLF working-tree bytes because the repository currently declares the JSONL as `text=auto`; the later task must calculate and record both the working-tree state and the canonical LF byte commitment, and the existing validator and workbook reader must pass without changing the committed file. The operator must not rewrite or normalize the tracked dataset to force a match.

### 4.2 Governed Member Inventory

The eight members, exact byte lengths, and SHA-256 values in the v1.7 manifest are authoritative. The later task must independently match every manifest row and the package root before creating a runtime resource. A member, manifest, dataset, script, test, or commitment mismatch results in `CONTROLLED_UAT_BLOCKED_BASELINE`.

### 4.3 Exact Scope

Included cases are `001`, `003`, `004`, `005`, `007`, `009`, `010`, `011`, `012`, `013`, `014`, `016`, `017`, `018`, `019`, `020`, `021`, `023`, and `024`. Excluded cases are `002`, `006`, `008`, `015`, `022`, and `025`. No excluded case may be loaded or recreated indirectly.

## 5. Authorized Environment

The later execution record must designate one Windows host by hostname and a non-secret host inventory reference. That host is authorized only when all following conditions are true:

| Control | Required posture |
| --- | --- |
| Environment classification | `CONTROLLED_UAT_ISOLATED_SYNTHETIC` |
| Windows PowerShell | `5.1`; `powershell.exe -NoProfile -ExecutionPolicy Bypass` for repository runners |
| .NET | Installed .NET 8 SDK capable of building and running the repository's `net8.0` projects; dependency assets already available locally |
| Microsoft Excel | Microsoft Excel `16.0.20228.20190`, the exact validated build; normal hidden read-only open with alerts and link updates disabled |
| Open XML | `DocumentFormat.OpenXml` `3.5.1` from the runtime test project |
| PDF rendering | Excel fixed-format export plus local `Windows.Data.Pdf`; record the actual Windows build and require behavior compatible with the validated Windows `10.0.26200.0` baseline |
| Docker image | Locally available `postgres:16-alpine`; record its immutable local image ID before execution and require PostgreSQL server version `16.x` |
| Database connectivity | An ephemeral host port bound only to `127.0.0.1`, selected by the existing runner; never an externally routable bind |
| Database resources | Uniquely named run-ID containers, databases, volumes, and networks created by `Invoke-AnnexE1V17IsolatedExecution.ps1` |
| Output | A new empty absolute directory under a run-ID task root, outside the repository, supplied through `-OutputDirectory` |
| Evidence | A separate, access-controlled run-ID evidence directory outside the repository and outside workbook working storage |
| Network | No external service is required; no shared database or inherited remote endpoint may be configured |
| Clock | Do not change dataset timestamps. Record UTC, Philippine Standard Time, host timezone, and clock source; v1.7 fixed timestamps remain authoritative |
| Storage | Record free space before execution and verify it exceeds the declared maximum for Docker working data, 19 workbooks, 19 PDFs, rendered images, transcripts, manifests, and evidence; otherwise block before resource creation |

The runner temporarily sets `ANNEX_E1_V17_HARNESS_DB_URL`, `ANNEX_E1_V17_DATASET_PATH`, `ANNEX_E1_V17_REPORT_PATH`, `ANNEX_E1_V17_WORKBOOK_DATASET_PATH`, `ANNEX_E1_V17_WORKBOOK_OUTPUT_PATH`, and `ANNEX_E1_V17_WORKBOOK_REPORT_PATH`. The preflight must reject inherited values for these variables and any shared, UAT, staging, Production, `exitpass_v12_dev`, HikCentral, payment-provider, or BIR connection setting. The runner-generated database password is ephemeral, must not appear in governed evidence, and must be discarded during cleanup.

## 6. Authorized Roles

| Role | Required responsibility and sign-off |
| --- | --- |
| Controlled UAT Executor | Performs the exact authorized sequence, records commands and exit codes, stops on every mandatory condition, and signs the execution result. |
| Evidence Recorder | Captures privacy-safe evidence, hashes, timestamps, inventories, checklists, and deviations without secrets or raw sensitive payloads; signs the evidence manifest. |
| Independent Reviewer | Reviews semantic, runtime, workbook, visual, and evidence results without relying solely on the executor's conclusion; signs acceptance or rejection. |
| Authorization Approver | Confirms baseline, scope, role assignments, entry criteria, and final disposition; cannot waive a mandatory stop condition. |
| Environment Owner | Confirms the host, toolchain, isolation, output roots, local Docker image, and resource inventory. |
| Accounting Reviewer | Reviews SP-A and R01-R12 zero-difference evidence and signs the financial reconciliation result. |
| Security/Privacy Reviewer | Confirms synthetic-only data, prohibited-access absence, credential exclusion, and privacy-safe evidence. |
| Cleanup Verifier | Independently confirms that every invocation-owned process, Docker resource, database, workbook, PDF, image, probe, script, and temporary directory is removed or retained under an approved evidence disposition. |

The executor cannot be the sole Independent Reviewer, Evidence Recorder, or Cleanup Verifier. One person may hold multiple other roles only when the execution record identifies each assignment and the Authorization Approver accepts the combination before execution. The record must never describe review as independent when the executor and reviewer are the same principal.

## 7. Preconditions

Every item must be recorded as `PASS` before the first validator command:

1. The task worktree is clean, is on the authorized execution branch, and its `HEAD` is exactly the approved execution commit derived from baseline `e6c85e3f80ba2773104416a649ea164baa81a1aa` without unexplained changes.
2. `git diff --check`, `git status --short --branch --untracked-files=all`, and the changed-path inventory show only files authorized by the later execution task.
3. The v1.7 manifest, its eight members, dataset, three runners, integration test, workbook tests, and runtime projects exist at the paths named in this document.
4. All baseline hashes, canonical dataset counts, and the Git blob identities match Section 4.
5. Windows PowerShell 5.1, .NET 8, Docker, local `postgres:16-alpine`, Microsoft Excel `16.0.20228.20190`, and the local PDF renderer are available and recorded.
6. The task run ID is unique; no process, container, database, volume, network, output directory, evidence directory, or Excel process already uses it.
7. The workbook output path does not exist; the task and evidence parent paths are exact, absolute, access controlled, and outside the repository.
8. No prohibited connection string, shared service configuration, Production setting, external credential, or inherited Annex E-1 runner variable is present.
9. Free-space evidence is recorded and satisfies the run's declared storage budget.
10. The operator acknowledges in writing that all inputs and outputs are synthetic internal test artifacts and are not approved for BIR submission.
11. The Evidence Recorder has an empty manifest, the reviewers have the per-case checklist, and retention and cleanup dispositions are accepted.
12. All assigned principals have accepted their role and mandatory stop authority.

Failure of any item produces `CONTROLLED_UAT_BLOCKED_BASELINE` or `CONTROLLED_UAT_BLOCKED_ENVIRONMENT`; no resource may be created.

## 8. Authorized Execution Sequence

The later task must execute these steps in order. A passed inner check does not permit skipping a later check.

### 8.1 Record And Verify

1. Record run ID, host, assigned principals, UTC and Asia/Manila start times, timezone, `git rev-parse HEAD`, `git status --short --branch --untracked-files=all`, PowerShell version, `dotnet --info`, Docker version, local `postgres:16-alpine` image ID, PostgreSQL image version, Excel product version, Windows build, and available storage.
2. Verify the paths and Section 4 commitments. Record the eight manifest-member lengths/hashes, package-root result, dataset canonical-LF length/hash, Git blobs, and current worktree EOL state.
3. Calculate the dataset commitment without writing the tracked file. Read the UTF-8 bytes with strict decoding, reject a lone carriage return, replace only CRLF pairs with LF in memory, require `8,070,439` bytes and SHA-256 `ed6971df7a142a54492eea968587a150c0229618650cee56ea2116154692a1ac`, and record the raw working-tree length/hash separately. This is the same canonical-LF posture used by the v1.7 workbook reader; it is not permission to edit the dataset.
4. Run the offline validator from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1SyntheticUatDatasetV17Validation.ps1
```

Require exit code zero and the exact expected count and commitment fields. Working-tree EOL reporting must not replace the canonical LF commitment.

### 8.2 Isolated Execution And Determinism

5. Run the existing isolated workflow, which creates only uniquely named invocation-owned resources and cleans them in `finally`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1V17IsolatedExecution.ps1 -DockerImage postgres:16-alpine
```

6. Require `ANNEX_E1_V17_HARNESS=PASS`, `CLEAN_EXECUTIONS=2`, `INCLUDED_CASES=19`, `LOADABLE_FAMILIES=29`, `LOADED_DATASET_RECORDS=4074`, `DISPOSABLE_RESOURCES_REMAINING=0`, and normalized SHA-256 `eb49cc8f3ba5a2935463e030f7e2b1707131aff68bcfd1d37c4b6ea1e224a18e`. Preserve the two normalized reports or their exact bytes/hashes in evidence. Verify all 30 statutory-finality comparisons, all 155 C02 facts, F22/F23 counts and chains, case-package commitments, SP-A, and R01-R12 results from the runner report.
7. Run the complete current runtime suite:

```powershell
dotnet test .\tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj --no-restore --configuration Release --verbosity minimal
```

Require `209 passed, 0 failed, 0 skipped`. A changed discovered-test count blocks the run until explained and separately approved; it is not silently substituted.

### 8.3 Workbook Generation And Automated Validation

8. Generate the 19 workbooks into the predeclared new empty task-owned directory:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1V17WorkbookGeneration.ps1 -OutputDirectory <new-empty-task-owned-directory> -DockerImage postgres:16-alpine
```

9. Require the generator's offline validation, isolated execution, 15 negative tests, Open XML validation, and two-run byte determinism to pass. Require exactly 19 `.xlsx` files, 22 Annex rows, aggregate manifest SHA-256 `d965ec39286ea3d92fc32e64a968ed014f0486a8698819ae6caff2b95d7d72b5`, and manifest-file SHA-256 `b2961b498ec7b6654366eb5ef30f99cde5def62326f52249f05cedf09405217d`.
10. Record every workbook filename, case, Annex-row count, byte length, XLSX SHA-256, normalized semantic SHA-256, and authorized AE1H workbook-content commitment. Validate no macro, formula, external link, external data connection, hidden sheet, embedded executable payload, duplicate ZIP entry, dangling relationship, or Open XML error exists.

### 8.4 Excel, PDF, And Visual Review

11. Use only the controlled pattern already recorded in the merged presentation revalidation: a hidden Microsoft Excel `16.0.20228.20190` COM instance with alerts, events, macros, and link updates disabled; `Workbooks.Open(<generated-workbook>, 0, $true)`; exactly three visible sheets in order `E-1`, `Validation`, `Fact Links`; `E-1` active; close without saving. Record one result for every case. Repair, recovery, extraction, compatibility handling, Protected View, or save-on-close is prohibited.
12. Export only `E-1` through `Worksheets.Item('E-1').ExportAsFixedFormat(0, <task-owned-pdf>)`. Require exactly one PDF page for each of the 19 cases and no diagnostic-sheet leakage. Record the exact invocation-owned PowerShell script bytes and SHA-256 used to apply this merged pattern; the script must remain outside the repository and may not change a workbook.
13. Rasterize every page locally with `Windows.Data.Pdf.PdfDocument.LoadFromFileAsync` and `PdfPage.RenderToStreamAsync`. Record the temporary renderer script hash and Windows build. No external renderer is permitted.
14. The Independent Reviewer visually inspects all 19 pages and all 22 rows, including two-row case `004` and three-row case `005`. The checklist must cover H01-H10, D01-D32, R01-R12, case/workbook identifiers, chronology, number precision, warning visibility, clipping, overlap, blank pages, row splitting, print area, pagination, and readability.
15. Require the exact warning `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION` exactly once in the visible `E-1` worksheet body and printable PDF of every case. It must remain at `A11:AF11`, inside the configured print area, and must not obscure governed values.
16. Inspect `Validation` and `Fact Links` structurally in all workbooks and visually for at least a standard one-row case, cases `004` and `005`, replay case `012`, correction case `013`, and finality case `024`. Record R01-R12 zero differences and readable identity/fact-link evidence. Long diagnostic values may require cell selection; this known non-blocking presentation observation is not a waiver for a missing or contradictory value.

### 8.5 Evidence, Cleanup, And Result

17. Assemble the Section 9 evidence package without secrets, connection strings, raw sensitive payloads, or unrestricted machine paths.
18. Stop only task-owned processes and perform Section 12 cleanup. Capture pre- and post-cleanup inventories. Do not remove retained evidence until its disposition is approved.
19. The Cleanup Verifier independently confirms zero remaining task-owned runtime resources and records the result.
20. The executor, Independent Reviewer, Accounting Reviewer, Security/Privacy Reviewer, Cleanup Verifier, and Authorization Approver sign their applicable sections.
21. Issue exactly one Section 13 Controlled UAT result.

## 9. Evidence Package

The Evidence Recorder must create one run manifest and one per-case record using the existing [Z-012D Evidence Manifest Template](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Evidence_Manifest_Template_v1.0.md), extended by the following required fields:

- this authorization document path, version, decision, and Git blob after merge;
- exact execution commit, branch, worktree status, changed paths, and baseline ancestry;
- run ID, host identifier, environment classification, and assigned principal-to-role references;
- UTC and Asia/Manila start, completion, review, and cleanup timestamps;
- PowerShell, .NET, Docker, PostgreSQL image ID/version, Excel, Open XML, Windows, and PDF renderer versions;
- preflight checklist, free-space result, prohibited-environment-variable scan, and pre-run resource inventory;
- eight governed member hashes/lengths, package root, dataset canonical-LF hash/length, Git blobs, and worktree EOL posture;
- raw command transcripts, sanitized arguments, exact exit codes, and output-file hashes;
- offline validator output and isolated-execution normalized reports/hashes;
- runtime-test result and discovered count;
- workbook manifest bytes/hash, aggregate hash, and per-workbook byte/semantic commitments;
- Open XML, safety, formula, macro, external-link, data-connection, hidden-sheet, and embedded-content results;
- 15 negative-test results;
- Excel normal-open result for all 19 cases, including absence of repair or warning;
- 19 PDF filenames/hashes/page counts and proof that only `E-1` was exported;
- visual checklist for every page and all 22 rows, warning verification, and representative diagnostic-sheet review;
- SP-A and R01-R12 zero-difference Accounting sign-off;
- deviation, observation, incident, and stop-condition log, including `NONE` when empty;
- created/removed resource inventories, post-cleanup absence evidence, and cleanup sign-off;
- final executor, reviewer, Accounting, security/privacy, cleanup, and approver decisions.

The evidence root must be task-owned, access controlled, and identified by an opaque run reference. Generated workbooks, PDFs, and images may be retained only inside that internal evidence root when the Authorization Approver explicitly records them as synthetic UAT evidence. They must retain the warning and must not be represented as official BIR artifacts. Database dumps, credentials, tokens, connection strings, raw request/response bodies, local passwords, real data, unrestricted stack traces, and machine-specific temporary files must not be retained.

Before deleting working copies, the Evidence Recorder must verify the retained manifest and artifact hashes. Internal evidence is retained until the Authorization Approver issues an explicit disposition consistent with applicable internal policy. This authorization does not define or satisfy BIR, legal-hold, regulatory, Production, or external-submission retention. No destructive purge of governed evidence is authorized.

## 10. Acceptance Criteria

Every criterion is mandatory for `CONTROLLED_UAT_PASSED` or `CONTROLLED_UAT_PASSED_WITH_NON_BLOCKING_OBSERVATIONS`:

1. The exact Git baseline, v1.7 package root, eight member hashes/lengths, dataset commitment, scripts, tests, and all Section 4 counts match.
2. The offline validator passes without changing a governed file.
3. Isolated execution finishes with no prohibited access, all 4,074 records staged/read back, 29 families loaded, 19 included cases completed, and no excluded case present.
4. Two clean isolated runs have identical normalized bytes and SHA-256 `eb49cc8f3ba5a2935463e030f7e2b1707131aff68bcfd1d37c4b6ea1e224a18e`.
5. The complete runtime suite reports `209 passed, 0 failed, 0 skipped`.
6. Exactly 19 workbooks contain exactly 22 Annex rows, and their aggregate and manifest-file hashes match Section 4.
7. Every workbook passes `DocumentFormat.OpenXml` 3.5.1 validation and all safety checks.
8. All 15 negative workbook tests pass.
9. Excel opens all 19 workbooks normally without repair, recovery, corruption, Protected View, external-link, or compatibility rejection.
10. Each `E-1` sheet exports as exactly one page; no blank page, clipping, overlap, row split, missing field, or diagnostic-sheet leakage exists.
11. All 22 Annex rows are visible, correctly ordered and chronological where required, aligned, and readable.
12. H01-H10 are visible and consistently associated; D01-D32 preserve order, formatting, and precision; R01-R12 have zero expected-versus-calculated monetary and row-count differences.
13. The exact synthetic warning appears visibly and printably exactly once on every `E-1` output.
14. No shared development, standing UAT, staging, Production, `exitpass_v12_dev`, HikCentral, payment-provider, BIR, or external business service is accessed.
15. Every task-owned runtime and temporary resource is removed, and retained evidence has an explicit approved disposition.
16. Evidence is complete, privacy-safe, hash-verifiable, and signed by the required roles.
17. No unauthorized source, scope, semantic, dataset, runtime, schema, test, workbook, or expected-result change occurs.

Non-blocking observations must not alter generated bytes, calculated results, evidence completeness, or any mandatory criterion.

## 11. Mandatory Stop Conditions

The executor must stop immediately and issue a blocked or failed result when any of the following occurs:

- wrong branch, wrong commit, missing ancestry, dirty source, unexplained changed path, or unauthorized baseline;
- governed member, package root, dataset, manifest, workbook manifest, script, test, count, or expected hash mismatch;
- missing, altered, duplicated, clipped, or non-printable synthetic warning;
- dataset, identity, semantic-family, semantic-instance, case, row, Accounting-fact, EJ, transition, source-record, negative-test, or runtime-test count mismatch;
- real, non-synthetic, sensitive, Production, or unexpected data;
- attempted or actual shared, remote, UAT, staging, Production, `exitpass_v12_dev`, HikCentral, payment-provider, BIR, or external-service access;
- inherited connection string, Production/shared configuration, credential, or resource-name collision;
- nondeterministic clean-run report, workbook, manifest, normalized semantic hash, or artifact byte;
- validator, build, isolated execution, runtime test, workbook generation, Open XML, safety scan, negative test, Excel open, PDF export, page count, visual review, or reconciliation failure;
- repair, recovery, corruption, Protected View, external-link, or compatibility warning;
- missing or contradictory H01-H10, D01-D32, R01-R12, Validation, or Fact Links evidence;
- missing transcript, hash, checklist, role sign-off, deviation record, or other required evidence;
- unapproved runtime, dataset, validator, generator, workbook, database, migration, controlled-code, test, expected-result, or scope modification;
- cleanup failure, uncertain ownership, or a remaining task-owned resource;
- any request to treat a workbook, PDF, image, manifest, or result as approved for official BIR submission, external delivery, or Production.

No operator, reviewer, or approver may waive a mandatory stop condition within the run. Baseline/environment failures map to a blocked result; an executed validation or semantic failure maps to `CONTROLLED_UAT_FAILED`; a scope breach or cleanup failure maps to its explicit blocked result in Section 13.

## 12. Cleanup And Recovery

Cleanup is exact and ownership-bound:

1. Use the existing runners' `finally` handling for their uniquely named containers, databases, volumes, networks, environment variables, reports, and internal temporary directories.
2. Close each task-owned workbook without saving. Quit only the task-owned Excel application instance and verify its recorded process ID is absent. Never terminate an unrelated Excel process.
3. Remove only the exact run-ID working workbook directory, PDFs, page images, renderer probes, temporary PowerShell scripts, reports, and temporary directories after evidence hashes and retention decisions are recorded.
4. Remove only exact Docker resources carrying the run's `exitpass.annex-e1-v17.invocation` label and names recorded in the created-resource manifest. Wildcards, broad Docker prune, broad process termination, and repository-wide or shared-path deletion are prohibited.
5. Do not delete immutable fiscal evidence, retained manifests, approved internal evidence, shared resources, archives, legal holds, or any resource whose ownership is uncertain.
6. Capture before/after process, container, database, volume, network, filesystem, output, evidence, and Git inventories. Require zero task-owned runtime resources after cleanup.

If cleanup is incomplete, the execution result is `CONTROLLED_UAT_BLOCKED_CLEANUP`. Record each remaining resource's exact type, name, owner label, location, reason, and bounded recovery action. The result remains blocked until the Cleanup Verifier confirms safe removal or an approved evidence-retention disposition. Recovery may target only the listed task-owned resources.

## 13. Controlled UAT Result Taxonomy

The later task must issue exactly one result:

| Result | Meaning |
| --- | --- |
| `CONTROLLED_UAT_PASSED` | Every mandatory acceptance criterion passed with no observation requiring follow-up. |
| `CONTROLLED_UAT_PASSED_WITH_NON_BLOCKING_OBSERVATIONS` | Every mandatory criterion passed; recorded observations do not change bytes, results, evidence completeness, isolation, cleanup, or scope. |
| `CONTROLLED_UAT_FAILED` | Execution began and a validation, calculation, determinism, runtime, workbook, render, visual, or evidence assertion failed. |
| `CONTROLLED_UAT_BLOCKED_BASELINE` | Baseline, source, hash, count, manifest, branch, or worktree preflight failed before authorized execution could proceed. |
| `CONTROLLED_UAT_BLOCKED_ENVIRONMENT` | Toolchain, host isolation, storage, configuration, resource ownership, or environment readiness failed. |
| `CONTROLLED_UAT_BLOCKED_SCOPE_VIOLATION` | A prohibited system, action, source modification, external access, real-data path, delivery, or submission boundary was attempted or crossed. |
| `CONTROLLED_UAT_BLOCKED_CLEANUP` | Task-owned resource cleanup or independent cleanup verification is incomplete. |

A passed result is internal technical evidence only. It does not imply external acceptance, an official BIR presentation decision, submission approval, Production authorization, or approval of an excluded scenario.

## 14. BIR Submission Boundary

**The Annex E-1 v1.7 workbooks and derived artifacts are synthetic internal Controlled UAT evidence. They are not approved for BIR submission.**

The following remain outside this authorization:

- official submission filename and workbook presentation;
- official signature, certification, footer, and page-numbering requirements;
- official print sequence or delivery format;
- signing, encryption, acceptance, external delivery, and submission;
- BIR or regulatory retention and legal-hold requirements;
- Production data, taxpayer filing, or any representation to BIR.

## 15. Next Authorized Task

Because the decision is `AUTHORIZED_FOR_ANNEX_E1_V1_7_CONTROLLED_UAT_EXECUTION`, the next bounded task is:

**Execute Annex E-1 v1.7 Controlled UAT under the approved authorization and produce the governed evidence package.**

That task must use a separate branch and worktree, bind its execution commit to this authorization after merge, assign the required principals, and satisfy every precondition before running a UAT command. No execution is authorized by implication outside that exact task.

## 16. Authoritative References

- [Annex E README](README.md)
- [v1.7 Dataset Specification](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.7.md)
- [v1.7 Package Manifest](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Manifest_v1.7.md)
- [v1.7 Canonical Source Row And Hash Contract](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.7.md)
- [v1.7 Canonical Source Row Population](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_Population_v1.7.md)
- [v1.7 Expected Value Matrix](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.7.md)
- [v1.7 Scenario Mapping](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.7.md)
- [v1.7 Source Population And Reconciliation Matrix](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.7.md)
- [v1.7 External Decision Matrix](ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.7.md)
- [v1.7 Dataset README](dataset/v1.7/README.md)
- [Original Internal Workbook Presentation Review](ExitPass_POS_Server_Annex_E1_v1.7_Internal_Workbook_Presentation_and_Controlled_UAT_Readiness_Review_v1.0.md)
- [Internal Workbook Presentation Revalidation](ExitPass_POS_Server_Annex_E1_v1.7_Internal_Workbook_Presentation_and_Controlled_UAT_Readiness_Revalidation_v1.0.md)
- [Z-012D Preparation Plan](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Preparation_Plan_v1.0.md)
- [Z-012D Environment Readiness Checklist](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Environment_Readiness_Checklist_v1.0.md)
- [Z-012D Authority And Responsibility Matrix](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Authority_and_Responsibility_Matrix_v1.0.md)
- [Z-012D Evidence Manifest Template](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Evidence_Manifest_Template_v1.0.md)
- [Z-012D Rollback And Cleanup Checklist](ExitPass_POS_Server_Z012D_Annex_E1_Controlled_UAT_Rollback_and_Cleanup_Checklist_v1.0.md)
