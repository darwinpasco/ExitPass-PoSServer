# ExitPass POS Server BIR Annex E Source-to-Output Mapping Matrix v1.0

## 1. Mapping rules

This matrix maps every E-1 field from the field dictionary. `Projection` means a future immutable Annex E/BIR projection, not live recomputation. No current Annex E runtime service exists. Z-012A revalidated the matrix on 2026-08-10 PHT against merged Z-010 and Z-011A at baseline `227cdc708d1a685cd56986f084d0cc1aad3e81dd`.

Readiness values are limited to the classifications required by Z-009A.

## 2. Header mapping

| Field | Schema.table.column or immutable field | Runtime/service | Kind / lifecycle / grain | Availability | Gap | Required Z-009 action |
| --- | --- | --- | --- | --- | --- | --- |
| H01 Taxpayer name | `pos.fiscal_document_header_snapshots.registered_business_name`; `pos.fiscal_identities.registered_business_name` | Future immutable header projection | Z / committed / report profile | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Z does not bind report-time header profile | Snapshot one approved identity/header profile per Annex E scope |
| H02 Taxpayer address | `pos.fiscal_document_header_snapshots.registered_business_address` | Future immutable header projection | Z / committed / report profile | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Same as H01 | Persist or bind immutable profile facts |
| H03 TIN | `pos.fiscal_document_header_snapshots.tin` | Future immutable header projection | Z / committed / report profile | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Same as H01 | Persist taxpayer TIN only; exclude beneficiary TIN |
| H04 Software name/version | `pos.fiscal_identities.software_name`, `software_version` | Future release-profile resolver | Z / committed / report profile | `REQUIRES_SCHEMA_CHANGE` | Values are mutable and not Z-bound | Add immutable approved release/profile reference |
| H05 Release no/date | No complete first-class historical source | Future release-profile resolver | Z / committed / report profile | `NOT_AVAILABLE` | Release number/date are not modeled together | Add approved release profile or first-class fields |
| H06 Serial no. | `pos.bir_sales_summary_reports.pos_serial_number` | Committed BIR Sales Summary readback | Z / committed / report profile | `READY_EXISTING_FIELD` | None for the committed summary binding | Copy the immutable BIR Summary value |
| H07 MIN | `pos.bir_sales_summary_reports.machine_identification_number` | Committed BIR Sales Summary readback | Z / committed / report profile | `READY_EXISTING_FIELD` | None for the committed summary binding | Copy the immutable BIR Summary value |
| H08 POS terminal no. | Governed Site POS Server/fiscal-identity header profile; child `pos.channel_terminals` are not output identities | Future header projection | Z / committed / scope | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Historical terminal identity is not Z-bound; examiner acceptance remains external | Snapshot the governed fiscal terminal identity; confirm under AE-DR-020A |
| H09 Generated date/time | `pos.annex_e_reports.generated_at` | Future export operation | Annex E / committed / output | `READY_EXISTING_FIELD` | Deterministic regeneration semantics unresolved | Freeze operation identity and regeneration lineage |
| H10 UserID | Fiscal report request/audit service identity reference | Future Annex E audit adapter | Annex E / committed / output | `REQUIRES_NEW_REPORT_PROJECTION` | No immutable Annex E actor projection | Persist the server-derived privacy-safe actor/service reference approved under AE-DR-005A |

## 3. Detail mapping

