# ExitPass POS Server Fiscal Totals

## Decision Summary

Fiscal document creation now accepts fiscal total inputs and persists them to the existing `pos.fiscal_totals` table.

Fiscal total rows are fiscal representation and summary rows for an approved upstream payable basis. They do not establish BIR report finality, tax return finality, payment finality, statutory entitlement, refund/reversal authority, exit authorization, or gate execution.

Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.

## Tables Written

Successful fiscal document creation writes, in one PostgreSQL transaction:

1. `pos.fiscal_documents`
2. `pos.fiscal_document_status_history`
3. `pos.fiscal_document_links`
4. `pos.fiscal_document_lines`
5. `pos.fiscal_tenders`
6. `pos.fiscal_tax_details`
7. `pos.fiscal_discount_privilege_details`
8. `pos.fiscal_totals`

The transaction commits only after every requested row is inserted. If any insert fails, the transaction is rolled back and the API returns a deterministic persistence failure through the existing fail-closed path.

## Input Posture

The API accepts fiscal totals through `totals`. Each row is schema-backed and fiscalization-only:

- `totalTypeCodeId`
- `amountMinorUnits`
- `currencyCode`
- optional `totalContext`

The current table has no sequence, display order, source reference, or report finalization fields. The runtime model therefore does not invent those fields.

## Validation Posture

Runtime validation rejects total rows when:

- total type code ID is missing;
- amount is negative;
- currency is not a three-letter uppercase code or does not match the payable-basis/document currency;
- total type code appears more than once in the same document request;
- optional context entries are blank;
- context values contain raw evidence, credential, token, secret, provider callback, or payment payload markers.

Pending, rejected, expired, unresolved, or inconsistent statutory discount references are still rejected before persistence.

## Boundary Rules

Fiscal total rows do not establish:

- BIR report finality;
- X-read or Z-read finality;
- Annex E finality;
- tax return finality;
- statutory entitlement;
- local ordinance eligibility;
- operator or supervisor approval;
- raw evidence storage;
- payment finality;
- refund/reversal authority;
- exit authorization;
- gate execution authority.

Totals are accepted as explicit fiscalization summary rows. POS Server does not derive missing totals or compute BIR/accounting formulas in this slice.

## Intentionally Unsupported

This slice does not implement:

- BIR X/Z reports;
- Annex E reports;
- Digital SI behavior;
- statutory discount entitlement validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization or gate behavior.

Manual API testing remains required before commit approval.
