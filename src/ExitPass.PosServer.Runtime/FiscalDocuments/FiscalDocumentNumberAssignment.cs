namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentNumberAssignment(
    Guid FiscalSequencePolicyId,
    long FiscalSequenceValue,
    string FiscalDocumentNumber,
    string FiscalSeries,
    string? FiscalNumberPrefixText,
    string? FiscalNumberSuffixText,
    DateTimeOffset FiscalNumberAssignedAt,
    string FiscalNumberAssignedByRef);
