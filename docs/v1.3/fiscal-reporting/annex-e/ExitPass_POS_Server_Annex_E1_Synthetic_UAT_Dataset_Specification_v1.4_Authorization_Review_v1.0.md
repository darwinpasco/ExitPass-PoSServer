# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.4 Authorization Review v1.0

## 1. Decision and authority

| Item | Result |
|---|---|
| Review task decision | `READY_FOR_REVIEW` |
| Canonical outcome | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Further specification version | `FURTHER_SYNTHETIC_DATASET_SPECIFICATION_VERSION_REQUIRED` |
| Reviewed baseline | `86350408d678e3f6c71784e2cdf18347d4fe7e73` |
| v1.4 merge | `86350408d678e3f6c71784e2cdf18347d4fe7e73`, parents `886a765076a368c276424e51f90eb1d7dfab9109` and `ebc85e9d10ffb6bffb6493f7ca652432acc730a1` |
| v1.4 content commit | `ebc85e9d10ffb6bffb6493f7ca652432acc730a1` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |

This is an independent documentation-only review. It authorizes neither dataset nor validator implementation. The reviewer created fresh Windows PowerShell 5.1-compatible reconstruction scripts outside the repository, derived expected values from normative fields and current source rules, and removed the scripts after validation. No package-author validator was used as review evidence.

## 2. Executive summary

M01, M02, and M04 reproduce successfully. All eight exact package members and the 1,897-byte package-root preimage match; all 30 statutory parking-session comparisons and current finality arithmetic pass; and the independently reconstructed hash-dependency graph is acyclic.

M03 does not pass. Appendix B independently reconstructs all 840 corrected semantic rows with zero length, byte, or digest mismatch. The incorporated v1.2 Electronic Journal ledger independently revalidates 148 inherited F22 rows. The other 702 inherited instances in F01, F07-F10, F12-F15, and F23 do not publish an exact ordered source/runtime value tuple, complete semantic preimage, byte length, and expected semantic digest. Appendix A cannot substitute for that evidence: it expressly labels its 1,690 digests informational and not source/runtime semantic closure.

Appendix A also leaves its AE1H family token unstated. Applying the published domain, version, ordered members, and general AE1H framing reproduces none of its 1,690 lengths/digests. This does not invalidate the separately reconstructed Appendix B rows, but it is an additional unreproducible published digest registry.

The bounded implementation remains blocked. A v1.5 documentation-only correction must publish authorization-critical semantic evidence for the remaining 702 inherited instances and either fully specify or remove the informational binding digests.

## 3. Baseline and provenance

The review branch was created from `origin/dev` at `86350408d678e3f6c71784e2cdf18347d4fe7e73`. `HEAD`, merge base, and `origin/dev` were identical at review start; ahead/behind was `0/0`. Pull request #113 merged the v1.4 content commit without runtime, API, database, migration, configuration, dependency, CI, binary, or generated-dataset changes.

Exact v1.4 blobs:

| Member | Git blob |
|---|---|
| External-decision matrix | `8c9eb96bbc79c018ec9b8cbb9371ef1aa43169b0` |
| Canonical population | `93dabe8add33222e288621fbbd03d79abfbad29b` |
| Hash contract | `c46a68bf8ef6928f3da64ef5c59d3a68e8ebc6b0` |
| Correction matrix | `81ecc97aa8575b9a22bb075cbfcd8de7c797a1af` |
| Manifest | `522ae42e488b49cf2958ff02043ac36fe9a59f6b` |
| Specification | `340cc2edd75291367a3ad7ee83efcba029bfacfe` |
| Expected values | `602289f658c325769eb674aaaa91bb90c55c8f76` |
| Scenario mapping | `3ed0210f53901b4a53f6ec29a576f1ccd5a56f5e` |
| Source/reconciliation matrix | `d5b9a66258d9daf32cc571f822fd742f7841a55f` |

