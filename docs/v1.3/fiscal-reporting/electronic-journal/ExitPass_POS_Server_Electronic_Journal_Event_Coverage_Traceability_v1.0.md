# ExitPass POS Server Electronic Journal Event Coverage and Source-Transition Traceability v1.0

## Classification rule

Every Z-011A-required category is classified as exactly one of:

- `IMPLEMENTED_WITH_ATOMIC_AUTHORITATIVE_WRITER`
- `CAPTURED_WITHIN_ANOTHER_GOVERNING_CANONICAL_EVENT`
- `NOT_APPLICABLE_NO_AUTHORITATIVE_RUNTIME_TRANSITION`
- `UNSUPPORTED_BY_CURRENT_FISCAL_CONTRACT`
- `EXPLICITLY_OUTSIDE_Z011A_AUTHORIZATION`

A controlled code without a source writer is not implementation evidence.

## Required coverage reconciliation

| Required fiscal activity | Classification | Event type | Authoritative transition and evidence | Atomic facts or fail-closed boundary |
|---|---|---|---|---|
| Fiscal-document acceptance and durable creation | `IMPLEMENTED_WITH_ATOMIC_AUTHORITATIVE_WRITER` | `fiscal_document_committed` | `PostgresFiscalDocumentRepository.CreateAsync` | Complete document, assigned number, status history, child facts, and event commit in one transaction. |
| Fiscal-number assignment | `CAPTURED_WITHIN_ANOTHER_GOVERNING_CANONICAL_EVENT` | `fiscal_document_committed` | Number allocation in the creation transaction | Sequence policy, sequence value, formatted number, and assignment timestamp are event facts; no second event fragments creation. |
| Fiscal-document status transitions | `CAPTURED_WITHIN_ANOTHER_GOVERNING_CANONICAL_EVENT` | `fiscal_document_committed`, `fiscal_document_voided` | Initial recorded status is part of creation; supported same-period void has its own writer | No other governed status-transition runtime exists. |
| Fiscal issuance success | `CAPTURED_WITHIN_ANOTHER_GOVERNING_CANONICAL_EVENT` | `fiscal_document_committed` | Durable creation result | Success is represented by the committed source transition, not a diagnostic outcome event. |
| Fiscal issuance failure, retry, replay, and conflict where fiscally relevant | `NOT_APPLICABLE_NO_AUTHORITATIVE_RUNTIME_TRANSITION` | None beyond original committed event | Failed/conflicting attempts commit no fiscal state; exact replay resolves the original document/event | Operational outcomes remain safe audit/log evidence and do not fabricate fiscal history. |
| Tender, tax, discount, total, and payable-basis facts | `CAPTURED_WITHIN_ANOTHER_GOVERNING_CANONICAL_EVENT` | `fiscal_document_committed` | Child inserts and complete document event share the creation transaction | Governed classifications, counts, and integer minor-unit facts are minimized canonical facts. |
| Digital Sales Invoice publication state | `NOT_APPLICABLE_NO_AUTHORITATIVE_RUNTIME_TRANSITION` | `digital_si_published` reserved only | Existing Digital SI routes render/read immutable document facts; no publication-state writer exists | No publication event is synthesized from reads or URL posture. |
| Reprint recording and copy sequencing | `IMPLEMENTED_WITH_ATOMIC_AUTHORITATIVE_WRITER` | `fiscal_document_reprint_recorded` | `PostgresFiscalDocumentReprintRepository.RecordAsync` | Canonical request, output reference, per-document copy sequence, event, and stream head commit together. Deferred source/event constraint plus event FK enforce both directions. |
| Supported same-period void | `IMPLEMENTED_WITH_ATOMIC_AUTHORITATIVE_WRITER` | `fiscal_document_voided` | `PostgresFiscalDocumentRepository.VoidAsync` | Prior/resulting status, number/sequence, and governed reason commit atomically. |
| Cancellation, refund, return, cross-period void, adjustment, and unsupported service charge | `UNSUPPORTED_BY_CURRENT_FISCAL_CONTRACT` | `fiscal_adjustment_recorded` reserved only | No approved attribution/sign contract or authoritative writer | Existing mutation paths fail closed; no event is emitted. |
| X Reading commitment | `IMPLEMENTED_WITH_ATOMIC_AUTHORITATIVE_WRITER` | `x_reading_committed` | `PostgresFiscalXReadingRepository.GenerateAsync` | Immutable snapshot and governed child facts commit with the event. |
| Z Reading commitment and fiscal-period closure | `IMPLEMENTED_WITH_ATOMIC_AUTHORITATIVE_WRITER` | `z_reading_committed` | `PostgresFiscalZReadingRepository.CloseAsync` | Report, children, state transition, and OPEN-to-CLOSED period transition commit with the event. |
| Reset-counter and grand-total transition | `CAPTURED_WITHIN_ANOTHER_GOVERNING_CANONICAL_EVENT` | `z_reading_committed` | Canonical Z-state transition | Previous/resulting reset, previous/resulting Z, previous/current/resulting GTA, and state version are bound to the Z event. |
| BIR sales-summary commitment | `IMPLEMENTED_WITH_ATOMIC_AUTHORITATIVE_WRITER` | `bir_sales_summary_committed` | `PostgresBirSalesSummaryRepository.GenerateAsync` | Immutable summary, governing Z identity, and event commit together. |
| Governed fiscal-report exports where classified as journal events | `NOT_APPLICABLE_NO_AUTHORITATIVE_RUNTIME_TRANSITION` | `fiscal_report_exported` reserved only | Z-008/Z-010 exports are deterministic read-only projections and are not classified as fiscal-state transitions | Access audit applies; no canonical fiscal event is appended. |
| ARTS POSLog 6.0.0 mapping and generation | `EXPLICITLY_OUTSIDE_Z011A_AUTHORIZATION` | None | Future projection from this canonical stream | No profile mapping, XML, XSD, extension profile, or separate POSLog store is introduced. |
| Annex E runtime | `EXPLICITLY_OUTSIDE_Z011A_AUTHORIZATION` | None | Separate externally gated task | No Annex E facts or requirements are inferred. |

