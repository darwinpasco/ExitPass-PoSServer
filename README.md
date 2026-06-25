# ExitPass POS Server

This repository is for the ExitPass POS Server system.

ExitPass POS Server is a separate system from the main ExitPass platform. It owns fiscal issuance and fiscal records for the resolved Site under the approved Site-level POS Server model.

## System Boundary

POS Server owns:

- Sales Invoice lifecycle and fiscal issuance.
- Fiscal records and canonical fiscal facts.
- Fiscal document numbering, counters, Grand Total Amount, and recovery continuity.
- X-read, Z-read, BIR Sales Summary, Annex E reporting, Electronic Journal, POSLog, exports, reprints, adjustments, audit, and retention.
- POS Server database design and future database artifacts.
- POS Server API lifecycle, implementation, tests, deployment, and engineering pack.

The main ExitPass platform / Central PMS owns:

- Parking session control state.
- Site resolution.
- Payment finality.
- PaymentAttempt and PaymentConfirmation.
- ExitAuthorization.

POS Server integrates with Central PMS through approved API contracts. POS Server does not issue ExitAuthorization and does not declare platform payment finality.

## Current Repository Content

Current content is a documentation baseline copied from the main ExitPass repository for POS/Invoicing, POS Server System Design, POS Server API Contract, and POS Server Database Design planning.

Future content is expected to include POS Server implementation, database artifacts, tests, deployment assets, CI/CD configuration, and engineering pack materials.
