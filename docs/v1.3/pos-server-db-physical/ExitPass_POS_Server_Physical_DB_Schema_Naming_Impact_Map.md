# ExitPass POS Server Physical DB Schema Naming Impact Map

## 1. Summary

This impact map links schema and naming decisions to future physical database artifact workstreams. It does not create SQL, object files, seed/reference data, scripts, CI workflows, source code, DOCX files, or diagrams.

## 2. Workstream Impact Matrix

| Workstream | Naming impact | Required future action |
| --- | --- | --- |
| `db/state/schemas` | Default primary schema posture is `pos`; additional schemas require approval. | Confirm schema split before schema SQL artifacts. |
| `db/state/tables` | Table names use lowercase `snake_case`, plural collection names, and domain clarity. | Confirm final table list and table file layout. |
| `db/state/views` | View names must be domain-clear and may use `vw_` if approved. | Confirm view strategy and inventory. |
| `db/state/functions` | Routine names must be purpose-clear and avoid hidden business authority. | Confirm whether DB routines are approved. |
| `db/state/triggers` | Trigger names must include table, timing/event, and purpose. | Confirm trigger policy before artifacts. |
| `db/state/types` | Enum/type names are domain-specific; enum vs controlled-code remains per-domain. | Confirm type strategy before type artifacts. |
| `db/state/sequences` | Sequence names must distinguish SI, adjustment family, reset, Z-counter, and technical sequences. | Confirm fiscal numbering/counter strategy or placeholder policy. |
| `db/state/policies` | Policy names must express protected object and purpose. | Confirm Security/Privacy and PostgreSQL policy strategy. |
| `db/reference-data` | Controlled-code sets should use domain-specific names and preserve historical readability. | Confirm reference-data format and governance. |
| `db/validation` | Validation script names should identify category and target. | Confirm validation tooling and evidence naming. |
| `db/rebuild` | Rebuild script names should identify target environment or workflow. | Confirm rebuild tooling and manifest strategy. |
| `db/drift` | Drift script/report names should identify comparison target and evidence purpose. | Confirm drift-check tooling and retention. |
| Engineering Pack | Naming must align with API semantics without mirroring DTOs directly. | Confirm event/outbox, routine, and generated-artifact strategy. |
| Security/Privacy Review | Names must avoid authority leakage and protect Digital SI URL/evidence concepts. | Confirm token/access/evidence naming and policy needs. |
| BIR/accreditation package | Names must preserve SI, EJ, BIR, VAT, MIN, PTU, GTA, and POSLog terminology. | Confirm final examiner package/profile naming. |

## 3. Authority Boundary Impact

| Boundary area | Naming safeguard | Future validation impact |
| --- | --- | --- |
| Payment finality | Use `payment_finality_ref` or `central_pms_payment_finality_ref`; do not create POS-owned finality names. | Validate against POS-owned finality object names. |
| PaymentAttempt | Use `central_pms_payment_attempt_ref`; avoid POS-owned lifecycle names. | Flag ambiguous payment attempt ownership. |
| PaymentConfirmation | Use `central_pms_payment_confirmation_ref`; avoid POS-owned lifecycle names. | Flag ambiguous confirmation ownership. |
| ExitAuthorization | Use `central_pms_exit_authorization_ref` only if approved as a reference. | Flag POS-owned exit authorization artifacts. |
| Gate execution | Avoid gate authority names. | Flag gate execution ownership language. |
| Vendor PMS | Use `vendor_ack_ref` or source-specific acknowledgement refs. | Flag vendor authority naming. |
| Terminal fiscal role | Use channel/terminal registry names; avoid independent terminal fiscal issuer names. | Flag terminal fiscal authority leakage. |

## 4. Future Artifact Sequencing Impact

| Sequence step | Naming package dependency | Notes |
| --- | --- | --- |
| Physical object design package | Use this package as naming baseline. | Still must confirm PostgreSQL details and object scope. |
| Schema artifact creation | Use `pos` default unless additional schema approval exists. | No schema SQL is created by this package. |
| Table artifact creation | Apply table, column, key, constraint, and index naming standards. | Final table list remains separate. |
| Reference-data artifact creation | Apply controlled-code naming standards. | Reference-data files are not created by this package. |
| Validation/rebuild/drift scripts | Apply file naming standards after tooling is selected. | Scripts are not created by this package. |
| Accreditation/sample artifacts | Preserve BIR terminology and source policy. | Sample/accreditation data is not created here. |

## 5. Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Over-fragmented schemas | Harder rebuild, validation, and review. | Use primary `pos` schema first; require approval for additional schemas. |
| Ambiguous authority names | POS Server may appear to own payment or exit authority. | Use `_ref`, `central_pms_*_ref`, and authority-boundary validation. |
| DTO names mirrored into tables | API contract may become coupled to persistence shape. | Use persistence semantics and logical DB design as source, not DTOs. |
| BIR terminology diluted by ARTS terms | Accreditation outputs may conflict with BIR requirements. | Preserve SI, EJ, BIR, VAT, MIN, PTU, GTA, and Annex terminology. |
| Script naming drift | CI/rebuild/drift evidence becomes hard to review. | Apply deterministic script naming after tooling decision. |
| Hidden local drift | Local DB changes could become de facto baseline. | Preserve no-local-drift promotion rule and state-based source of truth. |

## 6. Recommended Next Step

Complete approval-readiness review for the schema and naming standards package. After approval, use this package as the naming baseline for the next physical object design package without creating SQL/object artifacts until the remaining physical gates are resolved or explicitly handled by placeholder policy.
