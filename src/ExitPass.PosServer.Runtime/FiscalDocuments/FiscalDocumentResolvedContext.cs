namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentResolvedContext(
    Guid FiscalIdentityId,
    Guid FiscalSequencePolicyId,
    string PolicyCode,
    string? PrefixText,
    string? SuffixText,
    int? PaddingLength);
