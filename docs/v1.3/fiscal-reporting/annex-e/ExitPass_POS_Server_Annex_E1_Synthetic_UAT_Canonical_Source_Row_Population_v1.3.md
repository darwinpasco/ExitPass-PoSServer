# ExitPass POS Server Annex E-1 Synthetic UAT Canonical Source Row Population v1.3

## 1. Construction language

This document closes all 29 canonical row families. A future offline constructor must implement these expressions exactly and reject unresolved values.

| Expression | Exact meaning |
|---|---|
| `ID(token,o)` | UUIDv5 of `annex-e1-synthetic-uat:v1.1\|AE1-UAT-NNN\|token\|OOOO` in namespace `ae1d5e7a-7e2d-5c4d-9a11-202608140001` |
| `REF(type,o)` | UUIDv5 of `annex-e1-synthetic-reference:v1\|AE1-UAT-NNN\|type\|OOOO` in the same namespace; opaque reference, not a registry row |
| `NULL` | AE1H null; SQL-shaped nullable value is null |
| `EMPTY_OBJECT` | AE1H object with zero members; runtime JSON `{}` |
| `SETUP(k)` | `2026-09-01T00:00:00.0000000Z + NNN minutes + k seconds` |
| `PSTART(p)` | `2026-09-09T16:00:00.0000000Z + (p-1) days` |
| `PEND(p)` | `PSTART(p) + 1 day` |
| `DOC(o,p)` | `PSTART(p) + 9 hours + (o-1) seconds`, except case 004 document 0002 equals `PSTART(2)` |
| `XAT(p)` | `PEND(p) - 29 seconds` |
| `ZAT(p)` | `PEND(p) + 11 seconds` |
| `BIRAT(p)` | `PEND(p) + 13 seconds` |
| `FACTAT(f,p)` | `PEND(p) - 10 seconds + f milliseconds` |
| `FACTREC(f,p)` | `PEND(p) + 14 seconds + global-f milliseconds` |
| `AUDITAT(o)` | source event time plus 200 milliseconds, or `SETUP(1000+o)` for setup evidence |
| `HASH(row)` | source/evidence hash under the v1.3 AE1H family, excluding self-hash fields |
| `RUNTIME_HASH(row)` | current source canonicalizer named in the hash contract |

`NNN`, `p`, `o`, and `f` are fixed by the scenario mapping. All timestamps are UTC with seven fractional digits. Strings are literal and case-sensitive. `uuid`, `int64`, `int32`, `bool`, `date`, `time`, `timestamp`, `code`, `string`, `decimal(9,6)`, `binary32`, `object`, and ordered `array<T>` are the only types.

## 2. Universal literals

| Name | Literal |
|---|---|
| actor | `synthetic-data-steward` |
| service | `pos-server-annex-e1-uat-preparer` |
| runtime service | `pos-server-fiscal-document-runtime` |
| currency | `PHP` |
| timezone | `Asia/Manila` |
| taxpayer | `SYNTHETIC ANNEX E1 PARKING SERVICES INC.` |
| address | `100 SYNTHETIC AVENUE, TEST CITY 0000` |
| TIN | `000-000-000-000` |
| calculation profile | `pos-server-annex-e1-accounting-calculation:v1` |
| accounting approval | `SYN-AE1-ACCOUNTING-V13` |
| privacy | `SYNTHETIC_FISCAL_FACTS` |
| retention | `fiscal_reconstruction_hold` |

Codes and references are `SYN-AE1-SPS-NNN`, `SYN-AE1-FI-NNN`, `SYN-AE1-SERIAL-NNN`, `SYN-AE1-MIN-NNN`, series `SI-NNN`, document `SYN-AE1-SI-NNN-OOOO`, parking `SYN-AE1-PARKING-NNN-OOOO`, payment attempt `SYN-AE1-PAYMENT-NNN-OOOO`, confirmation `SYN-AE1-CONFIRM-NNN-OOOO`, finality `SYN-AE1-FINALITY-NNN-OOOO`, document operation `SYN-AE1-DOC-NNN-OOOO`, void operation `SYN-AE1-VOID-NNN-0004`, and case correlation `DS-AE1-NNN`.

All optional customer, vehicle, beneficiary, evidence, credential, external endpoint, provider, vendor, reprint, signature, encryption, correction-workbook, and raw-payload fields are `NULL`. No blank string stands for null.

