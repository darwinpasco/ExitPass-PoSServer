# ExitPass POS Server Z-012B Annex E-1 Deterministic Runtime Implementation v1.0

## 1. Status and authority

Z-012B implements the local bounded profile `pos-server-bir-annex-e1-rmo24-2023:v1` authorized by Z-012A2. The immutable Accounting calculation profile is `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md`, version `v1.0`, SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f`, approved by `Z-012B-ACCOUNTING-APPROVAL-001`.

The runtime is local generation, readback, and download only. It does not authorize Controlled UAT, external delivery, BIR submission, Production, signing, encryption, destructive purge, E-2 through E-5, or ARTS POSLog 6.0.0.

## 2. Authority boundary

- One immutable detail row consumes one committed Z Reading, its CLOSED reporting period, and its committed Z-010 BIR Sales Summary.
- The BIR Sales Summary supplies authoritative financial values. Stored Z children supply governed discount VAT adjustments and fiscal-range evidence.
- Z-011A Electronic Journal remains traceability evidence and is not used as the financial authority.
- The renderer consumes calculated immutable row values. It does not query or recalculate fiscal documents.
- Generation, readback, replay, download, and correction do not mutate documents, Z reports, periods, counters, GTA, or BIR Sales Summaries.

## 3. API

| Method and route | Permission | Purpose |
|---|---|---|
| `POST /v1/fiscal-reports/annex-e/e1/accounting-facts` | `fiscal_annex_e.accounting_fact.record` | Record a governed Manual SI/OR or accumulated-sales-capacity overflow fact, including immutable correction lineage. |
| `POST /v1/fiscal-reports/annex-e/e1/known-zero-attestations` | `fiscal_annex_e.known_zero.attest` | Record explicit exact-period zero evidence. |
| `POST /v1/fiscal-reports/annex-e/e1/workbooks` | `fiscal_annex_e.generate`; corrections also require `fiscal_annex_e.correct` | Generate, replay, or explicitly supersede a monthly workbook. |
| `GET /v1/fiscal-reports/annex-e/e1/workbooks/{id}` | `fiscal_annex_e.read` | Read immutable metadata and row evidence. |
| `GET /v1/fiscal-reports/annex-e/e1/workbooks/{id}/content` | `fiscal_annex_e.export` | Download the exact stored XLSX bytes after integrity validation. |

Every operation enforces the exact Site POS Server, fiscal identity, and PHP currency claims. Fixture/development authority is rejected in Production. A missing permission cannot be substituted by another Annex E or fiscal-report permission.

## 4. Canonical persistence

| Object | Purpose |
|---|---|
| `pos.annex_e1_period_accounting_facts` | Append-only `RECORDED` or `ATTESTED_ZERO` period facts, semantic identity, source references, approval reference, and correction predecessor. |
| `pos.annex_e1_workbooks` | Immutable monthly logical identity, revision, hashes, artifact metadata, header snapshot, and supersession lineage. |
| `pos.annex_e_reports` | Immutable per-Z D01-D32 calculated snapshot and authoritative Z/BIR source links. |
| `pos.annex_e1_report_fact_sources` | Ordered immutable links from each workbook row to its seven required first-class facts. |
| `pos.fiscal_action_audit` | Privacy-safe successful fact and workbook commit evidence using the existing `annex_e` report classification. |

Database checks enforce PHP, controlled-code families, hashes, positive recorded-source evidence, explicit zero posture, correction scope, immutable rows, one current correction child, one row per Z per workbook, exact source scope, nonnegative values, and the approved D19/D25/D26/D27/D29 reconciliations. The legacy empty Annex E placeholder is replaced during upgrade; a nonempty legacy table fails closed because historical conversion is not authorized.

## 5. First-class Accounting facts

`manual_si_or_net_income` represents positive VAT-exclusive income from qualifying continuity/BCP manual fiscal documents. A recorded value requires a positive amount, positive document count, first and last privacy-safe source references, exact scope/period, approval reference, and semantic hash. Drafts, cancellations, duplicate encoding, later-fiscalized duplicates, and wrong-scope sources remain excluded by the approved source authority.

`sales_overrun_overflow_net_income` represents positive VAT-exclusive income omitted solely by an approved accumulated-sales-capacity overflow event. It requires the same source evidence plus a governed source-event reference. Rounding difference, cash overage, fiscal-number exhaustion, counter rollover, transaction volume, manual continuity sales, and late posting are not accepted interpretations.

`ATTESTED_ZERO` requires an exact-scope approval reference and carries zero amount, zero count, and no source document or event values. Absence is never zero. NAAC, Solo Parent, other VAT adjustment, VAT on returns, and residual VAT adjustment are zero-only in this bounded profile; recorded or nonzero values fail closed.

## 6. Approved calculations

All inputs and results are checked signed 64-bit PHP minor units. No floating-point operation or workbook formula is authoritative.

```text
D07 = ACTIVE_GROSS + RETURN_AMOUNT + VOID_AMOUNT
D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
D25 = D20 + D21 + D22 + D23 + D24
D26 = D09 - D22
D27 = D07 - D19 - D09
D29 = D27 + D06 + D28
```

R01 through R07 use zero minor-unit tolerance. Negative, overflowing, missing, duplicate, unresolved, unexplained-gap, multi-range, refund, adjustment, service-charge, nonzero return, and unsupported privilege sources fail closed. Same-period void magnitude remains visible in D18 and is added back into D07 exactly once before deduction.

A committed no-activity Z emits one row only when every amount and required fact is governed zero, GTA beginning equals GTA ending, the reset counter is unchanged by Z, the Z counter is recorded, and the fiscal range is blank. D32 is `NO_ACTIVITY`; ordinary rows use `NONE`.

## 7. Workbook contract

The renderer directly writes a macro-free Open XML package matching the approved official workbook identity SHA-256 `7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197`. It preserves worksheet `E-1`, H01-H10, D01-D32 in A:AF, labels, official hidden annotations, merged regions, widths, heights, wrapping, formats, and landscape print configuration.

Determinism is controlled through fixed ZIP entry order and timestamps, uncompressed entries, fixed relationship and XML order, inline strings, invariant formatting, normalized package properties, application-calculated values, and SHA-256. The package has no macros, external links, remote connections, signatures, encryption, machine paths, current-locale formatting, or volatile formulas.

The internal filename is `ANNEX-E1_<FISCAL-ID-CODE>_<MIN>_<YYYYMM>_v1.xlsx`. It is an ExitPass convention, not a claimed BIR-prescribed name.

## 8. Identity, replay, and correction

The logical scope is Site POS Server, fiscal identity, PHP, calendar year/month, and profile. Semantic hash profile `pos-server-annex-e1-request:sha256:v1` binds the logical scope, approved calculation profile/hash, official template hash, renderer version, immutable header values, ordered period/Z/BIR identities, BIR semantic hashes, ordered fact identities/hashes, and correction lineage. Operation key, caller, correlation, and first-generation clock are not financial inputs.

Exact operation or current-scope replay returns the original metadata and stored bytes. Changed semantics conflict. An explicit correction must supersede the current workbook in the identical scope, use a controlled reason and approval reference, and creates a new immutable revision. Original metadata, rows, fact links, and artifact remain readable. Concurrent identical generation serializes under a transaction advisory lock and produces one authoritative result.

## 9. Artifact publication and recovery

Artifact bytes are stored outside PostgreSQL under a content-addressed SHA-256 key. Publication writes a unique temporary file with write-through and disk flush, then performs an atomic same-filesystem move. PostgreSQL metadata and row evidence commit only after the final file is readable and hash-equal. A failed transaction can leave only an unreferenced content-addressed orphan; no API can read it. Retry publishes or reuses the same verified bytes.

The API host requires an absolute `PosServer:AnnexE1:ArtifactRoot` configuration value. Missing, relative, or blank storage configuration registers the fail-closed unavailable repository; the runtime never selects an implicit working-directory or binary-directory store.

Download reads the stored artifact, verifies SHA-256 and byte length against immutable metadata, and fails closed for missing or altered bytes. Responses use the XLSX MIME type, a controlled filename, `private, no-store`, `nosniff`, and a SHA-256 ETag. Host paths are never returned.

## 10. Validation evidence

Automated coverage includes exact profile identity, checked calculations, every D-position, no-activity, missing and explicit-zero distinction, unsupported privilege/exception failure, deterministic Open XML structure and bytes, independent policies, scope and Production fixture denial, live PostgreSQL fact and workbook persistence, concurrent generation, replay, semantic conflict, correction/supersession, tamper/missing-file denial, immutable database triggers, and unchanged Z/BIR source state.

Repository validation uses Release restore/build/test, canonical database static/rebuild/replay/inventory/drift and controlled-code loading, an origin/dev upgrade proof, contract JSON parsing, documentation link checks, privacy/secret/prohibited-path scans, and `git diff --check`. Disposable PostgreSQL and temporary artifact storage are removed after proof.

## 11. Remaining gates

- AE-DR-012 blocks every nonzero unresolved Diplomat or other VAT-privilege path; bounded runtime rejects it.
- AE-DR-002, 004, 010, 011A, 019, 020A, and 024 remain BIR/examiner acceptance gates for Controlled UAT or Production presentation.
- AE-DR-016 blocks external signing, encryption, compression, and submission-channel work.
- AE-DR-016B blocks Production retention/archive/deletion policy.
- E-2 through E-5, ARTS POSLog 6.0.0, external delivery, Controlled UAT, and Production remain unauthorized.
