# ExitPass POS Server BIR Annex E Validation and Acceptance Scenarios v1.0

## 1. Common rules

Every scenario uses synthetic data. A successful action is read-only with respect to fiscal documents, Z, counters, GTA, periods, and sequences. Audit records contain references and outcomes, not output payloads or customer data.

All project-owned decisions are approved under `Z-009B-USER-APPROVAL-001`. `PENDING_EXTERNAL` means authoritative external evidence is still required. A fail-closed scenario can be design-ready even when unsupported positive-value behavior remains out of scope.

## 2. Scenarios

| ID | Precondition / authoritative input | Action | Expected output and validation | Safe error | Audit / mutation | Readiness |
| --- | --- | --- | --- | --- | --- | --- |
| AE-AC-001 | Closed period, committed Z, complete approved profile, normal VATable facts | Generate E-1 | Exact H01:H10 and D01:D32; all reconciliations exact | None | `annex_e_generated`; no fiscal mutation | User gates AE-DR-001/002A/003A/004A/005A/010A/011/016A/017/018/019A/021/022/024A; external mandatory-field gates remain |
| AE-AC-002 | Committed empty-period Z | Generate | One zero row; blank SI range; equal GTA; advanced Z; controlled `NO_ACTIVITY` | None when authoritative zero facts are complete | Success/none | Approved AE-DR-010A/021 |
| AE-AC-003 | Z has VATable, VAT-exempt, zero-rated values | Generate | Copy recorded categories without tax recalculation | None if profile complete | Success/none | Ready after common gates |
| AE-AC-004 | Z has separate SC/PWD discount and VAT-removal children | Generate | D12/D13/D20/D21 exact; no identity | None | Success/none | Ready after common gates |
| AE-AC-005 | Z tenders contain cash, QRPH, card; sum equals net | Generate | No tender columns; tender validation passes | None | Success/none | Ready after common gates |
| AE-AC-006 | Same-period governed void | Generate | D18 copied; active sale facts remain Z-authoritative | None | Success/none | Formula gate AE-DR-006 |
| AE-AC-007 | Cancellation classification exists but no approved E-1 mapping | Generate | No file | `annex_e_unsupported_classification` | Denial/none | Ready fail-closed behavior under AE-DR-015 |
| AE-AC-008 | Nonzero refund source | Generate | No file; do not fold into Returns | `annex_e_unsupported_classification` | Denial/none | Ready fail-closed behavior under AE-DR-015 |
| AE-AC-009 | Nonzero return source | Generate | No file until sign/period/VAT mapping is separately governed | `annex_e_unsupported_classification` | Denial/none | Ready fail-closed behavior under AE-DR-015 |
| AE-AC-010 | Nonzero adjustment source | Generate | No file; no mapping to Others/Remarks | `annex_e_unsupported_classification` | Denial/none | Ready fail-closed behavior under AE-DR-015 |
| AE-AC-011 | Reprint events only after Z | Regenerate | Bytes unchanged; reprints do not add sales | None | Replay/read audit only | Ready after AE-DR-017/023 |
| AE-AC-012 | Training transaction in source | Generate | No file | `annex_e_unsupported_classification` | Denial/none | Ready fail-closed behavior under AE-DR-023 |
| AE-AC-013 | Classified range gap cannot fit one E-1 range | Generate | No file; no flattening or truncation | `annex_e_range_ambiguous` | Denial/none | Approved fail-closed AE-DR-022 |
| AE-AC-014 | Unexplained sequence gap | Generate | No file | `annex_e_unexplained_gap` | Denial/none | Source support ready |
| AE-AC-015 | Stored Z/profile misses mandatory field | Generate | No partial file | `annex_e_mandatory_field_missing` | Failure reference/none | Approved AE-DR-005A schema posture |
| AE-AC-016 | Unsupported statutory/tax controlled code | Generate | No file | `annex_e_unsupported_classification` | Denial/none | Needs mappings AE-DR-011/012 |
| AE-AC-017 | Caller scope differs from report Site | Generate/read | Hidden not found | `annex_e_not_found` | Denied scope/none | Runtime task |
| AE-AC-018 | Caller fiscal identity differs | Generate/read | Hidden not found | `annex_e_not_found` | Denied scope/none | Runtime task |
| AE-AC-019 | Same operation and semantics | Retry | Byte-identical output, filename, hash | None | Replay event/none | Approved AE-DR-017/018 |
| AE-AC-020 | Same operation, changed period/profile | Retry | No new file or mutation | `annex_e_semantic_conflict` | Conflict/none | Runtime task after contract |
| AE-AC-021 | API/process restart after durable generation | Retrieve | Identical bytes/hash | None | Read/replay audit only | Runtime task |
| AE-AC-022 | Caller requests wrong/open period | Generate | No file | `annex_e_period_not_closed` | Denial/none | Ready source rule |
| AE-AC-023 | Period is CLOSED but governing committed Z missing | Generate | No file | `annex_e_governing_z_missing` | Failure/none | Ready source rule |
| AE-AC-024 | Existing successful export for same Z/profile, different operation | Generate | Existing byte-identical output unless an approved correction creates supersession | `annex_e_semantic_conflict` when semantics differ | Replay or conflict only | Approved AE-DR-017/018 |
| AE-AC-025 | Late fiscal issuance attempted after close | Generate after denied issuance | Existing output unchanged | None | Read only | Ready Z boundary |
| AE-AC-026 | Approved post-close correction request | Generate correction | Immutable superseding output with reason and approval reference; prior output unchanged | `annex_e_correction_not_authorized` when authority absent | No prior mutation | Approved AE-DR-017 |
| AE-AC-027 | Recorded negative category requested | Generate | Fail unless approved field/sign supports it | `annex_e_invalid_amount` | Failure/none | `BLOCKED` AE-DR-015/019 |
| AE-AC-028 | Amount exactly half-minor-unit cannot occur in integer source | Generate | No floating rounding; source validation rejects malformed value | `annex_e_malformed_source` | Failure/none | Ready after AE-DR-019 |
| AE-AC-029 | Valid integer values requiring decimal formatting | Generate | Exact currency-aware two-decimal output after approval | None | Success/none | `BLOCKED` AE-DR-019 |
| AE-AC-030 | Header contains customer/statutory identity in wrong field | Generate | Reject; never serialize | `annex_e_privacy_violation` | Privacy denial without value/none | Runtime task |
| AE-AC-031 | Output filename components contain unsafe characters | Generate | AE-DR-004A deterministic sanitization; no traversal or TIN | None or invalid profile | Safe audit/none | Approved AE-DR-004A; external prescribed-name question AE-DR-004 |
| AE-AC-032 | Approved workbook profile | Generate | Exact package/worksheet/column order and encoding | None | Success/none | `BLOCKED` AE-DR-002/024 |
| AE-AC-033 | Same immutable inputs rendered twice | Generate/replay | Byte-for-byte equality and equal content hash | None | Replay/none | Runtime task |
| AE-AC-034 | Z gross/net/GTA or child reconciliation mismatch | Generate | No file; no auto-repair | `annex_e_reconciliation_failed` | Safe support ref/none | Runtime task |
| AE-AC-035 | Source snapshot version unsupported | Generate | No file | `annex_e_version_unsupported` | Safe support ref/none | Runtime task |
| AE-AC-036 | Source snapshot malformed | Generate | No file and no live recomputation | `annex_e_malformed_source` | Safe support ref/none | Runtime task |
| AE-AC-037 | One profile contains mixed currencies | Generate | No file | `annex_e_mixed_currency` | Failure/none | Runtime task |
| AE-AC-038 | Multiple fiscal ranges/series in one Z | Generate | Fail; do not flatten or create extra detail rows | `annex_e_range_ambiguous` | Failure/none | Approved AE-DR-022; row grain resolved AE-DR-003 |
| AE-AC-039 | Nonzero manual SI/OR amount without approved source | Generate | No file | `annex_e_source_unavailable` | Failure/none | `BLOCKED` AE-DR-007 |
| AE-AC-040 | Nonzero overrun/overflow or total income has no equation | Generate | No file | `annex_e_contract_unresolved` | Failure/none | `BLOCKED` AE-DR-008/009 |
| AE-AC-041 | E-2 request under E-1-only runtime | Generate | Reject wrong profile | `annex_e_profile_unsupported` | Denial/none | Expected if AE-DR-001 approves E-1 only |
| AE-AC-042 | Export read by unauthorized caller | Read/download | 403 or hidden 404 by scope posture | `forbidden`/`not_found` | Denial without payload/none | Runtime task |
| AE-AC-043 | Generation succeeds | Compare DB manifests | Only governed Annex metadata/export evidence changes | None | Audit allowed; fiscal state unchanged | Runtime task |
| AE-AC-044 | Generation fails after output buffer but before commit | Retry | No durable success; safe retry | `annex_e_transient_failure` | Failure record if governed; no fiscal mutation | Runtime task |
| AE-AC-045 | Exact traceability audit | Inspect output field map | All 42 fields resolve to source or explicit decision; no orphan | None | Validation evidence only | Documentation gate |