## 4. Manifest and M01

| Governed member | Bytes | Independently calculated SHA-256 | Result |
|---|---:|---|---|
| External-decision matrix | 1,402 | `336379bbfb89e20fa2d38f1305993cf122d62545215edce0211150f0a24a543f` | PASS |
| Canonical population | 4,863,991 | `f505eba76cc38c6d493753f692a546c64990d9006425ceb8c4661bb7c0efcc5b` | PASS |
| Hash contract | 17,904 | `2b7f2470f145ba3f6aa27840f0b892c6cf3b860c64b8828883067b8a584415ba` | PASS |
| Correction matrix | 5,527 | `95930a4a3a4c7ab1632c79478706d0661eded45d3b7955094e721b039c202d9a` | PASS |
| Specification | 13,615 | `c9f1600a26c6bc6b621236367fcfb247281f76826c488dfb76f43c5556fae6b3` | PASS |
| Expected values | 3,597 | `f7fee59f1ecf19ebe8d2cbafc60bfb65dff94ba886f1b05d114f9c5c276f6c58` | PASS |
| Scenario mapping | 4,112 | `0fbdc1de0a1e41c91064f7a68b91e87b59496bd08ec0076a3fbf29c72e347577` | PASS |
| Source/reconciliation matrix | 6,112 | `db105379015af770fcfb61241b86f582be405e9f40c8532619f8ee4247591b35` | PASS |

The reviewer rebuilt the AE1H descriptor array from exact merged bytes, UTF-8 paths, big-endian lengths, and raw digest bytes. It matched the published Base64 preimage byte-for-byte. Length was 1,897 bytes and SHA-256 was `90c192e1c5b9e8e7290a8c1de5fa17c63d532539c8e31cc0ce0b7f4e50e2324f`. The manifest is excluded. M01 is `VERIFIED_RESOLVED`.

## 5. Runtime identity and M02

Current `FiscalDocumentCreationService` normalizes the command reference and compares it to `ParkingSessionId.Value.ToString("D")` using `StringComparison.OrdinalIgnoreCase`. It also requires complete nonempty statutory identities, supported controlled codes, matching currency and Site, distinct tariff snapshots, nonnegative amounts, payable equality across basis/tender/total, VAT equality, sufficient discount privilege, and `finalPayableAmount + statutoryDiscountAmount <= originalAmount`.

The review decoded the 67 F06 requests and 30 F11 statutory facts, joined each statutory fact to its document, and checked every current prerequisite represented by the package. Results: 30 comparisons, 30 lowercase UUID matches, zero parking mismatch, and zero finality mismatch. Each governed statutory row used original `11200`, VAT-exclusive basis `10000`, VAT `1200`, statutory discount `2000`, and payable `9200`. Coupon payable remained `10200`; void amount remained `11200`. No runtime change or translation layer is needed. M02 is `VERIFIED_RESOLVED`.

## 6. AE1H reconstruction and M03

### 6.1 Corrected rows

The reviewer decoded each Appendix B value tuple, reconstructed AE1H from member names, types, values, ordering, null posture, nested collections, and framing, then compared the result to the published complete preimage, byte length, and digest. Published preimage hex was not used as reconstruction input.

