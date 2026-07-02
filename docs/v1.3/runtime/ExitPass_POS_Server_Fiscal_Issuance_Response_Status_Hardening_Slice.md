# ExitPass POS Server Fiscal Issuance Response Status Hardening Slice

## Purpose

This slice hardens `POST /v1/fiscal-documents/` and `GET /v1/fiscal-documents/{fiscalDocumentId}` response semantics after fiscal sequence allocation.

The goal is to let Central PMS distinguish a newly created fiscal document from an idempotent replay, verify that fiscal numbering was assigned, and record POS Server fiscal issuance evidence without treating POS Server as payment, exit, entitlement, or gate authority.

## Response Changes

`POST /v1/fiscal-documents/` success responses now include:

- `resultClassification`
  - `newly_created`
  - `idempotent_replay`
- `fiscalIssuanceEvidenceStatus`
  - `fiscal_document_number_assigned`
- `fiscalNumberAssignmentState`
  - `assigned`
- `fiscalDocumentStatusCodeId`
- fiscal identity and fiscal numbering fields already returned by the sequence allocation slice

`GET /v1/fiscal-documents/{fiscalDocumentId}` now derives:

- `fiscalIssuanceEvidenceStatus`
- `fiscalNumberAssignmentState`
- `fiscalDocumentStatusCodeId`

from the persisted read model.

## Replay Behavior

Duplicate requests with the same idempotency key and semantic request hash return `resultClassification = idempotent_replay`, the original `fiscalDocumentId`, and the original fiscal numbering fields. Replay does not advance sequence state.

## Failure and Retry Posture

Creation failures now include a conservative `errorPosture` where useful:

- `do_not_retry_without_request_change`
- `retry_after_configuration_correction`
- `retry_after_service_recovery`

Conflict responses do not include fiscal numbering evidence. If persistence ever reports success without complete fiscal numbering evidence, the API fails closed with `fiscal_number_assignment_incomplete` instead of returning a misleading success.

## Central PMS Interpretation

Central PMS may record `fiscal_document_number_assigned` as POS Server fiscal issuance evidence for the returned fiscal document identity and number.

This status means only that POS Server created or replayed a persisted, numbered fiscal document record. It does not mean payment finality, ExitAuthorization, gate permission, entitlement approval, manual release approval, continuity activation, BIR report finality, X/Z finality, Annex E finality, Digital SI issuance, or recovery completion.

## Deferred Items

This slice does not implement Digital SI, X-read, Z-read, BIR Sales Summary, Annex E, Electronic Journal, POSLog, reprints, adjustments, reset counter mechanics, Z-counter mechanics, GTA mechanics, recovery automation, Central PMS integration, ExitAuthorization, gate behavior, statutory discount validation, payment finality ownership, refund/reversal authority, or manual release approval.

## Authority Boundary

POS Server remains fiscal issuance authority only. Central PMS remains authority for payment finality, fiscal reference recording, degraded resolve, and ExitAuthorization. Gate/exit execution remains outside POS Server.
