# ExitPass POS Server BIR Annex E Decision Register v1.0

## 1. Purpose

This register preserves AE-DR-001 through AE-DR-024 and adds only the sub-decisions needed to separate regulatory confirmation from project-owned choices. No recommendation is user-approved by this document. No status asserts BIR approval unless the cited existing source is explicit.

Authority classes and resolution statuses are limited to the vocabularies defined by Z-009B. Blocking level identifies the implementation stage affected; resolution status separately identifies whether approval remains outstanding.

## 2. Decision inventory

| ID | Exact question / affected fields | Options and selected or recommended option | Authority class | Sources / rationale / rejected alternatives | Schema, runtime, and tests | Resolution status | Remaining approver | Blocking level |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AE-DR-001 | Initial profile; all E-1 fields and E-2:E-5 scope | **E-1 only** as `pos-server-bir-annex-e1-rmo24-2023:v1`; reject bundling E-2:E-5 | `PRODUCT_OWNER_DECISION` | AE-SRC-001, 013, 014; approved by `Z-009B-USER-APPROVAL-001`. E-1 is Z-backed summary; other profiles are identity-bearing transaction books. | Bounds contract, projection, API, fixtures | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-002 | Is XLSX accepted as the official electronic Annex E artifact? | XLSX, PDF, JSON, CSV, or prescribed alternative; **no internal selection can establish BIR acceptance** | `EXAMINER_CONFIRMATION_REQUIRED` | AE-SRC-001 supplies XLSX; AE-SRC-015 leaves mandatory formats open. Reject calling Z-008 JSON/CSV official. | Accreditation tests and output labeling | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accreditation examiner | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-002A | Internal official-profile renderer for E-1 | **Deterministic XLSX faithful to AE-SRC-001**; canonical JSON is validation-only; reject CSV as official | `TECHNICAL_ARCHITECTURE_DECISION` | Strongest available physical source is XLSX; preserves exact worksheet structure; approved by `Z-009B-USER-APPROVAL-001`. | XLSX renderer/profile tests; no schema by itself | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-003 | Detail-row aggregation grain; D01:D32 | **One physical row per committed governing Z and fiscal identity** | `TECHNICAL_ARCHITECTURE_DECISION` | AE-SRC-016/017 bind Annex metadata and closed facts to one Z. Reject live daily recomputation. | Z FK, row projection, one-row traceability tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-003A | Workbook grouping period | **One workbook per Site POS Server, fiscal identity, currency, and calendar month; rows ordered by Z period sequence** | `PRODUCT_OWNER_DECISION` | AE-SRC-001 supports multiple rows but gives no grouping period. Monthly grouping is an ExitPass decision approved by `Z-009B-USER-APPROVAL-001`, not a regulatory claim. | Group identity, monthly query/read model, ordering tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-004 | Is there a prescribed BIR filename? | Prescribed pattern or none; **request confirmation** | `EXAMINER_CONFIRMATION_REQUIRED` | No local source specifies a filename. Reject claiming an internal pattern is required. | UAT evidence only unless prescribed pattern changes design | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accreditation examiner | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-004A | Safe internal filename pending confirmation | **`ANNEX-E1_<FISCAL-ID-CODE>_<MIN>_<YYYYMM>_<PROFILE-VERSION>.xlsx`** | `OPERATIONAL_DECISION` | Avoid TIN and taxpayer name in filenames; sanitize ASCII `[A-Z0-9_-]`; approved by `Z-009B-USER-APPROVAL-001` without claiming BIR prescription. | Filename helper, collision, traversal, replay tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-005 | Are the 10 E-1 header positions part of the profile? H01:H10 | **All 10 labeled positions are present and retained**; value nullability is separate | `REGULATORY_EXPLICIT` | AE-SRC-001 E-1 header. Reject omitting labels because a current table lacks a source. | Field/profile completeness tests | `APPROVED_BY_EXISTING_SOURCE` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-005A | How are header values and UserID sourced? H01:H10 | **Immutable historical profile snapshot; H10 server-derived privacy-safe actor; unknown mandatory value blocks** | `TECHNICAL_ARCHITECTURE_DECISION` | AE-SRC-016/017 immutability and privacy boundary. Reject mutable current config and credential text. | Header projection schema, actor mapping, replay tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `BLOCKS_SCHEMA_IMPLEMENTATION` |
| AE-DR-006 | Official mapping/formulas for D07, D16, D19, D25, D26, D27 and affected operands | **Use the exact named-operand profile:** `D07=ACTIVE_GROSS+RETURN_AMOUNT+VOID_AMOUNT`; `D16=OTHER_STATUTORY_DISCOUNT+COUPON_DISCOUNT+PROMOTIONAL_DISCOUNT`; `D19=D12+D13+D14+D15+D16+D17+D18`; `D25=D20+D21+D22+D23+D24`; `D26=D09-D22`; `D27=D07-D19-D09` | `ACCOUNTING_APPROVAL_REQUIRED` | Exact immutable profile SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f`; approved by `Z-012B-ACCOUNTING-APPROVAL-001`. Reject field-number substitution, VAT-inclusive Z net as D27, or any unnamed operand. | Implement exact profile sources, checked PHP minor-unit arithmetic, zero-tolerance reconciliations, and fail-closed inputs | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-007 | Manual SI/OR meaning/source; D06 | **D06 is VAT-exclusive net income of qualifying continuity Manual SI/OR originally issued in the governed period; later encoding does not move period attribution; electronic/fiscalized duplicates, drafts, cancellations, and duplicate encoding are excluded; missing is not zero** | `ACCOUNTING_APPROVAL_REQUIRED` | Exact immutable profile and first-class fact contract approved by `Z-012B-ACCOUNTING-APPROVAL-001`. Reject count/range substitution, current-entry-date attribution, or absent-row zero. | Implement immutable scoped period fact, `RECORDED`/`ATTESTED_ZERO`, attestation, semantic hash, correction lineage, source binding, and reconciliation | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-008 | Sales Overrun/Overflow meaning/source; D28 | **D28 is VAT-exclusive net income omitted from regular electronic sales solely because an approved accumulated-sales capacity boundary was reached; rounding, cash overage, SI-number exhaustion, counter rollover, transaction volume, manual sales, and late posting are excluded; missing is not zero** | `ACCOUNTING_APPROVAL_REQUIRED` | Exact immutable profile and first-class fact/event contract approved by `Z-012B-ACCOUNTING-APPROVAL-001`. Reject inferred variance and absent-row zero. | Implement immutable scoped period fact/event, `RECORDED`/`ATTESTED_ZERO`, attestation, semantic hash, correction lineage, source binding, and reconciliation | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-009 | Total Income equation; D29 | **`D29=D27+D06+D28`: VAT-exclusive regular electronic net income plus qualifying Manual SI/OR net income plus approved overrun/overflow net income** | `ACCOUNTING_APPROVAL_REQUIRED` | Exact immutable profile approved by `Z-012B-ACCOUNTING-APPROVAL-001`. Reject gross, net alone, net-plus-VAT, tender/payment, and GTA bases. | Implement exact named inputs, checked PHP minor-unit sum, immutable source binding, and `D29-D06-D28=D27` with zero tolerance | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-010 | What Remarks content is accepted? D32 | Blank, controlled codes, or text; **seek confirmation; reject arbitrary free text** | `EXAMINER_CONFIRMATION_REQUIRED` | Source has Remarks column without value contract. | UAT profile acceptance | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accreditation examiner | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-010A | Internal Remarks storage/rendering | **Controlled codes only: `NONE`, `NO_ACTIVITY`, and separately approved exception codes; blank only when profile permits** | `TECHNICAL_ARCHITECTURE_DECISION` | Deterministic, privacy-safe, and reconcilable; approved by `Z-009B-USER-APPROVAL-001`. Reject notes/metadata payloads. | Controlled codes, FK, renderer tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-011 | NAAC/Solo Parent E-1 posture; D14/D15 | **Separate immutable classifications; explicit zero only from recorded absence; unknown blocks** | `PRODUCT_OWNER_DECISION` | BRD requires extensibility but workflows are future; approved by `Z-009B-USER-APPROVAL-001`. Reject deriving from `other_statutory`. | Z/projection extension, code mapping, zero tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-011A | Does examiner accept explicit zero for unavailable NAAC/Solo Parent scope? D14/D15 | Zero, blank/N/A, or mandatory active support; **request confirmation** | `EXAMINER_CONFIRMATION_REQUIRED` | Workbook has physical columns but no null rule. | Controlled UAT fixture/profile evidence | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accreditation examiner | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-012 | Diplomat/other VAT mapping; D16, D22, D24 | VAT Others, Discount Others, extension, or separate profile; **retain VAT treatment and fail closed when nonzero until confirmed** | `ACCOUNTING_APPROVAL_REQUIRED` | BRD REP-011/012 explicitly leaves exact treatment open. Reject ordinary-discount coercion. | Mapping codes and nonzero fail-closed tests | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accounting authority | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-013 | E-2/E-3 applicability and identity authority | Separate privacy/legal task; **defer from E-1** | `DEFERRED_OUT_OF_SCOPE` | E-2/E-3 require names, IDs, TIN. Approved POS statutory snapshot excludes them. | No E-1 schema/runtime impact | `DEFERRED` | Legal, Privacy, BIR, Product before future task | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-014 | E-4/E-5 applicability and identity authority | Separate future entitlement/privacy tasks; **defer from E-1** | `DEFERRED_OUT_OF_SCOPE` | Future workflows and highly sensitive athlete/parent/child data. | No E-1 schema/runtime impact | `DEFERRED` | Legal, Privacy, BIR, Product before future task | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-015 | Void/cancel/refund/return/adjustment treatment | **Use governed same-period void; all unsupported/cross-period categories fail closed; never equate categories** | `TECHNICAL_ARCHITECTURE_DECISION` | Z-007A/Z-007 already freeze this boundary. Reject folding all exceptions into Returns/Voids. | Existing Z facts; fail-closed generator tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None for bounded E-1 | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-016 | Official signing, encryption, compression, and delivery requirements | **Request official/examiner confirmation; no silence-as-approval** | `EXAMINER_CONFIRMATION_REQUIRED` | Local package defines none. Reject production submission assumptions. | Extension interfaces; Controlled UAT evidence | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accreditation examiner | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-016A | Bounded initial delivery scope | **Local authorized generation/download only; no portal, signing, encryption, email, removable-media, or assumed compression workflow** | `TECHNICAL_ARCHITECTURE_DECISION` | Separates safe design from unresolved production delivery; approved by `Z-009B-USER-APPROVAL-001`. | API boundary and exclusion tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-016B | Production retention duration and archive controls | Configurable metadata now; **formal retention before production** | `LEGAL_APPROVAL_REQUIRED` | BRD requires confirmed long-term retention but gives no exact Annex E duration. | Retention metadata/schema; worker remains separate | `REQUIRES_EXTERNAL_CONFIRMATION` | Legal/Compliance/Records owner | `BLOCKS_PRODUCTION_ONLY` |
| AE-DR-017 | Replay, regeneration, correction lineage | **Byte-identical replay; immutable superseding output with original/corrected IDs, reason, approval ref, timestamps; no overwrite** | `TECHNICAL_ARCHITECTURE_DECISION` | Approved immutability/audit principles and `Z-009B-USER-APPROVAL-001`. Reject mutable replacement. | Lineage schema, semantic identity, replay/conflict tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-018 | Durable export history/hash | **Persist immutable privacy-safe metadata, profile/renderer version, filename, final-byte hash, source Z membership, lineage; keep workbook bytes outside the relational DB** | `TECHNICAL_ARCHITECTURE_DECISION` | Existing generic export posture is insufficient and JSON context is not authority; approved by `Z-009B-USER-APPROVAL-001`. | Hardened first-class schema, uniqueness, restart tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-019 | Official number/date format acceptance | Decimal/date/separator/currency profile; **seek accounting/examiner confirmation** | `ACCOUNTING_APPROVAL_REQUIRED` | Workbook cells use General; no normative serialization. | Controlled UAT golden workbook | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accounting authority | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-019A | Internal deterministic formatting | **PHP-only v1; integer minor units; 2 decimals; `.` decimal; no thousands separator; ISO `YYYY-MM-DD`; explicit zero; no floating point** | `TECHNICAL_ARCHITECTURE_DECISION` | Matches recorded arithmetic and deterministic output; approved by `Z-009B-USER-APPROVAL-001`. Reject locale-dependent formatting. | Serializer and edge-case tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-020 | E-1 fiscal scope and channel aggregation; H08 | **Site POS Server + fiscal identity + currency; all child channels combined; H08 uses governed Site POS Server fiscal terminal identity** | `TECHNICAL_ARCHITECTURE_DECISION` | Approved architecture makes channels children, not fiscal authorities. Reject per-channel recomputation. | Scope FK/profile, cross-scope tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None internally | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-020A | Examiner acceptance of Site POS Server fiscal terminal identity in H08 | One fiscal terminal ID versus channel IDs; **request confirmation** | `EXAMINER_CONFIRMATION_REQUIRED` | Workbook asks for one POS Terminal No.; architecture has many channels. | Controlled UAT header evidence | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accreditation examiner | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-021 | No-activity Z row; D02/D03/D32 | **Emit row: all recorded amounts zero, GTA unchanged, reset unchanged, Z advanced, SI range blank, Remarks `NO_ACTIVITY`** | `PRODUCT_OWNER_DECISION` | Preserves every Z close and counter continuity; approved by `Z-009B-USER-APPROVAL-001`. Reject dropping empty periods or inventing SI values. | Zero-row renderer and reconciliation tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-022 | Multi-series and gap representation; D02/D03/D32 | **One representable fiscal range per row; unexplained gap blocks; multi-range or unrepresentable classified gap blocks rather than flattening** | `TECHNICAL_ARCHITECTURE_DECISION` | E-1 has one range and no gap column; approved by `Z-009B-USER-APPROVAL-001`. Reject lexical range or lossy Remarks. | Range/gap guards and failure tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-023 | Reprints/training in E-1 | **Reprints excluded from sales; training unsupported and fails closed; neither gets an E-1 row** | `TECHNICAL_ARCHITECTURE_DECISION` | Existing report aggregation excludes reprints; no training source/profile exists. | Regression and unsupported classification tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |
| AE-DR-024 | Examiner acceptance of exact geometry/wrapping | Exact template, semantic structure, or adaptive layout; **request confirmation** | `EXAMINER_CONFIRMATION_REQUIRED` | Source supplies geometry but no overflow rule. | Controlled UAT visual/golden evidence | `REQUIRES_EXTERNAL_CONFIRMATION` | BIR/accreditation examiner | `BLOCKS_CONTROLLED_UAT` |
| AE-DR-024A | Internal layout/overflow policy | **Preserve official template geometry/order; no truncation; fail on unrepresentable mandatory value; wrapping only in designated header cells** | `TECHNICAL_ARCHITECTURE_DECISION` | Deterministic and audit-safe; approved by `Z-009B-USER-APPROVAL-001`. Reject adaptive column changes or hidden overflow. | Golden workbook, long-value, visual regression tests | `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | None | `DOES_NOT_BLOCK_INITIAL_E1` |

