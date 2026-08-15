# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.2

## 1. Document control

| Item | Value |
|---|---|
| Specification ID | `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.2` |
| Status | `READY_FOR_V1_2_SPECIFICATION_AUTHORIZATION_REVIEW` |
| Repository baseline | `f4a3a262f0e35bd91d58197fbf59aaeeda911c18` |
| Accounting profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Current implementation gate | `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` |
| Monetary unit | PHP integer minor units |
| Dataset implementation | Not authorized |
| Validator implementation | Not authorized |

The v1.0 and v1.1 packages remain immutable historical reviewed records. Version 1.2 supersedes them only for future implementation consideration and does not alter either earlier authorization review. Machine-readable dataset and validator implementation remain blocked until a separate v1.2 authorization review is merged.

## 2. Purpose and boundary

This package specifies deterministic offline artifacts for the same 19 future-eligible scenarios. It does not create those artifacts and does not authorize runtime or API changes, persisted-identity changes, controlled-clock implementation, database or migration work, environment provisioning, loading, scenario execution, workbook generation, evidence acceptance, delivery, BIR submission, or Production.

The future offline artifact is not a database seed or load script. Rows classified `INPUT_SOURCE` are proposed offline inputs. Rows classified `EXPECTED_RUNTIME_RESULT`, `EXPECTED_REPORT_RESULT`, and `EVIDENCE_ONLY` are comparison expectations and are never direct database inputs. Current runtime incompatibilities are labelled `FUTURE_RUNTIME_PREREQUISITE`; they do not prevent constructing an offline representation and remain unauthorized for implementation.

## 3. Scenario partition

Included: `AE1-UAT-001`, `AE1-UAT-003`, `AE1-UAT-004`, `AE1-UAT-005`, `AE1-UAT-007`, `AE1-UAT-009`, `AE1-UAT-010`, `AE1-UAT-011`, `AE1-UAT-012`, `AE1-UAT-013`, `AE1-UAT-014`, `AE1-UAT-016`, `AE1-UAT-017`, `AE1-UAT-018`, `AE1-UAT-019`, `AE1-UAT-020`, `AE1-UAT-021`, `AE1-UAT-023`, `AE1-UAT-024`.

Excluded and non-executable: `AE1-UAT-002`, `AE1-UAT-006`, `AE1-UAT-008`, `AE1-UAT-015`, `AE1-UAT-022`, `AE1-UAT-025`.

The sets are disjoint and their union is the complete 25-scenario catalogue. Every included scenario owns one `DS-AE1-NNN` case. No included population has the excluded no-activity fingerprint. The expected output inventory is exactly 22 rows: 17 single-row cases, two rows for 004, and three rows for 005.

## 4. Identity contract

Version 1.2 preserves identity contract `annex-e1-synthetic-uat-identity:v1.1`, namespace `ae1d5e7a-7e2d-5c4d-9a11-202608140001`, and grammar:

```text
annex-e1-synthetic-uat:v1.1|AE1-UAT-NNN|object-type|OOOO
```

UUIDv5 uses RFC 4122 network-order namespace bytes and UTF-8 NFC names. Case is significant. Leading/trailing whitespace, CR, LF, NUL, vertical bars inside segments, empty segments, ordinal zero, and unregistered object tokens are invalid. Scenario width is three and ordinal width is four. The complete v1.1 registry remains normative and contains 2,364 names; v1.2 changes no canonical name or UUID.

The deterministic offline code-reference namespace is separate and does not create catalog rows:

```text
annex-e1-synthetic-catalog:v1|<controlled-code-set>|<code-key>
```

It uses the same namespace UUID and UUIDv5 algorithm. These IDs are `OFFLINE_ARTIFACT_ONLY`. Future runtime validation must resolve the active repository set/key and compare its semantic meaning; it must not insert or force the offline UUID into a database.

Preserved vectors:

| Canonical name | UUIDv5 |
|---|---|
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|period\|0001` | `e1f31225-7457-5573-a8f2-1250e7382ee5` |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-001\|fiscal-document\|0001` | `192219dd-a7c9-5a53-adb8-ee1342aff771` |
| `annex-e1-synthetic-uat:v1.1\|AE1-UAT-013\|accounting-fact\|0008` | `e0602e8f-8662-5a63-8313-25672d017b8e` |

