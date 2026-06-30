# ExitPass POS Server Fiscal Document Lines

## Decision Summary

Fiscal document creation now persists fiscal document line rows to `pos.fiscal_document_lines` in the same PostgreSQL transaction that writes the fiscal document header, initial status history, and supported document-to-document links.

This slice is fiscal-line persistence only. It does not implement tenders, tax details, discount/privilege details, totals, BIR reports, Digital SI, Annex E, X/Z behavior, statutory discount validation, payment finality ownership, or gate/exit behavior.

## Tables Written

The PostgreSQL persistence adapter writes only:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`
- `pos.fiscal_document_lines`

No other POS schema tables are written by this slice.

## Line Input Posture

Fiscal document creation now requires at least one fiscal line. Each line carries schema-backed fiscalization detail only:

- document-local line sequence
- fiscal line type controlled-code ID
- optional fiscal line status controlled-code ID
- description
- quantity
- unit, gross, discount, tax, and net amounts in minor units
- currency code
- optional upstream source reference
- optional line context JSON object

Line inputs do not include entitlement approval, raw evidence, local ordinance decision, payment finality, exit authorization, gate execution, BIR report, tender, tax-detail, discount-detail, or total fields.

## Validation Posture

The runtime service rejects line input before persistence when:

- no fiscal lines are supplied
- line sequence is not positive
- duplicate line sequence values appear in the same document
- required line type or description is missing
- quantity is not positive
- any amount is negative
- net amount is not consistent with gross amount minus discount amount plus tax amount
- currency code is not a three-letter uppercase code after normalization
- optional source/context values are blank
- raw evidence or credential markers appear in line text/reference/context fields

This validation is local structural fiscalization validation only. It is not statutory discount entitlement validation and does not establish payment finality.

## Transaction Rule

Fiscal document creation uses one PostgreSQL transaction:

1. Insert `pos.fiscal_documents`.
2. Insert the initial `pos.fiscal_document_status_history` row.
3. Insert supported `pos.fiscal_document_links` rows.
4. Insert `pos.fiscal_document_lines` rows.
5. Commit only after all inserts succeed.

If any insert fails, the transaction rolls back.

## Statutory Discount Boundary

The locked rule remains:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

Fiscal lines represent the approved fiscalized result. POS Server still does not approve entitlement, review evidence, decide local ordinance eligibility, compute statutory entitlement independently, or turn pending/rejected/expired/unresolved/inconsistent discount validation into approved fiscal discount treatment.

## Intentionally Unsupported

Future slices may add tenders, tax details, discount/privilege details, totals, and reporting posture. This slice intentionally does not add:

- fiscal tenders
- fiscal tax details
- fiscal discount/privilege details
- fiscal totals
- BIR X/Z reports or Annex E
- Digital SI behavior
- statutory discount validation
- payment finality ownership
- exit authorization or gate execution
- schema changes or migrations
