namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed record VoidFiscalDocumentResponse(
    bool Succeeded,
    string Code,
    string Message,
    Guid? FiscalDocumentId = null,
    string? FiscalDocumentNumber = null,
    long? FiscalSequenceValue = null,
    string? FiscalDocumentStatus = null,
    string? VoidStatus = null,
    DateTimeOffset? VoidedAt = null,
    string? VoidReasonCode = null,
    string? VoidReasonText = null,
    string? RequestedByRef = null,
    string? IdempotencyKey = null,
    string? ResultClassification = null,
    string? CorrelationId = null,
    string? ErrorPosture = null,
    int HttpStatusCode = StatusCodes.Status200OK);
