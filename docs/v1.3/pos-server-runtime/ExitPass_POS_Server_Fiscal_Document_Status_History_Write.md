# ExitPass POS Server Fiscal Document Status History Write

## 1. Purpose

This note documents the first lifecycle persistence extension for fiscal document creation.

Fiscal document creation now writes the fiscal document header and one initial fiscal document status history row in the same PostgreSQL transaction.

This slice does not add fiscal lines, tenders, taxes, discount/privilege details, totals, reports, Digital SI, Annex E, X/Z, statutory discount validation, payment finality ownership, or gate/exit behavior.

## 2. Tables Written

The PostgreSQL adapter writes only:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`

No other POS schema tables are written by this slice.

## 3. Same-Transaction Rule

`PostgresFiscalDocumentRepository` opens one PostgreSQL transaction and performs:

1. insert into `pos.fiscal_documents`
2. insert into `pos.fiscal_document_status_history`
3. commit

If either insert fails, the transaction is rolled back and the repository raises `FiscalDocumentPersistenceException`.

This prevents a header row from being committed without its initial local lifecycle history row.

## 4. Initial Status Rule

The status history row uses the same status code as the fiscal document header:

- header: `fiscal_document_status_code_id`
- history: `new_fiscal_document_status_code_id`

The initial row uses:

- `prior_fiscal_document_status_code_id = NULL`
- `status_reason_code_id = NULL`
- `status_reason_text = NULL`
- `actor_ref = NULL`
- `service_identity_ref = NULL`

The current runtime command does not yet carry actor or service identity references. The schema permits those fields to be null, so no new runtime authority or identity fields were added for this slice.

## 5. Fail-Closed Behavior

Existing validation guardrails still run before persistence.

No fiscal document header or status history row is written when:

- payable basis is missing
- upstream payment/finality reference is missing
- statutory discount reference is pending, rejected, expired, unresolved, or inconsistent
- raw ID/evidence payload markers are present
- required local fiscal schema context is missing

If the header insert fails, the status history insert is not committed. If the status history insert fails, the header insert is rolled back.

## 6. Authority Boundary

The locked boundary remains:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

The status history row is local fiscal document lifecycle evidence only. It is not payment finality, statutory discount approval, ExitAuthorization, gate execution, or a full audit subsystem.

## 7. Intentionally Unsupported

This slice intentionally does not implement:

- fiscal line persistence
- tender persistence
- tax detail persistence
- discount/privilege detail persistence
- total persistence
- BIR X/Z reporting
- Annex E reporting
- Digital SI behavior
- statutory discount entitlement validation
- raw evidence/image storage
- POS-owned payment finality
- exit authorization or gate behavior

## 8. Test Posture

Tests inspect SQL and repository behavior to verify:

- only `pos.fiscal_documents` and `pos.fiscal_document_status_history` are targeted
- SQL uses parameters
- status history uses the same status code parameter as the header
- repository code uses one transaction with commit and rollback paths
- no unsupported table writes or authority-leaking behavior are introduced

Disposable PostgreSQL integration testing remains a future approved slice. The current adapter tests validate SQL mapping and transaction usage without requiring developer database secrets.
