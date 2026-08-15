# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.1 Authorization Review v1.0

## 1. Document control

| Item | Value |
|---|---|
| Document version | `v1.0` |
| Reviewed specification | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.1` |
| Review type | Independent documentation-only authorization review |
| Repository baseline | `b452edb628a4dc7bbdc86b2581b46ed43928ca5e` |
| Merge provenance | PR #107, `Merge pull request #107 from darwinpasco/docs/annex-e1-synthetic-uat-dataset-specification-v1-1` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Review status | Complete |
| Canonical authorization outcome | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |

## 2. Purpose and authority

This review determines whether the merged v1.1 documentation is sufficiently exact to authorize a separate task that creates machine-readable synthetic dataset artifacts and offline validators for the 19 included scenarios. It does not modify the v1.0 or v1.1 specifications and does not authorize runtime, API, database, environment, loading, workbook, UAT, delivery, submission, or Production work.

The review did not accept the v1.1 correction-resolution matrix's self-declared closure results without reproduction against the merged files and current bounded runtime.

## 3. Reviewed provenance and inventory

The v1.1 package is integrated at the repository baseline through PR #107. The seven expected v1.1 documents are present:

1. `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.1.md`
2. `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.1.md`
3. `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.1.md`
4. `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.1.md`
5. `ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.1.md`
6. `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Correction_Resolution_Matrix_v1.1.md`
7. `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Manifest_v1.1.md`

The review also inspected the immutable v1.0 package and authorization review, the approved Accounting profile, the Controlled UAT governance package, Z-012B implementation records, current Annex E-1 and Electronic Journal runtime code, state SQL, and focused tests. The six v1.0 documents match their PR #105 merge bytes. The v1.0 authorization review matches its PR #106 merge bytes. No v1.0 or v1.1 specification document changed during this review.

## 4. Manifest and package integrity

The manifest governs exactly six finalized content documents, excludes itself, and excludes `README.md`. SHA-256 was recalculated over exact repository bytes:

| Governed document | Recorded and calculated SHA-256 | Result |
|---|---|---|
| Dataset Specification v1.1 | `af47f89a103f58acc1bd233954c3d02a3ad92a776354c3a7193f2965616a6592` | match |
| Scenario-to-Dataset Mapping v1.1 | `f1234d19679350cc3585d4ffa45f0b29ed442fcd7734a73be9550dd49911216e` | match |
| Expected Value Matrix v1.1 | `dbb42746fa229ace418feb34605f93c20ed8eabe97bc8e2f8b36f39ecf609c3d` | match |
| Source Population and Reconciliation Matrix v1.1 | `e520f97287632cae460612e5c8ac29408a56b40a086d17004da2ccfbb177490d` | match |
| External Decision-to-Scenario Matrix v1.1 | `18d148415fe3e20564b16b4ef64d5ab0d9f46d9f927109be0ce03ede6d40c4c8` | match |
| Correction Resolution Matrix v1.1 | `b1bd98bd000f76c07b1cab4b90568ab42f7a51b659d7a00513de32cdc015d0aa` | match |

The manifest's UTF-8, LF, no-BOM, exact-byte hashing rule is unambiguous. Package identity and history pass. Content authorization does not pass for the findings below.

## 5. Independent eight-finding closure

