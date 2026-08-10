# ExitPass POS Server Z-009 Annex E Runtime Authorization Checklist v1.0

## 1. Current verdict

- Runtime design: `AUTHORIZED_FOR_RUNTIME_DESIGN` by `Z-009B-USER-APPROVAL-001`
- Bounded E-1 implementation: `AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME` by `Z-012B-ACCOUNTING-APPROVAL-001` and Z-012A2 revalidation
- Controlled UAT: not authorized
- Production: not authorized

## 2. Start technical design

- [x] Z-009A source, field, mapping, calculation, layout, sample, scenario, and handoff package merged.
- [x] AE-DR-001 through AE-DR-024 preserved and audited.
- [x] Regulatory and project authority classes separated.
- [x] E-2 through E-5 isolated from E-1.
- [x] User Approval Record completes all 13 project-owned recommendations under `Z-009B-USER-APPROVAL-001`.
- [x] Approved choices are incorporated into the versioned documentation contract; a future runtime task must add any required machine-readable contract.
- [x] Z-010 committed-Z BIR Sales Summary and Z-011A Electronic Journal authority boundaries revalidated at baseline `227cdc708d1a685cd56986f084d0cc1aad3e81dd`.
- [x] Design scope explicitly excludes Controlled UAT, production submission, E-2:E-5, EJ changes, and POSLog.

**Gate:** passed. Bounded technical design is authorized; runtime implementation remains separately gated.

## 3. Merge schema changes

- [x] AE-DR-001, 010A, 011, 017, and 018 user approvals recorded.
- [x] AE-DR-007 and 008 exact first-class source definitions approved by `Z-012B-ACCOUNTING-APPROVAL-001`.
- [x] Z-012B snapshots mandatory historical header values, generated timestamp, and privacy-safe H10 actor/service reference in immutable workbook metadata.
- [x] Z-012B implements controlled profile, remarks, fact status/type, workbook status, and correction-lineage codes for the bounded local profile.
- [ ] Existing non-empty metadata migration posture audited; no historical values invented.
- [ ] Rebuild, upgrade, replay, inventory, drift, wrong-family, immutability, and privacy proof designed.
- [ ] Generic JSON is prohibited as field or correction authority.

**Gate:** schema implementation cannot merge while required field/source definitions are absent.

## 4. Implement bounded generator

- [x] AE-DR-002A, 003A, 004A, 019A, 021, 022, and 024A user approvals recorded.
- [x] AE-DR-006 and 009 exact named-operand equations approved by `Z-012B-ACCOUNTING-APPROVAL-001`.
- [x] AE-DR-007 Manual SI/OR and AE-DR-008 Sales Overrun/Overflow source definitions approved by Accounting.
- [ ] Approved schema and controlled codes merged.
- [ ] Deterministic XLSX profile and canonical validation JSON contracts versioned.
- [ ] Replay, conflict, restart, supersession, and unknown-outcome behavior frozen.
- [ ] All 42 fields have executable source/null/zero/format rules.
- [ ] Unsupported Diplomat/cross-period/exception paths fail closed.
- [ ] No-state-mutation and privacy test manifests specified.

**Gate:** local bounded implementation is authorized. Z-012B must implement the unchecked runtime/schema/test items; those are deliverables, not unresolved decision gates.

## 5. Controlled UAT

- [ ] AE-DR-002, 004, 010, 011A, 016, 019, 020A, and 024 external evidence accepted.
- [ ] Examiner-approved artifact, filename, header, formatting, and geometry fixtures recorded.
- [ ] Signing/encryption/submission requirements either implemented or explicitly confirmed not required.
- [ ] Disposable PostgreSQL/API proof and byte-determinism proof pass.
- [ ] Security, privacy, audit, retention metadata, and operator runbook reviewed.
- [ ] Formal Controlled UAT authorization issued separately.

## 6. Production rollout

- [ ] Controlled UAT passed with signed evidence.
- [ ] AE-DR-016B legal/compliance retention schedule approved and implemented.
- [ ] Production delivery/submission controls approved.
- [ ] Operational ownership, monitoring, backup, archive, recovery, and incident runbooks approved.
- [ ] BIR/accreditation acceptance reference recorded without confidential contact data.
- [ ] Production rollout authorized separately.

