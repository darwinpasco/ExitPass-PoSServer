# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.2 Authorization Review v1.0

## 1. Document control

| Item | Value |
|---|---|
| Review type | Independent documentation-only authorization review |
| Reviewed package | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.2` |
| Repository baseline | `404d99c385b3d496ae2ca2c16a2122f955f73e02` |
| v1.2 merge provenance | PR #109; merge `404d99c385b3d496ae2ca2c16a2122f955f73e02`; parents `f4a3a262f0e35bd91d58197fbf59aaeeda911c18` and `21b189c42c3c7c13f025f8c54e963e76e19555dc` |
| Task decision | `READY_FOR_REVIEW` |
| Canonical authorization outcome | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Dataset or validator implementation | Not authorized |
| Review date | 2026-08-15 |

## 2. Executive summary

The v1.2 package is intact and materially improves the prior specification. Independent reconstruction produced all 2,364 UUIDv5 identities without collision, reproduced V01-V13, placed all 155 Accounting facts inside their half-open periods, and reproduced all 148 Electronic Journal semantic hashes and integrity chains with zero mismatch. Scenario partition, 22-row coverage, source counts, integer arithmetic, and the ten unresolved external decisions also reconcile.

Implementation authorization nevertheless remains blocked. Three material defects prevent two conforming implementers from producing the same complete dataset:

1. the package-content grammar has no normative key/preimage form for non-empty EJ, H01-H10, Annex-row, or evidence entries, and source-object rows do not define an exact JSON key mapping for every family;
2. the SP-A Senior Citizen and PWD statutory snapshots fail current `FiscalDocumentCreationService` validation because `9200 + 2000 > 10000` while the package fixes original amount at 10000;
3. the 29 row-family contracts enumerate columns but leave material values as `exact`, `synthetic`, or generation-time evidence, including statutory command/scope/tariff UUIDs, several source and approval references, lifecycle times, report semantic inputs, workbook artifact hash/length, recovery observations, and audit action/reason values.

The package therefore cannot authorize machine-readable artifacts or offline validators without discretionary value selection. No scenario subset is separately authorized because the merged specification defines one governed package and does not close a complete independent subset.

## 3. Baseline and provenance

Local `HEAD`, `origin/dev`, and merge base were all `404d99c385b3d496ae2ca2c16a2122f955f73e02`; divergence was zero ahead and zero behind. PR #109 is a regular two-parent merge. Its content parent `21b189c42c3c7c13f025f8c54e963e76e19555dc` added the eight v1.2 documents and modified only this directory's `README.md`. It changed no runtime, API, database, migration, configuration, CI, dependency, or generated artifact.

Prior provenance was also verified:

| Package/review | Merge | Content parent | Result |
|---|---|---|---|
| v1.0 specification | PR #105, `7b9ac19cfc1b84167eca18b3a82409e6e53344aa` | `9ea2bfd7e34cca1e79b55fffefe76847be1a9b13` | historical bytes retained |
| v1.0 authorization review | PR #106, `d16f23cef27d87f451ef08ff9f74eb4afe800c8f` | `088c78f48750488da6ccd354ea83950f3679fe27` | historical bytes retained |
| v1.1 specification | PR #107, `b452edb628a4dc7bbdc86b2581b46ed43928ca5e` | `4f405bb57e5e88f8bf980d48846f681a5be7897f` | historical bytes retained |
| v1.1 authorization review | PR #108, `f4a3a262f0e35bd91d58197fbf59aaeeda911c18` | `0d814e0ac230a69aac88b21f71bc17d99b0ba9e4` | historical bytes retained |

Blob-ID comparison against each content commit found every v1.0/v1.1 specification and authorization-review document byte-identical. Version 1.2 added new versioned paths; it did not rename, replace, overwrite, or reinterpret a historical file.

## 4. Documents reviewed

The eight v1.2 package documents, all v1.0/v1.1 package and authorization-review documents, this package README, the current Annex calculation/hash contracts, and these current implementation sources were inspected:

- `FiscalDocumentCreationService.cs` and `FiscalDocumentSemanticRequestHasher.cs`;
- `FiscalXReadingAggregationService.cs`;
- `AnnexE1SemanticHasher.cs` and `PostgresAnnexE1Repository.cs`;
- `ElectronicJournalCanonicalizer.cs`, `PostgresElectronicJournalWriter.cs`, and fiscal document/X/Z/BIR journal append sites;
- current fiscal reporting, document, journal, and Annex models and PostgreSQL mappings.

## 5. Manifest integrity

Hashes were calculated from exact merged bytes without line-ending normalization. The manifest intentionally excludes itself and governs the other seven v1.2 documents.

| Governed document | Recorded and calculated SHA-256 | Result |
|---|---|---|
| Specification | `70c2721645bd7846845ce8ee4be911a127ce6ab76b6dde08977f3e3a74592d9c` | match |
| Scenario mapping | `a6c82c296c431940649d16efb80fbebc8fc016dbd33e77c9df23500edce568e1` | match |
| Expected values | `eb93666cb3893dea742c45a066ffab7e1edc0cbed59dd955f1172b928ab0063e` | match |
| Source population | `90f9369d3bcc1c697ae480f7bc50458ae13fc7f5de810187ec14d8b275b9ece4` | match |
| External decisions | `856a8665938a204a82b8e6dbaf38db00842aab3d83e2ae62185d6b5e694c38b1` | match |
| Canonical source/hash contract | `ec060c47148b8d6ec24118584b34efae4b4460684569fe5866b708a6dcffb63f` | match |
| Correction resolution | `d9fd5907f11c93efd7f6d3a2d450956f0164d7b317a0117a28924e7538555450` | match |

## 6. F01-F08 disposition

| Finding | Independent disposition | Evidence |
|---|---|---|
| F01 deterministic identity | `VERIFIED_RESOLVED` | 2,364 reconstructed names, unique UUIDs, valid v5/variant bits, and three known vectors all match. |
| F02 semantic-hash contract | `NOT_RESOLVED` | V01-V13 match, but package entries for non-empty EJ/header/Annex/evidence collections and source-family JSON keys remain unspecified. |
| F03 SP-A compatibility | `NOT_RESOLVED` | Line equations and aggregates pass; current statutory finality guard rejects the SC/PWD source snapshot. |
| F04 canonical source rows | `NOT_RESOLVED` | Twenty-nine families exist, but multiple material values and identity sources remain non-literal or unavailable. |
| F05 C02 timing | `VERIFIED_RESOLVED` | 155/155 effective times are inside the governing half-open period; zero boundary failures. |
| F06 controlled clock boundary | `VERIFIED_RESOLVED` | Offline values are separated from unauthorized runtime clock and persistence prerequisites. |
| F07 no-activity leakage | `VERIFIED_RESOLVED` | Cases 004/005 contain qualifying activity and do not recreate the excluded no-activity fingerprint. |
| F08 Electronic Journal | `VERIFIED_RESOLVED` | 148 records/transitions, identities, versions, streams, sequences, semantic hashes, predecessors, and integrity hashes reproduce exactly. |

## 7. Five claimed v1.2 resolutions

| Claim | Result | Independent finding |
|---|---|---|
| byte-complete hash grammars | `NOT_RESOLVED` | Published vectors are correct, but non-empty package collection-entry names and complete source-object JSON keys remain open. |
| SP-A current arithmetic compatibility | `NOT_RESOLVED` | Arithmetic is valid; the runtime-shaped statutory facts are rejected by the current finality invariant. |
| complete canonical source rows | `NOT_RESOLVED` | Column inventories do not supply all material values or deterministic sources. |
| offline C02 separation | `VERIFIED_RESOLVED` | Offline artifact construction does not require changing the live clock or current period-end persistence. |
| complete EJ transition/hash chains | `VERIFIED_RESOLVED` | All 148 semantic and integrity chains independently match. |

## 8. Scenario partition and output coverage

Included scenarios are 001, 003, 004, 005, 007, 009, 010, 011, 012, 013, 014, 016, 017, 018, 019, 020, 021, 023, and 024. Excluded scenarios are 002, 006, 008, 015, 022, and 025. Counts are 19 and 6, overlap is zero, and the union is all 25 scenarios. Each included scenario has one `DS-AE1-NNN` case; each excluded row remains `EXCLUDED_NON_EXECUTABLE`.

The output expansion is 17 single-row cases plus two rows for 004 and three for 005, exactly 22 rows. H01-H10 and D01-D32 are present. Cases 004/005 have active documents, ranges, populated fiscal references, nonzero amounts, and D32 `NONE`; no included case recreates excluded no-activity behavior.

## 9. Deterministic identity reconstruction

The review generated names from scenario ownership and the closed v1.1 token/ordinal registry, normalized each name to NFC, encoded it as UTF-8, and applied RFC 4122 UUIDv5 using network-order namespace bytes.

| Measure | Result |
|---|---:|
| generated names | 2,364 |
| unique names | 2,364 |
| unique UUIDs | 2,364 |
| valid UUIDv5 version/variant | 2,364 |
| collisions | 0 |

| Exact preimage | Reconstructed UUIDv5 | Result |
|---|---|---|
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|period\|0001` | `e1f31225-7457-5573-a8f2-1250e7382ee5` | match |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|fiscal-document\|0001` | `192219dd-a7c9-5a53-adb8-ee1342aff771` | match |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-013\|accounting-fact\|0008` | `e0602e8f-8662-5a63-8313-25672d017b8e` | match |

