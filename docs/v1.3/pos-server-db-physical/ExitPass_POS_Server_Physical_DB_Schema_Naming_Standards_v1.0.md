# ExitPass POS Server Physical DB Schema and Naming Standards v1.0

## 1. Document Control

| Field | Value |
| --- | --- |
| Title | ExitPass POS Server Physical DB Schema and Naming Standards |
| Version | v1.0 |
| Repository | `ExitPass-PoSServer` |
| Product scope | POS Server future physical database artifact naming and schema/domain standards for ExitPass v1.3 |
| Status | Approved baseline |
| Output format | Markdown only |
| Physical artifact status | No SQL, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, or diagrams are created by this package. |

## 2. Approval / Baseline Status

This document is approved as the POS Server Physical DB Schema and Naming Standards v1.0 baseline. It governs future physical schema/domain naming, object naming, column naming, key/constraint/index naming, object-file naming, status/controlled-code naming, and authority-boundary naming safeguards for POS Server physical database artifact work.

Approval of this naming baseline does not authorize creating SQL files, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation scripts, rebuild scripts, drift scripts, CI workflows, source code, DOCX files, or diagrams.

Future SQL/object artifact creation remains gated by PostgreSQL version/features/extensions/hosting, final physical object design, idempotency uniqueness, fiscal numbering/counter strategy, retention/partitioning, Digital SI URL security, ARTS POSLog mapping, tamper-evident anchoring, and CI/rebuild/validation/drift tooling.
## 3. Purpose and Scope

This document resolves the physical schema/domain decomposition posture and naming standards needed before future POS Server database object artifacts are created.

In scope: schema/domain decomposition, schema names, table names, column names, object naming conventions, object file naming conventions, status/controlled-code naming, and authority-boundary naming safeguards.

Out of scope: SQL, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, final object definitions, and final PostgreSQL deployment details.

## 4. Approved Baseline References

| Source | Use |
| --- | --- |
| POS/Invoicing BRD v1.0 | Business and fiscal scope baseline. |
| POS Server System Design v1.0 | Architecture and authority baseline. |
| POS Server API Contract v1.0 | API boundary, status, idempotency, and trust boundary baseline. |
| POS Server Database Design v1.0 | Approved logical database design baseline. |
| POS Server Physical DB Artifact Plan v1.0 | Approved physical artifact planning baseline. |
| POS Server Physical DB Gate Resolution v1.0 | Approved gate classifications and placeholder policies. |
| Gate Readiness Matrix / Open Questions / Impact Map | Gate blocker and workstream context. |
| Physical DB Artifact Layout and Validation Plans | Folder, workflow, and validation inputs. |
| Repository Boundary and `db/README.md` | Repository, authority, and bootstrap boundaries. |

## 5. Schema / Domain Decomposition Recommendation

The physical naming plan uses the approved logical domains as naming and validation categories:

| Logical domain | Purpose | Naming impact |
| --- | --- | --- |
| Fiscal identity / boundary | Site POS Server, taxpayer, MIN/PTU/serial/supplier metadata. | Use fiscal identity terms. |
| Channel registry | Channel/terminal registration, capabilities, ONLINE/OFFLINE observability. | Use `channel_terminal` when both apply. |
| Fiscal documents | Sales Invoice and adjustment headers, status, Central PMS references. | Use `fiscal_document`, `sales_invoice`, or `si` carefully. |
| Fiscal details | Lines, totals, tender/tax/discount details. | Keep fiscal line and totals terms explicit. |
| Numbering/counters | SI sequences, adjustment sequences, reset counter, Z-counter, GTA. | Use `sequence`, `counter`, `gta`, and `fiscal_state` terms. |
| Operation safety / idempotency | Idempotency, retry, timeout, completion unknown. | Use `idempotency` and `retry` terms. |
| Digital delivery | Digital SI URL lifecycle and access audit. | Use `digital_si_url`. |
| Reports | X-read, Z-read, BIR Sales Summary, Annex E. | Preserve BIR report terminology. |
| Exports | EJ, POSLog, JSON/POSLog validation. | Preserve `ej`, `poslog`, and BIR naming. |
| Audit | Fiscal audit and privileged action evidence. | Audit names must not imply authority. |
| Recovery/continuity | Fiscal lock/block, hash chain, anchors, recovery. | Use `recovery`, `continuity`, and `fiscal_state`. |
| Security/privacy references | Actor, approval, evidence, access references. | Use reference naming without final RBAC assumptions. |
| Configuration/controlled codes | Status, reason, classification values. | Use controlled-code naming. |
| Optional events/outbox | Event publication support if approved later. | Use `outbox` only if approved. |
| Integration references | Central PMS and vendor context references. | Use `_ref` and explicit source prefixes. |