## Canonical reprint traceability

The recording route is `POST /v1/fiscal-documents/{fiscalDocumentId}/reprints` under permission `fiscal_document.reprint.record`. The request carries only operation identity, exact scoped references, and one governed reason. Actor, service, correlation, and Production authority are server-derived.

The source identity is `reprint_reference` under `pos-server-fiscal-document-reprint:v1`. Its semantic hash uses `pos-server-fiscal-document-reprint:sha256:v1` and binds the document, exact Site POS Server/fiscal identity/currency scope, governed reason, actor, and service. Correlation is attribution, not replay semantics.

The source row snapshots the original fiscal number and sequence and allocates one positive `copy_sequence` while the fiscal-document row is locked. The canonical event stores first-class source links plus privacy-safe facts for the reprint/output references, original fiscal-document reference, copy sequence, governed type/status/reason, output type, and required reprint-label posture. No output bytes, customer identity, arbitrary reason text, statutory evidence, or credentials are stored.

`pos.reprint_requests.trg_reprint_requests_require_journal` is deferred to transaction commit and rejects a canonical reprint without its matching event. `pos.electronic_journal_records.fk_ej_records__reprint` rejects a reprint event without its governing reprint. Canonical request, output, and event rows are immutable and retained without cascade deletion.

## Outcome semantics

- A successful authoritative transition and its event commit together.
- A failed source transaction has no canonical fiscal event because no fiscal state changed.
- Exact source replay returns the original reprint and event without changing the copy sequence.
- Changed-semantic operation-key reuse returns a terminal safe conflict.
- Concurrent copies serialize on the authoritative document row; stream sequencing separately serializes on the Electronic Journal stream row.
- Operational authorization denials, malformed reads, exports, and integrity inspections are safe access/audit evidence, not fiscal-history events.

## Reconstruction boundary

For implemented transitions, the ordered event stream provides immutable identity, chronology, attribution, source version, integrity metadata, and minimized fiscal facts needed to reconstruct supported fiscal activity. Canonical business tables remain authoritative for the full legal record. The Electronic Journal neither replaces those tables nor reconstructs unsupported historical or future activity by inference.
