# ExitPass POS Server First Physical Object Slice Design v1.0

## 1. Document Control

| Field | Value |
| --- | --- |
| Title | ExitPass POS Server First Physical Object Slice Design |
| Version | v1.0 |
| Repository | `ExitPass-PoSServer` |
| Status | Approved baseline |
| Output format | Markdown only |
| Approved BRD baseline | `docs/v1.3/pos-invoicing/ExitPass_POS_Invoicing_BRD_v1.0.md` |
| Approved System Design baseline | `docs/v1.3/pos-server/ExitPass_POS_Server_System_Design_v1.0.md` |
| Approved API Contract baseline | `docs/v1.3/pos-server-api/ExitPass_POS_Server_API_Contract_v1.0.md` |
| Approved Database Design baseline | `docs/v1.3/pos-server-db/ExitPass_POS_Server_Database_Design_v1.0.md` |
| Approved Physical DB Artifact Plan baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Artifact_Plan_v1.0.md` |
| Approved Gate Resolution baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Gate_Resolution_v1.0.md` |
| Approved Schema/Naming Standards baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_DB_Schema_Naming_Standards_v1.0.md` |
| Approved Physical Object Design baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_Physical_Object_Design_v1.0.md` |
| Approved First Slice Planning baseline | `docs/v1.3/pos-server-db-physical/ExitPass_POS_Server_First_Physical_Object_Slice_Source_Analysis.md` |
| Physical artifact status | No SQL, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, or diagrams are created or authorized by this approved baseline. |

## Approval / Baseline Status

This document is approved as the First Physical Object Slice Design v1.0 baseline. It governs future first-slice physical object artifact tasks for foundation/configuration/controlled-code posture, fiscal identity / Site POS Server boundary, channel/terminal registry, and Central PMS reference naming posture.

Approval of this design baseline does not authorize creating SQL files, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation scripts, rebuild scripts, drift scripts, CI workflows, source code, DOCX files, or diagrams.

The approved first-slice design scope remains limited to foundation / configuration / controlled-code posture, fiscal identity / Site POS Server boundary, channel/terminal registry, and Central PMS reference naming posture.

The excluded areas remain out of scope: fiscal document issuance, Sales Invoice issuance objects, SI/adjustment numbering implementation, fiscal counters, idempotency physical constraints/indexes, Digital SI URL token/access objects, reprints, adjustments, reports, exports, audit trail physical objects, recovery/anchoring physical objects, optional events/outbox, validation/rebuild/drift scripts, and CI workflows.

Any future SQL/object artifact task must be separate and must include explicit checks for PostgreSQL assumptions, first-slice object names, enum versus controlled-code decisions, Central PMS reference-only naming, authority-boundary validation, and no local drift promotion.

## 2. Purpose and Scope

This document defines the first POS Server physical object slice design at document/design level only. It translates the approved First Physical Object Slice Planning Package into proposed first-slice object groups, provisional candidate object names, key/reference posture, validation expectations, open questions, and artifact readiness.

In scope:

- foundation / configuration / controlled-code posture;
- fiscal identity / Site POS Server boundary;
- channel/terminal registry;
- Central PMS reference naming posture;
- validation expectations for those areas;
- first-slice open questions and blockers.

Out of scope:

- SQL DDL;
- Atlas files;
- migrations;
- physical database object files;
- final table definitions;
- final column lists;
- final constraints/indexes;
- final enum/type implementation;
- seed/reference/sample data;
- validation/rebuild/drift scripts;
- CI workflows;
- source code;
- final BIR/accreditation package;
- offline fiscal issuance approval;
- separate database repository.

All object names in this document are proposed examples only. They remain provisional until a separate SQL/object artifact task creates actual files under `db/state`.

## 3. Approved Baseline References

| Source | Role in this design draft |
| --- | --- |
| POS/Invoicing BRD v1.0 | Business and fiscal scope baseline. |
| POS Server System Design v1.0 | Authority, repository, channel, and operational boundary baseline. |
| POS Server API Contract v1.0 | API/reference posture baseline. |
| POS Server Database Design v1.0 | Approved logical persistence baseline. |
| Physical DB Artifact Plan v1.0 | State-based artifact, rebuild, validation, and drift posture. |
| Physical DB Gate Resolution v1.0 | Gate and placeholder-policy posture. |
| Schema/Naming Standards v1.0 | Approved schema and naming baseline. |
| Physical Object Design v1.0 | Approved physical object design baseline and first-slice recommendation. |
| First Physical Object Slice Planning Package | Approved source, decision, object, open-question, validation, and outline baseline. |
| `docs/REPOSITORY_BOUNDARY.md` | Repository and authority boundary. |
| `db/README.md` | `db/` folder boundary and artifact restrictions. |

