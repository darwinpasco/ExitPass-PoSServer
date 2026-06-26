# ExitPass POS Server DB Object Inventory and Roadmap Check

## 1. Review Summary

This inventory and roadmap check reviews the POS Server database object set after completion of the first eight SQL/object slices.

The current database object inventory covers the approved physical object posture from foundation through audit, recovery, anchoring references, and security/privacy references. The implemented SQL object set remains consistent with the approved physical object design: objects are structural, state, posture, reference, or evidence records only. The current state does not create SQL functions, triggers, PostgreSQL sequences, Atlas files, migrations, seed/reference/sample data, validation scripts, CI workflows, application source code, or generated output binaries.

The object set is now broad enough that the next workstream should shift from adding more tables to proving the state can be rebuilt, validated, inventoried, and checked for drift from repository source. A SQL application manifest and validation/rebuild/drift scripts should come before database functions, business/reporting views, reference data, or CI automation.

## 2. Current SQL Inventory

### Schema

| Area | SQL files |
| --- | --- |
| POS Server schema | `db/state/schemas/pos.sql` |

### Controlled-Code Foundation

| Area | SQL files |
| --- | --- |
| Controlled-code sets and values | `db/state/tables/pos.controlled_code_sets.sql`, `db/state/tables/pos.controlled_codes.sql` |

### Fiscal Identity / Site POS Server Boundary

| Area | SQL files |
| --- | --- |
| Site POS Server and fiscal identity posture | `db/state/tables/pos.site_pos_servers.sql`, `db/state/tables/pos.fiscal_identities.sql`, `db/state/tables/pos.site_pos_server_fiscal_identity_history.sql` |

### Channel / Terminal Registry

| Area | SQL files |
| --- | --- |
| Channel and terminal child endpoint posture | `db/state/tables/pos.channel_terminals.sql`, `db/state/tables/pos.channel_terminal_capabilities.sql`, `db/state/tables/pos.channel_terminal_status_history.sql` |

### Fiscal Document Core

| Area | SQL files |
| --- | --- |
| Fiscal document header, status, and document links | `db/state/tables/pos.fiscal_documents.sql`, `db/state/tables/pos.fiscal_document_status_history.sql`, `db/state/tables/pos.fiscal_document_links.sql` |

### Fiscal Lines / Tenders / Tax / Discount / Totals

| Area | SQL files |
| --- | --- |
| Fiscal detail posture | `db/state/tables/pos.fiscal_document_lines.sql`, `db/state/tables/pos.fiscal_tenders.sql`, `db/state/tables/pos.fiscal_tax_details.sql`, `db/state/tables/pos.fiscal_discount_privilege_details.sql`, `db/state/tables/pos.fiscal_totals.sql` |

### Numbering / Counters / Idempotency / Retry / Exception

| Area | SQL files |
| --- | --- |
| Fiscal sequence and counter state posture | `db/state/tables/pos.fiscal_sequence_policies.sql`, `db/state/tables/pos.fiscal_sequence_states.sql`, `db/state/tables/pos.fiscal_sequence_gap_audit.sql`, `db/state/tables/pos.fiscal_counter_states.sql` |
| Fiscal state, locks, idempotency, retry, and exception posture | `db/state/tables/pos.fiscal_state_snapshots.sql`, `db/state/tables/pos.fiscal_lock_states.sql`, `db/state/tables/pos.idempotency_records.sql`, `db/state/tables/pos.fiscal_operation_retries.sql`, `db/state/tables/pos.fiscal_operation_exceptions.sql` |

### Digital SI URL

| Area | SQL files |
| --- | --- |
| Digital SI URL and access posture | `db/state/tables/pos.digital_si_urls.sql`, `db/state/tables/pos.digital_si_url_access_events.sql` |

### Reprints

| Area | SQL files |
| --- | --- |
| Reprint request and output reference posture | `db/state/tables/pos.reprint_requests.sql`, `db/state/tables/pos.reprint_output_refs.sql` |

### Fiscal Adjustments

| Area | SQL files |
| --- | --- |
| Fiscal adjustment and status posture | `db/state/tables/pos.fiscal_adjustments.sql`, `db/state/tables/pos.fiscal_adjustment_status_history.sql` |

### Reports

| Area | SQL files |
| --- | --- |
| Fiscal report request, scope, X/Z, BIR summary, Annex E, and output references | `db/state/tables/pos.fiscal_report_requests.sql`, `db/state/tables/pos.fiscal_report_scopes.sql`, `db/state/tables/pos.x_z_reports.sql`, `db/state/tables/pos.bir_sales_summary_reports.sql`, `db/state/tables/pos.annex_e_reports.sql`, `db/state/tables/pos.fiscal_report_output_refs.sql` |

