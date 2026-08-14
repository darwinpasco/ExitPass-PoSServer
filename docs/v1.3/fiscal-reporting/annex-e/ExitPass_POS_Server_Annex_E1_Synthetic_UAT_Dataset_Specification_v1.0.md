# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification v1.0

## 1. Control record

| Item | Value |
|---|---|
| Specification ID | `annex-e1-synthetic-uat-dataset:v1.0` |
| Status | `READY_FOR_DATASET_SPECIFICATION_REVIEW` |
| Repository baseline | `22aad789ec52d10ef72124687cdcdaec2d563436` |
| Annex E profile | `pos-server-bir-annex-e1-rmo24-2023:v1` |
| Accounting profile | `pos-server-annex-e1-accounting-calculation:v1` |
| Accounting profile SHA-256 | `36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f` |
| Specification authority | Controlled UAT authorization review: `AUTHORIZED_FOR_NEXT_BOUNDED_PREPARATION` |
| Approval status | `PENDING_SPECIFICATION_REVIEW` |
| Implementation / loading / execution | `NOT_AUTHORIZED` / `NOT_AUTHORIZED` / `NOT_AUTHORIZED` |

This specification fixes synthetic inputs and expected results for the 19 future-eligible scenarios. It creates no fixture, database row, environment, workbook, or execution evidence.

## 2. Authoritative baseline

The source order is the immutable Accounting Calculation Profile, `Z-012B-ACCOUNTING-APPROVAL-001`, the Annex E field/layout package, the merged Z-012B runtime, Z-010 BIR Sales Summary, Z-011A Electronic Journal, Z-012D preparation documents, and the merged Controlled UAT authorization review. The Electronic Journal corroborates transition identity and chronology; it never supplies Annex E financial amounts.

Included scenarios are `AE1-UAT-001`, `003`, `004`, `005`, `007`, `009`, `010`, `011`, `012`, `013`, `014`, `016`, `017`, `018`, `019`, `020`, `021`, `023`, and `024`. Excluded scenarios are `AE1-UAT-002`, `006`, `008`, `015`, `022`, and `025`. No scenario is executable under this specification.

## 3. Deterministic identity profile

The namespace UUID is `ae1d5e7a-7e2d-5c4d-9a11-202608140001`. Every planned UUID is RFC 4122 UUIDv5 over UTF-8 bytes of:

```text
annex-e1-synthetic-uat:v1.0|<scenario-id>|<object-type>|<ordinal-four-digits>
```

Normalization is exact: lower-case object type; upper-case scenario ID; ASCII vertical bars; no leading/trailing whitespace; NFC text; no locale transformations. Ordinals start at `0001`. The shared Site, fiscal identity, header profile, contract version, sequence policy, and service principal use scenario ID `COMMON`. A conforming implementation must reject a generated UUID collision with any pre-existing row and must never substitute random UUIDs.

Human-readable references use `SYN-AE1-V1-<SCENARIO-NUMBER>-<TYPE>-<ORDINAL>`, upper-case ASCII, zero-padded three/four digit ordinals, and no spaces. Reserved prefixes are `SYN-AE1-V1-`, `SYN-AE1-SI-`, `SYN-AE1-Z-`, `SYN-AE1-BIR-`, and `SYN-AE1-EJ-`; they may be used only in an invocation-owned isolated synthetic database.

Common synthetic values are:

| Category | Exact value | Privacy class |
|---|---|---|
| Site POS Server code / H08 | `SYN-AE1-SPS-01` | Synthetic operational identifier |
| Fiscal identity code | `SYN-AE1-FI-01` | Synthetic fiscal identifier |
| Taxpayer name / H01 | `SYNTHETIC ANNEX E1 PARKING SERVICES INC.` | Synthetic organization |
| Address / H02 | `100 SYNTHETIC AVENUE, TEST CITY 0000` | Synthetic address |
| TIN / H03 | `000-000-000-000` | Reserved synthetic value; never a taxpayer identity |
| POS serial / H06 | `SYN-AE1-SERIAL-01` | Synthetic fiscal identifier |
| MIN / H07 | `SYN-AE1-MIN-01` | Synthetic fiscal identifier |
| Actor / H10 | `svc-syn-annex-e1-uat-v1` | Pseudonymous service reference |
| Currency | `PHP` | Non-personal |
| Time zone / cutoff | `Asia/Manila` / `00:00:00` | Non-personal |
| Approval reference | `SYN-AE1-SPEC-REVIEW-V1` | Synthetic governance reference |

No value may be replaced with a Production-like MIN, PTU, serial, TIN, Site ID, plate, customer name, statutory ID, credential, or external endpoint.

