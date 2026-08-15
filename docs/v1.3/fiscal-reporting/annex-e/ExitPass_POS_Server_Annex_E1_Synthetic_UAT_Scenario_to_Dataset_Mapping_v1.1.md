# ExitPass POS Server Annex E-1 Synthetic UAT Scenario-to-Dataset Mapping v1.1

## 1. Control rules

Each included scenario owns exactly one case and all cases remain `IMPLEMENTATION_NOT_AUTHORIZED`, `LOADING_NOT_AUTHORIZED`, and `EXECUTION_NOT_AUTHORIZED`. Identity names use `annex-e1-synthetic-uat:v1.1|<scenario>|<object-type>|<ordinal>` from the v1.1 specification. Ranges below are inclusive and every ordinal maps to one closed registry entry.

`POP-A11` contains one period, documents 0001 SC, 0002 PWD, 0003 coupon, and 0004 voided; facts 0001-0007; EJ 0001-0008; one X, one Z, one BIR, and one range child under each X/Z report. `POP-B11` contains one period, one ordinary document, seven facts, EJ 0001-0004, and one range child under each X/Z report. `POP-D11` differs only by two tenders. `POP-BOUNDARY11` contains two active `SP-B11` periods. `POP-MONTH11` contains active `SP-B11`, `SP-B11`, and `SP-D11` periods. No included population has transaction count zero, a blank fiscal range, or `NO_ACTIVITY`.

## 2. Included scenario mapping

| Scenario | Case | Population and exact object ordinals | Period / counter posture | Exact expected rows | Special request, replay, or recovery contract | External-decision posture |
|---|---|---|---|---|---|---|
| `AE1-UAT-001` | `DS-AE1-001` | `POP-A11`; period 0001; documents/lines/totals/tax 0001-0004; tenders 0001-0004; discounts 0001-0003; statutory 0001-0002; facts 0001-0007; EJ 0001-0008 | 2026-09-10; reset 0; Z 1; previous GTA 100000 | `RP-A11` | generation 0001; workbook 0001; exact replay comparison is evidence-only | 002/004/010/019/020A/024 remain unresolved for later acceptance |
| `AE1-UAT-003` | `DS-AE1-003` | `POP-B11`; document 0001 effective exactly period start | 2026-09-10; reset 0; Z 1; prior GTA 0 | `RP-B11-START` | inclusion occurs exactly once | common execution gate only |
| `AE1-UAT-004` | `DS-AE1-004` | `POP-BOUNDARY11`; periods 0001-0002; ordinary documents 0001-0002; second document effective exactly period-1 end | dates 2026-09-10/11; Z 1/2; GTA 0->11200->22400 | `RP-B11-BOUNDARY-1`, `RP-B11-BOUNDARY-2` | first row has document 0001; boundary document appears only in row 2 | no dependency on AE-DR-010/011A because both rows have qualifying activity |
| `AE1-UAT-005` | `DS-AE1-005` | `POP-MONTH11`; periods/documents 0001-0003; profiles B11/B11/D11 | dates 2026-09-10..12; Z 1..3; GTA 0->11200->22400->44800 | `RP-B11-MONTH-1`, `RP-B11-MONTH-2`, `RP-D11-MONTH-3` | generation must retain period sequence | 019/020A/024 remain unresolved for later presentation acceptance |
| `AE1-UAT-007` | `DS-AE1-007` | `POP-A11` exact ordinals defined for 001 | 2026-09-10; 0/1; previous GTA 100000 | `RP-A11` | validates D07/D16/D19/D25/D26/D27/D29 | AE-DR-019 remains unresolved for display acceptance |
| `AE1-UAT-009` | `DS-AE1-009` | `POP-D11`; tender 0001 cash 10000, 0002 digital wallet 12400 | 2026-09-10; 0/1; prior GTA 0 | `RP-D11-SINGLE` | tender evidence does not create an Annex field | common execution gate only |
| `AE1-UAT-010` | `DS-AE1-010` | `POP-A11`; Z 0001, BIR 0001, report requests/scopes 0001 | 2026-09-10; 0/1; previous GTA 100000 | `RP-A11` | copied operands bind immutable Z/BIR IDs and hashes | common execution gate only |
| `AE1-UAT-011` | `DS-AE1-011` | `POP-A11`; stream 0001; EJ 0001-0008 | stream sequence 1-8 | `RP-A11` | R10 verifies all transition bindings; EJ contributes no amount | common execution gate only |
| `AE1-UAT-012` | `DS-AE1-012` | `POP-A11`; generation request 0001; replay record 0001 | committed revision 1 | `RP-A11` | exact replay reuses operation 0001 and creates no second workbook | 002/004/019/024 remain unresolved for official acceptance |
| `AE1-UAT-013` | `DS-AE1-013` | `POP-A11`; facts 0001-0007 plus corrected Manual fact 0008 | same period; corrected fact effective time stays in period | original `RP-A11` remains authoritative | request 0002 changes current semantics under operation 0001; conflict record 0001; no correction workbook | common execution gate only |
| `AE1-UAT-014` | `DS-AE1-014` | `POP-A11`; replay record 0001; recovery record 0001 | restart after revision-1 commit | `RP-A11` | read/download after restart returns stored identity and bytes | common execution gate only |
| `AE1-UAT-016` | `DS-AE1-016` | `POP-A11`; artifact integrity attempt is request 0002 | after revision-1 commit | `RP-A11` control | missing or altered invocation-owned content returns no bytes and does not regenerate | common execution gate only |
| `AE1-UAT-017` | `DS-AE1-017` | precondition `POP-A11`; principal/evidence IDs 0001 | source exists; no generation authority | `RP-A11` control | unauthorized generation request creates no new workbook or audit success row | common execution gate only |
| `AE1-UAT-018` | `DS-AE1-018` | precondition `POP-A11`; read/export principals and access requests 0001-0003 | revision 1 exists | `RP-A11` control | read, download, and generation permissions remain separate | common execution gate only |
| `AE1-UAT-019` | `DS-AE1-019` | precondition `POP-A11`; wrong Site, identity, currency request identities 0001-0003 | governed scope unchanged | `RP-A11` control | all wrong-scope operations produce no authoritative mutation | common execution gate only |
| `AE1-UAT-020` | `DS-AE1-020` | precondition `POP-A11`; fixture authority request 0001 | Production host classification | `RP-A11` control | Production rejects fixture authority before disclosure or persistence | AE-DR-011A remains only a retained external cross-reference |
| `AE1-UAT-021` | `DS-AE1-021` | precondition `POP-A11`; invalid attempts profile/month/currency/open/uncommitted/missing-fact/correction = generation 0002-0008 | each attempt restores the exact control snapshot | `RP-A11` control | conflict record 0001 covers changed-semantic same operation; every attempt adds zero outputs | common execution gate only |
| `AE1-UAT-023` | `DS-AE1-023` | `POP-A11`; privacy evidence 0001-0003 | revision 1 exists | `RP-A11` | scanner checks DTO, stored metadata, and evidence manifest | common execution gate only |
| `AE1-UAT-024` | `DS-AE1-024` | `POP-A11`; generation attempts 0001 failed publication and 0002 clean retry; recovery 0001 | failed metadata count 0; retry revision 1 | `RP-A11` after retry | inaccessible orphan has no workbook ID; only proven unreferenced case file may be removed | AE-DR-016B keeps generalized cleanup prohibited |

