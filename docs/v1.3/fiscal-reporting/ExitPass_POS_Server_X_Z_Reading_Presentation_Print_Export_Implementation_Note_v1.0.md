# ExitPass POS Server X/Z Reading Presentation, Print, and Export Implementation Note v1.0

## 1. Scope and baseline

Z-008 adds a read-only fiscal-output projection over the merged Z-006B X Reading and Z-007 Z Reading runtimes. The authoritative source remains the committed immutable `pos.x_z_reports` snapshot plus its governed tender, discount, fiscal-number range, and sequence-gap children. The output layer does not query fiscal documents, lines, taxes, tenders, statutory facts, counter state, or reporting-period state to recompute a report.

No database schema change is required. Existing first-class snapshots contain the required facts. Output bytes and identities are derived deterministically; no export-history or generic JSON table is introduced.

## 2. Pre-change audit

| Area | Baseline | Z-008 decision |
|---|---|---|
| X runtime | Generate and stored read routes existed | Reuse stored read service |
| Z runtime | Atomic close and stored read routes existed | Reuse stored read service |
| Snapshot persistence | Immutable aggregate and child rows existed | No schema change |
| Period/counter/GTA facts | Z readback included closed state and transition snapshot | Project stored values only |
| Digital SI presentation | Structured adapter and invariant formatting existed | Reuse architectural separation, not Sales Invoice fields |
| Print/export | No X/Z renderer or route existed | Add deterministic text, JSON, and CSV |
| Authorization | Separate X read and Z read/close policies existed | Reuse read for presentation; add separate export permissions |
| Audit | Structured privacy-safe endpoint logging existed | Log operation metadata and output identity, never payload |

The Site POS Server identifier is persisted. A separate Central PMS Site identity is not part of the v1 report snapshot, so presentation reports `siteIdentity` as `not_recorded` instead of inferring it.

## 3. Contracts and routes

Machine-readable authority: `contracts/pos-server/fiscal-report-output-api.v1.json`.

| Route | Policy | Permission | Result |
|---|---|---|---|
| `GET /v1/fiscal-reports/x-readings/{fiscalReportReference}/presentation` | `FiscalXReadingRead` | `fiscal_x_reading.read` | X presentation JSON |
| `GET /v1/fiscal-reports/x-readings/{fiscalReportReference}/exports/{format}` | `FiscalXReadingExport` | `fiscal_x_reading.export` | `json`, `csv`, or `text` |
| `GET /v1/fiscal-reports/z-readings/{zReadingReference}/presentation` | `FiscalZReadingRead` | `fiscal_z_reading.read` | Z presentation JSON |
| `GET /v1/fiscal-reports/z-readings/{zReadingReference}/exports/{format}` | `FiscalZReadingExport` | `fiscal_z_reading.export` | `json`, `csv`, or `text` |

X scope is server-derived Site POS Server plus fiscal identity. Z scope also requires server-derived currency. Missing reports and scope mismatches both return the same safe 404 posture. Export permission grants neither X generation nor Z close.

## 4. Presentation contracts

- X: `fiscal-x-reading-presentation-json-v1`
- Z: `fiscal-z-reading-presentation-json-v1`
- canonical JSON: `fiscal-x-z-reading-export-json-v1`
- canonical CSV: `fiscal-x-z-reading-export-csv-v1`
- print text: `fiscal-x-z-reading-print-text-v1`
- output identity: `fiscal-report-output:sha256:v1`

Common output includes stored report/scope/period/time/currency/version facts, qualifying document count, recorded amounts, tender and discount breakdowns, ranges/gaps, correlation/support references, reconciliation posture, and immutability posture.

X is explicitly titled `X READING`, marked `INTERIM_READ_ONLY` and `OPEN_AT_OBSERVATION`, and states that it does not close the period or change counters/GTA. Counter and GTA fields absent from the authoritative X snapshot are classified `not_recorded`.

Z is explicitly titled `Z READING`, marked `IMMUTABLE_CLOSED`, and includes stored reset counter, Z counter, GTA before/contribution/after, expected/resulting state versions, close timestamp, period sequence, and prior period reference where recorded.

## 5. Integrity and source boundary

Before rendering, the service validates the source contract version, report kind, committed status, immutable posture, scope and period facts, timestamps, uppercase currency, non-negative stored report values, child currency/classification, fiscal-range ordering, range count, tender reconciliation, governed discount/VAT-removal reconciliation, and Z counter/GTA transition equations. These checks validate stored presentation integrity only.

The service never recalculates from live fiscal data, repairs a snapshot, changes a value, allocates a counter, or changes a period. Unsupported versions, incomplete snapshots, wrong report kind, and reconciliation failures fail closed with safe classifications.

## 6. Print-ready rendering

The canonical text renderer is UTF-8 fixed-width text without printer control codes:

| Profile | Width | Aliases |
|---|---:|---|
| narrow | 32 characters | `57mm`, `58mm` |
| standard | 48 characters | `80mm` |
| office | 80 characters | `plain` |

