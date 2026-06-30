# ExitPass POS Server Fiscal Document Persistence Wiring

## 1. Purpose

This note documents the first intentional persistence wiring behind `IFiscalDocumentRepository`.

The wiring persists the fiscal document header only, using the existing POS Server schema. It does not change schema SQL, create migrations, add BIR reporting, implement Digital SI, implement Annex E, implement X/Z reports, validate statutory discount entitlement, own payment finality, or add gate/exit behavior.

## 2. Persistence Scope

The initial PostgreSQL adapter writes only:

- `pos.fiscal_documents`

The adapter does not write:

- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`
- `pos.fiscal_document_lines`
- `pos.fiscal_tenders`
- `pos.fiscal_tax_details`
- `pos.fiscal_discount_privilege_details`
- `pos.fiscal_totals`

Those areas remain future slices because the current runtime command intentionally does not yet model fiscal lines, tender rows, tax rows, discount/privilege rows, or totals.

## 3. Added Components

Persistence project:

- `src/ExitPass.PosServer.Persistence.Postgres/`

Primary adapter:

- `PostgresFiscalDocumentRepository`

SQL mapping helper:

- `PostgresFiscalDocumentSql`

Tests:

- `tests/ExitPass.PosServer.Persistence.Postgres.Tests/`

Runtime exception:

- `FiscalDocumentPersistenceException`

API DI wiring:

- `AddPosServerFiscalDocumentApi(IConfiguration configuration)`

## 4. Table Mapping

`PostgresFiscalDocumentRepository` inserts into `pos.fiscal_documents` with explicit column lists and parameterized values.

Mapped schema columns:

- `fiscal_document_id`
- `site_pos_server_id`
- `channel_terminal_id`
- `fiscal_document_type_code_id`
- `fiscal_document_status_code_id`
- `central_pms_parking_session_ref`
- `central_pms_payment_attempt_ref`
- `central_pms_payment_confirmation_ref`
- `payment_finality_ref`
- `vendor_ack_ref`
- `business_day_date`
- `document_context`
- `is_active`
- `created_at`
- `updated_at`

The repository uses `document_context` JSONB for traceability values that do not have dedicated header columns, including:

- `site_pos_server_ref`
- `fiscal_document_type_code_key`
- `payable_basis_ref`
- `upstream_finality_ref`
- `currency_code`
- `payable_amount_minor_units`
- discount validation references and statuses

## 5. Upstream Reference Posture

The persistence adapter preserves upstream references needed for audit reconstruction:

- payable-basis reference in `document_context`
- upstream finality reference in `document_context`
- Central PMS parking session reference when provided
- Central PMS payment attempt reference when provided
- Central PMS payment confirmation reference when provided
- payment finality reference as reference-only context
- vendor acknowledgement reference when provided
- statutory discount validation reference in `document_context`

These references do not create POS-owned payment finality, entitlement approval, or statutory discount validation ownership.

## 6. Configuration

The API remains fail-closed when persistence is not configured.

If no connection string is available, DI uses:

- `PersistenceNotConfiguredFiscalDocumentRepository`

If a connection string is configured, DI uses:

- `PostgresFiscalDocumentRepository`

Connection string lookup order:

1. `ConnectionStrings:PosServer`
2. `POSSERVER_DB_URL`
3. `PosServer:Database:ConnectionString`

No secrets are committed in source, tests, docs, appsettings, or launch profiles.

## 7. Failure Behavior

If persistence is not configured, the API returns deterministic `persistence_not_configured`.

If a PostgreSQL write fails, the adapter wraps the database exception in `FiscalDocumentPersistenceException`. The API maps that to deterministic `persistence_write_failed`.

The API does not silently pretend fiscal document creation succeeded when persistence is unavailable or fails.

## 8. Statutory Discount Boundary

The locked boundary remains:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

The persistence adapter stores references only. It does not validate Senior/PWD entitlement, review raw evidence, decide local ordinance applicability, compute statutory entitlement, or transform unapproved discount references into fiscal treatment.

## 9. Tests

Persistence tests cover:

- SQL targets only `pos.fiscal_documents`
- SQL is parameterized and does not interpolate untrusted request values
- document context preserves references without raw evidence payloads
- persistence assembly does not expose authority-leaking behavior

API tests also cover:

- configured connection string resolves `PostgresFiscalDocumentRepository`
- missing connection string resolves fail-closed repository
- runtime guardrails reject bad inputs before persistence

Disposable PostgreSQL integration tests were not added in this slice because they would require an external database lifecycle in the unit test suite. Database rebuild/load validation remains covered by the existing validation scripts and CI workflow. A future integration-test slice can add an explicit disposable PostgreSQL harness if approved.

## 10. Non-Goals

This slice does not implement:

- database schema changes
- migrations
- controlled-code changes
- generated SQL changes
- status history writes
- fiscal lines, tenders, taxes, discounts, or totals persistence
- BIR X/Z reports
- Annex E
- Digital SI
- statutory discount entitlement validation
- raw evidence/image storage
- POS-owned payment finality
- exit authorization or gate behavior
