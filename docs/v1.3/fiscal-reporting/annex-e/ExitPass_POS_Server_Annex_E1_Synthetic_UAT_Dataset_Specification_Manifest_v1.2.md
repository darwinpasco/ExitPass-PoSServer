# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification Manifest v1.2

## 1. Package control

| Item | Value |
|---|---|
| Package | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.2` |
| Version | `v1.2` |
| Status | `READY_FOR_V1_2_SPECIFICATION_AUTHORIZATION_REVIEW` |
| Creation baseline | `f4a3a262f0e35bd91d58197fbf59aaeeda911c18` |
| Annex profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Canonical implementation gate | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Included / excluded scenarios | 19 / 6 |
| Expected Annex rows | 22 |
| Hash algorithm | SHA-256 |
| Hash text | lowercase hexadecimal |

## 2. Governed content inventory

Hashes are over exact merged-candidate file bytes. Files are UTF-8 without BOM with LF line endings. No newline normalization is performed during hashing.

| Governed document | SHA-256 |
|---|---|
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.2.md` | `70c2721645bd7846845ce8ee4be911a127ce6ab76b6dde08977f3e3a74592d9c` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.2.md` | `a6c82c296c431940649d16efb80fbebc8fc016dbd33e77c9df23500edce568e1` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.2.md` | `eb93666cb3893dea742c45a066ffab7e1edc0cbed59dd955f1172b928ab0063e` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.2.md` | `90f9369d3bcc1c697ae480f7bc50458ae13fc7f5de810187ec14d8b275b9ece4` |
| `ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.2.md` | `856a8665938a204a82b8e6dbaf38db00842aab3d83e2ae62185d6b5e694c38b1` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.2.md` | `ec060c47148b8d6ec24118584b34efae4b4460684569fe5866b708a6dcffb63f` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Correction_Resolution_Matrix_v1.2.md` | `d9fd5907f11c93efd7f6d3a2d450956f0164d7b317a0117a28924e7538555450` |

This manifest governs exactly the seven documents above. It excludes itself and `README.md`.

## 3. Historical and authorization boundary

The v1.0 and v1.1 packages and their reviews remain immutable historical records. Version 1.2 supersedes them only for future implementation consideration and does not retroactively change either prior authorization outcome.

A separate v1.2 authorization review is mandatory after merge. Dataset implementation, validator implementation, runtime clock or persisted-identity changes, other runtime/API changes, database or migration work, environment provisioning, role assignment, loading, Controlled UAT, evidence acceptance, workbook generation, delivery, BIR submission, and Production remain unauthorized.

All ten external confirmations remain `UNRESOLVED`. The six excluded scenarios remain non-executable.