There are 22 rows: 17 single-row cases other than 004/005, two rows for 004, and three rows for 005. `17 + 2 + 3 = 22`.

## 3. No-activity behavioral boundary

`DS-AE1-004` period 1 has ordinary document 0001 with transaction count 1, fiscal range count 1, D02/D03 populated, D07 11200, and D32 `NONE`. Its period-2 boundary document is distinct and appears only in row 2. `DS-AE1-005` has one or more qualifying documents in every period; all three rows have ranges and D32 `NONE`. Their behavioral fingerprints therefore cannot reproduce excluded `AE1-UAT-002`.

The prohibited no-activity fingerprint is: transaction count 0, range count 0, D02/D03 null, unchanged GTA, all amounts zero, advanced Z, and D32 `NO_ACTIVITY`. No included case has that combination.

## 4. Excluded scenarios

| Scenario | External blocker | Prohibited status | Reason and evidence required |
|---|---|---|---|
| `AE1-UAT-002` | AE-DR-010, AE-DR-011A | `EXCLUDED_NON_EXECUTABLE` | No-activity Remarks and inactive privilege presentation require examiner evidence. |
| `AE1-UAT-006` | AE-DR-020A, AE-DR-024 | `EXCLUDED_NON_EXECUTABLE` | H08 and golden workbook geometry require examiner evidence. |
| `AE1-UAT-008` | AE-DR-011A, AE-DR-012, AE-DR-019 | `EXCLUDED_NON_EXECUTABLE` | Privilege zero/nonzero and display acceptance remain externally controlled. |
| `AE1-UAT-015` | AE-DR-004 | `EXCLUDED_NON_EXECUTABLE` | Correction filename and duplicate/version rules remain unresolved. |
| `AE1-UAT-022` | AE-DR-012 | `EXCLUDED_NON_EXECUTABLE` | Nonzero unresolved privilege source/destination/formula/privacy treatment is not approved. |
| `AE1-UAT-025` | AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-019, AE-DR-020A, AE-DR-024 | `EXCLUDED_NON_EXECUTABLE` | Official acceptance composite requires all seven listed confirmations. |

No included case, alias, shared profile, expected row, or side effect implements an excluded scenario.