| Family | Instances | Length failures | Byte failures | Digest failures | Result |
|---|---:|---:|---:|---:|---|
| F02 Fiscal identity | 19 | 0 | 0 | 0 | PASS |
| F03 Header profile | 19 | 0 | 0 | 0 | PASS |
| F04 Fiscal period | 22 | 0 | 0 | 0 | PASS |
| F05 Fiscal range | 44 | 0 | 0 | 0 | PASS |
| F06 Fiscal document | 67 | 0 | 0 | 0 | PASS |
| F11 Statutory fact | 30 | 0 | 0 | 0 | PASS |
| F16 Accounting fact | 155 | 0 | 0 | 0 | PASS |
| F17 X Reading | 22 | 0 | 0 | 0 | PASS |
| F18 Z Reading | 22 | 0 | 0 | 0 | PASS |
| F19 BIR summary | 22 | 0 | 0 | 0 | PASS |
| F20 Annex row | 22 | 0 | 0 | 0 | PASS |
| F21 Fact link | 154 | 0 | 0 | 0 | PASS |
| F24 Workbook | 19 | 0 | 0 | 0 | PASS |
| F25 Workbook request | 31 | 0 | 0 | 0 | PASS |
| F26 Replay | 2 | 0 | 0 | 0 | PASS |
| F27 Conflict | 2 | 0 | 0 | 0 | PASS |
| F28 Recovery | 2 | 0 | 0 | 0 | PASS |
| F29 Audit | 186 | 0 | 0 | 0 | PASS |
| **Corrected total** | **840** | **0** | **0** | **0** | **PASS** |

The allocation independently equals 687 M03 rows, 97 M02 rows, and 56 M04 rows.

### 6.2 Inherited rows

| Family | Expected | Fully revalidated | Blocked | Evidence result |
|---|---:|---:|---:|---|
| F01 Site | 19 | 0 | 19 | no semantic tuple/preimage/digest |
| F07 Document line | 67 | 0 | 67 | no semantic tuple/preimage/digest |
| F08 Document total | 67 | 0 | 67 | no semantic tuple/preimage/digest |
| F09 Tax detail | 67 | 0 | 67 | no semantic tuple/preimage/digest |
| F10 Discount detail | 45 | 0 | 45 | no semantic tuple/preimage/digest |
| F12 Tender | 69 | 0 | 69 | no semantic tuple/preimage/digest |
| F13 Tender breakdown | 48 | 0 | 48 | no semantic tuple/preimage/digest |
| F14 Discount breakdown | 90 | 0 | 90 | no semantic tuple/preimage/digest |
| F15 Status history | 82 | 0 | 82 | no semantic tuple/preimage/digest |
| F22 Electronic Journal record | 148 | 148 | 0 | incorporated v1.2 ledger reproduced |
| F23 Source transition | 148 | 0 | 148 | identity/reference/version present; no transition semantic tuple/preimage/digest |
| **Inherited total** | **850** | **148** | **702** | **FAIL** |

Aggregate result: 29 families; 1,690 unique binding rows; 840 corrected semantic reconstructions; 148 inherited semantic reconstructions; 702 blocked instances; zero corrected-row length, byte, or digest mismatch. M03 is `NOT_RESOLVED` because aggregate semantic closure is 988 of 1,690, not 1,690 of 1,690.

### 6.3 Informational binding ledger

Appendix A says its digest is informational and cannot establish semantic closure. It names the domain/version and seven object members but does not define the AE1H envelope family token. The reviewer applied the published AE1H grammar and literal registry key as the family input; all 1,690 published lengths/digests mismatched. Alternative unstated family tokens are discretionary and were not guessed. This is an unreproducible informational registry and must be corrected or removed, but it does not alter the independently passing Appendix B rows.

## 7. Hash graph and M04

The independent graph contains nine digest-node types and 20 directed dependency types when implicit EJ-semantic and member-file digest nodes are made explicit. It orders fixed values, source semantics, EJ semantics, predecessor chains, workbook content, workbook row, evidence rows, case package, exact member bytes, and package root. No child includes a case-package or package-root ancestor; self-hash fields are excluded; the manifest is excluded; and all predecessor edges point backward. Cycle detection visited 17 digest/value node types and found zero back edges. M04 is `VERIFIED_RESOLVED`.

## 8. Preserved baseline results

