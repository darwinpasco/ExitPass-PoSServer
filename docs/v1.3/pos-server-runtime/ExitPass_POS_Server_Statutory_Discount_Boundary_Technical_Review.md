# ExitPass POS Server Statutory Discount Boundary Technical Review

## 1. Review Summary

This review covers `ExitPass_POS_Server_Statutory_Discount_Boundary.md`.

The boundary document is documentation only. It establishes a guardrail before POS Server fiscal document runtime work begins.

## 2. Boundary Review

The document clearly states:

- Operator Console validates statutory discount claims.
- Central PMS / Discount Service authorizes the payable-basis effect.
- POS Server fiscalizes the approved payable basis.

The document includes the required authority statement:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

## 3. Ownership Split Review

Operator Console ownership is limited to the operator-facing validation workflow, device/shift/role/site enforcement, evidence capture posture, operator attestation, supervisor review/override, validation approval/rejection activity, and validation audit/reporting UX.

Central PMS / Discount Service ownership is limited to the authoritative statutory discount validation record, policy resolution, local ordinance vs national fallback selection, approved payable-basis effect, non-reevaluation rule, and API boundary.

POS Server ownership is limited to BIR-authorized fiscal document issuance, fiscal representation, fiscal totals, invoice/receipt discount and VAT treatment from approved payable basis, fiscal audit trail, report impact, and fiscal output/evidence references.

## 4. Authority Leakage Review

The document prevents authority leakage by explicitly stating POS Server must not:

- approve Senior/PWD entitlement
- review or store raw ID images
- decide if the entitled person is present
- determine local ordinance eligibility
- compute statutory entitlement independently from approved payable basis
- override Central PMS payment/payable-basis authority
- create validation records as workflow owner
- fiscalize failed or pending validation as approved statutory discount treatment

## 5. Runtime Guardrail Review

The runtime rule is clear: POS Server may fiscalize statutory discount treatment only when it receives an approved payable-basis or discount-validation reference from the owning service.

If approval is missing, rejected, expired, unresolved, or inconsistent, POS Server must fail closed or produce the fiscal document without statutory discount treatment according to the approved payable basis.

The document also requires audit references without copying sensitive evidence payloads.

## 6. Scope Discipline Review

Confirmed:

- boundary is documentation only
- no source code changed
- no `db/state` SQL changed
- no controlled-code JSON changed
- no generated SQL changed
- no migrations added
- no CI changed
- no Atlas config changed
- no sample data added
- POS Server does not own statutory discount validation
- Operator Console / Discount Service / Central PMS / POS Server ownership split is clear
- the document prevents authority leakage before fiscal document runtime work

## 7. Finding Counts

P0 0 / P1 0 / P2 0 / Editorial 0

## 8. Final Recommendation

The statutory discount boundary note is ready for review as a runtime design guardrail. Do not proceed to POS Server fiscal document runtime behavior until implementation plans preserve this ownership split.
