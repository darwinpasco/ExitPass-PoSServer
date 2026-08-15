# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.3 Authorization Review v1.0

## 1. Document control

| Item | Value |
|---|---|
| Review type | Independent documentation-only authorization review |
| Reviewed package | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.3` |
| Repository baseline | `44fcfa8dccc2c2d752a17a4a562c3de6fcc00362` |
| v1.3 merge provenance | PR #111; merge `44fcfa8dccc2c2d752a17a4a562c3de6fcc00362`; content parent `cb8621f7c996f58ad4e68cdc8f2bb975038fe509` |
| Task decision | `READY_FOR_REVIEW` |
| Canonical outcome | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Another specification version required | Yes, for the four material findings in section 13 |
| Review date | 2026-08-15 |

The review changes no v1.0, v1.1, v1.2, or v1.3 specification or historical review. It authorizes no implementation or operational activity.

## 2. Executive decision

Version 1.3 is not implementation-ready. The exact eight member files are intact, all four AE1H vectors reproduce, the scenario partition, identities, C02 facts, Electronic Journal chains, expected values, counts, and R01-R12 arithmetic pass. Four material defects remain:

1. the normative 1,895-byte manifest root reconstructs to `9e357f64571fd9d8dae3ea78b55ae31c71bfaba2512287921adaf69a8bd22842`, not the published `b00427ae8110039867b1c8fcd64b904067686b97cdd94f9db34586d88c0e5080`;
2. SP-A13 still fails current fiscal-document finality because command `CentralPmsParkingSessionRef` is `SYN-AE1-PARKING-NNN-OOOO`, while the statutory snapshot `ParkingSessionId` is the different UUID `REF(parking-session,o)`;
3. eleven row families still omit exact AE1H member names, types, or field order through shorthand such as `taxpayer/address/TIN universal`, `Site/fiscal IDs`, `all snapshot amounts/counts`, and `ordered BIR+facts source object`;
4. the case package includes the workbook source row, while that workbook row requires `artifact_sha256=case-package digest`, creating an undefined self-referential digest.

These defects can change bytes, cause runtime rejection, or prevent deterministic construction. They are not editorial errata. Dataset and validator implementation therefore remain blocked.

## 3. Baseline, provenance, and immutability

Local `dev`, `origin/dev`, the review worktree HEAD, and merge base were all `44fcfa8dccc2c2d752a17a4a562c3de6fcc00362`; divergence was zero ahead and zero behind. PR #111 added the nine v1.3 documents and modified only the Annex E README. Historical v1.0, v1.1, and v1.2 package documents compared byte-for-byte with their merged content commits and had zero differences.

The approved Accounting Calculation Profile remains SHA-256 `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f`.

## 4. Reviewed inventory and manifest members

All nine required v1.3 documents were reviewed in full. The manifest excludes itself and governs these eight exact files:

| Member | Bytes | Recorded and calculated SHA-256 | Result |
|---|---:|---|---|
| External decision matrix | 1402 | `e100067d33b83607ebc932379658d2c6dc3221339c4bd29500d8369fcf658d64` | match |
| Canonical source-row/hash contract | 12893 | `49b73ed77aeb455d8829abe874a4ecc868f89b99aede615271510394d2064ac2` | match |
| Canonical source-row population | 18038 | `2ce583fcb1762e8ad6f844c31e909a7ba9d723c640073f5cafee956f3343cb10` | match |
| Correction-resolution matrix | 3274 | `29b6eb985c0376b9fc7ae2eaff8d9a3c5bfcb742f5a2b29ef2813ca7d186c9fa` | match |
| Dataset specification | 10359 | `6d9faaceed0cf3e4c109d3f14fe628b2f48663cbf6bab03fa8ccebf48da1b594` | match |
| Expected-value matrix | 3597 | `8fc6fdb9ab627a9c02a0cd103c5f4474de99ce124eeb4dde5da5f3a5c23f5202` | match |
| Scenario mapping | 3660 | `79696223619754fff8bffacc68219beb4a1390434e44543e2bb3047a0c4e55d1` | match |
| Source-population matrix | 5197 | `3dc031e12cccfa2d5c1e354e34b6c5c5ab91bf042d99ea9d2e682b42b737cc19` | match |

## 5. AE1H independent reconstruction

A new Windows PowerShell 5.1-compatible encoder was written outside the repository from the normative prose. It independently implemented strict UTF-8 NFC, big-endian U32/U64, all scalar tags, ordered objects and arrays, unsigned frame-byte set ordering, duplicate rejection, exact domain/version/family framing, binary digests, and self exclusions. It did not copy published preimage bytes.

| Vector | Reconstructed bytes | Reconstructed SHA-256 | Result |
|---|---:|---|---|
| V13-01 | 391 | `70cc646de58b4ddd5cfef4f0717605f11d24f29271a0fcfbca1ce6da42a09654` | match |
| V13-02 | 249 | `ec60d71d6d207376fb2070aa26b50112e2a2d5ac0279701f7cd628b7cb36ef20` | match |
| V13-03 | 222 | `cf46b20c5ea7d37646a5695fdcb90a6b0e3e0c4efb86299c53fc11ddc2dda4f8` | match |
| V13-04 | 497 | `e9836e6a35ac50a523dbaae12a0a2f18b2755dff5c536e0a0f22af3fb19a36ff` | match |

The vectors verify domain separation, versions, byte order, length framing, null/missing/empty distinctions, Unicode NFC, timestamps, UUIDs, integers/minor units, fixed decimals, binary values, objects, arrays, sets, duplicate policy, and source/integrity envelopes.

The same encoder built the manifest root exactly as specified: sorted UTF-8 paths; descriptor fields `path`, `length`, `sha256`; raw 32-byte digests; ordered `members` array; manifest and root digest excluded. The preimage length matched 1,895 bytes, but its SHA-256 was `9e357f64571fd9d8dae3ea78b55ae31c71bfaba2512287921adaf69a8bd22842`. This does not match the manifest's `b00427ae8110039867b1c8fcd64b904067686b97cdd94f9db34586d88c0e5080` and blocks package integrity.

## 6. Source-row and case-package hashing

The 29 families expand to 1,690 row-family instances. All eight file hashes and four vectors are reproducible, but every governed source-row semantic hash cannot be reconstructed because the population does not supply a byte-complete fields object for eleven families. Examples are:

- family 2 uses `taxpayer/address/TIN universal`, `MIN`, and actor shorthand instead of exact member names, types, and order;
- families 3 and 4 use grouped `Site/fiscal IDs` and actor-field shorthand;
- families 17-19 use `all snapshot amounts/counts` rather than the exact ordered field inventory required by AE1H;
- family 20 specifies `HASH(ordered BIR+facts source object)` without a domain, family, ordered schema, or exact member names;
- family 29's package-global `first 155`/`next 19` action allocation does not define each per-case audit ordinal's exact action and source binding.

There is also a digest cycle. The case-package family includes ordered source rows. Family 24 is a source row whose `artifact_sha256` is the case-package digest. Therefore:

```text
case digest -> workbook source-row hash -> workbook artifact_sha256 -> case digest
```

No placeholder, two-pass rule, fixed-point rule, or exclusion breaks the cycle. Families 24-28 depend on that unavailable case digest or artifact identity. Source-row/case-package hash completion is therefore blocked before machine-readable construction.

## 7. SP-A13 runtime validation

The corrected monetary invariant passes:

```text
SC/PWD: 10000 - 2000 + 1200 = 9200
final + discount: 9200 + 2000 = 11200 original
coupon: 10000 - 1000 + 1200 = 10200
void: 10000 + 1200 = 11200
```

Tender, total, VAT, and discount checks also pass. Current `FiscalDocumentCreationService.NormalizeAppliedStatutoryFiscalFacts`, however, additionally requires `CentralPmsParkingSessionRef` to equal `ParkingSessionId.ToString("D")`.

For case 001 document 0001, the v1.3 population fixes:

| Runtime input | Exact value |
|---|---|
| command parking reference | `SYN-AE1-PARKING-001-0001` |
| statutory parking UUID name | `annex-e1-synthetic-reference:v1\|AE1-UAT-001\|parking-session\|0001` |
| statutory parking UUID | `eb706d21-9161-5083-a4a2-6f703b62845a` |

The comparison is false, so the current runtime returns `AppliedStatutoryFactsNotFinal` before persistence. This affects all 30 statutory facts and blocks SP-A13 source producibility despite correct monetary arithmetic.

## 8. Twenty-nine-family closure summary

| Result | Families | Count |
|---|---|---:|
| `VERIFIED_CLOSED` | 1, 7-10, 12-15, 22-23 | 11 |
| `BLOCKED_FIELD_BINDING` | 2-5, 16-21, 29 | 11 |
| `BLOCKED_RUNTIME_CONFLICT` | 6, 11 | 2 |
| `BLOCKED_CASE_PACKAGE_DEPENDENCY` | 24-28 | 5 |

The eleven closed families remain globally unusable for an authorized package while the package root and case-package dependencies are invalid. The other eighteen families require material correction, not editorial cleanup.

## 9. Scenario, identity, chronology, journal, and arithmetic results

| Check | Independent result |
|---|---|
| Scenario partition | 19 included, 6 excluded, overlap 0, union 25; one case per included scenario |
| Expected Annex rows | exactly 22 |
| H and D coverage | H01-H10 complete; D01-D32 complete |
| UUIDv5 registry | 2,364 names and UUIDs; 2,364 unique; zero collisions; v5/RFC variant and three vectors match |
| C02 | 155 facts checked; 0 before start, at end, or after end |
| Electronic Journal | 148 records/transitions, 19 streams, 22 Z and 22 BIR events; 0 identity, byte-count, semantic, integrity, predecessor, reference, version, or sequence failures |
| R01-R12 | every monetary difference and row-count difference is 0 |
| No-activity leakage | cases 004 and 005 contain active documents and do not reproduce excluded scenario 002 |

SP-A13 active gross is 30000; net/tenders 28600; VATable 30000; VAT 3600; SC/PWD/coupon discounts 2000/2000/1000; void 11200; D07 41200; D19 16200; D27 21400; D29 22100. B13 and D13 arithmetic also passes exactly.

## 10. Governed counts

Counts independently reproduce: 19 Sites, fiscal identities, header profiles, and workbooks; 22 periods, X Readings, Z Readings, BIR summaries, and Annex rows; 67 documents, lines, totals, and tax details; 69 tenders; 45 discount details; 30 statutory facts; 82 status histories; 155 Accounting facts; 148 EJ records and transitions; 44 fiscal ranges; 48 tender breakdowns; 90 discount breakdowns; 154 fact links; 346 requests; 186 audits; and two each replay, conflict, and recovery. Every v1.2-to-v1.3 count delta is zero.

## 11. External decisions, privacy, and scope

All ten decisions remain exactly `UNRESOLVED`: AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-012, AE-DR-016, AE-DR-016B, AE-DR-019, AE-DR-020A, and AE-DR-024. Each records `None` for bounded offline byte selection. They do not cause the four material findings and none is resolved, waived, or assumed.

Values and identities are synthetic. No real customer, vehicle, taxpayer, operator, employee, payment credential, statutory evidence image, secret, endpoint, database, or external service is required. The reserved synthetic TIN `000-000-000-000` is not real personal data.

## 12. Activity authorization table

| Activity | Decision | Boundary |
|---|---|---|
| v1.3 specification acceptance | `BLOCKED` | four material findings |
| machine-readable dataset implementation | `BLOCKED` | root, source-row, runtime, and cycle defects |
| offline validator implementation | `BLOCKED` | no valid complete expected package |
| any excluded scenario | `BLOCKED` | remains `EXCLUDED_NON_EXECUTABLE` |
| documentation-only correction | `AUTHORIZED` | new version addressing only the four findings |
| runtime clock, identity, application, or API changes | `BLOCKED` | separate authority required |
| database, migration, configuration, dependency, or CI changes | `BLOCKED` | outside bounded authority |
| environment provisioning or role assignment | `BLOCKED` | not authorized |
| data loading or Controlled UAT execution | `BLOCKED` | prerequisites and execution authority absent |
| evidence acceptance or internal workbook generation | `BLOCKED` | no authorized execution or workbook authority |
| external delivery or BIR submission | `BLOCKED` | external authority unresolved |
| Production rollout | `BLOCKED` | explicitly unauthorized |
| Annex E-2 through E-5 or ARTS POSLog | `BLOCKED` | outside scope |
| signing, encryption, destructive retention, or purge | `BLOCKED` | AE-DR-016/016B unresolved |

## 13. Material findings and correction required

| ID | Material finding | Exact correction required |
|---|---|---|
| M01 | Manifest root mismatch: expected `b00427ae...`, reconstructed `9e357f64...` for the same 1,895-byte normative root | publish the correct root or correct the framing/member inventory and add the complete root preimage as a vector |
| M02 | SP-A parking scope mismatch causes `AppliedStatutoryFactsNotFinal` | make the command parking reference and statutory parking UUID text identical using a current-runtime-valid deterministic binding |
| M03 | Eleven families lack byte-complete ordered AE1H member schemas | enumerate every member name, type, null/omit choice, order, and deterministic value for every instance |
| M04 | Workbook artifact digest recursively depends on the case package containing that workbook row | define a non-recursive artifact boundary or explicit exclusion/two-stage contract with test vectors |

Non-blocking errata: None.

## 14. Validation and outcome

Validation used Git provenance and blob comparisons; exact-byte SHA-256; the independent temporary AE1H encoder; runtime source inspection; UUIDv5 reconstruction; integer arithmetic; scenario, field, C02, EJ, count, external-decision, Markdown, privacy, secret, token, path, and generated-artifact scans. No application, build, test, database, container, environment, UAT, HikCentral, BIR, workbook, delivery, or external-service command was run.

Task decision: `READY_FOR_REVIEW`.

Canonical authorization outcome: `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION`.

Another specification version is required. `NO_FURTHER_SYNTHETIC_DATASET_SPECIFICATION_VERSION_REQUIRED` does not apply.

The next bounded task is to prepare a documentation-only new-version correction for M01-M04 and submit it to another independent authorization review. Do not implement datasets, validators, runtime changes, databases, environments, loading, or Controlled UAT automatically.