| Check | Independent result |
|---|---|
| Scenario partition | 19 included, 6 excluded, overlap 0, union 25 |
| Annex coverage | 22 rows; H01-H10 and D01-D32 complete |
| UUIDv5 | 2,364 names and values; all unique; zero collision/version/variant/vector failures |
| AE1H non-empty vectors | 4 reconstructed; zero byte/length/digest mismatch |
| C02 | 22 periods, 155 facts; 0 before start, at end, or after end |
| Electronic Journal | 148 semantic and 148 integrity hashes; 129 predecessors; 19 gap-free streams; 22 Z and 22 BIR events; zero failures |
| SP-A | gross 30000; net/tenders 28600; VATable 30000; VAT 3600; discounts 5000; void 11200; D07 41200; D19 16200; D27 21400; D29 22100 |
| R01-R12 | zero monetary and row-count differences |
| Source counts | all 29 family counts reproduced; zero v1.3-to-v1.4 cardinality delta |
| External decisions | all 10 remain `UNRESOLVED`; none selects bounded offline bytes |

## 9. Historical immutability and scope

The 26 members governed by the v1.0-v1.3 manifests match their recorded exact-byte SHA-256 values. Each v1.0-v1.3 authorization review matches its last content-commit blob. No v1.4 package member changed in this review. The review adds only this file and modifies only `README.md`.

No application build, test, database, container, environment, UAT, workbook generator, HikCentral system, BIR system, or business service was run or contacted. No dataset, validator, executable, generated artifact, runtime, API, database, migration, configuration, dependency, or CI change was made.

## 10. Material findings and errata

| ID | Classification | Finding | Required correction |
|---|---|---|---|
| IR14-01 | MATERIAL | 702 inherited instances have no authorization-critical exact value tuple, semantic preimage, length, and expected semantic digest | publish complete evidence for F01, F07-F10, F12-F15, and F23 |
| IR14-02 | MATERIAL | Appendix A's AE1H family token is unspecified and none of 1,690 published binding digests reproduces from the stated grammar | define the exact envelope and correct all rows, or remove the informational digests |
| E14-01 | NON-BLOCKING | The package labels aggregate closure as 1,690 despite Appendix B containing only 840 corrected rows | replace the aggregate claim after IR14-01 is corrected |

## 11. Activity authorization matrix

| Activity | Decision | Reason |
|---|---|---|
| v1.4 specification acceptance | `BLOCKED` | IR14-01 and IR14-02 |
| Included-scenario machine-readable dataset implementation | `BLOCKED` | semantic population is incomplete |
| Offline validator implementation | `BLOCKED` | complete expected semantic registry is unavailable |
| v1.5 documentation-only correction | `AUTHORIZED` | may address only the confirmed findings |
| Excluded-scenario implementation | `BLOCKED` | remains non-executable |
| Runtime, application, or API changes | `BLOCKED` | outside this authority |
| Database, migration, configuration, dependency, or CI changes | `BLOCKED` | outside this authority |
| Provisioning, role assignment, or loading | `BLOCKED` | no implementation/execution authority |
| Controlled UAT or scenario execution | `BLOCKED` | no execution authority |
| Workbook generation or evidence acceptance | `BLOCKED` | no implementation/execution authority |
| External delivery or BIR submission | `BLOCKED` | external authority unresolved |
| Production | `BLOCKED` | explicitly unauthorized |
| Annex E-2 through E-5 or ARTS POSLog | `BLOCKED` | outside scope |
| Signing, encryption, retention, or purge | `BLOCKED` | separate decisions required |

## 12. Residual risk and next task

The principal residual risk is semantic divergence: an implementer must still invent bytes for 702 source/runtime instances, while the informational registry provides no reproducible independent check. Passing package-root cryptography proves the package bytes are stable, not that those missing values are defined.

The exact next bounded task is a documentation-only v1.5 correction. It must preserve verified M01, M02, M04, Appendix B's 840 rows, identities, counts, C02, EJ chains, and arithmetic; publish exact value tuples/preimages/lengths/digests for the 702 blocked inherited instances; and correct or remove Appendix A binding digests. A separate independent v1.5 authorization review remains mandatory.
