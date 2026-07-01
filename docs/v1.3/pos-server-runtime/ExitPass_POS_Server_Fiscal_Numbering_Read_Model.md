# ExitPass POS Server Fiscal Numbering Read Model

## 1. Purpose

This slice exposes the nullable fiscal numbering columns on `pos.fiscal_documents` through the existing persisted fiscal document read model and `GET /v1/fiscal-documents/{fiscalDocumentId}` endpoint.

The support is read-only. It does not allocate fiscal numbers, lock sequence state, mutate counters, or change `POST /v1/fiscal-documents` persistence behavior.

## 2. Fields Exposed

The read model and GET response now include:

- `fiscalIdentityId`
- `fiscalSequencePolicyId`
- `fiscalSequenceValue`
- `fiscalDocumentNumber`
- `fiscalSeries`
- `fiscalNumberPrefixText`
- `fiscalNumberSuffixText`
- `fiscalNumberAssignedAt`
- `fiscalNumberAssignedByRef`

The fields are nullable because runtime fiscal number allocation is not implemented yet.

## 3. Endpoint Response Impact

`GET /v1/fiscal-documents/{fiscalDocumentId}` returns the fiscal numbering fields as part of the existing document response.

For documents created by the current runtime path, these fields are expected to be null because `POST /v1/fiscal-documents` does not allocate or persist fiscal numbers yet.

If a disposable fixture or future approved runtime allocation stores fiscal numbering values in `pos.fiscal_documents`, the GET endpoint can read and return those persisted values.

## 4. Read-Only Behavior

The PostgreSQL reader selects the new fields from `pos.fiscal_documents` using the existing parameterized read path:

- no insert SQL is added to the reader;
- no update SQL is added to the reader;
- no transaction or lock is added to the reader;
- no sequence/counter state table is read or mutated.

`POST /v1/fiscal-documents` remains unchanged for fiscal numbering. It does not populate `fiscal_identity_id`, `fiscal_sequence_policy_id`, `fiscal_sequence_value`, `fiscal_document_number`, or assignment metadata.

## 5. Semantics

When present, `fiscalDocumentNumber` is persisted fiscal number data only.

It does not imply:

- BIR report finality;
- Digital SI issuance;
- X/Z finality;
- Annex E finality;
- payment finality;
- refund/reversal authority;
- exit authorization;
- gate execution.

Tender rows remain fiscal tender representation only. Discount/privilege rows remain fiscal representation only and do not establish entitlement approval or local ordinance eligibility.

## 6. `document_context` Posture

`document_context` remains non-authoritative for fiscal numbering.

Formal fiscal number data must be read from the dedicated nullable fiscal numbering columns on `pos.fiscal_documents`, not from JSON context.

## 7. Intentionally Unsupported

This slice does not add:

- runtime fiscal number allocation;
- sequence state locking;
- fiscal sequence state mutation;
- fiscal counter state mutation;
- idempotency allocation behavior;
- BIR reporting;
- Digital SI;
- Annex E;
- X/Z;
- statutory discount validation;
- payment finality ownership;
- refund/reversal authority;
- exit authorization;
- gate execution.

## 8. Smoke Testing

The disposable PostgreSQL API smoke test verifies that documents created by the current POST path read back with null fiscal numbering fields.

The smoke test also uses isolated disposable fixture data to verify the GET read model can return populated fiscal numbering fields when those fields are present in `pos.fiscal_documents`. This fixture update is not runtime allocation and must not be used as a production workflow.

Manual disposable PostgreSQL smoke testing remains required before commit approval:

```powershell
$env:POSSERVER_API_SMOKE_DB_URL = "Host=localhost;Port=5433;Database=posserver_api_smoke_validation_local;Username=exitpass;Password=<redacted>"
dotnet test .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj
```
