# ExitPass POS Server v1.3 Implementation Baseline Audit and Cross-Lane Gap Analysis v1.0

## 1. Executive Verdict

Verdict: HEALTHY_WITH_GAPS.

POS Server `origin/dev` is a credible implemented baseline, not merely a documentation repository. The current branch was audited at HEAD `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1`, which is also the exact `origin/dev` commit incorporated during preflight. Restore, Release build, all current POS Server test projects, static database validation, Docker-backed rebuild, inventory, drift validation, and controlled-code load validation passed during this audit.

The baseline is not production-authorized. The remaining blockers are cross-lane contract and UAT readiness gaps: Central PMS must supply final statutory and terminal-cash fiscal facts, Management Platform must reach POS Server through a backend proxy, APT statutory cash must wait for a statutory-aware Central PMS readiness facade, and controlled end-to-end payment-to-fiscal UAT evidence is not yet present. POS Server also has database-object naming findings where some constraint names exceed PostgreSQL's 63-byte identifier limit, although current database validation reports the static checks as passed.

Recommended first POS Server task: `feature/fiscal-issuance-applied-statutory-facts-contract`.

Purpose of that task: harden the Central PMS to POS Server fiscal issuance contract for applied statutory privilege facts and terminal-cash fiscal context, preserving POS Server authority and privacy boundaries before WebPay/APT statutory UAT.

## 2. Repository and Commit Baseline

| Item | Value |
| --- | --- |
| Codex persona | Codex Z |
| POS Server worktree | `D:\SourceCodes\ExitPass-PoSServer-Z-Audit` |
| Branch | `docs/pos-server-v13-implementation-and-cross-lane-gap-audit` |
| Base branch | `origin/dev` |
| HEAD | `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1` |
| origin/dev | `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1` |
| origin/master | `7d54a3be4699b80f69ebcc031fca7467c8252475` |
| origin/HEAD | `origin/dev` |
| Remote branches | `origin/dev`, `origin/master` |
| Tags | `posserver-controlled-codes-v1.3.0` through `posserver-controlled-codes-v1.3.4`, `posserver-fiscal-document-v1.3.0` |

Preflight result:

- Branch matched the requested audit branch.
- Worktree was clean before documentation edits.
- `git fetch origin --prune` completed after elevated read/write access to linked worktree metadata.
- HEAD and `origin/dev` were identical after fetch.
- `git diff --check` passed before edits.
- No reset, clean, stash, stage, commit, push, merge, or rebase was performed.

Stable branch posture: `origin/master` is behind `origin/dev` by the POS Server implementation stream from PR #1 through PR #80. No commits exist on `origin/master` that are absent from `origin/dev`.

## 3. Repositories Inspected

| Repository | Path | Branch | Commit | Status observed |
| --- | --- | --- | --- | --- |
| POS Server | `D:\SourceCodes\ExitPass-PoSServer-Z-Audit` | `docs/pos-server-v13-implementation-and-cross-lane-gap-audit` | `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1` | Clean at preflight |
| ExitPass | `D:\SourceCodes\ExitPass` | `feature/webpay-statutory-discount-local-walkthrough` | `d0a9d948ce7a6afb8b3c41c411fad8fef80c530c` | Untracked WebPay walkthrough docs/scripts |
| ExitPass-Discounts | `D:\SourceCodes\ExitPass-Discounts` | `dev` | `d291dfe0cac1f726e36700da788ae823bdf32aff` | Read-only evidence inspected |
| Management Platform | `D:\SourceCodes\ExitPass-ManagementPlatform` | `develop` | `488771f51eb358e384f94dcedd1209fd3d775519` | Clean |
| Assisted Payment Terminal | `D:\SourceCodes\ExitPass-AssistedPaymentTerminal` | `feature/apt-encrypted-database-key-envelope` | `c7b2e663126e66599169586fa9298e3b65d0a8b7` | Active encrypted database/cash blocking work present |
| APT/Central PMS contract worktree | `D:\SourceCodes\ExitPass-APT` | `dev` | `a7b259ff3f9e566e7fe5b8d1da7876d6f58df907` | Clean |
| WebPay local harness | `D:\SourceCodes\ExitPass-G-LocalHarness` | `feature/webpay-local-integration-harness-baseline` | `19315cb90442732c13d466bf7897ae59b6df2eea` | Behind `origin/dev` by 2, untracked harness docs/scripts |
| WebPay statutory service auth | `D:\SourceCodes\ExitPass-G-StatutoryServiceAuth` | `feature/webpay-statutory-service-auth-safe-errors` | `fb445d5afa79ecb83fb45965afe12f2a45df2bc0` | Active modified/untracked G-003 work |
| Canonical database | `D:\SourceCodes\exitpassdb_v1.2` | `develop` | `7a785fd93d592b019fbb6ac6bbdf4fc82d8485dc` | Clean |

The retired database repository `D:\SourceCodes\ExitPass_DBv1.2` was not used.

## 4. Complete Completed-Task Inventory

Inventory count: 80 POS Server merge slices were identified on `origin/dev`. They fall into six implementation groups: design and authority baseline, physical database foundation, controlled-code governance, fiscal document runtime persistence/API, numbering/idempotency/runtime presentation, and Sales Invoice profile administration.

