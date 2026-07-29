# ExitPass POS Server Applied Statutory Fiscal Facts Implementation Handoff v1.0

## 1. Runtime Changes Required

Add a future POS Server runtime slice that accepts a first-class `appliedStatutoryFiscalFacts` object during fiscal-document issuance.

Required runtime behavior:

- accept ordinary requests with no statutory object under current behavior;
- require the statutory object when Central PMS marks the fiscal issuance as statutory;
- validate that the statutory object represents final payment-time application, not approval-only state;
- reject pending, rejected, missing, incomplete, unsupported, inconsistent, or evidence-bearing statutory facts before fiscal document creation;
- snapshot accepted statutory facts immutably with the fiscal document;
- preserve replay and conflict behavior through idempotency;
- keep POS Server from calculating eligibility, discount, VAT treatment, tariff, or final amount.

Do not put the contract into generic context dictionaries as the implementation strategy.

## 2. DTO Changes Required

Add request DTOs equivalent to:

- `AppliedStatutoryFiscalFactsRequest`
- `AppliedStatutoryPolicyReferenceRequest`

Future parent:

- `CreateFiscalDocumentRequest.AppliedStatutoryFiscalFacts`
- `FiscalDocumentCreationCommand.AppliedStatutoryFiscalFacts`

Do not remove existing fields. Keep the change additive for ordinary fiscal requests.

Required DTO fields:

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

Conditional DTO fields:

- `terminalCashTenderId`

Do not add:

- raw ID numbers;
- beneficiary identity fields;
- evidence images or URLs;
- reviewer personal data;
- authorization headers;
- credentials;
- raw ordinance text;
- channel-local evidence fields without a separate fiscal contract.

## 3. Validation Changes Required

Add deterministic validation before fiscal document creation:

- required fields present and non-null;
- UUID fields parse and are non-empty;
- controlled code values are supported;
- source channel is supported;
- currency is uppercase ISO 4217 and matches all monetary rows;
- amounts are nonnegative;
- final payable amount equals payable basis amount and paid/tendered amount;
- statutory discount and VAT facts reconcile with tax and discount rows;
- site and site group match Central PMS payment/application scope;
- parking session matches the top-level POS request;
- applied tariff snapshot is final and distinct where Central PMS requires distinct applied snapshots;
- application is final and complete;
- prohibited sensitive fields are absent from statutory object and all context dictionaries.

Safe validation errors should use stable codes such as:

- `applied_statutory_facts_required`
- `applied_statutory_facts_not_final`
- `applied_statutory_application_reference_required`
- `applied_statutory_validation_reference_required`
- `applied_statutory_unsupported_entitlement_type`
- `applied_statutory_unsupported_benefit_classification`
- `applied_statutory_unsupported_vat_treatment`
- `applied_statutory_currency_mismatch`
- `applied_statutory_total_mismatch`
- `applied_statutory_prohibited_privacy_field`

## 4. Semantic-Hash Changes Required

Do not modify current POS Server `sha256:v1`.

Add a new governed POS semantic source version for fiscal requests that include `appliedStatutoryFiscalFacts`. Recommended name:

`pos-server-fiscal-document-create:sha256:v2`

The new canonical payload must include all required statutory fields and all present optional statutory fields except correlation ID.

Hash participation:

- decision command identity: yes;
- request reference: yes;
- application command identity: yes;
- validation identity: yes;
- parking/session/site/site group linkage: yes;
- entitlement type: yes;
- benefit classification: yes;
- policy reference: yes;
- tariff snapshots: yes;
- VAT treatment: yes;
- monetary values: yes;
- currency: yes;
- applied timestamp: yes;
- source payment channel: yes;
- terminal-cash tender ID when present: yes;
- correlation ID: no;
- evidence: prohibited and therefore no.

Compatibility rule:

- ordinary requests may continue using `sha256:v1`;
- statutory requests must use the new source version after implementation;
- Central PMS must coordinate before sending statutory facts into POS Server production.

