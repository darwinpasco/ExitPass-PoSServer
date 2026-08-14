# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification Authorization Review v1.0

## 1. Document control

| Item | Value |
|---|---|
| Review date | `2026-08-14` |
| Repository baseline | `7b9ac19cfc1b84167eca18b3a82409e6e53344aa` |
| Merged specification commit | `9ea2bfd7e34cca1e79b55fffefe76847be1a9b13` |
| Merged specification integration | `7b9ac19cfc1b84167eca18b3a82409e6e53344aa`, PR #105 |
| Specification ID | `annex-e1-synthetic-uat-dataset:v1.0` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Preceding authority | `AUTHORIZED_FOR_NEXT_BOUNDED_PREPARATION` |
| Review status | `COMPLETE` |
| Canonical outcome | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |

## 2. Purpose and authority

This review independently assesses whether the merged synthetic dataset specification is complete, reproducible, arithmetically sound, compatible with the approved Accounting Calculation Profile, and implementable against the current bounded Annex E-1 runtime without invention. It does not alter the approved Accounting profile or any of the six reviewed specification documents.

The specification package is internally useful and its visible Annex E arithmetic is consistent, but it is not implementation-ready. Material identity, source-population, chronology, runtime-clock, scenario-boundary, and source-authority defects require correction before machine-readable fixture or validator implementation can be authorized.

## 3. Provenance and reviewed inventory

The specification was merged by `Merge pull request #105 from darwinpasco/docs/annex-e1-synthetic-uat-dataset-specification`. The reviewed specification documents are:

1. [Synthetic UAT Dataset Specification](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.0.md).
2. [Synthetic UAT Scenario-to-Dataset Mapping](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.0.md).
3. [Synthetic UAT Expected Value Matrix](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.0.md).
4. [Synthetic UAT Source Population and Reconciliation Matrix](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Source_Population_and_Reconciliation_Matrix_v1.0.md).
5. [External Decision-to-Scenario Reconciliation Matrix](ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.0.md).
6. [Synthetic UAT Dataset Specification Manifest](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Manifest_v1.0.md).

The review also inspected the [Controlled UAT Authorization Review](ExitPass_POS_Server_Annex_E1_Controlled_UAT_Authorization_Review_v1.0.md), the approved Accounting Calculation Profile, the Z-012B implementation note, all seven Z-012D preparation documents, current Annex E runtime/API code, canonical PostgreSQL object sources, controlled codes, and relevant runtime and PostgreSQL integration tests.

## 4. Manifest and hash verification

The manifest governs exactly five finalized documents and intentionally does not hash itself. Every recorded hash matches the current merged file:

| Document | Recorded and calculated SHA-256 | Result |
|---|---|---|
| Dataset Specification | `f07ac74a241c56b64114909e9685d424e773bda103d7fcbab7a8d16c666637e8` | `PASS` |
| Scenario-to-Dataset Mapping | `1b13cd6592e7474952595da17c654482538ba7e6bef70af1adcad819568b8c9a` | `PASS` |
| Expected Value Matrix | `34168716d270458e2d256047fb40febc610c299dca4f5599f3f7ef485d26de18` | `PASS` |
| Source Population and Reconciliation Matrix | `b25c01b9545c3a83dc85058b760fa710a4aea2972e132b0c327d2cea8a7cdf69` | `PASS` |
| External Decision-to-Scenario Matrix | `706518ef774539b50c2b2d3cfc17205ef0005402878972b6ec237bba293b43db` | `PASS` |

The Accounting profile hash is exactly `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f`. No approved or hashed specification file was changed by this review.

## 5. Scenario partition review

The declared included set has 19 unique scenarios: `AE1-UAT-001`, `003`, `004`, `005`, `007`, `009`, `010`, `011`, `012`, `013`, `014`, `016`, `017`, `018`, `019`, `020`, `021`, `023`, and `024`.

The declared excluded set has six unique scenarios: `AE1-UAT-002`, `006`, `008`, `015`, `022`, and `025`. The declared sets are disjoint and their union is the 25-scenario catalogue. Each declared included scenario maps to one dataset case, and the case-to-output table totals 22 control or output rows.

