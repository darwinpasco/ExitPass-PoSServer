# ExitPass POS Server First Physical Object Slice Object Plan

## 1. Purpose

This object plan defines the documentation-only plan for the first future physical object artifact slice. It identifies candidate future object areas and provisional candidate names without creating SQL/object artifacts.

## 2. Object Planning Matrix

| Area | Purpose | Candidate future objects, provisional only | Key/reference posture | Dependencies | Authority-boundary safeguards | Readiness | Blocked items | Future artifact task notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Foundation / configuration / controlled-code posture | Prepare common status, type, reason, and classification families for future object groups. | Provisional examples: `fiscal_document_status_codes`, `channel_terminal_type_codes`, `channel_terminal_capability_codes`, `fiscal_identity_status_codes`, `bir_report_classification_codes`, `reason_code_sets`. | Use internal `_id` only if represented as records; source/BIR values should retain source reference fields where needed. | Approved schema/naming baseline and enum/control-code strategy. | Codes must not imply payment finality, ExitAuthorization, independent terminal fiscal authority, or offline issuance approval. | Ready for planning; ready for artifacts only after per-domain enum vs controlled-code decision. | Final enum/table choice, reference-data format, controlled-code governance. | Future task should first decide which code families are needed in slice 1 and whether they are object artifacts or deferred reference-data artifacts. |
| Fiscal identity / Site POS Server boundary | Prepare Site POS Server fiscal authority boundary, taxpayer/registration identity, and effective-dated identity posture. | Provisional examples: `site_pos_servers`, `fiscal_identities`, `site_pos_server_fiscal_identity_history`, `fiscal_identity_configuration_refs`. | POS-owned boundary identifiers use `_id`; Central PMS site/business references use `central_pms_*_ref`; supplier/vendor/accreditation identifiers use `_ref`. | Foundation/status posture; BIR/accreditation metadata confirmation. | Site POS Server is fiscal authority for the resolved Site; this area must not model Central PMS site resolution ownership. | Ready for planning; partially blocked for artifacts pending BIR/accreditation field confirmation. | MIN/PTU/serial/software/supplier assignment, uniqueness rules, effective-dating rules. | Future task may define object skeleton candidates only after field and uniqueness posture are explicitly approved or placeholder policy is accepted. |
| Channel / terminal registry | Prepare child endpoint registry for WebPay, APM, Cashier POS, EC Device/continuity terminal, operator-assisted, and future channels. | Provisional examples: `channel_terminals`, `channel_terminal_capabilities`, `channel_terminal_status_history`, `channel_terminal_configuration_refs`. | POS-owned registry records use `_id`; external/logical identifiers use `_ref`; capabilities and statuses use domain-specific code names. | Fiscal identity / Site POS Server boundary; capability/status code posture. | Channels/terminals are child endpoints under Site POS Server and are not independent fiscal authorities; ONLINE/OFFLINE is observability only. | Ready for planning; artifacts blocked pending final registry fields and capability model. | WebPay label confirmation, physical/logical identity fields, status/capability families. | Future task should support logical/non-physical channels and not require printer/hardware serial unless confirmed. |
| Central PMS reference naming posture | Prepare authority-safe naming for external Central PMS and vendor references used by later fiscal objects. | Provisional examples: `central_pms_reference_contexts`, `vendor_ack_refs`, `reconciliation_refs`, or embedded reference fields using `central_pms_*_ref`. | Use `central_pms_parking_session_ref`, `central_pms_site_resolution_ref`, `central_pms_payment_attempt_ref`, `central_pms_payment_confirmation_ref`, `payment_finality_ref`, `vendor_ack_ref`, and similar source-specific references. | Approved authority boundary and schema/naming baseline. | References only; no POS-owned PaymentAttempt, PaymentConfirmation, payment finality, ExitAuthorization, gate execution, or vendor authority lifecycle. | Ready as naming posture; object artifacts blocked until reference storage approach is approved. | Whether references are embedded fields, shared reference records, or both; Central PMS reference formats. | Future task should validate reference-only naming and prohibit authority-leaking object names. |

## 3. Explicitly Excluded Object Areas

The first future physical object slice must not include objects for fiscal document issuance, Sales Invoice issuance, SI/adjustment numbering, fiscal counters, idempotency constraints/indexes, Digital SI URL token/access records, reprints, adjustments, reports, exports, audit trail physical objects, recovery/anchoring, optional events/outbox, validation scripts, rebuild scripts, drift scripts, or CI workflows.

## 4. Readiness Classification

| Classification | Meaning for first slice |
| --- | --- |
| Ready for future physical object artifact task | Planning is mature enough for a future task to propose object artifacts, subject to explicit artifact-task authorization. |
| Ready only with placeholder policy | The future artifact task can proceed only if the placeholder policy is explicitly accepted for the affected detail. |
| Blocked pending BIR/accounting | BIR/accounting confirmation is required before artifact creation. |
| Blocked pending Security/Privacy | Security/Privacy confirmation is required before artifact creation. |
| Blocked pending physical DB design | Final object, naming, field, key, or storage decisions are still needed. |
| Blocked pending Engineering/CI | Runtime/tooling/validation decisions are still needed. |
| Out of scope for first slice | Must not be included in the first-slice artifact task. |

## 5. Authority-Safe Naming Rules

Future first-slice artifact planning must use `pos` as the primary schema posture, lowercase `snake_case`, `_id` for internal identifiers, `_ref` for external references, `central_pms_*_ref` for Central PMS authority references, `vendor_ack_ref` or source-specific vendor references, and domain-specific status names.

Future first-slice artifact planning must not use names implying POS-owned payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, ExitAuthorization, gate execution, independent terminal fiscal authority, or offline fiscal issuance approval.