## 3. Acceptance suites

### Contract suite

- Assert all 42 E-1 fields exist in dictionary and mapping.
- Assert every field has a requirement classification, source location, type/width posture, null/zero posture, source, validation, privacy, readiness, and sample.
- Assert every `PENDING_EXTERNAL` reference resolves to the decision register.
- Assert all 35 decision records have exactly one authority class, resolution status, and blocking level.
- Assert all 13 project-owned decisions reference `Z-009B-USER-APPROVAL-001`, and every external gate appears in the External Confirmation Register.
- Assert no output format is called BIR-approved.

### Future runtime suite

- Unit: mapping, checked arithmetic, profile/version validation, deterministic workbook construction.
- API: authority, anti-enumeration, safe headers/errors, replay/conflict.
- PostgreSQL: committed-Z binding, scope, immutable metadata/history, rollback.
- Disposable end-to-end: generation, byte replay, restart, no-state-mutation, privacy/log scan.

## 4. Mutation manifest

Future proof must compare fiscal documents and children, statutory facts, tenders/tax/discount/totals, numbering and sequences, reporting periods, X/Z snapshots and children, Z counters/GTA/state transitions, reprints, and payment-adjacent rows. Annex E generation may add only approved Annex metadata, export identity/history, output reference, and privacy-safe audit evidence.
