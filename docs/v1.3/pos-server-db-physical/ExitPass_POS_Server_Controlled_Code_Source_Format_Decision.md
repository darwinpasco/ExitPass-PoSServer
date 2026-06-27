# ExitPass POS Server Controlled-Code Source Format Decision

## 1. Decision Summary

Decision: future POS Server controlled-code seed/reference data should use JSON as the source of truth.

This decision covers source format only. It does not create controlled-code values, seed SQL, generated SQL, database rows, schema changes, migrations, scripts, CI workflows, or application source code.

## 2. Status

Status: source-format decision baseline for future controlled-code seed/reference implementation.

The actual controlled-code values remain blocked until the open questions from the controlled-code seed planning package are resolved. A future implementation task must create the source files, generated SQL, validation evidence, and loading workflow separately.

## 3. JSON vs YAML Decision

JSON is selected because:

- The repository does not show a clear YAML convention for database reference data sources.
- JSON can be parsed with standard PowerShell capabilities already used by the validation/rebuild/drift package.
- JSON has fewer parser-dependent ambiguities around scalars, dates, booleans, and indentation.
- JSON is a better fit for deterministic validation, generation, and evidence output.
- JSON can be compared and transformed consistently by future script tasks without adding external module dependencies.

YAML is not selected for this source package. YAML may be reconsidered only if the repository later adopts a strong YAML convention and a future architecture decision approves the additional parser dependency and validation posture.

## 4. Source-of-Truth Rule

Future controlled-code JSON source files should be the source of truth for controlled-code seed/reference data.

Generated SQL, when introduced later, should be treated as a generated artifact derived from the JSON source. Generated SQL must not become an independent source of truth for controlled-code values.

Repository state remains the source of truth. A live or local database must never be promoted into source JSON or generated SQL without an explicit review task.

## 5. Seed / Reference Boundary

Controlled-code source files should contain baseline reference data only.

They must not contain:

- sample fiscal documents
- sample payments
- sample reports
- sample exports
- sample audit rows
- sample recovery rows
- sample transaction rows
- environment-specific test data
- credentials, tokens, private keys, raw evidence, or raw identity documents

## 6. Future Generated SQL Posture

A future task may generate SQL from the approved JSON source. That generated SQL should:

- load only `pos.controlled_code_sets` and `pos.controlled_codes`
- use deterministic UUIDs derived from stable source keys
- use explicit column lists
- be idempotent
- preserve inactive and deprecated values for historical compatibility
- avoid schema DDL
- avoid functions, triggers, extensions, migrations, and Atlas files
- avoid sample or transaction data

No generated SQL is created by this decision package.

## 7. Non-Decisions

This document does not decide or create:

- actual controlled-code values
- seed SQL
- generated SQL
- source JSON files containing values
- SQL object files
- validation script changes
- migration strategy
- CI workflow behavior
- final governance owner vocabulary
- final deterministic UUID namespace value
- final controlled-code approval list

