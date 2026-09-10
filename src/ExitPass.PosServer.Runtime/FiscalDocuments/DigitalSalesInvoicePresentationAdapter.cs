using System.Globalization;

namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class DigitalSalesInvoicePresentationAdapter
{
    public const string PresentationVersion = "digital-sales-invoice-presentation-json-v1";

    public DigitalSalesInvoicePresentationResult Adapt(
        DigitalSalesInvoiceRenderResult renderResult,
        DigitalSalesInvoiceTemplateContractModel templateContract)
    {
        ArgumentNullException.ThrowIfNull(renderResult);
        ArgumentNullException.ThrowIfNull(templateContract);

        if (!renderResult.Succeeded)
        {
            return renderResult.ErrorCode == DigitalSalesInvoiceRenderErrorCode.NotFound
                ? DigitalSalesInvoicePresentationResult.NotFound(Guid.Empty)
                : DigitalSalesInvoicePresentationResult.InvalidSource(renderResult.Message);
        }

        if (renderResult.Render is null)
        {
            return DigitalSalesInvoicePresentationResult.InvalidSource("Digital Sales Invoice render payload is required.");
        }

        var render = renderResult.Render;
        var sections = new List<DigitalSalesInvoicePresentationSectionModel>
        {
            Section(templateContract, "header", "Header", 10, HeaderRows(render, templateContract)),
            Section(templateContract, "salesInvoiceHeaderSnapshot", "Sales Invoice Header Snapshot", 20, SalesInvoiceHeaderSnapshotRows(render, templateContract)),
            Section(templateContract, "sellerSitePosIdentity", "Seller / Site / POS Identity", 30, SellerSiteRows(render, templateContract)),
            Section(templateContract, "documentIdentity", "Document Identity", 40, DocumentIdentityRows(render, templateContract)),
            Section(templateContract, "fiscalNumbering", "Fiscal Numbering", 50, FiscalNumberingRows(render, templateContract)),
            Section(templateContract, "parkingPaymentReferences", "Parking / Payment References", 60, ParkingPaymentRows(render, templateContract)),
            Section(templateContract, "customerInformation", "Customer Information", 65, CustomerInformationRows(render, templateContract)),
            Section(templateContract, "lineItems", "Line Items", 70, LineRows(render, templateContract)),
            Section(templateContract, "discounts", "Discounts", 80, DiscountRows(render, templateContract)),
            Section(templateContract, "taxes", "Taxes", 90, TaxRows(render, templateContract)),
            Section(templateContract, "vatBreakdown", "VAT Breakdown", 95, VatBreakdownRows(render, templateContract)),
            Section(templateContract, "tenders", "Tenders", 100, TenderRows(render, templateContract)),
            Section(templateContract, "totals", "Totals", 110, TotalRows(render, templateContract)),
            Section(templateContract, "auditHashStatus", "Audit / Hash / Status", 120, AuditRows(render, templateContract)),
            Section(templateContract, "footerDisclaimers", "Footer / Disclaimers", 130, FooterRows(render, templateContract)),
            Section(templateContract, "deferredPlaceholders", "Deferred Placeholders", 140, DeferredRows(templateContract))
        };

        if (render.AppliedStatutoryFiscalFacts is not null)
        {
            sections.Insert(
                9,
                Section(
                    templateContract,
                    "appliedStatutoryFiscalFacts",
                    "Applied Statutory Fiscal Facts",
                    85,
                    AppliedStatutoryFiscalFactsRows(render, templateContract)));
        }

        var notices = new List<DigitalSalesInvoicePresentationNoticeModel>();
        if (!string.Equals(render.FiscalNumberAssignmentState, "assigned", StringComparison.OrdinalIgnoreCase))
        {
            notices.Add(new DigitalSalesInvoicePresentationNoticeModel(
                "fiscal_number_not_assigned",
                "warning",
                "Fiscal number has not been assigned; presentation remains display-only and must not imply issued numbering."));
        }

        return DigitalSalesInvoicePresentationResult.Success(
            new DigitalSalesInvoicePresentationModel(
                PresentationVersion,
                templateContract.TemplateContractVersion,
                templateContract.FiscalTemplateFamily,
                templateContract.RenderFormat,
                render.FiscalNumberAssignmentState,
                "Digital Sales Invoice",
                sections,
                notices));
    }

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> HeaderRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        [
            Row("header.documentTitle", "Document Title", "text", "required", "Digital Sales Invoice"),
            Row("header.templateContractVersion", "Template Contract Version", "identifier", FieldPosture(contract, "header.templateContractVersion"), contract.TemplateContractVersion),
            Row("header.fiscalTemplateFamily", "Fiscal Template Family", "identifier", FieldPosture(contract, "header.fiscalTemplateFamily"), contract.FiscalTemplateFamily),
            Row("header.renderFormat", "Render Format", "text", FieldPosture(contract, "header.renderFormat"), contract.RenderFormat),
            Row("header.presentationVersion", "Presentation Version", "identifier", "required", PresentationVersion),
            Row("header.numberingState", "Numbering State", "status", "required", render.FiscalNumberAssignmentState)
        ];

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> SalesInvoiceHeaderSnapshotRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract)
    {
        var snapshot = render.SalesInvoiceHeaderSnapshot;
        return
        [
            Row("salesInvoiceHeaderSnapshot.fiscalIdentityId", "Fiscal Identity ID", "identifier", "optional", snapshot?.FiscalIdentityId),
            Row("salesInvoiceHeaderSnapshot.salesInvoiceHeaderProfileId", "Sales Invoice Header Profile ID", "identifier", "optional", snapshot?.SalesInvoiceHeaderProfileId),
            Row("salesInvoiceHeaderSnapshot.profileVersion", "Profile Version", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.profileVersion"), snapshot?.ProfileVersion),
            Row("salesInvoiceHeaderSnapshot.registeredBusinessName", "Registered Business Name", "text", FieldPosture(contract, "salesInvoiceHeaderSnapshot.registeredBusinessName"), snapshot?.RegisteredBusinessName),
            Row("salesInvoiceHeaderSnapshot.registeredBusinessAddress", "Registered Business Address", "text", FieldPosture(contract, "salesInvoiceHeaderSnapshot.registeredBusinessAddress"), snapshot?.RegisteredBusinessAddress),
            Row("salesInvoiceHeaderSnapshot.tin", "TIN", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.tin"), snapshot?.Tin),
            Row("salesInvoiceHeaderSnapshot.posSerialNumber", "POS Serial Number", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.posSerialNumber"), snapshot?.PosSerialNumber),
            Row("salesInvoiceHeaderSnapshot.machineIdentificationNumber", "MIN", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.machineIdentificationNumber"), snapshot?.MachineIdentificationNumber),
            Row("salesInvoiceHeaderSnapshot.parkingLocationDisplay", "Parking Location", "text", FieldPosture(contract, "salesInvoiceHeaderSnapshot.parkingLocationDisplay"), snapshot?.ParkingLocationDisplay),
            Row("salesInvoiceHeaderSnapshot.terminalId", "Terminal ID", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.terminalId"), snapshot?.TerminalId),
            Row("salesInvoiceHeaderSnapshot.supplierDeveloperRegisteredName", "Supplier / Developer Registered Name", "text", FieldPosture(contract, "salesInvoiceHeaderSnapshot.supplierDeveloperRegisteredName"), snapshot?.SupplierDeveloperRegisteredName),
            Row("salesInvoiceHeaderSnapshot.supplierDeveloperAddress", "Supplier / Developer Address", "text", FieldPosture(contract, "salesInvoiceHeaderSnapshot.supplierDeveloperAddress"), snapshot?.SupplierDeveloperAddress),
            Row("salesInvoiceHeaderSnapshot.supplierDeveloperTin", "Supplier / Developer TIN", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.supplierDeveloperTin"), snapshot?.SupplierDeveloperTin),
            Row("salesInvoiceHeaderSnapshot.birAccreditationNumber", "BIR Accreditation Number", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.birAccreditationNumber"), snapshot?.BirAccreditationNumber),
            Row("salesInvoiceHeaderSnapshot.birAccreditationIssuedDate", "BIR Accreditation Issued Date", "dateTime", FieldPosture(contract, "salesInvoiceHeaderSnapshot.birAccreditationIssuedDate"), snapshot?.BirAccreditationIssuedDate, FormatDate(snapshot?.BirAccreditationIssuedDate)),
            Row("salesInvoiceHeaderSnapshot.birAccreditationValidUntil", "BIR Accreditation Valid Until", "dateTime", FieldPosture(contract, "salesInvoiceHeaderSnapshot.birAccreditationValidUntil"), snapshot?.BirAccreditationValidUntil, FormatDate(snapshot?.BirAccreditationValidUntil)),
            Row("salesInvoiceHeaderSnapshot.ptuNumber", "PTU Number", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.ptuNumber"), snapshot?.PtuNumber),
            Row("salesInvoiceHeaderSnapshot.ptuIssuedDate", "PTU Issued Date", "dateTime", FieldPosture(contract, "salesInvoiceHeaderSnapshot.ptuIssuedDate"), snapshot?.PtuIssuedDate, FormatDate(snapshot?.PtuIssuedDate)),
            Row("salesInvoiceHeaderSnapshot.salesInvoiceLegalStatement", "Sales Invoice Legal Statement", "text", FieldPosture(contract, "salesInvoiceHeaderSnapshot.salesInvoiceLegalStatement"), snapshot?.SalesInvoiceLegalStatement),
            Row("salesInvoiceHeaderSnapshot.customerServiceFooter", "Customer Service Footer", "text", FieldPosture(contract, "salesInvoiceHeaderSnapshot.customerServiceFooter"), snapshot?.CustomerServiceFooter),
            Row("salesInvoiceHeaderSnapshot.templateVersion", "Template Version", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.templateVersion"), snapshot?.TemplateVersion),
            Row("salesInvoiceHeaderSnapshot.presentationVersion", "Presentation Version", "identifier", FieldPosture(contract, "salesInvoiceHeaderSnapshot.presentationVersion"), snapshot?.PresentationVersion),
            Row("salesInvoiceHeaderSnapshot.effectiveAt", "Effective At", "dateTime", "optional", snapshot?.EffectiveAt, snapshot is null ? null : FormatTimestamp(snapshot.EffectiveAt)),
            Row("salesInvoiceHeaderSnapshot.snapshotCreatedAt", "Snapshot Created At", "dateTime", "optional", snapshot?.SnapshotCreatedAt, snapshot is null ? null : FormatTimestamp(snapshot.SnapshotCreatedAt))
        ];
    }

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> SellerSiteRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        [
            Row("sellerSitePosIdentity.sitePosServerId", "Site POS Server ID", "identifier", FieldPosture(contract, "sellerSitePosIdentity.sitePosServerId"), render.SitePosServerId),
            Row("sellerSitePosIdentity.channelTerminalId", "Channel Terminal ID", "identifier", FieldPosture(contract, "sellerSitePosIdentity.channelTerminalId"), render.ChannelTerminalId),
            Row("sellerSitePosIdentity.fiscalIdentityId", "Fiscal Identity ID", "identifier", FieldPosture(contract, "sellerSitePosIdentity.fiscalIdentityId"), render.FiscalIdentityId)
        ];

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> DocumentIdentityRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        [
            Row("documentIdentity.fiscalDocumentId", "Fiscal Document ID", "identifier", FieldPosture(contract, "documentIdentity.fiscalDocumentId"), render.FiscalDocumentId),
            Row("documentIdentity.fiscalDocumentTypeCodeId", "Fiscal Document Type Code ID", "identifier", FieldPosture(contract, "documentIdentity.fiscalDocumentTypeCodeId"), render.FiscalDocumentTypeCodeId),
            Row("documentIdentity.fiscalDocumentTypeCodeKey", "Fiscal Document Type", "status", FieldPosture(contract, "documentIdentity.fiscalDocumentTypeCodeKey"), render.FiscalDocumentTypeCodeKey),
            Row("documentIdentity.fiscalDocumentStatusCodeId", "Fiscal Document Status Code ID", "identifier", FieldPosture(contract, "documentIdentity.fiscalDocumentStatusCodeId"), render.FiscalDocumentStatusCodeId),
            Row("documentIdentity.fiscalDocumentStatusCodeKey", "Fiscal Document Status", "status", FieldPosture(contract, "documentIdentity.fiscalDocumentStatusCodeKey"), render.FiscalDocumentStatusCodeKey),
            Row("documentIdentity.businessDayDate", "Business Day", "dateTime", FieldPosture(contract, "documentIdentity.businessDayDate"), render.BusinessDayDate, FormatDate(render.BusinessDayDate)),
            Row("documentIdentity.recordedAt", "Recorded At", "dateTime", FieldPosture(contract, "documentIdentity.recordedAt"), render.CreatedAt, FormatTimestamp(render.CreatedAt)),
            Row("documentIdentity.voidStatus", "Void Status", "status", FieldPosture(contract, "documentIdentity.voidStatus"), render.VoidStatus),
            Row("documentIdentity.voidReasonCode", "Void Reason", "status", FieldPosture(contract, "documentIdentity.voidReasonCode"), render.VoidReasonCode),
            Row("documentIdentity.voidedAt", "Voided At", "dateTime", FieldPosture(contract, "documentIdentity.voidedAt"), render.VoidedAt, FormatTimestamp(render.VoidedAt))
        ];

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> FiscalNumberingRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        [
            Row("fiscalNumbering.fiscalNumberAssignmentState", "Fiscal Number Assignment State", "status", FieldPosture(contract, "fiscalNumbering.fiscalNumberAssignmentState"), render.FiscalNumberAssignmentState),
            Row("fiscalNumbering.fiscalSequencePolicyId", "Fiscal Sequence Policy ID", "identifier", "optional", render.FiscalSequencePolicyId),
            Row("fiscalNumbering.fiscalSequenceValue", "Fiscal Sequence Value", "identifier", "optional", render.FiscalSequenceValue),
            Row("fiscalNumbering.fiscalDocumentNumber", "Fiscal Document Number", "identifier", FieldPosture(contract, "fiscalNumbering.fiscalDocumentNumber"), render.FiscalDocumentNumber),
            Row("fiscalNumbering.fiscalSeries", "Fiscal Series", "text", FieldPosture(contract, "fiscalNumbering.fiscalSeries"), render.FiscalSeries),
            Row("fiscalNumbering.fiscalNumberPrefixText", "Fiscal Number Prefix", "text", FieldPosture(contract, "fiscalNumbering.fiscalNumberPrefixText"), render.FiscalNumberPrefixText),
            Row("fiscalNumbering.fiscalNumberSuffixText", "Fiscal Number Suffix", "text", FieldPosture(contract, "fiscalNumbering.fiscalNumberSuffixText"), render.FiscalNumberSuffixText),
            Row("fiscalNumbering.fiscalNumberAssignedAt", "Fiscal Number Assigned At", "dateTime", FieldPosture(contract, "fiscalNumbering.fiscalNumberAssignedAt"), render.FiscalNumberAssignedAt, FormatTimestamp(render.FiscalNumberAssignedAt)),
            Row("fiscalNumbering.fiscalNumberAssignedByRef", "Fiscal Number Assigned By", "identifier", FieldPosture(contract, "fiscalNumbering.fiscalNumberAssignedByRef"), render.FiscalNumberAssignedByRef)
        ];

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> ParkingPaymentRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        [
            Row("parkingPaymentReferences.centralPmsParkingSessionRef", "Central PMS Parking Session Ref", "identifier", FieldPosture(contract, "parkingPaymentReferences.centralPmsParkingSessionRef"), render.CentralPmsParkingSessionRef),
            Row("parkingPaymentReferences.centralPmsPaymentAttemptRef", "Central PMS Payment Attempt Ref", "identifier", FieldPosture(contract, "parkingPaymentReferences.centralPmsPaymentAttemptRef"), render.CentralPmsPaymentAttemptRef),
            Row("parkingPaymentReferences.centralPmsPaymentConfirmationRef", "Central PMS Payment Confirmation Ref", "identifier", FieldPosture(contract, "parkingPaymentReferences.centralPmsPaymentConfirmationRef"), render.CentralPmsPaymentConfirmationRef),
            Row("parkingPaymentReferences.paymentFinalityRef", "Payment Finality Ref", "identifier", FieldPosture(contract, "parkingPaymentReferences.paymentFinalityRef"), render.PaymentFinalityRef),
            Row("parkingPaymentReferences.vendorAckRef", "Vendor Acknowledgement Ref", "identifier", FieldPosture(contract, "parkingPaymentReferences.vendorAckRef"), render.VendorAckRef)
        ];

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> CustomerInformationRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract)
    {
        var customer = render.InvoiceCustomerInformation;
        return
        [
            Row("customerInformation.customerName", "Customer Name", "text", FieldPosture(contract, "customerInformation.customerName"), customer?.CustomerName),
            Row("customerInformation.address", "Address", "text", FieldPosture(contract, "customerInformation.address"), customer?.Address),
            Row("customerInformation.tin", "TIN", "identifier", FieldPosture(contract, "customerInformation.tin"), customer?.Tin),
            Row("customerInformation.businessStyle", "Business Style", "text", FieldPosture(contract, "customerInformation.businessStyle"), customer?.BusinessStyle),
            Row("customerInformation.statutoryIdNumber", "OSCA ID No. / PWD ID No.", "identifier", FieldPosture(contract, "customerInformation.statutoryIdNumber"), render.AppliedStatutoryFiscalFacts is null ? null : customer?.StatutoryIdNumber)
        ];
    }

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> VatBreakdownRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract)
    {
        var currency = render.TaxDetails.Select(tax => tax.CurrencyCode)
            .Concat(render.Totals.Select(total => total.CurrencyCode))
            .FirstOrDefault() ?? "PHP";
        var vatableSales = SumTaxableAmount(render.TaxDetails, "vatable");
        var vatExemptSales = SumTaxableAmount(render.TaxDetails, "vat_exempt");
        var zeroRatedSales = SumTaxableAmount(render.TaxDetails, "zero_rated");
        var vatAmount = render.TaxDetails
            .Where(tax => tax.TaxClassificationCodeKey is "vatable" or "vat_exempt" or "zero_rated")
            .Sum(tax => tax.TaxAmountMinorUnits);

        return
        [
            AmountRow("totals.vatableSales", "VATable Sales", FieldPosture(contract, "totals.vatableSales"), vatableSales, currency),
            AmountRow("totals.vatAmount", "VAT Amount", FieldPosture(contract, "totals.vatAmount"), vatAmount, currency),
            AmountRow("totals.vatExemptSales", "VAT Exempt Sales", FieldPosture(contract, "totals.vatExemptSales"), vatExemptSales, currency),
            AmountRow("totals.zeroRatedSales", "Zero Rated Sales", FieldPosture(contract, "totals.zeroRatedSales"), zeroRatedSales, currency)
        ];
    }

    private static long SumTaxableAmount(
        IEnumerable<DigitalSalesInvoiceTaxDetailRenderModel> taxDetails,
        string classification) =>
        taxDetails
            .Where(tax => string.Equals(tax.TaxClassificationCodeKey, classification, StringComparison.Ordinal))
            .Sum(tax => tax.TaxableAmountMinorUnits);

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> LineRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        render.Lines
            .OrderBy(line => line.LineSequence)
            .SelectMany((line, index) =>
            {
                var key = $"lineItems[{index:D4}]";
                return new[]
                {
                    Row($"{key}.lineSequence", "Line Sequence", "identifier", FieldPosture(contract, "lineItems"), line.LineSequence),
                    Row($"{key}.description", "Description", "text", FieldPosture(contract, "lineItems"), line.Description),
                    Row($"{key}.quantity", "Quantity", "amount", FieldPosture(contract, "lineItems"), line.Quantity, line.Quantity.ToString("0.####", CultureInfo.InvariantCulture)),
                    AmountRow($"{key}.unitAmount", "Unit Amount", FieldPosture(contract, "lineItems"), line.UnitAmountMinorUnits, line.CurrencyCode),
                    AmountRow($"{key}.grossAmount", "Gross Amount", FieldPosture(contract, "lineItems"), line.GrossAmountMinorUnits, line.CurrencyCode),
                    AmountRow($"{key}.discountAmount", "Discount Amount", FieldPosture(contract, "lineItems"), line.DiscountAmountMinorUnits, line.CurrencyCode),
                    AmountRow($"{key}.taxAmount", "Tax Amount", FieldPosture(contract, "lineItems"), line.TaxAmountMinorUnits, line.CurrencyCode),
                    AmountRow($"{key}.netAmount", "Net Amount", FieldPosture(contract, "lineItems"), line.NetAmountMinorUnits, line.CurrencyCode),
                    Row($"{key}.sourceRef", "Source Ref", "identifier", "optional", line.SourceRef)
                };
            })
            .ToArray();

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> DiscountRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        render.Discounts
            .OrderBy(discount => discount.FiscalDocumentLineId)
            .ThenBy(discount => discount.DiscountPrivilegeTypeCodeId)
            .SelectMany((discount, index) =>
            {
                var key = $"discounts[{index:D4}]";
                return new[]
                {
                    Row($"{key}.fiscalDocumentLineId", "Fiscal Document Line ID", "identifier", FieldPosture(contract, "discounts"), discount.FiscalDocumentLineId),
                    Row($"{key}.discountPrivilegeTypeCodeId", "Discount Privilege Type Code ID", "identifier", FieldPosture(contract, "discounts"), discount.DiscountPrivilegeTypeCodeId),
                    AmountRow($"{key}.basisAmount", "Basis Amount", FieldPosture(contract, "discounts"), discount.BasisAmountMinorUnits, discount.CurrencyCode),
                    AmountRow($"{key}.discountAmount", "Discount Amount", FieldPosture(contract, "discounts"), discount.DiscountAmountMinorUnits, discount.CurrencyCode),
                    AmountRow($"{key}.vatPrivilegeAmount", "VAT Privilege Amount", FieldPosture(contract, "discounts"), discount.VatPrivilegeAmountMinorUnits, discount.CurrencyCode),
                    Row($"{key}.beneficiaryRef", "Beneficiary Ref", "identifier", FieldPosture(contract, "discounts"), discount.BeneficiaryRef),
                    Row($"{key}.evidenceRef", "Evidence Ref", "identifier", FieldPosture(contract, "discounts"), discount.EvidenceRef),
                    Row($"{key}.approvalRef", "Approval Ref", "identifier", FieldPosture(contract, "discounts"), discount.ApprovalRef)
                };
            })
            .ToArray();

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> TaxRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        render.TaxDetails
            .OrderBy(tax => tax.FiscalDocumentLineId)
            .ThenBy(tax => tax.TaxTypeCodeId)
            .ThenBy(tax => tax.TaxClassificationCodeId)
            .SelectMany((tax, index) =>
            {
                var key = $"taxes[{index:D4}]";
                return new[]
                {
                    Row($"{key}.fiscalDocumentLineId", "Fiscal Document Line ID", "identifier", FieldPosture(contract, "taxes"), tax.FiscalDocumentLineId),
                    Row($"{key}.taxTypeCodeId", "Tax Type Code ID", "identifier", FieldPosture(contract, "taxes"), tax.TaxTypeCodeId),
                    Row($"{key}.taxClassificationCodeId", "Tax Classification Code ID", "identifier", FieldPosture(contract, "taxes"), tax.TaxClassificationCodeId),
                    Row($"{key}.taxRate", "Tax Rate", "amount", FieldPosture(contract, "taxes"), tax.TaxRate, tax.TaxRate?.ToString("0.####", CultureInfo.InvariantCulture)),
                    AmountRow($"{key}.taxableAmount", "Taxable Amount", FieldPosture(contract, "taxes"), tax.TaxableAmountMinorUnits, tax.CurrencyCode),
                    AmountRow($"{key}.taxAmount", "Tax Amount", FieldPosture(contract, "taxes"), tax.TaxAmountMinorUnits, tax.CurrencyCode)
                };
            })
            .ToArray();

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> AppliedStatutoryFiscalFactsRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract)
    {
        var facts = render.AppliedStatutoryFiscalFacts;
        var policy = facts?.PolicyReference;

        return
        [
            Row("appliedStatutoryFiscalFacts.entitlementType", "Entitlement Type", "status", FieldPosture(contract, "appliedStatutoryFiscalFacts.entitlementType"), facts?.EntitlementType),
            Row("appliedStatutoryFiscalFacts.benefitClassification", "Benefit Classification", "status", FieldPosture(contract, "appliedStatutoryFiscalFacts.benefitClassification"), facts?.BenefitClassification),
            Row("appliedStatutoryFiscalFacts.policyResolutionBasis", "Policy Resolution Basis", "status", FieldPosture(contract, "appliedStatutoryFiscalFacts.policyResolutionBasis"), policy?.ResolutionBasis),
            Row("appliedStatutoryFiscalFacts.policyCode", "Policy Code", "identifier", FieldPosture(contract, "appliedStatutoryFiscalFacts.policyCode"), policy?.PolicyCode),
            Row("appliedStatutoryFiscalFacts.nationalLawReference", "National Law Reference", "identifier", FieldPosture(contract, "appliedStatutoryFiscalFacts.nationalLawReference"), policy?.NationalLawReference),
            Row("appliedStatutoryFiscalFacts.ordinanceReference", "Ordinance Reference", "identifier", FieldPosture(contract, "appliedStatutoryFiscalFacts.ordinanceReference"), policy?.OrdinanceReference),
            facts is null ? AmountRow("appliedStatutoryFiscalFacts.originalAmount", "Original Amount", "not_available", 0, "PHP") with { DisplayValue = null, RawValue = null } : AmountRow("appliedStatutoryFiscalFacts.originalAmount", "Original Amount", FieldPosture(contract, "appliedStatutoryFiscalFacts.originalAmount"), facts.OriginalAmountMinorUnits, facts.Currency),
            facts is null ? AmountRow("appliedStatutoryFiscalFacts.vatExclusiveBasisAmount", "VAT-Exclusive Basis", "not_available", 0, "PHP") with { DisplayValue = null, RawValue = null } : AmountRow("appliedStatutoryFiscalFacts.vatExclusiveBasisAmount", "VAT-Exclusive Basis", FieldPosture(contract, "appliedStatutoryFiscalFacts.vatExclusiveBasisAmount"), facts.VatExclusiveBasisAmountMinorUnits, facts.Currency),
            facts is null ? AmountRow("appliedStatutoryFiscalFacts.vatAmount", "VAT Amount", "not_available", 0, "PHP") with { DisplayValue = null, RawValue = null } : AmountRow("appliedStatutoryFiscalFacts.vatAmount", "VAT Amount", FieldPosture(contract, "appliedStatutoryFiscalFacts.vatAmount"), facts.VatAmountMinorUnits, facts.Currency),
            Row("appliedStatutoryFiscalFacts.vatTreatment", "VAT Treatment", "status", FieldPosture(contract, "appliedStatutoryFiscalFacts.vatTreatment"), facts?.VatTreatment),
            facts is null ? AmountRow("appliedStatutoryFiscalFacts.statutoryDiscountAmount", "Statutory Discount Amount", "not_available", 0, "PHP") with { DisplayValue = null, RawValue = null } : AmountRow("appliedStatutoryFiscalFacts.statutoryDiscountAmount", "Statutory Discount Amount", FieldPosture(contract, "appliedStatutoryFiscalFacts.statutoryDiscountAmount"), facts.StatutoryDiscountAmountMinorUnits, facts.Currency),
            facts is null ? AmountRow("appliedStatutoryFiscalFacts.finalPayableAmount", "Final Payable Amount", "not_available", 0, "PHP") with { DisplayValue = null, RawValue = null } : AmountRow("appliedStatutoryFiscalFacts.finalPayableAmount", "Final Payable Amount", FieldPosture(contract, "appliedStatutoryFiscalFacts.finalPayableAmount"), facts.FinalPayableAmountMinorUnits, facts.Currency),
            Row("appliedStatutoryFiscalFacts.sourcePaymentChannel", "Source Payment Channel", "status", FieldPosture(contract, "appliedStatutoryFiscalFacts.sourcePaymentChannel"), facts?.SourcePaymentChannel),
            Row("appliedStatutoryFiscalFacts.appliedAt", "Applied At", "dateTime", FieldPosture(contract, "appliedStatutoryFiscalFacts.appliedAt"), facts?.AppliedAt, FormatTimestamp(facts?.AppliedAt))
        ];
    }

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> TenderRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        render.Tenders
            .OrderBy(tender => tender.PaymentFinalityRef)
            .ThenBy(tender => tender.ProviderRef)
            .ThenBy(tender => tender.TenderTypeCodeId)
            .SelectMany((tender, index) =>
            {
                var key = $"tenders[{index:D4}]";
                return new[]
                {
                    Row($"{key}.tenderTypeCodeId", "Tender Type Code ID", "identifier", FieldPosture(contract, "tenders"), tender.TenderTypeCodeId),
                    Row($"{key}.tenderTypeCodeKey", "Tender Type", "status", FieldPosture(contract, "tenders.tenderTypeCodeKey"), tender.TenderTypeCodeKey),
                    AmountRow($"{key}.amount", "Tender Amount", FieldPosture(contract, "tenders"), tender.AmountMinorUnits, tender.CurrencyCode),
                    Row($"{key}.centralPmsPaymentAttemptRef", "Central PMS Payment Attempt Ref", "identifier", FieldPosture(contract, "tenders"), tender.CentralPmsPaymentAttemptRef),
                    Row($"{key}.centralPmsPaymentConfirmationRef", "Central PMS Payment Confirmation Ref", "identifier", FieldPosture(contract, "tenders"), tender.CentralPmsPaymentConfirmationRef),
                    Row($"{key}.paymentFinalityRef", "Payment Finality Ref", "identifier", FieldPosture(contract, "tenders"), tender.PaymentFinalityRef),
                    Row($"{key}.providerRef", "Provider Ref", "identifier", FieldPosture(contract, "tenders"), tender.ProviderRef)
                };
            })
            .ToArray();

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> TotalRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        render.Totals
            .OrderBy(total => total.TotalTypeCodeId)
            .SelectMany((total, index) =>
            {
                var key = $"totals[{index:D4}]";
                return new[]
                {
                    Row($"{key}.totalTypeCodeId", "Total Type Code ID", "identifier", FieldPosture(contract, "totals"), total.TotalTypeCodeId),
                    AmountRow($"{key}.amount", "Total Amount", FieldPosture(contract, "totals"), total.AmountMinorUnits, total.CurrencyCode)
                };
            })
            .ToArray();

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> AuditRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        [
            Row("auditHashStatus.semanticRequestHash", "Semantic Request Hash", "identifier", FieldPosture(contract, "auditHashStatus.semanticRequestHash"), render.SemanticRequestHash),
            Row("auditHashStatus.semanticRequestHashVersion", "Semantic Request Hash Version", "identifier", FieldPosture(contract, "auditHashStatus.semanticRequestHashVersion"), render.SemanticRequestHashVersion),
            Row("auditHashStatus.semanticRequestHashStatus", "Semantic Request Hash Status", "status", FieldPosture(contract, "auditHashStatus.semanticRequestHashStatus"), render.SemanticRequestHashStatus),
            Row("auditHashStatus.createdAt", "Created At", "dateTime", "required", render.CreatedAt, FormatTimestamp(render.CreatedAt)),
            Row("auditHashStatus.updatedAt", "Updated At", "dateTime", "required", render.UpdatedAt, FormatTimestamp(render.UpdatedAt))
        ];

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> FooterRows(
        DigitalSalesInvoiceRenderModel render,
        DigitalSalesInvoiceTemplateContractModel contract) =>
        new[]
        {
            Row("footerDisclaimers.renderingStatus", "Rendering Status", "status", "placeholder", render.Footer.RenderingStatus)
        }
        .Concat(render.Footer.DisclaimerPlaceholders.Select((placeholder, index) =>
            Row($"footerDisclaimers.placeholders[{index:D4}]", "Footer Placeholder", "placeholder", FieldPosture(contract, "footerDisclaimers"), placeholder)))
        .ToArray();

    private static IReadOnlyList<DigitalSalesInvoicePresentationRowModel> DeferredRows(
        DigitalSalesInvoiceTemplateContractModel contract) =>
        contract.DeferredPlaceholders
            .Select(placeholder =>
                Row(
                    $"deferredPlaceholders.{placeholder.Name}",
                    ToLabel(placeholder.Name),
                    "deferred",
                    placeholder.Posture,
                    placeholder.Description))
            .ToArray();

    private static DigitalSalesInvoicePresentationSectionModel Section(
        DigitalSalesInvoiceTemplateContractModel contract,
        string name,
        string label,
        int sortOrder,
        IReadOnlyList<DigitalSalesInvoicePresentationRowModel> rows) =>
        new(
            name,
            label,
            sortOrder,
            SectionPosture(contract, name),
            rows);

    private static DigitalSalesInvoicePresentationRowModel AmountRow(
        string key,
        string label,
        string posture,
        long amountMinorUnits,
        string currencyCode) =>
        Row(key, label, "amount", posture, amountMinorUnits, FormatAmount(amountMinorUnits, currencyCode));

    private static DigitalSalesInvoicePresentationRowModel Row(
        string key,
        string label,
        string valueKind,
        string posture,
        object? rawValue,
        string? displayValue = null)
    {
        var resolvedPosture = rawValue is null && posture is not "placeholder" and not "deferred"
            ? "not_available"
            : posture;

        return new DigitalSalesInvoicePresentationRowModel(
            key,
            label,
            valueKind,
            resolvedPosture,
            displayValue ?? FormatRaw(rawValue),
            rawValue);
    }

    private static string SectionPosture(DigitalSalesInvoiceTemplateContractModel contract, string presentationSectionName)
    {
        var contractSectionName = ToContractSectionName(presentationSectionName);
        return contract.Sections.FirstOrDefault(section => section.Name == contractSectionName)?.Posture ?? "not_available";
    }

    private static string FieldPosture(DigitalSalesInvoiceTemplateContractModel contract, string fieldPath) =>
        contract.Fields.FirstOrDefault(field => field.Path == fieldPath)?.Posture ?? "not_available";

    private static string ToContractSectionName(string presentationSectionName) =>
        presentationSectionName switch
        {
            "sellerSitePosIdentity" => "seller_site_pos_identity",
            "salesInvoiceHeaderSnapshot" => "sales_invoice_header_snapshot",
            "documentIdentity" => "document_identity",
            "fiscalNumbering" => "fiscal_numbering",
            "parkingPaymentReferences" => "parking_payment_references",
            "customerInformation" => "customer_information",
            "lineItems" => "line_items",
            "appliedStatutoryFiscalFacts" => "applied_statutory_fiscal_facts",
            "vatBreakdown" => "vat_breakdown",
            "auditHashStatus" => "audit_hash_status",
            "footerDisclaimers" => "footer_disclaimers",
            "deferredPlaceholders" => "deferred_placeholders",
            _ => presentationSectionName
        };

    private static string? FormatRaw(object? value) =>
        value switch
        {
            null => null,
            DateTimeOffset timestamp => FormatTimestamp(timestamp),
            DateOnly date => FormatDate(date),
            Guid id => id.ToString("D"),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

    private static string FormatTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string? FormatTimestamp(DateTimeOffset? timestamp) =>
        timestamp is null ? null : FormatTimestamp(timestamp.Value);

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? FormatDate(DateOnly? date) =>
        date is null ? null : FormatDate(date.Value);

    private static string FormatAmount(long amountMinorUnits, string currencyCode) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1:0.00}",
            currencyCode,
            amountMinorUnits / 100m);

    private static string ToLabel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var chars = new List<char> { char.ToUpperInvariant(name[0]) };
        foreach (var character in name.Skip(1))
        {
            if (char.IsUpper(character))
            {
                chars.Add(' ');
            }

            chars.Add(character);
        }

        return new string(chars.ToArray());
    }
}