## 4. Canonicalization and hashing

Dataset semantic input is canonical UTF-8 text with one `key=value` line per field, keys sorted by ordinal code-point order, LF line endings, no BOM, invariant decimal integers, UTC timestamps in `yyyy-MM-ddTHH:mm:ss.fffffffZ`, `null` spelled exactly `null`, booleans lower-case, and arrays sorted by the declared sequence then joined with comma. The dataset hash profile is `annex-e1-synthetic-uat-dataset-semantic:sha256:v1`; SHA-256 is lower-case hexadecimal over those bytes.

The later implementation must preserve the runtime's governed hashes: `pos-server-annex-e1-period-fact:sha256:v1`, `pos-server-annex-e1-request:sha256:v1`, renderer `pos-server-annex-e1-openxml-renderer:v1`, and template hash `7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197`. This document does not precompute runtime hashes because executable rows do not yet exist; it fixes every semantic input those hashes must cover.

## 5. Time, sequence, and isolation model

Each case runs only in a fresh invocation-owned database restored to the same approved baseline. Cases never share mutable rows. Unless a case overrides it, the fiscal period is business date `2026-09-10`, start `2026-09-09T16:00:00.0000000Z`, end `2026-09-10T16:00:00.0000000Z`, status `closed`, period sequence `1`, reset counter `0`, Z counter `1`, Z generated/committed at `2026-09-10T16:00:10Z` / `2026-09-10T16:00:11Z`, BIR Summary generated/committed at `16:00:12Z` / `16:00:13Z`, and Annex generation time `2026-09-30T10:00:00Z` (`2026-09-30 10:00:00 UTC` in H09).

Fiscal documents are posted in fiscal-sequence order at `01:00:00Z` plus one second per ordinal. X is committed five seconds before Z. Electronic Journal sequences begin at 1 and follow source commit order. Restart boundaries occur only after a committed artifact. Replay uses the original operation key. A semantic-conflict attempt changes only the declared mutation and reuses that operation key.

Period windows are half-open `[start,end)`. `AE1-UAT-003` places its document exactly at the start and includes it once. `AE1-UAT-004` has a no-activity period ending `2026-09-10T16:00:00Z` and places the document exactly at that instant in the next period, never the first.

## 6. Exact monetary source profiles

All amounts below are signed 64-bit PHP minor units; display divides by 100 with exactly two decimal places. No binary floating point or extra rounding is permitted.

| Profile | Transaction/source facts in minor units |
|---|---|
| `SP-A` approved-calculation | active gross `16000`; void `1000`; BIR gross `16000`; BIR net/GTA contribution `13000`; VATable `10000`; VAT `2000`; VAT-exempt `1000`; zero-rated `0`; SC discount `1000`; PWD discount `1000`; other statutory `1000`; coupon/promotion `0`; SC/PWD VAT adjustment `300`/`200`; Manual `500`; overflow `200`; refunds/returns/adjustments/service charge `0`; two documents; one active tender `13000`, one voided-document tender `1000` excluded from final tender reconciliation. |
| `SP-B` ordinary VAT | gross/net/GTA contribution `11200`; VATable `10000`; VAT `1200`; every discount, exception, Manual, overflow, and unresolved privilege amount `0`; one document and one tender `11200`. |
| `SP-C` no activity | transaction count `0`; every monetary fact `0`; no fiscal range; GTA unchanged; all seven Accounting facts are `attested_zero`. |
| `SP-D` mixed tender | gross/net/GTA contribution `22400`; VATable `20000`; VAT `2400`; other monetary facts `0`; one document; tenders `cash=10000` and `qr_ph=12400`. |

Every period has exactly seven current `pos.annex_e1_period_accounting_facts`: Manual SI/OR, overflow, NAAC, Solo Parent, other VAT adjustment, VAT on returns, and residual VAT adjustment. Under `SP-A`, Manual `500` and overflow `200` are `recorded` with one source record each; the other five are `attested_zero`. Under `SP-B`, `SP-C`, and `SP-D`, all seven are `attested_zero`. Absence, unsupported, and not-applicable never mean zero.

## 7. Source population profiles

