using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

internal sealed class UnavailableFiscalZReadingRepository : IFiscalZReadingRepository
{
    private static readonly FiscalZReadingResult Failure = new(
        FiscalZReadingOutcome.PersistenceFailure,
        SafeMessage: "POS Server PostgreSQL persistence is not configured for Z Reading.");

    public Task<FiscalZReadingResult> CloseAsync(FiscalZReadingCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Failure);
    public Task<FiscalZReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) => Task.FromResult(Failure);
}
