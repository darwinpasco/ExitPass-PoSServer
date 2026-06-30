# ExitPass POS Server Fiscal Document Links

## Decision Summary

Fiscal document creation now persists supported document-to-document link rows in `pos.fiscal_document_links` as part of the same PostgreSQL transaction that writes the fiscal document header and initial status history row.

This slice is traceability/reference persistence only. It does not implement fiscal lines, tenders, taxes, discounts, totals, reports, Digital SI, Annex E, X/Z behavior, statutory discount validation, payment finality ownership, or gate/exit behavior.

## Tables Written

The PostgreSQL persistence adapter writes only:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`

No other POS schema tables are written by this slice.

## Link Table Posture

`pos.fiscal_document_links` is a document-to-document relationship table. Link rows are created only when the fiscal document creation command provides supported document link inputs:

- target fiscal document ID
- fiscal document link type controlled-code ID
- optional link reason controlled-code ID
- optional link reason text
- optional created-by reference

The source fiscal document ID is always the newly created fiscal document ID.

## Optional vs Required References

Required upstream fiscalization references remain enforced by the runtime service before persistence:

- upstream approved payable-basis reference
- upstream payment/finality reference
- local fiscal schema context IDs required by the existing schema

External upstream references such as payable-basis references, Central PMS parking session references, payment attempt references, payment confirmation references, vendor acknowledgement references, and statutory discount validation references remain header/context traceability fields where supported. They are not forced into `pos.fiscal_document_links` because that table requires another fiscal document as the target.

## Statutory Discount Boundary

The locked rule remains:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

Optional statutory discount validation references are traceability only. POS Server does not approve entitlement, review evidence, decide local ordinance eligibility, compute entitlement, or turn pending/rejected/expired/unresolved/inconsistent discount validation into approved fiscal discount treatment.

## Transaction Rule

Fiscal document creation uses one PostgreSQL transaction:

1. Insert `pos.fiscal_documents`.
2. Insert the initial `pos.fiscal_document_status_history` row.
3. Insert supported `pos.fiscal_document_links` rows.
4. Commit only after all inserts succeed.

If any insert fails, the transaction rolls back and the API/runtime returns deterministic persistence failure behavior.

## Intentionally Unsupported

This slice does not add:

- fiscal lines, tenders, taxes, discounts, or totals
- BIR reporting, Annex E, X/Z, or Digital SI behavior
- statutory discount entitlement validation
- raw evidence/image storage
- payment finality ownership
- exit authorization or gate execution
- schema changes or migrations
