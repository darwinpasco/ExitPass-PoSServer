# ExitPass POS Server Repository Boundary

## Purpose

This repository contains the ExitPass POS Server system boundary and its future implementation lifecycle.

## Belongs In ExitPass-PoSServer

- POS Server implementation.
- POS Server API contract and API versioning lifecycle.
- POS Server database design and database artifacts.
- Fiscal issuance logic for Sales Invoice and related fiscal documents.
- Fiscal records, counters, Grand Total Amount, X-read, Z-read, BIR Sales Summary, Annex E, EJ, POSLog, exports, reprints, adjustments, audit, retention, and recovery continuity.
- POS Server tests, deployment assets, CI/CD, engineering pack, operational runbooks, and BIR/accreditation package support.

## Remains In Main ExitPass Repository

- Central PMS.
- WebPay and payment orchestration ownership.
- Parking session control state.
- Site resolution.
- PaymentAttempt and PaymentConfirmation authority.
- Payment finality authority.
- ExitAuthorization authority.
- Operator Console and non-POS platform features unless explicitly split later.

## Integration Boundary With Central PMS

Central PMS sends verified payment finality context to the resolved Site POS Server. POS Server issues the Sales Invoice and returns fiscal document identity/status and digital Sales Invoice URL where applicable. Central PMS records the fiscal reference and remains the only authority that issues ExitAuthorization.

POS Server APIs and events must not issue, approve, mutate, imply, or bypass ExitAuthorization. POS/fiscal events are audit, integration, and observability signals only.

## Database Ownership Boundary

POS Server owns its fiscal database design and future fiscal database artifacts. Central PMS remains owner of parking session, payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization data.

POS Server may store references to Central PMS authority records, but those references do not transfer authority to POS Server.

## API Contract Ownership

POS Server owns the POS Server API Contract and API versioning lifecycle. Central PMS integrations must follow the approved contract and preserve the authority model.

## State-Based Database Versioning Posture

Future POS Server database work should use repository-owned state as the source of truth for repeatable rebuilds and drift checks. Database objects should be represented per object where practical. Local database drift must not become the baseline without explicit review and repository updates.

## Non-Negotiable Authority Rules

- Central PMS owns parking session control state.
- Central PMS owns site resolution.
- Central PMS owns payment finality.
- Central PMS owns PaymentAttempt and PaymentConfirmation.
- Central PMS owns ExitAuthorization.
- POS Server does not issue ExitAuthorization.
- POS Server does not declare platform payment finality.
- Payment Orchestrator and WebPay do not declare platform payment finality.
- POS Server owns fiscal issuance and fiscal records for the resolved Site.
- Channels/terminals are children of the Site POS Server.
- Vendor PMS / HikCentral acknowledgment is synchronization only.