## 3. Affected-document inventory

Abbreviations: `SA` source/applicability, `FD` field dictionary, `MM` mapping matrix, `CR` calculation/reconciliation, `FL` file layout, `SO` samples, `VA` validation scenarios, `RH` runtime handoff, `UA` user approval, and `EC` external confirmation.

| Decision | Affected documents |
| --- | --- |
| AE-DR-001 | SA, FD, FL, SO, VA, RH, UA |
| AE-DR-002 | SA, FL, VA, RH, EC |
| AE-DR-002A | FL, SO, VA, RH, UA |
| AE-DR-003 | SA, FD, MM, CR, SO, VA, RH |
| AE-DR-003A | SA, MM, FL, SO, VA, RH, UA |
| AE-DR-004 | FL, VA, RH, EC |
| AE-DR-004A | FL, VA, RH, UA |
| AE-DR-005 | SA, FD, MM, VA, RH |
| AE-DR-005A | FD, MM, CR, VA, RH |
| AE-DR-006 | FD, MM, CR, SO, VA, RH, EC |
| AE-DR-007 | FD, MM, CR, SO, VA, RH, EC |
| AE-DR-008 | FD, MM, CR, SO, VA, RH, EC |
| AE-DR-009 | FD, MM, CR, SO, VA, RH, EC |
| AE-DR-010 | FD, FL, SO, VA, RH, EC |
| AE-DR-010A | FD, MM, FL, SO, VA, RH, UA |
| AE-DR-011 | FD, MM, CR, SO, VA, RH, UA |
| AE-DR-011A | FD, SO, VA, RH, EC |
| AE-DR-012 | FD, MM, CR, SO, VA, RH, EC |
| AE-DR-013 | SA, FD, MM, VA, RH, EC |
| AE-DR-014 | SA, FD, MM, VA, RH, EC |
| AE-DR-015 | SA, FD, MM, CR, SO, VA, RH |
| AE-DR-016 | SA, FL, VA, RH, EC |
| AE-DR-016A | FL, VA, RH, UA |
| AE-DR-016B | FL, VA, RH, EC |
| AE-DR-017 | SA, MM, CR, FL, SO, VA, RH, UA |
| AE-DR-018 | SA, MM, FL, VA, RH, UA |
| AE-DR-019 | FD, CR, FL, SO, VA, RH, EC |
| AE-DR-019A | FD, CR, FL, SO, VA, RH, UA |
| AE-DR-020 | SA, FD, MM, CR, VA, RH |
| AE-DR-020A | SA, FD, MM, VA, RH, EC |
| AE-DR-021 | FD, CR, FL, SO, VA, RH, UA |
| AE-DR-022 | SA, FD, MM, CR, FL, SO, VA, RH, UA |
| AE-DR-023 | SA, CR, SO, VA, RH |
| AE-DR-024 | FD, FL, VA, RH, EC |
| AE-DR-024A | FD, FL, VA, RH, UA |

