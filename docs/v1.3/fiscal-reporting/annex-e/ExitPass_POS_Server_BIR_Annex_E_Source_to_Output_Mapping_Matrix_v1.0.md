# ExitPass POS Server BIR Annex E Source-to-Output Mapping Matrix v1.0

## 1. Mapping rules

This matrix maps every E-1 field from the field dictionary. `Projection` means a future immutable Annex E/BIR projection, not live recomputation. No current Annex E runtime service exists.

Readiness values are limited to the classifications required by Z-009A.

## 2. Header mapping

| Field | Schema.table.column or immutable field | Runtime/service | Kind / lifecycle / grain | Availability | Gap | Required Z-009 action |
| --- | --- | --- | --- | --- | --- | --- |
| H01 Taxpayer name | `pos.fiscal_document_header_snapshots.registered_business_name`; `pos.fiscal_identities.registered_business_name` | Future immutable header projection | Z / committed / report profile | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Z does not bind report-time header profile | Snapshot one approved identity/header profile per Annex E scope |
| H02 Taxpayer address | `pos.fiscal_document_header_snapshots.registered_business_address` | Future immutable header projection | Z / committed / report profile | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Same as H01 | Persist or bind immutable profile facts |
| H03 TIN | `pos.fiscal_document_header_snapshots.tin` | Future immutable header projection | Z / committed / report profile | `REQUIRES_Z_SNAPSHOT_EXTENSION` | Same as H01 | Persist taxpayer TIN only; exclude beneficiary TIN |
| H04 Software name/version | `pos.fiscal_identities.software_name`, `software_version` | Future release-profile resolver | Z / committed / report profile | `REQUIRES_SCHEMA_CHANGE` | Values are mutable and not Z-bound | Add immutable approved release/profile reference |
| H05 Release no/date | No complete first-class historical source | Future release-profile resolver | Z / committed / report profile | `NOT_AVAILABLE` | Release number/date are not modeled together | Add approved release profile or first-class fields |
| H06 Serial no. | `pos.sales_invoice_header_profiles.pos_serial_number`; BIR summary posture | Future BIR/Annex header projection | Z / committed / report profile | `READY_EXISTING_DERIVATION` | Must bind the profile historically | Reference immutable approved header profile |
| H07 MIN | `pos.sales_invoice_header_profiles.machine_identification_number`; BIR summary posture | Future BIR/Annex header projection | Z / committed / report profile | `READY_EXISTING_DERIVATION` | Must bind the profile historically | Reference immutable approved header profile |
| H08 POS terminal no. | `pos.channel_terminals.channel_terminal_code`; document snapshots have optional `terminal_id` | Future scope resolver | Z / committed / scope | `REQUIRES_EXTERNAL_DECISION` | Multi-channel period has no one terminal | Approve combined Site POS Server or per-terminal profile |
| H09 Generated date/time | `pos.annex_e_reports.generated_at` | Future export operation | Annex E / committed / output | `READY_EXISTING_FIELD` | Deterministic regeneration semantics unresolved | Freeze operation identity and regeneration lineage |
| H10 UserID | Fiscal report request/audit service identity reference | Future Annex E audit adapter | Annex E / committed / output | `REQUIRES_NEW_REPORT_PROJECTION` | Exact public actor field is not frozen | Persist privacy-safe actor/service reference |

## 3. Detail mapping

| Field | Schema.table.column or immutable field | Calculation input / service | Kind / lifecycle / grain | Availability | Current gap | Required Z-009 action |
| --- | --- | --- | --- | --- | --- | --- |
| D01 Date | `pos.x_z_reports.business_day_date` | Stored Z readback | Z / COMMITTED and period CLOSED / Z row | `READY_EXISTING_FIELD` | File grouping unresolved | Select governing Z only |
| D02 Beginning SI/OR | `pos.fiscal_report_fiscal_number_ranges.first_fiscal_number` and Z `beginning_si_ref` | Stored Z ranges | Z / committed / range | `READY_EXISTING_FIELD` | Multi-series representation unresolved | Approve one-row/multi-range rule |
| D03 Ending SI/OR | Range `last_fiscal_number` and Z `ending_si_ref` | Stored Z ranges | Z / committed / range | `READY_EXISTING_FIELD` | Same as D02 | Same as D02 |
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
| D17 Returns | Z `return_amount_minor_units` reserved | Z runtime currently fail-closed | Z / committed / period | `REQUIRES_EXTERNAL_DECISION` | Return attribution/sign unsupported | Approve return contract before nonzero output |
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
| D32 Remarks | No governed Annex E remark classification | None | Annex E / output / period | `REQUIRES_CONTROLLED_CODE` | Free text would be unsafe and nondeterministic | Approve controlled remarks and source rules |

## 4. Readiness totals

| Classification | Count |
| --- | ---: |
| `READY_EXISTING_FIELD` | 17 |
| `READY_EXISTING_DERIVATION` | 6 |
| `REQUIRES_Z_SNAPSHOT_EXTENSION` | 6 |
| `REQUIRES_NEW_REPORT_PROJECTION` | 1 |
| `REQUIRES_SCHEMA_CHANGE` | 1 |
| `REQUIRES_CONTROLLED_CODE` | 2 |
| `REQUIRES_EXTERNAL_DECISION` | 5 |
| `NOT_AVAILABLE` | 4 |
| `NOT_APPLICABLE` | 0 |

These are primary per-field readiness classifications and total 42. A field can also reference one or more broader decision gates without changing its primary classification.

## 5. Source-of-truth boundary

The future runtime must consume the committed Z snapshot, immutable children, immutable counter snapshot, and an approved historical header/profile binding. It must not query live transaction tables to recalculate closed-period totals. A new first-class BIR/Annex E projection may validate and reshape recorded facts, but it must not become a second independently recomputed fiscal truth.
