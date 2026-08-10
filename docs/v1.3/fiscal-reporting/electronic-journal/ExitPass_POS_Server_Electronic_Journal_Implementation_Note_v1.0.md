# ExitPass POS Server Electronic Journal Implementation Note v1.0

## Decision

Z-011A hardens the existing `pos.electronic_journal_records` posture into the canonical append-only fiscal event record. It does not create a competing event store. `pos.electronic_journal_streams` is the per-scope chronology head; `pos.electronic_journal_access_audit` is separate privacy-safe access evidence and is not fiscal history.

The canonical stream scope is `(site_pos_server_id, fiscal_identity_id, currency_code)`. Historical posture rows remain `is_canonical=false`; this implementation does not synthesize pre-cutover events.

## Source-of-truth boundary

Events are appended inside the existing PostgreSQL transaction that commits the authoritative fiscal transition. The writer receives the active `NpgsqlConnection` and `NpgsqlTransaction`; it cannot commit independently. A writer failure rolls back the source transition. A later source failure rolls back the event. Readback never polls business tables to reconstruct missing events.

Implemented atomic source hooks are:

- complete fiscal-document creation, including assigned fiscal number and recorded child-fact summaries;
- supported same-period fiscal-document void;
- governed fiscal-document reprint recording and per-document copy sequencing;
- committed X Reading;
- committed Z Reading, period close, Z/reset/GTA facts, and state version;
- committed BIR sales summary bound to its governing Z Reading.

Exact source replay returns the source result and creates no new event. The writer also checks `(scope, source_transition_ref, event_type)` and returns the existing event only when its canonical semantic hash matches. Changed semantics throw a terminal journal conflict and the transaction rolls back.

The reprint correction uses the existing `pos.reprint_requests` and `pos.reprint_output_refs` objects. Legacy rows remain `is_canonical=false`. New canonical rows are written only by `PostgresFiscalDocumentReprintRepository.RecordAsync`; one document-row lock serializes copy allocation. A deferred source constraint requires the matching event at commit, while the event has a composite FK to the governing reprint and document. Failure before event append, during append, or during output persistence rolls back every source and stream-head write.

## Identity, chronology, and integrity

Contract profiles:

- event: `pos-server-electronic-journal-event:v1`
- semantic hash: `pos-server-electronic-journal-event-semantic:sha256:v1`
- integrity hash: `pos-server-electronic-journal-integrity:sha256:v1`
- chronology: `pos-server-electronic-journal-chronology:v1`
- export: `pos-server-electronic-journal-export:v1`

The stream-head row is created once per exact scope and locked `FOR UPDATE`. The next positive sequence and previous hash are read under that lock. Event insertion and the compare-and-set stream-head update occur in the same source transaction. A unique stream-sequence index and unique source-transition/event-type index provide the final database guard.

Canonical semantic input uses ordinal key ordering, invariant integer/date formatting, UTC timestamps, explicit null markers, and length-prefixed values. The integrity hash binds the semantic hash, event reference, sequence, durable recorded timestamp, previous integrity hash, actor, service identity, correlation reference, and retention classification. Hashing does not depend on JSON property order, locale, local timezone, or default runtime serialization.

Integrity verification starts at sequence 1 and validates every event through the requested high-water sequence. It fails closed for sequence gaps, reordering, changed facts, changed attribution, a broken previous-hash link, or a stream-head mismatch. Time-filtered partial-chain verification is intentionally rejected.

## Event facts and privacy

`event_facts` is a versioned, privacy-minimized fiscal projection written only by internal authoritative repositories. It contains governed codes, references, counts, integer minor-unit amounts, fiscal numbers/ranges, and transition facts. It excludes request/response bodies, authorization material, credentials, personal evidence, raw statutory identifiers, provider secrets, stack traces, connection details, and arbitrary diagnostics. The legacy `journal_context` column must be null for canonical events.

Applied statutory journal facts preserve only aggregate entitlement/benefit classifications and amounts already committed on the fiscal document. POS Server does not adjudicate entitlement and the journal does not retain beneficiary identity or evidence.

## Readback and export

Routes:

| Method | Route | Permission |
|---|---|---|
| GET | `/v1/electronic-journal/events` | `electronic_journal.read` |
| GET | `/v1/electronic-journal/exports/{format}` | `electronic_journal.export` |
| POST | `/v1/electronic-journal/integrity-verifications` | `electronic_journal.integrity.verify` |
| POST | `/v1/fiscal-documents/{fiscalDocumentId}/reprints` | `fiscal_document.reprint.record` |

All routes require exact Site POS Server, fiscal identity, and currency scope. Wildcards are invalid for Electronic Journal credentials. Production requires authority class `PRODUCTION`; `FIXTURE` and `DEVELOPMENT` fail closed. A client permission header may only narrow permissions already bound to the server-side API key.

Readback supports period, document reference/number, Z reference, event type, effective/recorded time, and correlation filters. Pages use a sequence keyset cursor containing the immutable high-water sequence. New appends cannot enter an in-progress traversal. Page size is 1-200, export is capped at 10,000 events, and paired time ranges are capped at 31 days.

JSON and RFC-4180-style UTF-8 CSV exports are deterministic projections of the same immutable high-water page as readback. They return a deterministic filename, SHA-256 content hash, output identity, ETag, `private, no-store`, and `nosniff`. Export does not mutate fiscal state. Access evidence stores only safe scope, actor/service, correlation/support references, action/result, count, and timestamp.

## Retention and recovery

Canonical streams and events carry `fiscal_reconstruction_hold`. Event UPDATE and DELETE are rejected by the database trigger. Parent foreign keys do not cascade fiscal evidence. No delete endpoint, purge worker, or application repository mutation path exists.

The repository contains no approved retention duration, archive destination, legal-hold release process, or destructive purge authorization. Z-011A therefore fails closed: records remain retained. Physical archival and purge are follow-on work after those policy decisions are frozen.

Restart reads the persisted stream head and immutable records. It does not regenerate identities, sequences, timestamps, or hashes. Disposable proof produced byte-identical JSON after API-host restart and continued chronology without duplication.

## Validation evidence

PostgreSQL 16 disposable validation used a unique container, loopback port, synthetic databases, and no persistent volume. It proved clean rebuild, controlled-code load/replay, expected inventory, drift, real API generation, exact replay, concurrent append serialization and retry, source/event rollback in both directions, keyset high-water behavior, deterministic JSON/CSV export, restart byte equality, direct immutability rejection, and altered/reordered/missing-event detection. Focused reprint proof additionally exercised concurrent copy sequences, direct orphan-source/orphan-event rejection, failure before and after event append, clean retry, changed-semantic conflict, exact scoped authorization, Production fixture rejection, and byte-identical filtered export after restart.

Existing integration suites also exercised fiscal-document creation/void boundaries, X Reading, Z Reading/atomic close, and BIR sales-summary generation with journal hooks enabled. No real customer, payment, or statutory evidence was used.

## Exclusions

- POSLog v6.0 mapping and generation are not authorized. Future POSLog must project from this stream.
- Annex E and its unresolved external decisions are not part of this implementation.
- Adjustment and Digital SI publication event codes remain reserved because no authoritative runtime transition exists; a controlled code alone is not coverage.
- Failed attempts that produce no durable authoritative fiscal transition remain operational/audit outcomes, not fabricated fiscal events. Exact replay resolves to the original committed event.
- Staff UI, physical printing, destructive retention, historical synthesis, Controlled UAT, and production rollout are not authorized.
