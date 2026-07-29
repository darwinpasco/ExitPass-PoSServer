# ExitPass POS Server v1.3 Implementation Baseline Audit Handoff v1.0

## Current Trusted Baseline

Repository: `D:\SourceCodes\ExitPass-PoSServer-Z-Audit`.

Branch: `docs/pos-server-v13-implementation-and-cross-lane-gap-audit`.

Trusted source baseline: `origin/dev` at `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1`.

Baseline verdict: HEALTHY_WITH_GAPS.

POS Server currently owns and implements:

- Fiscal document creation, readback, status history, void posture, and PostgreSQL persistence.
- Fiscal lines, tenders, taxes, discount/privilege details, totals, and links.
- Fiscal sequence policy/identity resolution, runtime fiscal-number allocation, idempotency, replay, conflict behavior, and `sha256:v1` semantic hash parity fixture.
- Digital Sales Invoice rendering, template versioning, presentation adapter, and authoritative presentation endpoint `GET /v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation`.
- Fiscal Identity and Sales Invoice Header Profile persistence, readiness, resolution, immutable header snapshot, presentation integration, and proof fixture.
- Secured administration API for Fiscal Identities and Sales Invoice Header Profiles under `/v1/admin/*`.

Validation completed during audit:

- `dotnet restore`: passed.
- `dotnet build -c Release --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test -c Release --no-restore --no-build`: passed, 281 total tests, 0 failed, 0 skipped.
- Static database validation: passed with 7 PostgreSQL identifier length findings.
- Docker-backed rebuild/inventory/drift validation: passed.
- Controlled-code load validation: passed.

## Do-Not-Regress Rules

- POS Server must remain the only authority for fiscal document creation, fiscal numbers, fiscal status, fiscal snapshots, and Sales Invoice presentation.
- Central PMS must remain the authority for payable basis, payment finality, statutory application, and ExitAuthorization.
- POS Server must not decide statutory eligibility, approve statutory privileges, apply statutory privileges to payable amounts, calculate vendor parking tariff, establish provider finality, own evidence images, or open gates.
- APT and WebPay must not reconstruct Sales Invoice content; they must retrieve or receive the authoritative POS Server presentation through approved backend paths.
- Management Platform browser code must not call POS Server directly or receive POS Server admin secrets.
- Fiscal request idempotency must continue to hash governed client-submitted request semantics only. Server-resolved profile/header snapshot facts must stay outside `sha256:v1` unless a coordinated `sha256:v2` slice is explicitly approved.
- Runtime terminal context may be snapshotted from issuance context; APT terminal ID must not become a static Sales Invoice header profile field.
- POS Server must persist only safe fiscal/statutory references and final fiscal facts, never raw statutory IDs, evidence images, full entitlement documents, reviewer notes, or credentials.
- Reads and presentation endpoints must stay side-effect-free and must not create fiscal documents, allocate fiscal numbers, or modify print state.

## Critical Dependencies

| Dependency | Current status | Owner |
| --- | --- | --- |
| Central PMS finality-to-POS fiscal issuance gateway | Not UAT-proven | Central PMS + Codex Z |
| Applied statutory decision/application facts in POS fiscal request | Partially supported only through generic refs/context | Codex Z + Central PMS |
| Management Platform to Central PMS to POS profile admin proxy | POS API exists, backend proxy not proven | Central PMS + Codex H + Codex Z |
| WebPay payment-to-fiscal walkthrough | G-001 excludes POS fiscal issuance | Codex G + Central PMS + Codex Z |
| WebPay Sales Invoice safe retrieval | POS endpoint exists, WebPay gateway not proven | Codex G + Central PMS + Codex Z |
| APT terminal-cash fiscal issuance | Central PMS contract exists, end-to-end POS proof missing | Codex J + Central PMS + Codex Z |
| APT statutory cash readiness | Blocked by statutory-aware Central PMS readiness facade | Central PMS + Codex J |
| Customer Digital SI URL/access/expiry model | Open planning gap | Codex Z + Security |
| Fiscal report/export generation | Posture tables exist, runtime generation not implemented | Codex Z |

## Unresolved Decisions

- Whether applied statutory decision/application refs become first-class POS request fields, governed context keys, or a new versioned statutory fiscal fact object.
- Whether any statutory fiscal contract hardening requires `sha256:v2`; do not silently change `sha256:v1`.
- Exact fiscal facts POS Server must receive for statutory tax treatment, VAT privilege/exemption, entitlement/benefit type, ordinance/policy references, and final totals.
- Exact terminal-cash evidence references POS Server must snapshot: terminal, cashier, shift, custody, tender, payment/session correlation.
- Whether long PostgreSQL constraint identifiers should be renamed immediately or handled as a validation-tool hardening slice.
- Customer-facing Digital SI URL authentication, authorization, token, expiry, and retention posture.
- Production monitoring, health, and recovery runbooks for fiscal readiness and profile readiness.

