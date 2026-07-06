# ExitPass POS Server Digital Sales Invoice Rendering Foundation Implementation Note v1.0

## Purpose

This slice adds a read-only JSON rendering foundation for Digital Sales Invoice / receipt presentation from an existing POS Server fiscal document read model.

## What Was Added

- Runtime render model: `DigitalSalesInvoiceRenderModel` and child line, discount, tax, tender, total, and footer models.
- Runtime service: `DigitalSalesInvoiceRenderService`.
- API response and endpoint mapping for JSON rendering.
- Focused runtime and API tests for assigned, unassigned, missing, and boundary behavior.

## Endpoint And Service Behavior

Endpoint:

- `GET /v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice`

Behavior:

- Loads the existing fiscal document read model.
- Maps readback data into a JSON render model.
- Does not mutate the fiscal document.
- Does not allocate fiscal numbers.
- Returns `404` when the fiscal document does not exist.
- Marks fiscal number state as `assigned` only when the existing read model has complete numbering evidence.
- Marks fiscal number state as `not_assigned` when numbering evidence is incomplete or absent.

## Fields Rendered

The render model includes:

- fiscal document id;
- Site POS Server id, channel terminal id, and fiscal identity id;
- fiscal document type/status ids;
- fiscal sequence policy id, sequence value, document number, series, prefix, suffix, assigned timestamp, and assigned-by reference;
- business day;
- Central PMS parking session, payment attempt, payment confirmation, payment finality, and vendor acknowledgement references;
- semantic request hash, version, and status;
- fiscal lines;
- discount/privilege details;
- tax details;
- tenders;
- totals;
- placeholder footer/disclaimer text.

## Intentionally Not Implemented

This slice did not implement BIR X/Z reports, Annex E reports, PDF rendering, HTML rendering, final statutory template text, Digital SI URL storage/access events, fiscal number allocation changes, idempotency changes, fiscal document creation semantic changes, Central PMS behavior, payment finality ownership, refund/reversal authority, gate behavior, or ExitAuthorization behavior.

## Validation Results

- `dotnet test tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj`: passed, 80 tests.
- `dotnet test tests\ExitPass.PosServer.Api.Tests\ExitPass.PosServer.Api.Tests.csproj`: passed, 75 tests.
- `git diff --check`: passed.
- `git status --short --untracked-files=all`: showed only the files changed or added for this rendering foundation slice.

## Recommended Next Step

Add a template-design slice for Digital Sales Invoice presentation rules and, separately, a storage/access-control slice for generated Digital SI URLs after compliance and privacy review.
