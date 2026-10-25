# BIR Annex E-2 and E-3 Sales Books

## Authority

The implementation is based on BIR Revenue Memorandum Order No. 24-2023 and the official workbook `RMO 24-2023 Annex E-1 to E-5.xlsx`.

Reference workbook SHA-256:

`DE8B26453F50C5E54E17BD188F51F9FEEC1F76E4C2EEFC50004B5622CBBFDCF4`

## Official field matrix

Both reports use the official eleven-column geometry in columns A through K.

| Column | Annex E-2 | Annex E-3 | ExitPass source |
| --- | --- | --- | --- |
| A | Date | Date | Committed Sales Invoice fiscal business date |
| B | Name of Senior Citizen | Name of Person with Disability | Encrypted beneficiary snapshot committed with the Sales Invoice |
| C | OSCA/SC ID No. | PWD ID No. | Encrypted beneficiary snapshot committed with the Sales Invoice |
| D | SC TIN | PWD TIN | Encrypted beneficiary snapshot, when supplied |
| E | SI/OR Number | SI / OR Number | Committed Sales Invoice number |
| F | Sales Inclusive of VAT | Sales Inclusive of VAT | Committed applied statutory fiscal facts |
| G | VAT Amount | VAT Amount | Committed fiscal tax details |
| H | VAT Exempt Sales | VAT Exempt Sales | Committed fiscal tax details; no inferred exemption |
| I | Discount 5% | Discount 5% | Separately proven 500-basis-point LGU component |
| J | Discount 20% | Discount 20% | Separately proven 2000-basis-point LGU component |
| K | Net Sales | Net Sales | Committed final payable amount |

The header includes taxpayer name, registered address, TIN, software name/version and release, POS serial number, MIN, POS terminal, generation timestamp, and requesting user.

## Titles and layout

- Annex E-2: `Senior Citizen Sales Book/Report`
- Annex E-3: `Persons with Disability Sales Book/Report`
- Worksheet names: `E-2` and `E-3`
- Print area: `A1:K25`
- Paper: A4
- Output: one-page-wide landscape layout for readable print and PDF export

The source workbook's E-3 page setup is portrait and separates the eleven prescribed columns across multiple printed pages. ExitPass preserves the official cell geometry, headings, merged cells, colors, and column order, but normalizes the export to A4 landscape and one page wide so a row remains readable as one statutory record.

Governance status: `CONTROLLED_PRESENTATION_DEVIATION_PENDING_BIR_EXAMINER_CONFIRMATION`. This marker records the pending confirmation of the E-3 page orientation only; it does not assert BIR approval or alter any prescribed field, value, total, or column order.

## Qualification boundary

A committed Sales Invoice is included only when all of the following are true:

1. Its entitlement is `SENIOR_CITIZEN` for E-2 or `PWD` for E-3.
2. The policy resolution basis is `LOCAL_ORDINANCE`.
3. The approved percentage is exactly 5% or 20%.
4. The percentage component is separately proven in committed fiscal evidence.
5. The percentage discount amount is positive.
6. A finalized beneficiary identity snapshot is attached to the committed Sales Invoice.

Free parking, first-three-hours-free, full-fee exemptions, capped parking, and reduced-rate privileges are excluded unless committed fiscal evidence separately proves an applicable 5% or 20% LGU percentage component. ExitPass never infers a percentage by dividing a concession amount by the original fee.

## Status and empty reports

`FINAL` requires every scoped fiscal period to be closed and to have a committed Z Reading. All other valid results are `PRELIMINARY`.

A valid empty report retains the complete official header and column structure, contains no beneficiary rows, and reports zero totals. It is distinct from a generation or reconciliation failure.

## Reconciliation and immutability

- Rows come only from committed, active, non-voided Sales Invoices.
- Beneficiary identity is copied into an encrypted, immutable POS fiscal snapshot at issuance.
- Percentage, tax, discount, and payable values come from committed fiscal facts.
- Duplicate Sales Invoice rows and invalid monetary relationships fail report generation.
- Report reads and exports run in a read-only repeatable-read transaction.
- XLSX and PDF exports are persisted through the fiscal artifact catalog with a stable source fingerprint and SHA-256 hash.
- Reprint or repeated retrieval does not create another report transaction or another sales-book row.

## Authorization and privacy

Read access requires `fiscal_statutory_sales_book.read`. Export requires `fiscal_statutory_sales_book.export`. Site POS Server, fiscal identity, currency, and terminal scope must match the authenticated service identity.

Beneficiary fields are encrypted at rest in Central PMS and POS Server. Raw identity photographs are not transferred to POS Server and no OCR-derived identity fields are used. Missing mandatory beneficiary identity produces an explicit readiness failure rather than an incomplete report row.
