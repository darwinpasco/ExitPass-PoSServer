# ExitPass POS Server Fiscal Reporting Reconciliation Matrix v1.0

## 1. Verdict

Report reconciliation is **NOT_IMPLEMENTED**. Fiscal document persistence supports transaction-level consistency, but no reporting read model, reconciliation service, endpoint, fixture, or signed evidence bridges documents to reports and exports.

## 2. Matrix

| Reconciliation | Available source evidence | Executable today | Missing proof/read model |
| --- | --- | --- | --- |
| Fiscal documents to X | Immutable document components | No | Governed cutoff, status/type filter, aggregation query, snapshot. |
| Fiscal documents to Z | Documents, numbering, sequence state | No | Atomic closed set, range/gaps, counters/GTA, close identity. |
| X to Z | Shared candidate table | No | Comparable as-of semantics and immutable source version. |
| Z to BIR summary | Candidate tables | No | Required direct relationship and field equations. |
| Z to Annex E | Candidate tables | No | Approved dataset/version and source report linkage. |
| Lines/tax/tenders/totals | First-class fiscal components | No report-level proof | One-source rules, code taxonomy, currency/status filters. |
| Statutory facts to discounts | Immutable applied facts plus discount/tax/totals | No | Anti-double-count projection by entitlement/benefit/VAT treatment. |
| Fiscal numbers to sequence state | Number fields, policy/state/gap audit | No | Period/series range and classified gap query. |
| Reprints to originals | Reprint schema posture | No | Runtime and report-target support; reprints excluded from sales. |
| Adjustments to originals | Link/adjustment posture | No | Runtime, signs, original-period/subsequent-period rules. |
| Central PMS payments to tenders | External references/tender facts | No report query | Governed reference uniqueness and reconciliation view. |
| APT cash to tenders | Terminal-cash statutory/fiscal references where applicable | No report query | Cash/session/shift/custody boundary and aggregate view. |
| Digital SI readback | Authoritative document presentation exists | No report reconciliation | Report-to-document drill-through/check without altering SI. |
| Export to source report | Export metadata posture | No | Manifest, checksum, source version, reproducibility proof. |

## 3. Required Reconciliation Outputs

- Per-report source document count, included/excluded status counts, and currency.
- Component checks for line net/gross, taxes, discounts, totals, and tenders.
- Senior/PWD/other benefit projection with statutory VAT facts kept distinct.
- Fiscal-number range with every gap classified and linked to sequence state.
- Previous/current/resulting counter and GTA transition for Z.
- Z-to-BIR and Z-to-Annex field-level equality under a versioned mapping.
- Export manifest containing source report/version, byte checksum, item count, generated timestamp, and validation result.
- Safe discrepancy API/output that exposes operational references without customer evidence or secrets.

## 4. UAT Gate

Controlled UAT requires deterministic synthetic and controlled fixtures that reconcile end to end, including ordinary, multi-tender, Senior Citizen, PWD, VAT-exempt, void, number-gap, replay, adjustment, and export cases. Any unexplained difference must block close/export authorization.