No v1.1 identity changed in v1.2.

## 10. V01-V13 reconstruction

Structured fields were parsed and re-emitted in the declared property/record order using the normative scalar, JSON, LF, trailing-byte, and hash rules. EJ vectors were also reconstructed through the same field expansion used for the full ledger, not by hashing copied preimage text.

| Vector | Family / exact reproducible representation | Bytes | Expected and calculated SHA-256 | Result |
|---|---|---:|---|---|
| V01 | source object: ordered `contract,rowId,role,fields(amountMinorUnits,currency,label)` with NFC `SYN\|SC\nÑ` | 181 | `4483a2623709df3ee631e3f41da6d4d2c76b4fe0c3b85d18a7d0697e2bbc687d` | match |
| V02 | workbook request: exact runtime writer order through one ordered source/fact pair | 1534 | `5b0649f60d07121e7dcc9f4feb34c986427b2683e116174ba6e06d1fef669a23` | match |
| V03 | Accounting fact: exact runtime writer order with explicit nullable members | 572 | `89296aea1c64ab2d101aba6deeb6a31cec322f70e836ccb57b4ddc12fb218435` | match |
| V04 | LF package records from profile through zero evidence count, final LF | 343 | `b747f49fd6904637d6e024b8cbf54b40c5dae3122b305b2419a37725bde4a6d4` | match |
| V05 | 20-line `EJSEM` expansion for case 001/sequence 1 | 1495 | `4c34fdcbb0dcdf10bf932786dc7288718a3f6f5e587ed46c25729e025b3f6e98` | match |
| V06 | 11-line genesis `EJINT`, final LF | 540 | `241a23fc4156b360c7b3dd80156f1f8ccddaf159f9da68aba3a1a881c776cee2` | match |
| V07 | 11-line sequence-2 `EJINT` with V06 predecessor | 540 | `6eb374b788b2a6ce00ad8041fe09d38aa2549ec044b247e14dd212d4c4dee752` | match |
| V08 | compact ordered JSON `values=[a,b]` | 20 | `207571055a4dc87f9c27edd8f935f23fee19c6c3c1e0b96fe16369e7ac32ee89` | match |
| V09 | compact ordered JSON `values=[b,a]` | 20 | `ff26eaa80429b6e71e09658838120e216c1cb5ad60ecae64501ecfc9133fb17c` | match |
| V10 | compact ordered JSON with explicit null `value` | 14 | `1c197daef20de3f47eec5e2f735ec6669869d3180cc29f35be4788511e0af0f8` | match |
| V11 | compact empty JSON object; member omitted | 2 | `44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a` | match |
| V12 | compact ordered JSON with empty `values` array | 13 | `8138e5e9bb95f27b029fa5e119d99c2e9a6b7f44c8a07faada29f295b76297d3` | match |
| V13 | compact ordered JSON `amountMinorUnits=9201,currency=PHP` | 42 | `50ba9c607a626dc88c8449ebaada35ae71d1593af93132c088735458d5c031ef` | match |