## Next Recommended Task

Persona: Codex Z.

Repository: `D:\SourceCodes\ExitPass-PoSServer`.

Branch: `feature/fiscal-issuance-applied-statutory-facts-contract`.

Objective: govern the applied statutory and terminal-cash fiscal facts that Central PMS must submit to POS Server before Sales Invoice issuance.

Required predecessor: current POS Server `origin/dev` at or after `46ddd685fcca2a9b9ecac8fb5fddc670207e35b1`.

Why it is next:

- It is the single highest fiscal-compliance risk exposed by Codex G, I, and J.
- Current POS request DTOs support generic discount and reference context, but statutory audit linkage should not depend on ad hoc metadata.
- WebPay and APT statutory UAT require a stable fiscal contract before downstream implementation.
- This task can decide whether `sha256:v1` remains sufficient or a coordinated `sha256:v2` is required.

Likely production files:

- `src/ExitPass.PosServer.Runtime/FiscalDocuments/*`.
- `src/ExitPass.PosServer.Api/FiscalDocuments/*`.
- `src/ExitPass.PosServer.Persistence.Postgres/FiscalDocuments/*`.
- `contracts/pos-server/*`.
- `docs/v1.3/pos-server-runtime/*`.
- `docs/v1.3/pos-server/fiscal-numbering/fixtures/*`.
- `tests/ExitPass.PosServer.Runtime.Tests/*`.
- `tests/ExitPass.PosServer.Api.Tests/*`.
- `tests/ExitPass.PosServer.Persistence.Postgres.Tests/*`.

Expected validation:

- `dotnet restore`.
- `dotnet build --no-restore`.
- Focused fiscal creation/statutory contract tests.
- Fiscal semantic hash tests.
- Fiscal presentation tests.
- Persistence tests if schema changes.
- Database static/rebuild/inventory/drift validation if schema changes.
- Proof script demonstrating statutory/non-statutory compatibility, idempotent replay, semantic conflict, and no prohibited evidence persisted.

Significant manual testing required for that implementation task: Yes.

## Required Validation for Future Branches

Preserve the current baseline:

```powershell
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-restore --no-build
powershell -ExecutionPolicy Bypass -File db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir db\validation\evidence\<task-static>
```

When database behavior is affected:

```powershell
powershell -ExecutionPolicy Bypass -File db\scripts\Invoke-PosDbChecks.ps1 -Mode All -UseDockerPsql -ConnectionString <disposable-postgresql-url> -DatabaseName <disposable-db-name> -EvidenceDir db\validation\evidence\<task-docker-all>
powershell -ExecutionPolicy Bypass -File db\scripts\Invoke-PosDbChecks.ps1 -Mode ControlledCodeLoad -UseDockerPsql -ConnectionString <disposable-postgresql-url> -DatabaseName <disposable-db-name> -EvidenceDir db\validation\evidence\<task-controlled-code-load>
```

Before completion:

```powershell
git diff --check
git status --short --branch --untracked-files=all
```

## Manual Testing and UAT Posture

Significant manual testing required for this documentation-only audit: No.

Controlled UAT posture: not authorized.

Controlled UAT becomes plausible only after:

- POS fiscal issuance contract hardens applied statutory and terminal-cash facts.
- Central PMS maps payment finality and applied payable basis into POS fiscal issuance.
- Management Platform profile setup reaches POS Server through the approved backend service identity.
- WebPay and APT retrieve authoritative POS Server presentation through safe backend paths.
- Retry, replay, conflict, timeout/readback, and no-duplicate-number scenarios are proven.

Production rollout posture: not authorized.

## UAT Authorization Posture

Not authorized for production-like fiscal UAT yet.

Reason: POS Server baseline is healthy, but cross-lane fiscal finality, statutory application, terminal-cash, and configuration propagation paths are not yet proven end-to-end.

## Handoff Summary

Use the current POS Server baseline as the trusted foundation for fiscal issuance, numbering, presentation, and Sales Invoice header profile administration. Do not reopen already governed endpoint shape unless evidence shows a concrete contract gap. The next POS Server work should focus narrowly on the fiscal issuance request boundary for applied statutory and terminal-cash facts because that is the current blocking point for WebPay/APT statutory correctness and controlled UAT.

