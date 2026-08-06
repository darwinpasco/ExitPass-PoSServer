# ExitPass POS Server BIR Annex E Contract Package v1.0

## Purpose

This package records the source-grounded contract analysis required before an Annex E runtime is authorized. It does not implement report generation, persistence, an API, submission, signing, or encryption.

## Reading order

1. [Source and Applicability Assessment](ExitPass_POS_Server_BIR_Annex_E_Source_and_Applicability_Assessment_v1.0.md)
2. [Open Decisions and Assumptions Register](ExitPass_POS_Server_BIR_Annex_E_Open_Decisions_and_Assumptions_Register_v1.0.md)
3. [Field Dictionary](ExitPass_POS_Server_BIR_Annex_E_Field_Dictionary_v1.0.md)
4. [Source-to-Output Mapping Matrix](ExitPass_POS_Server_BIR_Annex_E_Source_to_Output_Mapping_Matrix_v1.0.md)
5. [Calculation and Reconciliation Rules](ExitPass_POS_Server_BIR_Annex_E_Calculation_and_Reconciliation_Rules_v1.0.md)
6. [File Layout and Export Specification](ExitPass_POS_Server_BIR_Annex_E_File_Layout_and_Export_Specification_v1.0.md)
7. [Sample Output and Traceability](ExitPass_POS_Server_BIR_Annex_E_Sample_Output_and_Traceability_v1.0.md)
8. [Validation and Acceptance Scenarios](ExitPass_POS_Server_BIR_Annex_E_Validation_and_Acceptance_Scenarios_v1.0.md)
9. [Z-009 Runtime Implementation Handoff](ExitPass_POS_Server_Z009_Annex_E_Runtime_Implementation_Handoff_v1.0.md)

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

**`BLOCKED_PENDING_DECISIONS`**

The local official workbook establishes five Annex E templates and the E-1 physical columns, but does not settle the external file packaging, grouping period, filename, correction lineage, mandatory null behavior, several E-1 formula interpretations, or sensitive E-2 through E-5 data authority. The decision register is the only source of readiness gates for Z-009.

Z-009 runtime implementation is not authorized by this package until every decision marked `BLOCKS_Z009` is explicitly approved and incorporated into a versioned contract.

