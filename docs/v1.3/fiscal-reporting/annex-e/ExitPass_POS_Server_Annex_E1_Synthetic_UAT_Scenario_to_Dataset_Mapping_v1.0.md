# ExitPass POS Server Annex E-1 Synthetic UAT Scenario-to-Dataset Mapping v1.0

## 1. Rules

Each included scenario has exactly one case. The case identity is UUIDv5 using the namespace and canonical name in the dataset specification. Every case is `PENDING_SPECIFICATION_REVIEW`, `IMPLEMENTATION_NOT_AUTHORIZED`, `LOADING_NOT_AUTHORIZED`, and `EXECUTION_NOT_AUTHORIZED`. Source IDs are the exact UUIDv5 names shown; ordinal child IDs append their four-digit ordinal.

Common scope is synthetic Site `SYN-AE1-SPS-01`, fiscal identity `SYN-AE1-FI-01`, `PHP`, September 2026. Each case uses a separately reset invocation-owned database, so identical fiscal references cannot collide between cases.

## 2. Included cases

| Scenario | Dataset case | Objective and population | Exact source identity roots | Fiscal period / counters | Expected rows and reconciliation | Replay/correction | Evidence | External dependency |
|---|---|---|---|---|---|---|---|---|
| AE1-UAT-001 | `DS-AE1-001` | Valid bounded generation; `POP-A` | `AE1-UAT-001\|period\|0001`, `\|fiscal-document\|0001..0002`, `\|z-report\|0001`, `\|bir-summary\|0001`, `\|accounting-fact\|0001..0007` | 2026-09-10; reset 0; Z 1 | one `RP-A`; R01-R12 zero difference | exact replay required | source/row map, metadata, SHA-256, length | Official acceptance still affected by AE-DR-002/004/010/019/020A/024; bounded specification proceeds |
| AE1-UAT-003 | `DS-AE1-003` | Inclusive period start; `POP-B`; document effective exactly `2026-09-09T16:00:00Z` | `AE1-UAT-003\|...` ordinals per `POP-B` | 2026-09-10; 0 / 1 | one `RP-B`; included once; R01-R12 zero difference | none | source timestamp, period membership, row | None beyond common gates |
| AE1-UAT-004 | `DS-AE1-004` | Exclusive period end; `POP-BOUNDARY` | `AE1-UAT-004\|period\|0001..0002`; document ordinal 0001 effective `2026-09-10T16:00:00Z` | 2026-09-10 no activity 0/1; 2026-09-11 active 0/2 | ordered `RP-C`, `RP-B2`; document only row 2 | none | both periods, assignment, ordered rows | None beyond common gates |
| AE1-UAT-005 | `DS-AE1-005` | Monthly chronology; `POP-MONTH` | `AE1-UAT-005\|period\|0001..0003`; Z/BIR/facts matching ordinals | 2026-09-10..12; reset 0; Z 1..3 | `RP-B`, `RP-C`, `RP-D` in that order; all R rules exact | repeated generation must preserve order | ordered membership, counters, hash | AE-DR-019/020A/024 affect official presentation acceptance only |
| AE1-UAT-007 | `DS-AE1-007` | Approved equations and nonzero Manual/overflow; `POP-A` | `AE1-UAT-007\|...` | 2026-09-10; 0 / 1 | one `RP-A`; D07/D16/D19/D25/D26/D27/D29 exact | none | named inputs and R01-R12 | AE-DR-019 affects display acceptance only |
| AE1-UAT-009 | `DS-AE1-009` | Mixed-tender independence; `POP-D` | `AE1-UAT-009\|tender\|0001` cash; `\|tender\|0002` qr_ph | 2026-09-10; 0 / 1 | one `RP-D`; tenders 10000+12400=22400; no Annex tender field | none | tender/BIR and field-absence proof | None beyond common gates |
| AE1-UAT-010 | `DS-AE1-010` | Immutable Z/BIR authority; `POP-A` | `AE1-UAT-010\|z-report\|0001`, `\|bir-summary\|0001` | 2026-09-10; 0 / 1 | one `RP-A`; source IDs/hashes bind every copied value | none | immutable Z/BIR snapshots and row | None beyond common gates |
| AE1-UAT-011 | `DS-AE1-011` | EJ traceability only; `POP-A` | EJ names `AE1-UAT-011\|electronic-journal-record\|0001..0006` | sequence 1..6 in source commit order | one `RP-A`; R10 matches opaque Z/BIR refs; no EJ amount used | none | successful chain verification and source refs | None beyond common gates |
| AE1-UAT-012 | `DS-AE1-012` | Exact stored-byte replay; `POP-A` | operation `SYN-AE1-V1-012-GENERATE-0001` | 2026-09-10; 0 / 1 | one `RP-A`; replay adds zero rows/files | same operation and semantics return revision 1 | before/replay IDs, hashes, bytes | AE-DR-002/004/019/024 affect official artifact acceptance only |
| AE1-UAT-013 | `DS-AE1-013` | Semantic conflict; `POP-A` plus superseding Manual fact | original and child names `\|accounting-fact\|0001` / `0008` | same period/scope; child Manual 600 | committed output remains original `RP-A`; conflicting retry produces no row | child uses `source_correction`; original operation conflicts | pre/post manifest and safe conflict | None beyond common gates |
| AE1-UAT-014 | `DS-AE1-014` | Restart recovery; `POP-A` | operation `SYN-AE1-V1-014-GENERATE-0001` | restart after committed revision 1 | one `RP-A`; metadata/bytes unchanged | post-restart replay exact | process boundary, before/after hashes | None beyond common gates |
| AE1-UAT-016 | `DS-AE1-016` | Missing/tampered artifact fail closed; `POP-A` | artifact copy owned by case; metadata identity unchanged | after committed revision 1 | control row `RP-A`; tampered/missing download yields no bytes | no regeneration | expected/actual hash, safe result | None beyond common gates |
| AE1-UAT-017 | `DS-AE1-017` | Generate permission isolation; `POP-A` source only | principal `SYN-AE1-V1-017-PRINCIPAL-READONLY` | 2026-09-10; 0 / 1 | `RP-A` is the control expectation; attempted generation creates zero workbook rows | none | 403 and unchanged manifest | None |
| AE1-UAT-018 | `DS-AE1-018` | Read/export/generate separation; pre-created control from `POP-A` | principals `...-READ-0001`, `...-EXPORT-0001` | 2026-09-10; 0 / 1 | one control `RP-A`; each unauthorized operation mutates nothing | stored bytes only for authorized export principal | policy names, responses, manifest | None |
| AE1-UAT-019 | `DS-AE1-019` | Cross-site/identity/currency denial; pre-created `POP-A` control | wrong-scope roots `\|wrong-site\|0001`, `\|wrong-identity\|0001`, `\|wrong-currency\|0001` | governed scope unchanged | one control `RP-A`; every wrong scope denied and adds zero rows | none | hidden/not-found or denial; no leakage | None |
| AE1-UAT-020 | `DS-AE1-020` | Production fixture-authority rejection; pre-created `POP-A` control | principal `SYN-AE1-V1-020-PRINCIPAL-FIXTURE` | Production hosting classification | one control `RP-A`; fixture read/generate creates zero rows | none | authority class and denial | None; AE-DR-011A reference is retained only in decision matrix |
| AE1-UAT-021 | `DS-AE1-021` | Safe invalid/unsupported requests; `POP-A` control plus isolated mutations | operations `...-INVALID-PROFILE`, `...-INVALID-MONTH`, `...-USD`, `...-OPEN`, `...-UNCOMMITTED`, `...-MISSING-FACT`, `...-INCOMPLETE-CORRECTION` | each mutation starts from clean case snapshot | `RP-A` control; every invalid attempt adds zero rows/artifacts | no successful replay | safe 4xx classification; no raw SQL/path/stack | None |
| AE1-UAT-023 | `DS-AE1-023` | Privacy exclusion; `POP-A` | only identifiers defined in specification section 3 | 2026-09-10; 0 / 1 | one `RP-A`; prohibited scanner findings 0 | exact replay may be inspected | DTO, workbook, evidence field inventory | None |
| AE1-UAT-024 | `DS-AE1-024` | Artifact publication rollback; `POP-A` | operation `SYN-AE1-V1-024-GENERATE-0001`; retry `...-0002` | DB failure after content-addressed publication, before metadata commit | failed attempt 0 rows; clean retry one `RP-A` | retry produces authoritative revision 1 | DB/artifact manifests and inaccessible orphan proof | AE-DR-016B prohibits generalized/destructive cleanup |