### EJ / POSLog / JSON / Exports

| Area | SQL files |
| --- | --- |
| Electronic journal and export package posture | `db/state/tables/pos.electronic_journal_records.sql`, `db/state/tables/pos.fiscal_export_requests.sql`, `db/state/tables/pos.fiscal_export_packages.sql`, `db/state/tables/pos.fiscal_export_package_items.sql`, `db/state/tables/pos.export_validation_results.sql`, `db/state/tables/pos.export_schema_profile_refs.sql` |

### Audit

| Area | SQL files |
| --- | --- |
| Fiscal, privileged, configuration, and access audit posture | `db/state/tables/pos.fiscal_action_audit.sql`, `db/state/tables/pos.privileged_action_audit.sql`, `db/state/tables/pos.configuration_audit.sql`, `db/state/tables/pos.access_audit.sql` |

### Recovery / Continuity / Anchoring

| Area | SQL files |
| --- | --- |
| Recovery, continuity, and anchor reference posture | `db/state/tables/pos.recovery_requests.sql`, `db/state/tables/pos.continuity_check_results.sql`, `db/state/tables/pos.fiscal_anchor_refs.sql` |
| Supporting fiscal state and lock posture | `db/state/tables/pos.fiscal_state_snapshots.sql`, `db/state/tables/pos.fiscal_lock_states.sql` |

### Security / Privacy References

| Area | SQL files |
| --- | --- |
| Security and privacy reference posture | `db/state/tables/pos.security_reference_contexts.sql` |

## 3. Object Area Coverage Matrix

| Object area | Classification | Coverage conclusion |
| --- | --- | --- |
| POS schema | Covered | `pos` schema is present as the POS Server-owned fiscal schema. |
| Controlled-code foundation | Covered; values deferred | Structural controlled-code tables are present. Actual controlled-code seed/reference values remain a future separate task. |
| Fiscal identity / Site POS Server boundary | Covered | Site POS Server, fiscal identity, and effective-dated identity assignment posture are represented. |
| Channel / terminal registry | Covered | Channel/terminal registry, capabilities, and status history are represented as child endpoints under Site POS Server. |
| Fiscal document core | Covered | Header/core, status history, and generic document linkage posture are represented without SI numbering or issuance behavior. |
| Fiscal details | Covered | Lines, tenders, tax details, discount/privilege details, and totals are represented without final BIR/accounting formulas. |
| Numbering / counters | Covered as state/posture | Sequence policy/state, gap audit, and counter state are represented without PostgreSQL sequences, allocation functions, or counter increment behavior. |
| Idempotency / retry / exceptions | Covered as posture | Idempotency, retry, and exception records protect fiscal side-effect posture without payment or exit authority. |
| Digital SI URL | Covered as lifecycle/access posture | Digital SI URL and access event posture are represented without raw token storage, QR binaries, or mutation behavior. |
| Reprints | Covered | Reprint request and output reference posture are represented without mutating original fiscal facts or allocating new fiscal numbers. |
| Fiscal adjustments | Covered | Adjustment relationship and adjustment status posture are represented without refund/reversal finality or adjustment numbering sequences. |
| Reports | Covered as report posture | Report request/scope, X/Z, BIR Sales Summary, Annex E, and output references are represented without calculation logic or output binaries. |
| EJ / POSLog / JSON / exports | Covered as export posture | EJ records, export requests/packages/items, validation results, and schema/profile references are represented without generated payloads or final ARTS/BIR mapping. |
| Audit | Covered as evidence posture | Fiscal, privileged, configuration, and access audit posture are represented without event publishing or authority workflow. |
| Recovery / continuity / anchoring | Covered as request/check/reference posture | Recovery requests, continuity check results, anchor references, snapshots, and locks are represented without automated recovery or hash anchoring implementation. |
| Security / privacy references | Covered as reference posture | Security reference contexts provide actor/service/approval/evidence grouping without IAM/RBAC or raw sensitive evidence storage. |
| Optional events / outbox | Should not be covered yet | Event/outbox persistence remains intentionally excluded until explicitly approved. |
| Views | Not yet covered | Views should be deferred until validation/rebuild posture and read-only view scope are approved. |
| Functions | Not yet covered | Functions should be deferred unless needed for validation support or later explicitly approved integrity/report/anchor helpers. |
| Seed/reference data | Intentionally deferred | Controlled-code reference data should be a separate task and must not be mixed with schema object changes. |
| Validation/rebuild/drift scripts | Intentionally deferred | These are now the recommended next workstream. |
| CI workflow | Intentionally deferred | CI should follow stable local validation/rebuild/drift scripts. |

