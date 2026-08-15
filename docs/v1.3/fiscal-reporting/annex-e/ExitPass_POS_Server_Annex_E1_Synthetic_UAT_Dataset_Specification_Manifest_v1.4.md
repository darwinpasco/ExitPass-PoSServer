# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification Manifest v1.4

## 1. Control

| Item | Value |
|---|---|
| Package | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.4` |
| Repository baseline | `886a765076a368c276424e51f90eb1d7dfab9109` |
| Package readiness | `READY_FOR_V1_4_SPECIFICATION_AUTHORIZATION_REVIEW` |
| Canonical implementation gate | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Hash algorithm | SHA-256 |
| File bytes | Exact repository bytes; no normalization |
| Manifest participation | Excluded from member set and package root |

## 2. Governed exact members

Descriptors are listed in unsigned UTF-8 path order. Length is the exact byte count. Digests are lowercase hexadecimal renderings of the member's raw 32-byte SHA-256.

| Relative path | Bytes | SHA-256 |
|---|---:|---|
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.4.md` | 1402 | `336379bbfb89e20fa2d38f1305993cf122d62545215edce0211150f0a24a543f` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_Population_v1.4.md` | 4863991 | `f505eba76cc38c6d493753f692a546c64990d9006425ceb8c4661bb7c0efcc5b` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.4.md` | 17904 | `2b7f2470f145ba3f6aa27840f0b892c6cf3b860c64b8828883067b8a584415ba` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Correction_Resolution_Matrix_v1.4.md` | 5527 | `95930a4a3a4c7ab1632c79478706d0661eded45d3b7955094e721b039c202d9a` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.4.md` | 13615 | `c9f1600a26c6bc6b621236367fcfb247281f76826c488dfb76f43c5556fae6b3` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.4.md` | 3597 | `f7fee59f1ecf19ebe8d2cbafc60bfb65dff94ba886f1b05d114f9c5c276f6c58` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.4.md` | 4112 | `0fbdc1de0a1e41c91064f7a68b91e87b59496bd08ec0076a3fbf29c72e347577` |
| `docs/v1.3/fiscal-reporting/annex-e/ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.4.md` | 6112 | `db105379015af770fcfb61241b86f582be405e9f40c8532619f8ee4247591b35` |

## 3. Package-root grammar and result

Apply AE1H section 3 from the v1.4 hash contract with domain `annex-e1-specification-package-root`, version `sha256:v1.4`, family `v1.4`, and root object `{members:ordered-array}`. Every descriptor object has exact member order `path:string`, `length:integer`, `sha256:binary32`. Paths are strict UTF-8 NFC with `/`; lengths are canonical nonnegative base-10 ASCII under AE1H integer tag `03`; digests are raw 32 bytes under tag `08`. The manifest and root digest are excluded. There is no BOM, normalization, or trailing byte outside the AE1H preimage.

Exact package-root preimage length: `1897` bytes.

Exact package-root preimage Base64:

```text
QUUxSAEAAAAjYW5uZXgtZTEtc3BlY2lmaWNhdGlvbi1wYWNrYWdlLXJvb3QAAAALc2hhMjU2OnYxLjQAAAAEdjEuNAkAAAAAAAAHHQAAAAEAAAAHbWVtYmVycwoAAAAAAAAHBQAAAAgJAAAAAAAAANoAAAADAAAABHBhdGgBAAAAAAAAAHtkb2NzL3YxLjMvZmlzY2FsLXJlcG9ydGluZy9hbm5leC1lL0V4aXRQYXNzX1BPU19TZXJ2ZXJfQW5uZXhfRTFfRXh0ZXJuYWxfRGVjaXNpb25fdG9fU2NlbmFyaW9fUmVjb25jaWxpYXRpb25fTWF0cml4X3YxLjQubWQAAAAGbGVuZ3RoAwAAAAAAAAAEMTQwMgAAAAZzaGEyNTYIAAAAAAAAACAzY3m7+4niD6LTjxMFmTzxItYlRSFe3OAhEVDwokpUPwkAAAAAAAAA1wAAAAMAAAAEcGF0aAEAAAAAAAAAdWRvY3MvdjEuMy9maXNjYWwtcmVwb3J0aW5nL2FubmV4LWUvRXhpdFBhc3NfUE9TX1NlcnZlcl9Bbm5leF9FMV9TeW50aGV0aWNfVUFUX0Nhbm9uaWNhbF9Tb3VyY2VfUm93X1BvcHVsYXRpb25fdjEuNC5tZAAAAAZsZW5ndGgDAAAAAAAAAAc0ODYzOTkxAAAABnNoYTI1NggAAAAAAAAAIPUF66dsw4xtSTdT9pKlRsZJkNkAZCXOuMRmG7fA78xbCQAAAAAAAADcAAAAAwAAAARwYXRoAQAAAAAAAAB8ZG9jcy92MS4zL2Zpc2NhbC1yZXBvcnRpbmcvYW5uZXgtZS9FeGl0UGFzc19QT1NfU2VydmVyX0FubmV4X0UxX1N5bnRoZXRpY19VQVRfQ2Fub25pY2FsX1NvdXJjZV9Sb3dfYW5kX0hhc2hfQ29udHJhY3RfdjEuNC5tZAAAAAZsZW5ndGgDAAAAAAAAAAUxNzkwNAAAAAZzaGEyNTYIAAAAAAAAACArfyRw8UW6P2qieEDwuJLGzzuGDGS4goiDBnuKWEQVugkAAAAAAAAA5wAAAAMAAAAEcGF0aAEAAAAAAAAAiGRvY3MvdjEuMy9maXNjYWwtcmVwb3J0aW5nL2FubmV4LWUvRXhpdFBhc3NfUE9TX1NlcnZlcl9Bbm5leF9FMV9TeW50aGV0aWNfVUFUX0RhdGFzZXRfU3BlY2lmaWNhdGlvbl9Db3JyZWN0aW9uX1Jlc29sdXRpb25fTWF0cml4X3YxLjQubWQAAAAGbGVuZ3RoAwAAAAAAAAAENTUyNwAAAAZzaGEyNTYIAAAAAAAAACCVkwpKOkx6sWMseUeHBtBmHt7UXTt5VQlOchsDnCAtmgkAAAAAAAAAywAAAAMAAAAEcGF0aAEAAAAAAAAAa2RvY3MvdjEuMy9maXNjYWwtcmVwb3J0aW5nL2FubmV4LWUvRXhpdFBhc3NfUE9TX1NlcnZlcl9Bbm5leF9FMV9TeW50aGV0aWNfVUFUX0RhdGFzZXRfU3BlY2lmaWNhdGlvbl92MS40Lm1kAAAABmxlbmd0aAMAAAAAAAAABTEzNjE1AAAABnNoYTI1NggAAAAAAAAAIMnxYAomxrxrYhI2Nn/PskcoH3aCbEiN+3b0PFVW+uazCQAAAAAAAADKAAAAAwAAAARwYXRoAQAAAAAAAABrZG9jcy92MS4zL2Zpc2NhbC1yZXBvcnRpbmcvYW5uZXgtZS9FeGl0UGFzc19QT1NfU2VydmVyX0FubmV4X0UxX1N5bnRoZXRpY19VQVRfRXhwZWN0ZWRfVmFsdWVfTWF0cml4X3YxLjQubWQAAAAGbGVuZ3RoAwAAAAAAAAAEMzU5NwAAAAZzaGEyNTYIAAAAAAAAACD3/uWfHs8Z6+jSy6/GC/tl3/lLqIbxsF0RT5xcJ29sWAkAAAAAAAAA0AAAAAMAAAAEcGF0aAEAAAAAAAAAcWRvY3MvdjEuMy9maXNjYWwtcmVwb3J0aW5nL2FubmV4LWUvRXhpdFBhc3NfUE9TX1NlcnZlcl9Bbm5leF9FMV9TeW50aGV0aWNfVUFUX1NjZW5hcmlvX3RvX0RhdGFzZXRfTWFwcGluZ192MS40Lm1kAAAABmxlbmd0aAMAAAAAAAAABDQxMTIAAAAGc2hhMjU2CAAAAAAAAAAgD73B3goeQckQZPemi5Hoe1lJa9COwAdqP78pxy40dXcJAAAAAAAAAOAAAAADAAAABHBhdGgBAAAAAAAAAIFkb2NzL3YxLjMvZmlzY2FsLXJlcG9ydGluZy9hbm5leC1lL0V4aXRQYXNzX1BPU19TZXJ2ZXJfQW5uZXhfRTFfU3ludGhldGljX1VBVF9Tb3VyY2VfUG9wdWxhdGlvbl9hbmRfUmVjb25jaWxpYXRpb25fTWF0cml4X3YxLjQubWQAAAAGbGVuZ3RoAwAAAAAAAAAENjExMgAAAAZzaGEyNTYIAAAAAAAAACDbEFN5AVr3cPz7YSQbhvWCvkBen0DIUyYZ+O5CR1kbNQ==
```

Package-root SHA-256: `90c192e1c5b9e8e7290a8c1de5fa17c63d532539c8e31cc0ce0b7f4e50e2324f`.

## 4. Result

All eight member byte lengths and SHA-256 values match the final files. Decoding the Base64 yields the recorded preimage and applying SHA-256 yields the published package root. M01-M04 are author-side resolved. The package is ready for an independent v1.4 authorization review; this status is not implementation authority. Any change to a governed member invalidates this manifest and requires complete recalculation. Dataset and validator implementation remain unauthorized.
