# ExitPass POS Server Controlled-Code UUID Namespace Decision

## 1. Decision Summary

Decision: use one fixed deterministic UUID version 5 namespace for all POS Server controlled-code reference data.

This document resolves the namespace and UUID name-input rules for future controlled-code seed/reference implementation. It does not create actual controlled-code values, source JSON files, seed SQL, generated SQL, database rows, schema changes, migrations, validation script changes, CI workflows, or application source code.

## 2. Approved Namespace Recommendation

Recommended namespace name: `exitpass.pos-server.controlled-codes.v1`

Recommended namespace UUID: `d07a7416-af9c-556d-86cc-7061335f7c11`

Derivation posture: UUID v5 using the standard DNS namespace UUID and the namespace name above.

The future source/generator task should treat this namespace UUID as a fixed governance constant. It should not regenerate or replace the namespace without a separate breaking-change decision.

## 3. UUID Version

Controlled-code set IDs and controlled-code value IDs should use UUID v5.

UUID v5 is selected because:

- generated IDs are deterministic from stable natural keys
- repeated generation from the same source produces the same IDs
- generated SQL can be reviewed and reproduced
- live database state does not need to be promoted into source files
- random UUID churn is avoided in reference data reviews

## 4. Exact UUID Name Inputs

Future generation must use these exact name inputs:

```text
pos.controlled_code_sets:<code_set_key>
pos.controlled_codes:<code_set_key>:<code_key>
```

Rules:

- `code_set_key` must be the approved source key exactly as it appears in the JSON source.
- `code_key` must be the approved value key exactly as it appears in the JSON source.
- No display names, descriptions, sort orders, effective dates, or governance fields may participate in UUID derivation.
- Whitespace normalization must not be applied silently. Source keys must already pass validation before UUID derivation.

## 5. Breaking-Change Rule

Changing `code_set_key` or `code_key` changes the deterministic UUID.

Therefore:

- changing a key after approval is a breaking reference-data change
- changing a key requires explicit review and migration planning
- a key must not be renamed just to improve wording after it has been seeded
- display text may be revised through approved reference-data updates when meaning is preserved
- semantic changes should normally be modeled as a new value with the old value deprecated

## 6. Deprecation / Replacement Rule

Historical keys should not be renamed in place.

If a code value becomes obsolete or its wording is no longer preferred:

1. Keep the existing `code_key`.
2. Mark the old value deprecated according to the approved source schema.
3. Set active/inactive posture according to the approved compatibility decision.
4. Add a replacement value only after its governance approval is complete.
5. Link replacement posture with `replaced_by_code_key` when the replacement is in the same code set.

This rule preserves historical foreign keys and prevents silent reference drift.

## 7. Namespace Change Rule

The namespace UUID must not change after source files or generated SQL are created.

A namespace change would invalidate every generated controlled-code UUID and must be treated as a major baseline break requiring:

- documented reason
- impact analysis
- migration plan
- database compatibility plan
- approval from architecture and data governance reviewers

## 8. Future Generator Requirements

A future generator should:

- use the namespace UUID exactly as defined here
- generate UUID v5 IDs for code sets and values
- reject duplicate natural keys
- reject invalid key names before UUID generation
- emit generated SQL with explicit UUID values
- emit evidence showing namespace UUID, name input, and generated UUID mapping
- avoid logging secrets or unrelated local database details

## 9. Non-Decisions

This document does not decide:

- actual controlled-code values
- actual source JSON files
- generated SQL file names
- final seed load command
- validation script implementation
- CI workflow behavior
- BIR/accounting values
- Security/Privacy values
- vendor/accreditation values

