using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed class UnavailableFiscalZCloseStateRepository : IFiscalZCloseStateRepository
{
    public Task<FiscalZCloseStateInitializationResult> InitializeAsync(
        InitializeFiscalZCloseStateCommand command,
        string semanticRequestHash,
        CancellationToken cancellationToken = default) =>
        throw new FiscalDocumentPersistenceNotConfiguredException();
}