Every `...|...` identity root above is prefixed by the canonical string `annex-e1-synthetic-uat:v1.0|`; omitted object ordinals are exactly those required by the referenced population profile, not open-ended values.

## 3. Excluded cases

| Scenario | External blocker | Reason | Implementation status | Evidence required before reconsideration |
|---|---|---|---|---|
| AE1-UAT-002 | AE-DR-010, AE-DR-011A | No-activity Remarks and inactive privilege display acceptance unresolved | `EXCLUDED_NOT_IMPLEMENTABLE` | Examiner-approved D32 and D14/D15 representation |
| AE1-UAT-006 | AE-DR-020A, AE-DR-024 | Header/geometry acceptance unresolved | `EXCLUDED_NOT_IMPLEMENTABLE` | Accepted H08 rule and golden geometry/wrapping evidence |
| AE1-UAT-008 | AE-DR-011A, AE-DR-012, AE-DR-019 | Privilege zero/nonzero and display profile unresolved | `EXCLUDED_NOT_IMPLEMENTABLE` | Exact privilege mapping/zero/display approvals |
| AE1-UAT-015 | AE-DR-004 | Correction filename convention unresolved | `EXCLUDED_NOT_IMPLEMENTABLE` | Exact correction filename and duplicate/version convention |
| AE1-UAT-022 | AE-DR-012 | Nonzero unresolved privilege mapping prohibited | `EXCLUDED_NOT_IMPLEMENTABLE` | Approved source, destination, formula, and privacy-safe treatment |
| AE1-UAT-025 | AE-DR-002, AE-DR-004, AE-DR-010, AE-DR-011A, AE-DR-019, AE-DR-020A, AE-DR-024 | Official acceptance composite remains unresolved | `EXCLUDED_NOT_IMPLEMENTABLE` | All seven listed confirmations |

Included and excluded sets are disjoint; their union is all 25 reviewed scenarios. Exclusion does not resolve, reject, or supersede any decision.