## 4. Duplicate / Overlap Review

| Area | Finding | Disposition |
| --- | --- | --- |
| Status history vs audit tables | Status history tables record local lifecycle transitions, while audit tables provide cross-cutting evidence posture. | Acceptable overlap. Future documentation should continue to distinguish lifecycle state transitions from evidence capture. |
| Fiscal document links vs fiscal adjustments | `fiscal_document_links` supports generic document relationships, while `fiscal_adjustments` models adjustment-specific posture. | Acceptable overlap. Future adjustment behavior should clarify when both are populated. |
| Report outputs vs export packages | `fiscal_report_output_refs` references report outputs, while export package tables represent export package metadata and validation posture. | Acceptable overlap. Generated binaries and export payloads remain excluded. |
| Digital SI URL access events vs access audit | Digital SI URL access events are URL-specific access posture; `access_audit` is generalized fiscal resource access evidence. | Acceptable overlap. Future usage guidance should prevent duplicate evidence semantics from becoming contradictory. |
| Fiscal state snapshots vs continuity check results | Snapshots capture state posture; continuity check rows record evaluation/check evidence against state. | Acceptable overlap. No cleanup needed. |
| Fiscal anchor refs vs EJ hash refs | EJ hash fields are references on journal records; anchor refs represent checkpoint/external anchor posture. | Acceptable overlap. Hash generation and external anchoring remain deferred. |
| Security reference contexts vs actor/approval refs in other tables | Security reference contexts group security/privacy references; local `_ref` fields keep each record self-describing. | Acceptable overlap. No local IAM/RBAC ownership is created. |
| Fiscal operation exceptions vs audit/recovery records | Operation exceptions describe fiscal operation exception posture; audit records evidence actions; recovery records supervise recovery posture. | Acceptable overlap. Future workflows should document handoff among exception, audit, and recovery records. |

No duplicate object family appears to require cleanup before validation/rebuild work. The main need is documentation and validation clarity so overlapping posture tables are used consistently.

## 5. Naming and Authority Boundary Review

The current SQL inventory preserves the approved naming baseline:

- primary schema is `pos`
- object names use lowercase `snake_case`
- internal identifiers use `_id`
- external references use `_ref`
- Central PMS references use `central_pms_*_ref` where applicable
- domain-specific status and type fields are represented through controlled-code references

The authority boundary remains clean:

- Central PMS payment references are stored as references/context only.
- POS Server does not own `PaymentAttempt` lifecycle.
- POS Server does not own `PaymentConfirmation` lifecycle.
- POS Server does not own payment finality.
- POS Server does not own, issue, or mutate `ExitAuthorization`.
- No gate execution authority is created.
- Channels and terminals remain child endpoints under Site POS Server.
- ONLINE/OFFLINE posture remains observability-only and does not authorize offline fiscal issuance.
- ARTS POSLog remains an export reference/profile posture and does not replace BIR outputs.

The inventory does not show unauthorized payment, gate, exit, offline issuance, or independent terminal fiscal authority objects.

## 6. Dependency Order Review

The table dependency model is logical but has outgrown simple alphabetical application. A future rebuild workflow should use an explicit SQL application manifest or topological order.

Examples that make a manifest useful:

- `pos.channel_terminals` depends on Site POS Server and fiscal identity objects, so it must be applied after the identity boundary tables.
- `pos.annex_e_reports` depends on `pos.bir_sales_summary_reports`, so report table order must be explicit.
- `pos.continuity_check_results` depends on `pos.recovery_requests`, so recovery/continuity order must be explicit.
- Export packages, package items, validation results, and schema/profile references have cross-references that should be deterministic in rebuild order.

The future application order should remain repository-defined and should not rely on local database state. No manifest is created in this task.

## 7. Gaps and Deferred Items

The following items remain intentionally deferred:

- SQL application manifest or ordered rebuild inventory
- validation/rebuild/drift scripts
- CI validation workflow
- controlled-code seed/reference data
- database views
- database functions
- additional secondary indexes and performance tuning
- final BIR/accounting formulas for reports, taxes, discounts, privileges, counters, and summaries
- final BIR/accreditation package layout and output samples
- final ARTS POSLog profile mapping
- Digital SI URL token/auth/expiry implementation details
- tamper-evident hash generation and external anchoring implementation
- production recovery automation
- optional outbox/event persistence
- application/service implementation

These are not gaps in the current SQL object slices. They are future workstreams that require separate approval and validation.

## 8. Functions Recommendation

Database functions should be deferred.