## 7. Z-012A staged gate reconciliation

- Generator implementation: authorized against the exact approved profile under `Z-012B-ACCOUNTING-APPROVAL-001`.
- Nonzero Diplomat or unresolved VAT-privilege scenarios: blocked by AE-DR-012; an eventual ordinary path must fail closed for these scenarios.
- Controlled UAT: blocked by the applicable BIR/examiner and Accounting confirmations.
- External delivery: blocked by AE-DR-002, AE-DR-004, AE-DR-016, and AE-DR-024.
- Production: blocked by Controlled UAT, formal acceptance, and AE-DR-016B retention approval.
- Deterministic XLSX: technically feasible only with deterministic Open XML and ZIP package construction as specified in the Z-012A revalidation.

## 8. Authorization sign-off

- Technical design authorized by/date/reference: `Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001`
- Schema implementation authorized by/date/reference: `Z-012A2 / 2026-08-10 / AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME`
- Bounded generator authorized by/date/reference: `Accounting and Z-012A2 / 2026-08-10 / Z-012B-ACCOUNTING-APPROVAL-001`
- Controlled UAT authorized by/date/reference: `NOT AUTHORIZED`
- Production authorized by/date/reference: `NOT AUTHORIZED`

## 9. Z-012A1 exact Accounting profile gate

- [x] Accounting authority and approval in principle confirmed on 2026-08-10.
- [x] One exact profile covers AE-DR-006 through AE-DR-009 with named operands and zero-tolerance reconciliations.
- [x] Manual SI/OR and Sales Overrun/Overflow minimum first-class source contracts are specified.
- [x] Known zero is separated from missing or unsupported source data.
- [x] Approval instrument references the profile filename, version, and SHA-256 hash.
- [x] Accounting selected `APPROVED_EXACTLY_AS_SPECIFIED` for that exact hash.
- [x] AE-DR-006 through AE-DR-009 were subsequently recorded as resolved.
- [x] Required schema, controlled codes, runtime, and tests are implemented by Z-012B and pending review/merge.

**Gate:** superseded by `Z-012B-ACCOUNTING-APPROVAL-001` on 2026-08-10.

## 10. Z-012A2 local runtime gate

- [x] Approved proposal filename, version, profile, scope, and SHA-256 verified exactly.
- [x] Approved proposal remains byte-for-byte unchanged.
- [x] AE-DR-006 through AE-DR-009 resolved without changing their executable meaning.
- [x] Manual SI/OR and Sales Overrun/Overflow first-class facts are approved Z-012B implementation requirements.
- [x] Missing source remains distinct from `ATTESTED_ZERO`.
- [x] Committed-Z, CLOSED-period, BIR Summary authority, Electronic Journal traceability, deterministic XLSX, artifact atomicity, correction lineage, exact scope, privacy, and fail-closed privilege boundaries are frozen.
- [x] Remaining external decisions do not block local bounded implementation.
- [x] Z-012B implementation, database objects, controlled codes, APIs, deterministic artifacts, and tests completed for review.

**Current decision:** `AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME`. Controlled UAT, external delivery, and Production remain unauthorized.

## 11. Z-012B implementation result

- [x] Exact approved calculation profile hash is enforced in runtime and canonical database checks.
- [x] Manual SI/OR and accumulated-sales-capacity overflow facts distinguish `RECORDED`, `ATTESTED_ZERO`, missing, and corrected evidence.
- [x] All 10 header and 32 detail positions are deterministically emitted.
- [x] Metadata, source rows, fact links, correction lineage, and artifact hashes are immutable.
- [x] Download returns stored bytes after SHA-256 and length verification; it does not regenerate.
- [x] Exact replay, semantic conflict, concurrent generation, correction, tamper, scope, and Production-fixture proofs pass.

**Implementation gate:** Z-012B is ready for review and merge. Controlled UAT, external delivery, Production, E-2 through E-5, and ARTS POSLog remain unauthorized.