The vector bytes do not close the full package grammar. V04 has zero EJ/header/Annex/evidence entries, so it cannot establish their non-empty key names, index widths, pair composition, or framing. The source-object family says fields follow row-family order, but does not define the exact JSON key spelling/casing for every SQL-shaped field.

## 11. SP-A current-source compatibility

Independent integer calculations match the stated line and aggregate arithmetic:

- SC `10000 - 2000 + 1200 = 9200`;
- PWD `10000 - 2000 + 1200 = 9200`;
- coupon `10000 - 1000 + 1200 = 10200`;
- void `10000 + 1200 = 11200`;
- active gross 30000, active net/tenders 28600, VATable 30000, VAT 3600, discounts 5000, void 11200;
- D07 41200, D19 16200, D27 21400, and D29 22100.

Current line validation accepts those four line equations. Current statutory validation does not accept the two statutory snapshots. `FiscalDocumentCreationService.NormalizeAppliedStatutoryFiscalFacts` rejects when `finalPayableAmount + statutoryDiscountAmount > originalAmount`. For SC/PWD, `9200 + 2000 = 11200 > 10000`. The package fixes original amount and basis at 10000, so no documented input can satisfy the current rule. No bypass, source change, or invented alternative amount is authorized. F03 therefore remains blocking.