The current object set intentionally avoids functions, triggers, allocation behavior, counter increment behavior, export generation, report calculation, recovery automation, and cryptographic anchoring. That posture should continue until validation/rebuild/drift capability is in place.

Functions may become useful later for:

- database integrity helpers
- validation helper queries
- report support helpers after formulas are approved
- tamper-evidence/hash helper posture after Security/Privacy and accreditation decisions

Any future function task must explicitly prohibit ownership of payment finality, `PaymentAttempt`, `PaymentConfirmation`, `ExitAuthorization`, external API calls, gate execution, and orchestration.

## 9. Views Recommendation

Views should be deferred until after validation/rebuild/drift scripts and the SQL application manifest are in place.

Simple read-only inventory or validation views may be useful later for:

- object inventory checks
- authority-boundary inspection
- code/reference status inspection
- report/export evidence inspection

Business/report views should not be created yet because final BIR report formulas, Annex layouts, ARTS POSLog mapping, and accreditation package rules remain unresolved.

## 10. Seed / Reference Data Recommendation

Controlled-code seed/reference data should be added soon, but only as a separate task after the rebuild and validation approach is defined.

Recommended posture:

- keep seed/reference data separate from schema object changes
- define controlled-code governance and ownership before adding values
- include only approved baseline code families and values
- provide repeatable load and validation evidence
- avoid sample transaction, fiscal document, payment, report, export, audit, or recovery data

Seed/reference data is important for realistic smoke checks and service integration, but it should not be mixed into the object inventory or validation planning task.

## 11. Validation / Rebuild / Drift Recommendation

Validation/rebuild/drift scripts should be the next primary engineering workstream.

The object set now covers the approved slices 1 through 8. Before adding more object families, views, functions, seed data, or CI workflows, the repository needs executable evidence that:

- SQL files apply cleanly to a disposable PostgreSQL database
- repository state is the source of truth
- SQL application order is deterministic
- expected schemas, tables, constraints, and references exist
- naming rules are enforced
- prohibited object families are absent
- authority-boundary naming remains clean
- local database drift is reported and not promoted
- PRs can attach consistent rebuild and drift evidence

The future scripts should run against disposable local or CI databases only. They must not promote local drift into repository baseline.

## 12. Recommended Next Roadmap

1. Create a SQL application manifest and validation/rebuild/drift script package.
2. Run and document clean disposable PostgreSQL rebuild evidence for all current SQL objects.
3. Add controlled-code seed/reference data in a separate approved task.
4. Add CI workflow only after local validation scripts are stable.
5. Consider simple read-only validation or inventory views after scripts and reference data exist.
6. Defer database functions until a concrete, approved integrity, validation, report-support, or tamper-evidence need exists.
7. Continue application/service implementation against the approved object set only after rebuild and reference-data posture is stable.

No additional table slice is recommended before validation/rebuild/drift work.

## 13. Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Rebuild order remains implicit | Clean rebuilds may fail even if individual SQL files are valid. | Create a manifest or script-owned topological order before CI. |
| Validation remains manual | Drift, prohibited objects, or naming defects may be missed. | Implement validation/rebuild/drift scripts next. |
| Controlled-code values are delayed too long | Service integration and smoke testing remain limited. | Add seed/reference data as a separate task after validation posture is ready. |
| Premature functions encode unresolved behavior | Functions could accidentally own issuance, counters, payment, exit, report, recovery, or anchoring behavior. | Defer functions and require explicit boundary review. |
| Premature views encode final formulas | Views could hard-code unresolved BIR/accounting or report assumptions. | Limit any future views to read-only inventory/validation until formulas are approved. |
| Overlapping evidence tables are misused | Status, audit, access, exception, recovery, and continuity rows may become semantically inconsistent. | Add usage guidance and validation queries in future documentation/scripts. |
| Local drift is promoted | Local database state could become the unreviewed baseline. | Keep repository SQL as source of truth and report drift only. |
| Authority leakage through application use | Reference fields could be misinterpreted as POS-owned payment or exit authority. | Preserve naming validation and authority-boundary review in every future DB and service task. |

## 14. Final Recommendation

The POS Server database object inventory is complete for the approved structural/posture coverage represented by slices 1 through 8. The current SQL object set matches the approved Physical Object Design at the intended level of detail and preserves the approved naming, fiscal, security, recovery, export, and authority boundaries.

The recommended next step is not another SQL object slice. The next work should be a validation/rebuild/drift implementation package, including a deterministic SQL application manifest, disposable PostgreSQL rebuild workflow, object inventory checks, naming and authority-boundary checks, prohibited-object checks, and PR evidence capture.