## 4. First Slice Rationale

The first slice is safe because it establishes low-risk database foundations without introducing fiscal side effects.

| Rationale | Design implication |
| --- | --- |
| Establish foundation/configuration posture first | Status, type, reason, and classification posture can be prepared before dependent fiscal objects. |
| Establish Site POS Server fiscal authority boundary | Future fiscal documents can reference a known fiscal authority boundary without being created now. |
| Establish channel/terminal registry before issuance references | Future issuance records can point to child endpoints without treating terminals as fiscal authorities. |
| Establish Central PMS reference naming posture | Later fiscal objects can reference Central PMS records without owning Central PMS lifecycle. |
| Avoid fiscal issuance side effects | This design does not create Sales Invoice issuance, fiscal document issuance, numbering, or counters. |
| Avoid payment or exit authority leakage | This design prohibits POS-owned payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization lifecycle naming. |

## 5. Authority Boundary

The first-slice design preserves these authority rules:

- Central PMS owns payment finality.
- Central PMS owns PaymentAttempt and PaymentConfirmation.
- Central PMS owns ExitAuthorization.
- POS Server owns fiscal issuance and fiscal records only.
- POS Server database stores references to Central PMS authority records only.
- POS Server database does not own payment finality.
- POS Server database does not own, issue, or mutate ExitAuthorization.
- Audit and events are evidence only.
- Channels/terminals are child endpoints under Site POS Server and are not independent fiscal authorities.
- ONLINE/OFFLINE is observability only.
- Offline fiscal issuance is disabled by default unless BIR/accounting approves a compliant model.
- ARTS POSLog is a structured export reference only and does not replace BIR outputs.

## 6. Schema and Naming Baseline

The first-slice design applies the approved naming baseline:

- primary schema posture: `pos`;
- lowercase `snake_case`;
- no quoted identifiers;
- `_id` for internal identifiers;
- `_ref` for external references;
- `central_pms_*_ref` for Central PMS authority references;
- `vendor_ack_ref` or source-specific vendor references;
- domain-specific status names.

The first-slice design explicitly prohibits names implying:

- POS-owned payment finality;
- POS-owned PaymentAttempt lifecycle;
- POS-owned PaymentConfirmation lifecycle;
- POS-owned ExitAuthorization;
- POS-owned gate execution;
- independent terminal fiscal authority;
- offline fiscal issuance approval.

## 7. Included Object Areas

| Included area | Design scope |
| --- | --- |
| Foundation / configuration / controlled-code posture | Candidate status, type, reason, and classification object groups. |
| Fiscal identity / Site POS Server boundary | Candidate Site POS Server and fiscal identity boundary object groups. |
| Channel/terminal registry | Candidate channel/terminal registry, capability, and status-history object groups. |
| Central PMS reference naming posture | Candidate reference naming and storage posture for Central PMS and vendor references. |

## 8. Excluded Object Areas

This design does not include object design for:

- fiscal document issuance;
- Sales Invoice issuance objects;
- SI/adjustment numbering implementation;
- fiscal counters;
- idempotency physical constraints/indexes;
- Digital SI URL token/access objects;
- reprints;
- adjustments;
- reports;
- exports;
- audit trail physical objects;
- recovery/anchoring physical objects;
- optional events/outbox;
- validation/rebuild/drift scripts;
- CI workflows.

These remain future tasks.

## 9. Foundation / Configuration / Controlled-Code Object Design