## 5. Persistence Changes Required

Current POS Server schema is not sufficient for the frozen contract.

Recommended smallest durable design:

- add a child table under `pos`, for example `pos.fiscal_document_applied_statutory_facts`;
- one row per fiscal document, optional for ordinary fiscal documents;
- immutable after insert;
- foreign key to `pos.fiscal_documents`;
- unique constraints on fiscal document ID and statutory application command ID;
- indexes for decision command ID, application command ID, validation ID, parking session ID, payment refs, terminal-cash tender ID, and applied tariff snapshot ID;
- controlled-code constraints or foreign keys for entitlement type, benefit classification, VAT treatment, and source payment channel.

Required persisted fields:

- every required `appliedStatutoryFiscalFacts` field;
- conditional `terminalCashTenderId` when present;
- snapshot timestamp;
- correlation/audit metadata according to existing POS conventions.

Do not persist:

- identity-document data;
- evidence images;
- object-storage or signed URL data;
- reviewer personal data;
- credentials;
- authorization headers;
- raw ordinance text.

## 6. Controlled-Code Changes Required

POS Server needs governed controlled-code posture for:

- entitlement type: `SENIOR_CITIZEN`, `PWD`;
- benefit classification: `VAT_EXEMPTION_ONLY`, `STATUTORY_DISCOUNT_ONLY`, `VAT_EXEMPTION_AND_STATUTORY_DISCOUNT`, `FREE_PARKING`, `REDUCED_PARKING_RATE`, `CAPPED_PARKING_FEE`;
- VAT treatment: `VAT_EXEMPT`, `VAT_EXCLUSIVE`, `VAT_INCLUSIVE_NO_EXEMPTION`, `NON_VAT`, `ZERO_RATED`, `NOT_APPLICABLE`;
- source payment channel: `WEBPAY`, `ASSISTED_PAYMENT_TERMINAL`, `OPERATOR_CONSOLE`;
- validation failure/error codes.

Unknown values must fail closed until a future controlled-code compatibility slice approves them.

## 7. Read-Model Changes Required

Add additive readback fields for:

- fiscal document read API;
- fiscal document presentation read model;
- internal audit/reconciliation read model.

Customer-facing presentation must use labels and amounts, not internal UUIDs, unless a legal or operational requirement explicitly demands an internal reference.

Internal readback may expose:

- decision command ID;
- application command ID;
- validation ID;
- policy reference;
- tariff snapshot IDs;
- terminal-cash tender ID;
- payment refs;
- applied timestamp;
- source channel.

## 8. Presentation Changes Required

Digital Sales Invoice presentation must preserve the existing POS Server-owned presentation shape and add statutory sections/rows only from the immutable fiscal document snapshot.

Customer-visible presentation should include:

- statutory discount label;
- entitlement label where required;
- benefit label where required;
- statutory discount amount;
- VAT treatment and VAT amount;
- final total;
- safe policy or ordinance reference only if required.

Reprints must show the original immutable statutory facts and current reprint metadata. Reprints must not re-resolve current Central PMS policy or evidence.

## 9. Central PMS Gateway Changes Required

Central PMS must:

- send `appliedStatutoryFiscalFacts` only after final payable-basis application;
- ensure decision and application linkage;
- ensure payment/finality linkage;
- exclude evidence and raw identity data;
- preserve correlation ID;
- use deterministic idempotency keys;
- revalidate payable basis before fiscal issuance;
- never send pending-review state as final fiscal facts;
- stop using generic context-only statutory fields once the first-class POS object is available.

Required Central PMS mapper additions:

- statutory payable-basis application command ID;
- statutory request reference;
- validation ID as first-class;
- site group ID;
- benefit classification;
- VAT amount;
- full policy reference object;
- terminal-cash tender ID when applicable.

## 10. Test Plan

Runtime tests:

- ordinary request without statutory object remains accepted;
- statutory request missing object is rejected when statutory mode is requested;
- final statutory request is accepted and snapshotted;
- pending/rejected/not-decided/application-failed states are rejected;
- missing required fields report deterministic error codes;
- prohibited privacy fields are rejected;
- unsupported entitlement, benefit, VAT, or source channel values are rejected;
- monetary mismatch rejects before fiscal document creation;
- same key/same facts replays original document and statutory snapshot;
- same key/changed statutory facts conflicts;
- evidence-only differences cannot enter the request.

Persistence tests:

- statutory snapshot inserts atomically with fiscal document;
- issuance failure creates neither document nor snapshot;
- duplicate application command ID cannot create a second fiscal document;
- indexes support decision/application/payment/session reconciliation;
- immutable snapshot cannot be updated through normal write paths.

Presentation tests:

- customer-visible statutory labels and amounts render from snapshot;
- internal UUIDs are absent from customer-facing display unless explicitly allowed;
- readback exposes internal audit refs only in governed APIs;
- reprint preserves old statutory facts.

Semantic hash tests:

- existing `sha256:v1` fixture remains unchanged;
- new statutory source version canonicalizes null/absent optional fields safely;
- changed application ID, entitlement, benefit, VAT, discount, final amount, policy, tariff snapshot, or payment refs changes the hash.

## 11. Migration Sequence

Recommended implementation sequence:

1. Add DTOs and validation in runtime without changing existing ordinary behavior.
2. Add new semantic hash source version and fixtures.
3. Add immutable PostgreSQL snapshot table and controlled-code posture.
4. Integrate snapshot persistence into fiscal issuance transaction.
5. Add readback and presentation projection from snapshot.
6. Update Central PMS mapper to send first-class statutory facts.
7. Run POS and Central PMS parity tests.
8. Run controlled UAT only after end-to-end payment-to-fiscal statutory flow is ready.

## 12. Rollback Posture

Before production enablement:

- ordinary non-statutory requests remain on `sha256:v1`;
- statutory object handling should be gated until Central PMS is coordinated;
- no migration of existing fiscal documents is required.

After production enablement:

- immutable statutory snapshot rows must not be deleted or rewritten as rollback;
- rollback must disable new statutory submissions at the integration boundary and preserve stored fiscal documents;
- replays of accepted statutory requests must continue to return original results.

## 13. UAT Scenarios

Required later UAT:

- WebPay Senior Citizen final applied payment to POS fiscal issuance;
- WebPay PWD final applied payment to POS fiscal issuance;
- APT terminal-cash statutory payment to POS fiscal issuance;
- ordinary payment while review is pending, then no retroactive fiscal adjustment after late approval;
- free-parking benefit behavior;
- same-key replay after Central PMS restart;
- same-key conflict on changed final amount;
- POS restart after completion-unknown posture;
- Digital Sales Invoice customer-visible statutory presentation;
- internal audit/readback reconciliation by decision/application/payment refs.

Significant manual testing for this documentation-only slice: No.

Controlled UAT authorization for runtime behavior: not authorized until the implementation slice exists and passes focused proof.

## 14. Blockers

No blocker remains for this contract-first slice.

Runtime implementation blockers to resolve in later branches:

- choose normalized table versus constrained JSON snapshot;
- confirm free-parking tender representation;
- confirm exact customer-visible policy/ordinance label requirement;
- coordinate new POS semantic source version with Central PMS;
- add Central PMS mapper fields not currently sent to POS Server.

## 15. Recommended Next Implementation Branch

Persona: Codex Z.

Repository: `D:\SourceCodes\ExitPass-PoSServer`.

Branch: `feature/fiscal-issuance-applied-statutory-facts-runtime`.

Objective: implement first-class POS Server request DTOs, validation, semantic hash v2, persistence snapshot, readback, and presentation integration for applied statutory fiscal facts.

Required predecessor: this contract document merged into `origin/dev`.

Recommended commit message for this contract slice:

`docs: freeze applied statutory fiscal facts contract`