Wrapping is deterministic and preserves complete safe references across lines. Timestamps use invariant UTC text. Monetary display uses integer minor units and invariant two-decimal formatting; PHP is rendered as `PHP`, avoiding device-dependent glyph assumptions. Zero and negative recorded values are explicit. Output never adds a reprint marker unless a future separately governed operation requests one.

Physical printer drivers, spooler integration, ESC/POS commands, and PDF are out of scope. The fixed-width contract is the adapter boundary for future printer and PDF work.

## 7. Export and identity

JSON is a versioned structured envelope. CSV is a stable row-oriented contract with `section,key,classification,count,amount_minor_units,currency,value`. Text is inline preview output; JSON and CSV are controlled attachments.

Output identity binds report reference, report kind, source report version, presentation contract version, authoritative snapshot identity, and format/width. The correlation ID is deliberately non-semantic. ETags use a quoted HTTP-safe form of the output identity. Filenames are deterministic and derived from the safe report kind/reference.

Responses set the governed content type, `Content-Disposition`, `Cache-Control: private, no-store`, `Pragma: no-cache`, `X-Content-Type-Options: nosniff`, ETag, output identity, and output contract headers.

## 8. Audit, privacy, and safe errors

Structured logs record report kind, safe report reference, output format, safe output identity, correlation reference, and outcome. Full payloads, raw fiscal data, semantic hash material, customer identity, statutory identity/evidence, reviewer data, credentials, SQL, connection details, and stack traces are excluded.

Safe failures distinguish missing correlation, unsupported format/width, report absence, unsupported source version, not-finalized snapshot, malformed snapshot, and temporary repository unavailability. Persistence failures are not disguised as absence; they return a generic 503 without database details.

## 9. Automated and PostgreSQL proof

Focused unit and API tests cover X/Z distinction, open/closed posture, counters/GTA, deterministic serialization, narrow/standard/office wrapping, invariant amount formatting, malformed/version failure, route metadata, permission separation, anti-enumeration, headers, and safe repository failure.

The disposable PostgreSQL 16 proof rebuilds canonical schema, loads controlled codes and synthetic fiscal fixtures, invokes actual X generation, renders/exports X, initializes Z state, invokes actual atomic Z close, renders/exports Z, repeats output reads, restarts the API, compares every byte, checks wrong-scope and read-only denial, and compares a before/after manifest covering fiscal documents and children, status history, sequence/counter/state rows, periods, report snapshots/children, Z transitions, and reprints. Rendering and export cause no database mutation.

No customer, payment credential, statutory identifier, or evidence data is used in proof fixtures.

## 10. Manual API verification

With a disposable API host and synthetic report references, use an authorized service key and one correlation reference:

```powershell
$headers = @{ 'X-PosServer-Admin-Key' = $env:Z008_API_KEY; 'X-Correlation-Id' = 'z008-manual-proof' }
Invoke-WebRequest "$base/v1/fiscal-reports/x-readings/$xRef/presentation" -Headers $headers
Invoke-WebRequest "$base/v1/fiscal-reports/x-readings/$xRef/exports/text?width=narrow" -Headers $headers
Invoke-WebRequest "$base/v1/fiscal-reports/x-readings/$xRef/exports/json" -Headers $headers
Invoke-WebRequest "$base/v1/fiscal-reports/x-readings/$xRef/exports/csv" -Headers $headers
Invoke-WebRequest "$base/v1/fiscal-reports/z-readings/$zRef/presentation" -Headers $headers
Invoke-WebRequest "$base/v1/fiscal-reports/z-readings/$zRef/exports/text?width=standard" -Headers $headers
Invoke-WebRequest "$base/v1/fiscal-reports/z-readings/$zRef/exports/json" -Headers $headers
Invoke-WebRequest "$base/v1/fiscal-reports/z-readings/$zRef/exports/csv" -Headers $headers
```

Repeat before and after API restart and compare response bytes and ETags. A wrong scoped key receives 404; a read-only key receives 403 on export; `exports/pdf` receives a safe unsupported-format response. Malformed-snapshot behavior is covered by bounded injected repository tests because committed PostgreSQL snapshot constraints and immutability intentionally prevent corrupting proof data.

## 11. Readiness and handoff

Z-008 implements structured X/Z presentation, print-ready fixed-width text, canonical JSON, and canonical CSV. It does not implement physical printing, PDF, BIR-specific accreditation layouts, Annex E, Electronic Journal, or POSLog.

Z-009 may consume the immutable governing Z snapshot and this deterministic output/versioning foundation for Annex E, but must freeze the external compliance profile before generation. Z-010 may reuse output identity, safe headers, scope enforcement, deterministic serialization, and privacy-safe audit conventions for Electronic Journal and POSLog; it must introduce their distinct event chronology and retention contracts rather than treating X/Z exports as those records.

Controlled UAT is not authorized. Production rollout is not authorized.