The substantive partition fails. `DS-AE1-004` and `DS-AE1-005` include `SP-C` no-activity periods with blank D02/D03, explicit zero D14/D15, and `NO_ACTIVITY` D32. Those are the material assertions of excluded `AE1-UAT-002`, whose D32 and inactive-privilege representation remains blocked by AE-DR-010 and AE-DR-011A. The external-decision matrix does not add `004` or `005` to those decision references. An excluded scenario is therefore reintroduced through shared populations and expected rows.

## 6. Deterministic identity review

The UUIDv5 algorithm is reproducible when a complete canonical input string is supplied. The implementation used RFC 4122 network byte order, SHA-1, UTF-8, version 5, and RFC variant bits. The RFC reference vector `DNS namespace + www.widgets.com` produced `21f7f8de-8051-5b89-8680-0195ef798b6a`.

Representative specification inputs reproduced as follows:

| Canonical input | Reproduced UUIDv5 |
|---|---|
| `annex-e1-synthetic-uat:v1.0\|AE1-UAT-001\|period\|0001` | `0fe9c9ae-80bc-56d1-be36-f0abbdc23bbf` |
| `annex-e1-synthetic-uat:v1.0\|AE1-UAT-001\|fiscal-document\|0001` | `b52c5cae-d622-5a22-ac2f-91c2b4b02df3` |
| `annex-e1-synthetic-uat:v1.0\|AE1-UAT-013\|accounting-fact\|0008` | `b252850e-8c8c-5bff-837b-d332715e98a5` |

The identity profile is nevertheless incomplete. It does not publish a closed object-type/ordinal registry for all required common, parent, child, report-request, transition, source-link, audit, stream, output, replay, and failure records. The mapping uses `AE1-UAT-nnn` as the scenario segment, while the source matrix says the per-case `DS-AE1-nnn` identity replaces that segment. The dataset-case identity itself has no canonical object type or ordinal. These rules yield multiple possible UUIDs and force an implementer to choose identifiers.

The dataset semantic hash profile also omits the complete canonical field inventory and the declared sequence for each nested array. Formatting rules alone do not define the bytes to hash.

## 7. Chronology and period-boundary review

The document, X, Z, BIR Summary, replay, conflict, restart, and row-order sequences are fixed in UTC and the document period windows are stated as half-open `[start,end)`. The start and end boundary examples are logically ordered.

The Accounting fact schedule is not compatible with the approved profile. Rule C02 requires each fact's effective instant to be in `[period_start_at, period_end_at)`, but every population fact is specified as effective exactly at `period_end_at`. The current repository also persists `effective_at = period_end_at`; this runtime behavior contradicts C02 rather than resolving the specification. A documentation-only reinterpretation is prohibited.

The fixed Annex generation time and H09 value are not reproducible through the current API. `PostgresAnnexE1Repository` obtains generation and fact-recorded timestamps from PostgreSQL `clock_timestamp()`, and neither `AnnexE1GenerationRequest` nor `AnnexE1FactRequest` accepts a governed time. The specification requires `2026-09-30T10:00:00Z` but defines no supported clock-control mechanism.

## 8. Monetary and arithmetic review

All stated monetary values are signed 64-bit PHP minor units. No binary floating-point operation is required. H01-H10 are all present. D01-D32 are all present for each of the eight row profiles; the only declared blanks are D02/D03 on the two no-activity profiles.

Independent integer recalculation produced zero difference for R01-R07, R11, and R12 for `RP-A`, ordinary `RP-B`, both no-activity GTA states, and both `RP-D` GTA states. The profile results are:

| Profile | R01 | R02 | R03 | R04 | R05 | R06 | R07 | R11 | R12 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| SP-A | 16000 | 1000 | 4000 | 500 | 2000 | 17000 | 11000 | 13000 | 113000 |
| SP-B | 11200 | 0 | 0 | 0 | 1200 | 11200 | 10000 | 11200 | 11200 |
| SP-C, GTA 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| SP-C, GTA 11200 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 11200 |
| SP-D, GTA 0 | 22400 | 0 | 0 | 0 | 2400 | 22400 | 20000 | 22400 | 22400 |
| SP-D, GTA 11200 | 22400 | 0 | 0 | 0 | 2400 | 22400 | 20000 | 22400 | 33600 |

