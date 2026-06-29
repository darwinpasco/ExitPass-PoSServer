# ExitPass POS Server Statutory Discount Boundary

## 1. Decision Summary

Statutory discount validation remains an Operator Console workflow backed by the ExitPass Discount/Policy service.

POS Server does not adjudicate Senior Citizen, PWD, or other statutory entitlement, evidence, identity, residency, or operator approval.

POS Server consumes approved payable-basis and validation references from the owning service and fiscalizes the approved result.

Authority statement:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

## 2. Ownership Split

Operator Console owns:

- operator-facing validation workflow
- device, shift, role, and site enforcement
- Senior/PWD evidence capture posture
- operator attestation
- supervisor review or override
- validation approval/rejection activity
- validation audit and reporting UX

Central PMS / Discount Service owns:

- authoritative statutory discount validation record
- policy resolution
- local ordinance vs national fallback selection
- approved payable-basis effect
- non-reevaluation rule
- API boundary for WebPay, Operator Console, and POS/Invoicing

POS Server owns:

- BIR-authorized fiscal document issuance
- fiscal line/tender/tax/discount representation
- fiscal totals
- invoice/receipt discount and VAT treatment based on approved payable basis
- fiscal audit trail
- X/Z/BIR report impact
- generated fiscal outputs and evidence references

## 3. POS Server Must Not

POS Server must not:

- approve Senior/PWD entitlement
- review or store raw ID images
- decide if the entitled person is present
- determine local ordinance eligibility
- compute statutory entitlement independently from an approved payable basis
- override Central PMS payment/payable-basis authority
- create discount validation records as if it owns the validation workflow
- turn failed or pending validation into fiscal discount treatment

## 4. Runtime Rule

POS Server may fiscalize statutory discount treatment only when it receives an approved payable-basis or discount-validation reference from the owning service.

If discount approval is missing, rejected, expired, unresolved, or inconsistent, POS Server must fail closed or produce the fiscal document without statutory discount treatment according to the approved payable basis.

POS Server must preserve references needed for audit reconstruction but must not copy sensitive evidence payloads.

## 5. Data / Reference Posture

POS Server may store references such as `statutory_discount_validation_ref` or an approved discount reference if the schema/API supports it.

POS Server should use controlled-code families only for fiscal representation and audit posture.

BIR/accounting-sensitive discount classifications remain blocked until explicitly approved.

Security/Privacy-sensitive evidence classifications remain blocked until explicitly approved.

## 6. Implementation Implications

Future fiscal document creation logic must consume discount results, not validate them.

Future POS Server APIs must not expose entitlement approval endpoints.

Future tests must prove POS Server rejects or avoids fiscalizing unapproved discount inputs.

Runtime fiscalization should remain traceable to the upstream approved payable basis.
