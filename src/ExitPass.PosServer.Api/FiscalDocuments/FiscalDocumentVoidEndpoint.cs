using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentVoidEndpoint
{
    public static async Task<VoidFiscalDocumentResponse> VoidAsync(
        Guid fiscalDocumentId,
        VoidFiscalDocumentRequest request,
        FiscalDocumentVoidService service,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await service
                .VoidAsync(MapToCommand(fiscalDocumentId, request), cancellationToken)
                .ConfigureAwait(false);

            return MapResult(result);
        }
        catch (FiscalDocumentPersistenceNotConfiguredException ex)
        {
            return new VoidFiscalDocumentResponse(
                false,
                "persistence_not_configured",
                ex.Message,
                ResultClassification: "rejected",
                ErrorPosture: "retry_after_configuration_correction",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentInvalidPersistenceConfigurationException ex)
        {
            return new VoidFiscalDocumentResponse(
                false,
                "invalid_persistence_configuration",
                ex.Message,
                ResultClassification: "rejected",
                ErrorPosture: "retry_after_configuration_correction",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentPersistenceException ex)
        {
            return new VoidFiscalDocumentResponse(
                false,
                "persistence_write_failed",
                ex.Message,
                ResultClassification: "rejected",
                ErrorPosture: "retry_after_service_recovery",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static FiscalDocumentVoidCommand MapToCommand(
        Guid fiscalDocumentId,
        VoidFiscalDocumentRequest request) =>
        new(
            fiscalDocumentId,
            request.IdempotencyKey ?? string.Empty,
            request.ReasonCode ?? string.Empty,
            request.ReasonText,
            request.RequestedByRef ?? string.Empty,
            request.RequestedAt ?? DateTimeOffset.UtcNow,
            request.CorrelationId ?? string.Empty,
            request.SourceSystemRef,
            request.BusinessDayDate);

    public static VoidFiscalDocumentResponse MapResult(FiscalDocumentVoidResult result)
    {
        if (result.Succeeded)
        {
            var record = result.Record!;
            return new VoidFiscalDocumentResponse(
                true,
                "accepted",
                result.Message,
                record.FiscalDocumentId,
                record.FiscalDocumentNumber,
                record.FiscalSequenceValue,
                record.FiscalDocumentStatus,
                record.VoidStatus,
                record.VoidedAt,
                record.VoidReasonCode,
                record.VoidReasonText,
                record.RequestedByRef,
                record.IdempotencyKey,
                result.ResultClassification,
                record.CorrelationId,
                HttpStatusCode: StatusCodes.Status200OK);
        }

        return new VoidFiscalDocumentResponse(
            false,
            ToResponseCode(result.ErrorCode),
            result.Message,
            ResultClassification: result.ResultClassification,
            ErrorPosture: ToErrorPosture(result.ErrorCode),
            HttpStatusCode: ToHttpStatusCode(result.ErrorCode));
    }

    private static int ToHttpStatusCode(FiscalDocumentVoidErrorCode errorCode) =>
        errorCode switch
        {
            FiscalDocumentVoidErrorCode.FiscalDocumentNotFound => StatusCodes.Status404NotFound,
            FiscalDocumentVoidErrorCode.IdempotencyConflict or
            FiscalDocumentVoidErrorCode.InvalidStateTransition => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

    private static string ToErrorPosture(FiscalDocumentVoidErrorCode errorCode) =>
        errorCode switch
        {
            FiscalDocumentVoidErrorCode.IdempotencyConflict or
            FiscalDocumentVoidErrorCode.InvalidStateTransition => "do_not_retry_without_request_change",
            FiscalDocumentVoidErrorCode.FiscalDocumentNotFound => "verify_fiscal_document_reference_before_retry",
            _ => "do_not_retry_without_request_change"
        };

    private static string ToResponseCode(FiscalDocumentVoidErrorCode errorCode) =>
        errorCode switch
        {
            FiscalDocumentVoidErrorCode.MissingIdempotencyKey => "missing_idempotency_key",
            FiscalDocumentVoidErrorCode.MissingReasonCode => "missing_reason_code",
            FiscalDocumentVoidErrorCode.InvalidReasonCode => "invalid_reason_code",
            FiscalDocumentVoidErrorCode.MissingRequestedByRef => "missing_requested_by_ref",
            FiscalDocumentVoidErrorCode.MissingCorrelationId => "missing_correlation_id",
            FiscalDocumentVoidErrorCode.IdempotencyConflict => "fiscal_document_void_idempotency_conflict",
            FiscalDocumentVoidErrorCode.FiscalDocumentNotFound => "fiscal_document_not_found",
            FiscalDocumentVoidErrorCode.InvalidStateTransition => "invalid_fiscal_document_void_state_transition",
            _ => "fiscal_document_void_rejected"
        };
}
