# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification Manifest v1.1

## 1. Package control

| Item | Value |
|---|---|
| Package | ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification |
| Specification identifier | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.1` |
| Version | `v1.1` |
| Document status | `READY_FOR_V1_1_SPECIFICATION_AUTHORIZATION_REVIEW` |
| Creation baseline | `d16f23cef27d87f451ef08ff9f74eb4afe800c8f` |
| Annex profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile | `ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Included scenarios | 19 |
| Excluded scenarios | 6 |
| Expected Annex rows | 22 |
| Current canonical authorization token | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |

The specification is ready for review only. Dataset implementation, validator implementation, runtime modification, database access or loading, environment provisioning, role assignment, scenario execution, evidence acceptance, workbook generation, external delivery, BIR submission, and Production remain unauthorized.

## 2. Governed document inventory and hashes

Hashes are SHA-256 over the exact repository file bytes. Files use UTF-8 text and LF line endings. No BOM, Unicode normalization, whitespace normalization, or line-ending conversion is performed before hashing. Lowercase hexadecimal is the governed representation.

| Governed v1.1 content document | SHA-256 |
|---|---|
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.1.md` | `af47f89a103f58acc1bd233954c3d02a3ad92a776354c3a7193f2965616a6592` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.1.md` | `f1234d19679350cc3585d4ffa45f0b29ed442fcd7734a73be9550dd49911216e` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.1.md` | `dbb42746fa229ace418feb34605f93c20ed8eabe97bc8e2f8b36f39ecf609c3d` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.1.md` | `e520f97287632cae460612e5c8ac29408a56b40a086d17004da2ccfbb177490d` |
| `ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.1.md` | `18d148415fe3e20564b16b4ef64d5ab0d9f46d9f927109be0ce03ede6d40c4c8` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Correction_Resolution_Matrix_v1.1.md` | `b1bd98bd000f76c07b1cab4b90568ab42f7a51b659d7a00513de32cdc015d0aa` |

This manifest intentionally does not hash itself. `README.md` is an index and is not governed by this manifest. A byte change in any listed document invalidates the corresponding recorded hash and requires a new finalized hash before review.

## 3. Historical and change-control posture

The six v1.0 specification documents remain immutable historical records of the blocked package. v1.1 supersedes v1.0 only for future review and implementation consideration. It does not alter the v1.0 authorization review, mutate a v1.0 identity, or authorize a runtime or operational change.

All eight documentation findings are closed as `RESOLVED_IN_V1_1` in the correction-resolution matrix. All ten external confirmations remain `UNRESOLVED`. The six excluded scenarios remain non-executable. Any change to a listed file requires a new version, recalculated hashes, and another authorization review.

## 4. Approval and implementation gates

| Gate | Authority | Status |
|---|---|---|
| Specification review | Annex E governance reviewer | pending separate review |
| Accounting calculation profile | Accounting | approved only under `Z-012B-ACCOUNTING-APPROVAL-001` |
| Dataset implementation | Product Owner and technical governance after specification review | not authorized |
| Offline validator implementation | Product Owner and technical governance after specification review | not authorized |
| Runtime-contract prerequisites | Product Owner, technical governance, and Security/Operations for the controlled-clock and isolated-environment boundary | not authorized |
| Database loading | Controlled UAT authority | not authorized |
| Environment provisioning | Controlled UAT authority and Operations | not authorized |
| Scenario execution and evidence acceptance | Controlled UAT authority and assigned reviewers | not authorized |
| Workbook generation, delivery, submission, Production | separately governed authorities | not authorized |

The exact next bounded activity is **Review Annex E-1 Synthetic UAT Dataset Specification v1.1**. A later merged review must explicitly record `AUTHORIZED_FOR_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` before dataset or validator implementation may begin.