## 3. Exact profile values

| Profile/doc | base/gross | discount | VAT | final | statutory original | tender |
|---|---:|---:|---:|---:|---:|---|
| A13/1 SC | 10000 | 2000 | 1200 | 9200 | 11200 | cash 9200 |
| A13/2 PWD | 10000 | 2000 | 1200 | 9200 | 11200 | cash 9200 |
| A13/3 coupon | 10000 | 1000 | 1200 | 10200 | NULL | cash 10200 |
| A13/4 void | 10000 | 0 | 1200 | 11200 | NULL | cash 11200, excluded after void |
| B13/1 | 10000 | 0 | 1200 | 11200 | NULL | cash 11200 |
| D13/1 | 20000 | 0 | 2400 | 22400 | NULL | cash 10000; digital wallet 12400 |

## 4. Complete family closure matrix

The `Fields` column is the canonical order used by AE1H. `?` means nullable but still present as `NULL`. Runtime-only omitted-null behavior is applied only by the named current runtime canonicalizer.

| # | Family / count / identity | Fields with exact types and values | Lifecycle, hashes, and result |
|---:|---|---|---|
| 1 | Site / 19 / `ID(site-pos-server,1)` | `id:uuid`; `code:string=SYN-AE1-SPS-NNN`; `display_name:string=Synthetic Annex E1 Site NNN`; `central_pms_site_ref?:string=NULL`; `central_pms_zone_ref?:string=NULL`; `time_zone:string=Asia/Manila`; `cutoff:time=00:00:00`; `operational_status_id?:uuid=NULL`; `is_active:bool=true`; `created_at:timestamp=SETUP(1)`; `updated_at:timestamp=SETUP(1)` | Create-only offline input; `HASH(row)`; R09; `CLOSED_V1_3` |
| 2 | Fiscal identity / 19 / `ID(fiscal-identity,1)` | `id:uuid`; `code:string=SYN-AE1-FI-NNN`; taxpayer/address/TIN universal; `status:code=APPROVED`; `MIN=SYN-AE1-MIN-NNN`; `serial=SYN-AE1-SERIAL-NNN`; `software=ExitPass POS Server`; `version=1.3`; `ptu_ref=SYN-AE1-PTU-NNN`; `accreditation_ref=SYN-AE1-ACCREDITATION-NNN`; `metadata=EMPTY_OBJECT`; `is_active=true`; `created_at=updated_at=approved_at=SETUP(2)`; `created_by=updated_by=approved_by=actor` | Approved immutable synthetic identity; `HASH`; R09; closed |
| 3 | Header profile / 19 / `ID(header-profile,1)` | `id`; Site/fiscal IDs; `profile_key=SYN-AE1-HEADER-NNN`; `template_profile=pos-server-sales-invoice-template:v1`; `presentation_profile=pos-server-digital-sales-invoice:v1`; serial/MIN; `location=Synthetic Annex E1 Site NNN`; `approval_ref=SYN-AE1-HEADER-APPROVAL-NNN`; `effective_start=2026-09-01T00:00:00.0000000Z`; `effective_end=NULL`; `status=APPROVED`; `approved_at=created_at=updated_at=SETUP(3)`; actor fields | H01-H08 source; `HASH`; closed |
| 4 | Fiscal period / 22 / `ID(period,p)` | `id`; Site/fiscal IDs; `currency=PHP`; `business_date=date(PEND(p))`; `start=PSTART(p)`; `end=PEND(p)`; `sequence=p`; `status=closed`; `prior_period_id=NULL` if p=1 else `ID(period,p-1)`; `opened_at=PSTART(p)`; `closing_started_at=PEND(p)+10s`; `closed_at=PEND(p)+11s`; actor/service; `created_at=SETUP(10+p)`; `updated_at=closed_at` | Half-open interval, C02/R09; `HASH`; closed |
| 5 | Fiscal range / 44 / `ID(fiscal-range,o)` | `id`; `report_id=ID(x-report,p)` for odd child and `ID(z-report,p)` for even child; `sequence_policy_id=ID(sequence-policy,1)`; `series=SI-NNN`; first/last sequence and number from profile; `gap_count=0`; `currency=PHP`; `created_at=XAT(p)` or `ZAT(p)` | Expected child; `HASH`; closed |
| 6 | Fiscal document / 67 / `ID(fiscal-document,o)` | `id`; Site/fiscal/period IDs; `type=sales_invoice`; `status=recorded` except A13 doc4 final `voided`; `currency=PHP`; `payable=profile final`; business date; sequence policy/value/number/series; prefix/suffix `NULL`; `assigned_at=DOC`; `assigned_by=runtime service`; parking/payment/confirmation/finality refs; `context=EMPTY_OBJECT`; `created_at=DOC`; `updated_at=void time` for doc4 else DOC; void fields exact only doc4 (`DOC+10s`, actor, `SYNTHETIC_SAME_PERIOD_VOID`) otherwise NULL | SC/PWD use v2 current request hash and corrected original 11200; others v1; report inputs; closed |
| 7 | Document line / 67 / `ID(document-line,o)` | `id`; document ID; `sequence=1`; `line_type=parking_fee`; `status=NULL`; `description=Synthetic parking fee NNN-OOOO`; `quantity:decimal=1.000000`; unit/gross/discount/tax/net from profile; `currency=PHP`; `source_ref=SYN-AE1-LINE-NNN-OOOO`; `context=EMPTY_OBJECT`; `created_at=updated_at=DOC` | Current request child; `HASH`; closed |
| 8 | Document total / 67 / `ID(document-total,o)` | `id`; document ID; `type=final_payable`; amount=profile final; PHP; context empty; created/updated DOC | Current request child; `HASH`; closed |
| 9 | Tax detail / 67 / `ID(tax-detail,o)` | `id`; document ID; `line_sequence=1`; `tax_type=vat`; `classification=vatable`; `rate=0.120000`; taxable/VAT profile; PHP; context empty; created/updated DOC | Current request child; `HASH`; closed |
| 10 | Discount detail / 45 / `ID(discount-detail,o)` | `id`; document ID; line 1; type `statutory_peer` docs1/2 or `coupon` doc3; basis 10000; discount profile; VAT privilege 0; PHP; `approval_ref=SYN-AE1-DISCOUNT-APPROVAL-NNN-OOOO`; beneficiary/evidence NULL; context empty; created/updated DOC | Current request child; exact approval; `HASH`; closed |
| 11 | Statutory fact / 30 / `ID(statutory-fact,o)` | fact/document IDs; `decision_command=REF(statutory-decision-command,o)`; `request=REF(statutory-request,o)`; `application_command=REF(statutory-application-command,o)`; `validation=REF(statutory-validation,o)`; `parking_session=REF(parking-session,o)`; Site ID; `site_group=REF(site-group,1)`; entitlement SC for 1/PWD for 2; benefit `STATUTORY_DISCOUNT_ONLY`; policy object `{resolution_basis:NATIONAL_LAW,applied_policy_id:REF(policy-reference,o),policy_code:SYN-AE1-STATUTORY-POLICY-V13,policy_version_id:NULL,national_law:RA_9994_OR_RA_10754,ordinance:NULL}`; original/applied tariff IDs `REF(original-tariff-snapshot,o)` and `REF(applied-tariff-snapshot,o)`; original 11200; basis 10000; VAT 1200; treatment `VAT_INCLUSIVE_NO_EXEMPTION`; discount 2000; final 9200; PHP; applied/snapshot/created/updated DOC; source `OPERATOR_CONSOLE`; terminal tender `ID(tender,o)` | IDs distinct/nonzero; full current finality invariant passes; v2 request hash; closed |
| 12 | Tender / 69 / `ID(tender,o)` | `id`; document ID; type cash except D13 second digital_wallet; amount profile; PHP; payment attempt/confirmation/finality refs always exact synthetic strings; provider ref NULL; context empty; created/updated DOC | No optional-reference choice remains; R11; `HASH`; closed |
| 13 | Tender breakdown / 48 / `ID(tender-breakdown,o)` | `id`; report ID; classification cash/digital_wallet; count and amount from report snapshot; PHP; `created_at=XAT/ZAT` | X/Z child; `HASH`; closed |
| 14 | Discount breakdown / 90 / `ID(discount-breakdown,o)` | `id`; report ID; classification coupon/PWD/SC in that order; count 1; amounts 1000/2000/2000; VAT privilege 0; PHP; created report time | A13 X/Z only; `HASH`; closed |
| 15 | Status history / 82 / `ID(status-history,o)` | `id`; document ID; previous NULL to recorded for commit, or recorded to voided for A13 doc4; `changed_at=DOC` or `DOC+10s`; changed_by runtime service or actor; reason code/text NULL except void code and `Synthetic same-period void`; correlation/idempotency document or void key; created_at=changed_at | Exact transition; `HASH`; closed |
| 16 | Accounting fact / 155 / `ID(accounting-fact,f)` | `id`; `operation_key=SYN-AE1-FACT-NNN-FFFF`; Site/fiscal/period/PHP; ordered type; status recorded for nonzero/correction else attested_zero; amount 500/200/0 or case13 correction 600; source count 1 for nonzero else 0; first/last ref `SYN-AE1-MANUAL-NNN` or `SYN-AE1-OVERFLOW-NNN`, else NULL; event ref NULL; approval universal; supersedes only case13 f8 -> f1; correction reason only f8 `SYNTHETIC_CORRECTION`; semantic version current; effective/recorded schedules; actor/service/correlation; created_at=recorded | `RUNTIME_HASH`; all C02 pass; D06/D14/D15/D22-D24/D28; closed |
| 17 | X Reading / 22 / `ID(x-report,p)` | report/request/scope IDs (`ID(x-request,p)`, `ID(x-scope,p)`); kind x_reading; committed; Site/fiscal/PHP/date/period; observed/generated/committed `XAT`; semantic version current; all snapshot amounts/counts; ordered ranges/tender/discount children; actor/service/correlation | request then committed; current report hash; EJ/R08; closed |
| 18 | Z Reading / 22 / `ID(z-report,p)` | all X fields with Z IDs; kind z_reading; previous/result reset 0; previous Z p-1/result p; previous GTA from prior, current=net, result=sum; state version p; closed period; transition/value IDs `ID(z-transition,p)`, `ID(z-value,p)`; all times ZAT | atomic close; current report/state hash; EJ/R08/R12; closed |
| 19 | BIR summary / 22 / `ID(bir-summary,p)` | summary/request/scope IDs; governing Z ID/ref/runtime hash; Site/fiscal/PHP/date/period; exact snapshot amounts/count/range; reset/Z/GTA; semantic version current; generated/committed BIRAT; actor/service/correlation | request then committed; current hash; all D operands/EJ; closed |
| 20 | Annex row / 22 / `ID(annex-row,p)` | `id`; workbook ID; row sequence p; exact H01-H10 and D01-D32 from expected matrix; `source_semantic_hash=HASH(ordered BIR+facts source object)`; `calculation_semantic_hash=HASH(ordered H/D/R object)`; ordered R01-R12 results each expected/calculated/difference/count difference/result PASS; `status=SPECIFIED` | Offline expected result; all values and hashes derivable; closed |
| 21 | Annex fact link / 154 / `ID(annex-fact-source,o)` | `id`; workbook/period/fact IDs; exact fact type; fact runtime semantic hash; `source_ordinal=1..7` per row; `created_at=2026-09-30T10:00:00.0000000Z` | Seven links per Annex row; ordered type then UUID; R08; closed |
| 22 | EJ record / 148 / `ID(electronic-journal-record,o)` | Every field, value, fact object, timestamp, stream, predecessor, semantic hash, and integrity hash is the incorporated v1.2 section-8 ledger; retention start recorded_at, end NULL, privacy universal, correction NULL, canonical true | 148 semantic and integrity hashes remain exact; closed |
| 23 | EJ transition / 148 / `ID(source-transition,o)` | transition ID; exact source object/version/kind/ref/version/request/effective/resulting EJ ID/hash/status from incorporated ledger | One transition/record; closed |
| 24 | Workbook / 19 / `ID(annex-workbook,1)` | ID; revision 1; status committed; Site/fiscal/PHP/year 2026/month 9; profile/calculation profile/hash; generator `pos-server-annex-e1-offline:v1.3`; template hash exact current approved template hash; source and calculation hashes from ordered rows; `artifact_sha256=case-package digest`; `artifact_length=case-package preimage length`; MIME `application/vnd.exitpass.annex-e1.synthetic-package`; filename `DS-AE1-NNN.annex-e1.bin`; storage key NULL; created `2026-09-30T10:00:00.0000000Z`; created_by H10; supersedes/correction NULL; correlation case | Deterministic envelope, not workbook generation; `HASH`; closed |
| 25 | Workbook request / 31 / `ID(annex-generation-request,o)` | operation key; Site/fiscal/PHP/year/month/profile/calculation profile/hash/template hash/renderer; H object; ordered period source objects with Z/BIR IDs/runtime hashes, date, ordered fact IDs/hashes; generated_by H10; correlation case; supersedes/correction NULL | One/case plus second attempts in 012/013/014/016/024 and seven additional in 021; current request hash; closed |
| 26 | Replay / 2 / `ID(replay-record,1)` cases 012/014 | ID; original operation/request/workbook IDs; original semantic hash; original case-package digest/length; replay at original request +1s; `result=exact_replay`; mutation_count 0 | Reuses identical bytes; evidence hash; closed |
| 27 | Conflict / 2 / `ID(conflict-record,1)` cases 013/021 | ID; reused operation; original semantic hash; new hash from same framed request with only `calendarMonth=10`; detected original request +2s; `result=conflict`; mutation_count 0; reason `SEMANTIC_HASH_MISMATCH` | No authoritative mutation; evidence hash; closed |
| 28 | Recovery / 2 / `ID(recovery-record,1)` cases 014/024 | ID; case/operation; boundary `restart_readback` case14 or `precommit_orphan_inaccessible` case24; observed request/workbook/hash refs exact for 14, all NULL for inaccessible 24; result `recovered_existing` or `retry_committed`; mutation count 0/1; cleanup evidence `NO_ORPHAN`/`INACCESSIBLE_ORPHAN_NOT_ADOPTED`; recorded original request +3s | Exact safe outcome; evidence hash; closed |
| 29 | Audit / 186 / `ID(audit-record,o)` | ID; action `accounting_fact_created` for first 155, `annex_generation_committed` next 19, then the 12 access actions in section 5; result allowed except specified denied/integrity_failed; actor/service/correlation; exact Site/source identity; event time AUDITAT; source semantic/content hashes; safe reason from registry; privacy universal; created_at=event time | Append-only evidence; no raw payload/PII/secret; evidence hash; closed |