## 12. Canonical row-family coverage

`COMPLETE` here means the row-field population can be produced without choice, independent of the global package/source-object serialization defect in F02. `BLOCKED` means at least one row-family value, type mapping, transition, request, audit, or family-specific hash input remains discretionary or incompatible. A `COMPLETE` row below is still not authorization to serialize or implement the package while F02 remains open.

| # | Family | Result | Evidence / remaining gap |
|---:|---|---|---|
| 1 | Site | `BLOCKED` | display name and lifecycle timestamps are not literal |
| 2 | Fiscal identity | `BLOCKED` | PTU/accreditation references and lifecycle actor/times remain `exact` |
| 3 | Header profile | `BLOCKED` | profile/template/presentation/location/approval bindings and times are incomplete |
| 4 | Fiscal period | `BLOCKED` | period bounds are exact, but open/close lifecycle values are not fully bound |
| 5 | Fiscal range | `BLOCKED` | child ordering exists; exact created value and complete row hash keys do not |
| 6 | Fiscal document | `BLOCKED` | statutory snapshot incompatibility plus incomplete source/assignment bindings |
| 7 | Document line | `BLOCKED` | description and source reference are described, not literal |
| 8 | Document total | `COMPLETE` | profile amount, parent, code, currency, context, and chronology close the row |
| 9 | Tax detail | `COMPLETE` | profile amount/rate, parent, codes, context, and chronology close the row |
| 10 | Discount detail | `BLOCKED` | approval reference is only `synthetic` and source JSON keys are not closed |
| 11 | Statutory fact | `BLOCKED` | command, parking/session/site/group, tariff, and terminal identities lack deterministic sources; current finality rule fails |
| 12 | Tender | `BLOCKED` | PMS/payment/provider reference null/non-null selection is not closed for every row |
| 13 | Tender breakdown | `COMPLETE` | profile, report parent, classification, count, amount, and chronology are fixed |
| 14 | Discount breakdown | `COMPLETE` | profile and report-parent expansion fixes all values |
| 15 | Status history | `COMPLETE` | commit/void schedule, actors, reason, correlation, and chronology are fixed |
| 16 | Accounting fact | `BLOCKED` | amounts/times are fixed, but all source-reference and canonical row-hash key bindings are not |
| 17 | X Reading | `BLOCKED` | aggregate values exist; complete literal request/scope/report field and semantic-hash payloads do not |
| 18 | Z Reading | `BLOCKED` | same gap as X plus complete close-state transition/value bindings |
| 19 | BIR Sales Summary | `BLOCKED` | amounts exist; complete request/scope/row semantic-hash inputs do not |
| 20 | Annex row | `BLOCKED` | 42 positions are fixed, but source/calculation semantic hashes depend on incomplete source rows |
| 21 | Annex fact link | `BLOCKED` | identity/order are fixed, but linked fact semantic hashes are not all constructible |
| 22 | Electronic Journal record | `COMPLETE` | all 148 ledger rows and hashes independently reproduce |
| 23 | EJ transition | `COMPLETE` | transition identities, refs, versions, source objects, requests, and results are fixed |
| 24 | Workbook definition | `BLOCKED` | artifact SHA-256, length, filename/storage evidence, and generated time require unauthorized generation/runtime evidence |
| 25 | Workbook request | `BLOCKED` | source row semantic hashes are not all constructible |
| 26 | Replay record | `BLOCKED` | original artifact hash/length/bytes are unavailable without workbook generation |
| 27 | Conflict record | `BLOCKED` | original/new complete package semantics depend on incomplete source/package hashes |
| 28 | Recovery record | `BLOCKED` | observed references, hashes, result, cleanup evidence, and exact time are not closed |
| 29 | Audit record | `BLOCKED` | complete action/result code registry, safe reasons, and several source/hash bindings are not literal |

