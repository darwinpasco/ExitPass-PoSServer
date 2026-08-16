# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.5 Authorization Review v1.0

## 1. Decision and authority

| Item | Result |
|---|---|
| Review task decision | `READY_FOR_REVIEW` |
| Canonical authorization outcome | `AUTHORIZED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Further specification version | `NO_FURTHER_SYNTHETIC_DATASET_SPECIFICATION_VERSION_REQUIRED` |
| Reviewed baseline | `b800a38dc2fa35c05d007803a5f385fbe78ce113` |
| v1.5 merge | `b800a38dc2fa35c05d007803a5f385fbe78ce113`, parents `2761a41b5ba7d4d5482d2973238238666425fa16` and `28982d814adfd518129a571ae59ad1af3be70c10` |
| v1.5 content commit | `28982d814adfd518129a571ae59ad1af3be70c10` |
| Pull request | `#115` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |

This independent, documentation-only review applies only the frozen v1.5 acceptance criteria. A fresh Windows PowerShell 5.1-compatible validator was created outside the repository. It parsed the normative Markdown, rebuilt AE1H bytes from published schemas and ordered values without using published preimage hexadecimal bytes as input, inspected current runtime source, and was removed after validation.

## 2. Executive summary

All frozen acceptance criteria pass. The eight exact member files and the 1,897-byte package-root preimage reproduce. The semantic registry contains exactly 1,690 unique governed instances across all 29 families, and every row independently reconstructs with zero schema, length, preimage, or digest mismatch. F22 and F23 contain 148 rows each, with 19 genesis transitions, 129 valid predecessor links, and 19 gap-free streams. All 30 statutory requests satisfy the current `FiscalDocumentCreationService` identity and finality rules.

The preserved scenario partition, UUID registry, C02 chronology, SP-A arithmetic, R01-R12, source counts, and acyclic hash graph also pass. No legitimate blocker or byte-affecting ambiguity remains. The exact v1.5 synthetic dataset and its deterministic offline validator are authorized for a later bounded implementation task. Loading, runtime changes, database changes, Controlled UAT, workbook generation or delivery, BIR submission, and Production remain unauthorized.

## 3. Provenance and independence

The review branch began at `origin/dev` commit `b800a38dc2fa35c05d007803a5f385fbe78ce113`; local `HEAD`, merge base, and `origin/dev` were equal, with ahead/behind `0/0`. Pull request #115 merged content commit `28982d814adfd518129a571ae59ad1af3be70c10`. The v1.5 merge added nine Markdown package documents and modified only this Annex E README. It introduced no runtime, API, database, migration, configuration, dependency, CI, binary, or generated-dataset change.

The reviewer did not reuse an author-side validator. Expected semantic bytes were derived from the normative AE1H grammar, per-row schema, and ordered value tuples. Runtime finality was checked against current source blob `09bc7094e433fb319216f7d98220edf180972c45`, not against a narrative restatement.

## 4. Manifest and package root

| Governed member | Exact bytes | Independently calculated SHA-256 | Result |
|---|---:|---|---|
| External-decision matrix | 1,402 | `43cffd6a32e1a5045bedfbc47ce4293d3f62298c99d56bc8afc36cfffcf13db5` | PASS |
| Canonical source-row population | 9,379,472 | `b7c15064e505332830407e4a807622084cdb9374bf1ed699f86fec339b286d63` | PASS |
| Canonical source-row and hash contract | 17,794 | `9eb091089a73d45acd662a882fc7cb71d9a58dfe25272366bfa1a5b487bc2fd3` | PASS |
| Correction-resolution matrix | 4,751 | `f07aaa8d0d7b197692325331182d874105c347889a66d28f05637505314515e0` | PASS |
| Dataset specification | 13,976 | `895c3925188b6ae1a3c888ce541d79caa801aea99c230a9bc5271be6cbd27636` | PASS |
| Expected-value matrix | 3,597 | `b1645524187360216f05d83eb10589f00a0f72000d183781a7f237dced9b756d` | PASS |
| Scenario mapping | 4,112 | `1c601d6bd5e8dc614eba62d74a8413e882f04d74c812bece69d85ffeb594b9d4` | PASS |
| Source-population and reconciliation matrix | 6,112 | `84bd9f25590bc04a4309d3b9df08e87647ddd513ea4732f44d1b895f47919d0c` | PASS |