## 5. Byte-complete hash contracts

The normative hash-family contracts and vectors are in the [Canonical Source Row and Hash Contract](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.2.md). It defines package content, source-object, fiscal-document request, workbook request, Accounting fact, Electronic Journal semantic, Electronic Journal integrity, replay, and conflict hashes.

Every SHA-256 digest is lowercase hexadecimal. Every normative preimage is UTF-8 without BOM. Runtime JSON profiles preserve the exact `.NET Utf8JsonWriter` property sequence stated in that contract. Offline JSON profile `annex-e1-offline-json:sha256:v1.2` defines NFC strings, no insignificant whitespace, mandatory property order, lowercase `\u00xx` escapes for U+0000-U+001F, literal UTF-8 for other Unicode, base-10 integers, `true`/`false`, explicit `null`, and ordered arrays. Missing, null, empty string, empty object, and empty array are distinct.

## 6. Chronology and time roles

Default period 1 is `[2026-09-09T16:00:00.0000000Z, 2026-09-10T16:00:00.0000000Z)`, business date `2026-09-10`. Later periods are adjacent 24-hour half-open windows. Documents are assigned at period start plus 9 hours plus their zero-based global document ordinal in seconds; case 004 period-2 document is exactly at the second period start. X commit is period end minus 29 seconds, Z commit is period end plus 11 seconds, and BIR commit is period end plus 13 seconds. Journal `recorded_at` is its event effective time plus 100 milliseconds in the offline expectation.

Accounting-fact effective time is period end minus 10 seconds plus its within-period fact ordinal in milliseconds. Recorded time is period end plus 14 seconds plus the global fact ordinal in milliseconds. Therefore all 155 facts satisfy:

```text
period_start_at <= effective_at < period_end_at
```

No fact uses the exclusive end.

| Timestamp class | Artifact role | Current support | Comparison | Hash posture |
|---|---|---|---|---|
| business date, source effective time, period bounds | `INPUT_SOURCE` | `SUPPORTED_CURRENT_RUNTIME` | `EXACT_OFFLINE` | included where current contract says included |
| H09 generation time | `EXPECTED_REPORT_RESULT` | `FUTURE_RUNTIME_PREREQUISITE` | `EXACT_AFTER_CLOCK_CONTROL` | excluded from workbook semantic hash |
| database-recorded and audit time | `EXPECTED_RUNTIME_RESULT` | `FUTURE_RUNTIME_PREREQUISITE` | `EXACT_AFTER_CLOCK_CONTROL` | integrity hash only where specified |
| deterministic persisted UUID | `EXPECTED_RUNTIME_RESULT` | `FUTURE_RUNTIME_PREREQUISITE` | `STRUCTURAL_ONLY` before that prerequisite | excluded or included exactly per family |
| offline artifact creation time | `EVIDENCE_ONLY` | `OFFLINE_ARTIFACT_ONLY` | `EXCLUDED_FROM_HASH` | excluded |

The future clock contract remains `pos-server-controlled-test-clock:v1`: isolated non-Production environment only, fixed case schedule, startup acknowledgement, fail-closed configuration validation, serial consumption except the concurrency case, transaction-local PostgreSQL clock boundary, replay reuse, override reset, evidence capture, and cleanup proof. Current support is absent. This package does not implement it and offline artifact construction does not depend on it.

## 7. Correct monetary profiles

All values are signed checked 64-bit PHP minor units.

| Profile | Fiscal documents | Runtime-valid line equation | Active report aggregate |
|---|---|---|---|
| `SP-A12` | SC, PWD, coupon, same-period void | SC/PWD `10000-2000+1200=9200`; coupon `10000-1000+1200=10200`; void `10000-0+1200=11200` | gross 30000; net/tenders 28600; VATable 30000; VAT 3600; discount 5000; void 11200 |
| `SP-B12` | one ordinary SI | `10000-0+1200=11200` | gross 10000; net/tender 11200; VATable 10000; VAT 1200 |
| `SP-D12` | one ordinary SI with two tenders | `20000-0+2400=22400` | gross 20000; net/tenders 22400; VATable 20000; VAT 2400 |

