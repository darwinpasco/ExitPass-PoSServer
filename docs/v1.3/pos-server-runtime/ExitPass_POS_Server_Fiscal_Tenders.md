# ExitPass POS Server Fiscal Tenders

## Decision Summary

Fiscal document creation now persists fiscal tender rows to `pos.fiscal_tenders` in the same PostgreSQL transaction that writes the fiscal document header, initial status history, supported document-to-document links, and fiscal lines.

This slice is fiscal tender persistence only. It does not implement tax details, discount/privilege details, totals, BIR reports, Digital SI, Annex E, X/Z behavior, statutory discount validation, payment finality ownership, or gate/exit behavior.

## Tables Written

The PostgreSQL persistence adapter writes only:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`
- `pos.fiscal_document_lines`
- `pos.fiscal_tenders`

No other POS schema tables are written by this slice.

## Tender Input Posture

Fiscal document creation now requires at least one fiscal tender allocation. Each tender carries schema-backed fiscalization detail only:

- tender type controlled-code ID
- amount in minor units
- currency code
- optional Central PMS payment attempt reference
- optional Central PMS payment confirmation reference
- optional payment finality reference
- optional provider reference
- optional tender context JSON object

The current table does not define tender sequence or tender status columns, so this slice does not model them.

## Validation Posture

The runtime service rejects tender input before persistence when:

- no fiscal tenders are supplied
- tender type ID is missing
- amount is not positive
- currency code is not a three-letter uppercase code after normalization
- tender currency does not match the payable-basis currency
- optional reference/context values are blank
- raw credential, token, secret, card, provider callback, or payment payload markers appear in tender reference/context fields

Tender validation is local structural fiscalization validation only. Tender rows represent fiscal tender allocation/evidence and do not establish payment finality.

## Payment Authority Boundary

Payment finality remains upstream-owned by Central PMS or another approved payment authority. POS Server stores references such as `central_pms_payment_attempt_ref`, `central_pms_payment_confirmation_ref`, and `payment_finality_ref` only for fiscal reconstruction.

POS Server does not own PaymentAttempt lifecycle, PaymentConfirmation lifecycle, payment settlement, refund/reversal authority, exit authorization, or gate execution.

## Transaction Rule

Fiscal document creation uses one PostgreSQL transaction:

1. Insert `pos.fiscal_documents`.
2. Insert the initial `pos.fiscal_document_status_history` row.
3. Insert supported `pos.fiscal_document_links` rows.
4. Insert `pos.fiscal_document_lines` rows.
5. Insert `pos.fiscal_tenders` rows.
6. Commit only after all inserts succeed.

If any insert fails, the transaction rolls back.

## Statutory Discount Boundary

The locked rule remains:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

Tender persistence does not change statutory discount ownership. POS Server still does not approve entitlement, review evidence, decide local ordinance eligibility, compute statutory entitlement independently, or turn pending/rejected/expired/unresolved/inconsistent discount validation into approved fiscal discount treatment.

## Intentionally Unsupported

Future slices may add tax details, discount/privilege details, totals, and reporting posture. This slice intentionally does not add:

- fiscal tax details
- fiscal discount/privilege details
- fiscal totals
- BIR X/Z reports or Annex E
- Digital SI behavior
- statutory discount validation
- payment finality ownership
- refund/reversal authority
- exit authorization or gate execution
- schema changes or migrations
