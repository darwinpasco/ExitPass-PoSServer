namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentLinkInput(
    Guid TargetFiscalDocumentId,
    Guid LinkTypeCodeId,
    Guid? LinkReasonCodeId = null,
    string? LinkReasonText = null,
    string? CreatedByRef = null);