The reviewer reconstructed descriptors from exact bytes, sorted UTF-8 repository paths, exact integer lengths, and raw 32-byte digests. The result matched the published Base64 preimage byte-for-byte. The preimage is exactly 1,897 bytes and hashes to `8b2a85312307ee2a8b1f7d8c9e413bc6220b7cdd900b3b0d8bcca0ca98600649`. The manifest and root digest are excluded. M01 passes.

## 5. Semantic registry

For every row, the reviewer decoded `Schema Base64` and `Ordered values Base64`, verified exact member names, AE1H types, order, and null posture, then rebuilt the `annex-e1-source-row` envelope from those values. Published preimage hex was used only as a comparison target.

| Family | Instances | Length mismatch | Preimage mismatch | Digest mismatch | Result |
|---|---:|---:|---:|---:|---|
| F01 Site | 19 | 0 | 0 | 0 | PASS |
| F02 Fiscal identity | 19 | 0 | 0 | 0 | PASS |
| F03 Header profile | 19 | 0 | 0 | 0 | PASS |
| F04 Fiscal period | 22 | 0 | 0 | 0 | PASS |
| F05 Fiscal range | 44 | 0 | 0 | 0 | PASS |
| F06 Fiscal document | 67 | 0 | 0 | 0 | PASS |
| F07 Fiscal-document line | 67 | 0 | 0 | 0 | PASS |
| F08 Fiscal-document total | 67 | 0 | 0 | 0 | PASS |
| F09 Tax detail | 67 | 0 | 0 | 0 | PASS |
| F10 Discount detail | 45 | 0 | 0 | 0 | PASS |
| F11 Statutory fact | 30 | 0 | 0 | 0 | PASS |
| F12 Tender | 69 | 0 | 0 | 0 | PASS |
| F13 Tender breakdown | 48 | 0 | 0 | 0 | PASS |
| F14 Discount breakdown | 90 | 0 | 0 | 0 | PASS |
| F15 Status history | 82 | 0 | 0 | 0 | PASS |
| F16 Accounting fact | 155 | 0 | 0 | 0 | PASS |
| F17 X Reading | 22 | 0 | 0 | 0 | PASS |
| F18 Z Reading | 22 | 0 | 0 | 0 | PASS |
| F19 BIR summary | 22 | 0 | 0 | 0 | PASS |
| F20 Annex row | 22 | 0 | 0 | 0 | PASS |
| F21 Fact link | 154 | 0 | 0 | 0 | PASS |
| F22 Electronic Journal record | 148 | 0 | 0 | 0 | PASS |
| F23 Source transition | 148 | 0 | 0 | 0 | PASS |
| F24 Workbook | 19 | 0 | 0 | 0 | PASS |
| F25 Workbook request | 31 | 0 | 0 | 0 | PASS |
| F26 Replay | 2 | 0 | 0 | 0 | PASS |
| F27 Conflict | 2 | 0 | 0 | 0 | PASS |
| F28 Recovery | 2 | 0 | 0 | 0 | PASS |
| F29 Audit | 186 | 0 | 0 | 0 | PASS |
| **Total** | **1,690** | **0** | **0** | **0** | **PASS** |

The registry has 1,690 unique keys and 1,690 unique governed UUIDs, with no missing, duplicate, unexpected, unnamed, untyped, or ambiguously ordered row. Provenance totals are 840 preserved v1.4 rows, 148 incorporated F22 rows, and 702 IR14-01 corrections. M03 passes.

## 6. Journal, runtime, and graph results

### 6.1 Electronic Journal and transitions

The reviewer separately reconstructed the 148 runtime Electronic Journal semantic hashes and 148 integrity hashes from the governed ledger. Results are 19 streams, 19 genesis rows, 129 valid non-genesis predecessor links, 22 Z events, 22 BIR events, and zero semantic, integrity, sequence, stream, reference, or predecessor failure. Each F23 transition resolves to the F22 record at the same scenario and ordinal. F22 and F23 remain distinct governed families.

### 6.2 Current runtime finality

Current source trims `CentralPmsParkingSessionRef` and compares it with `ParkingSessionId.Value.ToString("D")` using `StringComparison.OrdinalIgnoreCase`. It also requires matching Site and currency, nonnegative and complete statutory facts, distinct tariff snapshots, payable equality across basis, tenders, and totals, VAT equality, sufficient discount privilege, and `finalPayableAmount + statutoryDiscountAmount <= originalAmount`.

All 30 statutory cases pass every represented rule. Each parking identity is the same lowercase UUID on both sides. Statutory documents use original `11200`, VAT-exclusive basis `10000`, VAT `1200`, discount `2000`, and payable `9200`; payable, tender sum, maximum total, tax sum, discount capacity, Site, and currency reconcile. M02 passes without runtime change or translation.

