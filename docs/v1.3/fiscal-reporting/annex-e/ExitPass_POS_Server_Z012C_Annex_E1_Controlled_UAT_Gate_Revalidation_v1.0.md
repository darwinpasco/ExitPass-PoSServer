# ExitPass POS Server Z-012C Annex E-1 Controlled UAT Gate Revalidation v1.0

## 1. Purpose and verdict

This record independently reviews the merged Z-012B local bounded Annex E-1 runtime and separates preparation, execution, delivery, submission, and Production gates. Review date: 2026-08-10 PHT.

| Gate | Decision |
|---|---|
| Z-012B post-merge runtime acceptance | `PASSED` |
| Controlled UAT preparation | `AUTHORIZED_FOR_Z012D_CONTROLLED_UAT_PREPARATION` |
| Controlled UAT data assignment | `BLOCKED_PENDING_APPROVED_UAT_PLAN_AND_EXTERNAL_CONFIRMATIONS` |
| Controlled UAT execution | `BLOCKED_PENDING_EXTERNAL_CONFIRMATIONS` |
| External workbook delivery | `BLOCKED_PENDING_EXTERNAL_CONFIRMATIONS` |
| External BIR submission | `BLOCKED_PENDING_EXTERNAL_CONFIRMATIONS` |
| Production rollout | `BLOCKED_PENDING_EXTERNAL_CONFIRMATIONS_AND_PRODUCTION_CONTROLS` |

Preparation means drafting the controlled plan, synthetic scenario catalogue, evidence manifest, authority matrix, environment checklist, rollback/cleanup checklist, and external-question tracker. It does not permit assigning real data, creating Production fixtures, executing UAT, or delivering an artifact.

## 2. Baseline and immutable authorities

| Evidence | Verified value |
|---|---|
| `origin/dev`, HEAD, and merge base | `d0d2c139bdb9de1fb44ab13786c6ef39afc79172` |
| Divergence at review start | 0 ahead / 0 behind |
| Z-012B merge | merge commit `d0d2c139bdb9de1fb44ab13786c6ef39afc79172`, second parent `cc7668c7154703481d5c0c1ae059db6dffd48623` |
| Approved profile | `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md`, v1.0 |
| Approved profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Accounting approval | `Z-012B-ACCOUNTING-APPROVAL-001`, `APPROVED_EXACTLY_AS_SPECIFIED` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Official local workbook SHA-256 | `7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197` |
| Official structural evidence | workbook contains sheet `E-1`; sheet-1 dimension is `A1:AF16` |

The approved calculation profile was not modified. AE-DR-006 through AE-DR-009 remain resolved exactly by the Accounting approval. Controlled UAT remained unauthorized when Z-012B merged.

## 3. Complete merged Z-012B inventory

The integration changed 39 paths: 24 additions and 15 modifications. The authoritative inventory is the first-parent diff of merge commit `d0d2c139...` and includes the API contract; five controlled-code families and generated SQL; three new canonical Annex E-1 tables plus the hardened `pos.annex_e_reports`; rebuild, inventory, and drift controls; runtime models, calculation, hashing, deterministic renderer, service, PostgreSQL repository, and artifact store; API authorization/endpoints/registration; focused runtime/API/live-PostgreSQL tests; and governed documentation. No path outside that bounded slice was introduced except the two pre-existing test files reviewed in section 8.

