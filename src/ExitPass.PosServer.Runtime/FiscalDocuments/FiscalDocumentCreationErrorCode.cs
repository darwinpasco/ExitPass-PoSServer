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
    SensitiveTaxDetailPayloadNotAllowed = 10,
    InvalidFiscalDiscountPrivilegeDetail = 11,
    SensitiveDiscountPrivilegePayloadNotAllowed = 12,
    InvalidFiscalTotal = 13,
    SensitiveTotalPayloadNotAllowed = 14,
    IdempotencyConflict = 15,
    FiscalIdentityNotFound = 16,
    FiscalIdentityAmbiguous = 17,
    FiscalIdentityNotEffective = 18,
    FiscalSequencePolicyNotFound = 19,
    FiscalSequencePolicyAmbiguous = 20,
    FiscalSequencePolicyNotEffective = 21,
    FiscalSequenceStateNotFound = 22,
    FiscalSequenceStateNotEffective = 23,
    FiscalNumberAllocationFailed = 24,
    FiscalDocumentNumberFormatFailed = 25
}