Tolerance is exactly zero minor units. These equations prove that the expected matrix is internally arithmetical; they do not prove that current authoritative source rows can produce its operands. R08 and R10 fail source implementability for the reasons in sections 9 and 10.

## 9. Source-population review

The population multipliers independently reproduce the declared aggregate counts:

| Object | Declared | Recalculated |
|---|---:|---:|
| Sites / fiscal identities / header profiles | 19 / 19 / 19 | 19 / 19 / 19 |
| Fiscal periods | 22 | 22 |
| Fiscal documents / lines / totals | 35 / 35 / 35 | 35 / 35 / 35 |
| Tenders / tax details / discount details | 37 / 35 / 30 | 37 / 35 / 30 |
| X / Z / BIR reports | 22 / 22 / 22 | 22 / 22 / 22 |
| Accounting facts / EJ records / fiscal ranges | 154 / 116 / 20 | 154 / 116 / 20 |

The aggregate counts do not provide exact implementable rows. Required controlled-code keys, required and nullable columns, report-request rows, status/history rows, fiscal sequence policy/state rows, period-close transition rows, report child rows, source bindings, semantic-hash inputs, actor/correlation values, and audit records are not exhaustively defined. Lines are described only as amounts equal to a parent split, leaving quantity, line type/status, gross/discount/tax/net allocation, description, and source reference open. Tax, total, tender, discount, and statutory rows likewise omit required classifications and exact per-row allocation.

The 116 Electronic Journal rows are only identified by event type and order. Canonical rows additionally require a stream, event and source-transition versions, effective and recorded timestamps, actor/service/correlation references, event facts, retention classification, semantic and integrity hashes, predecessor linkage, and source bindings. Those values cannot be inferred without creating fixture policy.

## 10. Runtime and schema compatibility

| Specification area | Current repository behavior | Result |
|---|---|---|
| Site, fiscal identity, header | Canonical tables require keys, parent/scope links, lifecycle/status, effective dates, and governed references beyond the listed display values | `BLOCKING_MISSING_ROWS` |
| Fiscal document children | Lines, totals, tenders, taxes, discount privileges, and applied statutory snapshots require exact controlled codes and per-row values | `BLOCKING_MISSING_FIELDS` |
| SP-A SC/PWD facts | One `pos.fiscal_document_applied_statutory_facts` row is permitted per document; SP-A puts SC and PWD detail on one active document | `BLOCKING_CARDINALITY_CONFLICT` |
| SP-A other statutory | `FiscalXReadingAggregationService` initializes `otherStatutory` to zero and has no implemented source path that increments it | `BLOCKING_AUTHORITATIVE_SOURCE_MISMATCH` |
| X/Z/BIR reports | Runtime creates requests, scopes, child breakdowns, ranges, close-state transitions, hashes, and audit evidence not fully represented by the population matrix | `BLOCKING_MISSING_ROWS` |
| Accounting fact effective time | Approved C02 requires an in-period instant; runtime writes the exclusive period end | `BLOCKING_APPROVED_PROFILE_CONFLICT` |
| Electronic Journal | Canonical schema requires complete privacy-safe facts and hash-chain inputs; population specifies only taxonomy/order | `BLOCKING_MISSING_FIELDS` |
| Workbook IDs and H09 | Runtime uses generated UUIDs and PostgreSQL live time; fixed output identity/time is not injectable through the API | `BLOCKING_REPRODUCIBILITY_CONFLICT` |
| Calculations | Current engine preserves AE-DR-006 through AE-DR-009 and D29 = D27 + D06 + D28 | `PASS` |
| Replay/artifact bytes | Current runtime returns the stored artifact for exact replay and verifies stored hash/length | `PASS` |

SP-A is not constructible as written. The active document cannot hold both the SC and PWD authoritative statutory snapshots because the schema enforces one snapshot per document. The Z aggregation accepts only Senior Citizen or PWD for that snapshot. It also never produces a nonzero `other_statutory_discount_amount_minor_units`. Consequently D12, D13, and D16 cannot simultaneously reach the specified values through current authoritative runtime sources.

## 11. Replay, conflict, recovery, and correction review

