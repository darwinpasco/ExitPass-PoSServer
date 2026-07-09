namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalDocumentVoidIdempotency(
    string Scope,
    string Key,
    string SemanticRequestHash);