## 4. Inventory totals

| Category | Count |
| --- | ---: |
| Original AE-DR decisions preserved | 24 |
| Added bounded sub-decisions | 11 |
| Total decision records | 35 |
| `APPROVED_BY_EXISTING_SOURCE` | 1 |
| `RESOLVED_BY_EXISTING_EXITPASS_DECISION` | 22 |
| `RECOMMENDED_FOR_USER_APPROVAL` | 0 |
| `REQUIRES_EXTERNAL_CONFIRMATION` | 10 |
| `DEFERRED` | 2 |
| `REJECTED` | 0 |
| `SUPERSEDED` | 0 |

## 5. Approval rule

The [User Approval Record](ExitPass_POS_Server_BIR_Annex_E_User_Approval_Record_v1.0.md) records approval `Z-009B-USER-APPROVAL-001` for all 13 project-owned recommendations. External evidence remains tracked separately in the [External Confirmation Register](ExitPass_POS_Server_BIR_Annex_E_External_Confirmation_Register_v1.0.md); this project approval does not satisfy any external decision.

## 6. Z-012A runtime revalidation

Z-012A reviewed all 35 records on 2026-08-10 PHT against official workbook evidence, merged Z-010 BIR Sales Summary behavior, and merged Z-011A Electronic Journal behavior at baseline `227cdc708d1a685cd56986f084d0cc1aad3e81dd`.

