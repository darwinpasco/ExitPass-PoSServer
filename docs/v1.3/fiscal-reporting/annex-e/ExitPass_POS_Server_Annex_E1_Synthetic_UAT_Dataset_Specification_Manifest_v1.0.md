# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification Manifest v1.0

## 1. Identity and authority

| Item | Value |
|---|---|
| Specification ID | `annex-e1-synthetic-uat-dataset:v1.0` |
| Version | `v1.0` |
| Repository baseline | `22aad789ec52d10ef72124687cdcdaec2d563436` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Included / excluded scenarios | 19 / 6 |
| Specification status | `READY_FOR_REVIEW` |
| Specification approval | `PENDING_SPECIFICATION_REVIEW` |
| Dataset implementation | `NOT_AUTHORIZED` |
| Data loading / execution | `NOT_AUTHORIZED` / `NOT_AUTHORIZED` |
| Workbook generation | `NOT_AUTHORIZED` |
| External delivery / BIR submission | `NOT_AUTHORIZED` / `NOT_AUTHORIZED` |
| Production | `NOT_AUTHORIZED` |

## 2. Document hash inventory

Hashes are SHA-256 over the exact committed-candidate UTF-8 file bytes. Markdown files use LF line endings and no BOM. The manifest intentionally does not hash itself.

| Document | SHA-256 |
|---|---|
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.0.md` | `f07ac74a241c56b64114909e9685d424e773bda103d7fcbab7a8d16c666637e8` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.0.md` | `1b13cd6592e7474952595da17c654482538ba7e6bef70af1adcad819568b8c9a` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.0.md` | `34168716d270458e2d256047fb40febc610c299dca4f5599f3f7ef485d26de18` |
| `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.0.md` | `b25c01b9545c3a83dc85058b760fa710a4aea2972e132b0c327d2cea8a7cdf69` |
| `ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.0.md` | `706518ef774539b50c2b2d3cfc17205ef0005402878972b6ec237bba293b43db` |

## 3. Approval roles

Synthetic Data Steward prepares; Accounting Reviewer verifies exact preservation of the approved calculation profile; Technical Reviewer verifies schema/runtime terminology and implementability; Security/Privacy Reviewer verifies synthetic identity and prohibited-data controls; Controlled UAT Authorizer decides any later implementation authority. No personal name is assigned by this manifest.

## 4. Change control

Any change to a hashed document requires a new hash before review. Any approved post-review change to identity rules, values, source populations, scenarios, chronology, mappings, or reconciliations requires a new specification version and renewed review. Approval of this package does not authorize fixture implementation, environment provisioning, database access/loading, scenario execution, workbook generation, delivery, submission, Production, E-2 through E-5, ARTS POSLog, signing/encryption, or destructive retention.