For SP-A12, SC discount is 2000, PWD discount is 2000, coupon is 1000, other statutory is 0, VAT privilege is 0, and void magnitude is 11200. Each statutory document has one compatible immutable snapshot. Coupon is commercial and never statutory. The void contributes to D07 and D18 but not active gross, active tenders, or GTA.

## 8. Exact Annex values

The [Expected Value Matrix](ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.2.md) closes H01-H10 and D01-D32 for all 22 rows. SP-A12 has D07 41200, D16 1000, D19 16200, D26 3600, D27 21400, D28 200, and D29 22100. SP-B12 has D07 10000 and D27/D29 8800. SP-D12 has D07 20000 and D27/D29 17600. D29 remains exactly `D27 + D06 + D28`.

## 9. Accounting facts

Every period owns these seven fact types in order: `manual_si_or_net_income`, `sales_overrun_overflow_net_income`, `naac_discount`, `solo_parent_discount`, `other_vat_adjustment`, `vat_on_returns`, and `residual_vat_adjustment`.

SP-A12 records Manual 500 and overflow 200; other amounts are immutable `attested_zero`. SP-B12/SP-D12 use `attested_zero` for all seven. Case 013 owns correction fact 0008: Manual 600, `source_correction`, superseding fact 0001, without a correction workbook. Facts are offline input specifications. The current repository effective-time behavior remains a separately blocked runtime-compatibility issue; offline construction does not require changing it.

## 10. Reconciliations

Tolerance is 0 minor units and 0 rows.

```text
R01 D07 - D17 - D18 = ACTIVE_GROSS
R02 D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
R03 D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
R04 D25 = D20 + D21 + D22 + D23 + D24
R05 D26 + D22 = D09
R06 D27 + D19 + D09 = D07
R07 D29 - D06 - D28 = D27
R08 every copied operand equals its exact committed BIR, Z, or Accounting source expectation
R09 Site POS Server, fiscal identity, PHP, and period identities agree
R10 each expected Z/BIR transition has one exact integrity-valid journal event
R11 active tender total = BIR net_sales_amount_minor_units
R12 resulting GTA = previous GTA + BIR net_sales_amount_minor_units
```

SP-A12 R01-R07 are 30000, 1000, 16200, 0, 3600, 41200, and 21400; R11 is 28600; R12 is 128600. SP-B12 results are 10000, 0, 0, 0, 1200, 10000, 8800, 11200, and prior+11200. SP-D12 results are 20000, 0, 0, 0, 2400, 20000, 17600, 22400, and prior+22400.

## 11. Source counts

Version 1.2 retains: 19 Sites, 19 fiscal identities, 19 headers, 19 workbook definitions, 22 periods, 22 X, 22 Z, 22 BIR, 22 Annex rows, 67 documents/lines/totals/tax details, 69 tenders, 45 discounts, 30 statutory facts, 82 status histories, 155 Accounting facts, 148 journal records/transitions, 44 ranges, 48 tender breakdowns, 90 discount breakdowns, 154 Annex fact links, 346 requests, 186 audits, and 2 each replay/conflict/recovery. Arithmetic and hash corrections change values and digests, not row cardinality.

## 12. External decisions, privacy, and change control

All ten confirmations remain `UNRESOLVED`: AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-012, AE-DR-016, AE-DR-016B, AE-DR-019, AE-DR-020A, and AE-DR-024. The six excluded scenarios remain non-executable.

All labels and identities are synthetic. No customer, vehicle, payment-account, taxpayer, operator, employee, credential, statutory-ID image, evidence image, raw request/response, connection string, stack trace, filesystem path, secret, or external endpoint is required. Future artifacts and validators must run offline.

A change to any identity, row, value, hash grammar, time, scenario, formula, or decision cross-reference requires a new version and manifest. The exact next bounded activity is **Review Annex E-1 Synthetic UAT Dataset Specification v1.2**.
