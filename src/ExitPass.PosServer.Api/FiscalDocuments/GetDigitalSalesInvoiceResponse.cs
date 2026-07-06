using System.Text.Json.Serialization;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record GetDigitalSalesInvoiceResponse(
    bool Succeeded,
    string Code,
    string Message,
    DigitalSalesInvoiceRenderModel? Render = null,
    string? FiscalNumberAssignmentState = null,
    Guid? FiscalDocumentStatusCodeId = null,
    [property: JsonIgnore] int HttpStatusCode = StatusCodes.Status200OK);
