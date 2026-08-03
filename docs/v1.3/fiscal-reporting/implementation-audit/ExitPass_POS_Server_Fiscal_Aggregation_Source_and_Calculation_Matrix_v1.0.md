# ExitPass POS Server Fiscal Aggregation Source and Calculation Matrix v1.0

## 1. Decision

The current fiscal-document schema contains many candidate source facts, but no approved reporting query or calculation specification binds them into X, Z, BIR, or Annex E totals. Every rule below remains a contract dependency unless marked as established by immutable fiscal persistence.

## 2. Source Matrix

| Report fact | Candidate source | Current inclusion/exclusion evidence | Sign/rounding/currency | Test evidence | Verdict or ambiguity |
| --- | --- | --- | --- | --- | --- |
| Gross sales | `pos.fiscal_document_lines.line_gross_amount_minor`, `pos.fiscal_document_totals` | No approved status or document-type filter | Minor units exist; no report currency partition | Fiscal issuance tests only | Source precedence and void treatment unresolved. |
| Net sales | Line net or document totals | No report formula | Minor units | None | Meaning relative to VAT and discounts unresolved. |
| VATable sales | `pos.fiscal_document_tax_details` | Tax classification values not repository-governed | Minor units | Fiscal snapshot tests only | Code taxonomy and formula unresolved. |
| VAT amount | Tax details and totals | No report status rule | Minor units; runtime validates documents | No report aggregation test | Avoid summing duplicate detail/total sources. |
| VAT-exempt sales | Tax details; statutory VAT treatment | Applied statutory facts are first-class | Minor units | Statutory runtime tests | Projection and anti-double-count rule absent. |
| Zero-rated sales | Tax details | No governed ordinary tax code family | Minor units | None | Classification unsupported as a report contract. |
| Total discounts | Line discounts, privilege details, totals, applied statutory facts | Multiple representations can describe one economic adjustment | Minor units | Document-level consistency only | Authoritative source and deduplication unresolved. |
| Senior Citizen | `pos.fiscal_document_applied_statutory_facts` entitlement/benefit codes and amounts | Final applied snapshots are immutable | Minor units and currency | Statutory persistence/readback tests | Report query and presentation absent. |
| PWD | Same statutory snapshot | Final applied snapshots are immutable | Minor units and currency | Statutory persistence/readback tests | Report query and presentation absent. |
| Other statutory privilege | Privilege details or future controlled codes | No frozen supported report set | Unknown | None | Compliance/code dependency. |
| VAT exemption/removal | Statutory VAT treatment and adjustment amount | Runtime validates supplied snapshot | Minor units | Statutory runtime tests | Report label and relationship to tax totals unresolved. |
| Promotional/coupon discount | Discount/privilege details | No governed ordinary discount family | Minor units | None | Must remain separate from statutory benefit. |
| Voids/cancellations | `pos.fiscal_documents` void fields/status/history | Void API exists | No report sign or period rule | Void endpoint tests | Original-period versus void-period treatment unresolved. |
| Refunds/returns | Adjustment/link posture | No refund/return runtime | Unknown | Runtime negative boundary tests | NOT_IMPLEMENTED. |
| Adjustments | `pos.fiscal_document_adjustments` plus linked documents | Table carries references, not monetary facts | Unknown | None | Must derive from immutable linked document under frozen sign rules. |
| Service charges | Lines/totals if represented | No distinct governed classification | Minor units | None | Applicability and source unresolved. |
| Tender totals | `pos.fiscal_tenders` | No repository-governed tender taxonomy | Minor units/currency | Fiscal document tests | Method grouping, reversals, and multi-tender rules unresolved. |
| Document count | `pos.fiscal_documents` | No qualifying status/type rule | Not applicable | None | Recorded/voided/failed/reprint inclusion unresolved. |
| Original/reprint count | Documents plus `pos.reprint_requests` | Reprint runtime absent; requests target documents only | Not applicable | No report tests | Fiscal sales count must not count reprints as new sales. |
| Fiscal-number range | Fiscal document series/sequence/number fields | Number allocation exists | Per series | Numbering tests | Period/series partition and gap categories unresolved. |
| Cumulative/GTA | `pos.fiscal_counter_states`, `pos.fiscal_state_snapshots` | No mutation runtime | Minor units/currency | Schema checks only | Formula, reset, and transition unimplemented. |

## 3. Required Global Rules

- Aggregate only fully committed fiscal snapshots under an approved status/type matrix.
- Choose one authoritative source per amount; detail and total snapshots are reconciliation peers, not additive inputs.
- Partition or reject mixed currencies. Never sum different currencies.
- Sum integer minor units; Central PMS/POS fiscal issuance has already resolved transaction rounding.
- Preserve statutory entitlement, benefit, VAT treatment, and ordinary adjustment categories separately.
- Define whether a void reverses the original business period or appears as a current-period adjustment.
- Treat reprints and Digital SI reads as presentation events, never new fiscal sales.
- Derive range/gaps from assigned fiscal sequence facts, not lexical document-number comparison.
- Freeze timezone, business cutoff, and `[start,end)` semantics before implementation.

## 4. Minimum Reconciliation Equations

The future implementation must prove, for each currency and governed period:

- Sum of qualifying document totals equals report current totals.
- Sum of tender amounts equals paid fiscal amount under approved change/overpayment rules.
- Tax detail categories reconcile to fiscal tax totals.
- Discount/privilege detail reconciles once to total discounts.
- Applied statutory snapshot amounts reconcile once to Senior/PWD/benefit breakdowns.
- First/last fiscal numbers plus classified gaps account for the applicable sequence state.
- Z current totals plus previous accumulated totals equal the resulting accumulated totals under the frozen GTA model.