Seven row-field populations are complete and twenty-two are blocked. The seven complete populations still cannot be serialized into an authorized package while F02 remains open. An implementer would have to invent material values for the blocked families; B03/F04 is not resolved.

## 13. Offline/runtime boundary and C02

The role/status matrix correctly separates offline inputs, expected runtime/report comparison rows, and evidence-only rows. Offline implementation would not require database provisioning, fixture loading, a PostgreSQL clock change, runtime clock abstraction, persisted UUID change, API change, application change, migration, or live service.

Current `PostgresAnnexE1Repository` still assigns `effective_at = period.EndAt` and generates persisted UUIDs and clock values at runtime. Version 1.2 labels these as future runtime prerequisites and does not make them prerequisites to constructing offline expectations. This separation resolves the prior authorization-order conflict, but does not authorize runtime work.

C02 reconstruction result:

| Measure | Count |
|---|---:|
| total Accounting facts | 155 |
| before period start | 0 |
| exactly at period end | 0 |
| after period end | 0 |
| other boundary failures | 0 |

## 14. Electronic Journal reconstruction

The ledger was expanded through current `ElectronicJournalCanonicalizer` field order. Site, fiscal identity, period, report/request, document, stream, record, and transition identities were independently derived. Business dates were bound from the governing period, including case 004's period-boundary document. Void events correctly omit the sequence-policy ID.

| Check | Result |
|---|---:|
| records / source transitions | 148 / 148 |
| unique record / transition identities | 148 / 148 |
| streams | 19 |
| missing refs / versions | 0 / 0 |
| sequence or stream failures | 0 |
| semantic-hash mismatches | 0 |
| integrity-hash mismatches | 0 |
| predecessor/genesis failures | 0 |
| facts-byte mismatches | 0 |
| Z / BIR events | 22 / 22 |

Retention is `fiscal_reconstruction_hold`, deletion time is null, privacy is `SYNTHETIC_FISCAL_FACTS`, and correction lineage is null for every row. F08/B05 is resolved.

## 15. R01-R12 independent reconciliation

| Rule | Independent calculation | Difference / result |
|---|---|---|
| R01 | `41200 - 0 - 11200 = 30000` | 0 / pass |
| R02 | `1000 = 0 + 1000 + 0` | 0 / pass |
| R03 | `16200 = 2000 + 2000 + 0 + 0 + 1000 + 0 + 11200` | 0 / pass |
| R04 | `0 = 0 + 0 + 0 + 0 + 0` | 0 / pass |
| R05 | `3600 + 0 = 3600` | 0 / pass |
| R06 | `21400 + 16200 + 3600 = 41200` | 0 / arithmetic pass; source compatibility blocked |
| R07 | `22100 - 500 - 200 = 21400` | 0 / pass |
| R08 | copied operands against source IDs/hashes | row difference 0; blocked by incomplete source hashes and SP-A source rejection |
| R09 | Site, fiscal identity, PHP, period identities | 0 mismatches |
| R10 | one Z and BIR journal event per period | 22/22 and 22/22; 0 difference |
| R11 | `9200 + 9200 + 10200 = 28600` | 0 / arithmetic pass; statutory sources blocked |
| R12 | `100000 + 28600 = 128600` | 0 / arithmetic pass; statutory sources blocked |

B12/D12 equations and cumulative GTA rows also reproduce with zero monetary or row-count difference.

## 16. Source counts and v1.1 deltas

| Family | v1.1 | v1.2 reconstructed | Delta |
|---|---:|---:|---:|
| Sites / fiscal identities / headers / workbooks | 19 each | 19 each | 0 |
| periods / X / Z / BIR / Annex rows | 22 each | 22 each | 0 |
| documents / lines / totals / tax | 67 each | 67 each | 0 |
| tenders | 69 | 69 | 0 |
| discounts | 45 | 45 | 0 |
| statutory facts | 30 | 30 | 0 |
| histories | 82 | 82 | 0 |
| Accounting facts | 155 | 155 | 0 |
| EJ records / transitions | 148 / 148 | 148 / 148 | 0 / 0 |
| fiscal ranges | 44 | 44 | 0 |
| tender / discount breakdowns | 48 / 90 | 48 / 90 | 0 / 0 |
| Annex fact links | 154 | 154 | 0 |
| requests / audits | 346 / 186 | 346 / 186 | 0 / 0 |
| replay / conflict / recovery | 2 each | 2 each | 0 |

