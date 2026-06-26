# ExitPass POS Server Physical DB Artifact Plan v1.0 Outline

## Purpose

This outline proposes the future structure for `ExitPass_POS_Server_Physical_DB_Artifact_Plan_v1.0.md`.

This task does not create the final physical database artifact plan, SQL files, Atlas files, migrations, physical database object files, validation scripts, rebuild scripts, drift-check scripts, seed/reference data, or CI workflows.

## Proposed Document Structure

### 1. Document Control

- Document title.
- Version.
- Product scope.
- Repository: `ExitPass-PoSServer`.
- Status.
- Output format.
- Approved baseline references.
- Physical artifact status.

### 2. Purpose and Scope

- Purpose of the physical DB artifact plan.
- In-scope artifact planning.
- Out-of-scope items.
- Relationship to approved logical Database Design.

### 3. Approved Baseline References

- Approved POS/Invoicing BRD.
- Approved POS Server System Design.
- Approved POS Server API Contract.
- Approved POS Server Database Design.
- Open Questions Resolution Addendum.
- Technical and approval-readiness reviews.
- Repository Boundary.
- External reference inventory.

### 4. Physical Design Gate

- Gate checklist from approved Database Design.
- Gate status.
- Required confirmations.
- Decision owners.
- No artifact creation before gate completion.

### 5. Target Database Engine

- Recommended default: PostgreSQL.
- Version/edition/features pending confirmation.
- Extension policy.
- Deployment/runtime assumptions.
- Engine-specific risks.

### 6. Repository DB Artifact Layout

- Proposed `db/` root.
- Proposed `db/state/` folders.
- Proposed seed/reference/sample/accreditation separation.
- Proposed validation/rebuild/drift/script folders.
- Creation status and approval gate.

### 7. Schema / Domain Decomposition

- Candidate logical domains.
- Provisional physical schema grouping.
- Domain ownership and authority boundaries.
- Cross-domain dependency rules.

### 8. Naming Standards

- Schema naming.
- Table naming.
- Column naming.
- Key naming.
- Constraint naming.
- Index naming.
- Type/enum naming.
- Sequence naming.
- Function/routine naming.
- Trigger naming.
- View naming.
- Script and data file naming.

### 9. Object-Level State File Strategy

- One file per object where practical.
- Manifest/order strategy.
- Dependency handling.
- Generated versus hand-authored boundary.
- Reviewability of diffs.

### 10. Rebuild Strategy

- Clean database creation.
- Apply object state.
- Load seed/reference data.
- Run validation.
- Capture rebuild evidence.
- Local dev and CI usage.

### 11. Drift Check Strategy

- Repository state as source of truth.
- Compare live/test DB to repo state.
- Drift report categories.
- No automatic drift promotion.
- PR flow for accepted database changes.
- Optional Atlas/state-based comparison.

### 12. Validation Strategy

- Rebuild validation.
- Schema/object inventory validation.
- Constraint/index validation.
- Controlled-code validation.
- Authority boundary validation.
- Fiscal numbering/counter validation.
- Digital SI URL validation.
- Audit/recovery validation.
- Report/export validation.
- Security/privacy validation.
- Drift-check validation.

### 13. Seed and Reference Data Strategy

- Object definitions.
- Seed data.
- Reference data.
- Controlled code sets.
- Sample/accreditation data.
- Test fixtures.
- Environment-specific data.
- Secrets and external references.

### 14. Enum vs Controlled-Code Strategy

- Stable lifecycle states.
- Evolving classifications.
- BIR/accreditation codes.
- Operational reason codes.
- Historical readability.
- Migration/change governance.

### 15. Idempotency and Fiscal Numbering Strategy

- Idempotency keys and scope.
- Semantic request identity.
- Retry, timeout, completion unknown.
- SI sequence policy.
- Adjustment sequence policy.
- Reserved/issued/failed/abandoned state.
- Sequence-gap audit.
- No duplicate fiscal documents.
- BIR/accounting confirmation dependency.

### 16. Digital SI URL Security Gate

- Token/access reference.
- Lifecycle status.
- Issue/expiry timestamps.
- Access audit.
- Privacy/data minimization.
- Read-only customer view.
- Security/Privacy Review dependency.

### 17. Reports, EJ, POSLog, JSON, and Accreditation Outputs

- Report metadata.
- Output references.
- Print/PDF/JSON output modes.
- EJ and POSLog metadata.
- ARTS POSLog 6.x profile reference.
- Local/BIR extension mapping.
- Validation status/errors.
- Sample output data.
- Accreditation support.

### 18. Tamper-Evident Recovery and Anchoring

- Latest and previous fiscal state.
- Counters and GTA.
- Latest EJ hash.
- Last fiscal event timestamp.
- Hash chaining where practical.
- External anchor reference if used.
- Supervised recovery.
- Recovery block/resume status.
- Security/Engineering confirmation dependency.

### 19. CI / PR Review Evidence

- Rebuild success.
- Validation success.
- Drift-check result.
- Schema inventory.
- Constraint inventory.
- Index inventory.
- Seed/reference validation.
- Authority-boundary checklist.
- Security/privacy/BIR impact notes.

### 20. Open Questions

- BIR/accounting.
- Security/Privacy.
- Physical DB design.
- Engineering Pack.
- BIR/accreditation package.
- Operations.
- CI/CD.
- Vendor/supplier.

### 21. Risks and Mitigations

- Authority leakage.
- Premature physical design.
- Local drift promotion.
- Incomplete fiscal numbering policy.
- Digital SI URL privacy exposure.
- ARTS POSLog replacing BIR outputs.
- Offline fiscal issuance implication.
- Incomplete validation evidence.
- Accreditation mismatch.

### 22. Non-Decisions

- Final SQL DDL.
- Final physical names.
- Final constraints/indexes.
- Final enum implementation.
- Final Atlas/migration approach.
- Final seed/reference/sample data.
- Final CI workflow.
- Final BIR/accreditation package.
- Offline fiscal issuance approval.

### 23. Appendices

- Glossary.
- Physical Design Gate checklist.
- Proposed folder layout table.
- PR evidence checklist.
- Authority boundary checklist.
- Source traceability.