# ExitPass POS Server Annex E-1 External Confirmation Closure Package v1.0

## 1. Purpose

This package provides one independent, copy-ready closure request for each of the ten unresolved Annex E-1 decisions after Z-012B. No request is pre-approved. Responses must identify the decision ID, selected answer, authority, date, and retained evidence. Silence, implementation behavior, or successful automated testing is not approval.

## 2. BIR or examiner confirmations

### AE-DR-002 - official electronic artifact

- **Question:** Is deterministic XLSX faithful to the RMO 24-2023 Annex E-1 workbook accepted as the official electronic artifact, and are PDF, JSON, or another companion artifact required or permitted?
- **Current governed interpretation:** ExitPass generates deterministic XLSX as its internal official-profile artifact. Canonical JSON is internal validation/replay evidence only. CSV/PDF are not official artifacts.
- **Allowed evidence-based options:** XLSX alone; XLSX plus specifically identified companion formats; another prescribed artifact with exact specification.
- **Recommendation:** Accept deterministic XLSX alone unless an official companion is required.
- **Impact:** Blocks Controlled UAT execution against the official artifact claim, external delivery/submission, and Production acceptance. It does not block preparation.
- **Approval wording:** `For AE-DR-002, BIR/accreditation examiner confirms that [SELECTED ARTIFACT AND COMPANIONS] is the accepted Annex E-1 electronic artifact set for profile pos-server-bir-annex-e1-rmo24-2023:v1.`
- **Approver/evidence:** BIR or assigned accreditation examiner; retain signed minutes, written response, or controlled correspondence.
- **No response:** Keep output internally labeled; no UAT execution, delivery, submission, or Production acceptance claim.

### AE-DR-004 - filename convention

- **Question:** Is an Annex E-1 filename prescribed, including allowed characters and correction/duplicate naming?
- **Current governed interpretation:** `ANNEX-E1_<FISCAL-ID-CODE>_<MIN>_<YYYYMM>_<PROFILE-VERSION>.xlsx` is an internal deterministic pattern, not a BIR prescription.
- **Allowed options:** Confirm the internal pattern; prescribe an exact replacement; confirm no prescribed filename.
- **Recommendation:** Confirm no additional prescription or provide the exact required pattern.
- **Impact:** Blocks Controlled UAT filename acceptance, external delivery, and Production. It does not block preparation.
- **Approval wording:** `For AE-DR-004, BIR/accreditation examiner confirms that Annex E-1 filenames shall use [EXACT PATTERN / NO PRESCRIBED PATTERN], with [CHARACTER AND CORRECTION RULES].`
- **Approver/evidence:** BIR or assigned accreditation examiner; retain written rule and examples.
- **No response:** Keep the internal sanitized filename and do not represent it as prescribed.

### AE-DR-010 - Remarks values and blanks

- **Question:** Which D32 Remarks values are accepted, and when is blank permitted?
- **Current governed interpretation:** Only governed codes `NONE`, `NO_ACTIVITY`, and separately approved exception codes; no arbitrary text.
- **Allowed options:** Accept those codes; prescribe an exact code list; prescribe blank for identified cases.
- **Recommendation:** Accept `NONE` and `NO_ACTIVITY`, with future exception codes approved individually.
- **Impact:** Blocks Controlled UAT output acceptance, external delivery, and Production. It does not block preparation.
- **Approval wording:** `For AE-DR-010, BIR/accreditation examiner approves D32 values [EXACT LIST] and permits blank only when [EXACT CONDITION].`
- **Approver/evidence:** BIR or assigned accreditation examiner; retain approved values and worked rows.
- **No response:** Continue governed internal codes only; no free text; execution remains blocked.

### AE-DR-011A - inactive NAAC and Solo Parent representation

