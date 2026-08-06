# ExitPass POS Server Z-009 Annex E Runtime Implementation Handoff v1.0

## 1. Readiness verdict

**`BLOCKED_PENDING_DECISIONS`**

Z-009 runtime is not authorized. AE-SRC-001 establishes the five profile templates and E-1 physical field order, but 24 decision records remain open. Mandatory E-1 values or semantics remain unavailable for manual SI/OR sales, NAAC/Solo Parent separation, Other discount/VAT mapping, VAT on returns, VAT payable equation, sales overrun/overflow, total income, remarks, and historical header/export details.

## 2. Frozen scope available from sources

The following can be treated as source-grounded input to a future approved task:

- E-1 is a BIR Sales Summary with 10 header facts and 32 physical detail columns.
- E-1 is summary-level; E-2 through E-5 are transaction-level statutory sales books.
- Annex E output is downstream of a committed Z and cannot mutate fiscal state.
- The governing aggregation boundary is the immutable Z, its children, counter snapshot, fiscal ranges, and classified gaps.
- Site POS Server, fiscal identity, currency, business date, reporting period, Z counter, reset counter, and GTA are authoritative recorded facts.
- Senior Citizen, PWD, and VAT removal remain distinct; no entitlement is adjudicated by the exporter.
- E-1 excludes customer-level parking, plate, ticket, evidence, payment credential, and beneficiary identity facts.

## 3. Proposed bounded runtime after approvals

Target only `RMO 24-2023 Annex E-1 BIR Sales Summary`. Consume one committed Z per output row. Generate deterministic output from immutable sources and an approved historical report header profile. Do not include E-2 through E-5 in the same implementation.

Suggested future routes, subject to API design approval:

```text
POST /v1/fiscal-reports/annex-e/e1
GET  /v1/fiscal-reports/annex-e/{annexEReference}
GET  /v1/fiscal-reports/annex-e/{annexEReference}/export
```

The command should accept an operation key and governing Z reference or approved closed-period selection only. It must not accept totals, counters, taxpayer header values, format internals, remarks, or classification mappings from the caller.

## 4. Field summary

| Metric | Count / posture |
| --- | ---: |
| E-1 header facts | 10 |
| E-1 physical detail columns | 32 |
| Total dictionary fields | 42 |
| Official detail field numbers displayed | 29 |
| Direct existing immutable fields | 17 |
| Existing derivations | 6 |
| Fields needing Z/header extension | 6 |
| Fields needing new report projection | 1 |
| Fields needing explicit schema support | 1 |
| Fields needing controlled codes | 2 |
| Fields with external decisions as primary readiness | 5 |
| Fields not currently available | 4 |

Primary readiness categories total 42. Broader decision gates may affect fields whose primary readiness is otherwise technical.

## 5. Database impact assessment

**Assessment: schema change is expected, but no SQL is authorized by Z-009A.**

### 5.1 Reuse

- Keep `pos.annex_e_reports` as immutable profile metadata bound to a governing Z.
- Use `pos.bir_sales_summary_reports` as the existing first-class E-1 summary projection rather than creating a second generic JSON payload.
- Reuse `pos.fiscal_report_output_refs` and export package objects only after hardening them for Annex E identity, correct-family codes, immutability, and privacy-safe first-class metadata.
- Do not use `request_context`, `package_context`, or `item_context` JSON as authority for E-1 fields or decisions.

### 5.2 Additive changes expected after decisions

1. Extend the immutable BIR summary/header projection with the approved historical taxpayer name, address, TIN, software name/version, release number/date, POS terminal identity, generated actor reference, and exact E-1 profile version.
2. Add first-class minor-unit fields for every approved missing physical E-1 amount: manual SI/OR, NAAC, Solo Parent, VAT-adjustment components, VAT payable, sales overrun/overflow, and total income.
3. Add a governed remarks classification reference, not generic notes.
4. Add deterministic output identity/version, filename, content type, content hash, generation operation, and regeneration/correction lineage if AE-DR-017/018 approves persistence.
5. Add uniqueness for operation identity and governing Z/profile/version; preserve one immutable successful projection per approved semantic identity.
6. Add foreign keys that prove Site POS Server, fiscal identity, currency, governing Z, period, header profile, and BIR summary scope consistency.
7. Add immutable update/delete protection and restrictive parent retention.
8. Add indexes for operation replay, governing Z lookup, Site/business-date history, profile/version, and content-hash lookup where justified.

### 5.3 Upgrade posture

Use additive expand-and-validate. Existing `pos.annex_e_reports` rows may contain only metadata; do not invent historical E-1 facts. Non-empty environments require an explicit inventory and approved migration. Missing historical mandatory header or summary values remain blocked, not defaulted.

## 6. Controlled-code impact

Expected governed families after approval:

- Annex E external profile (`e1_rmo24_2023_v1` only if approved);
- Annex E operation/status separate from report lifecycle where needed;
- Annex E remarks/exception classification;
- correction/regeneration relationship classification;
- output/package type for the approved format;
- missing/manual-sales/overrun classifications only if source models require them;
- NAAC, Solo Parent, Diplomat, and other VAT/discount mappings only after their fiscal contracts are approved.

Wrong-family references must be database-rejected. Display labels are never identity.

## 7. Runtime components

1. Scoped Annex E authorization and anti-enumeration.
2. Period/Z resolver requiring committed Z and CLOSED period.
3. Version-aware immutable source loader.
4. Historical header/profile resolver that never reads mutable current values for regeneration.
5. E-1 projection validator and checked calculator limited to approved equations.
6. Deterministic workbook renderer using an approved template/profile.
7. Content identity/hash service.
8. Atomic metadata/output repository with replay/conflict and unknown-outcome recovery.
9. Stored readback/export service that never recomputes from live fiscal documents.
10. Privacy-safe audit adapter.

## 8. Generation lifecycle

Recommended state machine after approval:

```text
REQUESTED -> PROCESSING -> COMMITTED
                       -> FAILED
                       -> REJECTED
                       -> UNKNOWN_COMMIT_OUTCOME
```

Exact replay returns the committed artifact. Changed semantics conflict. A failed validation produces no output artifact. Unknown outcome is reconciled read-only against operation, Annex metadata, output reference, content hash, and governing Z before retry.

## 9. Deterministic identity

Use a new versioned semantic identity, not the X/Z request hash. Bind operation key, Site POS Server, fiscal identity, currency, approved E-1 profile/version, governing Z reference(s), historical header profile reference, grouping identity, and output format. Exclude correlation ID, response wording, current clock, mutable labels, raw credentials, and customer data.

The final-byte SHA-256 content hash is separate from the request semantic hash. Same immutable source/profile/format must produce byte-identical output after restart.

## 10. Authorization and audit recommendations

Separate permissions are recommended:

- `fiscal_annex_e.generate`
- `fiscal_annex_e.read`
- `fiscal_annex_e.export`

None grants Z close, fiscal create/void, state initialization, EJ/POSLog generation, or submission. Server-derived Site POS Server and fiscal identity scope is mandatory. Currency and profile scope must be enforced.

Audit: requested, allowed/denied, validation failure, generated, replayed, conflict, exported, correction linked, unknown outcome, and readback. Audit must not store workbook bytes, raw TIN beyond the governed report output, customer details, credentials, semantic source, or raw hashes.

## 11. Failure contract

At minimum distinguish: unauthorized/hidden scope, period not closed, governing Z missing, profile unsupported, mandatory field missing, source version unsupported, classification unsupported, formula contract unresolved, reconciliation failure, mixed currency, ambiguous range, unexplained gap, semantic conflict, duplicate output, correction not authorized, transient persistence failure, and unknown commit outcome.

Public errors do not expose SQL, schema/table/constraint names, internal UUIDs, stack traces, profile internals, source values, credentials, or filesystem/storage details.

## 12. Validation plan

### Unit and contract

- all 42 fields mapped in physical order;
- approved equations with integer minor units and exact tolerance;
- header/profile versioning;
- workbook layout and visual/template fidelity;
- deterministic package bytes and filename;
- privacy exclusions;
- unsupported profile/version/category failure.

### PostgreSQL

- clean rebuild/upgrade/replay;
- governing Z/profile/scope integrity;
- wrong-family rejection;
- uniqueness/replay/conflict;
- immutability and parent retention;
- rollback and unknown-outcome recovery;
- no generic JSON authority;
- expected inventory/drift.

### Disposable API proof

- normal, statutory, mixed-tender, same-period void, empty-period, gap, and counter/GTA samples;
- exact replay and restart-byte equality;
- wrong-scope denial;
- unsupported/malformed source failures;
- no fiscal-state mutation manifest;
- privacy/log/secret scan;
- complete cleanup.

## 13. Retention and regeneration

Retention length, external storage, correction lineage, signing, encryption, and submission remain AE-DR-016 through 018. The runtime must not implement them by convention. It may proceed only with an explicitly bounded local generation/download scope if those external concerns are formally excluded.

## 14. Explicit exclusions

- E-2, E-3, E-4, and E-5;
- customer and beneficiary identity acquisition;
- Annex E submission portal;
- BIR approval claims;
- Electronic Journal and POSLog;
- X/Z changes or live reaggregation;
- physical printing/PDF unless separately approved;
- digital signing/encryption;
- cross-period void/refund/return/adjustment semantics;
- production rollout and Controlled UAT.

## 15. Remaining gates

All `BLOCKS_Z009` entries in the decision register must be approved and reflected in a new contract revision. E-2 through E-5 decisions may remain open only if the approved runtime scope explicitly limits itself to E-1.

Until then:

```text
Z-009 Annex E runtime implementation: NOT AUTHORIZED
Readiness verdict: BLOCKED_PENDING_DECISIONS
```
