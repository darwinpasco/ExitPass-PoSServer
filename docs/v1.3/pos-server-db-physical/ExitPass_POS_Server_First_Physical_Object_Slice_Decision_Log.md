# ExitPass POS Server First Physical Object Slice Decision Log

## 1. Purpose

This decision log records inherited decisions, first-slice planning decisions, placeholder-policy dependencies, deferred decisions, and non-decisions for the first future physical object slice artifact task.

## 2. Inherited Approved Decisions

| Decision | Source / basis | First-slice impact |
| --- | --- | --- |
| POS Server database artifacts remain inside `ExitPass-PoSServer`. | Repository boundary and approved artifact plan. | First-slice artifacts, when later authorized, belong under this repository. |
| `db/` is a folder boundary, not a separate repository. | Approved Physical DB Artifact Plan and `db/README.md`. | No separate DB repository is introduced. |
| Primary schema posture is `pos`. | Approved Schema/Naming Standards. | Future first-slice objects should use `pos` unless later approval adds schemas. |
| Naming uses lowercase `snake_case` and no quoted identifiers. | Approved Schema/Naming Standards. | Candidate names must follow approved naming posture. |
| Internal identifiers use `_id`; external authority references use `_ref` or explicit source prefixes. | Approved Schema/Naming Standards. | Central PMS and vendor references remain reference-only. |
| Central PMS owns payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. | Approved Database Design and Physical Object Design. | First-slice names must not imply POS Server lifecycle ownership of those records. |
| Channels/terminals are child endpoints under Site POS Server. | Approved Database Design and Physical Object Design. | Registry design must avoid independent terminal fiscal authority. |
| ONLINE/OFFLINE is observability only. | Approved Database Design and Gate Resolution. | Registry status planning must not imply offline fiscal issuance approval. |

## 3. First-Slice Decisions

| Decision | Rationale | Impact |
| --- | --- | --- |
| Limit first slice to foundation/configuration/controlled-code posture, fiscal identity/Site POS Server boundary, channel/terminal registry, and Central PMS reference naming posture. | This is the approved low-risk first slice and avoids fiscal issuance side effects. | Future first-slice design can prepare boundary objects before fiscal document objects. |
| Treat all candidate object names as provisional examples. | Prevents documentation from becoming physical artifacts prematurely. | Final SQL/object names remain downstream. |
| Keep WebPay as a logical channel/terminal under Site POS Server. | Approved default posture. | WebPay does not become a separate fiscal authority. |
| Use Central PMS reference naming patterns only. | Preserves Central PMS authority. | Patterns include `central_pms_*_ref`, `payment_finality_ref`, and source-specific vendor references where approved. |
| Exclude fiscal documents, numbering, counters, Digital SI URL security objects, reports, exports, audit, recovery, and outbox from the first slice. | Those areas have additional gates and higher fiscal/compliance risk. | They remain future tasks. |

## 4. Placeholder-Policy Dependencies

| Dependency | Current posture | First-slice effect |
| --- | --- | --- |
| Enum vs controlled-code strategy | Strategy approved; per-domain storage decision pending. | Foundation/configuration/code objects may be planned but final implementation remains pending. |
| MIN/PTU/serial/software/supplier assignment | Flexible model approved; final assignment pending BIR/accreditation confirmation. | Fiscal identity object planning must preserve site-level and channel/terminal-level references. |
| Channel/terminal physical vs logical identity | Logical/non-physical channels are supported; final field set pending. | Registry object planning must support both physical and logical endpoints. |
| PostgreSQL version/features/extensions/hosting | PostgreSQL default; details pending. | SQL/object artifacts remain blocked. |

## 5. Decisions Deferred

| Deferred decision | Owner / dependency | Reason |
| --- | --- | --- |
| Final table names, column names, constraints, indexes, sequences, and types. | Physical DB design. | This package is planning-only. |
| Final enum vs controlled-code implementation per domain. | Physical DB design / BIR-accounting / Engineering. | Storage strategy must be confirmed before artifacts. |
| Final fiscal identity required fields and uniqueness/effective-dating rules. | BIR/accreditation / Physical DB design. | Depends on registration/accreditation requirements. |
| Final channel/terminal capability and status fields. | Engineering / Operations / Physical DB design. | Depends on runtime and operations requirements. |
| Final validation/rebuild/drift tooling. | Engineering / CI/CD. | Scripts/workflows are out of scope. |

## 6. Non-Decisions

This package does not decide or create SQL DDL, Atlas files, migrations, physical object files, final table/column lists, final constraints/indexes, final enum/type implementation, seed/reference/sample data, validation/rebuild/drift scripts, CI workflows, source code, final BIR/accreditation package, offline fiscal issuance approval, or a separate database repository.
