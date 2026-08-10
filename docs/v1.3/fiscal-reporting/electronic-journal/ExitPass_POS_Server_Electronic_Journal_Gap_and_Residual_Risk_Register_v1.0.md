# ExitPass POS Server Electronic Journal Gap and Residual-Risk Register v1.0

## Resolved correction

`EJ-GAP-001` is resolved in Z-011A: canonical reprint recording now has an authorized runtime writer, immutable per-document copy sequence, bidirectional source/event database binding, atomic append, replay/conflict handling, concurrent-copy proof, and deterministic readback/export proof.

## Active gaps

| ID | Priority | Gap or residual risk | Current fail-closed posture | Owner | Recommended bounded follow-up |
|---|---|---|---|---|---|
| EJ-GAP-002 | P0 | Refund, return, cross-period void, adjustment, cancellation, and unsupported service-charge semantics remain unresolved. | Existing writers reject unsupported mutations; no event is emitted. | Fiscal contract/accounting | Freeze source semantics, then implement source writer and journal hook together. |
| EJ-GAP-003 | P2 | Digital SI has deterministic rendering/readback but no authoritative publication-state transition. | `digital_si_published` remains reserved and unwritten. | Digital SI | Define publication state and atomically append its event. |
| EJ-GAP-004 | P1 | ARTS POSLog 6.0.0 record/profile mapping is unresolved. | No POSLog runtime or separate store exists. | Z-011B / compliance | Map POSLog strictly as a projection from canonical events. |
| EJ-GAP-005 | P1 | Annex E external confirmations remain unresolved. | No Annex E inference, code, or event mapping is introduced. | Z-009 / compliance | Resolve external confirmations before bounded E-1 implementation. |
| EJ-GAP-006 | P1 | Retention duration, archive destination, legal-hold release, and destructive purge authority are not frozen. | `fiscal_reconstruction_hold`; direct deletion and application purge are unavailable. | Legal/compliance/operations | Freeze retention and archival contract before any purge task. |
| EJ-GAP-007 | P2 | Canonical events begin only at the Z-011A cutover. | Legacy posture rows are excluded; no historical synthesis occurs. | Product/fiscal authority | Authorize a separate deterministic backfill contract or accept the documented cutover boundary. |
| EJ-GAP-008 | P2 | A stream can exceed the 10,000-event single-export limit. | Export rejects oversized snapshots; keyset readback remains available. | Electronic Journal | Add governed segmented export manifests without changing event authority. |
| EJ-GAP-009 | P3 | Denied access is emitted through safe security logging rather than canonical fiscal-event storage. | No attacker-controlled denied request can append fiscal history. | Security/observability | Define a durable security-audit sink separate from Electronic Journal facts if required. |
| EJ-GAP-010 | P2 | Physical archival and recovery from an external archive are not implemented. | Primary immutable PostgreSQL records remain retained; integrity verification reports gaps. | Operations/continuity | Implement governed archive manifests, restore verification, and legal-hold controls after policy approval. |

Controlled UAT and production rollout remain unauthorized. None of these gaps authorizes weakening append-only, scope, privacy, or integrity controls.
