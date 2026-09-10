# ExitPass POS Server Future Persistent IST Reconciliation Specification v1.0

## Status and boundary

This is a non-executable specification for a separately authorized future reconciliation. It was not applied to `exitpass-pos-ist-persistent-db`. It does not authorize fiscal recovery, fiscal-document creation, historical EJ synthesis, sequence reset, rebuild, or any business-state mutation.

The future operator must take a verified backup, use a reviewed additive reconciliation artifact derived from authoritative `db/state`, inspect the target first, and preserve all existing rows, identifiers, sequences, reporting periods, header snapshots, semantic hashes, integrity hashes, and legacy `journal_context` values. The operation must run in a transaction and fail closed before constraint validation when authoritative completion ancestry cannot be established.

## Required additive state

The future reconciliation must address both currently known drift families in one reviewed change:

1. `pos.fiscal_documents`
   - add `completion_basis varchar(64)` as nullable during the additive phase;
   - add `completion_authority_ref text NULL`;
   - populate only from already persisted authoritative ancestry according to `db/state/tables/pos.fiscal_documents.sql`; do not invent a reference;
   - require `completion_basis varchar(64) NOT NULL` after every existing row has proven ancestry;
   - install `ck_fiscal_documents__completion_basis` exactly from authoritative state;
   - install `ck_fiscal_documents__completion_ancestry` exactly from authoritative state.
2. `pos.electronic_journal_records`
   - add `printable_sales_invoice_text text NULL` with no default;
   - leave every existing row null in this column;
   - do not parse, copy, reconstruct, or backfill text from `journal_context`, `event_facts`, fiscal document data, header snapshots, customer data, or current renderer output;
   - preserve `ck_ej_records__legacy_context` so canonical rows require `journal_context IS NULL`;
   - install `ck_ej_records__printable_sales_invoice` exactly from authoritative state, limiting a present value to a canonical `fiscal_document_committed` event and 1 through 131,072 characters.

## Required before/after evidence

Before and after manifests must include counts and stable ordered identity hashes for fiscal documents, EJ records, EJ streams, reporting periods, and fiscal header snapshots; stream head sequence/hash values; fiscal sequence state values; counts of canonical and non-canonical EJ rows; counts and hashes of non-null historical `journal_context`; and counts of null/non-null `printable_sales_invoice_text`.

Acceptance requires unchanged protected row counts, identities, stream heads, sequence values, hashes, reporting-period assignments, header snapshots, and historical `journal_context`; zero pre-existing rows with non-null `printable_sales_invoice_text`; all four fiscal-document completion objects present; the EJ column and printable constraint present; and disposable proof that new canonical Sales Invoice issuance persists exact text with `journal_context NULL` while report-only EJ events remain valid with printable text null.

Any mismatch, unprovable completion ancestry, non-null pre-existing printable value, constraint validation failure, or protected-manifest change requires rollback and a blocked handoff. Terminal-cash recovery remains a later, independently authorized operation after schema reconciliation is reviewed and completed.
