using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

internal sealed class UnavailableFiscalXReadingRepository : IFiscalXReadingRepository
{
    private static readonly FiscalXReadingResult Failure = new(FiscalXReadingOutcome.PersistenceFailure, SafeMessage: "POS Server PostgreSQL persistence is not configured for fiscal reporting.");
    public Task<FiscalXReadingResult> GenerateAsync(FiscalXReadingCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Failure);
    public Task<FiscalXReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) => Task.FromResult(Failure);
}
