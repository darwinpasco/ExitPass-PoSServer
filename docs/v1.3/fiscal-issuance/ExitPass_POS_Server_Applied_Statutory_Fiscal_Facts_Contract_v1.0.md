# ExitPass POS Server Applied Statutory Fiscal Facts Contract v1.0

## 1. Executive Decision

This document freezes the contract-first decision for immutable statutory privilege facts accepted by POS Server during fiscal-document issuance.

Decision:

- Additive future API shape: `appliedStatutoryFiscalFacts`.
- Scope: accepted only from Central PMS after payment-time payable-basis application is final.
- Ordinary transactions: no statutory object is required.
- Statutory transactions: the object is required, complete, immutable, and fail-closed.
- Semantic hash: existing POS Server `sha256:v1` is not changed. A later runtime slice must introduce a new POS semantic source version for requests that carry `appliedStatutoryFiscalFacts`.
- Persistence: current POS Server schema is partially usable but incomplete. Later schema work is required for governed first-class statutory snapshot persistence.
- Privacy: POS Server must not receive raw statutory identity data, evidence images, document images, reviewer personal details, credentials, or signed evidence links.

No runtime, DTO, API endpoint, SQL, migration, persistence, or renderer change is implemented by this contract slice.

## 2. Authority Boundary

Central PMS owns:

- statutory decision identity;
- eligibility decision state;
- payment-time privilege application;
- tariff revalidation;
- payable-basis calculation;
- statutory discount calculation;
- VAT treatment;
- final amount due;
- statutory policy or ordinance resolution;
- payment readiness;
- statutory audit linkage.

POS Server owns:

- fiscal-document issuance;
- fiscal numbering;
- fiscal persistence;
- Sales Invoice rendering;
- tax, discount, tender, line, and total fiscal snapshots;
- fiscal replay and semantic-conflict behavior.

POS Server must not:

- approve or reject statutory eligibility;
- determine whether a customer is legally entitled;
- resolve a local ordinance;
- calculate the statutory privilege independently;
- retrieve evidence;
- inspect statutory identity documents;
- mutate Central PMS payable-basis state;
- initiate payment.

WebPay and APT are channels only. They do not calculate or author authoritative statutory fiscal facts.

## 3. Upstream Evidence

Evidence was inspected from the current POS Server worktree and the read-only upstream repositories listed below.

| Repository | Branch inspected | Commit inspected | Evidence used |
| --- | --- | --- | --- |
| `D:\SourceCodes\ExitPass-PoSServer-Z-StatutoryFacts` | `feature/fiscal-issuance-applied-statutory-facts-contract` | `195a5ae2c8738f0b9efe22d6b079824b5fab5c2b` | POS request DTOs, runtime commands, semantic hasher, creation service tests, SQL schema, Z-001 audit docs |
| `D:\SourceCodes\ExitPass-Discounts` | `dev` | `f9ae5bf935962d8cead9d43d1e193a0af7e6f958` | Central PMS statutory decision/application contracts, terminal-cash statutory linkage reader, POS request mapper |
| `D:\SourceCodes\exitpassdb_v1.2` | `develop` | `7a785fd93d592b019fbb6ac6bbdf4fc82d8485dc` | Canonical statutory decision/application tables, constraints, indexes, controlled values |
| `D:\SourceCodes\ExitPass` | current read-only worktree | `d0a9d948ce7a6afb8b3c41c411fad8fef80c530c` | WebPay local statutory walkthrough context |
| `D:\SourceCodes\ExitPass-APT` | `dev` | `a7b259ff3f9e566e7fe5b8d1da7876d6f58df907` | APT terminal cash, readiness, presentation delegation contracts |
| `D:\SourceCodes\ExitPass-AssistedPaymentTerminal` | current active worktree | `c7b2e663126e66599169586fa9298e3b65d0a8b7` | Active encrypted database and cash-blocking posture |

Current POS Server request evidence:

- `CreateFiscalDocumentRequest` accepts `PayableBasis`, `DocumentLines`, `Tenders`, `TaxDetails`, `DiscountPrivilegeDetails`, `Totals`, document references, and context dictionaries.
- `FiscalizationPayableBasisRequest` accepts discount references with status and statutory-treatment flag.
- `FiscalDiscountPrivilegeDetailRequest` accepts generic discount/privilege rows with basis, discount, VAT privilege amount, beneficiary reference, evidence reference, approval reference, and context.
- `FiscalDocumentSemanticRequestHasher` `sha256:v1` hashes only current governed client-submitted request semantics and does not hash server-resolved fiscal header profile facts.
- POS Server runtime currently rejects unapproved statutory discount references and rejects sensitive payload markers.

Current Central PMS evidence:

- `StatutoryDiscountPayableBasisApplicationV1Record` provides the final application command identity, decision command identity, validation identity, original/target/applied tariff snapshots, applied policy reference, policy resolution basis, approved discount, approved VAT-exclusive amount, approved VAT amount, approved final payable amount, currency, source channel, and application timestamps.
- `PostgresTerminalCashStatutoryFiscalLinkageReader` fails closed when decision/application state is not final, application rows are ambiguous, validation is missing, snapshots do not match, parking/session/site/scope does not match, monetary facts are missing, or currency/amounts are inconsistent.
- Current Central PMS `PosServerFiscalDocumentRequestMapper` already forwards a partial statutory set in `PayableBasis.DiscountReferences`: decision command ref, entitlement type, applied policy ref, original/applied tariff snapshots, original amount, VAT-exclusive basis, VAT treatment, discount amount, final payable amount, timestamp, and source channel.
- Current Central PMS mapper does not send the statutory payable-basis application command identity, final application status, benefit classification, site group, distinct VAT amount, complete policy authority posture, or a first-class POS statutory object.

Canonical database evidence:

- `discounts.statutory_discount_decision_commands` constrains source channel to `OPERATOR_CONSOLE`, `WEBPAY`, and `ASSISTED_PAYMENT_TERMINAL`; entitlement type to `SENIOR_CITIZEN` and `PWD`; command status to `RECEIVED`, `PROCESSING`, `AWAITING_REVIEW`, `COMPLETED`, `FAILED_RETRYABLE`, and `FAILED_NON_RETRYABLE`; and decision result to `APPROVED`, `REJECTED`, and `NOT_DECIDED`.
- `discounts.statutory_discount_payable_basis_application_commands` constrains application command status to `RECEIVED`, `PROCESSING`, `APPLIED`, `FAILED_RETRYABLE`, and `FAILED_NON_RETRYABLE`; result classification to `APPLIED`, `IDEMPOTENT_REPLAY`, `SEMANTIC_CONFLICT`, `DECISION_NOT_APPROVED`, `DECISION_NOT_FOUND`, `IN_PROGRESS`, `RETRYABLE_FAILURE`, and `NON_RETRYABLE_FAILURE`.
- `discounts.statutory_discount_payable_basis_applications` stores immutable applied monetary facts and requires nonnegative gross, VAT, VAT-exclusive, discount, and final amounts; gross components reconcile as `vat_exclusive_amount_minor_units + vat_amount_minor_units = gross_amount_minor_units`; final payable does not exceed gross; and applied rows require an applied tariff snapshot and applied timestamp.

## 4. Contract Eligibility

POS Server may accept `appliedStatutoryFiscalFacts` only when Central PMS reports a complete, final, payment-time application.

Accepted final posture:

- decision command status: `COMPLETED`;
- decision result status: `APPROVED`;
- payable-basis application command status: `APPLIED`;
- payable-basis application result classification: `APPLIED` or `IDEMPOTENT_REPLAY`;
- payable basis readiness: ready;
- final payable amount present;
- authoritative application command reference present;
- currency consistent with payable basis, lines, taxes, discounts, totals, and tenders;
- final payable amount equals the fiscal document amount actually paid.

Fail-closed states:

- awaiting review;
- not decided;
- rejected;
- application not requested;
- application pending;
- application failed;
- payable basis not ready;
- missing final amount;
- missing authoritative application reference;
- inconsistent currency;
- inconsistent totals;
- unsupported entitlement classification;
- unsupported benefit classification;
- evidence or raw identity content included in the request.

A mere approval is not enough. The payment-time application must be complete.

## 5. Exact Field Matrix

The future API object name is `appliedStatutoryFiscalFacts`.