## 5. Request and audit registries

The 346 operation requests are exactly: 67 document creates, 15 voids, 22 X, 22 Z, 22 BIR, 155 Accounting facts, 31 Annex attempts, and 12 access attempts. Within each case they are ordered document creates, optional void, period-ordered X/Z/BIR, Accounting facts, Annex attempts, then access attempts. Each request UUID uses `ID(operation-request,o)` and fields are operation key, exact Site/fiscal/period or year/month scope, profile/version, actor, service, case correlation, and exact source IDs. No authority override exists.

The 31 Annex attempts are one per case; a second in 012, 013, 014, 016, and 024; and seven additional attempts in 021. The 12 access attempts are: 001 deterministic export; 011 integrity verify; 014 restart read; 016 missing and tampered download; 018 metadata read and download; 019 wrong-scope metadata read and download; 023 privacy metadata inspect; 024 orphan lookup and post-retry metadata read.

The 186 audit rows are the 155 fact-created audits, 19 workbook-created audits, and those 12 access audits. Access outcomes are: allowed for 001, 011, 014, both 018, 023, and post-retry 024; denied `NOT_FOUND` and `INTEGRITY_MISMATCH` for 016; denied `SCOPE_MISMATCH` for both 019; denied `ORPHAN_INACCESSIBLE` for first 024. Each safe reason is that exact uppercase token.

## 6. Controlled-code identities

Offline controlled-code UUIDs remain evidence-only: cash `41fd35b9-07e4-5d54-a74a-c70119be6feb`, digital wallet `66a4e913-973b-5a42-8666-30b46b56db10`, VATable `bdd3f1e0-975a-579a-994a-723066e2b627`, statutory peer `ace40856-2b26-5145-9fea-3ed9d419b058`, coupon `5b4810b7-a43e-5c71-a2c8-b00d85d59e86`, final payable `2b476905-6209-59e2-ae0f-8f78b97e10b0`. They do not create catalog rows. Runtime comparison resolves the active set/key and recomputes runtime hashes.

## 7. Closure result

All 29 families are `CLOSED_V1_3`: seven preserved closures plus 22 corrected closures. Every material value is literal or derives from fixed inputs above. No unresolved external decision, runtime clock, artifact generation, loader choice, environment value, or database value is an input.
