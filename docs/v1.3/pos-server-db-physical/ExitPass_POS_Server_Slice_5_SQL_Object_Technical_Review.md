# ExitPass POS Server Slice 5 SQL/Object Technical Review

## 1. Review Summary

This review covers the Slice 5 Digital SI URL, reprint, and fiscal adjustment SQL/object artifacts:

- `db/state/tables/pos.digital_si_urls.sql`
- `db/state/tables/pos.digital_si_url_access_events.sql`
- `db/state/tables/pos.reprint_requests.sql`
- `db/state/tables/pos.reprint_output_refs.sql`
- `db/state/tables/pos.fiscal_adjustments.sql`
- `db/state/tables/pos.fiscal_adjustment_status_history.sql`

The review was performed against the approved Physical Object Design v1.0, Physical DB Schema and Naming Standards v1.0, Physical DB Gate Resolution v1.0, `db/README.md`, and the existing dependency SQL for `pos`, `pos.controlled_codes`, `pos.channel_terminals`, and `pos.fiscal_documents`.

The Slice 5 artifacts create only the approved lifecycle/posture objects. They preserve the documentation-approved boundary that this slice does not create report/export/EJ/POSLog objects, audit subsystem objects, recovery/anchoring implementation, outbox/events, QR image storage, raw token or credential storage, raw evidence storage, fiscal issuance functions, SI number allocation functions, counter increment functions, PostgreSQL sequences, functions, triggers, extensions, seed/reference/sample data, scripts, CI workflows, or source code.

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

The reviewed SQL files create only the approved Slice 5 objects:

- `pos.digital_si_urls`
- `pos.digital_si_url_access_events`
- `pos.reprint_requests`
- `pos.reprint_output_refs`
- `pos.fiscal_adjustments`
- `pos.fiscal_adjustment_status_history`

The artifacts stay within Digital SI URL lifecycle posture, Digital SI URL access event posture, reprint request posture, reprint output reference posture, fiscal adjustment posture, and fiscal adjustment status history posture.

## 8. SQL Inventory Review

The SQL inventory is consistent with the approved Slice 5 scope:

| File | Object | Review result |
| --- | --- | --- |
| `db/state/tables/pos.digital_si_urls.sql` | `pos.digital_si_urls` | Approved Digital SI URL lifecycle posture. |
| `db/state/tables/pos.digital_si_url_access_events.sql` | `pos.digital_si_url_access_events` | Approved Digital SI URL access event posture. |
| `db/state/tables/pos.reprint_requests.sql` | `pos.reprint_requests` | Approved reprint request posture. |
| `db/state/tables/pos.reprint_output_refs.sql` | `pos.reprint_output_refs` | Approved reprint output reference posture. |
| `db/state/tables/pos.fiscal_adjustments.sql` | `pos.fiscal_adjustments` | Approved fiscal adjustment posture. |
| `db/state/tables/pos.fiscal_adjustment_status_history.sql` | `pos.fiscal_adjustment_status_history` | Approved fiscal adjustment status history posture. |

Existing dependency objects were reviewed for compatibility:

- `pos` schema
- `pos.controlled_codes`
- `pos.channel_terminals`
- `pos.fiscal_documents`

The foreign key targets used by the Slice 5 objects exist in the approved current SQL state.

## 9. Naming Review

The Slice 5 SQL follows the approved naming baseline:

- Uses schema-qualified `pos.*` object names.
- Uses lowercase `snake_case`.
- Uses no quoted identifiers.
- Uses `_id` for internal identifiers.
- Uses `_ref` for external references and output/security references.
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
- Payment reversal and reconciliation values are references only.
- Approval and actor values are references only and do not create external authority.

The adjustment, reprint, and Digital SI URL comments reinforce that the tables are posture/reference metadata only.

## 11. Privacy / Security Boundary Review

The SQL preserves the Digital SI URL privacy/security posture:

- No raw token values are stored.
- `access_token_ref` is a reference to token material or a token handle only.
- No raw credential fields are present.
- No raw identity/evidence content fields are present.
- No QR image binary storage or QR generation object is created.
- URL access event rows are evidence metadata only.
- Token/auth/expiry implementation remains Security/Privacy-dependent.

The JSON context columns are flexible metadata containers but are documented as unresolved Security/Privacy context; they do not authorize storage of raw credentials, token values, or sensitive evidence.

