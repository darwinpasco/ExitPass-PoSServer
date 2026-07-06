namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class DigitalSalesInvoiceRenderService
{
    private static readonly string[] FooterPlaceholders =
    [
        "Digital Sales Invoice rendering foundation.",
        "Final statutory footer text and accredited template remain subject to compliance approval."
    ];

    private readonly FiscalDocumentReadService readService;

    public DigitalSalesInvoiceRenderService(FiscalDocumentReadService readService)
    {
        this.readService = readService ?? throw new ArgumentNullException(nameof(readService));
    }

    public async Task<DigitalSalesInvoiceRenderResult> RenderAsync(
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var readResult = await readService.GetByIdAsync(fiscalDocumentId, cancellationToken).ConfigureAwait(false);
        if (!readResult.Succeeded || readResult.Document is null)
        {
            return DigitalSalesInvoiceRenderResult.NotFound(fiscalDocumentId);
        }

        return DigitalSalesInvoiceRenderResult.Success(Map(readResult.Document));
    }

    private static DigitalSalesInvoiceRenderModel Map(FiscalDocumentReadModel document)
    {
        var numberingAssigned = HasCompleteFiscalNumbering(document);

        return new DigitalSalesInvoiceRenderModel(
            document.FiscalDocumentId,
            document.SitePosServerId,
            document.ChannelTerminalId,
            document.FiscalIdentityId,
            document.FiscalDocumentTypeCodeId,
            document.FiscalDocumentStatusCodeId,
            numberingAssigned ? "assigned" : "not_assigned",
            document.FiscalSequencePolicyId,
            document.FiscalSequenceValue,
            document.FiscalDocumentNumber,
            document.FiscalSeries,
            document.FiscalNumberPrefixText,
            document.FiscalNumberSuffixText,
            document.FiscalNumberAssignedAt,
            document.FiscalNumberAssignedByRef,
            document.BusinessDayDate,
            document.CentralPmsParkingSessionRef,
            document.CentralPmsPaymentAttemptRef,
            document.CentralPmsPaymentConfirmationRef,
            document.PaymentFinalityRef,
            document.VendorAckRef,
            document.SemanticRequestHash,
            document.SemanticRequestHashVersion,
            document.SemanticRequestHashStatus,
            document.CreatedAt,
            document.UpdatedAt,
            document.Lines.Select(MapLine).ToArray(),
            document.DiscountPrivilegeDetails.Select(MapDiscount).ToArray(),
            document.TaxDetails.Select(MapTaxDetail).ToArray(),
            document.Tenders.Select(MapTender).ToArray(),
            document.Totals.Select(MapTotal).ToArray(),
            new DigitalSalesInvoiceFooterRenderModel(
                "placeholder_only",
                FooterPlaceholders));
    }

    private static DigitalSalesInvoiceLineRenderModel MapLine(FiscalDocumentLineReadModel line) =>
        new(
            line.LineSequence,
            line.LineTypeCodeId,
            line.LineStatusCodeId,
            line.Description,
            line.Quantity,
            line.UnitAmountMinorUnits,
            line.GrossAmountMinorUnits,
            line.DiscountAmountMinorUnits,
            line.TaxAmountMinorUnits,
            line.NetAmountMinorUnits,
            line.CurrencyCode,
            line.SourceRef);

    private static DigitalSalesInvoiceDiscountRenderModel MapDiscount(FiscalDiscountPrivilegeDetailReadModel discount) =>
        new(
            discount.FiscalDocumentLineId,
            discount.DiscountPrivilegeTypeCodeId,
            discount.BasisAmountMinorUnits,
            discount.DiscountAmountMinorUnits,
            discount.VatPrivilegeAmountMinorUnits,
            discount.CurrencyCode,
            discount.BeneficiaryRef,
            discount.EvidenceRef,
            discount.ApprovalRef);

    private static DigitalSalesInvoiceTaxDetailRenderModel MapTaxDetail(FiscalTaxDetailReadModel taxDetail) =>
        new(
            taxDetail.FiscalDocumentLineId,
            taxDetail.TaxTypeCodeId,
            taxDetail.TaxClassificationCodeId,
            taxDetail.TaxRate,
            taxDetail.TaxableAmountMinorUnits,
            taxDetail.TaxAmountMinorUnits,
            taxDetail.CurrencyCode);

    private static DigitalSalesInvoiceTenderRenderModel MapTender(FiscalTenderReadModel tender) =>
        new(
            tender.TenderTypeCodeId,
            tender.AmountMinorUnits,
            tender.CurrencyCode,
            tender.CentralPmsPaymentAttemptRef,
            tender.CentralPmsPaymentConfirmationRef,
            tender.PaymentFinalityRef,
            tender.ProviderRef);

    private static DigitalSalesInvoiceTotalRenderModel MapTotal(FiscalTotalReadModel total) =>
        new(
            total.TotalTypeCodeId,
            total.AmountMinorUnits,
            total.CurrencyCode);

    private static bool HasCompleteFiscalNumbering(FiscalDocumentReadModel document) =>
        document.FiscalIdentityId is not null &&
        document.FiscalSequencePolicyId is not null &&
        document.FiscalSequenceValue is not null &&
        !string.IsNullOrWhiteSpace(document.FiscalDocumentNumber) &&
        !string.IsNullOrWhiteSpace(document.FiscalSeries) &&
        document.FiscalNumberAssignedAt is not null &&
        !string.IsNullOrWhiteSpace(document.FiscalNumberAssignedByRef);
}