| Slice | Branch or PR evidence | Merge commit | Implementation area | Main files or modules | Tests and validation | Status | Caveats or supersession |
| --- | --- | --- | --- | --- | --- | --- | --- |
| POS Server database design baseline | PR #1, #2 | `6a54322`, `b0153a3` | BRD/database design documentation | `docs/v1.3/pos-server-db/*` | Review/approval docs | Complete | Some early design text is superseded by SQL/runtime implementation |
| Physical database artifact planning/gate | PR #3 to #7 | `6265b9a` to `41b645e` | DB artifact plan and gate resolution | `docs/v1.3/pos-server-db-physical/*` | Review/approval docs | Complete | Open questions remain for final production format decisions |
| DB folder and naming standards | PR #8 to #10 | `7347423` to `1d83b2b` | Repository DB skeleton and naming | `db/*`, naming docs | Documentation checks | Complete | Static validation now finds 7 long constraint identifiers |
| Physical object design planning and approval | PR #11 to #18 | `99c4c19` to `8914ad5` | Object design | physical object docs | Review/approval docs | Complete | Superseded by state SQL where implemented |
| First POS schema slice | `db/first-slice-foundation-identity-channel`, PR #19 | `d2fa712` | `pos` schema foundation | `db/state/*.sql` | SQL review | Complete | Foundation has expanded substantially since this slice |
| DB validation/rebuild tooling | PR #20, #29, #30 | `f10ad81`, `c832321`, `d4605ec` | Static/rebuild/inventory/drift/Docker validation | `db/scripts/*`, validation docs | Static and Docker-backed proof | Complete | Inventory compares tables and reports selected object classes; column-level drift remains limited |
| Fiscal document core DB objects | PR #21 | `af6620a` | Header/status history | `db/state/*fiscal_documents*`, status SQL | Rebuild/inventory validation | Complete | Production UAT still requires cross-lane issuance proof |
| Fiscal detail DB objects | PR #22 | `954f913` | Lines, tenders, taxes, discounts, totals | `db/state/*lines*`, `*tenders*`, `*tax*`, `*discount*`, `*totals*` | Rebuild/inventory validation | Complete | Statutory facts need more explicit cross-lane DTO posture |
| Numbering/counter/idempotency DB objects | PR #23 | `cdc11d6` | Sequence states, counters, idempotency | `db/state/*sequence*`, `*counter*`, `idempotency_records.sql` | Rebuild/inventory validation | Complete | Production sequence policy still requires configured effective site data |
| Digital SI/reprint/adjustment DB objects | PR #24 | `3c74297` | Digital SI URLs, reprint, adjustment state | `db/state/digital_si*`, `reprint*`, `fiscal_adjust*` | Rebuild/inventory validation | Complete | Runtime printing/reprint behavior remains out of scope |
| Fiscal report posture DB objects | PR #25 | `477bb03` | X/Z/BIR/Annex E report posture | `db/state/*report*` | Rebuild/inventory validation | Complete | Report generation not implemented |
| Export/EJ posture DB objects | PR #26 | `a13604f` | EJ/POSLog/export package posture | `db/state/*export*`, `electronic_journal_records.sql` | Rebuild/inventory validation | Complete | Export generation not implemented |
| Audit/recovery/security references | PR #27 | `d3dd79f` | Audit, recovery, continuity, security refs | `db/state/*audit*`, `*recovery*`, `*security*` | Rebuild/inventory validation | Complete | Broad audit subsystem is not a runtime UI/workflow |
| DB object inventory roadmap | PR #28 | `c76253e` | Inventory proof planning | `docs/v1.3/pos-server-db-physical/*inventory*` | Documentation review | Complete | Superseded by current scripts but still useful |
| Controlled-code source schema and UUID governance | PR #31 to #33 | `e2f0f37`, `00adc24`, `42ddc62` | Source JSON/generator governance | `db/reference-data/controlled-codes/source/*` | Source schema review | Complete | Actual code families are still partial and controlled |
| Controlled-code values and generated SQL slices 1 to 3 | PR #34 to #42 | `d0fd7d1` to `c119b60` | Controlled-code data | source JSON and generated SQL | Load validation per slice | Complete | Tags record approved snapshots |
| Controlled-code validation workflow/CI | PR #43, #44 | `2689976`, `2689976`? latest `e052b7d` | CI/static controlled-code validation | `.github/workflows`, scripts | Controlled-code load validation | Complete | Audit did not change CI |
| Statutory discount POS boundary | `docs/statutory-discount-pos-boundary`, PR #45 | `2e0798a` | Authority documentation | `docs/v1.3/pos-server-runtime/ExitPass_POS_Server_Statutory_Discount_Boundary.md` | Technical review | Complete | Boundary remains correct: POS fiscalizes, does not approve or apply privileges |
| Fiscal document creation skeleton | PR #46 | `455bdbd` | Runtime model/service | `src/ExitPass.PosServer.Runtime/FiscalDocuments/*` | Runtime unit tests | Complete | Later persistence/idempotency extended it |
| Fiscal document API entrypoint | PR #47 | `0078191` | Minimal API POST | `src/ExitPass.PosServer.Api/FiscalDocuments/*` | API tests | Complete | Later status/error hardening superseded early response behavior |
| PostgreSQL persistence wiring | PR #48 | `f844431` | Postgres repository | `src/ExitPass.PosServer.Persistence.Postgres/*` | Persistence tests | Complete | Requires configured DB URL for runtime |
| Status history write | PR #49 | `4c96564` | Status append | repository and DB state | Runtime/persistence tests | Complete | Further lifecycle states remain bounded |
| Fiscal document links | PR #50 | `16fbda8` | Document links | runtime, API, persistence, docs | Focused tests | Complete | Current request supports links |
| Fiscal document lines | PR #51 | `6715cd2` | Line snapshots | runtime, API, persistence, docs | Focused tests | Complete | Current presentation uses lines |
| Fiscal tenders | PR #52 | `cecc538` | Tender snapshots | runtime, API, persistence, docs | Focused tests | Complete | APT cash details need cross-lane context, not denomination storage |
| Fiscal tax details | PR #53 | `f1f51ca` | Tax snapshots | runtime, API, persistence, docs | Focused tests | Complete | Statutory VAT facts need explicit request contract confirmation |
| Fiscal discount privilege details | PR #54 | `3dc5d96` | Discount/VAT privilege snapshots | runtime, API, persistence, docs | Focused tests | Complete | Generic refs exist; applied decision/application refs are not first-class |
| Fiscal totals | PR #55 | `5971afa` | Total snapshots | runtime, API, persistence, docs | Focused tests | Complete | Central PMS remains totals source |
| Persistence readiness review | PR #56 | `7ec6955`? merge `5971afa` sequence includes `79676bb` | Documentation review | docs runtime review | Review | Complete | Superseded by API Postgres smoke |
| API PostgreSQL smoke | PR #57 | `0b3e5bd` | Integration proof | `tests/ExitPass.PosServer.Api.IntegrationTests/*` | 6 integration tests | Complete | Uses disposable controlled-code fixture rows |
| Fiscal document read endpoint | PR #58 | `4e10bfe` | GET read model | API/runtime/persistence reader | API/runtime/persistence tests | Complete | Used by presentation/readback |
| Numbering counter planning/schema/read model | PR #59 to #63 | `6771cfe` to `98eae30` | Numbering design and read model | docs, DB columns, runtime read model | Tests | Complete | Runtime allocation later superseded planning-only gap |
| Fiscal number allocation design/runtime | PR #64, #68 | `9f782b1`, `a027cf5` | Runtime sequence allocation | `FiscalSequence*`, repository | Focused tests | Complete | Must be proven in cross-lane UAT |
| Fiscal issuance idempotency | PR #65, #66 | `f5aca08`, `eeecd0a` | Idempotency records and replay/conflict | `FiscalIssuanceIdempotency*`, repository, tests | Runtime/persistence/API tests | Complete | Semantic contract remains `sha256:v1` |
| Fiscal identity and sequence policy resolution | PR #67 | `9510de2` | Runtime context resolution | fiscal context services/repository | Focused tests | Complete | Later Sales Invoice header profile adds profile resolution |
| Response status hardening | PR #69 | `af77341` | Safe response status/error posture | API endpoint/service result models | API/runtime tests | Complete | Still no broad OpenAPI export found |
| Numbering/idempotency runtime foundation | PR #70 | `7e2773c` | Combined runtime foundation | runtime/persistence/tests/docs | Focused proof | Complete | Supersedes planning tasks |
| Semantic hash parity fixture | PR #71 | `e627ea5` | `sha256:v1` fixture | `FiscalDocumentSemanticRequestHasher.cs`, fixture, tests | Runtime parity tests | Complete | Current hash excludes server-resolved profile facts |
| Digital Sales Invoice rendering foundation | PR #72 | `fa2bbf8` | Render model/service | `DigitalSalesInvoiceRender*` | Runtime tests | Complete | Early render has placeholder footer posture for absent data |
| Digital Sales Invoice template contract | PR #73 | `ea29631` | Template versioning | `DigitalSalesInvoiceTemplateContract.cs`, fixtures | Runtime tests | Complete | Governed version: `digital-sales-invoice-json-v1` |
| Digital Sales Invoice presentation adapter | PR #74 | `bb5c880` | Presentation DTO/model | `DigitalSalesInvoicePresentation*` | Runtime tests | Complete | Authoritative presentation route later governed |
| Digital SI print preview consumer fixture | PR #75 | `610d8ac` | UI consumer fixture | fixtures/docs | Fixture tests | Complete | Consumer fixture, not runtime printing |
| Digital SI preview approval record | PR #76 | `2b12f4e` | Controlled approval doc | docs | Review record | Complete | Documentation-only |
| Fiscal document void endpoint | PR #77 | `c0fa9c5` | Void API/runtime/idempotency | void endpoint/service/hasher/tests | Runtime/API tests | Complete | Void command is separate from fiscal issuance |
| Fiscal document presentation contract | PR #78 | `5d9addf` | Read-only authoritative presentation | `/digital-sales-invoice/presentation`, contract, fixture, proof | API/runtime tests and proof script | Complete | `contracts/pos-server/fiscal-document-presentation.v1.json` still records feature-branch commit `c0fa9c5`, not current `origin/dev` |
| Fiscal identity and Sales Invoice header profiles | PR #79 | `0b8cc14` | Profile persistence/resolution/snapshot | profile models/service/repository, DB snapshot table, fixture, proof | Runtime/persistence/presentation/semantic tests | Complete | Enforcement default is compatible/non-breaking |
| Sales Invoice profile admin API | PR #80 | `46ddd68` | Secured admin API | admin endpoints/auth/service/contract/proof | API/runtime/persistence/semantic tests | Complete | Management Platform backend proxy not implemented in POS repo |

