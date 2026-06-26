# ExitPass POS Server DB Validation / Rebuild Open Questions

## 1. Purpose

This document tracks open questions for future POS Server database validation, clean rebuild, drift-check tooling, and CI evidence. It does not reopen approved database object decisions.

## 2. Engineering / Operations

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Which PostgreSQL version and client tools will be standard for local and CI validation? | PostgreSQL is the default engine; exact version/client is pending. | Blocks executable script finalization. | Confirm supported PostgreSQL and `psql` versions. |
| How should disposable local databases be provisioned? | Clean rebuild requires disposable target; mechanism not selected. | Blocks rebuild script implementation. | Decide local database provisioning approach. |
| How should credentials be supplied without committing secrets? | Secrets must not be committed. | Blocks CI/local script implementation. | Define environment variable or secret manager posture. |

## 3. CI/CD

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Which CI runner image will provide PostgreSQL client/server support? | CI workflow not created. | Blocks CI validation. | Select runner image or service container strategy. |
| Should validation evidence be stored as artifacts or only PR log output? | Evidence format is open. | Blocks final PR evidence workflow. | Decide evidence retention requirements. |
| What failures should block merge? | P0/P1-style validation failures should block; exact categories pending. | Blocks CI policy. | Define required vs advisory checks. |

## 4. Physical DB Design

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Should future SQL application order be manifest-driven or directory-order-driven? | Deterministic order is required; manifest is not created. | Blocks future rebuild scripts. | Decide manifest format before scripts. |
| Should standalone indexes be represented separately or table-inline where practical? | Current first slice has no standalone indexes. | Blocks future index validation details. | Decide before adding standalone index artifacts. |
| How should comments be compared for drift? | Comments exist in first-slice SQL; drift comparison method pending. | Non-blocking for planning, may block full drift tooling. | Decide whether comment drift is required evidence. |

## 5. Tooling / Atlas

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Should Atlas be used for optional state comparison? | Atlas is optional and not approved for this task. | Blocks Atlas workflow only. | Evaluate Atlas after basic rebuild/validation approach is approved. |
| If Atlas is used later, how will generated output be kept out of baseline unless reviewed? | No local drift promotion rule applies. | Blocks Atlas adoption. | Define review and artifact boundaries. |
| What non-Atlas drift fallback is required? | Static SQL and catalog inventory can be used. | Blocks minimal drift implementation if no Atlas. | Define catalog-query-based drift report. |

## 6. Security / Privacy

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Can validation logs include object comments and metadata containing references? | First-slice comments are authority-safe; future metadata may be sensitive. | Blocks evidence publication policy. | Review with Security/Privacy before CI artifact retention. |
| What data must be masked in future validation evidence? | No seed/sample data exists now. | Blocks future evidence once data artifacts exist. | Define evidence redaction policy. |
| Who can run drift checks against shared non-production databases? | Drift checks must not promote live state. | Blocks shared environment drift workflow. | Define access and approval process. |

