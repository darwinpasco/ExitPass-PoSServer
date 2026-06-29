# ExitPass POS Server Fiscal Document Creation Skeleton

## 1. Purpose

This note documents the first runtime skeleton for POS Server fiscal document creation.

The skeleton is intentionally narrow. It establishes application-layer command, input, result, service, and repository-port shapes for fiscalizing an upstream approved payable basis. It does not implement database persistence, BIR reporting, Digital SI behavior, Annex E, X/Z reporting, statutory discount entitlement validation, or payment finality ownership.

## 2. Boundary Rule

The runtime skeleton follows the locked statutory discount boundary:

“Operator Console validates; Central PMS/Discount Service authorizes the payable-basis effect; POS Server fiscalizes.”

POS Server consumes approved upstream references. It must not adjudicate Senior/PWD entitlement, review evidence, decide local ordinance eligibility, or compute statutory entitlement independently.

## 3. Added Runtime Components

Runtime project:

- `src/ExitPass.PosServer.Runtime/ExitPass.PosServer.Runtime.csproj`

Fiscal document creation components:

- `FiscalDocumentCreationCommand`
- `FiscalizationPayableBasisInput`
- `FiscalDiscountReferenceInput`
- `FiscalDiscountReferenceStatus`
- `FiscalDocumentCreationResult`
- `FiscalDocumentCreationErrorCode`
- `FiscalDocumentCreationService`
- `FiscalDocumentDraft`
- `IFiscalDocumentRepository`

The repository port is intentionally minimal. It allows the use case to reach a persistence boundary without selecting a database technology or changing schema SQL.

## 4. Runtime Behavior

The skeleton allows POS Server to create a fiscal document draft only when the command includes:

- local POS Server fiscal context
- upstream approved payable-basis reference
- upstream payment/finality reference
- optional traceability references such as Central PMS payment attempt, payment confirmation, payment finality, vendor acknowledgement, and statutory discount validation reference

The service returns deterministic failure results for:

- missing payable basis
- missing upstream finality/payment reference
- unapproved statutory discount reference
- sensitive evidence payload not allowed
- unsupported fiscal document request

## 5. Statutory Discount Guardrails

The runtime skeleton rejects statutory discount treatment when the discount reference status is:

- pending
- rejected
- expired
- unresolved
- inconsistent

The skeleton accepts approved discount validation references as traceability inputs only. It does not approve entitlement, inspect raw evidence, decide whether the entitled person is present, decide ordinance applicability, or calculate statutory entitlement independently from the approved payable basis.

Sensitive evidence markers such as raw ID image, identity document, evidence payload, credential, token, or secret are rejected when they appear in command/reference context.

## 6. Tests

Test project:

- `tests/ExitPass.PosServer.Runtime.Tests/ExitPass.PosServer.Runtime.Tests.csproj`

The tests cover:

- valid approved payable-basis input reaches the document creation path
- missing payable basis is rejected
- missing upstream finality/payment reference is rejected
- pending/rejected/expired/unresolved/inconsistent discount references are rejected
- raw ID image/evidence payload is rejected
- command models do not include entitlement approval fields
- no Operator Console validation behavior is introduced
- no local ordinance decision behavior is introduced

## 7. Non-Goals

This slice does not implement:

- database schema changes
- migrations
- controlled-code changes
- generated SQL changes
- BIR X/Z reports
- Annex E reports
- Digital SI behavior
- statutory discount entitlement validation
- raw evidence/image storage
- POS-owned payment finality
- exit authorization or gate behavior

## 8. Validation

Validated commands:

```powershell
dotnet build
dotnet test
git diff --check
```

`dotnet test` currently runs 12 fiscal document creation guardrail tests.
