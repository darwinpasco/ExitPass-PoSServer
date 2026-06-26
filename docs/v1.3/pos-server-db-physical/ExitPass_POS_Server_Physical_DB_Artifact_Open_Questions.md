# ExitPass POS Server Physical DB Artifact Open Questions

## Purpose

This document lists open questions that must be resolved or explicitly deferred before future POS Server physical database artifacts and state-based object files are created.

This is planning only. It does not create SQL, Atlas files, migrations, schema objects, validation scripts, or database schema.

## BIR / Accounting

| ID | Open question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- | --- |
| PDB-OQ-BIR-001 | Final MIN/PTU/serial/software/supplier assignment across Site POS Server and channels/terminals. | Flexible Site POS Server plus channel/terminal fiscal identity model. | Yes for final uniqueness/effective dating. | Yes for production identity objects. | May block accreditation-ready implementation. | Yes. |
| PDB-OQ-BIR-002 | Exact BIR-approved Sales Invoice numbering format. | Site POS Server-scoped configurable sequence policy. | Yes. | Yes unless placeholder policy approved. | Yes. | Yes. |
| PDB-OQ-BIR-003 | Exact adjustment document numbering format and document family set. | Separate configurable sequences by BIR-confirmed adjustment family. | Yes. | Yes unless placeholder policy approved. | Yes. | Yes. |
| PDB-OQ-BIR-004 | Official treatment of reserved numbers, failed issuance, abandoned issuance, and sequence gaps. | Conservative audit-first default; never reuse consumed numbers. | Yes. | Yes unless placeholder policy approved. | Yes. | Yes. |
| PDB-OQ-BIR-005 | Required X-read and Z-read aggregation scopes. | Site POS Server primary with optional terminal/channel and cashier/session dimensions. | May block report snapshot keys. | May block report/counter objects. | May block fiscal close. | Yes. |
| PDB-OQ-BIR-006 | Final VAT/tax treatment rules per fiscal line/charge type. | Logical model supports classifications and defers formulas. | No if flexible. | No for base objects; yes for formula/report objects. | Yes. | Yes. |
| PDB-OQ-BIR-007 | Final Diplomat VAT wording, evidence, reporting, and retention. | Active VAT privilege/exemption with evidence references by default. | May block evidence/retention details. | May block evidence/retention artifacts. | Yes. | Yes. |

## Security / Privacy

| ID | Open question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- | --- |
| PDB-OQ-SEC-001 | Final Digital SI URL token/access/expiry/authentication model. | Opaque tokenized URL, read-only view, minimum exposure, lifecycle states. | Yes for customer URL objects. | Yes for URL/token/access artifacts. | Yes. | May block customer SI evidence. |
| PDB-OQ-SEC-002 | Digital SI URL access audit retention and data minimization. | Store lifecycle and access audit where required. | Yes. | May block access audit objects. | Yes. | May block privacy evidence. |
| PDB-OQ-SEC-003 | Final RBAC/permission matrix for privileged fiscal actions. | Baseline roles identified; exact permissions downstream. | No if references stay flexible. | May block permission/reference objects. | Yes. | May block operational evidence. |
| PDB-OQ-SEC-004 | Final tamper-evident anchoring mechanism. | Append-only audit chain, hash chaining where practical, external checkpoints where practical. | Yes. | Yes for anchor/chain artifacts. | Yes. | May block recovery evidence. |

## Physical DB Design