The inventory and original resolution statuses remain unchanged: 13 project-owned recommendations are approved, 14 external confirmations remain unresolved, and AE-DR-013/014 remain deferred. The stage impact is refined as follows:

| Decision set | Authoritative Z-012A classification | Runtime gate |
|---|---|---|
| AE-DR-006, AE-DR-007, AE-DR-008, AE-DR-009 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_INITIAL_GENERATOR`; no complete ordinary or no-activity row can be emitted without invention |
| AE-DR-012 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_NONZERO_PRIVILEGE_PATH`; an eventual ordinary path must reject nonzero unresolved privileges |
| AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-016, AE-DR-020A, AE-DR-024 | `REQUIRES_BIR_OR_EXAMINER_CONFIRMATION` | Applied at the schema, nonzero privilege, Controlled UAT, external delivery, and Production gates recorded in the Z-012A review |
| AE-DR-019 | `REQUIRES_ACCOUNTING_APPROVAL` | `BLOCKING_CONTROLLED_UAT`, `BLOCKING_PRODUCTION` |
| AE-DR-016B | `REQUIRES_LEGAL_COMPLIANCE_APPROVAL` | `BLOCKING_PRODUCTION` |
| AE-DR-013, AE-DR-014 | `DEFERRED_NONBLOCKING` | E-2 through E-5 only |