| Design item | Posture |
| --- | --- |
| Purpose | Prepare future status, type, reason, and classification value families that will be reused by later fiscal object groups. |
| Candidate future object groups | Status families, type families, reason-code families, BIR/report classification code families, channel type/capability code families, fiscal identity status code families. |
| Provisional candidate object names | `fiscal_document_status_codes`, `channel_terminal_type_codes`, `channel_terminal_capability_codes`, `fiscal_identity_status_codes`, `bir_report_classification_codes`, `reason_code_sets`. |
| Status/type/reason/classification posture | Values should be domain-specific and governed; evolving operational reasons and BIR/report classifications should prefer controlled-code governance unless proven stable enough for enum storage. |
| Enum vs controlled-code dependency | Final enum/table/control-code storage remains a per-domain physical DB design decision. |
| Authority-boundary safeguards | Codes must not imply payment finality, ExitAuthorization, gate execution, independent terminal fiscal authority, or offline fiscal issuance approval. |
| Validation expectations | Validate naming compliance, controlled-code/status posture, authority-boundary naming, and absence of unauthorized payment/exit authority names. |
| Artifact readiness | Ready for design; SQL/object artifacts are blocked until the enum vs controlled-code decision is confirmed per code family. |

Open questions:

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Which first-slice code families should become physical objects versus deferred reference data? | Code families are planned; enum vs controlled-code remains per-domain decision. | Blocks controlled-code artifacts. | Decide per code family before first artifact task. |
| Which BIR/report classification code families are needed in the first slice? | BIR/report code families are candidates only. | May block controlled-code artifacts. | Confirm values before reference/control-code artifacts. |

## 10. Fiscal Identity / Site POS Server Boundary Object Design

| Design item | Posture |
| --- | --- |
| Purpose | Prepare the Site POS Server fiscal authority boundary and fiscal identity posture without creating fiscal issuance objects. |
| Candidate future object groups | Site POS Server boundary records, fiscal identity records, taxpayer/business identity references, branch/site/business-unit mapping references, registration metadata, effective-dated identity history, identity configuration audit references. |
| Provisional candidate object names | `site_pos_servers`, `fiscal_identities`, `site_pos_server_fiscal_identity_history`, `fiscal_identity_configuration_refs`. |
| Taxpayer/business identity posture | Store only database-required taxpayer/business identity references or metadata; privacy-sensitive evidence remains subject to Security/Privacy confirmation. |
| Site POS Server fiscal authority posture | Site POS Server is the fiscal authority for the resolved Site; channels/terminals remain children. |
| Site/branch/business-unit reference posture | Central PMS site, branch, or business-unit values must use reference naming such as `central_pms_*_ref`; POS Server must not own Central PMS site resolution. |
| Registration metadata posture | MIN, PTU, serial, software, supplier, and accreditation metadata must remain flexible for Site POS Server and channel/terminal-level assignment where BIR/accreditation requires. |
| Effective-dated identity history posture | Effective-dated identity history is expected, but final uniqueness, current-active, and overlap rules remain downstream. |
| Identity configuration audit reference posture | Configuration audit references may be included as references only; audit physical objects are not in this slice. |
| MIN/PTU/serial/software/supplier dependency | Final assignment remains BIR/accounting/accreditation dependent. |
| Validation expectations | Validate effective dating posture, active fiscal identity readiness, Central PMS reference-only naming, and no transfer of Central PMS site authority. |
| Artifact readiness | Ready for design; SQL/object artifacts are partially blocked pending final identity fields, uniqueness/effective-dating rules, and BIR/accreditation confirmation. |

Open questions:

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Which MIN/PTU/serial/software/supplier attributes belong at Site POS Server level, channel/terminal level, or both? | Flexible model supports both levels. | Blocks final identity uniqueness and effective-dating rules. | Confirm with BIR/accounting/accreditation before production artifacts. |
| What are the minimum physical columns for fiscal identity objects? | Logical attributes exist; final columns are not decided. | Blocks object artifacts. | Define candidate fields in future artifact task and create artifacts only if approved. |
| Are any taxpayer/business identity attributes sensitive enough to require separation or masking? | Privacy posture requires data minimization. | May block identity field artifacts. | Confirm during Security/Privacy review. |

## 11. Channel / Terminal Registry Object Design