Current completed-task count by category:

- Documentation/planning/review slices: 31.
- Database schema/data/tooling slices: 24.
- Runtime/API/persistence/test slices: 25.
- Total merge slices on `origin/dev`: 80.

## 5. Current Architecture Summary

POS Server owns fiscal document creation, status, numbering, persistence, fiscal line/tax/discount/tender/total snapshots, idempotency, semantic request hashing, Digital Sales Invoice rendering, presentation readback, void posture, fiscal identity/header profile resolution, immutable header snapshots, and the secured profile administration API.

POS Server does not own parking tariff calculation, statutory eligibility or application, payment provider finality, gate authorization, evidence images, or Operator Console reviewer impersonation.

The current fiscal request contract supports:

- Site POS Server reference and IDs.
- Optional Site ID and channel terminal ID.
- Optional runtime terminal reference.
- business day.
- Central PMS parking session, payment attempt, payment confirmation, finality, and vendor acknowledgement refs.
- payable basis, currency, final payable amount, upstream finality.
- discount references with status and statutory-treatment posture.
- line, tender, tax, discount privilege, and total snapshots.
- generic reference/context dictionaries.

The semantic hash contract is `sha256:v1`. It hashes governed client-submitted fiscal request semantics and does not include the server-resolved Sales Invoice header profile, fiscal identity, BIR/PTU fields, or header snapshot facts.

## 6. Current Database Summary

Schema: `pos`.

Current database state:

- 54 SQL files in `db/state`.
- 53 expected tables, 53 actual tables in Docker-backed inventory validation.
- 0 missing tables.
- 0 unexpected tables.
- 0 functions, triggers, non-default extensions, or sequences in `pos` schema inventory.
- Controlled-code inventory: 19 code sets and 101 code values.

Object families present:

- Site POS Server and fiscal identity boundary.
- Sales Invoice header profiles.
- Channel terminals and capability/status history.
- Fiscal document header, header snapshot, status history, links, lines, tenders, taxes, discount/privilege details, totals.
- Fiscal sequence policies, sequence states, gap audit, counter states, state snapshots, lock states.
- Idempotency, operation retry, exception state.
- Digital SI URL/access events, reprint requests/output refs.
- Fiscal adjustments and status history.
- Report requests/scopes, X/Z, BIR sales summary, Annex E, report output refs.
- Export schema profiles, electronic journal records, export requests/packages/items, validation results.
- Action, privileged-action, configuration, and access audit.
- Recovery requests, continuity checks, anchor refs, security reference contexts.

Database caveats:

- Static validation reports seven identifier length findings for constraint names above 63 bytes. The static command still returned passed. PostgreSQL truncates long identifiers, so this should be treated as a naming/tooling gap before production authorization.
- The inventory script compares schema/table presence and reports functions/triggers/sequences/extensions. It does not yet prove column, index, and constraint drift exhaustively against the canonical database repository.
- POS Server database is separate from the canonical ExitPass database repository. No direct drift mismatch against `D:\SourceCodes\exitpassdb_v1.2` was found because POS Server owns its own `pos` schema artifacts, but cross-repository operational contracts are still required.

## 7. Current API Summary

Current API endpoints observed in POS Server:

| Endpoint | Status | Authority posture |
| --- | --- | --- |
| `POST /v1/fiscal-documents` | Implemented | Fiscal document creation after Central PMS finality facts |
| `GET /v1/fiscal-documents/{fiscalDocumentId}` | Implemented | Authoritative fiscal document readback |
| `POST /v1/fiscal-documents/{fiscalDocumentId}/void` | Implemented | Fiscal void command with idempotency |
| `GET /v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice` | Implemented | Digital Sales Invoice render/read model |
| `GET /v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation` | Implemented | Selected authoritative receipt/Sales Invoice presentation endpoint |
| `POST /v1/admin/fiscal-identities` | Implemented | Secured fiscal identity create |
| `GET /v1/admin/fiscal-identities/{fiscalIdentityId}` | Implemented | Secured read |
| `PATCH /v1/admin/fiscal-identities/{fiscalIdentityId}` | Implemented | Secured safe update before governed use |
| `POST /v1/admin/sales-invoice-header-profiles` | Implemented | Secured draft profile creation |
| `GET /v1/admin/sales-invoice-header-profiles/{id}` | Implemented | Secured read |
| `GET /v1/admin/sales-invoice-header-profiles?siteId=...&sitePosServerId=...` | Implemented | Secured scoped listing |
| `PATCH /v1/admin/sales-invoice-header-profiles/{id}` | Implemented | Secured draft-only update |
| `POST /v1/admin/sales-invoice-header-profiles/{id}/validate` | Implemented | Secured completeness validation |
| `POST /v1/admin/sales-invoice-header-profiles/{id}/approve` | Implemented | Secured approval |
| `POST /v1/admin/sales-invoice-header-profiles/{id}/retire` | Implemented | Secured retirement |
| `GET /v1/admin/sales-invoice-header-profiles/effective-readiness` | Implemented | Secured readiness |
| `GET /v1/admin/sales-invoice-header-profiles/{id}/usage` | Implemented | Secured immutable usage visibility |