## 6. Recommended Schema Strategy

Recommended strategy: a hybrid approach with one primary PostgreSQL schema and domain-based object naming and folder organization.

| Option | Assessment | Recommendation |
| --- | --- | --- |
| One broad schema such as `pos` | Simple and avoids premature fragmentation. | Recommended initial posture. |
| Several domain schemas | Useful for strict security/retention boundaries but can over-fragment. | Defer unless justified. |
| Hybrid | Balances simplicity and domain clarity. | Adopt: primary `pos` schema plus domain-oriented object names/folders. |

Decision posture:

- `pos` is the recommended default primary schema name for future POS Server-owned objects.
- Domain separation should first use object names, folders, validation categories, and documentation traceability.
- Additional schemas require explicit approval based on security, retention, volume, extension, or operational needs.
- This package creates no schema SQL.

## 7. Naming Principles

| Principle | Standard |
| --- | --- |
| Case | Lowercase. |
| Separator | `snake_case`. |
| Quoting | No quoted identifiers. |
| Spaces | No spaces. |
| Clarity | Prefer clear domain terms over ambiguous abbreviations. |
| Approved acronyms | `si`, `ej`, `bir`, `vat`, `ptu`, `min`, `gta`, `qr`, `api`. |
| Authority boundary | Names must not imply POS Server owns Central PMS authority. |
| Stability | Names should be stable for state-based diffs and drift checks. |
| Searchability | Names should include useful domain context. |

## 8. Schema Naming Standards

| Item | Standard |
| --- | --- |
| Primary schema | `pos` is the recommended default schema name for future POS Server-owned physical objects. |
| Optional schemas | Additional schemas require approval and documented reason. |
| External authority schemas | Do not create schemas implying POS ownership of Central PMS, payments, ExitAuthorization, gate execution, or vendor PMS authority. |
| Naming style | Lowercase `snake_case`; no quoted identifiers. |
| Traceability | Schema documentation must map to approved logical database areas and gate decisions. |

## 9. Table Naming Standards

Future table names are not finalized by this package. When table artifacts are approved, use these standards:

| Area | Standard |
| --- | --- |
| Pluralization | Use plural table names unless project convention is explicitly changed. |
| Domain clarity | Include domain context when the table purpose is not obvious. |
| Junction/reference tables | Name by both sides of the relationship, ordered by ownership or lifecycle dependency. |
| History tables | Use `_history` for lifecycle or historical snapshots. |
| Audit tables | Use `_audit` only for audit evidence. Audit is not authority. |
| Status history | Use `_status_history` for separate auditable status tracking. |
| Controlled-code tables | Use `_codes` or `_code_sets` patterns for governed values. |
| External reference tables | Use source/authority terms, such as `central_pms_*_refs`. |
| Sample/accreditation tables | Do not create unless a later approved task requires physical sample support. |

Examples are illustrative only: `fiscal_documents`, `fiscal_document_lines`, `channel_terminals`, `central_pms_payment_refs`, `fiscal_document_status_history`, and `digital_si_urls`.

## 10. Column Naming Standards

| Column family | Standard |
| --- | --- |
| Internal identifiers | End with `_id`; prefer object-specific names where practical. |
| External references | End with `_ref` or use explicit prefixes such as `central_pms_*_ref`, `vendor_*_ref`, or `provider_*_ref`. |
| Timestamps | Use `_at`. |
| Dates | Use `_date`. |
| Booleans | Use `is_`, `has_`, or `requires_`. |
| Amounts | Include clear amount and currency/minor-unit meaning. |
| Status | Use domain-specific status names, not ambiguous generic `status`. |
| Reasons | Use domain-specific reason names such as `reprint_reason_code`. |
| Actors | Use `actor_ref`, `service_identity_ref`, or approved specific references. |
| Approvals | Use `approval_ref` or domain-specific approval references. |
| Audit references | Use `audit_ref` or domain-specific audit reference names. |
| Fiscal numbers | Keep display fiscal numbers distinct from internal IDs. |
| Central PMS references | Name as references only, not ownership fields. |

Authority-sensitive reference examples:

| Preferred | Avoid |
| --- | --- |
| `central_pms_payment_attempt_ref` | `payment_attempt_id` as POS-owned lifecycle. |
| `central_pms_payment_confirmation_ref` | `payment_confirmation_id` as POS-owned lifecycle. |
| `payment_finality_ref` | `payment_finality_status` unless clearly external context. |
| `central_pms_exit_authorization_ref` only if approved as reference | `exit_authorization_id` as POS-owned authority. |
| `vendor_ack_ref` | `vendor_authorization_id`. |

