# ExitPass POS Server BIR Annex E File Layout and Export Specification v1.0

## 1. Contract status

The source-grounded physical profile is the E-1 worksheet in AE-SRC-001. The mandatory electronic file format is **`UNRESOLVED`**. This document records the workbook layout and proposes a deterministic `.xlsx` artifact for approval; it does not declare that BIR accepts an electronic workbook.

## 2. Formats found and authority

| Format | Evidence | Classification | Z-009 posture |
| --- | --- | --- | --- |
| Spreadsheet/printed worksheet | AE-SRC-001 is `.xlsx` with E-1 through E-5 sheets, print areas, widths, headings, and cells | `EXPLICITLY_ALLOWED` as supplied template; mandatory electronic status unresolved | Recommended external profile after approval |
| Print/PDF/JSON | AE-SRC-015 line 569 describes BIR Sales Summary supported output semantics | `EXISTING_EXITPASS_DECISION`, but exact final layout remains open at line 571 | Optional companion outputs, not substitutes for E-1 |
| Canonical JSON/CSV/text | Z-008 internal output contracts | `NOT_APPLICABLE` as Annex E authority | May support diagnostics only; cannot be labeled Annex E submission |
| Fixed-width/XML | No source found | `UNRESOLVED` | Not implemented |

## 3. Recommended profile

| Property | Recommendation | Status / gate |
| --- | --- | --- |
| Contract ID | `pos-server-bir-annex-e1-rmo24-2023:v1` | `RECOMMENDED_FOR_APPROVAL`; AE-DR-001 |
| Container | Deterministic `.xlsx` workbook preserving the E-1 sheet | `RECOMMENDED_FOR_APPROVAL`; AE-DR-002 |
| Worksheet name | `E-1` | `EXPLICITLY_REQUIRED` by source artifact |
| Title | `BIR SALES SUMMARY REPORT` | `EXPLICITLY_REQUIRED` |
| Physical columns | `A:AF`, 32 detail columns | `EXPLICITLY_REQUIRED` |
| Header facts | H01:H10 before detail headings | `EXPLICITLY_REQUIRED` labels; placement based on source |
| Detail order | D01:D32 exactly | `EXPLICITLY_REQUIRED` |
| Detail grain | One row per governing committed Z | `RECOMMENDED_FOR_APPROVAL`; AE-DR-003 |
| File grouping | One Z per file versus multiple Z rows | `UNRESOLVED`; AE-DR-003 |
| Output mutation | None | `EXISTING_EXITPASS_DECISION` |

## 4. Spreadsheet layout

The canonical renderer must preserve the source order and group labels:

```text
Taxpayer and machine header
BIR SALES SUMMARY REPORT
Date | Beginning SI/OR | Ending SI/OR | GTA Ending | GTA Beginning |
Manual SI/OR | Gross | VATable | VAT | VAT-Exempt | Zero-Rated |
Deductions: SC | PWD | NAAC | Solo Parent | Others | Returns | Voids | Total |
VAT Adjustment: SC | PWD | Others | VAT on Returns | Others | Total |
VAT Payable | Net Sales | Sales Overrun/Overflow | Total Income |
Reset Counter | Z-Counter | Remarks
```

The renderer must not reorder columns to match internal DTO order. Merged heading geometry, fonts, row heights, print area, page orientation, and print scaling should be copied from an approved template artifact rather than recreated from memory. Exact geometry, overflow, and wrapping acceptance remain AE-DR-024.

## 5. Electronic serialization properties

| Property | Required posture |
| --- | --- |
| Character encoding inside OOXML | UTF-8 XML as produced by a deterministic trusted OOXML implementation; exact package canonicalization remains AE-DR-002 |
| BOM | Not applicable to OOXML package; companion CSV BOM posture unresolved |
| Delimiter/quote/escape | Not applicable to selected workbook recommendation; CSV rules are not Annex E rules |
| Line endings | OOXML package implementation detail; do not use line endings as business identity |
| Decimal separator | Recommended `.`; approval required under AE-DR-019 |
| Thousands separator | Recommended none for machine stability; workbook display decision required |
| Date/time | Exact source format unresolved; recommended ISO-like dates for deterministic data cells, display style approved separately |
| Timezone | H09 should include or be governed by the Site POS Server reporting timezone snapshot; no default invented |
| Currency | One currency per profile instance; no physical E-1 currency column |
| Empty cells | Only approved optional fields; unresolved mandatory fields block generation |
| Formula cells | Prefer materialized recorded values plus validation evidence; do not depend on client formula recalculation |
| Hidden cells/macros | Prohibited unless an approved official template requires them; no macros found in `.xlsx` source |

