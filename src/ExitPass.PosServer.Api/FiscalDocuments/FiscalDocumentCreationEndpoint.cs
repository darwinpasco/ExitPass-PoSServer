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
                FiscalNumberAssignmentState: "not_assigned",
                ErrorPosture: "retry_after_configuration_correction",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentInvalidPersistenceConfigurationException ex)
        {
            return new CreateFiscalDocumentResponse(
                false,
                "invalid_persistence_configuration",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                ErrorPosture: "retry_after_configuration_correction",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentPersistenceException ex)
        {
            return new CreateFiscalDocumentResponse(
                false,
                "persistence_write_failed",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                ErrorPosture: "retry_after_service_recovery",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static FiscalDocumentCreationCommand MapToCommand(CreateFiscalDocumentRequest request) =>
        new(
            request.SitePosServerRef ?? string.Empty,
            request.FiscalDocumentTypeCodeKey ?? string.Empty,
            MapPayableBasis(request.PayableBasis, request.UpstreamFinalityRef),
            request.SitePosServerId,
            request.SiteId,
            request.ChannelTerminalId,
            request.RuntimeTerminalRef,
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
            request.TaxDetails?.Select(MapTaxDetail).ToArray(),
            request.DiscountPrivilegeDetails?.Select(MapDiscountPrivilegeDetail).ToArray(),
            request.Totals?.Select(MapTotal).ToArray(),
            request.ReferenceContext);

    public static CreateFiscalDocumentResponse MapResult(FiscalDocumentCreationResult result)
    {
        if (result.Succeeded)
        {
            var numberingAssigned = HasCompleteFiscalNumbering(result.Draft);
            if (!numberingAssigned)
            {
                return new CreateFiscalDocumentResponse(
                    false,
                    "fiscal_number_assignment_incomplete",
                    "Fiscal document creation did not return complete fiscal numbering evidence.",
                    ResultClassification: result.Replayed ? "idempotent_replay" : "newly_created",
                    FiscalDocumentId: result.Draft?.FiscalDocumentId,
                    FiscalNumberAssignmentState: "not_assigned",
                    FiscalDocumentStatusCodeId: result.Draft?.FiscalDocumentStatusCodeId,
                    ErrorPosture: "retry_after_service_recovery",
                    HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return new CreateFiscalDocumentResponse(
                true,
                "accepted",
                result.Message,
                FiscalDocumentId: result.Draft?.FiscalDocumentId,
                ResultClassification: result.Replayed ? "idempotent_replay" : "newly_created",
                FiscalIssuanceEvidenceStatus: "fiscal_document_number_assigned",
                FiscalNumberAssignmentState: "assigned",
                FiscalIdentityId: result.Draft?.ResolvedFiscalIdentityId,
                FiscalDocumentStatusCodeId: result.Draft?.FiscalDocumentStatusCodeId,
                FiscalSequencePolicyId: result.Draft?.ResolvedFiscalSequencePolicyId,
                FiscalSequenceValue: result.Draft?.FiscalSequenceValue,
                FiscalDocumentNumber: result.Draft?.FiscalDocumentNumber,
                FiscalSeries: result.Draft?.FiscalSeries,
                FiscalNumberPrefixText: result.Draft?.FiscalNumberPrefixText,
                FiscalNumberSuffixText: result.Draft?.FiscalNumberSuffixText,
                FiscalNumberAssignedAt: result.Draft?.FiscalNumberAssignedAt,
                FiscalNumberAssignedByRef: result.Draft?.FiscalNumberAssignedByRef,
                HttpStatusCode: StatusCodes.Status202Accepted);
        }

        return new CreateFiscalDocumentResponse(
            false,
            ToResponseCode(result.ErrorCode),
            result.Message,
            FiscalNumberAssignmentState: "not_assigned",
            ErrorPosture: ToErrorPosture(result.ErrorCode),
            HttpStatusCode: ToHttpStatusCode(result.ErrorCode));
    }

    private static bool HasCompleteFiscalNumbering(FiscalDocumentDraft? draft) =>
        draft is not null &&
        draft.ResolvedFiscalIdentityId is not null &&
        draft.ResolvedFiscalSequencePolicyId is not null &&
        draft.FiscalSequenceValue is not null &&
        !string.IsNullOrWhiteSpace(draft.FiscalDocumentNumber) &&
        !string.IsNullOrWhiteSpace(draft.FiscalSeries) &&
        draft.FiscalNumberAssignedAt is not null &&
        !string.IsNullOrWhiteSpace(draft.FiscalNumberAssignedByRef);

    private static int ToHttpStatusCode(FiscalDocumentCreationErrorCode errorCode) =>
        errorCode == FiscalDocumentCreationErrorCode.IdempotencyConflict
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

    private static string ToErrorPosture(FiscalDocumentCreationErrorCode errorCode) =>
        errorCode switch
        {
            FiscalDocumentCreationErrorCode.IdempotencyConflict => "do_not_retry_without_request_change",
            FiscalDocumentCreationErrorCode.FiscalIdentityNotFound or
            FiscalDocumentCreationErrorCode.FiscalIdentityAmbiguous or
            FiscalDocumentCreationErrorCode.FiscalIdentityNotEffective or
            FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotFound or
            FiscalDocumentCreationErrorCode.FiscalSequencePolicyAmbiguous or
            FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotEffective or
            FiscalDocumentCreationErrorCode.FiscalSequenceStateNotFound or
            FiscalDocumentCreationErrorCode.FiscalSequenceStateNotEffective or
            FiscalDocumentCreationErrorCode.FiscalNumberAllocationFailed or
            FiscalDocumentCreationErrorCode.FiscalDocumentNumberFormatFailed or
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileNotFound or
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileAmbiguous or
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileIncomplete or
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileUnsupportedVersion => "retry_after_configuration_correction",
            _ => "do_not_retry_without_request_change"
        };

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

    private static FiscalTaxDetailInput MapTaxDetail(CreateFiscalTaxDetailRequest request) =>
        new(
            request.TaxTypeCodeId ?? Guid.Empty,
            request.TaxClassificationCodeId ?? Guid.Empty,
            request.TaxableAmountMinorUnits,
            request.TaxAmountMinorUnits,
            request.CurrencyCode ?? string.Empty,
            request.LineSequence,
            request.TaxRate,
            request.TaxContext);

    private static FiscalDiscountPrivilegeDetailInput MapDiscountPrivilegeDetail(
        CreateFiscalDiscountPrivilegeDetailRequest request) =>
        new(
            request.DiscountPrivilegeTypeCodeId ?? Guid.Empty,
            request.BasisAmountMinorUnits,
            request.DiscountAmountMinorUnits,
            request.VatPrivilegeAmountMinorUnits,
            request.CurrencyCode ?? string.Empty,
            request.LineSequence,
            request.BeneficiaryRef,
            request.EvidenceRef,
            request.ApprovalRef,
            request.DiscountPrivilegeContext);

    private static FiscalTotalInput MapTotal(CreateFiscalTotalRequest request) =>
        new(
            request.TotalTypeCodeId ?? Guid.Empty,
            request.AmountMinorUnits,
            request.CurrencyCode ?? string.Empty,
            request.TotalContext);

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
            FiscalDocumentCreationErrorCode.InvalidFiscalTaxDetail => "invalid_fiscal_tax_detail",
            FiscalDocumentCreationErrorCode.SensitiveTaxDetailPayloadNotAllowed => "sensitive_tax_detail_payload_not_allowed",
            FiscalDocumentCreationErrorCode.InvalidFiscalDiscountPrivilegeDetail => "invalid_fiscal_discount_privilege_detail",
            FiscalDocumentCreationErrorCode.SensitiveDiscountPrivilegePayloadNotAllowed => "sensitive_discount_privilege_payload_not_allowed",
            FiscalDocumentCreationErrorCode.InvalidFiscalTotal => "invalid_fiscal_total",
            FiscalDocumentCreationErrorCode.SensitiveTotalPayloadNotAllowed => "sensitive_total_payload_not_allowed",
            FiscalDocumentCreationErrorCode.IdempotencyConflict => "fiscal_document_idempotency_conflict",
            FiscalDocumentCreationErrorCode.FiscalIdentityNotFound => "fiscal_identity_not_found",
            FiscalDocumentCreationErrorCode.FiscalIdentityAmbiguous => "fiscal_identity_ambiguous",
            FiscalDocumentCreationErrorCode.FiscalIdentityNotEffective => "fiscal_identity_not_effective",
            FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotFound => "fiscal_sequence_policy_not_found",
            FiscalDocumentCreationErrorCode.FiscalSequencePolicyAmbiguous => "fiscal_sequence_policy_ambiguous",
            FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotEffective => "fiscal_sequence_policy_not_effective",
            FiscalDocumentCreationErrorCode.FiscalSequenceStateNotFound => "fiscal_sequence_state_not_found",
            FiscalDocumentCreationErrorCode.FiscalSequenceStateNotEffective => "fiscal_sequence_state_not_effective",
            FiscalDocumentCreationErrorCode.FiscalNumberAllocationFailed => "fiscal_number_allocation_failed",
            FiscalDocumentCreationErrorCode.FiscalDocumentNumberFormatFailed => "fiscal_document_number_format_failed",
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileNotFound => "sales_invoice_header_profile_not_found",
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileAmbiguous => "sales_invoice_header_profile_ambiguous",
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileIncomplete => "sales_invoice_header_profile_incomplete",
            FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileUnsupportedVersion => "sales_invoice_header_profile_unsupported_version",
            _ => "fiscal_document_creation_failed"
        };
}
