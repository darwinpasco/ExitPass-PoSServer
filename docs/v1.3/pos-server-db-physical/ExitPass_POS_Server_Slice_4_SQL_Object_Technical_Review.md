# ExitPass POS Server Slice 4 SQL/Object Technical Review

## 1. Review Summary

This review covers the Slice 4 numbering, counters, idempotency, retry, and exception SQL/object artifacts:

- `db/state/tables/pos.fiscal_sequence_policies.sql`
- `db/state/tables/pos.fiscal_sequence_states.sql`
- `db/state/tables/pos.fiscal_sequence_gap_audit.sql`
- `db/state/tables/pos.fiscal_counter_states.sql`
- `db/state/tables/pos.fiscal_state_snapshots.sql`
- `db/state/tables/pos.fiscal_lock_states.sql`
- `db/state/tables/pos.idempotency_records.sql`
- `db/state/tables/pos.fiscal_operation_retries.sql`
- `db/state/tables/pos.fiscal_operation_exceptions.sql`

The review was performed against the approved Physical Object Design v1.0, Physical DB Schema and Naming Standards v1.0, Physical DB Gate Resolution v1.0, `db/README.md`, and the existing dependency SQL for `pos`, `pos.controlled_codes`, `pos.site_pos_servers`, `pos.channel_terminals`, and `pos.fiscal_documents`.

The Slice 4 artifacts create only the approved state/posture objects. They preserve the documentation-approved boundary that this slice does not create PostgreSQL sequence objects, allocation functions, counter increment functions, triggers, extensions, fiscal issuance behavior, official fiscal number allocation, Digital SI URL/token/access behavior, reports/exports/EJ/POSLog, recovery/anchoring implementation, outbox/events, seed/reference/sample data, scripts, CI workflows, or source code.

## 2. Overall Recommendation

Proceed to commit after normal repository review.

The SQL artifacts are ready to commit from this focused technical review perspective. No P0, P1, P2, or editorial findings were identified.

## 3. Blocking Findings

None.

## 4. Should-Fix Findings

None.

## 5. Non-Blocking Findings

None.

## 6. Editorial Findings

None.

## 7. Scope Review

The reviewed SQL files create only the approved Slice 4 objects:

- `pos.fiscal_sequence_policies`
- `pos.fiscal_sequence_states`
- `pos.fiscal_sequence_gap_audit`
- `pos.fiscal_counter_states`
- `pos.fiscal_state_snapshots`
- `pos.fiscal_lock_states`
- `pos.idempotency_records`
- `pos.fiscal_operation_retries`
- `pos.fiscal_operation_exceptions`

The artifacts remain state/posture only. They do not create allocation logic, issuance behavior, final BIR/accounting numbering rules, final recovery or anchoring mechanisms, functions, triggers, extensions, seed data, scripts, CI workflows, or source code.

## 8. SQL Inventory Review

The SQL inventory is consistent with the approved Slice 4 scope:

| File | Object | Review result |
| --- | --- | --- |
| `db/state/tables/pos.fiscal_sequence_policies.sql` | `pos.fiscal_sequence_policies` | Approved fiscal sequence policy posture. |
| `db/state/tables/pos.fiscal_sequence_states.sql` | `pos.fiscal_sequence_states` | Approved fiscal sequence state posture. |
| `db/state/tables/pos.fiscal_sequence_gap_audit.sql` | `pos.fiscal_sequence_gap_audit` | Approved sequence gap audit posture. |
| `db/state/tables/pos.fiscal_counter_states.sql` | `pos.fiscal_counter_states` | Approved fiscal counter state posture. |
| `db/state/tables/pos.fiscal_state_snapshots.sql` | `pos.fiscal_state_snapshots` | Approved fiscal state snapshot posture. |
| `db/state/tables/pos.fiscal_lock_states.sql` | `pos.fiscal_lock_states` | Approved fiscal lock/block state posture. |
| `db/state/tables/pos.idempotency_records.sql` | `pos.idempotency_records` | Approved idempotency request identity posture. |
| `db/state/tables/pos.fiscal_operation_retries.sql` | `pos.fiscal_operation_retries` | Approved fiscal operation retry posture. |
| `db/state/tables/pos.fiscal_operation_exceptions.sql` | `pos.fiscal_operation_exceptions` | Approved fiscal operation exception posture. |

Existing dependency objects were reviewed for compatibility:

- `pos` schema
- `pos.controlled_codes`
- `pos.site_pos_servers`
- `pos.channel_terminals`
- `pos.fiscal_documents`

The foreign key targets used by the Slice 4 objects exist in the approved current SQL state.

## 9. Naming Review

The Slice 4 SQL follows the approved naming baseline:

- Uses schema-qualified `pos.*` object names.
- Uses lowercase `snake_case`.
- Uses no quoted identifiers.
- Uses `_id` for internal identifiers.
- Uses `_ref` for external references.
- Uses domain-specific object and column names.
- Uses explicit primary key, foreign key, unique, and check constraint names.

Constraint names were checked against PostgreSQL's 63-byte identifier limit. No constraint names exceed the limit.

## 10. Authority-Boundary Review

The SQL preserves the approved authority model:

- No POS-owned payment finality object is created.
- No POS-owned PaymentAttempt lifecycle is created.
- No POS-owned PaymentConfirmation lifecycle is created.
- No POS-owned ExitAuthorization or gate execution authority is created.
- No independent terminal fiscal authority is introduced.
- Idempotency records protect fiscal side effects only and do not create payment or exit authority.
- Retry and exception objects record posture only and do not implement execution behavior.

