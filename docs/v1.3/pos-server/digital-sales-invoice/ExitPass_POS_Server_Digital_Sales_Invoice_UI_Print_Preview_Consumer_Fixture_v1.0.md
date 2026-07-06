# ExitPass POS Server Digital Sales Invoice UI / Print Preview Consumer Fixture v1.0

## Purpose

This is a consumer fixture for UI and print-preview experiments against the POS Server Digital Sales Invoice presentation JSON. It is not a production UI, not a print engine, and not a statutory template.

## Fixture Inputs

Sample files:

- `docs/v1.3/pos-server/digital-sales-invoice/fixtures/digital-sales-invoice-presentation-assigned-sample-v1.json`
- `docs/v1.3/pos-server/digital-sales-invoice/fixtures/digital-sales-invoice-presentation-not-assigned-sample-v1.json`

Input contract:

- `presentationVersion`: `digital-sales-invoice-presentation-json-v1`
- `sourceTemplateContractVersion`: `digital-sales-invoice-json-v1`
- `fiscalTemplateFamily`: `PH_DIGITAL_SALES_INVOICE`
- `renderFormat`: `application/json`

The sample files are endpoint-shaped JSON responses containing `templateContract` and `presentation`. A consumer should read the `presentation` object for display ordering and row metadata.

## Reading Ordered Sections

Consumers should render `presentation.sections` in array order or by ascending `sortOrder`. The fixture uses these section names:

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

Do not infer new fiscal meaning from section labels. Labels are display hints only; row keys are the stable support/debug identifiers.

## Reading Rows

Each row exposes:

- `key`: stable field or repeated-row path for support/debug.
- `label`: display label suitable for a simple preview.
- `valueKind`: display hint such as `text`, `amount`, `dateTime`, `identifier`, `status`, `placeholder`, or `deferred`.
- `posture`: field posture such as `required`, `optional`, `not_available`, `placeholder`, or `deferred`.
- `displayValue`: safe deterministic text when available.
- `rawValue`: source value when useful for support/debug.

A simple preview should prefer `displayValue` for human-readable output and keep `rawValue` available for diagnostics. When `displayValue` is null, show a quiet unavailable marker only if the row is useful to the operator.

## Not-Assigned Fiscal Number State

When `presentation.numberingState` is `not_assigned`, consumers should show the `fiscal_number_not_assigned` notice clearly. The not-assigned fixture marks unavailable fiscal number rows as `not_available`.

The preview must not imply that an official fiscal number has been issued when numbering is not assigned.

## Placeholder And Deferred Fields

Rows with `posture` equal to `placeholder` are safe placeholders only. They are not final statutory wording.

Rows with `posture` equal to `deferred` must not be rendered as generated artifacts. In this fixture, QR code, PDF document, HTML document, and final BIR template text are deferred markers only.

## What Must Not Be Shown As Final BIR Text

Footer disclaimer placeholder rows must not be displayed as final BIR-approved text. They exist only to preserve the shape expected by future UI/print/PDF work.

## What Must Not Be Treated As Generated Output

The deferred placeholder rows must not be treated as:

- generated PDF content;
- generated HTML content;
- rendered QR code content;
- Digital SI URL storage;
- access event evidence;
- final statutory template text.

## Optional Local Preview Helper

A disposable helper script is available at:

- `tmp/manual-smoke/pos-server-digital-sales-invoice-preview-fixture/Render-DigitalSalesInvoicePreviewText.ps1`

The script reads a sample JSON file and prints a plain-text preview to the console. It does not call the API, does not generate PDF or HTML, does not render QR codes, and does not require external packages.
