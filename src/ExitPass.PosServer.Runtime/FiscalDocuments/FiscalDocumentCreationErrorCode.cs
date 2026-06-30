namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public enum FiscalDocumentCreationErrorCode
{
    None = 0,
    MissingPayableBasis = 1,
    MissingUpstreamFinalityReference = 2,
    UnapprovedDiscountReference = 3,
    SensitiveEvidencePayloadNotAllowed = 4,
    UnsupportedFiscalDocumentRequest = 5,
    MissingFiscalTender = 6,
    InvalidFiscalTender = 7,
    SensitiveTenderPayloadNotAllowed = 8,
    InvalidFiscalTaxDetail = 9,
    SensitiveTaxDetailPayloadNotAllowed = 10
}
