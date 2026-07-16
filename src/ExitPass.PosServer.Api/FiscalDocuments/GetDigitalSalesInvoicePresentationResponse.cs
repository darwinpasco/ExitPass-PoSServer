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
    string? FiscalDocumentStatus = null,
    Guid? FiscalDocumentTypeCodeId = null,
    string? FiscalDocumentType = null,
    Guid? FiscalDocumentId = null,
    string? FiscalDocumentNumber = null,
    string? FiscalSeries = null,
    string? FiscalNumberPrefixText = null,
    string? FiscalNumberSuffixText = null,
    DateTimeOffset? FiscalNumberAssignedAt = null,
    DateTimeOffset? RecordedAt = null,
    string? VoidStatus = null,
    string? VoidReasonCode = null,
    DateTimeOffset? VoidedAt = null,
    string? PresentationVersion = null,
    string? TemplateVersion = null,
    string? ContentType = null,
    [property: JsonIgnore] int HttpStatusCode = StatusCodes.Status200OK);
