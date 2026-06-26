# ExitPass POS Server Controlled-Code Seed Source Analysis

## 1. Purpose

This document analyzes the current POS Server database state for a future controlled-code seed/reference data package.

This is a planning document only. It does not create seed SQL, insert reference values, create sample data, modify `db/state`, change validation scripts, or authorize implementation.

## 2. Sources Inspected

- `db/state/schemas/pos.sql`
- `db/state/tables/pos.controlled_code_sets.sql`
- `db/state/tables/pos.controlled_codes.sql`
- current `db/state/tables/*.sql` objects from slices 1 through 8
- `db/README.md`
- `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_Object_Design_v1.0.md`
- `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_DB_Object_Inventory_Roadmap_Check.md`
- `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_DB_Validation_Rebuild_Drift_Scripts_Implementation_Notes.md`

## 3. Current Database Context

The database object slices 1 through 8 are implemented under `db/state`.

The current controlled-code foundation consists of:

- `pos.controlled_code_sets`
- `pos.controlled_codes`

All controlled-code values remain empty until a future approved seed/reference data task.

The validation/rebuild/drift package is implemented and merged. Static mode passed, and Docker-backed `Mode All` passed against disposable database `posserver_validation_local` on host port `5433`.

## 4. Planning Boundary

This package is limited to controlled-code seed/reference planning.

It does not create:

- seed SQL files
- JSON/YAML source files
- generated SQL files
- sample fiscal documents
- sample payments
- sample reports
- sample exports
- sample audit/recovery/transaction data
- schema changes
- functions
- triggers
- migrations
- Atlas files
- CI workflows
- source code

## 5. Baseline Reference Data vs Sample Data

Baseline controlled-code/reference data means governed, environment-neutral values needed for the application and database to interpret controlled-code foreign keys.

Examples:

- document type codes
- status codes
- reason-code families
- channel/terminal type codes
- report/export type codes
- tax and privilege classification codes

Sample transaction data means example business records that represent operational transactions or evidence.

Examples explicitly excluded from this planning package:

- sample fiscal documents
- sample fiscal lines
- sample tenders/payments
- sample reports
- sample exports
- sample audit rows
- sample recovery requests
- sample Digital SI URLs

Future seed/reference data must include baseline controlled-code values only unless a separate task explicitly approves sample data.

## 6. Controlled-Code Family Coverage

The SQL inventory implies controlled-code families across these areas:

- fiscal document types, statuses, link types, and status/link reasons
- fiscal line types and statuses
- tender types
- tax types and classifications
- discount and privilege classifications
- fiscal total types
- channel/terminal types, health statuses, operational statuses, capabilities, and status reasons
- fiscal identity statuses and assignment reasons
- sequence families, sequence states, policy statuses, and gap reasons
- counter families and counter statuses
- fiscal state snapshot types/statuses and fiscal lock states/reasons
- idempotency operation types/statuses
- fiscal operation retry statuses/reasons and exception types/statuses
- Digital SI URL statuses, status reasons, access event types, and access result types
- reprint types, statuses, reasons, and output types
- adjustment types, statuses, and reasons
- report types, statuses, scopes, output types/statuses, X/Z report kinds, and Annex E types
- export types/statuses, package types/statuses, item types, validation statuses/severities, and schema/profile types
- fiscal action audit action/result types
- privileged action audit action/result/reason types
- configuration audit area/action/reason types
- access audit subject/action/result types
- recovery types/statuses/reasons
- continuity check types/results
- anchor types/statuses
- security reference types/statuses and privacy classifications

## 7. Recommended Source Format

Recommended future source format: JSON or YAML source of truth with generated SQL later.

Rationale:

- supports reviewable code-set families and values before SQL generation
- separates source data from generated load artifacts
- reduces hand-authored SQL repetition
- supports deterministic validation against `pos.controlled_code_sets` and `pos.controlled_codes`
- enables future checks for code-set keys, code keys, display names, source references, effective dates, and sort order

Direct SQL seed files should be deferred unless the team intentionally chooses a simpler first load. If direct SQL is chosen later, it should still be generated or reviewed from a structured inventory and must remain separate from schema SQL.

## 8. Future Validation Needs

Future seed/reference validation should confirm:

- every expected code-set family exists
- every seeded code belongs to a valid code set
- `code_set_key` and `code_key` are lowercase `snake_case` or otherwise explicitly approved
- required display names are not blank
- sort order is deterministic
- effective dates are valid
- active/inactive posture is explicit
- no sample transaction data is present
- no payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, gate execution, or offline issuance authority is created
- seed load is repeatable against a disposable database
- seed inventory is included in PR evidence

## 9. Out of Scope

- final code values
- final BIR/accreditation values
- final ARTS POSLog mapping values
- privacy/security classification values requiring Security/Privacy approval
- sample transaction rows
- seed SQL implementation
- generated SQL implementation
- CI workflow changes

## 10. Conclusion

The current database schema is ready for controlled-code/reference data planning. Actual seed data creation remains blocked by code-family governance decisions, value approval, source format approval, and validation requirements.
