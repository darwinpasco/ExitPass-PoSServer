# ExitPass POS Server Fiscal Discount / Privilege Details

## Decision Summary

Fiscal document creation now accepts fiscal discount/privilege detail inputs and persists them to the existing `pos.fiscal_discount_privilege_details` table.

These rows are fiscal representation only. They record the approved discount/privilege result that POS Server was instructed to fiscalize from an upstream payable-basis decision. They do not validate Senior/PWD entitlement, decide local ordinance eligibility, approve statutory discounts, compute entitlement independently, or establish BIR report finality.

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

The transaction commits only after every requested row is inserted. If any insert fails, the transaction is rolled back and the API returns a deterministic persistence failure through the existing fail-closed path.

## Input Posture

The API accepts discount/privilege details through `discountPrivilegeDetails`. Each row is schema-backed and fiscalization-only:

- `discountPrivilegeTypeCodeId`
- `basisAmountMinorUnits`
- `discountAmountMinorUnits`
- `vatPrivilegeAmountMinorUnits`
- `currencyCode`
- optional `lineSequence`
- optional `beneficiaryRef`
- optional `evidenceRef`
- optional `approvalRef`
- optional `discountPrivilegeContext`

The current table has one type code field and no separate classification code field. The runtime model therefore does not invent a classification field.

When `lineSequence` is supplied, it must reference an input fiscal line sequence in the same request. The PostgreSQL adapter resolves that sequence to the `fiscal_document_line_id` generated in the same transaction.

## Validation Posture

Runtime validation rejects discount/privilege rows when:

- discount/privilege type code ID is missing;
- basis amount, discount amount, or VAT privilege amount is negative;
- discount amount exceeds basis amount;
- VAT privilege amount exceeds basis amount;
- currency is not a three-letter uppercase code or does not match the payable-basis/document currency;
- line-scoped detail references a missing line sequence;
- optional reference fields are present but blank;
- optional context entries are blank;
- reference/context values contain raw evidence, credential, token, secret, provider callback, or payment payload markers.

Pending, rejected, expired, unresolved, or inconsistent statutory discount references are still rejected before persistence.

## Boundary Rules

Discount/privilege rows do not establish:

- statutory entitlement approval;
- local ordinance eligibility;
- operator or supervisor approval;
- raw evidence storage;
- payment finality;
- refund/reversal authority;
- BIR report finality;
- exit authorization;
- gate execution authority.

Reference fields such as `evidenceRef` and `approvalRef` are references only. Raw ID images, raw evidence files, provider callback payloads, credentials, tokens, and secrets are not accepted.

## Intentionally Unsupported

This slice does not implement:

- fiscal totals;
- BIR X/Z reports;
- Annex E reports;
- Digital SI behavior;
- statutory discount entitlement validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization or gate behavior.

Manual API testing remains required before commit approval.
