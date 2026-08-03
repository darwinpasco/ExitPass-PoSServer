# ExitPass POS Server Report API, Rendering, and Print Review v1.0

## 1. API Inventory

No report-related endpoint is mapped in production code. The API contract contains provisional families only: `/v1/pos/reports/xz/*`, `/v1/pos/reports/bir/*`, `/v1/pos/exports/*`, and `/v1/pos/reports/exports/*`.

| Expected operation | Route/method today | DTO/authorization/idempotency | Status |
| --- | --- | --- | --- |
| Generate X | None | None | NOT_IMPLEMENTED |
| Close/generate Z | None | None | NOT_IMPLEMENTED |
| Read report by ID | None | None | NOT_IMPLEMENTED |
| List report history | None | None | NOT_IMPLEMENTED |
| Read BIR summary | None | None | NOT_IMPLEMENTED |
| Generate/read Annex E | None | None | NOT_IMPLEMENTED |
| Query/export EJ | None | None | NOT_IMPLEMENTED |
| Generate/read export | None | None | NOT_IMPLEMENTED |
| Read presentation | None | None | NOT_IMPLEMENTED |
| Request report reprint | None | Existing reprint schema targets fiscal documents only | NOT_IMPLEMENTED |

No request/response contract, Site POS Server scoping rule, correlation behavior, safe error taxonomy, retryability classification, support reference, transaction boundary, or OpenAPI artifact is frozen for these operations.

## 2. Required API Posture

- Separate read-only X generation from privileged mutating Z close.
- Use server-derived authorization and explicit Site POS Server scope.
- Require idempotency for Z close and stateful export generation; define deterministic X semantics.
- Return committed report IDs/status/support references after durable outcomes.
- Preserve correlation IDs under existing conventions without making them semantic identity.
- Use safe 400/401/403/404/409/422/503/unknown-outcome classifications consistent with repository conventions.
- Never return SQL errors, constraint names, hashes, credentials, internal paths, or personal evidence.
- Add versioned machine-readable contracts and focused API/integration tests.

## 3. Rendering and Print Verdict

| Output | JSON presentation | HTML/print | PDF/CSV/text | Physical proof | Verdict |
| --- | --- | --- | --- | --- | --- |
| X Reading | None | None | None | None | NOT_IMPLEMENTED |
| Z Reading | None | None | None | None | NOT_IMPLEMENTED |
| BIR summary | None | None | None | None | NOT_IMPLEMENTED |
| Annex E | None | Not established as a print document | None | None | NOT_IMPLEMENTED |
| EJ/export manifest | None | None | None | None | NOT_IMPLEMENTED |

Digital Sales Invoice presentation and printing posture cannot be reused as proof of reporting. There is no report render model, template, paper-width behavior, totals alignment, page layout, report labels, original/duplicate/reprint marker, signature posture, or printer integration.

## 4. Presentation Decisions Required

- Authoritative JSON presentation field set and version.
- Report labels, Site POS Server identity, business date/window, generated timestamp, fiscal-number range, report sequence, reset counter, and GTA display.
- Customer/operator-visible versus internal reconciliation fields.
- Original/duplicate/reprint semantics and durable print history.
- Paper width and pagination independent of stored fiscal facts.
- Approved PDF/CSV/text formats, encoding, filenames, and schema version.
- Whether signature/approval fields are legally required.

Rendering must consume immutable report snapshots without recalculation. Reprint must reproduce the governed original facts while adding only approved reprint metadata.

