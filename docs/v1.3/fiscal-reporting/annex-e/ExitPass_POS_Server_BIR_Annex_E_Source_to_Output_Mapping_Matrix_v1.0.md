# ExitPass POS Server BIR Annex E Source-to-Output Mapping Matrix v1.0

## 1. Mapping rules

This matrix maps every E-1 field from the field dictionary. Z-012B implements the immutable Annex E projection without live fiscal-document recomputation. Z-012A revalidated the source contract on 2026-08-10 PHT; Z-012B records the implemented availability below.

Readiness values are limited to the classifications required by Z-009A.

## 2. Header mapping

| Field | Schema.table.column or immutable field | Runtime/service | Kind / lifecycle / grain | Availability | Gap | Required Z-009 action |
| --- | --- | --- | --- | --- | --- | --- |
| H01 Taxpayer name | `pos.fiscal_identities.registered_business_name`; immutable `pos.annex_e1_workbooks.taxpayer_name` | Z-012B header snapshot | Annex E / committed / workbook | `READY_EXISTING_FIELD` | A changed value inside one month blocks rather than mixing history | Snapshot and require one stable monthly value |
| H02 Taxpayer address | `pos.fiscal_identities.registered_business_address`; immutable workbook snapshot | Z-012B header snapshot | Annex E / committed / workbook | `READY_EXISTING_FIELD` | Same as H01 | Snapshot and require one stable monthly value |
| H03 TIN | `pos.fiscal_identities.tin`; immutable workbook snapshot | Z-012B header snapshot | Annex E / committed / workbook | `READY_EXISTING_FIELD` | Beneficiary TIN remains excluded | Snapshot taxpayer TIN only |
| H04 Software name/version | Versioned Z-012B release constants; immutable workbook snapshot | Z-012B renderer profile | Annex E / committed / workbook | `READY_EXISTING_FIELD` | External acceptance remains staged | Persist the versioned renderer values |
| H05 Release no/date | Versioned Z-012B release constants; immutable workbook snapshot | Z-012B renderer profile | Annex E / committed / workbook | `READY_EXISTING_FIELD` | External acceptance remains staged | Persist the versioned renderer values |
| H06 Serial no. | `pos.bir_sales_summary_reports.pos_serial_number` | Committed BIR Sales Summary readback | Z / committed / report profile | `READY_EXISTING_FIELD` | None for the committed summary binding | Copy the immutable BIR Summary value |
| H07 MIN | `pos.bir_sales_summary_reports.machine_identification_number` | Committed BIR Sales Summary readback | Z / committed / report profile | `READY_EXISTING_FIELD` | None for the committed summary binding | Copy the immutable BIR Summary value |
| H08 POS terminal no. | `pos.site_pos_servers.site_pos_server_code`; immutable workbook snapshot | Z-012B header snapshot | Annex E / committed / scope | `READY_EXISTING_FIELD` | Examiner acceptance remains AE-DR-020A | Snapshot the governed Site POS Server identity; never concatenate child channels |
| H09 Generated date/time | `pos.annex_e1_workbooks.generated_at` | Z-012B generation operation | Annex E / committed / output | `READY_EXISTING_FIELD` | Official display remains AE-DR-019 | Preserve the first committed generation instant on replay |
| H10 UserID | `pos.annex_e1_workbooks.generated_by_ref` and `service_identity_ref` | Z-012B audit projection | Annex E / committed / output | `READY_EXISTING_FIELD` | None for local bounded runtime | Persist server-derived privacy-safe authority reference |

## 3. Detail mapping

