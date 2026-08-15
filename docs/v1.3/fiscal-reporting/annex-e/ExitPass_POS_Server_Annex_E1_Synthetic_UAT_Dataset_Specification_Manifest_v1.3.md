# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification Manifest v1.3

## 1. Control

| Item | Value |
|---|---|
| Package | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.3` |
| Baseline | `cfb9d0c49912bd2efdc5185bdc9725b7c1c936d9` |
| Readiness | `READY_FOR_V1_3_SPECIFICATION_AUTHORIZATION_REVIEW` |
| Canonical implementation gate | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Member hash | SHA-256 of exact repository bytes; lowercase hex |
| Manifest membership | This manifest excludes itself |

No line-ending, encoding, Unicode, whitespace, or final-newline normalization is permitted before member hashing.

## 2. Governed members

| Repository-relative path | Bytes | SHA-256 |
|---|---:|---|
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.3.md` | 1402 | `e100067d33b83607ebc932379658d2c6dc3221339c4bd29500d8369fcf658d64` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.3.md` | 12893 | `49b73ed77aeb455d8829abe874a4ecc868f89b99aede615271510394d2064ac2` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_Population_v1.3.md` | 18038 | `2ce583fcb1762e8ad6f844c31e909a7ba9d723c640073f5cafee956f3343cb10` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Correction_Resolution_Matrix_v1.3.md` | 3274 | `29b6eb985c0376b9fc7ae2eaff8d9a3c5bfcb742f5a2b29ef2813ca7d186c9fa` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.3.md` | 10359 | `6d9faaceed0cf3e4c109d3f14fe628b2f48663cbf6bab03fa8ccebf48da1b594` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.3.md` | 3597 | `8fc6fdb9ab627a9c02a0cd103c5f4474de99ce124eeb4dde5da5f3a5c23f5202` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.3.md` | 3660 | `79696223619754fff8bffacc68219beb4a1390434e44543e2bb3047a0c4e55d1` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.3.md` | 5197 | `3dc031e12cccfa2d5c1e354e34b6c5c5ab91bf042d99ea9d2e682b42b737cc19` |

## 3. Package-root construction

Use AE1H framing from the v1.3 hash contract with domain `annex-e1-specification-package-root`, version `sha256:v1.3`, and family `v1.3`. The root object contains one member named `members`, an ordered array. Sort member descriptors by unsigned UTF-8 repository-relative path bytes. Each descriptor object has fields in exact order: `path` as string, `length` as integer, and `sha256` as 32 raw binary digest bytes.

The eight-member root preimage is 1895 bytes. Its SHA-256 is:

```text
b00427ae8110039867b1c8fcd64b904067686b97cdd94f9db34586d88c0e5080
```

The manifest and package-root digest are excluded from the root, avoiding recursive self-hashing. Any governed-member byte change invalidates its member digest and the package root and requires a new manifest update before review.
