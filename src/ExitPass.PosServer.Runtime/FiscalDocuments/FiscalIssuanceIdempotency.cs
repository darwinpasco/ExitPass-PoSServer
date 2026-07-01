namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalIssuanceIdempotency(
    string Scope,
    string Key,
    string SemanticRequestHash);