The intended replay, changed-semantic conflict, stored-byte readback, restart, tamper, and publication-rollback outcomes agree with the current bounded runtime at the behavioral level. Exact replay retains committed workbook identity and stored bytes; changed semantics under the same operation conflict; a superseding Accounting fact does not mutate a prior workbook; correction workbooks remain excluded; destructive cleanup remains unauthorized.

The specification cannot yet produce exact replay fixtures because it does not completely define operation/source hash inputs, runtime-generated workbook/report identities, live timestamps, or all source rows. The correction-fact lineage for `AE1-UAT-013` is conceptually valid, but its fixed recorded time is not controllable through the current API.

## 12. External-decision reconciliation

All ten decisions remain exactly `UNRESOLVED`: AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-012, AE-DR-016, AE-DR-016B, AE-DR-019, AE-DR-020A, and AE-DR-024. None is resolved, accepted, waived, assumed, or marked inapplicable by this review.

The merged matrix contains the prior-source union for AE-DR-011A, AE-DR-012, AE-DR-019, AE-DR-020A, and AE-DR-024. It is incomplete against the new dataset populations because no-activity behavior in `DS-AE1-004` and `DS-AE1-005` also exercises AE-DR-010 and AE-DR-011A. The six declared excluded scenarios remain labeled non-implementable, but the no-activity assertions of excluded `AE1-UAT-002` leak into included cases.

## 13. Privacy and security review

The planned display identifiers and references are visibly synthetic, require no external connectivity, and exclude customer, vehicle, payment, employee, credential, raw statutory evidence, request/response payload, and stack-trace content. The dataset can remain offline. The synthetic TIN, MIN, serial, accreditation, and PTU references are explicitly fixture-only.

Privacy design passes at the value-classification level. Authorization remains blocked because the missing row definitions could otherwise lead an implementer to invent unrestricted JSON, event facts, or source references outside the approved privacy boundary.

## 14. Numbered findings

1. **Deterministic identity is not closed.** Scenario versus case identity inputs conflict, the dataset-case identity is undefined, and no complete object-type/ordinal registry exists. Correction: complete and reconcile the deterministic identity input rules and publish one closed object-type/ordinal registry with representative UUID test vectors.
2. **Dataset hash bytes are not fully defined.** The field inventory and nested-array sequence for the dataset semantic hash are absent. Correction: complete the semantic-hash field inventory, null rules, and nested-array ordering, then publish the expected dataset hash per case.
3. **SP-A cannot be produced by current authoritative sources.** One active document cannot authoritatively represent both SC and PWD snapshots, and current Z aggregation cannot produce nonzero other statutory discount. Correction: redesign SP-A so every statutory population is producible from current authoritative sources without requiring one document to carry incompatible SC and PWD snapshots; recalculate every affected row, count, and hash without changing approved Accounting formulas or runtime code.
4. **Exact canonical source rows are incomplete.** Required columns, controlled-code keys, parent/child rows, requests, transitions, breakdowns, hashes, and audit/EJ fields are omitted. Correction: specify all missing canonical columns, controlled codes, requests, transitions, breakdowns, hashes, audit rows, source facts, explicit nullable values, and relationships.
5. **Accounting fact chronology violates approved C02.** Facts are effective at the exclusive period end, and the current runtime writes the same invalid boundary. Correction: place specified Accounting-fact effective timestamps within the approved half-open fiscal period under C02 and document any required runtime-contract change without implementing it.
6. **Fixed H09 and fact-recorded timestamps are not reproducible.** The runtime uses PostgreSQL live time and exposes no governed clock input. Correction: define a deterministic-clock control contract as documentation only; do not modify runtime in this authorization branch or under this outcome.
7. **Excluded no-activity behavior leaks into included cases.** `DS-AE1-004` and `DS-AE1-005` reproduce excluded `AE1-UAT-002` and omit the resulting AE-DR-010/011A links. Correction: remove excluded no-activity behavior from included cases 004 and 005, or explicitly apply the governing AE-DR-010 and AE-DR-011A blocks.
8. **Electronic Journal fixtures are under-specified.** Event taxonomy and order do not define canonical facts, hashes, source transitions, streams, retention, and timestamps. Correction: complete all 116 Electronic Journal row definitions, including canonical facts, transitions, streams, retention metadata, and expected semantic and integrity hashes, without deriving Annex financial amounts from EJ.

