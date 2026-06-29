using System.Text.Json.Serialization;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record CreateFiscalDocumentResponse(
    bool Succeeded,
    string Code,
    string Message,
    Guid? FiscalDocumentId = null,
    [property: JsonIgnore] int HttpStatusCode = StatusCodes.Status400BadRequest);