| Design item | Posture |
| --- | --- |
| Purpose | Prepare the child channel/terminal registry used by later fiscal issuance records without creating issuance records now. |
| Candidate future object groups | Channel/terminal registry, channel/terminal type posture, capabilities, presentation capability, Digital SI URL capability, QR presentation capability, health/status history, configuration/status history. |
| Provisional candidate object names | `channel_terminals`, `channel_terminal_capabilities`, `channel_terminal_status_history`, `channel_terminal_configuration_refs`. |
| WebPay logical channel posture | WebPay is a logical channel/terminal under the Site POS Server, not a separate fiscal authority. |
| APM / Cashier POS / EC Device / operator-assisted / future channel posture | These are channel/terminal types or endpoint classes under Site POS Server. |
| Channel/terminal type posture | Type values should be domain-specific controlled-code or enum candidates pending per-domain strategy. |
| Capability posture | Capabilities should cover print, display, Digital SI URL, QR presentation, and future approved presentation capabilities. |
| Digital SI URL capability posture | Registry may record whether a channel can present a Digital SI URL; Digital SI URL token/access objects are excluded from this slice. |
| QR presentation capability posture | Registry may record QR presentation capability; POS Server does not store required QR image binaries as fiscal authority. |
| ONLINE/OFFLINE observability-only posture | ONLINE/OFFLINE or health values are observability only and must not imply offline fiscal issuance approval. |
| Active/inactive/degraded/continuity state posture | Operational state posture is allowed as registry/status metadata; final status values remain Engineering/Operations dependent. |
| Configuration/status history posture | Status/configuration history may be planned for future artifacts; audit physical objects are excluded from this slice. |
| Validation expectations | Validate child-endpoint posture, capability naming, ONLINE/OFFLINE observability-only wording, and no independent terminal fiscal authority. |
| Artifact readiness | Ready for design; SQL/object artifacts are blocked pending final registry fields, status/capability values, and logical/physical identity details. |

Open questions:

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| What channel/terminal health statuses and capability values are operationally required for initial deployment? | ONLINE/OFFLINE is observability only; capability families are candidates. | Blocks final status/capability artifacts. | Confirm with Engineering/Operations. |
| How should logical WebPay channel identity be represented in physical objects? | WebPay is logical channel/terminal under Site POS Server. | May block WebPay identity fields. | Confirm label/identifier policy before artifacts. |
| Does accreditation require terminal-level fiscal identity evidence before fiscal document objects exist? | Identity model supports Site POS Server and channel/terminal references. | May block identity/registry artifact finalization. | Confirm with accreditation package workstream. |

## 12. Central PMS Reference Naming Object Design

| Design item | Posture |
| --- | --- |
| Purpose | Define authority-safe reference naming for Central PMS and vendor values needed by future fiscal object groups. |
| Central PMS reference-only posture | POS Server may store references to Central PMS authority records only. It must not own Central PMS lifecycle, payment finality, PaymentAttempt, PaymentConfirmation, site resolution, or ExitAuthorization. |
| Candidate reference naming patterns | `central_pms_parking_session_ref`, `central_pms_site_resolution_ref`, `central_pms_payment_attempt_ref`, `central_pms_payment_confirmation_ref`, `payment_finality_ref`, `vendor_ack_ref`, `reconciliation_ref`. |
| Reference storage posture | References may be embedded fields, shared reference records, or both; final storage pattern remains a physical object design decision. |
| Parking session reference posture | Parking session values are Central PMS references only. |
| Site resolution reference posture | Site resolution remains Central PMS authority; POS Server references the resolved context only. |
| PaymentAttempt reference posture | PaymentAttempt remains Central PMS authority; POS Server stores reference/context only. |
| PaymentConfirmation reference posture | PaymentConfirmation remains Central PMS authority; POS Server stores reference/context only. |
| Payment finality reference posture | Payment finality remains Central PMS authority; `payment_finality_ref` is reference/context only. |
| Reconciliation reference posture | Reconciliation references may connect later fiscal records to Central PMS or vendor context but do not transfer authority. |
| Vendor acknowledgement reference posture | `vendor_ack_ref` or source-specific vendor references are synchronization/context only, not vendor authority. |
| Authority-boundary safeguards | Prohibit POS-owned lifecycle names such as `payment_attempts`, `payment_confirmations`, `payment_finality_status`, `exit_authorizations`, `gate_authorizations`, and `vendor_authority`. |
| Validation expectations | Validate `_ref` naming, Central PMS reference-only fields, no payment finality ownership, no ExitAuthorization ownership, and no vendor authority ownership. |
| Artifact readiness | Ready as naming posture; SQL/object artifacts are blocked until reference storage approach and formats are approved. |

