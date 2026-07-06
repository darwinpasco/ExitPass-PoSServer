using System.Text.Json.Serialization;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record GetDigitalSalesInvoicePresentationResponse(
    bool Succeeded,
    string Code,
    string Message,
    DigitalSalesInvoiceTemplateContractModel? TemplateContract = null,
    DigitalSalesInvoicePresentationModel? Presentation = null,
    string? FiscalNumberAssignmentState = null,
    Guid? FiscalDocumentStatusCodeId = null,
    [property: JsonIgnore] int HttpStatusCode = StatusCodes.Status200OK);