Table comments reinforce that idempotency, retry, exception, lock, and snapshot records are reference/evidence posture only.

## 11. Fiscal-Boundary Review

The Slice 4 artifacts preserve fiscal-boundary limits:

- Sequence/counter tables are state/posture only.
- No actual SI number allocation behavior is created.
- No PostgreSQL sequence object is created.
- No final BIR/accounting sequence-gap rule is over-decided.
- No fiscal issuance commit logic is created.
- No counter increment behavior is created.
- Reset counter and Z-counter posture remains descriptive through controlled-code families.
- Fiscal state snapshots do not implement anchoring or recovery.

The placeholder policy remains intact: display fiscal numbers stay separate from internal IDs, consumed numbers are not reused, reserved-number reuse remains blocked unless a compliant future rule is approved, and gap records are permanent evidence posture.

## 12. PostgreSQL Compatibility Review

The SQL uses PostgreSQL-compatible constructs:

- `CREATE TABLE IF NOT EXISTS`
- schema-qualified table names
- `uuid`, `text`, `char(3)`, `integer`, `bigint`, `boolean`, `jsonb`, and `timestamptz` types
- `CURRENT_TIMESTAMP` defaults
- explicit primary keys
- explicit foreign keys
- explicit unique constraints where locally safe
- safe local check constraints
- `COMMENT ON TABLE`
- `COMMENT ON COLUMN`

No extensions are required. No functions, triggers, or PostgreSQL sequence objects are created. No seed data is inserted.

## 13. Constraint and Reference Review

References remain inside the approved current object set:

- `site_pos_server_id` references `pos.site_pos_servers`.
- `fiscal_document_id` references `pos.fiscal_documents`.
- `idempotency_record_id` references `pos.idempotency_records`.
- code references point to `pos.controlled_codes`.
- sequence state and gap records reference `pos.fiscal_sequence_policies`.

The check constraints are local and conservative:

- required text fields must be non-blank.
- optional reference text fields must be non-blank when present.
- numeric state values are non-negative or positive where appropriate.
- timestamp range checks are limited to local order checks.
- JSON context columns must be JSON objects when present.
- currency codes must be three uppercase letters where present.

The constraints do not implement final numbering, issuance, recovery, anchoring, or BIR/accounting rules.

## 14. Fiscal Sequence Policy / State Review

`pos.fiscal_sequence_policies` and `pos.fiscal_sequence_states` are scoped correctly:

- No PostgreSQL sequence objects are created.
- Sequence values are state fields only.
- Policy uniqueness is safely scoped to `(site_pos_server_id, policy_code)`.
- State uniqueness is safely scoped to one state record per sequence policy.
- Reserved and issued values are constrained within current state but do not implement allocation behavior.
- Constraints do not over-decide final BIR/accounting numbering rules.

## 15. Sequence Gap Audit Review

`pos.fiscal_sequence_gap_audit` is scoped correctly:

- Gap audit is permanent evidence posture only.
- It does not approve or implement sequence reuse.
- It does not implement final BIR sequence-gap treatment.
- Gap values are positive and unique per sequence policy.
- Actor, service, reason, and context fields remain conservative and reference/context-only.

## 16. Counter State Review

`pos.fiscal_counter_states` is scoped correctly:

- Reset, Z, GTA, and related counter posture is flexible through controlled codes.
- No counter increment behavior is implemented.
- Counter values and monetary amounts are non-negative.
- Currency constraints are local and safe.
- No fiscal issuance, day close, report close, or production counter behavior is created.

## 17. Fiscal State Snapshot / Lock State Review

`pos.fiscal_state_snapshots` and `pos.fiscal_lock_states` are scoped correctly:

- Snapshots are continuity posture only.
- Latest EJ hash is a reference only and not an anchoring implementation.
- Lock/block/resume posture does not implement recovery workflow.
- No unsafe resume behavior is implied.
- No gate authority or offline fiscal issuance approval is created.

## 18. Idempotency / Retry / Exception Review

`pos.idempotency_records`, `pos.fiscal_operation_retries`, and `pos.fiscal_operation_exceptions` are scoped correctly:

- Idempotency protects fiscal side effects only.
- Unique `(idempotency_scope, idempotency_key)` is safe for duplicate request identity posture.
- Completion-unknown posture is represented without over-deciding workflow.
- Retry objects record attempts but do not execute retries.
- Exception objects record exception posture but do not create a full audit or recovery subsystem.
- No payment finality, payment lifecycle, ExitAuthorization, or gate authority is created.

## 19. Prohibited Object Review

No prohibited artifacts were created. The reviewed SQL does not create:

- PostgreSQL `CREATE SEQUENCE`
- sequence allocation functions
- counter increment functions
- triggers
- extensions
- fiscal issuance behavior
- Sales Invoice issuance behavior
- official fiscal number allocation behavior
- fiscal line, tender, tax, discount, or totals tables
- Digital SI URL, token, or access tables
- reprint tables
- adjustment tables
- report, export, EJ, or POSLog tables
- audit subsystem tables beyond local retry, exception, and gap posture
- recovery or anchoring implementation
- outbox or event tables
- seed/reference/sample data
- scripts
- CI workflows
- source code

## 20. Optional Smoke Check Result

The optional PostgreSQL smoke check was not run because `psql` was not available in the local environment.

Static review and repository validation were completed instead.

## 21. Recommended Targeted Edits

None.

## 22. Recommended Next Step

Commit the Slice 4 SQL artifacts and this technical review after normal maintainer review of the diff.