Admin API security:

- Scheme: `PosServerAdminApiKey`.
- Policy: `SalesInvoiceHeaderProfileAdministration`.
- Required server-derived permission: `sales_invoice_header_profile.admin`.
- Required headers: `X-PosServer-Admin-Key`, `X-Correlation-Id`.
- Optional diagnostics/compatibility header: `X-PosServer-Admin-Permission`.
- The current code resolves permissions from server-side API-key configuration and does not grant admin authority solely from the permission header.
- API keys are compared with `CryptographicOperations.FixedTimeEquals`.

## 8. Current Test Summary

Validation run during this audit:

| Command | Result |
| --- | --- |
| `dotnet restore` | Passed |
| `dotnet build -c Release --no-restore` | Passed, 0 warnings, 0 errors, elapsed `00:00:10.33` |
| `dotnet test -c Release --no-restore --no-build` | Passed |

Test project results:

| Test project | Result |
| --- | --- |
| `ExitPass.PosServer.Runtime.Tests` | 126 passed, 0 failed, 0 skipped, duration 243 ms |
| `ExitPass.PosServer.Api.Tests` | 113 passed, 0 failed, 0 skipped, duration 713 ms |
| `ExitPass.PosServer.Persistence.Postgres.Tests` | 36 passed, 0 failed, 0 skipped, duration 832 ms |
| `ExitPass.PosServer.Api.IntegrationTests` | 6 passed, 0 failed, 0 skipped, duration 533 ms |

Focused proof scripts present:

- `scripts/Invoke-PosServerFiscalDocumentPresentationProof.ps1`.
- `scripts/Invoke-PosServerSalesInvoiceHeaderProfileProof.ps1`.
- `scripts/Invoke-PosServerSalesInvoiceHeaderProfileAdminApiProof.ps1`.

Skipped or disabled tests:

- No skipped tests were reported by the full `dotnet test -c Release --no-restore --no-build` run.

## 9. Current Operational-Readiness Summary

Operationally ready foundations:

- POS Server can be built and tested from source.
- PostgreSQL schema can be statically checked and Docker-applied into a disposable PostgreSQL database.
- Fiscal document creation/read/status/idempotency/numbering/presentation/profile administration have automated coverage.
- The profile enforcement flag preserves development compatibility while allowing production to require a complete effective Sales Invoice header profile.
- Admin API has a dedicated authorization policy and server-derived key-to-permission mapping.

Operational gaps:

- Production deployment is not authorized.
- No controlled end-to-end UAT evidence exists for WebPay or APT payment finality through POS fiscal issuance and authoritative presentation readback.
- Management Platform standalone UI does not directly prove a real Central PMS to POS Server profile-administration proxy.
- POS Server README is stale: it still says future content is expected to include implementation.
- No browser/UI testing is required for this audit, but downstream WebPay/APT/Management Platform tasks require controlled UAT.
- Monitoring/health for fiscal readiness and profile readiness exists as application behavior/contracts but is not proven in a production operations runbook.

## 10. Superseded or Obsolete Work

| Work | Current posture |
| --- | --- |
| README current-content statement | Obsolete. Repo now contains implementation, tests, DB artifacts, contracts, and proof scripts. |
| Early "final DTO pending" API design text | Partially superseded by implemented DTOs and contract artifacts. |
| Early fiscal numbering planning gaps | Superseded by numbering columns, read model, runtime sequence allocation, and idempotency foundation. |
| Early Digital SI presentation planning | Superseded by rendering, template, adapter, presentation contract, and proof script. |
| Early Sales Invoice profile gap | Superseded by profile persistence/resolution/snapshot/admin API. |
| `GET /digital-sales-invoice` as downstream presentation candidate | Superseded for downstream receipt presentation by `GET /digital-sales-invoice/presentation`. |
| Static naming assumption that all identifiers fit PostgreSQL limits | Contradicted by current static identifier findings. |

## 11. Codex G Gap Analysis

### G-001 WebPay Local Integration Walkthrough Harness

Evidence inspected: WebPay local harness branch `feature/webpay-local-integration-harness-baseline`, commit `19315cb90442732c13d466bf7897ae59b6df2eea`.

Current G-001 scope is ordinary WebPay through Payment Orchestrator and Central PMS using disposable local data. It explicitly excludes POS Server fiscal issuance, real Sales Invoice issuance, ExitAuthorization, gates, entitlement/discount, and APT.

POS Server implications:

- A real payment-to-fiscal walkthrough is still missing.
- The walkthrough needs disposable POS fiscal configuration, Site POS Server identity, complete Sales Invoice header profile, sequence policy, controlled-code IDs, and fiscal request fixture.
- It must prove fiscal status readback, authoritative Sales Invoice presentation, idempotent replay, semantic conflict, correlation evidence, safe customer error mapping, and non-statutory baseline fiscal issuance.
- Ownership is shared: Codex G owns WebPay harness and customer-facing safe errors; Codex Z owns POS Server fiscal contract/readback/proof; Central PMS owns finality-to-fiscal orchestration.

G verdict: POS Server is ready to be integrated, but WebPay controlled fiscal UAT is blocked until a cross-lane fiscal issuance walkthrough exists.

### G-003 WebPay Statutory Service Authentication and Safe Error Mapping

Evidence inspected: WebPay statutory service auth branch `feature/webpay-statutory-service-auth-safe-errors`, commit `fb445d5afa79ecb83fb45965afe12f2a45df2bc0`.

Current G-003 posture:

- Browser calls Payment Orchestrator WebPay statutory routes.
- Payment Orchestrator calls Central PMS with server-side service identity and permission headers.
- Browser never supplies Central PMS service credentials or reviewer/operator authority.
- Safe browser errors map upstream authorization/config/unavailable/internal errors to browser-safe codes.

POS Server implications:

- POS Server must preserve safe fiscal errors for consumers through Central PMS/WebPay.
- POS Server must not expose admin credentials, raw exception details, database details, or fiscal configuration internals to browser surfaces.
- WebPay Sales Invoice retrieval must use backend delegation to the authoritative POS Server presentation endpoint, not local reconstruction.
- Correlation propagation must remain visible end-to-end.

G verdict: service-auth direction is aligned. POS Server work remains a downstream fiscal contract/readback and safe error surface task, not a WebPay browser task.

### WebPay Statutory Architecture

Evidence inspected: WebPay statutory integration impact analysis in `ExitPass-Discounts`.

Gaps:

