# ExitPass POS Server Annex E-1 Synthetic UAT Dataset Specification Correction Resolution Matrix v1.3

| Finding | Prior status | v1.3 status | Changed document and section | Correction and independent validation | Remaining dependency | Authorization effect |
|---|---|---|---|---|---|---|
| F01 deterministic identity | verified resolved | `PRESERVED_RESOLVED` | Specification 4 | Preserves namespace/name grammar; reconstructs 2,364 unique v5 UUIDs and three vectors | independent v1.3 review | none yet |
| F02 semantic/hash contract | not resolved | `RESOLVED_IN_V1_3` | Hash Contract 2-7 | AE1H binary framing fixes every scalar, object, array, set, package, source and evidence byte; four non-empty vectors and exact hex/digests reproduce | independent review | package may be reviewed |
| F03 SP-A compatibility | not resolved | `RESOLVED_IN_V1_3` | Specification 7-9; Population 3-4 | statutory original corrected to tax-inclusive 11200; `9200+2000=11200`; source definitions and runtime guard inspected; arithmetic R01-R12 zero-difference | independent review | package may be reviewed |
| F04 canonical source rows | not resolved | `RESOLVED_IN_V1_3` | Population 1-7 | all 29 families have complete types, null choices, literals/rules, times, IDs, relations, codes, hashes, requests, audits, artifacts and ordering | independent review | package may be reviewed |
| F05 C02 timing | verified resolved | `PRESERVED_RESOLVED` | Specification 6; Population 1 | fixed half-open chronology; 155/155 facts inside periods | independent review | none yet |
| F06 controlled clock | verified resolved | `PRESERVED_RESOLVED` | Specification 6, 11 | offline fixed values require no runtime clock | runtime clock separately blocked | none yet |
| F07 no-activity leakage | verified resolved | `PRESERVED_RESOLVED` | Mapping 2-4 | cases 004/005 remain active and excluded no-activity scenarios remain non-executable | external decisions | none yet |
| F08 Electronic Journal | verified resolved | `PRESERVED_RESOLVED` | Hash Contract 1, 5; Specification 10 | exact v1.2 ledger incorporated by SHA; original-amount correction does not enter EJ facts; 148/148 chains retained | independent review | none yet |
| v1.2 blocker 1 non-empty hash grammar | blocking | `RESOLVED_IN_V1_3` | Hash Contract 3-7 | length-prefixes all names/values; defines null/missing/empty, Unicode, binary, nested values, sorting, duplicates, self exclusion and root recursion | independent review | package may be reviewed |
| v1.2 blocker 2 SP-A runtime finality | blocking | `RESOLVED_IN_V1_3` | Specification 7; Source Matrix 3; Population 4 row 11 | authoritative original amount uses gross tax-inclusive 11200; all current finality inequalities/equalities pass without bypass | independent review | package may be reviewed |
| v1.2 blocker 3 incomplete row families | blocking | `RESOLVED_IN_V1_3` | Population 4-7 | closes the exact 22 blocked families while preserving seven complete families and all counts | independent review | package may be reviewed |

`RESOLVED_IN_V1_3` is package-author status only. It is not an authorization token. Canonical implementation remains `BLOCKED_ANNEX_E1_SYNTHETIC_DATASET_IMPLEMENTATION` until a separate independent v1.3 authorization review is merged.
