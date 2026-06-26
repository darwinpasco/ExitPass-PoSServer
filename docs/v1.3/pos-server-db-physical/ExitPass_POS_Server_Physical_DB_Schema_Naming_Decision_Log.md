# ExitPass POS Server Physical DB Schema Naming Decision Log

## 1. Approved Inherited Decisions

| Decision | Source | Impact |
| --- | --- | --- |
| POS Server database artifacts remain inside `ExitPass-PoSServer`. | Repository boundary and Physical DB Artifact Plan. | Naming applies inside this repository. |
| No separate `ExitPass-PoSServer-Db` repository exists at this stage. | Physical DB Artifact Plan and Gate Resolution. | Use `db/` as folder boundary. |
| Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. | Approved BRD, System Design, API Contract, Database Design. | Names must express references, not ownership. |
| POS Server owns fiscal issuance and fiscal records only. | Approved baselines. | Names center on fiscal records, reports, exports, audit, recovery, and references. |
| ONLINE/OFFLINE is observability only. | Approved baselines. | Names must not imply offline fiscal issuance approval. |
| ARTS POSLog is a structured export reference only. | Approved API Contract and Database Design. | Preserve BIR fiscal terminology. |
| State-based DB artifacts are future repository source of truth. | Physical DB Artifact Plan. | Object file names must support review and drift checks. |

## 2. Naming Decisions Resolved Now

| Decision | Resolution | Notes |
| --- | --- | --- |
| General style | Lowercase `snake_case`; no spaces; no quoted identifiers. | Applies to future physical artifacts. |
| Approved acronyms | `si`, `ej`, `bir`, `vat`, `ptu`, `min`, `gta`, `qr`, `api`. | Use only where clear and aligned with approved terms. |
| Internal identifiers | End with `_id`. | Prefer object-specific IDs where practical. |
| External references | End with `_ref` or use explicit source prefixes. | Example: `central_pms_*_ref`. |
| Timestamps/dates | Use `_at` for timestamps and `_date` for dates. | Applies to business dates and event times. |
| Booleans | Use `is_`, `has_`, or `requires_`. | Avoid ambiguous true/false names. |
| Status fields | Use domain-specific status names. | Avoid generic `status` where ambiguous. |
| Fiscal numbers | Keep display fiscal numbers separate from internal IDs. | SI number is not fiscal document ID. |
| Constraint/index names | Use deterministic purpose-bearing names. | Supports validation evidence. |
| File names | Use object-specific deterministic names under `db/state` when approved. | No object files created here. |

## 3. Schema / Domain Recommendations

| Topic | Recommendation | Status |
| --- | --- | --- |
| Schema strategy | Hybrid approach with default primary schema `pos` and domain-oriented names/folders. | Recommended for approval. |
| Additional schemas | Defer unless justified by security, retention, volume, extension, or operational boundaries. | Deferred. |
| Domain decomposition | Use approved logical domains as naming and validation categories. | Recommended. |
| Events/outbox | Optional only if later approved by Engineering Pack/event contract work. | Deferred. |

## 4. Decisions Pending Physical Object Design

| Pending decision | Why pending | Target step |
| --- | --- | --- |
| Final table names | Requires object-specific physical design. | Physical object design package. |
| Final column names | Requires confirmed object model. | Physical object design package. |
| Final key, constraint, and index names | Requires final object definitions and uniqueness rules. | Physical object design package. |
| Final enum/type names | Requires enum vs controlled-code decision per domain. | Physical DB design and Engineering review. |
| Final schema split beyond `pos` | Requires security/retention/volume justification. | Gate update if needed. |
| Final script extensions and tooling names | Requires validation/rebuild/drift tooling decisions. | Engineering/CI task. |

## 5. Decisions Explicitly Deferred

| Deferred decision | Owner / dependency |
| --- | --- |
| PostgreSQL major version/features/extensions/hosting. | Engineering / Operations. |
| Fiscal numbering/counter final implementation names. | BIR/accounting and physical DB design. |
| Digital SI URL security names tied to token/auth model. | Security/Privacy. |
| ARTS POSLog final profile and schema mapping names. | Engineering Pack and BIR/accreditation. |
| Tamper-evident anchoring object names. | Security / Engineering / Operations. |
| CI workflow and PR evidence script names. | CI/CD. |

## 6. Non-Decisions

This decision log does not decide or create SQL files, DDL, Atlas files, migrations, physical database object files, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, final object definitions, final RBAC matrix, offline fiscal issuance approval, or a separate DB repository.