| Gap | Owner |
| --- | --- |
| Payment intent must wait for Central PMS applied payable-basis readiness | Codex G + Central PMS |
| Approved privilege application at payment time | Central PMS |
| Final applied payable basis before fiscal issuance | Central PMS |
| Ordinary payment while review is pending | Codex G + Central PMS UX/workflow |
| Late approval after ordinary payment with no retroactive adjustment | Central PMS + Codex G |
| Payment/approval race behavior | Central PMS + Payment Orchestrator |
| Fiscal audit linkage to statutory decision/application | Codex Z + Central PMS shared contract |
| POS receipt retrieval through authoritative presentation | Codex Z + Central PMS/WebPay gateway |

## 12. Codex H Gap Analysis

Evidence inspected: Management Platform repository `develop` at commit `488771f51eb358e384f94dcedd1209fd3d775519`, README, and `contracts/management-platform/sales-invoice-profile-api.v1.json`.

Current Management Platform posture:

- Standalone frontend owns UI only.
- Browser calls Central PMS relative routes and must not call POS Server directly.
- Sales Invoice profile UI routes and tests exist for read/manage/approve/retire/new version.
- Contracts describe Central PMS as the browser-facing API boundary and POS Server as the authoritative fiscal profile lifecycle/readiness/usage source.
- The frontend contract includes a disabled integration posture if the Central PMS to POS Server administration bridge is not available.

Observed gap: no standalone Management Platform evidence proves a real Central PMS backend proxy to POS Server admin API. The UI appears to use local/dev contracts and frontend tests rather than a live POS Server administration API.

Gap separation:

| Gap | Category | Owner |
| --- | --- | --- |
| Browser UI for profile authoring | Frontend gap if not complete | Codex H |
| Central PMS backend routes for Management Platform profile APIs | Central PMS/API gap | Central PMS lane |
| POS Server admin API itself | POS Server API | Codex Z, implemented |
| POS Server admin API security and permission mapping | POS Server security | Codex Z, implemented baseline |
| POS admin key distribution to Central PMS service identity | RBAC/config gap | Shared security + Central PMS + POS Server deployment |
| Fiscal profile source-of-truth propagation | Contract/config gap | Central PMS + POS Server |
| Site/Site POS Server assignment UI-readback | Frontend plus backend integration | Codex H + Central PMS |
| Profile version immutability | POS Server behavior | Codex Z, implemented |
| Management Platform audit logging | Central PMS/backend and UI audit | Central PMS + Codex H |

H verdict: POS Server's admin API foundation is present. The blocking gap is not a new POS Server admin endpoint; it is the Management Platform to Central PMS to POS Server integration boundary and service credential mapping.

## 13. Codex I Gap Analysis

Evidence inspected: statutory discount backend/docs in `ExitPass-Discounts`, APT statutory impact analysis, and POS Server statutory boundary docs.

Operator Console posture:

- Operator Console initiates/reviews eligibility only where authorized.
- Operator Console approval does not apply the privilege to payable amount.
- Central PMS applies approved statutory privilege at payment time and owns final payable basis.
- POS Server receives final fiscal facts only.

POS Server should receive:

- Immutable final fiscal line, tax, discount, total, and tender facts.
- Applied statutory decision reference.
- Applied payable-basis application reference.
- Entitlement classification and benefit type where fiscally required.
- Statutory discount amount.
- VAT-exempt or VAT privilege amount.
- Tax classification/treatment.
- Final authoritative totals.
- Safe ordinance or policy reference where legally required.
- Central PMS parking session/payment/finality references.

POS Server should not receive:

- Evidence images.
- Raw statutory IDs.
- Full entitlement documents.
- Reviewer identity as fiscal authority.
- Reviewer notes.
- Operator Console device/shift details.
- Authority to approve or apply privilege policy.

Current fiscal request support:

- Supports discount references, approval refs, evidence refs, beneficiary refs, discount/VAT privilege amounts, line/tax/tender/total facts, payable basis, and context dictionaries.
- Rejects raw evidence/payment marker style unsafe payload in discount privilege detail tests.
- Does not expose first-class applied statutory decision/application fields; those currently fit only through generic refs/context.

I verdict: the authority boundary is correct, but the POS Server issuance contract should be hardened so statutory decision/application and fiscal tax-treatment facts are governed fields or governed context keys, not ad hoc per-client metadata.

## 14. Codex J Gap Analysis

Evidence inspected: APT contracts and active Assisted Payment Terminal branch `feature/apt-encrypted-database-key-envelope`, commit `c7b2e663126e66599169586fa9298e3b65d0a8b7`.

Merged APT capabilities:

- Terminal cash command baseline.
- Authoritative receipt presentation retrieval through Central PMS delegation to POS Server.
- Sales Invoice display.
- Original/reprint printing and print history.
- Restart recovery.
- Cash readiness and fiscal readiness.
- Payable-basis revalidation.
- Non-statutory CASH_RECEIVED path.

Active work:

- SQLCipher encrypted local database.
- DPAPI key envelope.
- Eager encrypted startup.
- Durable shift/custody.
- Cash blocking on local persistence failure.

POS Server implications:

- POS Server must expose fiscal readiness/readback/presentation and reject duplicate fiscal issuance.
- Terminal-cash fiscal issuance is orchestrated by Central PMS, not direct APT to POS.
- CASH tender must be snapshotted in POS Server fiscal tenders.
- Terminal identity can be snapshotted from runtime context.
- Shift/cashier/custody references may be fiscal evidence if Central PMS submits them as governed request facts.
- Denomination evidence, cash drawer local state, encrypted local DB details, print queue, spool outcome, restart recovery, and local custody state remain APT-local or Central PMS workflow state unless a safe reference is required.

J verdict: APT receipt retrieval aligns with POS Server presentation authority. Statutory cash and terminal-cash fiscal issuance remain blocked until Central PMS readiness/fiscal issuance contracts are statutory-aware and POS Server's fiscal request contract explicitly governs cash/statutory evidence boundaries.

## 15. Authority Matrix

