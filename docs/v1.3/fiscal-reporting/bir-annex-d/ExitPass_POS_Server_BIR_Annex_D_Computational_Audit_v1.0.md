# ExitPass POS Server BIR Annex D Computational Audit v1.0

## Source chain

The governed source chain is:

`fiscal document -> active invoice lines -> tax details -> discount details / applied statutory facts -> fiscal tenders -> checked X/Z aggregation -> immutable X/Z snapshot and child breakdowns -> versioned presentation -> immutable artifact revision`

The presentation layer does not recalculate tax, discounts, tenders, GTA, counters, or fiscal ranges.

## Direct source-PDF evidence

All four supplied PDFs were read and rendered directly:

| Evidence | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:\XZ_Compare\RMO 24-2023 Annex D-1_Sample X-Reading.pdf` | 252,997 | `c0003de379938f2e3bd70b2f163ed9b281f625d0333aeb2913d06b837cc50584` |
| `D:\XZ_Compare\RMO 24-2023 Annex D-2_Sample Z-Reading.pdf` | 418,099 | `43352d92b649d603e3b3d4d2bfb404f310de98a7ad41106a6b1061b4af57f029` |
| `D:\XZ_Compare\x-reading-x-20261021-96dbacd0fdcd40faaf5366fc7c02fa99-57mm.pdf` | 3,625 | `f03e13ad9ac6728410b6f11a29a71f9deaa9d67e5d4d67b1f48c0a187b466a44` |
| `D:\XZ_Compare\z-reading-z-20261020-00000033-492353440f344e5997a3db9ce4f45ad6-57mm.pdf` | 3,645 | `e49822a75e320a7c8c5b67e987168e36e30058f639b71de52ada37f3ce6a35ec` |

The baseline ExitPass PDFs prove the displayed aggregate values, not their invoice-level provenance. No approved operational read-only evidence connection was available, so the exact October 20/21 source-record audit remains `HISTORICAL_INVOICE_LEVEL_RECONCILIATION_NOT_VERIFIED`.

## Per-invoice acceptance

For each active issued or recorded Sales Invoice, the report aggregation now independently verifies:

1. every line uses non-negative PHP integer minor units;
2. `line net = line gross - line discount + line tax` using checked arithmetic;
3. sum of line tax equals sum of authoritative tax-detail tax amounts;
4. payment-finality invoices have at least one tender;
5. zero-payable statutory-finality invoices have governed statutory facts and no tender;
6. tender total equals invoice line net;
7. statutory final amount equals invoice line net;
8. line discount reconciles to governed statutory or commercial discount evidence;
9. one tax classification cannot mix document-level and line-level facts;
10. all source currencies equal the reporting-period currency.

Voided Sales Invoices undergo the same line and tax arithmetic validation. Their line net is accumulated as Void Amount, while their lines, taxes, discounts, and tenders do not enter active sales or collection totals.

## Report amount definitions

| Amount | Source definition | VAT posture |
| --- | --- | --- |
| Gross Sales | Active SI line gross before line discount and line tax | VAT-exclusive line basis under the current fiscal line formula |
| Discounts | Active SI line discount, reconciled to statutory/commercial evidence | Does not include VAT privilege amount unless that amount is also an authoritative line discount |
| VATable Sales | Authoritative `vatable` tax-detail taxable bases | Never recomputed from gross or net |
| VAT Amount | Sum of authoritative tax-detail tax amounts | Invoice/line rounding is preserved |
| VAT Exempt Sales | Authoritative `vat_exempt` tax-detail taxable bases | Separate from VAT privilege adjustment |
| Zero Rated Sales | Authoritative `zero_rated` tax-detail taxable bases | Separate from any future zero-rating adjustment |
| Net Sales | Sum of active SI line net | `gross - discount + tax` per line |
| Total Collected | Sum of authoritative fiscal tenders | Digital tenders are not physical drawer cash |
| Void Amount | Sum of voided SI line net | Not subtracted again from active Gross or Net |
| GTA movement | Current-period Z Net Sales | Existing state-transition rule; printed explicitly as net-based GTA and not claimed to be BIR accumulated sales |

## VAT rounding

VAT is the sum of immutable invoice/line tax-detail amounts. The renderer never applies `12%` to an aggregate taxable base.

The automated boundary fixture uses two invoices with taxable base `4` minor units each and independently rounded VAT `0` each. The report VAT is `0`, while applying 12% to the aggregate base `8` and rounding once would produce `1`. The test proves the authoritative sum is retained.

For the user-supplied October 20 Z values, `PHP 34.30` can be correct when it is the sum of individually rounded tax-detail amounts even though `PHP 285.70 x 12%` rounds to `PHP 34.28`. The original ExitPass Z PDF was directly inspected, but it does not contain invoice-level tax facts. Exact proof therefore requires the immutable source tax details or an approved read-only export from the report's source period. The implementation does not change `34.30` to `34.28` based on aggregate-rate arithmetic.

## Supplied ExitPass arithmetic

### October 21 X

| Relationship | Calculation | Result |
| --- | --- | --- |
| Gross less discounts | `151785 - 41964` | `109821` minor units |
| VATable plus VAT | `109821 + 13179` | `123000` minor units |
| Tenders | `106000 + 17000` | `123000` minor units |

These relationships are arithmetically consistent. Invoice-level proof still requires the committed SI lines, tax details, discounts, and tenders for the exact X observation cutoff.

### October 20 Z

| Relationship | Calculation | Result |
| --- | --- | --- |
| Gross less discounts | `120534 - 91964` | `28570` minor units |
| VATable plus authoritative VAT | `28570 + 3430` | `32000` minor units |
| Tenders | `10000 + 22000` | `32000` minor units |

These relationships are arithmetically consistent. The VAT difference from aggregate-rate arithmetic is classified as an invoice-level rounding question, not a defect, until immutable tax details are inspected.

## Synthetic independent reconciliation

`ExitPass_POS_Server_BIR_Annex_D_Synthetic_Reconciliation_v1.0.csv` contains five active Sales Invoices and one voided Sales Invoice. It covers cash, card, QRPh, GCash, coupon discount, statutory discount, VATable sales, VAT-exempt sales, zero-payable statutory completion, and a void.

Independent totals in PHP minor units:

| Fact | Expected |
| --- | ---: |
| Active Gross Sales | 38,395 |
| Active Discounts | 5,358 |
| Active VAT | 3,106 |
| Active Net Sales | 36,143 |
| Tenders | 36,143 |
| Void Amount | 10,000 |
| Previous GTA | 500,000 |
| Current-period GTA movement | 36,143 |
| Present GTA | 536,143 |
| Z counter transition | 32 -> 33 |
| Reset counter transition | 1 -> 1 |

Identities:

`38,395 - 5,358 + 3,106 = 36,143`

`10,000 + 7,143 + 10,000 + 9,000 = 36,143`

`500,000 + 36,143 = 536,143`

All CSV variances are zero.

## Accumulated-sales disposition

The repository proves that the current ExitPass GTA transition is net-based:

`present GTA = previous GTA + current-period Z Net Sales`

`PostgresFiscalZReadingRepository` assigns `currentGta` from `NetSalesAmountMinorUnits`, persists that value in the immutable Z counter snapshot, and rejects a mismatch. This proves ExitPass GTA continuity only. It does not prove that BIR Annex D-2 defines Present Accumulated Sales, Previous Accumulated Sales, and Sales for the Day on the same net basis. The supplied Annex D-2 sample visually aligns its accumulated movement with displayed gross amount.

The corrected receipt therefore prints `GRAND TOTAL AMOUNT (GTA)`, `Previous GTA`, `Period Net GTA`, and `Present GTA`. It does not label those values as BIR accumulated sales. Mapping BIR accumulated sales to gross, net, or another statutory basis remains `EXAMINER_CLARIFICATION`; no counter, GTA value, historical Z reading, or state transition is changed.

## Cashier accountability disposition

The current POS X Reading is a site/fiscal-period interim report. It has no authoritative opening fund, safe-drop/withdrawal, direct-remittance, actual blind count, or cashier custody owner facts. Consequently:

- Cash In Drawer is not derived from SI cash tenders.
- Card, QRPh, GCash, and Maya never enter physical-cash arithmetic.
- Short/Over is not printed.
- A cashier-specific supplemental report requires an approved immutable APT custody integration and a BIR scope decision.

Stage 2B custody arithmetic and blind-count controls are unchanged.

## Historical compatibility

New snapshots carry `pos-server-fiscal-reporting-annex-d-source-facts:v2`. Historical rows retain null additive columns and are not updated. Corrected presentation and artifacts use v2 identities. The artifact repository creates a new revision when the source fingerprint or contract changes and retains prior bytes, SHA-256, revision, and supersession history.

## Isolated PostgreSQL proof

Validation used an invocation-owned PostgreSQL 16 container and database named `posserver_bir_xz_completion_003_validation`. The container was loopback-bound, used no persistent volume, and was not connected to any operational network or database.

The complete canonical schema was applied before exercising real repository writes. The proof established:

1. X generation persisted and read back `snapshot_detail_version`, five nullable document-count columns, `last_z_counter_value`, and `last_z_closed_at`.
2. Manual Z close persisted the same detail contract, preserved counter/GTA continuity, and allowed a subsequent X to capture the committed prior Z counter and close time.
3. Historical rows with all eight additive columns null remained readable.
4. `ck_x_z_reports__counts` accepted valid v2 rows and rejected invalid detail versions, count reconciliation, partial prior-Z pairs, non-X prior-Z facts, and future prior-Z close times.
5. Fiscal report artifacts remained byte-identical across readback and restart; successor, supersession, concurrency, and immutable-update protections passed.
6. A simulated pre-v2 table was reconciled by applying `db/state/tables/pos.x_z_reports.sql` twice. Both runs succeeded, the eight columns existed, the final constraint matched canonical state, and all three protected historical rows retained the same fingerprint: `3|9d541989b104f588d717432ba6253aa4`.

The database-backed result was X `2/2`, Z `5/5`, output readback `1/1`, and artifact immutability `2/2` passing.

## Deployment reconciliation procedure

Changing the canonical CREATE TABLE file does not update an existing database by itself. Deployment requires a separately authorized schema reconciliation. The reviewed procedure is:

1. Take and hash a database backup.
2. Record the target `pos.x_z_reports` row count, immutable row fingerprint, existing column inventory, and `pg_get_constraintdef` value for `ck_x_z_reports__counts`.
3. Confirm the target is the intended non-production or separately authorized deployment database; never substitute `exitpass_pos_ist` without that authorization.
4. Apply only `db/state/tables/pos.x_z_reports.sql` through `psql --single-transaction --set ON_ERROR_STOP=1 --file db/state/tables/pos.x_z_reports.sql` using the approved connection path.
5. Re-run the same command to prove idempotency.
6. Verify the eight additive columns, the exact canonical constraint, unchanged report row count and fingerprint, null additive fields on historical rows, and immutable UPDATE/DELETE rejection.
7. Run Inventory, Drift, X/Z PostgreSQL integration, artifact readback, and readiness checks before deploying source.

The reconciliation performs no report-row DML. Rollback is restoration of the verified backup into a separately named database followed by an explicit cutover decision; dropping additive columns after new writes is not an approved rollback.

## Integration prerequisite result

The isolated Annex E harness was executed, rather than skipped. The original failure was confirmed: `BuildFiscalCommand()` omitted the canonical channel terminal required by current fiscal issuance. The harness now inserts deterministic synthetic terminals owned by each synthetic Site POS Server and supplies the matching ID/reference without changing frozen v1.7 JSONL bytes. A focused negative test proves a foreign synthetic terminal is rejected with `ChannelTerminalMismatch`; the owned/foreign test passed `1/1`.

The complete harness now proceeds beyond terminal validation and fails independently on frozen row `F06|001|0001` with `AppliedStatutoryTotalMismatch`. Frozen v1.7 evidence specifies VAT-exclusive consideration `10000`, statutory discount `2000`, taxable basis `10000`, VAT `1200`, and final payable `9200`. Current runtime validation requires discounted taxable basis `8000` and VAT `960`. The dataset validator, corruption rejection, missing-reference rejection, and cleanup checks pass, but runtime execution does not. Frozen dataset bytes and financial commitments were not altered, and production terminal validation was not weakened. This is a separate Annex E dataset/current-runtime statutory-accounting contract regression, not an X/Z source-correction defect.

## Validation ledger

| Validation | Result |
| --- | --- |
| Runtime tests | `352/352 PASS` |
| Persistence tests | `94/94 PASS` |
| API tests | `199/199 PASS` |
| PostgreSQL X generation/readback | `2/2 PASS` |
| PostgreSQL Z close/readback | `5/5 PASS` |
| Output determinism/restart | `1/1 PASS` |
| Artifact immutability/successors | `2/2 PASS` |
| Canonical database checks | `PASS` |
| v2 reconciliation applied twice | `PASS`; protected fingerprint unchanged |
| Annex E owned/foreign terminal compatibility | `1/1 PASS` |
| Annex E full isolated execution | `FAIL` at frozen `F06\|001\|0001` statutory taxable-basis/VAT mismatch after terminal validation succeeds |
| Release build | `PASS`; zero warnings, zero errors |
| Synthetic financial reconciliation | `PASS`; all variances zero |
| Corrected 57 mm PDF review | `PASS`; exact width, no clipping or overlap |

## Remaining computational gaps

| Gap | Required owner / evidence |
| --- | --- |
| Exact October 21 X invoice-level proof | POS immutable document, line, tax, discount, and tender export for the X observation cutoff |
| Exact October 20 Z VAT-rounding proof | POS immutable invoice-level tax details for that closed period |
| Cancellation, return, and refund amounts/counts | Governed POS fiscal adjustment contracts and immutable facts |
| SC/PWD category-specific VAT adjustments | Additive immutable attribution facts; aggregate privilege evidence cannot be estimated or split |
| NAAC and Solo Parent | Approved statutory entitlement model and source evidence |
| Opening fund, withdrawal, drawer cash, actual count, short/over | Approved APT cash-custody integration and scope ownership |
| D-1 site X versus cashier accountability | BIR examiner clarification |
| D-2 accumulated-sales accounting basis | BIR examiner confirmation of gross, net, or other statutory accumulation basis; existing ExitPass GTA is proven net-based only |
| Human cashier identity | Governed human-session identity contract; `requested_by_ref` is only an authenticated request principal and is printed as Request Actor |
| Annex E v1.7 statutory fixture/runtime drift | Annex E package owner and fiscal-accounting owner must reconcile frozen F06 taxable-basis/VAT semantics with current runtime; X/Z source changes must not rewrite the frozen dataset |
