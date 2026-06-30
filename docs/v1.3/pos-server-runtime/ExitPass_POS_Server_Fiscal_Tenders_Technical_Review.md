# ExitPass POS Server Fiscal Tenders Technical Review

## Review Summary

Fiscal document creation now persists fiscal tender rows to `pos.fiscal_tenders` in the same PostgreSQL transaction as the fiscal document header, initial status history, supported fiscal document links, and fiscal lines.

Finding counts: P0 0 / P1 0 / P2 0 / Editorial 0.

## Scope Review

The change is limited to runtime/API tender input mapping, PostgreSQL persistence wiring, tests, and runtime documentation.

No `db/state` SQL changed. No migrations, controlled-code JSON changes, generated SQL changes, CI changes, Atlas config, or sample transaction data were added.

Manual API testing initially exposed that requests were failing before tender validation with `unsupported_fiscal_document_request` because the payload did not include every local fiscal schema context field required by the runtime service. The required local fiscal schema context fields are:

- `sitePosServerRef`
- `fiscalDocumentTypeCodeKey`
- `sitePosServerId`
- `fiscalDocumentTypeCodeId`
- `fiscalDocumentStatusCodeId`

The API already accepted the schema-backed ID fields. The DTO/mapping was updated to also accept `lines` as an alias for `documentLines`, because manual payloads commonly used `lines`.

Manual API testing then exposed a second mapping mismatch: callers supplied `upstreamFinalityRef` at the top level, while the runtime command consumes `PayableBasis.UpstreamFinalityRef`. The API now accepts top-level `upstreamFinalityRef` as an alias and maps it into payable-basis finality when the nested payable-basis value is absent.

## Persistence Review

Writes are limited to existing POS schema tables:

- `pos.fiscal_documents`
- `pos.fiscal_document_status_history`
- `pos.fiscal_document_links`
- `pos.fiscal_document_lines`
- `pos.fiscal_tenders`

The repository inserts header, status history, links, lines, and tenders inside one PostgreSQL transaction. Commit occurs only after all inserts succeed; failures roll back the full transaction.

## Tender Mapping Review

Tender input maps to schema-backed columns only:

- `tender_type_code_id`
- `amount_minor_units`
- `currency_code`
- `central_pms_payment_attempt_ref`
- `central_pms_payment_confirmation_ref`
- `payment_finality_ref`
- `provider_ref`
- `tender_context`

The current table does not define tender sequence or status columns, so this slice does not model them.

## Validation Review

Runtime validation requires at least one fiscal tender and rejects missing tender type, nonpositive amount, currency mismatch with the payable basis, invalid currency, blank optional references, blank context entries, and raw credential/token/payment payload markers before persistence.

Tender validation is local fiscalization structure only. Tender rows do not establish payment finality, settlement, refund/reversal authority, exit authorization, or gate execution.

Tender validation now returns tender-specific deterministic response codes:

- `missing_fiscal_tender`
- `invalid_fiscal_tender`
- `sensitive_tender_payload_not_allowed`

## SQL Safety Review

The tender insert uses an explicit column list and parameterized Npgsql commands. Untrusted request values are not interpolated into SQL.

## Boundary Review

No tax details, discount/privilege details, totals, BIR reporting, Digital SI, Annex E, X/Z behavior, statutory discount validation, payment finality ownership, refund/reversal authority, exit authorization, or gate behavior were added.

The locked boundary remains: Operator Console validates; Central PMS/Discount Service authorizes payable-basis effect; POS Server fiscalizes.

## Test Review

Tests cover:

- valid fiscal tender input reaching the creation path
- API DTO mapping for fiscal tenders
- complete local fiscal schema context reaching tender validation
- `lines` alias mapping into fiscal document lines
- tender-required behavior
- invalid tender amount, currency, type, and blank reference rejection
- raw credential/payment payload markers rejected before persistence
- tender SQL targeting `pos.fiscal_tenders`
- header, status history, links, lines, and tenders in one transaction
- parameterized SQL posture
- authority-boundary guardrails