| Field | Schema.table.column or immutable field | Calculation input / service | Kind / lifecycle / grain | Availability | Current gap | Required Z-009 action |
| --- | --- | --- | --- | --- | --- | --- |
| D01 Date | `pos.x_z_reports.business_day_date` | Stored Z readback | Z / COMMITTED and period CLOSED / Z row | `READY_EXISTING_FIELD` | Official cell display remains external | Select governing Z business date; grouping is AE-DR-003A |
| D02 Beginning SI/OR | `pos.fiscal_report_fiscal_number_ranges.first_fiscal_number` and Z `beginning_si_ref` | Stored Z ranges | Z / committed / range | `READY_EXISTING_FIELD` | E-1 has one representable range; multi-range/gap output has no approved representation | Apply AE-DR-022 fail-closed recommendation |
| D03 Ending SI/OR | Range `last_fiscal_number` and Z `ending_si_ref` | Stored Z ranges | Z / committed / range | `READY_EXISTING_FIELD` | Same as D02 | Apply AE-DR-022 fail-closed recommendation |
| D04 GTA ending | `pos.x_z_reports.present_grand_total_amount_minor_units`; Z counter snapshot resulting GTA | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None for one currency | Copy recorded value |
| D05 GTA beginning | Z previous GTA; counter snapshot previous GTA | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D06 Manual SI/OR sales | `pos.annex_e1_period_accounting_facts.amount_minor_units` with type `manual_si_or_net_income` | Z-012B period-fact service | Annex E source / exact scope+period | `READY_EXISTING_FIELD` | Missing fact still blocks | Accept only governed `RECORDED` or explicit `ATTESTED_ZERO` evidence |
| D07 Gross sales | `pos.bir_sales_summary_reports.gross_sales_amount_minor_units`, return, and void facts | Z-012B calculation engine | Committed Z / CLOSED period | `READY_EXISTING_DERIVATION` | None for bounded sources | Calculate `ACTIVE_GROSS+RETURN_AMOUNT+VOID_AMOUNT` exactly |
| D08 VATable sales | Z `vatable_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D09 VAT amount | Z `vat_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D10 VAT-exempt sales | Z `vat_exempt_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D11 Zero-rated sales | Z `zero_rated_sales_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D12 SC discount | Z `senior_citizen_discount_amount_minor_units`; discount child `senior_citizen` | Stored Z readback | Z / committed / classification | `READY_EXISTING_FIELD` | None | Copy and reconcile top-level to child |
| D13 PWD discount | Z `pwd_discount_amount_minor_units`; discount child `pwd` | Stored Z readback | Z / committed / classification | `READY_EXISTING_FIELD` | None | Copy and reconcile top-level to child |
| D14 NAAC discount | `pos.annex_e1_period_accounting_facts` type `naac_discount` | Z-012B zero-only fact projection | Annex E / exact scope+period | `READY_EXISTING_FIELD` | Nonzero path remains blocked | Require explicit `ATTESTED_ZERO`; never infer zero |
| D15 Solo Parent discount | `pos.annex_e1_period_accounting_facts` type `solo_parent_discount` | Z-012B zero-only fact projection | Annex E / exact scope+period | `READY_EXISTING_FIELD` | Nonzero path remains blocked | Require explicit `ATTESTED_ZERO`; never infer zero |
| D16 Other discount | Z/BIR other statutory, coupon, and promotional facts | Approved Annex E calculation profile | Committed Z / CLOSED period / classification | `READY_EXISTING_DERIVATION` | Nonzero AE-DR-012 privilege remains unsupported | Calculate the three approved named operands; reject unclassified or unresolved privilege facts |
| D17 Returns | BIR Summary `return_amount_minor_units` | Z-012B bounded source guard | Z / committed / period | `READY_EXISTING_FIELD` | Nonzero return attribution/sign unsupported | Copy governed zero; reject nonzero |
| D18 Voids | Z `void_amount_minor_units` | Stored Z readback | Z / committed / period | `READY_EXISTING_FIELD` | Same-period only | Copy governed same-period void total |
| D19 Total deductions | D12:D18 under approved profile | Z-012B calculation engine | Annex E / output / period | `READY_EXISTING_DERIVATION` | None for bounded sources | Calculate `D12+D13+D14+D15+D16+D17+D18` with zero tolerance |
| D20 SC VAT adjustment | `pos.fiscal_report_discount_breakdowns.vat_exemption_amount_minor_units` for SC | Stored Z child projection | Z / committed / classification | `READY_EXISTING_DERIVATION` | Ensure child is always present or governed zero | Copy immutable child amount |
| D21 PWD VAT adjustment | Discount child VAT exemption for PWD | Stored Z child projection | Z / committed / classification | `READY_EXISTING_DERIVATION` | Same as D20 | Copy immutable child amount |
| D22 Other VAT adjustment | `pos.annex_e1_period_accounting_facts` type `other_vat_adjustment` | Z-012B zero-only fact projection | Annex E / exact scope+period | `READY_EXISTING_FIELD` | AE-DR-012 blocks nonzero | Require explicit zero and reject nonzero |
| D23 VAT on returns | `pos.annex_e1_period_accounting_facts` type `vat_on_returns` | Z-012B zero-only fact projection | Annex E / exact scope+period | `READY_EXISTING_FIELD` | Nonzero return-tax contract unavailable | Require explicit zero and D17 zero; reject nonzero |
| D24 Other VAT adjustment | `pos.annex_e1_period_accounting_facts` type `residual_vat_adjustment` | Z-012B zero-only fact projection | Annex E / exact scope+period | `READY_EXISTING_FIELD` | Nonzero residual mapping unavailable | Require explicit zero and reject nonzero |
| D25 Total VAT adjustment | Sum D20:D24 | Z-012B calculation engine | Annex E / output / period | `READY_EXISTING_DERIVATION` | None for bounded zero-only components | Checked sum with zero tolerance |
| D26 VAT payable | Approved D09 and D22 operands | Z-012B calculation engine | Annex E / output / period | `READY_EXISTING_DERIVATION` | None for bounded zero-only D22 | Calculate `D09-D22`; never substitute D25 |
| D27 Net sales | Approved D07, D19, and D09 operands | Z-012B calculation engine | Annex E / output / period | `READY_EXISTING_DERIVATION` | None for bounded sources | Calculate `D07-D19-D09`; never copy VAT-inclusive Z net |
| D28 Sales overrun/overflow | `pos.annex_e1_period_accounting_facts.amount_minor_units` with type `sales_overrun_overflow_net_income` | Z-012B period-fact service | Annex E source / exact scope+period | `READY_EXISTING_FIELD` | Missing fact still blocks | Accept governed capacity-event `RECORDED` or explicit `ATTESTED_ZERO` |
| D29 Total income | Approved D27, D06, and D28 operands | Z-012B calculation engine | Annex E / output / period | `READY_EXISTING_DERIVATION` | None for bounded sources | Calculate `D27+D06+D28` with zero tolerance |
| D30 Reset counter | Z counter snapshot resulting reset; Z report reset | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D31 Z counter | Z counter snapshot resulting Z; Z report Z counter | Stored Z readback | Z / committed / scope+currency | `READY_EXISTING_FIELD` | None | Copy recorded value |
| D32 Remarks | `annex_e1_remarks` controlled code `none` or `no_activity` | Z-012B row projection | Annex E / output / period | `READY_EXISTING_FIELD` | Examiner acceptance remains external | Emit controlled values only; never allow free text |