| Field | Schema.table.column or immutable field | Calculation input / service | Kind / lifecycle / grain | Availability | Current gap | Required Z-009 action |
| --- | --- | --- | --- | --- | --- | --- |
| D01 Date | `pos.x_z_reports.business_day_date` | Stored Z readback | Z / COMMITTED and period CLOSED / Z row | `READY_EXISTING_FIELD` | Official cell display remains external | Select governing Z business date; grouping is AE-DR-003A |
| D02 Beginning SI/OR | `pos.fiscal_report_fiscal_number_ranges.first_fiscal_number` and Z `beginning_si_ref` | Stored Z ranges | Z / committed / range | `READY_EXISTING_FIELD` | E-1 has one representable range; multi-range/gap output has no approved representation | Apply AE-DR-022 fail-closed recommendation |
| D03 Ending SI/OR | Range `last_fiscal_number` and Z `ending_si_ref` | Stored Z ranges | Z / committed / range | `READY_EXISTING_FIELD` | Same as D02 | Apply AE-DR-022 fail-closed recommendation |
| D04 GTA ending | `pos.x_z_reports.present_grand_total_amount_minor_units`; Z counter snapshot resulting GTA | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None for one currency | Copy recorded value |
| D05 GTA beginning | Z previous GTA; counter snapshot previous GTA | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D06 Manual SI/OR sales | None | None | External/manual / period | `NOT_AVAILABLE` | No source or ingestion contract | Separate approved manual-sales source and reconciliation |
| D07 Gross sales | Z `gross_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | E-1 formula relation unresolved | Copy recorded value |
| D08 VATable sales | Z `vatable_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D09 VAT amount | Z `vat_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D10 VAT-exempt sales | Z `vat_exempt_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D11 Zero-rated sales | Z `zero_rated_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D12 SC discount | Z `senior_citizen_discount_amount_minor_units`; discount child `senior_citizen` | Stored Z readback | Z / committed / classification | `READY_EXISTING_FIELD` | None | Copy and reconcile top-level to child |
| D13 PWD discount | Z `pwd_discount_amount_minor_units`; discount child `pwd` | Stored Z readback | Z / committed / classification | `READY_EXISTING_FIELD` | None | Copy and reconcile top-level to child |
| D14 NAAC discount | No separate Z field; possibly folded into other statutory in future | None | Z / committed / classification | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Cannot distinguish NAAC | Add governed classification and immutable child projection |
| D15 Solo Parent discount | No separate Z field | None | Z / committed / classification | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Cannot distinguish Solo Parent | Add governed classification and immutable child projection |
| D16 Other discount | Z other statutory, coupon, promotional; exact composition unresolved | Future classification projection | Z / committed / classification | `REQUIRES_EXTERNAL_DECISION` | Official `Others` meaning is undefined | Approve included controlled codes |
| D17 Returns | Z `return_amount_minor_units` reserved | Z runtime fail-closed | Z / committed / period | `REQUIRES_SCHEMA_CHANGE` | Nonzero return attribution/sign unsupported | Preserve recorded zero only; block nonzero until a separate approved contract |
| D18 Voids | Z `void_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | Same-period only | Copy governed same-period void total |
| D19 Total deductions | D12:D18 after approved composition | Future Annex E projection | Annex E / output / period | `READY_EXISTING_DERIVATION` | Formula/field numbering unresolved | Implement checked sum only after AE-DR-006 |
| D20 SC VAT adjustment | `pos.fiscal_report_discount_breakdowns.vat_exemption_amount_minor_units` for SC | Stored Z child projection | Z / committed / classification | `READY_EXISTING_DERIVATION` | Ensure child is always present or governed zero | Copy immutable child amount |
| D21 PWD VAT adjustment | Discount child VAT exemption for PWD | Stored Z child projection | Z / committed / classification | `READY_EXISTING_DERIVATION` | Same as D20 | Copy immutable child amount |
| D22 Other VAT adjustment | Other discount child VAT exemption | Future controlled projection | Z / committed / classification | `REQUIRES_EXTERNAL_DECISION` | Diplomat/other mapping unresolved | Approve included VAT treatment codes |
| D23 VAT on returns | No first-class Z VAT-on-return field | None | Z / committed / period | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Return tax component unavailable | Extend immutable Z or add governed BIR projection |
| D24 Other VAT adjustment | No governed E-1 category mapping | None | Z / committed / classification | `REQUIRES_CONTROLLED_CODE` | Arbitrary other values prohibited | Add approved classification family/value set |
| D25 Total VAT adjustment | Sum D20:D24 | Future Annex E projection | Annex E / output / period | `READY_EXISTING_DERIVATION` | Components D22-D24 unresolved | Checked sum after approvals |
| D26 VAT payable | Z VAT and adjustment facts exist, but workbook formula interpretation unresolved | Future Annex E projection | Annex E / output / period | `REQUIRES_EXTERNAL_DECISION` | Formula `23 = 8-19` is ambiguous against physical columns | Obtain accounting/BIR interpretation |
| D27 Net sales | Z `net_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | Must reconcile to approved E-1 equation | Copy, then validate only |
| D28 Sales overrun/overflow | None | None | External exception / period | `NOT_AVAILABLE` | No source definition or state | Approve meaning and add first-class source if applicable |
| D29 Total income | None as named E-1 fact | None | Annex E / output / period | `NOT_AVAILABLE` | No approved equation | Approve equation/source |
| D30 Reset counter | Z counter snapshot resulting reset; Z report reset | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D31 Z counter | Z counter snapshot resulting Z; Z report Z counter | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D32 Remarks | No governed Annex E remark classification | Future controlled projection | Annex E / output / period | `REQUIRES_CONTROLLED_CODE` | Controlled values are internally approved; examiner acceptance remains external | Implement approved AE-DR-010A; never allow free text |

## 4. Readiness totals

| Classification | Count |
| --- | ---: |
| `READY_EXISTING_FIELD` | 19 |
| `READY_EXISTING_DERIVATION` | 4 |
| `REQUIRES_Z_SNAPSHOT_EXTENSION` | 7 |
| `REQUIRES_NEW_REPORT_PROJECTION` | 1 |
| `REQUIRES_SCHEMA_CHANGE` | 2 |
| `REQUIRES_CONTROLLED_CODE` | 2 |
| `REQUIRES_EXTERNAL_DECISION` | 3 |
| `NOT_AVAILABLE` | 4 |
| `NOT_APPLICABLE` | 0 |

These are primary per-field readiness classifications and total 42. A field can also reference one or more broader decision gates without changing its primary classification.

## 5. Source-of-truth boundary

The future runtime must consume the committed Z snapshot, immutable children, immutable counter snapshot, committed BIR Sales Summary, and an approved historical header/profile binding. It must not query live transaction tables to recalculate closed-period totals. Z-010's BIR Sales Summary is the preferred first-class source where it carries the required fact. A new Annex E projection may validate and reshape recorded facts, but it must not become a second independently recomputed fiscal truth. Z-011A Electronic Journal events provide traceability and integrity evidence only; they are not the financial authority.

The readiness classifications above describe data availability. AE-DR-005A, AE-DR-011, AE-DR-010A, AE-DR-017, and AE-DR-018 are approved ExitPass decisions under `Z-009B-USER-APPROVAL-001`. AE-DR-006 through AE-DR-009, AE-DR-011A, AE-DR-012, and AE-DR-019 remain external gates where they affect mandatory E-1 values.

The exact per-position Z-012A reconciliation, including null, zero, no-activity, unsupported behavior, and staged gate classification, is in [Z-012A Runtime Authorization Revalidation](ExitPass_POS_Server_Z012A_Annex_E1_Runtime_Authorization_Revalidation_v1.0.md).

## 6. Z-012A1 candidate source map

The following candidate map is pending exact Accounting approval and does not replace the primary readiness classifications above.

| Position | Candidate source or derivation | Candidate source class | Required capability before runtime |
|---|---|---|---|
| D06 | Immutable period fact `manual_si_or_net_income_minor_units` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Governed Manual SI/OR fact and explicit zero attestation |
| D07 | BIR Summary gross plus governed return and void magnitudes | `AVAILABLE_CANONICAL_DERIVATION` | Exact-profile approval |
| D09 | BIR Summary VAT amount | `AVAILABLE_CANONICAL_SOURCE` | Exact-profile approval for equation use |
| D12-D13 | BIR Summary SC and PWD discount amounts | `AVAILABLE_CANONICAL_SOURCE` | Exact-profile approval |
| D14-D15 | Separate immutable NAAC and Solo Parent facts | `REQUIRES_NEW_FIRST_CLASS_FACT` | Classification facts or explicit zero attestations |
| D16 | Other statutory plus coupon plus promotional discount | `AVAILABLE_CANONICAL_DERIVATION` | Reject nonzero unresolved privilege facts |
| D17-D18 | BIR Summary return and same-period void amounts | `AVAILABLE_CANONICAL_SOURCE` | Return remains zero-only under bounded contract |
| D19 | Sum D12 through D18 | `AVAILABLE_CANONICAL_DERIVATION` | Exact-profile approval and authoritative operands |
| D20-D21 | Governing Z statutory discount-child VAT exemptions | `AVAILABLE_CANONICAL_DERIVATION` | Child-to-summary binding validation |
| D22-D24 | Explicitly attested zero under bounded unsupported paths | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | First-class absence/zero evidence; nonzero blocks |
| D25 | Sum D20 through D24 | `AVAILABLE_CANONICAL_DERIVATION` | Authoritative component facts |
| D26 | D09 minus D22, preserving literal fields 8 and 19 | `AVAILABLE_CANONICAL_DERIVATION` | Exact-profile approval |
| D27 | D07 minus D19 minus D09 | `AVAILABLE_CANONICAL_DERIVATION` | Exact-profile approval; remains distinct from VAT-inclusive Z net |
| D28 | Immutable period fact `sales_overrun_overflow_net_income_minor_units` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Governed overflow fact/event and explicit zero attestation |
| D29 | D27 plus D06 plus D28 | `AVAILABLE_CANONICAL_DERIVATION` | Exact-profile approval and authoritative D06/D28 |

The complete definitions and source statuses are in the [Accounting Calculation Profile Proposal](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md). Missing first-class facts remain unknown, not zero.
