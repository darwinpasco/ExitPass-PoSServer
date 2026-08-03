# ExitPass POS Server Electronic Journal, POSLog, and Export Review v1.0

## 1. Verdicts

| Area | Verdict | Reason |
| --- | --- | --- |
| Electronic Journal schema | PARTIALLY_READY | Metadata and optional sequence/hash references exist. |
| Electronic Journal runtime | NOT_IMPLEMENTED | No event writer, chronology, immutable chain, query, or export exists. |
| POSLog | NOT_IMPLEMENTED | No approved profile mapping or payload generator exists. |
| Export schema | PARTIALLY_READY | Request/package/item/profile/validation metadata exists. |
| Export runtime/API | NOT_IMPLEMENTED | No generator, adapter, manifest/checksum execution, retry worker, or route exists. |

## 2. Electronic Journal Matrix

| Required behavior | Current evidence | Status |
| --- | --- | --- |
| Immutable entries | `pos.electronic_journal_records` is mutable | NOT_IMPLEMENTED |
| Fiscal event chronology | Optional event timestamp/sequence posture | PARTIALLY_READY |
| Request/response evidence | No safe governed event payload | NOT_IMPLEMENTED |
| Fiscal issuance/sequence events | No writer | NOT_IMPLEMENTED |
| Print/reprint events | Reprint schema posture only; no printer runtime | NOT_IMPLEMENTED |
| Void/refund/adjustment events | Void runtime exists, no EJ writer; other runtime absent | NOT_IMPLEMENTED |
| X/Z events | No report runtime | NOT_IMPLEMENTED |
| Hash chain/anchor | Nullable hash references; no uniqueness or validation | NOT_IMPLEMENTED |
| Retention/archive | No executed policy/tooling | NOT_IMPLEMENTED |
| Read/export/audit API | None | NOT_IMPLEMENTED |
| Redaction/privacy | No EJ writer; approved exclusions not operationalized | UNKNOWN |

EJ must capture governed event facts, not request/response bodies, credentials, personal evidence, or stack traces. Its sequence and integrity mechanism must be enforced rather than inferred from optional columns.

## 3. POSLog and Export Matrix

| Capability | Current evidence | Status |
| --- | --- | --- |
| POSLog v6.0 mapping | Mentioned as an open design dependency | NOT_IMPLEMENTED |
| Versioned export schema profile | Metadata table and unique key/version | PARTIALLY_READY |
| Export request lifecycle | Metadata/status posture | PARTIALLY_READY |
| Package/items | Reference metadata posture | PARTIALLY_READY |
| File generation | None | NOT_IMPLEMENTED |
| Filename/content type/encoding | Not governed | NOT_IMPLEMENTED |
| Manifest/checksum | No executable generation or verification | NOT_IMPLEMENTED |
| Validation execution/results | Results table only | PARTIALLY_READY |
| Retry/recovery | No worker/idempotency/commit protocol | NOT_IMPLEMENTED |
| Controlled download API | None | NOT_IMPLEMENTED |
| Audit evidence | Generic schema only; no writer | NOT_IMPLEMENTED |
| Retention/archive/deletion | Not implemented | NOT_IMPLEMENTED |

## 4. Required Event Coverage

The future EJ design should cover committed fiscal issuance, fiscal-number assignment, status transition, void/adjustment, X generation, Z close, report/export generation, output verification, and print/reprint events. Failed and unknown-outcome operations require safe correlation and support references without leaking payloads or hashes.

## 5. Integrity and Privacy Requirements

- Govern record type/status and export type/package/item/profile/validation classifications by code family.
- Make journal chronology and finalized export packages immutable.
- Bind every export to a committed source report/version and scoped Site POS Server.
- Generate a manifest and cryptographic checksum over exact output bytes; verify on read/download.
- Never store or export credentials, authorization headers, connection strings, raw statutory identity, evidence images, signed evidence URLs, or generic request bodies.
- Define retention and archival before production; distinguish fiscal retention from transient job diagnostics.
- Prove deterministic retry, lost-response recovery, and no duplicate package publication.