## 11. Key, Constraint, Index, Sequence, Function, Trigger, View, and Type Naming Standards

| Object family | Naming standard |
| --- | --- |
| Primary keys | `pk_<table>`. |
| Foreign keys | `fk_<table>__<referenced_table_or_source>`. |
| Unique constraints | `uq_<table>__<purpose_or_columns>`. |
| Check constraints | `ck_<table>__<rule>`. |
| Exclusion constraints | `ex_<table>__<rule>` if ever used. |
| Indexes | `ix_<table>__<purpose_or_columns>` for non-unique indexes. |
| Sequences | `seq_<domain>__<purpose>` or table-aligned sequence names where approved. |
| Functions/routines | `<verb>_<domain>_<purpose>` or `fn_<domain>__<purpose>` if a prefix convention is approved. |
| Triggers | `trg_<table>__<timing>_<event>__<purpose>`. |
| Views | `vw_<domain>__<purpose>` if view prefixes are approved; otherwise use domain-clear names. |
| Enum/type names | `<domain>_<value_family>_type` or approved controlled-code naming. |
| Policies | `pol_<table>__<purpose>` if PostgreSQL policies are approved. |

Names must state purpose, especially for idempotency, fiscal numbering, recovery, authority-boundary, and Digital SI URL safety rules.

## 12. Object File Naming Standards

Future object files under `db/state` must be deterministic and reviewable. This package creates no object files.

| Folder | Future file naming standard |
| --- | --- |
| `db/state/schemas/` | `<schema>.sql` when schema artifacts are approved. |
| `db/state/tables/` | `<schema>.<table>.sql` or `<domain>/<table>.sql`. |
| `db/state/views/` | `<schema>.<view>.sql` or `<domain>/<view>.sql`. |
| `db/state/functions/` | `<schema>.<function>.sql`; overload/version policy must be defined before use. |
| `db/state/triggers/` | `<schema>.<table>.<trigger>.sql` where practical. |
| `db/state/types/` | `<schema>.<type>.sql`. |
| `db/state/sequences/` | `<schema>.<sequence>.sql`. |
| `db/state/policies/` | `<schema>.<table>.<policy>.sql`. |
| `db/state/extensions/` | `<extension>.sql` or an approved manifest. |
| `db/reference-data/` | `<domain>.<code_set>.<format>` after format approval. |
| `db/validation/` | `validate_<category>.<extension>` after tooling approval. |
| `db/rebuild/` | `rebuild_<target>.<extension>` after tooling approval. |
| `db/drift/` | `drift_check_<target>.<extension>` after tooling approval. |

Ordering rules: extensions and schemas first, then types/sequences, tables, views/functions/triggers/policies, and controlled seed/reference data as approved. A future manifest may define deterministic ordering but must remain reviewable.

## 13. Status and Controlled-Code Naming Standards

| Value family | Naming standard |
| --- | --- |
| Lifecycle statuses | Domain-specific names such as `fiscal_document_status`, `reprint_status`, `export_validation_status`, or `digital_si_url_status`. |
| Operational statuses | Names such as `channel_terminal_health_status` or `pos_server_operational_status`; ONLINE/OFFLINE is observability only. |
| Reason codes | Domain-specific families such as `adjustment_reason_code` or `reprint_reason_code`. |
| BIR/report classifications | Preserve BIR-aligned terms and controlled-code governance. |
| Fiscal document types | Preserve Sales Invoice / SI and BIR fiscal terminology. |
| Adjustment types | Use explicit families such as void, refund, cancel, return, or BIR-confirmed equivalents. |
| Export validation statuses | Include pending, passed, failed posture unless later changed by approved contract. |
| Digital SI URL lifecycle | Use active, expired, revoked, blocked posture unless Security/Privacy Review changes it. |

Enum vs controlled-code storage remains downstream per domain. Stable lifecycle states may use PostgreSQL enums or controlled state tables. Evolving classifications, operational reasons, BIR/report classifications, and accreditation-related values should favor controlled-code governance unless proven stable enough for enum storage.

## 14. Authority-Boundary Naming Safeguards

Physical schema, object, column, file, and script names must not imply POS Server owns payment finality, PaymentAttempt lifecycle, PaymentConfirmation lifecycle, ExitAuthorization, gate execution, vendor PMS authority, or independent terminal fiscal authority.

