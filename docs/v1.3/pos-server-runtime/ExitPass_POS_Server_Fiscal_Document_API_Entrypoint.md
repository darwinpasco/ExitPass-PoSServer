# ExitPass POS Server Fiscal Document API Entrypoint

## 1. Purpose

This note documents the first thin API/application entry point for POS Server fiscal document creation.

The entry point exposes the existing fiscal document creation runtime service through a minimal ASP.NET Core API layer. It does not implement persistence, BIR reporting, Digital SI behavior, Annex E, X/Z reporting, statutory discount entitlement validation, payment finality ownership, or gate/exit behavior.

## 2. Route

The API project maps:

```text
POST /v1/fiscal-documents
```

The route accepts upstream fiscalization input and delegates to `FiscalDocumentCreationService`.

## 3. Added Projects

API project:

- `src/ExitPass.PosServer.Api/ExitPass.PosServer.Api.csproj`

API tests:

- `tests/ExitPass.PosServer.Api.Tests/ExitPass.PosServer.Api.Tests.csproj`

The API project references the existing runtime project and uses ASP.NET Core minimal API conventions.

## 4. Request / Response Mapping

Request DTOs:

- `CreateFiscalDocumentRequest`
- `FiscalizationPayableBasisRequest`
- `FiscalDiscountReferenceRequest`

The entry point maps request DTOs into:

- `FiscalDocumentCreationCommand`
- `FiscalizationPayableBasisInput`
- `FiscalDiscountReferenceInput`

Response DTO:

- `CreateFiscalDocumentResponse`

Runtime errors are mapped to deterministic response codes:

- `missing_payable_basis`
- `missing_upstream_finality_reference`
- `unapproved_discount_reference`
- `sensitive_evidence_payload_not_allowed`
- `unsupported_fiscal_document_request`
- `persistence_not_configured`

## 5. Persistence Posture

Persistence remains abstract.

The API DI registration wires `FiscalDocumentCreationService` and a default `PersistenceNotConfiguredFiscalDocumentRepository`. That repository fails closed with `persistence_not_configured`, so an API host cannot silently pretend fiscal document creation succeeded before persistence is explicitly implemented.

Future persistence work must replace the repository port intentionally and must not change the statutory discount authority boundary.

## 6. Authority Boundary

The entry point follows the locked boundary:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

The entry point:

- accepts upstream payable-basis and finality references
- accepts optional statutory discount validation references for traceability
- delegates statutory discount treatment checks to the runtime service guardrails
- rejects unapproved statutory discount references through the runtime service
- rejects raw ID/evidence payload markers through the runtime service

The entry point does not:

- expose statutory entitlement approval fields
- accept raw ID image/evidence payloads
- decide Senior/PWD eligibility
- decide local ordinance applicability
- compute statutory entitlement independently
- create payment finality
- create exit authorization
- expose gate behavior

## 7. Tests

API tests cover:

- valid approved payable-basis request maps into the fiscal document creation command
- missing payable basis maps to deterministic failure
- missing upstream finality/payment reference maps to deterministic failure
- pending/rejected/expired/unresolved/inconsistent statutory discount references are rejected through the entry point
- raw ID/evidence payload markers are rejected through the entry point
- request DTOs do not contain entitlement approval fields
- no Operator Console validation behavior is introduced
- no local ordinance decision behavior is introduced
- no payment finality or gate/exit behavior is exposed
- persistence-not-configured path fails closed

## 8. Non-Goals

This slice does not implement:

- database persistence
- database schema changes
- migrations
- controlled-code changes
- generated SQL changes
- BIR X/Z reports
- Annex E
- Digital SI
- statutory discount entitlement validation
- raw evidence/image storage
- POS-owned payment finality
- exit authorization or gate behavior

## 9. Validation

Validated commands:

```powershell
dotnet build
dotnet test
git diff --check
```

`dotnet test` currently runs runtime and API test projects.
