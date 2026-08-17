# ExitPass POS Server Annex E-1 Synthetic UAT Scenario-to-Dataset Mapping v1.7

## 1. Control

Every included case is `OFFLINE_DATASET_IMPLEMENTED`, `ISOLATED_TEST_LOAD_AUTHORIZED`, and `ISOLATED_TEST_EXECUTION_AUTHORIZED`. Shared, UAT, and Production loading or execution remain unauthorized. Identity names remain under the v1.1 identity grammar. Version 1.7 preserves the authorized parking-session reference serialization and the v1.6 arithmetic correction while changing only scenario 013 fact 0008 from the unsupported synthetic alias to canonical `source_correction`.

## 2. Included mapping

| Scenario | Case | Population | Periods | Ordered expected rows | Purpose |
|---|---|---|---:|---|---|
| `AE1-UAT-001` | `DS-AE1-001` | `SP-A13` | 1 | `RP-A13` | deterministic generation/export evidence |
| `AE1-UAT-003` | `DS-AE1-003` | `SP-B13` | 1 | `RP-B13-START` | document exactly at period start |
| `AE1-UAT-004` | `DS-AE1-004` | `SP-B13 x2` | 2 | `RP-B13-BOUNDARY-1; RP-B13-BOUNDARY-2` | active document in each period |
| `AE1-UAT-005` | `DS-AE1-005` | `SP-B13, SP-B13, SP-D13` | 3 | `RP-B13-MONTH-1; RP-B13-MONTH-2; RP-D13-MONTH-3` | chronological month sequence |
| `AE1-UAT-007` | `DS-AE1-007` | `SP-A13` | 1 | `RP-A13` | affected-position proof |
| `AE1-UAT-009` | `DS-AE1-009` | `SP-D13` | 1 | `RP-D13-SINGLE` | tender split |
| `AE1-UAT-010` | `DS-AE1-010` | `SP-A13` | 1 | `RP-A13` | Z/BIR source binding |
| `AE1-UAT-011` | `DS-AE1-011` | `SP-A13` | 1 | `RP-A13` | eight-event journal chain |
| `AE1-UAT-012` | `DS-AE1-012` | `SP-A13` | 1 | `RP-A13` | exact replay evidence |
| `AE1-UAT-013` | `DS-AE1-013` | `SP-A13` | 1 | `RP-A13` | fact correction and conflict |
| `AE1-UAT-014` | `DS-AE1-014` | `SP-A13` | 1 | `RP-A13` | restart readback evidence |
| `AE1-UAT-016` | `DS-AE1-016` | `SP-A13` | 1 | `RP-A13` | stored-byte integrity failure evidence |
| `AE1-UAT-017` | `DS-AE1-017` | `SP-A13` | 1 | `RP-A13` | generation permission denial |
| `AE1-UAT-018` | `DS-AE1-018` | `SP-A13` | 1 | `RP-A13` | permission separation |
| `AE1-UAT-019` | `DS-AE1-019` | `SP-A13` | 1 | `RP-A13` | cross-scope denial |
| `AE1-UAT-020` | `DS-AE1-020` | `SP-A13` | 1 | `RP-A13` | Production fixture rejection |
| `AE1-UAT-021` | `DS-AE1-021` | `SP-A13` | 1 | `RP-A13` | invalid request/conflict matrix |
| `AE1-UAT-023` | `DS-AE1-023` | `SP-A13` | 1 | `RP-A13` | privacy scan |
| `AE1-UAT-024` | `DS-AE1-024` | `SP-A13` | 1 | `RP-A13` | orphan-inaccessible retry boundary |

There are 19 cases and 22 rows: 15 + 1 + 2 + 3 + 1. Every row has H01-H10 and D01-D32.

## 3. Exact ordinals

Every A13 case owns period 0001; documents, lines, totals, and tax rows 0001-0004; tenders 0001-0004; discounts 0001-0003; statutory facts 0001-0002; histories 0001-0005; Accounting facts 0001-0007 except case 013 also 0008; EJ records/transitions 0001-0008; X/Z/BIR 0001; and ranges 0001-0002.

Each of the 15 A13 cases contributes two runtime parking-session comparisons, documents 0001 and 0002, for exactly 30 comparisons. For each comparison, command `CentralPmsParkingSessionRef=lowercase-D(REF(parking-session,o))` and snapshot `ParkingSessionId=REF(parking-session,o)`; expected mismatches are zero.

B13 owns one document/line/total/tax/tender and four EJ events per period. D13 adds tender 0002. Case 004 owns period/report ordinals 0001-0002; case 005 owns 0001-0003. Global document ordinals follow period then in-period document order.

## 4. Excluded mapping

| Scenario | Decision dependencies | Status |
|---|---|---|
| `AE1-UAT-002` | AE-DR-010, AE-DR-011A | `EXCLUDED_NON_EXECUTABLE` |
| `AE1-UAT-006` | AE-DR-020A, AE-DR-024 | `EXCLUDED_NON_EXECUTABLE` |
| `AE1-UAT-008` | AE-DR-011A, AE-DR-012, AE-DR-019 | `EXCLUDED_NON_EXECUTABLE` |
| `AE1-UAT-015` | AE-DR-004 | `EXCLUDED_NON_EXECUTABLE` |
| `AE1-UAT-022` | AE-DR-012 | `EXCLUDED_NON_EXECUTABLE` |
| `AE1-UAT-025` | AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-019, AE-DR-020A, AE-DR-024 | `EXCLUDED_NON_EXECUTABLE` |

No alias, shared population, expected row, or side effect makes an excluded scenario executable.
