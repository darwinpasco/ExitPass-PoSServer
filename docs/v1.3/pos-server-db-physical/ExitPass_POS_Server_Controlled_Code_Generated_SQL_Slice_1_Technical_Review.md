# ExitPass POS Server Controlled-Code Generated SQL Slice 1 Technical Review

## 1. Review Summary

This review covers generated SQL for controlled-code source values Slice 1.

Generated SQL reviewed:

```text
db/reference-data/controlled-codes/generated/sql/001_controlled_codes_slice_1.sql
```

Source reviewed:

```text
db/reference-data/controlled-codes/source/controlled_code_source_index.json
db/reference-data/controlled-codes/source/families/*.json
```

Review result: generated SQL is ready for commit review as a reference-data generated artifact. It is not a database loading approval and no PostgreSQL load was performed.

## 2. Finding Counts

| Severity | Count |
| --- | ---: |
| P0 | 0 |
| P1 | 0 |
| P2 | 0 |
| Editorial | 0 |

## 3. Source Derivation Review

The generated SQL is derived from the approved JSON source values Slice 1.

Confirmed source inventory:

- 7 code sets
- 36 code values
- source index under `db/reference-data/controlled-codes/source/controlled_code_source_index.json`
- family files under `db/reference-data/controlled-codes/source/families/`

The generated SQL header records the JSON source path and source index path.

## 4. Deterministic UUID Review

The generated SQL uses the approved UUID v5 namespace:

```text
d07a7416-af9c-556d-86cc-7061335f7c11
```

The approved UUID name inputs are preserved:

```text
pos.controlled_code_sets:<code_set_key>
pos.controlled_codes:<code_set_key>:<code_key>
```

Static validation confirmed 43 unique deterministic v5 UUIDs:

- 7 code-set UUIDs
- 36 code-value UUIDs

Parent code-set UUIDs also appear as foreign key values in code rows, which is expected.

## 5. SQL Target Review

The generated SQL targets only:

- `pos.controlled_code_sets`
- `pos.controlled_codes`

It inserts/upserts code sets before code values.

The generated SQL uses explicit column lists and `ON CONFLICT` upsert posture for idempotent reapplication.

## 6. DDL / Artifact Boundary Review

Static validation found no `CREATE`, `ALTER`, `DROP`, or `TRUNCATE` statements.

The generated SQL contains no:

- schema DDL
- functions
- triggers
- extensions
- migrations
- Atlas artifacts
- sample transaction data
- fiscal document rows
- report rows
- export rows
- audit rows

## 7. Mapping Review

The target tables support these mapped source fields:

- code set ID
- code set key
- display name
- description
- governance owner
- source reference
- active posture
- effective start/end
- code value ID
- code key
- sort order

These source fields are not represented in generated SQL because the current target tables do not expose matching columns:

- `is_deprecated`
- `replaced_by_code_key`
- `metadata`

Those fields remain preserved in the JSON source of truth.

## 8. Scope Review

The generated SQL includes only the approved low-risk Slice 1 families:

- `channel_terminal_health_status`
- `configuration_action`
- `fiscal_action_audit_result`
- `fiscal_export_status`
- `fiscal_operation_exception_status`
- `fiscal_operation_retry_status`
- `fiscal_report_status`

The generated SQL does not introduce:

- BIR/accounting-sensitive values
- Security/Privacy-sensitive values
- vendor/accreditation values
- tax values
- fiscal document type values
- Annex E values
- X/Z report kind values
- discount/privilege values
- Digital SI URL values
- privacy classification values
- ARTS POSLog values
- export profile values

## 9. Authority Boundary Review

The generated SQL does not imply:

- POS-owned payment finality
- POS-owned PaymentAttempt lifecycle
- POS-owned PaymentConfirmation lifecycle
- POS-owned ExitAuthorization
- gate execution authority
- independent terminal fiscal authority
- offline fiscal issuance approval

The `offline` channel terminal health status remains observability-only and does not approve offline fiscal issuance.

## 10. Repository Boundary Review

Confirmed:

- generated SQL remains under `db/reference-data/controlled-codes/generated/sql/`
- schema SQL under `db/state` was not modified
- validation scripts were not modified
- JSON source values were not modified
- no CI workflow, Atlas config, migration, source code, or sample data was created

## 11. Static Validation Evidence

Static checks performed:

- generated SQL has no DDL statements
- generated SQL has exactly two insert/upsert targets
- insert/upsert targets are only `pos.controlled_code_sets` and `pos.controlled_codes`
- generated SQL includes all deterministic UUIDs derived from the JSON source
- generated SQL row inventory matches source inventory: 7 code sets and 36 code values
- generated SQL contains 43 unique deterministic v5 UUIDs

No database load was performed.

## 12. Final Recommendation

Recommendation: proceed to commit review for the generated SQL Slice 1 artifact.

Do not proceed to PostgreSQL loading until a separate load-validation task is approved.

