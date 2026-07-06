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
            Section(templateContract, "sellerSitePosIdentity", "Seller / Site / POS Identity", 20, SellerSiteRows(render, templateContract)),
            Section(templateContract, "documentIdentity", "Document Identity", 30, DocumentIdentityRows(render, templateContract)),
            Section(templateContract, "fiscalNumbering", "Fiscal Numbering", 40, FiscalNumberingRows(render, templateContract)),
            Section(templateContract, "parkingPaymentReferences", "Parking / Payment References", 50, ParkingPaymentRows(render, templateContract)),
            Section(templateContract, "lineItems", "Line Items", 60, LineRows(render, templateContract)),
            Section(templateContract, "discounts", "Discounts", 70, DiscountRows(render, templateContract)),
            Section(templateContract, "taxes", "Taxes", 80, TaxRows(render, templateContract)),
            Section(templateContract, "tenders", "Tenders", 90, TenderRows(render, templateContract)),
            Section(templateContract, "totals", "Totals", 100, TotalRows(render, templateContract)),
            Section(templateContract, "auditHashStatus", "Audit / Hash / Status", 110, AuditRows(render, templateContract)),
            Section(templateContract, "footerDisclaimers", "Footer / Disclaimers", 120, FooterRows(render, templateContract)),
            Section(templateContract, "deferredPlaceholders", "Deferred Placeholders", 130, DeferredRows(templateContract))
        };

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
            Row("documentIdentity.fiscalDocumentStatusCodeId", "Fiscal Document Status Code ID", "identifier", FieldPosture(contract, "documentIdentity.fiscalDocumentStatusCodeId"), render.FiscalDocumentStatusCodeId),
            Row("documentIdentity.businessDayDate", "Business Day", "dateTime", FieldPosture(contract, "documentIdentity.businessDayDate"), render.BusinessDayDate, FormatDate(render.BusinessDayDate))
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
            "documentIdentity" => "document_identity",
            "fiscalNumbering" => "fiscal_numbering",
            "parkingPaymentReferences" => "parking_payment_references",
            "lineItems" => "line_items",
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