| Fact or action | Vendor PMS | Central PMS | Payment Orchestrator | Operator Console | WebPay | APT | Management Platform | POS Server |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Parking tariff | Owns raw/vendor tariff inputs | Owns payable projection/final basis | No | No | Displays/requests | Displays/requests | No | Consumes final fiscal facts only |
| Statutory eligibility | No | Owns canonical decision/application model | No | Initiates/reviews where authorized | Requests | Requests | Config visibility only | Must not decide |
| Statutory approval | No | Stores canonical decision | No | Owns human review | No | No | No | Must not approve |
| Statutory application | No | Owns payment-time application | No | No | Requests application | Requests application | No | Consumes applied facts |
| Payable basis | No | Owns authoritative payable basis | No | No | Consumes | Consumes | No | Consumes immutable fiscalized basis |
| Provider finality | No | Owns platform finality acceptance | Owns provider execution/outcome evidence | No | Initiates channel payment | Cash local only, then Central PMS | No | Consumes finality refs only |
| Cash receipt | No | Owns platform terminal-cash payment state | No | No | No | Owns local cash custody command/evidence | No | Consumes CASH tender snapshot |
| Fiscal readiness | No | Coordinates readiness for channels | No | Visibility only | Consumes safe status | Consumes safe status | Config readiness | Owns fiscal configuration/readiness facts |
| Fiscal issuance | No | Requests after finality | No | No | No direct | No direct | No | Owns |
| Fiscal number | No | Records reference/readback | No | No | Displays via readback | Displays/prints via readback | No | Owns |
| Sales Invoice presentation | No | Delegates/transports | No | Read-only visibility if authorized | Displays via backend | Displays/prints authoritative payload | Configures profile indirectly | Owns |
| Original/reprint classification | No | Coordinates/request records | No | May request where authorized | No | Owns local print attempt/history | No | Owns fiscal presentation posture, not local spool |
| Evidence image | No | Stores/links under privacy policy | No | May review allowed evidence | Submits safe upload reference | Submits safe upload reference | No | Must not store image |
| Ordinance policy configuration | No | Owns policy application references | No | Visibility/review | No | No | May configure through backend | Consumes safe policy refs only |
| Fiscal profile configuration | No | Future proxy/control plane | No | Visibility only | No | No | Owns admin UI/control plane | Owns runtime validated profile store |
| Receipt printing | No | Coordinates state if needed | No | No | No | Owns local physical print attempts | No | Must not own print queue in current slice |
| Gate authorization | Owns physical acknowledgment | Owns ExitAuthorization | No | No | No | No | No | Must not open gates |

Current contradictions:

- README says implementation is future work, but implemented runtime/DB/API/tests exist.
- Some planning docs still say DTO/status values are pending where runtime DTOs now exist.
- Management Platform UI contracts describe POS-backed profile administration, but the standalone frontend evidence does not prove the Central PMS backend proxy.
- APT contracts depend on Central PMS fiscal issuance/readiness delegation, but the statutory readiness facade remains a bounded gap.

## 16. Contract Gap Matrix

| Contract | Producer | Consumer | Current route/DTO | Current status | Missing or unsafe fields | Owner | Severity | Recommended task |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Central PMS to POS fiscal issuance | Central PMS | POS Server | `POST /v1/fiscal-documents`, `CreateFiscalDocumentRequest` | Implemented, needs cross-lane hardening | First-class applied statutory decision/application refs, entitlement/benefit classification, policy refs, terminal-cash refs, explicit privacy exclusions | Codex Z + Central PMS | HIGH | `feature/fiscal-issuance-applied-statutory-facts-contract` |
| Fiscal issuance status read | POS Server | Central PMS | `GET /v1/fiscal-documents/{id}` | Implemented | Central PMS timeout/unknown recovery contract not end-to-end proven | Central PMS + Codex Z | MEDIUM | Fiscal exception/readback integration proof |
| WebPay Sales Invoice read | POS Server via Central PMS/WebPay | WebPay | POS `GET /digital-sales-invoice/presentation`; WebPay route TBD | POS route implemented | Browser-safe gateway, auth, correlation and safe error mapping not UAT-proven | Codex G + Central PMS + Codex Z | HIGH | WebPay fiscal readback gateway proof |
| APT Sales Invoice read | POS Server via Central PMS | APT | Central PMS receipt-presentation contract delegates to POS presentation | Contract exists | End-to-end live proof with POS Server disposable DB missing | Codex J + Central PMS + Codex Z | HIGH | APT terminal-cash receipt presentation UAT |
| APT terminal-cash fiscal path | Central PMS | POS Server/APT | `/v1/terminal-cash-payments/.../fiscal-issuance` in Central PMS contract | Contracted outside POS | Cashier/shift/custody references and statutory applied facts need POS issuance contract governance | Central PMS + Codex Z + Codex J | HIGH | Terminal-cash fiscal issuance integration |
| Management Platform fiscal configuration | Management Platform via Central PMS | POS Server | POS `/v1/admin/*`; MP `/v1/management-platform/*` | POS API implemented, backend proxy gap | Central PMS service credential mapping, proxy error mapping, audit propagation | Central PMS + Codex H + Codex Z | HIGH | Central PMS POS profile admin proxy |
| Statutory privilege fiscal facts | Central PMS | POS Server | Generic discount refs/context | Partially implemented | First-class decision/application refs, tax treatment, ordinance/policy refs | Codex Z + Central PMS | CRITICAL | Applied statutory fiscal facts contract |
| Correlation propagation | All services | All consumers | `X-Correlation-Id` conventions | Implemented in POS endpoints/tests | Full WebPay/APT/MP end-to-end correlation proof missing | Shared | MEDIUM | Cross-lane proof harness |
| Idempotency | Central PMS | POS Server | `Idempotency-Key`, semantic `sha256:v1` | Implemented and tested | End-to-end replay after profile/statutory changes not UAT-proven across Central PMS | Codex Z + Central PMS | MEDIUM | Fiscal retry/UAT proof |
| Semantic hash | POS Server and Central PMS | POS Server/Central PMS | `sha256:v1` representative fixture | Compatible | Any new governed fields may require v2 coordination | Codex Z + Central PMS | HIGH if fields change | Hash version decision in statutory facts task |
| Fiscal numbering | POS Server | Central PMS/APT/WebPay | Runtime allocation/read model | Implemented | Production sequence configuration and UAT not proven | Codex Z + Management Platform/Central PMS | MEDIUM | Fiscal sequence controlled UAT |
| Original/reprint presentation | POS Server/APT | APT/WebPay | POS presentation, APT print history | Partially split | POS vs local print state must remain separated in UAT | Codex J + Codex Z | MEDIUM | APT print/reprint integration proof |

## 17. Database Gap Matrix

| Gap | Evidence | Severity | Owner | Database work required |
| --- | --- | --- | --- | --- |
| Long constraint identifiers above PostgreSQL 63-byte limit | Static validation identifier findings | MEDIUM | Codex Z | Yes, rename constraints in a future DB-only task |
| Column/index/constraint drift proof is limited | `db/scripts/README.md` says inventory scope is table-focused | MEDIUM | Codex Z | Possibly tooling only |
| Applied statutory decision/application refs not first-class in POS schema | Current request/model uses generic discount refs/context | HIGH | Codex Z + Central PMS | Likely yes |
| Terminal-cash cashier/shift/custody refs not governed in POS schema | Current request has runtime terminal ref only | HIGH | Codex Z + Central PMS + APT | Possibly yes |
| Canonical DB to POS Server config propagation not implemented | Management Platform contract routes through Central PMS but no proxy evidence | HIGH | Central PMS + Codex Z | Possibly no POS schema change, but integration config needed |
| Production fiscal sequence policy data not UAT-proven | Runtime exists, controlled UAT absent | MEDIUM | Codex Z + Management Platform/Central PMS | Data/config, not necessarily schema |

## 18. Security and Privacy Gaps

Security findings:

- POS admin API uses server-derived key-to-permission mapping and constant-time key comparison.
- Contract artifacts and tests use non-production values.
- No production key material was copied into this audit.
- Repo search found expected documentation and test placeholders involving "secret", "connection string", and local disposable values; no production credential was intentionally reproduced.

Security gaps:

- Service credential distribution from future Management Platform/Central PMS to POS Server is not UAT-proven.
- Broad POS Server internal API RBAC beyond the Sales Invoice profile admin policy remains a future governed area.
- Public/customer Digital SI URL access model and expiry remain planning gaps in older API docs.
- Cross-lane safe fiscal error mapping for browser/APT is not end-to-end proven.

Privacy gaps:

- POS Server tests reject raw evidence/payment-marker style discount payloads, but statutory evidence exclusion should be made explicit in the Central PMS to POS fiscal issuance contract.
- APT encrypted local DB protects local cash evidence but must not cause POS Server to persist denominations, raw local DB state, or full custody details.
- Operator Console reviewer details and evidence images must remain out of POS Server fiscal facts.

## 19. Fiscal Compliance Gaps

| Gap | Severity | Impact |
| --- | --- | --- |
| No controlled end-to-end WebPay/APT fiscal issuance UAT evidence | CRITICAL | Cannot authorize production rollout |
| Applied statutory facts are not first-class in POS fiscal contract | CRITICAL | Statutory Sales Invoice audit linkage could be inconsistent |
| Management Platform to POS Server profile administration proxy not proven | HIGH | Production header profiles cannot be governed through approved control plane |
| Static DB identifier length findings | MEDIUM | PostgreSQL truncation may obscure constraint names in operations |
| Final report/export/BIR package generation remains unimplemented | HIGH | Production certification/reporting path incomplete |
| Digital SI customer access model remains open | HIGH | Customer receipt access could be under-secured or unusable |

## 20. UAT Gaps

Controlled UAT is not authorized until these blockers are resolved:

- Disposable POS Server fiscal configuration seeded through approved admin/profile path.
- Central PMS finality-to-POS fiscal issuance integration.
- WebPay ordinary payment-to-fiscal walkthrough.
- WebPay statutory approved application-to-fiscal walkthrough.
- APT non-statutory terminal-cash fiscal issuance and receipt retrieval walkthrough.
- APT statutory cash readiness/application/fiscal walkthrough after Central PMS facade support.
- Repeated idempotent replay and semantic conflict proof across Central PMS and POS Server.
- Fiscal presentation readback proof through downstream gateways.
- No duplicate fiscal document/number proof under retry and timeout scenarios.

## 21. Gap Classification

### Critical

| Gap | Impact | Evidence | Owner | Repository | Predecessor | Branch | DB work | Manual testing | UAT blocked |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Applied statutory fiscal facts not governed as first-class POS contract | Statutory Sales Invoices may rely on ad hoc context keys | POS request supports generic refs/context only | Codex Z + Central PMS | POS Server + Central PMS | Current profile/admin API baseline | `feature/fiscal-issuance-applied-statutory-facts-contract` | Likely | Yes | Yes |
| No payment-to-fiscal controlled UAT | Production fiscal rollout cannot be authorized | G-001 excludes POS fiscal issuance | Codex Z + G + Central PMS + J | POS Server + ExitPass + APT/WebPay | Fiscal contract hardening | `test/webpay-apt-pos-fiscal-uat-harness` | No, unless fixture gaps | Yes | Yes |

### High

| Gap | Impact | Evidence | Owner | Repository | Predecessor | Branch | DB work | Manual testing | UAT blocked |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Management Platform POS admin proxy not proven | Header profiles cannot be administered through future control plane | MP contract uses Central PMS facade, POS API exists only in POS repo | Central PMS + Codex H + Codex Z | ExitPass/ManagementPlatform/POS Server | POS admin API | `feature/central-pms-pos-sales-invoice-profile-admin-proxy` | Maybe | Yes | Yes |
| APT statutory payable-basis facade gap | Statutory cash acceptance remains unauthorized | APT statutory analysis says facade is blocking gap | Central PMS + Codex J | ExitPass-APT + AssistedPaymentTerminal | Statutory backend readback | `feature/apt-statutory-payable-basis-readiness-facade` | Maybe | Yes | Yes |
| WebPay fiscal readback gateway not proven | Browser could lack safe SI retrieval after payment | G-001 excludes POS fiscal, G-003 excludes fiscal rendering | Codex G + Central PMS + Codex Z | ExitPass + POS Server | POS presentation route | `feature/webpay-sales-invoice-readback-gateway` | No | Yes | Yes |
| Customer Digital SI URL access model open | Receipt access security/retention unresolved | POS API open questions | Codex Z + Security | POS Server | Presentation baseline | `feature/digital-si-customer-access-contract` | Maybe | Yes | Yes |
| Fiscal report/export package unimplemented | Production fiscal reporting incomplete | DB posture objects only | Codex Z | POS Server | Issuance UAT | `feature/pos-fiscal-report-export-foundation` | Yes | Yes | Yes |

### Medium

- Long PostgreSQL constraint identifiers need cleanup.
- POS README is stale.
- Table-level inventory validation should expand to column/index/constraint drift.
- End-to-end correlation proof is absent.
- Original/reprint state split needs downstream UAT.
- Fiscal sequence configuration UAT is absent.

### Low

- Some historical docs still use provisional wording now superseded by implementation.
- Contract artifacts include source branch/commit metadata from their feature branch rather than current `origin/dev`.
- Proof evidence directories are intentionally ignored and therefore not retained in Git.

### Informational

- No unmerged POS Server remote feature branches were visible beyond `origin/dev` and `origin/master`.
- Full POS Server tests currently pass without skipped tests.

## 22. Prioritized Roadmap

