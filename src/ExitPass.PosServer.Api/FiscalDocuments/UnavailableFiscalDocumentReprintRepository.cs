using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class UnavailableFiscalDocumentReprintRepository : IFiscalDocumentReprintRepository
{
    public Task<FiscalDocumentReprintResult> RecordAsync(
        FiscalDocumentReprintCommand command,
        string semanticRequestHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new FiscalDocumentReprintResult(
            FiscalDocumentReprintOutcome.PersistenceFailure,
            SafeMessage: "Fiscal reprint persistence is unavailable."));
}