## 12. Fiscal-Boundary Review

The Slice 5 artifacts preserve fiscal-boundary limits:

- Reprints do not mutate original fiscal facts.
- Reprints do not create new Sales Invoice numbers.
- Reprints do not create fiscal issuance behavior.
- Reprint output rows store references and metadata only.
- Fiscal adjustments link original and adjustment fiscal documents.
- Original and adjustment document IDs must differ.
- No refund/reversal finality is created.
- No money-movement ownership is created.
- No adjustment numbering sequence is created.
- No adjustment line/detail table is created.
- No payment lifecycle state is created.

## 13. PostgreSQL Compatibility Review

The SQL uses PostgreSQL-compatible constructs:

- `CREATE TABLE IF NOT EXISTS`
- schema-qualified table names
- `uuid`, `text`, `boolean`, `jsonb`, and `timestamptz` types
- `CURRENT_TIMESTAMP` defaults
- explicit primary keys
- explicit foreign keys
- explicit unique constraints where locally safe
- safe local check constraints
- `COMMENT ON TABLE`
- `COMMENT ON COLUMN`

No extensions are required. No functions, triggers, or PostgreSQL sequence objects are created. No seed data is inserted.

## 14. Constraint and Reference Review

References remain inside the approved current object set:

- `fiscal_document_id` references `pos.fiscal_documents`.
- `channel_terminal_id` references `pos.channel_terminals`.
- code references point to `pos.controlled_codes`.
- reprint output rows reference `pos.reprint_requests`.
- fiscal adjustment status history references `pos.fiscal_adjustments`.

The check constraints are local and conservative:

- optional reference text fields must be non-blank when present.
- status reason text must be non-blank when present.
- Digital SI lifecycle timestamps are not before `issued_at` when both values are present.
- original and adjustment fiscal document IDs must differ.
- JSON context columns must be JSON objects when present.

The constraints do not implement final Security/Privacy token/auth policy, final BIR/accounting adjustment rules, final report/export behavior, or fiscal issuance behavior.

## 15. Digital SI URL Review

`pos.digital_si_urls` is scoped correctly:

- Digital SI URL lifecycle is represented as posture only.
- URL and token values are references only.
- Raw token and credential storage are excluded.
- QR image/object storage is excluded.
- URL access cannot mutate fiscal document records.
- issued/expiry/revoked/blocked timestamp constraints are local and safe.
- token/auth/expiry implementation remains Security/Privacy-dependent.

## 16. Digital SI URL Access Event Review

`pos.digital_si_url_access_events` is scoped correctly:

- Access events are posture/evidence metadata only.
- No full audit subsystem is created.
- No raw credentials or sensitive evidence are stored.
- Optional channel/terminal reference is safe and points to the approved `pos.channel_terminals` object.
- Access context remains a JSON object when present.

## 17. Reprint Review

`pos.reprint_requests` and `pos.reprint_output_refs` are scoped correctly:

- Original fiscal facts remain immutable.
- Reprint requests are metadata/reference posture only.
- Reprint output refs store references and metadata only.
- No new fiscal numbers are generated.
- No fiscal issuance behavior is created.
- No report/export objects are created.
- `REPRINT` and `DATE / TIME REPRINTED` posture is supported through `reprint_label_applied` and `reprinted_at`.

## 18. Fiscal Adjustment Review

`pos.fiscal_adjustments` and `pos.fiscal_adjustment_status_history` are scoped correctly:

- Adjustment relationship is document-to-document posture only.
- Original and adjustment fiscal documents must differ.
- No refund/reversal finality is owned by POS Server.
- No money-movement ownership is created.
- No adjustment numbering/counter behavior is created.
- No adjustment line/detail object is created.
- Status history is local lifecycle posture only and not a full audit subsystem.

## 19. Prohibited Object Review

No prohibited artifacts were created. The reviewed SQL does not create:

- report tables
- export, EJ, or POSLog tables
- audit subsystem tables
- recovery or anchoring tables
- outbox or event tables
- QR image storage
- raw token or credential storage
- raw evidence storage
- fiscal issuance functions
- SI number allocation functions
- counter increment functions
- PostgreSQL sequences
- functions
- triggers
- extensions
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

Commit the Slice 5 SQL artifacts and this technical review after normal maintainer review of the diff.
