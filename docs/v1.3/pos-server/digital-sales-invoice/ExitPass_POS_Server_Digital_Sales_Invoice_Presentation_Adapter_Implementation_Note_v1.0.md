# ExitPass POS Server Digital Sales Invoice Presentation Adapter Implementation Note v1.0

## Purpose

This slice adds a read-only JSON presentation adapter for the existing Digital Sales Invoice render payload and `digital-sales-invoice-json-v1` template contract.

## What Was Added

- Runtime presentation model: `DigitalSalesInvoicePresentationModel` with ordered sections, rows, notices, value kinds, display values, raw values, and posture metadata.
- Runtime adapter: `DigitalSalesInvoicePresentationAdapter`.
- API response and endpoint mapping for presentation JSON.
- Route registration for the presentation endpoint.
- Focused runtime and API tests for assigned, not-assigned, ordering, deferred placeholders, fail-closed behavior, and boundary behavior.

## Presentation Version

- `presentationVersion`: `digital-sales-invoice-presentation-json-v1`
- `sourceTemplateContractVersion`: `digital-sales-invoice-json-v1`
- `fiscalTemplateFamily`: `PH_DIGITAL_SALES_INVOICE`
- `renderFormat`: `application/json`

## Endpoint Behavior

Endpoint:

- `GET /v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation`

Behavior:

- Reuses the existing Digital Sales Invoice render service.
- Reuses the existing Digital Sales Invoice template contract.
- Maps the render payload and template contract into display-ready JSON sections and rows.
- Does not reload persistence from the adapter.
- Does not mutate fiscal documents.
- Does not allocate fiscal numbers.
- Does not invent missing fiscal values.
- Returns `404` when the fiscal document does not exist.
- Returns JSON only.

## Sections And Rows Produced

The presentation output uses deterministic section ordering:

- `header`
- `sellerSitePosIdentity`
- `documentIdentity`
- `fiscalNumbering`
- `parkingPaymentReferences`
- `lineItems`
- `discounts`
- `taxes`
- `tenders`
- `totals`
- `auditHashStatus`
- `footerDisclaimers`
- `deferredPlaceholders`

Rows include a stable key, label, value kind, posture, safe display value, and raw value where useful. Value kinds include `text`, `amount`, `dateTime`, `identifier`, `status`, `placeholder`, and `deferred`.

Money rows include the existing raw minor-unit value and a simple deterministic display value such as `PHP 125.00`. Timestamp rows use ISO-8601 UTC formatting. IDs remain visible for support and debugging.

## Numbering Behavior

Assigned fiscal documents expose assigned fiscal numbering values from the existing read model. Not-assigned fiscal documents remain renderable but include a `fiscal_number_not_assigned` warning notice and mark unavailable numbering values as `not_available`.

## Deferred And Placeholder Behavior

Footer/disclaimer rows are marked as `placeholder`. QR, PDF, HTML, Digital SI URL, and final statutory template text are represented only as `deferred` placeholder rows. No generated PDF, HTML, QR content, Digital SI URL storage, access events, or final BIR statutory text are produced.

## Intentionally Not Implemented

This slice did not implement PDF generation, HTML rendering, QR code rendering, Digital SI URL storage, Digital SI access events, final BIR statutory template text, BIR X/Z reports, Annex E reports, fiscal numbering changes, idempotency changes, fiscal document creation changes, Central PMS changes, payment finality ownership, gate behavior, ExitAuthorization behavior, refund authority, or reversal authority.

## Validation Results

- `dotnet test tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj`: passed, 87 tests.
- `dotnet test tests\ExitPass.PosServer.Api.Tests\ExitPass.PosServer.Api.Tests.csproj`: passed, 83 tests.
- `git diff --check`: passed.
- `git status --short --untracked-files=all`: showed only the files changed or added for this presentation adapter slice.

## Recommended Next Step

Use a follow-up branch for a UI/print preview consumer of `digital-sales-invoice-presentation-json-v1`, keeping PDF/HTML/QR generation separate until statutory wording, QR rules, storage, and access-control requirements are approved.
