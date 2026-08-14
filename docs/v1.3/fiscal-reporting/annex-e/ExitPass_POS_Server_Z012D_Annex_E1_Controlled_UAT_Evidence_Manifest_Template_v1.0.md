# ExitPass POS Server Z-012D Annex E-1 Controlled UAT Evidence Manifest Template v1.0

## 1. Use boundary

This is a metadata template for a future separately authorized Controlled UAT. It is not execution evidence and must not be pre-populated with fabricated results.

## 2. Run manifest

| Field | Required value |
|---|---|
| UAT run ID | Invocation-owned opaque reference |
| Authorization record | Exact data-assignment/execution authorization; never Z-012D alone |
| Scenario ID | Stable ID from the Z-012D scenario catalogue |
| Repository commit | Full 40-character commit |
| Runtime/profile version | Runtime version and `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Calculation profile/hash | Approved profile ID and SHA-256 |
| Template/renderer version | Governed template hash and renderer version |
| Environment identifier | Non-secret isolated-environment reference |
| Environment classification | `CONTROLLED_UAT_ISOLATED_SYNTHETIC` |
| Synthetic dataset identifier | Approved immutable dataset reference/hash |
| Site POS Server scope | Synthetic public/opaque scope reference |
| Fiscal identity/currency scope | Synthetic identity reference and `PHP` |
| Executor role | Role name only |
| Reviewer role | Role name only |
| Accounting reviewer role | Role name only or `NOT_REQUIRED` |
| Start/completion timestamps | UTC ISO-8601 timestamps |
| Result | `PASS`, `FAIL`, or `BLOCKED` |
| Failure classification | Governed Z-012D classification or `NONE` |
| Correlation reference | Opaque safe reference |
| Produced artifact names | Sanitized governed filenames only |
| Artifact SHA-256/length | Lowercase SHA-256 and integer byte length |
| Source identity evidence | Committed Z, BIR Summary, fact, and revision references |
| Reconciliation evidence | Rule IDs R01-R07 and minor-unit differences |
| Determinism evidence | Compared artifact IDs/hashes/byte result |
| Authorization evidence | Safe allowed/denied result references |
| Failure evidence | Safe classification and support reference; no raw diagnostics |
| Pre/post mutation manifest | Governed object counts/hashes, no payloads |
| Cleanup evidence | Resource labels, removal result, verifier role, timestamp |
| External-decision dependencies | Exact AE-DR IDs or `NONE` |
| Approval status | `PENDING_REVIEW`, `ACCEPTED`, or `REJECTED` |
| Remarks | Controlled concise text without personal or diagnostic content |

## 3. Artifact entry template

```text
artifact_logical_reference:
artifact_filename:
artifact_profile:
artifact_revision:
supersedes_reference:
semantic_hash_version:
semantic_hash:
sha256:
byte_length:
mime_type:
download_replay_sha256:
byte_identical_replay: NOT_EVALUATED
```

## 4. Reconciliation entry template

```text
rule_id:
named_operands_reference:
expected_difference_minor_units: 0
actual_difference_minor_units:
result: NOT_EVALUATED
reviewer_role:
```

## 5. Prohibited manifest content

Do not include passwords, API keys, tokens, authorization headers, connection strings, raw HTTP bodies, raw SQL, stack traces, local absolute artifact paths, customer names, plate/ticket numbers, payment credentials, statutory IDs, entitlement images, evidence documents, examiner contact data, or Production endpoints.

If prohibited content is detected, classify the scenario `UAT_PRIVACY_FAILURE`, isolate the affected draft evidence, and do not copy it into the governed manifest.