- **Question:** Are explicit zeros accepted in D14/D15 when immutable scoped evidence proves no NAAC/Solo Parent amount, or must the cells be blank/N/A or actively supported?
- **Current governed interpretation:** Explicit zero is emitted only from governed `attested_zero`; unknown or missing blocks generation.
- **Allowed options:** Explicit zero; blank/N/A under exact conditions; mandatory active classification support.
- **Recommendation:** Accept explicit zero when backed by immutable scoped evidence.
- **Impact:** Blocks Controlled UAT execution involving these columns and Production acceptance. Known-zero evidence safely supports preparation but does not resolve acceptance.
- **Approval wording:** `For AE-DR-011A, BIR/accreditation examiner confirms that D14 and D15 shall contain [ZERO / BLANK / N/A / OTHER] when immutable period evidence proves no applicable amount.`
- **Approver/evidence:** BIR or assigned accreditation examiner; retain written rule and zero/nonzero examples.
- **No response:** Unknown and nonzero unsupported values fail closed; UAT execution remains blocked.

### AE-DR-016 - signing, encryption, compression, and channel

- **Question:** Are signing, encryption, compression, or a prescribed submission/delivery channel mandatory?
- **Current governed interpretation:** Authorized local generation/download only; no signing, encryption, compression policy, portal, email, removable media, or external transfer.
- **Allowed options:** No additional controls for local artifact; provide exact cryptographic/compression requirements; provide exact channel protocol.
- **Recommendation:** Confirm local unsigned/uncompressed XLSX is sufficient for local review and separately specify any submission controls.
- **Impact:** Does not block bounded preparation or strictly local automated review; blocks any UAT artifact transfer, external delivery/submission, and Production external workflow.
- **Approval wording:** `For AE-DR-016, BIR/accreditation examiner confirms that Annex E-1 requires [SIGNING], [ENCRYPTION], [COMPRESSION], and delivery through [CHANNEL], with the attached exact technical specification.`
- **Approver/evidence:** BIR or assigned accreditation examiner; retain formal delivery specification.
- **No response:** Local download only; no external transfer or submission.

### AE-DR-020A - H08 POS Terminal Number

- **Question:** For one Site POS Server with multiple child channels, may H08 contain the governed Site POS Server fiscal terminal code, or is another exact identifier required?
- **Current governed interpretation:** H08 snapshots the Site POS Server code. Channel labels are never concatenated or substituted.
- **Allowed options:** Accept Site POS Server code; require MIN/registered terminal identifier; prescribe another immutable first-class source.
- **Recommendation:** Accept the Site POS Server fiscal terminal code for the single fiscal authority.
- **Impact:** Blocks Controlled UAT header acceptance and Production. It does not block preparation.
- **Approval wording:** `For AE-DR-020A, BIR/accreditation examiner confirms that H08 POS Terminal Number shall contain [EXACT AUTHORITATIVE IDENTIFIER] for the ExitPass Site POS Server architecture.`
- **Approver/evidence:** BIR or assigned accreditation examiner; retain mapping to registered machine/fiscal identity evidence.
- **No response:** Keep the internal Site POS Server code; make no examiner-accepted claim.

### AE-DR-024 - template geometry and wrapping

- **Question:** Must the exact official geometry be preserved, and which cells may wrap or overflow?
- **Current governed interpretation:** Preserve `E-1`, `A1:AF16`, order, labels, merges, widths/heights, and print setup; wrapping only in designated header cells; never truncate; unrepresentable mandatory values fail.
- **Allowed options:** Accept exact current geometry; approve identified wrapping exceptions; prescribe a revised official template.
- **Recommendation:** Accept the preserved official geometry and designated header wrapping with fail-closed overflow.
- **Impact:** Blocks Controlled UAT visual acceptance, external delivery, and Production. It does not block preparation.
- **Approval wording:** `For AE-DR-024, BIR/accreditation examiner approves the attached Annex E-1 golden workbook geometry and permits wrapping only in [EXACT CELLS], with mandatory overflow handled by [EXACT RULE].`
- **Approver/evidence:** BIR or assigned accreditation examiner; retain approved golden workbook/hash and rendered examples.
- **No response:** Preserve current geometry; reject unrepresentable values; no execution/acceptance claim.

