# ExitPass POS Server Controlled-Code Source Schema

## 1. Purpose

This document defines the proposed JSON source schema for future controlled-code seed/reference data.

It is a documentation and schema-decision artifact only. It does not create actual controlled-code values, source JSON files, seed SQL, generated SQL, or database rows.

## 2. Source Document Shape

Each future controlled-code source file should be a JSON object with these top-level fields:

| Field | Required | Type | Rule |
| --- | --- | --- | --- |
| `schema_version` | Yes | string | Version of the controlled-code source schema. |
| `source_key` | Yes | string | Stable key for the source file or source bundle. |
| `source_name` | Yes | string | Human-readable source name. |
| `source_description` | Yes | string | Purpose of the source file or bundle. |
| `governance_owner` | Yes | string | Owner accountable for the source content. |
| `source_ref` | Yes | string | Approved decision, document, issue, or authority reference. |
| `code_sets` | Yes | array | Array of controlled-code set definitions. |
| `notes` | No | array | Optional source-level notes. |

Illustrative placeholder shape only:

```json
{
  "schema_version": "<schema-version>",
  "source_key": "<source-file-key>",
  "source_name": "<source-name>",
  "source_description": "<source-description>",
  "governance_owner": "<governance-owner>",
  "source_ref": "<approved-source-reference>",
  "code_sets": []
}
```

The placeholder example intentionally contains no actual controlled-code values.

## 3. Code Set Fields

Each item in `code_sets` should define one row for future loading into `pos.controlled_code_sets`.

| Field | Required | Type | Rule |
| --- | --- | --- | --- |
| `code_set_key` | Yes | string | Stable lowercase `snake_case` natural key for the code family. |
| `display_name` | Yes | string | Human-readable label. |
| `description` | Yes | string | Clear functional description. |
| `governance_owner` | Yes | string | Owner accountable for the code set. |
| `source_ref` | Yes | string | Approved source or decision reference. |
| `is_active` | Yes | boolean | Whether the code set is active for new use. |
| `effective_start_at` | Yes | string | ISO 8601 UTC timestamp. |
| `effective_end_at` | No | string or null | ISO 8601 UTC timestamp later than start, when present. |
| `codes` | Yes | array | Controlled-code values for the set. |
| `metadata` | No | object | Non-authoritative structured metadata. |

The future loader should derive `controlled_code_set_id` deterministically and should not require source authors to hand-enter UUIDs.

## 4. Code Value Fields

Each item in a code set's `codes` array should define one row for future loading into `pos.controlled_codes`.

| Field | Required | Type | Rule |
| --- | --- | --- | --- |
| `code_key` | Yes | string | Stable lowercase `snake_case` key unique within the code set. |
| `display_name` | Yes | string | Human-readable label. |
| `description` | Yes | string | Clear meaning and intended use. |
| `source_ref` | Yes | string | Approved source or decision reference. |
| `sort_order` | Yes | integer | Deterministic display/order value. |
| `is_active` | Yes | boolean | Whether the value is active for new use. |
| `is_deprecated` | Yes | boolean | Whether the value is retained for history but discouraged for new use. |
| `effective_start_at` | Yes | string | ISO 8601 UTC timestamp. |
| `effective_end_at` | No | string or null | ISO 8601 UTC timestamp later than start, when present. |
| `replaced_by_code_key` | No | string or null | Replacement value key in the same code set, when approved. |
| `metadata` | No | object | Non-authoritative structured metadata. |

The future loader should derive `controlled_code_id` deterministically from `code_set_key` and `code_key`.

## 5. Deterministic UUID Strategy

Recommendation: use deterministic UUID version 5 for controlled-code set and controlled-code IDs.

The future implementation should use one approved namespace UUID for POS Server controlled-code reference data. That namespace UUID must be decided before source files are created.

Recommended name inputs:

- Code set ID input: `pos.controlled_code_sets:<code_set_key>`
- Code value ID input: `pos.controlled_codes:<code_set_key>:<code_key>`

Rules:

- UUIDs are generated from stable natural keys, not manually entered.
- Changing `code_set_key` or `code_key` changes the deterministic UUID and must be treated as a breaking change.
- Renames should normally be modeled by deprecating the old key and adding a replacement key, not by mutating a historical key.
- Generated SQL should include the derived UUIDs explicitly so database loads are deterministic.

## 6. Key Naming Rules

`code_set_key` and `code_key` must:

- use lowercase `snake_case`
- start with a lowercase letter
- contain only lowercase letters, digits, and underscores
- avoid leading, trailing, or repeated underscores
- avoid spaces, hyphens, periods, slashes, and quoted identifiers
- be stable once approved
- be domain-specific enough to avoid ambiguous generic meanings

They must not imply:

- POS-owned payment finality
- POS-owned PaymentAttempt lifecycle
- POS-owned PaymentConfirmation lifecycle
- POS-owned ExitAuthorization
- gate execution authority
- independent terminal fiscal authority
- offline fiscal issuance approval

## 7. Effective Dating and Deprecation

Future source files should use explicit effective dating:

- `effective_start_at` is required.
- `effective_end_at` is optional and must be later than `effective_start_at` when present.
- Active values have `is_active = true`.
- Deprecated values should have `is_deprecated = true` and may remain active or inactive depending on approved compatibility needs.
- Values required for historical records should not be deleted from the source.

Deprecated values are reference-history posture. Deprecation does not automatically migrate existing records.

## 8. Sort Order Rules

`sort_order` is required for each code value.

Rules:

- Must be an integer greater than or equal to zero.
- Must be unique within a code set unless a future decision explicitly allows ties.
- Should use gaps to allow future insertions without renumbering existing values.
- Must not be used as a business meaning or fiscal authority indicator.

## 9. Governance Owner and Source Reference

Every source document, code set, and code value must include governance context.

`governance_owner` identifies the accountable owner for the source content. `source_ref` identifies the approved source, such as a baseline document, decision log, accreditation input, accounting decision, security decision, or implementation issue.

Rules:

- Governance fields must not be blank.
- Source references must be traceable.
- Vendor-provided values remain references and do not become vendor authority unless separately approved.
- BIR/accounting-sensitive values require explicit BIR/accounting source references before seed implementation.

## 10. Generated SQL Shape Later

Future generated SQL should:

- be produced from approved JSON source files
- include a generated-file header
- insert or upsert controlled-code sets before controlled-code values
- use deterministic UUIDs
- use explicit column lists
- preserve effective dating and active/deprecated flags
- never create schema objects
- never insert transaction/sample data

No generated SQL is created by this document.

