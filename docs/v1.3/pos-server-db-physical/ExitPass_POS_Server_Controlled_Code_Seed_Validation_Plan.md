# ExitPass POS Server Controlled-Code Seed Validation Plan

## 1. Purpose

This document defines validation expectations for a future controlled-code seed/reference data implementation.

No validation scripts, seed files, SQL files, or data rows are created by this planning package.

## 2. Validation Goals

Future controlled-code seed validation should prove:

- source files are complete and well-formed
- generated/load artifacts match the approved source
- loading is repeatable in a disposable database
- baseline reference values are separated from sample data
- no authority-boundary leakage is introduced
- no schema SQL under `db/state` is modified by seed loading
- evidence can be attached to PR review

## 3. Static Source Validation

Future source validation should check:

- valid JSON/YAML syntax
- no duplicate `code_set_key`
- no duplicate `code_key` within a code set
- canonical lowercase `snake_case` keys unless explicitly approved
- non-blank display names
- governance owner or source reference for every family
- deterministic sort order
- valid effective date ranges
- no sample transaction data fields
- no raw credential, token, evidence, key, or identity document content

## 4. Authority-Boundary Validation

Future validation should reject code-set keys, code keys, or display labels that imply:

- POS-owned payment finality
- POS-owned `PaymentAttempt` lifecycle
- POS-owned `PaymentConfirmation` lifecycle
- POS-owned `ExitAuthorization`
- POS-owned gate execution
- independent terminal fiscal authority
- offline fiscal issuance approval

Allowed reference-only terminology should remain explicit and narrow, such as:

- tender context
- Central PMS reference
- payment finality reference
- vendor acknowledgement reference
- evidence reference

## 5. Baseline vs Sample Data Validation

Future validation must confirm seed/reference data does not include:

- fiscal document rows
- fiscal line rows
- tender/payment rows
- Digital SI URL rows
- reprint request rows
- adjustment rows
- report rows
- export rows
- audit rows
- recovery rows
- security reference rows

Only `pos.controlled_code_sets` and `pos.controlled_codes` should be loaded by the first controlled-code seed task unless separately approved.

## 6. Load Validation

Future load validation should run against a disposable PostgreSQL database and confirm:

- schema objects rebuild successfully before seed load
- seed load succeeds with `ON_ERROR_STOP=1`
- expected code-set count matches source
- expected code count matches source
- every seeded code references an expected code set
- no unexpected code sets are present
- no unexpected codes are present
- idempotent reload behavior is defined and tested
- no local drift is promoted back into source files

## 7. Integration With Existing DB Checks

The existing `Invoke-PosDbChecks.ps1` package may need future enhancement to support seed/reference validation.

Potential future modes or additions:

- expected seed inventory config
- source-file schema validation
- loaded code-set/code comparison
- prohibited authority-leaking code value checks
- PR evidence output for seed counts and differences

This planning package does not modify the validation scripts.

## 8. Evidence Expectations

Future seed/reference PR evidence should include:

- source file inventory
- generated SQL inventory if generated SQL is committed
- static validation summary
- disposable DB rebuild result
- seed load result
- code-set/code count comparison
- authority-boundary scan result
- confirmation no sample data was loaded
- confirmation generated evidence remains ignored unless explicitly approved

## 9. Recommended Validation Order

1. Validate source format and metadata.
2. Generate or review load SQL if applicable.
3. Rebuild current schema into a disposable database.
4. Load controlled-code seed/reference data.
5. Compare loaded database state to source.
6. Run authority-boundary and prohibited sample-data checks.
7. Capture evidence.
8. Confirm no `db/state` or schema SQL changes were introduced.

## 10. Current Non-Validation Items

The future validation plan should not attempt to validate:

- final fiscal formulas
- final BIR report layouts
- final ARTS POSLog mappings
- application behavior
- CI behavior before CI is approved
- sample transaction behavior

Those remain outside controlled-code seed/reference planning.