```text
A contracts/pos-server/annex-e1-api.v1.json
M db/rebuild/pos_sql_apply_order.txt
A db/reference-data/controlled-codes/generated/sql/009_controlled_codes_annex_e1.sql
M db/reference-data/controlled-codes/source/controlled_code_source_index.json
A db/reference-data/controlled-codes/source/families/annex_e1_accounting_fact_status.json
A db/reference-data/controlled-codes/source/families/annex_e1_accounting_fact_type.json
A db/reference-data/controlled-codes/source/families/annex_e1_correction_reason.json
A db/reference-data/controlled-codes/source/families/annex_e1_remarks.json
A db/reference-data/controlled-codes/source/families/annex_e1_workbook_status.json
A db/state/tables/pos.annex_e1_period_accounting_facts.sql
A db/state/tables/pos.annex_e1_report_fact_sources.sql
A db/state/tables/pos.annex_e1_workbooks.sql
M db/state/tables/pos.annex_e_reports.sql
M db/validation/pos_expected_inventory.json
M db/validation/pos_prohibited_patterns.json
M docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_BIR_Annex_E_Calculation_and_Reconciliation_Rules_v1.0.md
M docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_BIR_Annex_E_Source_to_Output_Mapping_Matrix_v1.0.md
M docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_BIR_Annex_E_Validation_and_Acceptance_Scenarios_v1.0.md
M docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Z009_Annex_E_Runtime_Authorization_Checklist_v1.0.md
M docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Z009_Annex_E_Runtime_Implementation_Handoff_v1.0.md
A docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Z012B_Annex_E1_Deterministic_Runtime_Implementation_v1.0.md
M docs/v1.3/fiscal-reporting/annex-e/README.md
M src/ExitPass.PosServer.Api/FiscalDocuments/FiscalDocumentServiceCollectionExtensions.cs
A src/ExitPass.PosServer.Api/FiscalReports/AnnexE1Authorization.cs
A src/ExitPass.PosServer.Api/FiscalReports/AnnexE1Endpoint.cs
A src/ExitPass.PosServer.Api/FiscalReports/AnnexE1EndpointRouteBuilderExtensions.cs
A src/ExitPass.PosServer.Api/FiscalReports/UnavailableAnnexE1Repository.cs
M src/ExitPass.PosServer.Api/Program.cs
A src/ExitPass.PosServer.Persistence.Postgres/FiscalReports/FileSystemAnnexE1ArtifactStore.cs
A src/ExitPass.PosServer.Persistence.Postgres/FiscalReports/PostgresAnnexE1Repository.cs
A src/ExitPass.PosServer.Runtime/FiscalReports/AnnexE1CalculationEngine.cs
A src/ExitPass.PosServer.Runtime/FiscalReports/AnnexE1DeterministicXlsxRenderer.cs
A src/ExitPass.PosServer.Runtime/FiscalReports/AnnexE1Models.cs
A src/ExitPass.PosServer.Runtime/FiscalReports/AnnexE1SemanticHasher.cs
A src/ExitPass.PosServer.Runtime/FiscalReports/AnnexE1Service.cs
M tests/ExitPass.PosServer.Api.IntegrationTests/BirSalesSummaryPostgresIntegrationTests.cs
A tests/ExitPass.PosServer.Api.Tests/FiscalReports/AnnexE1ApiTests.cs
M tests/ExitPass.PosServer.Runtime.Tests/DigitalSalesInvoiceRenderServiceTests.cs
A tests/ExitPass.PosServer.Runtime.Tests/FiscalReports/AnnexE1RuntimeTests.cs
```

## 4. API and authorization acceptance

| Operation | Route | Policy | Permission |
|---|---|---|---|
| Record a nonzero approved Accounting fact | `POST /v1/fiscal-reports/annex-e/e1/accounting-facts` | `AnnexE1AccountingFactRecord` | `fiscal_annex_e.accounting_fact.record` |
| Record governed zero evidence | `POST /v1/fiscal-reports/annex-e/e1/known-zero-attestations` | `AnnexE1KnownZeroAttest` | `fiscal_annex_e.known_zero.attest` |
| Generate or replay workbook | `POST /v1/fiscal-reports/annex-e/e1/workbooks` | `AnnexE1Generate` | `fiscal_annex_e.generate` |
| Read metadata | `GET /v1/fiscal-reports/annex-e/e1/workbooks/{id}` | `AnnexE1Read` | `fiscal_annex_e.read` |
| Download immutable bytes | `GET /v1/fiscal-reports/annex-e/e1/workbooks/{id}/content` | `AnnexE1Download` | `fiscal_annex_e.export` |
| Authorize correction in generation request | same generation route plus endpoint check | `AnnexE1Correct` | `fiscal_annex_e.correct` |