## 6. Deterministic ordering

- Header order H01:H10 is fixed.
- Detail rows, if multiple, order by governing Z period sequence, then stable Z report reference.
- Detail columns are D01:D32.
- Multi-series fiscal ranges cannot be flattened until AE-DR-003/AE-DR-022 is approved.
- Worksheet and package member ordering must be deterministic if byte identity is required.

## 7. Filename

No prescribed BIR filename was found. The following is a recommendation only:

```text
ANNEX-E1_<sanitized-fiscal-identity-code>_<sanitized-min>_<period-or-range>_<contract-version>.xlsx
```

Rules proposed for approval:

- uppercase ASCII filename components;
- replace characters outside `[A-Z0-9_-]` with `_`;
- collapse repeated `_`;
- no taxpayer name, TIN, ticket, plate, credential, or internal UUID;
- use business date `YYYYMMDD` for one-Z files, or `YYYYMMDD-YYYYMMDD` for a multi-row period;
- deterministic same inputs produce the same filename.

AE-DR-004 blocks implementation because neither the filename nor the grouping component is authoritative.

## 8. Output identity, hash, and replay

Recommended output identity input:

```text
contract-version
annex-e-profile
governing-z-reference(s), in period order
historical-header-profile-reference
file-grouping identity
output format
```

Use a repository-approved SHA-256 versioned scheme. The hash must be over final bytes for content integrity and exposed only as a safe ETag/content identity. It must not use raw semantic source strings in public output.

Exact replay returns identical bytes, filename, content type, and hash. Restart must not alter bytes. A changed approved profile or period set creates a new operation/version, not mutation.

## 9. Generation, duplicates, and correction

| Case | Proposed behavior | Status |
| --- | --- | --- |
| First generation | Requires committed Z, approved E-1 profile, complete sources, and export authority | `RECOMMENDED_FOR_APPROVAL` |
| Same operation replay | Return original authoritative bytes or deterministically identical regenerated bytes | `RECOMMENDED_FOR_APPROVAL` |
| Same Z/profile, different operation | Existing metadata uniqueness permits one profile per Z; public behavior unresolved | AE-DR-017/018 |
| Prior-period regeneration | Allowed only from immutable sources and must be auditable | AE-DR-017 |
| Correction | Never mutate Z or prior file; replacement versus supplemental lineage unresolved | AE-DR-017 |
| Duplicate filename | Must not overwrite silently | `RECOMMENDED_FOR_APPROVAL` |
| No-activity period | Produce zero row or no file is unresolved | AE-DR-021 |

## 10. Delivery, retention, and controls

No official source in the local package defines electronic submission, portal upload, digital signing, encryption, compression, or delivery channel. These are AE-DR-016.

Recommended internal posture pending approval:

- no compression for a single workbook;
- private/no-store API caching;
- `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`;
- `Content-Disposition: attachment` with deterministic filename;
- `X-Content-Type-Options: nosniff`;
- immutable export audit record with safe content hash;
- retention no shorter than the approved fiscal-report retention policy, exact Annex E period unresolved;
- no email or removable-media delivery built into the generator.

## 11. Size and streaming

The source workbook has bounded rows in its print area, but no maximum reporting-period size. One-Z-per-file would be small and can be buffered for deterministic hashing. Multi-Z workbooks may be streamed only if the OOXML library can preserve deterministic package bytes. Maximum size and row count remain AE-DR-003/016.

## 12. Fail-closed cases

Generation must fail safely for unsupported profile/version, open period, missing governing Z, missing mandatory header, unresolved mandatory field, unsupported classification, mixed currency, ambiguous fiscal ranges, malformed stored snapshot, formula/reconciliation mismatch, or unapproved correction attempt. It must not emit a partially populated workbook as a compliant Annex E artifact.
