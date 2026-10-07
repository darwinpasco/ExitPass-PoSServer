using System.Security.Cryptography;
using System.Text;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class DigitalSalesInvoicePresentationEndpoint
{
    private const string PersistedOriginalPresentationVersion =
        "digital-sales-invoice-presentation-json-v2-persisted-original";
    private const string PersistedOriginalCanonicalTextAuthority =
        "persisted_original_electronic_journal";

    public static async Task<GetDigitalSalesInvoicePresentationResponse> GetByFiscalDocumentIdAsync(
        Guid fiscalDocumentId,
        DigitalSalesInvoiceRenderService renderService,
        DigitalSalesInvoicePresentationAdapter presentationAdapter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var renderResult = await renderService.RenderAsync(fiscalDocumentId, cancellationToken).ConfigureAwait(false);
            return MapResult(renderResult, presentationAdapter, fiscalDocumentId);
        }
        catch (FiscalDocumentPersistenceNotConfiguredException ex)
        {
            return new GetDigitalSalesInvoicePresentationResponse(
                false,
                "persistence_not_configured",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentInvalidPersistenceConfigurationException ex)
        {
            return new GetDigitalSalesInvoicePresentationResponse(
                false,
                "invalid_persistence_configuration",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentPersistenceException ex)
        {
            return new GetDigitalSalesInvoicePresentationResponse(
                false,
                "persistence_read_failed",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static GetDigitalSalesInvoicePresentationResponse MapResult(
        DigitalSalesInvoiceRenderResult renderResult,
        DigitalSalesInvoicePresentationAdapter presentationAdapter,
        Guid fiscalDocumentId)
    {
        ArgumentNullException.ThrowIfNull(renderResult);
        ArgumentNullException.ThrowIfNull(presentationAdapter);

        if (!renderResult.Succeeded)
        {
            return new GetDigitalSalesInvoicePresentationResponse(
                false,
                renderResult.ErrorCode == DigitalSalesInvoiceRenderErrorCode.NotFound
                    ? "fiscal_document_not_found"
                    : "digital_sales_invoice_presentation_failed",
                renderResult.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: renderResult.ErrorCode == DigitalSalesInvoiceRenderErrorCode.NotFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest);
        }

        var templateContract = DigitalSalesInvoiceTemplateContract.Create();
        var presentationResult = presentationAdapter.Adapt(renderResult, templateContract);
        if (!presentationResult.Succeeded || presentationResult.Presentation is null)
        {
            return new GetDigitalSalesInvoicePresentationResponse(
                false,
                presentationResult.ErrorCode == DigitalSalesInvoicePresentationErrorCode.NotFound
                    ? "fiscal_document_not_found"
                    : "digital_sales_invoice_presentation_failed",
                presentationResult.ErrorCode == DigitalSalesInvoicePresentationErrorCode.NotFound
                    ? $"Fiscal document '{fiscalDocumentId}' was not found."
                    : presentationResult.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: presentationResult.ErrorCode == DigitalSalesInvoicePresentationErrorCode.NotFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest);
        }

        var render = renderResult.Render;
        var canonicalText = RenderCanonicalText(render);
        var usesPersistedOriginal = render is not null &&
            IsCustomerSafePersistedOriginal(render.OriginalCanonicalText);

        return new GetDigitalSalesInvoicePresentationResponse(
            true,
            "presented",
            presentationResult.Message,
            templateContract,
            presentationResult.Presentation,
            FiscalNumberAssignmentState: render?.FiscalNumberAssignmentState,
            FiscalDocumentStatusCodeId: render?.FiscalDocumentStatusCodeId,
            FiscalDocumentStatus: render?.FiscalDocumentStatusCodeKey,
            FiscalDocumentTypeCodeId: render?.FiscalDocumentTypeCodeId,
            FiscalDocumentType: render?.FiscalDocumentTypeCodeKey,
            FiscalDocumentId: render?.FiscalDocumentId,
            FiscalDocumentNumber: render?.FiscalDocumentNumber,
            FiscalSeries: render?.FiscalSeries,
            FiscalNumberPrefixText: render?.FiscalNumberPrefixText,
            FiscalNumberSuffixText: render?.FiscalNumberSuffixText,
            FiscalNumberAssignedAt: render?.FiscalNumberAssignedAt,
            RecordedAt: render?.CreatedAt,
            VoidStatus: render?.VoidStatus,
            VoidReasonCode: render?.VoidReasonCode,
            VoidedAt: render?.VoidedAt,
            PresentationVersion: usesPersistedOriginal
                ? PersistedOriginalPresentationVersion
                : presentationResult.Presentation.PresentationVersion,
            TemplateVersion: templateContract.TemplateContractVersion,
            ContentType: templateContract.RenderFormat,
            CanonicalText: canonicalText,
            CanonicalTextAuthority: usesPersistedOriginal
                ? PersistedOriginalCanonicalTextAuthority
                : null,
            CanonicalTextHash: usesPersistedOriginal && canonicalText is not null
                ? ComputeCanonicalTextHash(canonicalText)
                : null,
            HttpStatusCode: StatusCodes.Status200OK);
    }

    private static string? RenderCanonicalText(DigitalSalesInvoiceRenderModel? render)
    {
        if (render is null) return null;
        if (IsCustomerSafePersistedOriginal(render.OriginalCanonicalText)) return render.OriginalCanonicalText;

        try
        {
            return new CanonicalSalesInvoiceTextRenderer().Render(render).Text;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool IsCustomerSafePersistedOriginal(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        !text.Contains("Completion Basis", StringComparison.OrdinalIgnoreCase) &&
        !text.Contains("Completion Authority", StringComparison.OrdinalIgnoreCase);

    private static string ComputeCanonicalTextHash(string canonicalText)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