| Population | Periods | Documents / lines / totals | Tenders | Tax / discount detail | X / Z / BIR | Accounting facts | EJ records | Fiscal ranges |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `POP-A` (`SP-A`) | 1 | 2 / 2 / 2 | 2 | 2 / 2 | 1 / 1 / 1 | 7 | 6 | 1 |
| `POP-B` (`SP-B`) | 1 | 1 / 1 / 1 | 1 | 1 / 0 | 1 / 1 / 1 | 7 | 4 | 1 |
| `POP-BOUNDARY` | 2 | 1 / 1 / 1 | 1 | 1 / 0 | 2 / 2 / 2 | 14 | 7 | 1 |
| `POP-MONTH` (`SP-B`,`SP-C`,`SP-D`) | 3 | 2 / 2 / 2 | 3 | 2 / 0 | 3 / 3 / 3 | 21 | 11 | 2 |
| `POP-D` (`SP-D`) | 1 | 1 / 1 / 1 | 2 | 1 / 0 | 1 / 1 / 1 | 7 | 4 | 1 |

EJ types are only implemented governed types: `fiscal_document_committed`, `fiscal_document_voided` when applicable, `x_reading_committed`, `z_reading_committed`, and `bir_sales_summary_committed`. `SP-A` has two document commits, one void, X, Z, and BIR events. Read/export activity creates no fiscal event.

Across the 19 isolated case definitions the planned counts are: 19 Sites, 19 fiscal identities, 19 header profiles, 22 fiscal periods, 35 fiscal documents, 35 lines, 35 totals, 37 tenders, 35 tax details, 30 discount details, 22 X reports, 22 Z reports, 22 BIR summaries, 154 Accounting facts, 116 EJ records, and 20 fiscal ranges. These are specification counts, not created records.

## 8. Reconciliation rules

Tolerance is zero minor units:

```text
R01 D07 - D17 - D18 = ACTIVE_GROSS
R02 D16 = OTHER_STATUTORY_DISCOUNT + COUPON_DISCOUNT + PROMOTIONAL_DISCOUNT
R03 D19 = D12 + D13 + D14 + D15 + D16 + D17 + D18
R04 D25 = D20 + D21 + D22 + D23 + D24
R05 D26 + D22 = D09
R06 D27 + D19 + D09 = D07
R07 D29 - D06 - D28 = D27
R08 copied operands equal the committed BIR Summary/Z/fact source
R09 Site POS Server, fiscal identity, PHP currency, and period agree across sources
R10 EJ references corroborate Z/BIR transitions and contribute no amount
R11 included active tender total = BIR net_sales_amount_minor_units
R12 present GTA = previous GTA + BIR net_sales_amount_minor_units
```

`SP-A` expected results are R01 `16000`, R02 `1000`, R03 `4000`, R04 `500`, R05 `2000`, R06 `17000`, R07 `11000`, R11 `13000`, R12 `113000`. `SP-B` gives `11200`, `0`, `0`, `0`, `1200`, `11200`, `10000`, `11200`, and its period GTA. `SP-C` gives all zero with unchanged GTA. `SP-D` gives `22400`, `0`, `0`, `0`, `2400`, `22400`, `20000`, `22400`, and its period GTA.

## 9. Replay, conflict, correction, and recovery

Replay uses the original operation identity and exact source set; it must return the same workbook UUID, revision, filename, SHA-256, byte length, and stored bytes. `AE1-UAT-013` changes the current Manual fact from `500` to `600` through a new fact that supersedes the original, then retries the original workbook operation without a correction request; expected result is semantic conflict and no workbook mutation. The superseding fact uses correction reason `source_correction` and preserves period/scope.

No correction workbook is authorized in the 19-case population because correction scenario `AE1-UAT-015` is excluded by AE-DR-004. The lineage contract is nevertheless fixed: a later authorized correction must use a new operation, `supersedes_workbook_id`, `authorized_restatement`, an approval reference, revision increment, and immutable prior bytes. Restart (`014`) reads the stored bytes. Tamper/missing (`016`) returns no bytes. Publication rollback (`024`) leaves no committed metadata and permits deletion only of the proven invocation-owned unreferenced orphan.

## 10. Implementation and approval boundary

A later implementation must materialize only the records in the source matrix, use supported runtime/API paths, verify all identities and hashes, and fail if the time-controlled environment cannot produce the fixed timestamps. It must not seed Production-like identities, synthesize historical facts, resolve external decisions, include excluded scenarios, or alter approved formulas.

Approval roles are Synthetic Data Steward, Accounting Reviewer for formula-preservation review, Technical Reviewer, Security/Privacy Reviewer, and Controlled UAT Authorizer. Approval of this document would authorize only a separate decision on fixture implementation. Dataset implementation, environment provisioning, loading, UAT execution, workbook generation, external delivery, BIR submission, Production, E-2 through E-5, ARTS POSLog, signing/encryption, and destructive retention remain unauthorized.

Any changed value, formula, identity namespace, included scenario, source status, chronology rule, or expected field requires a new version, recalculated manifest hashes, and renewed specification review.
