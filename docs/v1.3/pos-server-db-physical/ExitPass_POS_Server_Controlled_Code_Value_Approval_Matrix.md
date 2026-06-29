# ExitPass POS Server Controlled-Code Value Approval Matrix

## 1. Purpose

This matrix defines the approval posture for future controlled-code family groups.

It is a governance planning document only. It does not create actual controlled-code values, source JSON files, seed SQL, generated SQL, database rows, folders, scripts, CI workflows, migrations, Atlas config, or source code.

## 2. Approval Matrix

| Family group | Primary owner | Minimum approval before values | Initial seed posture |
| --- | --- | --- | --- |
| Operational statuses | Operations | Operations plus Engineering validation review | Candidate for first seed. |
| Health statuses | Operations | Operations plus Engineering validation review | Candidate for first seed. |
| Channel/terminal types and capabilities | Operations | Operations plus Engineering architecture review | Candidate only if no fiscal authority is implied. |
| Configuration/audit action posture | Engineering | Engineering plus Operations review where operational procedures are affected | Candidate for first seed. |
| Access audit action/result posture | Security/Privacy | Security/Privacy plus Engineering review | Candidate only for non-sensitive posture; sensitive outcomes remain blocked. |
| Retry lifecycle posture | Engineering | Engineering review | Candidate for first seed. |
| Exception lifecycle posture | Engineering | Engineering review | Candidate for first seed. |
| Report/export lifecycle statuses | Engineering | Engineering review; BIR/accounting review if content-specific | Candidate only when lifecycle-only. |
| Fiscal document types/statuses | BIR/accounting | BIR/accounting plus Engineering architecture review | Blocked pending BIR/accounting approval. |
| Fiscal line types/statuses | BIR/accounting | BIR/accounting plus Engineering review | Blocked pending fiscal treatment review. |
| Tender types | Operations | Operations plus Engineering authority-boundary review | Blocked until payment-finality wording is reviewed. |
| Tax types/classifications | BIR/accounting | BIR/accounting approval required | Blocked. |
| Discount/privilege classifications | BIR/accounting | BIR/accounting approval required; Security/Privacy review if evidence-sensitive | Blocked. |
| Fiscal total types | BIR/accounting | BIR/accounting approval required | Blocked. |
| Sequence families/states/gap reasons | BIR/accounting | BIR/accounting plus Engineering fiscal-safety review | Blocked where numbering or gap treatment is implied. |
| Counter families/statuses | BIR/accounting | BIR/accounting plus Engineering fiscal-safety review | Blocked where reset, Z, or GTA behavior is implied. |
| Digital SI URL statuses and access values | Security/Privacy | Security/Privacy plus Engineering review | Blocked for security-sensitive values. |
| Reprint types/statuses/reasons/output types | Operations | Operations plus BIR/accounting review where fiscal labeling or reason codes are affected | Partially blocked. |
| Adjustment types/statuses/reasons | BIR/accounting | BIR/accounting plus Engineering review | Blocked. |
| Report types/scopes/output types | BIR/accounting | BIR/accounting review for fiscal/report semantics; Engineering review for lifecycle-only posture | Partially blocked. |
| X/Z report kinds | BIR/accounting | BIR/accounting approval required when tied to BIR report behavior | Blocked when behavior-sensitive. |
| Annex E types | BIR/accounting | BIR/accounting and accreditation review | Blocked. |
| Export types/package/item/validation/profile types | Vendor/accreditation | Vendor/accreditation plus Engineering review; BIR/accounting review for BIR outputs | Blocked for profile/content values. |
| ARTS POSLog/export profile values | Vendor/accreditation | Vendor/accreditation plus BIR/accounting review | Blocked. |
| Fiscal action audit action/result types | Engineering | Engineering plus Operations review where operational procedures are affected | Candidate for first seed if evidence-only. |
| Privileged action audit values | Security/Privacy | Security/Privacy plus Operations review | Blocked until privileged-action vocabulary is approved. |
| Recovery/check/anchor/security reference types/statuses | Security/Privacy | Security/Privacy plus Engineering recovery/fiscal-safety review | Blocked. |
| Privacy classifications | Security/Privacy | Security/Privacy approval required | Blocked. |
| Fiscal identity statuses | BIR/accounting | BIR/accounting plus Vendor/accreditation review where registration/accreditation is affected | Blocked. |

## 3. Candidate First-Seed Rule

A family marked as a candidate for first seed still requires explicit value approval before source JSON creation.

Candidate status means only that the family is lower risk and may be sequenced earlier once values are reviewed.

## 4. Blocked Family Rule

A blocked family must not have values created until the required owner approval is complete.

Blocked status applies to:

- BIR/accounting-sensitive values
- tax and privilege values
- fiscal document and report semantics
- privacy/security-sensitive values
- vendor/accreditation profile values
- values that could imply payment, exit, gate, offline issuance, or independent terminal fiscal authority

## 5. Review Checklist

Before approving any family for source JSON creation, reviewers must confirm:

- values are baseline reference data, not sample data
- every value has a stable `code_key`
- every value has display text and description
- every value has source reference
- every value has active/deprecated posture
- every value has effective dating
- sort order is deterministic
- no value implies prohibited authority ownership
- no value encodes unresolved BIR/accounting treatment
- no value encodes unresolved Security/Privacy treatment
- no value encodes vendor/accreditation authority before approval
- deterministic UUID name inputs can be generated exactly

## 6. Stop Condition

Stop before creating values if any reviewer cannot confirm ownership, authority boundary, source reference, or approval status.

Do not create partial source JSON files for blocked families as placeholders.

