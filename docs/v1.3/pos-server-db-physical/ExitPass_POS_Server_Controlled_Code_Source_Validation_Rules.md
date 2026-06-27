# ExitPass POS Server Controlled-Code Source Validation Rules

## 1. Purpose

This document defines validation expectations for future controlled-code JSON source files and generated SQL.

It does not create validation scripts, source JSON files, actual controlled-code values, seed SQL, generated SQL, or database rows.

## 2. File Inventory Rules

Future validation should confirm:

- controlled-code source files live only under the approved reference-data source path
- generated SQL lives only under the approved generated SQL path
- no controlled-code seed/reference files are added under `db/state`
- every source file is listed in the source index if an index is approved
- no untracked source files are silently ignored

## 3. JSON Syntax and Schema Rules

Future validation should confirm:

- every source file is valid JSON
- every source file declares the expected `schema_version`
- required top-level fields are present
- each `code_sets` item has required fields
- each code value has required fields
- optional metadata fields are JSON objects when present
- unexpected fields are either rejected or reported, depending on the approved schema strictness

## 4. Naming Rules

Future validation should confirm:

- `code_set_key` and `code_key` use lowercase `snake_case`
- keys start with a lowercase letter
- keys contain no spaces, hyphens, dots, slashes, quoted identifiers, leading underscores, trailing underscores, or repeated underscores
- keys are stable and unique within their scope
- keys do not imply prohibited authority ownership

## 5. Required Field Rules

Future validation should confirm:

- display names and descriptions are not blank
- governance owners are not blank
- source references are not blank
- `is_active` is boolean
- `is_deprecated` is boolean for code values
- `sort_order` is an integer greater than or equal to zero
- effective timestamps are ISO 8601 UTC timestamps

## 6. Deterministic UUID Rules

Future validation should confirm:

- the approved namespace UUID is present in the generator configuration
- deterministic UUID inputs follow the approved naming strings
- generated code set IDs are stable for unchanged `code_set_key` values
- generated code value IDs are stable for unchanged `code_set_key` and `code_key` pairs
- no random UUIDs are used for controlled-code seed/reference rows

## 7. Effective Dating and Deprecation Rules

Future validation should confirm:

- `effective_start_at` is present
- `effective_end_at` is later than `effective_start_at` when present
- inactive values are not deleted when historical compatibility is required
- deprecated values are retained with explicit posture
- replacement references point to existing values in the same code set when present

## 8. Sort Order Rules

Future validation should confirm:

- `sort_order` is present for every code value
- `sort_order` is non-negative
- `sort_order` is unique within each code set unless a future exception is approved
- sort order changes are treated as reviewable source changes

## 9. Authority-Boundary Rules

Future validation should reject or report source keys and metadata that imply:

- POS-owned payment finality
- POS-owned PaymentAttempt lifecycle
- POS-owned PaymentConfirmation lifecycle
- POS-owned ExitAuthorization
- gate execution authority
- independent terminal fiscal authority
- offline fiscal issuance approval
- vendor authority beyond reference posture

Reference-only names such as `payment_finality_ref`, `central_pms_payment_attempt_ref`, `central_pms_payment_confirmation_ref`, `vendor_ack_ref`, and `evidence_ref` remain allowed when used as references.

## 10. Sample and Transaction Data Rules

Future validation should confirm controlled-code source files do not contain:

- fiscal document rows
- payment rows
- report rows
- export rows
- audit/recovery rows
- transaction examples intended for loading
- raw credentials, tokens, private keys, identity documents, or evidence files

## 11. Generated SQL Validation Rules

Future generated SQL should be validated to confirm:

- it loads only `pos.controlled_code_sets` and `pos.controlled_codes`
- it contains no DDL
- it contains no functions, triggers, extensions, migrations, or Atlas artifacts
- it contains no sample transaction data
- it uses deterministic UUIDs
- it uses explicit columns
- it is idempotent
- it is generated from the approved JSON source

## 12. Database Load Validation Rules

Future disposable database validation should confirm:

- code sets load before code values
- generated SQL applies cleanly to a rebuilt disposable database
- expected row counts match source counts
- source keys are unique
- deterministic IDs match source derivation
- no local database drift is promoted into source files or generated SQL

## 13. Evidence Expectations

Future validation should emit evidence for:

- source file inventory
- schema validation result
- naming validation result
- deterministic UUID validation result
- generated SQL validation result
- disposable database load result
- drift check result

Evidence must not include secrets or raw sensitive data.