Real PostgreSQL integration testing was not added in this slice; adapter tests inspect SQL mapping and transaction behavior without requiring developer secrets or a disposable database.

## Manual Test Payload

Use this shape to get past local fiscal schema context validation and reach tender validation:

```json
{
  "sitePosServerRef": "site-pos-server-001",
  "fiscalDocumentTypeCodeKey": "sales_invoice",
  "sitePosServerId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "channelTerminalId": null,
  "fiscalDocumentTypeCodeId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
  "fiscalDocumentStatusCodeId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
  "businessDayDate": "2026-06-30",
  "centralPmsParkingSessionRef": "parking-session-001",
  "centralPmsPaymentAttemptRef": "payment-attempt-001",
  "centralPmsPaymentConfirmationRef": "payment-confirmation-001",
  "upstreamFinalityRef": "central-finality-001",
  "paymentFinalityRef": "central-finality-001",
  "payableBasis": {
    "payableBasisRef": "payable-basis-001",
    "currencyCode": "PHP",
    "payableAmountMinorUnits": 12500,
    "discountReferences": [
      {
        "discountValidationRef": "discount-validation-001",
        "status": "approved",
        "appliesStatutoryDiscountTreatment": true
      }
    ]
  },
  "lines": [
    {
      "lineSequence": 1,
      "lineTypeCodeId": "11111111-1111-1111-1111-111111111111",
      "description": "Parking fee",
      "quantity": 1,
      "unitAmountMinorUnits": 12500,
      "grossAmountMinorUnits": 12500,
      "discountAmountMinorUnits": 0,
      "taxAmountMinorUnits": 0,
      "netAmountMinorUnits": 12500,
      "currencyCode": "PHP",
      "sourceRef": "line-source-001",
      "lineContext": {
        "source_system": "central_pms"
      }
    }
  ],
  "tenders": [
    {
      "tenderTypeCodeId": "22222222-2222-2222-2222-222222222222",
      "amountMinorUnits": 12500,
      "currencyCode": "PHP",
      "centralPmsPaymentAttemptRef": "payment-attempt-001",
      "centralPmsPaymentConfirmationRef": "payment-confirmation-001",
      "paymentFinalityRef": "central-finality-001",
      "providerRef": "provider-ref-001",
      "tenderContext": {
        "source_system": "central_pms"
      }
    }
  ]
}
```

Final manual-testable outcomes:

- Remove `tenders` or send an empty array to get `missing_fiscal_tender`.
- Set tender `amountMinorUnits` to `0` to get `invalid_fiscal_tender`.
- Set tender `currencyCode` to a value different from payable basis currency to get `invalid_fiscal_tender`.
- Put `payment_payload`, `provider_callback`, `card_number`, `cvv`, `token`, `secret`, or similar raw credential markers in tender references/context to get `sensitive_tender_payload_not_allowed`.
- The top-level `upstreamFinalityRef` alias reaches tender validation; nested `payableBasis.upstreamFinalityRef` is also accepted.
- With a valid payload and no persistence configured, the request reaches persistence and returns `persistence_not_configured`.
- With URL-style `POSSERVER_DB_URL`, the request returns `invalid_persistence_configuration`, not HTTP 500.

## Validation Results

Local validation completed:

- `dotnet build`: blocked in the normal output path because a running manual-test `ExitPass.PosServer.Api` process locked API output files
- `dotnet build -p:OutputPath=D:\SourceCodes\ExitPass\.tmp-posserver-build\bin\`: passed with 0 warnings and 0 errors
- `dotnet test -p:OutputPath=D:\SourceCodes\ExitPass\.tmp-posserver-build\bin\`: passed, 68 total tests
- `.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static`: passed
- `git diff --check`: passed
- Targeted scope check: no `db/state`, controlled-code JSON, generated SQL, CI, Atlas, or migration changes

## Recommendation

Ready for commit.
