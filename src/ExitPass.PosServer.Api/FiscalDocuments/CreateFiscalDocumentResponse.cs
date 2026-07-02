using System.Text.Json.Serialization;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record CreateFiscalDocumentResponse(
    bool Succeeded,
    string Code,
    string Message,
    Guid? FiscalDocumentId = null,
    string? ResultClassification = null,
    string? FiscalIssuanceEvidenceStatus = null,
    string? FiscalNumberAssignmentState = null,
    Guid? FiscalIdentityId = null,
    Guid? FiscalDocumentStatusCodeId = null,
    Guid? FiscalSequencePolicyId = null,
    long? FiscalSequenceValue = null,
    string? FiscalDocumentNumber = null,
    string? FiscalSeries = null,
    string? FiscalNumberPrefixText = null,
    string? FiscalNumberSuffixText = null,
    DateTimeOffset? FiscalNumberAssignedAt = null,
    string? FiscalNumberAssignedByRef = null,
    string? ErrorPosture = null,
    [property: JsonIgnore] int HttpStatusCode = StatusCodes.Status400BadRequest);
