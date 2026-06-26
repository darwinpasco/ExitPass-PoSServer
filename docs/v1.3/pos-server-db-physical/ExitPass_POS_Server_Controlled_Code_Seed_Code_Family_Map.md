# ExitPass POS Server Controlled-Code Seed Code Family Map

## 1. Purpose

This map identifies controlled-code families implied by the current POS Server database objects from slices 1 through 8.

All family names and sample code-set keys in this document are planning names only. No seed values are created by this package.

## 2. Mapping Conventions

Recommended future code-set key posture:

- lowercase `snake_case`
- domain-specific family names
- no names implying POS-owned payment finality, `PaymentAttempt`, `PaymentConfirmation`, `ExitAuthorization`, gate execution authority, independent terminal fiscal authority, or offline fiscal issuance approval
- stable keys suitable for repeatable seed/reference loading

## 3. Code Family Matrix

| Area | SQL columns | Proposed code-set family key | Reference data posture |
| --- | --- | --- | --- |
| Fiscal document types | `fiscal_document_type_code_id`, `document_type_code_id` | `fiscal_document_type` | Baseline values such as SI and future adjustment document families require BIR/accounting approval. |
| Fiscal document statuses | `fiscal_document_status_code_id`, `prior_fiscal_document_status_code_id`, `new_fiscal_document_status_code_id` | `fiscal_document_status` | Must describe POS fiscal document lifecycle only, not payment or exit finality. |
| Fiscal document status reasons | `status_reason_code_id` on fiscal document status history | `fiscal_document_status_reason` | Reason values should remain operational/fiscal-context only. |
| Fiscal document link types | `fiscal_document_link_type_code_id` | `fiscal_document_link_type` | Generic document relationship posture only. |
| Fiscal document link reasons | `link_reason_code_id` | `fiscal_document_link_reason` | Should not create adjustment/refund workflow authority. |
| Fiscal line types | `line_type_code_id` | `fiscal_line_type` | Requires product/service/fee line treatment review. |
| Fiscal line statuses | `line_status_code_id` | `fiscal_line_status` | Optional detail lifecycle only. |
| Tender types | `tender_type_code_id` | `tender_type` | Tender context only; no payment finality ownership. |
| Tax types | `tax_type_code_id` | `tax_type` | Requires accounting/BIR review. |
| Tax classifications | `tax_classification_code_id` | `tax_classification` | VATable, VAT-exempt, zero-rated, non-VAT, and future categories require accounting/BIR confirmation. |
| Discount / privilege types | `discount_privilege_type_code_id` | `discount_privilege_type` | Must distinguish statutory discount, commercial discount, and VAT privilege/exemption posture. |
| Fiscal total types | `total_type_code_id` | `fiscal_total_type` | Gross, discount, tax, VAT privilege, net, tendered, and related totals require accounting review. |
| Site POS Server operational statuses | `operational_status_code_id` on `site_pos_servers` | `site_pos_server_operational_status` | Operational posture only. |
| Fiscal identity statuses | `fiscal_identity_status_code_id` | `fiscal_identity_status` | Registration/accreditation dependent. |
| Fiscal identity assignment reasons | `assignment_reason_code_id` | `fiscal_identity_assignment_reason` | Effective-dated assignment reason only. |
| Channel/terminal types | `channel_terminal_type_code_id` | `channel_terminal_type` | WebPay, APM, cashier POS, EC device, and future endpoint categories. |
| Channel/terminal health statuses | `health_status_code_id`, `prior_health_status_code_id`, `new_health_status_code_id` | `channel_terminal_health_status` | ONLINE/OFFLINE observability only; no offline fiscal issuance approval. |
| Channel/terminal operational statuses | `operational_status_code_id` on `channel_terminals` | `channel_terminal_operational_status` | Active/inactive/degraded/continuity posture. |
| Channel/terminal status reasons | `status_reason_code_id` on channel status history | `channel_terminal_status_reason` | Operational reason only. |
| Channel/terminal capabilities | `capability_code_id` | `channel_terminal_capability` | Print/display/Digital SI URL/QR presentation capability posture. |
| Sequence families | `sequence_family_code_id` | `fiscal_sequence_family` | SI and adjustment sequence families without allocation behavior. |
| Sequence policy statuses | `current_policy_status_code_id` | `fiscal_sequence_policy_status` | Policy lifecycle only. |
| Sequence states | `sequence_state_code_id` | `fiscal_sequence_state` | Reserved/issued/failed/abandoned posture only; no allocation function. |
| Sequence gap reasons | `gap_reason_code_id` | `fiscal_sequence_gap_reason` | Permanent gap audit posture; no reuse approval. |
| Counter families | `counter_family_code_id` | `fiscal_counter_family` | Reset counter, Z-counter, GTA, and related posture. |
| Counter statuses | `counter_status_code_id` | `fiscal_counter_status` | State/status only; no counter increment behavior. |
| Fiscal snapshot types | `snapshot_type_code_id` | `fiscal_state_snapshot_type` | Continuity snapshot classification. |
| Fiscal snapshot statuses | `snapshot_status_code_id` | `fiscal_state_snapshot_status` | Snapshot lifecycle/status only. |
| Fiscal lock states | `lock_state_code_id` | `fiscal_lock_state` | Lock/block/resume posture only. |
| Fiscal lock reasons | `lock_reason_code_id` | `fiscal_lock_reason` | Reason values require operations and fiscal safety review. |
| Idempotency operation types | `operation_type_code_id` | `idempotency_operation_type` | Fiscal side-effect protection only. |
| Idempotency operation statuses | `operation_status_code_id` | `idempotency_operation_status` | Does not create payment or exit authority. |
| Retry statuses | `retry_status_code_id` | `fiscal_operation_retry_status` | Retry attempt posture only. |
| Retry reasons | `retry_reason_code_id` | `fiscal_operation_retry_reason` | Operational reason values. |
| Exception types | `exception_type_code_id` | `fiscal_operation_exception_type` | Exception classification without recovery automation. |
| Exception statuses | `exception_status_code_id` | `fiscal_operation_exception_status` | Exception lifecycle posture. |
| Digital SI URL statuses | `digital_si_url_status_code_id` | `digital_si_url_status` | URL lifecycle only; no fiscal mutation path. |
| Digital SI URL status reasons | `status_reason_code_id` on Digital SI URLs | `digital_si_url_status_reason` | Security/Privacy review required. |
| Digital SI access event types | `access_event_type_code_id` | `digital_si_access_event_type` | Access evidence posture only. |
| Digital SI access results | `access_result_code_id` on Digital SI access events | `digital_si_access_result` | No raw token/credential values. |
| Reprint types | `reprint_type_code_id` | `reprint_type` | Required reprint posture without new fiscal numbers. |
| Reprint statuses | `reprint_status_code_id` | `reprint_status` | Request lifecycle only. |
| Reprint reasons | `reprint_reason_code_id` | `reprint_reason` | Requires operations/BIR/accounting review. |
| Reprint output types | `output_type_code_id` on reprint outputs | `reprint_output_type` | Print/PDF/reference metadata only. |
| Adjustment types | `adjustment_type_code_id` | `fiscal_adjustment_type` | Void/refund/cancel/return or BIR-confirmed families later. |
| Adjustment statuses | `adjustment_status_code_id`, `prior_adjustment_status_code_id`, `new_adjustment_status_code_id` | `fiscal_adjustment_status` | Adjustment lifecycle only; no money movement finality. |
| Adjustment reasons | `adjustment_reason_code_id`, `status_reason_code_id` on adjustment status history | `fiscal_adjustment_reason` | Requires accounting/BIR review. |
| Report types | `report_type_code_id` | `fiscal_report_type` | X-read, Z-read, BIR Sales Summary, Annex E, and future approved reports. |
| Report statuses | `report_status_code_id` | `fiscal_report_status` | Report request lifecycle only. |
| Report scopes | `scope_type_code_id` | `fiscal_report_scope_type` | Site/channel/business-day/range/counter scope posture. |
| Report output types | `output_type_code_id` on report outputs | `fiscal_report_output_type` | Print/PDF/JSON references only. |
| Report output statuses | `output_status_code_id` | `fiscal_report_output_status` | Output reference lifecycle only. |
| X/Z report kinds | `report_kind_code_id` | `x_z_report_kind` | X-read vs Z-read posture; no counter increment behavior. |
| Annex E types | `annex_e_type_code_id` | `annex_e_type` | E-1 to E-5 applicability requires BIR/accreditation confirmation. |
| Export types | `export_type_code_id` | `fiscal_export_type` | EJ, POSLog, JSON, report export, and future approved export families. |
| Export statuses | `export_status_code_id` | `fiscal_export_status` | Export request lifecycle only. |
| Export package types | `export_package_type_code_id` | `export_package_type` | Package metadata only. |
| Export package statuses | `export_package_status_code_id` | `export_package_status` | Package lifecycle only. |
| Export item types | `export_item_type_code_id` | `export_item_type` | Document/report/output/EJ references. |
| Export validation statuses | `validation_status_code_id` | `export_validation_status` | Validation evidence posture only. |
| Export validation severities | `validation_severity_code_id` | `export_validation_severity` | Severity classification only. |
| Export schema/profile types | `profile_type_code_id` | `export_schema_profile_type` | ARTS POSLog, BIR/local JSON, EJ, and future profiles as references only. |
| Fiscal action audit action types | `audit_action_type_code_id` | `fiscal_action_audit_action_type` | Evidence posture only; no event publishing. |
| Fiscal action audit result types | `audit_result_code_id` | `fiscal_action_audit_result` | Evidence result only. |
| Privileged action types | `privileged_action_type_code_id` | `privileged_action_type` | Approval/reference evidence only; no IAM/RBAC. |
| Privileged action results | `privileged_action_result_code_id` | `privileged_action_result` | Result posture only. |
| Privileged action reasons | `reason_code_id` on privileged audit | `privileged_action_reason` | Operations/security review required. |
| Configuration areas | `configuration_area_code_id` | `configuration_area` | POS Server, fiscal identity, terminal, sequence, profile, and code configuration areas. |
| Configuration actions | `configuration_action_code_id` | `configuration_action` | Create/update/activate/deactivate and similar posture. |
| Configuration reasons | `reason_code_id` on configuration audit | `configuration_change_reason` | Reason posture only. |
| Access audit subject types | `access_subject_type_code_id` | `access_subject_type` | Digital SI, report, export, and privileged fiscal resources. |
| Access audit actions | `access_action_code_id` | `access_action` | View/download/print/access posture only. |
| Access audit results | `access_result_code_id` on access audit | `access_result` | Success/denied/error posture; no credential storage. |
| Recovery types | `recovery_type_code_id` | `recovery_type` | Supervised recovery posture only. |
| Recovery statuses | `recovery_status_code_id` | `recovery_status` | Request lifecycle only. |
| Recovery reasons | `recovery_reason_code_id` | `recovery_reason` | Requires fiscal safety/operations review. |
| Continuity check types | `continuity_check_type_code_id` | `continuity_check_type` | Counter/hash/range/check posture. |
| Continuity check results | `continuity_check_result_code_id` | `continuity_check_result` | Check result evidence only. |
| Anchor types | `anchor_type_code_id` | `fiscal_anchor_type` | Tamper-evident reference posture only. |
| Anchor statuses | `anchor_status_code_id` | `fiscal_anchor_status` | Anchor reference lifecycle only. |
| Security reference types | `security_reference_type_code_id` | `security_reference_type` | Actor/service/approval/evidence grouping only. |
| Security reference statuses | `security_reference_status_code_id` | `security_reference_status` | Reference lifecycle only. |
| Privacy classifications | `privacy_classification_code_id` | `privacy_classification` | Security/Privacy approval required. |

## 4. Minimum Future Metadata Per Family

Future source records should include, at minimum:

- `code_set_key`
- `display_name`
- `description`
- `governance_owner`
- `source_ref`
- `code_key`
- `sort_order`
- `is_active`
- effective date posture

## 5. Explicit Exclusions

The family map does not include:

- sample fiscal document rows
- sample tender/payment rows
- sample report/export output rows
- sample audit/recovery rows
- user/customer data
- raw evidence/token/credential data
- authority-owned Central PMS lifecycle values
