# ExitPass POS Server Digital Sales Invoice UI / Print Preview Fixture Approval Record v1.0

## Approval Scope

The assigned and not-assigned plain-text preview fixture outputs for the POS Server Digital Sales Invoice UI / print preview consumer fixture were reviewed and approved.

This approval covers fixture structure and preview behavior only. It does not approve final BIR statutory wording, production document generation, production UI behavior, or generated document artifacts.

## Identifier Display Decision

`fiscalDocumentId` is the internal POS Server fiscal document/API record ID.

`fiscalDocumentNumber` is the official customer/business-facing Sales Invoice number.

UI and print consumers should display `fiscalDocumentNumber` as the Sales Invoice No. when it is assigned.

`fiscalDocumentId` should remain available for support, audit, and API lookup, but it should not be presented as the official invoice number.

## Assigned Fixture Approval

The assigned sample correctly demonstrates that an assigned `fiscalDocumentNumber` can be displayed as the Sales Invoice No. while preserving the internal `fiscalDocumentId` for support and audit lookup.

## Not-Assigned Fixture Approval

The not-assigned sample correctly shows the `fiscal_number_not_assigned` warning.

The not-assigned preview must not imply that an official fiscal number or official Sales Invoice number has been issued.

## Placeholder And Deferred Items

Footer and disclaimer text remains placeholder only. It must not be treated as final BIR-approved statutory text.

The following remain deferred:

- QR rendering;
- PDF generation;
- HTML rendering;
- Digital SI URL storage;
- access events;
- final BIR statutory text.

## Boundaries

This approval record does not add features, modify fiscal numbering, modify idempotency, modify fiscal document creation, change Central PMS behavior, add payment/gate/ExitAuthorization/refund/reversal behavior, add BIR X/Z reports, or add Annex E reports.