| Context | Preferred naming pattern |
| --- | --- |
| Central PMS parking session | `central_pms_parking_session_ref`. |
| Central PMS PaymentAttempt | `central_pms_payment_attempt_ref`. |
| Central PMS PaymentConfirmation | `central_pms_payment_confirmation_ref`. |
| Central PMS payment finality context | `payment_finality_ref` or `central_pms_payment_finality_ref`. |
| Central PMS ExitAuthorization reference, if approved | `central_pms_exit_authorization_ref` with reference-only documentation. |
| Vendor PMS acknowledgement | `vendor_ack_ref` or `hikcentral_ack_ref`. |
| Channel/terminal registry | `channel_terminal_ref` or POS-owned channel/terminal identifiers. |
| Fiscal issuance | `fiscal_document`, `si`, `sales_invoice`, or BIR-aligned fiscal terms. |

| Avoid | Reason |
| --- | --- |
| `payment_finality` as a POS-owned schema/table family | Implies POS Server owns payment finality. |
| `payment_attempts` without Central PMS reference wording | Implies POS Server owns PaymentAttempt lifecycle. |
| `payment_confirmations` without Central PMS reference wording | Implies POS Server owns PaymentConfirmation lifecycle. |
| `exit_authorizations` as POS-owned records | Violates Central PMS ExitAuthorization authority. |
| `gate_authorizations` | Implies gate/exit authority in POS Server. |
| `terminal_fiscal_issuers` | Could imply terminal/channel independent fiscal authority. |
| `qr_invoices` | Confuses QR presentation with fiscal issuance and Digital SI URL. |

## 15. Examples and Anti-Examples

Examples are illustrative only and do not create final object names.

| Category | Preferred example | Anti-example |
| --- | --- | --- |
| Schema | `pos` | `payment` |
| Fiscal document | `fiscal_documents` | `receipts` if it conflicts with SI terminology |
| SI number | `si_number` | `invoice_id` for display fiscal number |
| Central PMS payment reference | `central_pms_payment_confirmation_ref` | `payment_confirmation_id` as POS-owned authority |
| Digital SI URL lifecycle | `digital_si_url_status` | `qr_status` as a fiscal lifecycle field |
| Channel/terminal status | `channel_terminal_health_status` | `offline_issuance_status` if it implies offline issuance approval |
| Audit evidence | `fiscal_action_audit` | `payment_finality_audit` as POS-owned finality evidence |
| Export validation | `export_validation_status` | `bir_approval_status` if it implies examiner approval by POS Server |
| Sequence | `seq_si_number` after approval | `seq_exit_authorization` |
| Vendor acknowledgement | `vendor_ack_ref` | `vendor_authority_id` |

## 16. Open Questions

| Owner / dependency | Question | Current posture | Blocks SQL/object artifacts? | Target resolution step |
| --- | --- | --- | --- | --- |
| Engineering / Operations | PostgreSQL major version, extensions, hosting, collation/timezone, deployment topology. | PostgreSQL recommended. | Yes | Confirm before first SQL/object artifact task. |
| Physical DB design | Whether to keep only `pos` or add additional schemas. | `pos` first; additional schemas only by approval. | Partially | Confirm during physical object design. |
| Physical DB design | Final table, column, constraint, index, and file names. | Standards approved here; names pending object design. | Yes | Decide in object-specific design. |
| BIR/accounting | Fiscal numbering/counter details and sequence-gap treatment. | Placeholder policy exists. | Yes for production issuance artifacts | Confirm or approve placeholder. |
| Security/Privacy | Digital SI URL token/access/expiry/authentication details. | Use `digital_si_url` lifecycle naming. | Yes for URL artifacts | Complete Security/Privacy Review. |
| Engineering Pack | Event/outbox physical persistence and payload naming. | Optional; events are evidence only. | No for non-event objects | Decide before outbox artifacts. |
| CI/CD | File formats and tooling for validation/rebuild/drift scripts. | Naming patterns only. | Yes for script artifacts | Confirm tooling before scripts. |
| Accreditation | ARTS POSLog profile/schema mapping and BIR sample package naming. | BIR terminology preserved. | Yes for accreditation artifacts | Confirm during package work. |
| Vendor/supplier | Supplier/accreditation metadata names and source references. | Use supplier/vendor refs. | May block supplier artifacts | Confirm with supplier/accreditation package. |

## 17. Out of Scope

This package does not create or approve final SQL DDL, final physical object files, final table/column lists, final constraints/indexes, final enum/type implementation, final Atlas or migration approach, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, final BIR/accreditation package, offline fiscal issuance approval, or source code.

## 18. Recommended Next Step

Proceed to approval-readiness review for this schema and naming standards package. After approval, use this package as the naming baseline for future physical object design while preserving the remaining gates for PostgreSQL details, object definitions, idempotency uniqueness, fiscal numbering, retention, Digital SI URL security, ARTS POSLog mapping, tamper-evident anchoring, and CI/rebuild/validation/drift tooling.
