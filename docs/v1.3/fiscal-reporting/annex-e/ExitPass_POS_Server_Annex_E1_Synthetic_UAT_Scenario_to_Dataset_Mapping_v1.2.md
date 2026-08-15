# ExitPass POS Server Annex E-1 Synthetic UAT Scenario-to-Dataset Mapping v1.2

## 1. Control

The v1.0 and v1.1 packages remain immutable historical reviewed records. Version 1.2 supersedes them only for future implementation consideration and does not alter either earlier authorization review. Machine-readable dataset and validator implementation remain blocked until a separate v1.2 authorization review is merged.

Every case is `IMPLEMENTATION_NOT_AUTHORIZED`, `LOADING_NOT_AUTHORIZED`, and `EXECUTION_NOT_AUTHORIZED`. Identity names retain the v1.1 grammar. `SP-A12` owns four documents and eight journal events; `SP-B12` owns one document and four events; `SP-D12` owns one document, two tenders, and four events.

## 2. Included mapping

| Scenario | Case | Population | Periods | Ordered expected rows | Purpose and exact boundary | Eligibility |
|---|---|---|---:|---|---|---|
| `AE1-UAT-001` | `DS-AE1-001` | `SP-A12` | 1 | `RP-A12` | deterministic generation/export evidence | specification only; external decisions retained |
| `AE1-UAT-003` | `DS-AE1-003` | `SP-B12` | 1 | `RP-B12-START` | document exactly period start | specification only; external decisions retained |
| `AE1-UAT-004` | `DS-AE1-004` | `SP-B12 x2` | 2 | `RP-B12-BOUNDARY-1; RP-B12-BOUNDARY-2` | active document in each period; boundary inclusion once | specification only; external decisions retained |
| `AE1-UAT-005` | `DS-AE1-005` | `SP-B12, SP-B12, SP-D12` | 3 | `RP-B12-MONTH-1; RP-B12-MONTH-2; RP-D12-MONTH-3` | chronological month sequence | specification only; external decisions retained |
| `AE1-UAT-007` | `DS-AE1-007` | `SP-A12` | 1 | `RP-A12` | affected-position formula proof | specification only; external decisions retained |
| `AE1-UAT-009` | `DS-AE1-009` | `SP-D12` | 1 | `RP-D12-SINGLE` | cash and digital-wallet tender split | specification only; external decisions retained |
| `AE1-UAT-010` | `DS-AE1-010` | `SP-A12` | 1 | `RP-A12` | Z/BIR source binding | specification only; external decisions retained |
| `AE1-UAT-011` | `DS-AE1-011` | `SP-A12` | 1 | `RP-A12` | eight-event journal chain | specification only; external decisions retained |
| `AE1-UAT-012` | `DS-AE1-012` | `SP-A12` | 1 | `RP-A12` | exact replay evidence | specification only; external decisions retained |
| `AE1-UAT-013` | `DS-AE1-013` | `SP-A12` | 1 | `RP-A12` | fact correction plus semantic conflict; no correction workbook | specification only; external decisions retained |
| `AE1-UAT-014` | `DS-AE1-014` | `SP-A12` | 1 | `RP-A12` | restart readback evidence | specification only; external decisions retained |
| `AE1-UAT-016` | `DS-AE1-016` | `SP-A12` | 1 | `RP-A12` | stored-byte integrity failure evidence | specification only; external decisions retained |
| `AE1-UAT-017` | `DS-AE1-017` | `SP-A12` | 1 | `RP-A12` | generation permission denial | specification only; external decisions retained |
| `AE1-UAT-018` | `DS-AE1-018` | `SP-A12` | 1 | `RP-A12` | read/download/generate permission separation | specification only; external decisions retained |
| `AE1-UAT-019` | `DS-AE1-019` | `SP-A12` | 1 | `RP-A12` | cross-scope denial | specification only; external decisions retained |
| `AE1-UAT-020` | `DS-AE1-020` | `SP-A12` | 1 | `RP-A12` | Production fixture rejection | specification only; external decisions retained |
| `AE1-UAT-021` | `DS-AE1-021` | `SP-A12` | 1 | `RP-A12` | invalid request and conflict matrix | specification only; external decisions retained |
| `AE1-UAT-023` | `DS-AE1-023` | `SP-A12` | 1 | `RP-A12` | privacy scan | specification only; external decisions retained |
| `AE1-UAT-024` | `DS-AE1-024` | `SP-A12` | 1 | `RP-A12` | orphan-inaccessible retry boundary | specification only; external decisions retained |

Every included scenario has exactly one case. Output rows total `15 + 1 + 2 + 3 + 1 = 22`.

## 3. Exact population ordinals

For every SP-A12 case: period 0001; documents/lines/totals/tax 0001-0004; tenders 0001-0004; discount details 0001-0003; statutory facts 0001-0002; status history 0001-0005; Accounting facts 0001-0007 except case 013 also 0008; journal records/transitions/source transitions 0001-0008; X/Z/BIR 0001; ranges 0001-0002.

SP-B12 owns one document/line/total/tax/tender and journal 0001-0004 per period. SP-D12 adds tender 0002. Case 004 owns period/report ordinals 0001-0002 and case 005 owns 0001-0003. Global document ordinals follow chronological period order.

## 4. No-activity boundary

Case 004 has one qualifying document, one range, nonzero D07, populated D02/D03, and D32 `NONE` in both periods. Case 005 has the same active fingerprint in each of three periods. Neither has the excluded fingerprint: zero qualifying documents, zero ranges, null range endpoints, unchanged GTA, all monetary values zero, advanced Z, and D32 `NO_ACTIVITY`.

## 5. Excluded mapping

| Scenario | External confirmation set | Status | Reconsideration evidence |
|---|---|---|---|
| `AE1-UAT-002` | AE-DR-010, AE-DR-011A | `EXCLUDED_NON_EXECUTABLE` | examiner-approved no-activity Remarks and inactive privilege presentation |
| `AE1-UAT-006` | AE-DR-020A, AE-DR-024 | `EXCLUDED_NON_EXECUTABLE` | H08 rule and approved golden geometry |
| `AE1-UAT-008` | AE-DR-011A, AE-DR-012, AE-DR-019 | `EXCLUDED_NON_EXECUTABLE` | privilege source/formula/display approval |
| `AE1-UAT-015` | AE-DR-004 | `EXCLUDED_NON_EXECUTABLE` | correction filename, duplicate, and version rules |
| `AE1-UAT-022` | AE-DR-012 | `EXCLUDED_NON_EXECUTABLE` | nonzero privilege source/destination/formula/privacy treatment |
| `AE1-UAT-025` | AE-DR-002, 004, 010, 011A, 019, 020A, 024 | `EXCLUDED_NON_EXECUTABLE` | complete official acceptance evidence |

No alias, shared population, expected row, or side effect makes an excluded scenario executable.

