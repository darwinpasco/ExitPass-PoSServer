using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentReadEndpoint
{
    public static async Task<GetFiscalDocumentResponse> GetByIdAsync(
        Guid fiscalDocumentId,
        FiscalDocumentReadService service,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await service.GetByIdAsync(fiscalDocumentId, cancellationToken).ConfigureAwait(false);
            return MapResult(result);
        }
        catch (FiscalDocumentPersistenceNotConfiguredException ex)
        {
            return new GetFiscalDocumentResponse(
                false,
                "persistence_not_configured",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentInvalidPersistenceConfigurationException ex)
        {
            return new GetFiscalDocumentResponse(
                false,
                "invalid_persistence_configuration",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FiscalDocumentPersistenceException ex)
        {
            return new GetFiscalDocumentResponse(
                false,
                "persistence_read_failed",
                ex.Message,
                FiscalNumberAssignmentState: "not_assigned",
                HttpStatusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static GetFiscalDocumentResponse MapResult(FiscalDocumentReadResult result)
    {
        if (result.Succeeded)
        {
            var numberingAssigned = HasCompleteFiscalNumbering(result.Document);
            return new GetFiscalDocumentResponse(
                true,
                "found",
                result.Message,
                result.Document,
                FiscalIssuanceEvidenceStatus: numberingAssigned ? "fiscal_document_number_assigned" : null,
                FiscalNumberAssignmentState: numberingAssigned ? "assigned" : "not_assigned",
                FiscalDocumentStatusCodeId: result.Document?.FiscalDocumentStatusCodeId,
                HttpStatusCode: StatusCodes.Status200OK);
        }

        return new GetFiscalDocumentResponse(
            false,
            result.ErrorCode == FiscalDocumentReadErrorCode.NotFound ? "fiscal_document_not_found" : "fiscal_document_read_failed",
            result.Message,
            FiscalNumberAssignmentState: "not_assigned",
            HttpStatusCode: result.ErrorCode == FiscalDocumentReadErrorCode.NotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest);
    }

    private static bool HasCompleteFiscalNumbering(FiscalDocumentReadModel? document) =>
        document is not null &&
        document.FiscalIdentityId is not null &&
        document.FiscalSequencePolicyId is not null &&
        document.FiscalSequenceValue is not null &&
        !string.IsNullOrWhiteSpace(document.FiscalDocumentNumber) &&
        !string.IsNullOrWhiteSpace(document.FiscalSeries) &&
        document.FiscalNumberAssignedAt is not null &&
        !string.IsNullOrWhiteSpace(document.FiscalNumberAssignedByRef);
}