| Finding | Independent result | Evidence and disposition |
|---|---|---|
| F01 Deterministic identity grammar and registry | `VERIFIED_RESOLVED` | Namespace, grammar, normalization, object tokens, ordinals, and parent rules reconstructed 2,364 unique names and 2,364 unique UUIDv5 values with valid version/variant bits and no collision. |
| F02 Semantic-hash canonicalization | `NOT_RESOLVED` | Current Annex fact and workbook vectors reproduce, but the package-level contract does not give a literal record grammar, exact key names for each sequence member, collection delimiters, or a package-hash test vector. Multiple byte streams satisfy the prose inventory. Several source rows also defer to an unnamed current hasher rather than fixing canonical inputs. |
| F03 SP-A authoritative-source compatibility | `NOT_RESOLVED` | SP-A line values conflict with current fiscal-document validation. The documented SC/PWD line gives `11200 - 2000 + 1200 = 10400`, not net `9200`; the coupon line gives `11200 - 1000 + 1200 = 11400`, not net `10200`. Current `FiscalDocumentCreationService` requires `net = gross - discount + tax`. These rows cannot be created as specified, so the stated Z/BIR/Annex values are not producible from current authoritative sources. |
| F04 Canonical source-row completeness | `NOT_RESOLVED` | Row contracts remain aggregate prose. They omit material literal columns, data types, precision/scale, resolved controlled-code UUIDs, nullable-field values, full request payloads, exact status/audit rows, and literal semantic/content hashes. Phrases such as `exact scope`, `from profile`, `semantic hash from current ... hasher`, and `deterministic opaque ... references` still require implementer choices. |
| F05 Accounting-fact C02 effective time | `VERIFIED_RESOLVED` | The 155 documented facts calculate to instants inside their half-open periods; zero facts equal `period_end_at`. The current repository still writes `period_end_at`, and v1.1 expressly requires a future runtime change before dataset implementation or loading. That ordering contradiction is a separate authorization blocker, not a defect in the corrected timestamp formula. |
| F06 Controlled-test-clock documentation | `VERIFIED_RESOLVED` | The documentation distinguishes time sources, marks current support absent, and specifies environment, startup, PostgreSQL, fail-closed, replay, reset, evidence, cleanup, and Production-prohibition controls. Offline artifacts can be specified independently, but runtime implementation remains blocked. |
| F07 No-activity leakage | `VERIFIED_RESOLVED` | Cases 004 and 005 contain respectively two and three active periods with qualifying documents, fiscal ranges, populated D02/D03, nonzero amounts, and D32 `NONE`. Neither reproduces the excluded zero-document `NO_ACTIVITY` fingerprint. |
| F08 Electronic Journal contract | `NOT_RESOLVED` | The 148-row count and sequence schedule reconstruct, but hashes do not. Section 6.1 assigns runtime-shaped `source_transition_ref` values such as `fiscal-document:<uuid>` and `fiscal-report-request:<uuid>`, while sections 6.2 assigns the same field `source-transition:q`. No exact `source_transition_version` is specified even though it is mandatory in `ElectronicJournalCanonicalizer.CanonicalSemanticText`. Actor/service/idempotency values and several fact values are also described rather than fixed. Consequently none of the 148 semantic and integrity chains can be independently reproduced. |

Four findings are `NOT_RESOLVED`; implementation authorization is therefore blocked.

## 6. Scenario partition and behavior

Included scenarios are exactly `AE1-UAT-001`, `003`, `004`, `005`, `007`, `009`, `010`, `011`, `012`, `013`, `014`, `016`, `017`, `018`, `019`, `020`, `021`, `023`, and `024`. Each maps to one case.

Excluded scenarios are exactly `AE1-UAT-002`, `006`, `008`, `015`, `022`, and `025`. They remain `EXCLUDED_NON_EXECUTABLE`. The sets are disjoint and their union is all 25 catalogue scenarios. No included case aliases the excluded no-activity, correction-workbook, unresolved privilege, golden-workbook, or external-acceptance behaviors.

The output inventory is exactly 22 rows: 17 single-row cases, two rows for case 004, and three rows for case 005. H01-H10 and D01-D32 are structurally populated for every row; no governed blank is used. Structural completeness does not cure the SP-A source incompatibility.

## 7. Identity reconstruction

The namespace `ae1d5e7a-7e2d-5c4d-9a11-202608140001` is valid. Independent UUIDv5 reproduction used RFC 4122 network-order namespace bytes and UTF-8 canonical names.