| Priority | Persona | Repository | Branch | Objective | Exact dependency | Why next | Likely files | Test types | Manual testing | UAT impact |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Codex Z | POS Server | `feature/fiscal-issuance-applied-statutory-facts-contract` | Govern applied statutory decision/application and terminal-cash fiscal facts in POS issuance contract | Current POS `origin/dev` | Blocks statutory WebPay/APT fiscal correctness | Runtime/API DTOs, semantic fixture, contracts, DB if needed, tests | Runtime/API/persistence/contract/proof | Significant | Unblocks statutory fiscal UAT |
| 2 | Central PMS lane | ExitPass | `feature/central-pms-pos-fiscal-issuance-gateway-hardening` | Map finality/payable/statutory/cash facts to POS Server request and readback | Task 1 | Central PMS is producer of fiscal request | Central PMS contracts/services/tests | Integration/contract | Significant | Required for all channel UAT |
| 3 | Central PMS lane + Codex H | ExitPass + ManagementPlatform | `feature/central-pms-pos-sales-invoice-profile-admin-proxy` | Expose Management Platform browser-safe facade to POS admin API | POS admin API baseline | Allows governed profile setup | Central PMS API/client/config, MP API client tests | API/e2e/security | Significant | Required before production config |
| 4 | Codex G | ExitPass | `feature/webpay-sales-invoice-readback-gateway` | Add WebPay-safe SI retrieval and fiscal errors | Task 2 | WebPay needs browser receipt access | PO/WebPay/Central PMS clients | API/e2e | Significant | Required for WebPay UAT |
| 5 | Codex J + Central PMS | ExitPass-APT + AssistedPaymentTerminal | `feature/apt-terminal-cash-fiscal-issuance-integration` | Wire terminal-cash fiscal issuance and receipt readback using POS presentation | Task 2 | APT cash path needs fiscal proof | Central PMS facade, APT clients | API/desktop/e2e | Significant | Required for APT UAT |
| 6 | Central PMS + Codex J | ExitPass-APT + AssistedPaymentTerminal | `feature/apt-statutory-payable-basis-readiness-facade` | Add statutory readiness/application facts before CASH_RECEIVED | Task 1 and statutory backend | Blocks statutory cash | Readiness DTO/service, APT UI/local state | Contract/UI/e2e | Significant | Required for statutory cash UAT |
| 7 | Codex Z | POS Server | `feature/pos-db-identifier-and-drift-validation-hardening` | Rename long constraints and expand drift validation | Current DB baseline | Reduces operational risk | DB state/scripts/docs | DB static/rebuild/drift | No | Supports production readiness |
| 8 | Codex Z | POS Server | `feature/digital-si-customer-access-contract` | Define customer-safe Digital SI URL/access/expiry posture | Presentation baseline | Needed for customer receipt delivery | Contracts/API/security tests | API/security | Significant | Required for public customer receipt UAT |
| 9 | Codex Z | POS Server | `feature/pos-fiscal-report-export-foundation` | Begin report/export generation from existing posture tables | Fiscal issuance UAT | Needed for fiscal operations package | Runtime/DB/API/tests | Runtime/persistence | Significant | Required for production rollout |

## 23. Recommended First POS Server Task

Task: `feature/fiscal-issuance-applied-statutory-facts-contract`.

Objective: make POS Server's fiscal issuance request/readback contract explicitly govern the final applied statutory facts and terminal-cash context it must snapshot, while preserving Central PMS authority for payable basis and statutory application.

Why this is first:

- It is the highest-risk fiscal correctness gap exposed by Codex G, I, and J.
- Current POS Server can persist generic discount/VAT facts, but applied decision/application identity is not first-class.
- WebPay and APT statutory UAT cannot be authorized if fiscal audit linkage depends on ad hoc context keys.
- Any new fiscal semantic fields may require a coordinated `sha256:v2` decision; that must be resolved before Central PMS client work.

## 24. Production-Authorization Verdict

Controlled UAT: not authorized.

Production rollout: not authorized.

Reason: the POS Server baseline is technically healthy, but cross-lane finality-to-fiscal, statutory application-to-fiscal, Management Platform configuration propagation, APT terminal-cash fiscal issuance, customer-safe Sales Invoice retrieval, and fiscal reporting/export evidence are incomplete or unproven.

## 25. Validation Evidence

Environment:

- .NET SDKs: `10.0.302` and `8.0.421`.
- .NET host: `10.0.10`.
- Docker client/server: `29.5.3` / `29.5.3`.
- PostgreSQL image: `postgres:16-alpine`.
- Docker container: `posserver-audit-validation-20260729`.
- Host port: `55434`.
- Disposable database: `posserver_audit_validation_20260729`.
- Controlled-code disposable database: `posserver_controlled_code_audit_validation_20260729`.

Commands and results:

| Command | Result |
| --- | --- |
| `git branch --show-current` | `docs/pos-server-v13-implementation-and-cross-lane-gap-audit` |
| `git status --short --branch --untracked-files=all` | Clean before documentation edits |
| `git fetch origin --prune` | Passed after elevated linked-worktree metadata access |
| `git rev-parse HEAD` | `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1` |
| `git rev-parse origin/dev` | `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1` |
| `git diff --check` | Passed before edits |
| `dotnet restore` | Passed |
| `dotnet build -c Release --no-restore` | Passed, 0 warnings, 0 errors |
| `dotnet test -c Release --no-restore --no-build` | Passed: 281 total tests, 0 failed, 0 skipped |
| `powershell -ExecutionPolicy Bypass -File db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir db\validation\evidence\audit-static` | Passed, with 7 identifier length findings |
| `docker run --name posserver-audit-validation-20260729 -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=posserver_audit_validation_20260729 -p 55434:5432 -d postgres:16-alpine` | Passed |
| `docker exec posserver-audit-validation-20260729 pg_isready -U postgres -d posserver_audit_validation_20260729` | Accepting connections |
| `powershell -ExecutionPolicy Bypass -File db\scripts\Invoke-PosDbChecks.ps1 -Mode All -UseDockerPsql -ConnectionString postgresql://postgres:postgres@host.docker.internal:55434/posserver_audit_validation_20260729 -DatabaseName posserver_audit_validation_20260729 -EvidenceDir db\validation\evidence\audit-docker-all` | Passed |
| `powershell -ExecutionPolicy Bypass -File db\scripts\Invoke-PosDbChecks.ps1 -Mode ControlledCodeLoad -UseDockerPsql -ConnectionString postgresql://postgres:postgres@host.docker.internal:55434/posserver_controlled_code_audit_validation_20260729 -DatabaseName posserver_controlled_code_audit_validation_20260729 -EvidenceDir db\validation\evidence\audit-controlled-code-load` | Passed |
| `docker rm -f posserver-audit-validation-20260729` | Passed |

Evidence locations:

- `db/validation/evidence/audit-static`.
- `db/validation/evidence/audit-docker-all`.
- `db/validation/evidence/audit-controlled-code-load`.

Evidence ignore posture:

- `db/validation/.gitignore` ignores `evidence/`, `*.local.json`, and `*.local.txt`.

Significant manual testing required for this documentation-only audit: No.

## 26. Security Review Result

Security scan scope included POS Server source, tests, docs, contracts, database scripts, proof scripts, and the new audit documentation.

Findings:

- No production password, API key, bearer token, private key, certificate, customer data, raw statutory ID, entitlement image, real fiscal identity, or real sequence value was intentionally added.
- Existing repository files contain test/local/disposable terms such as API-key configuration names, example/local connection-string shapes, `postgres` disposable database credentials for validation, and documentation warnings about secrets. These are non-production validation placeholders and should not be copied into production configuration.
- The audit deliberately does not reproduce any secret value from read-only repositories.

## 27. Documentation Deliverables

Added documentation files:

- `docs/reviews/ExitPass_POS_Server_v1.3_Implementation_Baseline_Audit_and_Cross_Lane_Gap_Analysis_v1.0.md`.
- `docs/reviews/ExitPass_POS_Server_v1.3_Implementation_Baseline_Audit_Handoff_v1.0.md`.

Production files changed: None.

