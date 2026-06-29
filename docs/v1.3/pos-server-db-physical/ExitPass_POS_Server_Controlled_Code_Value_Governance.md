# ExitPass POS Server Controlled-Code Value Governance

## 1. Purpose

This document defines governance rules for future baseline controlled-code value approval.

It does not create actual controlled-code values, source JSON files, seed SQL, generated SQL, database rows, folders, scripts, migrations, Atlas config, CI workflows, or application source code.

## 2. Governance Principles

Future controlled-code values must be:

- approved before source JSON files are created
- traceable to an accountable governance owner
- traceable to an approved source reference
- stable after approval
- separated from schema SQL
- separated from sample or transaction data
- validated before generated SQL is produced

Repository source remains the source of truth. Live or local database state must not be promoted into source values.

## 3. Governance Owners

Controlled-code families should be assigned to one primary governance owner.

| Governance owner | Scope |
| --- | --- |
| Engineering | Technical lifecycle, validation, retry, exception, configuration, export/request status posture, and generated-source mechanics. |
| Operations | Site, channel/terminal, health, operational status, reprint workflow, continuity operations, and supervised operating procedures. |
| BIR/accounting | Fiscal document types, tax, discount/privilege, fiscal totals, report kinds, Annex E, fiscal adjustment, fiscal identity, and BIR-sensitive report/export posture. |
| Security/Privacy | Digital SI URL access/security, access audit, privacy classifications, security reference context, credential/token/evidence reference posture, and sensitive access outcomes. |
| Vendor/accreditation | Supplier, accreditation, export profile, ARTS POSLog profile, vendor acknowledgement, and externally certified profile posture. |

## 4. Minimum Approval Requirement

Before any controlled-code values may be created, the proposed family must have:

- assigned governance owner
- approved `code_set_key`
- approved value list
- approved `code_key` for each value
- approved display name and description for each value
- approved source reference
- approved active/deprecated posture
- approved effective dating
- approved sort order
- authority-boundary review
- confirmation that no sample or transaction data is included

For BIR/accounting, Security/Privacy, and Vendor/accreditation families, the accountable owner must approve the values before source JSON creation.

## 5. Low-Risk First-Seed Candidates

The first future seed implementation may prioritize lower-risk families that do not define fiscal content, tax treatment, privacy classification, vendor accreditation, or report/accreditation semantics.

Candidate family groups:

- operational statuses
- health statuses
- access audit result/action posture, when no sensitive outcome taxonomy is finalized
- retry/exception lifecycle posture
- report/export lifecycle statuses where not BIR-content-specific
- configuration/audit action posture

These are still not approved values. They are only candidate groups for a future source-value task.

## 6. Blocked or Later-Review Families

These family groups should remain blocked until the accountable owner completes review:

- fiscal document types
- tax types
- tax classifications
- discount/privilege classifications
- fiscal total types
- Annex E types
- X/Z report kinds if tied to BIR report behavior
- privacy classifications
- Digital SI URL access/security-sensitive values
- ARTS POSLog/export profile values
- vendor/accreditation profile values

The blocked posture prevents accidental approval of values that could encode BIR/accounting, Security/Privacy, or accreditation decisions prematurely.

## 7. Authority-Leak Prevention

Future value reviews must reject values that imply:

- POS-owned payment finality
- POS-owned PaymentAttempt lifecycle
- POS-owned PaymentConfirmation lifecycle
- POS-owned ExitAuthorization
- gate execution authority
- independent terminal fiscal authority
- offline fiscal issuance approval
- vendor authority beyond approved reference posture
- ARTS POSLog replacing required BIR outputs

Reference-only posture remains allowed when the source clearly preserves Central PMS and external authority boundaries.

## 8. Value Change Governance

After values are approved and seeded:

- key changes are breaking changes
- semantic changes require governance review
- display text changes require review but do not necessarily change IDs
- obsolete values should be deprecated, not deleted
- replacements should be added only after approval
- sort order changes should be reviewed because they affect deterministic presentation

## 9. Stop Condition for Future Value Creation

A future value-creation task must stop before writing source JSON or generated SQL if:

- any family lacks an accountable owner
- any value lacks approval
- any value lacks a source reference
- BIR/accounting-sensitive values lack BIR/accounting review
- Security/Privacy-sensitive values lack Security/Privacy review
- vendor/accreditation values lack vendor/accreditation review
- any proposed value leaks payment, exit, gate, offline issuance, or independent terminal authority
- deterministic UUID namespace or name-input rules are not implemented exactly

## 10. Non-Decisions

This document does not create or approve any actual controlled-code values.

It also does not decide generated SQL structure, loader behavior, CI integration, or seed execution workflow.