All policies require authentication and their exact permission claim. Generation of a correction requires both the generation permission and an explicit correction permission check. The API derives actor/service authority from authenticated claims, rejects fixture/development authority under Production hosting, enforces exact Site POS Server, fiscal identity, and currency claims, requires PHP for generation, and uses 404 anti-enumeration behavior for inaccessible report scope.

## 5. Source, calculation, and fact acceptance

The repository requires a CLOSED period, committed Z request, and committed Z-bound BIR Sales Summary before a period fact can be recorded. Monthly generation selects the exact Site POS Server, fiscal identity, currency, year, and month; orders rows by period sequence; and rejects a configured period without a CLOSED state, committed Z, or BIR Sales Summary.

Z-010 BIR Sales Summary remains the financial authority. Electronic Journal data is not substituted for financial facts. Seven immutable period facts are required per row: Manual SI/OR net income, accumulated-sales-capacity overflow net income, NAAC discount, Solo Parent discount, other VAT adjustment, VAT on returns, and residual VAT adjustment. The five unresolved privilege/VAT classifications are accepted only as explicit `attested_zero`; absence, unsupported state, and unknown values are not zero.

The checked `long` PHP-minor-unit implementation preserves the approved equations:

```text
D07 = ACTIVE_GROSS + RETURN_AMOUNT + VOID_AMOUNT
D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
D25 = D20 + D21 + D22 + D23 + D24
D26 = D09 - D22
D27 = D07 - D19 - D09
D29 = D27 + D06 + D28
```

R01 through R07 require exactly zero minor-unit difference. Overflow, negative governed results, missing facts, unclassified source facts, unexplained ranges/gaps, or nonzero unresolved privileges fail closed. A no-activity row additionally requires all governed source amounts and counts to be zero, equal beginning/ending GTA, a blank fiscal range, and complete explicit zero evidence.

## 6. Determinism, replay, correction, and persistence

The semantic identity binds the governed scope, month, profile, approved calculation-profile hash, official template hash, renderer version, immutable header values, ordered Z and BIR Summary identities/hashes, ordered fact identities/hashes, and correction lineage. Operation key, actor, correlation, and generation clock do not alter source semantics.

The renderer writes a fixed ten-entry Open XML package with fixed ZIP timestamps/order, normalized package metadata, deterministic relationships/XML/shared text behavior, invariant formatting, and no volatile formula or locale dependency. Exact replay returns the existing metadata and downloads the originally stored bytes rather than rendering again.

A correction uses `POST /v1/fiscal-reports/annex-e/e1/workbooks` with a new `operationKey` and all three fields: `supersedesWorkbookId`, controlled `correctionReason`, and `correctionApprovalReference`. The caller must also hold `fiscal_annex_e.correct`. The referenced workbook must be the current workbook in the exact scope. A new immutable revision is created; the previous workbook remains immutable and readable; one child may supersede each predecessor; and changed semantics without explicit correction return conflict.

Artifact publication occurs inside the open database transaction but before workbook metadata insertion and commit: deterministic bytes are hashed, written to a same-filesystem temporary file with write-through/flush, atomically moved to a SHA-256-derived final key, reread and verified, then referenced by immutable metadata and rows committed together. If publication fails, no metadata commits. If database work fails after publication, the content-addressed file is an unreferenced orphan: no committed workbook ID or storage key can be read through the API, the artifact root is not publicly exposed, and download always resolves a key from committed metadata before reading bytes. Z-012B intentionally does not implement destructive orphan purge; a future cleanup process must prove non-reference before deletion.

Download recomputes SHA-256 and checks byte length. Missing or tampered bytes fail closed. Advisory transaction locks and unique constraints serialize identical/conflicting generation; durable lookup reconciles unique/unknown outcomes; immutable triggers prohibit update/delete of facts, workbooks, rows, and source links.

## 7. Remaining external-decision stage matrix

`Preparation` below means bounded local preparation only. `Execution` means Controlled UAT execution.