## 17. External decisions

All ten required entries remain present and exactly `UNRESOLVED`: AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-012, AE-DR-016, AE-DR-016B, AE-DR-019, AE-DR-020A, and AE-DR-024. None is silently resolved, waived, or assumed. They continue to block excluded scenarios, official presentation/acceptance, signing/encryption, destructive retention, delivery, submission, and Controlled UAT. The included offline values that depend on provisional presentation rules are fixed as internal expected bytes, so these external decisions do not cause the three implementation blockers above and do not authorize external use.

## 18. Privacy, security, and artifact posture

The package uses synthetic people, identities, vehicle-like references, addresses, contacts, fiscal activity, and transactions. Scans found no credential, token, password, private key, certificate, connection string, external endpoint, real statutory evidence, production payload, or executable artifact. Offline reconstruction requires no database or network service. No generated dataset, binary, workbook, validator, migration, or executable payload was introduced by v1.2 or this review.

## 19. Activity decision table

| Activity | Status | Reason / prerequisite |
|---|---|---|
| v1.2 specification acceptance | `BLOCKED` | F02, F03, and F04 remain material |
| included-scenario machine-readable dataset implementation | `BLOCKED` | complete deterministic rows/hashes are unavailable |
| excluded-scenario dataset implementation | `BLOCKED` | six scenarios remain externally blocked/non-executable |
| offline validator implementation | `BLOCKED` | complete expected package/source hashes are unavailable |
| runtime clock changes | `BLOCKED` | separate runtime authorization required |
| application changes | `BLOCKED` | outside this review |
| API changes | `BLOCKED` | outside this review |
| database changes | `BLOCKED` | outside this review |
| migrations | `BLOCKED` | outside this review |
| environment provisioning | `BLOCKED` | no provisioning authority |
| role assignment | `BLOCKED` | no assignment authority |
| dataset loading | `BLOCKED` | dataset absent; loading unauthorized |
| scenario-subset approval | `BLOCKED` | no complete independently governed subset is defined |
| Controlled UAT execution | `BLOCKED` | dataset, environment, role, and external gates remain |
| evidence acceptance | `BLOCKED` | no authorized execution evidence exists |
| internal workbook generation | `BLOCKED` | generation is outside offline dataset authority |
| external delivery | `BLOCKED` | external decisions and delivery authority unresolved |
| BIR submission | `BLOCKED` | no submission authority |
| Production | `BLOCKED` | explicitly prohibited |
| Annex E-2 through E-5 | `BLOCKED` | out of scope |
| ARTS POSLog | `BLOCKED` | deferred and unauthorized |
| signing | `BLOCKED` | AE-DR-016 unresolved |
| encryption | `BLOCKED` | AE-DR-016 unresolved |
| destructive retention or purge | `BLOCKED` | AE-DR-016B and records authority unresolved |

## 20. Validation limits and residual risks

The focused .NET test assembly could not run with `--no-restore` because this fresh worktree had no NuGet assets file. No restore was attempted because contacting an external package service is outside this review. Source control-flow inspection and independent reconstruction were sufficient to establish the blocking statutory invariant and to reproduce current hash behavior.

The published V01-V13 byte strings hash correctly, but that alone does not validate a full 19-case package. Journal chains are closed; other source/package families are not. A later correction must not solve the issue by adding runtime code, database objects, environment assumptions, random values, or generated-time defaults.

## 21. Canonical outcome and next bounded activity

Task decision: `READY_FOR_REVIEW`.

Canonical authorization outcome: `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION`.

This review does not authorize dataset or validator implementation, excluded scenarios, runtime/application/API/database/configuration changes, migrations, provisioning, role assignment, loading, Controlled UAT, evidence acceptance, workbook generation, external delivery, BIR submission, Production, Annex E-2 through E-5, ARTS POSLog, signing, encryption, or destructive retention/purge.

The next bounded activity is a new-version, documentation-only correction package that:

1. defines exact non-empty package-entry keys and source-row JSON keys for every family;
2. supplies a current-runtime-valid statutory source snapshot or explicitly removes SP-A from the authorized offline subset without inference;
3. replaces every unresolved material `exact`/`synthetic` value with a literal or deterministic grammar and adds full non-empty package/source vectors;
4. receives another independent authorization review before any dataset or validator implementation.
