# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.7

## 1. Document control

| Item | Value |
|---|---|
| Specification ID | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.7` |
| Status | `READY_FOR_REVIEW` |
| Repository baseline | `e53a6859899e5a7d63c821b67834c469e5b5e6a8` |
| Identity contract | `annex-e1-synthetic-uat-identity:v1.1` |
| Accounting profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Monetary unit | PHP integer minor units |
| Canonical implementation authority | v1.5 authorization, merged v1.6 implementation, and the operator-approved v1.7 correction-reason compatibility decision |
| Dataset or validator implementation | Exact offline v1.7 artifacts implemented in this branch |

Version 1.7 is the minimum prospective correction to the merged v1.6 package. It changes only `F16|013|0008.correction_reason` from the unsupported `SYNTHETIC_CORRECTION` value to the existing canonical `source_correction` code and regenerates its deterministic descendants. Versions 1.0 through 1.6 remain immutable historical records. The same explicitly authorized task implements the corrected machine-readable dataset, offline validator, and isolated execution harness; no additional specification authorization review is required.

## 2. Purpose and boundary

This package is the minimum prospective correction to v1.6. Scenario 013 Accounting fact 0008 keeps UUID `e0602e8f-8662-5a63-8313-25672d017b8e`, amount 600, and superseded fact `96cc7392-4dc3-550c-b7e0-8521f77d86ef`, but now uses canonical correction reason `source_correction`. The correction reason resolves through the repository-governed `annex_e1_correction_reason` set; no alias, translation, runtime change, or reference-data change is introduced. The same bounded task creates the exact v1.7 machine-readable dataset, offline validator, and invocation-owned isolated execution harness. Shared database loading, UAT, workbook generation, evidence acceptance, delivery, BIR submission, and Production work remain unauthorized.

The complete construction authority is the ordered combination of:

1. this specification;
2. the v1.7 scenario mapping and expected-value matrix;
3. the v1.7 source-population matrix;
4. the v1.7 canonical row population;
5. the v1.7 canonical source-row and hash contract;
6. the v1.2 148-row Electronic Journal ledger incorporated by exact hash in the v1.7 hash contract;
7. the v1.7 AE1H family schemas, deterministic instance ranges, and single authorization-critical 1,690-row semantic-preimage registry in the canonical row population.

No environment value, current time, random value, loader default, database-generated value, external decision, or operator choice participates in the bounded offline bytes.

## 3. Scenario partition

Included scenarios are `001, 003, 004, 005, 007, 009, 010, 011, 012, 013, 014, 016, 017, 018, 019, 020, 021, 023, 024`. Excluded scenarios are `002, 006, 008, 015, 022, 025`. The sets are disjoint, their union is 1 through 25, and every included scenario owns exactly one `DS-AE1-NNN` case. Excluded scenarios remain non-executable and are not recreated indirectly.

The expected Annex output contains exactly 22 rows: 15 A13 rows, one B13 start row, two boundary rows, three month rows, and one D13 row. H01-H10 and D01-D32 are complete in the expected-value matrix.

## 4. Preserved identity contract

Namespace UUID is `ae1d5e7a-7e2d-5c4d-9a11-202608140001`. The exact identity-name grammar remains:

```text
annex-e1-synthetic-uat:v1.1|AE1-UAT-NNN|object-type|OOOO
```

UUIDv5 uses RFC 4122 network-order namespace bytes and UTF-8 NFC name bytes. Scenario width is three and ordinal width is four. The registry remains 2,364 names and 2,364 unique UUIDv5 values. Version 1.7 changes no identity name or UUID.

Required upstream references that are not package-owned rows use a separate deterministic reference grammar and are excluded from the 2,364 object registry: `annex-e1-synthetic-reference:v1|AE1-UAT-NNN|reference-type|OOOO`. It uses the same namespace and UUIDv5 algorithm. Reference types are exactly `statutory-decision-command`, `statutory-request`, `statutory-application-command`, `statutory-validation`, `parking-session`, `site-group`, `policy-reference`, `original-tariff-snapshot`, `applied-tariff-snapshot`, `fiscal-document-status`, and `fiscal-document-type`. These values are offline opaque references, not database identities, controlled-code identities, or rows.

Preserved vectors:

| Object | Canonical name | UUIDv5 |
|---|---|---|
| period | `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|period\|0001` | `e1f31225-7457-5573-a8f2-1250e7382ee5` |
| fiscal document | `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|fiscal-document\|0001` | `192219dd-a7c9-5a53-adb8-ee1342aff771` |
| Accounting fact | `annex-e1-synthetic-uat:v1.1\|AE1-UAT-013\|accounting-fact\|0008` | `e0602e8f-8662-5a63-8313-25672d017b8e` |

## 5. Hash posture

The v1.7 hash contract preserves AE1H framing and defines exact binary framing for offline source rows, evidence rows, workbook content, case packages, and the specification package root. Runtime-shaped fiscal-document, workbook-request, Accounting-fact, Electronic Journal semantic, and Electronic Journal integrity hashes continue to use their current source canonicalizers. Every family identifies included and excluded fields, ordering, null/omission behavior, predecessor behavior, and digest output.

The v1.7 manifest excludes itself. It hashes exact repository bytes for the other eight v1.7 members and frames their descriptors under `annex-e1-specification-package-root:sha256:v1.7`. The hash contract fixes path encoding, descriptor member order, integer endianness, binary digest representation, member ordering, and the complete root preimage. The manifest publishes the final byte lengths, member digests, exact root-preimage Base64, root-preimage byte length, and final root digest.

## 6. Controlled chronology

Period 1 is `[2026-09-09T16:00:00.0000000Z, 2026-09-10T16:00:00.0000000Z)` with business date `2026-09-10`. Later periods are adjacent 24-hour windows. Document effective time is period start plus 9 hours plus the zero-based global document ordinal in seconds; case 004 period 2 starts exactly at its second-period lower boundary. X commit is end minus 29 seconds, Z commit end plus 11 seconds, and BIR commit end plus 13 seconds.

Accounting-fact effective time is period end minus 10 seconds plus within-period fact ordinal milliseconds. Recorded time is period end plus 14 seconds plus global fact ordinal milliseconds. All 155 facts satisfy `period_start_at <= effective_at < period_end_at`; none is before start or at/after end.

All other exact created, updated, requested, approved, transitioned, generated, detected, observed, and audited times are derived by the schedules in the canonical row population. Runtime clock work remains blocked and is not needed to construct offline bytes.

## 7. SP-A13 current-runtime correction

Current `FiscalDocumentCreationService.NormalizeAppliedStatutoryFiscalFacts` requires final payable to match payable basis, tender, and total; VAT to match tax; statutory discount not to exceed discount plus VAT privilege; and `finalPayableAmount + statutoryDiscountAmount <= originalAmount`.

The same service normalizes `CentralPmsParkingSessionRef` and compares it with `ParkingSessionId.Value.ToString("D")` using `StringComparison.OrdinalIgnoreCase`. Version 1.5 therefore serializes deterministic `REF(parking-session,o)` in lowercase RFC 4122 `D` form in both fields. For case 001 document 0001, both are exactly `eb706d21-9161-5083-a4a2-6f703b62845a`; `SYN-AE1-PARKING-001-0001` is forbidden for this field. The rule applies to all 30 statutory snapshots and their 30 fiscal-document commands. Author-side comparison: 30 checked, 30 equal, 0 mismatches.

For each Senior Citizen and PWD document, v1.7 uses the tax-inclusive pre-discount amount as the statutory snapshot original amount:

```text
original amount                 11200
VAT-exclusive basis             10000
VAT                              1200
statutory discount               2000
final payable                    9200
final payable + discount        11200
```

This agrees with the authoritative applied-statutory-facts contract: original amount is the Central PMS original gross tariff/payable amount before statutory application, while VAT-exclusive basis plus VAT reconciles to that gross model. Original and applied tariff snapshot IDs remain distinct. One SC snapshot and one PWD snapshot are attached to separate documents. No document carries conflicting entitlement snapshots.

Coupon document: original 11200, VAT-exclusive base 10000, VAT 1200, commercial coupon 1000, final 10200. It has no statutory snapshot. Void document: original and final pre-void amount 11200, base 10000, VAT 1200, discount 0; it is first recorded and then voided. Other-statutory remains zero and the supported coupon path supplies D16.

## 8. Correct monetary profiles

| Profile | Documents | Active gross | Active net/tender | VATable | VAT | Discounts | Void |
|---|---|---:|---:|---:|---:|---:|---:|
| `SP-A13` | SC, PWD, coupon, void | 30000 | 28600 | 30000 | 3600 | SC 2000; PWD 2000; coupon 1000 | 11200 |
| `SP-B13` | one ordinary SI | 10000 | 11200 | 10000 | 1200 | 0 | 0 |
| `SP-D13` | one ordinary SI, two tenders | 20000 | 22400 | 20000 | 2400 | 0 | 0 |

SP-A13 Annex values are D07 41200, D19 16200, D27 21400, and D29 22100. For each of the fifteen affected rows, R01 is `41200 - 0 - 11200 = 30000` and R06 is `21400 + 16200 + 3600 = 41200`. Version 1.6 corrected and committed these values and their deterministic descendants. Version 1.7 preserves those bytes and changes only the scenario 013 correction-fact semantic row, its audit evidence row, the v1.7 case-package envelopes, governed member hashes, and package root. The committed scenario 013 workbook contains the original seven facts and therefore is not a descendant of correction fact 0008.

## 9. Acyclic artifact-hash graph

The v1.7 graph is directed from a digest to the bytes it hashes. Its topological order is: fixed literals and UUIDs; canonical source rows; source-row semantic hashes; workbook canonical content bytes; workbook content hash; workbook row semantic hash; remaining evidence-row hashes; case-package digest; specification-member file digests; specification package root. A workbook row contains `workbook_content_sha256`, never a case-package digest. A case-package descriptor contains the completed workbook-row semantic hash. No child row contains an ancestor digest and every self-hash field is excluded from its own preimage. Cycle detection over the declared node and edge registry returns zero cycles.

The exact graph, exclusions, node framing, and worked case `DS-AE1-001` are normative in the v1.7 hash contract. The machine-readable dataset includes all nineteen completed case-package preimages and digests; the offline validator independently reconstructs and compares them with Appendix B of the canonical population.

## 10. Reconciliation rules

R01 through R12 remain the v1.2 equations. The v1.7 source population supplies exact row identities and hashes for R08 and current-runtime-valid statutory inputs for R11/R12. Tolerance is zero monetary units and zero rows.

| Rule | A13 result | B13 result | D13 result |
|---|---:|---:|---:|
| R01 | 30000 | 10000 | 20000 |
| R02 | 1000 | 0 | 0 |
| R03 | 16200 | 0 | 0 |
| R04 | 0 | 0 | 0 |
| R05 | 3600 | 1200 | 2400 |
| R06 | 41200 | 10000 | 20000 |
| R07 | 21400 | 8800 | 17600 |
| R08 | exact IDs and hashes | exact IDs and hashes | exact IDs and hashes |
| R09 | exact scope/currency/period | same | same |
| R10 | one Z and BIR event/period | same | same |
| R11 | 28600 | 11200 | 22400 |
| R12 | 100000 + 28600 = 128600 | prior + 11200 | prior + 22400 |

## 11. Governed population and journal

Counts remain unchanged from v1.2: 19 Sites, identities, headers, and workbooks; 22 periods, X, Z, BIR, and Annex rows; 67 documents, lines, totals, and tax rows; 69 tenders; 45 discounts; 30 statutory facts; 82 histories; 155 Accounting facts; 148 EJ records and transitions; 44 ranges; 48 tender breakdowns; 90 discount breakdowns; 154 fact links; 346 requests; 186 audits; and two each replay, conflict, and recovery.

The validated v1.2 Electronic Journal ledger is incorporated without identity or byte changes because its document facts include payable, VAT, discount, entitlement, and benefit, but neither statutory `originalAmount` nor the Central PMS parking-session reference. It remains 148 records, 148 transitions, 19 streams, 22 Z events, 22 BIR events, and zero semantic, integrity, sequence, predecessor, reference, or version failures.

The 29 family schemas expand to exactly 1,690 instances. Population Appendix A is the single definitive authorization-critical semantic registry. It publishes, for every instance, the exact AE1H family token, stable schema version, identity, scenario and ordinal, hash purpose, role, ordered member schema, ordered source/runtime value tuple, complete semantic preimage bytes, byte length, digest, and provenance. It contains 840 preserved v1.4 rows, 148 incorporated F22 Electronic Journal rows, and 702 newly corrected F01/F07-F10/F12-F15/F23 rows. The redundant v1.4 informational binding-digest registry, its preimage lengths, and its commitment are removed. Independent Windows PowerShell 5.1 reconstruction from names, types, and values produced 1,690/1,690 byte-for-byte and digest matches with zero blocked instances.

## 12. External decisions and authorization

AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-012, AE-DR-016, AE-DR-016B, AE-DR-019, AE-DR-020A, and AE-DR-024 remain `UNRESOLVED`. They do not select any byte in the bounded 19-scenario offline dataset. They continue to block their mapped external, excluded-scenario, delivery, signing, retention, workbook-presentation, or Controlled UAT activities.

All identities, people-like labels, references, and transactions are synthetic. No secret, credential, token, key, certificate, real personal data, live database, network service, or external authority is required.

Package and implementation readiness is `READY_FOR_REVIEW`. The v1.6 SP-A correction remains preserved, and `V16-CR-01` is resolved by exact canonical-code compatibility. The corrected v1.7 JSONL dataset, deterministic offline validator, and invocation-owned isolated execution harness are part of the same bounded implementation. Runtime changes, shared database loading, Controlled UAT, workbook generation, delivery, BIR submission, and Production remain unauthorized.
