# ExitPass POS Server Z-009 Annex E Runtime Authorization Checklist v1.0

## 1. Current verdict

- Runtime design: `AUTHORIZED_FOR_RUNTIME_DESIGN` by `Z-009B-USER-APPROVAL-001`
- Bounded E-1 implementation: `BLOCKED_PENDING_EXTERNAL_CONFIRMATION`
- Controlled UAT: not authorized
- Production: not authorized

## 2. Start technical design

- [x] Z-009A source, field, mapping, calculation, layout, sample, scenario, and handoff package merged.
- [x] AE-DR-001 through AE-DR-024 preserved and audited.
- [x] Regulatory and project authority classes separated.
- [x] E-2 through E-5 isolated from E-1.
- [x] User Approval Record completes all 13 project-owned recommendations under `Z-009B-USER-APPROVAL-001`.
- [x] Approved choices are incorporated into the versioned documentation contract; a future runtime task must add any required machine-readable contract.
- [ ] Design scope explicitly excludes Controlled UAT, production submission, E-2:E-5, EJ, and POSLog.

**Gate:** passed. Bounded technical design is authorized; runtime implementation remains separately gated.

## 3. Merge schema changes

- [x] AE-DR-001, 010A, 011, 017, and 018 user approvals recorded.
- [ ] AE-DR-007 and 008 source definitions externally confirmed because they affect first-class fields.
- [ ] Historical header/profile fields and H10 actor semantics frozen.
- [ ] Controlled profile, remarks, statutory mapping, status, and lineage codes approved.
- [ ] Existing non-empty metadata migration posture audited; no historical values invented.
- [ ] Rebuild, upgrade, replay, inventory, drift, wrong-family, immutability, and privacy proof designed.
- [ ] Generic JSON is prohibited as field or correction authority.

**Gate:** schema implementation cannot merge while required field/source definitions are absent.

## 4. Implement bounded generator

- [x] AE-DR-002A, 003A, 004A, 019A, 021, 022, and 024A user approvals recorded.
- [ ] AE-DR-006 and 009 equations externally confirmed.
- [ ] Approved schema and controlled codes merged.
- [ ] Deterministic XLSX profile and canonical validation JSON contracts versioned.
- [ ] Replay, conflict, restart, supersession, and unknown-outcome behavior frozen.
- [ ] All 42 fields have executable source/null/zero/format rules.
- [ ] Unsupported Diplomat/cross-period/exception paths fail closed.
- [ ] No-state-mutation and privacy test manifests specified.

**Gate:** current bounded implementation verdict remains blocked pending external formula/source confirmation.

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

## 7. Authorization sign-off

- Technical design authorized by/date/reference: `Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001`
- Schema implementation authorized by/date/reference: `________________`
- Bounded generator authorized by/date/reference: `________________`
- Controlled UAT authorized by/date/reference: `NOT AUTHORIZED`
- Production authorized by/date/reference: `NOT AUTHORIZED`