| Field | JSON/API name | Type | Required | Nullable | Source authority | Semantic hash | Persistence target | Sales Invoice use | Privacy classification | Validation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Statutory decision command ID | `statutoryDiscountDecisionCommandId` | UUID string | Yes | No | Central PMS | Yes, statutory hash version | New governed statutory snapshot table/column | Internal audit/readback only | Internal reference | Non-empty UUID; decision completed and approved |
| Statutory request reference | `statutoryRequestReference` | UUID string | Yes | No | Central PMS | Yes | New governed statutory snapshot table/column | Internal audit/readback only | Internal reference | Non-empty UUID; matches decision/application chain |
| Payable-basis application command ID | `statutoryPayableBasisApplicationCommandId` | UUID string | Yes | No | Central PMS | Yes | New governed statutory snapshot table/column | Internal audit/readback only | Internal reference | Non-empty UUID; application final and applied |
| Statutory validation ID | `statutoryValidationId` | UUID string | Yes | No | Central PMS | Yes | New governed statutory snapshot table/column | Internal audit/readback only | Internal reference | Non-empty UUID; tied to decision/application |
| Parking session ID | `parkingSessionId` | UUID string | Yes | No | Central PMS | Yes | Existing `pos.fiscal_documents.central_pms_parking_session_ref`; first-class statutory snapshot still required | Internal audit/readback only | Internal reference | Must match top-level fiscal parking reference |
| Site ID | `siteId` | UUID string | Yes | No | Central PMS/POS routing | Yes | Existing `pos.fiscal_documents.site_id` equivalent absent; profile uses site scope; statutory snapshot required | Internal audit/readback only | Internal reference | Must match issuing Site POS Server scope |
| Site Group ID | `siteGroupId` | UUID string | Yes | No | Central PMS | Yes | Absent in POS schema | Internal audit/readback only | Internal reference | Non-empty UUID; must match Central PMS payment/application scope |
| Entitlement type | `entitlementType` | string controlled code | Yes | No | Central PMS | Yes | Present only as generic discount reference property/context; first-class snapshot required | Customer label only where legally/operationally required | Fiscal classification, no PII | Allowed: `SENIOR_CITIZEN`, `PWD` |
| Benefit classification | `benefitClassification` | string controlled code | Yes | No | Central PMS | Yes | Absent in POS schema | Customer discount/tax label where required | Fiscal classification, no PII | Allowed values in section 9 |
| Policy or ordinance reference | `policyReference` | object | Yes | No | Central PMS | Yes | Absent first-class; generic applied policy ref exists only in discount reference | Customer-visible safe code/name only where required | Public/legal reference, no raw ordinance text | Must include `resolutionBasis` and one safe policy authority reference |
| Original tariff snapshot ID | `originalTariffSnapshotId` | UUID string | Yes | No | Central PMS | Yes | Present only as generic discount reference property/context | Internal audit/readback only | Internal reference | Non-empty UUID; matches application chain |
| Applied tariff snapshot ID | `appliedTariffSnapshotId` | UUID string | Yes | No | Central PMS | Yes | Present only as generic discount reference property/context | Internal audit/readback only | Internal reference | Non-empty UUID; final payable basis source |
| Original amount | `originalAmountMinorUnits` | integer int64 | Yes | No | Central PMS | Yes | Present only as generic discount reference property/context | May appear as original gross/tariff amount if required | Monetary fiscal fact | >= 0 |
| VAT-exclusive basis | `vatExclusiveBasisAmountMinorUnits` | integer int64 | Yes | No | Central PMS | Yes | Present only as generic discount reference property/context; tax rows also exist | Tax/discount/totals presentation | Monetary fiscal fact | >= 0 |
| VAT amount | `vatAmountMinorUnits` | integer int64 | Yes | No | Central PMS | Yes | Absent in current discount reference; tax rows exist but are not linked to statutory application | Tax presentation | Monetary fiscal fact | >= 0 |
| VAT treatment | `vatTreatment` | string controlled code | Yes | No | Central PMS | Yes | Present only as generic discount reference property/context | Tax and discount presentation | Fiscal classification | Allowed values in section 11; POS must not infer from entitlement |
| Statutory discount amount | `statutoryDiscountAmountMinorUnits` | integer int64 | Yes | No | Central PMS | Yes | Generic `pos.fiscal_discount_privilege_details.discount_amount_minor_units`; first-class linkage required | Discount presentation | Monetary fiscal fact | >= 0 |
| Final payable amount | `finalPayableAmountMinorUnits` | integer int64 | Yes | No | Central PMS | Yes | Present in payable basis/top-level totals; statutory snapshot still required | Totals and tender reconciliation | Monetary fiscal fact | >= 0; must match paid amount and fiscal total |
| Currency | `currency` | string | Yes | No | Central PMS | Yes | Existing currency fields across lines/taxes/discounts/tenders/totals | Sales Invoice currency | Monetary fiscal fact | ISO 4217 uppercase; consistent across fiscal request |
| Applied timestamp | `appliedAt` | date-time offset | Yes | No | Central PMS | Yes | Absent first-class in POS schema | Internal audit/readback; customer display only if required | Operational audit | Must be present for final application |
| Source payment channel | `sourcePaymentChannel` | string controlled code | Yes | No | Central PMS | Yes | Present only as generic discount reference property/context | Internal audit/readback only | Operational reference | Allowed: `WEBPAY`, `ASSISTED_PAYMENT_TERMINAL`, `OPERATOR_CONSOLE` |
| Terminal-cash tender ID | `terminalCashTenderId` | UUID string | Conditional | Yes | Central PMS | Yes when present | Absent first-class; tender context can hold but is incomplete | Internal reconciliation only | Internal reference | Required for terminal-cash issuance; omitted for non-cash statutory payments |
| Payment attempt reference | `paymentAttemptRef` | string/UUID | Yes via parent request | No | Central PMS/Payment Orchestrator | Yes via parent request | Existing `pos.fiscal_documents.central_pms_payment_attempt_ref` and `pos.fiscal_tenders.central_pms_payment_attempt_ref` | Internal audit/readback only | Internal reference | Must match paid transaction |
| Payment confirmation reference | `paymentConfirmationRef` | string/UUID | Yes via parent request | No | Central PMS/Payment Orchestrator | Yes via parent request | Existing `pos.fiscal_documents.central_pms_payment_confirmation_ref` and `pos.fiscal_tenders.central_pms_payment_confirmation_ref` | Internal audit/readback only | Internal reference | Must be final before fiscal issuance |
| Correlation ID | `correlationId` | UUID string/header | Yes as header | No | Caller/Central PMS | No | Existing correlation/audit/status conventions | Not customer-visible | Operational trace | Preserve through response/error; not a fiscal semantic fact |

