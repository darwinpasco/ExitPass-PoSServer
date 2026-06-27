# ExitPass POS Server Controlled-Code Source Layout

## 1. Purpose

This document defines the proposed future repository layout for controlled-code source files and generated artifacts.

It does not create folders, source JSON files, actual values, seed SQL, generated SQL, or database rows.

## 2. Layout Decision

Future controlled-code source should be separated from schema SQL.

Recommended future layout:

```text
db/reference-data/
  controlled-codes/
    source/
      controlled_code_source_index.json
      families/
        <code_set_key>.json
    generated/
      sql/
        <generated-controlled-code-load-file>.sql
    evidence/
      <local-validation-evidence-files>
```

This layout is not created by this task.

## 3. Layout Rules

The future `source/` directory should contain human-reviewed JSON source files.

The future `generated/sql/` directory should contain generated SQL derived from the approved JSON source. Generated SQL must be reproducible from source and should not be manually edited.

The future `evidence/` directory should contain local or CI validation outputs and should be ignored unless a later task explicitly approves committed evidence artifacts.

## 4. File Granularity

Recommended granularity: one JSON source file per controlled-code set family.

Rationale:

- Limits merge conflicts.
- Keeps ownership review focused.
- Allows BIR/accounting, security/privacy, operations, and engineering-owned families to move independently.
- Makes validation and generated SQL diffs easier to review.

A future `controlled_code_source_index.json` may list source files in deterministic order. The index should be introduced only when the source implementation task creates actual source files.

## 5. Dependency and Generation Order

Future generation should follow this order:

1. Validate all JSON source files.
2. Build deterministic code set IDs.
3. Build deterministic code value IDs.
4. Generate code set SQL before code value SQL.
5. Validate generated SQL against expected `pos.controlled_code_sets` and `pos.controlled_codes` columns.
6. Run disposable database load checks.
7. Emit evidence without promoting live database state.

## 6. Separation From `db/state`

Controlled-code source and generated seed/reference SQL must not be placed under `db/state`.

`db/state` remains for physical database object SQL. Controlled-code source belongs under a future reference-data path and must be reviewed separately from schema object changes.

## 7. Layout Non-Decisions

This layout document does not decide:

- final generated SQL file names
- whether generated SQL is committed or generated during validation only
- whether an index file is mandatory
- final evidence retention policy
- final CI workflow behavior
- final seed load execution command