The resulting generator decision is `BLOCKED_PENDING_ACCOUNTING_CONFIRMATION`. See [Z-012A Runtime Authorization Revalidation](ExitPass_POS_Server_Z012A_Annex_E1_Runtime_Authorization_Revalidation_v1.0.md) for the exact 42-position map and each external gate.

## 7. Z-012A1 exact-profile proposal

On 2026-08-10, Accounting authority and approval in principle were confirmed for AE-DR-006 through AE-DR-009. Z-012A1 converts the prior alternatives into one implementation-ready recommendation in the [Accounting Calculation Profile Proposal](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md), with selection captured by the [Accounting Approval Form](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Approval_Form_v1.0.md).

This evidence does not change the four decision rows above. They remain `REQUIRES_EXTERNAL_CONFIRMATION` with Accounting as approver until Accounting selects `APPROVED_EXACTLY_AS_SPECIFIED` for the identified profile hash. The proposal's statuses are `ACCOUNTING_AUTHORITY_AND_APPROVAL_IN_PRINCIPLE_CONFIRMED`, `PENDING_EXACT_PROFILE_APPROVAL`, and `EXECUTABLE_CALCULATION_PROFILE_NOT_YET_APPROVED`.

## 8. Z-012A2 Accounting approval and current gate

Accounting selected `APPROVED_EXACTLY_AS_SPECIFIED` on 2026-08-10 for the exact proposal filename, version `v1.0`, Annex E profile, decision scope, and SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f`. Approval record `Z-012B-ACCOUNTING-APPROVAL-001` supersedes the pending executable-profile status without modifying the approved proposal.

AE-DR-006 through AE-DR-009 are therefore resolved. The active external-confirmation count is now 10. The approved first-class facts, zero attestations, source bindings, calculations, and tests are implementation requirements for Z-012B rather than new decision gates. The current local bounded-runtime decision is `AUTHORIZED_FOR_Z012B_LOCAL_BOUNDED_RUNTIME`; nonzero unresolved privilege facts remain fail closed, and Controlled UAT, external delivery, Production, E-2 through E-5, and ARTS POSLog remain unauthorized.
