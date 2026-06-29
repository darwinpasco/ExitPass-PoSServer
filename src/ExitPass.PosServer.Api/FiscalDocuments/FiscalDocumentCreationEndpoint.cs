using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentCreationEndpoint
{
    public static async Task<CreateFiscalDocumentResponse> CreateAsync(
        CreateFiscalDocumentRequest request,
        FiscalDocumentCreationService service,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await service.CreateAsync(MapToCommand(request), cancellationToken).ConfigureAwait(false);
            return MapResult(result);
        }
        catch (FiscalDocumentPersistenceNotConfiguredException ex)
        {
            return new CreateFiscalDocumentResponse(
                false,
                "persistence_not_configured",
                ex.Message,
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static FiscalDocumentCreationCommand MapToCommand(CreateFiscalDocumentRequest request) =>
        new(
            request.SitePosServerRef ?? string.Empty,
            request.FiscalDocumentTypeCodeKey ?? string.Empty,
            MapPayableBasis(request.PayableBasis),
            request.CentralPmsPaymentAttemptRef,
            request.CentralPmsPaymentConfirmationRef,
            request.PaymentFinalityRef,
            request.VendorAckRef,
            request.ReferenceContext);

    public static CreateFiscalDocumentResponse MapResult(FiscalDocumentCreationResult result)
    {
        if (result.Succeeded)
        {
            return new CreateFiscalDocumentResponse(
                true,
                "accepted",
                result.Message,
                result.Draft?.FiscalDocumentId,
                StatusCodes.Status202Accepted);
        }

        return new CreateFiscalDocumentResponse(
            false,
            ToResponseCode(result.ErrorCode),
            result.Message,
            HttpStatusCode: StatusCodes.Status400BadRequest);
    }

    private static FiscalizationPayableBasisInput? MapPayableBasis(FiscalizationPayableBasisRequest? request)
    {
        if (request is null)
        {
            return null;
        }

        return new FiscalizationPayableBasisInput(
            request.PayableBasisRef ?? string.Empty,
            request.UpstreamFinalityRef ?? string.Empty,
            request.CurrencyCode ?? string.Empty,
            request.PayableAmountMinorUnits,
            request.DiscountReferences?.Select(MapDiscountReference).ToArray(),
            request.ReferenceContext);
    }

    private static FiscalDiscountReferenceInput MapDiscountReference(FiscalDiscountReferenceRequest request) =>
        new(
            request.DiscountValidationRef ?? string.Empty,
            MapDiscountStatus(request.Status),
            request.AppliesStatutoryDiscountTreatment,
            request.ReferenceContext);

    private static FiscalDiscountReferenceStatus MapDiscountStatus(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "approved" => FiscalDiscountReferenceStatus.Approved,
            "pending" => FiscalDiscountReferenceStatus.Pending,
            "rejected" => FiscalDiscountReferenceStatus.Rejected,
            "expired" => FiscalDiscountReferenceStatus.Expired,
            "unresolved" => FiscalDiscountReferenceStatus.Unresolved,
            "inconsistent" => FiscalDiscountReferenceStatus.Inconsistent,
            _ => FiscalDiscountReferenceStatus.None
        };

    private static string ToResponseCode(FiscalDocumentCreationErrorCode errorCode) =>
        errorCode switch
        {
            FiscalDocumentCreationErrorCode.MissingPayableBasis => "missing_payable_basis",
            FiscalDocumentCreationErrorCode.MissingUpstreamFinalityReference => "missing_upstream_finality_reference",
            FiscalDocumentCreationErrorCode.UnapprovedDiscountReference => "unapproved_discount_reference",
            FiscalDocumentCreationErrorCode.SensitiveEvidencePayloadNotAllowed => "sensitive_evidence_payload_not_allowed",
            FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest => "unsupported_fiscal_document_request",
            _ => "fiscal_document_creation_failed"
        };
}