## 6. Required Fields

When `appliedStatutoryFiscalFacts` is present, these fields are required and non-null:

- `statutoryDiscountDecisionCommandId`
- `statutoryRequestReference`
- `statutoryPayableBasisApplicationCommandId`
- `statutoryValidationId`
- `parkingSessionId`
- `siteId`
- `siteGroupId`
- `entitlementType`
- `benefitClassification`
- `policyReference`
- `originalTariffSnapshotId`
- `appliedTariffSnapshotId`
- `originalAmountMinorUnits`
- `vatExclusiveBasisAmountMinorUnits`
- `vatAmountMinorUnits`
- `vatTreatment`
- `statutoryDiscountAmountMinorUnits`
- `finalPayableAmountMinorUnits`
- `currency`
- `appliedAt`
- `sourcePaymentChannel`

Conditional required:

- `terminalCashTenderId` is required only for terminal-cash fiscal issuance.
- Parent fiscal request `centralPmsPaymentAttemptRef`, `centralPmsPaymentConfirmationRef`, payable basis, lines, taxes, discounts, totals, and tenders remain required according to current POS Server behavior.

## 7. Optional Fields

Optional fields may be added only when they have fiscal, audit, reconciliation, or idempotency value and are safe to persist:

- `policyReference.nationalLawReference`
- `policyReference.ordinanceReference`
- `policyReference.appliedPolicyReferenceId`
- `policyReference.policyCode`
- `policyReference.policyVersionId`
- `paymentFinalityRef` through the parent request
- safe source-system references already governed by the parent fiscal request

