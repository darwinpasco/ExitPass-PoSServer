# ExitPass POS Server Database Artifacts

This `db/` folder is the future home of POS Server database artifacts for the `ExitPass-PoSServer` repository.

The POS Server database artifacts remain inside `ExitPass-PoSServer`. There is no separate `ExitPass-PoSServer-Db` repository at this stage. Repository-owned, state-based database artifacts are the future source of truth, and future database objects should be represented per object where practical.

Future database changes require clean rebuild, validation, drift-check, and pull request evidence. Local database drift must not be promoted unless it is expressed as reviewed repository artifacts and merged through the normal review process.

## Authority Boundary

Central PMS remains the authority for payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization. POS Server owns fiscal issuance and fiscal records only. POS Server database artifacts must store references to Central PMS authority records only and must not create POS-owned payment finality or ExitAuthorization authority.

Audit and events are evidence only. ONLINE/OFFLINE is observability only. Offline fiscal issuance is disabled by default unless BIR/accounting approves a compliant model. ARTS POSLog is a structured export reference only and does not replace Philippine BIR outputs.

## Bootstrap Boundary

This bootstrap creates only the empty `db/` folder skeleton, `db/README.md`, and approved `.gitkeep` placeholders. This bootstrap does not authorize and this PR must not include:

- SQL files
- DDL
- Atlas files
- migrations
- physical schemas
- physical database object files
- seed data
- reference data
- sample/accreditation data
- validation scripts
- rebuild scripts
- drift scripts
- CI workflows
- source code
- secrets
- external BIR/ARTS/vendor source files

## Folder Purposes

| Folder | Reserved future purpose |
| --- | --- |
| `db/state/` | Reserved for repository-owned desired database state after approval of the physical artifact task. |
| `db/state/schemas/` | Reserved for future schema/domain definitions. |
| `db/state/tables/` | Reserved for future per-table object files where practical. |
| `db/state/views/` | Reserved for future view definitions. |
| `db/state/functions/` | Reserved for future function or routine definitions. |
| `db/state/triggers/` | Reserved for future trigger definitions. |
| `db/state/types/` | Reserved for future enum, domain, composite type, or equivalent approved type definitions. |
| `db/state/sequences/` | Reserved for future approved sequence definitions. |
| `db/state/policies/` | Reserved for future security policies if approved. |
| `db/state/extensions/` | Reserved for future PostgreSQL extension declarations if approved. |
| `db/seeds/` | Reserved for future minimal environment-neutral seed data. |
| `db/reference-data/` | Reserved for future controlled code sets and approved reference values. |
| `db/validation/` | Reserved for future validation scripts and evidence generation. |
| `db/rebuild/` | Reserved for future clean rebuild scripts and manifests. |
| `db/drift/` | Reserved for future drift-check scripts or configuration. |
| `db/scripts/` | Reserved for future database utility scripts outside rebuild, validation, and drift checks. |
| `db/samples/` | Reserved for future synthetic or approved sample data/output support. |
| `db/accreditation/` | Reserved for future BIR/accreditation package support generated from approved sources. |

Each folder is reserved for future approved work only. The `.gitkeep` files are placeholders and are not database artifacts.

## Next Gate

Before any SQL or physical database object artifacts are added, the following must be confirmed or approved as placeholder policy:

- PostgreSQL version/features/extensions/hosting
- final schema names
- final object naming standards
- enum vs controlled-code strategy per domain
- exact idempotency constraints/indexes
- fiscal numbering/counter strategy
- retention/partitioning
- Digital SI URL security details
- BIR/accreditation package expectations
- ARTS POSLog profile/schema mapping
- tamper-evident anchoring
- CI/rebuild/validation/drift tooling