Open questions:

| Question | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- |
| Are Central PMS references embedded in first-slice boundary/registry records, represented in shared reference records, or both? | Reference-only naming is approved; storage pattern is not final. | Blocks reference object artifacts. | Decide during first-slice physical object design or artifact task. |
| Which vendor acknowledgement references are needed at first slice versus later fiscal document work? | `vendor_ack_ref` and source-specific references are approved naming patterns. | May block vendor reference object planning. | Confirm vendor synchronization needs. |

## 13. Validation Plan

Future validation for the first slice must cover:

| Validation category | Expectation |
| --- | --- |
| Naming compliance | Validate `pos`, lowercase `snake_case`, no quoted identifiers, `_id`, `_ref`, approved acronyms, and source prefixes. |
| Object existence | Validate only approved first-slice object artifacts when a future artifact task creates them. |
| Authority-boundary naming | Block names implying POS-owned payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, gate execution, vendor authority, independent terminal fiscal authority, or offline issuance approval. |
| Reference-only Central PMS fields | Confirm Central PMS values use `central_pms_*_ref` or approved reference-only names. |
| Channel/terminal child-endpoint posture | Confirm channels/terminals remain children of Site POS Server. |
| ONLINE/OFFLINE observability-only posture | Confirm health/status names do not imply offline fiscal issuance approval. |
| Controlled-code/status posture | Confirm status/type/reason/classification values are domain-specific and governed. |
| No payment finality ownership | Confirm payment finality appears only as reference/context if used. |
| No ExitAuthorization ownership | Confirm no POS-owned ExitAuthorization or gate authorization artifacts. |
| No SQL/object artifact until approved | Confirm this design task creates no SQL, DDL, Atlas, migrations, object files, data files, scripts, or CI workflows. |
| Drift-check readiness | Confirm future artifacts can be compared against repository state and drift is not promoted automatically. |

This document does not create validation scripts.

## 14. Open Questions

### Physical DB Design

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Which first-slice code families should become physical objects versus deferred reference data? | Foundation / configuration / controlled-code posture | Code families are planned; enum vs controlled-code remains per-domain decision. | Blocks controlled-code artifacts. | Decide per code family before first artifact task. |
| Should first-slice tables use flat `db/state/tables/` placement or domain subfolders? | All first-slice object files | Approved standards allow either flat or domain-subfoldered pattern. | Blocks table artifact file layout. | Choose layout before creating table object files. |
| Are Central PMS references embedded in first-slice boundary/registry records, represented in shared reference records, or both? | Central PMS reference naming posture; fiscal identity; channel registry | Reference-only naming is approved; storage pattern is not final. | Blocks reference object artifacts. | Decide during first-slice physical object design. |
| What are the minimum physical columns for fiscal identity and channel/terminal registry objects? | Fiscal identity / Site POS Server boundary; channel/terminal registry | Logical attributes exist; final columns are not decided. | Blocks object artifacts. | Define candidate fields in future first-slice artifact task, then create artifacts only if approved. |

### BIR / Accounting

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Which MIN/PTU/serial/software/supplier attributes belong at Site POS Server level, channel/terminal level, or both? | Fiscal identity / Site POS Server boundary; channel/terminal registry | Flexible model supports both levels. | Blocks final identity uniqueness and effective-dating rules. | Confirm with BIR/accounting/accreditation before production artifacts. |
| Which BIR/report classification code families are needed in the first slice? | Foundation / configuration / controlled-code posture | BIR/report code families are candidates only. | May block controlled-code artifacts. | Confirm which values are needed before reference/control-code artifacts. |

### Security / Privacy

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Should identity configuration audit references store only references or any local metadata? | Fiscal identity / Site POS Server boundary | Audit references are planned; audit physical objects are out of scope. | May block audit-reference fields. | Confirm with Security/Privacy and audit planning. |
| Are any taxpayer/business identity attributes sensitive enough to require separation or masking? | Fiscal identity / Site POS Server boundary | Privacy posture requires data minimization. | May block identity field artifacts. | Confirm during Security/Privacy review. |