All eight findings block dataset implementation. The authorized response is one documentation-only correction package. It may specify required runtime-contract changes but cannot implement them. The corrected specification requires a new version, new manifest hashes, and renewed authorization review; the merged v1.0 historical documents must not be rewritten.

## 15. Activity authorization table

| Activity | Decision | Basis and permitted boundary |
|---|---|---|
| Dataset specification acceptance | `BLOCKED` | Eight material findings prevent acceptance; documentation correction only is permitted |
| Documentation-only specification correction package | `AUTHORIZED` | May address all eight findings in a new version and specify, but not implement, required runtime-contract changes |
| Machine-readable synthetic dataset implementation | `BLOCKED` | Canonical implementation token is not granted |
| Offline dataset validation-tool implementation | `BLOCKED` | Validators would encode unresolved identities, rows, and timestamps |
| Isolated-environment specification | `AUTHORIZED` | Existing documentation-only environment planning remains valid |
| Environment provisioning | `BLOCKED` | No provisioning authority, owner assignment, or accepted dataset |
| Named role assignment | `BLOCKED` | Role definitions are not governed principal assignments |
| Executable scenario-subset approval | `BLOCKED` | Scenario leakage and external gates remain unresolved |
| Dataset loading | `BLOCKED` | No accepted or implemented dataset; no database authority |
| Controlled UAT execution | `BLOCKED` | Dataset, environment, role, scenario, and external prerequisites fail |
| Execution evidence review and acceptance | `BLOCKED` | No authorized execution evidence may exist |
| Internal Annex E-1 workbook generation | `BLOCKED` | Fixed-time and scenario gates fail; generation remains unauthorized |
| External Annex E-1 delivery | `BLOCKED` | External confirmations and delivery authority absent |
| BIR submission | `BLOCKED` | Submission authority and channel requirements absent |
| Production rollout | `BLOCKED` | UAT, external decisions, retention, delivery, and Production controls absent |
| Annex E-2 through E-5 | `BLOCKED` | Outside the approved E-1 profile |
| ARTS POSLog 6.0.0 | `BLOCKED` | Separate deferred workstream |
| Signing or encryption | `BLOCKED` | AE-DR-016 unresolved and outside local bounded scope |
| Destructive retention, archival, cleanup, or purge | `BLOCKED` | AE-DR-016B unresolved; only non-destructive invocation cleanup is documented |

## 16. Canonical outcome and scope

`BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION`

This outcome authorizes only isolated-environment specification and documentation correction. It authorizes no runtime modification, machine-readable dataset, validator, SQL, fixture, seed, migration, database change or load, environment provisioning, scenario, workbook, evidence acceptance, delivery, submission, or Production activity. It does not modify or resolve any external confirmation.

> Prepare a documentation-only Annex E-1 synthetic UAT dataset specification correction package addressing all eight blocking findings. The correction package may specify required runtime-contract changes but must not implement runtime code, executable datasets, validators, database changes, environment provisioning, loading, or Controlled UAT execution. The corrected specification must be reviewed again before dataset implementation can be authorized.

The corrected specification must use a new version and must not rewrite the merged v1.0 historical documents. Runtime modification is not authorized. Dataset and validator implementation remain blocked. The six excluded scenarios remain non-executable, all ten external confirmations remain unresolved, and re-review is mandatory after the correction package is merged. No new task identifier is assigned here.

## 17. Residual risks and explicit prohibitions

Until a corrected package is merged and explicitly authorized:

- dataset implementation and offline validator implementation are prohibited;
- runtime modification and database changes are prohibited;
- all 19 declared cases remain non-executable;
- all six excluded scenarios remain excluded;
- all ten external confirmations remain unresolved;
- database access/loading and environment provisioning remain prohibited;
- Controlled UAT, internal workbook generation, evidence acceptance, delivery, BIR submission, and Production remain prohibited;
- Annex E-2 through E-5, ARTS POSLog, signing, encryption, and destructive retention remain prohibited.