| ID | Authority | Known-zero/local bound | Preparation | Execution | External delivery/submission | Production | Runtime safe posture |
|---|---|---|---|---|---|---|---|
| AE-DR-002 | BIR/examiner | Not applicable | Does not block | Blocks | Blocks | Blocks | Internal XLSX only; no acceptance claim |
| AE-DR-004 | BIR/examiner | Not applicable | Does not block | Blocks | Blocks | Blocks | Internal deterministic filename labeled non-prescriptive |
| AE-DR-010 | BIR/examiner | `NONE`/`NO_ACTIVITY` only | Does not block | Blocks | Blocks | Blocks | Controlled codes only; no free text |
| AE-DR-011A | BIR/examiner | Explicit scoped zero evidence is safe for preparation | Does not block | Blocks until zero/blank acceptance | Does not independently block transport | Blocks | Unknown/nonzero unsupported value fails closed |
| AE-DR-012 | Accounting/BIR | Explicit zero evidence permits a bounded zero-only test plan | Does not block | Blocks nonzero privilege scenarios, not zero-only preparation | Does not independently block transport | Blocks nonzero path | Every nonzero unresolved privilege fact fails closed |
| AE-DR-016 | BIR/examiner | Not applicable | Does not block | Does not block strictly local preparation; blocks any UAT transfer/submission | Blocks | Blocks external workflow | Local authorized download only |
| AE-DR-016B | Legal/Compliance/Records | Not applicable | Does not block | Does not block non-destructive UAT | Does not independently define transport | Blocks | No purge or destructive archival |
| AE-DR-019 | Accounting/examiner | Not applicable | Does not block | Blocks | Blocks official acceptance | Blocks | Internal invariant PHP/date profile only |
| AE-DR-020A | BIR/examiner | Not applicable | Does not block | Blocks | Blocks official acceptance | Blocks | H08 uses immutable Site POS Server code; no channel concatenation |
| AE-DR-024 | BIR/examiner | Not applicable | Does not block | Blocks | Blocks | Blocks | Official geometry preserved; unrepresentable values fail |

None of the ten decisions is related to Annex E-2 through E-5 or ARTS POSLog authorization; those remain separately deferred. AE-DR-016B is a Production-only legal/records gate. AE-DR-012 does not block preparation of an explicitly zero-only scenario set, but it remains unresolved and blocks every nonzero privilege scenario.

## 8. Pre-existing test-file review

The BIR Sales Summary integration test change is necessary and isolated: it reuses the existing real PostgreSQL/API host after creating the governing committed Z and BIR Summary, then exercises the complete Annex E-1 API flow, concurrency, replay, correction, immutable original bytes, tamper/missing-artifact denial, scope denial, permission separation, and Production fixture rejection. Existing BIR Summary assertions remain intact.

The Digital Sales Invoice test change is necessary and isolated: the prior assembly-wide prohibition on any type name containing `Annex` would falsely reject the now-authorized Annex E runtime in the shared runtime assembly. The test still prohibits payment, gate, exit, refund, and reversal authority and does not weaken Digital Sales Invoice behavior assertions.

## 9. Acceptance and residual risks

No material contradiction with the approved calculation profile, integrity model, security boundary, or exact fiscal scope was found. The merged runtime is accepted for its local bounded purpose. Automated/disposable PostgreSQL evidence is not Controlled UAT evidence.

Residual implementation follow-ups that do not block this review:

1. A future retention task must govern physical archive, legal hold, and any deletion or orphan-cleanup process after AE-DR-016B.
2. Nonzero Diplomat/other VAT privilege support requires AE-DR-012 and a separately authorized runtime change.
3. External signing, encryption, compression, transport, and submission require AE-DR-016 and separate implementation authorization.

## 10. Next authorized activity

The final Z-012C decision is `AUTHORIZED_FOR_Z012D_CONTROLLED_UAT_PREPARATION`. The next bounded activity is preparation of a Controlled UAT plan and synthetic-data/evidence specification that references the ten closure requests. It may not assign actual UAT data or execute requests until a separate authorization record closes the execution gates. External delivery, BIR submission, Production rollout, Annex E-2 through E-5, and ARTS POSLog remain unauthorized.
