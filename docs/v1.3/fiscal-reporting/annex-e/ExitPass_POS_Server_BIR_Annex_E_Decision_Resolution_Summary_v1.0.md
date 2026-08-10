# ExitPass POS Server BIR Annex E Decision Resolution Summary v1.0

## Executive summary

Z-009B converts the Z-009A open register into a 35-record inventory while preserving AE-DR-001 through AE-DR-024. All 13 project-owned recommendations were explicitly approved by Darwin Pasco under `Z-009B-USER-APPROVAL-001`. This approval does not claim BIR acceptance.

The source, merged architecture, and recorded project approval settle the bounded E-1 profile, header posture, one-row-per-Z source grain, monthly grouping, deterministic XLSX projection, local delivery boundary, replay/correction posture, Site POS Server scope, and fail-closed rules. Z-012A revalidated those decisions on 2026-08-10 PHT against merged Z-010 and Z-011A. Accounting then approved the exact immutable Z-012A1 calculation profile through `Z-012B-ACCOUNTING-APPROVAL-001`, resolving AE-DR-006 through AE-DR-009. Ten questions still require examiner, Accounting, or Legal/Compliance confirmation, but none blocks local bounded implementation. E-2 through E-5 remain deferred.

## Resolution groups

### Existing source

- AE-DR-005: all ten labeled E-1 header positions are part of the source profile.

### Existing ExitPass decisions

- AE-DR-003: one E-1 row consumes one committed governing Z.
- AE-DR-005A: header values are historical immutable facts and H10 is server-derived.
- AE-DR-015: same-period governed void is supported; unsupported/cross-period exceptions fail closed.
- AE-DR-020: scope is Site POS Server, fiscal identity, and currency with child channels combined.
- AE-DR-023: reprints do not count; training is unsupported.

### Approved ExitPass project decisions

AE-DR-001, 002A, 003A, 004A, 010A, 011, 016A, 017, 018, 019A, 021, 022, and 024A were approved by `Z-009B-USER-APPROVAL-001`. They define the bounded E-1 profile, deterministic XLSX renderer, monthly grouping, safe filename, controlled remarks, statutory zero/fail-closed posture, local delivery boundary, immutable corrections/history, deterministic formatting, empty periods, ranges/gaps, and overflow handling.

### Accounting-approved executable profile

AE-DR-006, 007, 008, and 009 are resolved by `Z-012B-ACCOUNTING-APPROVAL-001` against profile SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f`. The exact immutable profile controls every operand, formula, source class, period, sign, zero/null/no-activity rule, correction rule, reconciliation, and bounded-runtime limitation.

### External confirmations

AE-DR-002, 004, 010, 011A, 012, 016, 016B, 019, 020A, and 024 remain externally controlled. AE-DR-012 blocks nonzero unresolved privilege paths. The others are confined to Controlled UAT, external delivery, or Production acceptance because approved internal profiles and fail-closed behavior exist for local implementation.

### Deferred

AE-DR-013 and AE-DR-014 defer E-2 through E-5 to separate legal/privacy/compliance tasks. They do not authorize collection of beneficiary names, statutory IDs, TINs, child data, or evidence in POS Server.

## Recommended bounded contract

- Profile: `pos-server-bir-annex-e1-rmo24-2023:v1`.
- Grain: one physical detail row per committed governing Z and fiscal identity.
- Grouping: one workbook per Site POS Server, fiscal identity, currency, and calendar month.
- Trigger: capture immutable row/header facts at or after Z close; monthly assembly is a read-only projection.
- Artifact: deterministic XLSX using the official E-1 workbook structure; internal canonical JSON is validation-only.
- Filename: `ANNEX-E1_<FISCAL-ID-CODE>_<MIN>_<YYYYMM>_<PROFILE-VERSION>.xlsx`.
- Replay: byte-identical for the same immutable membership, profile, renderer, and options.
- Correction: immutable supersession lineage, never overwrite.
- Delivery: authorized local generation/download only in the bounded task.

## Runtime and schema implications

Technical design and local bounded runtime implementation are authorized. Z-010 supplies the committed-Z BIR Sales Summary financial authority; Z-011A supplies traceability evidence but is not a financial source. Z-012B must implement first-class historical headers, Manual SI/OR and overrun period facts, known-zero attestations, missing E-1 classifications, controlled remarks, output history/hash, correction lineage, deterministic artifacts, exact scope, and fail-closed unsupported paths.

## Privacy implications

E-1 contains taxpayer/machine registration data but no beneficiary identity. Safe filenames exclude TIN and taxpayer name. E-2 through E-5 remain separate and deferred. Generic JSON, notes, evidence, and mutable live lookups are prohibited as compliance authority.

## Final verdicts

- Runtime design: `AUTHORIZED_FOR_RUNTIME_DESIGN`.
- Bounded E-1 implementation: `AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME`.
- Controlled UAT: not authorized.
- External delivery: not authorized.
- Production: not authorized.

The complete Z-012A evidence and exact 42-position reconciliation are in [Z-012A Runtime Authorization Revalidation](ExitPass_POS_Server_Z012A_Annex_E1_Runtime_Authorization_Revalidation_v1.0.md).

## Z-012A1 Accounting calculation proposal

Accounting authority and approval in principle were confirmed on 2026-08-10, but the merged recommendations did not contain executable definitions. Z-012A1 therefore adds one exact proposal and an unselected approval instrument:

* [Accounting Calculation Profile Proposal](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md)
* [Accounting Calculation Profile Approval Form](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Approval_Form_v1.0.md)

The proposal names every operand, source classification, formula, period rule, sign, zero/null behavior, no-activity rule, and zero-tolerance reconciliation for AE-DR-006 through AE-DR-009. This paragraph preserves the Z-012A1 pre-approval posture; it was superseded on 2026-08-10 by `Z-012B-ACCOUNTING-APPROVAL-001` with `APPROVED_EXACTLY_AS_SPECIFIED`.

## Z-012A2 runtime-authorization result

The approved profile is unchanged at its approved hash. The missing first-class facts and runtime objects are now implementation requirements, not unresolved business decisions. Local Z-012B may implement them, require explicit same-scope/period `RECORDED` or `ATTESTED_ZERO` evidence, and fail closed where no source exists. Nonzero AE-DR-012 privilege facts remain unsupported. Controlled UAT, external delivery, Production, E-2 through E-5, and ARTS POSLog remain unauthorized.
