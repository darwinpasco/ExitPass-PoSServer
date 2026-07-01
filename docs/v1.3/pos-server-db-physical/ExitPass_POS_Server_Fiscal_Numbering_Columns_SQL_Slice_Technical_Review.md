# ExitPass POS Server Fiscal Numbering Columns SQL Slice Technical Review

## 1. Review Summary

This review covers the fiscal numbering column SQL slice for `pos.fiscal_documents`.

The slice adds nullable fiscal numbering columns, FK relationships, local checks, and indexes needed for a future fiscal number allocation runtime slice. Runtime allocation remains blocked.

## 2. Overall Recommendation

Ready for review after validation.

The schema change is scoped to the approved Option A posture: dedicated nullable columns on `pos.fiscal_documents`. `document_context` remains non-authoritative for fiscal number storage.

## 3. Blocking Findings

P0: 0

## 4. Should-Fix Findings

P1: 0

## 5. Non-Blocking Findings

P2: 0

## 6. Editorial Findings

Editorial: 0

## 7. SQL Scope Review

Modified SQL object file:

- `db/state/tables/pos.fiscal_documents.sql`

The slice adds only approved fiscal numbering columns, constraints, comments, and indexes to `pos.fiscal_documents`.

`db/rebuild/pos_sql_apply_order.txt` was updated only to keep rebuild dependency order valid after adding the new FK to `pos.fiscal_sequence_policies`.

No other `db/state` SQL files were modified.

## 8. Columns Review

Added nullable columns:

- `fiscal_identity_id`
- `fiscal_sequence_policy_id`
- `fiscal_sequence_value`
- `fiscal_document_number`
- `fiscal_series`
- `fiscal_number_prefix_text`
- `fiscal_number_suffix_text`
- `fiscal_number_assigned_at`
- `fiscal_number_assigned_by_ref`

`fiscal_number_allocation_status_code_id` was not added and remains deferred.

## 9. Constraint Review

FK constraints:

- `fk_fiscal_documents__fiscal_identity`
- `fk_fiscal_documents__sequence_policy`

Check constraints:

- positive sequence value when present;
- nonblank fiscal number, series, prefix, suffix, and assigned-by reference when present;
- paired core assignment fields for sequence policy, sequence value, formatted number, and assignment timestamp.

The paired assignment check correctly does not require `fiscal_identity_id` during the initial nullable rollout.

## 10. Index Review

Partial unique indexes:

- `ux_fiscal_documents__seq_policy_value`
- `ux_fiscal_documents__seq_policy_number`

Lookup indexes:

- `ix_fiscal_documents__fiscal_identity`
- `ix_fiscal_documents__seq_policy`
- `ix_fiscal_documents__document_number`

Index names are lowercase snake_case and remain below PostgreSQL's 63-byte identifier limit.

## 11. Rebuild and Inventory Review

`db/rebuild/pos_sql_apply_order.txt` now applies `pos.fiscal_sequence_policies.sql` before `pos.fiscal_documents.sql`, which is required by the new FK dependency.

`db/validation/pos_expected_inventory.json` was not changed because it tracks expected schemas and tables only. This slice adds no new table, schema, function, trigger, sequence, extension, migration, or Atlas artifact.

## 12. Boundary Review

This slice does not implement:

- runtime fiscal number allocation;
- counter or sequence state mutation;
- fiscal gap automation;
- BIR reporting;
- Digital SI;
- Annex E;
- X/Z;
- statutory discount validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution.

## 13. Scope Discipline Review

This task did not modify:

- runtime code;
- API code;
- persistence code;
- controlled-code JSON;
- generated controlled-code SQL;
- CI workflow files;
- Atlas configuration;
- migrations;
- sample data.

## 14. Validation Review

Local validation performed:

- `dotnet build`
- `dotnet test`
- `powershell -ExecutionPolicy Bypass -File .\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`
- Darwin disposable rebuild validation with `Invoke-PosDbChecks.ps1 -Mode All -UseDockerPsql -DatabaseName posserver_fiscal_numbering_columns_validation_local -EvidenceDir .\db\validation\evidence\fiscal-numbering-columns`
- `git status --short --untracked-files=all`
- `git diff --check`

Results:

- `dotnet build` passed.
- `dotnet test` passed with 128 succeeded and 0 failed.
- Static DB validation passed.
- Darwin ran disposable validation against `posserver_fiscal_numbering_columns_validation_local`.
- The disposable database was dropped and recreated through `docker exec exitpass-postgres psql -U exitpass -d template1`.
- `Invoke-PosDbChecks.ps1` completed successfully for Mode All with Docker-backed psql.
- `git diff --check` passed with only an LF/CRLF warning on `db/rebuild/pos_sql_apply_order.txt`.
- `git status --short --untracked-files=all` showed only the expected SQL, manifest, and documentation changes.
- Evidence remains ignored under `db/validation/evidence/` and must not be committed.

Changed schema files remain limited to:

- `db/state/tables/pos.fiscal_documents.sql`
- `db/rebuild/pos_sql_apply_order.txt`

Documentation files added:

- `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Fiscal_Numbering_Columns_SQL_Slice.md`
- `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Fiscal_Numbering_Columns_SQL_Slice_Technical_Review.md`

No runtime/API/persistence code, migrations, controlled-code JSON, generated SQL, CI, Atlas config, sample data, runtime allocation, counter mutation, BIR reporting, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, refund/reversal authority, or gate/exit behavior was added.

## 15. Final Recommendation

The SQL slice is ready to proceed toward review after validation. Runtime number allocation remains blocked until a separate implementation slice updates allocation logic, sequence locking, idempotency behavior, and persistence mapping.