### 6.3 Hash dependency graph

The independently derived graph contains 16 governed node types and 17 dependency edges. Topological traversal visited all 16 nodes. Workbook content precedes workbook-row hashing; completed child hashes precede case packages; member bytes precede the specification root; no child contains an ancestor digest; self-hash fields and the manifest root are excluded. Cycle count is zero. M04 passes.

## 7. Preserved acceptance results

| Check | Independent result |
|---|---|
| Scenario partition | 19 included, 6 excluded, overlap 0, union 25 |
| Annex coverage | 22 rows; H01-H10 and D01-D32 complete |
| UUIDv5 registry | 2,364 names, 2,364 unique UUIDs, zero collisions, valid version/variant bits, all representative vectors pass |
| C02 | 155 facts; 0 before start, at end, after end, or missing period |
| SP-A | SC 9200; PWD 9200; coupon 10200; void 11200; active gross 30000; net/tenders 28600; VATable 30000; VAT 3600; discounts 5000; D07 41200; D19 16200; D27 21400; D29 22100 |
| R01-R12 | all formulas reproduced with zero monetary and row-count difference |
| Governed counts | all 29 family counts reproduced; aggregate 1,690 |
| External decisions | all ten remain `UNRESOLVED`; none supplies a bounded offline dataset byte |

## 8. Frozen acceptance criteria

| # | Criterion | Result |
|---:|---|---|
| 1 | Eight member lengths and SHA-256 values | PASS |
| 2 | 1,897-byte package-root preimage and root hash | PASS |
| 3 | Exactly 1,690 unique governed instances across 29 families | PASS |
| 4 | All rows reconstruct without missing, duplicate, length, preimage, or digest mismatch | PASS |
| 5 | F22/F23 internal consistency, ordering, and predecessors | PASS |
| 6 | Thirty statutory references pass current runtime finality | PASS |
| 7 | Scenario, UUID, C02, SP-A, R01-R12, and governed counts | PASS |
| 8 | M04 graph has no cycle or self-reference | PASS |
| 9 | Historical and implementation files unchanged | PASS |

## 9. Historical immutability and scope

All governed v1.0 through v1.5 package and prior authorization-review blobs remain identical to `origin/dev`. No v1.5 package member changed during this review. The review adds only this document and modifies only `README.md`.

No dataset, validator, runtime, application, API, database, migration, configuration, dependency, CI, generated artifact, or executable was created or changed. No application, database, container, environment, UAT, workbook generator, HikCentral system, BIR system, Production system, or external business service was run or contacted.

## 10. Findings and errata

Legitimate blockers: None.

Non-blocking errata: None identified that changes generated dataset bytes or validator behavior. Document size and explanatory redundancy were not treated as defects.

## 11. Activity authorization matrix

| Activity | Decision | Boundary |
|---|---|---|
| Exact v1.5 machine-readable dataset implementation for 19 included scenarios | `AUTHORIZED` | later bounded implementation task only |
| Deterministic offline validator implementation | `AUTHORIZED` | later bounded implementation task only |
| Excluded-scenario implementation | `BLOCKED` | not part of v1.5 authority |
| Runtime, application, or API changes | `BLOCKED` | outside authorization |
| Database, migration, configuration, dependency, or CI changes | `BLOCKED` | outside authorization |
| Environment provisioning, database loading, or role assignment | `BLOCKED` | no execution authority |
| Controlled UAT execution | `BLOCKED` | separate authorization required |
| Workbook generation, evidence acceptance, or external delivery | `BLOCKED` | separate authorization required |
| BIR submission or Production use | `BLOCKED` | explicitly unauthorized |
| Annex E-2 through E-5 or ARTS POSLog | `BLOCKED` | outside scope |
| Signing, encryption, destructive retention, or purge | `BLOCKED` | separate authority required |

## 12. Canonical outcome and next task

Canonical outcome: `AUTHORIZED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION`.

Further specification version: `NO_FURTHER_SYNTHETIC_DATASET_SPECIFICATION_VERSION_REQUIRED`.

This authorization permits only a later task to implement the exact Annex E-1 synthetic UAT dataset described by v1.5 and its deterministic offline validator. It does not authorize executing or loading either artifact.

Exact next bounded task: **Implement Annex E-1 Synthetic UAT Dataset and Offline Validator from Authorized Specification v1.5**.
