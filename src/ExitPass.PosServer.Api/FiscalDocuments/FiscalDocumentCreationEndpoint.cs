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
        catch (FiscalDocumentInvalidPersistenceConfigurationException ex)
        {
            return new CreateFiscalDocumentResponse(
                false,
                "invalid_persistence_configuration",
                ex.Message,
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentPersistenceException ex)
        {
            return new CreateFiscalDocumentResponse(
                false,
                "persistence_write_failed",
                ex.Message,
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static FiscalDocumentCreationCommand MapToCommand(CreateFiscalDocumentRequest request) =>
        new(
            request.SitePosServerRef ?? string.Empty,
            request.FiscalDocumentTypeCodeKey ?? string.Empty,
            MapPayableBasis(request.PayableBasis, request.UpstreamFinalityRef),
            request.SitePosServerId,
            request.ChannelTerminalId,
            request.FiscalDocumentTypeCodeId,
            request.FiscalDocumentStatusCodeId,
            request.BusinessDayDate,
            request.CentralPmsParkingSessionRef,
            request.CentralPmsPaymentAttemptRef,
            request.CentralPmsPaymentConfirmationRef,
            request.PaymentFinalityRef,
            request.VendorAckRef,
            request.DocumentLinks?.Select(MapDocumentLink).ToArray(),
            (request.DocumentLines ?? request.Lines)?.Select(MapDocumentLine).ToArray(),
            request.Tenders?.Select(MapTender).ToArray(),
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

    private static FiscalizationPayableBasisInput? MapPayableBasis(
        FiscalizationPayableBasisRequest? request,
        string? upstreamFinalityRef)
    {
        if (request is null)
        {
            return null;
        }

        var mappedUpstreamFinalityRef = string.IsNullOrWhiteSpace(request.UpstreamFinalityRef)
            ? upstreamFinalityRef
            : request.UpstreamFinalityRef;

        return new FiscalizationPayableBasisInput(
            request.PayableBasisRef ?? string.Empty,
            mappedUpstreamFinalityRef ?? string.Empty,
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

    private static FiscalDocumentLinkInput MapDocumentLink(FiscalDocumentLinkRequest request) =>
        new(
            request.TargetFiscalDocumentId ?? Guid.Empty,
            request.LinkTypeCodeId ?? Guid.Empty,
            request.LinkReasonCodeId,
            request.LinkReasonText,
            request.CreatedByRef);

    private static FiscalDocumentLineInput MapDocumentLine(CreateFiscalDocumentLineRequest request) =>
        new(
            request.LineSequence,
            request.LineTypeCodeId ?? Guid.Empty,
            request.Description ?? string.Empty,
            request.Quantity,
            request.UnitAmountMinorUnits,
            request.GrossAmountMinorUnits,
            request.DiscountAmountMinorUnits,
            request.TaxAmountMinorUnits,
            request.NetAmountMinorUnits,
            request.CurrencyCode ?? string.Empty,
            request.LineStatusCodeId,
            request.SourceRef,
            request.LineContext);

    private static FiscalTenderInput MapTender(CreateFiscalTenderRequest request) =>
        new(
            request.TenderTypeCodeId ?? Guid.Empty,
            request.AmountMinorUnits,
            request.CurrencyCode ?? string.Empty,
            request.CentralPmsPaymentAttemptRef,
            request.CentralPmsPaymentConfirmationRef,
            request.PaymentFinalityRef,
            request.ProviderRef,
            request.TenderContext);

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
            FiscalDocumentCreationErrorCode.MissingFiscalTender => "missing_fiscal_tender",
            FiscalDocumentCreationErrorCode.InvalidFiscalTender => "invalid_fiscal_tender",
            FiscalDocumentCreationErrorCode.SensitiveTenderPayloadNotAllowed => "sensitive_tender_payload_not_allowed",
            _ => "fiscal_document_creation_failed"
        };
}