| ID | Open question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- | --- |
| PDB-OQ-PHY-001 | PostgreSQL version, edition, extensions, and deployment assumptions. | PostgreSQL recommended as default target engine. | Yes. | Yes. | Yes. | No direct block. |
| PDB-OQ-PHY-002 | Physical schema/domain decomposition. | Candidate logical domains identified. | Yes. | Yes. | Yes. | No direct block. |
| PDB-OQ-PHY-003 | Physical object naming standards. | Naming categories planned but not finalized. | Yes. | Yes. | Yes. | No direct block. |
| PDB-OQ-PHY-004 | File/folder layout and manifest ordering. | Proposed `db/...` layout in layout plan. | Yes. | Yes. | Yes. | No direct block. |
| PDB-OQ-PHY-005 | Enum versus controlled-code table strategy. | Strategy pending. | Yes. | Yes. | Yes. | May affect report/export evidence. |
| PDB-OQ-PHY-006 | Retention/partitioning strategy. | Categories identified; exact periods open. | Yes. | Yes. | Yes. | Yes. |

## Engineering Pack

| ID | Open question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- | --- |
| PDB-OQ-ENG-001 | Rebuild script technology and command style. | Rebuild workflow planned only. | No. | Yes for scripts. | Yes for CI. | No direct block. |
| PDB-OQ-ENG-002 | Validation framework/script language. | Validation categories planned only. | No. | Yes for validation scripts. | Yes for CI. | May affect evidence. |
| PDB-OQ-ENG-003 | Atlas or other state-comparison tool usage. | Optional only after review. | No if manual validation acceptable. | May block drift tooling. | May block CI drift checks. | No direct block. |
| PDB-OQ-ENG-004 | Generated versus hand-authored object file boundary. | Hand-authored preferred unless generation approved. | Yes for workflow. | Yes. | Yes. | No direct block. |

## BIR / Accreditation Package

| ID | Open question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- | --- |
| PDB-OQ-ACC-001 | Final report/export layouts and file naming conventions. | Print/PDF/JSON/EJ/POSLog/BIR Summary/Annex E/audit exports supported. | May block output metadata. | May block sample/export objects. | Yes. | Yes. |
| PDB-OQ-ACC-002 | ARTS POSLog profile/schema mapping accepted by examiner. | ARTS POSLog 6.x default reference with BIR/local extensions. | May block export profile storage. | May block validation artifacts. | Yes. | Yes. |
| PDB-OQ-ACC-003 | Sample data/output package required for examiner submission. | Minimum sample package identified. | No if sample data separated. | May block sample/accreditation folders. | May block sample generation. | Yes. |

## Operations, CI/CD, Vendor/Supplier

| ID | Open question | Current posture | Blocks physical design | Blocks SQL/object artifacts | Blocks implementation | Blocks accreditation |
| --- | --- | --- | --- | --- | --- | --- |
| PDB-OQ-OPS-001 | Backup, restore, failover, and recovery topology assumptions. | Recovery logical model approved; topology open. | May block recovery/anchor artifacts. | May block operational scripts. | Yes. | May block recovery evidence. |
| PDB-OQ-CI-001 | CI runner and PostgreSQL service image/version. | PostgreSQL recommended; CI details open. | No. | Yes for CI workflow. | Yes. | No direct block. |
| PDB-OQ-CI-002 | PR evidence artifacts and drift failure rules. | Evidence checklist planned; drift not auto-promoted. | No. | Yes for automation. | Yes. | May support accreditation. |
| PDB-OQ-VEN-001 | Supplier/accreditation metadata required for POS Server and channel/terminal identities. | Flexible fiscal identity model supports metadata. | May block identity physical fields. | May block identity artifacts. | Yes. | Yes. |
| PDB-OQ-VEN-002 | Vendor-specific export/audit fields beyond BIR/ARTS mapping. | Vendor references are synchronization/context only. | No unless required. | May block export extensions. | May block integration. | May block evidence. |

## Questions That Do Not Reopen Approved Decisions

The following are not open unless approved baselines are formally revised: POS Server does not own payment finality; POS Server does not issue or mutate ExitAuthorization; POS Server stores Central PMS references only; channels/terminals are child endpoints; channels/terminals perform QR presentation from Digital SI URL; ONLINE/OFFLINE is observability only; offline fiscal issuance is disabled by default; ARTS POSLog does not replace BIR outputs; physical table/column/index/constraint/schema/enum/Atlas/migration details are downstream.