## 4. Readiness totals

| Classification | Count |
| --- | ---: |
| `READY_EXISTING_FIELD` | 35 |
| `READY_EXISTING_DERIVATION` | 7 |
| `REQUIRES_Z_SNAPSHOT_EXTENSION` | 0 |
| `REQUIRES_NEW_REPORT_PROJECTION` | 0 |
| `REQUIRES_SCHEMA_CHANGE` | 0 |
| `REQUIRES_CONTROLLED_CODE` | 0 |
| `REQUIRES_EXTERNAL_DECISION` | 0 |
| `NOT_AVAILABLE` | 0 |
| `NOT_APPLICABLE` | 0 |

These are primary per-field readiness classifications and total 42. A field can also reference one or more broader decision gates without changing its primary classification.

## 5. Source-of-truth boundary

The Z-012B runtime consumes the committed Z snapshot, immutable children, immutable counter snapshot, committed BIR Sales Summary, and immutable Annex E first-class facts. It does not query live transaction tables to recalculate closed-period totals. Z-010's BIR Sales Summary is the financial authority where it carries the required fact. The Annex E projection validates and reshapes recorded facts but is not a second independently recomputed fiscal truth. Z-011A Electronic Journal events provide traceability and integrity evidence only; they are not the financial authority.

The readiness classifications above describe implementation availability. AE-DR-006 through AE-DR-009 are resolved by `Z-012B-ACCOUNTING-APPROVAL-001`. AE-DR-011A and AE-DR-019 retain Controlled UAT/Production gates; AE-DR-012 retains the nonzero privilege fail-closed gate.