## 3. Accounting or joint Accounting/BIR confirmation

### AE-DR-012 - Diplomat and other VAT privileges

- **Question:** Where must nonzero Diplomat and other unresolved VAT-privilege amounts map among D16, D22, D24, or another approved profile, and how are discount and VAT-removal components separated?
- **Current governed interpretation:** No nonzero mapping is authorized. Explicit scoped zero evidence is accepted for the bounded path; nonzero data fails closed and is never coerced.
- **Allowed options:** Provide exact field mapping and worked equation; require an extension/new profile; declare a supported category not applicable with authoritative basis.
- **Recommendation:** Approve an explicit field-by-field mapping with tax/discount separation and worked examples before enabling nonzero data.
- **Impact:** Does not block zero-only preparation. Blocks every nonzero privilege UAT scenario, nonzero external artifact, and Production nonzero support.
- **Approval wording:** `For AE-DR-012, Accounting and the applicable BIR authority approve the attached mapping of [PRIVILEGE TYPE] to [D-POSITIONS], including [DISCOUNT/VAT COMPONENT EQUATIONS], effective for [PERIOD/SCOPE].`
- **Approver/evidence:** Accounting plus BIR/examiner when regulatory placement is involved; retain signed mapping and reconciled examples.
- **No response:** Require immutable `attested_zero`; reject any nonzero unresolved privilege fact.

### AE-DR-019 - number and date display profile

- **Question:** Are PHP, two decimal places, period decimal separator, no thousands separator, `YYYY-MM-DD`, explicit zero, and the governed negative-value rules accepted?
- **Current governed interpretation:** That invariant profile is used internally; it is not claimed as examiner-approved.
- **Allowed options:** Accept the internal profile; prescribe exact alternative formats for every affected field.
- **Recommendation:** Accept the invariant internal profile because committed minor units remain unchanged.
- **Impact:** Blocks Controlled UAT display acceptance and Production; does not block preparation.
- **Approval wording:** `For AE-DR-019, Accounting/BIR examiner approves PHP monetary display as [FORMAT], dates as [FORMAT], zero as [FORMAT], and negative values as [FORMAT] for Annex E-1.`
- **Approver/evidence:** Accounting and/or assigned examiner as required; retain approved golden rows and edge cases.
- **No response:** Continue internal deterministic formatting; do not claim official acceptance.

## 4. Legal/Compliance/Records confirmation

### AE-DR-016B - retention, archive, and deletion hold

- **Question:** What retention period, archive controls, legal holds, and deletion evidence govern Annex E-1 metadata and artifacts?
- **Current governed interpretation:** Preserve immutable evidence; no destructive purge or archive worker is authorized.
- **Allowed options:** Approve a documented schedule and hold process; require indefinite preservation pending another policy; prescribe a statutory schedule.
- **Recommendation:** Approve a records schedule before Production and separately authorize any purge implementation.
- **Impact:** Does not block bounded preparation or non-destructive local UAT. Blocks Production retention operations and destructive deletion.
- **Approval wording:** `For AE-DR-016B, Legal/Compliance/Records approves Annex E-1 retention for [DURATION], archive controls [CONTROLS], legal-hold behavior [RULE], and deletion evidence [EVIDENCE].`
- **Approver/evidence:** Legal/Compliance and designated records owner; retain approved policy and authority record.
- **No response:** Preserve all committed metadata/artifacts; no purge or destructive archive transition.

## 5. Closure rules

1. Record each response independently against its exact AE-DR ID.
2. Preserve the original question, selected option, approver role, decision date, evidence reference, and effective scope.
3. Do not infer approval for another decision.
4. Any answer requiring changed calculations, field meaning, template, source authority, security policy, or runtime behavior requires a separately authorized implementation task and renewed validation.
5. Controlled UAT execution requires a separate authorization record after the execution-blocking decisions are reconciled. External delivery, submission, and Production require their own later gates.

