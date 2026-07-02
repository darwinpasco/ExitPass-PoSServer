using System.Text.Json.Serialization;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record GetFiscalDocumentResponse(
    bool Succeeded,
    string Code,
    string Message,
    FiscalDocumentReadModel? Document = null,
    string? FiscalIssuanceEvidenceStatus = null,
    string? FiscalNumberAssignmentState = null,
    Guid? FiscalDocumentStatusCodeId = null,
    [property: JsonIgnore] int HttpStatusCode = StatusCodes.Status200OK);
