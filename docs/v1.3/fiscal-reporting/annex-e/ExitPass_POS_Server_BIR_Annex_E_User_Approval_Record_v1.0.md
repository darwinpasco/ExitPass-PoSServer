# ExitPass POS Server BIR Annex E User Approval Record v1.0

## Instructions

This record contains the explicit ExitPass project approval received for all 13 recommendations. It is an ExitPass project decision, not BIR acceptance.

Allowed status entry: `APPROVE_RECOMMENDATION`, `SELECT_ALTERNATIVE`, `REJECT`, or `DEFER`.

## Recorded approval

- Approver: Darwin Pasco
- Role: ExitPass Product Owner / Technical Authority
- Approval date/time and timezone: 2026-08-06, Philippine Standard Time (UTC+08:00)
- Approval/reference ID: `Z-009B-USER-APPROVAL-001`
- Status: `APPROVE_RECOMMENDATION` for all 13 items
- Explicit statement: `These selections are ExitPass project decisions and do not claim BIR approval.`
- Signature/status: `APPROVED`

This approval authorizes bounded Annex E-1 technical design only. It does not authorize BIR acceptance, accounting or legal conclusions, Controlled UAT, production, E-2 through E-5, Electronic Journal, or POSLog implementation.

## Approval items

### AE-DR-001 - Initial profile

- Question: Is bounded Z-009 limited to E-1?
- Recommended: E-1 only as `pos-server-bir-annex-e1-rmo24-2023:v1`.
- Alternatives: E-1:E-3; all five; defer runtime.
- Impact: Defines privacy boundary, schema, API, and fixtures.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status: `APPROVE_RECOMMENDATION`
- Approver/date/reference: `Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001`
- Notes: `Bounded E-1 technical design only.`

### AE-DR-002A - Internal artifact

- Question: Which renderer is built while BIR acceptance remains external?
- Recommended: deterministic XLSX faithful to E-1; canonical JSON only for validation/replay.
- Alternatives: defer all rendering; another approved deterministic profile.
- Impact: Renderer dependency, golden fixtures, output identity.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Canonical JSON remains internal only.`

### AE-DR-003A - Workbook grouping

- Question: How are one-row-per-Z facts grouped into files?
- Recommended: one workbook per Site POS Server, fiscal identity, currency, and calendar month.
- Alternatives: one workbook per Z; another fixed approved period.
- Impact: Operation identity, source membership, filename, replay.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001`

### AE-DR-004A - Internal filename

- Question: What safe filename is used pending external prescription?
- Recommended: `ANNEX-E1_<FISCAL-ID-CODE>_<MIN>_<YYYYMM>_<PROFILE-VERSION>.xlsx`.
- Alternatives: another deterministic non-personal pattern; defer export.
- Impact: Download, collisions, audit, replay. TIN/taxpayer name are deliberately excluded.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Not represented as BIR-prescribed.`

### AE-DR-010A - Remarks

- Question: How is D32 represented internally?
- Recommended: governed codes only (`NONE`, `NO_ACTIVITY`, approved exceptions); no free text.
- Alternatives: blank-only; another approved controlled vocabulary.
- Impact: Controlled codes, privacy, deterministic output.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Arbitrary free text prohibited.`

### AE-DR-011 - NAAC/Solo Parent

- Question: How are D14/D15 represented before active workflows exist?
- Recommended: separate immutable classifications; explicit zero only from recorded absence; unknown blocks.
- Alternatives: block every workbook until active support; approved blank/N/A posture.
- Impact: Z/projection schema and classification tests.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Unknown values block generation.`

### AE-DR-016A - Bounded delivery

- Question: What delivery scope may initial runtime design include?
- Recommended: authorized local generation/download only; no portal, signing, encryption, email, or removable-media workflow.
- Alternatives: design-only with no download; defer runtime.
- Impact: API and security boundary.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Local authorized generation and download only.`

### AE-DR-017 - Corrections and replay

- Question: How are replay, regeneration, and corrections represented?
- Recommended: byte-identical replay; immutable superseding versions with original/corrected identity, reason, approval reference, and timestamps; no overwrite.
- Alternatives: regeneration only with corrections deferred; reject correction support.
- Impact: Schema, idempotency, audit, recovery.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Prior outputs are immutable.`

### AE-DR-018 - Export history

- Question: What durable export evidence is persisted?
- Recommended: immutable metadata/hash/profile/renderer/source-membership/lineage; bytes outside DB; no generic JSON authority.
- Alternatives: metadata without lineage; deterministic derivation only.
- Impact: Schema, restart recovery, retention.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Workbook bytes remain outside the relational database.`

### AE-DR-019A - Internal formatting

- Question: What deterministic formatting is used before examiner confirmation?
- Recommended: PHP-only v1, integer minor units, two decimals, `.`, no grouping separator, `YYYY-MM-DD`, explicit zero.
- Alternatives: another fixed approved invariant profile.
- Impact: Golden workbooks and numeric/date tests.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / External display acceptance remains pending.`

### AE-DR-021 - No-activity period

- Question: How is an empty committed Z represented?
- Recommended: emit a row with recorded zeros, equal GTA, unchanged reset, advanced Z, blank SI range, and `NO_ACTIVITY`.
- Alternatives: no workbook row; another approved N/A marker.
- Impact: Counter continuity and blank/zero rules.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001`

### AE-DR-022 - Ranges and gaps

- Question: How are unrepresentable multi-series/gap facts handled?
- Recommended: one range per row; unexplained gaps, multi-range, or lossy representation block generation.
- Alternatives: approved separate rows/sheets or companion schedule.
- Impact: Generator guards and error contract.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / Lossy representation prohibited.`

### AE-DR-024A - Layout and overflow

- Question: What technical layout policy applies?
- Recommended: preserve official geometry/order; never truncate; fail unrepresentable mandatory values; wrap only designated header cells.
- Alternatives: approved adaptive widths; stricter fixed-length validation.
- Impact: Renderer and visual/golden tests.
- Selection: `[x] APPROVE_RECOMMENDATION  [ ] SELECT_ALTERNATIVE  [ ] REJECT  [ ] DEFER`
- Status/approver/date/reference/notes: `APPROVE_RECOMMENDATION / Darwin Pasco / 2026-08-06 PHT / Z-009B-USER-APPROVAL-001 / No truncation or silent adaptive meaning changes.`

## Consolidated sign-off

- All 13 items completed: `[x]`
- Approver: `Darwin Pasco`
- Role: `ExitPass Product Owner / Technical Authority`
- Approval date/time and timezone: `2026-08-06, Philippine Standard Time (UTC+08:00)`
- Approval/reference ID: `Z-009B-USER-APPROVAL-001`
- Explicit statement: `These selections are ExitPass project decisions and do not claim BIR approval.`
- Signature/status: `APPROVED`