Optional absent and explicit `null` values must canonicalize the same way for semantic hashing. Blank strings are invalid.

## 8. Prohibited Fields

The statutory fiscal object and all related parent context dictionaries must not include:

- beneficiary name;
- beneficiary address;
- birth date;
- raw Senior Citizen ID number;
- raw PWD ID number;
- identity-document image;
- evidence image;
- Base64 evidence;
- object-storage credentials;
- signed evidence URLs;
- reviewer display name;
- reviewer email;
- browser-supplied reviewer authority;
- raw ordinance text;
- service credentials;
- authorization headers;
- internal policy permissions;
- Operator Console device facts unless separately justified by a later fiscal contract;
- Operator Console shift facts unless separately justified by a later fiscal contract;
- caller-calculated values not issued by Central PMS.

`maskedIdReference` is available upstream for operational workflow but is prohibited from POS Server fiscal issuance. POS Server needs fiscal and reconciliation facts, not beneficiary identity evidence.

## 9. Classification Model

Entitlement type:

- `SENIOR_CITIZEN`
- `PWD`

Unknown entitlement values must be rejected. POS Server must not collapse entitlement into a generic discount type.

Benefit classification:

- `VAT_EXEMPTION_ONLY`
- `STATUTORY_DISCOUNT_ONLY`
- `VAT_EXEMPTION_AND_STATUTORY_DISCOUNT`
- `FREE_PARKING`
- `REDUCED_PARKING_RATE`
- `CAPPED_PARKING_FEE`

Unknown benefit values must be rejected in this contract version. Entitlement type identifies the legal class of the beneficiary; benefit classification identifies the fiscal effect. They are intentionally separate because not every statutory privilege is a 20 percent discount, and some ordinance-defined parking benefits may be free, capped, reduced, VAT-only, or discount-only.

Future extension posture:

- add new values through a controlled-code and semantic-hash compatibility slice;
- do not accept unknown values as pass-through;
- do not map unknown values to generic promotional discount behavior.

## 10. Monetary Invariants

Amounts use minor currency units.

Required invariants:

- `originalAmountMinorUnits >= 0`
- `vatExclusiveBasisAmountMinorUnits >= 0`
- `vatAmountMinorUnits >= 0`
- `statutoryDiscountAmountMinorUnits >= 0`
- `finalPayableAmountMinorUnits >= 0`
- `currency` is explicit and uppercase ISO 4217.
- `currency` matches payable basis, lines, tax details, discount details, totals, and tenders.
- `finalPayableAmountMinorUnits` equals the authoritative amount actually paid.
- POS Server must not recompute the privilege from percentages.
- POS Server must not infer VAT treatment from entitlement type.
- Rounding must already be resolved by Central PMS.
- Replay must preserve identical monetary values.

Authoritative monetary model:

- `originalAmountMinorUnits` is the Central PMS original gross tariff/payable amount before statutory application.
- `vatExclusiveBasisAmountMinorUnits + vatAmountMinorUnits` must reconcile to the gross VAT model reported by Central PMS.
- `statutoryDiscountAmountMinorUnits` is a final applied amount, not a rate.
- `finalPayableAmountMinorUnits` is the amount due after statutory application and must match the fiscal paid amount.

Free parking:

- `benefitClassification = FREE_PARKING`
- `statutoryDiscountAmountMinorUnits` may equal the discount/benefit value needed to reduce the final payable amount to zero, according to Central PMS.
- `finalPayableAmountMinorUnits = 0`
- tender behavior must follow the later fiscal-issuance implementation decision for zero-amount fiscal documents; POS Server must not fabricate a cash/card tender.

## 11. VAT and Tax Behavior

VAT treatment values accepted by this contract version:

- `VAT_EXEMPT`
- `VAT_EXCLUSIVE`
- `VAT_INCLUSIVE_NO_EXEMPTION`
- `NON_VAT`
- `ZERO_RATED`
- `NOT_APPLICABLE`

Rules:

- Central PMS sends the VAT treatment. POS Server does not infer it from entitlement.
- VAT exemption, statutory discount, ordinary promotional discount, coupon adjustment, tariff adjustment, and free-parking benefit are distinct facts.
- POS Server must not collapse unlike adjustments into one ambiguous field.
- `vatAmountMinorUnits` is the authoritative VAT amount after Central PMS payable-basis application.
- `vatExclusiveBasisAmountMinorUnits` is the authoritative VAT-exclusive basis after Central PMS application.
- Tax detail rows must reconcile to the statutory VAT facts.
- Discount/privilege rows must reconcile to the statutory discount facts.
- If VAT treatment is `NOT_APPLICABLE`, VAT amount may be zero but the field remains required for statutory documents.

## 12. Idempotency

Same idempotency key and same statutory facts:

- returns the original fiscal document;
- returns the original fiscal number if already assigned;
- returns the original immutable statutory snapshot;
- does not allocate a second fiscal number;
- does not create a second fiscal document.

Same idempotency key and changed governed statutory facts:

- returns semantic conflict;
- does not create a second fiscal document;
- does not allocate a second fiscal number.

Profile, header, evidence, reviewer, or mutable runtime configuration changes:

- must not affect the request semantic hash unless those facts are client-submitted governed request semantics;
- must not cause replay conflict;
- must not change the original fiscal statutory snapshot.

## 13. Semantic Hash

Current POS Server `sha256:v1` canonical input fields are:

- `business_day_date`
- `central_pms_parking_session_ref`
- `central_pms_payment_attempt_ref`
- `central_pms_payment_confirmation_ref`
- `channel_terminal_id`
- `discount_privilege_details`
- `document_lines`
- `document_links`
- `fiscal_document_status_code_id`
- `fiscal_document_type_code_id`
- `fiscal_document_type_code_key`
- `payable_basis`
- `payment_finality_ref`
- `reference_context`
- `site_pos_server_id`
- `site_pos_server_ref`
- `tax_details`
- `tenders`
- `totals`
- `vendor_ack_ref`

Decision:

- Do not modify `sha256:v1`.
- Ordinary non-statutory fiscal requests remain compatible with `sha256:v1`.
- A later implementation must add a new POS Server semantic source version for requests containing `appliedStatutoryFiscalFacts`.
- The new statutory semantic source version must include every required and present optional field in `appliedStatutoryFiscalFacts`, except correlation ID.
- Required missing fields reject before hashing.
- Optional missing versus explicit null canonicalizes identically.
- Evidence-only differences cannot exist because evidence is prohibited from the request.

Fields that must participate in the future statutory semantic hash:

- final application identity;
- decision identity;
- validation identity;
- request reference;
- parking/session/site/site-group linkage;
- entitlement type;
- benefit classification;
- policy reference fields;
- original and applied tariff snapshots;
- VAT treatment;
- authoritative monetary values;
- currency;
- applied timestamp;
- source payment channel;
- terminal-cash tender ID when present;
- parent payment references already hashed by POS Server.

## 14. Replay and Conflict

Expected outcomes:

| Scenario | Expected POS Server behavior |
| --- | --- |
| First fiscal issuance | Validate statutory facts, compute semantic hash, create one fiscal document, snapshot statutory facts, allocate fiscal number according to existing numbering rules |
| Exact replay | Return the original fiscal document and original statutory snapshot |
| Same key, changed statutory reference | Semantic conflict |
| Same key, changed entitlement type | Semantic conflict |
| Same key, changed VAT treatment | Semantic conflict |
| Same key, changed discount amount | Semantic conflict |
| Same key, changed final payable amount | Semantic conflict |
| Same key, evidence-only differences | Request rejected because evidence is prohibited |
| Upstream retry after timeout | Idempotent read of original result when durable commit completed; safe retry posture when completion is unknown |
| Central PMS restart | Reuse original idempotency key and canonical request facts |
| POS Server restart | Durable idempotency record and fiscal document determine replay |
| Fiscal document already completed | Replay returns completed document/readback |
| Fiscal document failed before durable commit | Existing idempotency failure/retry posture applies; no duplicate fiscal number allocation |
| Application reference reused across different payment | Reject as conflict or duplicate application use, depending on later schema constraint |
| Payment reference reused across different statutory application | Reject as conflict |

## 15. API Compatibility

Compatibility decision:

- The statutory object is optional for ordinary transactions.
- Adding the object is API-additive but not semantic-hash-neutral.
- A new API media type is not required if the existing create route accepts additive JSON fields under normal ASP.NET model binding rules; however, the runtime implementation must explicitly govern the new field and document it.
- A new POS Server semantic hash source version is required for statutory requests.
- Existing stored `sha256:v1` requests do not require migration.
- Existing read APIs need additive statutory readback fields after persistence is implemented.
- Presentation APIs need additive statutory presentation fields after renderer integration.
- Existing clients that do not send statutory facts continue unchanged.

Do not place this contract into ungoverned context dictionaries as a compatibility shortcut. Context storage is insufficient for validation, indexing, immutability, and cross-lane contract clarity.

## 16. Persistence Mapping

| Proposed field | POS Server persistence status | Evidence |
| --- | --- | --- |
| Payment/parking refs | PRESENT_AND_USABLE | `pos.fiscal_documents.central_pms_parking_session_ref`, `central_pms_payment_attempt_ref`, `central_pms_payment_confirmation_ref`; tender equivalents in `pos.fiscal_tenders` |
| Lines, taxes, discounts, tenders, totals | PRESENT_AND_USABLE | `pos.fiscal_document_lines`, `pos.fiscal_tax_details`, `pos.fiscal_discount_privilege_details`, `pos.fiscal_tenders`, `pos.fiscal_totals` |
| Semantic hash and idempotency | PRESENT_AND_USABLE | `pos.idempotency_records.semantic_request_hash`, `idempotency_scope`, `idempotency_key` |
| Decision command ID | PRESENT_BUT_INCOMPLETE | Current Central PMS mapper can place it on a discount reference; POS schema lacks first-class constrained statutory field |
| Entitlement type | PRESENT_BUT_INCOMPLETE | Current mapper can place it on a discount reference; POS schema lacks controlled first-class field |
| Applied policy reference | PRESENT_BUT_INCOMPLETE | Current mapper can place it on a discount reference; POS schema lacks first-class policy authority fields |
| Original/applied tariff snapshots | PRESENT_BUT_INCOMPLETE | Current mapper can place them on a discount reference; POS schema lacks constrained snapshot fields |
| Original amount, VAT-exclusive basis, VAT treatment, discount, final payable | PRESENT_BUT_INCOMPLETE | Current mapper can place partial values on discount references; POS schema lacks complete statutory application snapshot |
| VAT amount | ABSENT | POS tax details store tax rows but no first-class statutory VAT fact linked to application |
| Payable-basis application command ID | ABSENT | No POS request model or schema field |
| Statutory request reference | ABSENT | No POS request model or schema field |
| Statutory validation ID as first-class field | ABSENT | Existing `discountValidationRef` is string reference only |
| Site group ID | ABSENT | No POS fiscal document/statutory snapshot field |
| Benefit classification | ABSENT | No POS controlled code or schema field |
| Policy resolution basis and ordinance/national law safe refs | ABSENT | No POS first-class policy authority fields |
| Applied timestamp | ABSENT | No POS first-class statutory application timestamp |
| Source payment channel | PRESENT_BUT_INCOMPLETE | Current mapper can place it on a discount reference; no POS controlled first-class field |
| Terminal-cash tender ID | ABSENT | Tender/payment refs exist, but no governed terminal-cash tender reference |
| Evidence images, raw IDs, signed URLs, credentials | SHOULD_NOT_BE_PERSISTED | POS runtime already rejects sensitive payload markers; contract prohibits them |

Later implementation requires:

- new first-class request DTO object;
- new POS semantic hash version;
- new immutable statutory child snapshot table or constrained JSON snapshot with indexes;
- controlled-code additions for entitlement type, benefit classification, VAT treatment, source channel, and validation failure codes;
- read-model fields;
- presentation adapter additions;
- database uniqueness for final application identity and payment/application linkage;
- indexes for reconciliation by decision command, application command, validation, parking session, payment ref, terminal-cash tender, and fiscal document.

## 17. Sales Invoice Presentation

Customer-visible facts may include:

- statutory discount label;
- entitlement label only when required or operationally useful, such as `Senior Citizen` or `PWD`;
- benefit label, such as VAT exemption, statutory discount, or free parking;
- statutory discount amount;
- VAT exemption/tax treatment display;
- VAT amount and taxable basis as required by the Sales Invoice template;
- final total and tender amount;
- safe policy or ordinance code/reference only when legally required.

Internal audit/readback facts may include:

- decision command ID;
- payable-basis application command ID;
- validation ID;
- tariff snapshot IDs;
- applied policy reference ID;
- payment refs;
- terminal-cash tender ID;
- applied timestamp;
- source payment channel.

The customer Sales Invoice, Digital Sales Invoice JSON, printed receipt, and reprint must never expose raw statutory identity data, evidence images, signed links, reviewer personal data, internal permissions, credentials, or raw ordinance text.

## 18. Privacy and Security

Privacy posture:

- POS Server receives the minimum fiscal facts needed for fiscal audit and reconciliation.
- POS Server does not receive beneficiary identity documents or evidence.
- POS Server does not receive reviewer personal details.
- POS Server does not receive channel-local device or shift facts unless a later fiscal contract justifies them.
- Internal UUIDs are for audit/readback/reconciliation APIs, not customer-visible receipt text unless justified.

Security posture:

- Central PMS authenticates to POS Server through the existing secured service-to-service fiscal issuance boundary.
- Correlation ID is preserved for diagnostics and safe error mapping.
- POS Server must return safe validation errors without leaking statutory evidence, identity details, SQL details, stack traces, credentials, or authorization headers.

## 19. Failure Behavior

Required failure behavior:

- ordinary request without statutory object: accepted according to existing rules;
- statutory object present but incomplete: reject before fiscal document creation;
- pending/rejected/non-final decision or application: reject before fiscal document creation;
- inconsistent totals/currency/tender amount: reject before fiscal document creation;
- unsupported entitlement, benefit, VAT treatment, or source channel: reject before fiscal document creation;
- prohibited evidence or raw identity field: reject before fiscal document creation;
- same idempotency key with changed statutory facts: semantic conflict;
- exact replay: return original fiscal document and original snapshot;
- persistence failure before durable commit: no fiscal document and no fiscal number are created;
- persistence failure after unknown commit state: existing POS completion-unknown/idempotency posture applies.

## 20. Examples

Machine-readable non-production examples are stored in:

`docs/v1.3/fiscal-issuance/fixtures/pos-server-applied-statutory-fiscal-facts-v1.examples.json`

The examples cover:

- ordinary transaction without statutory facts;
- Senior Citizen applied transaction;
- PWD applied transaction;
- free-parking benefit;
- invalid pending-review example;
- semantic-conflict example.

The examples use deterministic non-production identifiers and do not include production taxpayer data, raw statutory IDs, evidence, credentials, or signed URLs.

## 21. Open Questions

- Whether POS Server should store the statutory snapshot in a normalized child table or a constrained immutable JSON snapshot with generated/indexed columns.
- Whether `siteId` should become a first-class POS fiscal document column or remain in the statutory child snapshot with profile-scope validation.
- Whether customer-visible policy or ordinance references are required for the Philippine parking Sales Invoice template, and at what label granularity.
- Whether zero-amount/free-parking fiscal issuance requires a zero tender, no tender, or a governed non-cash benefit tender classification.
- Whether terminal-cash cashier, shift, and custody references are fiscal facts or Central PMS/APT-only operational evidence.
- Whether the current Central PMS `VatTreatment = VAT_EXCLUSIVE` terminal-cash linkage is sufficient for all benefit classifications or needs richer upstream classification before POS implementation.

## 22. Final Contract Decision

The POS Server applied statutory fiscal facts contract is frozen as an additive, first-class future request object named `appliedStatutoryFiscalFacts`.

The object is optional for ordinary fiscal requests and required for statutory-privilege fiscal requests. It must carry only final, payment-time, Central PMS-authored statutory application facts. POS Server validates the object, hashes it under a new statutory semantic source version, persists it immutably, and renders only the approved customer-visible subset.

POS Server must not accept approval-only, pending, evidence-bearing, identity-bearing, or caller-calculated statutory data. POS Server must not silently expand `sha256:v1`; runtime implementation must add a new governed semantic hash version for statutory requests.
