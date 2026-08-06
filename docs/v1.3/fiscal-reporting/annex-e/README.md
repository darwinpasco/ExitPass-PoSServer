# ExitPass POS Server BIR Annex E Contract Package v1.0

## Purpose

This package records the source-grounded contract analysis required before an Annex E runtime is authorized. It does not implement report generation, persistence, an API, submission, signing, or encryption.

## Reading order

1. [Source and Applicability Assessment](ExitPass_POS_Server_BIR_Annex_E_Source_and_Applicability_Assessment_v1.0.md)
2. [Decision Resolution Summary](ExitPass_POS_Server_BIR_Annex_E_Decision_Resolution_Summary_v1.0.md)
3. [Open Decisions and Assumptions Register](ExitPass_POS_Server_BIR_Annex_E_Open_Decisions_and_Assumptions_Register_v1.0.md)
4. [User Approval Record](ExitPass_POS_Server_BIR_Annex_E_User_Approval_Record_v1.0.md)
5. [External Confirmation Register](ExitPass_POS_Server_BIR_Annex_E_External_Confirmation_Register_v1.0.md)
6. [Field Dictionary](ExitPass_POS_Server_BIR_Annex_E_Field_Dictionary_v1.0.md)
7. [Source-to-Output Mapping Matrix](ExitPass_POS_Server_BIR_Annex_E_Source_to_Output_Mapping_Matrix_v1.0.md)
8. [Calculation and Reconciliation Rules](ExitPass_POS_Server_BIR_Annex_E_Calculation_and_Reconciliation_Rules_v1.0.md)
9. [File Layout and Export Specification](ExitPass_POS_Server_BIR_Annex_E_File_Layout_and_Export_Specification_v1.0.md)
10. [Sample Output and Traceability](ExitPass_POS_Server_BIR_Annex_E_Sample_Output_and_Traceability_v1.0.md)
11. [Validation and Acceptance Scenarios](ExitPass_POS_Server_BIR_Annex_E_Validation_and_Acceptance_Scenarios_v1.0.md)
12. [Z-009 Runtime Implementation Handoff](ExitPass_POS_Server_Z009_Annex_E_Runtime_Implementation_Handoff_v1.0.md)
13. [Runtime Authorization Checklist](ExitPass_POS_Server_Z009_Annex_E_Runtime_Authorization_Checklist_v1.0.md)

## Source hierarchy

1. Local RMO 24-2023 and Annex artifacts under `D:\Docs\ExitPass\POS`.
2. Local accreditation checklist, minutes, samples, and project review material in the same folder.
3. Approved ExitPass POS/Invoicing BRD, POS Server System Design, and POS Server API Contract.
4. Merged POS Server reporting contracts, schemas, runtimes, presentations, and audits.
5. Recommendations in this package, which are not approvals.

No web source was used. ARTS POSLog v6.0 was inventoried but is not authority for an Annex E layout.

## Classification vocabulary

Every material rule uses one of: `EXPLICITLY_REQUIRED`, `EXPLICITLY_ALLOWED`, `EXPLICITLY_PROHIBITED`, `EXISTING_EXITPASS_DECISION`, `RECOMMENDED_FOR_APPROVAL`, `UNRESOLVED`, or `NOT_APPLICABLE`.

## Readiness

**Runtime design: `AUTHORIZED_FOR_RUNTIME_DESIGN` by `Z-009B-USER-APPROVAL-001`.**

**Bounded E-1 implementation: `BLOCKED_PENDING_EXTERNAL_CONFIRMATION`.**

The resolution model preserves AE-DR-001 through AE-DR-024 and adds 11 lettered sub-decisions where regulatory and project authority had to be separated. All 13 project-owned recommendations were approved by `Z-009B-USER-APPROVAL-001`. Fourteen decisions still require external confirmation; E-2 through E-5 are deferred to separate privacy-authorized work. Controlled UAT and production remain unauthorized.
