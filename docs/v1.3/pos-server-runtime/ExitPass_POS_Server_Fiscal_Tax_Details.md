# ExitPass POS Server Fiscal Tax Details

## Decision Summary

Fiscal document creation now accepts fiscal tax detail inputs and persists them to the existing `pos.fiscal_tax_details` table as fiscalization detail/evidence for an upstream approved payable basis.

This slice does not calculate tax, finalize BIR reporting, determine statutory discount entitlement, or establish tax return/payment finality. Tax detail rows represent the fiscal tax allocation that POS Server was instructed to fiscalize.

## Tables Written

Successful fiscal document creation writes, in one PostgreSQL transaction:

1. `pos.fiscal_documents`
2. `pos.fiscal_document_status_history`
3. `pos.fiscal_document_links`
4. `pos.fiscal_document_lines`
5. `pos.fiscal_tenders`
6. `pos.fiscal_tax_details`

The transaction commits only after every requested row is inserted. If any insert fails, the transaction is rolled back and the API returns a deterministic persistence failure through the existing fail-closed path.

## Tax Detail Input Posture

The API accepts tax details through `taxDetails`. Each row is schema-backed and fiscalization-only:

- `taxTypeCodeId`
- `taxClassificationCodeId`
- `taxableAmountMinorUnits`
- `taxAmountMinorUnits`
- `currencyCode`
- optional `lineSequence`
- optional `taxRate`
- optional `taxContext`

When `lineSequence` is supplied, it must reference an input fiscal line sequence in the same request. The PostgreSQL adapter resolves that sequence to the `fiscal_document_line_id` generated in the same transaction.

## Validation Posture

Runtime validation rejects tax detail rows when:

- tax type or classification code IDs are missing;
- taxable amount or tax amount is negative;
- tax rate is negative;
- currency is not a three-letter uppercase code or does not match the payable-basis/document currency;
- line-scoped tax detail references a missing line sequence;
- optional context entries are blank;
- context contains raw evidence, credential, token, secret, provider callback, or payment payload markers.

## Boundary Rules

Tax detail rows do not establish BIR report finality, tax return finality, statutory entitlement, payment finality, refund/reversal authority, exit authorization, or gate execution authority.

Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.

## Intentionally Unsupported

This slice does not implement:

- discount/privilege detail persistence;
- fiscal total persistence;
- BIR X/Z reports;
- Annex E reports;
- Digital SI behavior;
- statutory discount entitlement validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization or gate behavior.

Manual API testing remains required before commit approval.