The exact per-position Z-012A reconciliation, including null, zero, no-activity, unsupported behavior, and staged gate classification, is in [Z-012A Runtime Authorization Revalidation](ExitPass_POS_Server_Z012A_Annex_E1_Runtime_Authorization_Revalidation_v1.0.md).

## 6. Z-012A1 source map approved by Z-012A2

Accounting approved this map exactly under `Z-012B-ACCOUNTING-APPROVAL-001`. The primary readiness classifications above now reflect its implementation consequences.

| Position | Candidate source or derivation | Candidate source class | Required capability before runtime |
|---|---|---|---|
| D06 | Immutable period fact `manual_si_or_net_income_minor_units` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Governed Manual SI/OR fact and explicit zero attestation |
| D07 | BIR Summary gross plus governed return and void magnitudes | `AVAILABLE_CANONICAL_DERIVATION` | Implement exact approved formula |
| D09 | BIR Summary VAT amount | `AVAILABLE_CANONICAL_SOURCE` | Bind exact committed source |
| D12-D13 | BIR Summary SC and PWD discount amounts | `AVAILABLE_CANONICAL_SOURCE` | Bind exact committed source |
| D14-D15 | Separate immutable NAAC and Solo Parent facts | `REQUIRES_NEW_FIRST_CLASS_FACT` | Classification facts or explicit zero attestations |
| D16 | Other statutory plus coupon plus promotional discount | `AVAILABLE_CANONICAL_DERIVATION` | Reject nonzero unresolved privilege facts |
| D17-D18 | BIR Summary return and same-period void amounts | `AVAILABLE_CANONICAL_SOURCE` | Return remains zero-only under bounded contract |
| D19 | Sum D12 through D18 | `AVAILABLE_CANONICAL_DERIVATION` | Implement exact approved formula and authoritative operands |
| D20-D21 | Governing Z statutory discount-child VAT exemptions | `AVAILABLE_CANONICAL_DERIVATION` | Child-to-summary binding validation |
| D22-D24 | Explicitly attested zero under bounded unsupported paths | `KNOWN_ZERO_ONLY_WITH_ENFORCED_PRECONDITION` | First-class absence/zero evidence; nonzero blocks |
| D25 | Sum D20 through D24 | `AVAILABLE_CANONICAL_DERIVATION` | Authoritative component facts |
| D26 | D09 minus D22, preserving literal fields 8 and 19 | `AVAILABLE_CANONICAL_DERIVATION` | Implement exact approved formula |
| D27 | D07 minus D19 minus D09 | `AVAILABLE_CANONICAL_DERIVATION` | Implement exact approved formula; remains distinct from VAT-inclusive Z net |
| D28 | Immutable period fact `sales_overrun_overflow_net_income_minor_units` | `REQUIRES_NEW_FIRST_CLASS_FACT` | Governed overflow fact/event and explicit zero attestation |
| D29 | D27 plus D06 plus D28 | `AVAILABLE_CANONICAL_DERIVATION` | Implement exact approved formula and authoritative D06/D28 |

The complete definitions and source statuses are in the [Accounting Calculation Profile Proposal](ExitPass_POS_Server_Annex_E1_Accounting_Calculation_Profile_Proposal_v1.0.md). Missing first-class facts remain unknown, not zero.