| Canonical name | Reproduced UUIDv5 | Result |
|---|---|---|
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|period\|0001` | `e1f31225-7457-5573-a8f2-1250e7382ee5` | match |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|fiscal-document\|0001` | `192219dd-a7c9-5a53-adb8-ee1342aff771` | match |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-013\|accounting-fact\|0008` | `e0602e8f-8662-5a63-8313-25672d017b8e` | match |

Complete expansion produced 2,364 names, 2,364 unique names, and 2,364 unique UUIDs. No collision or malformed version/variant was found. These are documentation identities; current report, workbook, stream, event, child, and audit repositories still generate persisted UUIDs at runtime.

## 8. Semantic-hash review

Current runtime Annex fact and workbook vectors reproduce exactly:

| Vector | Recalculated SHA-256 | Result |
|---|---|---|
| Normal fact / exact replay | `4ef18d69421a1e8a289f4bb7327c1b3da86dc260b38bb6c50119ed7e31418c4d` | match |
| Changed fact amount | `5f1f028745d3e2205efc1f339befb98c9e06bf1fb3a72d9013037ceb738b5159` | match/conflict |
| Omitted rather than null fact field | `f32c10c4982811e357230d439a35eabc5281a4739ff869111d23f342635bd0c7` | match/not replay-equivalent |
| Normal workbook request | `2e88439b86615f33934026243d57c0f3b2da2aa4ea1aaaaf66458cf37bc10d95` | match |
| Empty workbook sources | `7616f56c09787d7944a0d80f3026c588243dd42c50222e862120a9cc226991c9` | match/rejected input |
| Array `a,b` | `207571055a4dc87f9c27edd8f935f23fee19c6c3c1e0b96fe16369e7ac32ee89` | match |
| Array `b,a` | `ff26eaa80429b6e71e09658838120e216c1cb5ad60ecae64501ecfc9133fb17c` | match/material reorder |

The current runtime's property order and null behavior are documented correctly for these two hashers. The offline package hash and several source-object hashes remain under-specified as recorded in F02 and F04.

## 9. SP-A and arithmetic review

The package arithmetic is internally self-consistent after accepting its expected positions: R01-R07, R11, and R12 recalculate to the stated values for RP-A11, RP-B11, and RP-D11 with zero tolerance. The SP-A expected figures are gross 33,600, net/tenders 28,600, VATable sales 30,000, VAT 3,600, SC discount 2,000, PWD discount 2,000, coupon 1,000, and void 11,200.

They are not source-producible as written. The line validator adds tax to gross after discount. Correcting only line gross to make the documented net valid would change current X/Z `gross_sales_amount_minor_units`, which sums line gross, and would change D07, D27, D29, and related reconciliations. Choosing a replacement amount is an Accounting/specification correction, not an authorization-review action.

## 10. Source rows, C02, and clock boundary

The C02 expansion yields 155 facts and zero boundary failures. Effective timestamps are unique and inside their periods. The current `PostgresAnnexE1Repository` persists `period.PeriodEndAt`, so current loading cannot produce the documented rows without a separately authorized runtime change.

The clock contract correctly labels support `ABSENT`. It separates business/effective time from runtime/database/audit time and can support a future runtime review. Machine-readable offline values do not inherently require that runtime feature. However, the v1.1 text itself says the C02 runtime change must exist before dataset implementation; this must be reconciled in the next documentation version before an implementation token can be issued.

Canonical row review found no machine-readable row ledger. The schema exposes constraints and fields not fixed by the prose contracts, including complete fiscal-document void columns, controlled-code IDs, report request hashes, audit action/result IDs, source transition versions, and exact nullable values. An implementer would have to inspect code and choose values, contrary to the stated objective.

## 11. Electronic Journal review

The population count is arithmetically correct: 22 periods contribute document, X, Z, and BIR events (88), and 15 SP-A periods contribute three additional document commits plus one void event (60), for 148. Per-case stream sequences are gap-free under the schedule, and 22 Z plus 22 BIR events are represented.

The hash chain is not closed. Current semantic hashing includes `source_transition_ref`, `source_transition_version`, effective time, optional identities, idempotency, and exact facts. Current integrity hashing additionally includes event reference, stream sequence, recorded time, semantic hash, predecessor hash, actor, service, correlation, and retention policy. The documented conflicting transition reference and missing version prevent the first semantic hash, so every downstream integrity hash is indeterminate. R08/R10 can verify counts conceptually but cannot verify the required exact journal identities and hashes.

## 12. Reconciliation results

| Rule | Independent result |
|---|---|
| R01 | stated integer equations pass; SP-A authoritative input fails runtime producibility |
| R02 | stated integer equations pass |
| R03 | stated integer equations pass |
| R04 | stated integer equations pass |
| R05 | stated integer equations pass |
| R06 | stated integer equations pass; SP-A depends on invalid D07 source value |
| R07 | stated integer equations pass; SP-A depends on invalid D27 source value |
| R08 | blocked by incomplete source hashes and SP-A source incompatibility |
| R09 | scope rule is stated; exact row values are incomplete |
| R10 | blocked by indeterminate Electronic Journal hashes |
| R11 | stated tender equations pass; SP-A source documents fail creation validation |
| R12 | stated GTA equations pass; SP-A contribution is not currently producible |

Monetary tolerance remains 0 minor units and count tolerance remains 0 rows.

## 13. Source populations and deltas

The v1.1 aggregate schedule recalculates to these counts:

| Object | v1.1 count | v1.0 count | Delta |
|---|---:|---:|---:|
| Sites / Site POS Server scopes | 19 | 19 | 0 |
| Fiscal identities | 19 | 19 | 0 |
| Header profiles | 19 | 19 | 0 |
| Workbook definitions | 19 | not governed | new inventory |
| Fiscal periods / X / Z / BIR / Annex rows | 22 each | 22 each except Annex rows not separately governed | 0 for reports/periods |
| Fiscal documents / lines / totals / tax details | 67 each | 35 each | +32 each |
| Tenders | 69 | 37 | +32 |
| Discount details | 45 | 30 | +15 |
| Statutory facts | 30 | not governed | new inventory |
| Status-history rows | 82 | not governed | new inventory |
| Accounting facts | 155 | 154 | +1 |
| Electronic Journal records / transitions | 148 each | 116 records; transitions not governed | +32 records |
| Fiscal ranges | 44 | 20 | +24 |
| Tender breakdowns | 48 | not governed | new inventory |
| Discount breakdowns | 90 | not governed | new inventory |
| Annex fact links | 154 | not governed | new inventory |
| Requests | 346 | not governed | new inventory |
| Audit rows | 186 | not governed | new inventory |
| Replay / conflict / recovery rows | 2 each | not governed | new inventory |

The aggregate counts are mathematically reproducible. They do not prove that the rows are valid or fully specified; F03, F04, and F08 remain blocking.

## 14. External decisions, privacy, and offline posture

All ten confirmations remain exactly `UNRESOLVED`: AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-012, AE-DR-016, AE-DR-016B, AE-DR-019, AE-DR-020A, and AE-DR-024. The union references for AE-DR-011A, AE-DR-012, AE-DR-019, AE-DR-020A, and AE-DR-024 are preserved. None is resolved, waived, accepted, assumed, superseded, or made inapplicable. The six externally blocked scenarios remain excluded.

The package uses synthetic labels and requires no real person, customer, vehicle, payment account, taxpayer, operator, employee, credential, statutory-ID image, raw payload, secret, network call, or external authority. Offline implementation would be technically possible after the specification defects are corrected; the block is precision and compatibility, not privacy or connectivity.

## 15. Runtime and schema compatibility

| Area | Current implementation | v1.1 posture | Result |
|---|---|---|---|
| Annex fact/workbook semantic hashes | current canonicalizers match published vectors | exact for published vectors | compatible |
| Persisted identifiers | runtime uses `Guid.NewGuid()` for reports, workbook rows, streams, events, and audits | deterministic planned UUIDs; runtime change unauthorized | blocked from runtime/loading; offline only |
| Accounting fact effective time | repository writes exclusive period end | v1.1 requires in-period C02 instant | runtime incompatible; documentation ordering must be corrected |
| Fiscal line validation | net equals gross minus discount plus tax | SP-A uses gross minus discount | incompatible and blocks source creation |
| X/Z aggregation | gross sums recorded line gross; void sums voided line net | v1.1 expected SP-A assumes invalid line values | incompatible |
| Electronic Journal transition input | writer requires runtime source reference and nonblank version | v1.1 conflicts on reference and omits version | incompatible/unhashable |
| Runtime/database clocks | live PostgreSQL/application clocks | future test-clock contract explicitly absent | properly blocked; offline values separable |
| Source row contracts | SQL has exact types, nullability, constraints, code IDs, and relationships | v1.1 supplies aggregate prose | incomplete for implementation |

## 16. Numbered blocking findings

1. The offline dataset semantic-hash grammar and broad source-object hash inventory are not byte-complete. Publish exact key names, record/collection framing, canonical payloads, and vectors for every governed hash family.
2. SP-A cannot pass current fiscal-document validation. Publish a new version with source values that satisfy `net = gross - discount + tax` and then recompute every affected X, Z, BIR, Annex, tender, Accounting, and reconciliation value without changing approved Accounting semantics.
3. Canonical row definitions are not row-level implementation contracts. Publish complete per-row field/type/null/code/relationship/request/transition/audit/hash values, including exact resolved governed code identities or a deterministic read-only resolution contract.
4. The specification states that a runtime C02 change must precede dataset implementation even though this review may authorize offline artifacts only. Correct the ordering: offline artifacts may represent documented values, while runtime acceptance/loading remains separately blocked, or explicitly retain the implementation block.
5. Electronic Journal transition references are contradictory and transition versions are missing. Fix the field mapping, specify every canonical input, and publish or reproducibly derive all 148 semantic and chained integrity hashes.

## 17. Activity authorization table

| Activity | Decision | Basis |
|---|---|---|
| v1.1 specification acceptance | `BLOCKED` | Four original findings and five material review findings remain. |
| Machine-readable dataset implementation for 19 included scenarios | `BLOCKED` | Source values and canonical rows/hashes are not implementation-ready. |
| Offline deterministic-validator implementation | `BLOCKED` | Validators lack closed source and EJ expected values. |
| Dataset implementation for excluded scenarios | `BLOCKED` | Six scenarios remain non-executable. |
| Controlled-test-clock runtime implementation | `BLOCKED` | Separately governed runtime change. |
| Persisted deterministic-identity runtime changes | `BLOCKED` | Separately governed runtime/database behavior. |
| Other runtime or API changes | `BLOCKED` | Outside review scope. |
| Database or migration changes | `BLOCKED` | Outside review scope. |
| Isolated-environment specification | `AUTHORIZED` | Existing documentation-only preparation authority remains. |
| Environment provisioning | `BLOCKED` | No provisioning authority or assigned environment. |
| Named role assignment | `BLOCKED` | No authoritative named assignments. |
| Controlled UAT execution-subset approval | `BLOCKED` | Dataset and environment prerequisites remain unmet. |
| Dataset loading | `BLOCKED` | Dataset absent and runtime prerequisites unmet. |
| Controlled UAT execution | `BLOCKED` | External and operational gates remain. |
| Evidence review and acceptance | `BLOCKED` | No authorized execution evidence exists. |
| Internal workbook generation | `BLOCKED` | Generation is not authorized by this review. |
| External Annex E-1 delivery | `BLOCKED` | External decisions and delivery authority remain unresolved. |
| BIR submission | `BLOCKED` | Submission is not authorized. |
| Production rollout | `BLOCKED` | Production authority and controls remain absent. |
| Annex E-2 through E-5 | `BLOCKED` | Out of scope and unauthorized. |
| ARTS POSLog 6.0.0 | `BLOCKED` | Deferred and unauthorized. |
| Signing or encryption | `BLOCKED` | AE-DR-016 remains unresolved. |
| Destructive retention, archival, cleanup, or purge | `BLOCKED` | AE-DR-016B and governed retention authority remain unresolved. |

## 18. Canonical outcome and boundaries

`BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION`

No partial implementation authorization is granted. The block covers machine-readable datasets, offline validators, all six excluded scenarios, runtime clock or identity work, APIs, database changes, loading, provisioning, role assignment, Controlled UAT, workbook generation, evidence acceptance, delivery, BIR submission, Production, Annex E-2 through E-5, ARTS POSLog, signing, encryption, and destructive retention actions.

Only isolated-environment specification and a new documentation-only correction package are authorized. This review does not change any historical token or resolve an external confirmation.

## 19. Residual prerequisites and next bounded activity

Prepare a new-version, documentation-only Annex E-1 synthetic UAT dataset specification correction package that resolves all five numbered findings in section 16 and revalidates the original eight findings. Do not rewrite v1.1. The correction may specify future runtime-contract prerequisites but must not implement runtime code, datasets, validators, database changes, environment provisioning, loading, workbook generation, or Controlled UAT. A separate authorization review is mandatory after the corrected package is merged.