### Engineering / Operations

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| What PostgreSQL version, extensions, hosting, collation/timezone, and deployment assumptions apply? | All first-slice artifacts | PostgreSQL is default engine; details pending. | Blocks SQL/object artifacts. | Confirm before any SQL/object artifact creation. |
| What channel/terminal health statuses and capability values are operationally required for initial deployment? | Channel/terminal registry; controlled codes | ONLINE/OFFLINE is observability only; capability families are candidates. | Blocks final status/capability artifacts. | Confirm with Engineering/Operations. |
| How should logical WebPay channel identity be represented in physical objects? | Channel/terminal registry | WebPay is logical channel/terminal under Site POS Server. | May block WebPay identity fields. | Confirm label/identifier policy before artifacts. |

### Vendor / Supplier

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Which supplier/accreditation metadata fields are required for first-slice identity objects? | Fiscal identity / Site POS Server boundary | Supplier/accreditation metadata is supported as references/metadata. | May block fiscal identity artifacts. | Confirm with supplier/accreditation package. |
| Which vendor acknowledgement references are needed at first slice versus later fiscal document work? | Central PMS reference naming posture | `vendor_ack_ref` and source-specific references are approved naming patterns. | May block vendor reference object planning. | Confirm vendor synchronization needs. |

### Accreditation

| Question | Affected first-slice area | Current posture | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Does the accreditation package require terminal-level fiscal identity evidence before fiscal document objects exist? | Fiscal identity / Site POS Server boundary; channel/terminal registry | Identity model supports Site POS Server and channel/terminal references. | May block identity artifact finalization. | Confirm with accreditation package workstream. |
| Are any accreditation sample/support structures needed in first slice? | Foundation; fiscal identity; channel/terminal registry | Sample/accreditation data files are out of scope. | Does not block first-slice design; blocks sample artifacts. | Keep sample/accreditation artifacts separate. |

## 15. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Premature SQL/object creation | Keep this document design-only and require a separate approved artifact task. |
| Controlled-code artifacts before enum/control-code decision | Keep code-family names provisional and block artifacts until per-domain strategy is approved. |
| Fiscal identity fields before BIR/accreditation confirmation | Preserve flexible Site POS Server and channel/terminal identity posture. |
| Channel/terminal registry implying independent fiscal authority | Document channels/terminals as child endpoints under Site POS Server. |
| ONLINE/OFFLINE implying offline fiscal issuance approval | Treat ONLINE/OFFLINE as observability only. |
| Central PMS references implying POS-owned lifecycle | Use `central_pms_*_ref` and reference-only documentation. |
| WebPay being treated as separate fiscal authority | Treat WebPay as a logical channel/terminal under Site POS Server. |
| Vendor acknowledgement being treated as vendor authority | Use `vendor_ack_ref` as synchronization/context only. |
| Local drift promotion | Require reviewed repository artifacts and future drift-check evidence. |

## 16. Non-Decisions

This draft does not decide or create:

- SQL DDL;
- Atlas files;
- migrations;
- physical object files;
- final table definitions;
- final column lists;
- final constraints/indexes;
- final enum/type implementation;
- seed/reference/sample data;
- validation/rebuild/drift scripts;
- CI workflows;
- source code;
- final BIR/accreditation package;
- offline fiscal issuance approval;
- separate database repository.

## 17. Appendices

### Appendix A: Candidate Name Disclaimer

All candidate future object names and reference names in this document are provisional examples only. They do not create final physical object names, SQL files, Atlas files, migration files, constraints, indexes, seed/reference/sample data, scripts, CI workflows, or database schema.

### Appendix B: First-Slice Authority Checklist

- No POS-owned payment finality object.
- No POS-owned PaymentAttempt lifecycle object.
- No POS-owned PaymentConfirmation lifecycle object.
- No POS-owned ExitAuthorization object.
- No POS-owned gate execution object.
- No independent terminal fiscal authority object.
- No offline fiscal issuance approval implied by ONLINE/OFFLINE state.
- Central PMS values are references only.
- Vendor acknowledgements are synchronization/context only.

### Appendix C: Future Artifact Gate Reminder

Before any SQL/object artifacts are created, a separate task must confirm PostgreSQL version/features/extensions/hosting, final object file layout, final object names, final column groups, enum vs controlled-code strategy per domain, identity uniqueness/effective-dating rules, channel/terminal field/capability rules, Central PMS reference storage pattern, validation approach, and drift-check approach.
